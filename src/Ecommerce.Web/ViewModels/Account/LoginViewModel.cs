using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // UI hint only; the server uses the stored role.
    public bool RememberMe { get; set; } = true;
}

