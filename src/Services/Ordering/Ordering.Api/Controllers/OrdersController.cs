using BuildingBlocks.Api.Controllers;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.Api.Contracts;
using Ordering.Application.Orders;
using Ordering.Application.Orders.Cancel;
using Ordering.Application.Orders.Create;
using Ordering.Application.Orders.Queries;

namespace Ordering.Api.Controllers;

// Todo o serviço de pedidos exige login; quem pode ver ou cancelar cada pedido
// é decidido na Application (OrderAccess): o dono ou o administrador.
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ApiController
{
    [HttpGet]
    [ProducesResponseType<PagedResult<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromServices] IQueryHandler<ListOrdersQuery, PagedResult<OrderSummaryResponse>> handler,
        [FromQuery] string? customerEmail,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(new ListOrdersQuery(customerEmail, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetOrderByIdQuery, OrderResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetOrderByIdQuery(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(
        CreateOrderRequest request,
        [FromServices] ICommandHandler<CreateOrderCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.ShippingMethod,
            (request.Items ?? []).Select(item => new CreateOrderItem(item.ProductId, item.Quantity)).ToList());

        var result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
            : Problem(result.Error);
    }

    /// <summary>
    /// Cancelar é uma ação de negócio, não uma edição genérica: vira um endpoint próprio.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] ICommandHandler<CancelOrderCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelOrderCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}
