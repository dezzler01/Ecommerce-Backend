using System;

namespace PicksAndMore.Domain.Entities;

public class VisitorLog
{
    public Guid Id { get; set; }
    public string IpAddress { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string Governorate { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    public VisitorLog()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }
}
