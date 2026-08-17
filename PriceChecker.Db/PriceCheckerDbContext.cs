using Genius.PriceChecker.Db.Models;
using Genius.PriceChecker.Dto.References;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Genius.PriceChecker.Db;

public sealed class PriceCheckerDbContext : DbContext
{
    public PriceCheckerDbContext(DbContextOptions<PriceCheckerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSource> ProductSources => Set<ProductSource>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Guard.NotNull(optionsBuilder);

        // The value-converted primary keys cause a false-positive "pending model changes"
        // warning during MigrateAsync().
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Guard.NotNull(modelBuilder);

        // The keys are always generated on the client (see the CreateWithNoLinking factories), never by the database.
        modelBuilder.ConfigureReferenceId<Agent, AgentRef>(id => new AgentRef(id));
        modelBuilder.ConfigureReferenceId<Product, ProductRef>(id => new ProductRef(id));
        modelBuilder.ConfigureReferenceId<ProductSource, ProductSourceRef>(id => new ProductSourceRef(id));
        modelBuilder.ConfigureReferenceId<ProductPrice, ProductPriceRef>(id => new ProductPriceRef(id));

        modelBuilder.ConfigureReferenceFk<ProductSource, ProductRef>(nameof(ProductSource.ProductId), id => new ProductRef(id));
        modelBuilder.ConfigureReferenceFk<ProductSource, AgentRef>(nameof(ProductSource.AgentId), id => new AgentRef(id));
        modelBuilder.ConfigureReferenceFk<ProductPrice, ProductSourceRef>(nameof(ProductPrice.ProductSourceId), id => new ProductSourceRef(id));

        modelBuilder.Entity<Agent>()
            .HasIndex(x => x.Key)
            .IsUnique();

        modelBuilder.Entity<ProductSource>()
            .HasOne(x => x.Product)
            .WithMany(x => x.Sources)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductSource>()
            .HasOne(x => x.Agent)
            .WithMany()
            .HasForeignKey(x => x.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductPrice>()
            .HasOne(x => x.ProductSource)
            .WithMany(x => x.Prices)
            .HasForeignKey(x => x.ProductSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store all timestamps as Unix seconds
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUnixTimeSeconds(),
            v => DateTimeOffset.FromUnixTimeSeconds(v));
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(DateTimeOffset)))
            {
                property.SetValueConverter(dateTimeOffsetConverter);
            }
        }
    }
}
