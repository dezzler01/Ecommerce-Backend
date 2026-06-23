using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PicksAndMore.Infrastructure.Repositories;

public class ColorOptionRepository : IColorOptionRepository
{
    private readonly ApplicationDbContext _context;

    public ColorOptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ColorOption?> GetByIdAsync(Guid id)
    {
        return await _context.ColorOptions.FindAsync(id);
    }

    public async Task<List<ColorOption>> GetAllAsync()
    {
        return await _context.ColorOptions.ToListAsync();
    }

    public async Task AddAsync(ColorOption color)
    {
        await _context.ColorOptions.AddAsync(color);
    }

    public void Delete(ColorOption color)
    {
        _context.ColorOptions.Remove(color);
    }
}
