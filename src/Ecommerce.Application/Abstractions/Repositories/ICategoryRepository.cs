using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Abstractions.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(CategoryDto category, CancellationToken cancellationToken = default);
    Task UpdateAsync(CategoryDto category, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int categoryId, CancellationToken cancellationToken = default);
}

