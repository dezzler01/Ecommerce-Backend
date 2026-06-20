namespace PicksAndMore.Domain.Entities;

public class Category : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string TargetAudience { get; set; } = null!;
    public bool IsVisible { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();

    public Category()
    {
    }

    public Category(Guid id, string name, string targetAudience, bool isVisible)
    {
        Id = id;
        Name = name;
        TargetAudience = targetAudience;
        IsVisible = isVisible;
    }
}
