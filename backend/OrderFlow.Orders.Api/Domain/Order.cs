namespace OrderFlow.Orders.Api.Domain;

internal sealed class Order
{
    public const int CustomerNameMaxLength = 200;
    public const int SkuMaxLength = 50;
    public const int MinQuantity = 1;
    public const int MaxQuantity = 100;

    private Order()
    {
    }

    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedOn { get; private set; }

    public static Order Create(string customerName, string sku, int quantity, DateTimeOffset createdOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, MinQuantity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, MaxQuantity);

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            Sku = sku,
            Quantity = quantity,
            Status = OrderStatus.Pending,
            CreatedOn = createdOn
        };
    }

    public bool Confirm()
    {
        if (Status != OrderStatus.Pending)
        {
            return false;
        }

        Status = OrderStatus.Confirmed;
        return true;
    }

    public bool Reject(string reason)
    {
        if (Status != OrderStatus.Pending)
        {
            return false;
        }

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        return true;
    }
}
