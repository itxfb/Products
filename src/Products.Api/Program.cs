using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Products.Api.Persistence;
using Products.Api.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<ProductsDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString(ProductsDbContext.ConnectionStringName),
    npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddAuthentication().AddJwtBearer(options => options.MapInboundClaims = false);

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(ProductScopes.Read, policy => policy.RequireAssertion(context => HasScope(context.User, ProductScopes.Read)))
    .AddPolicy(ProductScopes.Write, policy => policy.RequireAssertion(context => HasScope(context.User, ProductScopes.Write)));

builder.Services.AddHealthChecks().AddDbContextCheck<ProductsDbContext>();
builder.Services.Configure<HealthCheckServiceOptions>(options =>
{
    foreach (var registration in options.Registrations)
    {
        registration.Timeout = TimeSpan.FromSeconds(3);
    }
});

builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ProductsDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapProductEndpoints();

app.Run();

static bool HasScope(ClaimsPrincipal user, string scope) =>
    user.FindFirst(ProductScopes.ClaimType)?.Value.Split(' ').Contains(scope) is true;
