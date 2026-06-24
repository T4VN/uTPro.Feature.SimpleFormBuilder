import { html, nothing } from '@umbraco-cms/backoffice/external/lit';

/**
 * Renders the form list view.
 * - Admin: can create, edit, delete forms
 * - Non-admin: can only view list and access entries
 * @param {object} host - the dashboard element
 */
export function renderList(host) {
    const isAdmin = host._permissions?.isAdmin;

    return html`
        <uui-box>
            <div class="toolbar">
                <h2>Form Builder</h2>
                ${isAdmin ? html`
                    <uui-button look="primary" @click=${() => host._newForm()}>+ New Form</uui-button>
                ` : nothing}
            </div>
            ${host._loading ? html`<div class="loading"><uui-loader></uui-loader></div>` : nothing}
            ${!host._forms.length && !host._loading ? html`<div class="empty">No forms yet.${isAdmin ? ' Create one!' : ''}</div>` : nothing}
            ${host._forms.length ? html`
            <uui-table aria-label="Forms">
                <uui-table-head>
                    <uui-table-head-cell>Name</uui-table-head-cell>
                    <uui-table-head-cell>Alias</uui-table-head-cell>
                    <uui-table-head-cell>Fields</uui-table-head-cell>
                    <uui-table-head-cell>Status</uui-table-head-cell>
                    <uui-table-head-cell style="width:260px">Actions</uui-table-head-cell>
                </uui-table-head>
                ${host._forms.map(f => html`
                    <uui-table-row>
                        <uui-table-cell>
                            ${isAdmin
                                ? html`<a class="link" @click=${() => host._editExisting(f.id)}>${f.name}</a>`
                                : html`<span>${f.name}</span>`}
                        </uui-table-cell>
                        <uui-table-cell><code>${f.alias}</code></uui-table-cell>
                        <uui-table-cell>${f.fields?.length || 0}</uui-table-cell>
                        <uui-table-cell>${f.isEnabled ? html`<span class="badge on">Active</span>` : html`<span class="badge off">Disabled</span>`}</uui-table-cell>
                        <uui-table-cell class="action-cell">
                            ${isAdmin ? html`
                                <uui-button look="outline" compact @click=${() => host._editExisting(f.id)}>Edit</uui-button>
                            ` : nothing}
                            <uui-button look="outline" compact @click=${() => host._viewEntries(f.id)}>Entries</uui-button>
                            ${isAdmin ? html`
                                <uui-button look="outline" color="danger" compact @click=${() => host._deleteForm(f.id)}>Delete</uui-button>
                            ` : nothing}
                        </uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>` : nothing}
        </uui-box>`;
}
