using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IRolePermissionRepository
{
    Task<List<string>> GetPermissionsByRoleIdAsync(Guid roleId);
    Task<List<RolePermission>> GetByRoleIdAsync(Guid roleId);
    Task AddRangeAsync(IEnumerable<RolePermission> permissions);
    void RemoveRange(IEnumerable<RolePermission> permissions);
}
