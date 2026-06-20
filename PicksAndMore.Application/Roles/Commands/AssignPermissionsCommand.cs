using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Roles.Commands;

public record AssignPermissionsCommand(AssignPermissionsDto Dto) : IRequest<ApiResponse<bool>>;

public class AssignPermissionsCommandHandler : IRequestHandler<AssignPermissionsCommand, ApiResponse<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignPermissionsCommandHandler(
        IRoleRepository roleRepository, 
        IRolePermissionRepository rolePermissionRepository, 
        IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(AssignPermissionsCommand request, CancellationToken cancellationToken)
    {
        var roleExists = await _roleRepository.RoleExistsAsync(request.Dto.RoleId);
        if (!roleExists)
        {
            return ApiResponse<bool>.Failure(false, "Role not found.");
        }

        // 1. Remove existing permissions mapped to this role
        var existingPermissions = await _rolePermissionRepository.GetByRoleIdAsync(request.Dto.RoleId);
        _rolePermissionRepository.RemoveRange(existingPermissions);

        // 2. Map new permissions
        var newPermissions = request.Dto.Permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => new RolePermission(Guid.NewGuid(), request.Dto.RoleId, p))
            .ToList();

        await _rolePermissionRepository.AddRangeAsync(newPermissions);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.Success(true, "Permissions assigned successfully.");
    }
}
