using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Api.Persistence;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(order => order.CustomerName)
            .HasMaxLength(Order.CustomerNameMaxLength);

        builder.Property(order => order.Sku)
            .HasMaxLength(Order.SkuMaxLength);

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
