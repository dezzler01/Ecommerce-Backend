using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PicksAndMore.API.Authorization;
using PicksAndMore.Application.Common;
using PicksAndMore.Domain.Enums;
using PicksAndMore.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PicksAndMore.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AnalyticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    [HasPermission("Analytics:Read")]
    public async Task<ActionResult<ApiResponse<AnalyticsSummaryDto>>> GetSummary()
    {
        try
        {
            // 1. Fetch active Delivered and OutForDelivery orders with their items
            var activeOrders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderStatus == OrderStatus.Delivered || o.OrderStatus == OrderStatus.OutForDelivery)
                .ToListAsync();

            // TotalSales: Sum of order prices for active Delivered and OutForDelivery items
            decimal totalSales = activeOrders
                .SelectMany(o => o.Items)
                .Sum(item => item.UnitPrice * item.Quantity);

            // NetProfit: Total Sales minus the base wholesale CostPrice of items
            decimal totalWholesaleCost = activeOrders
                .SelectMany(o => o.Items)
                .Sum(item => (item.Product != null ? item.Product.CostPrice : 0) * item.Quantity);

            decimal netProfit = totalSales - totalWholesaleCost;

            // TotalShippingCollected: Total revenue from shipping fees
            decimal totalShippingCollected = activeOrders.Sum(o => o.ShippingCost);

            // 2. GeographicMetrics aggregation
            // Visitors grouped by Country
            var visitorCountries = await _context.VisitorLogs
                .GroupBy(v => v.Country)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Country, x => x.Count, StringComparer.OrdinalIgnoreCase);

            // Group all finalized orders by Governorate
            var finalizedOrders = await _context.Orders
                .Where(o => o.OrderStatus != OrderStatus.ReturnedRejected)
                .ToListAsync();

            var orderGovernorateGroup = finalizedOrders
                .GroupBy(o => o.ShippingAddress.Governorate)
                .Select(g => new { Governorate = g.Key, Count = g.Count() })
                .ToList();

            // Group visitor logs by Governorate
            var visitorGovernorateGroup = await _context.VisitorLogs
                .GroupBy(v => v.Governorate)
                .Select(g => new { Governorate = g.Key, Count = g.Count() })
                .ToListAsync();

            // Merge Governorate counters
            var governorateMetrics = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in visitorGovernorateGroup)
            {
                if (!string.IsNullOrWhiteSpace(item.Governorate) && !item.Governorate.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    governorateMetrics[item.Governorate] = item.Count;
                }
            }

            foreach (var item in orderGovernorateGroup)
            {
                if (!string.IsNullOrWhiteSpace(item.Governorate))
                {
                    if (governorateMetrics.ContainsKey(item.Governorate))
                    {
                        governorateMetrics[item.Governorate] += item.Count;
                    }
                    else
                    {
                        governorateMetrics[item.Governorate] = item.Count;
                    }
                }
            }

            // Add finalized orders count into the "Egypt" country metric count
            var totalOrdersCount = finalizedOrders.Count;
            if (visitorCountries.ContainsKey("Egypt"))
            {
                visitorCountries["Egypt"] += totalOrdersCount;
            }
            else
            {
                visitorCountries["Egypt"] = totalOrdersCount;
            }

            // Sort metrics descending for presentation
            var sortedCountries = visitorCountries
                .OrderByDescending(kv => kv.Value)
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            var sortedGovernorates = governorateMetrics
                .OrderByDescending(kv => kv.Value)
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            // 3. TransactionMetrics: Successful vs rejected wallet counts with failure reason summaries
            var walletVerifications = await _context.DigitalWalletVerifications.ToListAsync();

            int successfulWalletCount = walletVerifications.Count(v => v.IsVerified);
            int rejectedWalletCount = walletVerifications.Count(v => !v.IsVerified);

            var failureReasonSummary = walletVerifications
                .Where(v => !v.IsVerified && !string.IsNullOrWhiteSpace(v.RejectionReason))
                .GroupBy(v => v.RejectionReason!)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var summaryDto = new AnalyticsSummaryDto
            {
                TotalSales = totalSales,
                NetProfit = netProfit,
                TotalShippingCollected = totalShippingCollected,
                CountryMetrics = sortedCountries,
                GovernorateMetrics = sortedGovernorates,
                TransactionMetrics = new TransactionMetricsDto
                {
                    SuccessfulWalletCount = successfulWalletCount,
                    RejectedWalletCount = rejectedWalletCount,
                    FailureReasonSummary = failureReasonSummary
                }
            };

            return Ok(ApiResponse<AnalyticsSummaryDto>.Success(summaryDto, "Analytics summary calculated successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AnalyticsSummaryDto>.Failure(null, $"Failed to calculate analytics summary: {ex.Message}"));
        }
    }
}

public class AnalyticsSummaryDto
{
    public decimal TotalSales { get; set; }
    public decimal NetProfit { get; set; }
    public decimal TotalShippingCollected { get; set; }
    public Dictionary<string, int> CountryMetrics { get; set; } = new();
    public Dictionary<string, int> GovernorateMetrics { get; set; } = new();
    public TransactionMetricsDto TransactionMetrics { get; set; } = null!;
}

public class TransactionMetricsDto
{
    public int SuccessfulWalletCount { get; set; }
    public int RejectedWalletCount { get; set; }
    public Dictionary<string, int> FailureReasonSummary { get; set; } = new();
}
