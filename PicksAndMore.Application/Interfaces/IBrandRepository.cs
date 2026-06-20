using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IBrandRepository
{
    Task<Brand?> GetByIdAsync(Guid id);
    Task<List<Brand>> GetAllAsync();
    Task AddAsync(Brand brand);
    void Delete(Brand brand);
}
