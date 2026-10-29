using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OrderFlow.Orders.Api.Features;
using OrderFlow.Orders.Api.Persistence;

namespace OrderFlow.Orders.Api.Messaging;

internal static class MessagingExtensions
{
    public static IServiceCollection AddOrdersMessaging(this IServiceCollection services)
    {
        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<OrdersDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
            });

            bus.AddConfigureEndpointsCallback((_, _, endpoint) =>
                endpoint.UseMessageRetry(retry => retry.Intervals(500, 2000, 5000)));

            bus.AddConsumer<StockReservedConsumer>();
            bus.AddConsumer<StockRejectedConsumer>();

            bus.ConfigureHealthCheckOptions(health => health.MinimalFailureStatus = HealthStatus.Degraded);

            bus.UsingRabbitMq((context, rabbit) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                rabbit.Host(options.Host, "/", host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
