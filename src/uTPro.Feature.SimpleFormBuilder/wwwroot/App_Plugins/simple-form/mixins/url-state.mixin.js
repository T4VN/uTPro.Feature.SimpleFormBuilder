// Deep-linking: keep the address bar in sync with the current view + filters,
// and restore that state on load so links are shareable and survive refresh.
//   Scheme: ?view=list|new|edit|entries & form=<id> & settings=1
//           & q=<search> & from=<date> & to=<date> & range=<key> & page=<n>

export const UrlStateMixin = (Base) => class extends Base {

    _syncUrl() {
        try {
            const p = new URLSearchParams(window.location.search);
            ['view', 'form', 'settings', 'q', 'from', 'to', 'range', 'page'].forEach(k => p.delete(k));
            if (this._view === 'edit') {
                if (this._editForm && this._editForm.id > 0) {
                    p.set('view', 'edit');
                    p.set('form', String(this._editForm.id));
                    if (this._showColumnSettings) p.set('settings', '1');
                } else {
                    p.set('view', 'new');
                }
            } else if (this._view === 'entries') {
                p.set('view', 'entries');
                if (this._viewFormId > 0) p.set('form', String(this._viewFormId));
                if (this._search) p.set('q', this._search);
                if (this._dateFrom) p.set('from', this._dateFrom);
                if (this._dateTo) p.set('to', this._dateTo);
                if (this._rangeMode) p.set('range', this._rangeMode);
                const page = Math.floor((this._entrySkip || 0) / 20) + 1;
                if (page > 1) p.set('page', String(page));
            } else if (this._view === 'list') {
                if (this._listSearch) p.set('q', this._listSearch);
            }
            const url = new URL(window.location.href);
            url.search = p.toString();
            window.history.replaceState(window.history.state, '', url.toString());
        } catch { /* address bar sync is best-effort; never break the UI */ }
    }

    async _restoreFromUrl() {
        try {
            const p = new URLSearchParams(window.location.search);
            const view = p.get('view');
            const formId = parseInt(p.get('form') || '0', 10);
            const settings = p.get('settings') === '1';
            // Ignore stale links pointing at a form that no longer exists.
            if (formId > 0 && !this._forms.some(f => f.id === formId)) return;
            if (view === 'new' && this._permissions?.canEdit) {
                this._newForm();
            } else if (formId > 0) {
                if (view === 'entries' || !this._permissions?.canEdit) {
                    const page = parseInt(p.get('page') || '1', 10);
                    await this._viewEntries(formId, {
                        search: p.get('q') || '',
                        from: p.get('from') || '',
                        to: p.get('to') || '',
                        range: p.get('range') || '',
                        skip: (!isNaN(page) && page > 1) ? (page - 1) * 20 : 0,
                    });
                } else if (settings) {
                    await this._editSettings(formId);
                } else {
                    await this._editExisting(formId);
                }
            } else if (p.get('q')) {
                // List view search restored from a shared link.
                this._listSearch = p.get('q');
            }
            // else: stay on list
        } catch { /* bad/stale URL → just stay on the list */ }
    }

    // Toggle the editor's Settings panel and keep the URL in sync.
    _toggleColumnSettings() {
        this._showColumnSettings = !this._showColumnSettings;
        this._syncUrl();
        this.requestUpdate();
    }
};
