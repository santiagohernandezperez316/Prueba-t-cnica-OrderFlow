using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Tests;

public sealed class OrderTests
{
    [Theory]
    [InlineData(nameof(OrderStatus.Pending), nameof(Order.Confirm), true, nameof(OrderStatus.Confirmed))]
    [InlineData(nameof(OrderStatus.Pending), nameof(Order.Reject), true, nameof(OrderStatus.Rejected))]
    [InlineData(nameof(OrderStatus.Confirmed), nameof(Order.Confirm), false, nameof(OrderStatus.Confirmed))]
    [InlineData(nameof(OrderStatus.Confirmed), nameof(Order.Reject), false, nameof(OrderStatus.Confirmed))]
    [InlineData(nameof(OrderStatus.Rejected), nameof(Order.Confirm), false, nameof(OrderStatus.Rejected))]
    [InlineData(nameof(OrderStatus.Rejected), nameof(Order.Reject), false, nameof(OrderStatus.Rejected))]
    public void ConfirmOrReject_FromEachStatus_AppliesOnlyFromPending(string initialStatus, string transition, bool expectedApplied, string expectedStatus)
    {
        var order = OrderIn(Enum.Parse<OrderStatus>(initialStatus));
        var rejectionReasonBefore = order.RejectionReason;

        var applied = transition == nameof(Order.Confirm) ? order.Confirm() : order.Reject("stock insuficiente");

        Assert.Equal(expectedApplied, applied);
        Assert.Equal(Enum.Parse<OrderStatus>(expectedStatus), order.Status);
        if (!applied)
        {
            Assert.Equal(rejectionReasonBefore, order.RejectionReason);
        }
    }

    private static Order OrderIn(OrderStatus status)
    {
        var order = Order.Create("Ana", "SKU-001", 1, DateTimeOffset.UtcNow);
        switch (status)
        {
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Rejected:
                order.Reject("motivo original");
                break;
        }

        return order;
    }
}
