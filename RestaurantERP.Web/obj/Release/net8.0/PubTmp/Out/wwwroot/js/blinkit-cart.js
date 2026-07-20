document.addEventListener('DOMContentLoaded', () => {
    function notify(message, type, options) {
        if (window.RestaurantNotify) {
            window.RestaurantNotify.show(message, type, options);
        }
    }

    function updateCartUI(count, total) {
        const bar = document.getElementById('floatingCartBar');
        const countEl = document.getElementById('floatingCartCount');
        const totalEl = document.getElementById('floatingCartTotal');
        const desktopWrapper = document.getElementById('desktopCartWrapper');
        const desktopCount = document.getElementById('desktopCartCount');
        const desktopTotal = document.getElementById('desktopCartTotal');
        const navBadge = document.getElementById('navCartBadge');

        const formatted = '₹' + Math.round(total).toLocaleString('en-IN');

        if (bar) {
            bar.classList.toggle('d-none', count <= 0);
        }
        if (countEl) countEl.textContent = count;
        if (totalEl) totalEl.textContent = formatted;

        if (desktopWrapper) {
            if (count > 0) {
                desktopWrapper.classList.remove('d-none');
                desktopWrapper.classList.add('d-md-block');
            } else {
                desktopWrapper.classList.add('d-none');
                desktopWrapper.classList.remove('d-md-block');
            }
        }

        if (desktopCount) desktopCount.textContent = count;
        if (desktopTotal) desktopTotal.textContent = formatted;

        if (navBadge) {
            navBadge.textContent = count;
            navBadge.classList.toggle('d-none', count <= 0);
        }
    }

    window.updateCartUI = updateCartUI;

    function renderStepper(container, itemId, quantity) {
        container.outerHTML = `
            <div class="blinkit-qty-stepper" data-item-id="${itemId}">
                <button type="button" class="btn qty-minus" aria-label="Decrease">−</button>
                <span class="qty-value">${quantity}</span>
                <button type="button" class="btn qty-plus" aria-label="Increase">+</button>
            </div>`;
        bindStepper(document.querySelector(`.blinkit-qty-stepper[data-item-id="${itemId}"]`));
    }

    function renderAddButton(container, itemId) {
        container.outerHTML = `<button type="button" class="btn btn-sm blinkit-add-btn" data-item-id="${itemId}">ADD</button>`;
        bindAddButton(document.querySelector(`.blinkit-add-btn[data-item-id="${itemId}"]`));
    }

    async function parseCartResponse(res) {
        const contentType = res.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            throw new Error('Unexpected response from server');
        }
        return res.json();
    }

    async function quickAdd(itemId, button) {
        try {
            const res = await fetch('/Cart/QuickAdd', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ menuItemId: itemId })
            });
            const data = await parseCartResponse(res);
            if (data.requiresOptions) {
                window.location.href = data.detailUrl;
                return;
            }
            if (!data.success) {
                notify(data.message || 'Could not add to cart', 'error');
                return;
            }
            updateCartUI(data.cartCount, data.cartTotal);
            renderStepper(button, itemId, data.quantity);
            notify(data.quantity > 1 ? `Quantity updated (${data.quantity} in cart)` : 'Item added to cart', 'cart');
        } catch {
            notify('Could not update cart. Please try again.', 'error');
        }
    }

    async function quickUpdate(itemId, quantity, container) {
        try {
            const res = await fetch('/Cart/QuickUpdate', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ menuItemId: itemId, quantity })
            });
            const data = await parseCartResponse(res);
            if (!data.success) {
                notify(data.message || 'Could not update cart', 'error');
                return;
            }
            updateCartUI(data.cartCount, data.cartTotal);
            if (data.quantity <= 0) {
                renderAddButton(container, itemId);
                notify('Item removed from cart', 'cart');
            } else {
                container.querySelector('.qty-value').textContent = data.quantity;
                notify(`Cart updated (${data.cartCount} items)`, 'cart', { duration: 2500 });
            }
        } catch {
            notify('Could not update cart. Please try again.', 'error');
        }
    }

    function bindAddButton(btn) {
        if (!btn) return;
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            quickAdd(btn.dataset.itemId, btn);
        });
    }

    function bindStepper(stepper) {
        if (!stepper) return;
        const itemId = stepper.dataset.itemId;
        stepper.querySelector('.qty-plus')?.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const qty = parseInt(stepper.querySelector('.qty-value').textContent, 10) + 1;
            quickUpdate(itemId, qty, stepper);
        });
        stepper.querySelector('.qty-minus')?.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const qty = parseInt(stepper.querySelector('.qty-value').textContent, 10) - 1;
            quickUpdate(itemId, qty, stepper);
        });
    }

    document.querySelectorAll('.blinkit-add-btn[data-item-id]').forEach(bindAddButton);
    document.querySelectorAll('.blinkit-qty-stepper').forEach(bindStepper);
});
