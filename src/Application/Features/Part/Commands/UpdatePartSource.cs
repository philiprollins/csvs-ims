using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Commands;

public sealed record UpdatePartSourceCommand(PartSku Sku, PartSource Source) : ICommand
{
    public static Result<UpdatePartSourceCommand> Create(string sku, string sourceName, string sourceUri)
    {
        var skuResult = PartSku.Create(sku);
        var sourceResult = PartSource.Create(sourceName, sourceUri);

        var combined = Result.Combine(skuResult, sourceResult);

        if (combined.IsFailure)
            return Result.Fail<UpdatePartSourceCommand>(combined.Errors);

        return Result.Ok(new UpdatePartSourceCommand(
            skuResult.Value,
            sourceResult.Value
        ));
    }
}

public class UpdatePartSourceCommandHandler(IAggregateRepository<PartAggregate> partAggregateRepository)
    : ICommandHandler<UpdatePartSourceCommand, Result>
{
    public async Task<Result> HandleAsync(UpdatePartSourceCommand command, CancellationToken cancellationToken)
    {
        var partResult = await partAggregateRepository.GetByIdAsync(command.Sku.Value, cancellationToken);
        if (!partResult.HasValue)
            return Result.Fail($"Part with SKU '{command.Sku.Value}' does not exist.");

        var updateResult = partResult.Value.UpdateSource(command.Source);
        if (updateResult.IsFailure)
            return Result.Fail(updateResult.Errors);

        await partAggregateRepository.SaveAsync(updateResult.Value, cancellationToken);

        return Result.Ok();
    }
}