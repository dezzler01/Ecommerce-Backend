using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface INotificationSubscriptionRepository
{
    Task AddAsync(NotificationSubscription subscription);
    Task DeleteAsync(NotificationSubscription subscription);
    Task<List<NotificationSubscription>> GetByNotificationTypeAsync(string notificationType);
    Task<NotificationSubscription?> GetByUserIdAndTypeAsync(Guid userId, string notificationType);
    Task<List<NotificationSubscription>> GetAllAsync();
}
