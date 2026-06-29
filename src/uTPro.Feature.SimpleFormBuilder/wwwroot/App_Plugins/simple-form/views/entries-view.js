import { html, nothing } from '@umbraco-cms/backoffice/external/lit';

/**
 * Renders the entries list view with search, date filter, and checkboxes.
 * @param {object} host - the dashboard element
 */
export function renderEntries(host) {
    const isAdmin = host._permissions?.canEdit;
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
            <!-- Header -->
            <div class="toolbar">
                <uui-button look="outline" @click=${() => host._backToList()}><uui-icon name="icon-arrow-left"></uui-icon> Back</uui-button>
                <h2>Entries: ${formName}</h2>
                <div class="toolbar-right">
                    <uui-button look="primary" compact @click=${() => host._viewEntries(host._viewFormId)}>Entries (${host._entryTotal})</uui-button>
                    ${isAdmin ? html`
                        <uui-button look="outline" compact @click=${() => host._editSettings(host._viewFormId)}><uui-icon name="icon-settings"></uui-icon> Settings</uui-button>
                    ` : nothing}
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
                    @keydown=${(e) => { if (e.key === 'Enter') { host._entrySkip = 0; host._selectedEntries = []; host._syncUrl(); host._loadEntries(); } }}>
                </uui-input>
                <div class="filter-dates">
                    <label class="filter-date-label">From:
                        <input type="date" .value=${host._dateFrom || ''}
                            @change=${(e) => host._setDateFrom(e.target.value)} />
                    </label>
                    <label class="filter-date-label">To:
                        <input type="date" .value=${host._dateTo || ''}
                            @change=${(e) => host._setDateTo(e.target.value)} />
                    </label>
                    <select class="quick-select" .value=${host._rangeMode || 'custom'}
                        @change=${(e) => host._setQuickRange(e.target.value)}>
                        <option value="custom">Custom...</option>
                        <option value="month">This month</option>
                        <option value="30d">Last 30 days</option>
                        <option value="7d">Last 7 days</option>
                        <option value="today">Today</option>
                    </select>
                    <uui-button look="outline" compact
                        @click=${() => { host._search = ''; const r = host._quickRange('month'); host._dateFrom = r.from; host._dateTo = r.to; host._rangeMode = 'month'; host._entrySkip = 0; host._selectedEntries = []; host._syncUrl(); host._loadEntries(); }}>Clear</uui-button>
                </div>
            </div>

            <!-- Table -->
            ${!host._entries.length ? html`<div class="empty">No entries yet</div>` : html`
            <uui-table aria-label="Entries">
                <uui-table-head>
                    <uui-table-head-cell style="width:40px">
                        <uui-checkbox title="Select all entries" .checked=${allSelected} @change=${() => host._toggleSelectAll()}></uui-checkbox>
                    </uui-table-head-cell>
                    <uui-table-head-cell style="width:160px">Date</uui-table-head-cell>
                    <uui-table-head-cell style="width:120px">IP</uui-table-head-cell>
                    ${allKeys.map(k => html`<uui-table-head-cell>${k}</uui-table-head-cell>`)}
                    <uui-table-head-cell style="width:100px">Actions</uui-table-head-cell>
                </uui-table-head>
                ${host._entries.map(s => html`
                    <uui-table-row class=${host._selectedEntries.includes(s.id) ? 'row-selected' : ''}>
                        <uui-table-cell>
                            <uui-checkbox title="Select entry" .checked=${host._selectedEntries.includes(s.id)}
                                @change=${() => host._toggleEntrySelect(s.id)}></uui-checkbox>
                        </uui-table-cell>
                        <uui-table-cell>${new Date(s.createdUtc).toLocaleString()}</uui-table-cell>
                        <uui-table-cell>${s.ipAddress || ''}</uui-table-cell>
                        ${allKeys.map(k => html`<uui-table-cell class="cell-truncate">${s.data?.[k] || ''}</uui-table-cell>`)}
                        <uui-table-cell class="action-cell">
                            <uui-button look="outline" compact @click=${() => host._viewDetail(s)} title="View"><uui-icon name="icon-eye"></uui-icon></uui-button>
                            ${isAdmin ? html`
                                <uui-button look="outline" color="danger" compact @click=${() => host._deleteEntry(s.id)} title="Delete"><uui-icon name="icon-trash"></uui-icon></uui-button>
                            ` : nothing}
                        </uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>
            ${host._entryTotal > 20 ? html`
                <div class="pagination">
                    <uui-button look="outline" ?disabled=${host._entrySkip === 0}
                        @click=${() => { host._entrySkip = Math.max(0, host._entrySkip - 20); host._syncUrl(); host._loadEntries(); }}>Prev</uui-button>
                    <span>Page ${page} of ${pages}</span>
                    <uui-button look="outline" ?disabled=${host._entrySkip + 20 >= host._entryTotal}
                        @click=${() => { host._entrySkip += 20; host._syncUrl(); host._loadEntries(); }}>Next</uui-button>
                </div>` : nothing}
            `}
        `;
}
