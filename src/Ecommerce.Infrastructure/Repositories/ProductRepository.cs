using System.Data;
using System.Linq;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbConnection _connection;

    public ProductRepository(IDbConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
    }

    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(int? categoryId, int? subCategoryId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;
        const string countSql = @"
SELECT COUNT(1)
FROM Products p
INNER JOIN SubCategories sc ON sc.Id = p.SubCategoryId
INNER JOIN Categories c ON c.Id = sc.CategoryId
WHERE p.IsActive = 1
  AND sc.IsActive = 1
  AND c.IsActive = 1
  AND (@CategoryId IS NULL OR c.Id = @CategoryId)
  AND (@SubCategoryId IS NULL OR p.SubCategoryId = @SubCategoryId);";

        const string sql = @"
SELECT
    p.Id,
    p.Name,
    p.Description,
    p.Price,p.RegularPrice,
    p.SubCategoryId,
    c.Id AS CategoryId,
    c.Name AS CategoryName,
    sc.Name AS SubCategoryName,
    p.ImageUrl
FROM Products p
INNER JOIN SubCategories sc ON sc.Id = p.SubCategoryId
INNER JOIN Categories c ON c.Id = sc.CategoryId
WHERE p.IsActive = 1
  AND sc.IsActive = 1
  AND c.IsActive = 1
  AND (@CategoryId IS NULL OR c.Id = @CategoryId)
  AND (@SubCategoryId IS NULL OR p.SubCategoryId = @SubCategoryId)
ORDER BY p.CreatedAt DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { CategoryId = categoryId, SubCategoryId = subCategoryId }, cancellationToken: cancellationToken));

        var items = (await _connection.QueryAsync<ProductListItemDto>(
            new CommandDefinition(sql, new { CategoryId = categoryId, SubCategoryId = subCategoryId, Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken)))
            .ToList();

        return new PagedResult<ProductListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<PagedResult<ProductListItemDto>> SearchAdminAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;
        const string countSql = @"
SELECT COUNT(1)
FROM Products p
INNER JOIN SubCategories sc ON sc.Id = p.SubCategoryId
INNER JOIN Categories c ON c.Id = sc.CategoryId
WHERE (@Search IS NULL OR (p.Name LIKE '%' + @Search + '%' OR p.Description LIKE '%' + @Search + '%'))
  AND (@CategoryId IS NULL OR c.Id = @CategoryId)
  AND sc.IsActive = 1
  AND c.IsActive = 1
  AND p.IsActive = 1;";

        const string sql = @"
SELECT
    p.Id,
    p.Name,
    p.Description,
    p.Price,p.RegularPrice,
    p.SubCategoryId,
    c.Id AS CategoryId,
    c.Name AS CategoryName,
    sc.Name AS SubCategoryName,
    p.ImageUrl
FROM Products p
INNER JOIN SubCategories sc ON sc.Id = p.SubCategoryId
INNER JOIN Categories c ON c.Id = sc.CategoryId
WHERE (@Search IS NULL OR (p.Name LIKE '%' + @Search + '%' OR p.Description LIKE '%' + @Search + '%'))
  AND p.IsActive = 1
  AND sc.IsActive = 1
  AND c.IsActive = 1
  AND (@CategoryId IS NULL OR c.Id = @CategoryId)
ORDER BY p.CreatedAt DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var totalCount = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { Search = string.IsNullOrWhiteSpace(search) ? null : search, CategoryId = categoryId }, cancellationToken: cancellationToken));

        var items = (await _connection.QueryAsync<ProductListItemDto>(
            new CommandDefinition(sql, new { Search = string.IsNullOrWhiteSpace(search) ? null : search, CategoryId = categoryId, Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken)))
            .ToList();

        return new PagedResult<ProductListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<ProductDetailDto?> GetDetailAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    p.Id,
    p.Name,
    p.Description,
    p.Price,p.RegularPrice,
    p.SubCategoryId,
    c.Id AS CategoryId,
    c.Name AS CategoryName,
    sc.Name AS SubCategoryName,
    p.ImageUrl,
    p.IsActive
FROM Products p
INNER JOIN SubCategories sc ON sc.Id = p.SubCategoryId
INNER JOIN Categories c ON c.Id = sc.CategoryId
WHERE p.Id = @ProductId
  AND p.IsActive = 1
  AND sc.IsActive = 1
  AND c.IsActive = 1;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.QuerySingleOrDefaultAsync<ProductDetailDto>(
            new CommandDefinition(sql, new { ProductId = productId }, cancellationToken: cancellationToken));
    }

    public async Task<ProductDetailDto?> GetForEditAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    p.Id,
    p.Name,
    p.Description,
    p.Price,
    p.RegularPrice,
    p.SubCategoryId,
    c.Id AS CategoryId,
    c.Name AS CategoryName,
    sc.Name AS SubCategoryName,
    p.ImageUrl,
    p.IsActive
FROM Products p
INNER JOIN SubCategories sc ON sc.Id = p.SubCategoryId
INNER JOIN Categories c ON c.Id = sc.CategoryId
WHERE p.Id = @ProductId;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.QuerySingleOrDefaultAsync<ProductDetailDto>(
            new CommandDefinition(sql, new { ProductId = productId }, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateAsync(ProductCreateDto product, string imageUrl, CancellationToken cancellationToken = default)
    {
        const string sql = @"
INSERT INTO Products (Name, Description, Price, RegularPrice, SubCategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt)
OUTPUT INSERTED.Id
VALUES (@Name, @Description, @Price,@RegularPrice, @SubCategoryId, @ImageUrl, @IsActive, SYSUTCDATETIME(), SYSUTCDATETIME());";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        return await _connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
        {
            product.Name,
            product.Description,
            product.Price,
            product.RegularPrice,
            product.SubCategoryId,
            ImageUrl = imageUrl ?? string.Empty,
            product.IsActive
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(ProductUpdateDto product, string? imageUrl, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE Products
SET Name = @Name,
    Description = @Description,
    Price = @Price,
     RegularPrice = @RegularPrice,
    SubCategoryId = @SubCategoryId,
    ImageUrl = COALESCE(@ImageUrl, ImageUrl),
    IsActive = @IsActive,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        await _connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.RegularPrice,
            product.SubCategoryId,
            ImageUrl = imageUrl,
            product.IsActive
        }, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE Products
SET IsActive = 0,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @ProductId;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var rows = await _connection.ExecuteAsync(new CommandDefinition(sql, new { ProductId = productId }, cancellationToken: cancellationToken));
        return rows > 0;
    }

    public async Task<IReadOnlyList<ProductCartInfoDto>> GetCartProductsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
    {
        // Dapper supports expanding list parameters for IN clauses.
        const string sql = @"
SELECT
    p.Id AS ProductId,
    p.Name AS ProductName,
    p.Price AS UnitPrice,
    p.ImageUrl,
    p.IsActive
FROM Products p
INNER JOIN SubCategories sc ON sc.Id = p.SubCategoryId
INNER JOIN Categories c ON c.Id = sc.CategoryId
WHERE p.IsActive = 1
  AND sc.IsActive = 1
  AND c.IsActive = 1
  AND p.Id IN @ProductIds;";

        var ids = productIds?.Distinct().ToArray() ?? Array.Empty<int>();
        if (ids.Length == 0)
            return Array.Empty<ProductCartInfoDto>();

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var items = await _connection.QueryAsync<ProductCartInfoDto>(
            new CommandDefinition(sql, new { ProductIds = ids }, cancellationToken: cancellationToken));

        return items.ToList();
    }
}