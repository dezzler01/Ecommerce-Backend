namespace PicksAndMore.Domain.Entities;

public abstract class BaseAuditableEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
