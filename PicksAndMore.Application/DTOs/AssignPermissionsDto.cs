namespace PicksAndMore.Application.DTOs;

public record AssignPermissionsDto(Guid RoleId, List<string> Permissions);
