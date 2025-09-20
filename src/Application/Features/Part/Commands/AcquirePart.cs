using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Commands;

public sealed record AcquirePartCommand(PartSku Sku, Quantity Quantity, string Justification) : ICommand
{
    public static Result<AcquirePartCommand> Create(string sku, int quantity, string justification)
    {
        var skuResult      = PartSku.Create(sku);
        var quantityResult = Quantity.Create(quantity);
        var justificationResult = string.IsNullOrWhiteSpace(justification)
            ? Result.Fail("justification", "Justification is required")
            : Result.Ok();

        var combined = Result.Combine(skuResult, quantityResult, justificationResult);
        if (combined.IsFailure)
            return Result.Fail<AcquirePartCommand>(combined.Errors);

        return Result.Ok(new AcquirePartCommand(
            skuResult.Value,
            quantityResult.Value,
            justification.Trim()
        ));
    }
}

public class AcquirePartCommandHandler(IAggregateRepository<PartAggregate> partAggregateRepository)
    : ICommandHandler<AcquirePartCommand, Result>
{
    public async Task<Result> HandleAsync(AcquirePartCommand command, CancellationToken cancellationToken)
    {
        var partResult = await partAggregateRepository.GetByIdAsync(command.Sku.Value, cancellationToken);
        if (!partResult.HasValue)
            return Result.Fail($"Part with SKU '{command.Sku.Value}' does not exist.");

        var acquireResult = partResult.Value.Acquire(command.Quantity, command.Justification);
        if (acquireResult.IsFailure)
            return Result.Fail(acquireResult.Errors);

        await partAggregateRepository.SaveAsync(acquireResult.Value, cancellationToken);

        return Result.Ok();
    }
}