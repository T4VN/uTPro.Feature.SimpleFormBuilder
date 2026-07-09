namespace uTPro.Feature.SimpleFormBuilder.Models;

public class SubmitFormRequest
{
    public string Alias { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = [];
}
