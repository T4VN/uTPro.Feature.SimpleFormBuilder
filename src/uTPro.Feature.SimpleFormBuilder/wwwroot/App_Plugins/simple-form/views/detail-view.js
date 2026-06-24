import { html, nothing } from '@umbraco-cms/backoffice/external/lit';

/**
 * Renders the entry detail overlay.
 * @param {object} host - the dashboard element
 */
export function renderDetail(host) {
    const s = host._detailEntry;
    if (!s) return nothing;

    const isAdmin = host._permissions?.isAdmin;
    const entries = Object.entries(s.data || {});

    return html`
        <div class="overlay" @click=${(e) => { if (e.target === e.currentTarget) host._closeDetail(); }}>
            <div class="detail-panel">
                <div class="detail-header">
                    <h3>Entry #${s.id}</h3>
                    <uui-button look="secondary" compact @click=${() => host._closeDetail()}>&#10005; Close</uui-button>
                </div>
                <div class="detail-body">
                    <div class="detail-row">
                        <span class="detail-label">Date</span>
                        <span class="detail-value">${new Date(s.createdUtc).toLocaleString()}</span>
                    </div>
                    <div class="detail-row">
                        <span class="detail-label">IP Address</span>
                        <span class="detail-value">${s.ipAddress || 'N/A'}</span>
                    </div>
                    ${entries.map(([k, v]) => html`
                        <div class="detail-row">
                            <span class="detail-label">${k}</span>
                            <span class="detail-value">${v || ''}</span>
                        </div>
                    `)}
                </div>
                ${isAdmin ? html`
                    <div class="detail-footer">
                        <uui-button look="outline" color="danger" @click=${() => { host._deleteEntry(s.id); host._closeDetail(); }}>Delete</uui-button>
                    </div>
                ` : nothing}
            </div>
        </div>`;
}
