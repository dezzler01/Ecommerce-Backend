namespace PicksAndMore.Application.DTOs;

/// <summary>
/// Tracking-only view of an order returned to anonymous callers.
/// PII is masked — no full address, no full phone, no internal IDs.
/// </summary>
public class OrderTrackingDto
{
    public string OrderNumber   { get; set; } = string.Empty;
    public string OrderStatus   { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public decimal TotalPrice   { get; set; }
    public DateTime OrderDate   { get; set; }

    // Masked PII
    public string MaskedName  { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public string City        { get; set; } = string.Empty;

    public List<OrderTrackingItemDto> Items { get; set; } = new();
}

public class OrderTrackingItemDto
{
    public string  ProductTitle { get; set; } = string.Empty;
    public int     Quantity     { get; set; }
    public decimal UnitPrice    { get; set; }
}
