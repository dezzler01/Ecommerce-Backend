namespace PicksAndMore.Application.DTOs;

public class ProductCreateDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required decimal Price { get; set; }
    public required decimal CostPrice { get; set; }
    public required int StockQuantity { get; set; }
    public required string MainCategory { get; set; } // e.g. "Women", "Kids"
    public required string SubCategory { get; set; }  // e.g. "Bags", "Shoes", "Clothes", "DiaperBags"
    public required List<string> Colors { get; set; }
    public required List<string> Sizes { get; set; }
    public List<string>? Materials { get; set; }
    public List<string>? SeasonTags { get; set; }
    public required string ShippingSize { get; set; } // e.g. "Small", "Medium", "Large"
    public bool IsVisible { get; set; } = true;
    public DateTime? ScheduledPublishDate { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? Age { get; set; }
    public List<string>? SubCategories { get; set; }
    public Guid? BrandId { get; set; }

    public bool OverrideStandardShipping { get; set; }
    public bool IsFreeShipping { get; set; }
    public decimal? FixedShippingPrice { get; set; }
}
