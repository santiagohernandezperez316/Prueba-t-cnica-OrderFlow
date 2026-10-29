using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace OrderFlow.Orders.Tests;

public sealed class OrdersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public async ValueTask InitializeAsync() => await _sqlServer.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sqlServer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = new SqlConnectionStringBuilder(_sqlServer.GetConnectionString())
        {
            InitialCatalog = "OrdersTests"
        }.ConnectionString;

        builder.UseSetting("ConnectionStrings:OrdersDb", connectionString);

        builder.UseSetting("RabbitMq:Host", "unused");
        builder.UseSetting("RabbitMq:Username", "unused");
        builder.UseSetting("RabbitMq:Password", "unused");

        builder.ConfigureTestServices(services => services.AddMassTransitTestHarness());
    }
}
