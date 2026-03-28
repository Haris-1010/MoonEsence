using Ecommerce.Application.Events;

namespace Ecommerce.Application.Abstractions.Events;

public interface IOrderEventPublisher
{
    Task PublishOrderPlacedAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default);
    Task PublishOrderStatusChangedAsync(OrderStatusChangedEvent @event, CancellationToken cancellationToken = default);
}

