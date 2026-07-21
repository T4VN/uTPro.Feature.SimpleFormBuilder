using Microsoft.Extensions.DependencyInjection; // SwaggerDoc(...) extension lives here
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
#if NET10_0_OR_GREATER
// Umbraco 17/18: Swashbuckle 10.x + Microsoft.OpenApi 2.x (namespace flattened to Microsoft.OpenApi).
using Microsoft.OpenApi;
#else
// Umbraco 16: Swashbuckle 8.x + Microsoft.OpenApi 1.x (types live under Microsoft.OpenApi.Models).
using Microsoft.OpenApi.Models;
#endif

namespace uTPro.Feature.SimpleFormBuilder;

/// <summary>
/// Registers a dedicated Swagger document for the Simple Form Builder API so it appears as its own
/// entry in the backoffice Swagger UI "Select a definition" dropdown. Paired with
/// <c>[MapToApi(ApiName)]</c> on the controller and registered from the composer.
/// </summary>
public class ConfigureSimpleFormSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    /// <summary>The Swagger document name. Must match the value passed to <c>[MapToApi]</c>.</summary>
    public const string ApiName = "utpro-simple-form";

    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(ApiName, new OpenApiInfo
        {
            Title = "uTPro Simple Form API",
            Version = "1.0",
            Description = "Form management, entries and field-type endpoints for the uTPro backoffice.",
        });
    }
}
