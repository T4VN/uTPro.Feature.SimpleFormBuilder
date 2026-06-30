// ── Property Editor UI: uTPro Form Picker — "Allowed forms" config ──
// Used in the DATA TYPE settings (not on content). Lists EVERY uTPro Form
// (regardless of each form's "Show in content picker" flag) so a data type can
// be restricted to a fixed set of forms. The stored value is an array of form
// aliases. Leaving it empty keeps the picker's default behaviour.

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

import { API, apiPost } from '../api.js';

export class UtproFormListConfigElement extends UmbLitElement {

    static properties = {
        // Persisted config value: an array of selected form aliases.
        value: { type: Array },
        _forms: { type: Array, state: true },
        _loading: { type: Boolean, state: true },
    };

    #authContext;

    constructor() {
        super();
        this.value = [];
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
            // ALL forms — intentionally NOT filtered by showInPicker.
            this._forms = (await apiPost(API + '/list', {}, this.#authContext)) || [];
        } catch {
            this._forms = [];
        }
        this._loading = false;
    }

    #selected() {
        return new Set(Array.isArray(this.value) ? this.value : []);
    }

    #toggle(alias, checked) {
        const set = this.#selected();
        if (checked) set.add(alias); else set.delete(alias);
        this.value = [...set];
        this.dispatchEvent(new Event('change', { bubbles: true, composed: false, cancelable: false }));
    }

    render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }
        if (!this._forms.length) {
            return html`<div style="color: var(--uui-color-text-alt);">No forms found. Create a form in the uTPro Form section first.</div>`;
        }
        const selected = this.#selected();
        return html`
            <div style="display:flex;flex-direction:column;gap:6px;">
                ${this._forms.map(f => {
            const isOn = selected.has(f.alias);
            return html`
                    <div style="display:flex;align-items:center;gap:8px;cursor:pointer;"
                         @click=${() => this.#toggle(f.alias, !isOn)}>
                        <uui-checkbox ?checked=${isOn} style="pointer-events:none;"></uui-checkbox>
                        <span>${f.name} <span style="opacity:.6;">(${f.alias})</span></span>
                    </div>`;
        })}
            </div>
            <small style="display:block;margin-top:8px;color:var(--uui-color-text-alt);">
                Ticked forms still only appear in Content if they have <em>Show in content picker</em> enabled. Leave all unchecked to show every pickable form.
            </small>`;
    }
}

customElements.define('utpro-form-list-config', UtproFormListConfigElement);
export default UtproFormListConfigElement;
