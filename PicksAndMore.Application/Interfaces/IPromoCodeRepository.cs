using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IPromoCodeRepository
{
    Task<PromoCode?> GetByIdAsync(Guid id);
    Task<PromoCode?> GetByCodeAsync(string code);
    Task AddAsync(PromoCode promoCode);
}
