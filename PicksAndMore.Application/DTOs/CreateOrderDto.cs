namespace PicksAndMore.Application.DTOs;

public class CreateOrderDto
{
    public required string CustomerName { get; set; }
    public required string ShippingGovernorate { get; set; }
    public required string ShippingDetailedAddress { get; set; }
    public required string ShippingPrimaryPhone { get; set; }
    public string? ShippingSecondaryPhone { get; set; }
    public required string PaymentMethod { get; set; } // e.g. "COD", "DigitalWallet"
    public string? WalletScreenshotUrl { get; set; }
    public string? WalletSenderPhoneNumberOrName { get; set; }
    public string? PromoCode { get; set; } // Optional promo code
    public required List<CreateOrderItemDto> Items { get; set; }
}

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
