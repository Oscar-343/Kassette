// Formularios del panel: se envían con fetch para que la música no se corte
(() => {
  // La duración del MP3 se lee en el navegador al elegir el archivo
  document.addEventListener('change', e => {
    const f = e.target;
    if (!f.matches || !f.matches('input[name=audio]')) return;
    const hid = f.form.querySelector('input[name=duration]');
    hid.value = 0;
    const file = f.files[0]; if (!file) return;
    const a = new Audio(URL.createObjectURL(file));
    a.addEventListener('loadedmetadata', () => { hid.value = Math.round(a.duration) || 0; URL.revokeObjectURL(a.src); });
  });

  document.addEventListener('submit', async e => {
    const form = e.target.closest('form[data-ajax]'); if (!form) return;
    e.preventDefault();
    if (form.dataset.confirm && !confirm(form.dataset.confirm)) return; // eliminar pide confirmación
    const btn = form.querySelector('button[type=submit]');
    if (btn) { btn.disabled = true; btn.textContent = 'Enviando…'; }
    try {
      const r = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { 'X-Requested-With': 'fetch' } });
      document.getElementById('content').innerHTML = await r.text();
      document.dispatchEvent(new Event('kassette:refresh')); // actualiza la cola en el panel del reproductor
    } catch {
      alert('No se pudo enviar el formulario. Revisa tu conexión.');
      if (btn) btn.disabled = false;
    }
  });
})();
