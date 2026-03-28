using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.ViewModels;

public class AdminSubCategoryFormViewModel
{
    public int? SubCategoryId { get; set; }
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public SubCategoryDto SubCategory { get; set; } = new();
}

