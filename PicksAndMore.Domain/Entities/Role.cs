using Microsoft.AspNetCore.Identity;

namespace PicksAndMore.Domain.Entities;

public class Role : IdentityRole<Guid>
{
    public string Description { get; set; } = null!;

    public Role()
    {
    }

    public Role(Guid id, string name, string description)
    {
        Id = id;
        Name = name;
        NormalizedName = name.ToUpper();
        Description = description;
    }
}
