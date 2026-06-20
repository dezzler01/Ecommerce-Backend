using Microsoft.AspNetCore.SignalR;

namespace PicksAndMore.Application.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinAdminGroup()
    {
        var user = Context.User;
        
        // Dynamically inspect JWT claims to verify if this user holds any admin-level console permissions
        var hasAdminPermission = user?.Claims.Any(c => c.Type == "permissions" && 
            (c.Value == "Orders.Read" || c.Value == "Products.Create" || c.Value == "Shipping.Read")) == true;

        if (hasAdminPermission)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
        }
    }
}
