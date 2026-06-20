using MediatR;
using Microsoft.AspNetCore.Mvc;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Users.Commands;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<Guid>>> Register([FromBody] RegisterUserDto dto)
    {
        var response = await _mediator.Send(new RegisterUserCommand(dto));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequest request)
    {
        var response = await _mediator.Send(new LoginCommand(request.Email, request.Password));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile(
        [FromServices] PicksAndMore.Infrastructure.Persistence.ApplicationDbContext dbContext,
        [FromServices] PicksAndMore.Application.Interfaces.ICurrentUserService currentUserService)
    {
        var userIdStr = currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return BadRequest(ApiResponse<UserProfileDto>.Failure(null, "User is not authenticated."));
        }

        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserProfileDto>.Failure(null, "User not found."));
        }

        var profileDto = new UserProfileDto
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            SecondaryPhoneNumber = user.SecondaryPhoneNumber,
            AddressDetails = user.AddressDetails,
            Governorate = user.Governorate
        };

        return Ok(ApiResponse<UserProfileDto>.Success(profileDto, "Profile retrieved successfully."));
    }

    [HttpPut("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(
        [FromBody] UserProfileDto dto,
        [FromServices] PicksAndMore.Infrastructure.Persistence.ApplicationDbContext dbContext,
        [FromServices] PicksAndMore.Application.Interfaces.ICurrentUserService currentUserService)
    {
        var userIdStr = currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return BadRequest(ApiResponse<UserProfileDto>.Failure(null, "User is not authenticated."));
        }

        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserProfileDto>.Failure(null, "User not found."));
        }

        user.FullName = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.SecondaryPhoneNumber = dto.SecondaryPhoneNumber;
        user.AddressDetails = dto.AddressDetails;
        user.Governorate = dto.Governorate;

        await dbContext.SaveChangesAsync();

        return Ok(ApiResponse<UserProfileDto>.Success(dto, "Profile updated successfully."));
    }
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class UserProfileDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? SecondaryPhoneNumber { get; set; }
    public string? AddressDetails { get; set; }
    public string? Governorate { get; set; }
}
