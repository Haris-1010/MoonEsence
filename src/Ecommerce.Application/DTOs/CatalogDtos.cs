using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SubCategoryDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ProductListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal RegularPrice { get; set; }
    public int SubCategoryId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string SubCategoryName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal RegularPrice { get; set; }
    public int SubCategoryId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string SubCategoryName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ProductCreateDto
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 2)]
    public string Description { get; set; } = string.Empty;

    [Required, Range(0.01, 999999)]
    public decimal Price { get; set; }
    [Required, Range(0.01, 999999)]
    public decimal RegularPrice { get; set; }   
    [Required]
    public int SubCategoryId { get; set; }

    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
}

public class ProductUpdateDto : ProductCreateDto
{
    [Required]
    public int Id { get; set; }
}

