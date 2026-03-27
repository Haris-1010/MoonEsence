using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Web.ViewModels;

public class AdminOrdersIndexViewModel
{
    public PagedResult<AdminOrderSummaryDto> Orders { get; set; } = default!;
    public OrderStatus? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)Orders.TotalCount / PageSize);
}