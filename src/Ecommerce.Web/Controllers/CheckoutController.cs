using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Web.Helpers;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class CheckoutController : Controller
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orders;

    public CheckoutController(ICartService cartService, IOrderService orders)
    {
        _cartService = cartService;
        _orders = orders;
    }

    private List<CartLineQuantityDto> CartLines() => CartSessionStore.GetCartLines(HttpContext.Session);

    [HttpGet("/Checkout")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var lines = CartLines();
        var cart = await _cartService.BuildCartSummaryAsync(lines, cancellationToken);

        var vm = new CheckoutViewModel
        {
            Cart = cart,
            Checkout = new CheckoutDto()
        };

        return View("~/Views/Checkout/Index.cshtml", vm);
    }

    [HttpPost("/Checkout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            model.Cart = await _cartService.BuildCartSummaryAsync(CartLines(), cancellationToken);
            return View("~/Views/Checkout/Index.cshtml", model);
        }

        var lines = CartLines();
        if (lines.Count == 0)
            return Redirect("/");

        var result = await _orders.CheckoutAsync(
            model.Checkout.FullName,
            model.Checkout.Phone,
            model.Checkout.Email,
            model.Checkout.City,
            model.Checkout.AddressLine1,
            model.Checkout.AddressLine2,
            model.Checkout.ZipCode,
            lines,
            cancellationToken);

        CartSessionStore.ClearCartLines(HttpContext.Session);
        return Redirect($"/Orders/Details/{result.OrderId}?code={result.AccessCode}");
    }
}