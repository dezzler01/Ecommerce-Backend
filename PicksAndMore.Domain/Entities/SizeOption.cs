using System;

namespace PicksAndMore.Domain.Entities;

public class SizeOption : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string TargetAudience { get; set; } = "Both"; // "Women", "Kids", or "Both"
    public int SortOrder { get; set; }
    public string CategoryType { get; set; } = "Women Clothing"; // "Women Clothing", "Women Shoes", "Kids Clothing", "Kids Shoes", "Universal"

    public SizeOption() { }

    public SizeOption(Guid id, string name, string targetAudience, int sortOrder, string categoryType = "Women Clothing")
    {
        Id = id;
        Name = name;
        TargetAudience = targetAudience;
        SortOrder = sortOrder;
        CategoryType = categoryType;
    }
}
