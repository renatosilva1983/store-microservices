using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;
using Catalog.Application.Categories;
using Catalog.Domain.Products;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Products.Create;

/// <summary>
/// Caso de uso: orquestra o fluxo. Conhece só as portas (interfaces);
/// as implementações chegam por injeção de dependência.
/// </summary>
public sealed class CreateProductCommandHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (!await categories.ExistsAsync(command.CategoryId, cancellationToken))
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound(command.CategoryId));
        }

        var sku = Sku.Create(command.Sku);
        if (await products.SkuExistsAsync(sku, cancellationToken))
        {
            return Result.Failure<Guid>(ProductErrors.SkuAlreadyExists(sku.Value));
        }

        var product = Product.Create(
            command.Name,
            command.Description,
            sku,
            Money.Create(command.Price),
            command.InitialStock,
            command.CategoryId,
            timeProvider.GetUtcNow().UtcDateTime);

        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(product.Id);
    }
}
