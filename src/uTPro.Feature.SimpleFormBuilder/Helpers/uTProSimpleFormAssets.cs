using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace uTPro.Feature.SimpleFormBuilder.Helpers;

/// <summary>
/// Resolves the correct base path for uTProSimpleForm static assets (CSS, JS).
/// 
/// When consumed via NuGet (RCL), assets are served at:
///   ~/_content/uTPro.Feature.SimpleFormBuilder/uTPro/simple-form/...
/// 
/// When consumed via project reference with the copy target, assets are at:
///   ~/uTPro/simple-form/...
/// 
/// This helper checks which path exists at runtime and returns the correct one.
/// </summary>
public static class uTProSimpleFormAssets
{
    private const string PackageId = "uTPro.Feature.SimpleFormBuilder";
    private const string LocalBase = "/uTPro/simple-form";
    private const string RclBase = "/_content/" + PackageId + "/uTPro/simple-form";

    private static string? _resolvedBase;

    /// <summary>Path to simple-form.css</summary>
    public static string Css => GetBase() + "/css/simple-form.css";

    /// <summary>Path to simple-form.js</summary>
    public static string Js => GetBase() + "/js/simple-form.js";

    private static string GetBase()
    {
        // Cache after first resolution
        return _resolvedBase ??= LocalBase;
    }

    /// <summary>
    /// Call once at startup (or first request) to detect whether assets are served
    /// from the local wwwroot or from the RCL _content path.
    /// </summary>
    public static void Resolve(IWebHostEnvironment env)
    {
        if (_resolvedBase != null) return;

        // Check if local file exists (project reference / copy target scenario)
        var localPath = Path.Combine(env.WebRootPath ?? "", "uTPro", "simple-form", "css", "simple-form.css");
        if (File.Exists(localPath))
        {
            _resolvedBase = LocalBase;
            return;
        }

        // Otherwise assume RCL content path
        _resolvedBase = RclBase;
    }
}
