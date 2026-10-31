using Catalog.Domain.Products;
using FluentValidation;

namespace Catalog.Application.Products;

/// <summary>
/// Regras de entrada compartilhadas entre criar e editar (evita duplicação nos validadores).
/// Os limites vêm do domínio: a regra tem uma fonte só.
/// </summary>
internal static class ProductRules
{
    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(Product.NameMaxLength);

    public static IRuleBuilderOptions<T, string?> ValidDescription<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(Product.DescriptionMaxLength);

    public static IRuleBuilderOptions<T, decimal> ValidPrice<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
}
