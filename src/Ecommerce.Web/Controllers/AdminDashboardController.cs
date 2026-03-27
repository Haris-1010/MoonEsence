using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly IOrderService _orders;

    public AdminDashboardController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpGet("/Admin/Dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var vm = new AdminDashboardViewModel
        {
            Stats = await _orders.GetAdminDashboardAsync(cancellationToken)
        };

        return View("~/Views/Admin/Dashboard/Index.cshtml", vm);
    }
}

