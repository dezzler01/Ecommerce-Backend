namespace PicksAndMore.Application.DTOs;

public class UpdateProductDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required decimal Price { get; set; }
    public required decimal CostPrice { get; set; }
    public required int StockQuantity { get; set; }
    public bool IsVisible { get; set; }
    public DateTime? ScheduledPublishDate { get; set; }
    public Guid CategoryId { get; set; }
    public required string ShippingSize { get; set; }
    public string? ImageUrl { get; set; }
    public List<ProductImageDto>? ImageUrls { get; set; }
    public string? Age { get; set; }
    public string? CollectionType { get; set; }
    public List<Guid>? CategoryIds { get; set; }
    public Guid? BrandId { get; set; }

    // Metadata updates
    public List<string> Sizes { get; set; } = new();
    public List<string> Colors { get; set; } = new();
    public List<string> Materials { get; set; } = new();
    public List<string> SeasonTags { get; set; } = new();

    public bool OverrideStandardShipping { get; set; }
    public bool IsFreeShipping { get; set; }
    public decimal? FixedShippingPrice { get; set; }
}
