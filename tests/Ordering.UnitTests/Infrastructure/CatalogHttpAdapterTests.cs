using System.Net;
using System.Text;
using BuildingBlocks.Application.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Ordering.Infrastructure.Catalog;

namespace Ordering.UnitTests.Infrastructure;

/// <summary>
/// Testa a tradução feita pelo adaptador com um HttpMessageHandler falso:
/// nenhum servidor real, nenhuma rede.
/// </summary>
public class CatalogHttpAdapterTests
{
    private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static CatalogHttpAdapter CreateAdapter(HttpStatusCode status, string body = "")
    {
        var handler = new StubHandler(status, body);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://catalog.test/") };
        return new CatalogHttpAdapter(client, NullLogger<CatalogHttpAdapter>.Instance);
    }

    [Fact]
    public async Task GetProduct_WhenFound_TranslatesCatalogContract()
    {
        const string json = """
            {"id":"11111111-1111-1111-1111-111111111111","name":"Notebook","description":null,
             "sku":"NB-001","price":4000.00,"currency":"BRL","stockQuantity":7,
             "categoryId":"22222222-2222-2222-2222-222222222222","categoryName":"Eletrônicos",
             "createdAtUtc":"2026-11-01T12:00:00Z"}
            """;
        var adapter = CreateAdapter(HttpStatusCode.OK, json);

        var result = await adapter.GetProductAsync(ProductId, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Notebook");
        result.Value.Price.ShouldBe(4000m);
        result.Value.StockQuantity.ShouldBe(7);
    }

    [Fact]
    public async Task GetProduct_WhenCatalogReturns404_ReturnsNotFound()
    {
        var adapter = CreateAdapter(HttpStatusCode.NotFound);

        var result = await adapter.GetProductAsync(ProductId, CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Catalog.ProductNotFound");
    }

    [Fact]
    public async Task GetProduct_WhenCatalogFails_ReturnsUnavailable()
    {
        var adapter = CreateAdapter(HttpStatusCode.InternalServerError);

        var result = await adapter.GetProductAsync(ProductId, CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Unavailable);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }
}
