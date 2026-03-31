using System.Security.Claims;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Application.Abstractions.Events;
using Ecommerce.Application.Abstractions.Repositories;
using Ecommerce.Application.Abstractions.Services;
using Ecommerce.Application.Abstractions.Storage;
using Ecommerce.Application.Services;
using Ecommerce.Infrastructure.Database;
using Ecommerce.Infrastructure.Repositories;
using Ecommerce.Infrastructure.Security;
using Ecommerce.Infrastructure.Storage;
using Ecommerce.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Npgsql;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add MVC
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// Session
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(7);
});

// CSRF token for AJAX POSTs
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// Authentication & Authorization
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ecom.auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// Application services
builder.Services.AddScoped<IClientCatalogService, ClientCatalogService>();
builder.Services.AddScoped<IAdminCatalogService, AdminCatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Infrastructure services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

// PostgreSQL Connection Factory
builder.Services.AddSingleton<IDbConnectionFactory>(sp => new PostgresConnectionFactory(connectionString));
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

// Blob / Local storage
var azureConn = builder.Configuration["Azure:BlobConnectionString"];
var azureContainer = builder.Configuration["Azure:BlobContainerName"];
if (!string.IsNullOrWhiteSpace(azureConn) && !string.IsNullOrWhiteSpace(azureContainer))
{
    builder.Services.AddSingleton<IBlobStorage>(_ => new AzureBlobStorage(azureConn, azureContainer));
}
else
{
    builder.Services.AddSingleton<IBlobStorage>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        return new LocalBlobStorage(env.WebRootPath, rootRelative: "uploads");
    });
}

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// SignalR publisher
builder.Services.AddSingleton<IOrderEventPublisher, OrderEventPublisher>();

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// SignalR hub
app.MapHub<OrderHub>("/hubs/order");

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        // Do NOT call Open() here. Let callers open and dispose connections per-operation.
        return new NpgsqlConnection(_connectionString);
    }
}