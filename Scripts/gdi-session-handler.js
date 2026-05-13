/**
 * Handler global: sessão expirada em respostas AJAX (401 + JSON do CustomAuthorize).
 * Depende de jQuery. Fecha modais via API Bootstrap 5.
 *
 * CHECKLIST — ordem de scripts (_Layout e equivalentes)
 * 1) jQuery
 * 2) @Scripts.Render("~/bundles/libui-swal-compat") — SweetAlert2 + GdiSwalCompat
 * 3) start.js — LibMessageError / LibMessageAlert
 * 4) Este ficheiro (gdi-session-handler.js) — depois dos itens 1–3 para existir LibMessage* em runtime no ajaxError
 *
 * Fallback: se LibMessageError ou GdiSwalCompat não existirem, usa alert() nativo e redireciona após OK.
 */
(function ($) {
    'use strict';

    function hideOpenBootstrap5Modals() {
        if (typeof bootstrap === 'undefined' || !bootstrap.Modal) {
            return;
        }
        document.querySelectorAll('.modal.show').forEach(function (el) {
            var inst = bootstrap.Modal.getInstance(el);
            if (inst) {
                inst.hide();
            }
        });
    }

    function tryParseJson401(xhr) {
        var ct = xhr.getResponseHeader('Content-Type');
        if (!xhr.responseText || !xhr.responseText.trim()) {
            return null;
        }
        if (ct && ct.indexOf('application/json') < 0 && xhr.responseText.trim().charAt(0) !== '{') {
            return null;
        }
        try {
            return JSON.parse(xhr.responseText);
        } catch (e) {
            return null;
        }
    }

    function redirectToLogin() {
        window.location.href = window.GDI_LoginUrl || '/UserIdentity/Index';
    }

    /**
     * Mensagem de sessão expirada: SweetAlert2 via LibMessageError quando disponível;
     * redireciona após o utilizador fechar o diálogo (callback do shim).
     */
    function notifySessionExpiredThenRedirect(message) {
        var msg = message || 'Sessão expirada. Efetue nova conexão.';
        if (typeof LibMessageError === 'function' &&
            typeof GdiSwalCompat !== 'undefined' &&
            GdiSwalCompat &&
            typeof GdiSwalCompat.alert === 'function') {
            LibMessageError('Sessão', msg, {
                callback: redirectToLogin
            });
            return;
        }
        alert(msg);
        redirectToLogin();
    }

    $(document).ajaxError(function (event, xhr, settings, thrownError) {
        if (xhr.status === 0 || thrownError === 'abort') {
            return;
        }

        if (xhr.status === 401) {
            var resp = tryParseJson401(xhr);
            if (resp && resp.sessaoExpirada) {
                hideOpenBootstrap5Modals();
                notifySessionExpiredThenRedirect(resp.mensagem);
                return;
            }
        }

        if (xhr.status === 500) {
            console.error('[GDI] Erro interno:', settings && settings.url, xhr.responseText);
        }
    });
})(jQuery);
