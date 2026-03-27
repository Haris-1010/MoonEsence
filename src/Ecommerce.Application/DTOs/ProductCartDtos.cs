namespace Ecommerce.Application.DTOs;

public class ProductCartInfoDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

