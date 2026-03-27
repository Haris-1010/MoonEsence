using System.Security.Claims;
using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        var session = await _authService.LoginAsync(model.Username, model.Password, cancellationToken);
        if (session is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ClaimTypes.Name, session.Username),
            new Claim(ClaimTypes.Role, session.Role.ToString()) // "Admin" / "Client"
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var userId = await _authService.RegisterClientAsync(
                new Ecommerce.Application.DTOs.RegisterDto
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password
                },
                cancellationToken);

            // After registration, sign-in automatically.
            var session = await _authService.LoginAsync(model.Username, model.Password, cancellationToken);
            if (session is null)
                throw new InvalidOperationException("Registration succeeded but login failed.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new Claim(ClaimTypes.Name, session.Username),
                new Claim(ClaimTypes.Role, session.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return LocalRedirect("/");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

