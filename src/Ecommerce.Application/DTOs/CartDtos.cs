using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class CartLineDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public string ImageUrl { get; set; } = string.Empty;
}

public class CartLineQuantityDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CartSummaryDto
{
    public IReadOnlyList<CartLineDto> Lines { get; set; } = Array.Empty<CartLineDto>();
    public decimal Subtotal { get; set; }
    public int TotalItems { get; set; }
}

public class CheckoutDto
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(30, MinimumLength = 7)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [Required, StringLength(80, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 5)]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(200)]
    public string? AddressLine2 { get; set; }

    [ StringLength(20, MinimumLength = 3)]
    public string? ZipCode { get; set; } = string.Empty;
}

