import { css } from '@umbraco-cms/backoffice/external/lit';

export const dashboardStyles = css`
    :host { display: block; padding: 20px; }

    /* ── Toolbar ── */
    .toolbar { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; gap: 12px; flex-wrap: wrap; }
    .toolbar h2 { margin: 0; font-size: 1.3rem; }
    .toolbar-right { display: flex; align-items: center; gap: 8px; margin-left: auto; }

    /* ── Messages ── */
    .msg { padding: 8px 14px; border-radius: 4px; margin-bottom: 10px; font-size: 0.9rem; }
    .error { background: #fde8e8; color: #c0392b; }
    .success { background: #e8fde8; color: #27ae60; }

    /* ── States ── */
    .loading { display: flex; justify-content: center; padding: 40px; }
    .empty { text-align: center; padding: 40px; color: #888; font-style: italic; }

    /* ── List view ── */
    .link { color: var(--uui-color-interactive, #1b264f); cursor: pointer; font-weight: 500; text-decoration: none; }
    .link:hover { text-decoration: underline; }
    .badge { padding: 2px 8px; border-radius: 10px; font-size: 0.8rem; font-weight: 500; }
    .badge.on { background: #e8fde8; color: #27ae60; }
    .badge.off { background: #fde8e8; color: #c0392b; }
    .action-cell { display: flex; gap: 4px; }
    code { background: #f0f0f0; padding: 2px 6px; border-radius: 3px; font-size: 0.85rem; }
    uui-table { width: 100%; }

    /* ── Form editor grid ── */
    .form-grid {
        display: grid; grid-template-columns: 1fr 1fr; gap: 12px;
        margin-bottom: 20px; padding: 16px;
        background: var(--uui-color-surface-alt, #f9f9f9); border-radius: 6px;
    }
    .form-grid label { display: flex; flex-direction: column; gap: 4px; font-size: 0.85rem; font-weight: 500; }
    .check-label { flex-direction: row !important; align-items: center; gap: 8px !important; }

    /* ── Section headers ── */
    .section-header { display: flex; justify-content: space-between; align-items: center; margin: 16px 0 8px; }
    .section-header h3 { margin: 0; }

    /* ── Field cards ── */
    .field-card {
        border: 1px solid var(--uui-color-border, #ddd); border-radius: 6px;
        overflow: hidden; margin-bottom: 10px;
    }
    .field-card.field-hidden {
        opacity: 0.5; border-style: dashed;
    }
    .field-header {
        display: flex; align-items: center; gap: 8px; padding: 10px 14px;
        background: var(--uui-color-surface-alt, #f4f4f4); flex-wrap: wrap;
    }
    .field-num { font-weight: 600; color: #888; min-width: 30px; }
    .field-header select, .field-body select {
        padding: 4px 8px; border: 1px solid #ccc; border-radius: 4px;
        font-size: 0.9rem; background: #fff; height: 32px; box-sizing: border-box;
    }
    .field-actions { margin-left: auto; display: flex; gap: 4px; }

    /* ── Type picker button ── */
    .type-picker-btn {
        padding: 4px 12px; border: 1px solid #ccc; border-radius: 4px;
        font-size: 0.9rem; background: #fff; height: 32px; box-sizing: border-box;
        cursor: pointer; display: flex; align-items: center; gap: 6px;
        transition: border-color 0.2s;
    }
    .type-picker-btn:hover { border-color: #888; }

    /* ── Type picker dialog ── */
    .type-picker-dialog {
        background: var(--uui-color-surface, #fff); border-radius: 8px;
        width: 400px; max-width: 90vw; height: 480px; display: flex; flex-direction: column;
        box-shadow: 0 8px 32px rgba(0,0,0,0.3);
    }
    .type-picker-header {
        display: flex; justify-content: space-between; align-items: center;
        padding: 14px 18px; border-bottom: 1px solid #e0e0e0;
    }
    .type-picker-header h3 { margin: 0; font-size: 1rem; }
    .type-picker-search { padding: 12px 18px; border-bottom: 1px solid #f0f0f0; }
    .type-picker-search uui-input { width: 100%; }
    .type-picker-list { overflow-y: auto; flex: 1; padding: 6px 0; }
    .type-picker-option {
        display: flex; align-items: center; justify-content: space-between;
        width: 100%; padding: 7px 18px; border: none; background: none;
        cursor: pointer; font-size: 0.85rem; text-align: left;
        transition: background 0.1s;
    }
    .type-picker-option:hover { background: var(--uui-color-surface-alt, #f4f4f4); }
    .type-picker-option.active {
        background: var(--uui-color-surface-alt, #f3f3f5);
        font-weight: 600;
        border-left: 3px solid var(--uui-color-interactive, #1b264f);
    }
    .type-picker-label { flex: 1; }
    .type-picker-type { color: #999; font-size: 0.8rem; font-family: monospace; }
    .type-picker-empty { padding: 20px; text-align: center; color: #888; font-style: italic; }
    .field-body {
        display: grid; grid-template-columns: 1fr 1fr; gap: 10px; padding: 14px;
    }
    .field-body label { display: flex; flex-direction: column; gap: 4px; font-size: 0.8rem; font-weight: 500; min-width: 0; overflow: hidden; }
    .field-body uui-input { width: 100%; min-width: 0; }
    .field-body select { width: 100%; }

    /* ── Options (select/radio/checkbox) ── */
    .options-section { padding: 0 14px 14px; }

    /* ── Type-specific attributes ── */
    .field-attrs {
        display: flex; gap: 10px; padding: 8px 14px;
        background: var(--uui-color-surface-alt, #fafafa);
        border-top: 1px solid #eee; flex-wrap: wrap;
    }
    .field-attrs label {
        display: flex; flex-direction: column; gap: 4px;
        font-size: 0.8rem; font-weight: 500; flex: 1; min-width: 120px;
    }
    .field-attrs uui-input { width: 100%; }
    .div-content-label { grid-column: 1 / -1; }
    .div-content-editor {
        width: 100%; padding: 10px; border: 1px solid #ccc; border-radius: 4px;
        font-family: monospace; font-size: 0.85rem; resize: vertical;
        background: #fff; box-sizing: border-box;
    }

    /* ── Settings panel ── */
    .settings-panel {
        margin-bottom: 16px; padding: 16px;
        background: var(--uui-color-surface-alt, #f0f4ff);
        border: 1px solid #c8d6f0; border-radius: 6px;
    }
    .settings-header { margin-bottom: 12px; }
    .settings-header h3 { margin: 0 0 4px; font-size: 1rem; }
    .settings-hint { font-size: 0.8rem; color: #888; }
    .settings-body {
        display: flex; flex-wrap: wrap; gap: 10px 20px;
    }
    .settings-col-item {
        min-width: 140px; padding: 4px 8px;
        background: #fff; border: 1px solid #e0e0e0; border-radius: 4px;
        cursor: grab; user-select: none; transition: box-shadow 0.15s, opacity 0.15s;
    }
    .settings-col-item:active { cursor: grabbing; }
    .settings-col-item.dragging { opacity: 0.4; }
    .settings-col-item.drag-over { box-shadow: inset 0 0 0 2px var(--uui-color-interactive, #1b264f); }
    .drag-handle { color: #aaa; margin-right: 4px; font-size: 0.8rem; }
    .option-row { display: flex; gap: 8px; margin-bottom: 6px; align-items: center; }
    .option-row uui-input { flex: 1; }

    /* ── Embed info ── */
    .embed-render-row {
        display: flex; align-items: center; gap: 12px; margin: 6px 0;
    }
    .embed-render-row code {
        flex: 1; display: inline-block; padding: 6px 10px;
        background: #fff; border: 1px solid #e0e0e0; border-radius: 4px;
        font-size: 0.85rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
    }

    /* ── Entries ── */
    .filter-bar {
        display: flex; align-items: center; gap: 12px; margin-bottom: 16px;
        padding: 10px 14px; background: var(--uui-color-surface-alt, #f9f9f9);
        border-radius: 6px; flex-wrap: wrap;
    }
    .filter-search { flex: 1; min-width: 200px; }
    .filter-dates { display: flex; align-items: center; gap: 10px; margin-left: auto; }
    .filter-date-label {
        display: flex; align-items: center; gap: 4px; font-size: 0.85rem; font-weight: 500;
    }
    .filter-date-label input[type="date"] {
        padding: 4px 8px; border: 1px solid #ccc; border-radius: 4px;
        font-size: 0.85rem; background: #fff; height: 32px; box-sizing: border-box;
    }
    .pagination { display: flex; justify-content: center; align-items: center; gap: 12px; margin-top: 16px; }
    .page-info { color: #888; font-size: 0.9rem; }
    .cell-truncate { max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .row-selected { background: var(--uui-color-surface-alt, #f0f4ff) !important; }

    /* ── Detail overlay ── */
    .overlay {
        position: fixed; top: 0; left: 0; right: 0; bottom: 0;
        background: rgba(0,0,0,0.5); z-index: 9999;
        display: flex; justify-content: center; align-items: center;
    }
    .overlay.overlay-top { z-index: 10001; }
    .detail-panel {
        background: var(--uui-color-surface, #fff); border-radius: 8px;
        width: 600px; max-width: 90vw; max-height: 80vh; display: flex; flex-direction: column;
        box-shadow: 0 8px 32px rgba(0,0,0,0.3);
    }
    .detail-header {
        display: flex; justify-content: space-between; align-items: center;
        padding: 16px 20px; border-bottom: 1px solid #e0e0e0;
    }
    .detail-header h3 { margin: 0; }
    .detail-body { padding: 20px; overflow-y: auto; flex: 1; }
    .detail-row {
        display: flex; padding: 10px 0; border-bottom: 1px solid #f0f0f0;
    }
    .detail-label { font-weight: 600; min-width: 120px; color: #555; font-size: 0.9rem; }
    .detail-value { flex: 1; word-break: break-word; white-space: pre-wrap; }
    .detail-footer { padding: 12px 20px; border-top: 1px solid #e0e0e0; display: flex; justify-content: flex-end; }

    /* ── Group cards ── */
    .group-card {
        border: 2px solid var(--uui-color-border, #ccc); border-radius: 8px;
        margin-bottom: 16px; overflow: hidden;
        background: var(--uui-color-surface, #fff);
    }
    .group-header {
        display: flex; align-items: center; gap: 12px; padding: 12px 16px;
        background: var(--uui-color-surface-alt, #f0f0f5); flex-wrap: wrap;
    }
    .group-num { font-weight: 700; font-size: 1rem; color: #555; min-width: 80px; }
    .group-settings { display: flex; gap: 10px; flex: 1; flex-wrap: wrap; align-items: flex-end; }
    .group-setting-label {
        display: flex; flex-direction: column; gap: 3px;
        font-size: 0.8rem; font-weight: 500; min-width: 100px;
    }
    .group-setting-label uui-input { width: 100%; min-width: 80px; }
    .group-actions { display: flex; gap: 4px; margin-left: auto; }
    .group-preview {
        padding: 10px 16px; border-bottom: 1px solid #eee;
        background: var(--uui-color-surface-alt, #fafafa);
    }
    .group-preview-label { font-size: 0.8rem; color: #666; margin-bottom: 6px; display: block; }
    .group-col-preview { min-height: 32px; }
    .group-col-cell {
        background: #e0e0e0; border-radius: 4px; padding: 6px 10px;
        text-align: center; font-size: 0.8rem; font-weight: 600; color: #555;
    }
    .group-grid-empty {
        grid-column: 1 / -1; text-align: center; color: #aaa;
        font-size: 0.8rem; font-style: italic; padding: 4px;
    }
    .group-columns-container {
        display: flex; gap: 12px; padding: 12px 16px; flex-wrap: wrap;
    }

    /* ── Column cards within a group ── */
    .col-card {
        min-width: 200px; box-sizing: border-box;
        border: 1px dashed var(--uui-color-border, #ccc); border-radius: 6px;
        background: var(--uui-color-surface-alt, #fafafa);
    }
    .col-header {
        display: flex; align-items: center; gap: 8px; padding: 8px 12px;
        background: var(--uui-color-surface-alt, #f0f0f0);
        border-bottom: 1px solid #e0e0e0;
    }
    .col-num { font-weight: 600; font-size: 0.85rem; color: #555; }
    .col-width-label {
        display: flex; align-items: center; gap: 4px;
        font-size: 0.8rem; font-weight: 500;
    }
    .col-width-label uui-input { width: 55px; }
    .col-actions { margin-left: auto; }
    .col-fields { padding: 8px 10px; }
    .col-add-field {
        text-align: center; padding: 6px 0; margin-top: 4px;
        border-top: 1px dashed #ddd;
    }

    /* ── Move to select ── */
    .move-to-select {
        padding: 3px 6px; border: 1px solid #ccc; border-radius: 4px;
        font-size: 0.78rem; background: #fff; height: 33px; box-sizing: border-box;
        cursor: pointer; color: #555;
    }
    .move-to-select:hover { border-color: #888; }

    /* ── Column drag & drop ── */
    .col-drag-handle {
        cursor: grab; color: #aaa; font-size: 0.9rem; user-select: none;
    }
    .col-drag-handle:active { cursor: grabbing; }
    .col-card.col-dragging { opacity: 0.4; }
    .col-card.col-drag-over {
        box-shadow: inset 0 0 0 2px var(--uui-color-interactive, #1b264f);
        border-color: var(--uui-color-interactive, #1b264f);
    }

    /* ── Compact field card ── */
    .fc {
        display: flex; align-items: center; gap: 6px; padding: 6px 8px;
        border: 1px solid #e0e0e0; border-radius: 4px; margin-bottom: 4px;
        background: #fff; cursor: grab; user-select: none;
        transition: box-shadow 0.15s, opacity 0.15s;
    }
    .fc:hover { border-color: #bbb; }
    .fc-hidden { opacity: 0.45; border-style: dashed; }
    .fc-dragging { opacity: 0.3; }
    .fc-drag-over { box-shadow: inset 0 0 0 2px var(--uui-color-interactive, #1b264f); }
    .fc-type {
        font-size: 0.7rem; font-weight: 600; color: #888; text-transform: uppercase;
        background: #f0f0f0; padding: 1px 5px; border-radius: 3px; white-space: nowrap;
    }
    .fc-label { flex: 1; font-size: 0.85rem; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .fc-req { color: #e53935; font-weight: 700; font-size: 1rem; }
    .fc-actions { display: flex; gap: 2px; margin-left: auto; flex-shrink: 0; }
    .fc-btn {
        border: none; background: none; cursor: pointer; padding: 2px 4px;
        font-size: 0.75rem; color: #888; border-radius: 3px;
    }
    .fc-btn:hover { background: #f0f0f0; color: #333; }
    .fc-btn:disabled { opacity: 0.3; cursor: default; }
    .fc-btn-danger:hover { background: #fde8e8; color: #c0392b; }

    /* ── Field settings dialog ── */
    .field-dialog {
        background: var(--uui-color-surface, #fff); border-radius: 8px;
        width: 640px; max-width: 90vw; max-height: 85vh; display: flex; flex-direction: column;
        box-shadow: 0 8px 32px rgba(0,0,0,0.3);
    }
    .field-dialog-header {
        display: flex; justify-content: space-between; align-items: center;
        padding: 14px 20px; border-bottom: 1px solid #e0e0e0;
    }
    .field-dialog-header h3 { margin: 0; font-size: 1rem; }
    .field-dialog-body { padding: 16px 20px; overflow-y: auto; flex: 1; }
    .field-dialog-footer { padding: 12px 20px; border-top: 1px solid #e0e0e0; display: flex; justify-content: flex-end; }
    .fd-row { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; }
    .fd-label { font-weight: 600; font-size: 0.85rem; min-width: 80px; }
    .fd-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 12px; }
    .fd-grid label { display: flex; flex-direction: column; gap: 3px; font-size: 0.8rem; font-weight: 500; }
    .fd-toggles { display: flex; gap: 16px; margin-bottom: 12px; flex-wrap: wrap; }
    .fd-options { margin-top: 12px; }
    .fd-html-textarea {
        width: 100%; min-height: 120px; padding: 8px; border: 1px solid #ccc; border-radius: 4px;
        font-family: monospace; font-size: 0.85rem; resize: vertical; box-sizing: border-box;
        background: #fff; margin-top: 4px;
    }
`;
