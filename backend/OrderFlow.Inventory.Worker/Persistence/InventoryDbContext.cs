using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace OrderFlow.Inventory.Worker.Persistence;

internal sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public const string Schema = "inventory";

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    public static async Task SeedAsync(DbContext context, bool _, CancellationToken cancellationToken)
    {
        var products = context.Set<Product>();
        if (await products.AnyAsync(cancellationToken))
        {
            return;
        }

        products.AddRange(
            new Product { Sku = "SKU-001", Name = "Teclado mecánico", Stock = 50 },
            new Product { Sku = "SKU-002", Name = "Mouse inalámbrico", Stock = 30 },
            new Product { Sku = "SKU-003", Name = "Monitor 24 pulgadas", Stock = 10 });
        await context.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
