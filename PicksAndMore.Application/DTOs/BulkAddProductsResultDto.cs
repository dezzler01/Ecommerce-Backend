namespace PicksAndMore.Application.DTOs;

public class BulkAddProductsResultDto
{
    public int TotalProcessed { get; set; }
    public int SuccessfullyCreated { get; set; }
    public int ErrorsCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
