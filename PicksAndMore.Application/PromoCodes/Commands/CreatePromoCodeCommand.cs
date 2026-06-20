using MediatR;
using Microsoft.AspNetCore.SignalR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.Hubs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Application.PromoCodes.Commands;

public record CreatePromoCodeCommand(
    string Code,
    string DiscountType,
    decimal Value,
    decimal MinOrderAmount,
    DateTime ExpiryDate,
    bool IsActive,
    int UsageLimit
) : IRequest<ApiResponse<PromoCode>>;

public class CreatePromoCodeCommandHandler : IRequestHandler<CreatePromoCodeCommand, ApiResponse<PromoCode>>
{
    private readonly IPromoCodeRepository _promoCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CreatePromoCodeCommandHandler(
        IPromoCodeRepository promoCodeRepository,
        IUnitOfWork unitOfWork,
        IHubContext<NotificationHub> hubContext)
    {
        _promoCodeRepository = promoCodeRepository;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
    }

    public async Task<ApiResponse<PromoCode>> Handle(CreatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return ApiResponse<PromoCode>.Failure(null, "Promo code is required.");
        }

        var codeUpper = request.Code.Trim().ToUpper();
        var existing = await _promoCodeRepository.GetByCodeAsync(codeUpper);
        if (existing != null)
        {
            return ApiResponse<PromoCode>.Failure(null, $"Promo code '{request.Code}' already exists.");
        }

        if (!Enum.TryParse<DiscountType>(request.DiscountType, true, out var discountType))
        {
            return ApiResponse<PromoCode>.Failure(null, $"Invalid DiscountType '{request.DiscountType}'.");
        }

        var promoCode = new PromoCode(
            Guid.NewGuid(),
            codeUpper,
            discountType,
            request.Value,
            request.MinOrderAmount,
            request.ExpiryDate,
            request.IsActive,
            request.UsageLimit,
            0
        );

        await _promoCodeRepository.AddAsync(promoCode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Broadcast a global notification to all connected clients announcing the new promotional event live
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
        { 
            type = "PromoCodeCreated", 
            code = codeUpper, 
            message = $"New Promo Code Available: {codeUpper}! Use it now for exclusive discounts." 
        }, cancellationToken);

        return ApiResponse<PromoCode>.Success(promoCode, "Promo code created successfully.");
    }
}
