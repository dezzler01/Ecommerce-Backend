using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Domain.Entities;

public class PromoCode : BaseAuditableEntity
{
    public string Code { get; set; } = null!;
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public int UsageLimit { get; set; }
    public int CurrentUsage { get; set; }

    public PromoCode()
    {
    }

    public PromoCode(
        Guid id,
        string code,
        DiscountType discountType,
        decimal value,
        decimal minOrderAmount,
        DateTime expiryDate,
        bool isActive,
        int usageLimit,
        int currentUsage)
    {
        Id = id;
        Code = code;
        DiscountType = discountType;
        Value = value;
        MinOrderAmount = minOrderAmount;
        ExpiryDate = expiryDate;
        IsActive = isActive;
        UsageLimit = usageLimit;
        CurrentUsage = currentUsage;
    }
}
