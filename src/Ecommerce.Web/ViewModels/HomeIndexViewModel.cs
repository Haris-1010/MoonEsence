using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;

namespace Ecommerce.Web.ViewModels;

public class HomeIndexViewModel
{
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<SubCategoryDto> SubCategories { get; set; } = Array.Empty<SubCategoryDto>();
    public PagedResult<ProductListItemDto> Products { get; set; } = default!;
    public int? SelectedCategoryId { get; set; }
    public int? SelectedSubCategoryId { get; set; }
    public int CurrentPage { get; set; }
}

