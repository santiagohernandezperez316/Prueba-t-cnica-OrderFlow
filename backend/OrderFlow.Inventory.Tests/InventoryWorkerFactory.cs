using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts.Events;
using OrderFlow.Inventory.Worker.Persistence;
using Testcontainers.MsSql;

namespace OrderFlow.Inventory.Tests;

public sealed class InventoryWorkerFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync() => await _sqlServer.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }

    internal async Task<string> SeedProductAsync(int stock, CancellationToken cancellationToken)
    {
        var sku = $"TEST-{Guid.NewGuid():N}"[..20];

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        db.Products.Add(new Product { Sku = sku, Name = "Producto de prueba", Stock = stock });
        await db.SaveChangesAsync(cancellationToken);

        return sku;
    }

    internal async Task<int> GetStockAsync(string sku, CancellationToken cancellationToken)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await db.Products.Where(product => product.Sku == sku).Select(product => product.Stock).SingleAsync(cancellationToken);
    }

    internal async Task<int> CountProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await db.ProcessedEvents.CountAsync(processedEvent => processedEvent.EventId == eventId, cancellationToken);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "InventoryTests"
        }.ConnectionString;

        builder.UseSetting("ConnectionStrings:InventoryDb", connectionString);

        builder.UseSetting("RabbitMq:Host", "unused");
        builder.UseSetting("RabbitMq:Username", "unused");
        builder.UseSetting("RabbitMq:Password", "unused");

        builder.ConfigureTestServices(services => services.AddMassTransitTestHarness(bus => bus.AddConsumer<StockOutcomeConsumer>()));
    }
}

public sealed class StockOutcomeConsumer : IConsumer<StockReserved>, IConsumer<StockRejected>
{
    public Task Consume(ConsumeContext<StockReserved> context) => Task.CompletedTask;

    public Task Consume(ConsumeContext<StockRejected> context) => Task.CompletedTask;
}
