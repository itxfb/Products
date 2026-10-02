namespace Products.Api.Products;

public sealed class Product
{
    public const int NameMaxLength = 100;
    public const int ColourMaxLength = 50;
    public const int PricePrecision = 18;
    public const int PriceScale = 2;

    private Product(Guid id, string name, string colour, decimal price)
    {
        Id = id;
        Name = name;
        Colour = colour;
        Price = price;
    }

    public Guid Id { get; private init; }

    public string Name { get; private init; }

    public string Colour { get; private init; }

    public decimal Price { get; private init; }

    public static Product Create(string name, string colour, decimal price) =>
        new(Guid.CreateVersion7(), name.Trim(), colour.Trim(), decimal.Round(price, PriceScale, MidpointRounding.AwayFromZero));
}
