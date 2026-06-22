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
    /// Track order status. Requires the last 4 digits of the registered phone number
    /// to prevent unauthorised PII exposure via GUID enumeration.
    /// </summary>
    [HttpGet("track/{orderId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OrderTrackingDto>>> TrackOrder(
        Guid orderId,
        [FromQuery] string phone,
        [FromServices] PicksAndMore.Infrastructure.Persistence.ApplicationDbContext context)
    {
        // Require phone verification (last 4 digits)
        if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length != 4 || !phone.Trim().All(char.IsDigit))
        {
            return BadRequest(ApiResponse<OrderTrackingDto>.Failure(null,
                "Please provide the last 4 digits of the phone number used for this order."));
        }

        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            // Return generic message — don't reveal whether the GUID exists
            return NotFound(ApiResponse<OrderTrackingDto>.Failure(null,
                "Order not found. Please check your Order ID and phone number."));
        }

        // Verify last 4 digits of primary phone
        var storedPhone = order.ShippingAddress.PrimaryPhone?.Trim() ?? string.Empty;
        var last4 = storedPhone.Length >= 4 ? storedPhone[^4..] : storedPhone;
        if (!string.Equals(last4, phone.Trim(), StringComparison.Ordinal))
        {
            return Unauthorized(ApiResponse<OrderTrackingDto>.Failure(null,
                "Order not found. Please check your Order ID and phone number."));
        }

        // Map to a tracking-only DTO with masked PII
        var dto = new OrderTrackingDto
        {
            OrderNumber    = $"ORD-{order.Id.ToString()[..8].ToUpper()}",
            OrderStatus    = order.OrderStatus.ToString(),
            PaymentMethod  = order.PaymentMethod.ToString(),
            ShippingCost   = order.ShippingCost,
            TotalPrice     = order.TotalPrice,
            OrderDate      = order.OrderDate,
            // Mask PII: show only first name initial + last name, last 4 of phone, city only
            MaskedName     = MaskName(order.User?.FullName ?? "Guest"),
            MaskedPhone    = MaskPhone(storedPhone),
            City           = order.ShippingAddress.Governorate,
            Items = order.Items.Select(i => new OrderTrackingItemDto
            {
                ProductTitle = i.Product?.Title ?? "Item",
                Quantity     = i.Quantity,
                UnitPrice    = i.UnitPrice
            }).ToList()
        };

        return Ok(ApiResponse<OrderTrackingDto>.Success(dto, "Order retrieved successfully."));
    }

    private static string MaskName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "***";
        var first = parts[0].Length > 0 ? parts[0][0] + "***" : "***";
        var last  = parts.Length > 1 ? parts[^1] : string.Empty;
        return string.IsNullOrEmpty(last) ? first : $"{first} {last}";
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return "****";
        return new string('*', phone.Length - 4) + phone[^4..];
    }
}

public class ValidatePromoRequest
{
    public string Code { get; set; } = null!;
    public decimal Subtotal { get; set; }
}
