using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByPhoneAsync(string phoneNumber);
    Task AddAsync(ApplicationUser user);
    Task<bool> EmailExistsAsync(string email);
}
