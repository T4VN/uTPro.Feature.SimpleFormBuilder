using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Umbraco.Cms.Core.Dictionary;
using uTPro.Feature.SimpleFormBuilder.Models;

namespace uTPro.Feature.SimpleFormBuilder.Helpers;

/// <summary>
/// Razor helper methods shared across all uTProSimpleForm field partials.
/// Eliminates boilerplate for labels, error spans, field IDs, and validation messages.
///
/// MULTI-LANGUAGE: any text can contain one or more <c>{{ DictionaryKey }}</c> tokens.
/// At render time each token is replaced with the Umbraco dictionary value for the current
/// culture. If a key has no translation the original token text is kept (safe fallback).
/// This applies to labels, placeholders, option text, validation messages, group titles,
/// button text and any other user-facing string routed through <see cref="Localize"/>.
///
/// Usage in a field partial:
///   @using uTPro.Feature.SimpleFormBuilder.Helpers
///   @{ var h = new FieldHelper(Model, ViewData); }
///   @h.Label()
///   &lt;input type="text" id="@h.FieldId" name="@h.Name" placeholder="@h.Placeholder" ... /&gt;
///   @h.Error()
/// </summary>
public class FieldHelper
{
    // Matches {{ key }} — allows surrounding whitespace, disallows nested braces.
    private static readonly Regex TokenRegex =
        new(@"\{\{\s*([^{}]+?)\s*\}\}", RegexOptions.Compiled);

    public FormFieldViewModel Field { get; }
    public string FormId { get; }
    public string FieldId { get; }
    public string Name => Field.Name;
    public string ResolvedValidationMessage { get; }

    private readonly ICultureDictionary? _cultureDictionary;

    public FieldHelper(FormFieldViewModel field, ViewDataDictionary viewData)
    {
        Field = field;
        FormId = viewData["FormId"] as string ?? "uTProForm";
        FieldId = FormId + "-" + field.Name;
        _cultureDictionary = viewData["CultureDictionary"] as ICultureDictionary;
        ResolvedValidationMessage = Localize(field.ValidationMessage, _cultureDictionary);
    }

    /// <summary>
    /// Replaces every <c>{{ key }}</c> token in <paramref name="text"/> with its Umbraco
    /// dictionary translation for the current culture. Non-token text is returned unchanged,
    /// and unknown keys fall back to the original token so nothing silently disappears.
    /// </summary>
    public static string Localize(string? text, ICultureDictionary? dictionary)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        if (dictionary == null || text.IndexOf("{{", StringComparison.Ordinal) < 0)
            return text;

        return TokenRegex.Replace(text, match =>
        {
            var key = match.Groups[1].Value.Trim();
            var translated = dictionary[key];
            return string.IsNullOrEmpty(translated) ? match.Value : translated;
        });
    }

    /// <summary>Localizes a string using the current field's culture dictionary.</summary>
    public string L(string? text) => Localize(text, _cultureDictionary);

    /// <summary>Localized placeholder text (empty when not set).</summary>
    public string Placeholder => L(Field.Placeholder);

    /// <summary>Renders a &lt;label&gt; with optional required marker.</summary>
    public IHtmlContent Label(string? forId = null)
    {
        var id = forId ?? FieldId;
        var html = $"<label for=\"{Encode(id)}\">{Encode(L(Field.Label))}";
        if (Field.Required)
            html += " <span class=\"uTProForm-required\">*</span>";
        html += "</label>";
        return new HtmlString(html);
    }

    /// <summary>Renders a &lt;label&gt; without the "for" attribute (for checkbox/radio groups).</summary>
    public IHtmlContent LabelNoFor()
    {
        var html = $"<label>{Encode(L(Field.Label))}";
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

    /// <summary>Gets an attribute value and localizes any {{ key }} tokens in it.</summary>
    public string AttrLocalized(string key, string fallback = "")
        => L(Attr(key, fallback));

    /// <summary>Returns a conditional HTML attribute, or empty if value is blank.</summary>
    public IHtmlContent OptionalAttr(string attrName, string value)
        => new HtmlString(string.IsNullOrEmpty(value) ? "" : $"{attrName}=\"{Encode(value)}\"");

    private static string Encode(string? value)
        => System.Net.WebUtility.HtmlEncode(value ?? "");
}
