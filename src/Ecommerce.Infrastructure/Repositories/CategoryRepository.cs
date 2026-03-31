using System.Data;
using System.Linq;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.DTOs;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ""Id"", ""Name"", ""Description"", ""IsActive""
FROM ""Categories""
WHERE ""IsActive"" = TRUE
ORDER BY ""CreatedAt"" DESC;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var items = await conn.QueryAsync<CategoryDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return items.ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT ""Id"", ""Name"", ""Description"", ""IsActive""
FROM ""Categories""
WHERE ""Id"" = @CategoryId;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.QuerySingleOrDefaultAsync<CategoryDto>(new CommandDefinition(sql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(CategoryDto category, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO ""Categories"" (""Name"", ""Description"", ""IsActive"", ""CreatedAt"")
VALUES (@Name, @Description, @IsActive, NOW())
RETURNING ""Id"";";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(CategoryDto category, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE ""Categories""
SET ""Name"" = @Name,
    ""Description"" = @Description,
    ""IsActive"" = @IsActive
WHERE ""Id"" = @Id;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        await conn.ExecuteAsync(new CommandDefinition(sql, new
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
UPDATE ""Categories""
SET ""IsActive"" = FALSE
WHERE ""Id"" = @CategoryId;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { CategoryId = categoryId }, cancellationToken: cancellationToken));
        return rows > 0;
    }
}