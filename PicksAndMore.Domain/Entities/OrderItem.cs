namespace PicksAndMore.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsReturnedPartially { get; set; }
    public int OriginalQuantity { get; set; }

    public OrderItem()
    {
    }

    public OrderItem(Guid id, Guid orderId, Guid productId, int quantity, decimal unitPrice, bool isReturnedPartially, int originalQuantity)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        IsReturnedPartially = isReturnedPartially;
        OriginalQuantity = originalQuantity;
    }
}
