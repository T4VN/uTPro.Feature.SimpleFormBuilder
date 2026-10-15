> **Rendered documentation:** [docs.utpro.dev](https://docs.utpro.dev/uTPro.Feature.SimpleFormBuilder/) is the canonical, always-current home of this page.

# Conditional Field Visibility

*Available since v5.0.0*

Fields can show or hide themselves at runtime based on the value of other fields in the same form. This is useful for branching forms â€” for example, showing different help text or follow-up inputs depending on a Dropdown selection.

---

## Setting up conditions (Backoffice)

1. Open the form in the builder, then open the **Field Settings** dialog for the field you want to conditionally show/hide.
2. Scroll to the **Conditions** section at the bottom of the dialog.
3. Toggle **On** to enable conditions.
4. Choose the **action**:
   - **Show** â€” the field is hidden by default and becomes visible when the conditions match.
   - **Hide** â€” the field is visible by default and becomes hidden when the conditions match.
5. Choose the **match logic**:
   - **all** â€” every rule must match for the action to trigger.
   - **any** â€” at least one rule matching is enough.
6. Add one or more **rules**. Each rule consists of:
   - **Field** â€” the name of another field in the form whose value is evaluated.
   - **Operator** â€” how to compare the field's value to the target.
   - **Value** â€” the target value (or omitted for `is empty` / `is not empty`).

### Operators

| Operator | Description |
|---|---|
| `is` | Exact match (case-sensitive) |
| `is not` | Does not exactly match |
| `contains` | Field value contains the target substring |
| `does not contain` | Field value does not contain the target |
| `starts with` | Field value starts with the target |
| `ends with` | Field value ends with the target |
| `is greater than` | Numeric comparison: field > target |
| `is less than` | Numeric comparison: field < target |
| `is empty` | Field has no value (no target needed) |
| `is not empty` | Field has any value (no target needed) |

### Smart value picker

When the referenced field has predefined options (Dropdown, Radio, or Checkbox field types), the **Value** column renders as a dropdown populated with those options â€” no manual typing required. For fields without options (text, number, email, etc.), a free-text input is shown instead.

---

## Runtime behaviour (Frontend)

Conditions are evaluated **client-side** in real time:

- The form emits a `data-conditions` attribute (JSON) on each conditional field wrapper.
- The bundled `simple-form.js` listens for `change` and `input` events on referenced fields and re-evaluates all conditions immediately.
- Hidden fields are set to `display: none` and marked with `data-cond-disabled`, which causes them to be:
  - **Skipped during validation** â€” a required field that is currently hidden will not block submission.
  - **Excluded from the submitted data** â€” the value is not sent to the server.

### Initial state

On page load, conditions are evaluated once so the form starts in the correct state (e.g. a field that depends on a Dropdown with a default value will already be visible or hidden appropriately).

---

## Data model

Conditions are stored as part of the field's JSON inside the existing `GroupsJson` column â€” **no database migration is required**.

```jsonc
// Inside a field object:
{
  "id": "f3",
  "type": "text",
  "name": "contractNumber",
  "label": "Contract Number",
  // ...other field properties...
  "conditions": {
    "enabled": true,
    "actionType": "show",   // "show" or "hide"
    "logicType": "all",     // "all" or "any"
    "rules": [
      {
        "field": "queryType",       // name of the field to evaluate
        "operator": "is",           // comparison operator
        "value": "contract"         // target value
      }
    ]
  }
}
```

When `conditions` is `null` or `enabled` is `false`, the field behaves normally (always visible, subject only to the static `isHidden` toggle).

---

## Example: branching by query type

A payment lookup form has a "Query Type" dropdown with three options:
- Contract Number
- National ID (CCCD/CMND)
- Card Number

Three help-text fields are configured with conditions:

| Field | Condition |
|---|---|
| Contract Helptext | **Show** if `queryType` **is** `contract` |
| NID Helptext | **Show** if `queryType` **is** `nid` |
| Card Helptext | **Show** if `queryType` **is** `card` |

When the user selects "Contract Number", only the Contract Helptext is visible. Switching the dropdown instantly shows the appropriate help text and hides the others.

---

## CSS customization

Hidden fields use inline `display: none`. If you need animated transitions, you can override the behaviour with CSS:

```css
.uTProForm-group-field[data-conditions] {
    transition: opacity 0.3s, max-height 0.3s;
    overflow: hidden;
}
```

Then in your own JS, replace `display:none` with class-based hiding for smoother animations.
