using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Products.Api.Products;

namespace Products.Api.IntegrationTests;

public sealed class ProductEndpointsTests(ProductsApiFactory factory)
{
    private const string ProblemJson = "application/problem+json";
    private const string AnyProductPath = $"{ProductEndpoints.Route}/0199a9a0-0000-7000-8000-000000000000";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", ProductEndpoints.Route)]
    [InlineData("GET", AnyProductPath)]
    [InlineData("POST", ProductEndpoints.Route)]
    public async Task Request_WithoutToken_Returns401ProblemDetails(string method, string path)
    {
        var response = await factory.CreateClient().SendAsync(Request(method, path), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("GET", ProductEndpoints.Route, ProductScopes.Write)]
    [InlineData("GET", AnyProductPath, ProductScopes.Write)]
    [InlineData("POST", ProductEndpoints.Route, ProductScopes.Read)]
    public async Task Request_WithoutRequiredScope_Returns403ProblemDetails(string method, string path, string grantedScope)
    {
        var response = await factory.CreateClientWithScopes(grantedScope).SendAsync(Request(method, path), CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_ValidProduct_Returns201WithRelativeLocationOfSameProduct()
    {
        var client = ReadWriteClient();
        var colour = UniqueColour();

        var response = await client.PostAsJsonAsync(ProductEndpoints.Route, new CreateProductRequest(" Desk ", colour, 49.99m), CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductResponse>(CancellationToken);
        Assert.Equal(new ProductResponse(created!.Id, "Desk", colour, 49.99m), created);
        Assert.Equal($"{ProductEndpoints.Route}/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(created, await client.GetFromJsonAsync<ProductResponse>(response.Headers.Location, CancellationToken));
    }

    [Fact]
    public async Task Create_InvalidProduct_Returns400WithFieldErrors()
    {
        var invalid = new CreateProductRequest("", new string('c', Product.ColourMaxLength + 1), 0m);

        var response = await ReadWriteClient().PostAsJsonAsync(ProductEndpoints.Route, invalid, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken);
        Assert.Equal([nameof(CreateProductRequest.Colour), nameof(CreateProductRequest.Name), nameof(CreateProductRequest.Price)], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404ProblemDetails()
    {
        var response = await ReadWriteClient().GetAsync(new Uri(AnyProductPath, UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task List_WithoutFilter_ReturnsCreatedProducts()
    {
        var client = ReadWriteClient();
        var created = await CreateProductsAsync(client, UniqueColour(), UniqueColour());

        var listed = await ListAllAsync(client);

        Assert.Superset(created.ToHashSet(), listed.ToHashSet());
    }

    [Fact]
    public async Task List_ColourInDifferentCase_ReturnsOnlyThatColour()
    {
        var client = ReadWriteClient();
        var colour = UniqueColour();
        var matching = await CreateProductsAsync(client, colour, colour);
        await CreateProductsAsync(client, UniqueColour());

        var listed = await client.GetFromJsonAsync<List<ProductResponse>>($"{ProductEndpoints.Route}?colour={colour.ToUpperInvariant()}", CancellationToken);

        Assert.Equal(matching.OrderBy(product => product.Id), listed);
    }

    [Fact]
    public async Task List_LimitAndAfter_ReturnsConsecutivePagesWithoutOverlap()
    {
        var client = ReadWriteClient();
        var colour = UniqueColour();
        var created = await CreateProductsAsync(client, colour, colour, colour);
        var path = $"{ProductEndpoints.Route}?colour={colour}&limit=2";

        var first = await client.GetFromJsonAsync<List<ProductResponse>>(path, CancellationToken);
        var second = await client.GetFromJsonAsync<List<ProductResponse>>($"{path}&after={first![^1].Id}", CancellationToken);

        Assert.Equal(2, first.Count);
        Assert.Single(second!);
        Assert.Empty(first.Intersect(second!));
        Assert.Equal(created.OrderBy(product => product.Id), [.. first, .. second!]);
    }

    [Fact]
    public async Task List_LimitAboveMaximum_Returns400()
    {
        var response = await ReadWriteClient().GetAsync(new Uri($"{ProductEndpoints.Route}?limit={ProductQuery.MaxLimit + 1}", UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    private static HttpRequestMessage Request(string method, string path) => new(new HttpMethod(method), new Uri(path, UriKind.Relative));

    private static string UniqueColour() => $"Colour-{Guid.NewGuid():N}";

    private HttpClient ReadWriteClient() => factory.CreateClientWithScopes(ProductScopes.Read, ProductScopes.Write);

    private static async Task<List<ProductResponse>> CreateProductsAsync(HttpClient client, params string[] colours)
    {
        var created = new List<ProductResponse>();
        foreach (var colour in colours)
        {
            var response = await client.PostAsJsonAsync(ProductEndpoints.Route, new CreateProductRequest("Product", colour, 10m), CancellationToken);
            created.Add((await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<ProductResponse>(CancellationToken))!);
        }

        return created;
    }

    private static async Task<List<ProductResponse>> ListAllAsync(HttpClient client)
    {
        var listed = new List<ProductResponse>();
        List<ProductResponse> page;
        do
        {
            var after = listed.Count > 0 ? $"&after={listed[^1].Id}" : "";
            page = (await client.GetFromJsonAsync<List<ProductResponse>>($"{ProductEndpoints.Route}?limit={ProductQuery.MaxLimit}{after}", CancellationToken))!;
            listed.AddRange(page);
        }
        while (page.Count == ProductQuery.MaxLimit);

        return listed;
    }
}
