using PicksAndMore.Domain.Enums;
using PicksAndMore.Domain.ValueObjects;

namespace PicksAndMore.Domain.Entities;

public class Order : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal ShippingCost { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    
    public Address ShippingAddress { get; set; } = null!;

    private readonly List<OrderItem> _items = new();
    public virtual IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public DigitalWalletVerification? WalletVerification { get; set; }

    public Order()
    {
        OrderDate = DateTime.UtcNow;
    }

    public Order(
        Guid id,
        Guid userId,
        DateTime orderDate,
        decimal totalPrice,
        decimal shippingCost,
        OrderStatus orderStatus,
        PaymentMethod paymentMethod,
        Address shippingAddress)
    {
        Id = id;
        UserId = userId;
        OrderDate = orderDate;
        TotalPrice = totalPrice;
        ShippingCost = shippingCost;
        OrderStatus = orderStatus;
        PaymentMethod = paymentMethod;
        ShippingAddress = shippingAddress;
    }

    public void AddOrderItem(Guid productId, int quantity, decimal unitPrice)
    {
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            existingItem.OriginalQuantity = existingItem.Quantity;
        }
        else
        {
            var newItem = new OrderItem(
                Guid.NewGuid(),
                Id,
                productId,
                quantity,
                unitPrice,
                isReturnedPartially: false,
                originalQuantity: quantity
            );
            _items.Add(newItem);
        }

        RecalculateTotalPrice();
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        OrderStatus = newStatus;
    }

    public void RecalculateTotalPrice()
    {
        TotalPrice = _items.Sum(item => item.UnitPrice * item.Quantity) + ShippingCost;
    }
}
