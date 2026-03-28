namespace Ecommerce.Application.DTOs;

public class CheckoutResultDto
{
    public long OrderId { get; set; }
    public Guid AccessCode { get; set; }
}

