// Entries view: loading, filtering (search + date range), selection,
// bulk actions, CSV export, and the detail overlay.

import { API } from '../api.js';
import { formBus } from '../bus.js';
import { quickRange } from '../date-range.js';

const PAGE_SIZE = 20;

export const EntriesMixin = (Base) => class extends Base {

    async _viewEntries(formId, opts = null) {
        // Guard unsaved edits when navigating away from a dirty editor
        // (e.g. clicking the Entries button or a form in the sidebar).
        if (!opts && !this._confirmLeave()) return;
        this._viewFormId = formId;
        this._selectedEntries = [];
        if (opts) {
            // Restore filters from a shared/deep link.
            this._search = opts.search ?? '';
            this._dateFrom = opts.from ?? '';
            this._dateTo = opts.to ?? '';
            this._rangeMode = opts.range || ((opts.from || opts.to) ? 'custom' : 'month');
            this._entrySkip = opts.skip ?? 0;
        } else {
            // Default filter: This month.
            this._entrySkip = 0;
            this._search = '';
            const { from, to } = quickRange('month');
            this._dateFrom = from;
            this._dateTo = to;
            this._rangeMode = 'month';
        }
        this._view = 'entries';
        // Highlight the corresponding form in the sidebar.
        formBus.setActive(formId);
        this._syncUrl();
        await this._loadEntries();
    }

    // ── Date range filter ──
    // Exposed as an instance method so views (e.g. the Clear button) can call it.
    _quickRange(range) { return quickRange(range); }

    _setQuickRange(range) {
        this._rangeMode = range;
        if (range === 'custom') { this.requestUpdate(); return; } // keep typed dates
        const { from, to } = quickRange(range);
        this._dateFrom = from;
        this._dateTo = to;
        this._applyEntryFilter();
    }

    _setDateFrom(value) {
        this._dateFrom = value;
        this._rangeMode = 'custom';
        this._applyEntryFilter();
    }

    _setDateTo(value) {
        this._dateTo = value;
        this._rangeMode = 'custom';
        this._applyEntryFilter();
    }

    // Reset to first page, drop selection, sync URL and reload.
    _applyEntryFilter() {
        this._entrySkip = 0;
        this._selectedEntries = [];
        this._syncUrl();
        this._loadEntries();
    }

    async _loadEntries() {
        try {
            const body = { formId: this._viewFormId, skip: this._entrySkip, take: PAGE_SIZE };
            if (this._search) body.search = this._search;
            if (this._dateFrom) body.dateFrom = this._dateFrom;
            if (this._dateTo) body.dateTo = this._dateTo;
            const res = await this._api(API + '/entries', body);
            this._entries = res.items || [];
            this._entryTotal = res.total || 0;
        } catch (e) { this._msg(e.message, true); }
    }

    async _deleteEntry(id) {
        if (!confirm('Delete this entry?')) return;
        try {
            await this._api(API + '/delete-entry', { id });
            this._msg('Deleted');
            this._selectedEntries = this._selectedEntries.filter(x => x !== id);
            await this._loadEntries();
        } catch (e) { this._msg(e.message, true); }
    }

    _toggleEntrySelect(id) {
        if (this._selectedEntries.includes(id))
            this._selectedEntries = this._selectedEntries.filter(x => x !== id);
        else
            this._selectedEntries = [...this._selectedEntries, id];
    }

    _toggleSelectAll() {
        if (this._selectedEntries.length === this._entries.length)
            this._selectedEntries = [];
        else
            this._selectedEntries = this._entries.map(s => s.id);
    }

    async _bulkDelete() {
        if (!this._selectedEntries.length) return;
        if (!confirm('Delete ' + this._selectedEntries.length + ' entries?')) return;
        for (const id of this._selectedEntries) {
            try { await this._api(API + '/delete-entry', { id }); } catch {}
        }
        this._selectedEntries = [];
        this._msg('Deleted');
        await this._loadEntries();
    }

    _exportCsv() {
        if (!this._entries.length) return;
        const allKeys = [...new Set(this._entries.flatMap(s => Object.keys(s.data || {})))];
        const headers = ['Date', 'IP', ...allKeys];
        const rows = this._entries.map(s => {
            const date = new Date(s.createdUtc).toLocaleString();
            const ip = s.ipAddress || '';
            const fields = allKeys.map(k => '"' + (s.data?.[k] || '').replace(/"/g, '""') + '"');
            return ['"' + date + '"', '"' + ip + '"', ...fields].join(',');
        });
        const csv = headers.join(',') + '\n' + rows.join('\n');
        const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        const formName = this._forms.find(f => f.id === this._viewFormId)?.alias || 'form';
        a.href = url; a.download = formName + '-entries.csv'; a.click();
        URL.revokeObjectURL(url);
    }

    _viewDetail(entry) { this._detailEntry = entry; }
    _closeDetail() { this._detailEntry = null; }
};
