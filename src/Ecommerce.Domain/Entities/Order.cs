using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities;

public class Order
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public Guid AccessCode { get; set; }

    // Simple checkout fields (extend if needed)
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string City { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string? ZipCode { get; set; } = string.Empty;

    public List<OrderItem> Items { get; set; } = [];
}

