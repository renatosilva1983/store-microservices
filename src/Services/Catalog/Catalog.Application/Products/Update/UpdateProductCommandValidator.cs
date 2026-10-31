using FluentValidation;

namespace Catalog.Application.Products.Update;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).ValidName();
        RuleFor(command => command.Description).ValidDescription();
        RuleFor(command => command.Price).ValidPrice();
        RuleFor(command => command.CategoryId).NotEmpty();
    }
}
