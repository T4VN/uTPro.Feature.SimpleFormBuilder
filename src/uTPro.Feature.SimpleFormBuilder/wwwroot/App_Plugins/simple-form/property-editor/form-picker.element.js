// ── Property Editor UI: uTPro Form Picker ──
// A dropdown of forms loaded live from the uTPro Form backoffice list.
// Stores the selected form's ALIAS (string) so templates can render it by alias,
// e.g. as an alternative to hard-coding the form in markup.
//
// NOTE: publish-time blocking (when the chosen form is no longer available) is
// enforced SERVER-SIDE by FormPickerValueValidator (PropertyEditors/FormPickerDataEditor.cs).
// We deliberately avoid a client-side custom validator here because it can trip an
// Umbraco core hint-mapping bug. This element only provides a clear visual cue +
// lets the editor clear/replace a stale value.

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

import { API, apiPost } from '../api.js';

export class UtproFormPickerElement extends UmbLitElement {

    static properties = {
        // The persisted value (form alias). Umbraco reads/writes this.
        value: { type: String },
        // Data type configuration passed by Umbraco. May contain "allowedForms".
        config: { attribute: false },
        _forms: { type: Array, state: true },
        _loading: { type: Boolean, state: true },
    };

    #authContext;

    constructor() {
        super();
        this.value = '';
        this._forms = [];
        this._loading = false;
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => {
            this.#authContext = ctx;
            this._loadForms();
        });
    }

    async _loadForms() {
        if (!this.#authContext) return;
        this._loading = true;
        try {
            // Keep the raw list; the visible subset is computed at render time so
            // it reacts to the data type's "allowedForms" config.
            this._forms = (await apiPost(API + '/list', {}, this.#authContext)) || [];
        } catch {
            this._forms = [];
        }
        this._loading = false;
    }

    // Forms chosen in the data type settings (array of aliases), if any.
    #allowedAliases() {
        const v = this.config?.getValueByAlias?.('allowedForms');
        return Array.isArray(v) ? v.filter(Boolean) : [];
    }

    // The forms shown to the editor in Content. The "Show in content picker"
    // rule ALWAYS applies; the optional "Allowed forms" config further narrows
    // (and orders) that set.
    #visibleForms() {
        const all = Array.isArray(this._forms) ? this._forms : [];
        // Base list always respects each form's Show-in-content-picker flag.
        const base = all.filter(f => f.showInPicker !== false);

        const allowed = this.#allowedAliases();
        if (allowed.length) {
            const byAlias = new Map(base.map(f => [f.alias, f]));
            return allowed
                .map(a => byAlias.get(a))      // only forms that are also pickable
                .filter(Boolean)
                .map(f => ({ name: f.name, alias: f.alias }));
        }
        return base.map(f => ({ name: f.name, alias: f.alias }));
    }

    #onChange(e) {
        let v = e.target.value || '';
        // Guard: some uui-combobox builds fall back to the option's display text
        // when its `value` is empty, which would persist the literal "(none)".
        if (v === '(none)') v = '';
        this.value = v;
        // Notify Umbraco that the property value changed so it can be saved.
        // (UmbPropertyValueChangeEvent is internally just a native 'change' event.)
        this.dispatchEvent(new Event('change', { bubbles: true, composed: false, cancelable: false }));
    }

    render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        const forms = this.#visibleForms();

        // Always make the current value selectable so the editor can SEE and
        // CLEAR it — even if its form is no longer in the visible list (e.g. its
        // "Show in content picker" was turned off, it was removed from Allowed
        // forms, or the form was deleted). Otherwise the old value gets stuck.
        const options = [...forms];
        const unavailable = this.value && !options.some(f => f.alias === this.value);
        if (unavailable) {
            const raw = (Array.isArray(this._forms) ? this._forms : [])
                .find(f => f.alias === this.value);
            options.push({ name: raw?.name || this.value, alias: this.value, unavailable: true });
        }

        // uui-combobox provides a built-in search box and filters options as you
        // type, which scales much better than a plain <select> when there are
        // many forms.
        return html`
            <uui-combobox
                .value=${this.value || ''}
                @change=${this.#onChange}>
                <uui-combobox-list>
                    <uui-combobox-list-option value="" display-value="(none)">(none)</uui-combobox-list-option>
                    ${options.map(f => html`
                        <uui-combobox-list-option
                            value=${f.alias}
                            display-value="${f.name} (${f.alias})${f.unavailable ? ' — not available' : ''}">
                            ${f.name} <span style="opacity:.6;">(${f.alias})</span>${f.unavailable
                ? html`<em style="color:var(--uui-color-danger, #d42054);"> — not available</em>`
                : nothing}
                        </uui-combobox-list-option>`)}
                </uui-combobox-list>
            </uui-combobox>
            ${!forms.length && !unavailable ? html`
                <small style="display:block;margin-top:6px;color:var(--uui-color-text-alt);">
                    No selectable forms. Choose <strong>(none)</strong> to clear, or enable a form in the uTPro Form section / this data type's Allowed forms.
                </small>` : nothing}`;
    }
}

customElements.define('utpro-form-picker', UtproFormPickerElement);
export default UtproFormPickerElement;
