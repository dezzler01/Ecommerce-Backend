using PicksAndMore.Application.DTOs;

namespace PicksAndMore.Application.Interfaces;

public interface IShippingService
{
    Task<decimal> CalculateShippingCostAsync(Guid userId, List<OrderItemDto> items, string governorate, bool isPromoFreeShipping = false);
}
