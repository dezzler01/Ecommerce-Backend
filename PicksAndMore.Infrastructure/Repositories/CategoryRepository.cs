using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Set<Category>().FindAsync(id);
    }

    public async Task<Category?> GetByNameAndAudienceAsync(string name, string targetAudience)
    {
        return await _context.Set<Category>()
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() && c.TargetAudience.ToLower() == targetAudience.ToLower());
    }

    public async Task AddAsync(Category category)
    {
        await _context.Set<Category>().AddAsync(category);
    }
}
