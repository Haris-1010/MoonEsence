using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;

namespace Ecommerce.Web.ViewModels;

public class AdminProductsIndexViewModel
{
    public PagedResult<ProductListItemDto> Products { get; set; } = default!;
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
}

