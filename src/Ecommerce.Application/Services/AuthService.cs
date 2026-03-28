using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<int> RegisterClientAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        var existingByUsername = await _users.GetByUsernameAsync(registerDto.Username, cancellationToken);
        if (existingByUsername is not null)
            throw new InvalidOperationException("Username already exists.");

        var existingByEmail = await _users.GetByEmailAsync(registerDto.Email, cancellationToken);
        if (existingByEmail is not null)
            throw new InvalidOperationException("Email already exists.");

        var salt = string.Empty;
        var hash = _passwordHasher.HashPassword(registerDto.Password, out salt);

        var user = new Domain.Entities.User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Client,
            CreatedAt = DateTime.UtcNow
        };

        return await _users.CreateAsync(user, cancellationToken);
    }

    public async Task<AuthSessionDto?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByUsernameAsync(username, cancellationToken);
        if (user is null)
            return null;

        var ok = _passwordHasher.VerifyPassword(password, user.PasswordSalt, user.PasswordHash);
        if (!ok)
            return null;

        return new AuthSessionDto
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role
        };
    }
}

