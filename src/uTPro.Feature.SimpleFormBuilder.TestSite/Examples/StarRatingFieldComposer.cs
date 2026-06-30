using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.SimpleFormBuilder.TestSite.Examples;

/// <summary>
/// EXAMPLE — how a site that installs uTPro.Feature.SimpleFormBuilder from NuGet
/// adds a brand-new field type WITHOUT touching the package source.
///
/// Two consumer-side steps, both done in THIS project:
///   1. Register the field type so it shows in the backoffice picker (this file).
///   2. Provide its Razor partial at
///      Views/Partials/uTProSimpleForm/Fields/star-rating.cshtml
///
/// The package merges this custom type with its built-in ones and the front-end
/// resolver automatically picks up the matching partial.
/// </summary>
public class StarRatingFieldComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AdduTProSimpleFormFieldType("star-rating", "Star Rating");
}
