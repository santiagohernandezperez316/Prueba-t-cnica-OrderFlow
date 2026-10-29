using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OrderFlow.Orders.Api.Common;
using OrderFlow.Orders.Api.Features;
using OrderFlow.Orders.Api.Messaging;
using OrderFlow.Orders.Api.Persistence;
using OrderFlow.Orders.Api.Realtime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

ValidatorOptions.Global.PropertyNameResolver = (_, member, _) =>
    member?.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
    ?? (member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

builder.Services.AddOptions<ConnectionStringsOptions>()
    .BindConfiguration(ConnectionStringsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<OrdersDbContext>((services, options) => options
    .UseSqlServer(
        services.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.OrdersDb,
        sql =>
        {
            sql.EnableRetryOnFailure();
            sql.MigrationsHistoryTable("__EFMigrationsHistory", OrdersDbContext.Schema);
        })
    .UseAsyncSeeding(OrdersDbContext.SeedAsync));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);

builder.Services.AddOrdersMessaging();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>("orders-db", tags: ["ready"]);

builder.Services.Configure<HealthCheckServiceOptions>(options =>
    options.Registrations.Single(registration => registration.Name == "orders-db").Timeout = TimeSpan.FromSeconds(2));

builder.Services.AddOpenApi();

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "OrderFlow Orders API"));
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

var orders = app.MapGroup("/orders").WithTags("Orders");
orders.MapCreateOrder();
orders.MapGetOrders();
orders.MapGetOrderById();

app.MapGroup("/skus").WithTags("Skus").MapGetSkus();

app.MapHub<OrdersHub>("/hubs/orders");

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
}

await app.RunAsync();
