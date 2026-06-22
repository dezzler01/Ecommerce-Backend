using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Notifications.Commands;
using PicksAndMore.Application.Notifications.Queries;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications()
    {
        var response = await _mediator.Send(new GetNotificationsQuery());
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
    {
        var response = await _mediator.Send(new GetUnreadNotificationsCountQuery());
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id)
    {
        var response = await _mediator.Send(new MarkNotificationAsReadCommand(id));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
    {
        var response = await _mediator.Send(new MarkAllNotificationsAsReadCommand());
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }
}
