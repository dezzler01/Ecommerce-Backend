using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Domain.Entities;

public class ShippingComboRule : BaseAuditableEntity
{
    public int InputSmallCount { get; set; }
    public int InputMediumCount { get; set; }
    public ShippingSize ResultingSize { get; set; }

    public ShippingComboRule()
    {
    }

    public ShippingComboRule(Guid id, int inputSmallCount, int inputMediumCount, ShippingSize resultingSize)
    {
        Id = id;
        InputSmallCount = inputSmallCount;
        InputMediumCount = inputMediumCount;
        ResultingSize = resultingSize;
    }
}
