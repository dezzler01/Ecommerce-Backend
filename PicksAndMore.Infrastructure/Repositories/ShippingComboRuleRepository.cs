using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class ShippingComboRuleRepository : IShippingComboRuleRepository
{
    private readonly ApplicationDbContext _context;

    public ShippingComboRuleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ShippingComboRule>> GetAllAsync()
    {
        return await _context.ShippingComboRules.ToListAsync();
    }

    public async Task AddAsync(ShippingComboRule rule)
    {
        await _context.ShippingComboRules.AddAsync(rule);
    }

    public async Task<ShippingComboRule?> GetByIdAsync(Guid id)
    {
        return await _context.ShippingComboRules.FindAsync(id);
    }

    public void Delete(ShippingComboRule rule)
    {
        _context.ShippingComboRules.Remove(rule);
    }
}
