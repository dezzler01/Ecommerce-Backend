using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IShippingComboRuleRepository
{
    Task<List<ShippingComboRule>> GetAllAsync();
    Task AddAsync(ShippingComboRule rule);
    Task<ShippingComboRule?> GetByIdAsync(Guid id);
    void Delete(ShippingComboRule rule);
}
