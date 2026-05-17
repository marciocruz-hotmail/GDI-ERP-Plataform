/**
 * Controle de inatividade da sessão - logout automático após 15 min sem interação.
 * Reinicia o contador em: mousemove, mousedown, keydown, scroll, touchstart, click.
 * Envia keepalive ao servidor a cada 5 min enquanto o usuário estiver ativo,
 * mantendo a ASP.NET Session e o MemoryCache vivos (SlidingExpiration).
 * Usa window.GDI_SessionTimeout se definido pelo layout; caso contrário usa valores padrão.
 */
(function () {
    var DEFAULT_TIMEOUT_SECONDS = 900; /* 15 min - alinhado ao Web.config sessionState timeout="15" */
    var DEFAULT_LOGOUT_PATH = '/UserIdentity/Logout';
    var KEEPALIVE_PATH = '/UserIdentity/KeepAlive';
    var KEEPALIVE_INTERVAL_MS = 5 * 60 * 1000; /* ping ao servidor a cada 5 min */

    var config = window.GDI_SessionTimeout || {};
    var LOGOUT_URL = (config.logoutUrl && config.logoutUrl.length > 0) ? config.logoutUrl : DEFAULT_LOGOUT_PATH;
    var TIMEOUT_SECONDS = (config.timeoutSeconds > 0) ? config.timeoutSeconds : DEFAULT_TIMEOUT_SECONDS;
    var COUNTDOWN_ID = 'sessionCountdown';
    var lastReset = 0;
    var lastKeepalive = 0;
    var throttleMs = 1000;
    var timer = null;
    var remaining = TIMEOUT_SECONDS;

    function formatMmSs(sec) {
        var m = Math.floor(sec / 60);
        var s = sec % 60;
        return (m < 10 ? '0' : '') + m + ':' + (s < 10 ? '0' : '') + s;
    }

    function updateDisplay() {
        var el = document.getElementById(COUNTDOWN_ID);
        if (el) el.textContent = formatMmSs(remaining);
    }

    function resetCountdown() {
        remaining = TIMEOUT_SECONDS;
        updateDisplay();
    }

    function sendKeepalive() {
        try {
            var xhr = new XMLHttpRequest();
            xhr.open('POST', KEEPALIVE_PATH, true);
            xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');
            xhr.send();
        } catch (e) { /* silencioso */ }
    }

    function doLogout() {
        if (timer) clearInterval(timer);
        timer = null;
        var sep = LOGOUT_URL.indexOf('?') >= 0 ? '&' : '?';
        window.location.href = LOGOUT_URL + sep + 'reason=inactivity';
    }

    function onActivity() {
        var now = Date.now();
        if (now - lastReset < throttleMs) return;
        lastReset = now;
        resetCountdown();
        /* keepalive ao servidor: no máximo 1 vez a cada 5 min */
        if (now - lastKeepalive >= KEEPALIVE_INTERVAL_MS) {
            lastKeepalive = now;
            sendKeepalive();
        }
    }

    function startTimer() {
        if (timer) clearInterval(timer);
        timer = setInterval(function () {
            remaining--;
            updateDisplay();
            if (remaining <= 0) doLogout();
        }, 1000);
    }

    function init() {
        lastKeepalive = Date.now(); /* evita ping imediato no carregamento da página */
        resetCountdown();
        startTimer();

        var events = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'];
        for (var i = 0; i < events.length; i++) {
            document.addEventListener(events[i], onActivity, { passive: true });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
