// Vault Guard in-page autofill: field icons + suggestions menu.
//
// Everything is drawn in a closed Shadow DOM overlay attached to <html>, so the page's CSS can't distort it and
// the page's scripts can't read it, and the page's own markup is never modified (the old script wrapped inputs
// in new elements, which broke some layouts). Nothing is ever filled automatically: only when the user picks
// something, and only into visible fields — hidden "harvesting" forms get nothing.
(() => {
  if (window.top !== window || window.__vaultGuardAutofill) return;
  window.__vaultGuardAutofill = true;

  const HOST = location.hostname.replace(/^www\./, '');
  const MAX_ICONS = 24;

  // ---------- messaging ----------
  const send = (msg) => new Promise((resolve) => {
    try {
      chrome.runtime.sendMessage(msg, (r) => resolve(chrome.runtime.lastError ? { success: false, error: chrome.runtime.lastError.message } : r));
    } catch (e) { resolve({ success: false, error: e.message }); }
  });

  // ---------- field detection ----------
  const TEXTY = new Set(['text', 'email', 'tel', 'number', '']);
  const attr = (f, n) => (f.getAttribute(n) || '').toLowerCase();
  const hint = (f) => `${f.name} ${f.id} ${attr(f, 'placeholder')} ${attr(f, 'aria-label')} ${attr(f, 'autocomplete')}`.toLowerCase();

  function isVisible(f) {
    if (f.disabled || f.readOnly || f.type === 'hidden') return false;
    const r = f.getBoundingClientRect();
    if (r.width < 40 || r.height < 14) return false;
    const cs = getComputedStyle(f);
    return cs.visibility !== 'hidden' && cs.display !== 'none' && parseFloat(cs.opacity || '1') > 0.05;
  }

  function scopeOf(f) { return f.form || f.closest('form, [role="form"], dialog') || document; }

  function classify(f) {
    if (!(f instanceof HTMLInputElement)) return null;
    const type = (f.type || '').toLowerCase();
    const ac = attr(f, 'autocomplete');
    const h = hint(f);
    if (ac.startsWith('cc-') || /card.?num|cc.?num|cardnumber|\bcvv\b|\bcvc\b|csc|expir/.test(h)) return 'card';
    if (ac === 'one-time-code' || (/(\botp\b|totp|2fa|mfa|one.?time|verification.?code|auth.?code|security.?code)/.test(h) && (f.maxLength <= 0 || f.maxLength <= 10))) return 'otp';
    if (type === 'password') {
      const pwCount = scopeOf(f).querySelectorAll('input[type="password"]').length;
      return ac === 'new-password' || pwCount >= 2 || /new|confirm|create|register|sign.?up/.test(h) ? 'new-password' : 'password';
    }
    if (!TEXTY.has(type)) return null;
    if (ac === 'username' || ac === 'email' || type === 'email' || /user|email|e-mail|login|account|identifier/.test(h)) {
      return scopeOf(f).querySelector('input[type="password"]') || /user|email|login/.test(h) ? 'username' : null;
    }
    // a lone text box right before a password field
    const pw = scopeOf(f).querySelector('input[type="password"]');
    if (pw && f.compareDocumentPosition(pw) & Node.DOCUMENT_POSITION_FOLLOWING) {
      const texts = [...scopeOf(f).querySelectorAll('input')].filter(i => TEXTY.has((i.type || '').toLowerCase()) && isVisible(i));
      if (texts.length === 1) return 'username';
    }
    return null;
  }

  // ---------- shadow overlay ----------
  const host = document.createElement('vaultguard-autofill');
  host.style.cssText = 'all: initial; position: fixed; inset: 0 auto auto 0; width: 0; height: 0; z-index: 2147483647;';
  const root = host.attachShadow({ mode: 'closed' });
  root.innerHTML = `<style>${css()}</style><div class="layer"></div>`;
  const layer = root.querySelector('.layer');
  (document.documentElement || document.body).appendChild(host);

  const fields = new Map();   // input -> { kind, icon }
  let menu = null;            // { el, field, items, index }
  let dismissed = new WeakSet();

  function logoSvg(size) {
    return `<svg viewBox="0 0 128 128" width="${size}" height="${size}" aria-hidden="true">
      <defs><linearGradient id="vgb" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#6C8DF3"/><stop offset="1" stop-color="#2252DA"/></linearGradient></defs>
      <rect x="4" y="4" width="120" height="120" rx="28" fill="url(#vgb)"/>
      <path d="M44 58V45a20 20 0 0 1 40 0v13" fill="none" stroke="#fff" stroke-width="11" stroke-linecap="round"/>
      <rect x="32" y="56" width="64" height="48" rx="10" fill="#fff"/>
      <circle cx="64" cy="75" r="7" fill="#2252DA"/><path d="M60.5 78h7l-1.6 13h-3.8z" fill="#2252DA"/></svg>`;
  }

  function track(f) {
    if (fields.has(f) || fields.size >= MAX_ICONS) return;
    const kind = classify(f);
    if (!kind) return;
    const icon = document.createElement('button');
    icon.className = 'field-icon';
    icon.type = 'button';
    icon.tabIndex = -1;
    icon.title = 'Vault Guard';
    icon.setAttribute('aria-label', 'Open Vault Guard');
    icon.innerHTML = logoSvg(16);
    icon.addEventListener('mousedown', (e) => e.preventDefault()); // keep focus in the field
    icon.addEventListener('click', (e) => { e.stopPropagation(); dismissed.delete(f); openMenu(f, true); });
    layer.appendChild(icon);
    fields.set(f, { kind, icon });
    f.addEventListener('focus', () => { if (!dismissed.has(f)) openMenu(f, false); });
    f.addEventListener('keydown', (e) => onFieldKey(e, f));
    position();
  }

  function scan() {
    document.querySelectorAll('input').forEach(track);
    for (const [f, st] of fields) if (!f.isConnected) { st.icon.remove(); fields.delete(f); }
    position();
  }

  let raf = 0;
  function position() {
    if (raf) return;
    raf = requestAnimationFrame(() => {
      raf = 0;
      for (const [f, st] of fields) {
        const visible = f.isConnected && isVisible(f);
        st.icon.style.display = visible ? 'flex' : 'none';
        if (!visible) continue;
        const r = f.getBoundingClientRect();
        const size = Math.min(22, Math.max(16, r.height - 10));
        st.icon.style.width = st.icon.style.height = `${size}px`;
        st.icon.style.left = `${r.right - size - Math.min(10, (r.height - size) / 2 + 4)}px`;
        st.icon.style.top = `${r.top + (r.height - size) / 2}px`;
      }
      if (menu) placeMenu();
    });
  }

  // ---------- menu ----------
  async function openMenu(field, fromIcon) {
    const st = fields.get(field);
    if (!st || !isVisible(field)) return;
    closeMenu();
    const el = document.createElement('div');
    el.className = 'menu';
    el.setAttribute('role', 'listbox');
    el.innerHTML = `<div class="head">${logoSvg(18)}<span class="brand">Vault Guard</span><span class="site">${esc(HOST)}</span>
      <button class="close" type="button" aria-label="Close">✕</button></div><div class="body"><div class="loading">Checking your vault…</div></div>`;
    el.addEventListener('mousedown', (e) => e.preventDefault());
    el.querySelector('.close').addEventListener('click', () => { dismissed.add(field); closeMenu(); });
    layer.appendChild(el);
    menu = { el, field, items: [], index: 0 };
    placeMenu();
    const body = el.querySelector('.body');

    const status = (await send({ action: 'getLinkStatus' }))?.link;
    if (!menu || menu.field !== field) return;
    if (!status?.linked) return renderNotice(body, 'Open Vault Guard on this computer to fill passwords here.', 'Check again', () => openMenu(field, true));
    if (!status.paired) return renderNotice(body, 'Connect this browser to Vault Guard first.', 'Connect…', () => send({ action: 'openPopup' }));
    if (!status.unlocked) return renderNotice(body, 'Vault Guard is locked. Unlock the app to fill.', "I've unlocked it", () => openMenu(field, true));

    const rows = [];
    if (st.kind === 'card') {
      const r = await send({ action: 'getCreditCards', domain: HOST });
      (r?.creditCards || []).forEach(c => rows.push({
        title: c.title || 'Card', sub: `${c.cardType || 'Card'} •••• ${(c.cardNumber || '').slice(-4)}`, initial: '💳',
        run: () => fillCard(field, c)
      }));
    } else {
      const r = await send({ action: 'getCredentials', domain: HOST });
      const creds = (r?.credentials || []).filter(c => c.password || st.kind === 'otp');
      if (st.kind === 'otp') {
        creds.filter(c => c.hasTotp).forEach(c => rows.push({
          title: c.title, sub: `One-time code · ${c.username || ''}`, initial: c.title,
          run: async () => {
            const t = await send({ action: 'getTotpCode', itemId: c.id, domain: HOST });
            if (t?.success) { setValue(field, t.code); toast('One-time code filled'); } else toast(t?.error || 'No code available');
          }
        }));
      } else {
        creds.forEach(c => rows.push({ title: c.title || HOST, sub: c.username || '(no username)', initial: c.title || HOST, run: () => fillLogin(field, c) }));
      }
      if (st.kind === 'new-password' || st.kind === 'password') {
        const g = await send({ action: 'generatePassword', options: { length: 20, includeUppercase: true, includeLowercase: true, includeNumbers: true, includeSymbols: true } });
        const pw = g?.password;
        const row = pw && { title: 'Use a strong password', sub: pw, mono: true, suggest: true, run: () => fillNewPassword(field, pw) };
        // On sign-up / change-password forms the new password is what's wanted; on a sign-in it's a last resort.
        if (row) st.kind === 'new-password' ? rows.unshift(row) : rows.push(row);
      }
    }
    if (!menu || menu.field !== field) return;
    if (!rows.length) {
      const what = st.kind === 'card' ? 'cards' : st.kind === 'otp' ? 'one-time codes' : 'logins';
      return renderNotice(body, `No saved ${what} for ${HOST}.`);
    }
    menu.items = rows;
    menu.index = 0;
    renderRows(body);
    if (!fromIcon && rows.length && rows.every(r => r.suggest) && st.kind === 'password') closeMenu(); // nothing saved: don't nag on plain logins
  }

  function renderRows(body) {
    body.innerHTML = '';
    menu.items.forEach((row, i) => {
      const item = document.createElement('div');
      item.className = 'row' + (i === menu.index ? ' active' : '') + (row.suggest ? ' suggest' : '');
      item.setAttribute('role', 'option');
      item.setAttribute('aria-selected', String(i === menu.index));
      item.innerHTML = `<div class="avatar" style="${row.suggest ? '' : `background:${color(row.title)}`}">${row.suggest ? '✦' : esc(initial(row.initial))}</div>
        <div class="text"><div class="title">${esc(row.title)}</div><div class="sub${row.mono ? ' mono' : ''}">${esc(row.sub)}</div></div>
        <div class="enter">↵</div>`;
      item.addEventListener('mouseenter', () => { menu.index = i; highlight(); });
      item.addEventListener('click', () => choose(i));
      body.appendChild(item);
    });
    const foot = document.createElement('div');
    foot.className = 'foot';
    foot.textContent = '↑↓ to move · Enter to fill · Esc to close';
    body.appendChild(foot);
  }

  function renderNotice(body, text, action, onAction) {
    body.innerHTML = `<div class="notice"><div>${esc(text)}</div>${action ? `<button class="btn" type="button">${esc(action)}</button>` : ''}</div>`;
    if (action) body.querySelector('.btn').addEventListener('click', onAction);
    if (menu) menu.items = [];
  }

  function highlight() {
    if (!menu) return;
    menu.el.querySelectorAll('.row').forEach((r, i) => { r.classList.toggle('active', i === menu.index); r.setAttribute('aria-selected', String(i === menu.index)); });
  }

  async function choose(i) {
    const row = menu?.items[i];
    if (!row) return;
    const field = menu.field;
    closeMenu();
    dismissed.add(field);
    await row.run();
  }

  function placeMenu() {
    if (!menu) return;
    const r = menu.field.getBoundingClientRect();
    const width = Math.max(300, Math.min(380, r.width));
    const left = Math.min(Math.max(8, r.left), window.innerWidth - width - 8);
    const below = window.innerHeight - r.bottom > 260 || r.top < 260;
    menu.el.style.width = `${width}px`;
    menu.el.style.left = `${left}px`;
    menu.el.style.top = below ? `${r.bottom + 6}px` : '';
    menu.el.style.bottom = below ? '' : `${window.innerHeight - r.top + 6}px`;
  }

  function closeMenu() { if (menu) { menu.el.remove(); menu = null; } }

  function onFieldKey(e, f) {
    if ((e.metaKey || e.ctrlKey) && e.key === '\\') { e.preventDefault(); dismissed.delete(f); openMenu(f, true); return; }
    if (!menu || menu.field !== f) return;
    if (e.key === 'Escape') { e.preventDefault(); dismissed.add(f); closeMenu(); }
    else if (e.key === 'ArrowDown' && menu.items.length) { e.preventDefault(); menu.index = (menu.index + 1) % menu.items.length; highlight(); }
    else if (e.key === 'ArrowUp' && menu.items.length) { e.preventDefault(); menu.index = (menu.index - 1 + menu.items.length) % menu.items.length; highlight(); }
    else if (e.key === 'Enter' && menu.items.length) { e.preventDefault(); choose(menu.index); }
  }

  // ---------- filling ----------
  function setValue(f, value) {
    if (!f || !isVisible(f)) return false;
    f.focus();
    const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
    setter.call(f, value); // works with React/Vue-controlled inputs
    f.dispatchEvent(new Event('input', { bubbles: true }));
    f.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
  }

  function sameScope(f, kinds) {
    return [...fields.entries()].filter(([g, st]) => kinds.includes(st.kind) && scopeOf(g) === scopeOf(f) && isVisible(g)).map(([g]) => g);
  }

  function fillLogin(field, c) {
    const users = sameScope(field, ['username']);
    const pws = sameScope(field, ['password', 'new-password']);
    const user = fields.get(field).kind === 'username' ? field : users[0];
    if (user && c.username) setValue(user, c.username);
    if (pws[0] && c.password) setValue(pws[0], c.password);
    else if (!pws.length && c.password) {
      // username-first sign-in (password step comes next): remember nothing, the user picks again on the next step
    }
    toast(`Filled ${c.title || 'login'}`);
  }

  function fillNewPassword(field, pw) {
    sameScope(field, ['new-password', 'password']).forEach(g => setValue(g, pw));
    navigator.clipboard?.writeText(pw).catch(() => {});
    toast('Strong password filled — save it in Vault Guard after signing up');
  }

  function fillCard(field, c) {
    const exp = (c.expiryDate || '').split('/');
    const scope = scopeOf(field);
    scope.querySelectorAll('input, select').forEach(el => {
      const ac = attr(el, 'autocomplete'); const h = hint(el);
      let v = null;
      if (ac === 'cc-number' || /card.?num|cc.?num|cardnumber/.test(h)) v = c.cardNumber;
      else if (ac === 'cc-name' || /card.?holder|name.?on.?card|cc.?name/.test(h)) v = c.cardholderName;
      else if (ac === 'cc-exp' || /expir.*date|\bexp\b/.test(h)) v = c.expiryDate;
      else if (ac === 'cc-exp-month' || /exp.*month|\bmm\b/.test(h)) v = exp[0];
      else if (ac === 'cc-exp-year' || /exp.*year|\byy/.test(h)) v = exp[1];
      else if (ac === 'cc-csc' || /\bcvv\b|\bcvc\b|csc|security.?code/.test(h)) v = c.cvv;
      if (v == null || v === '') return;
      if (el instanceof HTMLSelectElement) {
        const opt = [...el.options].find(o => o.value === v || o.text.trim() === v || o.value.endsWith(v));
        if (opt) { el.value = opt.value; el.dispatchEvent(new Event('change', { bubbles: true })); }
      } else setValue(el, v);
    });
    toast(`Filled ${c.title || 'card'}`);
  }

  // ---------- bits ----------
  function toast(text) {
    const t = document.createElement('div');
    t.className = 'toast';
    t.innerHTML = `${logoSvg(16)}<span>${esc(text)}</span>`;
    layer.appendChild(t);
    setTimeout(() => t.classList.add('out'), 2200);
    setTimeout(() => t.remove(), 2600);
  }
  const esc = (s) => String(s ?? '').replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
  const initial = (s) => (String(s || '?').trim()[0] || '?').toUpperCase();
  function color(s) { let h = 0; for (const ch of String(s)) h = (h * 31 + ch.charCodeAt(0)) >>> 0; return `hsl(${h % 360} 55% 45%)`; }

  function css() {
    return `
    :host { all: initial; }
    .layer { position: fixed; inset: 0; width: 0; height: 0; font: 13px/1.35 -apple-system, "Segoe UI", system-ui, sans-serif; }
    .field-icon { position: fixed; display: none; align-items: center; justify-content: center; padding: 0; margin: 0;
      border: 0; border-radius: 6px; background: transparent; cursor: pointer; opacity: .9; transition: transform .12s, opacity .12s; }
    .field-icon:hover { opacity: 1; transform: scale(1.08); }
    .field-icon svg { width: 100%; height: 100%; filter: drop-shadow(0 1px 1px rgb(0 0 0 / .18)); }
    .menu { position: fixed; box-sizing: border-box; color: #172033; background: #fff; border: 1px solid rgb(16 24 40 / .10);
      border-radius: 14px; box-shadow: 0 18px 50px -12px rgb(16 24 40 / .35), 0 4px 12px rgb(16 24 40 / .08);
      overflow: hidden; animation: pop .14s ease-out; }
    @keyframes pop { from { opacity: 0; transform: translateY(-4px) scale(.98); } }
    .head { display: flex; align-items: center; gap: 8px; padding: 10px 12px; border-bottom: 1px solid rgb(16 24 40 / .07); }
    .brand { font-weight: 650; }
    .site { margin-left: auto; color: #667085; font-size: 12px; max-width: 45%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .close { border: 0; background: transparent; color: #98a2b3; cursor: pointer; font-size: 12px; padding: 2px 4px; border-radius: 6px; }
    .close:hover { background: rgb(16 24 40 / .06); color: #344054; }
    .body { max-height: 300px; overflow-y: auto; padding: 6px; }
    .row { display: flex; align-items: center; gap: 10px; padding: 8px; border-radius: 10px; cursor: pointer; }
    .row.active { background: #eef3ff; }
    .avatar { flex: none; width: 32px; height: 32px; border-radius: 9px; display: grid; place-items: center; color: #fff; font-weight: 650; }
    .row.suggest .avatar { background: linear-gradient(135deg, #6C8DF3, #2252DA); }
    .text { min-width: 0; flex: 1; }
    .title { font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .sub { color: #667085; font-size: 12px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .sub.mono { font-family: ui-monospace, "Cascadia Mono", Menlo, monospace; letter-spacing: .02em; }
    .enter { color: #98a2b3; font-size: 12px; opacity: 0; }
    .row.active .enter { opacity: 1; }
    .foot { padding: 6px 8px 2px; color: #98a2b3; font-size: 11px; }
    .notice { padding: 10px; display: grid; gap: 10px; color: #344054; }
    .loading { padding: 12px 10px; color: #667085; }
    .btn { justify-self: start; border: 0; border-radius: 8px; padding: 7px 12px; font: inherit; font-weight: 600; color: #fff;
      background: linear-gradient(180deg, #4A72EC, #2252DA); cursor: pointer; }
    .toast { position: fixed; right: 16px; bottom: 16px; display: flex; align-items: center; gap: 8px; padding: 9px 12px;
      color: #fff; background: #172033; border-radius: 10px; box-shadow: 0 10px 30px rgb(0 0 0 / .25); transition: opacity .35s, transform .35s; }
    .toast.out { opacity: 0; transform: translateY(6px); }
    @media (prefers-color-scheme: dark) {
      .menu { color: #e6e9f0; background: #1c2130; border-color: rgb(255 255 255 / .09); }
      .head { border-color: rgb(255 255 255 / .07); }
      .site, .sub, .loading { color: #98a2b3; }
      .row.active { background: #263252; }
      .notice { color: #cfd5e2; }
      .close:hover { background: rgb(255 255 255 / .08); color: #e6e9f0; }
      .toast { background: #2b3348; }
    }`;
  }

  // ---------- lifecycle ----------
  scan();
  new MutationObserver(() => { clearTimeout(scan.t); scan.t = setTimeout(scan, 150); })
    .observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['type', 'style', 'class', 'hidden'] });
  window.addEventListener('scroll', position, true);
  window.addEventListener('resize', position);
  document.addEventListener('mousedown', (e) => { if (menu && e.target !== menu.field && !e.composedPath().includes(host)) closeMenu(); }, true);
  document.addEventListener('focusin', (e) => { if (menu && e.target !== menu.field) closeMenu(); });
})();
