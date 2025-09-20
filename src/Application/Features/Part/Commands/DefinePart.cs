using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Commands;

public sealed record DefinePartCommand(PartSku Sku, PartName Name) : ICommand
{
    public static Result<DefinePartCommand> Create(string sku, string name)
    {
        var skuResult = PartSku.Create(sku);
        var nameResult = PartName.Create(name);

        var combined = Result.Combine(skuResult, nameResult);

        if (combined.IsFailure)
            return Result.Fail<DefinePartCommand>(combined.Errors);

        return Result.Ok(new DefinePartCommand(
            skuResult.Value,
            nameResult.Value
        ));
    }
}

public class DefinePartCommandHandler(IAggregateRepository<PartAggregate> partAggregateRepository)
    : ICommandHandler<DefinePartCommand, Result>
{
    public async Task<Result> HandleAsync(DefinePartCommand command, CancellationToken cancellationToken)
    {
        var existingPart = await partAggregateRepository.GetByIdAsync(command.Sku.Value, cancellationToken);
        if (existingPart.HasValue)
            return Result.Fail($"Part with SKU '{command.Sku.Value}' already exists.");

        var defineResult = PartAggregate.Define(command.Sku, command.Name);
        if (defineResult.IsFailure)
            return Result.Fail(defineResult.Errors);

        await partAggregateRepository.SaveAsync(defineResult.Value, cancellationToken);

        return Result.Ok();
    }
}