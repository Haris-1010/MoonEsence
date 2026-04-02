using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Abstractions.Storage;
using Ecommerce.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminProductsController : Controller
{
    private readonly IAdminCatalogService _adminCatalog;
    private readonly IBlobStorage _blobStorage;

    public AdminProductsController(IAdminCatalogService adminCatalog, IBlobStorage blobStorage)
    {
        _adminCatalog = adminCatalog;
        _blobStorage = blobStorage;
    }

    private async Task<string?> SaveProductImage(IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile is null || imageFile.Length == 0)
            return null;

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".svg"
        };

        var ext = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !allowed.Contains(ext))
            return null;
         
        await using var stream = imageFile.OpenReadStream();
        return await _blobStorage.UploadAsync(stream, imageFile.FileName, imageFile.ContentType, "products", cancellationToken);
    }

    [HttpGet("/Admin/Products")]
    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1, CancellationToken cancellationToken = default)
    {
        var products = await _adminCatalog.SearchProductsAsync(search, categoryId, page, pageSize: 10, cancellationToken);
        var vm = new AdminProductsIndexViewModel
        {
            Products = products,
            Search = search,
            CategoryId = categoryId
        };
        return View("~/Views/Admin/Products/Index.cshtml", vm);
    }

    [HttpGet("/Admin/Products/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var categories = await _adminCatalog.GetAllCategoriesAsync(cancellationToken);
        var subCategories = await _adminCatalog.GetAllSubCategoriesAsync(cancellationToken);
        var vm = new AdminProductFormViewModel
        {
            Categories = categories,
            SubCategories = subCategories
        };
        return View("~/Views/Admin/Products/Edit.cshtml", vm);
    }

    [HttpPost("/Admin/Products/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductFormViewModel vm, IFormFile? imageFile, CancellationToken cancellationToken = default)
    {
        

        var imageUrl = await SaveProductImage(imageFile, cancellationToken);
        var id = await _adminCatalog.CreateProductAsync(vm.Create, imageUrl, cancellationToken);
        return Redirect($"/Admin/Products/Edit/{id}");
    }

    [HttpGet("/Admin/Products/Edit/{productId:int}")]
    public async Task<IActionResult> Edit(int productId, CancellationToken cancellationToken = default)
    {
        
        var product = await _adminCatalog.GetProductForEditAsync(productId, cancellationToken);
        if (product is null)
            return NotFound();

        var categories = await _adminCatalog.GetAllCategoriesAsync(cancellationToken);
        var subCategories = await _adminCatalog.GetAllSubCategoriesAsync(cancellationToken);

        var vm = new AdminProductFormViewModel
        {
            ProductId = productId,
            Categories = categories,
            SubCategories = subCategories,
            ExistingImageUrl = product.ImageUrl,
            Update = new ProductUpdateDto
            {
                Id = productId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                RegularPrice = product.RegularPrice,
                SubCategoryId = product.SubCategoryId,
                IsActive = product.IsActive,
                ImageUrl = null
            },
            Create = new ProductCreateDto()
        };

        return View("~/Views/Admin/Products/Edit.cshtml", vm);
    }

    [HttpPost("/Admin/Products/Edit/{productId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int productId, AdminProductFormViewModel vm, IFormFile? imageFile, CancellationToken cancellationToken = default)
    {
        

        var uploadedUrl = await SaveProductImage(imageFile, cancellationToken);
        var imageUrl = uploadedUrl ?? vm.ExistingImageUrl;

        // Ensure ID matches route
        vm.Update.Id = productId;

        await _adminCatalog.UpdateProductAsync(vm.Update, imageUrl, cancellationToken);
        return Redirect($"/Admin/Products/Edit/{productId}");
    }

    [HttpPost("/Admin/Products/Delete/{productId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int productId, CancellationToken cancellationToken = default)
    {
        await _adminCatalog.DeleteProductAsync(productId, cancellationToken);
        return Redirect("/Admin/Products");
    }
}

