using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Commands;

public class ConsumePartCommand : ICommand
{
    public PartSku Sku { get; set; }
    public Quantity Quantity { get; set; }
    public string Justification { get; set; }

    private ConsumePartCommand(PartSku sku, Quantity quantity, string justification)
    {
        Sku = sku;
        Quantity = quantity;
        Justification = justification;
    }

    public static Result<ConsumePartCommand> Create(string sku, int quantity, string justification)
    {
        var skuResult = PartSku.Create(sku);
        var quantityResult = Quantity.Create(quantity);

        var combined = Result.Combine(skuResult, quantityResult);

        if (combined.IsFailure)
            return Result.Fail<ConsumePartCommand>(combined.Errors);

        if (string.IsNullOrWhiteSpace(justification))
            return Result.Fail<ConsumePartCommand>("justification", "Justification is required");

        return Result.Ok(new ConsumePartCommand(skuResult.Value, quantityResult.Value, justification.Trim()));
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