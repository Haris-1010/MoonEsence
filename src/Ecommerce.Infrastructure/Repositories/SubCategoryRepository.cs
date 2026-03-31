using System.Data;
using System.Linq;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.DTOs;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class SubCategoryRepository : ISubCategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SubCategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<SubCategoryDto>> GetActiveByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ""Id"", ""CategoryId"", ""Name"", ""Description"", ""IsActive""
FROM ""SubCategories""
WHERE ""IsActive"" = TRUE AND ""CategoryId"" = @CategoryId
ORDER BY ""CreatedAt"" DESC;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var items = await conn.QueryAsync<SubCategoryDto>(
            new CommandDefinition(sql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));

        return items.ToList();
    }

    public async Task<IReadOnlyList<SubCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ""Id"", ""CategoryId"", ""Name"", ""Description"", ""IsActive""
FROM ""SubCategories""
WHERE ""IsActive"" = TRUE
ORDER BY ""CreatedAt"" DESC;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var items = await conn.QueryAsync<SubCategoryDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return items.ToList();
    }

    public async Task<SubCategoryDto?> GetByIdAsync(int subCategoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ""Id"", ""CategoryId"", ""Name"", ""Description"", ""IsActive""
FROM ""SubCategories""
WHERE ""Id"" = @SubCategoryId;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.QuerySingleOrDefaultAsync<SubCategoryDto>(
            new CommandDefinition(sql, new { SubCategoryId = subCategoryId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(SubCategoryDto subCategory, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO ""SubCategories"" (""CategoryId"", ""Name"", ""Description"", ""IsActive"", ""CreatedAt"")
VALUES (@CategoryId, @Name, @Description, @IsActive, NOW())
RETURNING ""Id"";";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.ExecuteScalarAsync<int>(
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
UPDATE ""SubCategories""
SET ""CategoryId"" = @CategoryId,
    ""Name"" = @Name,
    ""Description"" = @Description,
    ""IsActive"" = @IsActive
WHERE ""Id"" = @Id;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        await conn.ExecuteAsync(
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
UPDATE ""SubCategories""
SET ""IsActive"" = FALSE
WHERE ""Id"" = @SubCategoryId;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var rows = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { SubCategoryId = subCategoryId }, cancellationToken: cancellationToken));

        return rows > 0;
    }
}