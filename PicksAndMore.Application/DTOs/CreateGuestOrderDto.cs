namespace PicksAndMore.Application.DTOs;

public class CreateGuestOrderDto
{
    public required string CustomerName { get; set; }
    public required string PrimaryPhone { get; set; }
    public string? SecondaryPhone { get; set; }
    public required string DetailedAddress { get; set; }
    public required string Governorate { get; set; }
    public required string PaymentMethod { get; set; } // "COD" or "DigitalWallet"
    public string? WalletScreenshotUrl { get; set; }
    public string? WalletSenderPhoneNumberOrName { get; set; }
    public string? PromoCode { get; set; }
    public required List<CreateOrderItemDto> Items { get; set; }
}
