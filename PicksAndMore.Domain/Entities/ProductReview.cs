using System;

namespace PicksAndMore.Domain.Entities;

public class ProductReview : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string ReviewerName { get; set; } = null!;
    public string Comment { get; set; } = null!;
    public int Rating { get; set; } // 1-5

    public ProductReview()
    {
    }

    public ProductReview(Guid id, Guid productId, Guid userId, string reviewerName, string comment, int rating)
    {
        Id = id;
        ProductId = productId;
        UserId = userId;
        ReviewerName = reviewerName;
        Comment = comment;
        Rating = rating;
    }
}
