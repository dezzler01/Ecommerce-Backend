namespace PicksAndMore.Application.DTOs;

public class UpdateOrderStatusDto
{
    public required string Status { get; set; } // e.g. "ConfirmedPreparing", "OutForDelivery", etc.
}
