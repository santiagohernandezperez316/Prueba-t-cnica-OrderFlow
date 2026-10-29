using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Contracts.Events;
using OrderFlow.Orders.Api.Domain;
using OrderFlow.Orders.Api.Persistence;

namespace OrderFlow.Orders.Tests;

public sealed class CreateOrderTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    [Fact]
    public async Task CreateOrder_InvalidFields_ReturnsValidationProblemWithEveryField()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/orders",
            new { clienteNombre = "", sku = "NO-EXISTE", cantidad = 0 },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var fields = body.RootElement.GetProperty("errors").EnumerateObject().Select(error => error.Name).Order();
        Assert.Equal(["cantidad", "clienteNombre", "sku"], fields);
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_SavesPendingOrderAndPublishesOrderCreated()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/orders",
            new { clienteNombre = "Ana", sku = "SKU-001", cantidad = 2 },
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var orderId = body.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Pending", body.RootElement.GetProperty("estado").GetString());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = await db.Orders.SingleAsync(order => order.Id == orderId, cancellationToken);
            Assert.Equal(OrderStatus.Pending, order.Status);
        }

        var harness = factory.Services.GetTestHarness();
        Assert.True(await harness.Published.Any<OrderCreated>(
            message => message.Context.Message.OrderId == orderId && message.Context.Message.Quantity == 2,
            cancellationToken));
    }
}
