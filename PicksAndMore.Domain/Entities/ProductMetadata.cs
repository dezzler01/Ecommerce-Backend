namespace PicksAndMore.Domain.Entities;

public class ProductMetadata : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public List<string> Sizes { get; set; } = new();
    public List<string> Colors { get; set; } = new();
    public List<string> Materials { get; set; } = new();
    public List<string> SeasonTags { get; set; } = new();

    public ProductMetadata()
    {
    }

    public ProductMetadata(Guid id, Guid productId, List<string> sizes, List<string> colors, List<string> materials, List<string> seasonTags)
    {
        Id = id;
        ProductId = productId;
        Sizes = sizes ?? new List<string>();
        Colors = colors ?? new List<string>();
        Materials = materials ?? new List<string>();
        SeasonTags = seasonTags ?? new List<string>();
    }
}
