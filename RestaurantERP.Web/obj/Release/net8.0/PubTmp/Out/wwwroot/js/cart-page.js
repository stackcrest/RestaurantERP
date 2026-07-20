document.addEventListener('DOMContentLoaded', () => {
    const cartRoot = document.getElementById('cartPageRoot');
    if (!cartRoot) return;

    async function apiUpdate(cartItemId, quantity) {
        const res = await fetch('/Cart/ApiUpdateLine', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: JSON.stringify({ id: cartItemId, quantity })
        });
        if (!res.ok) throw new Error('Update failed');
        return res.json();
    }

    async function apiRemove(cartItemId) {
        const res = await fetch('/Cart/ApiRemoveLine', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: JSON.stringify({ id: cartItemId })
        });
        if (!res.ok) throw new Error('Remove failed');
        return res.json();
    }

    function formatMoney(n) {
        return '₹' + Math.round(n).toLocaleString('en-IN');
    }

    function notify(msg, type) {
        if (window.RestaurantNotify) window.RestaurantNotify.show(msg, type || 'info');
    }

    function updateSummary(data) {
        const sub = document.getElementById('cartSubtotal');
        const discountRow = document.getElementById('cartDiscountRow');
        const discount = document.getElementById('cartDiscount');
        const tax = document.getElementById('cartTax');
        const total = document.getElementById('cartTotal');
        const checkoutBtn = document.getElementById('cartCheckoutBtn');
        const discountAlert = document.getElementById('cartDiscountAlert');

        if (sub) sub.textContent = formatMoney(data.subTotal);
        if (tax) tax.textContent = formatMoney(data.tax);
        if (total) total.textContent = formatMoney(data.total);
        if (checkoutBtn) checkoutBtn.textContent = `Proceed to Checkout — ${formatMoney(data.total)}`;

        if (discountRow && discount) {
            if (data.discount > 0) {
                discountRow.classList.remove('d-none');
                discount.textContent = '-' + formatMoney(data.discount);
            } else {
                discountRow.classList.add('d-none');
            }
        }

        if (discountAlert) {
            if (data.discount > 0) {
                discountAlert.className = 'alert alert-success small mb-3';
                discountAlert.innerHTML = `<i class="bi bi-check-circle me-1"></i>You get ${data.discountPercent}% discount on this order!`;
                discountAlert.classList.remove('d-none');
            } else if (data.subTotal < data.discountThreshold) {
                const remaining = data.discountThreshold - data.subTotal;
                discountAlert.className = 'alert alert-warning small mb-3';
                discountAlert.innerHTML = `<i class="bi bi-info-circle me-1"></i>Add ${formatMoney(remaining)} more to get ${data.discountPercent}% discount!`;
                discountAlert.classList.remove('d-none');
            } else {
                discountAlert.classList.add('d-none');
            }
        }

        if (typeof window.updateCartUI === 'function') {
            window.updateCartUI(data.cartCount, data.cartTotal);
        }
    }

    function updateLineRow(row, line) {
        const qtyEl = row.querySelector('.cart-line-qty');
        const totalEl = row.querySelector('.cart-line-total');
        if (qtyEl) qtyEl.textContent = line.quantity;
        if (totalEl) totalEl.textContent = formatMoney(line.lineTotal);
    }

    async function handleQuantityChange(row, cartItemId, quantity) {
        try {
            const data = await apiUpdate(cartItemId, quantity);
            if (!data.success) return;

            if (quantity <= 0 || !data.lines.some(l => l.id === cartItemId)) {
                row.remove();
                if (data.lines.length === 0) {
                    window.location.reload();
                    return;
                }
            } else {
                const line = data.lines.find(l => l.id === cartItemId);
                if (line) updateLineRow(row, line);
            }
            updateSummary(data);
            notify(data.quantity <= 0 ? 'Item removed from cart' : 'Cart updated', 'cart', { duration: 2500 });
        } catch {
            notify('Could not update cart. Please try again.', 'error');
        }
    }

    cartRoot.addEventListener('click', async (e) => {
        const minusBtn = e.target.closest('.cart-qty-minus');
        const plusBtn = e.target.closest('.cart-qty-plus');
        const removeBtn = e.target.closest('.cart-remove-btn');

        if (minusBtn) {
            e.preventDefault();
            const row = minusBtn.closest('[data-cart-item-id]');
            const id = row?.dataset.cartItemId;
            const qty = parseInt(row?.querySelector('.cart-line-qty')?.textContent || '1', 10) - 1;
            if (id) await handleQuantityChange(row, id, qty);
        }

        if (plusBtn) {
            e.preventDefault();
            const row = plusBtn.closest('[data-cart-item-id]');
            const id = row?.dataset.cartItemId;
            const qty = parseInt(row?.querySelector('.cart-line-qty')?.textContent || '0', 10) + 1;
            if (id) await handleQuantityChange(row, id, qty);
        }

        if (removeBtn) {
            e.preventDefault();
            const row = removeBtn.closest('[data-cart-item-id]');
            const id = row?.dataset.cartItemId;
            if (!id) return;
            try {
                const data = await apiRemove(id);
                if (!data.success) return;
                row.remove();
                if (data.lines.length === 0) {
                    window.location.reload();
                    return;
                }
                updateSummary(data);
                notify('Item removed from cart', 'cart');
            } catch {
                notify('Could not remove item. Please try again.', 'error');
            }
        }
    });
});
