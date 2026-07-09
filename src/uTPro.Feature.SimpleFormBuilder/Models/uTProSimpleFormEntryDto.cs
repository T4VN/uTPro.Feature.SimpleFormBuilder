using NPoco;

namespace uTPro.Feature.SimpleFormBuilder.Models;

[TableName("uTProSimpleFormEntry")]
[PrimaryKey("id", AutoIncrement = true)]
public class uTProSimpleFormEntryDto
{
    [Column("id")]
    public int Id { get; set; }
    public int FormId { get; set; }
    public string? DataJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedUtc { get; set; }
}
