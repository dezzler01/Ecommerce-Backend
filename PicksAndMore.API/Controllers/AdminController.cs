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
            return NotFound("Order not found.");

        var order = response.Data;
        decimal codAmount      = order.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase) ? order.TotalPrice : 0;
        decimal itemsSubtotal  = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        string statusColor = order.Status switch
        {
            "PendingVerification" => "#F59E0B",
            "ConfirmedPreparing"  => "#3B82F6",
            "OutForDelivery"      => "#8B5CF6",
            "Delivered"           => "#10B981",
            "ReturnedRejected"    => "#EF4444",
            _                     => "#6B7280"
        };
        string statusLabel = order.Status switch
        {
            "PendingVerification" => "Pending Verification",
            "ConfirmedPreparing"  => "Confirmed — Preparing",
            "OutForDelivery"      => "Out for Delivery",
            "Delivered"           => "Delivered ✓",
            "ReturnedRejected"    => "Returned / Rejected",
            _                     => order.Status
        };

        // ── Build items rows ──
        var itemRows = new System.Text.StringBuilder();
        foreach (var item in order.Items)
        {
            string returnTag = item.IsReturnedPartially
                ? $"<div class=\"return-tag\">&#9888; Partial Return — Original Qty: {item.OriginalQuantity}</div>"
                : "";
            itemRows.Append($@"
            <tr>
                <td class=""center""><span class=""check-box""></span></td>
                <td>
                    <div class=""item-name"">{System.Net.WebUtility.HtmlEncode(item.ProductName)}</div>
                    {returnTag}
                </td>
                <td class=""mono"">EGP {item.UnitPrice:N2}</td>
                <td class=""center""><span class=""qty-badge"">{item.Quantity}</span></td>
                <td class=""mono"">EGP {(item.UnitPrice * item.Quantity):N2}</td>
            </tr>");
        }

        // ── Payment panel ──
        string payPanel = codAmount > 0
            ? $@"<div class=""pay-label"">COD — Amount to Collect</div>
                 <div class=""cod-amount""><span class=""cod-currency"">EGP</span>{codAmount:N2}</div>
                 <span class=""pay-method-tag"">Cash on Delivery</span>"
            : $@"<div class=""pay-label"">Payment Method</div>
                 <div class=""cod-amount"" style=""font-size:28px;color:#10B981;"">&#10003; PREPAID</div>
                 <span class=""pay-method-tag"" style=""background:#10B981;"">{order.PaymentMethod}</span>
                 <div class=""prepaid-note"">No cash collection required</div>";

        string phone2Row = string.IsNullOrEmpty(order.ShippingSecondaryPhone) ? "" :
            $@"<div class=""ship-row""><span class=""ship-key"">Phone 2</span><span class=""ship-val"">{order.ShippingSecondaryPhone}</span></div>";

        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8""/>
<meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
<title>Invoice {order.OrderNumber} | Picks &amp; More</title>
<link rel=""preconnect"" href=""https://fonts.googleapis.com"">
<link href=""https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;600;700&family=Inter:wght@300;400;500;600;700&display=swap"" rel=""stylesheet"">
<style>
*,*::before,*::after{{box-sizing:border-box;margin:0;padding:0}}
:root{{--ch:#2A2522;--tr:#E07A5F;--tr2:#C05F45;--bg:#FBF9F6;--bg2:#F4EFE8;--mu:#6B5E57;--bd:#E8E0D8}}
body{{font-family:'Inter','Segoe UI',sans-serif;background:#EDEAE6;color:var(--ch);padding:32px 16px 64px}}
.toolbar{{display:flex;justify-content:center;gap:12px;margin-bottom:28px}}
.btn{{display:inline-flex;align-items:center;gap:8px;padding:11px 32px;background:var(--tr);color:#fff;border:none;border-radius:8px;font-family:'Inter',sans-serif;font-size:13px;font-weight:600;letter-spacing:.08em;text-transform:uppercase;cursor:pointer;box-shadow:0 4px 14px rgba(224,122,95,.4)}}
.btn:hover{{background:var(--tr2)}}
.invoice{{width:100%;max-width:840px;margin:0 auto;background:#fff;border-radius:4px;box-shadow:0 2px 40px rgba(42,37,34,.12);overflow:hidden}}
.accent{{height:5px;background:linear-gradient(90deg,#F4A261 0%,var(--tr) 45%,#B84F7D 100%)}}
.inv-hd{{display:flex;justify-content:space-between;align-items:flex-start;padding:32px 36px 24px;border-bottom:1px solid var(--bd);background:var(--bg)}}
.brand{{font-family:'Playfair Display',Georgia,serif;font-size:30px;font-weight:700;letter-spacing:-.02em;color:var(--ch)}}
.brand-sub{{font-size:10px;font-weight:500;letter-spacing:.22em;text-transform:uppercase;color:var(--tr);margin-top:5px}}
.meta{{text-align:right;font-size:13px;line-height:1.7;color:var(--mu)}}
.meta strong{{color:var(--ch);font-weight:600}}
.ord-num{{font-family:'Playfair Display',serif;font-size:20px;font-weight:700;color:var(--ch);letter-spacing:.05em;margin-bottom:4px}}
.badge{{display:inline-block;padding:3px 10px;border-radius:20px;font-size:10px;font-weight:600;letter-spacing:.12em;text-transform:uppercase;color:#fff;background:{statusColor};margin-top:4px}}
.body{{padding:32px 36px}}
.two{{display:grid;grid-template-columns:1fr 1fr;gap:24px;margin-bottom:32px}}
.sec{{font-size:9px;font-weight:700;letter-spacing:.22em;text-transform:uppercase;color:var(--tr);margin-bottom:12px;padding-bottom:6px;border-bottom:1.5px solid var(--tr)}}
.ship-card{{background:var(--bg2);border:1px solid var(--bd);border-radius:8px;padding:18px 20px}}
.ship-row{{display:flex;gap:8px;font-size:13px;line-height:1.5;margin-bottom:6px}}
.ship-row:last-child{{margin-bottom:0}}
.ship-key{{font-weight:600;color:var(--mu);min-width:76px;font-size:11px;letter-spacing:.04em;text-transform:uppercase;padding-top:1px}}
.ship-val{{font-weight:500;color:var(--ch)}}
.pay-card{{border:2px solid var(--tr);border-radius:8px;padding:20px;text-align:center;background:linear-gradient(135deg,#FFF6F3 0%,var(--bg) 100%);display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:180px}}
.pay-label{{font-size:9px;font-weight:700;letter-spacing:.22em;text-transform:uppercase;color:var(--mu);margin-bottom:10px}}
.cod-amount{{font-family:'Playfair Display',serif;font-size:42px;font-weight:700;color:var(--tr);line-height:1;margin-bottom:10px}}
.cod-currency{{font-size:20px;vertical-align:super;margin-right:2px}}
.pay-method-tag{{display:inline-block;padding:4px 14px;background:var(--ch);color:#fff;border-radius:20px;font-size:10px;font-weight:600;letter-spacing:.1em;text-transform:uppercase}}
.prepaid-note{{font-size:12px;color:var(--mu);margin-top:10px}}
.tbl-wrap{{border:1px solid var(--bd);border-radius:8px;overflow:hidden;margin-bottom:24px}}
table{{width:100%;border-collapse:collapse}}
thead tr{{background:var(--ch);color:var(--bg)}}
thead th{{padding:12px 14px;font-size:9px;font-weight:700;letter-spacing:.18em;text-transform:uppercase;text-align:left}}
th.center,td.center{{text-align:center}}
tbody tr{{border-bottom:1px solid var(--bd)}}
tbody tr:last-child{{border-bottom:none}}
tbody tr:nth-child(even){{background:var(--bg2)}}
td{{padding:13px 14px;font-size:13px;vertical-align:middle}}
td.mono{{font-family:'Courier New',monospace;font-size:13px;font-weight:600}}
.item-name{{font-weight:600;color:var(--ch);margin-bottom:2px}}
.qty-badge{{display:inline-flex;align-items:center;justify-content:center;width:28px;height:28px;border-radius:50%;background:var(--ch);color:#fff;font-weight:700;font-size:13px}}
.return-tag{{display:inline-block;font-size:10px;color:#EF4444;font-weight:600;margin-top:3px}}
.check-box{{width:18px;height:18px;border:2px solid var(--ch);border-radius:3px;display:inline-block}}
.totals{{display:flex;justify-content:flex-end;margin-bottom:8px}}
.totals-inner{{width:290px}}
.t-row{{display:flex;justify-content:space-between;font-size:13px;padding:4px 0;color:var(--mu)}}
.t-row.grand{{border-top:2px solid var(--ch);margin-top:8px;padding-top:10px;font-weight:700;font-size:16px;color:var(--ch)}}
.t-row.grand .val{{color:var(--tr)}}
.inv-ft{{padding:20px 36px 28px;border-top:1px dashed var(--bd);display:flex;justify-content:space-between;align-items:center;background:var(--bg2)}}
.ft-brand{{font-family:'Playfair Display',serif;font-size:14px;font-weight:600;color:var(--ch)}}
.ft-txt{{font-size:11px;color:var(--mu);text-align:center;line-height:1.6}}
.ft-ord{{font-size:11px;text-align:right;color:var(--mu);font-family:'Courier New',monospace}}
@media print{{
  body{{background:#fff;padding:0}}
  .toolbar{{display:none}}
  .invoice{{box-shadow:none;border-radius:0;max-width:100%}}
  @page{{margin:10mm;size:A4}}
}}
</style>
</head>
<body>
<div class=""toolbar"">
  <button class=""btn"" onclick=""window.print()"">&#128438;&nbsp; Print Invoice</button>
</div>
<div class=""invoice"">
  <div class=""accent""></div>
  <div class=""inv-hd"">
    <div>
      <div class=""brand"">Picks &amp; More</div>
      <div class=""brand-sub"">Luxury Curated Collections</div>
    </div>
    <div class=""meta"">
      <div class=""ord-num"">{order.OrderNumber}</div>
      <div><strong>Date:</strong> {order.CreatedAt:dd MMM yyyy, HH:mm}</div>
      <div><strong>Invoice:</strong> INV-{order.CreatedAt:yyyyMMdd}-{order.OrderNumber.Replace("ORD-","")}</div>
      <div><span class=""badge"">{statusLabel}</span></div>
    </div>
  </div>
  <div class=""body"">
    <div class=""two"">
      <div>
        <div class=""sec"">&#x1F4E6;&nbsp; Ship To</div>
        <div class=""ship-card"">
          <div class=""ship-row""><span class=""ship-key"">Customer</span><span class=""ship-val"">{System.Net.WebUtility.HtmlEncode(order.CustomerName)}</span></div>
          <div class=""ship-row""><span class=""ship-key"">Governorate</span><span class=""ship-val"">{System.Net.WebUtility.HtmlEncode(order.ShippingGovernorate)}</span></div>
          <div class=""ship-row""><span class=""ship-key"">Address</span><span class=""ship-val"">{System.Net.WebUtility.HtmlEncode(order.ShippingDetailedAddress)}</span></div>
          <div class=""ship-row""><span class=""ship-key"">Phone 1</span><span class=""ship-val"">{order.ShippingPrimaryPhone}</span></div>
          {phone2Row}
        </div>
      </div>
      <div>
        <div class=""sec"">&#x1F4B3;&nbsp; Collection &amp; Payment</div>
        <div class=""pay-card"">
          {payPanel}
        </div>
      </div>
    </div>
    <div class=""sec"">&#x1F4CB;&nbsp; Fulfillment Packing Checklist</div>
    <div class=""tbl-wrap"">
      <table>
        <thead>
          <tr>
            <th class=""center"" style=""width:44px"">&#x2714;</th>
            <th>Product</th>
            <th style=""width:120px"">Unit Price</th>
            <th class=""center"" style=""width:64px"">Qty</th>
            <th style=""width:130px"">Subtotal</th>
          </tr>
        </thead>
        <tbody>
          {itemRows}
        </tbody>
      </table>
    </div>
    <div class=""totals"">
      <div class=""totals-inner"">
        <div class=""t-row""><span>Items Subtotal</span><span class=""mono"">EGP {itemsSubtotal:N2}</span></div>
        <div class=""t-row""><span>Shipping Cost</span><span class=""mono"">EGP {order.ShippingCost:N2}</span></div>
        <div class=""t-row grand""><span>Grand Total</span><span class=""val mono"">EGP {order.TotalPrice:N2}</span></div>
      </div>
    </div>
  </div>
  <div class=""inv-ft"">
    <div class=""ft-brand"">Picks &amp; More</div>
    <div class=""ft-txt"">Thank you for your order.<br/>For support please contact our logistics team.</div>
    <div class=""ft-ord"">{order.OrderNumber}<br/>{order.CreatedAt:yyyy-MM-dd}</div>
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

    [HttpGet("payment-settings")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<PaymentSettingsDto>> GetPaymentSettings()
    {
        var address = "picksandmore@instapay";
        var phone = "";
        var number = "01001234567";

        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payment-settings.json");
            if (System.IO.File.Exists(settingsPath))
            {
                var json = System.IO.File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<PaymentSettingsDto>(json);
                if (settings != null)
                {
                    address = settings.InstaPayAddress;
                    phone = settings.InstaPayPhone;
                    number = settings.VodafoneCashNumber;
                }
            }
        }
        catch
        {
            // Fallback to default values
        }

        return Ok(ApiResponse<PaymentSettingsDto>.Success(new PaymentSettingsDto
        {
            InstaPayAddress = address,
            InstaPayPhone = phone,
            VodafoneCashNumber = number
        }, "Payment settings retrieved successfully."));
    }

    [HttpPost("payment-settings")]
    [HasPermission("Shipping:Update")]
    public ActionResult<ApiResponse<PaymentSettingsDto>> UpdatePaymentSettings([FromBody] PaymentSettingsDto dto)
    {
        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "payment-settings.json");
            var json = System.Text.Json.JsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(settingsPath, json);
            return Ok(ApiResponse<PaymentSettingsDto>.Success(dto, "Payment settings updated successfully."));
        }
        catch (System.Exception ex)
        {
            return BadRequest(ApiResponse<PaymentSettingsDto>.Failure(null, $"Failed to update payment settings: {ex.Message}"));
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

public class PaymentSettingsDto
{
    public string InstaPayAddress { get; set; } = "picksandmore@instapay";
    public string InstaPayPhone { get; set; } = "";
    public string VodafoneCashNumber { get; set; } = "01001234567";
}
