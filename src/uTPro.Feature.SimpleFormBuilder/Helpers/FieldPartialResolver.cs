using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;

namespace uTPro.Feature.SimpleFormBuilder.Helpers;

/// <summary>
/// Resolves the correct partial view path for a given field type.
/// Looks for ~/Views/Partials/SimpleForm/Fields/{type}.cshtml first,
/// then falls back to ~/Views/Partials/SimpleForm/Fields/_Default.cshtml.
///
/// HOW TO ADD A NEW FIELD TYPE:
/// 1. Create a new file: Views/Partials/SimpleForm/Fields/{yourType}.cshtml
/// 2. Use @model FormFieldViewModel
/// 3. Use FieldHelper for consistent label/error rendering
/// 4. Register the type in SimpleFormApiController.FieldTypes() if you want it in the backoffice picker
/// That's it — the resolver picks it up automatically.
/// </summary>
public static class FieldPartialResolver
{
    private const string FieldsBasePath = "~/Views/Partials/SimpleForm/Fields/";
    private const string FallbackPartial = FieldsBasePath + "_Default.cshtml";

    /// <summary>
    /// Returns the partial view path for the given field type.
    /// </summary>
    public static string Resolve(string fieldType, ViewContext viewContext)
    {
        var customPath = FieldsBasePath + fieldType + ".cshtml";

        var viewEngine = viewContext.HttpContext.RequestServices
            .GetRequiredService<ICompositeViewEngine>();

        var exists = viewEngine.GetView(null, customPath, false).Success
                  || viewEngine.FindView(viewContext, customPath, false).Success;

        return exists ? customPath : FallbackPartial;
    }
}
