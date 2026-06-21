namespace PicksAndMore.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsVisible { get; set; }
    public DateTime? ScheduledPublishDate { get; set; }
    public Guid CategoryId { get; set; }
    public string MainCategory { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public List<string> Colors { get; set; } = new();
    public List<string> Sizes { get; set; } = new();
    public List<string> Materials { get; set; } = new();
    public List<string> SeasonTags { get; set; } = new();
    public string ShippingSize { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<ProductImageDto> ImageUrls { get; set; } = new();
    public string? Age { get; set; }
    public string? CollectionType { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();

    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public string? BrandLogoUrl { get; set; }

    public bool OverrideStandardShipping { get; set; }
    public bool IsFreeShipping { get; set; }
    public decimal? FixedShippingPrice { get; set; }
}
