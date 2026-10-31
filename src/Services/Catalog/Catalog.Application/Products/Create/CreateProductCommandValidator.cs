using Catalog.Domain.ValueObjects;
using FluentValidation;

namespace Catalog.Application.Products.Create;

/// <summary>
/// Validação de formato da entrada (ADR-18). Roda no ValidationDecorator, antes do handler.
/// Regras de negócio que dependem do banco (SKU repetido, categoria existe) ficam no handler.
/// </summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name).ValidName();
        RuleFor(command => command.Description).ValidDescription();
        RuleFor(command => command.Price).ValidPrice();
        RuleFor(command => command.InitialStock).GreaterThanOrEqualTo(0);
        RuleFor(command => command.CategoryId).NotEmpty();

        RuleFor(command => command.Sku)
            .Must(Sku.IsValid)
            .WithMessage($"SKU must have {Sku.MinLength} to {Sku.MaxLength} characters: letters, digits or hyphen.");
    }
}
