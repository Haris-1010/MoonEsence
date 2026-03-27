using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;

namespace Ecommerce.Application.Abstractions.Services;

public interface IClientCatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubCategoryDto>> GetActiveSubCategoriesAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductListItemDto>> GetProductsAsync(int? categoryId, int? subCategoryId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default);
}

