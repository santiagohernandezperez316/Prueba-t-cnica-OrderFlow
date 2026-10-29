using MassTransit.Testing;
using OrderFlow.Contracts.Events;

namespace OrderFlow.Inventory.Tests;

public sealed class ReserveStockTests(InventoryWorkerFactory factory) : IClassFixture<InventoryWorkerFactory>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task ReserveStock_SameEventIdWithNewMessageId_DecrementsStockOnce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var sku = await factory.SeedProductAsync(stock: 10, cancellationToken);
        var harness = factory.Services.GetTestHarness();
        var orderCreated = new OrderCreated(Guid.NewGuid(), Guid.NewGuid(), sku, 1, DateTimeOffset.UtcNow);

        await harness.Bus.Publish(orderCreated, cancellationToken);
        Assert.True(await WaitUntilAsync(() => ConsumedCount(harness, orderCreated.EventId, cancellationToken) == 1, cancellationToken));
        await harness.Bus.Publish(orderCreated, cancellationToken);
        Assert.True(await WaitUntilAsync(() => ConsumedCount(harness, orderCreated.EventId, cancellationToken) == 2, cancellationToken));

        Assert.True(await harness.Consumed.Any<StockReserved>(message => message.Context.Message.OrderId == orderCreated.OrderId, cancellationToken));
        Assert.Equal(9, await factory.GetStockAsync(sku, cancellationToken));
        Assert.Equal(1, await factory.CountProcessedAsync(orderCreated.EventId, cancellationToken));
        Assert.Single(harness.Consumed.Select<StockReserved>(message => message.Context.Message.OrderId == orderCreated.OrderId, cancellationToken));
    }

    [Fact]
    public async Task ReserveStock_ConcurrentOrdersExceedStock_NeverOversells()
    {
        const int stock = 48;
        const int validOrders = 60;
        var cancellationToken = TestContext.Current.CancellationToken;
        var sku = await factory.SeedProductAsync(stock, cancellationToken);
        var harness = factory.Services.GetTestHarness();

        var invalidEvents = new[] { 0, -5 }
            .Select(quantity => new OrderCreated(Guid.NewGuid(), Guid.NewGuid(), sku, quantity, DateTimeOffset.UtcNow))
            .ToList();
        var events = Enumerable.Range(0, validOrders)
            .Select(_ => new OrderCreated(Guid.NewGuid(), Guid.NewGuid(), sku, 1, DateTimeOffset.UtcNow))
            .Concat(invalidEvents)
            .ToList();
        var orderIds = events.Select(orderCreated => orderCreated.OrderId).ToHashSet();

        await Task.WhenAll(events.Select(orderCreated => harness.Bus.Publish(orderCreated, cancellationToken)));

        int Reserved() => harness.Consumed.Select<StockReserved>(message => orderIds.Contains(message.Context.Message.OrderId), cancellationToken).Count();
        int Rejected() => harness.Consumed.Select<StockRejected>(message => orderIds.Contains(message.Context.Message.OrderId), cancellationToken).Count();
        Assert.True(await WaitUntilAsync(() => Reserved() + Rejected() == events.Count, cancellationToken));

        Assert.Equal(stock, Reserved());
        Assert.Equal(events.Count - stock, Rejected());
        Assert.Equal(0, await factory.GetStockAsync(sku, cancellationToken));
        foreach (var invalid in invalidEvents)
        {
            Assert.True(await harness.Consumed.Any<StockRejected>(
                message => message.Context.Message.OrderId == invalid.OrderId && message.Context.Message.Reason.StartsWith("cantidad inválida"),
                cancellationToken));
        }
    }

    private static int ConsumedCount(ITestHarness harness, Guid eventId, CancellationToken cancellationToken) =>
        harness.Consumed.Select<OrderCreated>(message => message.Context.Message.EventId == eventId, cancellationToken).Count();

    private static async Task<bool> WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                return false;
            }

            await Task.Delay(100, cancellationToken);
        }

        return true;
    }
}
