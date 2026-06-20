using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Application.Services;

public class DiscountService : IDiscountService
{
    private readonly IPromoCodeRepository _promoCodeRepository;

    public DiscountService(IPromoCodeRepository promoCodeRepository)
    {
        _promoCodeRepository = promoCodeRepository;
    }

    public async Task<PromoCodeValidationResultDto> ApplyPromoCodeAsync(string code, decimal currentOrderTotal, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new PromoCodeValidationResultDto
            {
                IsSuccess = false,
                Message = "Promo code cannot be empty."
            };
        }

        var promo = await _promoCodeRepository.GetByCodeAsync(code);
        if (promo == null)
        {
            return new PromoCodeValidationResultDto
            {
                IsSuccess = false,
                Message = "Invalid promo code."
            };
        }

        if (!promo.IsActive)
        {
            return new PromoCodeValidationResultDto
            {
                IsSuccess = false,
                Message = "Promo code is inactive."
            };
        }

        if (promo.ExpiryDate < DateTime.UtcNow)
        {
            return new PromoCodeValidationResultDto
            {
                IsSuccess = false,
                Message = "Promo code has expired."
            };
        }

        if (promo.CurrentUsage >= promo.UsageLimit)
        {
            return new PromoCodeValidationResultDto
            {
                IsSuccess = false,
                Message = "Promo code usage limit has been reached."
            };
        }

        if (currentOrderTotal < promo.MinOrderAmount)
        {
            return new PromoCodeValidationResultDto
            {
                IsSuccess = false,
                Message = $"Minimum order amount of {promo.MinOrderAmount:C} is required to apply this promo code."
            };
        }

        decimal discountAmount = 0;
        bool isFreeShipping = false;

        switch (promo.DiscountType)
        {
            case DiscountType.FixedAmount:
                discountAmount = Math.Min(promo.Value, currentOrderTotal);
                break;

            case DiscountType.Percentage:
                discountAmount = Math.Round(currentOrderTotal * (promo.Value / 100m), 2);
                break;

            case DiscountType.FreeShipping:
                isFreeShipping = true;
                break;
        }

        return new PromoCodeValidationResultDto
        {
            IsSuccess = true,
            Message = "Promo code applied successfully.",
            DiscountAmount = discountAmount,
            IsFreeShipping = isFreeShipping,
            PromoCodeId = promo.Id
        };
    }
}
