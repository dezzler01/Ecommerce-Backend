using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Application.Orders.Commands;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Authenticated users can submit orders. The current user's identity is
    /// resolved from the JWT token via ICurrentUserService.
    /// </summary>
    [HttpPost("submit")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var response = await _mediator.Send(new CreateOrderCommand(dto));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Public guest checkout endpoint. No authentication required.
    /// The system auto-creates a guest account from the phone number,
    /// or links to an existing account if the phone is already registered.
    /// </summary>
    [HttpPost("guest-submit")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateGuestOrder([FromBody] CreateGuestOrderDto dto)
    {
        var response = await _mediator.Send(new CreateGuestOrderCommand(dto));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Validates a promo code against the current cart total.
    /// </summary>
    [HttpPost("validate-promo")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PromoCodeValidationResultDto>>> ValidatePromo(
        [FromBody] ValidatePromoRequest request,
        [FromServices] IDiscountService discountService,
        [FromServices] ICurrentUserService currentUserService)
    {
        Guid userId = Guid.Empty;
        var userIdStr = currentUserService.UserId;
        if (!string.IsNullOrEmpty(userIdStr))
        {
            Guid.TryParse(userIdStr, out userId);
        }

        var result = await discountService.ApplyPromoCodeAsync(request.Code, request.Subtotal, userId);
        return result.IsSuccess 
            ? Ok(ApiResponse<PromoCodeValidationResultDto>.Success(result, result.Message)) 
            : BadRequest(ApiResponse<PromoCodeValidationResultDto>.Failure(result, result.Message));
    }

    /// <summary>
    /// Fetch all orders placed by the currently logged-in user.
    /// </summary>
    [HttpGet("my-orders")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetMyOrders(
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] PicksAndMore.Infrastructure.Persistence.ApplicationDbContext context)
    {
        var userIdStr = currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(ApiResponse<List<OrderDto>>.Failure(null, "User identity not resolved."));
        }

        var orders = await context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Include(o => o.WalletVerification)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var dtos = orders.Select(o => PicksAndMore.Application.Mappings.MappingExtensions.ToDto(o)).ToList();
        return Ok(ApiResponse<List<OrderDto>>.Success(dtos, "My orders fetched successfully."));
    }

    /// <summary>
    /// Track order status by its Guid ID.
    /// </summary>
    [HttpGet("track/{orderId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OrderDto>>> TrackOrder(
        Guid orderId,
        [FromServices] PicksAndMore.Infrastructure.Persistence.ApplicationDbContext context)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Include(o => o.WalletVerification)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound(ApiResponse<OrderDto>.Failure(null, "Order not found."));
        }

        var dto = PicksAndMore.Application.Mappings.MappingExtensions.ToDto(order);
        return Ok(ApiResponse<OrderDto>.Success(dto, "Order retrieved successfully."));
    }
}

public class ValidatePromoRequest
{
    public string Code { get; set; } = null!;
    public decimal Subtotal { get; set; }
}
