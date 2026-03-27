using System.ComponentModel.DataAnnotations;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.DTOs;

public class OrderSummaryDto
{
    public long OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid AccessCode { get; set; }
}

public class OrderDetailDto
{
    public long OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }

    public Guid AccessCode { get; set; }

    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string City { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string ZipCode { get; set; } = string.Empty;

    public IReadOnlyList<OrderItemDto> Items { get; set; } = Array.Empty<OrderItemDto>();
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderUpdateStatusDto
{
    [Required]
    public long OrderId { get; set; }

    [Required]
    public OrderStatus NewStatus { get; set; }
}

public class OrderStatusChangedDto
{
    public long OrderId { get; set; }
    public int? UserId { get; set; }
    public OrderStatus NewStatus { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid AccessCode { get; set; }
}

