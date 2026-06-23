using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PicksAndMore.Infrastructure.Repositories;

public class SizeOptionRepository : ISizeOptionRepository
{
    private readonly ApplicationDbContext _context;

    public SizeOptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SizeOption?> GetByIdAsync(Guid id)
    {
        return await _context.SizeOptions.FindAsync(id);
    }

    public async Task<List<SizeOption>> GetAllAsync()
    {
        return await _context.SizeOptions.ToListAsync();
    }

    public async Task AddAsync(SizeOption size)
    {
        await _context.SizeOptions.AddAsync(size);
    }

    public void Delete(SizeOption size)
    {
        _context.SizeOptions.Remove(size);
    }
}
