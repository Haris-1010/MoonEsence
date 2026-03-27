using System.Data;
using System.Linq;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.DTOs;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class SubCategoryRepository : ISubCategoryRepository
{
    private readonly IDbConnection _connection;

    public SubCategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
    }

    public async Task<IReadOnlyList<SubCategoryDto>> GetActiveByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT Id, CategoryId, Name, Description, IsActive
FROM SubCategories
WHERE IsActive = 1 AND CategoryId = @CategoryId
ORDER BY CreatedAt DESC;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var items = await _connection.QueryAsync<SubCategoryDto>(
            new CommandDefinition(sql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));

        return items.ToList();
    }

    public async Task<IReadOnlyList<SubCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT Id, CategoryId, Name, Description, IsActive
FROM SubCategories
WHERE IsActive = 1
ORDER BY CreatedAt DESC;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var items = await _connection.QueryAsync<SubCategoryDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return items.ToList();
    }

    public async Task<SubCategoryDto?> GetByIdAsync(int subCategoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT Id, CategoryId, Name, Description, IsActive
FROM SubCategories
WHERE Id = @SubCategoryId;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.QuerySingleOrDefaultAsync<SubCategoryDto>(
            new CommandDefinition(sql, new { SubCategoryId = subCategoryId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO SubCategories (CategoryId, Name, Description, IsActive, CreatedAt)
OUTPUT INSERTED.Id
VALUES (@CategoryId, @Name, @Description, @IsActive, SYSUTCDATETIME());";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new
                {
                    CategoryId = subCategory.CategoryId,
                    Name = subCategory.Name,
                    Description = subCategory.Description,
                    IsActive = subCategory.IsActive
                },
                cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE SubCategories
SET CategoryId = @CategoryId,
    Name = @Name,
    Description = @Description,
    IsActive = @IsActive
WHERE Id = @Id;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        await _connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = subCategory.Id,
                    CategoryId = subCategory.CategoryId,
                    subCategory.Name,
                    subCategory.Description,
                    IsActive = subCategory.IsActive
                },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteAsync(int subCategoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE SubCategories
SET IsActive = 0
WHERE Id = @SubCategoryId;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var rows = await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { SubCategoryId = subCategoryId }, cancellationToken: cancellationToken));

        return rows > 0;
    }
}

