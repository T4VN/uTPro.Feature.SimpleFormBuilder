// ── API helper & constants ──
export const API = '/umbraco/management/api/v1/utpro/simple-form';

/**
 * Make an authenticated POST request to the backoffice API.
 * @param {string} url
 * @param {object} body
 * @param {object} authContext - UMB_AUTH_CONTEXT instance
 * @returns {Promise<any>}
 */
export async function apiPost(url, body = {}, authContext = null) {
    const config = authContext?.getOpenApiConfiguration();
    const headers = { 'Content-Type': 'application/json' };
    if (config?.token) {
        const t = await config.token();
        if (t) headers['Authorization'] = 'Bearer ' + t;
    }
    const resp = await fetch(url, {
        method: 'POST',
        headers,
        credentials: config?.credentials || 'same-origin',
        body: JSON.stringify(body)
    });
    if (!resp.ok) {
        const e = await resp.json().catch(() => ({}));
        throw new Error(e.message || 'Failed');
    }
    return resp.json();
}
