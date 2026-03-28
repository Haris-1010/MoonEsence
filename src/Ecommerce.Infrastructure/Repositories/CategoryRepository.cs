using System.Data;
using System.Linq;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.DTOs;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnection _connection;

    public CategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT Id, Name, Description, IsActive
FROM Categories
WHERE IsActive = 1
ORDER BY CreatedAt DESC;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var items = await _connection.QueryAsync<CategoryDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return items.ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT Id, Name, Description, IsActive
FROM Categories
WHERE Id = @CategoryId;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.QuerySingleOrDefaultAsync<CategoryDto>(
            new CommandDefinition(sql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(CategoryDto category, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO Categories (Name, Description, IsActive, CreatedAt)
OUTPUT INSERTED.Id
VALUES (@Name, @Description, @IsActive, SYSUTCDATETIME());";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(CategoryDto category, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE Categories
SET Name = @Name,
    Description = @Description,
    IsActive = @IsActive
WHERE Id = @Id;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        }, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE Categories
SET IsActive = 0
WHERE Id = @CategoryId;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var rows = await _connection.ExecuteAsync(new CommandDefinition(sql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));
        return rows > 0;
    }
}

