using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;

namespace PicksAndMore.Application.Notifications.Queries;

public record GetNotificationSubscriptionsQuery : IRequest<ApiResponse<List<NotificationSubscriptionDto>>>;

public class GetNotificationSubscriptionsQueryHandler : IRequestHandler<GetNotificationSubscriptionsQuery, ApiResponse<List<NotificationSubscriptionDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly INotificationSubscriptionRepository _subscriptionRepository;

    public GetNotificationSubscriptionsQueryHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        INotificationSubscriptionRepository subscriptionRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<ApiResponse<List<NotificationSubscriptionDto>>> Handle(GetNotificationSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync();
        var roles = await _roleRepository.GetAllAsync();
        var subscriptions = await _subscriptionRepository.GetByNotificationTypeAsync("NewOrder");

        var dtos = users
            .Where(u => !u.IsGuest)
            .Select(u => new NotificationSubscriptionDto
            {
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                RoleName = roles.FirstOrDefault(r => r.Id == u.RoleId)?.Name ?? "User",
                IsSubscribed = subscriptions.Any(s => s.UserId == u.Id)
            })
            .OrderBy(d => d.RoleName)
            .ThenBy(d => d.FullName)
            .ToList();

        return ApiResponse<List<NotificationSubscriptionDto>>.Success(dtos, "Notification subscriptions retrieved successfully.");
    }
}
