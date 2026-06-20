namespace PicksAndMore.Application.DTOs;

public class ProductImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? AltText { get; set; }
}
