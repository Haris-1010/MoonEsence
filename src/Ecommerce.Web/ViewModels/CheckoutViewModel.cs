using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.ViewModels;

public class CheckoutViewModel
{
    public CartSummaryDto Cart { get; set; } = new();
    public CheckoutDto Checkout { get; set; } = new();
}

