// El navegador solo reproduce el audio; toda la lógica vive en el servidor (PlayerService).
(() => {
  const $ = s => document.querySelector(s);
  const audio = new Audio();
  let st = null, loadedNonce = -1, simT = 0, simTimer = null;
  const fmt = s => (!isFinite(s) || s <= 0) ? '0:00' : Math.floor(s / 60) + ':' + String(Math.floor(s % 60)).padStart(2, '0');
  const hasAudio = () => st && st.current && st.current.url;

  async function api(path, method = 'POST', body) {
    const r = await fetch('/api/player/' + path, { method, headers: { 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined });
    st = await r.json(); render(); return st;
  }

  // Reproducción simulada para canciones de demostración (sin archivo MP3)
  function startSim() {
    if (simTimer) return;
    simTimer = setInterval(() => {
      simT += 0.25; progress(simT, st.current.duration);
      if (simT >= st.current.duration) { stopSim(); api('next?auto=true'); }
    }, 250);
  }
  function stopSim() { clearInterval(simTimer); simTimer = null; }

  function progress(cur, dur) {
    $('#tCur').textContent = fmt(cur); $('#tTot').textContent = fmt(dur);
    $('#seek').value = dur > 0 ? Math.round(cur / dur * 1000) : 0;
  }
  const li = (arr, f) => arr.length ? arr.map(s => `<li>${f(s)}</li>`).join('') : '<li class="dim">vacía</li>';

  function render() {
    const c = st.current;
    $('#title').textContent = c ? c.title : 'Nada sonando';
    $('#artist').textContent = c ? `${c.artist} · ${c.album}` : 'Elige una canción';
    $('#coverBox').innerHTML = c && c.cover ? `<img class="cover" style="width:100%;height:100%" src="${c.cover}" alt="">` : '♪';
    $('#bPlay').textContent = st.playing ? '❚❚' : '▶';
    $('#player').classList.toggle('playing', st.playing);
    $('#bShuffle').classList.toggle('on', st.shuffle);
    const rl = { off: 'no', all: 'todo', one: 'una' };
    $('#bRepeat').textContent = 'Repetir: ' + rl[st.repeat]; $('#bRepeat').classList.toggle('on', st.repeat !== 'off');
    $('#lQueue').innerHTML = li(st.queue, s => `${s.title} <span class="dim">${s.artist}</span>`);
    $('#lHistory').innerHTML = li(st.history, s => s.title);
    $('#lRecent').innerHTML = li(st.recent, s => s.title);
    document.querySelectorAll('tr.song').forEach(r => r.classList.toggle('now', !!c && +r.dataset.id === c.id));

    if (c && st.nonce !== loadedNonce) {          // canción nueva o reinicio
      loadedNonce = st.nonce; stopSim(); simT = 0; progress(0, c.duration);
      if (c.url) { audio.src = c.url; audio.currentTime = 0; }
    }
    if (c && st.playing) { if (hasAudio()) audio.play().catch(() => {}); else startSim(); }
    else { audio.pause(); stopSim(); }
  }

  audio.addEventListener('timeupdate', () => progress(audio.currentTime, audio.duration));
  audio.addEventListener('loadedmetadata', () => progress(audio.currentTime, audio.duration));
  audio.addEventListener('ended', () => api('next?auto=true'));

  $('#bPlay').onclick = () => api('toggle');
  $('#bNext').onclick = () => api('next');
  $('#bPrev').onclick = () => api('previous');
  $('#bShuffle').onclick = () => api('shuffle');
  $('#bRepeat').onclick = () => api('repeat');
  $('#bQueue').onclick = () => { const d = $('#drawer'); d.hidden = !d.hidden; $('#bQueue').classList.toggle('on', !d.hidden); };
  $('#vol').oninput = e => audio.volume = e.target.value / 100;
  audio.volume = 0.8;
  $('#seek').oninput = e => {
    if (!st || !st.current) return;
    const f = e.target.value / 1000;
    if (hasAudio()) audio.currentTime = f * audio.duration; else { simT = f * st.current.duration; progress(simT, st.current.duration); }
  };

  // Navegación sin recargar la página, para que la música no se corte
  async function go(url, push = true) {
    const r = await fetch(url, { headers: { 'X-Requested-With': 'fetch' } });
    $('#content').innerHTML = await r.text();
    if (push) history.pushState({}, '', url);
    markNav(); if (st) render();
  }
  function markNav() {
    document.querySelectorAll('.side nav a').forEach(a => a.classList.toggle('on', new URL(a.href).pathname === location.pathname));
  }
  window.addEventListener('popstate', () => go(location.href, false));

  document.addEventListener('click', e => {
    const a = e.target.closest('a[data-nav]');
    if (a) { e.preventDefault(); go(a.href); return; }
    const all = e.target.closest('[data-playall]');
    if (all) { const ids = all.dataset.playall.split(',').map(Number); if (ids[0]) api('play', 'POST', { ids, startId: ids[0] }); return; }
    const row = e.target.closest('tr.song'); if (!row) return;
    const id = +row.dataset.id;
    if (e.target.closest('.q')) { api('enqueue/' + id); return; }
    api('play', 'POST', { ids: row.closest('table').dataset.ids.split(',').map(Number), startId: id });
  });

  document.addEventListener('kassette:refresh', () => api('state', 'GET'));
  markNav();
  api('state', 'GET').then(s => { if (s.playing) api('toggle'); }); // al abrir, nada suena solo
})();
