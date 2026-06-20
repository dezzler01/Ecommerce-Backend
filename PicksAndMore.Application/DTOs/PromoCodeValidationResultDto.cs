namespace PicksAndMore.Application.DTOs;

public class PromoCodeValidationResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public bool IsFreeShipping { get; set; }
    public Guid? PromoCodeId { get; set; }
}
