using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Services;

class DIuTProSimpleFormService : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Singleton (not Scoped): the service opens its own DB scope per method via
        // IScopeProvider and has only singleton dependencies. Being a singleton lets
        // it be injected into the Form Picker value editor, which Umbraco builds from
        // the root service provider (scoped services can't be resolved there).
        builder.Services.AddSingleton<IuTProSimpleFormService, uTProSimpleFormService>();

        // Form-submission pipeline: options + the built-in rate-limit gatekeeper.
        // Additional handlers (e.g. the Turnstile addon) register their own IFormSubmissionHandler.
        builder.Services.Configure<FormSubmissionOptions>(
            builder.Config.GetSection(FormSubmissionOptions.SectionPath));
        builder.Services.AddSingleton<IFormSubmissionHandler, RateLimitSubmissionHandler>();
    }
}
