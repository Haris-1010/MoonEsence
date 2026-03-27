using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;

namespace Ecommerce.Application.Abstractions.Services;

public interface IAdminCatalogService
{
    // Products
    Task<PagedResult<ProductListItemDto>> SearchProductsAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductForEditAsync(int productId, CancellationToken cancellationToken = default);
    Task<int> CreateProductAsync(ProductCreateDto createDto, string? imageUrl, CancellationToken cancellationToken = default);
    Task UpdateProductAsync(ProductUpdateDto updateDto, string? imageUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryForEditAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<int> CreateCategoryAsync(CategoryDto category, CancellationToken cancellationToken = default);
    Task UpdateCategoryAsync(CategoryDto category, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    // SubCategories
    Task<IReadOnlyList<SubCategoryDto>> GetAllSubCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubCategoryDto>> GetSubCategoriesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<SubCategoryDto?> GetSubCategoryForEditAsync(int subCategoryId, CancellationToken cancellationToken = default);
    Task<int> CreateSubCategoryAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default);
    Task UpdateSubCategoryAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default);
    Task<bool> DeleteSubCategoryAsync(int subCategoryId, CancellationToken cancellationToken = default);
}

