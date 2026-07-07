// Form-builder layout actions: groups → columns → fields → options.
// Pure structural mutations on `this._editForm` followed by requestUpdate().

const newId = () => crypto.randomUUID?.() || Date.now().toString(36);

export const BuilderMixin = (Base) => class extends Base {

    // ── Group management ──
    _addGroup() {
        const f = this._editForm;
        if (!f.groups) f.groups = [];
        const idx = f.groups.length;
        f.groups = [...f.groups, {
            id: newId(),
            name: '',
            cssClass: '',
            columns: [{ id: newId(), width: 12, fields: [] }],
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
        g.columns = [...g.columns, { id: newId(), width: 6, fields: [] }];
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
            id: newId(),
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

    // ── Field settings dialog open/close ──
    // Snapshot the field on open so Cancel can revert any edits made inside the dialog.
    _openFieldSettings(loc, fIdx) {
        const field = this._editForm?.groups?.[loc.gIdx]?.columns?.[loc.cIdx]?.fields?.[fIdx];
        this._fieldSettingsSnapshot = field ? JSON.parse(JSON.stringify(field)) : null;
        this._fieldSettingsLoc = { ...loc, fIdx };
        this.requestUpdate();
    }

    // Keep edits (Save button) — snapshot is discarded.
    _closeFieldSettings() {
        this._fieldSettingsSnapshot = null;
        this._fieldSettingsLoc = null;
        this.requestUpdate();
    }

    // Revert edits made in the dialog by restoring the snapshot (Cancel button).
    _cancelFieldSettings() {
        const loc = this._fieldSettingsLoc;
        if (loc && this._fieldSettingsSnapshot) {
            const col = this._editForm?.groups?.[loc.gIdx]?.columns?.[loc.cIdx];
            if (col?.fields?.[loc.fIdx]) col.fields[loc.fIdx] = this._fieldSettingsSnapshot;
        }
        this._fieldSettingsSnapshot = null;
        this._fieldSettingsLoc = null;
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

    /**
     * Move a field from one location to another.
     * @param {object} from - { gIdx, cIdx, fIdx } source
     * @param {object} to   - { gIdx, cIdx } destination
     */
    _moveFieldTo(from, to) {
        const f = this._editForm;
        const field = f.groups[from.gIdx].columns[from.cIdx].fields.splice(from.fIdx, 1)[0];
        if (!field) return;
        const destCol = f.groups[to.gIdx].columns[to.cIdx];
        field.sortOrder = destCol.fields.length;
        destCol.fields.push(field);
        this.requestUpdate();
    }
};
