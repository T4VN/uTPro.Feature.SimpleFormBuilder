using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Umbraco.Cms.Core.Dictionary;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Helpers;

/// <summary>
/// Razor helper methods shared across all uTProSimpleForm field partials.
/// Eliminates boilerplate for labels, error spans, field IDs, and validation messages.
/// 
/// Usage in a field partial:
///   @using uTPro.Feature.SimpleFormBuilder.Helpers
///   @{ var h = new FieldHelper(Model, ViewData); }
///   @h.Label()
///   &lt;input type="text" id="@h.FieldId" name="@h.Name" ... /&gt;
///   @h.Error()
/// </summary>
public class FieldHelper
{
    public FormFieldViewModel Field { get; }
    public string FormId { get; }
    public string FieldId { get; }
    public string Name => Field.Name;
    public string ResolvedValidationMessage { get; }

    public FieldHelper(FormFieldViewModel field, ViewDataDictionary viewData)
    {
        Field = field;
        FormId = viewData["FormId"] as string ?? "uTProForm";
        FieldId = FormId + "-" + field.Name;
        ResolvedValidationMessage = ResolveValidationMessage(field, viewData);
    }

    /// <summary>Renders a &lt;label&gt; with optional required marker.</summary>
    public IHtmlContent Label(string? forId = null)
    {
        var id = forId ?? FieldId;
        var html = $"<label for=\"{Encode(id)}\">{Encode(Field.Label)}";
        if (Field.Required)
            html += " <span class=\"uTProForm-required\">*</span>";
        html += "</label>";
        return new HtmlString(html);
    }

    /// <summary>Renders a &lt;label&gt; without the "for" attribute (for checkbox/radio groups).</summary>
    public IHtmlContent LabelNoFor()
    {
        var html = $"<label>{Encode(Field.Label)}";
        if (Field.Required)
            html += " <span class=\"uTProForm-required\">*</span>";
        html += "</label>";
        return new HtmlString(html);
    }

    /// <summary>Renders the error &lt;span&gt; for client-side validation.</summary>
    public IHtmlContent Error()
        => new HtmlString($"<span class=\"uTProForm-error\" data-for=\"{Encode(Name)}\"></span>");

    /// <summary>Returns "required" attribute or empty.</summary>
    public IHtmlContent RequiredAttr()
        => new HtmlString(Field.Required ? "required" : "");

    /// <summary>Returns pattern attribute or empty.</summary>
    public IHtmlContent PatternAttr()
        => new HtmlString(string.IsNullOrEmpty(Field.Validation) ? "" : $"pattern=\"{Encode(Field.Validation)}\"");

    /// <summary>Returns data-msg attribute.</summary>
    public IHtmlContent DataMsgAttr()
        => new HtmlString($"data-msg=\"{Encode(ResolvedValidationMessage)}\"");

    /// <summary>Gets a raw attribute value from Field.Attributes with a fallback default.</summary>
    public string Attr(string key, string fallback = "")
        => Field.Attributes?.GetValueOrDefault(key) ?? fallback;

    /// <summary>Returns a conditional HTML attribute, or empty if value is blank.</summary>
    public IHtmlContent OptionalAttr(string attrName, string value)
        => new HtmlString(string.IsNullOrEmpty(value) ? "" : $"{attrName}=\"{Encode(value)}\"");

    private static string ResolveValidationMessage(FormFieldViewModel field, ViewDataDictionary viewData)
    {
        var msg = field.ValidationMessage;
        if (string.IsNullOrEmpty(msg)) return "";

        // Support dictionary keys wrapped in {{ }}
        if (msg.StartsWith("{{") && msg.EndsWith("}}"))
        {
            var dictKey = msg[2..^2].Trim();
            var cultureDictionary = viewData["CultureDictionary"] as ICultureDictionary;
            if (cultureDictionary != null)
            {
                var translated = cultureDictionary[dictKey];
                if (!string.IsNullOrEmpty(translated))
                    return translated;
            }
        }

        return msg;
    }

    private static string Encode(string? value)
        => System.Net.WebUtility.HtmlEncode(value ?? "");
}
