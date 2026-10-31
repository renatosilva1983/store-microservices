using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;
using Catalog.Application.Categories;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Products.Update;

public sealed class UpdateProductCommandHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(command.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure(ProductErrors.NotFound(command.Id));
        }

        var categoryChanged = product.CategoryId != command.CategoryId;
        if (categoryChanged && !await categories.ExistsAsync(command.CategoryId, cancellationToken))
        {
            return Result.Failure(CategoryErrors.NotFound(command.CategoryId));
        }

        product.UpdateDetails(command.Name, command.Description, command.CategoryId);
        product.ChangePrice(Money.Create(command.Price, product.Price.Currency));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
