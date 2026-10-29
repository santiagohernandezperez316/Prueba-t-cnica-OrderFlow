namespace OrderFlow.Inventory.Worker.Persistence;

internal sealed class Product
{
    public const int SkuMaxLength = 50;
    public const int NameMaxLength = 100;

    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required int Stock { get; init; }
}
