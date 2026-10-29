using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Orders.Api.Persistence;

internal sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string OrdersDb { get; init; } = string.Empty;
}
