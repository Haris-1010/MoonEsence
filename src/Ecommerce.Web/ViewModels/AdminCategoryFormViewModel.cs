using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.ViewModels;

public class AdminCategoryFormViewModel
{
    public int? CategoryId { get; set; }
    public CategoryDto Category { get; set; } = new();
}

