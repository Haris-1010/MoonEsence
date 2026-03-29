using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Services;

public class CartService : ICartService
{
    private readonly IProductRepository _products;

    public CartService(IProductRepository products)
    {
        _products = products;
    }

    public async Task<CartSummaryDto> BuildCartSummaryAsync(
      IReadOnlyList<CartLineQuantityDto> lines,
      CancellationToken cancellationToken = default)
    {
        if (lines == null || lines.Count == 0)
        {
            return new CartSummaryDto
            {
                Lines = Array.Empty<CartLineDto>(),
                Subtotal = 0m,
                TotalItems = 0,
                DeliveryCharge = 0m,
                FinalTotal = 0m
            };
        }

        var productIds = lines.Select(x => x.ProductId).Distinct().ToArray();
        var productInfos = await _products.GetCartProductsAsync(productIds, cancellationToken);
        var productsById = productInfos.ToDictionary(p => p.ProductId, p => p);

        var resultLines = new List<CartLineDto>(lines.Count);
        decimal subtotal = 0m;
        int totalItems = 0;

        foreach (var line in lines)
        {
            if (!productsById.TryGetValue(line.ProductId, out var product))
                continue;

            var qty = Math.Max(0, line.Quantity);
            if (qty == 0)
                continue;

            var unitPrice = product.UnitPrice;
            var lineTotal = unitPrice * qty;

            subtotal += lineTotal;
            totalItems += qty;

            resultLines.Add(new CartLineDto
            {
                ProductId = line.ProductId,
                ProductName = product.ProductName,
                UnitPrice = unitPrice,
                Quantity = qty,
                ImageUrl = product.ImageUrl
            });
        } 
        const decimal DELIVERY_CHARGE = 230m;
        decimal deliveryCharge = (totalItems >= 2) ? 0m : DELIVERY_CHARGE;

        return new CartSummaryDto
        {
            Lines = resultLines,
            Subtotal = subtotal,
            TotalItems = totalItems,
            DeliveryCharge = deliveryCharge,
            FinalTotal = subtotal + deliveryCharge    
        };
    }




}

