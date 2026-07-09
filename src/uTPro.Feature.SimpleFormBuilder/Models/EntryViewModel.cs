namespace uTPro.Feature.SimpleFormBuilder.Models;

public class EntryViewModel
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public Dictionary<string, string> Data { get; set; } = [];
    public string? IpAddress { get; set; }
    public DateTime CreatedUtc { get; set; }
}
