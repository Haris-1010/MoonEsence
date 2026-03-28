using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Events;

public record OrderPlacedEvent(long OrderId, int? UserId, Guid AccessCode, decimal TotalAmount, DateTime CreatedAt, OrderStatus Status);

public record OrderStatusChangedEvent(long OrderId, int? UserId, Guid AccessCode, OrderStatus NewStatus, DateTime UpdatedAt);

