// ── Tab switching ─────────────────────────────────────────────────────────────
let allOrders   = [];
let currentStatus = 'all';
let currentSearch = '';
let currentOrderId = null;
let salesPeriod = 'day';

document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        const tab = this.dataset.tab;
        document.querySelectorAll('.tab-content').forEach(t => t.style.display = 'none');
        document.getElementById(`tab-${tab}`).style.display = 'block';
        if (tab === 'orders') loadOrders();
        if (tab === 'products') loadProducts();
        if (tab === 'sales')  loadSales();
        if (tab === 'users')    loadStaff();    
        if (tab === 'settings') loadSettings(); 

        loadSales();
        loadSettings();
    });
});

// ── Sales ─────────────────────────────────────────────────────────────────────
async function loadSales() {
    const res  = await fetch(`/Admin/GetSalesData?period=${salesPeriod}`);
    const data = await res.json();

    document.getElementById('admin-totalOrders').textContent = data.totalOrders;
    document.getElementById('admin-totalSales').textContent  = '₱' + data.totalSales.toFixed(2);

    const tbody = document.getElementById('admin-sales-tbody');
    if (!data.orders.length) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;padding:40px;color:#888;">No completed orders.</td></tr>`;
        return;
    }
    tbody.innerHTML = data.orders.map(o => `
        <tr>
            <td style="color:#CCBEA8;font-weight:700;">${o.orderId}</td>
            <td>${o.fullName}</td>
            <td>${o.paymentMethod.toUpperCase()}</td>
            <td>₱${o.total.toFixed(2)}</td>
            <td>${o.createdAt}</td>
        </tr>
    `).join('');

    document.getElementById('footer-info').textContent = `${data.totalOrders} completed orders • ₱${data.totalSales.toFixed(2)} total sales`;
}

document.querySelectorAll('.period-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        document.querySelectorAll('.period-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        salesPeriod = this.dataset.period;
        loadSales();
    });
});

// ── Orders ────────────────────────────────────────────────────────────────────
async function loadOrders() {
    const res = await fetch('/Admin/GetOrders');
    allOrders = await res.json();
    renderOrders();
}

function renderOrders() {
    const tbody = document.getElementById('admin-orders-tbody');
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
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;padding:40px;color:#888;">No orders found.</td></tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(o => `
        <tr class="order-row" data-orderid="${o.orderId}">
            <td style="font-weight:700;color:#CCBEA8;">${o.orderId}</td>
            <td>${o.fullName}</td>
            <td>${o.phone}</td>
            <td>${o.paymentMethod.toUpperCase()}</td>
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

document.getElementById('adminOrderSearch')?.addEventListener('input', function () {
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

// ── Order Modal ───────────────────────────────────────────────────────────────
function openOrderModal(order) {
    currentOrderId = order.orderId;
    document.getElementById('modal-orderId').textContent  = order.orderId;
    document.getElementById('modal-date').textContent     = order.createdAt;
    document.getElementById('modal-name').textContent     = order.fullName;
    document.getElementById('modal-phone').textContent    = order.phone;
    document.getElementById('modal-address').textContent  = order.address;
    document.getElementById('modal-payment').textContent  = order.paymentMethod.toUpperCase();
    document.getElementById('modal-notes').textContent    = order.deliveryNotes || '—';
    document.getElementById('modal-subtotal').textContent = '₱' + order.subtotal.toFixed(2);
    document.getElementById('modal-delivery').textContent = '₱' + order.deliveryFee.toFixed(2);
    document.getElementById('modal-total').textContent    = '₱' + order.total.toFixed(2);
    document.getElementById('modal-points').textContent   = '+' + order.pointsEarned + ' pts';

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

    document.getElementById('modal-status-select').value = order.status;
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
    const res = await fetch('/Admin/UpdateOrderStatus', {
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



// Better — add GetStaff to AdminController and call it here
async function loadStaff() {
    const res   = await fetch('/Admin/GetStaff');
    const users = await res.json();
    const tbody = document.getElementById('admin-users-tbody');

    if (!users.length) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;padding:40px;color:#888;">No staff found.</td></tr>`;
        return;
    }

    tbody.innerHTML = users.map(u => `
        <tr>
            <td>#${u.id}</td>
            <td>${u.name}</td>
            <td>${u.phone || '—'}</td>
            <td><span class="badge" style="background:#3A322B;color:#CCBEA8;">${u.role}</span></td>
            <td>${u.createdAt}</td>
            <td>
                <button class="save-status-btn delete-staff-btn" data-id="${u.id}"
                    style="background:#721c24;padding:6px 12px;font-size:12px;">Remove</button>
            </td>
        </tr>
    `).join('');

    document.querySelectorAll('.delete-staff-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            if (!confirm('Remove this staff member?')) return;
            const res = await fetch('/Admin/DeleteUser', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: parseInt(this.dataset.id) })
            });
            if (res.ok) loadStaff();
        });
    });
}

document.getElementById('addStaffBtn')?.addEventListener('click', () => {
    document.getElementById('addStaffModal').style.display = 'flex';
    document.getElementById('staffMsg').style.display = 'none';
});

document.getElementById('closeAddStaffModal')?.addEventListener('click', () => {
    document.getElementById('addStaffModal').style.display = 'none';
});

document.getElementById('submitStaff')?.addEventListener('click', async () => {
    const name     = document.getElementById('staffName').value.trim();
    const phone    = document.getElementById('staffPhone').value.trim();
    const password = document.getElementById('staffPassword').value;
    const role     = document.getElementById('staffRole').value;
    const msg      = document.getElementById('staffMsg');

    if (!name || !phone || !password) {
        msg.textContent = 'All fields are required.';
        msg.style.color = '#e63946';
        msg.style.display = 'block';
        return;
    }

    const res = await fetch('/Admin/AddUser', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, phone, password, role })
    });

    const data = await res.json();
    msg.style.display = 'block';

    if (res.ok) {
        msg.textContent = 'Staff added!';
        msg.style.color = '#4CAF50';
        loadStaff();
    } else {
        msg.textContent = data.message || 'Failed.';
        msg.style.color = '#e63946';
    }
});




// ── Products ──────────────────────────────────────────────────────────────────
let allProducts  = [];
let productSearch = '';

async function loadProducts() {
    const res   = await fetch('/Admin/GetProducts');
    allProducts = await res.json();
    renderProducts();
}

function renderProducts() {
    const grid = document.getElementById('products-grid');
    let filtered = allProducts;

    if (productSearch.trim()) {
        const q = productSearch.toLowerCase();
        filtered = filtered.filter(p => p.name.toLowerCase().includes(q));
    }

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
                    <span style="color:${p.isActive ? '#4CAF50' : '#888'};">
                        ${p.isActive ? 'Active' : 'Hidden'}
                    </span>
                </div>
            </div>
            <div class="product-admin-actions">
                <button class="save-status-btn edit-product-btn" data-id="${p.id}"
                    style="font-size:12px;padding:6px 14px;">Edit</button>
                <button class="save-status-btn delete-product-btn" data-id="${p.id}"
                    style="font-size:12px;padding:6px 14px;background:#721c24;">Delete</button>
            </div>
        </div>
    `).join('');

    document.querySelectorAll('.edit-product-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const p = allProducts.find(p => p.id === parseInt(this.dataset.id));
            if (!p) return;
            document.getElementById('productModalTitle').textContent = 'Edit Product';
            document.getElementById('editProductId').value   = p.id;
            document.getElementById('productName').value     = p.name;
            document.getElementById('productPrice').value    = p.price;
            document.getElementById('productImage').value    = p.image;
            document.getElementById('productCategory').value = p.category;
            document.getElementById('productStock').value    = p.stock;
            document.getElementById('productActive').checked = p.isActive;
            document.getElementById('productMsg').style.display = 'none';
            document.getElementById('productModal').style.display = 'flex';
        });
    });

    document.querySelectorAll('.delete-product-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            if (!confirm('Delete this product?')) return;
            const res = await fetch('/Admin/DeleteProduct', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: parseInt(this.dataset.id) })
            });
            if (res.ok) {
                allProducts = allProducts.filter(p => p.id !== parseInt(this.dataset.id));
                renderProducts();
            }
        });
    });

    document.getElementById('footer-info').textContent = `${filtered.length} products`;
}

document.getElementById('productSearch')?.addEventListener('input', function () {
    productSearch = this.value;
    renderProducts();
});

document.getElementById('addProductBtn')?.addEventListener('click', () => {
    document.getElementById('productModalTitle').textContent = 'Add Product';
    document.getElementById('editProductId').value   = '0';
    document.getElementById('productName').value     = '';
    document.getElementById('productPrice').value    = '';
    document.getElementById('productImage').value    = '';
    document.getElementById('productCategory').value = 'hot';
    document.getElementById('productStock').value    = '0';
    document.getElementById('productActive').checked = true;
    document.getElementById('productMsg').style.display = 'none';
    document.getElementById('productModal').style.display = 'flex';
});

document.getElementById('closeProductModal')?.addEventListener('click', () => {
    document.getElementById('productModal').style.display = 'none';
});

document.getElementById('productModal')?.addEventListener('click', function (e) {
    if (e.target === this) this.style.display = 'none';
});

document.getElementById('submitProduct')?.addEventListener('click', async () => {
    const id       = parseInt(document.getElementById('editProductId').value);
    const name     = document.getElementById('productName').value.trim();
    const price    = parseFloat(document.getElementById('productPrice').value);
    const image    = document.getElementById('productImage').value.trim();
    const category = document.getElementById('productCategory').value;
    const stock    = parseInt(document.getElementById('productStock').value);
    const isActive = document.getElementById('productActive').checked;
    const msg      = document.getElementById('productMsg');

    if (!name || isNaN(price)) {
        msg.textContent   = 'Name and price are required.';
        msg.style.color   = '#e63946';
        msg.style.display = 'block';
        return;
    }

    const url  = id === 0 ? '/Admin/AddProduct' : '/Admin/UpdateProduct';
    const res  = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id, name, price, image, category, stock, isActive })
    });

    const data = await res.json();
    msg.style.display = 'block';

    if (res.ok) {
        msg.textContent = id === 0 ? 'Product added!' : 'Product updated!';
        msg.style.color = '#4CAF50';
        loadProducts();
    } else {
        msg.textContent = data.message || 'Failed.';
        msg.style.color = '#e63946';
    }
});

// ── Settings ──────────────────────────────────────────────────────────────────
async function loadSettings() {
    const res      = await fetch('/Admin/GetSettings');
    const settings = await res.json();

    const codToggle = document.getElementById('codToggle');
    if (!codToggle) return;

    // Default to true if key doesn't exist yet
    const codEnabled = settings['cod_enabled'];
    codToggle.checked = codEnabled === undefined || codEnabled === 'true';

    const newToggle = codToggle.cloneNode(true);
    codToggle.parentNode.replaceChild(newToggle, codToggle);

    newToggle.addEventListener('change', async function () {
        const value = this.checked ? 'true' : 'false';
        const res = await fetch('/Admin/UpdateSetting', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ key: 'cod_enabled', value })
        });

        if (res.ok) {
            const msg = document.getElementById('settingsSaveMsg');
            msg.textContent   = `Cash on Delivery ${this.checked ? 'enabled' : 'disabled'}.`;
            msg.style.display = 'block';
            setTimeout(() => msg.style.display = 'none', 2000);
        }
    });
}

// Also load products when tab clicked — add to tab-btn listener:
// if (tab === 'products') loadProducts();

// Init
loadSales();