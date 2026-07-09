using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

/// <summary>Provider backed by a fixed set of field types.</summary>
internal sealed class StaticFieldTypeProvider(IReadOnlyList<SimpleFormFieldType> types)
    : IuTProFormFieldTypeProvider
{
    public IEnumerable<SimpleFormFieldType> GetFieldTypes() => types;
}
