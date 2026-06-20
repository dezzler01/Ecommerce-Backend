using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class PromoCodeRepository : IPromoCodeRepository
{
    private readonly ApplicationDbContext _context;

    public PromoCodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PromoCode?> GetByIdAsync(Guid id)
    {
        return await _context.PromoCodes.FindAsync(id);
    }

    public async Task<PromoCode?> GetByCodeAsync(string code)
    {
        return await _context.PromoCodes
            .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower());
    }

    public async Task AddAsync(PromoCode promoCode)
    {
        await _context.PromoCodes.AddAsync(promoCode);
    }
}
