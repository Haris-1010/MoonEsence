using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;

namespace Ecommerce.Application.Services;

public class AdminCatalogService : IAdminCatalogService
{
    private readonly ICategoryRepository _categories;
    private readonly ISubCategoryRepository _subCategories;
    private readonly IProductRepository _products;

    public AdminCatalogService(ICategoryRepository categories, ISubCategoryRepository subCategories, IProductRepository products)
    {
        _categories = categories;
        _subCategories = subCategories;
        _products = products;
    }

    public Task<PagedResult<ProductListItemDto>> SearchProductsAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default)
        => _products.SearchAdminAsync(search, categoryId, page, pageSize, cancellationToken);

    public Task<ProductDetailDto?> GetProductForEditAsync(int productId, CancellationToken cancellationToken = default)
        => _products.GetForEditAsync(productId, cancellationToken);

    public Task<int> CreateProductAsync(ProductCreateDto createDto, string? imageUrl, CancellationToken cancellationToken = default)
        => _products.CreateAsync(createDto, imageUrl, cancellationToken);

    public Task UpdateProductAsync(ProductUpdateDto updateDto, string? imageUrl, CancellationToken cancellationToken = default)
        => _products.UpdateAsync(updateDto, imageUrl, cancellationToken);

    public Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
        => _products.DeleteAsync(productId, cancellationToken);

    public Task<IReadOnlyList<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        => _categories.GetActiveAsync(cancellationToken);

    public Task<CategoryDto?> GetCategoryForEditAsync(int categoryId, CancellationToken cancellationToken = default)
        => _categories.GetByIdAsync(categoryId, cancellationToken);

    public Task<int> CreateCategoryAsync(CategoryDto category, CancellationToken cancellationToken = default)
        => _categories.CreateAsync(category, cancellationToken);

    public Task UpdateCategoryAsync(CategoryDto category, CancellationToken cancellationToken = default)
        => _categories.UpdateAsync(category, cancellationToken);

    public Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
        => _categories.DeleteAsync(categoryId, cancellationToken);

    public Task<IReadOnlyList<SubCategoryDto>> GetAllSubCategoriesAsync(CancellationToken cancellationToken = default)
        => _subCategories.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<SubCategoryDto>> GetSubCategoriesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
        => _subCategories.GetActiveByCategoryAsync(categoryId, cancellationToken);

    public Task<SubCategoryDto?> GetSubCategoryForEditAsync(int subCategoryId, CancellationToken cancellationToken = default)
        => _subCategories.GetByIdAsync(subCategoryId, cancellationToken);

    public Task<int> CreateSubCategoryAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default)
        => _subCategories.CreateAsync(subCategory, cancellationToken);

    public Task UpdateSubCategoryAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default)
        => _subCategories.UpdateAsync(subCategory, cancellationToken);

    public Task<bool> DeleteSubCategoryAsync(int subCategoryId, CancellationToken cancellationToken = default)
        => _subCategories.DeleteAsync(subCategoryId, cancellationToken);
}

