using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;

namespace Ecommerce.Application.Abstractions.Repositories;

public interface IProductRepository
{
    Task<PagedResult<ProductListItemDto>> GetProductsAsync(int? categoryId, int? subCategoryId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductListItemDto>> SearchAdminAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetDetailAsync(int productId, CancellationToken cancellationToken = default);

    Task<ProductDetailDto?> GetForEditAsync(int productId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(ProductCreateDto product, string imageUrl, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProductUpdateDto product, string? imageUrl, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductCartInfoDto>> GetCartProductsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);
}

