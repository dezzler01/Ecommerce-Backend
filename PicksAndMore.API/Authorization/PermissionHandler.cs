using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PicksAndMore.API.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User == null)
        {
            return Task.CompletedTask;
        }

        // Retrieve the dynamic permission list claims from the JWT token
        var permissions = context.User.FindAll(c => c.Type == "permissions").Select(c => c.Value);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
