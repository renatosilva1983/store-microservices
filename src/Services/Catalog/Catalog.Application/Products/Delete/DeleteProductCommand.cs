using BuildingBlocks.Application.Messaging;

namespace Catalog.Application.Products.Delete;

public sealed record DeleteProductCommand(Guid Id) : ICommand;
