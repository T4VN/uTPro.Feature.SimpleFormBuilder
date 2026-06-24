import { html, nothing } from '@umbraco-cms/backoffice/external/lit';

/**
 * Renders the entries list view with search, date filter, and checkboxes.
 * @param {object} host - the dashboard element
 */
export function renderEntries(host) {
    const isAdmin = host._permissions?.isAdmin;
    const form = host._forms.find(f => f.id === host._viewFormId);
    const formName = form?.name || 'Form';
    const pages = Math.max(1, Math.ceil(host._entryTotal / 20));
    const page = Math.floor(host._entrySkip / 20) + 1;
    const allDataKeys = [...new Set(host._entries.flatMap(s => Object.keys(s.data || {})))];
    const visibleCols = form?.visibleColumns;
    const allKeys = visibleCols && visibleCols.length > 0
        ? allDataKeys.filter(k => visibleCols.includes(k))
        : allDataKeys;
    const allSelected = host._entries.length > 0 && host._selectedEntries.length === host._entries.length;

    return html`
        <uui-box>
            <!-- Header -->
            <div class="toolbar">
                <uui-button look="outline" @click=${() => { host._view = 'list'; host._selectedEntries = []; }}>&#8592; Back</uui-button>
                <h2>Entries: ${formName}</h2>
                <div class="toolbar-right">
                    <span class="page-info">${host._entryTotal} entries</span>
                    ${host._entries.length ? html`
                        <uui-button look="outline" compact @click=${() => host._exportCsv()}>Export CSV</uui-button>
                    ` : nothing}
                    ${isAdmin && host._selectedEntries.length ? html`
                        <uui-button look="outline" color="danger" compact @click=${() => host._bulkDelete()}>
                            Delete (${host._selectedEntries.length})
                        </uui-button>
                    ` : nothing}
                </div>
            </div>

            <!-- Search & Date filters -->
            <div class="filter-bar">
                <uui-input class="filter-search"
                    placeholder="Filter entries..."
                    .value=${host._search || ''}
                    @input=${(e) => { host._search = e.target.value; }}
                    @keydown=${(e) => { if (e.key === 'Enter') { host._entrySkip = 0; host._selectedEntries = []; host._loadEntries(); } }}>
                </uui-input>
                <div class="filter-dates">
                    <label class="filter-date-label">From:
                        <input type="date" .value=${host._dateFrom || ''}
                            @change=${(e) => { host._dateFrom = e.target.value; host._entrySkip = 0; host._selectedEntries = []; host._loadEntries(); }} />
                    </label>
                    <label class="filter-date-label">To:
                        <input type="date" .value=${host._dateTo || ''}
                            @change=${(e) => { host._dateTo = e.target.value; host._entrySkip = 0; host._selectedEntries = []; host._loadEntries(); }} />
                    </label>
                    <uui-button look="outline" compact
                        @click=${() => { host._search = ''; host._dateFrom = ''; host._dateTo = ''; host._entrySkip = 0; host._selectedEntries = []; host._loadEntries(); }}>Clear</uui-button>
                </div>
            </div>

            <!-- Table -->
            ${!host._entries.length ? html`<div class="empty">No entries yet</div>` : html`
            <uui-table aria-label="Entries">
                <uui-table-head>
                    <uui-table-head-cell style="width:40px">
                        <input type="checkbox" ?checked=${allSelected} @change=${() => host._toggleSelectAll()} />
                    </uui-table-head-cell>
                    <uui-table-head-cell style="width:160px">Date</uui-table-head-cell>
                    <uui-table-head-cell style="width:120px">IP</uui-table-head-cell>
                    ${allKeys.map(k => html`<uui-table-head-cell>${k}</uui-table-head-cell>`)}
                    <uui-table-head-cell style="width:100px">Actions</uui-table-head-cell>
                </uui-table-head>
                ${host._entries.map(s => html`
                    <uui-table-row class=${host._selectedEntries.includes(s.id) ? 'row-selected' : ''}>
                        <uui-table-cell>
                            <input type="checkbox" ?checked=${host._selectedEntries.includes(s.id)}
                                @change=${() => host._toggleEntrySelect(s.id)} />
                        </uui-table-cell>
                        <uui-table-cell>${new Date(s.createdUtc).toLocaleString()}</uui-table-cell>
                        <uui-table-cell>${s.ipAddress || ''}</uui-table-cell>
                        ${allKeys.map(k => html`<uui-table-cell class="cell-truncate">${s.data?.[k] || ''}</uui-table-cell>`)}
                        <uui-table-cell class="action-cell">
                            <uui-button look="outline" compact @click=${() => host._viewDetail(s)} title="View">&#9776;</uui-button>
                            ${isAdmin ? html`
                                <uui-button look="outline" color="danger" compact @click=${() => host._deleteEntry(s.id)} title="Delete">&#10005;</uui-button>
                            ` : nothing}
                        </uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>
            ${host._entryTotal > 20 ? html`
                <div class="pagination">
                    <uui-button look="outline" ?disabled=${host._entrySkip === 0}
                        @click=${() => { host._entrySkip = Math.max(0, host._entrySkip - 20); host._loadEntries(); }}>Prev</uui-button>
                    <span>Page ${page} of ${pages}</span>
                    <uui-button look="outline" ?disabled=${host._entrySkip + 20 >= host._entryTotal}
                        @click=${() => { host._entrySkip += 20; host._loadEntries(); }}>Next</uui-button>
                </div>` : nothing}
            `}
        </uui-box>`;
}
