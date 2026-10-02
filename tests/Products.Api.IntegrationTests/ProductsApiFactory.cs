using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Products.Api.IntegrationTests;
using Products.Api.Persistence;
using Products.Api.Products;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ProductsApiFactory))]

namespace Products.Api.IntegrationTests;

public sealed class ProductsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Issuer = "https://identity.test/realms/products";
    private const string Audience = "products-api";

    private readonly SymmetricSecurityKey _signingKey = new(RandomNumberGenerator.GetBytes(32));
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public HttpClient CreateClientWithScopes(params string[] scopes)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, CreateToken(scopes));
        return client;
    }

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting($"ConnectionStrings:{ProductsDbContext.ConnectionStringName}", _database.GetConnectionString());
        builder.UseSetting($"Authentication:Schemes:{JwtBearerDefaults.AuthenticationScheme}:ValidIssuer", Issuer);
        builder.ConfigureTestServices(services => services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer, SigningKeys = { _signingKey } }));
    }

    private string CreateToken(string[] scopes) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = Issuer,
        Audience = Audience,
        Expires = DateTime.UtcNow.AddMinutes(5),
        Claims = new Dictionary<string, object> { [ProductScopes.ClaimType] = string.Join(' ', scopes) },
        SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
    });
}
