using BuildingBlocks.Application.Messaging;
using FluentValidation;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders.Create;

/// <summary>
/// O cliente informa só produto e quantidade. Nome e preço vêm do Catálogo,
/// nunca do cliente (senão qualquer um compraria pelo preço que quisesse).
/// </summary>
public sealed record CreateOrderCommand(
    string CustomerName,
    string CustomerEmail,
    string ShippingMethod,
    IReadOnlyList<CreateOrderItem> Items) : ICommand<Guid>;

public sealed record CreateOrderItem(Guid ProductId, int Quantity);

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public const int MaxDistinctItems = 20;

    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerName).NotEmpty().MaximumLength(Order.CustomerNameMaxLength);
        RuleFor(command => command.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(Order.CustomerEmailMaxLength);

        RuleFor(command => command.ShippingMethod)
            .NotEmpty()
            .IsEnumName(typeof(ShippingMethod), caseSensitive: false)
            .WithMessage($"Shipping method must be one of: {string.Join(", ", Enum.GetNames<ShippingMethod>())}.");

        RuleFor(command => command.Items)
            .NotEmpty()
            .Must(items => items is null || items.Count <= MaxDistinctItems)
            .WithMessage($"An order can have at most {MaxDistinctItems} items.");

        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, Order.MaxQuantityPerItem);
        });
    }
}
