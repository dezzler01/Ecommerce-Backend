using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<List<Notification>> GetByUserIdAsync(Guid userId);
    Task<int> GetUnreadCountByUserIdAsync(Guid userId);
    Task MarkAllAsReadByUserIdAsync(Guid userId);
    Task UpdateAsync(Notification notification);
}
