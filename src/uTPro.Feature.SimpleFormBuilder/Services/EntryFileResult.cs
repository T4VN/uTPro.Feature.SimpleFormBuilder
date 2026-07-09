namespace uTPro.Feature.SimpleFormBuilder.Services;

/// <summary>Physical file resolved from an entry's file-field value.</summary>
public sealed record EntryFileResult(Stream Stream, string FileName, string ContentType);
