using Ecommerce.Application.DTOs;

namespace Ecommerce.Web.ViewModels;

public class AdminProductFormViewModel
{
    public int? ProductId { get; set; }
    public IReadOnlyList<CategoryDto> Categories { get; set; } = Array.Empty<CategoryDto>();
    public IReadOnlyList<SubCategoryDto> SubCategories { get; set; } = Array.Empty<SubCategoryDto>();
    public ProductCreateDto Create { get; set; } = new();
    public ProductUpdateDto Update { get; set; } = new();
    public string? ExistingImageUrl { get; set; }
}

