using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class NotificationSubscriptionRepository : INotificationSubscriptionRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationSubscriptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(NotificationSubscription subscription)
    {
        await _context.NotificationSubscriptions.AddAsync(subscription);
    }

    public async Task DeleteAsync(NotificationSubscription subscription)
    {
        _context.NotificationSubscriptions.Remove(subscription);
        await Task.CompletedTask;
    }

    public async Task<List<NotificationSubscription>> GetByNotificationTypeAsync(string notificationType)
    {
        return await _context.NotificationSubscriptions
            .Where(ns => ns.NotificationType == notificationType)
            .ToListAsync();
    }

    public async Task<NotificationSubscription?> GetByUserIdAndTypeAsync(Guid userId, string notificationType)
    {
        return await _context.NotificationSubscriptions
            .FirstOrDefaultAsync(ns => ns.UserId == userId && ns.NotificationType == notificationType);
    }

    public async Task<List<NotificationSubscription>> GetAllAsync()
    {
        return await _context.NotificationSubscriptions.ToListAsync();
    }
}
