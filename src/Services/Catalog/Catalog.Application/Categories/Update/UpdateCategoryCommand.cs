using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain.Categories;
using FluentValidation;

namespace Catalog.Application.Categories.Update;

public sealed record UpdateCategoryCommand(Guid Id, string Name, string? Description) : ICommand;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(Category.DescriptionMaxLength);
    }
}

public sealed class UpdateCategoryCommandHandler(ICategoryRepository categories, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCategoryCommand>
{
    public async Task<Result> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(command.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure(CategoryErrors.NotFound(command.Id));
        }

        var name = command.Name.Trim();
        if (await categories.NameExistsAsync(name, excludingId: command.Id, cancellationToken))
        {
            return Result.Failure(CategoryErrors.NameAlreadyExists(name));
        }

        category.Update(name, command.Description);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
