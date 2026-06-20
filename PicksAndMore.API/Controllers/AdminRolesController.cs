using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicksAndMore.API.Authorization;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Roles.Commands;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminRolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminRolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [HasPermission("Roles:Create")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateRole([FromBody] CreateRoleDto dto)
    {
        var response = await _mediator.Send(new CreateRoleCommand(dto));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("assign-permissions")]
    [HasPermission("Roles:Update")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignPermissions([FromBody] AssignPermissionsDto dto)
    {
        var response = await _mediator.Send(new AssignPermissionsCommand(dto));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }
}
