using Application.Features.Part.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Part.Commands;

public class DefinePartCommand : ICommand
{
    public PartSku Sku { get; set; } = null!;
    public PartName Name { get; set; } = null!;

    private DefinePartCommand() { }

    public static Result<DefinePartCommand> Create(string sku, string name)
    {
        var skuResult = PartSku.Create(sku);
        var nameResult = PartName.Create(name);

        var combined = Result.Combine(skuResult, nameResult);

        if (combined.IsFailure)
            return Result.Fail<DefinePartCommand>(combined.Errors);

        return Result.Ok(new DefinePartCommand
        {
            Sku = skuResult.Value,
            Name = nameResult.Value
        });
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