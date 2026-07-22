document.addEventListener('DOMContentLoaded', () => {
    function notify(message, type, options) {
        if (window.RestaurantNotify) {
            window.RestaurantNotify.show(message, type, options);
        }
    }

    function stickyCartHidden() {
        const bar = document.getElementById('floatingCartBar');
        return document.body.classList.contains('hide-sticky-cart')
            || bar?.dataset.hideOnCart === '1';
    }

    function updateCartUI(count, total) {
        const bar = document.getElementById('floatingCartBar');
        const countEl = document.getElementById('floatingCartCount');
        const totalEl = document.getElementById('floatingCartTotal');
        const desktopWrapper = document.getElementById('desktopCartWrapper');
        const desktopCount = document.getElementById('desktopCartCount');
        const desktopTotal = document.getElementById('desktopCartTotal');
        const navBadge = document.getElementById('navCartBadge');
        const hideSticky = stickyCartHidden();

        const formatted = '₹' + Math.round(total).toLocaleString('en-IN');

        if (bar) {
            bar.classList.toggle('d-none', count <= 0 || hideSticky);
        }
        if (countEl) countEl.textContent = count;
        if (totalEl) totalEl.textContent = formatted;

        if (desktopWrapper) {
            if (count > 0 && !hideSticky) {
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

        document.body.classList.toggle('has-sticky-cart', count > 0 && !hideSticky);
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

    function renderAddButton(container, itemId, hasOptions) {
        const opts = hasOptions ? ' data-has-options="true"' : '';
        container.outerHTML = `<button type="button" class="btn btn-sm blinkit-add-btn" data-item-id="${itemId}"${opts}>ADD</button>`;
        bindAddButton(document.querySelector(`.blinkit-add-btn[data-item-id="${itemId}"]`));
    }

    async function parseCartResponse(res) {
        const contentType = res.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            throw new Error('Unexpected response from server');
        }
        return res.json();
    }

    let optionsSheet;
    let optionsState = { itemId: null, basePrice: 0 };

    function getOptionsSheet() {
        if (!optionsSheet) {
            const el = document.getElementById('menuOptionsSheet');
            if (el && window.bootstrap) optionsSheet = new bootstrap.Offcanvas(el);
        }
        return optionsSheet;
    }

    function calcOptionsTotal() {
        let total = optionsState.basePrice;
        const variant = document.querySelector('#menuOptionsBody input[name="sheetVariant"]:checked');
        if (variant) total += parseFloat(variant.dataset.adj || '0');
        document.querySelectorAll('#menuOptionsBody input[name="sheetAddOn"]:checked').forEach((cb) => {
            total += parseFloat(cb.dataset.price || '0');
        });
        const qty = parseInt(document.getElementById('sheetQty')?.value || '1', 10);
        return total * (qty > 0 ? qty : 1);
    }

    function refreshOptionsPrice() {
        const priceEl = document.getElementById('menuOptionsPrice');
        const btn = document.getElementById('menuOptionsAddBtn');
        const total = calcOptionsTotal();
        if (priceEl) priceEl.textContent = '₹' + Math.round(total).toLocaleString('en-IN');
        if (btn) {
            btn.disabled = false;
            btn.textContent = 'Add to cart · ₹' + Math.round(total).toLocaleString('en-IN');
        }
    }

    async function openOptionsSheet(itemId) {
        const sheet = getOptionsSheet();
        const body = document.getElementById('menuOptionsBody');
        const title = document.getElementById('menuOptionsTitle');
        const btn = document.getElementById('menuOptionsAddBtn');
        if (!body) {
            window.location.href = '/Menu/Details/' + itemId;
            return;
        }

        body.innerHTML = '<div class="text-center py-4 text-muted">Loading…</div>';
        if (btn) btn.disabled = true;
        sheet?.show();

        try {
            const res = await fetch('/Menu/Options/' + itemId, { headers: { Accept: 'application/json' } });
            const data = await res.json();
            if (!data.success) {
                notify(data.message || 'Could not load options', 'error');
                sheet?.hide();
                return;
            }

            optionsState = { itemId: data.id, basePrice: data.basePrice };
            if (title) title.textContent = data.name;

            let html = '';
            if (data.variants?.length) {
                html += '<div class="mb-3"><h6 class="fw-bold mb-2">Choose size</h6><div class="d-flex flex-column gap-2">';
                data.variants.forEach((v, i) => {
                    const checked = v.isDefault || i === 0 ? 'checked' : '';
                    html += `<label class="option-chip d-flex justify-content-between align-items-center">
                        <span><input type="radio" name="sheetVariant" value="${v.id}" data-adj="${v.priceAdjustment}" ${checked}> ${v.name}</span>
                        <span class="text-muted">${v.priceAdjustment >= 0 ? '+' : ''}₹${Math.round(v.priceAdjustment)}</span>
                    </label>`;
                });
                html += '</div></div>';
            }

            if (data.addOns?.length) {
                html += '<div class="mb-3"><h6 class="fw-bold mb-2">Add extras</h6><div class="d-flex flex-column gap-2">';
                data.addOns.forEach((a) => {
                    html += `<label class="option-chip d-flex justify-content-between align-items-center">
                        <span><input type="checkbox" name="sheetAddOn" value="${a.id}" data-price="${a.price}"> ${a.name}</span>
                        <span class="text-muted">+₹${Math.round(a.price)}</span>
                    </label>`;
                });
                html += '</div></div>';
            }

            html += `<div class="d-flex align-items-center justify-content-between mb-2">
                <span class="fw-bold">Quantity</span>
                <div class="d-flex align-items-center gap-2">
                    <button type="button" class="btn btn-outline-secondary" id="sheetQtyMinus" style="width:44px;height:44px;">−</button>
                    <input type="number" id="sheetQty" value="1" min="1" max="20" class="form-control text-center fw-bold" style="width:64px;height:44px;" readonly>
                    <button type="button" class="btn btn-primary" id="sheetQtyPlus" style="width:44px;height:44px;">+</button>
                </div>
            </div>`;

            body.innerHTML = html;
            body.querySelectorAll('input').forEach((el) => el.addEventListener('change', refreshOptionsPrice));
            document.getElementById('sheetQtyMinus')?.addEventListener('click', () => {
                const input = document.getElementById('sheetQty');
                const n = Math.max(1, parseInt(input.value, 10) - 1);
                input.value = n;
                refreshOptionsPrice();
            });
            document.getElementById('sheetQtyPlus')?.addEventListener('click', () => {
                const input = document.getElementById('sheetQty');
                const n = Math.min(20, parseInt(input.value, 10) + 1);
                input.value = n;
                refreshOptionsPrice();
            });
            refreshOptionsPrice();
        } catch {
            notify('Could not load options. Opening full page…', 'warning');
            window.location.href = '/Menu/Details/' + itemId;
        }
    }

    async function submitOptionsSheet() {
        if (!optionsState.itemId) return;
        const variant = document.querySelector('#menuOptionsBody input[name="sheetVariant"]:checked');
        const addOnIds = Array.from(document.querySelectorAll('#menuOptionsBody input[name="sheetAddOn"]:checked'))
            .map((cb) => cb.value);
        const quantity = parseInt(document.getElementById('sheetQty')?.value || '1', 10);

        try {
            const res = await fetch('/Cart/QuickAddConfigured', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                body: JSON.stringify({
                    menuItemId: optionsState.itemId,
                    variantId: variant?.value || null,
                    addOnIds,
                    quantity
                })
            });
            const data = await parseCartResponse(res);
            if (data.requiresAuth) {
                window.location.href = data.joinUrl || '/Account/QuickJoin?returnUrl=/Menu';
                return;
            }
            if (!data.success) {
                notify(data.message || 'Could not add to cart', 'error');
                return;
            }
            updateCartUI(data.cartCount, data.cartTotal);
            getOptionsSheet()?.hide();
            notify('Added to cart', 'cart');
        } catch {
            notify('Could not add to cart. Please try again.', 'error');
        }
    }

    document.getElementById('menuOptionsAddBtn')?.addEventListener('click', submitOptionsSheet);

    async function quickAdd(itemId, button) {
        if (button?.dataset.hasOptions === 'true') {
            await openOptionsSheet(itemId);
            return;
        }

        try {
            const res = await fetch('/Cart/QuickAdd', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                body: JSON.stringify({ menuItemId: itemId })
            });
            const data = await parseCartResponse(res);
            if (data.requiresAuth) {
                window.location.href = data.joinUrl || '/Account/QuickJoin?returnUrl=/Menu';
                return;
            }
            if (data.requiresOptions) {
                await openOptionsSheet(data.menuItemId || itemId);
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
                headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                body: JSON.stringify({ menuItemId: itemId, quantity })
            });
            const data = await parseCartResponse(res);
            if (!data.success) {
                notify(data.message || 'Could not update cart', 'error');
                return;
            }
            updateCartUI(data.cartCount, data.cartTotal);
            if (data.quantity <= 0) {
                renderAddButton(container, itemId, false);
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
