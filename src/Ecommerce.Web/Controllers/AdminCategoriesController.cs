using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminCategoriesController : Controller
{
    private readonly IAdminCatalogService _adminCatalog;

    public AdminCategoriesController(IAdminCatalogService adminCatalog)
    {
        _adminCatalog = adminCatalog;
    }

    [HttpGet("/Admin/Categories")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var categories = await _adminCatalog.GetAllCategoriesAsync(cancellationToken);
        var vm = new AdminCategoriesIndexViewModel { Categories = categories };
        return View("~/Views/Admin/Categories/Index.cshtml", vm);
    }

    [HttpGet("/Admin/Categories/Create")]
    public IActionResult Create(CancellationToken cancellationToken = default)
    {
        return View("~/Views/Admin/Categories/Edit.cshtml", new AdminCategoryFormViewModel());
    }

    [HttpPost("/Admin/Categories/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCategoryFormViewModel vm, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Categories/Edit.cshtml", vm);

        var dto = new CategoryDto
        {
            Name = vm.Category.Name,
            Description = vm.Category.Description,
            IsActive = true
        };

        await _adminCatalog.CreateCategoryAsync(dto, cancellationToken);
        return Redirect("/Admin/Categories");
    }

    [HttpGet("/Admin/Categories/Edit/{categoryId:int}")]
    public async Task<IActionResult> Edit(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _adminCatalog.GetCategoryForEditAsync(categoryId, cancellationToken);
        if (category is null)
            return NotFound();

        return View("~/Views/Admin/Categories/Edit.cshtml", new AdminCategoryFormViewModel
        {
            CategoryId = categoryId,
            Category = category
        });
    }

    [HttpPost("/Admin/Categories/Edit/{categoryId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int categoryId, AdminCategoryFormViewModel vm, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            vm.CategoryId = categoryId;
            return View("~/Views/Admin/Categories/Edit.cshtml", vm);
        }

        var dto = new CategoryDto
        {
            Id = categoryId,
            Name = vm.Category.Name,
            Description = vm.Category.Description,
            IsActive = vm.Category.IsActive
        };

        await _adminCatalog.UpdateCategoryAsync(dto, cancellationToken);
        return Redirect("/Admin/Categories");
    }

    [HttpPost("/Admin/Categories/Delete/{categoryId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int categoryId, CancellationToken cancellationToken = default)
    {
        await _adminCatalog.DeleteCategoryAsync(categoryId, cancellationToken);
        return Redirect("/Admin/Categories");
    }
}

