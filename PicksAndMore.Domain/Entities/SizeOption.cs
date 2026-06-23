using System;

namespace PicksAndMore.Domain.Entities;

public class SizeOption : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string TargetAudience { get; set; } = "Both"; // "Women", "Kids", or "Both"
    public int SortOrder { get; set; }

    public SizeOption() { }

    public SizeOption(Guid id, string name, string targetAudience, int sortOrder)
    {
        Id = id;
        Name = name;
        TargetAudience = targetAudience;
        SortOrder = sortOrder;
    }
}
