using System.Data;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""PasswordSalt"", ""Role"", ""CreatedAt""
FROM ""Users""
WHERE ""Username"" = @Username
LIMIT 1;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Username = username }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""PasswordSalt"", ""Role"", ""CreatedAt""
FROM ""Users""
WHERE ""Email"" = @Email
LIMIT 1;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""PasswordSalt"", ""Role"", ""CreatedAt""
FROM ""Users""
WHERE ""Id"" = @UserId
LIMIT 1;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO ""Users"" (""Username"", ""Email"", ""PasswordHash"", ""PasswordSalt"", ""Role"", ""CreatedAt"")
VALUES (@Username, @Email, @PasswordHash, @PasswordSalt, @Role, NOW())
RETURNING ""Id"";";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new
            {
                user.Username,
                user.Email,
                user.PasswordHash,
                user.PasswordSalt,
                user.Role
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    ""Id"", ""Username"", ""Email"", ""Role"", ""CreatedAt""
FROM ""Users""
ORDER BY ""CreatedAt"" DESC;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        var users = await conn.QueryAsync<User>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return users.ToList();
    }
}