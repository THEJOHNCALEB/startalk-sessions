(function () {
  'use strict';

  var CURRENCIES = { USD: { s: '$', r: 1 }, EUR: { s: '€', r: 0.92 }, GBP: { s: '£', r: 0.79 }, NGN: { s: '₦', r: 1550 }, AED: { s: 'د.إ', r: 3.67 } };
  var FREE_SHIP = 1500;

  var activeCurrency = localStorage.getItem('sass_currency') || 'NGN';

  function money(usd) {
    var cur = CURRENCIES[activeCurrency] || CURRENCIES.USD;
    var val = (parseFloat(usd) || 0) * cur.r;
    var decimals = activeCurrency === 'NGN' ? 0 : (val % 1 === 0 ? 0 : 2);
    return cur.s + val.toLocaleString('en-US', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
  }

  function renderPrices(root) {
    (root || document).querySelectorAll('[data-usd]').forEach(function (el) {
      var v = el.getAttribute('data-usd');
      if (el.classList.contains('price-compare') && parseFloat(v) === 0) { el.textContent = ''; return; }
      el.textContent = money(v);
    });
  }

  function setCurrency(code) {
    activeCurrency = code;
    localStorage.setItem('sass_currency', code);
    var sel = document.querySelector('[data-currency] select');
    if (sel) sel.value = code;
    renderPrices();
    renderCart();
  }

  function store(key, val) {
    localStorage.setItem(key, val ? JSON.stringify(val) : '');
  }
  function load(key) {
    try { return JSON.parse(localStorage.getItem(key) || '[]'); } catch (e) { return []; }
  }

  var cart = load('sass_cart');
  var wishlist = load('sass_wishlist');

  function uniqueId() { return Math.random().toString(36).slice(2, 10); }

  function saveCart() { store('sass_cart', cart); renderCart(); }
  function saveWishlist() { store('sass_wishlist', wishlist); renderWishlist(); }

  function addToCart(item, variants) {
    var qty = parseInt(item.qty, 10) || 1;
    var price = parseFloat(item.price) || 0;
    var v = variants || [];
    var existing = cart.find(function (c) {
      return c.slug === item.slug && (v.map(function (x) { return x.name; }).join('|') === (c.variants || []).map(function (x) { return x.name; }).join('|'));
    });
    if (existing) { existing.qty += qty; }
    else {
      cart.push({
        id: uniqueId(), slug: item.slug, name: item.name, image: item.image || '',
        price: price + (item.delta || 0), base: price, category: item.category || '', url: item.url || ('/shop/' + item.slug),
        variants: v, qty: qty
      });
    }
    saveCart();
    openOverlay('cart');
    pulseBadge('[data-cart-count]');
  }

  function cartCount() { return cart.reduce(function (n, c) { return n + c.qty; }, 0); }
  function cartSubtotal() { return cart.reduce(function (n, c) { return n + (parseFloat(c.price) || 0) * c.qty; }, 0); }

  function renderCart() {
    var badge = document.querySelector('[data-cart-count]');
    var count = cartCount();
    if (badge) {
      badge.hidden = count === 0;
      if (count > 0) badge.textContent = count > 99 ? '99+' : count;
    }
    var wrap = document.querySelector('[data-cart-items]');
    var foot = document.querySelector('[data-cart-foot]');
    var empty = document.querySelector('[data-cart-empty]');
    if (!wrap) return;

    if (cart.length === 0) {
      if (empty) empty.classList.add('show');
      if (foot) foot.hidden = true;
      wrap.innerHTML = '';
      updateProgress(0);
      return;
    }
    if (empty) empty.classList.remove('show');
    wrap.innerHTML = cart.map(function (c) {
      var varLine = (c.variants || []).map(function (v) { return v.name; }).join(' · ');
      return '<div class="flex items-center gap-3 py-4" data-cart-row="' + c.id + '">' +
        (c.image ? '<img src="' + c.image + '" alt="" loading="lazy" class="h-16 w-16 shrink-0 rounded-lg border border-brand-100 object-cover">' : '') +
        '<div class="min-w-0 flex-1"><a class="block truncate text-sm font-medium text-brand-900 hover:underline" href="' + c.url + '">' + c.name + '</a>' +
        (varLine ? '<span class="block truncate text-xs text-neutral-400">' + varLine + '</span>' : '') +
        '<div class="mt-1.5 inline-flex items-center gap-2 rounded-lg border border-brand-200"><button type="button" class="px-2 py-0.5 text-neutral-500 hover:text-brand-900" data-cart-minus="' + c.id + '">−</button><span class="min-w-4 text-center text-xs font-semibold text-brand-900">' + c.qty + '</span><button type="button" class="px-2 py-0.5 text-neutral-500 hover:text-brand-900" data-cart-plus="' + c.id + '">+</button></div>' +
        '</div>' +
        '<span class="text-sm font-bold text-brand-900">' + money(c.price) + '</span>' +
        '<button type="button" class="px-1 text-neutral-400 transition hover:text-brand-900" data-cart-remove="' + c.id + '" aria-label="Remove">×</button>' +
        '</div>';
    }).join('');
    if (foot) {
      foot.hidden = false;
      document.querySelector('[data-cart-subtotal]').textContent = money(cartSubtotal());
    }
    updateProgress(cartSubtotal());
  }

  function updateProgress(subtotal) {
    var bar = document.querySelector('[data-cart-progress-bar]');
    var label = document.querySelector('[data-cart-progress-label]');
    var wrap = document.querySelector('[data-cart-progress]');
    if (!bar) return;
    var pct = Math.min(100, subtotal / FREE_SHIP * 100);
    bar.style.width = pct + '%';
    if (label) label.textContent = subtotal >= FREE_SHIP ? 'Free white-glove delivery unlocked' : money(FREE_SHIP - subtotal) + ' away from free white-glove delivery';
    if (wrap) wrap.hidden = false;
  }

  function wishlistCount() { return wishlist.length; }

  function renderWishlist() {
    var badge = document.querySelector('[data-wishlist-count]');
    var count = wishlistCount();
    if (badge) {
      badge.hidden = count === 0;
      if (count > 0) badge.textContent = count > 99 ? '99+' : count;
    }
    var wrap = document.querySelector('[data-wishlist-items]');
    var stat = document.querySelector('[data-wishlist-stat]');
    if (stat) stat.textContent = count;
    if (!wrap) return;
    if (wishlist.length === 0) {
      var empty = document.querySelector('[data-wishlist-empty]');
      if (empty) empty.classList.add('show');
      wrap.innerHTML = '';
      return;
    }
    document.querySelectorAll('[data-wishlist-empty]').forEach(function (e) { e.classList.remove('show'); });
    wrap.innerHTML = wishlist.map(function (w) {
      var isInCart = cart.some(function (c) { return c.slug === w.slug; });
      return '<div class="wish-item flex items-start gap-3 py-4" data-wish-row="' + (w.slug || '') + '">' +
        (w.image ? '<img src="' + w.image + '" alt="" loading="lazy" class="h-14 w-14 shrink-0 rounded-lg border border-brand-100 object-cover">' : '') +
        '<div class="wi-info min-w-0 flex-1"><a class="block truncate text-sm font-medium text-brand-900 hover:underline" href="' + w.url + '">' + w.name + '</a>' +
        '<span class="block truncate text-xs text-neutral-400">' + (w.category || '') + '</span></div>' +
        (w.price ? '<strong class="text-sm font-bold text-brand-900">' + money(w.price) + '</strong>' : '') +
        (w.price
          ? (isInCart
              ? '<button type="button" class="shrink-0 rounded-lg border border-brand-200 px-2.5 py-1.5 text-xs font-semibold text-neutral-500" data-wish-in-cart>In cart ✓</button>'
              : '<button type="button" class="shrink-0 rounded-lg bg-brand-700 px-2.5 py-1.5 text-xs font-semibold text-white" data-wish-atc=\'' + JSON.stringify(w).replace(/'/g, '&#39;') + '\'>Add to cart</button>')
          : '') +
        '<button type="button" class="px-1 text-neutral-400 transition hover:text-brand-900" data-wish-remove="' + (w.slug || '') + '" aria-label="Remove">×</button>' +
        '</div>';
    }).join('');
  }

  function toggleWishlist(slug, meta) {
    var idx = wishlist.findIndex(function (w) { return w.slug === slug; });
    if (idx >= 0) { wishlist.splice(idx, 1); }
    else {
      var item = Object.assign({ slug: slug }, meta || {});
      var card = document.querySelector('[data-product-card][data-slug="' + slug + '"]');
      if (card && !item.name) {
        var link = card.querySelector('.product-link'); if (link) item.url = link.getAttribute('href');
        var name = card.querySelector('.product-name'); if (name) item.name = name.textContent.trim();
        var img = card.querySelector('.img-main'); if (img) item.image = img.getAttribute('src');
        var priceEl = card.querySelector('.price[data-usd]'); if (priceEl) item.price = priceEl.getAttribute('data-usd');
        var cat = card.querySelector('.product-category'); if (cat) item.category = cat.textContent.trim();
      }
      wishlist.push(item);
    }
    saveWishlist();
    syncHearts();
  }
  window.toggleWishlist = toggleWishlist;

  function syncHearts() {
    document.querySelectorAll('[data-wishlist-toggle]').forEach(function (btn) {
      var slug = btn.getAttribute('data-wishlist-toggle');
      var active = wishlist.some(function (w) { return w.slug === slug; });
      btn.classList.toggle('active', active);
    });
  }

  function pulseBadge(sel) {
    var el = document.querySelector(sel);
    if (!el) return;
    el.style.transform = 'scale(1.35)';
    setTimeout(function () { el.style.transform = ''; }, 300);
  }

  function openOverlay(name) {
    var ov = document.querySelector('[data-overlay="' + name + '"]');
    if (!ov) return;
    ov.classList.add('open');
    ov.setAttribute('aria-hidden', 'false');
    document.body.style.overflow = 'hidden';
    if (name === 'search') {
      var inp = document.querySelector('[data-search-input]');
      if (inp) { inp.focus(); showSearchHints(); }
    }
  }
  function closeOverlay(ov) {
    ov.classList.remove('open');
    ov.setAttribute('aria-hidden', 'true');
    if (!document.querySelector('.overlay.open')) document.body.style.overflow = '';
  }
  window.openOverlay = openOverlay;
  window.closeOverlay = closeOverlay;

  function showSearchHints() {
    var hints = document.querySelector('[data-search-hints]');
    var results = document.querySelector('[data-search-results]');
    if (hints) hints.style.display = '';
    if (results) results.innerHTML = '';
  }

  function runSearch(q) {
    var hints = document.querySelector('[data-search-hints]');
    var results = document.querySelector('[data-search-results]');
    if (!q) { showSearchHints(); return; }
    if (hints) hints.style.display = 'none';
    if (!results) return;
    results.innerHTML = '<p class="px-3 py-2 text-sm text-neutral-400">Searching…</p>';
    fetch('/Shop/SearchJson?q=' + encodeURIComponent(q), { headers: { 'Accept': 'application/json' } })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        var res = data.results || [];
        if (res.length === 0) {
          results.innerHTML = '<p class="px-3 py-2 text-sm text-neutral-400">No matches for "' + escapeHtml(q) + '". Try "sectional", "walnut" or "office".</p>';
          return;
        }
        results.innerHTML = res.map(function (r) {
          return '<a class="search-result flex items-center gap-3 rounded-xl px-3 py-2.5 transition hover:bg-brand-50" href="' + r.url + '">' +
            (r.image ? '<img src="' + r.image + '" alt="" loading="lazy" class="h-11 w-11 shrink-0 rounded-lg object-cover">' : '<span class="sr-type flex h-11 w-11 shrink-0 items-center justify-center rounded-lg bg-brand-100 text-xs font-bold uppercase text-neutral-500">' + r.type + '</span>') +
            '<div class="min-w-0 flex-1"><span class="sr-name block truncate text-sm font-medium text-brand-900">' + escapeHtml(r.name) + '</span>' +
            '<span class="sr-sub block truncate text-xs text-neutral-400">' + (r.type === 'post' ? 'Journal · ' : '') + escapeHtml(r.sub || '') + '</span></div>' +
            (r.price ? '<span class="sr-price text-sm font-semibold text-brand-900">' + money(r.price) + '</span>' : '') +
            '</a>';
        }).join('');
      })
      .catch(function () {
        results.innerHTML = '<p class="px-3 py-2 text-sm text-neutral-400">Search is unavailable right now.</p>';
      });
  }

  function escapeHtml(s) {
    var d = document.createElement('div');
    d.textContent = String(s == null ? '' : s);
    return d.innerHTML;
  }

  function renderQuickview(payload) {
    var root = document.querySelector('[data-quickview-root]');
    if (!root) return;
    var cta = payload.price
      ? '<button type="button" class="flex-1 rounded-lg bg-brand-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-brand-500" data-add-to-cart=\'' + escapeJson(payload) + '\'>Add to Cart</button>' +
        '<button type="button" class="flex h-11 w-11 items-center justify-center rounded-lg border border-brand-200 text-neutral-700" data-wishlist-toggle="' + payload.slug + '" aria-label="Wishlist"><svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 20s-7-4.6-9.2-9A5.2 5.2 0 0 1 12 6.6 5.2 5.2 0 0 1 21.2 11C19 15.4 12 20 12 20z"/></svg></button>'
      : '<a href="/custom-furniture" class="flex-1 rounded-lg bg-brand-700 px-4 py-2.5 text-center text-sm font-semibold text-white transition hover:bg-brand-500">Request a Quote</a>';
    root.innerHTML =
      '<div class="grid grid-cols-1 gap-6 sm:grid-cols-2">' +
      '<div class="overflow-hidden rounded-xl border border-brand-100"><img src="' + payload.image + '" alt="' + escapeHtml(payload.name) + '" class="h-full w-full object-cover"></div>' +
      '<div class="quickview-info">' +
      '<span class="product-category text-xs font-semibold uppercase tracking-widest text-neutral-400">' + escapeHtml(payload.category) + '</span>' +
      '<h3 class="mt-2 text-xl font-bold tracking-tight text-brand-900">' + escapeHtml(payload.name) + '</h3>' +
      '<div class="price-price mt-2 text-2xl font-black text-brand-900">' + money(payload.price) + '</div>' +
      '<p class="mt-3 text-sm leading-relaxed text-neutral-500">Configure materials and sizes on the product page, then add to cart with your exact spec.</p>' +
      '<div class="quickview-actions mt-5 flex gap-2">' + cta + '</div>' +
      '<div class="folder mt-5"><a href="/shop/' + payload.slug + '" class="text-sm font-semibold text-brand-900 underline underline-offset-4">View full details →</a></div>' +
      '</div></div>';
    syncHearts();
  }

  function escapeJson(obj) {
    return JSON.stringify(obj).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/'/g, '&#39;');
  }

  function queryParam(url, key, value) {
    var u = new URL(url, window.location.origin);
    if (value === '' || value === null || value === undefined) u.searchParams.delete(key);
    else u.searchParams.set(key, value);
    return u.pathname + '?' + u.search;
  }

  document.addEventListener('DOMContentLoaded', function () {

    var curSel = document.querySelector('[data-currency] select');
    if (curSel) {
      curSel.value = activeCurrency;
      curSel.addEventListener('change', function () { setCurrency(this.value); });
    }

    renderPrices();
    renderCart();
    renderWishlist();
    syncHearts();

    document.addEventListener('click', function (e) {
      var openBtn = e.target.closest('[data-trigger-search]');
      if (openBtn) { e.preventDefault(); openOverlay('search'); return; }

      var cartBtn = e.target.closest('[data-trigger-cart]');
      if (cartBtn) { e.preventDefault(); openOverlay('cart'); return; }

      var wishBtn = e.target.closest('[data-trigger-wishlist]');
      if (wishBtn) { e.preventDefault(); openOverlay('wishlist'); return; }

      var close = e.target.closest('[data-close-overlay]');
      if (close) {
        var ovName = close.getAttribute('data-close-overlay');
        var ovEl = document.querySelector('[data-overlay="' + ovName + '"]');
        if (ovEl) closeOverlay(ovEl);
        return;
      }
      if (e.target.classList.contains('overlay')) { closeOverlay(e.target); return; }

      var atc = e.target.closest('[data-add-to-cart]');
      if (atc) {
        e.preventDefault();
        try { addToCart(JSON.parse(atc.getAttribute('data-add-to-cart'))); }
        catch (err) { addToCart({ slug: atc.getAttribute('data-add-to-cart') }); }
        return;
      }

      var qv = e.target.closest('[data-quick-view]');
      if (qv) {
        e.preventDefault();
        var payload = JSON.parse(qv.getAttribute('data-quick-view').replace(/&quot;/g, '"'));
        renderQuickview(payload);
        openOverlay('quickview');
        return;
      }

      var wt = e.target.closest('[data-wishlist-toggle]');
      if (wt) {
        e.preventDefault();
        toggleWishlist(wt.getAttribute('data-wishlist-toggle'));
        return;
      }

      var wishAtc = e.target.closest('[data-wish-atc]');
      if (wishAtc) {
        e.preventDefault();
        var witem = JSON.parse(wishAtc.getAttribute('data-wish-atc').replace(/&#39;/g, "'"));
        addToCart(witem);
        return;
      }

      var wremove = e.target.closest('[data-wish-remove]');
      if (wremove) {
        var slug = wremove.getAttribute('data-wish-remove');
        wishlist = wishlist.filter(function (w) { return w.slug !== slug; });
        saveWishlist();
        syncHearts();
        return;
      }

      var cminus = e.target.closest('[data-cart-minus]');
      if (cminus) { updateQty(cminus.getAttribute('data-cart-minus'), -1); return; }
      var cplus = e.target.closest('[data-cart-plus]');
      if (cplus) { updateQty(cplus.getAttribute('data-cart-plus'), 1); return; }

      var cremove = e.target.closest('[data-cart-remove]');
      if (cremove) {
        cart = cart.filter(function (c) { return c.id !== cremove.getAttribute('data-cart-remove'); });
        saveCart();
        return;
      }
    });

    function updateQty(id, delta) {
      var row = cart.find(function (c) { return c.id === id; });
      if (!row) return;
      row.qty += delta;
      if (row.qty <= 0) cart = cart.filter(function (c) { return c.id !== id; });
      saveCart();
    }

    var checkout = document.querySelector('[data-checkout-btn]');
    if (checkout) {
      checkout.addEventListener('click', function (e) {
        e.preventDefault();
        if (cart.length === 0) { openOverlay('cart'); return; }
        var btn = checkout;
        btn.textContent = 'Placing order…';
        var payload = {
          customerName: '',
          customerPhone: '',
          shippingAddress: '',
          country: '',
          currency: activeCurrency,
          json: JSON.stringify(cart.map(function (c) {
            return { productId: '', productName: c.name, sku: '', variants: (c.variants || []).map(function (v) { return v.name; }).join(' · '), quantity: c.qty, unitPrice: c.price, image: c.image || '' };
          }))
        };
        fetch('/Account/PlaceOrder', {
          method: 'POST',
          headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
          body: Object.keys(payload).map(function (k) { return encodeURIComponent(k) + '=' + encodeURIComponent(payload[k]); }).join('&')
        }).then(function (res) {
          if (res.status === 401) { window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent('/account'); return; }
          return res.json();
        }).then(function (data) {
          if (!data) return;
          if (data.ok) {
            cart = [];
            saveCart();
            checkout.textContent = 'Order placed ✓';
            var foot = document.querySelector('[data-cart-foot]');
            if (foot) foot.innerHTML = '<div class="flex items-center justify-between"><span class="text-sm text-neutral-600">Order confirmed</span><strong class="text-sm font-bold text-brand-900">' + data.orderNumber + '</strong></div><p class="mt-2 text-xs text-neutral-400">Tracking: <strong class="text-neutral-700">' + data.trackingRef + '</strong>. See <a href="/account/Orders" class="underline underline-offset-2">Orders</a> for updates.</p>';
          } else {
            checkout.textContent = 'Checkout securely';
          }
        }).catch(function () {
          checkout.textContent = 'Checkout securely';
        });
      });
    }

    var searchForm = document.querySelector('[data-search-form]');
    var searchInput = document.querySelector('[data-search-input]');
    if (searchForm) {
      searchForm.addEventListener('submit', function (e) { e.preventDefault(); });
      searchInput.addEventListener('input', function () { runSearch(this.value); });
      document.querySelectorAll('[data-search-chip]').forEach(function (chip) {
        chip.addEventListener('click', function () {
          searchInput.value = chip.textContent.trim();
          runSearch(searchInput.value);
        });
      });
    }

    document.addEventListener('keydown', function (e) {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        openOverlay('search');
        return;
      }
      if (e.key === 'Escape') {
        document.querySelectorAll('.overlay.open').forEach(closeOverlay);
        var res = document.querySelector('[data-search-results]');
        if (res) res.innerHTML = '';
        var hs = document.querySelector('[data-search-hints]');
        if (hs) hs.style.display = '';
      }
    });

    var filterForm = document.querySelector('[data-filter-form]');
    var openFilters = document.querySelector('[data-open-filters]');
    var closeFilters = document.querySelector('[data-close-filters]');
    var filterPanel = document.querySelector('[data-filter-panel]');
    if (openFilters) openFilters.addEventListener('click', function () { filterPanel.classList.add('open'); });
    if (closeFilters) closeFilters.addEventListener('click', function () { filterPanel.classList.remove('open'); });

    var sortSelect = document.querySelector('[data-sort-select]');
    if (sortSelect) {
      sortSelect.addEventListener('change', function () {
        var form = filterForm;
        if (form) {
          var url = new URL(form.action, window.location.origin);
          url.search = new URLSearchParams(new FormData(form)).toString();
          if (this.value && this.value !== 'featured') url.searchParams.set('sort', this.value);
          else url.searchParams.delete('sort');
          window.location.href = url.pathname + '?' + url.search;
        }
      });
    }

    document.querySelectorAll('[data-before-after]').forEach(function (ba) {
      var range = ba.querySelector('[data-ba-range]');
      var handle = ba.querySelector('[data-ba-handle]');
      var after = ba.querySelector('.ba-after');
      function set(v) {
        if (after) after.style.setProperty('--ba', v + '%');
        if (handle) handle.style.setProperty('--ba', v + '%');
      }
      if (range) {
        range.addEventListener('input', function () { set(this.value); });
        set(range.value);
      }
    });

    document.querySelectorAll('[data-accordion] .acc-head').forEach(function (head) {
      head.addEventListener('click', function () {
        head.parentElement.classList.toggle('open');
      });
    });

    var thumbs = document.querySelector('[data-thumbs]');
    var mainImage = document.querySelector('[data-main-image]');
    var lightbox = document.querySelector('[data-lightbox]');
    var lightboxContent = document.querySelector('[data-lightbox-content]');
    var videoExts = ['.mp4', '.webm', '.mov', '.m4v'];

    function isVid(u) {
      u = String(u || '').toLowerCase();
      return videoExts.some(function (e) { return u.indexOf(e) > -1; });
    }

    function setMainMedia(url) {
      if (!mainImage) return;
      var wrap = mainImage.parentElement;
      var el;
      if (isVid(url)) {
        el = document.createElement('video');
        el.controls = true;
        el.playsInline = true;
        el.preload = 'metadata';
        el.className = 'h-auto max-h-[40rem] w-full';
        el.src = url;
      } else {
        el = document.createElement('img');
        el.loading = 'lazy';
        el.className = 'h-auto max-h-[40rem] w-full cursor-zoom-in object-contain';
        el.setAttribute('data-open-lightbox', '');
        el.alt = mainImage.getAttribute('alt') || '';
        el.src = url;
      }
      el.id = 'mainImage';
      el.setAttribute('data-main-image', '');
      mainImage.replaceWith(el);
      mainImage = el;
      bindLightboxOpen();
    }

    function openLightbox(url) {
      if (!lightbox || !lightboxContent) return;
      if (isVid(url)) {
        lightboxContent.innerHTML = '<video src="' + url + '" controls autoplay playsinline class="max-h-[88vh] w-auto rounded-xl bg-black"></video>';
      } else {
        lightboxContent.innerHTML = '<img src="' + url + '" alt="Full-size view" class="max-h-[88vh] w-auto max-w-full rounded-xl object-contain">';
      }
      openOverlay('lightbox');
    }

    function bindLightboxOpen() {
      if (mainImage && mainImage.hasAttribute('data-open-lightbox')) {
        mainImage.addEventListener('click', function () {
          openLightbox(mainImage.src || (mainImage.currentSrc || ''));
        });
      }
    }
    bindLightboxOpen();

    if (thumbs) {
      thumbs.addEventListener('click', function (e) {
        var t = e.target.closest('[data-thumb]');
        if (!t) return;
        setMainMedia(t.getAttribute('data-thumb'));
        thumbs.querySelectorAll('.thumb').forEach(function (th) { th.classList.remove('active'); });
        t.classList.add('active');
      });
    }

    var closeLightbox = document.querySelector('[data-close-lightbox]');
    if (closeLightbox) closeLightbox.addEventListener('click', function () { closeOverlay(lightbox); });
    if (lightbox) lightbox.addEventListener('click', function (e) { if (e.target === lightbox) closeOverlay(lightbox); });

    setTimeout(function () {
      document.querySelectorAll('a[data-ad-view]').forEach(function (a) {
        var url = a.getAttribute('data-ad-view');
        if (!url || a.getAttribute('data-ad-tracked')) return;
        a.setAttribute('data-ad-tracked', '1');
        fetch(url, { method: 'GET' }).catch(function () {});
      });
    }, 1200);

    var config = document.querySelector('[data-config]');
    var pdpPrice = document.querySelector('[data-pdp-price]');
    if (config) {
      var selected = {};
      config.querySelectorAll('[data-opt]').forEach(function (btn) {
        btn.addEventListener('click', function () {
          var group = btn.getAttribute('data-opt');
          config.querySelectorAll('[data-opt="' + group + '"]').forEach(function (b) { b.classList.remove('active'); });
          btn.classList.add('active');
          selected[group] = {
            name: btn.getAttribute('data-name'),
            delta: parseFloat(btn.getAttribute('data-delta')) || 0
          };
          var field = config.querySelector('[data-var-field="' + group + '"]');
          if (field) field.value = btn.getAttribute('data-name');
          var label = config.querySelector('[data-var-label="' + group + '"]');
          if (label) label.textContent = btn.getAttribute('data-name') +
            (selected[group].delta ? '  (+' + money(selected[group].delta) + ')' : '');
          updatePdpPrice();
          updateAddBtns();
        });
      });
      updateAddBtns();
    }

    function selectedVariants() {
      return Object.keys(selected).map(function (k) { return { name: selected[k].name }; });
    }
    function currentDelta() {
      return Object.keys(selected).reduce(function (n, k) { return n + selected[k].delta; }, 0);
    }
    function updatePdpPrice() {
      if (!pdpPrice) return;
      var base = parseFloat(pdpPrice.getAttribute('data-base')) || 0;
      if (base <= 0) return;
      var priceEl = pdpPrice.querySelector('.price[data-usd]');
      if (priceEl && base > 0) priceEl.textContent = money(base + currentDelta());
    }
    function updateAddBtns() {
      var base = pdpPrice ? (parseFloat(pdpPrice.getAttribute('data-base')) || 0) : 0;
      var itemEl = document.querySelector('[data-item-json]');
      if (!itemEl) return;
      var item = JSON.parse(itemEl.getAttribute('data-item-json'));
      item.price = base + currentDelta();
      item.delta = currentDelta();
      document.querySelectorAll('[data-add-config-cart], [data-add-config-cart-sm]').forEach(function (b) {
        b.setAttribute('data-payload', escapeJson(item));
      });
    }

    document.addEventListener('click', function (e) {
      var cfgAtc = e.target.closest('[data-add-config-cart]');
      if (cfgAtc) {
        e.preventDefault();
        var payload = cfgAtc.getAttribute('data-payload');
        if (!payload) return;
        addToCart(JSON.parse(payload.replace(/&#39;/g, "'")), selectedVariants());
        return;
      }
      var buyNow = e.target.closest('[data-buy-now]');
      if (buyNow) {
        e.preventDefault();
        var item = JSON.parse(document.querySelector('[data-item-json]').getAttribute('data-item-json'));
        item.price = (parseFloat(pdpPrice.getAttribute('data-base')) || 0) + currentDelta();
        item.delta = currentDelta();
        addToCart(item, selectedVariants());
        var fo = document.querySelector('[data-cart-foot]');
        if (fo) fo.scrollIntoView({ behavior: 'smooth' });
        return;
      }
    });

    var shipForm = document.querySelector('[data-shipping-calc]');
    if (shipForm) {
      var shipPostal = shipForm.querySelector('[data-ship-postal]');
      var shipResult = shipForm.querySelector('[data-ship-result]');
      shipForm.querySelector('[data-ship-estimate]').addEventListener('click', function () {
        var zip = (shipPostal.value || '').trim();
        if (!zip) { shipResult.innerHTML = '<p class="muted">Enter a postal code to see a delivery estimate.</p>'; return; }
        var code = zip.toUpperCase().slice(0, 2);
        var zones = { US: 95, GB: 145, AE: 190, NG: 240, CA: 175 };
        var cost = zones[code] || Math.round(160 + Math.abs(code.length * 7));
        shipResult.innerHTML =
          '<div class="est-line"><span>Standard white-glove</span><strong>' + money(cost) + '</strong></div>' +
          '<div class="est-line"><span>Tracking &amp; insurance</span><strong>Included</strong></div>' +
          '<div class="est-line"><span>Estimated window</span><strong>7–12 business days</strong></div>';
      });
    }

    var builder = document.querySelector('[data-builder]');
    if (builder) {
      var steps = Array.prototype.slice.call(builder.querySelectorAll('[data-step]'));
      var dots = Array.prototype.slice.call(builder.querySelectorAll('[data-step-dot]'));
      var step = 0;
      var form = builder.querySelector('[data-builder-form]');
      var prevBtn = builder.querySelector('[data-builder-prev]');
      var nextBtn = builder.querySelector('[data-builder-next]');
      var submitBtn = builder.querySelector('[data-builder-submit]');

      function showStep(n) {
        step = n;
        steps.forEach(function (s, idx) { s.classList.toggle('active', idx === n); });
        dots.forEach(function (d, idx) {
          d.classList.toggle('active', idx === n);
          d.classList.toggle('done', idx < n);
        });
        prevBtn.disabled = n === 0;
        nextBtn.hidden = n === steps.length - 1;
        submitBtn.hidden = n !== steps.length - 1;
        submitBtn.disabled = false;
      }
      prevBtn.addEventListener('click', function () { if (step > 0) showStep(step - 1); });
      nextBtn.addEventListener('click', function () { if (step < steps.length - 1) showStep(step + 1); });

      var unitToggle = builder.querySelector('[data-unit-toggle]');
      if (unitToggle) {
        unitToggle.querySelectorAll('button').forEach(function (b) {
          b.addEventListener('click', function () {
            unitToggle.querySelectorAll('button').forEach(function (x) { x.classList.remove('active'); });
            b.classList.add('active');
            unitToggle.querySelector('[data-unit-field]').value = b.getAttribute('data-unit');
          });
        });
      }

      var drop = builder.querySelector('[data-drop-zone]');
      var refInput = builder.querySelector('[data-ref-input]');
      var preview = builder.querySelector('[data-ref-preview]');
      function renderFiles() {
        if (!preview) return;
        var names = Array.prototype.map.call(refInput.files || [], function (f) { return f.name; });
        preview.innerHTML = names.map(function (n) {
          return '<span class="ref-chip">📎 ' + escapeHtml(n) + '</span>';
        }).join('');
        if (refInput._listeners) return;
        refInput._listeners = true;
      }
      if (refInput) refInput.addEventListener('change', renderFiles);
      if (drop) {
        drop.addEventListener('dragover', function (e) { e.preventDefault(); drop.classList.add('drag'); });
        drop.addEventListener('dragleave', function () { drop.classList.remove('drag'); });
        drop.addEventListener('drop', function (e) {
          e.preventDefault();
          drop.classList.remove('drag');
          refInput.files = e.dataTransfer.files;
          renderFiles();
        });
      }
    }

    var newsletter = document.querySelector('[data-newsletter]');
    if (newsletter) {
      newsletter.addEventListener('submit', function (e) {
        e.preventDefault();
        var fb = newsletter.querySelector('[data-newsletter-feedback]');
        var input = newsletter.querySelector('input[name="email"]');
        var val = (input.value || '').trim();
        if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(val)) {
          if (fb) fb.textContent = 'Please enter a valid email address.';
          return;
        }
        fetch('/Newsletter/Subscribe', {
          method: 'POST',
          headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
          body: 'email=' + encodeURIComponent(val)
        }).then(function (r) { return r.json(); }).then(function (data) {
          if (fb) {
            fb.textContent = data.message || 'Thank you for subscribing!';
            if (data.ok) input.value = '';
          }
        }).catch(function () {
          if (fb) fb.textContent = 'Something went wrong — try again shortly.';
        });
      });
    }

    var serviceInquiry = document.querySelectorAll('[data-inquiry]');
    var inquiryOverlay = document.querySelector('[data-inquiry-overlay]');
    if (inquiryOverlay) {
      serviceInquiry.forEach(function (btn) {
        btn.addEventListener('click', function () {
          var t = btn.getAttribute('data-inquiry');
          try {
            var p = JSON.parse(t);
            var title = inquiryOverlay.querySelector('[data-inquiry-title]');
            if (title) title.textContent = 'Start a project — ' + p.title;
          } catch (err) {}
          openOverlay(inquiryOverlay.getAttribute('data-overlay'));
        });
      });
      inquiryOverlay.querySelector('[data-close-inquiry]').addEventListener('click', function () {
        closeOverlay(inquiryOverlay);
      });
    }

    function pushRecent() {
      var meta = document.querySelector('[data-pdp-meta]');
      var list = document.querySelector('[data-recent-viewed]');
      if (!meta) return;
      var item = JSON.parse(meta.getAttribute('data-pdp-meta'));
      var recent = load('sass_recent');
      recent = recent.filter(function (r) { return r.slug !== item.slug; });
      recent.unshift(item);
      recent = recent.slice(0, 4);
      store('sass_recent', recent);
      if (list) {
        list.innerHTML = recent.map(function (r) {
          return '<a class="product-card recent-card group flex flex-col overflow-hidden rounded-2xl border border-brand-200 bg-white transition hover:border-brand-300 hover:shadow-lg" href="' + r.url + '">' +
            '<div class="product-media relative"><img class="img-main h-40 w-full object-cover" src="' + r.image + '" alt="' + escapeHtml(r.name) + '"><span class="tag-custom absolute left-3 top-3 rounded-full bg-brand-700/90 px-2.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-white">Recently viewed</span></div>' +
            '<div class="product-info flex-1 p-4"><span class="product-category text-xs font-semibold uppercase tracking-widest text-neutral-400">' + escapeHtml(r.category || '') + '</span>' +
            '<h3 class="product-name mt-1 text-sm font-semibold text-brand-900">' + escapeHtml(r.name) + '</h3>' +
            (r.price ? '<span class="price mt-1 block text-sm font-bold text-brand-900" data-usd="' + r.price + '"></span>' : '<span class="price mt-1 block text-sm font-semibold text-neutral-500">Request Custom Quote</span>') +
            '</div></a>';
        }).join('');
        renderPrices(list);
      }
    }
    pushRecent();

    window.addEventListener('storage', function () {
      if (event.key === 'sass_currency') setCurrency(localStorage.getItem('sass_currency') || 'NGN');
    });
  });

  /* ---- Horizontal product rail ([data-carousel]) ---- */
  document.querySelectorAll('[data-carousel]').forEach(function (rail) {
    var track = rail.querySelector('[data-carousel-track]');
    if (!track) return;

    var scope = rail.closest('section') || document;
    var prev = scope.querySelector('[data-carousel-prev]');
    var next = scope.querySelector('[data-carousel-next]');

    function step() {
      var item = track.firstElementChild;
      if (!item) return track.clientWidth;
      var gap = parseFloat(getComputedStyle(track).columnGap || '0') || 0;
      return item.getBoundingClientRect().width + gap;
    }
    function maxScroll() { return track.scrollWidth - track.clientWidth; }
    function sync() {
      var atStart = track.scrollLeft <= 2;
      var atEnd = track.scrollLeft >= maxScroll() - 2;
      var scrollable = maxScroll() > 4;
      if (prev) prev.disabled = atStart || !scrollable;
      if (next) next.disabled = atEnd || !scrollable;
    }
    function go(dir) { track.scrollBy({ left: dir * step(), behavior: 'smooth' }); }

    if (prev) prev.addEventListener('click', function () { go(-1); });
    if (next) next.addEventListener('click', function () { go(1); });
    track.addEventListener('scroll', sync, { passive: true });
    window.addEventListener('resize', sync);
    sync();
  });
})();
  /* ---- Checkout page (/checkout) ---- */
  var checkoutPage = document.querySelector('[data-checkout-page]');
  if (checkoutPage) {
    var coItems = document.querySelector('[data-checkout-items]');
    var coSub = document.querySelector('[data-checkout-subtotal]');
    var coTotal = document.querySelector('[data-checkout-total]');
    var coCount = document.querySelector('[data-checkout-count]');
    var coCur = document.querySelector('#co-currency');
    var coForm = document.querySelector('[data-checkout-form]');

    function money2(v) { return money(v); }

    function renderCheckoutCart() {
      if (!coItems) return;
      var total = cartSubtotal();
      var items = cart;
      if (!items || items.length === 0) {
        coItems.innerHTML = '<p class="py-4 text-sm text-neutral-400">Your cart is empty. <a href="/shop" class="font-semibold text-brand-900 underline underline-offset-2">Browse the collection</a> to get started.</p>';
        if (coSub) coSub.textContent = money2(0);
        if (coTotal) coTotal.textContent = money2(0);
        if (coCount) coCount.textContent = '0 items';
        return;
      }
      coItems.innerHTML = items.map(function (c) {
        var varLine = (c.variants || []).map(function (v) { return v.name; }).join(' · ');
        return '<div class="co-item flex items-center gap-3 py-3">' +
          (c.image ? '<img class="co-item-img h-14 w-14 shrink-0 rounded-lg border border-brand-100 object-cover" src="' + c.image + '" alt="" loading="lazy">' : '<span class="co-item-img h-14 w-14 shrink-0 rounded-lg bg-brand-100"></span>') +
          '<div class="co-item-info min-w-0 flex-1"><span class="co-item-name block truncate text-sm font-medium text-brand-900">' + c.name + '</span>' +
          (varLine ? '<span class="co-item-var block truncate text-xs text-neutral-400">' + varLine + '</span>' : '') +
          '<span class="co-item-qty block text-xs text-neutral-500">Qty ' + c.qty + '</span></div>' +
          '<span class="co-item-price text-sm font-bold text-brand-900">' + money2(c.price * c.qty) + '</span></div>';
      }).join('');
      if (coSub) coSub.textContent = money2(total);
      if (coCount) coCount.textContent = items.length + (items.length === 1 ? ' item' : ' items');
      var freeNote = document.querySelector('[data-checkout-free-note]');
      if (freeNote) freeNote.textContent = money2(FREE_SHIP);
      if (coTotal) {
        var note = document.querySelector('[data-checkout-delivery-note]');
        coTotal.textContent = money2(total);
        if (note) note.textContent = total >= FREE_SHIP ? 'Free white-glove delivery included' : money2(FREE_SHIP - total) + ' from free delivery';
      }
    }

    if (coCur) {
      Object.keys(CURRENCIES).forEach(function (code) {
        var el = document.createElement('option');
        el.value = code;
        el.textContent = code + ' · ' + CURRENCIES[code].s + '/USD';
        el.selected = code === activeCurrency;
        coCur.appendChild(el);
      });
      coCur.addEventListener('change', function () {
        setCurrency(this.value);
        renderCheckoutCart();
      });
    }

    renderCheckoutCart();

    var coSubmitBtn = document.querySelector('[data-checkout-submit]');
    if (coForm && coSubmitBtn) {
      coForm.addEventListener('submit', function (e) {
        e.preventDefault();
        if (cart.length === 0) { renderCheckoutCart(); return; }
        var payload = {
          customerName: document.querySelector('[name="customerName"]')?.value || '',
          customerPhone: document.querySelector('[name="customerPhone"]')?.value || '',
          shippingAddress: document.querySelector('[name="shippingAddress"]')?.value || '',
          country: document.querySelector('[name="country"]')?.value || '',
          currency: activeCurrency,
          json: JSON.stringify(cart.map(function (c) {
            return { productId: '', productName: c.name, sku: '', variants: (c.variants || []).map(function (v) { return v.name; }).join(' · '), quantity: c.qty, unitPrice: c.price, image: c.image || '' };
          }))
        };
        var btn = coSubmitBtn;
        var original = btn.textContent;
        btn.disabled = true;
        btn.textContent = 'Placing order…';
        fetch('/Account/PlaceOrder', {
          method: 'POST',
          headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
          body: Object.keys(payload).map(function (k) { return encodeURIComponent(k) + '=' + encodeURIComponent(payload[k]); }).join('&')
        }).then(function (res) {
          if (res.status === 401) {
            window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent('/checkout');
            return;
          }
          return res.json();
        }).then(function (data) {
          if (!data) return;
          if (data.ok) {
            cart = [];
            saveCart();
            renderCheckoutCart();
            var orderNote = document.createElement('div');
            orderNote.className = 'rounded-2xl border border-emerald-200 bg-emerald-50 p-6 text-emerald-800';
            orderNote.innerHTML = '<h3 class="text-lg font-bold text-emerald-900">Order confirmed</h3><p class="mt-1 text-sm">Your order <strong>' + data.orderNumber + '</strong> is processing. Tracking ref <strong>' + data.trackingRef + '</strong>. We\'ll email delivery details within 24h.</p><a href="/account/dashboard" class="mt-4 inline-flex rounded-lg bg-brand-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-brand-500">View in dashboard</a>';
            document.querySelector('.checkout-form').replaceWith(orderNote);
            renderCart();
          } else {
            btn.disabled = false;
            btn.textContent = original;
          }
        }).catch(function () {
          btn.disabled = false;
          btn.textContent = original;
        });
      });
    }
  }
