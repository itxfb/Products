using System.ComponentModel.DataAnnotations;

namespace Products.Api.Products;

public sealed record ProductQuery(
    [property: PrintableText] string? Colour,
    Guid? After,
    [property: Range(1, ProductQuery.MaxLimit)] int Limit = ProductQuery.DefaultLimit)
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 100;
}
