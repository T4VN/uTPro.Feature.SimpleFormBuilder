namespace uTPro.Feature.SimpleFormBuilder.Models;

public class EntryListRequest
{
    public int FormId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
