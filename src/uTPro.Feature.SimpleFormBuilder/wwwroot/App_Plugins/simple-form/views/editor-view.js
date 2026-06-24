import { html, nothing } from '@umbraco-cms/backoffice/external/lit';

// ── Main editor ──
export function renderEditor(host) {
    const f = host._editForm;
    if (!f) return nothing;
    const showSettings = host._showColumnSettings;
    if (!f.groups) f.groups = [];

    return html`
        <uui-box>
            <div class="toolbar">
                <uui-button look="outline" @click=${() => { host._view = 'list'; host._showColumnSettings = false; }}>&#8592; Back</uui-button>
                <h2>${f.id ? 'Edit' : 'New'} Form</h2>
                <div class="toolbar-right">
                    ${f.id ? html`
                        <uui-button look="outline" compact @click=${() => host._viewEntries(f.id)}>Entries (${host._entryCount ?? 0})</uui-button>
                        <uui-button look="${showSettings ? 'primary' : 'outline'}" compact
                            @click=${() => { host._showColumnSettings = !host._showColumnSettings; host.requestUpdate(); }}>&#9881; Settings</uui-button>
                    ` : nothing}
                    <uui-button look="primary" @click=${() => host._saveForm()}>Save Form</uui-button>
                </div>
            </div>
            ${showSettings && f.id ? html`
                ${_renderEmbedSettings(host, f)}
                ${_renderGeneralSettings(host, f)}
                ${_renderColumnSettings(host, f)}
            ` : nothing}
            ${!f.id ? _renderGeneralSettings(host, f) : nothing}
            <div class="section-header">
                <h3>Groups</h3>
                <uui-button look="primary" compact @click=${() => host._addGroup()}>+ Add Group</uui-button>
            </div>
            ${f.groups.length === 0 ? html`<div class="empty">No groups yet. Add a group to organise fields.</div>` : nothing}
            ${f.groups.map((group, gIdx) => _renderGroupCard(host, group, gIdx))}
        </uui-box>
        ${host._typePickerIdx >= 0 ? _renderTypePicker(host) : nothing}
        ${host._fieldSettingsLoc ? _renderFieldSettingsDialog(host) : nothing}`;
}

// ── Group card ──
function _renderGroupCard(host, group, gIdx) {
    const f = host._editForm;
    if (!group.columns) group.columns = [];
    const totalWidth = group.columns.reduce((sum, c) => sum + (c.width || 1), 0);
    return html`
        <div class="group-card">
            <div class="group-header">
                <div>
                    <span class="group-num">Group #${gIdx + 1}</span>
                    <br>
                    <span class="group-preview-label">
                        ${group.columns.length} column${group.columns.length !== 1 ? 's' : ''} · ${totalWidth}/12
                        ${totalWidth > 12 ? html`<br><span style="color:#c0392b;">⚠ exceeds 12!</span>` : nothing}
                    </span>
                </div>
                <div class="group-settings">
                    <label class="group-setting-label">Name
                        <uui-input .value=${group.name || ''} placeholder="(optional)" @input=${(e) => host._updateGroup(gIdx, 'name', e.target.value)}></uui-input>
                    </label>
                    <label class="group-setting-label">CSS Class
                        <uui-input .value=${group.cssClass || ''} placeholder="(optional)" @input=${(e) => host._updateGroup(gIdx, 'cssClass', e.target.value)}></uui-input>
                    </label>
                    <uui-button look="outline" compact style="margin-top:8px;" @click=${() => host._addColumn(gIdx)}>+ Add Column</uui-button>
                </div>
                <div class="group-actions">
                    <uui-button look="outline" compact @click=${() => host._moveGroup(gIdx, -1)} ?disabled=${gIdx === 0}>&#9650;</uui-button>
                    <uui-button look="outline" compact @click=${() => host._moveGroup(gIdx, 1)} ?disabled=${gIdx === f.groups.length - 1}>&#9660;</uui-button>
                    <uui-button look="outline" color="danger" compact @click=${() => host._removeGroup(gIdx)}>&#128465;</uui-button>
                </div>
            </div>
            <div class="group-preview">
                
            </div>
            <div class="group-columns-container">
                ${group.columns.map((col, cIdx) => _renderColumnCard(host, col, gIdx, cIdx, group.columns.length))}
            </div>
        </div>`;
}

// ── Column card (draggable) ──
function _renderColumnCard(host, col, gIdx, cIdx, totalCols) {
    const widthPct = ((col.width || 12) / 12 * 100).toFixed(2);
    return html`
        <div class="col-card" style="width: calc(${widthPct}% - 12px);"
            draggable="true"
            @dragstart=${(e) => { e.dataTransfer.setData('application/col-drag', JSON.stringify({ gIdx, cIdx })); e.dataTransfer.effectAllowed = 'move'; e.currentTarget.classList.add('col-dragging'); }}
            @dragend=${(e) => { e.currentTarget.classList.remove('col-dragging'); }}
            @dragover=${(e) => { if (e.dataTransfer.types.includes('application/col-drag')) { e.preventDefault(); e.dataTransfer.dropEffect = 'move'; e.currentTarget.classList.add('col-drag-over'); } }}
            @dragleave=${(e) => { e.currentTarget.classList.remove('col-drag-over'); }}
            @drop=${(e) => {
            e.preventDefault(); e.currentTarget.classList.remove('col-drag-over');
            try {
                const from = JSON.parse(e.dataTransfer.getData('application/col-drag'));
                if (from.gIdx === gIdx && from.cIdx !== cIdx) {
                    const cols = host._editForm.groups[gIdx].columns; const [moved] = cols.splice(from.cIdx, 1); cols.splice(cIdx, 0, moved); host.requestUpdate();
                } else if (from.gIdx !== gIdx) { host._moveColumnTo(from.gIdx, from.cIdx, gIdx); }
            } catch { }
        }}>
            <div class="col-header">
                <span class="col-drag-handle" title="Drag to reorder">&#9776;</span>
                <span class="col-num">Col ${cIdx + 1}</span>
                <div class="col-actions">
                    ${totalCols > 1 ? html`<uui-button look="outline" color="danger" compact @click=${() => host._removeColumn(gIdx, cIdx)}>&#10005;</uui-button>` : nothing}
                </div>
            </div>
            <div class="col-fields">
                <div class="col-actions">
                    <label class="col-width-label">
                        <uui-input type="number" .value=${String(col.width || 12)} min="1" max="12"
                            @input=${(e) => host._updateColumnWidth(gIdx, cIdx, e.target.value)}></uui-input>
                        ${_renderColMoveToSelect(host, gIdx, cIdx)}
                    </label>
                </div>
                <hr />
                ${col.fields.map((field, fIdx) => _renderFieldCompact(host, field, fIdx, { gIdx, cIdx }))}
                <div class="col-add-field">
                    <uui-button look="outline" compact @click=${() => host._addFieldToColumn(gIdx, cIdx)}>+ Add Field</uui-button>
                </div>
            </div>
        </div>`;
}

// ── Compact field card (summary only) ──
function _renderFieldCompact(host, field, fIdx, loc) {
    const typeLabel = host._fieldTypes.find(ft => ft.type === field.type)?.label || field.type;
    const label = field.label || field.name || '(no label)';
    const totalFields = host._editForm.groups[loc.gIdx].columns[loc.cIdx].fields.length;

    return html`
        <div class="fc${field.isHidden ? ' fc-hidden' : ''}"
            draggable="true"
            @dblclick=${() => { host._fieldSettingsLoc = { ...loc, fIdx }; host.requestUpdate(); }}
            @dragstart=${(e) => { e.dataTransfer.setData('application/field-drag', JSON.stringify({ ...loc, fIdx })); e.dataTransfer.effectAllowed = 'move'; e.currentTarget.classList.add('fc-dragging'); }}
            @dragend=${(e) => { e.currentTarget.classList.remove('fc-dragging'); }}
            @dragover=${(e) => { if (e.dataTransfer.types.includes('application/field-drag')) { e.preventDefault(); e.dataTransfer.dropEffect = 'move'; e.currentTarget.classList.add('fc-drag-over'); } }}
            @dragleave=${(e) => { e.currentTarget.classList.remove('fc-drag-over'); }}
            @drop=${(e) => {
            e.preventDefault(); e.currentTarget.classList.remove('fc-drag-over');
            try {
                const from = JSON.parse(e.dataTransfer.getData('application/field-drag'));
                if (from.gIdx === loc.gIdx && from.cIdx === loc.cIdx && from.fIdx !== fIdx) {
                    const arr = host._editForm.groups[loc.gIdx].columns[loc.cIdx].fields;
                    const [moved] = arr.splice(from.fIdx, 1); arr.splice(fIdx, 0, moved);
                    arr.forEach((f, i) => f.sortOrder = i); host.requestUpdate();
                } else if (from.gIdx !== loc.gIdx || from.cIdx !== loc.cIdx) {
                    const srcArr = host._editForm.groups[from.gIdx].columns[from.cIdx].fields;
                    const [moved] = srcArr.splice(from.fIdx, 1);
                    const destArr = host._editForm.groups[loc.gIdx].columns[loc.cIdx].fields;
                    moved.sortOrder = fIdx; destArr.splice(fIdx, 0, moved);
                    destArr.forEach((f, i) => f.sortOrder = i); host.requestUpdate();
                }
            } catch { }
        }}>
            <span class="fc-label">${label} ${field.required ? html`<span class="fc-req" title="Required">*</span>` : nothing}</span>
            
            <div class="fc-actions">
                <button class="fc-btn" title="Settings" @click=${() => { host._fieldSettingsLoc = { ...loc, fIdx }; host.requestUpdate(); }}>&#9881;</button>
                <button class="fc-btn" title="Move up" ?disabled=${fIdx === 0} @click=${() => host._moveFieldInColumn(loc.gIdx, loc.cIdx, fIdx, -1)}>&#9650;</button>
                <button class="fc-btn" title="Move down" ?disabled=${fIdx === totalFields - 1} @click=${() => host._moveFieldInColumn(loc.gIdx, loc.cIdx, fIdx, 1)}>&#9660;</button>
                <button class="fc-btn fc-btn-danger" title="Remove" @click=${() => host._removeFieldFromColumn(loc.gIdx, loc.cIdx, fIdx)}>&#128465;</button>
            </div>
        </div>`;
}

// ── Field settings dialog ──
function _renderFieldSettingsDialog(host) {
    const loc = host._fieldSettingsLoc;
    if (!loc) return nothing;
    const field = host._editForm.groups?.[loc.gIdx]?.columns?.[loc.cIdx]?.fields?.[loc.fIdx];
    if (!field) return nothing;

    const needsOptions = ['select', 'radio', 'checkbox'].includes(field.type);
    const currentTypeLabel = host._fieldTypes.find(ft => ft.type === field.type)?.label || field.type;
    const updateFn = (key, val) => { host._updateFieldInColumn(loc.gIdx, loc.cIdx, loc.fIdx, key, val); };
    const close = () => { host._fieldSettingsLoc = null; host.requestUpdate(); };

    return html`
        <div class="overlay" @click=${(e) => { if (e.target === e.currentTarget) close(); }}>
            <div class="field-dialog">
                <div class="field-dialog-header">
                    <h3>Field Settings — ${field.label || field.name || '(untitled)'}</h3>
                    <uui-button look="secondary" compact @click=${close}>&#10005;</uui-button>
                </div>
                <div class="field-dialog-body">
                    <div class="fd-grid">
                        <!-- Type -->
                        <label>Type
                        <uui-button look="outline" compact @click=${() => {
            host._typePickerIdx = loc.fIdx; host._typePickerGroupIdx = loc.gIdx;
            host._typePickerColIdx = loc.cIdx; host._typePickerSearch = ''; host.requestUpdate();
        }}>${currentTypeLabel}</uui-button>
                        </label>

                        <!-- Move to -->
                        ${_renderMoveToInDialog(host, loc)}
                    </div>

                    ${field.type !== 'div' && field.type !== 'step' ? html`
                    <!-- Core fields -->
                    <div class="fd-grid">
                        <label>Label <uui-input .value=${field.label} @input=${(e) => updateFn('label', e.target.value)}></uui-input></label>
                        <label>Name <uui-input .value=${field.name} @input=${(e) => updateFn('name', e.target.value)}></uui-input></label>
                        <label>Placeholder <uui-input .value=${field.placeholder || ''} @input=${(e) => updateFn('placeholder', e.target.value)}></uui-input></label>
                        <label>CSS Class <uui-input .value=${field.cssClass || ''} @input=${(e) => updateFn('cssClass', e.target.value)}></uui-input></label>
                        <label>Default Value <uui-input .value=${field.defaultValue || ''} @input=${(e) => updateFn('defaultValue', e.target.value)}></uui-input></label>
                        <label>Validation Regex <uui-input .value=${field.validation || ''} @input=${(e) => updateFn('validation', e.target.value)}></uui-input></label>
                        <label>Validation Message <uui-input .value=${field.validationMessage || ''} @input=${(e) => updateFn('validationMessage', e.target.value)}></uui-input></label>
                    </div>
                    ` : html`
                    <!-- div/step content -->
                    <div class="fd-grid">
                        <label>CSS Class <uui-input .value=${field.cssClass || ''} @input=${(e) => updateFn('cssClass', e.target.value)}></uui-input></label>
                    </div>
                    <label style="display:block;margin-top:8px;">Content (HTML)
                        <textarea class="fd-html-textarea" .value=${field.defaultValue || ''}
                            @input=${(e) => updateFn('defaultValue', e.target.value)}></textarea>
                    </label>
                    `}

                    <!-- Type-specific attributes -->
                    ${_renderTypeAttributes(host, field, loc.fIdx, loc)}

                    <!-- Options -->
                    ${needsOptions ? html`
                        <div class="fd-options">
                            <div class="section-header"><span>Options</span>
                                <uui-button look="outline" compact @click=${() => host._addOptionInColumn(loc.gIdx, loc.cIdx, loc.fIdx)}>+ Option</uui-button>
                            </div>
                            ${(field.options || []).map((opt, oIdx) => html`
                                <div class="option-row">
                                    <uui-input placeholder="Text" .value=${opt.text} @input=${(e) => { opt.text = e.target.value; host.requestUpdate(); }}></uui-input>
                                    <uui-input placeholder="Value" .value=${opt.value} @input=${(e) => { opt.value = e.target.value; host.requestUpdate(); }}></uui-input>
                                    <uui-button look="outline" color="danger" compact @click=${() => host._removeOptionInColumn(loc.gIdx, loc.cIdx, loc.fIdx, oIdx)}>&#10005;</uui-button>
                                </div>`)}
                        </div>
                    ` : nothing}

                </div>
                <div class="field-dialog-footer">
                    <!-- Toggles -->
                    <div class="fd-toggles">
                        <uui-toggle ?checked=${!field.isHidden} @change=${(e) => updateFn('isHidden', !e.target.checked)} label="Visible"></uui-toggle>
                        <uui-toggle ?checked=${field.required} @change=${(e) => updateFn('required', e.target.checked)} label="Required"></uui-toggle>
                        <uui-toggle ?checked=${field.isSensitive || field.type === 'password'} @change=${(e) => updateFn('isSensitive', e.target.checked)} label="Sensitive Data"></uui-toggle>
                        <uui-button look="primary" @click=${close}>Done</uui-button>
                    </div>
                </div>
            </div>
        </div>`;
}

function _renderMoveToInDialog(host, loc) {
    const f = host._editForm;
    const destinations = [];
    (f.groups || []).forEach((g, gi) => {
        (g.columns || []).forEach((c, ci) => {
            if (gi === loc.gIdx && ci === loc.cIdx) return;
            const gName = g.name || `Group #${gi + 1}`;
            const colLabel = g.columns.length > 1 ? ` / Col ${ci + 1}` : '';
            destinations.push({ label: `${gName}${colLabel}`, gIdx: gi, cIdx: ci });
        });
    });
    if (destinations.length === 0) return nothing;
    return html`
        <label>Move to
            <select class="move-to-select" @change=${(e) => {
            const val = e.target.value; if (!val) return;
            const [tg, tc] = val.split(',').map(Number);
            host._moveFieldTo({ gIdx: loc.gIdx, cIdx: loc.cIdx, fIdx: loc.fIdx }, { gIdx: tg, cIdx: tc });
            host._fieldSettingsLoc = null; host.requestUpdate();
        }}>
                <option value="">Select destination…</option>
                ${destinations.map(d => html`<option value="${d.gIdx},${d.cIdx}">→ ${d.label}</option>`)}
            </select>
        </label>`;
}

// ── Shared helpers ──

function _renderEmbedSettings(host, f) {
    return html`
        <div class="settings-panel">
            <div class="settings-header"><h3>Embed API Settings</h3></div>
            <div class="embed-render-row"><code>POST /api/utpro/simple-form/submit { "alias": "${f.alias}", "data": { ... } }</code></div>
            <div class="embed-render-row">
                <uui-toggle ?checked=${f.enableRenderApi} @change=${(e) => { f.enableRenderApi = e.target.checked; host.requestUpdate(); }} label=${f.enableRenderApi ? 'Enabled' : 'Disabled'}></uui-toggle>
                <code>GET /api/utpro/simple-form/render/${f.alias}</code>
            </div>
            <div class="embed-render-row">
                <uui-toggle ?checked=${f.enableEntriesApi} @change=${(e) => { f.enableEntriesApi = e.target.checked; host.requestUpdate(); }} label=${f.enableEntriesApi ? 'Enabled' : 'Disabled'}></uui-toggle>
                <code>GET /api/utpro/simple-form/entries/${f.alias}</code>
            </div>
        </div>`;
}

function _renderGeneralSettings(host, f) {
    return html`
        <div class="settings-panel">
            <div class="settings-header">
                <h3>General Settings
                    <label class="check-label">
                        <uui-toggle ?checked=${f.storeEntries} @change=${(e) => { f.storeEntries = e.target.checked; }} label="Store Entries"></uui-toggle>
                        <uui-toggle ?checked=${f.isEnabled} @change=${(e) => { f.isEnabled = e.target.checked; }} label="Enabled"></uui-toggle>
                    </label>
                </h3>
            </div>
            <div class="form-grid">
                <label>Name <uui-input .value=${f.name} @input=${(e) => { f.name = e.target.value; }}></uui-input></label>
                <label>Alias <uui-input .value=${f.alias} @input=${(e) => { f.alias = e.target.value; }}></uui-input></label>
                <label>Success Message <uui-input .value=${f.successMessage || ''} @input=${(e) => { f.successMessage = e.target.value; }}></uui-input></label>
                <label>Redirect URL <uui-input .value=${f.redirectUrl || ''} @input=${(e) => { f.redirectUrl = e.target.value; }}></uui-input></label>
                <label>Email To <uui-input .value=${f.emailTo || ''} @input=${(e) => { f.emailTo = e.target.value; }}></uui-input></label>
                <label>Email Subject <uui-input .value=${f.emailSubject || ''} @input=${(e) => { f.emailSubject = e.target.value; }}></uui-input></label>
            </div>
        </div>`;
}

function _renderColumnSettings(host, f) {
    const allFieldNames = (f.groups || []).flatMap(g => (g.columns || []).flatMap(c => c.fields.map(field => field.name).filter(n => n)));
    let orderedNames;
    if (f.visibleColumns && f.visibleColumns.length > 0) {
        const checked = f.visibleColumns.filter(n => allFieldNames.includes(n));
        const unchecked = allFieldNames.filter(n => !f.visibleColumns.includes(n));
        orderedNames = [...checked, ...unchecked];
    } else { orderedNames = [...allFieldNames]; }
    return html`
        <div class="settings-panel">
            <div class="settings-header"><h3>&#9881; Entries Column Settings</h3><span class="settings-hint">Drag to reorder.</span></div>
            <div class="settings-body">
                ${allFieldNames.length === 0 ? html`<div class="empty">No fields yet.</div>` : nothing}
                ${orderedNames.map((name, idx) => {
        const isVisible = f.visibleColumns == null ? true : f.visibleColumns.includes(name);
        return html`<label class="check-label settings-col-item" draggable="true"
                        @dragstart=${(e) => { e.dataTransfer.setData('text/plain', idx.toString()); e.currentTarget.classList.add('dragging'); }}
                        @dragend=${(e) => { e.currentTarget.classList.remove('dragging'); }}
                        @dragover=${(e) => { e.preventDefault(); e.currentTarget.classList.add('drag-over'); }}
                        @dragleave=${(e) => { e.currentTarget.classList.remove('drag-over'); }}
                        @drop=${(e) => {
                e.preventDefault(); e.currentTarget.classList.remove('drag-over');
                const from = parseInt(e.dataTransfer.getData('text/plain')); if (from === idx) return;
                if (!f.visibleColumns) f.visibleColumns = [...allFieldNames];
                const arr = [...orderedNames]; const [m] = arr.splice(from, 1); arr.splice(idx, 0, m);
                f.visibleColumns = arr.filter(n => f.visibleColumns.includes(n)); host.requestUpdate();
            }}>
                        <span class="drag-handle">&#9776;</span>
                        <input type="checkbox" ?checked=${isVisible} @change=${(e) => {
                if (!f.visibleColumns) f.visibleColumns = [...allFieldNames];
                if (e.target.checked) { if (!f.visibleColumns.includes(name)) f.visibleColumns.push(name); }
                else { f.visibleColumns = f.visibleColumns.filter(c => c !== name); } host.requestUpdate();
            }} />
                        ${name}</label>`;
    })}
            </div>
        </div>`;
}

function _renderTypePicker(host) {
    const search = (host._typePickerSearch || '').toLowerCase();
    const filtered = host._fieldTypes.filter(ft => ft.label.toLowerCase().includes(search) || ft.type.toLowerCase().includes(search));
    const idx = host._typePickerIdx, gIdx = host._typePickerGroupIdx, cIdx = host._typePickerColIdx ?? -1;
    let currentType;
    if (gIdx >= 0 && cIdx >= 0) currentType = host._editForm?.groups?.[gIdx]?.columns?.[cIdx]?.fields?.[idx]?.type;
    return html`
        <div class="overlay overlay-top" @click=${(e) => { if (e.target === e.currentTarget) { host._typePickerIdx = -1; host.requestUpdate(); } }}>
            <div class="type-picker-dialog">
                <div class="type-picker-header"><h3>Select Field Type</h3>
                    <uui-button look="secondary" compact @click=${() => { host._typePickerIdx = -1; host.requestUpdate(); }}>&#10005;</uui-button></div>
                <div class="type-picker-search"><uui-input placeholder="Search..." .value=${host._typePickerSearch || ''} @input=${(e) => { host._typePickerSearch = e.target.value; host.requestUpdate(); }}></uui-input></div>
                <div class="type-picker-list">
                    ${filtered.map(ft => html`<button class="type-picker-option ${ft.type === currentType ? 'active' : ''}"
                        @click=${() => { host._updateFieldInColumn(gIdx, cIdx, idx, 'type', ft.type); host._typePickerIdx = -1; host.requestUpdate(); }}>
                        <span class="type-picker-label">${ft.label}</span><span class="type-picker-type">${ft.type}</span></button>`)}
                    ${filtered.length === 0 ? html`<div class="type-picker-empty">No matching types</div>` : nothing}
                </div>
            </div>
        </div>`;
}

function _renderTypeAttributes(host, field, idx, loc) {
    const t = field.type; if (!field.attributes) field.attributes = {};
    const a = field.attributes, s = (k, v) => { field.attributes[k] = v; host.requestUpdate(); };
    if (t === 'number') return html`<div class="field-attrs"><label>Min <uui-input .value=${a.min || ''} @input=${(e) => s('min', e.target.value)}></uui-input></label><label>Max <uui-input .value=${a.max || ''} @input=${(e) => s('max', e.target.value)}></uui-input></label><label>Step <uui-input .value=${a.step || ''} @input=${(e) => s('step', e.target.value)}></uui-input></label></div>`;
    if (t === 'date') return html`<div class="field-attrs"><label>Min <uui-input type="date" .value=${a.min || ''} @input=${(e) => s('min', e.target.value)}></uui-input></label><label>Max <uui-input type="date" .value=${a.max || ''} @input=${(e) => s('max', e.target.value)}></uui-input></label></div>`;
    if (t === 'time') return html`<div class="field-attrs"><label>Min <uui-input .value=${a.min || ''} @input=${(e) => s('min', e.target.value)} placeholder="09:00"></uui-input></label><label>Max <uui-input .value=${a.max || ''} @input=${(e) => s('max', e.target.value)} placeholder="17:00"></uui-input></label></div>`;
    if (t === 'textarea') return html`<div class="field-attrs"><label>Rows <uui-input .value=${a.rows || '4'} @input=${(e) => s('rows', e.target.value)}></uui-input></label></div>`;
    if (t === 'file') return html`<div class="field-attrs"><label>Accept <uui-input .value=${a.accept || ''} @input=${(e) => s('accept', e.target.value)} placeholder=".pdf,.jpg"></uui-input></label><label>Max MB <uui-input .value=${a.maxSize || ''} @input=${(e) => s('maxSize', e.target.value)}></uui-input></label></div>`;
    if (t === 'range') return html`<div class="field-attrs"><label>Min <uui-input .value=${a.min || '0'} @input=${(e) => s('min', e.target.value)}></uui-input></label><label>Max <uui-input .value=${a.max || '100'} @input=${(e) => s('max', e.target.value)}></uui-input></label><label>Step <uui-input .value=${a.step || '1'} @input=${(e) => s('step', e.target.value)}></uui-input></label></div>`;
    if (t === 'accept') return html`<div class="field-attrs"><label>Text <uui-input .value=${a.text || ''} @input=${(e) => s('text', e.target.value)}></uui-input></label><label>Link URL <uui-input .value=${a.linkUrl || ''} @input=${(e) => s('linkUrl', e.target.value)}></uui-input></label><label>Link Text <uui-input .value=${a.linkText || ''} @input=${(e) => s('linkText', e.target.value)}></uui-input></label></div>`;
    if (t === 'step') return html`<div class="field-attrs"><label>Title <uui-input .value=${a.title || ''} @input=${(e) => s('title', e.target.value)}></uui-input></label></div>`;
    return nothing;
}

function _renderColMoveToSelect(host, gIdx, cIdx) {
    const f = host._editForm;
    if (!f.groups || f.groups.length < 2) return nothing;
    const dests = f.groups.map((g, gi) => ({ label: g.name || `Group #${gi + 1}`, gi })).filter(d => d.gi !== gIdx);
    if (!dests.length) return nothing;
    return html`<select class="move-to-select" title="Move column to another group" @change=${(e) => { const v = e.target.value; if (!v) return; host._moveColumnTo(gIdx, cIdx, parseInt(v)); e.target.value = ''; }}>
        <option value="">Move col…</option>${dests.map(d => html`<option value="${d.gi}">→ ${d.label}</option>`)}</select>`;
}
