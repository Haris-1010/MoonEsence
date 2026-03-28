using System.Data;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP 1 
    Id, Username, Email, PasswordHash, PasswordSalt, Role, CreatedAt
FROM Users
WHERE Username = @Username;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Username = username }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP 1 
    Id, Username, Email, PasswordHash, PasswordSalt, Role, CreatedAt
FROM Users
WHERE Email = @Email;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT TOP 1 
    Id, Username, Email, PasswordHash, PasswordSalt, Role, CreatedAt
FROM Users
WHERE Id = @UserId;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, Role, CreatedAt)
OUTPUT INSERTED.Id
VALUES (@Username, @Email, @PasswordHash, @PasswordSalt, @Role, @CreatedAt);";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            user,
            cancellationToken: cancellationToken));
    }
}

