using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Web.Helpers;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private List<CartLineQuantityDto> CartLines()
        => CartSessionStore.GetCartLines(HttpContext.Session);

    [HttpGet("/Cart/Get")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        var lines = CartLines();
        var summary = await _cartService.BuildCartSummaryAsync(lines, cancellationToken);
        return Json(new { ok = true, summary });
    }

    [HttpPost("/Cart/Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 1) quantity = 1;
        quantity = Math.Min(99, quantity);

        var lines = CartLines();
        var existing = lines.FirstOrDefault(x => x.ProductId == productId);
        if (existing is null)
        {
            lines.Add(new CartLineQuantityDto { ProductId = productId, Quantity = quantity });
        }
        else
        {
            existing.Quantity = Math.Min(99, existing.Quantity + quantity);
        }

        CartSessionStore.SetCartLines(HttpContext.Session, lines);

        var summary = await _cartService.BuildCartSummaryAsync(lines, cancellationToken);
        return Json(new { ok = true, summary });
    }

    [HttpPost("/Cart/Update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        quantity = Math.Max(0, quantity);
        var lines = CartLines();

        var existing = lines.FirstOrDefault(x => x.ProductId == productId);
        if (existing is null && quantity > 0)
        {
            lines.Add(new CartLineQuantityDto { ProductId = productId, Quantity = Math.Min(99, quantity) });
        }
        else if (existing is not null)
        {
            if (quantity == 0) lines.Remove(existing);
            else existing.Quantity = Math.Min(99, quantity);
        }

        CartSessionStore.SetCartLines(HttpContext.Session, lines);
        var summary = await _cartService.BuildCartSummaryAsync(lines, cancellationToken);
        return Json(new { ok = true, summary });
    }

    [HttpPost("/Cart/Remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, CancellationToken cancellationToken = default)
    {
        var lines = CartLines();
        lines.RemoveAll(x => x.ProductId == productId);
        CartSessionStore.SetCartLines(HttpContext.Session, lines);

        var summary = await _cartService.BuildCartSummaryAsync(lines, cancellationToken);
        return Json(new { ok = true, summary });
    }

    [HttpPost("/Cart/Clear")]
    public IActionResult Clear()
    {
        CartSessionStore.ClearCartLines(HttpContext.Session);
        return Ok();
    }
}

