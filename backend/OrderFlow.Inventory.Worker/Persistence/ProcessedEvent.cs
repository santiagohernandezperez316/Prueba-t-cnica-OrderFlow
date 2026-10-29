namespace OrderFlow.Inventory.Worker.Persistence;

internal sealed class ProcessedEvent
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset ProcessedOn { get; init; }
}
