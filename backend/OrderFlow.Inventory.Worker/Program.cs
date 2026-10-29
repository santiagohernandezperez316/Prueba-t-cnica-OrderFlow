using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OrderFlow.Inventory.Worker.Features;
using OrderFlow.Inventory.Worker.Messaging;
using OrderFlow.Inventory.Worker.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ConnectionStringsOptions>()
    .BindConfiguration(ConnectionStringsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<InventoryDbContext>((services, options) => options
    .UseSqlServer(
        services.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.InventoryDb,
        sql =>
        {
            sql.EnableRetryOnFailure();
            sql.MigrationsHistoryTable("__EFMigrationsHistory", InventoryDbContext.Schema);
        })
    .UseAsyncSeeding(InventoryDbContext.SeedAsync));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddInventoryMessaging();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<InventoryDbContext>("inventory-db", tags: ["ready"]);

builder.Services.Configure<HealthCheckServiceOptions>(options =>
    options.Registrations.Single(registration => registration.Name == "inventory-db").Timeout = TimeSpan.FromSeconds(2));

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapGroup("/products").MapGetProducts();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
}

await app.RunAsync();
