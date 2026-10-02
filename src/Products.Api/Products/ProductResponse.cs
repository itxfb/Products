using System.Linq.Expressions;

namespace Products.Api.Products;

public sealed record ProductResponse(Guid Id, string Name, string Colour, decimal Price)
{
    public static Expression<Func<Product, ProductResponse>> Projection { get; } =
        product => new ProductResponse(product.Id, product.Name, product.Colour, product.Price);

    public static ProductResponse From(Product product) => new(product.Id, product.Name, product.Colour, product.Price);
}
