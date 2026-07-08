using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
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

/// <summary>Provider backed by a fixed set of field types.</summary>
internal sealed class StaticFieldTypeProvider(IReadOnlyList<SimpleFormFieldType> types)
    : IuTProFormFieldTypeProvider
{
    public IEnumerable<SimpleFormFieldType> GetFieldTypes() => types;
}

/// <summary>
/// Registration helpers for consuming sites.
/// </summary>
public static class uTProSimpleFormBuilderExtensions
{
    /// <summary>
    /// Registers a single custom field type so it appears in the backoffice
    /// picker. Pair it with a Razor partial in your own site at
    /// <c>Views/Partials/uTProSimpleForm/Fields/{type}.cshtml</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyComposer : IComposer
    /// {
    ///     public void Compose(IUmbracoBuilder builder)
    ///         => builder.AdduTProSimpleFormFieldType("star-rating", "Star Rating");
    /// }
    /// </code>
    /// </example>
    public static IUmbracoBuilder AdduTProSimpleFormFieldType(
        this IUmbracoBuilder builder, string type, string label)
        => builder.AdduTProSimpleFormFieldTypes(new SimpleFormFieldType(type, label));

    /// <summary>
    /// Registers a single custom field type together with the custom settings the builder
    /// should render for it (labelled inputs saved into the field's <c>Attributes</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// builder.AdduTProSimpleFormFieldType("turnstile", "Cloudflare Turnstile",
    ///     new SimpleFormFieldAttribute("siteKey", "Site Key"),
    ///     new SimpleFormFieldAttribute("secretKey", "Secret Key"));
    /// </code>
    /// </example>
    public static IUmbracoBuilder AdduTProSimpleFormFieldType(
        this IUmbracoBuilder builder, string type, string label,
        params SimpleFormFieldAttribute[] attributes)
        => builder.AdduTProSimpleFormFieldTypes(new SimpleFormFieldType(type, label, attributes));

    /// <summary>Registers several custom field types at once.</summary>
    public static IUmbracoBuilder AdduTProSimpleFormFieldTypes(
        this IUmbracoBuilder builder, params SimpleFormFieldType[] types)
    {
        builder.Services.AddSingleton<IuTProFormFieldTypeProvider>(
            new StaticFieldTypeProvider(types));
        return builder;
    }
}
