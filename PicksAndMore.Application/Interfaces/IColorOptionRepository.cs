using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IColorOptionRepository
{
    Task<ColorOption?> GetByIdAsync(Guid id);
    Task<List<ColorOption>> GetAllAsync();
    Task AddAsync(ColorOption color);
    void Delete(ColorOption color);
}
