using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.ViewModels;

public class AdminCategoriesIndexViewModel
{
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
}

