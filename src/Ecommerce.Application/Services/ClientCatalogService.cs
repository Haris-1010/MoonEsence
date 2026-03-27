using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;

namespace Ecommerce.Application.Services;

public class ClientCatalogService : IClientCatalogService
{
    private readonly ICategoryRepository _categories;
    private readonly ISubCategoryRepository _subCategories;
    private readonly IProductRepository _products;

    public ClientCatalogService(ICategoryRepository categories, ISubCategoryRepository subCategories, IProductRepository products)
    {
        _categories = categories;
        _subCategories = subCategories;
        _products = products;
    }

    public Task<IReadOnlyList<CategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
        => _categories.GetActiveAsync(cancellationToken);

    public Task<IReadOnlyList<SubCategoryDto>> GetActiveSubCategoriesAsync(int categoryId, CancellationToken cancellationToken = default)
        => _subCategories.GetActiveByCategoryAsync(categoryId, cancellationToken);

    public Task<PagedResult<ProductListItemDto>> GetProductsAsync(int? categoryId, int? subCategoryId, int page, int pageSize, CancellationToken cancellationToken = default)
        => _products.GetProductsAsync(categoryId, subCategoryId, page, pageSize, cancellationToken);

    public Task<ProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default)
        => _products.GetDetailAsync(productId, cancellationToken);
}

