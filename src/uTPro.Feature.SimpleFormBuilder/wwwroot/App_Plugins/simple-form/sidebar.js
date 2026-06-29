// ── Section sidebar: Forms tree ──
// Left panel for the uTPro Form section (similar to the Dictionary tree).
// Shows a "Forms" header with a "+" button (admin only) and the list of forms
// loaded from the API. Selecting a form / clicking "+" is relayed to the main
// dashboard via the shared event bus.

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

import { API, apiPost } from './api.js';
import { formBus } from './bus.js';

export class UtproSimpleFormSidebar extends UmbLitElement {

    static properties = {
        _forms: { type: Array, state: true },
        _selectedId: { type: Number, state: true },
        _isAdmin: { type: Boolean, state: true },
        _loading: { type: Boolean, state: true },
    };

    #authContext;

    constructor() {
        super();
        this._forms = [];
        this._selectedId = 0;
        this._isAdmin = false;
        this._loading = false;

        // Re-load when the dashboard reports a change, and track the active form.
        this._onRefresh = () => this._load();
        this._onActive = (e) => { this._selectedId = Number(e.detail) || 0; };
        this._onNew = () => { this._selectedId = 0; };

        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => {
            this.#authContext = ctx;
            this._load();
        });
    }

    connectedCallback() {
        super.connectedCallback();
        formBus.addEventListener('refresh', this._onRefresh);
        formBus.addEventListener('active', this._onActive);
        formBus.addEventListener('new', this._onNew);
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        formBus.removeEventListener('refresh', this._onRefresh);
        formBus.removeEventListener('active', this._onActive);
        formBus.removeEventListener('new', this._onNew);
    }

    async _load() {
        if (!this.#authContext) return;
        this._loading = true;
        try {
            const perms = await apiPost(API + '/permissions', {}, this.#authContext);
            this._isAdmin = perms?.canEdit === true;
        } catch { /* ignore */ }
        try {
            this._forms = (await apiPost(API + '/list', {}, this.#authContext)) || [];
        } catch {
            this._forms = [];
        }
        this._loading = false;
    }

    _new() {
        this._selectedId = 0;
        formBus.requestNew();
    }

    _closeMenu() {
        this.renderRoot?.querySelector('#utpro-forms-menu')?.hidePopover?.();
    }

    _menuReload() {
        this._closeMenu();
        this._load();
    }

    _menuCreate() {
        this._closeMenu();
        this._new();
    }

    _menuImport() {
        this._closeMenu();
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json,application/json';
        input.addEventListener('change', async () => {
            const file = input.files?.[0];
            if (!file) return;
            try {
                const text = await file.text();
                let model;
                try { model = JSON.parse(text); }
                catch { formBus.notify('Invalid JSON file'); return; }
                const res = await apiPost(API + '/import', model, this.#authContext);
                formBus.notify(res?.message || 'Form imported');
                await this._load();
                formBus.refresh();
                if (res?.id) { this._open(res.id); }
            } catch (e) {
                formBus.notify(e.message || 'Import failed');
            }
        });
        input.click();
    }

    _open(id) {
        this._selectedId = id;
        formBus.selectForm(id);
    }

    _closeItemMenu(id) {
        this.renderRoot?.querySelector(`#utpro-fa-${id}`)?.hidePopover?.();
    }

    _itemEdit(id) { this._closeItemMenu(id); this._open(id); }
    _itemEntries(id) { this._closeItemMenu(id); this._selectedId = id; formBus.requestEntries(id); }
    _itemExport(id) { this._closeItemMenu(id); formBus.requestExport(id); }
    _itemDelete(id) { this._closeItemMenu(id); formBus.requestDelete(id); }

    render() {
        return html`
            <div id="header">
                <h3>Forms</h3>
                <div class="header-actions">
                    <uui-button
                        look="secondary"
                        compact
                        label="Options"
                        title="Options"
                        popovertarget="utpro-forms-menu">
                        <uui-symbol-more></uui-symbol-more>
                    </uui-button>
                    ${this._isAdmin ? html`
                        <uui-button
                            look="secondary"
                            compact
                            label="Add form"
                            title="Add form"
                            @click=${() => this._new()}>
                            <uui-icon name="icon-add"></uui-icon>
                        </uui-button>` : nothing}
                </div>
            </div>

            <uui-popover-container id="utpro-forms-menu" placement="bottom-end">
                <div class="menu-popover">
                    <uui-menu-item label="Reload" @click=${() => this._menuReload()}>
                        <uui-icon slot="icon" name="icon-refresh"></uui-icon>
                    </uui-menu-item>
                    ${this._isAdmin ? html`
                        <uui-menu-item label="Create" @click=${() => this._menuCreate()}>
                            <uui-icon slot="icon" name="icon-add"></uui-icon>
                        </uui-menu-item>
                        <uui-menu-item label="Import" @click=${() => this._menuImport()}>
                            <uui-icon slot="icon" name="icon-page-up"></uui-icon>
                        </uui-menu-item>` : nothing}
                </div>
            </uui-popover-container>

            <div id="list">
                ${this._loading && !this._forms.length
                    ? html`<div class="muted"><uui-loader></uui-loader></div>`
                    : nothing}
                ${this._forms.map(f => html`
                    <uui-menu-item
                        label=${f.name}
                        ?active=${f.id === this._selectedId}
                        @click=${() => this._open(f.id)}>
                        <uui-icon slot="icon" name="icon-document"></uui-icon>
                        ${this._isAdmin ? html`
                            <uui-button
                                slot="actions"
                                look="secondary"
                                compact
                                label="Actions"
                                title="Actions"
                                popovertarget="utpro-fa-${f.id}"
                                @click=${(e) => e.stopPropagation()}>
                                <uui-symbol-more></uui-symbol-more>
                            </uui-button>
                        ` : html`
                            <uui-button
                                slot="actions"
                                look="secondary"
                                compact
                                label="Entries"
                                title="Entries"
                                @click=${(e) => { e.stopPropagation(); this._itemEntries(f.id); }}>
                                <uui-icon name="icon-list"></uui-icon>
                            </uui-button>
                        `}
                    </uui-menu-item>`)}
            </div>

            ${this._isAdmin ? this._forms.map(f => html`
                <uui-popover-container id="utpro-fa-${f.id}" placement="bottom-end">
                    <div class="menu-popover">
                        <uui-menu-item label="Edit" @click=${() => this._itemEdit(f.id)}>
                            <uui-icon slot="icon" name="icon-edit"></uui-icon>
                        </uui-menu-item>
                        <uui-menu-item label="Entries" @click=${() => this._itemEntries(f.id)}>
                            <uui-icon slot="icon" name="icon-list"></uui-icon>
                        </uui-menu-item>
                        <uui-menu-item label="Export" @click=${() => this._itemExport(f.id)}>
                            <uui-icon slot="icon" name="icon-download-alt"></uui-icon>
                        </uui-menu-item>
                        <uui-menu-item label="Delete" @click=${() => this._itemDelete(f.id)}>
                            <uui-icon slot="icon" name="icon-trash"></uui-icon>
                        </uui-menu-item>
                    </div>
                </uui-popover-container>`) : nothing}`;
    }

    static styles = css`
        :host {
            display: flex;
            flex-direction: column;
            height: 100%;
            box-sizing: border-box;
        }
        #header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: var(--uui-size-space-3, 9px) var(--uui-size-space-4, 12px);
            border-bottom: 1px solid var(--uui-color-divider, #e9e9eb);
        }
        #header h3 {
            margin: 0;
            font-size: 0.95rem;
            font-weight: 700;
            color: var(--uui-color-text, #1b264f);
        }
        .header-actions {
            display: flex;
            align-items: center;
            gap: 2px;
        }
        .menu-popover {
            background: var(--uui-color-surface, #fff);
            border-radius: var(--uui-border-radius, 3px);
            box-shadow: var(--uui-shadow-depth-3, 0 8px 22px rgba(0,0,0,0.20));
            padding: var(--uui-size-space-2, 6px) 0;
            min-width: 180px;
        }
        #list {
            flex: 1;
            overflow-y: auto;
            padding: var(--uui-size-space-2, 6px) 0;
        }
        .muted {
            padding: 12px 14px;
            color: var(--uui-color-text-alt, #868690);
            font-size: 0.85rem;
            font-style: italic;
        }
    `;
}

customElements.define('utpro-simple-form-sidebar', UtproSimpleFormSidebar);
export default UtproSimpleFormSidebar;
