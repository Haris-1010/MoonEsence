(() => {
    const csrfToken = document.querySelector('meta[name="csrf-token"]')?.getAttribute("content") || "";

    $.ajaxSetup({
        headers: { "X-CSRF-TOKEN": csrfToken }
    });

    const cartOverlay = document.getElementById("cartOverlay");
    const cartDrawer = document.getElementById("cartDrawer");
    const cartToggle = document.getElementById("cartToggle");
    const cartClose = document.getElementById("cartClose");

    function openCart() {
        if (cartOverlay) cartOverlay.hidden = false;
        if (cartDrawer) cartDrawer.classList.add("is-open");
    }

    function closeCart() {
        if (cartOverlay) cartOverlay.hidden = true;
        if (cartDrawer) cartDrawer.classList.remove("is-open");
    }

    if (cartToggle) cartToggle.addEventListener("click", () => openCart());
    if (cartClose) cartClose.addEventListener("click", () => closeCart());
    if (cartOverlay) cartOverlay.addEventListener("click", () => closeCart());

    function statusText(statusInt) {
        // Matches Domain.Enums.OrderStatus underlying values.
        const map = {
            0: "Placed",
            1: "Paid",
            2: "Processing",
            3: "Shipped",
            4: "Delivered",
            5: "Cancelled"
        };
        return map[statusInt] ?? `Status ${statusInt}`;
    }

    async function updateCartUI() {
        const badge = document.getElementById("cartBadge");
        const itemsEl = document.getElementById("cartItems");
        const subtotalEl = document.getElementById("cartSubtotal");
        const totalItemsEl = document.getElementById("cartTotalItems");

        if (!itemsEl || !badge || !subtotalEl || !totalItemsEl) return; // page doesn't show cart

        try {
            const res = await $.getJSON("/Cart/Get");
            const summary = res.summary;
            const lines = summary.lines || [];

            badge.textContent = summary.totalItems ?? 0;
            totalItemsEl.textContent = summary.totalItems ?? 0;
            subtotalEl.textContent = (summary.subtotal ?? 0).toFixed(2);

            if (!lines.length) {
                itemsEl.innerHTML = `<div class="emptyState" style="padding:12px;color:rgba(229,231,235,.8)">Your cart is empty.</div>`;
                return;
            }

            itemsEl.innerHTML = lines.map(l => {
                const qty = l.quantity ?? 1;
                return `
                    <div class="cartItem" data-product-id="${l.productId}">
                        <img src="${l.imageUrl}" alt="${escapeHtml(l.productName)}" />
                        <div>
                            <p class="cartItemTitle">${escapeHtml(l.productName)}</p>
                            <div class="cartItemMeta">$${(l.unitPrice ?? 0).toFixed(2)} each</div>

                            <div class="cartQtyRow">
                                <div class="qtyStepper">
                                    <button type="button" class="qtyBtn" data-action="dec" aria-label="Decrease quantity">-</button>
                                    <span class="qtyValue">${qty}</span>
                                    <button type="button" class="qtyBtn" data-action="inc" aria-label="Increase quantity">+</button>
                                </div>
                                <button type="button" class="removeBtn" aria-label="Remove item">Remove</button>
                            </div>
                        </div>
                    </div>
                `;
            }).join("");
        } catch {
            // Most pages are public; if not logged in, /Cart/Get will be unauthorized.
        }
    }

    function escapeHtml(str) {
        return String(str).replace(/[&<>"']/g, (m) => ({
            "&": "&amp;",
            "<": "&lt;",
            ">": "&gt;",
            "\"": "&quot;",
            "'": "&#039;"
        }[m]));
    }

    // Cart line interactions (delegate)
    $(document).on("click", ".addToCartBtn", async function () {
        const productId = Number($(this).data("product-id"));
        const qty = Number($(this).data("qty") || 1);
        if (!productId || qty < 1) return;

        try {
            const res = await $.post("/Cart/Add", { productId, quantity: qty });
            if (res?.summary) {
                await updateCartUI();
                openCart();
            }
        } catch (err) {
            // If the user is not authenticated, redirect is handled by cookie auth.
        }
    });

    // Buy now: add to cart then redirect to checkout
    $(document).on("click", ".buyNowBtn", async function () {
        const productId = Number($(this).data("product-id"));
        const qty = Number($(this).data("qty") || 1);
        if (!productId || qty < 1) return;

        try {
            await $.post("/Cart/Add", { productId, quantity: qty });
        } catch {
            // even if add fails, let user try again from cart
        }

        window.location.href = "/Checkout";
    });

    $(document).on("click", "#cartItems .removeBtn", async function () {
        const itemEl = $(this).closest(".cartItem");
        const productId = Number(itemEl.data("product-id"));
        if (!productId) return;
        await $.post("/Cart/Remove", { productId });
        await updateCartUI();
    });

    $(document).on("click", "#cartItems .qtyBtn", async function () {
        const itemEl = $(this).closest(".cartItem");
        const productId = Number(itemEl.data("product-id"));
        const qtyEl = itemEl.find(".qtyValue");
        const currentQty = Number(qtyEl.text());
        const action = $(this).data("action");

        let nextQty = currentQty;
        if (action === "inc") nextQty = Math.min(99, currentQty + 1);
        if (action === "dec") nextQty = Math.max(0, currentQty - 1);

        if (!productId) return;
        await $.post("/Cart/Update", { productId, quantity: nextQty });
        await updateCartUI();
    });

    // Category + subcategory filtering (mobile-first)
    async function loadProducts(categoryId, subCategoryId, page) {
        const cat = categoryId ? Number(categoryId) : null;
        const sub = subCategoryId ? Number(subCategoryId) : null;
        const res = await $.get("/Catalog/Products", { categoryId: cat, subCategoryId: sub, page });
        $("#productGrid").html(res);
    }

    async function loadSubCategories(categoryId) {
        const cat = categoryId ? Number(categoryId) : null;
        // If no category selected, just reset to "All subcategories".
        if (!cat) {
            const bar = document.querySelector(".subFilterBar");
            if (!bar) return;
            bar.innerHTML = `
                <button type="button" class="filterChip is-active" data-subcategory-id="">All subcategories</button>
            `;
            return;
        }

        try {
            const res = await $.getJSON("/Catalog/SubCategories", { categoryId: cat });
            const items = res?.items || [];

            const bar = document.querySelector(".subFilterBar");
            if (!bar) return;

            const chips = items.map(sc => {
                return `<button type="button" class="filterChip" data-subcategory-id="${sc.id}">${escapeHtml(sc.name)}</button>`;
            }).join("");

            bar.innerHTML = `
                <button type="button" class="filterChip is-active" data-subcategory-id="">All subcategories</button>
                ${chips}
            `;
        } catch {
            // keep existing chips on error
        }
    }

    // Category chips
    $(document).on("click", ".filterBar:not(.subFilterBar) .filterChip", async function () {
        const categoryId = $(this).attr("data-category-id");

        document.querySelectorAll(".filterBar:not(.subFilterBar) .filterChip").forEach(el => el.classList.remove("is-active"));
        $(this).addClass("is-active");

        // Reset subcategory filter
        await loadSubCategories(categoryId || null);
        await loadProducts(categoryId || null, null, 1);

        window.scrollTo({ top: 0, behavior: "smooth" });
    });

    // Subcategory chips
    $(document).on("click", ".subFilterBar .filterChip", async function () {
        const subCategoryId = $(this).attr("data-subcategory-id");

        document.querySelectorAll(".subFilterBar .filterChip").forEach(el => el.classList.remove("is-active"));
        $(this).addClass("is-active");

        // Determine selected category
        const activeCategory = document.querySelector(".filterBar:not(.subFilterBar) .filterChip.is-active")?.getAttribute("data-category-id");
        await loadProducts(activeCategory || null, subCategoryId || null, 1);

        window.scrollTo({ top: 0, behavior: "smooth" });
    });

    $(document).on("click", ".pagination .pageLink", async function (e) {
        e.preventDefault();
        const page = Number($(this).attr("data-page"));
        if (!page) return;
        const activeCategory = document.querySelector(".filterBar:not(.subFilterBar) .filterChip.is-active")?.getAttribute("data-category-id");
        const activeSub = document.querySelector(".subFilterBar .filterChip.is-active")?.getAttribute("data-subcategory-id");
        await loadProducts(activeCategory || null, activeSub || null, page);
    });

    // SignalR: live order status updates (works for guests via accessCode)
    async function initSignalROrderStatus() {
        const el = document.querySelector("[data-order-id][data-access-code]");
        const orderId = el ? Number(el.getAttribute("data-order-id")) : null;
        const accessCode = el ? el.getAttribute("data-access-code") : null;

        if (!window.signalR || !orderId || !accessCode) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`/hubs/order?accessCode=${encodeURIComponent(accessCode)}`)
            .build();

        connection.on("orderStatusChanged", (payload) => {
            const incomingOrderId = Number(payload.orderId);
            if (incomingOrderId && incomingOrderId === orderId) {
                const statusEl = document.getElementById("orderStatusText");
                if (statusEl) statusEl.textContent = statusText(payload.newStatus);
            }
        });

        await connection.start();
    }

    async function initAdminSignalR() {
        const hasAdmin = document.getElementById("newOrders");
        if (!hasAdmin || !window.signalR) return;

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/order")
            .build();

        connection.on("orderPlaced", (payload) => {
            const list = document.getElementById("newOrders");
            if (!list) return;

            const item = document.createElement("div");
            item.className = "newOrder";
            item.textContent = `New order #${payload.orderId} placed`;
            list.prepend(item);
        });

        await connection.start();
    }

    $(async function () {
        const isAuthed = document.body?.dataset?.authenticated === "true";

        // Cart drawer works for guests too (session-based).
        await updateCartUI();

        // Order detail pages work for guests via access code.
        await initSignalROrderStatus();

        if (isAuthed)
            await initAdminSignalR();
    });
})();

