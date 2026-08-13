using System.Linq.Expressions;
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
        // The value-converted primary keys cause a false-positive "pending model changes"
        // warning during MigrateAsync().
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureReferenceId<Agent, AgentRef>(modelBuilder, id => new AgentRef(id));
        ConfigureReferenceId<Product, ProductRef>(modelBuilder, id => new ProductRef(id));
        ConfigureReferenceId<ProductSource, ProductSourceRef>(modelBuilder, id => new ProductSourceRef(id));
        ConfigureReferenceId<ProductPrice, ProductPriceRef>(modelBuilder, id => new ProductPriceRef(id));

        ConfigureReferenceFk<ProductSource, ProductRef>(modelBuilder, nameof(ProductSource.ProductId), id => new ProductRef(id));
        ConfigureReferenceFk<ProductSource, AgentRef>(modelBuilder, nameof(ProductSource.AgentId), id => new AgentRef(id));
        ConfigureReferenceFk<ProductPrice, ProductSourceRef>(modelBuilder, nameof(ProductPrice.ProductSourceId), id => new ProductSourceRef(id));

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

    /// <summary>
    ///   Configures the strongly typed reference of an entity's primary key. The keys are always
    ///   generated on the client (see the <c>CreateWithNoLinking</c> factories), never by the database.
    /// </summary>
    private static void ConfigureReferenceId<TEntity, TReference>(ModelBuilder modelBuilder,
        Expression<Func<Guid, TReference>> fromProvider)
        where TEntity : EntityBase<Guid, TReference>
        where TReference : IReference<Guid, TReference>
    {
        modelBuilder.Entity<TEntity>()
            .Property(e => e.Id)
            .HasConversion(r => r.Id, fromProvider)
            .ValueGeneratedNever();
    }

    private static void ConfigureReferenceFk<TEntity, TReference>(ModelBuilder modelBuilder,
        string propertyName, Expression<Func<Guid, TReference>> fromProvider)
        where TEntity : class
        where TReference : IReference<Guid, TReference>
    {
        modelBuilder.Entity<TEntity>()
            .Property<TReference>(propertyName)
            .HasConversion(r => r.Id, fromProvider);
    }
}
