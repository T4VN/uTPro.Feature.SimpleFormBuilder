// ── Shared event bus ──
// The section sidebar (sidebar.js) and the main dashboard (index.js) are separate
// backoffice extensions rendered in different slots. They both import THIS module,
// and because the URL is identical they share the same singleton instance.
//
// Events:
//   'new'     – sidebar "+" button asks the dashboard to start a new form
//   'select'  – sidebar asks the dashboard to open a form by id (detail = id)
//   'active'  – dashboard tells the sidebar which form is focused (detail = id, 0 = none)
//   'notify'  – any app shows a transient message via the dashboard banner (detail = text)
//   'refresh' – dashboard tells the sidebar the form list changed (re-fetch)

class FormBus extends EventTarget {
    requestNew() {
        this.dispatchEvent(new Event('new'));
    }
    selectForm(id) {
        this.dispatchEvent(new CustomEvent('select', { detail: id }));
    }
    setActive(id) {
        this.dispatchEvent(new CustomEvent('active', { detail: id }));
    }
    notify(message) {
        this.dispatchEvent(new CustomEvent('notify', { detail: message }));
    }
    refresh() {
        this.dispatchEvent(new Event('refresh'));
    }
}

// Guarantee a single shared instance across extension boundaries.
// Relying on ES-module singleton behaviour alone is fragile: if the backoffice
// loader ever evaluates this module more than once (different base URL, cache
// busting, HMR), the sidebar and dashboard would end up with separate buses and
// events would never cross. Keying off globalThis removes that risk entirely.
const BUS_KEY = Symbol.for('uTPro.SimpleForm.FormBus');
globalThis[BUS_KEY] ??= new FormBus();

export const formBus = globalThis[BUS_KEY];
export default formBus;
