using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
    }

    public async Task<ApplicationUser?> GetByPhoneAsync(string phoneNumber)
    {
        return await _context.Users.FirstOrDefaultAsync(u =>
            (u.PhoneNumber != null && u.PhoneNumber == phoneNumber) ||
            (u.UserName != null && u.UserName == phoneNumber));
    }


    public async Task AddAsync(ApplicationUser user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email != null && u.Email.ToLower() == email.ToLower());
    }

    public async Task<List<ApplicationUser>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }
}
