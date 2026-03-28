using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.ViewModels;

public class ProductGridViewModel
{
    public IReadOnlyList<ProductListItemDto> Products { get; set; } = Array.Empty<ProductListItemDto>();
}

