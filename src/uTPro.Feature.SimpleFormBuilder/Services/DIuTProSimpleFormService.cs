using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Feature.SimpleFormBuilder.Services;

class DIuTProSimpleFormService : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        // Singleton (not Scoped): the service opens its own DB scope per method via
        // IScopeProvider and has only singleton dependencies. Being a singleton lets
        // it be injected into the Form Picker value editor, which Umbraco builds from
        // the root service provider (scoped services can't be resolved there).
        => builder.Services.AddSingleton<IuTProSimpleFormService, uTProSimpleFormService>();
}
