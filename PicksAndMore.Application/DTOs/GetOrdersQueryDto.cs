using PicksAndMore.Domain.Enums;

namespace PicksAndMore.Application.DTOs;

public class GetOrdersQueryDto
{
    public OrderStatus? Status { get; set; }
    public string? Governorate { get; set; }
    public string? SearchText { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
