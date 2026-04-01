using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Abstractions.Repositories;

public interface IOrderRepository
{
    Task<CheckoutResultDto> CreateOrderAsync(
        string fullName,
        string phone,
        string? email,
        string city,
        string addressLine1,
        string? addressLine2,
        string zipCode, decimal deliveryCharge,
        IReadOnlyList<CartLineQuantityDto> lines,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryDto>> GetOrderHistoryAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryDto>> GetOrderHistoryByEmailOrPhoneAsync(string emailOrPhone, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<OrderDetailDto?> GetOrderDetailForUserAsync(int userId, long orderId, CancellationToken cancellationToken = default);

    Task<OrderDetailDto?> GetOrderDetailByAccessCodeAsync(long orderId, Guid accessCode, CancellationToken cancellationToken = default);

    Task<OrderDetailDto?> GetOrderDetailForAdminAsync(long orderId, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminOrderSummaryDto>> GetAdminOrdersAsync(
        OrderStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrderStatusChangedDto?> UpdateOrderStatusAsync(long orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);

    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
    Task<int> GetNewOrdersTodayAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);
}

