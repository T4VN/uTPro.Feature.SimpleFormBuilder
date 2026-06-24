// ── Entry point: Simple Form Dashboard ──
// Views and styles are split into separate files for maintainability.
//   api.js              – API helper & constants
//   styles.js           – All CSS styles
//   views/list-view.js  – Form list
//   views/editor-view.js – Form editor
//   views/entries-view.js – Entries table
//   views/detail-view.js – Entry detail overlay

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

import { API, apiPost } from './api.js';
import { dashboardStyles } from './styles.js';
import { renderList } from './views/list-view.js';
import { renderEditor } from './views/editor-view.js';
import { renderEntries } from './views/entries-view.js';
import { renderDetail } from './views/detail-view.js';

export class UtproSimpleFormDashboard extends UmbLitElement {

    // ── Reactive properties ──
    static properties = {
        _view: { type: String, state: true },
        _forms: { type: Array, state: true },
        _loading: { type: Boolean, state: true },
        _editForm: { type: Object, state: true },
        _fieldTypes: { type: Array, state: true },
        _entries: { type: Array, state: true },
        _entryTotal: { type: Number, state: true },
        _entrySkip: { type: Number, state: true },
        _viewFormId: { type: Number, state: true },
        _error: { type: String, state: true },
        _success: { type: String, state: true },
        _selectedEntries: { type: Array, state: true },
        _detailEntry: { type: Object, state: true },
        _permissions: { type: Object, state: true },
        _search: { type: String, state: true },
        _dateFrom: { type: String, state: true },
        _dateTo: { type: String, state: true },
        _showColumnSettings: { type: Boolean, state: true },
        _entryCount: { type: Number, state: true },
        _typePickerIdx: { type: Number, state: true },
        _typePickerSearch: { type: String, state: true },
        _typePickerGroupIdx: { type: Number, state: true },
        _typePickerColIdx: { type: Number, state: true },
        _fieldSettingsLoc: { type: Object, state: true },
    };

    // ── Styles ──
    static styles = dashboardStyles;

    #authContext;

    constructor() {
        super();
        this._view = 'list';
        this._forms = [];
        this._loading = false;
        this._editForm = null;
        this._fieldTypes = [];
        this._entries = [];
        this._entryTotal = 0;
        this._entrySkip = 0;
        this._viewFormId = 0;
        this._error = '';
        this._success = '';
        this._selectedEntries = [];
        this._detailEntry = null;
        this._permissions = { isAdmin: false, canViewSensitive: false };
        this._search = '';
        this._dateFrom = '';
        this._dateTo = '';
        this._showColumnSettings = false;
        this._entryCount = 0;
        this._typePickerIdx = -1;
        this._typePickerSearch = '';
        this._typePickerGroupIdx = -1;
        this._typePickerColIdx = -1;
        this._fieldSettingsLoc = null;
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => { this.#authContext = ctx; });
    }

    async connectedCallback() {
        super.connectedCallback();
        await this._loadPermissions();
        await this._loadForms();
        await this._loadFieldTypes();
    }

    // ── API helper ──
    async _api(url, body = {}) {
        return apiPost(url, body, this.#authContext);
    }

    _msg(m, err = false) {
        if (err) { this._error = m; this._success = ''; }
        else { this._success = m; this._error = ''; }
        setTimeout(() => { this._error = ''; this._success = ''; }, 3000);
    }

    // ── Permissions ──
    async _loadPermissions() {
        try {
            this._permissions = await this._api(API + '/permissions');
        } catch {
            this._permissions = { isAdmin: false, canViewSensitive: false };
        }
    }

    // ── Data loading ──
    async _loadForms() {
        this._loading = true;
        try { this._forms = await this._api(API + '/list'); }
        catch (e) { this._msg(e.message, true); }
        this._loading = false;
    }

    async _loadFieldTypes() {
        try { this._fieldTypes = await this._api(API + '/field-types'); } catch {}
    }

    // ── Form CRUD ──
    _newForm() {
        this._editForm = {
            id: 0, name: '', alias: '', fields: [], groups: [],
            successMessage: 'Thank you!', redirectUrl: '', emailTo: '', emailSubject: '',
            storeEntries: true, isEnabled: true
        };
        this._view = 'edit';
    }

    async _editExisting(id) {
        try {
            this._editForm = await this._api(API + '/get', { id });
            this._showColumnSettings = false;
            const res = await this._api(API + '/entries', { formId: id, skip: 0, take: 1 });
            this._entryCount = res.total || 0;
            this._view = 'edit';
        } catch (e) { this._msg(e.message, true); }
    }

    async _saveForm() {
        if (!this._editForm.name || !this._editForm.alias) {
            this._msg('Name and Alias required', true);
            return;
        }
        try {
            const res = await this._api(API + '/save', this._editForm);
            this._msg(res.message);
            this._editForm.id = res.id;
            await this._loadForms();
        } catch (e) { this._msg(e.message, true); }
    }

    async _deleteForm(id) {
        if (!confirm('Delete this form and all entries?')) return;
        try {
            await this._api(API + '/delete', { id });
            this._msg('Deleted');
            await this._loadForms();
            if (this._editForm?.id === id) { this._editForm = null; this._view = 'list'; }
        } catch (e) { this._msg(e.message, true); }
    }

    // ── Group management ──
    _addGroup() {
        const f = this._editForm;
        if (!f.groups) f.groups = [];
        const idx = f.groups.length;
        f.groups = [...f.groups, {
            id: crypto.randomUUID?.() || Date.now().toString(36),
            name: '',
            cssClass: '',
            columns: [{ id: crypto.randomUUID?.() || Date.now().toString(36), width: 12, fields: [] }],
            sortOrder: idx
        }];
        this.requestUpdate();
    }

    _removeGroup(gIdx) {
        if (!confirm('Remove this group and all its columns/fields?')) return;
        this._editForm.groups = this._editForm.groups.filter((_, i) => i !== gIdx);
        this._editForm.groups.forEach((g, i) => g.sortOrder = i);
        this.requestUpdate();
    }

    _moveGroup(gIdx, dir) {
        const arr = [...this._editForm.groups];
        const newIdx = gIdx + dir;
        if (newIdx < 0 || newIdx >= arr.length) return;
        [arr[gIdx], arr[newIdx]] = [arr[newIdx], arr[gIdx]];
        arr.forEach((g, i) => g.sortOrder = i);
        this._editForm.groups = arr;
        this.requestUpdate();
    }

    _updateGroup(gIdx, key, val) {
        this._editForm.groups[gIdx][key] = val;
        this.requestUpdate();
    }

    // ── Column management within a group ──
    _addColumn(gIdx) {
        const g = this._editForm.groups[gIdx];
        if (!g.columns) g.columns = [];
        g.columns = [...g.columns, {
            id: crypto.randomUUID?.() || Date.now().toString(36),
            width: 6,
            fields: []
        }];
        this.requestUpdate();
    }

    _removeColumn(gIdx, cIdx) {
        if (!confirm('Remove this column and all its fields?')) return;
        this._editForm.groups[gIdx].columns = this._editForm.groups[gIdx].columns.filter((_, i) => i !== cIdx);
        this.requestUpdate();
    }

    _updateColumnWidth(gIdx, cIdx, val) {
        this._editForm.groups[gIdx].columns[cIdx].width = Math.min(12, Math.max(1, parseInt(val) || 1));
        this.requestUpdate();
    }

    /**
     * Move an entire column (with all its fields) to another group.
     * @param {number} fromGIdx - source group index
     * @param {number} cIdx - column index within source group
     * @param {number} toGIdx - destination group index
     */
    _moveColumnTo(fromGIdx, cIdx, toGIdx) {
        const f = this._editForm;
        const col = f.groups[fromGIdx].columns.splice(cIdx, 1)[0];
        if (!col) return;
        f.groups[toGIdx].columns.push(col);
        this.requestUpdate();
    }

    _swapColumn(gIdx, cIdx, dir) {
        const cols = this._editForm.groups[gIdx].columns;
        const newIdx = cIdx + dir;
        if (newIdx < 0 || newIdx >= cols.length) return;
        [cols[cIdx], cols[newIdx]] = [cols[newIdx], cols[cIdx]];
        this.requestUpdate();
    }

    // ── Field management within a column ──
    _addFieldToColumn(gIdx, cIdx) {
        const col = this._editForm.groups[gIdx].columns[cIdx];
        const idx = col.fields.length;
        col.fields = [...col.fields, {
            id: crypto.randomUUID?.() || Date.now().toString(36),
            type: 'text', label: '', name: 'field_' + Date.now().toString(36),
            placeholder: '', cssClass: '', required: false,
            validation: '', validationMessage: '', defaultValue: '',
            options: [], sortOrder: idx, attributes: {}
        }];
        this.requestUpdate();
    }

    _removeFieldFromColumn(gIdx, cIdx, fIdx) {
        const removedName = this._editForm.groups[gIdx].columns[cIdx].fields[fIdx]?.name;
        this._editForm.groups[gIdx].columns[cIdx].fields = this._editForm.groups[gIdx].columns[cIdx].fields.filter((_, i) => i !== fIdx);
        if (removedName && this._editForm.visibleColumns) {
            this._editForm.visibleColumns = this._editForm.visibleColumns.filter(c => c !== removedName);
        }
        this.requestUpdate();
    }

    _moveFieldInColumn(gIdx, cIdx, fIdx, dir) {
        const arr = [...this._editForm.groups[gIdx].columns[cIdx].fields];
        const newIdx = fIdx + dir;
        if (newIdx < 0 || newIdx >= arr.length) return;
        [arr[fIdx], arr[newIdx]] = [arr[newIdx], arr[fIdx]];
        arr.forEach((f, i) => f.sortOrder = i);
        this._editForm.groups[gIdx].columns[cIdx].fields = arr;
        this.requestUpdate();
    }

    _updateFieldInColumn(gIdx, cIdx, fIdx, key, val) {
        this._editForm.groups[gIdx].columns[cIdx].fields[fIdx][key] = val;
        if (key === 'type' && val === 'password') {
            this._editForm.groups[gIdx].columns[cIdx].fields[fIdx].isSensitive = true;
        }
        this.requestUpdate();
    }

    _addOptionInColumn(gIdx, cIdx, fIdx) {
        if (!this._editForm.groups[gIdx].columns[cIdx].fields[fIdx].options)
            this._editForm.groups[gIdx].columns[cIdx].fields[fIdx].options = [];
        this._editForm.groups[gIdx].columns[cIdx].fields[fIdx].options.push({ text: '', value: '' });
        this.requestUpdate();
    }

    _removeOptionInColumn(gIdx, cIdx, fIdx, oIdx) {
        this._editForm.groups[gIdx].columns[cIdx].fields[fIdx].options.splice(oIdx, 1);
        this.requestUpdate();
    }

    // ── Move field between groups/columns ──
    /**
     * Move a field from one location to another.
     * @param {object} from - { gIdx, cIdx, fIdx } source (-1 for ungrouped)
     * @param {object} to   - { gIdx, cIdx } destination (-1 for ungrouped)
     */
    _moveFieldTo(from, to) {
        const f = this._editForm;

        // Remove from source column
        const field = f.groups[from.gIdx].columns[from.cIdx].fields.splice(from.fIdx, 1)[0];
        if (!field) return;

        // Add to destination column
        const destCol = f.groups[to.gIdx].columns[to.cIdx];
        field.sortOrder = destCol.fields.length;
        destCol.fields.push(field);

        this.requestUpdate();
    }

    // ── Entries ──
    async _viewEntries(formId) {
        this._viewFormId = formId;
        this._entrySkip = 0;
        this._search = '';
        this._dateFrom = '';
        this._dateTo = '';
        this._selectedEntries = [];
        this._view = 'entries';
        await this._loadEntries();
    }

    async _loadEntries() {
        try {
            const body = {
                formId: this._viewFormId, skip: this._entrySkip, take: 20
            };
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

    // ── Render ──
    render() {
        return html`
            ${this._error ? html`<div class="msg error">${this._error}</div>` : nothing}
            ${this._success ? html`<div class="msg success">${this._success}</div>` : nothing}
            ${this._view === 'list' ? renderList(this)
                : this._view === 'edit' ? renderEditor(this)
                : renderEntries(this)}
            ${this._detailEntry ? renderDetail(this) : nothing}`;
    }
}

customElements.define('utpro-simple-form-dashboard', UtproSimpleFormDashboard);
export default UtproSimpleFormDashboard;
