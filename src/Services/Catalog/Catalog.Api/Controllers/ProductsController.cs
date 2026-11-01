using BuildingBlocks.Api.Controllers;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Pagination;
using Catalog.Api.Contracts;
using Catalog.Application.Products;
using Catalog.Application.Products.Create;
using Catalog.Application.Products.Delete;
using Catalog.Application.Products.GetById;
using Catalog.Application.Products.List;
using Catalog.Application.Products.Update;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

/// <summary>
/// Controller fino (MVC, ADR-21): recebe, monta o comando, chama o handler e traduz o Result em HTTP.
/// Os handlers entram por [FromServices] na action: cada action recebe só o que usa.
/// </summary>
[Route("api/products")]
public sealed class ProductsController : ApiController
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromServices] IQueryHandler<ListProductsQuery, PagedResult<ProductResponse>> handler,
        [FromQuery] string? name,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(new ListProductsQuery(name, categoryId, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetProductByIdQuery, ProductResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetProductByIdQuery(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateProductRequest request,
        [FromServices] ICommandHandler<CreateProductCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Sku,
            request.Price,
            request.InitialStock,
            request.CategoryId);

        var result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
            : Problem(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request,
        [FromServices] ICommandHandler<UpdateProductCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.CategoryId);

        var result = await handler.Handle(command, cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteProductCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeleteProductCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}
