// Form-level actions: dirty tracking, CRUD navigation, and import/export.
// All methods operate on the host element's reactive state (`this`).

import { API } from '../api.js';
import { formBus } from '../bus.js';

export const FormActionsMixin = (Base) => class extends Base {

    // ── Dirty tracking (edits only persist via Save Form) ──
    _snapshotForm() { this._editFormSnapshot = this._editForm ? JSON.stringify(this._editForm) : ''; }
    _isDirty() {
        return this._view === 'edit'
            && this._editForm != null
            && JSON.stringify(this._editForm) !== this._editFormSnapshot;
    }

    // Single guard for any navigation away from a dirty editor.
    // Returns true when it's safe to proceed (not dirty, or user confirmed).
    // On cancel, re-asserts the current form in the sidebar so its highlight stays put.
    _confirmLeave() {
        if (!this._isDirty()) return true;
        if (confirm('You have unsaved changes. Leave without saving?')) return true;
        formBus.setActive(this._editForm?.id || 0);
        return false;
    }

    // Accept leaving: drop the dirty state so guards stop prompting.
    _discardDirty() {
        this._editFormSnapshot = this._editForm ? JSON.stringify(this._editForm) : '';
    }

    // ── Form CRUD ──
    _newForm() {
        if (!this._confirmLeave()) return;
        this._editForm = {
            id: 0, name: '', alias: '', fields: [], groups: [],
            successMessage: 'Thank you!', redirectUrl: '', emailTo: '', emailSubject: '',
            storeEntries: true, isEnabled: true, showInPicker: true
        };
        this._view = 'edit';
        this._snapshotForm();
        // Sync sidebar (clear active highlight for a brand-new form).
        formBus.requestNew();
        this._syncUrl();
    }

    async _editExisting(id) {
        if (!this._confirmLeave()) return;
        try {
            this._editForm = await this._api(API + '/get', { id });
            this._showColumnSettings = false;
            const res = await this._api(API + '/entries', { formId: id, skip: 0, take: 1 });
            this._entryCount = res.total || 0;
            this._view = 'edit';
            this._snapshotForm();
            // Sync sidebar highlight regardless of where the open was triggered.
            formBus.setActive(id);
            this._syncUrl();
        } catch (e) { this._msg(e.message, true); }
    }

    // Open the editor for a form and immediately reveal the Settings panel
    // (used by the Settings button on the Entries view).
    async _editSettings(id) {
        await this._editExisting(id);
        this._showColumnSettings = true;
        this._syncUrl();
        this.requestUpdate();
    }

    // Return to the form list and clear the sidebar highlight (id 0 = none).
    _backToList() {
        if (!this._confirmLeave()) return;
        this._editFormSnapshot = '';
        this._view = 'list';
        this._showColumnSettings = false;
        this._selectedEntries = [];
        formBus.setActive(0);
        this._syncUrl();
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
            this._snapshotForm();
            this._syncUrl();
            await this._loadForms();
        } catch (e) { this._msg(e.message, true); }
    }

    async _deleteForm(id) {
        if (!confirm('Delete this form and all entries?')) return;
        try {
            await this._api(API + '/delete', { id });
            this._msg('Deleted');
            await this._loadForms();
            if (this._editForm?.id === id) { this._editForm = null; this._view = 'list'; formBus.selectForm(0); this._syncUrl(); }
        } catch (e) { this._msg(e.message, true); }
    }

    // ── Import / Export (edit permission required) ──
    async _exportForm(id) {
        // If exporting the form currently being edited with unsaved changes,
        // offer to save first so the export reflects the latest edits.
        if (this._view === 'edit' && this._editForm?.id === id && this._isDirty()) {
            if (!confirm('You have unsaved changes. Save before exporting?')) return; // Cancel → no export
            await this._saveForm();
            if (this._isDirty()) return; // save failed (e.g. validation) → abort export
        }
        try {
            const model = await this._api(API + '/export', { id });
            const alias = model?.alias || 'form';
            const json = JSON.stringify(model, null, 2);
            const blob = new Blob([json], { type: 'application/json;charset=utf-8;' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${alias}.form.json`;
            a.click();
            URL.revokeObjectURL(url);
            this._msg('Form exported');
        } catch (e) { this._msg(e.message, true); }
    }

    _importForm() {
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
                catch { this._msg('Invalid JSON file', true); return; }
                const res = await this._api(API + '/import', model);
                this._msg(res.message || 'Form imported');
                await this._loadForms();
                if (res.id) { await this._editExisting(res.id); }
            } catch (e) { this._msg(e.message, true); }
        });
        input.click();
    }
};
