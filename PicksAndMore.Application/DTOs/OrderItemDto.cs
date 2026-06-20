namespace PicksAndMore.Application.DTOs;

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool IsReturnedPartially { get; set; }
    public int OriginalQuantity { get; set; }
}
