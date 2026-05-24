let allOrders    = [];
let currentStatus = 'all';
let currentSearch = '';
let currentOrderId = null;

document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        const tab = this.dataset.tab;
        document.querySelectorAll('.tab-content').forEach(t => t.style.display = 'none');
        document.getElementById(`tab-${tab}`).style.display = 'block';
        if (tab === 'orders') loadOrders();
    });
});

async function loadOrders() {
    const res = await fetch('/Employee/GetOrders');
    allOrders = await res.json();
    renderOrders();
}

function renderOrders() {
    const tbody = document.getElementById('emp-orders-tbody');
    let filtered = allOrders;

    if (currentStatus !== 'all')
        filtered = filtered.filter(o => o.status === currentStatus);

    if (currentSearch.trim()) {
        const q = currentSearch.toLowerCase();
        filtered = filtered.filter(o =>
            o.orderId.toLowerCase().includes(q) ||
            o.fullName.toLowerCase().includes(q) ||
            o.phone.toLowerCase().includes(q)
        );
    }

    if (!filtered.length) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;padding:40px;color:#888;">No orders found.</td></tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(o => `
        <tr class="order-row" data-orderid="${o.orderId}">
            <td style="font-weight:700;color:#CCBEA8;">${o.orderId}</td>
            <td>${o.fullName}</td>
            <td>${o.phone}</td>
            <td>₱${o.total.toFixed(2)}</td>
            <td><span class="badge badge-${o.status.toLowerCase()}">${o.status}</span></td>
            <td>${o.createdAt}</td>
        </tr>
    `).join('');

    document.querySelectorAll('.order-row').forEach(row => {
        row.addEventListener('click', function () {
            const order = allOrders.find(o => o.orderId === this.dataset.orderid);
            if (order) openOrderModal(order);
        });
    });

    document.getElementById('footer-info').textContent = `${filtered.length} orders`;
}

document.getElementById('empOrderSearch')?.addEventListener('input', function () {
    currentSearch = this.value;
    renderOrders();
});

document.querySelectorAll('.status-filter').forEach(btn => {
    btn.addEventListener('click', function () {
        document.querySelectorAll('.status-filter').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        currentStatus = this.dataset.status;
        renderOrders();
    });
});

function openOrderModal(order) {
    currentOrderId = order.orderId;
    document.getElementById('modal-orderId').textContent  = order.orderId;
    document.getElementById('modal-date').textContent     = order.createdAt;
    document.getElementById('modal-name').textContent     = order.fullName;
    document.getElementById('modal-phone').textContent    = order.phone;
    document.getElementById('modal-address').textContent  = order.address;
    document.getElementById('modal-subtotal').textContent = '₱' + order.subtotal.toFixed(2);
    document.getElementById('modal-delivery').textContent = '₱' + order.deliveryFee.toFixed(2);
    document.getElementById('modal-total').textContent    = '₱' + order.total.toFixed(2);

    document.getElementById('modal-items').innerHTML = order.items.map(item => `
        <div class="modal-item-row">
            <img src="${item.image}" alt="${item.productName}" />
            <div class="modal-item-info">
                <div class="modal-item-name">${item.productName}</div>
                <div class="modal-item-qty">x${item.quantity}</div>
            </div>
            <div class="modal-item-price">₱${(item.price * item.quantity).toFixed(2)}</div>
        </div>
    `).join('');

    document.getElementById('modal-status-select').value  = order.status;
    document.getElementById('statusSaveMsg').style.display = 'none';
    document.getElementById('orderModal').style.display   = 'flex';
}

document.getElementById('closeOrderModal')?.addEventListener('click', () => {
    document.getElementById('orderModal').style.display = 'none';
});

document.getElementById('orderModal')?.addEventListener('click', function (e) {
    if (e.target === this) this.style.display = 'none';
});

document.getElementById('saveStatusBtn')?.addEventListener('click', async () => {
    const newStatus = document.getElementById('modal-status-select').value;
    const res = await fetch('/Employee/UpdateOrderStatus', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ orderId: currentOrderId, status: newStatus })
    });
    if (res.ok) {
        const order = allOrders.find(o => o.orderId === currentOrderId);
        if (order) order.status = newStatus;
        document.getElementById('statusSaveMsg').style.display = 'block';
        renderOrders();
        setTimeout(() => document.getElementById('statusSaveMsg').style.display = 'none', 2000);
    }
});


// ── Products ──────────────────────────────────────────────────────────────────
let allEmpProducts = [];

async function loadEmpProducts() {
    const res      = await fetch('/Employee/GetProducts');
    allEmpProducts = await res.json();
    renderEmpProducts();
}

function renderEmpProducts() {
    const grid = document.getElementById('emp-products-grid');
    const q    = document.getElementById('empProductSearch')?.value.toLowerCase() || '';

    const filtered = allEmpProducts.filter(p =>
        p.name.toLowerCase().includes(q)
    );

    if (!filtered.length) {
        grid.innerHTML = `<p style="padding:40px;color:#888;">No products found.</p>`;
        return;
    }

    grid.innerHTML = filtered.map(p => `
        <div class="product-admin-card">
            <img src="${p.image}" alt="${p.name}" onerror="this.src='/img/logowhite.png'" />
            <div class="product-admin-info">
                <div class="product-admin-name">${p.name}</div>
                <div class="product-admin-price">₱${p.price.toFixed(2)}</div>
                <div class="product-admin-meta">
                    <span class="badge" style="background:#3A322B;color:#CCBEA8;">${p.category}</span>
                    <span style="color:${p.stock > 0 ? '#4CAF50' : '#e63946'};">
                        Stock: ${p.stock}
                    </span>
                </div>
            </div>
            <div class="product-admin-actions" style="align-items:center;">
                <input type="number" class="stock-input" data-id="${p.id}"
                    value="${p.stock}" min="0"
                    style="width:70px;padding:6px;background:#3A322B;border:1px solid #4f4439;
                           border-radius:6px;color:#CCBEA8;text-align:center;" />
                <button class="save-status-btn update-stock-btn" data-id="${p.id}"
                    style="font-size:12px;padding:6px 14px;">Update Stock</button>
            </div>
        </div>
    `).join('');

    document.querySelectorAll('.update-stock-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            const id    = parseInt(this.dataset.id);
            const input = document.querySelector(`.stock-input[data-id="${id}"]`);
            const stock = parseInt(input.value);

            const res = await fetch('/Employee/UpdateStock', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id, stock })
            });

            if (res.ok) {
                const p = allEmpProducts.find(p => p.id === id);
                if (p) p.stock = stock;
                alert('Stock updated!');
                renderEmpProducts();
            }
        });
    });
}

document.getElementById('empProductSearch')?.addEventListener('input', renderEmpProducts);

loadOrders();