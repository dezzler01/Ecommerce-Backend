namespace PicksAndMore.Domain.Entities;

public class ProductShippingOverride : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public bool IsFreeShipping { get; set; }
    public decimal? FixedPrice { get; set; }

    public ProductShippingOverride()
    {
    }

    public ProductShippingOverride(Guid id, Guid productId, bool isFreeShipping, decimal? fixedPrice)
    {
        Id = id;
        ProductId = productId;
        IsFreeShipping = isFreeShipping;
        FixedPrice = fixedPrice;
    }
}
