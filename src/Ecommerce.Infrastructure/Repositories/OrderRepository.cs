using System.Data;
using System.Linq;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnection _connection;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
    }

    public async Task<CheckoutResultDto> CreateOrderAsync(
      string fullName,
      string phone,
      string? email,
      string city,
      string addressLine1,
      string? addressLine2,
      string? zipCode,
      decimal deliveryCharge,                    // ← Received from checkout
      IReadOnlyList<CartLineQuantityDto> lines,
      CancellationToken cancellationToken = default)
    {
        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("Cart lines empty.");

        var productIds = lines.Select(x => x.ProductId).Distinct().ToArray();
        if (productIds.Length == 0)
            throw new InvalidOperationException("Cart product ids empty.");

        var accessCode = Guid.NewGuid();

        // Fetch current prices in one round-trip (only active products)
        const string productsSql = @"
SELECT
    p.Id AS ProductId,
    p.Price AS UnitPrice
FROM Products p
INNER JOIN SubCategories sc ON sc.Id = p.SubCategoryId
INNER JOIN Categories c ON c.Id = sc.CategoryId
WHERE p.IsActive = 1
  AND sc.IsActive = 1
  AND c.IsActive = 1
  AND p.Id IN @ProductIds;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var productInfos = (await _connection.QueryAsync<(int ProductId, decimal UnitPrice)>(
            new CommandDefinition(productsSql, new { ProductIds = productIds }, cancellationToken: cancellationToken))).ToList();

        if (productInfos.Count != productIds.Length)
            throw new InvalidOperationException("Some products are no longer available.");

        var now = DateTime.UtcNow;

        using var tx = _connection.BeginTransaction();
        try
        {
            // Compute subtotal from products
            decimal subtotal = 0m;
            var orderItems = new List<(int ProductId, int Quantity, decimal UnitPrice, decimal LineTotal)>(lines.Count);

            foreach (var line in lines)
            {
                var qty = Math.Max(0, line.Quantity);
                if (qty == 0) continue;

                var product = productInfos.First(p => p.ProductId == line.ProductId);
                var lineTotal = product.UnitPrice * qty;

                orderItems.Add((line.ProductId, qty, product.UnitPrice, lineTotal));
                subtotal += lineTotal;
            }

            if (orderItems.Count == 0)
                throw new InvalidOperationException("Cart is empty.");

            // Final Total = Subtotal + Delivery Charge
            decimal finalTotal = subtotal + deliveryCharge;

            // Updated INSERT SQL with DeliveryCharge
            const string insertOrderSql = @"
INSERT INTO Orders 
    (UserId, Status, PaymentMethod, CreatedAt, UpdatedAt, TotalAmount, DeliveryCharge, 
     FullName, Phone, Email, City, AddressLine1, AddressLine2, ZipCode, AccessCode)
OUTPUT INSERTED.Id, INSERTED.AccessCode
VALUES 
    (@UserId, @Status, @PaymentMethod, @CreatedAt, @UpdatedAt, @TotalAmount, @DeliveryCharge,
     @FullName, @Phone, @Email, @City, @AddressLine1, @AddressLine2, @ZipCode, @AccessCode);";

            var result = await _connection.QuerySingleAsync<(long OrderId, Guid AccessCode)>(
                new CommandDefinition(
                    insertOrderSql,
                    new
                    {
                        UserId = (int?)null,
                        Status = (int)OrderStatus.Placed,
                        PaymentMethod = (int)PaymentMethod.Cod,
                        CreatedAt = now,
                        UpdatedAt = now,
                        TotalAmount = finalTotal,           // ← Final amount (Subtotal + Delivery)
                        DeliveryCharge = deliveryCharge,    // ← Delivery charge separately saved
                        FullName = fullName,
                        Phone = phone,
                        Email = email,
                        City = city,
                        AddressLine1 = addressLine1,
                        AddressLine2 = addressLine2,
                        ZipCode = zipCode ?? string.Empty,
                        AccessCode = accessCode
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));

            // Insert Order Items
            const string insertItemSql = @"
INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, LineTotal)
VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @LineTotal);";

            foreach (var item in orderItems)
            {
                await _connection.ExecuteAsync(new CommandDefinition(
                    insertItemSql,
                    new
                    {
                        OrderId = result.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        LineTotal = item.LineTotal
                    },
                    transaction: tx,
                    cancellationToken: cancellationToken));
            }

            tx.Commit();

            return new CheckoutResultDto
            {
                OrderId = result.OrderId,
                AccessCode = result.AccessCode
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
    public async Task<PagedResult<OrderSummaryDto>> GetOrderHistoryAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        const string countSql = @"
SELECT COUNT(1)
FROM Orders
WHERE UserId = @UserId;";

        const string sql = @"
SELECT
    Id AS OrderId,
    Status AS Status,
    CreatedAt,
    TotalAmount,
    AccessCode
FROM Orders
WHERE UserId = @UserId
ORDER BY CreatedAt DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var totalCount = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, new { UserId = userId }, cancellationToken: cancellationToken));

        var items = await _connection.QueryAsync<OrderSummaryDto>(
            new CommandDefinition(sql, new { UserId = userId, Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken));

        return new PagedResult<OrderSummaryDto>(items.ToList(), page, pageSize, totalCount);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetOrderHistoryByEmailOrPhoneAsync(string emailOrPhone, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        const string countSql = @"
SELECT COUNT(1)
FROM Orders
WHERE Email = @EmailOrPhone OR Phone = @EmailOrPhone;";

        const string sql = @"
SELECT
    Id AS OrderId,
    Status AS Status,
    CreatedAt,
    TotalAmount,
    AccessCode
FROM Orders
WHERE Email = @EmailOrPhone OR Phone = @EmailOrPhone
ORDER BY CreatedAt DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var totalCount = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            new { EmailOrPhone = emailOrPhone },
            cancellationToken: cancellationToken));

        var items = await _connection.QueryAsync<OrderSummaryDto>(
            new CommandDefinition(
                sql,
                new { EmailOrPhone = emailOrPhone, Offset = offset, PageSize = pageSize },
                cancellationToken: cancellationToken));

        return new PagedResult<OrderSummaryDto>(items.ToList(), page, pageSize, totalCount);
    }

    public async Task<OrderDetailDto?> GetOrderDetailForUserAsync(int userId, long orderId, CancellationToken cancellationToken = default)
    {
        const string orderSql = @"
SELECT
    Id AS OrderId,
    Status AS Status,
    PaymentMethod AS PaymentMethod,
    CreatedAt,
    TotalAmount,
    AccessCode,
    FullName,
    Phone,
    Email,
    City,
    AddressLine1,
    AddressLine2,
    ZipCode
FROM Orders
WHERE Id = @OrderId
  AND UserId = @UserId;";

        const string itemsSql = @"
SELECT
    oi.ProductId,
    p.Name AS ProductName,
    p.ImageUrl AS ImageUrl,
    oi.Quantity,
    oi.UnitPrice,
    oi.LineTotal
FROM OrderItems oi
INNER JOIN Products p ON p.Id = oi.ProductId
WHERE oi.OrderId = @OrderId
ORDER BY oi.Id ASC;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var order = await _connection.QuerySingleOrDefaultAsync<OrderDetailDto>(
            new CommandDefinition(orderSql, new { OrderId = orderId, UserId = userId }, cancellationToken: cancellationToken));
        if (order is null)
            return null;

        var items = await _connection.QueryAsync<OrderItemDto>(
            new CommandDefinition(itemsSql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        order.Items = items.ToList();
        return order;
    }

    public async Task<OrderDetailDto?> GetOrderDetailByAccessCodeAsync(long orderId, Guid accessCode, CancellationToken cancellationToken = default)
    {
        const string orderSql = @"
SELECT
    Id AS OrderId,
    Status AS Status,
    PaymentMethod AS PaymentMethod,
    CreatedAt,
    TotalAmount,
    AccessCode,
    FullName,
    Phone,
    Email,
    City,
    AddressLine1,
    AddressLine2,
    ZipCode,DeliveryCharge
FROM Orders
WHERE Id = @OrderId
  AND AccessCode = @AccessCode;";

        const string itemsSql = @"
SELECT
    oi.ProductId,
    p.Name AS ProductName,
    p.ImageUrl AS ImageUrl,
    oi.Quantity,
    oi.UnitPrice,
    oi.LineTotal
FROM OrderItems oi
INNER JOIN Products p ON p.Id = oi.ProductId
WHERE oi.OrderId = @OrderId
ORDER BY oi.Id ASC;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var order = await _connection.QuerySingleOrDefaultAsync<OrderDetailDto>(
            new CommandDefinition(orderSql, new { OrderId = orderId, AccessCode = accessCode }, cancellationToken: cancellationToken));
        if (order is null)
            return null;

        var items = await _connection.QueryAsync<OrderItemDto>(
            new CommandDefinition(itemsSql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        order.Items = items.ToList();
        return order;
    }

    public async Task<OrderDetailDto?> GetOrderDetailForAdminAsync(long orderId, CancellationToken cancellationToken = default)
    {
        const string orderSql = @"
SELECT
    Id AS OrderId,
    Status AS Status,
    PaymentMethod AS PaymentMethod,
    CreatedAt,
    TotalAmount,
    AccessCode,
DeliveryCharge,
    FullName,
    Phone,
    Email,
    City,
    AddressLine1,
    AddressLine2,
    ZipCode
FROM Orders
WHERE Id = @OrderId;";

        const string itemsSql = @"
SELECT
    oi.ProductId,
    p.Name AS ProductName,
    p.ImageUrl AS ImageUrl,
    oi.Quantity,
    oi.UnitPrice,
    oi.LineTotal
FROM OrderItems oi
INNER JOIN Products p ON p.Id = oi.ProductId
WHERE oi.OrderId = @OrderId
ORDER BY oi.Id ASC;";

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        var order = await _connection.QuerySingleOrDefaultAsync<OrderDetailDto>(
            new CommandDefinition(orderSql, new { OrderId = orderId }, cancellationToken: cancellationToken));
        if (order is null)
            return null;

        var items = await _connection.QueryAsync<OrderItemDto>(
            new CommandDefinition(itemsSql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        order.Items = items.ToList();
        return order;
    }

    public async Task<PagedResult<AdminOrderSummaryDto>> GetAdminOrdersAsync(
        OrderStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string countSql = @"
SELECT COUNT(1)
FROM Orders o
WHERE (@Status IS NULL OR o.Status = @Status)
  AND (@FromUtc IS NULL OR o.CreatedAt >= @FromUtc)
  AND (@ToUtc IS NULL OR o.CreatedAt <= @ToUtc);";

        if (toUtc.HasValue)
        {
            // Make ToUtc exclusive by adding 1 day
            toUtc = toUtc.Value.AddDays(1);
        }

        const string sql = @"
SELECT
    o.Id AS OrderId,
    o.UserId AS UserId,
    o.FullName AS CustomerName,
    o.Phone AS CustomerPhone,
    o.Status AS Status,
    o.CreatedAt,
    o.TotalAmount
FROM Orders o
WHERE (@Status IS NULL OR o.Status = @Status)
  AND (@FromUtc IS NULL OR o.CreatedAt >= @FromUtc)
  AND (@ToUtc IS NULL OR o.CreatedAt < @ToUtc)   -- notice < instead of <=
ORDER BY o.CreatedAt DESC
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;";

        var totalCount = await _connection.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            new
            {
                Status = status.HasValue ? (int)status.Value : (int?)null,
                FromUtc = fromUtc,
                ToUtc = toUtc,
            },
            cancellationToken: cancellationToken));

        var items = await _connection.QueryAsync<AdminOrderSummaryDto>(new CommandDefinition(
            sql,
            new
            {
                Status = status.HasValue ? (int)status.Value : (int?)null,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Offset = offset,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken));

        return new PagedResult<AdminOrderSummaryDto>(items.ToList(), page, pageSize, totalCount);
    }

    public async Task<OrderStatusChangedDto?> UpdateOrderStatusAsync(long orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sql = @"
UPDATE Orders
SET Status = @NewStatus,
    UpdatedAt = SYSUTCDATETIME()
OUTPUT
    INSERTED.Id AS OrderId,
    INSERTED.UserId AS UserId,
    INSERTED.Status AS NewStatus,
    INSERTED.UpdatedAt AS UpdatedAt,
    INSERTED.AccessCode AS AccessCode
WHERE Id = @OrderId;";

        var dto = await _connection.QuerySingleOrDefaultAsync<OrderStatusChangedDto>(
            new CommandDefinition(sql, new { NewStatus = (int)newStatus, OrderId = orderId }, cancellationToken: cancellationToken));

        if (dto is null)
            return null;

        // Dapper will map int to enum by underlying values if compatible.
        return dto;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sql = @"
SELECT
    (SELECT COUNT(1) FROM Products) AS TotalProducts,
    (SELECT COUNT(1) FROM Categories) AS TotalCategories,
    (SELECT COUNT(1) FROM Orders) AS TotalOrders,
    (SELECT COUNT(1) FROM Orders WHERE CreatedAt >= DATEADD(day, DATEDIFF(day, 0, SYSUTCDATETIME()), 0)) AS NewOrdersToday,
    (SELECT COUNT(1) FROM Orders WHERE Status IN (0, 1, 2)) AS PendingOrders;";

        return await _connection.QuerySingleAsync<AdminDashboardDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<int> GetNewOrdersTodayAsync(CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        const string sql = @"
SELECT COUNT(1)
FROM Orders
WHERE CreatedAt >= DATEADD(day, DATEDIFF(day, 0, SYSUTCDATETIME()), 0);";

        return await _connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}