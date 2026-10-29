using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Orders.Api.Domain;

namespace OrderFlow.Orders.Api.Persistence;

internal sealed class KnownSkuConfiguration : IEntityTypeConfiguration<KnownSku>
{
    public void Configure(EntityTypeBuilder<KnownSku> builder)
    {
        builder.HasKey(knownSku => knownSku.Sku);

        builder.Property(knownSku => knownSku.Sku)
            .HasMaxLength(Order.SkuMaxLength);
    }
}
