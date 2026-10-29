using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Events;
using OrderFlow.Inventory.Worker.Persistence;

namespace OrderFlow.Inventory.Worker.Features;

internal sealed class OrderCreatedConsumer(ISender sender) : IConsumer<OrderCreated>
{
    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;
        return sender.Send(new ReserveStockCommand(message.EventId, message.OrderId, message.Sku, message.Quantity), context.CancellationToken);
    }
}

internal sealed record ReserveStockCommand(Guid EventId, Guid OrderId, string Sku, int Quantity) : IRequest;

internal sealed class ReserveStockHandler(
    InventoryDbContext db,
    IPublishEndpoint publishEndpoint,
    TimeProvider timeProvider,
    ILogger<ReserveStockHandler> logger) : IRequestHandler<ReserveStockCommand>
{
    public async Task Handle(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        if (!await TryMarkProcessedAsync(command.EventId, cancellationToken))
        {
            logger.LogInformation("Event {EventId} for order {OrderId} already processed; duplicate ignored.", command.EventId, command.OrderId);
            return;
        }

        if (await TryReserveAsync(command.Sku, command.Quantity, cancellationToken))
        {
            await publishEndpoint.Publish(new StockReserved(command.OrderId), cancellationToken);
            logger.LogInformation("Reserved {Quantity} of {Sku} for order {OrderId}.", command.Quantity, command.Sku, command.OrderId);
        }
        else
        {
            var reason = await GetRejectionReasonAsync(command, cancellationToken);
            await publishEndpoint.Publish(new StockRejected(command.OrderId, reason), cancellationToken);
            logger.LogInformation("Rejected order {OrderId}: {Reason}.", command.OrderId, reason);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GetRejectionReasonAsync(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        if (command.Quantity <= 0)
        {
            return $"cantidad inválida: {command.Quantity}";
        }

        return await db.Products.AnyAsync(product => product.Sku == command.Sku, cancellationToken)
            ? $"stock insuficiente para {command.Sku}"
            : $"el sku {command.Sku} no existe en inventario";
    }

    private async Task<bool> TryMarkProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var processedOn = timeProvider.GetUtcNow();

        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO inventory.ProcessedEvents (EventId, ProcessedOn)
            SELECT {eventId}, {processedOn}
            WHERE NOT EXISTS (
                SELECT 1 FROM inventory.ProcessedEvents WITH (UPDLOCK, HOLDLOCK)
                WHERE EventId = {eventId})
            """, cancellationToken);

        return inserted == 1;
    }

    private async Task<bool> TryReserveAsync(string sku, int quantity, CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            return false;
        }

        var affectedRows = await db.Products
            .Where(product => product.Sku == sku && product.Stock >= quantity)
            .ExecuteUpdateAsync(setters => setters.SetProperty(product => product.Stock, product => product.Stock - quantity), cancellationToken);

        return affectedRows == 1;
    }
}
