namespace OrderFlow.Contracts.Events;

public sealed record StockRejected(Guid OrderId, string Reason);
