namespace PicksAndMore.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    
    // Flattened Shipping Address fields
    public string ShippingGovernorate { get; set; } = string.Empty;
    public string ShippingDetailedAddress { get; set; } = string.Empty;
    public string ShippingPrimaryPhone { get; set; } = string.Empty;
    public string? ShippingSecondaryPhone { get; set; }
    
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DigitalWalletVerificationDto? WalletVerification { get; set; }
}
