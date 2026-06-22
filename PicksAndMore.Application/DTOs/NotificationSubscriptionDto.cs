using System;

namespace PicksAndMore.Application.DTOs;

public class NotificationSubscriptionDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public bool IsSubscribed { get; set; }
}
