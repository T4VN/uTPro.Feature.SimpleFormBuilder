using Microsoft.AspNetCore.Hosting;

namespace uTPro.Feature.SimpleFormBuilder.Helpers;

/// <summary>
/// Resolves the base path for uTProSimpleForm front-end static assets (CSS, JS).
///
/// The package csproj sets <c>&lt;StaticWebAssetBasePath&gt;/&lt;/StaticWebAssetBasePath&gt;</c>,
/// which makes the package's wwwroot assets serve from the site ROOT in BOTH
/// scenarios:
///   - project reference  → ~/uTPro/simple-form/...
///   - NuGet package (RCL) → ~/uTPro/simple-form/...   (NOT ~/_content/{id}/...)
///
/// So the path is always <c>/uTPro/simple-form/...</c>. The previous runtime
/// <c>File.Exists</c> detection was unreliable because RCL static web assets are
/// served virtually and never exist physically in the consuming app's wwwroot,
/// which made it fall back to the wrong <c>/_content/...</c> path (404).
/// </summary>
public static class uTProSimpleFormAssets
{
    private const string Base = "/uTPro/simple-form";

    /// <summary>Path to simple-form.css</summary>
    public static string Css => Base + "/css/simple-form.css";

    /// <summary>Path to simple-form.js</summary>
    public static string Js => Base + "/js/simple-form.js";

    /// <summary>
    /// Kept for backward compatibility with existing views; no longer needed since
    /// the asset base path is fixed at the site root. Intentionally a no-op.
    /// </summary>
    public static void Resolve(IWebHostEnvironment env) { }
}
