using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Inventory.Worker.Persistence;

internal sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string InventoryDb { get; init; } = string.Empty;
}
