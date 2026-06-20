using Microsoft.AspNetCore.Identity;

namespace PicksAndMore.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = null!;
    public string? SecondaryPhoneNumber { get; set; }
    public string? AddressDetails { get; set; }
    public string? Governorate { get; set; }
    public bool IsGuest { get; set; }
    public Guid RoleId { get; set; }

    public ApplicationUser()
    {
    }

    public ApplicationUser(Guid id, string fullName, string email, string passwordHash, Guid roleId)
    {
        Id = id;
        FullName = fullName;
        Email = email;
        UserName = email;
        PasswordHash = passwordHash;
        RoleId = roleId;
    }
}
