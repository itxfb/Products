using Products.Api.Products;

namespace Products.Api.UnitTests;

public sealed class CreateProductRequestTests
{
    private static readonly string MaxName = new('n', Product.NameMaxLength);
    private static readonly string MaxColour = new('c', Product.ColourMaxLength);

    public static TheoryData<string, string, decimal> ValidRequests => new()
    {
        { "Desk", "Black", 120m },
        { MaxName, MaxColour, 0.01m },
        { "D", "B", 1_000_000m },
    };

    public static TheoryData<string, string, decimal, string> InvalidRequests => new()
    {
        { "", "Black", 10m, nameof(CreateProductRequest.Name) },
        { "   ", "Black", 10m, nameof(CreateProductRequest.Name) },
        { MaxName + "n", "Black", 10m, nameof(CreateProductRequest.Name) },
        { "Desk", "", 10m, nameof(CreateProductRequest.Colour) },
        { "Desk", MaxColour + "c", 10m, nameof(CreateProductRequest.Colour) },
        { "Desk", "Black", 0m, nameof(CreateProductRequest.Price) },
        { "Desk", "Black", 0.009m, nameof(CreateProductRequest.Price) },
        { "Desk", "Black", 1_000_000.01m, nameof(CreateProductRequest.Price) },
    };

    [Theory]
    [MemberData(nameof(ValidRequests))]
    public void Validate_ValidRequest_HasNoErrors(string name, string colour, decimal price)
    {
        Assert.Empty(Validation.Errors(new CreateProductRequest(name, colour, price)));
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void Validate_InvalidField_ReportsOnlyThatField(string name, string colour, decimal price, string field)
    {
        Assert.Equal([field], Validation.Errors(new CreateProductRequest(name, colour, price)));
    }
}
