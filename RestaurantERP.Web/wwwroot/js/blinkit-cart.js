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
    let optionsState = { itemId: null, basePrice: 0, imageUrl: null };

    function getOptionsSheet() {
        if (!optionsSheet) {
            const el = document.getElementById('menuOptionsSheet');
            if (el && window.bootstrap) optionsSheet = new bootstrap.Offcanvas(el);
        }
        return optionsSheet;
    }

    function moneyInr(n) {
        return '₹' + Math.round(n || 0).toLocaleString('en-IN');
    }

    function getSheetQty() {
        return Math.max(1, parseInt(document.getElementById('sheetQty')?.value || '1', 10));
    }

    function setSheetQty(n) {
        const qty = Math.min(20, Math.max(1, n));
        const hidden = document.getElementById('sheetQty');
        const label = document.getElementById('sheetQtyValue');
        if (hidden) hidden.value = String(qty);
        if (label) label.textContent = String(qty);
        refreshOptionsPrice();
    }

    function calcOptionsTotal() {
        let unit = optionsState.basePrice;
        const variant = document.querySelector('#menuOptionsBody input[name="sheetVariant"]:checked');
        if (variant) {
            if (variant.dataset.price) unit = parseFloat(variant.dataset.price);
            else unit = optionsState.basePrice + parseFloat(variant.dataset.adj || '0');
        }
        document.querySelectorAll('#menuOptionsBody input[name="sheetAddOn"]:checked').forEach((cb) => {
            unit += parseFloat(cb.dataset.price || '0');
        });
        return unit * getSheetQty();
    }

    function refreshOptionsPrice() {
        const priceEl = document.getElementById('menuOptionsPrice');
        const btn = document.getElementById('menuOptionsAddBtn');
        const total = calcOptionsTotal();
        if (priceEl && !priceEl.dataset.locked) priceEl.textContent = '';
        if (btn) {
            btn.disabled = false;
            btn.textContent = 'Add item — ' + moneyInr(total);
        }
    }

    function sizeSubtitle(name) {
        const n = (name || '').toLowerCase();
        if (n === 'half') return 'Serves 1–2';
        if (n === 'full') return 'Serves 2–3';
        return '';
    }

    async function openOptionsSheet(itemId) {
        const sheet = getOptionsSheet();
        const body = document.getElementById('menuOptionsBody');
        const title = document.getElementById('menuOptionsTitle');
        const thumb = document.getElementById('menuOptionsThumb');
        const btn = document.getElementById('menuOptionsAddBtn');
        if (!body) {
            window.location.href = '/Menu/Details/' + itemId;
            return;
        }

        body.innerHTML = '<div class="text-center py-4 text-secondary">Loading…</div>';
        if (btn) {
            btn.disabled = true;
            btn.textContent = 'Add item';
        }
        setSheetQty(1);
        sheet?.show();

        try {
            const res = await fetch('/Menu/Options/' + itemId, { headers: { Accept: 'application/json' } });
            const data = await res.json();
            if (!data.success) {
                notify(data.message || 'Could not load options', 'error');
                sheet?.hide();
                return;
            }

            optionsState = { itemId: data.id, basePrice: data.basePrice, imageUrl: data.imageUrl || null };
            if (title) title.textContent = data.name;
            if (thumb) {
                if (data.imageUrl) {
                    thumb.src = data.imageUrl;
                    thumb.alt = data.name || '';
                    thumb.classList.remove('d-none');
                } else {
                    thumb.classList.add('d-none');
                    thumb.removeAttribute('src');
                }
            }

            const isHalfFull = Array.isArray(data.variants) && data.variants.length >= 2
                && data.variants.some(v => /^half$/i.test(v.name))
                && data.variants.some(v => /^full$/i.test(v.name));

            let html = '';
            if (data.variants?.length) {
                html += `<div class="menu-customizer-section">
                    <h6>${isHalfFull ? 'Quantity' : 'Choose size'}</h6>
                    <div class="menu-customizer-hint">Required · Select any 1 option</div>`;
                data.variants.forEach((v, i) => {
                    const checked = v.isDefault || i === 0 ? 'checked' : '';
                    const absolute = typeof v.price === 'number' ? v.price : (data.basePrice + (v.priceAdjustment || 0));
                    const sub = sizeSubtitle(v.name);
                    html += `<label class="menu-customizer-row">
                        <span>
                            <span class="row-label d-block">${v.name}</span>
                            ${sub ? `<span class="row-sub">${sub}</span>` : ''}
                        </span>
                        <span class="d-flex align-items-center">
                            <span class="row-price">${moneyInr(absolute)}</span>
                            <input type="radio" name="sheetVariant" value="${v.id}" data-adj="${v.priceAdjustment || 0}" data-price="${absolute}" ${checked}>
                        </span>
                    </label>`;
                });
                html += '</div>';
            }

            if (data.addOns?.length) {
                html += `<div class="menu-customizer-section">
                    <h6>Add accompaniments</h6>
                    <div class="menu-customizer-hint">Optional · Select extras</div>`;
                data.addOns.forEach((a) => {
                    html += `<label class="menu-customizer-row">
                        <span class="row-label">${a.name}</span>
                        <span class="d-flex align-items-center">
                            <span class="row-price">+${moneyInr(a.price)}</span>
                            <input type="checkbox" name="sheetAddOn" value="${a.id}" data-price="${a.price}">
                        </span>
                    </label>`;
                });
                html += '</div>';
            }

            if (!data.variants?.length && !data.addOns?.length) {
                html = '<div class="text-center text-secondary py-3">No options for this item.</div>';
            }

            body.innerHTML = html;
            body.querySelectorAll('input').forEach((el) => el.addEventListener('change', refreshOptionsPrice));
            refreshOptionsPrice();
        } catch {
            notify('Could not load options. Opening full page…', 'warning');
            window.location.href = '/Menu/Details/' + itemId;
        }
    }

    async function submitOptionsSheet() {
        if (!optionsState.itemId) return;
        const variant = document.querySelector('#menuOptionsBody input[name="sheetVariant"]:checked');
        const variantsExist = document.querySelectorAll('#menuOptionsBody input[name="sheetVariant"]').length > 0;
        if (variantsExist && !variant) {
            notify('Please select Half or Full', 'warning');
            return;
        }
        const addOnIds = Array.from(document.querySelectorAll('#menuOptionsBody input[name="sheetAddOn"]:checked'))
            .map((cb) => cb.value);
        const quantity = getSheetQty();

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
    document.getElementById('sheetQtyMinus')?.addEventListener('click', () => setSheetQty(getSheetQty() - 1));
    document.getElementById('sheetQtyPlus')?.addEventListener('click', () => setSheetQty(getSheetQty() + 1));

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
