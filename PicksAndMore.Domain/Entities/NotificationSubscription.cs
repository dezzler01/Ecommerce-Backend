using System;

namespace PicksAndMore.Domain.Entities;

public class NotificationSubscription : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string NotificationType { get; set; } = null!; // e.g. "NewOrder"

    public NotificationSubscription()
    {
    }

    public NotificationSubscription(Guid id, Guid userId, string notificationType)
    {
        Id = id;
        UserId = userId;
        NotificationType = notificationType;
    }
}
