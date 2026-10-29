using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderFlow.Inventory.Worker.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint("CK_Products_Stock_NonNegative", "[Stock] >= 0"));

        builder.HasKey(product => product.Sku);

        builder.Property(product => product.Sku)
            .HasMaxLength(Product.SkuMaxLength);

        builder.Property(product => product.Name)
            .HasMaxLength(Product.NameMaxLength);
    }
}
