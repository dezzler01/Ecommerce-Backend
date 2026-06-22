using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Notifications.Commands;

public record UpdateNotificationSubscriptionCommand(Guid UserId, bool IsSubscribed) : IRequest<ApiResponse<bool>>;

public class UpdateNotificationSubscriptionCommandHandler : IRequestHandler<UpdateNotificationSubscriptionCommand, ApiResponse<bool>>
{
    private readonly INotificationSubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNotificationSubscriptionCommandHandler(
        INotificationSubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateNotificationSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _subscriptionRepository.GetByUserIdAndTypeAsync(request.UserId, "NewOrder");

        if (request.IsSubscribed)
        {
            if (existing == null)
            {
                var subscription = new NotificationSubscription(
                    Guid.NewGuid(),
                    request.UserId,
                    "NewOrder"
                )
                {
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };
                await _subscriptionRepository.AddAsync(subscription);
            }
        }
        else
        {
            if (existing != null)
            {
                await _subscriptionRepository.DeleteAsync(existing);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true, "Notification subscription updated successfully.");
    }
}
