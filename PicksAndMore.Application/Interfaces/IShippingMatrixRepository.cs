using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IShippingMatrixRepository
{
    Task<ShippingMatrix?> GetByIdAsync(Guid id);
    Task<ShippingMatrix?> GetByGovernorateAsync(string governorate);
    Task AddAsync(ShippingMatrix shippingMatrix);
    Task<List<ShippingMatrix>> GetAllAsync();
}
