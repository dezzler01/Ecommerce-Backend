using System;

namespace PicksAndMore.Domain.Entities;

public class ColorOption : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string HexCode { get; set; } = null!;

    public ColorOption() { }

    public ColorOption(Guid id, string name, string hexCode)
    {
        Id = id;
        Name = name;
        HexCode = hexCode;
    }
}
