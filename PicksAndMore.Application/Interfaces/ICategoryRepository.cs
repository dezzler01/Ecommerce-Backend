using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id);
    Task<Category?> GetByNameAndAudienceAsync(string name, string targetAudience);
    Task AddAsync(Category category);
}
