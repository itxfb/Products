using Products.Api.Products;

namespace Products.Api.UnitTests;

public sealed class ProductTests
{
    public static TheoryData<decimal, decimal> Prices => new()
    {
        { 10.005m, 10.01m },
        { 10.004m, 10.00m },
        { 49.99m, 49.99m },
    };

    [Fact]
    public void Create_PaddedNameAndColour_TrimsBoth()
    {
        var product = Product.Create("  Desk ", " Black  ", 120m);

        Assert.Equal("Desk", product.Name);
        Assert.Equal("Black", product.Colour);
    }

    [Fact]
    public void Create_AnyProduct_AssignsVersion7Id()
    {
        var product = Product.Create("Desk", "Black", 120m);

        Assert.Equal(7, product.Id.Version);
    }

    [Theory]
    [MemberData(nameof(Prices))]
    public void Create_PriceWithExtraDecimals_RoundsToTwoPlacesAwayFromZero(decimal price, decimal expected)
    {
        var product = Product.Create("Desk", "Black", price);

        Assert.Equal(expected, product.Price);
    }
}
