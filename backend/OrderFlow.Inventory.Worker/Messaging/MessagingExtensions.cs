using MassTransit;
using Microsoft.Extensions.Options;
using OrderFlow.Inventory.Worker.Features;
using OrderFlow.Inventory.Worker.Persistence;

namespace OrderFlow.Inventory.Worker.Messaging;

internal static class MessagingExtensions
{
    public static IServiceCollection AddInventoryMessaging(this IServiceCollection services)
    {
        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<InventoryDbContext>(outbox => outbox.UseSqlServer());

            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                endpoint.UseMessageRetry(retry => retry.Intervals(500, 2000, 5000));
                endpoint.UseEntityFrameworkOutbox<InventoryDbContext>(context);
            });

            bus.AddConsumer<OrderCreatedConsumer>();

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
