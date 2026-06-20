using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PicksAndMore.API.Authorization;
using PicksAndMore.Application.Common;
using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Orders.Commands;
using PicksAndMore.Application.Orders.Queries;
using PicksAndMore.Application.PromoCodes.Commands;
using PicksAndMore.Domain.Entities;
using PicksAndMore.Domain.Enums;
using PicksAndMore.Infrastructure.Persistence;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("orders/{id}/verify-wallet")]
    [HasPermission("Orders:Update")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> VerifyWallet(Guid id, [FromBody] VerifyWalletRequestDto request)
    {
        var response = await _mediator.Send(new VerifyWalletCommand(id, request.Approve, request.RejectReason));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPut("orders/{id}/status")]
    [HasPermission("Orders:Update")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var targetStatus))
        {
            return BadRequest(ApiResponse<OrderDto>.Failure(null, $"Invalid OrderStatus value '{dto.Status}'."));
        }

        var response = await _mediator.Send(new UpdateOrderStatusCommand(id, targetStatus));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPost("orders/{id}/partial-return")]
    [HasPermission("Orders:Update")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> PartialReturn(Guid id, [FromBody] List<OrderItemReturnDto> returns)
    {
        var response = await _mediator.Send(new ReturnOrderItemsCommand(id, returns));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("orders/{id}/shipping-label")]
    [HasPermission("Orders:Read")]
    public async Task<IActionResult> GetShippingLabel(Guid id)
    {
        var response = await _mediator.Send(new GetOrderQuery(id));
        if (!response.IsSuccess || response.Data == null)
        {
            return NotFound("Order not found.");
        }

        var order = response.Data;
        
        // Determine COD Collection amount (only collect total if COD)
        decimal codAmount = order.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase) 
            ? order.TotalPrice 
            : 0;

        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Shipping Label - Order {order.OrderNumber}</title>
    <style>
        @media print {{
            body {{
                margin: 0;
                padding: 0;
                background: #fff;
                font-family: 'Segoe UI', Roboto, sans-serif;
            }}
            .no-print {{
                display: none;
            }}
        }}
        body {{
            font-family: 'Segoe UI', Roboto, sans-serif;
            color: #333;
            padding: 20px;
            background-color: #fafafa;
        }}
        .label-container {{
            width: 100%;
            max-width: 800px;
            background: #fff;
            margin: 0 auto;
            border: 2px solid #000;
            padding: 24px;
            box-sizing: border-box;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        }}
        .header {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 2px double #000;
            padding-bottom: 15px;
            margin-bottom: 20px;
        }}
        .header-title {{
            font-size: 28px;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .order-meta {{
            font-size: 14px;
            text-align: right;
            line-height: 1.4;
        }}
        .grid {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 20px;
        }}
        .section-title {{
            font-size: 16px;
            font-weight: bold;
            text-transform: uppercase;
            border-bottom: 1px solid #000;
            padding-bottom: 5px;
            margin-bottom: 10px;
            color: #111;
        }}
        .info-block p {{
            margin: 6px 0;
            line-height: 1.4;
            font-size: 14px;
        }}
        .cod-box {{
            border: 2px solid #000;
            padding: 15px;
            text-align: center;
            background-color: #f8f9fa;
        }}
        .cod-amount {{
            font-size: 32px;
            font-weight: bold;
            margin-top: 5px;
            color: #000;
        }}
        .packing-list {{
            margin-top: 30px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }}
        th, td {{
            border: 1px solid #000;
            padding: 10px;
            text-align: left;
            font-size: 14px;
        }}
        th {{
            background-color: #f8f9fa;
            font-weight: bold;
            text-transform: uppercase;
        }}
        .checkbox-cell {{
            width: 30px;
            text-align: center;
        }}
        .checkbox-box {{
            width: 16px;
            height: 16px;
            border: 1px solid #000;
            display: inline-block;
            vertical-align: middle;
        }}
        .footer {{
            margin-top: 40px;
            border-top: 1px dashed #000;
            padding-top: 15px;
            text-align: center;
            font-size: 12px;
            color: #666;
        }}
        .print-btn {{
            display: block;
            width: 120px;
            margin: 10px auto 20px auto;
            padding: 10px;
            background-color: #1F85A0;
            color: #fff;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            text-align: center;
            font-weight: bold;
            box-shadow: 0 2px 4px rgba(0,0,0,0.2);
        }}
    </style>
</head>
<body>
    <button class=""print-btn no-print"" onclick=""window.print()"">Print Label</button>
    <div class=""label-container"">
        <div class=""header"">
            <div class=""header-title"">Picks & More</div>
            <div class=""order-meta"">
                <strong>Order #:</strong> {order.OrderNumber}<br/>
                <strong>Date:</strong> {order.CreatedAt:yyyy-MM-dd HH:mm}<br/>
                <strong>Status:</strong> {order.Status}
            </div>
        </div>
        
        <div class=""grid"">
            <div class=""info-block"">
                <div class=""section-title"">Ship To</div>
                <p><strong>Customer:</strong> {order.CustomerName}</p>
                <p><strong>Governorate:</strong> {order.ShippingGovernorate}</p>
                <p><strong>Address:</strong> {order.ShippingDetailedAddress}</p>
                <p><strong>Primary Phone:</strong> {order.ShippingPrimaryPhone}</p>
                {(!string.IsNullOrEmpty(order.ShippingSecondaryPhone) ? $"<p><strong>Secondary Phone:</strong> {order.ShippingSecondaryPhone}</p>" : "")}
            </div>
            
            <div class=""info-block"">
                <div class=""section-title"">Collection & Payment</div>
                <div class=""cod-box"">
                    <div><strong>COD AMOUNT TO COLLECT</strong></div>
                    <div class=""cod-amount"">{codAmount:C}</div>
                    <div style=""margin-top:5px; font-size:12px; color:#555;"">Payment Method: {order.PaymentMethod}</div>
                </div>
            </div>
        </div>

        <div class=""packing-list"">
            <div class=""section-title"">Fulfillment Packing Checklist</div>
            <table>
                <thead>
                    <tr>
                        <th class=""checkbox-cell"">Ok</th>
                        <th>Product Details</th>
                        <th>UnitPrice</th>
                        <th>Qty</th>
                        <th>Subtotal</th>
                    </tr>
                </thead>
                <tbody>";

        foreach (var item in order.Items)
        {
            html += $@"
                    <tr>
                        <td class=""checkbox-cell""><div class=""checkbox-box""></div></td>
                        <td>
                            <strong>{item.ProductName}</strong><br/>
                            <span style=""font-size:11px; color:#555;"">Product ID: {item.ProductId}</span>
                        </td>
                        <td>{item.UnitPrice:C}</td>
                        <td><strong>{item.Quantity}</strong> {(item.IsReturnedPartially ? $"<span style='color:red;'>(Original: {item.OriginalQuantity})</span>" : "")}</td>
                        <td>{(item.UnitPrice * item.Quantity):C}</td>
                    </tr>";
        }

        html += $@"
                </tbody>
            </table>
        </div>

        <div class=""footer"">
            Thank you for shopping at Picks & More. For support or logistics issues, contact SupportLogistics.
        </div>
    </div>
</body>
</html>";

        return Content(html, "text/html");
    }

    [HttpGet("orders")]
    [HasPermission("Orders:Read")]
    public async Task<ActionResult<ApiResponse<PaginationResult<OrderDto>>>> GetOrders([FromQuery] GetOrdersQueryDto queryParams)
    {
        var response = await _mediator.Send(new GetOrdersQuery(queryParams));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpPatch("categories/{id}/toggle-visibility")]
    [HasPermission("Products:Update")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> ToggleCategoryVisibility(Guid id)
    {
        var response = await _mediator.Send(new PicksAndMore.Application.Categories.Commands.ToggleCategoryVisibilityCommand(id));
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    [HttpGet("shipping-settings")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<PicksAndMore.Application.Services.ShippingSettingsDto>> GetShippingSettings()
    {
        var threshold = 2000m;
        var isActive = true;

        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shipping-settings.json");
            if (System.IO.File.Exists(settingsPath))
            {
                var json = System.IO.File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<PicksAndMore.Application.Services.ShippingSettingsDto>(json);
                if (settings != null)
                {
                    threshold = settings.FreeShippingThreshold;
                    isActive = settings.IsFreeShippingActive;
                }
            }
        }
        catch
        {
            // Fallback to default
        }

        return Ok(ApiResponse<PicksAndMore.Application.Services.ShippingSettingsDto>.Success(new PicksAndMore.Application.Services.ShippingSettingsDto
        {
            FreeShippingThreshold = threshold,
            IsFreeShippingActive = isActive
        }, "Shipping settings retrieved successfully."));
    }

    [HttpPost("shipping-settings")]
    [HasPermission("Shipping:Update")]
    public ActionResult<ApiResponse<PicksAndMore.Application.Services.ShippingSettingsDto>> UpdateShippingSettings([FromBody] PicksAndMore.Application.Services.ShippingSettingsDto dto)
    {
        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shipping-settings.json");
            var json = System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(settingsPath, json);
            return Ok(ApiResponse<PicksAndMore.Application.Services.ShippingSettingsDto>.Success(dto, "Shipping settings updated successfully."));
        }
        catch (System.Exception ex)
        {
            return BadRequest(ApiResponse<PicksAndMore.Application.Services.ShippingSettingsDto>.Failure(null, $"Failed to update settings: {ex.Message}"));
        }
    }

    [HttpGet("promocodes")]
    [HasPermission("PromoCodes:Read")]
    public async Task<ActionResult<ApiResponse<List<PromoCode>>>> GetPromoCodes([FromServices] ApplicationDbContext context)
    {
        var promoCodes = await context.PromoCodes
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse<List<PromoCode>>.Success(promoCodes, "Promo codes retrieved successfully."));
    }

    [HttpPost("promocodes")]
    [HasPermission("PromoCodes:Create")]
    public async Task<ActionResult<ApiResponse<PromoCode>>> CreatePromoCode(
        [FromBody] CreatePromoCodeRequestDto dto)
    {
        var command = new CreatePromoCodeCommand(
            dto.Code,
            dto.DiscountType,
            dto.Value,
            dto.MinOrderAmount,
            dto.ExpiryDate,
            dto.IsActive,
            dto.UsageLimit
        );
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("promocodes/{id}")]
    [HasPermission("PromoCodes:Delete")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePromoCode(
        Guid id,
        [FromServices] ApplicationDbContext context)
    {
        var promoCode = await context.PromoCodes.FindAsync(id);
        if (promoCode == null)
        {
            return NotFound(ApiResponse<bool>.Failure(false, "Promo code not found."));
        }

        context.PromoCodes.Remove(promoCode);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Success(true, "Promo code deleted successfully."));
    }

    [HttpPatch("promocodes/{id}/toggle-active")]
    [HasPermission("PromoCodes:Update")]
    public async Task<ActionResult<ApiResponse<PromoCode>>> TogglePromoCodeActive(
        Guid id,
        [FromServices] ApplicationDbContext context)
    {
        var promoCode = await context.PromoCodes.FindAsync(id);
        if (promoCode == null)
        {
            return NotFound(ApiResponse<PromoCode>.Failure(null, "Promo code not found."));
        }

        promoCode.IsActive = !promoCode.IsActive;
        await context.SaveChangesAsync();

        return Ok(ApiResponse<PromoCode>.Success(promoCode, "Promo code active status toggled successfully."));
    }

    [HttpGet("roles")]
    [HasPermission("Roles:Read")]
    public async Task<ActionResult<ApiResponse<List<AdminRoleResponseDto>>>> GetRoles([FromServices] ApplicationDbContext context)
    {
        var roles = await context.Roles.AsNoTracking().ToListAsync();
        var rolePermissions = await context.RolePermissions.AsNoTracking().ToListAsync();

        var result = roles.Select(r => new AdminRoleResponseDto
        {
            Id = r.Id,
            Name = r.Name ?? string.Empty,
            Description = r.Description ?? string.Empty,
            Permissions = rolePermissions.Where(rp => rp.RoleId == r.Id).Select(rp => rp.Permission).ToList()
        }).ToList();

        return Ok(ApiResponse<List<AdminRoleResponseDto>>.Success(result, "Roles retrieved successfully."));
    }

    [HttpPost("roles")]
    [HasPermission("Roles:Create")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateRole(
        [FromBody] CreateRoleRequestDto dto,
        [FromServices] ApplicationDbContext context)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(ApiResponse<Guid>.Failure(Guid.Empty, "Role name is required."));
        }

        var normalizedName = dto.Name.Trim().ToUpper();
        var exists = await context.Roles.AnyAsync(r => r.NormalizedName == normalizedName);
        if (exists)
        {
            return BadRequest(ApiResponse<Guid>.Failure(Guid.Empty, $"Role '{dto.Name}' already exists."));
        }

        var role = new Role(Guid.NewGuid(), dto.Name.Trim(), dto.Description ?? string.Empty);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        return Ok(ApiResponse<Guid>.Success(role.Id, "Role created successfully."));
    }

    [HttpPost("roles/assign")]
    [HasPermission("Roles:Update")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignPermissions(
        [FromBody] AssignPermissionsRequestDto dto,
        [FromServices] ApplicationDbContext context)
    {
        var role = await context.Roles.FindAsync(dto.RoleId);
        if (role == null)
        {
            return NotFound(ApiResponse<bool>.Failure(false, "Role not found."));
        }

        var existing = await context.RolePermissions.Where(rp => rp.RoleId == dto.RoleId).ToListAsync();
        context.RolePermissions.RemoveRange(existing);

        if (dto.Permissions != null && dto.Permissions.Count > 0)
        {
            var newPermissions = dto.Permissions.Select(p => new RolePermission(Guid.NewGuid(), dto.RoleId, p)).ToList();
            await context.RolePermissions.AddRangeAsync(newPermissions);
        }

        await context.SaveChangesAsync();
        return Ok(ApiResponse<bool>.Success(true, "Permissions assigned successfully."));
    }

    [HttpGet("users")]
    [HasPermission("Roles:Read")]
    public async Task<ActionResult<ApiResponse<List<AdminUserResponseDto>>>> GetUsers(
        [FromServices] ApplicationDbContext context)
    {
        var users = await context.Users.Where(u => !u.IsGuest).ToListAsync();
        var roles = await context.Roles.ToListAsync();

        var result = users.Select(u => new AdminUserResponseDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            PhoneNumber = u.PhoneNumber ?? string.Empty,
            RoleId = u.RoleId,
            RoleName = roles.FirstOrDefault(r => r.Id == u.RoleId)?.Name ?? "User"
        }).ToList();

        return Ok(ApiResponse<List<AdminUserResponseDto>>.Success(result, "Users retrieved successfully."));
    }

    [HttpPost("users/assign-role")]
    [HasPermission("Roles:Update")]
    public async Task<ActionResult<ApiResponse<bool>>> AssignUserRole(
        [FromBody] AssignUserRoleRequestDto dto,
        [FromServices] ApplicationDbContext context)
    {
        var user = await context.Users.FindAsync(dto.UserId);
        if (user == null)
        {
            return NotFound(ApiResponse<bool>.Failure(false, "User not found."));
        }

        var roleExists = await context.Roles.AnyAsync(r => r.Id == dto.RoleId);
        if (!roleExists)
        {
            return BadRequest(ApiResponse<bool>.Failure(false, "Role not found."));
        }

        user.RoleId = dto.RoleId;
        await context.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Success(true, "User role assigned successfully."));
    }
}

public class AdminUserResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = null!;
}

public class AssignUserRoleRequestDto
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class VerifyWalletRequestDto
{
    public bool Approve { get; set; }
    public string? RejectReason { get; set; }
}

public class CreatePromoCodeRequestDto
{
    public string Code { get; set; } = null!;
    public string DiscountType { get; set; } = null!; // "FixedAmount", "Percentage", "FreeShipping"
    public decimal Value { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageLimit { get; set; } = 100;
}

public class AdminRoleResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<string> Permissions { get; set; } = new();
}

public class CreateRoleRequestDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class AssignPermissionsRequestDto
{
    public Guid RoleId { get; set; }
    public List<string> Permissions { get; set; } = new();
}
