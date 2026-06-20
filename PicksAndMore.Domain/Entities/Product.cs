using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsVisible { get; set; }
    public DateTime? ScheduledPublishDate { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ShippingSize ShippingSize { get; set; }
    public string? ImageUrl { get; set; }
    public string? Age { get; set; }
    
    // Brand relationship
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    
    // One-to-one relationship with ProductMetadata
    public ProductMetadata? Metadata { get; set; }

    // One-to-many relationship with ProductImage
    public List<ProductImage> Images { get; set; } = new();

    // Many-to-many relationship with Category
    public List<Category> Categories { get; set; } = new();

    public Product()
    {
    }

    public Product(
        Guid id,
        string title,
        string description,
        decimal price,
        decimal costPrice,
        int stockQuantity,
        bool isVisible,
        DateTime? scheduledPublishDate,
        Guid categoryId,
        ShippingSize shippingSize,
        string? imageUrl = null)
    {
        Id = id;
        Title = title;
        Description = description;
        Price = price;
        CostPrice = costPrice;
        StockQuantity = stockQuantity;
        IsVisible = isVisible;
        ScheduledPublishDate = scheduledPublishDate;
        CategoryId = categoryId;
        ShippingSize = shippingSize;
        ImageUrl = imageUrl;
    }
}
