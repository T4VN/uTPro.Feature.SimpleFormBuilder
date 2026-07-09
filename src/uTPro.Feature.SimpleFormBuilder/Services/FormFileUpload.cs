using Microsoft.AspNetCore.Http;

namespace uTPro.Feature.SimpleFormBuilder.Services;

/// <summary>A file uploaded together with a form submission, mapped to its field.</summary>
public sealed record FormFileUpload(string FieldName, IFormFile File);
