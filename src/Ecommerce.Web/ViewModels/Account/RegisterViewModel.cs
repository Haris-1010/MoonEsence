using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

