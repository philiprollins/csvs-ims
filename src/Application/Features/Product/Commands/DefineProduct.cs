using Application.Features.Product.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Product.Commands;

public class DefineProductCommand : ICommand
{
    public ProductSku Sku { get; set; }
    public ProductName Name { get; set; }

    private DefineProductCommand(ProductSku sku, ProductName name)
    {
        Sku = sku;
        Name = name;
    }

    public static Result<DefineProductCommand> Create(string sku, string name)
    {
        var skuResult = ProductSku.Create(sku);
        var nameResult = ProductName.Create(name);

        var combined = Result.Combine(skuResult, nameResult);

        if (combined.IsFailure)
            return Result.Fail<DefineProductCommand>(combined.Errors);

        return Result.Ok(new DefineProductCommand(skuResult.Value, nameResult.Value));
    }
}

public class DefineProductCommandHandler(IAggregateRepository<ProductAggregate> productAggregateRepository)
    : ICommandHandler<DefineProductCommand, Result>
{
    public async Task<Result> HandleAsync(DefineProductCommand command, CancellationToken cancellationToken)
    {
        var existingProduct = await productAggregateRepository.GetByIdAsync(command.Sku.Value, cancellationToken);
        if (existingProduct.HasValue)
            return Result.Fail($"Product with SKU '{command.Sku.Value}' already exists.");

        var defineResult = ProductAggregate.Define(command.Sku, command.Name);
        if (defineResult.IsFailure)
            return Result.Fail(defineResult.Errors);

        await productAggregateRepository.SaveAsync(defineResult.Value, cancellationToken);

        return Result.Ok();
    }
}
