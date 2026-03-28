using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.ViewModels;

public class AdminSubCategoriesIndexViewModel
{
    public IReadOnlyList<SubCategoryDto> SubCategories { get; set; } = Array.Empty<SubCategoryDto>();
}

