using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.ViewModels;

public class CartSummaryViewModel
{
    public CartSummaryDto Summary { get; set; } = new();
}

