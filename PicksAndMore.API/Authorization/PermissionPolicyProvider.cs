using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace PicksAndMore.API.Authorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Support dynamic policy checking for naming convention e.g., "Permissions.Products.Create"
        if (policyName.StartsWith("Permissions.", StringComparison.OrdinalIgnoreCase))
        {
            var permissionName = policyName.Substring("Permissions.".Length);
            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(permissionName));
            return policy.Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}
