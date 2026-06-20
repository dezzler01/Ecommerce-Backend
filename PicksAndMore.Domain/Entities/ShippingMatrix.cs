namespace PicksAndMore.Domain.Entities;

public class ShippingMatrix : BaseAuditableEntity
{
    public string Governorate { get; set; } = null!;
    public decimal BasePriceSmall { get; set; }
    public decimal BasePriceMedium { get; set; }
    public decimal BasePriceLarge { get; set; }

    public ShippingMatrix()
    {
    }

    public ShippingMatrix(Guid id, string governorate, decimal basePriceSmall, decimal basePriceMedium, decimal basePriceLarge)
    {
        Id = id;
        Governorate = governorate;
        BasePriceSmall = basePriceSmall;
        BasePriceMedium = basePriceMedium;
        BasePriceLarge = basePriceLarge;
    }
}
