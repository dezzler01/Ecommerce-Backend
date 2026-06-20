using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IProductShippingOverrideRepository
{
    Task<ProductShippingOverride?> GetByProductIdAsync(Guid productId);
    Task AddAsync(ProductShippingOverride overrideRule);
    Task<List<ProductShippingOverride>> GetByProductIdsAsync(List<Guid> productIds);
    void Delete(ProductShippingOverride overrideRule);
}
