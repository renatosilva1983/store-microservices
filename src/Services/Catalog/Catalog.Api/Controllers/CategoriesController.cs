using BuildingBlocks.Application.Messaging;
using Catalog.Api.Contracts;
using Catalog.Application.Categories;
using Catalog.Application.Categories.Create;
using Catalog.Application.Categories.Delete;
using Catalog.Application.Categories.Queries;
using Catalog.Application.Categories.Update;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[Route("api/categories")]
public sealed class CategoriesController : ApiController
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromServices] IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ListCategoriesQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetCategoryByIdQuery, CategoryResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCategoryByIdQuery(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CategoryRequest request,
        [FromServices] ICommandHandler<CreateCategoryCommand, Guid> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateCategoryCommand(request.Name, request.Description), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
            : Problem(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        CategoryRequest request,
        [FromServices] ICommandHandler<UpdateCategoryCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateCategoryCommand(id, request.Name, request.Description),
            cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteCategoryCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeleteCategoryCommand(id), cancellationToken);

        return result.IsSuccess ? NoContent() : Problem(result.Error);
    }
}
