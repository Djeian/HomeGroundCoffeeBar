let allOrders    = [];
let currentStatus = 'all';
let currentSearch = '';
let currentOrder  = null;
let gpsInterval   = null;

async function loadOrders() {
    const res = await fetch('/Rider/GetAssignedOrders');
    allOrders = await res.json();
    renderCards();
}

function renderCards() {
    const container = document.getElementById('rider-cards');
    let filtered = allOrders;

    if (currentStatus !== 'all')
        filtered = filtered.filter(o => o.status === currentStatus);

    if (currentSearch.trim()) {
        const q = currentSearch.toLowerCase();
        filtered = filtered.filter(o =>
            o.orderId.toLowerCase().includes(q) ||
            o.fullName.toLowerCase().includes(q)
        );
    }

    if (!filtered.length) {
        container.innerHTML = `<p style="padding:40px;color:#888;text-align:center;">No deliveries assigned.</p>`;
        return;
    }

    container.innerHTML = filtered.map(o => `
        <div class="rider-card" data-orderid="${o.orderId}">
            <div class="rider-card-header">
                <span class="rider-order-id">${o.orderId}</span>
                <span class="badge badge-${o.status.toLowerCase()}">${o.status}</span>
            </div>
            <div class="rider-card-body">
                <div class="rider-detail"><span>👤</span> ${o.fullName}</div>
                <div class="rider-detail"><span>📞</span> ${o.phone}</div>
                <div class="rider-detail"><span>📍</span> ${o.address}</div>
                <div class="rider-detail"><span>💰</span> ₱${o.total.toFixed(2)}</div>
            </div>
            <div class="rider-action-btns">
                ${o.status === 'Preparing' || o.status === 'Pickup' ? `
                    <button class="save-status-btn mark-otw-btn" data-orderid="${o.orderId}">
                        🛵 Mark as On the Way
                    </button>` : ''}
                ${o.status === 'Otw' ? `
                    <button class="save-status-btn mark-delivered-btn" data-orderid="${o.orderId}"
                        style="background:#2d4e3b;">
                        ✅ Mark as Delivered
                    </button>` : ''}
            </div>
        </div>
    `).join('');

    // Mark OTW
    document.querySelectorAll('.mark-otw-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            const orderId = this.dataset.orderid;
            await updateStatus(orderId, 'Otw');
            startSharingLocation(orderId);
        });
    });

    // Mark Delivered
    document.querySelectorAll('.mark-delivered-btn').forEach(btn => {
        btn.addEventListener('click', async function () {
            const orderId = this.dataset.orderid;
            await updateStatus(orderId, 'Delivered');
            stopSharingLocation();
        });
    });

    document.getElementById('footer-info').textContent = `${filtered.length} active deliveries`;
}

async function updateStatus(orderId, status) {
    const res = await fetch('/Rider/UpdateOrderStatus', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ orderId, status })
    });

    if (res.ok) {
        const order = allOrders.find(o => o.orderId === orderId);
        if (order) order.status = status;
        renderCards();
    } else {
        const data = await res.json();
        alert('Error: ' + (data.message || 'Failed to update status'));
    }
}

function startSharingLocation(orderId) {
    if (!navigator.geolocation) {
        alert('GPS not supported on this device.');
        return;
    }

    stopSharingLocation(); // clear any existing interval

    gpsInterval = setInterval(() => {
        navigator.geolocation.getCurrentPosition(pos => {
            fetch('/Rider/UpdateLocation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    orderId:   orderId,
                    latitude:  pos.coords.latitude,
                    longitude: pos.coords.longitude
                })
            });
        }, err => {
            console.warn('GPS error:', err.message);
        });
    }, 5000); // update every 5 seconds
}

function stopSharingLocation() {
    if (gpsInterval) {
        clearInterval(gpsInterval);
        gpsInterval = null;
    }
}

document.getElementById('riderSearch')?.addEventListener('input', function () {
    currentSearch = this.value;
    renderCards();
});

document.querySelectorAll('.status-filter').forEach(btn => {
    btn.addEventListener('click', function () {
        document.querySelectorAll('.status-filter').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        currentStatus = this.dataset.status;
        renderCards();
    });
});

function startSharingLocation(orderId) {
    console.log('startSharingLocation called for:', orderId);

    if (!navigator.geolocation) {
        alert('GPS not supported on this device.');
        console.log('GPS not supported');
        return;
    }

    console.log('GPS supported, starting interval...');
    stopSharingLocation();

    // Ask for permission immediately before interval
    navigator.geolocation.getCurrentPosition(pos => {
        console.log('Initial GPS position:', pos.coords.latitude, pos.coords.longitude);
    }, err => {
        console.warn('Initial GPS error:', err.code, err.message);
        alert('GPS error: ' + err.message);
    });

    gpsInterval = setInterval(() => {
        navigator.geolocation.getCurrentPosition(pos => {
            console.log('Sending GPS:', pos.coords.latitude, pos.coords.longitude);

            fetch('/Rider/UpdateLocation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    orderId:   orderId,
                    latitude:  pos.coords.latitude,
                    longitude: pos.coords.longitude
                })
            })
            .then(res => console.log('Location sent, status:', res.status))
            .catch(err => console.error('Fetch error:', err));

        }, err => {
            console.warn('GPS error:', err.code, err.message);
        }, {
            enableHighAccuracy: true,
            timeout:            10000,
            maximumAge:         0
        });
    }, 5000);
}

loadOrders();