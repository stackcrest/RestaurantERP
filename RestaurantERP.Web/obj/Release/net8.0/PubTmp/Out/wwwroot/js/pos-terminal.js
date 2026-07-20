// POS Terminal - offline bill generation
const posCart = [];
const CGST = 2.5, SGST = 2.5, DISCOUNT_THRESHOLD = 500, DISCOUNT_PCT = 5, DELIVERY_FEE = 40;

function posNotify(msg, type, opts) {
    if (window.RestaurantNotify) window.RestaurantNotify.show(msg, type, opts);
}

function renderCart() {
    const list = document.getElementById('posCartList');
    if (!posCart.length) {
        list.innerHTML = '<div class="text-center text-muted py-5 small">Tap items to add</div>';
        updateTotals();
        return;
    }
    list.innerHTML = posCart.map((item, idx) => `
        <div class="pos-cart-row d-flex justify-content-between align-items-center">
            <div class="small flex-grow-1">
                <div class="fw-semibold">${item.name}</div>
                <div class="text-muted">₹${item.price} each</div>
            </div>
            <div class="d-flex align-items-center gap-1">
                <button type="button" class="btn btn-sm btn-outline-secondary py-0 px-2" onclick="changeQty(${idx}, -1)">−</button>
                <span class="fw-bold" style="min-width:20px;text-align:center">${item.qty}</span>
                <button type="button" class="btn btn-sm btn-outline-secondary py-0 px-2" onclick="changeQty(${idx}, 1)">+</button>
                <button type="button" class="btn btn-sm btn-link text-danger p-0 ms-1" onclick="removeItem(${idx})"><i class="bi bi-x-lg"></i></button>
            </div>
        </div>`).join('');
    updateTotals();
}

function updateTotals() {
    const subtotal = posCart.reduce((s, i) => s + i.price * i.qty, 0);
    const discount = subtotal >= DISCOUNT_THRESHOLD ? Math.round(subtotal * DISCOUNT_PCT / 100) : 0;
    const taxable = subtotal - discount;
    const tax = Math.round(taxable * (CGST + SGST) / 100);
    const isDelivery = document.getElementById('orderType').value === 'Delivery';
    const delivery = isDelivery ? DELIVERY_FEE : 0;
    const total = taxable + tax + delivery;

    document.getElementById('posSubtotal').textContent = '₹' + subtotal;
    document.getElementById('posDiscount').textContent = '₹' + discount;
    document.getElementById('posTax').textContent = '₹' + tax;
    document.getElementById('posTotal').textContent = '₹' + total;
}

window.changeQty = (idx, delta) => {
    posCart[idx].qty += delta;
    if (posCart[idx].qty <= 0) posCart.splice(idx, 1);
    renderCart();
};
window.removeItem = (idx) => { posCart.splice(idx, 1); renderCart(); };

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.pos-add-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            const el = btn.closest('.pos-item');
            const id = el.dataset.id;
            const existing = posCart.find(i => i.menuItemId === id);
            if (existing) existing.qty++;
            else posCart.push({ menuItemId: id, name: el.dataset.nameDisplay, price: parseFloat(el.dataset.price), qty: 1 });
            renderCart();
            posNotify(`${el.dataset.nameDisplay} added to bill`, 'success', { title: 'POS', duration: 2000 });
        });
    });

    document.querySelectorAll('.cat-tab').forEach(tab => {
        tab.addEventListener('click', () => {
            document.querySelectorAll('.cat-tab').forEach(t => { t.classList.remove('btn-primary'); t.classList.add('btn-outline-secondary'); });
            tab.classList.remove('btn-outline-secondary');
            tab.classList.add('btn-primary');
            const cat = tab.dataset.category;
            document.querySelectorAll('.pos-item').forEach(item => {
                item.style.display = !cat || item.dataset.category === cat ? '' : 'none';
            });
        });
    });

    document.getElementById('itemSearch').addEventListener('input', (e) => {
        const q = e.target.value.toLowerCase();
        document.querySelectorAll('.pos-item').forEach(item => {
            item.style.display = item.dataset.name.includes(q) ? '' : 'none';
        });
    });

    document.getElementById('orderType').addEventListener('change', (e) => {
        document.getElementById('tableSelectWrap').style.display = e.target.value === 'DineIn' ? 'block' : 'none';
        updateTotals();
    });

    document.getElementById('clearCartBtn').addEventListener('click', () => {
        posCart.length = 0;
        renderCart();
        posNotify('Bill cleared', 'info', { title: 'POS', duration: 2000 });
    });

    document.getElementById('checkoutBtn').addEventListener('click', async () => {
        if (!posCart.length) {
            posNotify('Add items to the bill first.', 'warning', { title: 'POS' });
            return;
        }

        const btn = document.getElementById('checkoutBtn');
        btn.disabled = true;
        btn.textContent = 'Generating...';

        try {
            const tableEl = document.getElementById('tableId');
            const tableVal = tableEl ? tableEl.value : '';
            const payload = {
                orderType: document.getElementById('orderType').value,
                tableId: tableVal || null,
                customerName: document.getElementById('customerName').value || null,
                customerPhone: document.getElementById('customerPhone').value || null,
                paymentMethod: document.querySelector('input[name="payment"]:checked')?.value || 'Cash',
                items: posCart.map(i => ({ menuItemId: i.menuItemId, quantity: i.qty }))
            };

            const res = await fetch('/Admin/Pos/Checkout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            });

            const text = await res.text();
            let data;
            try { data = JSON.parse(text); } catch {
                posNotify('Bill generation failed. Check POS access and SuperAdmin settings.', 'error', { title: 'POS', duration: 6000 });
                return;
            }

            if (data.success && data.printUrl) {
                window.open(data.printUrl, '_blank');
                posCart.length = 0;
                renderCart();
                document.getElementById('customerName').value = '';
                document.getElementById('customerPhone').value = '';
                posNotify('Bill generated successfully', 'success', { title: 'POS', actionLabel: 'Print again', actionUrl: data.printUrl });
            } else {
                posNotify(data.message || 'Failed to generate bill.', 'error', { title: 'POS' });
            }
        } catch (err) {
            posNotify('Network error: ' + err.message, 'error', { title: 'POS' });
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-printer me-1"></i>Generate Bill';
        }
    });
});
