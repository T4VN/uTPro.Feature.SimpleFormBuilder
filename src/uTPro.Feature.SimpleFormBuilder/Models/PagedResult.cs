namespace uTPro.Feature.SimpleFormBuilder.Models;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public long Total { get; set; }
}
