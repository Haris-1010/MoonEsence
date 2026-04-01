using Ecommerce.Application.Abstractions.Events;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICartService _cartService;
    private readonly IOrderEventPublisher _events;

    public OrderService(IOrderRepository orders, ICartService cartService, IOrderEventPublisher events)
    {
        _orders = orders;
        _cartService = cartService;
        _events = events;
    }

    public async Task<CheckoutResultDto> CheckoutAsync(
        string fullName,
        string phone,
        string? email,
        string city,
        string addressLine1,
        string? addressLine2,
        string? zipCode, decimal deliveryCharge,
        IReadOnlyList<CartLineQuantityDto> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        var cartSummary = await _cartService.BuildCartSummaryAsync(lines, cancellationToken);
        if (cartSummary.TotalItems <= 0)
            throw new InvalidOperationException("Cart is empty.");

        var result = await _orders.CreateOrderAsync(
            fullName,
            phone,
            email,
            city,
            addressLine1,
            addressLine2,
            zipCode,
            deliveryCharge,
            lines,
            cancellationToken);

        await _events.PublishOrderPlacedAsync(
            new Ecommerce.Application.Events.OrderPlacedEvent(
                result.OrderId,
                null,
                result.AccessCode,
                cartSummary.FinalTotal,
                DateTime.UtcNow,
                OrderStatus.Placed),
            cancellationToken);

        return result;
    }

    public Task<PagedResult<OrderSummaryDto>> GetOrderHistoryAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => _orders.GetOrderHistoryAsync(userId, page, pageSize, cancellationToken);

    public Task<PagedResult<OrderSummaryDto>> GetOrderHistoryByEmailOrPhoneAsync(
        string emailOrPhone,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => _orders.GetOrderHistoryByEmailOrPhoneAsync(emailOrPhone, page, pageSize, cancellationToken);

    public Task<OrderDetailDto?> GetOrderDetailAsync(
        int userId,
        long orderId,
        CancellationToken cancellationToken = default)
        => _orders.GetOrderDetailForUserAsync(userId, orderId, cancellationToken);

    public Task<OrderDetailDto?> GetOrderDetailByAccessCodeAsync(
        long orderId,
        Guid accessCode,
        CancellationToken cancellationToken = default)
        => _orders.GetOrderDetailByAccessCodeAsync(orderId, accessCode, cancellationToken);

    public Task<OrderDetailDto?> GetOrderDetailForAdminAsync(
        long orderId,
        CancellationToken cancellationToken = default)
        => _orders.GetOrderDetailForAdminAsync(orderId, cancellationToken);

    public Task<PagedResult<AdminOrderSummaryDto>> GetAdminOrdersAsync(
        OrderStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => _orders.GetAdminOrdersAsync(status, fromUtc, toUtc, page, pageSize, cancellationToken);

    public async Task<OrderStatusChangedDto?> UpdateOrderStatusAsync(
        long orderId,
        OrderStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var changed = await _orders.UpdateOrderStatusAsync(orderId, newStatus, cancellationToken);
        if (changed is null)
            return null;

        await _events.PublishOrderStatusChangedAsync(
            new Ecommerce.Application.Events.OrderStatusChangedEvent(
                changed.OrderId,
                changed.UserId,
                changed.AccessCode,
                changed.NewStatus,
                changed.UpdatedAt),
            cancellationToken);

        return changed;
    }

    public Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
        => _orders.GetAdminDashboardAsync(cancellationToken);
}