using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Products.Api.Persistence;

namespace Products.Api.IntegrationTests;

public sealed class HealthEndpointsTests(ProductsApiFactory factory)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    public async Task Get_WithoutToken_ReturnsHealthy(string path)
    {
        var response = await factory.CreateClient().GetAsync(new Uri(path, UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task Startup_EmptyDatabase_AppliesEveryMigration()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ProductsDbContext>().Database;

        Assert.NotEmpty(await database.GetAppliedMigrationsAsync(CancellationToken));
        Assert.Empty(await database.GetPendingMigrationsAsync(CancellationToken));
    }
}
