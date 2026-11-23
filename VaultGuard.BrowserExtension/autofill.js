// Vault Guard in-page autofill: field icons + suggestions menu.
//
// Everything is drawn in a closed Shadow DOM overlay attached to <html>, so the page's CSS can't distort it and
// the page's scripts can't read it, and the page's own markup is never modified (the old script wrapped inputs
// in new elements, which broke some layouts). Nothing is ever filled automatically: only when the user picks
// something, and only into visible fields — hidden "harvesting" forms get nothing.
(() => {
  if (window.top !== window || window.__vaultGuardAutofill) return;
  window.__vaultGuardAutofill = true;
  const ext = globalThis.browser ?? globalThis.chrome; // Firefox: `browser`; Chrome/Edge/Brave: `chrome`

  const HOST = location.hostname.replace(/^www\./, '');
  const MAX_ICONS = 24;

  // ---------- messaging ----------
  const send = (msg) => new Promise((resolve) => {
    try {
      ext.runtime.sendMessage(msg).then(resolve, (e) => resolve({ success: false, error: e?.message || String(e) }));
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
    watchForReuse(f);
    f.addEventListener('focus', () => { if (!dismissed.has(f)) openMenu(f, false); });
    f.addEventListener('keydown', (e) => onFieldKey(e, f));
    // Typing your own value means you don't want a suggestion: get out of the way (the menu sits over the
    // next field and the sign-in button). The icon or Ctrl/Cmd+\ brings it back.
    f.addEventListener('input', (e) => { if (e.isTrusted && menu && menu.field === f) { dismissed.add(f); closeMenu(); } });
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
      // Sign-up form: offer a fresh email alias (SimpleLogin / addy.io, set up in the app) for the email field.
      const signUp = sameScope(field, ['new-password']).length > 0;
      if (st.kind === 'username' && status.aliases && (signUp || (field.type === 'email' && !creds.length))) {
        rows.unshift({
          title: 'Use a new email alias', sub: `A unique address for ${HOST} that forwards to your inbox`, suggest: true,
          run: async () => {
            const r = await send({ action: 'createEmailAlias' });
            if (r?.success && r.email) { setValue(field, r.email); toast(`Alias ${r.email} created`); }
            else toast(r?.error || "Couldn't create an alias");
          }
        });
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
    filledByUs.set(f, value);
    f.dispatchEvent(new Event('input', { bubbles: true }));
    f.dispatchEvent(new Event('change', { bubbles: true }));
    return true;
  }

  function sameScope(f, kinds) {
    return [...fields.entries()].filter(([g, st]) => kinds.includes(st.kind) && scopeOf(g) === scopeOf(f) && isVisible(g)).map(([g]) => g);
  }

  /**
   * After filling a login that has an authenticator, put its current one-time code on the clipboard, ready to
   * paste on the 2-step page (on by default; popup Settings → "Copy one-time code after filling").
   */
  async function copyTotpAfterFill(c) {
    if (!c?.hasTotp) return;
    const pref = await ext.storage.local.get('copyTotpAfterFill').catch(() => ({}));
    if (pref.copyTotpAfterFill === false) return;
    const t = await send({ action: 'getTotpCode', itemId: c.id, domain: HOST });
    if (!t?.success || !t.code) return;
    try {
      await navigator.clipboard.writeText(t.code); // allowed: this follows the user's click / Enter
      toast(`One-time code copied (valid ${t.secondsRemaining ?? 30} s)`);
    } catch {
      toast('Filled. Use the Vault Guard icon on the code field for the one-time code');
    }
  }

  function fillLogin(field, c) {
    const users = sameScope(field, ['username']);
    const pws = sameScope(field, ['password', 'new-password']);
    const user = fields.get(field).kind === 'username' ? field : users[0];
    const filledUser = !!(user && c.username && setValue(user, c.username));
    const filledPw = !!(pws[0] && c.password && setValue(pws[0], c.password));
    toast(`Filled ${c.title || 'login'}`);
    copyTotpAfterFill(c);
    // Sign in for the user only on a sign-in form: never submit a sign-up or change-password form for them.
    if (filledPw && fields.get(pws[0])?.kind === 'password') submitAfterFill(pws[0]);
    else if (filledUser && !pws.length) {
      // Username first, password on the next step: carry this login on to it, then press "Next".
      send({ action: 'startPendingFill', itemId: c.id });
      submitAfterFill(user);
    }
  }

  // ---------- sign in after filling ----------
  // After a login is filled (picked from the menu or the popup), press the form's sign-in button for the user.
  // On by default; popup Settings → "Sign in after filling" turns it off. It's skipped when the form wants more
  // than we filled: a captcha, or another visible required field that's still empty.
  const SUBMIT_TEXT = /^(sign|log)\s*(in|on)\b|^continue\b|^next\b|^submit\b|^go\b|^enter\b|^login\b|^signin\b|^weiter|^anmelden|^connexion|^se connecter|^iniciar/i;

  function findSubmit(scope) {
    const all = [...scope.querySelectorAll('button, input[type="submit"], input[type="image"], [role="button"]')]
      .filter(b => !b.disabled && b.getAttribute('aria-disabled') !== 'true' && isVisible(b));
    const label = (b) => (b.textContent || b.value || b.getAttribute('aria-label') || '').trim();
    return all.find(b => b.matches('button[type="submit"], input[type="submit"], input[type="image"]'))
        || all.find(b => SUBMIT_TEXT.test(label(b)))
        || (scope instanceof HTMLFormElement ? all.find(b => b.matches('button:not([type])')) : null);
  }

  function needsMore(scope) {
    if (scope.querySelector('iframe[src*="recaptcha"], iframe[src*="hcaptcha"], iframe[src*="turnstile"], .g-recaptcha, .h-captcha, .cf-turnstile')) return true;
    return [...scope.querySelectorAll('input[required], select[required], textarea[required]')]
      .some(i => isVisible(i) && !i.value && i.type !== 'checkbox' && i.type !== 'radio' && !fields.has(i));
  }

  async function submitAfterFill(field) {
    const pref = await ext.storage.local.get('autoSubmitAfterFill').catch(() => ({}));
    if (pref.autoSubmitAfterFill === false) return;
    await new Promise(r => setTimeout(r, 350)); // let the page's own scripts see the new values (and enable the button)
    if (!field.isConnected) return;
    const scope = scopeOf(field);
    if (needsMore(scope)) { toast('Filled. Finish the form, then sign in'); return; }
    const button = findSubmit(scope === document ? document : scope) || (field.form ? null : findSubmit(document));
    const form = field.form;
    if (button) {
      if (form && form.contains(button) && button.matches('button[type="submit"], input[type="submit"], input[type="image"], button:not([type])') && form.requestSubmit) {
        try { form.requestSubmit(button); return; } catch { /* button not a submitter of this form */ }
      }
      button.click();
    } else if (form?.requestSubmit) {
      form.requestSubmit();
    } else {
      field.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true }));
      field.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true }));
    }
  }

  // ---------- keyboard shortcuts and right-click menu ----------
  let lastContextTarget = null;
  document.addEventListener('contextmenu', (e) => { if (e.target instanceof HTMLInputElement) lastContextTarget = e.target; }, true);

  const visibleOf = (...kinds) => [...fields.entries()].filter(([f, st]) => kinds.includes(st.kind) && isVisible(f)).map(([f]) => f);

  /** Treat a field the extension didn't recognise (e.g. right-clicked) as the kind the user asked for. */
  function adopt(f, kind) {
    if (!(f instanceof HTMLInputElement) || !isVisible(f)) return null;
    if (!fields.has(f)) { track(f); if (!fields.has(f)) fields.set(f, { kind, icon: document.createElement('span') }); }
    return f;
  }

  /** Ctrl+Shift+L / "Fill login": one saved login → fill and sign in; several → show the menu to pick. */
  async function fillLoginHere(preferred) {
    const field = (preferred && fields.has(preferred) && preferred) || visibleOf('username')[0] || visibleOf('password')[0]
      || (preferred && adopt(preferred, 'username'));
    if (!field) return toast(`No sign-in fields found on ${HOST}`);
    const r = await send({ action: 'getCredentials', domain: HOST });
    if (r?.success === false && !r.credentials) return toast('Unlock Vault Guard to fill passwords here');
    const creds = (r?.credentials || []).filter(c => c.password || c.username);
    if (!creds.length) return toast(`No saved logins for ${HOST}`);
    if (creds.length === 1) return fillLogin(field, creds[0]);
    dismissed.delete(field);
    field.focus();
    openMenu(field, true);
  }

  /** Ctrl+Shift+G / "Fill a strong new password": into the selected field (or this form's new-password fields). */
  async function fillStrongPassword(preferred) {
    const active = document.activeElement instanceof HTMLInputElement ? document.activeElement : null;
    const field = (preferred && adopt(preferred, 'new-password')) || (active && adopt(active, 'new-password'))
      || visibleOf('new-password')[0] || visibleOf('password')[0];
    if (!field) return toast('Select a password field first');
    const g = await send({ action: 'generatePassword', options: { length: 20, includeUppercase: true, includeLowercase: true, includeNumbers: true, includeSymbols: true } });
    if (!g?.password) return toast(g?.error || "Couldn't make a password");
    if (field.type === 'password') fillNewPassword(field, g.password);
    else { setValue(field, g.password); navigator.clipboard?.writeText(g.password).catch(() => {}); toast('Strong password filled and copied'); }
  }

  async function fillCardHere(preferred) {
    const field = (preferred && fields.get(preferred)?.kind === 'card' && preferred) || visibleOf('card')[0] || (preferred && adopt(preferred, 'card'));
    if (!field) return toast('No card fields found on this page');
    dismissed.delete(field);
    field.focus();
    openMenu(field, true);
  }

  // ---------- open & fill ----------
  // The popup opened this tab for a login (or asked to fill it here). Fill it as soon as the sign-in fields
  // appear, then sign in; for a username-then-password sign-in, the password step is filled on the next page.
  let pendingBusy = false, pendingChecks = 0, pendingTimer = 0;

  async function runPendingFill() {
    if (pendingBusy) return;
    pendingBusy = true;
    try {
      const r = await send({ action: 'takePendingFill', step: 'peek' });
      if (!r?.pending) { pendingChecks = 99; return; }
      if (r.error) { toast(r.error); await send({ action: 'takePendingFill', step: 'done' }); pendingChecks = 99; return; }
      const c = r.credential;
      const visible = (kind) => [...fields.entries()].filter(([f, st]) => kinds(kind).includes(st.kind) && isVisible(f)).map(([f]) => f);
      const pw = visible('password')[0];
      const user = (pw ? sameScope(pw, ['username'])[0] : null) || visible('username')[0];
      if (pw) {
        if (user && c.username && !user.value) setValue(user, c.username);
        else if (user && c.username && user.value !== c.username && !r.usernameDone) setValue(user, c.username);
        if (c.password) setValue(pw, c.password);
        await send({ action: 'takePendingFill', step: 'done' });
        toast(`Filled ${c.title || 'login'}`); // the popup already copied any one-time code
        if (c.password) submitAfterFill(pw);
        pendingChecks = 99;
      } else if (user && !r.usernameDone) {
        if (c.username) setValue(user, c.username);
        await send({ action: 'takePendingFill', step: 'username' });
        toast(`Filled ${c.title || 'login'}`);
        submitAfterFill(user);
      }
    } finally { pendingBusy = false; }
  }
  const kinds = (k) => k === 'password' ? ['password'] : ['username'];

  /** Keep looking for the sign-in fields for a while: many sign-in pages draw them after loading. */
  function schedulePendingFill() {
    if (pendingChecks >= 40) return;
    clearTimeout(pendingTimer);
    pendingTimer = setTimeout(async () => { pendingChecks++; await runPendingFill(); if (pendingChecks < 40) schedulePendingFill(); }, pendingChecks === 0 ? 400 : 1500);
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
    .warn-card { border-top: 4px solid #DC2626; }
    .warn-icon { width: 20px; height: 20px; border-radius: 50%; background: #DC2626; color: #fff; display: grid; place-items: center; font-weight: 800; font-size: 13px; }
    .warn-card .btn.primary { background: linear-gradient(180deg, #EF4444, #DC2626); }
    .save-card { position: fixed; top: 16px; right: 16px; width: 340px; max-width: calc(100vw - 32px); box-sizing: border-box;
      color: #172033; background: #fff; border: 1px solid rgb(16 24 40 / .10); border-radius: 14px;
      box-shadow: 0 18px 50px -12px rgb(16 24 40 / .35), 0 4px 12px rgb(16 24 40 / .08); animation: pop .16s ease-out; }
    .save-body { padding: 12px; display: grid; gap: 10px; }
    .save-text { color: #344054; }
    .save-user { color: #667085; font-size: 12px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .save-label { display: grid; gap: 4px; font-size: 12px; font-weight: 600; color: #475467; }
    .save-title { font: inherit; font-weight: 400; color: inherit; padding: 7px 9px; border-radius: 8px; border: 1px solid #d0d5dd; background: transparent; }
    .save-title:focus { outline: 2px solid #4A72EC; outline-offset: -1px; }
    .save-actions { display: flex; flex-wrap: wrap; gap: 6px; }
    .btn.ghost { background: transparent; color: #344054; font-weight: 600; }
    .btn.ghost:hover { background: rgb(16 24 40 / .06); }
    .btn:disabled { opacity: .6; cursor: default; }
    @media (prefers-color-scheme: dark) {
      .save-card { color: #e6e9f0; background: #1c2130; border-color: rgb(255 255 255 / .09); }
      .save-text, .btn.ghost { color: #cfd5e2; }
      .save-user, .save-label { color: #98a2b3; }
      .save-title { border-color: #3a4257; }
      .btn.ghost:hover { background: rgb(255 255 255 / .08); }
    }
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

  // ---------- "Save this login?" ----------
  // When a sign-in or sign-up form is submitted, the typed username + password go to the extension's background
  // (held in memory for 2 minutes, never on disk), and on the page that follows we ask whether to save it — or,
  // if a login for this site and username already exists with another password, whether to update it.
  const USER_KEY = '__vaultguard_last_username'; // two-step sign-ins: the username is typed on the page before
  let lastCaptureAt = 0;
  let saveCard = null;

  function rememberUsername(scope) {
    const u = [...scope.querySelectorAll('input')].find(i => fields.get(i)?.kind === 'username' && i.value.trim() && isVisible(i));
    if (u) { try { sessionStorage.setItem(USER_KEY, u.value.trim()); } catch { /* storage blocked */ } }
  }

  function usernameIn(scope, pw) {
    const tracked = [...scope.querySelectorAll('input')].filter(i => fields.get(i)?.kind === 'username' && i.value.trim() && isVisible(i));
    if (tracked.length) return tracked[0].value.trim();
    // any visible text/email box before the password field
    const before = [...scope.querySelectorAll('input')].filter(i => TEXTY.has((i.type || '').toLowerCase()) && i.value.trim() && isVisible(i)
      && (i.compareDocumentPosition(pw) & Node.DOCUMENT_POSITION_FOLLOWING));
    if (before.length) return before[before.length - 1].value.trim();
    try { return sessionStorage.getItem(USER_KEY) || ''; } catch { return ''; }
  }

  function capture(from) {
    if (!from || !(from instanceof Element)) return;
    const scope = scopeOf(from instanceof HTMLFormElement ? from.querySelector('input') || from : from);
    const pws = [...scope.querySelectorAll('input[type="password"]')].filter(p => p.value && isVisible(p));
    if (!pws.length) { rememberUsername(scope); return; }      // username-only step
    if (Date.now() - lastCaptureAt < 1500) return;              // submit + click for the same action
    lastCaptureAt = Date.now();
    // Sign-up / change-password: save the new password (current + new + confirm → the new one).
    const fresh = pws.filter(p => attr(p, 'autocomplete') === 'new-password');
    const isNew = fresh.length > 0 || pws.length >= 2;
    const pw = fresh[0] || (pws.length >= 3 ? pws[1] : pws.length === 2 ? pws[0] : pws[0]);
    send({ action: 'captureLogin', username: usernameIn(scope, pw), password: pw.value, isNew });
    if (!isNew && filledByUs.get(pw) !== pw.value && checked.get(pw) !== pw.value && pw.value.length >= 6) {
      checked.set(pw, pw.value);
      send({ action: 'checkPasswordReuse', password: pw.value }).then(r => { if (r?.matches?.length) showReuseWarning(pw, r.matches, r.site || HOST); });
    }
    // Single-page apps often don't navigate after signing in: look for the offer here too.
    setTimeout(checkPendingSave, 2500);
  }

  const looksLikeSubmit = (b) => b.matches('button:not([type]), button[type="submit"], input[type="submit"], input[type="image"]')
    || /(sign|log)\s*(in|on|up)|continue|next|submit|create|register|save|change|update/i.test(b.textContent || b.value || '');

  document.addEventListener('submit', (e) => capture(e.target), true);
  document.addEventListener('click', (e) => {
    const b = e.target instanceof Element && e.target.closest('button, input[type="submit"], input[type="button"], input[type="image"], [role="button"]');
    if (b && !e.composedPath().includes(host) && looksLikeSubmit(b)) capture(b);
  }, true);
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' && e.target instanceof HTMLInputElement && fields.has(e.target) && !(menu && menu.field === e.target)) capture(e.target);
  }, true);

  async function checkPendingSave() {
    if (saveCard) return;
    const r = await send({ action: 'getPendingSave' });
    if (r?.offer) showSaveCard(r.offer);
  }

  function closeSaveCard() { if (saveCard) { saveCard.remove(); saveCard = null; } }

  function showSaveCard(offer) {
    closeSaveCard();
    const el = document.createElement('div');
    el.className = 'save-card';
    el.setAttribute('role', 'dialog');
    el.setAttribute('aria-label', 'Vault Guard');
    const user = offer.username ? `<div class="save-user">${esc(offer.username)}</div>` : '';
    if (offer.mode === 'update') {
      el.innerHTML = `<div class="head">${logoSvg(18)}<span class="brand">Update password?</span><button class="close" type="button" aria-label="Close">✕</button></div>
        <div class="save-body"><div class="save-text">Update the saved password for <strong>${esc(offer.title || offer.host)}</strong> to the one you just used?</div>${user}
        <div class="save-actions"><button class="btn primary" data-act="update" type="button">Update</button><button class="btn ghost" data-act="later" type="button">Not now</button></div></div>`;
    } else if (offer.mode === 'locked') {
      el.innerHTML = `<div class="head">${logoSvg(18)}<span class="brand">Save login?</span><button class="close" type="button" aria-label="Close">✕</button></div>
        <div class="save-body"><div class="save-text">Unlock Vault Guard to save this login for <strong>${esc(offer.host)}</strong>.</div>${user}
        <div class="save-actions"><button class="btn primary" data-act="retry" type="button">I've unlocked it</button><button class="btn ghost" data-act="later" type="button">Not now</button></div></div>`;
    } else {
      el.innerHTML = `<div class="head">${logoSvg(18)}<span class="brand">Save login to Vault Guard?</span><button class="close" type="button" aria-label="Close">✕</button></div>
        <div class="save-body"><label class="save-label">Name<input class="save-title" type="text" maxlength="200" value="${esc(offer.title || offer.host)}"></label>${user}
        <div class="save-actions"><button class="btn primary" data-act="save" type="button">Save</button><button class="btn ghost" data-act="never" type="button">Never for this site</button><button class="btn ghost" data-act="later" type="button">Not now</button></div></div>`;
    }
    const act = async (what) => {
      if (what === 'retry') { closeSaveCard(); return checkPendingSave(); }
      if (what === 'later' || what === 'never') { closeSaveCard(); await send({ action: 'dismissPendingSave', never: what === 'never' }); return; }
      el.querySelectorAll('button').forEach(b => b.disabled = true);
      const title = el.querySelector('.save-title')?.value || offer.title || offer.host;
      const r = await send({ action: 'savePendingLogin', title, itemId: what === 'update' ? offer.itemId : undefined });
      closeSaveCard();
      toast(r?.success ? (r.updated ? 'Password updated in Vault Guard' : r.created ? 'Saved to Vault Guard' : 'Already saved in Vault Guard')
                       : (r?.error || "Couldn't save to Vault Guard"));
    };
    el.addEventListener('click', (e) => {
      const b = e.target.closest('button'); if (!b) return;
      act(b.classList.contains('close') ? 'later' : b.dataset.act);
    });
    el.addEventListener('keydown', (e) => {
      e.stopPropagation(); // typing the name mustn't trigger the page's shortcuts
      if (e.key === 'Escape') act('later');
      if (e.key === 'Enter' && e.target.classList.contains('save-title')) act('save');
    });
    layer.appendChild(el);
    saveCard = el;
  }

  // ---------- Phishing guard ----------
  // Typing a saved password on a site it doesn't belong to (e.g. your bank password on bank-secure-login.xyz) is
  // the classic phishing trap. When you pause typing in a sign-in password field, the extension asks the Vault
  // Guard app privately whether that password is saved for another site; the app answers with titles/sites only.
  const filledByUs = new WeakMap();   // field -> value we filled (no need to check our own fills)
  const checked = new WeakMap();      // field -> last value checked
  let warnCard = null;

  function watchForReuse(f) {
    if (fields.get(f)?.kind !== 'password') return; // sign-up / new-password fields are new passwords
    let timer = 0;
    const check = () => {
      clearTimeout(timer);
      const v = f.value;
      if (v.length < 6 || checked.get(f) === v || filledByUs.get(f) === v || warnCard) return;
      checked.set(f, v);
      send({ action: 'checkPasswordReuse', password: v }).then(r => {
        if (r?.matches?.length && f.value === v) showReuseWarning(f, r.matches, r.site || HOST);
      });
    };
    f.addEventListener('input', (e) => { if (e.isTrusted) { clearTimeout(timer); timer = setTimeout(check, 800); } });
    f.addEventListener('blur', check);
  }

  function closeWarnCard() { if (warnCard) { warnCard.remove(); warnCard = null; } }

  function showReuseWarning(field, matches, site) {
    closeWarnCard();
    const first = matches[0];
    const others = matches.length > 1 ? ` and ${matches.length - 1} other login${matches.length > 2 ? 's' : ''}` : '';
    const el = document.createElement('div');
    el.className = 'save-card warn-card';
    el.setAttribute('role', 'alertdialog');
    el.setAttribute('aria-label', 'Vault Guard warning');
    el.innerHTML = `<div class="head"><span class="warn-icon">!</span><span class="brand">Is this the right site?</span><button class="close" type="button" aria-label="Close">✕</button></div>
      <div class="save-body">
        <div class="save-text">That's your password for <strong>${esc(first.title)}</strong> (${esc(first.site)})${esc(others)}.</div>
        <div class="save-text">You're on <strong>${esc(site)}</strong>. If you weren't expecting to sign in to ${esc(first.title)} here, this could be a fake site trying to steal it.</div>
        <div class="save-actions">
          <button class="btn primary" data-act="leave" type="button">Leave this site</button>
          <button class="btn ghost" data-act="clear" type="button">Clear password</button>
          <button class="btn ghost" data-act="trust" type="button">I trust this site</button>
        </div>
      </div>`;
    el.addEventListener('click', async (e) => {
      const b = e.target.closest('button'); if (!b) return;
      const act = b.classList.contains('close') ? 'close' : b.dataset.act;
      if (act === 'leave') { closeWarnCard(); if (history.length > 1) history.back(); else location.replace('about:blank'); return; }
      if (act === 'clear') { setValue(field, ''); checked.delete(field); closeWarnCard(); toast('Password cleared'); return; }
      if (act === 'trust') { await send({ action: 'trustSiteForReuse' }); }
      closeWarnCard();
    });
    el.addEventListener('keydown', (e) => { e.stopPropagation(); if (e.key === 'Escape') closeWarnCard(); });
    layer.appendChild(el);
    warnCard = el;
  }

  // ---------- confirmation card (passkeys) ----------
  // content.js asks before a passkey is created or used. The card sits in our closed shadow layer, so the page
  // can't read it or click it for the user (only trusted clicks count). Exposed on the extension's own isolated
  // window, which the page can't reach.
  let confirmCard = null;
  window.__vaultGuardConfirm = ({ title, text, ok = 'OK', cancel = 'Cancel' } = {}) => new Promise((resolve) => {
    if (confirmCard) { confirmCard.remove(); confirmCard = null; }
    const el = document.createElement('div');
    el.className = 'save-card';
    el.setAttribute('role', 'alertdialog');
    el.setAttribute('aria-label', title || 'Vault Guard');
    el.innerHTML = `<div class="head">${logoSvg(18)}<span class="brand">${esc(title || 'Vault Guard')}</span><button class="close" type="button" aria-label="Close">✕</button></div>
      <div class="save-body"><div class="save-text">${esc(text || '')}</div>
      <div class="save-actions"><button class="btn primary" data-act="ok" type="button">${esc(ok)}</button>
      <button class="btn ghost" data-act="cancel" type="button">${esc(cancel)}</button></div></div>`;
    let timer = 0;
    const done = (v) => { clearTimeout(timer); el.remove(); if (confirmCard === el) confirmCard = null; resolve(v); };
    el.addEventListener('click', (e) => {
      if (!e.isTrusted) return;
      const b = e.target.closest('button'); if (!b) return;
      done(b.dataset.act === 'ok');
    });
    el.addEventListener('keydown', (e) => { e.stopPropagation(); if (e.key === 'Escape' && e.isTrusted) done(false); });
    timer = setTimeout(() => done(false), 120000); // unanswered: the site falls back to the browser's own passkeys
    layer.appendChild(el);
    confirmCard = el;
    el.querySelector('[data-act="ok"]').focus();
  });

  // ---------- lifecycle ----------
  scan();
  new MutationObserver(() => { clearTimeout(scan.t); scan.t = setTimeout(scan, 150); })
    .observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['type', 'style', 'class', 'hidden'] });
  window.addEventListener('scroll', position, true);
  window.addEventListener('resize', position);
  document.addEventListener('mousedown', (e) => { if (menu && e.target !== menu.field && !e.composedPath().includes(host)) closeMenu(); }, true);
  document.addEventListener('focusin', (e) => { if (menu && e.target !== menu.field) closeMenu(); });
  setTimeout(checkPendingSave, 700); // a sign-in on the previous page may be waiting to be saved
  schedulePendingFill();             // opened by "Open & fill" from the popup?
  // The popup fills through content.js and copies the one-time code itself; it asks us to say so.
  ext.runtime.onMessage.addListener((msg, _sender, sendResponse) => {
    if (msg?.action === 'vaultGuardToast' && typeof msg.text === 'string') toast(msg.text.slice(0, 120));
    if (msg?.action === 'vaultGuardRunPendingFill') { pendingChecks = 0; schedulePendingFill(); sendResponse({ success: true }); }
    if (msg?.action === 'vaultGuardCommand') {
      if (msg.command === 'fill-login') fillLoginHere(document.activeElement);
      else if (msg.command === 'generate-password') fillStrongPassword(null);
    }
    if (msg?.action === 'vaultGuardContext') {
      const t = lastContextTarget;
      if (msg.kind === 'login') fillLoginHere(t);
      else if (msg.kind === 'password') fillStrongPassword(t);
      else if (msg.kind === 'card') fillCardHere(t);
    }
  });
})();
