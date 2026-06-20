using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class ShippingMatrixRepository : IShippingMatrixRepository
{
    private readonly ApplicationDbContext _context;

    public ShippingMatrixRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShippingMatrix?> GetByIdAsync(Guid id)
    {
        return await _context.Set<ShippingMatrix>().FindAsync(id);
    }

    public async Task<ShippingMatrix?> GetByGovernorateAsync(string governorate)
    {
        return await _context.Set<ShippingMatrix>()
            .FirstOrDefaultAsync(s => s.Governorate.ToLower() == governorate.ToLower());
    }

    public async Task AddAsync(ShippingMatrix shippingMatrix)
    {
        await _context.Set<ShippingMatrix>().AddAsync(shippingMatrix);
    }

    public async Task<List<ShippingMatrix>> GetAllAsync()
    {
        return await _context.Set<ShippingMatrix>().ToListAsync();
    }
}
