using System.Text.Json.Serialization;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts.Events;
using OrderFlow.Orders.Api.Domain;
using OrderFlow.Orders.Api.Persistence;

namespace OrderFlow.Orders.Api.Features;

internal static class CreateOrderEndpoint
{
    public static void MapCreateOrder(this IEndpointRouteBuilder app) =>
        app.MapPost("/", async (CreateOrderCommand command, ISender sender, CancellationToken cancellationToken) =>
        {
            var order = await sender.Send(command, cancellationToken);
            return TypedResults.Created($"/orders/{order.Id}", order);
        })
        .ProducesValidationProblem()
        .WithSummary("Create a new order");
}

internal sealed record CreateOrderCommand(
    [property: JsonPropertyName("clienteNombre")] string CustomerName,
    string Sku,
    [property: JsonPropertyName("cantidad")] int Quantity) : IRequest<OrderResponse>;

internal sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator(OrdersDbContext db)
    {
        RuleFor(command => command.CustomerName)
            .NotEmpty().WithMessage("clienteNombre es obligatorio.")
            .MaximumLength(Order.CustomerNameMaxLength)
            .WithMessage($"clienteNombre no puede superar {Order.CustomerNameMaxLength} caracteres.");

        RuleFor(command => command.Sku)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("sku es obligatorio.")
            .MustAsync((sku, cancellationToken) => db.KnownSkus.AnyAsync(knownSku => knownSku.Sku == sku, cancellationToken))
            .WithMessage(command => $"el sku '{command.Sku}' no existe.");

        RuleFor(command => command.Quantity)
            .InclusiveBetween(Order.MinQuantity, Order.MaxQuantity)
            .WithMessage($"cantidad debe estar entre {Order.MinQuantity} y {Order.MaxQuantity}.");
    }
}

internal sealed class CreateOrderHandler(OrdersDbContext db, IPublishEndpoint publishEndpoint, TimeProvider timeProvider)
    : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var order = Order.Create(command.CustomerName, command.Sku, command.Quantity, now);
        db.Orders.Add(order);

        await publishEndpoint.Publish(new OrderCreated(Guid.NewGuid(), order.Id, order.Sku, order.Quantity, now), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return OrderResponse.From(order);
    }
}
