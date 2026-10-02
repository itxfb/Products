using System.ComponentModel.DataAnnotations;

namespace Products.Api.Products;

public sealed record CreateProductRequest(
    [property: Required, StringLength(Product.NameMaxLength), PrintableText] string Name,
    [property: Required, StringLength(Product.ColourMaxLength), PrintableText] string Colour,
    [property: Range(0.01, 1_000_000)] decimal Price);
