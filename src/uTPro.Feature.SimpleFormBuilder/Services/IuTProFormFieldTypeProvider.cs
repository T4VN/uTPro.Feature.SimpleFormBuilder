using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

/// <summary>
/// Supplies additional field types to the form builder's type picker.
///
/// Implement and register this from a consuming site to add custom field types
/// WITHOUT modifying the package. For the common case, prefer the
/// <see cref="uTProSimpleFormBuilderExtensions.AdduTProSimpleFormFieldType"/>
/// helper instead of implementing this interface directly.
/// </summary>
public interface IuTProFormFieldTypeProvider
{
    IEnumerable<SimpleFormFieldType> GetFieldTypes();
}
