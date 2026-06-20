using PicksAndMore.Application.DTOs;
using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Mappings;

public static class MappingExtensions
{
    public static ProductDto ToDto(this Product product)
    {
        var sortedImages = (product.Images ?? new())
            .OrderBy(i => i.SortOrder)
            .Select(i => new ProductImageDto
            {
                Id = i.Id,
                Url = i.Url,
                SortOrder = i.SortOrder,
                AltText = i.AltText
            })
            .ToList();

        return new ProductDto
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            Price = product.Price,
            CostPrice = product.CostPrice,
            StockQuantity = product.StockQuantity,
            IsVisible = product.IsVisible,
            ScheduledPublishDate = product.ScheduledPublishDate,
            CategoryId = product.CategoryId,
            MainCategory = product.Category?.TargetAudience ?? string.Empty,
            SubCategory = product.Category?.Name ?? string.Empty,
            Colors = product.Metadata?.Colors ?? new(),
            Sizes = product.Metadata?.Sizes ?? new(),
            Materials = product.Metadata?.Materials ?? new(),
            SeasonTags = product.Metadata?.SeasonTags ?? new(),
            ShippingSize = product.ShippingSize.ToString(),
            ImageUrl = sortedImages.FirstOrDefault()?.Url ?? product.ImageUrl,
            ImageUrls = sortedImages,
            Age = product.Age,
            Categories = (product.Categories ?? new()).Select(c => c.ToDto()).ToList(),
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            BrandLogoUrl = product.Brand?.LogoUrl
        };
    }

    public static OrderItemDto ToDto(this OrderItem item)
    {
        return new OrderItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product?.Title ?? string.Empty,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            IsReturnedPartially = item.IsReturnedPartially,
            OriginalQuantity = item.OriginalQuantity
        };
    }

    public static OrderDto ToDto(this Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = $"ORD-{order.Id.ToString().Substring(0, 8).ToUpper()}",
            UserId = order.UserId,
            CustomerName = order.User?.FullName ?? "Guest",
            ShippingGovernorate = order.ShippingAddress.Governorate,
            ShippingDetailedAddress = order.ShippingAddress.DetailedAddress,
            ShippingPrimaryPhone = order.ShippingAddress.PrimaryPhone,
            ShippingSecondaryPhone = order.ShippingAddress.SecondaryPhone,
            Status = order.OrderStatus.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            ShippingCost = order.ShippingCost,
            TotalPrice = order.TotalPrice,
            CreatedAt = order.OrderDate,
            Items = order.Items.Select(item => item.ToDto()).ToList(),
            WalletVerification = order.WalletVerification != null ? new DigitalWalletVerificationDto
            {
                Id = order.WalletVerification.Id,
                ScreenshotUrl = order.WalletVerification.ScreenshotUrl,
                SenderPhoneNumberOrName = order.WalletVerification.SenderPhoneNumberOrName,
                IsVerified = order.WalletVerification.IsVerified,
                VerifiedByUserId = order.WalletVerification.VerifiedByUserId
            } : null
        };
    }

    public static CategoryDto ToDto(this Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            TargetAudience = category.TargetAudience,
            IsVisible = category.IsVisible
        };
    }
}
