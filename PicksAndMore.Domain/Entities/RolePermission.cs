namespace PicksAndMore.Domain.Entities;

public class RolePermission
{
    public Guid Id { get; private set; }
    public Guid RoleId { get; private set; }
    public string Permission { get; private set; }

    private RolePermission()
    {
        Permission = null!;
    }

    public RolePermission(Guid id, Guid roleId, string permission)
    {
        Id = id;
        RoleId = roleId;
        Permission = permission;
    }
}
