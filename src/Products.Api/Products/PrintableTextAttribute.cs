using System.ComponentModel.DataAnnotations;

namespace Products.Api.Products;

public sealed class PrintableTextAttribute : RegularExpressionAttribute
{
    public PrintableTextAttribute()
        : base(@"^[^\p{Cc}]*$") =>
        ErrorMessage = "The {0} field must not contain control characters.";
}
