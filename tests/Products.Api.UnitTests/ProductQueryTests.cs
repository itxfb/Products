using Products.Api.Products;

namespace Products.Api.UnitTests;

public sealed class ProductQueryTests
{
    [Fact]
    public void Limit_NotSupplied_DefaultsToFifty()
    {
        Assert.Equal(50, new ProductQuery(null, null).Limit);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(ProductQuery.MaxLimit)]
    public void Validate_LimitWithinRange_HasNoErrors(int limit)
    {
        Assert.Empty(Validation.Errors(new ProductQuery(null, null, limit)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ProductQuery.MaxLimit + 1)]
    public void Validate_LimitOutOfRange_ReportsLimit(int limit)
    {
        Assert.Equal([nameof(ProductQuery.Limit)], Validation.Errors(new ProductQuery(null, null, limit)));
    }
}
