using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Commands;

public sealed record RecountPartCommand(PartSku Sku, Quantity Quantity, string Justification) : ICommand
{
    public static Result<RecountPartCommand> Create(string sku, int quantity, string justification)
    {
        var skuResult = PartSku.Create(sku);
        var quantityResult = Quantity.Create(quantity);

        var combined = Result.Combine(skuResult, quantityResult);

        if (combined.IsFailure)
            return Result.Fail<RecountPartCommand>(combined.Errors);

        if (string.IsNullOrWhiteSpace(justification))
            return Result.Fail<RecountPartCommand>("justification", "Justification is required");

        return Result.Ok(new RecountPartCommand(
            skuResult.Value,
            quantityResult.Value,
            justification.Trim()
        ));
    }
}

public class RecountPartCommandHandler(IAggregateRepository<PartAggregate> partAggregateRepository)
    : ICommandHandler<RecountPartCommand, Result>
{
    public async Task<Result> HandleAsync(RecountPartCommand command, CancellationToken cancellationToken)
    {
        var partResult = await partAggregateRepository.GetByIdAsync(command.Sku.Value, cancellationToken);
        if (!partResult.HasValue)
            return Result.Fail($"Part with SKU '{command.Sku.Value}' does not exist.");

        var recountResult = partResult.Value.Recount(command.Quantity, command.Justification);
        if (recountResult.IsFailure)
            return Result.Fail(recountResult.Errors);

        await partAggregateRepository.SaveAsync(recountResult.Value, cancellationToken);

        return Result.Ok();
    }
}