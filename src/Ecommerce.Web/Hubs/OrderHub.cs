using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Ecommerce.Web.Hubs;

public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }

        // Guest clients connect with an access code query param.
        var accessCodeStr = Context.GetHttpContext()?.Request.Query["accessCode"].ToString();
        if (Guid.TryParse(accessCodeStr, out var accessCode))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{accessCode}");
        }

        await base.OnConnectedAsync();
    }
}

