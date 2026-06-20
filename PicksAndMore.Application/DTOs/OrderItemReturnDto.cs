namespace PicksAndMore.Application.DTOs;

public class OrderItemReturnDto
{
    public Guid ProductId { get; set; }
    public int ReturnedQuantity { get; set; }
}
