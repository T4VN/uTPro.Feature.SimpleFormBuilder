import { html, nothing } from '@umbraco-cms/backoffice/external/lit';

/**
 * Renders the form list view (Audit-Log style: no outer box, no title,
 * a Create button + filter bar, then the table).
 * - canEdit: can create, edit, delete forms
 * - others: can only view list and access entries
 * @param {object} host - the dashboard element
 */
export function renderList(host) {
    const canEdit = host._permissions?.canEdit;
    const q = (host._listSearch || '').trim().toLowerCase();
    const forms = q
        ? host._forms.filter(f =>
            (f.name || '').toLowerCase().includes(q) ||
            (f.alias || '').toLowerCase().includes(q))
        : host._forms;

    return html`
        <div class="list-toolbar">
            ${canEdit ? html`
                <uui-button look="primary" @click=${() => host._newForm()}>Create</uui-button>
            ` : nothing}
            <uui-input
                class="list-filter"
                type="search"
                placeholder="Type to filter..."
                label="Filter forms"
                .value=${host._listSearch || ''}
                @input=${(e) => { host._listSearch = e.target.value; }}>
            </uui-input>
        </div>

        ${host._loading ? html`<div class="loading"><uui-loader></uui-loader></div>` : nothing}
        ${!forms.length && !host._loading ? html`
            <div class="empty">
                ${q ? 'No forms match your filter.' : `No forms yet.${canEdit ? ' Create one!' : ''}`}
            </div>` : nothing}
        ${forms.length ? html`
            <uui-table aria-label="Forms">
                <uui-table-head>
                    <uui-table-head-cell>Name</uui-table-head-cell>
                    <uui-table-head-cell>Alias</uui-table-head-cell>
                    <uui-table-head-cell>Fields</uui-table-head-cell>
                    <uui-table-head-cell>Status</uui-table-head-cell>
                    <uui-table-head-cell style="width:260px">Actions</uui-table-head-cell>
                </uui-table-head>
                ${forms.map(f => html`
                    <uui-table-row>
                        <uui-table-cell>
                            ${canEdit
                                ? html`<button type="button" class="link" @click=${() => host._editExisting(f.id)}>${f.name}</button>`
                                : html`<button type="button" class="link" @click=${() => host._viewEntries(f.id)} title="View entries">${f.name}</button>`}
                        </uui-table-cell>
                        <uui-table-cell><code>${f.alias}</code></uui-table-cell>
                        <uui-table-cell>${f.fields?.length || 0}</uui-table-cell>
                        <uui-table-cell>${f.isEnabled ? html`<span class="badge on">Active</span>` : html`<span class="badge off">Disabled</span>`}</uui-table-cell>
                        <uui-table-cell class="action-cell">
                            ${canEdit ? html`
                                <uui-button look="outline" compact @click=${() => host._editExisting(f.id)}>Edit</uui-button>
                            ` : nothing}
                            <uui-button look="outline" compact @click=${() => host._viewEntries(f.id)}>Entries</uui-button>
                            ${canEdit ? html`
                                <uui-button look="outline" color="danger" compact @click=${() => host._deleteForm(f.id)}>Delete</uui-button>
                            ` : nothing}
                        </uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>` : nothing}`;
}
