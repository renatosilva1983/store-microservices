using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders;
using Ordering.Domain.Orders;

namespace Ordering.Infrastructure.Persistence.Queries;

/// <summary>
/// Lado de leitura: AsNoTracking. A listagem projeta só as colunas do resumo
/// (sem carregar os itens); o detalhe carrega o pedido com itens e mapeia em memória.
/// </summary>
internal sealed class OrderQueries(OrderingDbContext context) : IOrderQueries
{
    public async Task<OrderResponse?> GetByIdAsync(Guid id, string? customerId, CancellationToken cancellationToken)
    {
        // WHERE id = @id [AND customer_id = @customerId]: o banco só devolve o pedido se for do usuário.
        var order = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Where(o => customerId == null || o.CustomerId == customerId)
            .FirstOrDefaultAsync(cancellationToken);

        return order is null ? null : ToResponse(order);
    }

    public async Task<PagedResult<OrderSummaryResponse>> ListAsync(
        OrderFilter filter,
        CancellationToken cancellationToken)
    {
        var query = context.Orders.AsNoTracking();

        if (filter.CustomerId is not null)
        {
            query = query.Where(order => order.CustomerId == filter.CustomerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.CustomerEmail))
        {
            var email = filter.CustomerEmail.Trim().ToLowerInvariant();
            query = query.Where(order => order.CustomerEmail == email);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(order => new
            {
                order.Id,
                order.CustomerName,
                order.Status,
                order.Total,
                order.Currency,
                ItemCount = order.Items.Count(),
                order.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new OrderSummaryResponse(
                row.Id,
                row.CustomerName,
                row.Status.ToString(),
                row.Total,
                row.Currency,
                row.ItemCount,
                row.CreatedAtUtc))
            .ToList();

        return new PagedResult<OrderSummaryResponse>(items, filter.Page, filter.PageSize, totalCount);
    }

    private static OrderResponse ToResponse(Order order) =>
        new(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            order.Status.ToString(),
            order.ShippingMethod.ToString(),
            order.Subtotal,
            order.ShippingCost,
            order.Total,
            order.Currency,
            order.CreatedAtUtc,
            order.CancelledAtUtc,
            order.Items
                .Select(item => new OrderItemResponse(
                    item.ProductId,
                    item.ProductName,
                    item.Sku,
                    item.UnitPrice,
                    item.Quantity,
                    item.LineTotal))
                .ToList());
}
