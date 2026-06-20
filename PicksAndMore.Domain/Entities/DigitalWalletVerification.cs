namespace PicksAndMore.Domain.Entities;

public class DigitalWalletVerification : BaseAuditableEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string ScreenshotUrl { get; set; } = null!;
    public string SenderPhoneNumberOrName { get; set; } = null!;
    public bool IsVerified { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public ApplicationUser? VerifiedByUser { get; set; }
    public string? RejectionReason { get; set; }

    public DigitalWalletVerification()
    {
    }

    public DigitalWalletVerification(Guid id, Guid orderId, string screenshotUrl, string senderPhoneNumberOrName, bool isVerified, Guid? verifiedByUserId)
    {
        Id = id;
        OrderId = orderId;
        ScreenshotUrl = screenshotUrl;
        SenderPhoneNumberOrName = senderPhoneNumberOrName;
        IsVerified = isVerified;
        VerifiedByUserId = verifiedByUserId;
    }
}
