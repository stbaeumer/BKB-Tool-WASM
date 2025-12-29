// Minimal helper to show/hide/update the startup progress UI.
// Robust: prüft Existenz der Elemente und fängt Fehler still ab.
window.updateStartupProgress = (percent, message) => {
    try {
        percent = Math.max(0, Math.min(100, parseInt(percent) || 0));
        const bar = document.getElementById('blazor-loader-bar');
        const msg = document.getElementById('blazor-loader-message');
        const loader = document.getElementById('blazor-loader');
        if (!loader) return;
        if (bar) {
            bar.style.width = percent + '%';
            bar.setAttribute('aria-valuenow', String(percent));
            bar.textContent = percent + '%';
        }
        if (msg && message !== undefined && message !== null) {
            msg.textContent = message;
        }
        // ensure visible
        loader.style.display = '';
        loader.setAttribute('aria-hidden', 'false');
    } catch (e) {
        // ignore
    }
};

window.hideStartupProgress = () => {
    try {
        const loader = document.getElementById('blazor-loader');
        if (!loader) return;
        // kleine Verzögerung für UX: ggf. verbleibende Animation kurz anzeigen
        setTimeout(() => {
            loader.style.display = 'none';
            loader.setAttribute('aria-hidden', 'true');
        }, 150);
    } catch (e) {
        // ignore
    }
};