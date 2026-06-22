using System;

namespace PicksAndMore.Domain.Entities;

public class Notification : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!; // "NewOrder", "OrderStatusChanged", etc.
    public bool IsRead { get; set; }
    public string? RelatedEntityId { get; set; }

    public Notification()
    {
    }

    public Notification(Guid id, Guid userId, string title, string message, string type, string? relatedEntityId = null)
    {
        Id = id;
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        IsRead = false;
        RelatedEntityId = relatedEntityId;
    }
}
