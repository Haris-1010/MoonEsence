using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;

    public AdminController(IUserRepository userRepository, IOrderRepository orderRepository)
    {
        _userRepository = userRepository;
        _orderRepository = orderRepository;
    }

    public async Task<IActionResult> Users()
    {
        var users = await _userRepository.GetAllAsync();
        var viewModel = users.Select(u => new UserViewModel
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        }).ToList();

        return View(viewModel);
    }

    public async Task<IActionResult> AllOrders()
    {
        var orders = await _orderRepository.GetAllAsync();
        var viewModel = orders.Select(o => new OrderListViewModel
        {
            OrderId = o.Id,
            CustomerName = o.FullName,
            CustomerPhone = o.Phone,
            CustomerEmail = o.Email,
            CreatedAt = o.CreatedAt,
            TotalAmount = o.TotalAmount,
            City = o.City
        }).ToList();

        return View(viewModel);
    }
}