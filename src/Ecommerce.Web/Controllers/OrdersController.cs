using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class OrdersController : Controller
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("/Orders/History")]
    public async Task<IActionResult> History(string? emailOrPhone, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailOrPhone) && User?.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var userId))
            {
                var historyVm = new OrderHistoryViewModel
                {
                    Orders = await _orders.GetOrderHistoryAsync(userId, page, pageSize, cancellationToken)
                };
                return View("~/Views/Orders/History.cshtml", historyVm);
            }
        }

        if (string.IsNullOrWhiteSpace(emailOrPhone))
        {
            var historyVm = new OrderHistoryViewModel
            {
                Orders = new Ecommerce.Application.Shared.PagedResult<Ecommerce.Application.DTOs.OrderSummaryDto>(
                    Array.Empty<Ecommerce.Application.DTOs.OrderSummaryDto>(),
                    page,
                    pageSize,
                    0)
            };
            return View("~/Views/Orders/History.cshtml", historyVm);
        }

        var vm = new OrderHistoryViewModel
        {
            Orders = await _orders.GetOrderHistoryByEmailOrPhoneAsync(emailOrPhone, page, pageSize, cancellationToken)
        };
        return View("~/Views/Orders/History.cshtml", vm);
    }

    [HttpGet("/Orders/Details/{orderId:long}")]
    public async Task<IActionResult> Details(long orderId, Guid code, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetOrderDetailByAccessCodeAsync(orderId, code, cancellationToken);
        if (order is null)
            return NotFound();

        var vm = new OrderDetailViewModel { Order = order };
        return View("~/Views/Orders/Details.cshtml", vm);
    }

    [HttpGet("/Orders/Invoice/{orderId:long}")]
    public async Task<IActionResult> Invoice(long orderId, Guid code, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetOrderDetailByAccessCodeAsync(orderId, code, cancellationToken);
        if (order is null)
            return NotFound();

        var vm = new OrderDetailViewModel { Order = order };
        return View("~/Views/Orders/Invoice.cshtml", vm);
    }
}

