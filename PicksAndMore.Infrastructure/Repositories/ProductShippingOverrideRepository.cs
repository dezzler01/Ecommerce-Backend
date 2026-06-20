using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class ProductShippingOverrideRepository : IProductShippingOverrideRepository
{
    private readonly ApplicationDbContext _context;

    public ProductShippingOverrideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductShippingOverride?> GetByProductIdAsync(Guid productId)
    {
        return await _context.ProductShippingOverrides
            .FirstOrDefaultAsync(o => o.ProductId == productId);
    }

    public async Task AddAsync(ProductShippingOverride overrideRule)
    {
        await _context.ProductShippingOverrides.AddAsync(overrideRule);
    }

    public async Task<List<ProductShippingOverride>> GetByProductIdsAsync(List<Guid> productIds)
    {
        return await _context.ProductShippingOverrides
            .Where(o => productIds.Contains(o.ProductId))
            .ToListAsync();
    }

    public void Delete(ProductShippingOverride overrideRule)
    {
        _context.ProductShippingOverrides.Remove(overrideRule);
    }
}
