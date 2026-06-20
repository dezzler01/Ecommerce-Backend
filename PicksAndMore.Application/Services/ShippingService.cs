using PicksAndMore.Application.DTOs;
using PicksAndMore.Application.Interfaces;
using PicksAndMore.Domain.Enums;
using System.IO;
using System.Text.Json;

namespace PicksAndMore.Application.Services;

public class ShippingService : IShippingService
{
    private readonly IShippingMatrixRepository _shippingMatrixRepository;
    private readonly IShippingComboRuleRepository _shippingComboRuleRepository;
    private readonly IProductShippingOverrideRepository _overrideRepository;
    private readonly IProductRepository _productRepository;

    public ShippingService(
        IShippingMatrixRepository shippingMatrixRepository,
        IShippingComboRuleRepository shippingComboRuleRepository,
        IProductShippingOverrideRepository overrideRepository,
        IProductRepository productRepository)
    {
        _shippingMatrixRepository = shippingMatrixRepository;
        _shippingComboRuleRepository = shippingComboRuleRepository;
        _overrideRepository = overrideRepository;
        _productRepository = productRepository;
    }

    public async Task<decimal> CalculateShippingCostAsync(Guid userId, List<OrderItemDto> items, string governorate, bool isPromoFreeShipping = false)
    {
        if (items == null || !items.Any())
        {
            return 0;
        }

        // If promo code signals free shipping, zero out all delivery charges
        if (isPromoFreeShipping)
        {
            return 0;
        }

        // Dynamic free shipping based on admin configuration settings
        decimal threshold = 2000m;
        bool isActive = true;

        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shipping-settings.json");
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<ShippingSettingsDto>(json);
                if (settings != null)
                {
                    threshold = settings.FreeShippingThreshold;
                    isActive = settings.IsFreeShippingActive;
                }
            }
        }
        catch
        {
            // Fallback to hardcoded defaults on read/parse failures
        }

        if (isActive)
        {
            decimal itemsTotal = items.Sum(i => i.Quantity * i.UnitPrice);
            if (itemsTotal >= threshold)
            {
                return 0;
            }
        }

        // 1. Fetch base prices for the governorate (fallback to default rates if not registered)
        decimal basePriceSmall = 30.00m;
        decimal basePriceMedium = 50.00m;
        decimal basePriceLarge = 80.00m;

        var matrix = await _shippingMatrixRepository.GetByGovernorateAsync(governorate);
        if (matrix != null)
        {
            basePriceSmall = matrix.BasePriceSmall;
            basePriceMedium = matrix.BasePriceMedium;
            basePriceLarge = matrix.BasePriceLarge;
        }

        decimal totalCost = 0;
        int countSmall = 0;
        int countMedium = 0;
        int countLarge = 0;

        // 2. Evaluate Product Overrides
        foreach (var item in items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
            {
                continue;
            }

            var overrideRule = await _overrideRepository.GetByProductIdAsync(item.ProductId);
            if (overrideRule != null)
            {
                if (overrideRule.IsFreeShipping)
                {
                    // Skip volume and price calculations for this product (Free Shipping)
                    continue;
                }
                if (overrideRule.FixedPrice.HasValue)
                {
                    // Add fixed shipping price directly and isolate it
                    totalCost += overrideRule.FixedPrice.Value * item.Quantity;
                    continue;
                }
            }

            // No overrides: add to size combination bins based on quantity
            if (product.ShippingSize == ShippingSize.Small)
            {
                countSmall += item.Quantity;
            }
            else if (product.ShippingSize == ShippingSize.Medium)
            {
                countMedium += item.Quantity;
            }
            else if (product.ShippingSize == ShippingSize.Large)
            {
                countLarge += item.Quantity;
            }
        }

        // 3. Evaluate Combination Rules dynamically from database
        var rules = await _shippingComboRuleRepository.GetAllAsync();

        // Rule 1: 1 Small + 1 Medium -> 1 Large
        var rule1 = rules.FirstOrDefault(r => r.InputSmallCount == 1 && r.InputMediumCount == 1 && r.ResultingSize == ShippingSize.Large) 
                    ?? new Domain.Entities.ShippingComboRule(Guid.NewGuid(), 1, 1, ShippingSize.Large);

        int combo1 = Math.Min(countSmall, countMedium);
        countSmall -= combo1 * rule1.InputSmallCount;
        countMedium -= combo1 * rule1.InputMediumCount;
        countLarge += combo1;

        // Rule 2: 4 Small -> 1 Large
        var rule2 = rules.FirstOrDefault(r => r.InputSmallCount == 4 && r.InputMediumCount == 0 && r.ResultingSize == ShippingSize.Large)
                    ?? new Domain.Entities.ShippingComboRule(Guid.NewGuid(), 4, 0, ShippingSize.Large);

        int combo2 = countSmall / rule2.InputSmallCount;
        countLarge += combo2;
        countSmall %= rule2.InputSmallCount;

        // Rule 3: 3 Small -> 1 Medium
        var rule3 = rules.FirstOrDefault(r => r.InputSmallCount == 3 && r.InputMediumCount == 0 && r.ResultingSize == ShippingSize.Medium)
                    ?? new Domain.Entities.ShippingComboRule(Guid.NewGuid(), 3, 0, ShippingSize.Medium);

        int combo3 = countSmall / rule3.InputSmallCount;
        countMedium += combo3;
        countSmall %= rule3.InputSmallCount;

        // Calculate final cost
        totalCost += (countSmall * basePriceSmall) + (countMedium * basePriceMedium) + (countLarge * basePriceLarge);

        return totalCost;
    }
}

public class ShippingSettingsDto
{
    public decimal FreeShippingThreshold { get; set; } = 2000m;
    public bool IsFreeShippingActive { get; set; } = true;
}
