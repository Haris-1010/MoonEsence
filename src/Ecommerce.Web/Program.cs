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

// Get connection string - prioritize Railway's DATABASE_URL
string connectionString = GetConnectionString(builder);
Console.WriteLine($"Using database connection: {(connectionString.Contains("password") ? "Connected" : "Not configured")}");

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

// Test database connection on startup
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    try
    {
        using var connection = dbFactory.CreateConnection();
        connection.Open();
        Console.WriteLine("✅ Database connection successful!");

        // Optional: Test a simple query
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        cmd.ExecuteScalar();
        Console.WriteLine("✅ Database query test successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection failed: {ex.Message}");
    }
}

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

app.MapHub<OrderHub>("/hubs/order");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/health", async (IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var connection = dbFactory.CreateConnection();
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        cmd.ExecuteScalar();
        return Results.Ok(new { status = "Healthy", database = "Connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.StatusCode(503);
    }
});

app.Run();

string GetConnectionString(WebApplicationBuilder builder)
{
    var railwayDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(railwayDbUrl))
    {
        Console.WriteLine("📡 Using Railway DATABASE_URL");
        try
        {
            var uri = new Uri(railwayDbUrl);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo[1];
            var database = uri.AbsolutePath.TrimStart('/');
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;Timeout=30;";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to parse DATABASE_URL: {ex.Message}");
        }
    }

    var configConnection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(configConnection))
    {
        Console.WriteLine("📝 Using connection string from appsettings.json");
        return configConnection;
    }

    throw new InvalidOperationException("Missing connection string 'DefaultConnection' or 'DATABASE_URL' environment variable.");
}

public class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}