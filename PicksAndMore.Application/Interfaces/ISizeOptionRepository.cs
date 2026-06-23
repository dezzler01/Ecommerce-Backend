using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface ISizeOptionRepository
{
    Task<SizeOption?> GetByIdAsync(Guid id);
    Task<List<SizeOption>> GetAllAsync();
    Task AddAsync(SizeOption size);
    void Delete(SizeOption size);
}
