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

        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return ApiResponse<Guid>.Failure(null, "Email and Password are required.");
        }

        var emailExists = await _userRepository.EmailExistsAsync(dto.Email);
        if (emailExists)
        {
            return ApiResponse<Guid>.Failure(null, "A user with this email already exists.");
        }

        var roleExists = await _roleRepository.RoleExistsAsync(dto.RoleId);
        if (!roleExists)
        {
            return ApiResponse<Guid>.Failure(null, "Specified role does not exist.");
        }

        var userId = Guid.NewGuid();
        
        var tempUser = new ApplicationUser(userId, dto.FullName, dto.Email, string.Empty, dto.RoleId);
        var passwordHash = _passwordHasher.HashPassword(tempUser, dto.Password);
        
        var user = new ApplicationUser(userId, dto.FullName, dto.Email, passwordHash, dto.RoleId);

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<Guid>.Success(user.Id, "User registered successfully.");
    }
}
