using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Interfaces;

namespace PicksAndMore.Application.Notifications.Queries;

public record GetUnreadNotificationsCountQuery : IRequest<ApiResponse<int>>;

public class GetUnreadNotificationsCountQueryHandler : IRequestHandler<GetUnreadNotificationsCountQuery, ApiResponse<int>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationsCountQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<int>> Handle(GetUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return ApiResponse<int>.Failure(0, "Unauthorized: User context is invalid.");
        }

        var count = await _notificationRepository.GetUnreadCountByUserIdAsync(userId);
        return ApiResponse<int>.Success(count, "Unread count retrieved successfully.");
    }
}
