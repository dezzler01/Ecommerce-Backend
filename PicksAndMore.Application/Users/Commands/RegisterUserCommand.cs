using MediatR;
using Microsoft.AspNetCore.Identity;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Users.Commands;

public record RegisterUserCommand(RegisterUserDto Dto) : IRequest<ApiResponse<Guid>>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ApiResponse<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // 1. FullName validation
        if (string.IsNullOrWhiteSpace(dto.FullName) || dto.FullName.Trim().Length < 3)
        {
            return ApiResponse<Guid>.Failure(null, "FullName is required and must be at least 3 characters.");
        }

        // 2. Email format validation (Official Regex check)
        var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (string.IsNullOrWhiteSpace(dto.Email) || !emailRegex.IsMatch(dto.Email))
        {
            return ApiResponse<Guid>.Failure(null, "Please enter a valid email address.");
        }

        // 3. Duplicate Email check
        var emailExists = await _userRepository.EmailExistsAsync(dto.Email);
        if (emailExists)
        {
            return ApiResponse<Guid>.Failure(null, "Email already registered");
        }

        // 4. Password Strength Validation
        if (string.IsNullOrWhiteSpace(dto.Password) || 
            dto.Password.Length < 8 || 
            !dto.Password.Any(char.IsUpper) || 
            !dto.Password.Any(char.IsDigit) || 
            !dto.Password.Any(c => !char.IsLetterOrDigit(c)))
        {
            return ApiResponse<Guid>.Failure(null, "Password must be at least 8 characters with a capital letter, a number, and a special character.");
        }

        // 5. Role Mapping & Fallback creation
        var targetRoleId = dto.RoleId;
        // Admin role mapping check (if ADMIN_ROLE_ID a3b07384-d113-40e1-a3f2-861f2113d077 is passed)
        if (targetRoleId == Guid.Parse("a3b07384-d113-40e1-a3f2-861f2113d077"))
        {
            targetRoleId = Guid.Parse("a5e2f7b4-3c82-411a-85d1-12c8a2bbdd01"); // backend admin role
        }
        else
        {
            targetRoleId = Guid.Parse("c3b07384-d113-40e1-a3f2-861f2113d077"); // Customer role
            var hasCustomerRole = await _roleRepository.RoleExistsAsync(targetRoleId);
            if (!hasCustomerRole)
            {
                var customerRole = new Role(targetRoleId, "Customer", "Regular retail storefront customer");
                await _roleRepository.AddAsync(customerRole);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        var userId = Guid.NewGuid();
        
        var tempUser = new ApplicationUser(userId, dto.FullName, dto.Email, string.Empty, targetRoleId);
        var passwordHash = _passwordHasher.HashPassword(tempUser, dto.Password);
        
        var user = new ApplicationUser(userId, dto.FullName, dto.Email, passwordHash, targetRoleId);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<Guid>.Success(user.Id, "User registered successfully.");
    }
}
