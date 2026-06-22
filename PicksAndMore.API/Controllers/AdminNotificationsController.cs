using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PicksAndMore.API.Authorization;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Notifications.Commands;
using PicksAndMore.Application.Notifications.Queries;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/admin/notifications")]
[Authorize]
public class AdminNotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminNotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("subscriptions")]
    [HasPermission("Orders:Read")]
    public async Task<ActionResult<ApiResponse<List<NotificationSubscriptionDto>>>> GetSubscriptions()
    {
        var response = await _mediator.Send(new GetNotificationSubscriptionsQuery());
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("subscriptions")]
    [HasPermission("Orders:Update")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSubscription([FromBody] UpdateNotificationSubscriptionCommand command)
    {
        var response = await _mediator.Send(command);
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }
}
