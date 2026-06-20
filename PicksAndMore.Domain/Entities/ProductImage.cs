namespace PicksAndMore.Domain.Entities;

public class ProductImage : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Url { get; set; } = null!;
    public int SortOrder { get; set; }
    public string? AltText { get; set; }

    public ProductImage()
    {
    }

    public ProductImage(Guid id, Guid productId, string url, int sortOrder, string? altText = null)
    {
        Id = id;
        ProductId = productId;
        Url = url;
        SortOrder = sortOrder;
        AltText = altText;
    }
}
