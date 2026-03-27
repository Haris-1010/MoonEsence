using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;

namespace Ecommerce.Web.ViewModels;

public class OrderHistoryViewModel
{
    public PagedResult<OrderSummaryDto> Orders { get; set; } = default!;
}

