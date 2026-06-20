using MediatR;
using Microsoft.AspNetCore.Identity;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Users.Commands;

public record LoginCommand(string Email, string Password) : IRequest<ApiResponse<AuthResponseDto>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository, 
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ApiResponse<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<AuthResponseDto>.Failure(null, "Email and Password are required.");
        }

        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return ApiResponse<AuthResponseDto>.Failure(null, "Invalid email or password.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return ApiResponse<AuthResponseDto>.Failure(null, "Invalid email or password.");
        }

        var role = await _roleRepository.GetByIdAsync(user.RoleId);
        var roleName = role?.Name ?? "User";

        // Query permissions mapped to this role
        var permissions = await _rolePermissionRepository.GetPermissionsByRoleIdAsync(user.RoleId);

        var token = _jwtTokenGenerator.GenerateJwtToken(user, roleName, permissions);

        var responseDto = new AuthResponseDto(token, user.FullName, roleName);
        return ApiResponse<AuthResponseDto>.Success(responseDto, "Login successful.");
    }
}
