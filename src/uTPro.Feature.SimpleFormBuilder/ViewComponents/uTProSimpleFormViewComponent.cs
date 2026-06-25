using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.SimpleFormBuilder.ViewComponents;

public class uTProSimpleFormViewComponent(
    IuTProSimpleFormService formService,
    ICompositeViewEngine viewEngine) : ViewComponent
{
    public IViewComponentResult Invoke(
        string alias,
        string? template = null,
        string? cssClass = null,
        string? submitBtnText = null,
        bool? showReset = null,
        string? resetBtnText = null)
    {
        var form = formService.GetFormByAlias(alias);
        if (form == null || !form.IsEnabled)
            return Content($"<!-- uTProSimpleForm '{alias}' not found or disabled -->");

        ViewBag.FormCssClass = cssClass ?? "";
        ViewBag.SubmitBtnText = submitBtnText ?? "Submit";
        ViewBag.ShowReset = showReset;
        ViewBag.ResetBtnText = resetBtnText;

        var viewPath = ResolveTemplate(template, alias);
        return View(viewPath, form);
    }

    /// <summary>
    /// Resolves the template path. Checks both local files and RCL-compiled views
    /// so it works whether uTProSimpleForm is a project reference or a NuGet package.
    /// </summary>
    private string ResolveTemplate(string? template, string alias)
    {
        if (!string.IsNullOrEmpty(template))
        {
            var path = $"~/Views/Partials/uTProSimpleForm/{template}.cshtml";
            if (ViewExists(path)) return path;
        }

        var aliasPath = $"~/Views/Partials/uTProSimpleForm/{alias}.cshtml";
        if (ViewExists(aliasPath)) return aliasPath;

        return "~/Views/Partials/uTProSimpleForm/Default.cshtml";
    }

    private bool ViewExists(string viewPath)
        => viewEngine.GetView(null, viewPath, false).Success;
}
