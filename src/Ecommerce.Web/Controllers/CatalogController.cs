using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class CatalogController : Controller
{
    private readonly IClientCatalogService _catalog;

    public CatalogController(IClientCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("/Catalog/Products")]
    public async Task<IActionResult> Products(int? categoryId, int? subCategoryId, int page = 1, CancellationToken cancellationToken = default)
    {
        var products = await _catalog.GetProductsAsync(categoryId, subCategoryId, page, pageSize: 12, cancellationToken);
        return PartialView("~/Views/Catalog/_ProductGrid.cshtml", products);
    }

    [HttpGet("/Catalog/SubCategories")]
    public async Task<IActionResult> SubCategories(int categoryId, CancellationToken cancellationToken = default)
    {
        var items = await _catalog.GetActiveSubCategoriesAsync(categoryId, cancellationToken);
        return Json(new { ok = true, items });
    }

    [HttpGet("/Catalog/Details/{productId:int}")]
    public async Task<IActionResult> Details(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _catalog.GetProductDetailAsync(productId, cancellationToken);
        if (product is null)
            return NotFound();

        ViewData["ProductImageUrl"] = product.ImageUrl; 

        var vm = new ProductDetailViewModel { Product = product };
        return View("~/Views/Catalog/Details.cshtml", vm);
    }
}

