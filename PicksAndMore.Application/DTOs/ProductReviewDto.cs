using System;

namespace PicksAndMore.Application.DTOs;

public class ProductReviewDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductReviewDto
{
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; }
}
