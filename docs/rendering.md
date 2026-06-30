# Rendering a Form

[← Back to README](../README.md)

Drop this into any Razor view or block component:

```razor
@await Component.InvokeAsync("uTProSimpleForm", new { alias = "contact-us" })
```

The `alias` matches the form you created in the **uTPro Form** section.

## Optional parameters

```razor
@await Component.InvokeAsync("uTProSimpleForm", new {
    alias = "contact-us",
    template = "MyLayout",       // use a custom Razor template
    cssClass = "my-form",        // add a CSS class to the <form> tag
    submitBtnText = "Send",      // change the submit button text
    showReset = true,            // show or hide the reset button
    resetBtnText = "Clear"       // change the reset button text
})
```

## Template resolution order

1. `Views/Partials/uTProSimpleForm/{template}.cshtml` — if a `template` parameter was passed
2. `Views/Partials/uTProSimpleForm/{alias}.cshtml` — a view named after the form alias
3. `Views/Partials/uTProSimpleForm/Default.cshtml` — the built-in default

To customize the layout for a specific form, create a file matching its alias. No config changes needed.

## Overriding Views (NuGet users)

When installed via NuGet, all Razor views are compiled into the package DLL. To customize any view, create a file at the **same path** in your web project:

```
YourWebProject/
  Views/Partials/uTProSimpleForm/
    Default.cshtml                  ← overrides the form layout
    Fields/
      textarea.cshtml               ← overrides just the textarea field
      star-rating.cshtml            ← adds a brand new field type
```

ASP.NET Core picks up local files over the ones in the package. No configuration needed.

## JavaScript Hooks

Two front-end hooks for custom client-side logic:

```javascript
// Runs before submission. Return false to cancel, or an object to merge extra data.
window.__sfBeforeSubmit = async function (alias, data, formElement) {
    if (alias === 'contact-us') {
        data.source = 'homepage';
    }
    return data;
};

// Runs after a successful submission.
window.__sfAfterSubmit = function (alias, success, result) {
    console.log('Submitted:', alias, result.message);
};
```
