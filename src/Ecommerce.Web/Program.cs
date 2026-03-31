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

// Configure Kestrel - only for Railway
var isRailway = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL"));
if (isRailway)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8080);
    });
    Console.WriteLine("🚀 Running on Railway - binding to port 8080");
}
else
{
    Console.WriteLine("💻 Running locally - using default ports");
}

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromDays(7);
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

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

builder.Services.AddSignalR();

builder.Services.AddScoped<IClientCatalogService, ClientCatalogService>();
builder.Services.AddScoped<IAdminCatalogService, AdminCatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();

string connectionString = GetConnectionString(builder);
Console.WriteLine($"Using database: {(connectionString.Contains("railway") ? "Railway PostgreSQL" : "Local PostgreSQL")}");

builder.Services.AddSingleton<IDbConnectionFactory>(sp => new PostgresConnectionFactory(connectionString));
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

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

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddSingleton<IOrderEventPublisher, OrderEventPublisher>();

var app = builder.Build();

// Request logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path}");
    await next();
    Console.WriteLine($"[RESPONSE] {context.Response.StatusCode} for {context.Request.Path}");
});
app.MapGet("/", () => Results.Text("🚀 App is running on Railway!"));
// Health check endpoint
app.MapGet("/health", async (IDbConnectionFactory dbFactory) =>
{
    Console.WriteLine("Health check endpoint hit!");
    try
    {
        using var connection = dbFactory.CreateConnection();
        connection.Open();
        return Results.Ok(new
        {
            status = "Healthy",
            database = "Connected",
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName,
            platform = isRailway ? "Railway" : "Local"
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Health check failed: {ex.Message}");
        return Results.Json(new { status = "Unhealthy", error = ex.Message }, statusCode: 503);
    }
});

// Test endpoints
app.MapGet("/test", () => Results.Json(new
{
    message = "API is working!",
    time = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    platform = isRailway ? "Railway" : "Local"
}));

app.MapGet("/test-db", async (IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var connection = dbFactory.CreateConnection();
        connection.Open();
        return Results.Json(new { success = true, message = "Database connected successfully!" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message });
    }
});
// Test database connection on startup
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    try
    {
        using var connection = dbFactory.CreateConnection();
        connection.Open();
        Console.WriteLine("✅ Database connection successful!");

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        // Fix: Cast to NpgsqlCommand to use ExecuteScalarAsync
        if (cmd is Npgsql.NpgsqlCommand npgsqlCmd)
        {
            await npgsqlCmd.ExecuteScalarAsync();
        }
        else
        {
            // Fallback to synchronous if not NpgsqlCommand
            cmd.ExecuteScalar();
        }
        Console.WriteLine("✅ Database query test successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection failed: {ex.Message}");
        if (!isRailway)
        {
            Console.WriteLine("💡 Tip: Make sure PostgreSQL is running locally and database 'EcommerceDb' exists");
        }
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

Console.WriteLine($"Application starting - {(isRailway ? "listening on http://0.0.0.0:8080" : "running locally")}");
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
        Console.WriteLine("📝 Using connection string from appsettings.json (local development)");
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