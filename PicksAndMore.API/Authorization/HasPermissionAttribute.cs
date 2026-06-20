using Microsoft.AspNetCore.Authorization;

namespace PicksAndMore.API.Authorization;

/// <summary>
/// Shorthand attribute for applying dynamic permission-based authorization.
/// Usage: [HasPermission("ManageProducts")] maps to policy "Permissions.ManageProducts".
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base(policy: $"Permissions.{permission}")
    {
    }
}
