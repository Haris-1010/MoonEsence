using System.Data;
using System.Linq;
using Dapper;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Shared;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Database;

namespace Ecommerce.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CheckoutResultDto> CreateOrderAsync(
      string fullName,
      string phone,
      string? email,
      string city,
      string addressLine1,
      string? addressLine2,
      string? zipCode,
      decimal deliveryCharge,
      IReadOnlyList<CartLineQuantityDto> lines,
      CancellationToken cancellationToken = default)
    {
        if (lines is null || lines.Count == 0)
            throw new InvalidOperationException("Cart lines empty.");

        var productIds = lines.Select(x => x.ProductId).Distinct().ToArray();
        if (productIds.Length == 0)
            throw new InvalidOperationException("Cart product ids empty.");

        var accessCode = Guid.NewGuid();

        const string productsSql = @"
SELECT
    p.""Id"" AS ProductId,
    p.""Price"" AS UnitPrice
FROM ""Products"" p
INNER JOIN ""SubCategories"" sc ON sc.""Id"" = p.""SubCategoryId""
INNER JOIN ""Categories"" c ON c.""Id"" = sc.""CategoryId""
WHERE p.""IsActive"" = TRUE
  AND sc.""IsActive"" = TRUE
  AND c.""IsActive"" = TRUE
  AND p.""Id"" = ANY(@ProductIds);";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var productInfos = (await conn.QueryAsync<(int ProductId, decimal UnitPrice)>(
            new CommandDefinition(productsSql, new { ProductIds = productIds }, cancellationToken: cancellationToken))).ToList();

        if (productInfos.Count != productIds.Length)
            throw new InvalidOperationException("Some products are no longer available.");

        var now = DateTime.UtcNow;

        using var tx = conn.BeginTransaction();
        try
        {
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

            decimal finalTotal = subtotal + deliveryCharge;

            const string insertOrderSql = @"
INSERT INTO ""Orders""
    (""UserId"", ""Status"", ""PaymentMethod"", ""CreatedAt"", ""UpdatedAt"", ""TotalAmount"", ""DeliveryCharge"",
     ""FullName"", ""Phone"", ""Email"", ""City"", ""AddressLine1"", ""AddressLine2"", ""ZipCode"", ""AccessCode"")
VALUES
    (@UserId, @Status, @PaymentMethod, @CreatedAt, @UpdatedAt, @TotalAmount, @DeliveryCharge,
     @FullName, @Phone, @Email, @City, @AddressLine1, @AddressLine2, @ZipCode, @AccessCode)
RETURNING ""Id"", ""AccessCode"";";

            var result = await conn.QuerySingleAsync<(long OrderId, Guid AccessCode)>(
                new CommandDefinition(
                    insertOrderSql,
                    new
                    {
                        UserId = (int?)null,
                        Status = (int)OrderStatus.Placed,
                        PaymentMethod = (int)PaymentMethod.Cod,
                        CreatedAt = now,
                        UpdatedAt = now,
                        TotalAmount = finalTotal,
                        DeliveryCharge = deliveryCharge,
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

            const string insertItemSql = @"
INSERT INTO ""OrderItems"" (""OrderId"", ""ProductId"", ""Quantity"", ""UnitPrice"", ""LineTotal"")
VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @LineTotal);";

            foreach (var item in orderItems)
            {
                await conn.ExecuteAsync(new CommandDefinition(
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
            try { tx.Rollback(); } catch { }
            throw;
        }
    }

    public async Task<PagedResult<OrderSummaryDto>> GetOrderHistoryAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        const string countSql = @"
SELECT COUNT(1)
FROM ""Orders""
WHERE ""UserId"" = @UserId;";

        const string sql = @"
SELECT
    ""Id"" AS OrderId,
    ""Status"" AS Status,
    ""CreatedAt"",
    ""TotalAmount"",
    ""AccessCode""
FROM ""Orders""
WHERE ""UserId"" = @UserId
ORDER BY ""CreatedAt"" DESC
LIMIT @PageSize OFFSET @Offset;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var totalCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(countSql, new { UserId = userId }, cancellationToken: cancellationToken));

        var items = await conn.QueryAsync<OrderSummaryDto>(
            new CommandDefinition(sql, new { UserId = userId, Offset = offset, PageSize = pageSize }, cancellationToken: cancellationToken));

        return new PagedResult<OrderSummaryDto>(items.ToList(), page, pageSize, totalCount);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetOrderHistoryByEmailOrPhoneAsync(string emailOrPhone, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        const string countSql = @"
SELECT COUNT(1)
FROM ""Orders""
WHERE ""Email"" = @EmailOrPhone OR ""Phone"" = @EmailOrPhone;";

        const string sql = @"
SELECT
    ""Id"" AS OrderId,
    ""Status"" AS Status,
    ""CreatedAt"",
    ""TotalAmount"",
    ""AccessCode""
FROM ""Orders""
WHERE ""Email"" = @EmailOrPhone OR ""Phone"" = @EmailOrPhone
ORDER BY ""CreatedAt"" DESC
LIMIT @PageSize OFFSET @Offset;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var totalCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            new { EmailOrPhone = emailOrPhone },
            cancellationToken: cancellationToken));

        var items = await conn.QueryAsync<OrderSummaryDto>(
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
    ""Id"" AS OrderId,
    ""Status"" AS Status,
    ""PaymentMethod"" AS PaymentMethod,
    ""CreatedAt"",
    ""TotalAmount"",
    ""AccessCode"",
    ""FullName"",
    ""Phone"",
    ""Email"",
    ""City"",
    ""AddressLine1"",
    ""AddressLine2"",
    ""ZipCode""
FROM ""Orders""
WHERE ""Id"" = @OrderId
  AND ""UserId"" = @UserId;";

        const string itemsSql = @"
SELECT
    oi.""ProductId"",
    p.""Name"" AS ProductName,
    p.""ImageUrl"" AS ImageUrl,
    oi.""Quantity"",
    oi.""UnitPrice"",
    oi.""LineTotal""
FROM ""OrderItems"" oi
INNER JOIN ""Products"" p ON p.""Id"" = oi.""ProductId""
WHERE oi.""OrderId"" = @OrderId
ORDER BY oi.""Id"" ASC;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var order = await conn.QuerySingleOrDefaultAsync<OrderDetailDto>(
            new CommandDefinition(orderSql, new { OrderId = orderId, UserId = userId }, cancellationToken: cancellationToken));
        if (order is null)
            return null;

        var items = await conn.QueryAsync<OrderItemDto>(
            new CommandDefinition(itemsSql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        order.Items = items.ToList();
        return order;
    }

    public async Task<OrderDetailDto?> GetOrderDetailByAccessCodeAsync(long orderId, Guid accessCode, CancellationToken cancellationToken = default)
    {
        const string orderSql = @"
SELECT
    ""Id"" AS OrderId,
    ""Status"" AS Status,
    ""PaymentMethod"" AS PaymentMethod,
    ""CreatedAt"",
    ""TotalAmount"",
    ""AccessCode"",
    ""FullName"",
    ""Phone"",
    ""Email"",
    ""City"",
    ""AddressLine1"",
    ""AddressLine2"",
    ""ZipCode"",
    ""DeliveryCharge""
FROM ""Orders""
WHERE ""Id"" = @OrderId
  AND ""AccessCode"" = @AccessCode;";

        const string itemsSql = @"
SELECT
    oi.""ProductId"",
    p.""Name"" AS ProductName,
    p.""ImageUrl"" AS ImageUrl,
    oi.""Quantity"",
    oi.""UnitPrice"",
    oi.""LineTotal""
FROM ""OrderItems"" oi
INNER JOIN ""Products"" p ON p.""Id"" = oi.""ProductId""
WHERE oi.""OrderId"" = @OrderId
ORDER BY oi.""Id"" ASC;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var order = await conn.QuerySingleOrDefaultAsync<OrderDetailDto>(
            new CommandDefinition(orderSql, new { OrderId = orderId, AccessCode = accessCode }, cancellationToken: cancellationToken));
        if (order is null)
            return null;

        var items = await conn.QueryAsync<OrderItemDto>(
            new CommandDefinition(itemsSql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        order.Items = items.ToList();
        return order;
    }

    public async Task<OrderDetailDto?> GetOrderDetailForAdminAsync(long orderId, CancellationToken cancellationToken = default)
    {
        const string orderSql = @"
SELECT
    ""Id"" AS OrderId,
    ""Status"" AS Status,
    ""PaymentMethod"" AS PaymentMethod,
    ""CreatedAt"",
    ""TotalAmount"",
    ""AccessCode"",
    ""DeliveryCharge"",
    ""FullName"",
    ""Phone"",
    ""Email"",
    ""City"",
    ""AddressLine1"",
    ""AddressLine2"",
    ""ZipCode""
FROM ""Orders""
WHERE ""Id"" = @OrderId;";

        const string itemsSql = @"
SELECT
    oi.""ProductId"",
    p.""Name"" AS ProductName,
    p.""ImageUrl"" AS ImageUrl,
    oi.""Quantity"",
    oi.""UnitPrice"",
    oi.""LineTotal""
FROM ""OrderItems"" oi
INNER JOIN ""Products"" p ON p.""Id"" = oi.""ProductId""
WHERE oi.""OrderId"" = @OrderId
ORDER BY oi.""Id"" ASC;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var order = await conn.QuerySingleOrDefaultAsync<OrderDetailDto>(
            new CommandDefinition(orderSql, new { OrderId = orderId }, cancellationToken: cancellationToken));
        if (order is null)
            return null;

        var items = await conn.QueryAsync<OrderItemDto>(
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

        const string countSql = @"
SELECT COUNT(1)
FROM ""Orders"" o
WHERE (@Status::int IS NULL OR o.""Status"" = @Status)
  AND (@FromUtc::timestamp IS NULL OR o.""CreatedAt"" >= @FromUtc)
  AND (@ToUtc::timestamp IS NULL OR o.""CreatedAt"" <= @ToUtc);";

        DateTime? adjustedToUtc = toUtc;
        if (adjustedToUtc.HasValue)
        {
            adjustedToUtc = adjustedToUtc.Value.AddDays(1);
        }

        const string sql = @"
SELECT
    o.""Id"" AS OrderId,
    o.""UserId"" AS UserId,
    o.""FullName"" AS CustomerName,
    o.""Phone"" AS CustomerPhone,
    o.""Status"" AS Status,
    o.""CreatedAt"",
    o.""TotalAmount""
FROM ""Orders"" o
WHERE (@Status::int IS NULL OR o.""Status"" = @Status)
  AND (@FromUtc::timestamp IS NULL OR o.""CreatedAt"" >= @FromUtc)
  AND (@ToUtc::timestamp IS NULL OR o.""CreatedAt"" < @ToUtc)
ORDER BY o.""CreatedAt"" DESC
LIMIT @PageSize OFFSET @Offset;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var totalCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            countSql,
            new
            {
                Status = status.HasValue ? (int)status.Value : (int?)null,
                FromUtc = fromUtc,
                ToUtc = adjustedToUtc,
            },
            cancellationToken: cancellationToken));

        var items = await conn.QueryAsync<AdminOrderSummaryDto>(new CommandDefinition(
            sql,
            new
            {
                Status = status.HasValue ? (int)status.Value : (int?)null,
                FromUtc = fromUtc,
                ToUtc = adjustedToUtc,
                Offset = offset,
                PageSize = pageSize
            },
            cancellationToken: cancellationToken));

        return new PagedResult<AdminOrderSummaryDto>(items.ToList(), page, pageSize, totalCount);
    }

    public async Task<OrderStatusChangedDto?> UpdateOrderStatusAsync(long orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        const string sql = @"
UPDATE ""Orders""
SET ""Status"" = @NewStatus,
    ""UpdatedAt"" = NOW()
WHERE ""Id"" = @OrderId
RETURNING
    ""Id"" AS OrderId,
    ""UserId"" AS UserId,
    ""Status"" AS NewStatus,
    ""UpdatedAt"" AS UpdatedAt,
    ""AccessCode"" AS AccessCode;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        var dto = await conn.QuerySingleOrDefaultAsync<OrderStatusChangedDto>(
            new CommandDefinition(sql, new { NewStatus = (int)newStatus, OrderId = orderId }, cancellationToken: cancellationToken));

        return dto;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    (SELECT COUNT(1) FROM ""Products"") AS TotalProducts,
    (SELECT COUNT(1) FROM ""Categories"") AS TotalCategories,
    (SELECT COUNT(1) FROM ""Orders"") AS TotalOrders,
    (SELECT COUNT(1) FROM ""Orders"" WHERE ""CreatedAt"" >= CURRENT_DATE) AS NewOrdersToday,
    (SELECT COUNT(1) FROM ""Orders"" WHERE ""Status"" IN (0, 1, 2)) AS PendingOrders;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.QuerySingleAsync<AdminDashboardDto>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<int> GetNewOrdersTodayAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT COUNT(1)
FROM ""Orders""
WHERE ""CreatedAt"" >= CURRENT_DATE;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != ConnectionState.Open)
            conn.Open();

        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT
    ""Id"" ,
    ""UserId"",
    ""FullName"",
    ""Phone"",
    ""Email"",
    ""City"",
    ""Status"",
    ""CreatedAt"",
    ""TotalAmount""
FROM ""Orders""
ORDER BY ""CreatedAt"" DESC;";

        using var conn = _connectionFactory.CreateConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            conn.Open();

        var orders = await conn.QueryAsync<Order>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return orders.ToList();
    }
}