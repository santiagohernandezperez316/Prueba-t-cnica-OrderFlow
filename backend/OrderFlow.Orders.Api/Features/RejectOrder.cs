using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using OrderFlow.Contracts.Events;
using OrderFlow.Orders.Api.Persistence;
using OrderFlow.Orders.Api.Realtime;

namespace OrderFlow.Orders.Api.Features;

internal sealed class StockRejectedConsumer(ISender sender) : IConsumer<StockRejected>
{
    public Task Consume(ConsumeContext<StockRejected> context) =>
        sender.Send(new RejectOrderCommand(context.Message.OrderId, context.Message.Reason), context.CancellationToken);
}

internal sealed record RejectOrderCommand(Guid OrderId, string Reason) : IRequest;

internal sealed class RejectOrderHandler(OrdersDbContext db, IHubContext<OrdersHub> hubContext, ILogger<RejectOrderHandler> logger)
    : IRequestHandler<RejectOrderCommand>
{
    public async Task Handle(RejectOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync([command.OrderId], cancellationToken);
        if (order is null)
        {
            logger.LogWarning("StockRejected ignored: order {OrderId} does not exist.", command.OrderId);
            return;
        }

        if (!order.Reject(command.Reason))
        {
            logger.LogWarning("StockRejected ignored for order {OrderId}: it is already {Status}.", order.Id, order.Status);
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order {OrderId} rejected: {Reason}.", order.Id, command.Reason);

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
