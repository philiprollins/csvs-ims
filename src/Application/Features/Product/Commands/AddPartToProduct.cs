using Application.Features.Product.ValueObjects;
using Library;
using Library.Interfaces;

namespace Application.Features.Product.Commands;

public class AddPartToProductCommand : ICommand
{
    public ProductSku ProductSku { get; set; } = null!;
    public ProductPart ProductPart { get; set; } = null!;

    private AddPartToProductCommand() { }

    public static Result<AddPartToProductCommand> Create(string productSku, string partSku, int quantity)
    {
        var productSkuResult = ProductSku.Create(productSku);
        var productPartResult = ProductPart.Create(partSku, quantity);

        var combined = Result.Combine(productSkuResult, productPartResult);

        if (combined.IsFailure)
            return Result.Fail<AddPartToProductCommand>(combined.Errors);

        return Result.Ok(new AddPartToProductCommand
        {
            ProductSku = productSkuResult.Value,
            ProductPart = productPartResult.Value
        });
    }
}

public class AddPartToProductCommandHandler(IAggregateRepository<ProductAggregate> productAggregateRepository)
    : ICommandHandler<AddPartToProductCommand, Result>
{
    public async Task<Result> HandleAsync(AddPartToProductCommand command, CancellationToken cancellationToken)
    {
        var productResult = await productAggregateRepository.GetByIdAsync(command.ProductSku.Value, cancellationToken);
        if (!productResult.HasValue)
            return Result.Fail($"Product with SKU '{command.ProductSku.Value}' does not exist.");

        var addPartResult = productResult.Value.AddPart(command.ProductPart);
        if (addPartResult.IsFailure)
            return Result.Fail(addPartResult.Errors);

        await productAggregateRepository.SaveAsync(addPartResult.Value, cancellationToken);

        return Result.Ok();
    }
}
