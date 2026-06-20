using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly ApplicationDbContext _context;

    public BrandRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Brand?> GetByIdAsync(Guid id)
    {
        return await _context.Set<Brand>().FindAsync(id);
    }

    public async Task<List<Brand>> GetAllAsync()
    {
        return await _context.Set<Brand>().ToListAsync();
    }

    public async Task AddAsync(Brand brand)
    {
        await _context.Set<Brand>().AddAsync(brand);
    }

    public void Delete(Brand brand)
    {
        _context.Set<Brand>().Remove(brand);
    }
}
