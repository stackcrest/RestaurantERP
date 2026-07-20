(function () {
    const ICONS = {
        success: 'bi-check-circle-fill',
        error: 'bi-x-circle-fill',
        warning: 'bi-exclamation-triangle-fill',
        info: 'bi-info-circle-fill',
        cart: 'bi-cart-check-fill'
    };

    const TITLES = {
        success: 'Success',
        error: 'Error',
        warning: 'Warning',
        info: 'Info',
        cart: 'Cart updated'
    };

    let host = null;

    function getHost() {
        if (!host) host = document.getElementById('notificationBarHost');
        return host;
    }

    function show(message, type = 'success', options = {}) {
        const container = getHost();
        if (!container || !message) return null;

        const {
            title = TITLES[type] || TITLES.info,
            duration = 4000,
            actionLabel = null,
            actionUrl = null
        } = options;

        const item = document.createElement('div');
        item.className = `notification-bar-item type-${type}`;
        item.setAttribute('role', 'alert');

        const actionHtml = actionLabel && actionUrl
            ? `<a href="${escapeHtml(actionUrl)}" class="d-inline-block mt-1 small fw-semibold text-decoration-none">${escapeHtml(actionLabel)}</a>`
            : '';

        item.innerHTML = `
            <div class="notify-icon"><i class="bi ${ICONS[type] || ICONS.info}"></i></div>
            <div class="notify-body">
                <div class="notify-title">${escapeHtml(title)}</div>
                <div class="notify-message">${escapeHtml(message)}${actionHtml}</div>
            </div>
            <button type="button" class="notify-close" aria-label="Dismiss"><i class="bi bi-x-lg"></i></button>
            <div class="notify-progress"></div>`;

        const progress = item.querySelector('.notify-progress');
        const closeBtn = item.querySelector('.notify-close');

        let timeoutId = null;
        let remaining = duration;
        let started = Date.now();

        function dismiss() {
            if (timeoutId) clearTimeout(timeoutId);
            item.classList.add('is-leaving');
            setTimeout(() => item.remove(), 240);
        }

        closeBtn.addEventListener('click', dismiss);

        if (duration > 0) {
            progress.style.transition = `transform ${duration}ms linear`;
            requestAnimationFrame(() => { progress.style.transform = 'scaleX(0)'; });
            timeoutId = setTimeout(dismiss, duration);
        } else {
            progress.style.display = 'none';
        }

        item.addEventListener('mouseenter', () => {
            if (!timeoutId) return;
            clearTimeout(timeoutId);
            remaining -= Date.now() - started;
            progress.style.transition = 'none';
            progress.style.transform = `scaleX(${remaining / duration})`;
        });

        item.addEventListener('mouseleave', () => {
            if (duration <= 0) return;
            started = Date.now();
            progress.style.transition = `transform ${remaining}ms linear`;
            progress.style.transform = 'scaleX(0)';
            timeoutId = setTimeout(dismiss, remaining);
        });

        container.appendChild(item);

        while (container.children.length > 5) {
            container.firstElementChild?.remove();
        }

        return item;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    const api = {
        show,
        success: (msg, opts) => show(msg, 'success', opts),
        error: (msg, opts) => show(msg, 'error', { duration: 6000, ...opts }),
        warning: (msg, opts) => show(msg, 'warning', { duration: 5000, ...opts }),
        info: (msg, opts) => show(msg, 'info', opts),
        cart: (msg, opts) => show(msg, 'cart', { title: 'Cart updated', actionLabel: 'View cart', actionUrl: '/Cart', ...opts })
    };

    window.RestaurantNotify = api;
    window.showNotification = (msg, type, opts) => api.show(msg, type, opts);

    document.addEventListener('DOMContentLoaded', () => {
        const payload = document.getElementById('serverNotifications');
        if (!payload) return;
        try {
            const items = JSON.parse(payload.textContent || '[]');
            items.forEach((n, i) => {
                setTimeout(() => api.show(n.message, n.type || 'info', { title: n.title }), i * 200);
            });
        } catch { /* ignore */ }
    });
})();
