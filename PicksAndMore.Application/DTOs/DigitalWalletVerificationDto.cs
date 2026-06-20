namespace PicksAndMore.Application.DTOs;

public class DigitalWalletVerificationDto
{
    public Guid Id { get; set; }
    public string ScreenshotUrl { get; set; } = string.Empty;
    public string SenderPhoneNumberOrName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public Guid? VerifiedByUserId { get; set; }
}
