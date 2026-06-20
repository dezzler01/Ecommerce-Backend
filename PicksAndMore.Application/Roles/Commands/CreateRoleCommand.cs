using MediatR;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Roles.Commands;

public record CreateRoleCommand(CreateRoleDto Dto) : IRequest<ApiResponse<Guid>>;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, ApiResponse<Guid>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Dto.Name))
        {
            return ApiResponse<Guid>.Failure(null, "Role name cannot be empty.");
        }

        var exists = await _roleRepository.NameExistsAsync(request.Dto.Name);
        if (exists)
        {
            return ApiResponse<Guid>.Failure(null, "Role with this name already exists.");
        }

        var role = new Role(Guid.NewGuid(), request.Dto.Name, request.Dto.Description);
        
        await _roleRepository.AddAsync(role);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<Guid>.Success(role.Id, "Role created successfully.");
    }
}
