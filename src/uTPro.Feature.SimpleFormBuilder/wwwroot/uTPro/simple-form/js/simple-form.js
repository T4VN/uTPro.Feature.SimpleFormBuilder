/**
 * SimpleForm — client-side validation & submission handler.
 * Reads config from data attributes on the <form> element:
 *   data-alias       — form alias (required)
 *   data-redirect    — redirect URL after success (optional)
 *   data-btn-text    — submit button text for reset after submit (optional)
 */
(function () {
    document.querySelectorAll('form.uTProForm').forEach(function (form) {
        var alias = form.dataset.alias;
        if (!alias) return;

        var redirectUrl = form.dataset.redirect || '';
        var btnText = form.dataset.btnText || 'Submit';
        // Server-rendered (already localized for the page culture) success message.
        var successMsg = form.dataset.successMsg || '';

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            var msgEl = form.querySelector('.uTProForm-message');
            var btn = form.querySelector('[type="submit"]');
            form.querySelectorAll('.uTProForm-error').forEach(function (el) { el.textContent = ''; });
            if (msgEl) msgEl.style.display = 'none';

            var data = {};
            var valid = true;
            var fileInputs = [];
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
                } else if (input.type === 'file') {
                    // Files are uploaded separately (below); only their reference goes into `data`.
                    var file = input.files && input.files[0];
                    if (file) {
                        var maxMb = parseFloat(input.dataset.maxSize || '');
                        if (!isNaN(maxMb) && maxMb > 0 && file.size > maxMb * 1024 * 1024) {
                            var errElF = form.querySelector('.uTProForm-error[data-for="' + name + '"]');
                            if (errElF) errElF.textContent = 'File exceeds ' + maxMb + ' MB';
                            valid = false;
                        } else {
                            fileInputs.push({ name: name, file: file });
                        }
                    } else if (input.hasAttribute('required')) {
                        var errElR = form.querySelector('.uTProForm-error[data-for="' + name + '"]');
                        if (errElR) errElR.textContent = input.dataset.msg || 'Required';
                        valid = false;
                    }
                    return; // file inputs skip the generic required/pattern checks below
                } else {
                    data[name] = input.value;
                }
                if (input.hasAttribute('required') && !data[name]) {
                    var errEl = form.querySelector('.uTProForm-error[data-for="' + name + '"]');
                    if (errEl) errEl.textContent = input.dataset.msg || 'Required';
                    valid = false;
                }
                if (input.pattern && data[name]) {
                    if (!new RegExp(input.pattern).test(data[name])) {
                        var errEl2 = form.querySelector('.uTProForm-error[data-for="' + name + '"]');
                        if (errEl2) errEl2.textContent = input.dataset.msg || 'Invalid';
                        valid = false;
                    }
                }
            });

            if (!valid) return;
            if (btn) { btn.disabled = true; btn.textContent = 'Sending...'; }

            if (window.__uTProFormBeforeSubmit) {
                var hookResult = await window.__uTProFormBeforeSubmit(alias, data, form);
                if (hookResult === false) { if (btn) { btn.disabled = false; btn.textContent = btnText; } return; }
                if (typeof hookResult === 'object') Object.assign(data, hookResult);
            }
            try {
                // Data + any files go up together in a single multipart request. Files are
                // only persisted server-side once the submission validates and is stored,
                // so an abandoned/failed submit never leaves orphaned uploads.
                var body = new FormData();
                body.append('alias', alias);
                body.append('data', JSON.stringify(data));
                for (var i = 0; i < fileInputs.length; i++) {
                    body.append('file:' + fileInputs[i].name, fileInputs[i].file);
                }
                var resp = await fetch('/api/utpro/simple-form/submit', {
                    method: 'POST',
                    body: body
                });
                var result = await resp.json();
                if (resp.ok) {
                    if (redirectUrl) { window.location.href = redirectUrl; return; }
                    if (msgEl) {
                        msgEl.className = 'uTProForm-message uTProForm-success';
                        msgEl.textContent = successMsg || result.message || 'Thank you!';
                        msgEl.style.display = 'block';
                    }
                    form.reset();
                    if (window.__uTProFormAfterSubmit) window.__uTProFormAfterSubmit(alias, true, result);
                } else {
                    if (msgEl) {
                        msgEl.className = 'uTProForm-message uTProForm-fail';
                        msgEl.textContent = result.message || 'Error';
                        msgEl.style.display = 'block';
                    }
                }
            } catch (err) {
                if (msgEl) {
                    msgEl.className = 'uTProForm-message uTProForm-fail';
                    msgEl.textContent = 'Network error';
                    msgEl.style.display = 'block';
                }
            }
            if (btn) { btn.disabled = false; btn.textContent = btnText; }
        });
    });
})();
