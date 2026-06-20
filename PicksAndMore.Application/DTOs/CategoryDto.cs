namespace PicksAndMore.Application.DTOs;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
}
