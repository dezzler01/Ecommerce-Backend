using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Interfaces;

namespace PicksAndMore.Application.Notifications.Commands;

public record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest<ApiResponse<bool>>;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, ApiResponse<bool>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return ApiResponse<bool>.Failure(false, "Unauthorized: User context is invalid.");
        }

        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId);
        if (notification == null)
        {
            return ApiResponse<bool>.Failure(false, "Notification not found.");
        }

        if (notification.UserId != userId)
        {
            return ApiResponse<bool>.Failure(false, "Forbidden: You cannot modify other users' notifications.");
        }

        notification.IsRead = true;
        await _notificationRepository.UpdateAsync(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Success(true, "Notification marked as read successfully.");
    }
}
