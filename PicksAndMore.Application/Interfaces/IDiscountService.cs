using PicksAndMore.Application.DTOs;

namespace PicksAndMore.Application.Interfaces;

public interface IDiscountService
{
    Task<PromoCodeValidationResultDto> ApplyPromoCodeAsync(string code, decimal currentOrderTotal, Guid userId);
}
