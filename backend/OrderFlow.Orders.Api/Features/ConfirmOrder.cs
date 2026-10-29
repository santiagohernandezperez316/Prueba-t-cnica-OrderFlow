using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using OrderFlow.Contracts.Events;
using OrderFlow.Orders.Api.Persistence;
using OrderFlow.Orders.Api.Realtime;

namespace OrderFlow.Orders.Api.Features;

internal sealed class StockReservedConsumer(ISender sender) : IConsumer<StockReserved>
{
    public Task Consume(ConsumeContext<StockReserved> context) =>
        sender.Send(new ConfirmOrderCommand(context.Message.OrderId), context.CancellationToken);
}

internal sealed record ConfirmOrderCommand(Guid OrderId) : IRequest;

internal sealed class ConfirmOrderHandler(OrdersDbContext db, IHubContext<OrdersHub> hubContext, ILogger<ConfirmOrderHandler> logger)
    : IRequestHandler<ConfirmOrderCommand>
{
    public async Task Handle(ConfirmOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync([command.OrderId], cancellationToken);
        if (order is null)
        {
            logger.LogWarning("StockReserved ignored: order {OrderId} does not exist.", command.OrderId);
            return;
        }

        if (!order.Confirm())
        {
            logger.LogWarning("StockReserved ignored for order {OrderId}: it is already {Status}.", order.Id, order.Status);
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order {OrderId} confirmed.", order.Id);

        try
        {
            await hubContext.NotifyStatusChangedAsync(order, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Status push for order {OrderId} failed.", order.Id);
        }
    }
}
