using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Abstractions.Services;

public interface IAuthService
{
    Task<int> RegisterClientAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);
    Task<AuthSessionDto?> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}

