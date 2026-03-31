using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Web.Models;
using System.Diagnostics;
using Ecommerce.Application.DTOs;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class HomeController : Controller
{
    private readonly IClientCatalogService _catalog;

    public HomeController(IClientCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index(int? categoryId, int? subCategoryId, int page = 1, CancellationToken cancellationToken = default)
    {
        var categories = await _catalog.GetActiveCategoriesAsync(cancellationToken);
        var subCategories = categoryId.HasValue ? await _catalog.GetActiveSubCategoriesAsync(categoryId.Value, cancellationToken) : Array.Empty<SubCategoryDto>();
        var products = await _catalog.GetProductsAsync(categoryId, subCategoryId, page, pageSize: 12, cancellationToken);

        var vm = new HomeIndexViewModel
        {
            Categories = categories,
            SubCategories = subCategories,
            Products = products,
            SelectedCategoryId = categoryId,
            SelectedSubCategoryId = subCategoryId,
            CurrentPage = page
        };

        return View(vm);
    }

    public async Task<IActionResult> Featured(int? categoryId, int? subCategoryId, int page = 1, CancellationToken cancellationToken = default)
    {
        var categories = await _catalog.GetActiveCategoriesAsync(cancellationToken);
        var subCategories = categoryId.HasValue ? await _catalog.GetActiveSubCategoriesAsync(categoryId.Value, cancellationToken) : Array.Empty<SubCategoryDto>();
        var products = await _catalog.GetProductsAsync(categoryId, subCategoryId, page, pageSize: 12, cancellationToken);

        var vm = new HomeIndexViewModel
        {
            Categories = categories,
            SubCategories = subCategories,
            Products = products,
            SelectedCategoryId = categoryId,
            SelectedSubCategoryId = subCategoryId,
            CurrentPage = page
        };

        return View(vm);
    }
    public async Task<IActionResult> Collections(int? categoryId, int? subCategoryId, int page = 1, CancellationToken cancellationToken = default)
    {
        var categories = await _catalog.GetActiveCategoriesAsync(cancellationToken);
        var subCategories = categoryId.HasValue ? await _catalog.GetActiveSubCategoriesAsync(categoryId.Value, cancellationToken) : Array.Empty<SubCategoryDto>();
        var products = await _catalog.GetProductsAsync(categoryId, subCategoryId, page, pageSize: 12, cancellationToken);

        var vm = new HomeIndexViewModel
        {
            Categories = categories,
            SubCategories = subCategories,
            Products = products,
            SelectedCategoryId = categoryId,
            SelectedSubCategoryId = subCategoryId,
            CurrentPage = page
        };

        return View(vm);
    }

    [HttpGet("/Privacy")]
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("Error", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    [HttpGet("/About")]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet("/Contact")]
    public IActionResult Contact()
    {
        return View();
    }

    [HttpGet("/Terms")]
    public IActionResult Terms()
    {
        return View();
    }
}