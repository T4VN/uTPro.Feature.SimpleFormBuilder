// Copy / paste for groups, columns and fields — backed by the SYSTEM clipboard
// (navigator.clipboard) instead of localStorage.
//
// Why the clipboard?
//  • No persistent local storage footprint.
//  • If the user copies something else (any other text), our payload is gone,
//    so the "Paste" button stops being valid — exactly the desired behaviour.
//
// Reading the clipboard requires a user gesture, so:
//  • Copy writes a tagged JSON payload AND keeps an in-memory ref (`_clip`) so
//    the Paste button can render immediately.
//  • Paste re-reads the live clipboard (the click is a valid gesture); if the
//    content is no longer our payload, it aborts and the button hides.
//  • A best-effort refresh runs when the window regains focus (see index.js).
//
// On paste every `id` is regenerated and field `name`s are de-duped against the
// destination form so submissions never clash.

const CLIP_MARKER = 'uTPro.SimpleForm.clipboard';

const newId = () =>
    crypto.randomUUID?.() ||
    (Date.now().toString(36) + Math.random().toString(36).slice(2));

const clone = (o) => JSON.parse(JSON.stringify(o));

function regenFieldIds(field) {
    field.id = newId();
    return field;
}

function regenColumnIds(col) {
    col.id = newId();
    (col.fields || []).forEach(regenFieldIds);
    return col;
}

function regenGroupIds(group) {
    group.id = newId();
    (group.columns || []).forEach(regenColumnIds);
    return group;
}

// Collect every field name already used in the form.
function existingFieldNames(form) {
    const names = [];
    (form.groups || []).forEach(g =>
        (g.columns || []).forEach(c =>
            (c.fields || []).forEach(f => { if (f.name) names.push(f.name); })));
    return names;
}

// Rename pasted fields whose name collides with an existing one (or with an
// earlier field in the same paste) by appending "_copy", "_copy2", ...
function dedupeFieldNames(existing, fields) {
    const used = new Set(existing);
    fields.forEach(f => {
        if (!f.name) return;
        if (!used.has(f.name)) { used.add(f.name); return; }
        let i = 2;
        let candidate = f.name + '_copy';
        while (used.has(candidate)) { candidate = f.name + '_copy' + i; i++; }
        f.name = candidate;
        used.add(candidate);
    });
}

export const ClipboardMixin = (Base) => class extends Base {

    // ── Clipboard primitives ──

    // Parse clipboard text into our payload, or null if it isn't ours.
    _parseClip(text) {
        try {
            const o = JSON.parse(text);
            if (o && o.__marker === CLIP_MARKER && o.type && o.data) {
                return { type: o.type, data: o.data };
            }
        } catch { /* not JSON / not ours */ }
        return null;
    }

    // Synchronous check used by the render to decide whether to show a Paste
    // button. Reflects the in-memory snapshot (kept in sync with the clipboard).
    _clipPeek(type) {
        return this._clip && this._clip.type === type ? this._clip : null;
    }

    // Read the live system clipboard and update `_clip` (best-effort).
    // Safe to call without a gesture: failures are swallowed and the in-memory
    // snapshot is kept.
    async _refreshClip() {
        try {
            const text = await navigator.clipboard.readText();
            const parsed = this._parseClip(text);
            if (JSON.stringify(parsed) !== JSON.stringify(this._clip ?? null)) {
                this._clip = parsed;
                this.requestUpdate();
            }
        } catch { /* permission/gesture unavailable — keep current snapshot */ }
    }

    async _clipWrite(type, data, label) {
        const snapshot = { type, data: clone(data) };
        this._clip = snapshot; // show Paste immediately
        try {
            await navigator.clipboard.writeText(
                JSON.stringify({ __marker: CLIP_MARKER, type, data: snapshot.data }));
        } catch { /* clipboard unavailable; in-memory copy still works this session */ }
        this._msg?.(label + ' copied');
        this.requestUpdate();
    }

    // Re-read the clipboard at paste time (valid user gesture). Returns the
    // matching payload, or null. On any mismatch / read failure it clears the
    // in-memory snapshot so the Paste buttons hide immediately.
    async _clipTake(type) {
        let parsed = null;
        try {
            const text = await navigator.clipboard.readText();
            parsed = this._parseClip(text);
        } catch { parsed = null; }

        if (!parsed) {
            if (this._clip) { this._clip = null; this.requestUpdate(); }
            return null;
        }
        // Sync the snapshot with what's really on the clipboard.
        if (JSON.stringify(parsed) !== JSON.stringify(this._clip ?? null)) {
            this._clip = parsed;
            this.requestUpdate();
        }
        return parsed.type === type ? parsed : null;
    }

    // ── Group copy / paste ──
    _copyGroup(gIdx) {
        const g = this._editForm?.groups?.[gIdx];
        if (g) this._clipWrite('group', g, 'Group');
    }

    async _pasteGroup() {
        const c = await this._clipTake('group');
        if (!c) { this._msg?.('Clipboard no longer contains a copied group', true); return; }
        const g = regenGroupIds(clone(c.data));
        dedupeFieldNames(existingFieldNames(this._editForm),
            (g.columns || []).flatMap(col => col.fields || []));
        g.sortOrder = this._editForm.groups.length;
        this._editForm.groups = [...this._editForm.groups, g];
        this.requestUpdate();
        this._msg?.('Group pasted');
    }

    // ── Column copy / paste ──
    _copyColumn(gIdx, cIdx) {
        const col = this._editForm?.groups?.[gIdx]?.columns?.[cIdx];
        if (col) this._clipWrite('column', col, 'Column');
    }

    async _pasteColumn(gIdx) {
        const c = await this._clipTake('column');
        if (!c) { this._msg?.('Clipboard no longer contains a copied column', true); return; }
        const group = this._editForm?.groups?.[gIdx];
        if (!group) return;
        const col = regenColumnIds(clone(c.data));
        dedupeFieldNames(existingFieldNames(this._editForm), col.fields || []);
        group.columns = [...group.columns, col];
        this.requestUpdate();
        this._msg?.('Column pasted');
    }

    // ── Field copy / paste ──
    _copyField(gIdx, cIdx, fIdx) {
        const field = this._editForm?.groups?.[gIdx]?.columns?.[cIdx]?.fields?.[fIdx];
        if (field) this._clipWrite('field', field, 'Field');
    }

    async _pasteField(gIdx, cIdx) {
        const c = await this._clipTake('field');
        if (!c) { this._msg?.('Clipboard no longer contains a copied field', true); return; }
        const col = this._editForm?.groups?.[gIdx]?.columns?.[cIdx];
        if (!col) return;
        const field = regenFieldIds(clone(c.data));
        dedupeFieldNames(existingFieldNames(this._editForm), [field]);
        field.sortOrder = col.fields.length;
        col.fields = [...col.fields, field];
        this.requestUpdate();
        this._msg?.('Field pasted');
    }
};
