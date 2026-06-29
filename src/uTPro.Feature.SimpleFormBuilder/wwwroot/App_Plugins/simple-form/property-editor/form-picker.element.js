// ── Property Editor UI: uTPro Form Picker ──
// A dropdown of forms loaded live from the uTPro Form backoffice list.
// Stores the selected form's ALIAS (string) so templates can render it by alias,
// e.g. as an alternative to hard-coding the form in markup.

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { UmbPropertyValueChangeEvent } from '@umbraco-cms/backoffice/property-editor';

import { API, apiPost } from '../api.js';

export class UtproFormPickerElement extends UmbLitElement {

    static properties = {
        // The persisted value (form alias). Umbraco reads/writes this.
        value: { type: String },
        // Data type configuration passed by Umbraco (unused for now).
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
            const forms = await apiPost(API + '/list', {}, this.#authContext);
            // Only forms flagged to appear in the content picker (default true).
            this._forms = (forms || [])
                .filter(f => f.showInPicker !== false)
                .map(f => ({ name: f.name, alias: f.alias }));
        } catch {
            this._forms = [];
        }
        this._loading = false;
    }

    #onChange(e) {
        this.value = e.target.value || '';
        // Notify Umbraco that the property value changed so it can be saved.
        this.dispatchEvent(new UmbPropertyValueChangeEvent());
    }

    render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }
        if (!this._forms.length) {
            return html`<div style="color: var(--uui-color-text-alt);">No forms found. Create a form in the uTPro Form section first.</div>`;
        }
        const options = [
            { name: '(none)', value: '', selected: !this.value },
            ...this._forms.map(f => ({
                name: `${f.name} (${f.alias})`,
                value: f.alias,
                selected: f.alias === this.value,
            })),
        ];
        return html`
            <uui-select
                label="Select a form"
                .options=${options}
                @change=${this.#onChange}>
            </uui-select>`;
    }
}

customElements.define('utpro-form-picker', UtproFormPickerElement);
export default UtproFormPickerElement;
