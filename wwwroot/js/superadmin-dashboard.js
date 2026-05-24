let salesPeriod   = 'day';
let allUsers      = [];
let userSearch    = '';

// Tab switching
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        const tab = this.dataset.tab;
        document.querySelectorAll('.tab-content').forEach(t => t.style.display = 'none');
        document.getElementById(`tab-${tab}`).style.display = 'block';
        if (tab === 'sales')    loadSales();
        if (tab === 'earnings') loadAllTimeEarnings();
        if (tab === 'users')    loadUsers();
    });
});

// ── Sales ─────────────────────────────────────────────────────────────────────
async function loadSales() {
    const res  = await fetch(`/SuperAdmin/GetSalesData?period=${salesPeriod}`);
    const data = await res.json();

    document.getElementById('totalOrders').textContent = data.totalOrders;
    document.getElementById('totalSales').textContent  = '₱' + data.totalSales.toFixed(2);
    document.getElementById('devEarnings').textContent = '₱' + data.developerEarnings.toFixed(2);

    const tbody = document.getElementById('superadmin-orders-tbody');
    if (!data.orders.length) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;padding:40px;color:#888;">No completed orders.</td></tr>`;
        return;
    }

    tbody.innerHTML = data.orders.map(o => `
        <tr>
            <td style="color:#CCBEA8;font-weight:700;">${o.orderId}</td>
            <td>${o.fullName}</td>
            <td>${o.paymentMethod.toUpperCase()}</td>
            <td>₱${o.total.toFixed(2)}</td>
            <td style="color:#a8d5a2;">₱${(o.total * 0.03).toFixed(2)}</td>
            <td>${o.createdAt}</td>
        </tr>
    `).join('');

    document.getElementById('footer-info').textContent =
        `${data.totalOrders} orders • ₱${data.totalSales.toFixed(2)} sales • ₱${data.developerEarnings.toFixed(2)} earned`;
}

document.querySelectorAll('.period-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        document.querySelectorAll('.period-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        salesPeriod = this.dataset.period;
        loadSales();
    });
});

async function loadAllTimeEarnings() {
    const res  = await fetch(`/SuperAdmin/GetSalesData?period=all`);
    const data = await res.json();
    document.getElementById('allTimeEarnings').textContent = '₱' + data.developerEarnings.toFixed(2);
}

// ── Users ─────────────────────────────────────────────────────────────────────
async function loadUsers() {
    const res = await fetch('/SuperAdmin/GetAllUsers');
    allUsers  = await res.json();
    renderUsers();
}

function renderUsers() {
    const tbody = document.getElementById('superadmin-users-tbody');
    let filtered = allUsers;

    if (userSearch.trim()) {
        const q = userSearch.toLowerCase();
        filtered = filtered.filter(u =>
            u.name.toLowerCase().includes(q) ||
            u.phone.toLowerCase().includes(q) ||
            u.role.toLowerCase().includes(q)
        );
    }

    if (!filtered.length) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;padding:40px;color:#888;">No users found.</td></tr>`;
        return;
    }

    tbody.innerHTML = filtered.map(u => `
        <tr>
            <td>#${u.id}</td>
            <td>${u.name}</td>
            <td>${u.phone || '—'}</td>
            <td><span class="badge" style="background:#3A322B;color:#CCBEA8;">${u.role}</span></td>
            <td>${u.points}</td>
            <td>${u.createdAt}</td>
            <td>
                <button class="save-status-btn delete-user-btn" data-id="${u.id}"
                    style="background:#721c24;padding:6px 12px;font-size:12px;">
                    Delete
                </button>
            </td>
        </tr>
    `).join('');

    document.querySelectorAll('.delete-user-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            if (!confirm('Delete this user?')) return;
            const res = await fetch('/SuperAdmin/DeleteUser', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: parseInt(this.dataset.id) })
            });
            if (res.ok) {
                allUsers = allUsers.filter(u => u.id !== parseInt(this.dataset.id));
                renderUsers();
            }
        });
    });

    document.getElementById('footer-info').textContent = `${filtered.length} users`;
}

document.getElementById('superUserSearch')?.addEventListener('input', function () {
    userSearch = this.value;
    renderUsers();
});

// Add User Modal
document.getElementById('addUserBtn')?.addEventListener('click', () => {
    document.getElementById('addUserModal').style.display = 'flex';
    document.getElementById('addUserMsg').style.display   = 'none';
});

document.getElementById('closeAddUserModal')?.addEventListener('click', () => {
    document.getElementById('addUserModal').style.display = 'none';
});

document.getElementById('addUserModal')?.addEventListener('click', function (e) {
    if (e.target === this) this.style.display = 'none';
});

document.getElementById('submitAddUser')?.addEventListener('click', async () => {
    const name     = document.getElementById('newUserName').value.trim();
    const phone    = document.getElementById('newUserPhone').value.trim();
    const password = document.getElementById('newUserPassword').value;
    const role     = document.getElementById('newUserRole').value;
    const msg      = document.getElementById('addUserMsg');

    if (!name || !phone || !password) {
        msg.textContent = 'All fields are required.';
        msg.style.color = '#e63946';
        msg.style.display = 'block';
        return;
    }

    const res = await fetch('/SuperAdmin/AddUser', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, phone, password, role })
    });

    const data = await res.json();
    msg.style.display = 'block';

    if (res.ok) {
        msg.textContent = 'User created successfully!';
        msg.style.color = '#4CAF50';
        document.getElementById('newUserName').value     = '';
        document.getElementById('newUserPhone').value    = '';
        document.getElementById('newUserPassword').value = '';
        loadUsers();
    } else {
        msg.textContent = data.message || 'Failed to create user.';
        msg.style.color = '#e63946';
    }
});

// Init
loadSales();