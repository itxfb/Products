using Microsoft.EntityFrameworkCore;
using Products.Api.Products;

namespace Products.Api.Persistence;

public sealed class ProductsDbContext(DbContextOptions<ProductsDbContext> options) : DbContext(options)
{
    public const string ConnectionStringName = "Products";

    private const string CaseInsensitiveCollation = "case_insensitive";

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCollation(CaseInsensitiveCollation, locale: "und-u-ks-level2", provider: "icu", deterministic: false);

        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Name).HasMaxLength(Product.NameMaxLength);
            product.Property(p => p.Colour).HasMaxLength(Product.ColourMaxLength).UseCollation(CaseInsensitiveCollation);
            product.Property(p => p.Price).HasPrecision(Product.PricePrecision, Product.PriceScale);
            product.HasIndex(p => new { p.Colour, p.Id });
        });
    }
}
