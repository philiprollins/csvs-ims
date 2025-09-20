using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Commands;

public class AcquirePartCommand : ICommand
{
    public PartSku Sku { get; set; } = null!;
    public Quantity Quantity { get; set; } = null!;
    public string Justification { get; set; } = string.Empty;

    private AcquirePartCommand() { }

    public static Result<AcquirePartCommand> Create(string sku, int quantity, string justification)
    {
        var skuResult = PartSku.Create(sku);
        var quantityResult = Quantity.Create(quantity);

        var combined = Result.Combine(skuResult, quantityResult);

        if (combined.IsFailure)
            return Result.Fail<AcquirePartCommand>(combined.Errors);

        if (string.IsNullOrWhiteSpace(justification))
            return Result.Fail<AcquirePartCommand>("justification", "Justification is required");

        return Result.Ok(new AcquirePartCommand
        {
            Sku = skuResult.Value,
            Quantity = quantityResult.Value,
            Justification = justification.Trim()
        });
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