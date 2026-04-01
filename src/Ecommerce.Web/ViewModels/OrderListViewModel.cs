namespace Ecommerce.Web.ViewModels;

public class OrderListViewModel
{
    public long OrderId { get; set; }
    public string? CustomerPhone { get; set; } = string.Empty;
    public string? CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string City { get; set; } = string.Empty;
}