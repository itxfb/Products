using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Products.Api.Persistence;

namespace Products.Api.Products;

public static class ProductEndpoints
{
    public const string Route = "/api/products";

    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup(Route);

        products.MapGet("/", List).RequireAuthorization(ProductScopes.Read);
        products.MapGet("/{id:guid}", GetById).RequireAuthorization(ProductScopes.Read);
        products.MapPost("/", Create).RequireAuthorization(ProductScopes.Write);
    }

    public static async Task<Ok<List<ProductResponse>>> List(
        [AsParameters] ProductQuery query, ProductsDbContext db, CancellationToken cancellationToken)
    {
        var products = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Colour))
        {
            var colour = query.Colour.Trim();
            products = products.Where(product => product.Colour == colour);
        }

        if (query.After is { } after)
        {
            products = products.Where(product => product.Id > after);
        }

        var page = await products
            .OrderBy(product => product.Id)
            .Take(query.Limit)
            .Select(ProductResponse.Projection)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(page);
    }

    public static async Task<Results<Ok<ProductResponse>, NotFound>> GetById(
        Guid id, ProductsDbContext db, CancellationToken cancellationToken) =>
        await db.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(ProductResponse.Projection)
            .SingleOrDefaultAsync(cancellationToken) is { } product
            ? TypedResults.Ok(product)
            : TypedResults.NotFound();

    public static async Task<Created<ProductResponse>> Create(
        CreateProductRequest request, ProductsDbContext db, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.Name, request.Colour, request.Price);

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"{Route}/{product.Id}", ProductResponse.From(product));
    }
}
