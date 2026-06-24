/**
 * SimpleForm — client-side validation & submission handler.
 * Reads config from data attributes on the <form> element:
 *   data-alias       — form alias (required)
 *   data-redirect    — redirect URL after success (optional)
 *   data-btn-text    — submit button text for reset after submit (optional)
 */
(function () {
    document.querySelectorAll('form.sf').forEach(function (form) {
        var alias = form.dataset.alias;
        if (!alias) return;

        var redirectUrl = form.dataset.redirect || '';
        var btnText = form.dataset.btnText || 'Submit';

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            var msgEl = form.querySelector('.sf-message');
            var btn = form.querySelector('[type="submit"]');
            form.querySelectorAll('.sf-error').forEach(function (el) { el.textContent = ''; });
            if (msgEl) msgEl.style.display = 'none';

            var data = {};
            var valid = true;
            var inputs = form.querySelectorAll('[name]');
            inputs.forEach(function (input) {
                if (input.name === '__alias') return;
                var name = input.name;
                if (input.type === 'checkbox') {
                    var checked = form.querySelectorAll('input[name="' + name + '"]:checked');
                    if (checked.length > 0) data[name] = Array.from(checked).map(function (c) { return c.value; }).join(', ');
                } else if (input.type === 'radio') {
                    var sel = form.querySelector('input[name="' + name + '"]:checked');
                    if (sel) data[name] = sel.value;
                } else if (input.type !== 'file') {
                    data[name] = input.value;
                }
                if (input.hasAttribute('required') && !data[name]) {
                    var errEl = form.querySelector('.sf-error[data-for="' + name + '"]');
                    if (errEl) errEl.textContent = input.dataset.msg || 'Required';
                    valid = false;
                }
                if (input.pattern && data[name]) {
                    if (!new RegExp(input.pattern).test(data[name])) {
                        var errEl2 = form.querySelector('.sf-error[data-for="' + name + '"]');
                        if (errEl2) errEl2.textContent = input.dataset.msg || 'Invalid';
                        valid = false;
                    }
                }
            });

            if (window.__sfBeforeSubmit) {
                var hookResult = await window.__sfBeforeSubmit(alias, data, form);
                if (hookResult === false) return;
                if (typeof hookResult === 'object') Object.assign(data, hookResult);
            }

            if (!valid) return;
            if (btn) { btn.disabled = true; btn.textContent = 'Sending...'; }
            try {
                var resp = await fetch('/api/utpro/simple-form/submit', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ alias: alias, data: data })
                });
                var result = await resp.json();
                if (resp.ok) {
                    if (redirectUrl) { window.location.href = redirectUrl; return; }
                    if (msgEl) {
                        msgEl.className = 'sf-message sf-success';
                        msgEl.textContent = result.message || 'Thank you!';
                        msgEl.style.display = 'block';
                    }
                    form.reset();
                    if (window.__sfAfterSubmit) window.__sfAfterSubmit(alias, true, result);
                } else {
                    if (msgEl) {
                        msgEl.className = 'sf-message sf-fail';
                        msgEl.textContent = result.message || 'Error';
                        msgEl.style.display = 'block';
                    }
                }
            } catch (err) {
                if (msgEl) {
                    msgEl.className = 'sf-message sf-fail';
                    msgEl.textContent = 'Network error';
                    msgEl.style.display = 'block';
                }
            }
            if (btn) { btn.disabled = false; btn.textContent = btnText; }
        });
    });
})();
