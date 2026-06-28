namespace PicksAndMore.Application.DTOs;

public record RegisterUserDto(string FullName, string Email, string Password, Guid RoleId, string? PhoneNumber = null);
