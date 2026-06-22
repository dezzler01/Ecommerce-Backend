using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByNameAsync(string name);
    Task AddAsync(Role role);
    Task<bool> RoleExistsAsync(Guid id);
    Task<bool> NameExistsAsync(string name);
    Task<List<Role>> GetAllAsync();
}
