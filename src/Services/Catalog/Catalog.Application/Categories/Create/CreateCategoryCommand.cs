using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain.Categories;
using FluentValidation;

namespace Catalog.Application.Categories.Create;

public sealed record CreateCategoryCommand(string Name, string? Description) : ICommand<Guid>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
        RuleFor(command => command.Description).MaximumLength(Category.DescriptionMaxLength);
    }
}

public sealed class CreateCategoryCommandHandler(ICategoryRepository categories, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        if (await categories.NameExistsAsync(name, excludingId: null, cancellationToken))
        {
            return Result.Failure<Guid>(CategoryErrors.NameAlreadyExists(name));
        }

        var category = Category.Create(name, command.Description);

        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(category.Id);
    }
}
