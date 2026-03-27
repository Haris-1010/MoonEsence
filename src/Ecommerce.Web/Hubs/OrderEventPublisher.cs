using Ecommerce.Application.Abstractions.Events;
using Ecommerce.Application.Events;
using Microsoft.AspNetCore.SignalR;

namespace Ecommerce.Web.Hubs;

public class OrderEventPublisher : IOrderEventPublisher
{
    private readonly IHubContext<OrderHub> _hubContext;

    public OrderEventPublisher(IHubContext<OrderHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishOrderPlacedAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("admins").SendAsync(
            "orderPlaced",
            new
            {
                orderId = @event.OrderId,
                totalAmount = @event.TotalAmount,
                createdAt = @event.CreatedAt,
                status = (int)@event.Status
            },
            cancellationToken);

        await _hubContext.Clients.Group($"order-{@event.AccessCode}").SendAsync(
            "orderPlaced",
            new
            {
                orderId = @event.OrderId,
                accessCode = @event.AccessCode,
                createdAt = @event.CreatedAt,
                status = (int)@event.Status
            },
            cancellationToken);

        // Ensure clients instantly receive initial status too.
        await _hubContext.Clients.Group($"order-{@event.AccessCode}").SendAsync(
            "orderStatusChanged",
            new
            {
                orderId = @event.OrderId,
                newStatus = (int)@event.Status,
                updatedAt = @event.CreatedAt
            },
            cancellationToken);
    }

    public async Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent @event, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            orderId = @event.OrderId,
            accessCode = @event.AccessCode,
            newStatus = (int)@event.NewStatus,
            updatedAt = @event.UpdatedAt
        };

        await _hubContext.Clients.Group("admins").SendAsync("orderStatusChanged", payload, cancellationToken);
        await _hubContext.Clients.Group($"order-{@event.AccessCode}").SendAsync("orderStatusChanged", payload, cancellationToken);
    }
}

