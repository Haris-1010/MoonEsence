using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Enums;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminSubCategoriesController : Controller
{
    private readonly IAdminCatalogService _adminCatalog;

    public AdminSubCategoriesController(IAdminCatalogService adminCatalog)
    {
        _adminCatalog = adminCatalog;
    }

    [HttpGet("/Admin/SubCategories")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        // For index listing, show active only.
        // When needed, you can extend to support category-based filtering.
        var subCategories = await _adminCatalog.GetAllSubCategoriesAsync(cancellationToken);
        var vm = new AdminSubCategoriesIndexViewModel { SubCategories = subCategories };
        return View("~/Views/Admin/SubCategories/Index.cshtml", vm);
    }

    [HttpGet("/Admin/SubCategories/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var categories = await _adminCatalog.GetAllCategoriesAsync(cancellationToken);
        return View("~/Views/Admin/SubCategories/Edit.cshtml", new AdminSubCategoryFormViewModel
        {
            Categories = categories,
            SubCategory = new SubCategoryDto()
            {
                IsActive = true
            }
        });
    }

    [HttpPost("/Admin/SubCategories/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminSubCategoryFormViewModel vm, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            vm.Categories = await _adminCatalog.GetAllCategoriesAsync(cancellationToken);
            return View("~/Views/Admin/SubCategories/Edit.cshtml", vm);
        }

        var dto = new SubCategoryDto
        {
            CategoryId = vm.SubCategory.CategoryId,
            Name = vm.SubCategory.Name,
            Description = vm.SubCategory.Description,
            IsActive = vm.SubCategory.IsActive
        };

        await _adminCatalog.CreateSubCategoryAsync(dto, cancellationToken);
        return Redirect("/Admin/SubCategories");
    }

    [HttpGet("/Admin/SubCategories/Edit/{subCategoryId:int}")]
    public async Task<IActionResult> Edit(int subCategoryId, CancellationToken cancellationToken = default)
    {
        var dto = await _adminCatalog.GetSubCategoryForEditAsync(subCategoryId, cancellationToken);
        if (dto is null)
            return NotFound();

        var categories = await _adminCatalog.GetAllCategoriesAsync(cancellationToken);
        return View("~/Views/Admin/SubCategories/Edit.cshtml", new AdminSubCategoryFormViewModel
        {
            SubCategoryId = subCategoryId,
            Categories = categories,
            SubCategory = dto
        });
    }

    [HttpPost("/Admin/SubCategories/Edit/{subCategoryId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int subCategoryId, AdminSubCategoryFormViewModel vm, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            vm.SubCategoryId = subCategoryId;
            vm.Categories = await _adminCatalog.GetAllCategoriesAsync(cancellationToken);
            return View("~/Views/Admin/SubCategories/Edit.cshtml", vm);
        }

        var dto = new SubCategoryDto
        {
            Id = subCategoryId,
            CategoryId = vm.SubCategory.CategoryId,
            Name = vm.SubCategory.Name,
            Description = vm.SubCategory.Description,
            IsActive = vm.SubCategory.IsActive
        };

        await _adminCatalog.UpdateSubCategoryAsync(dto, cancellationToken);
        return Redirect("/Admin/SubCategories");
    }

    [HttpPost("/Admin/SubCategories/Delete/{subCategoryId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int subCategoryId, CancellationToken cancellationToken = default)
    {
        await _adminCatalog.DeleteSubCategoryAsync(subCategoryId, cancellationToken);
        return Redirect("/Admin/SubCategories");
    }
}

