using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Abstractions.Repositories;

public interface ISubCategoryRepository
{
    Task<IReadOnlyList<SubCategoryDto>> GetActiveByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SubCategoryDto?> GetByIdAsync(int subCategoryId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default);
    Task UpdateAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int subCategoryId, CancellationToken cancellationToken = default);
}

