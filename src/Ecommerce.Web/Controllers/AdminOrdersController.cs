using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Web.ViewModels;

namespace Ecommerce.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOrdersController : Controller
{
    private readonly IOrderService _orders;

    public AdminOrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("/Admin/Orders")]
    public async Task<IActionResult> Index(OrderStatus? status, DateTime? fromUtc, DateTime? toUtc, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        // MVC will bind enum from query string if provided.
        var vm = new ViewModels.AdminOrdersIndexViewModel
        {
            Status = status,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Orders = await _orders.GetAdminOrdersAsync(status, fromUtc, toUtc, page, pageSize, cancellationToken)
        };

        return View("~/Views/Admin/Orders/Index.cshtml", vm);
    }

    [HttpGet("/Admin/Orders/Details/{orderId:long}")]
    public async Task<IActionResult> Details(long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetOrderDetailForAdminAsync(orderId, cancellationToken);
        if (order is null)
            return NotFound();

        var vm = new OrderDetailViewModel { Order = order };
        return View("~/Views/Admin/Orders/Details.cshtml", vm);
    }

    [HttpPost("/Admin/Orders/UpdateStatus")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(typeof(OrderStatus), newStatus))
            return BadRequest(new { ok = false, error = "Invalid status." });

        var updated = await _orders.UpdateOrderStatusAsync(orderId, newStatus, cancellationToken);
        if (updated is null)
            return NotFound(new { ok = false, error = "Order not found." });

        return Json(new { ok = true, updated });
    }
}