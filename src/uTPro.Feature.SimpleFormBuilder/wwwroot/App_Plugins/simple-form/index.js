// ── Entry point: Simple Form Dashboard ──
// The dashboard is composed from small, focused modules:
//   api.js                     – API helper & constants
//   bus.js                     – cross-component event bus (sidebar ↔ dashboard)
//   styles.js                  – all CSS
//   date-range.js              – pure date helpers (quick range)
//   mixins/url-state.mixin.js  – deep-linking (URL ↔ view/filters)
//   mixins/form-actions.mixin.js – form CRUD + import/export
//   mixins/builder.mixin.js    – group/column/field/option editing
//   mixins/entries.mixin.js    – entries loading, filtering, selection, CSV
//   views/*.js                 – presentational render functions
//
// This file owns only: reactive state, lifecycle wiring, shared helpers
// (_api / _msg), data loaders, and the top-level render switch.

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';

import { API, apiPost } from './api.js';
import { dashboardStyles } from './styles.js';
import { formBus } from './bus.js';

import { UrlStateMixin } from './mixins/url-state.mixin.js';
import { FormActionsMixin } from './mixins/form-actions.mixin.js';
import { BuilderMixin } from './mixins/builder.mixin.js';
import { ClipboardMixin } from './mixins/clipboard.mixin.js';
import { EntriesMixin } from './mixins/entries.mixin.js';

import { renderList } from './views/list-view.js';
import { renderEditor } from './views/editor-view.js';
import { renderEntries } from './views/entries-view.js';
import { renderDetail } from './views/detail-view.js';

// Compose the feature mixins onto the Umbraco Lit base element.
const DashboardBase = EntriesMixin(ClipboardMixin(BuilderMixin(FormActionsMixin(UrlStateMixin(UmbLitElement)))));

export class UtproSimpleFormDashboard extends DashboardBase {

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
        _selectedEntries: { type: Array, state: true },
        _detailEntry: { type: Object, state: true },
        _permissions: { type: Object, state: true },
        _search: { type: String, state: true },
        _listSearch: { type: String, state: true },
        _dateFrom: { type: String, state: true },
        _dateTo: { type: String, state: true },
        _rangeMode: { type: String, state: true },
        _showColumnSettings: { type: Boolean, state: true },
        _entryCount: { type: Number, state: true },
        _typePickerIdx: { type: Number, state: true },
        _typePickerSearch: { type: String, state: true },
        _typePickerGroupIdx: { type: Number, state: true },
        _typePickerColIdx: { type: Number, state: true },
        _fieldSettingsLoc: { type: Object, state: true },
        _fieldSettingsSnapshot: { type: Object, state: true },
        _clip: { state: true },
        _languages: { type: Array, state: true },
        _previewLang: { type: String, state: true },
    };

    static styles = dashboardStyles;

    #authContext;
    #notificationContext;

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
        this._selectedEntries = [];
        this._detailEntry = null;
        this._permissions = { isAdmin: false, canEdit: false, canViewSensitive: false };
        this._search = '';
        this._listSearch = '';
        this._dateFrom = '';
        this._dateTo = '';
        this._rangeMode = 'month';
        this._showColumnSettings = false;
        this._entryCount = 0;
        this._typePickerIdx = -1;
        this._typePickerSearch = '';
        this._typePickerGroupIdx = -1;
        this._typePickerColIdx = -1;
        this._fieldSettingsLoc = null;
        this._fieldSettingsSnapshot = null;
        this._editFormSnapshot = '';
        this._clip = null;
        this._languages = [];
        this._previewLang = '';   // '' → show raw {{ }} syntax
        this._dictMap = {};       // { key: { isoLower: value } }
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => { this.#authContext = ctx; });
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (ctx) => { this.#notificationContext = ctx; });
    }

    async connectedCallback() {
        super.connectedCallback();
        this.#wireBus();

        // ── Unsaved-changes guards ──
        // 1) Full reload / tab close / external URL.
        this._beforeUnload = (e) => {
            if (this._isDirty()) { e.preventDefault(); e.returnValue = ''; }
        };
        window.addEventListener('beforeunload', this._beforeUnload);

        // 2) SPA navigation that LEAVES the uTPro Form section (e.g. clicking a
        //    different Umbraco section/menu). The Navigation API lets us cancel
        //    a same-origin route change based on the target URL. In-section moves
        //    (Entries, sidebar form switch, etc.) stay within `utpro-form` and are
        //    guarded by _confirmLeave() inside the navigation methods instead.
        if (window.navigation && typeof window.navigation.addEventListener === 'function') {
            this._navGuard = (e) => {
                if (!this._isDirty() || !e.cancelable) return;
                let dest = '';
                try { dest = e.destination?.url || ''; } catch { /* ignore */ }
                if (dest && dest.includes('utpro-form')) return; // staying in our section
                if (confirm('You have unsaved changes. Leave without saving?')) {
                    this._discardDirty();
                } else {
                    e.preventDefault();
                }
            };
            window.navigation.addEventListener('navigate', this._navGuard);
        }

        // Keep the paste buttons in sync with the system clipboard.
        // 1) Returning focus to this window → re-read the clipboard (best-effort).
        this._clipFocus = () => { this._refreshClip(); };
        window.addEventListener('focus', this._clipFocus);
        document.addEventListener('visibilitychange', this._clipFocus);

        // 2) The user copied something else IN the page (Ctrl+C, copying another
        //    field, etc.). Our own copy buttons use navigator.clipboard.writeText
        //    which does NOT fire a 'copy' event, so any 'copy' here means external
        //    content replaced our payload → hide the Paste buttons immediately.
        this._onExternalCopy = () => {
            if (this._clip) { this._clip = null; this.requestUpdate(); }
        };
        document.addEventListener('copy', this._onExternalCopy, true);

        // 3) Live clipboard-change events (Chromium) cover copies from other apps
        //    while the tab is focused. Re-read to refresh/clear the snapshot.
        if (navigator.clipboard && typeof navigator.clipboard.addEventListener === 'function') {
            this._onClipChange = () => { this._refreshClip(); };
            try { navigator.clipboard.addEventListener('clipboardchange', this._onClipChange); } catch { /* unsupported */ }
        }

        await this._loadPermissions();
        await this._loadForms();
        await this._loadFieldTypes();
        await this._loadDictionary();
        await this._restoreFromUrl();
        this._refreshClip();
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        formBus.removeEventListener('new', this._busNew);
        formBus.removeEventListener('select', this._busSelect);
        formBus.removeEventListener('notify', this._busNotify);
        formBus.removeEventListener('entries', this._busEntries);
        formBus.removeEventListener('export', this._busExport);
        formBus.removeEventListener('delete', this._busDelete);
        window.removeEventListener('beforeunload', this._beforeUnload);
        window.removeEventListener('focus', this._clipFocus);
        document.removeEventListener('visibilitychange', this._clipFocus);
        document.removeEventListener('copy', this._onExternalCopy, true);
        if (this._onClipChange && navigator.clipboard?.removeEventListener) {
            try { navigator.clipboard.removeEventListener('clipboardchange', this._onClipChange); } catch { /* ignore */ }
        }
        if (this._navGuard && window.navigation?.removeEventListener) {
            window.navigation.removeEventListener('navigate', this._navGuard);
        }
    }

    // ── Sidebar bus wiring ──
    // Relay sidebar intents to this dashboard. Guards prevent feedback loops
    // when the dashboard echoes its own selection back onto the bus.
    #wireBus() {
        this._busNew = () => {
            if (this._view === 'edit' && this._editForm && this._editForm.id === 0) return;
            this._newForm();
        };
        this._busSelect = (e) => {
            const id = Number(e.detail) || 0;
            if (!id) return;
            // Users who can edit open the editor; everyone else (Entries is their
            // only action) jumps straight to the Entries view.
            if (this._permissions?.canEdit) {
                if (this._view === 'edit' && this._editForm?.id === id) return;
                this._editExisting(id);
            } else {
                if (this._view === 'entries' && this._viewFormId === id) return;
                this._viewEntries(id);
            }
        };
        this._busNotify = (e) => this._msg(e.detail);
        this._busEntries = (e) => { const id = Number(e.detail) || 0; if (id) this._viewEntries(id); };
        this._busExport = (e) => { const id = Number(e.detail) || 0; if (id) this._exportForm(id); };
        this._busDelete = (e) => { const id = Number(e.detail) || 0; if (id) this._deleteForm(id); };

        formBus.addEventListener('new', this._busNew);
        formBus.addEventListener('select', this._busSelect);
        formBus.addEventListener('notify', this._busNotify);
        formBus.addEventListener('entries', this._busEntries);
        formBus.addEventListener('export', this._busExport);
        formBus.addEventListener('delete', this._busDelete);
    }

    // ── Shared helpers ──
    async _api(url, body = {}) {
        return apiPost(url, body, this.#authContext);
    }

    _msg(m, err = false) {
        // Use Umbraco's built-in floating notifications (toast) for all messages.
        this.#notificationContext?.peek(err ? 'danger' : 'positive', { data: { message: m } });
    }

    // Downloads an uploaded file for an entry field through the authenticated
    // backoffice endpoint (a plain <a href> can't carry the bearer token).
    async _downloadEntryFile(entryId, fieldName, fileName) {
        try {
            const config = this.#authContext?.getOpenApiConfiguration();
            const headers = {};
            if (config?.token) {
                const t = await config.token();
                if (t) headers['Authorization'] = 'Bearer ' + t;
            }
            const url = API + '/entry-file?entryId=' + encodeURIComponent(entryId)
                + '&fieldName=' + encodeURIComponent(fieldName);
            const resp = await fetch(url, {
                headers,
                credentials: config?.credentials || 'same-origin'
            });
            if (!resp.ok) throw new Error('Download failed');
            const blob = await resp.blob();
            const objUrl = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = objUrl;
            a.download = fileName || 'download';
            a.click();
            URL.revokeObjectURL(objUrl);
        } catch (e) { this._msg(e.message || 'Download failed', true); }
    }

    // Closes the entries "Export" dropdown after a choice is made.
    _closeExportMenu() {
        this.renderRoot?.querySelector('#utpro-entries-export-menu')?.hidePopover?.();
    }

    // Exports all matching entries as a ZIP (one folder per entry: its CSV row + uploaded
    // files), streamed through the authenticated backoffice endpoint.
    async _exportEntriesZip() {
        try {
            const config = this.#authContext?.getOpenApiConfiguration();
            const headers = { 'Content-Type': 'application/json' };
            if (config?.token) {
                const t = await config.token();
                if (t) headers['Authorization'] = 'Bearer ' + t;
            }
            const body = { formId: this._viewFormId };
            if (this._search) body.search = this._search;
            if (this._dateFrom) body.dateFrom = this._dateFrom;
            if (this._dateTo) body.dateTo = this._dateTo;

            const resp = await fetch(API + '/export-entries-zip', {
                method: 'POST',
                headers,
                credentials: config?.credentials || 'same-origin',
                body: JSON.stringify(body)
            });
            if (!resp.ok) throw new Error('Export failed');
            const blob = await resp.blob();
            const objUrl = URL.createObjectURL(blob);
            const a = document.createElement('a');
            const formName = this._forms.find(f => f.id === this._viewFormId)?.alias || 'form';
            a.href = objUrl;
            a.download = formName + '-entries.zip';
            a.click();
            URL.revokeObjectURL(objUrl);
        } catch (e) { this._msg(e.message || 'Export failed', true); }
    }

    // Parses a stored "utpro-file:{name}|{token}" value into its display name,
    // or returns null when the value is not a file reference.
    _fileValueName(value) {
        if (typeof value !== 'string' || !value.startsWith('utpro-file:')) return null;
        const body = value.slice('utpro-file:'.length);
        const sep = body.lastIndexOf('|');
        return sep < 0 ? body : body.slice(0, sep);
    }

    // ── Data loading ──
    async _loadPermissions() {
        try {
            this._permissions = await this._api(API + '/permissions');
        } catch {
            this._permissions = { isAdmin: false, canEdit: false, canViewSensitive: false };
        }
    }

    async _loadForms() {
        this._loading = true;
        try { this._forms = await this._api(API + '/list'); }
        catch (e) { this._msg(e.message, true); }
        this._loading = false;
        formBus.refresh(); // keep the sidebar list in sync
    }

    async _loadFieldTypes() {
        try { this._fieldTypes = await this._api(API + '/field-types'); } catch {}
    }

    // Load site languages + dictionary translations for the builder's live preview.
    async _loadDictionary() {
        try {
            const res = await this._api(API + '/dictionary');
            this._languages = res?.languages || [];
            const map = {};
            for (const item of (res?.items || [])) {
                const t = {};
                for (const [iso, val] of Object.entries(item.translations || {})) {
                    t[String(iso).toLowerCase()] = val;
                }
                map[item.key] = t;
            }
            this._dictMap = map;
        } catch {
            this._languages = [];
            this._dictMap = {};
        }
    }

    /**
     * Resolve {{ Key }} tokens in a preview string using the currently selected
     * preview language. When no language is selected the raw text (including the
     * tokens) is returned unchanged. Unknown keys / missing translations keep the
     * original token so nothing silently disappears.
     */
    _localizePreview(text) {
        if (!text || !this._previewLang || text.indexOf('{{') < 0) return text;
        const iso = this._previewLang.toLowerCase();
        return text.replace(/\{\{\s*([^{}]+?)\s*\}\}/g, (m, key) => {
            const val = this._dictMap?.[key.trim()]?.[iso];
            return (val === undefined || val === null || val === '') ? m : val;
        });
    }

    // ── Render ──
    render() {
        return html`
            ${this._view === 'list' ? renderList(this)
                : this._view === 'edit' ? renderEditor(this)
                : renderEntries(this)}
            ${this._detailEntry ? renderDetail(this) : nothing}`;
    }
}

customElements.define('utpro-simple-form-dashboard', UtproSimpleFormDashboard);
export default UtproSimpleFormDashboard;
