using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Commands;

public sealed record ConsumePartCommand(PartSku Sku, Quantity Quantity, string Justification) : ICommand
{
    public static Result<ConsumePartCommand> Create(string sku, int quantity, string justification)
    {
        var skuResult         = PartSku.Create(sku);
        var quantityResult    = Quantity.Create(quantity);
        var justificationResult = string.IsNullOrWhiteSpace(justification)
            ? Result.Fail("justification", "Justification is required")
            : Result.Ok();

        var combined = Result.Combine(skuResult, quantityResult, justificationResult);
        if (combined.IsFailure)
            return Result.Fail<ConsumePartCommand>(combined.Errors);

        return Result.Ok(new ConsumePartCommand(
            skuResult.Value,
            quantityResult.Value,
            justification.Trim()
        ));
    }
}

public class ConsumePartCommandHandler(IAggregateRepository<PartAggregate> partAggregateRepository)
    : ICommandHandler<ConsumePartCommand, Result>
{
    public async Task<Result> HandleAsync(ConsumePartCommand command, CancellationToken cancellationToken)
    {
        var partResult = await partAggregateRepository.GetByIdAsync(command.Sku.Value, cancellationToken);
        if (!partResult.HasValue)
            return Result.Fail($"Part with SKU '{command.Sku.Value}' does not exist.");

        var consumeResult = partResult.Value.Consume(command.Quantity, command.Justification);
        if (consumeResult.IsFailure)
            return Result.Fail(consumeResult.Errors);

        await partAggregateRepository.SaveAsync(consumeResult.Value, cancellationToken);

        return Result.Ok();
    }
}