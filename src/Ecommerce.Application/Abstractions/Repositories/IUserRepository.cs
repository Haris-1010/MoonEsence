using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<Domain.Entities.User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Domain.Entities.User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(Domain.Entities.User user, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
}