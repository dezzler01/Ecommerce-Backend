namespace PicksAndMore.Domain.Entities;

public class Brand : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string LogoUrl { get; set; } = null!;
    public bool ShowInCards { get; set; }
    public bool IsVisible { get; set; }

    // One-to-many relationship with Product
    public ICollection<Product> Products { get; set; } = new List<Product>();

    public Brand()
    {
    }

    public Brand(Guid id, string name, string logoUrl, bool showInCards, bool isVisible)
    {
        Id = id;
        Name = name;
        LogoUrl = logoUrl;
        ShowInCards = showInCards;
        IsVisible = isVisible;
    }
}
