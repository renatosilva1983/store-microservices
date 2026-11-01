using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;

namespace Catalog.Application.Categories.Delete;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand;

/// <summary>
/// Regra de negócio: categoria com produtos não pode ser excluída (409 Conflict).
/// </summary>
public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categories,
    IProductRepository products,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(CategoryErrors.NotFound(command.Id));
        }

        if (await products.AnyInCategoryAsync(command.Id, cancellationToken))
        {
            return Result.Failure(CategoryErrors.HasProducts(command.Id));
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
