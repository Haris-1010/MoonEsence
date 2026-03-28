using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.DTOs;

public class AdminDashboardDto
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public long TotalOrders { get; set; }
    public int NewOrdersToday { get; set; }
    public int PendingOrders { get; set; }
}

public class AdminOrderSummaryDto
{
    public long OrderId { get; set; }
    public int? UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
}

