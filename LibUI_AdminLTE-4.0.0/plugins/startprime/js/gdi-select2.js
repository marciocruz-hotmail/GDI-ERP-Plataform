/**
 * @fileoverview GDI — Select2 (Bootstrap 5 / AdminLTE 4). Único ponto de entrada para inicialização.
 * Não chame `$(...).select2({...})` nas views; use `gdiInitSelect2OnCollection` / `gdiInitSelect2Page` / `gdiInitSelect2Modal`.
 * Antes de substituir HTML de partials ou recarregar nós com `<select>`, use `gdiDestroySelect2OnCollection`.
 *
 * Debug: `window.GDI_DEBUG = true` no console (ou antes deste script) para logs de skips e exceções.
 */

/** Opções padrão globais — fundidas em cada init (não sobrescrever opções nas views). */
var GDI_SELECT2_DEFAULTS = {
    theme: 'bootstrap-5',
    language: 'pt-BR',
    width: '100%',
    /* 0 = sempre mostrar pesquisa (default Select2). >0 pode ocultá-la por contagem interna e “sumir” em listas grandes. */
    minimumResultsForSearch: 0
};

/** True se o utilizador está com dropdown Select2 aberto ou a escrever na pesquisa (evita roubar foco, ex.: footerCallback do DataTables). */
function gdiIsSelect2Interacting() {
    try {
        if (document.querySelector('.select2-container--open')) {
            return true;
        }
        var a = document.activeElement;
        if (a && a.classList && a.classList.contains('select2-search__field')) {
            return true;
        }
    } catch (ex) { /* ignore */ }
    return false;
}

/** @returns {number} Número de `<option>` (lista estática). */
function gdiSelect2StaticOptionCount($el) {
    try {
        return $el.find('option').length;
    } catch (e) {
        return 0;
    }
}

/**
 * Resolve o elemento jQuery para `dropdownParent` do Select2.
 * Ordem: `data-select2-dropdown-parent` (seletor CSS) → `#mainModal` / `#containerModalPedidoSeparacaoLotes`
 * (hospedeiros AJAX conhecidos) → `.modal` ascendente → `scopeParent` → `document.body`.
 * Os ids fixam o dropdown ao mesmo nó do FocusTrap BS5 (evita lista fora do trap em casos raros).
 *
 * @param {jQuery} $el `<select>` alvo
 * @param {jQuery} [scopeParent] parent passado por `gdiInitSelect2Page` / `gdiInitSelect2Modal`
 * @returns {jQuery}
 */
function gdiSelect2ResolveDropdownParent($el, scopeParent) {
    try {
        var sel = $el.attr('data-select2-dropdown-parent');
        if (sel && String(sel).trim().length) {
            var $c = $(sel);
            if ($c.length) {
                return $c;
            }
        }
        var $mainHost = $el.closest('#mainModal');
        if ($mainHost.length) {
            return $mainHost;
        }
        var $sepHost = $el.closest('#containerModalPedidoSeparacaoLotes');
        if ($sepHost.length) {
            return $sepHost;
        }
        var $modal = $el.closest('.modal');
        if ($modal.length) {
            return $modal;
        }
        if (scopeParent && scopeParent.length) {
            return scopeParent;
        }
    } catch (ex) { /* ignore */ }
    return $(document.body);
}

/**
 * Constrói o objeto de opções Select2 a partir dos defaults GDI + regras de negócio (placeholder, allowClear, múltiplo).
 *
 * @param {jQuery} $el `<select>`
 * @param {jQuery} dropdownParentResolved parent já resolvido (ver `gdiSelect2ResolveDropdownParent`)
 * @param {Object} [extraOpts] fundido por último (uso pontual; preferir data-* quando possível)
 * @returns {Object}
 */
function gdiSelect2BuildOptions($el, dropdownParentResolved, extraOpts) {
    var $parent = dropdownParentResolved && dropdownParentResolved.length ? dropdownParentResolved : $(document.body);
    var opts = $.extend({}, GDI_SELECT2_DEFAULTS, {
        dropdownParent: $parent
    });
    var $first = $el.find('option:first');
    if ($el.prop('multiple')) {
        opts.placeholder = opts.placeholder || 'Selecione…';
        opts.allowClear = false;
    } else if ($first.length && String($first.val()) === '') {
        opts.allowClear = true;
    } else if ($el.attr('data-gdi-select2-allow-clear') === 'true') {
        opts.allowClear = true;
    } else {
        opts.allowClear = false;
    }
    if (extraOpts && typeof extraOpts === 'object') {
        $.extend(opts, extraOpts);
    }
    return opts;
}

/**
 * Pedido Ajax cancelado pelo Select2 ao digitar (termo anterior) — não é falha de servidor.
 * @param {jqXHR} jqXHR
 * @param {string} textStatus
 * @param {string} errorThrown
 * @returns {boolean}
 */
function gdiSelect2IsLookupAjaxAbort(jqXHR, textStatus, errorThrown) {
    if (textStatus === 'abort' || errorThrown === 'abort') {
        return true;
    }
    if (jqXHR && jqXHR.statusText && /abort|canceled/i.test(String(jqXHR.statusText))) {
        return true;
    }
    return false;
}

/**
 * Select2 com Ajax (contrato servidor: { items: [{ id, text }] } — ver GetClientesLookup / GetProdutosLookup).
 * Atributos no &lt;select&gt;: data-gdi-lookup-url (obrig.), data-gdi-lookup-min-length (opc., default 2).
 */
function gdiSelect2BuildAjaxOptions($el, dropdownParentResolved, extraOpts) {
    var url = ($el.attr('data-gdi-lookup-url') || '').trim();
    var minLen = parseInt($el.attr('data-gdi-lookup-min-length') || '2', 10);
    if (isNaN(minLen) || minLen < 0) {
        minLen = 2;
    }
    var opts = gdiSelect2BuildOptions($el, dropdownParentResolved, extraOpts);
    opts.minimumInputLength = minLen;
    var $ph = $el.find('option:first');
    if ($ph.length && String($ph.val()) === '') {
        opts.placeholder = ($ph.text() || '').trim() || 'Selecione…';
        opts.allowClear = true;
    }
    opts.ajax = {
        url: url,
        dataType: 'json',
        delay: 300,
        cache: true,
        data: function (params) {
            return {
                q: params.term || '',
                page: params.page || 1
            };
        },
        transport: function (params, success, failure) {
            var request = $.ajax($.extend({}, params, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            }));
            request.then(success);
            request.fail(function (jqXHR, textStatus, errorThrown) {
                /* Select2 cancela o GET anterior quando o utilizador ainda digita (delay 300ms).
                   jQuery dispara .fail() com textStatus "abort" — não mostrar modal nesse caso. */
                if (gdiSelect2IsLookupAjaxAbort(jqXHR, textStatus, errorThrown)) {
                    failure();
                    return;
                }
                var msg = 'Não foi possível carregar os resultados do lookup.';
                if (jqXHR && jqXHR.status === 404) {
                    msg = 'Endpoint de lookup não encontrado (404). Em views de área, use Url.Action(..., new { area = "" }) para controllers na raiz.';
                } else if (jqXHR && jqXHR.responseJSON) {
                    var j = jqXHR.responseJSON;
                    if (j.errorMessage) {
                        msg = j.errorMessage;
                    } else if (j.mensagem) {
                        msg = j.mensagem;
                    }
                }
                if (typeof GdiAjaxNotifyInconsistencias === 'function') {
                    GdiAjaxNotifyInconsistencias(msg, { severity: 'error' });
                } else if (typeof LibMessageError === 'function') {
                    LibMessageError('Atenção', msg);
                }
                failure();
            });
            return request;
        },
        processResults: function (data) {
            if (data && data.errorMessage) {
                if (typeof GdiAjaxNotifyInconsistencias === 'function') {
                    GdiAjaxNotifyInconsistencias(data.errorMessage, { severity: data.severity || 'error' });
                } else if (typeof LibMessageError === 'function') {
                    LibMessageError('Atenção', data.errorMessage);
                }
                return { results: [] };
            }
            var items = (data && data.items) ? data.items : [];
            return {
                results: items.map(function (x) {
                    return { id: x.id, text: x.text };
                })
            };
        }
    };
    return opts;
}

function gdiDestroySelect2IfAny($el) {
    try {
        if ($el.hasClass('select2-hidden-accessible') && typeof $el.select2 === 'function') {
            $el.select2('destroy');
        }
    } catch (e) { /* ignore */ }
}

/**
 * Destrói todas as instâncias Select2 de `<select>` dentro do nó (ex.: antes de `$(container).html(...)` ou reload de partial).
 *
 * @param {jQuery|string} root Elemento raiz ou seletor CSS
 * @example
 *   gdiDestroySelect2OnCollection('#mainModal');
 *   $('#meuPartial').load(url, function () { gdiInitSelect2OnCollection('#meuPartial select', null); });
 */
function gdiDestroySelect2OnCollection(root) {
    var $root = typeof root === 'string' ? $(root) : root;
    if (!$root || !$root.length || typeof $.fn.select2 !== 'function') {
        return;
    }
    $root.find('select.select2-hidden-accessible').each(function () {
        gdiDestroySelect2IfAny($(this));
    });
}

/** Sincroniza classes de erro do `<select>` para o `.select2-container` seguinte (BS5 + unobtrusive MVC). */
function gdiSyncSelect2ValidationToContainer($select) {
    try {
        var $c = $select.next('.select2-container');
        if (!$c.length) {
            return;
        }
        var invalid = $select.hasClass('input-validation-error')
            || $select.hasClass('is-invalid');
        $c.toggleClass('is-invalid', !!invalid);
    } catch (e) { /* ignore */ }
}

var _gdiSelect2ValidatorHooksBound = false;
function gdiBindSelect2ValidatorHooksOnce() {
    if (_gdiSelect2ValidatorHooksBound || typeof jQuery === 'undefined' || !$.validator) {
        return;
    }
    _gdiSelect2ValidatorHooksBound = true;
    try {
        /* Select2 esconde o <select>: o default :hidden faria o jQuery Validate ignorar o campo. */
        $.validator.setDefaults({
            ignore: ':hidden:not(select.select2-hidden-accessible)'
        });
        var oldHighlight = $.validator.defaults.highlight;
        var oldUnhighlight = $.validator.defaults.unhighlight;
        $.validator.defaults.highlight = function (element, errorClass, validClass) {
            if (typeof oldHighlight === 'function') {
                oldHighlight.call(this, element, errorClass, validClass);
            }
            var $el = $(element);
            if ($el.is('select') && $el.hasClass('select2-hidden-accessible')) {
                gdiSyncSelect2ValidationToContainer($el);
            }
        };
        $.validator.defaults.unhighlight = function (element, errorClass, validClass) {
            if (typeof oldUnhighlight === 'function') {
                oldUnhighlight.call(this, element, errorClass, validClass);
            }
            var $el = $(element);
            if ($el.is('select') && $el.hasClass('select2-hidden-accessible')) {
                gdiSyncSelect2ValidationToContainer($el);
            }
        };
    } catch (e) {
        if (window.GDI_DEBUG) {
            console.warn('[gdi-select2] gdiBindSelect2ValidatorHooksOnce', e);
        }
    }
}

var _gdiSelect2SearchFocusBound = false;
function gdiBindSelect2SearchFocusOnce() {
    if (_gdiSelect2SearchFocusBound || typeof $.fn.select2 !== 'function') {
        return;
    }
    _gdiSelect2SearchFocusBound = true;
    $(document).on('select2:open', 'select', function (e) {
        var $sel = $(e.target);
        if (!$sel.is('select') || $sel.is('[data-gdi-no-select2-search-focus]')) {
            return;
        }
        gdiFocusSelect2SearchField($sel);
    });
    $(document).on('select2:close change', 'select.select2-hidden-accessible', function () {
        gdiSyncSelect2ValidationToContainer($(this));
    });
}

function gdiFocusSelect2SearchField($select) {
    var attempt = 0;
    var maxAttempts = 12;
    function tryFocus() {
        attempt++;
        var input = null;
        try {
            var s2 = $select.data('select2');
            if (s2) {
                if (s2.dropdown && s2.dropdown.$search && s2.dropdown.$search.length) {
                    input = s2.dropdown.$search[0];
                }
                if (!input && s2.$container && s2.$container.length) {
                    var $inContainer = s2.$container.find('.select2-search__field');
                    if ($inContainer.length) {
                        input = $inContainer[0];
                    }
                }
            }
        } catch (ex) { /* ignore */ }
        if (!input && typeof jQuery !== 'undefined') {
            var $open = $select.next('.select2-container.select2-container--open');
            if ($open.length) {
                var $fb = $open.find('.select2-search__field');
                if ($fb.length) {
                    input = $fb[0];
                }
            }
        }
        if (input && !input.disabled && document.body.contains(input)) {
            try {
                input.focus({ preventScroll: true });
            } catch (e1) {
                try {
                    input.focus();
                } catch (e2) { /* ignore */ }
            }
            if (document.activeElement === input) {
                return;
            }
        }
        if (attempt < maxAttempts) {
            setTimeout(tryFocus, attempt < 3 ? 0 : 20);
        }
    }
    requestAnimationFrame(tryFocus);
}

/**
 * Inicializa Select2 em cada `<select>` da coleção (único ponto de entrada).
 *
 * @param {jQuery|string} selector Coleção jQuery ou seletor CSS
 * @param {jQuery|null} [dropdownParent] parent sugerido quando o select **não** está dentro de `.modal` (ex.: `$(document.body)`)
 * @param {Object} [extraOpts] opções Select2 fundidas após as regras GDI (evite nas views; uso interno/exceções)
 */
function gdiInitSelect2OnCollection(selector, dropdownParent, extraOpts) {
    if (typeof $.fn.select2 !== 'function') {
        return;
    }
    gdiBindSelect2ValidatorHooksOnce();
    gdiBindSelect2SearchFocusOnce();
    var $coll = typeof selector === 'string' ? $(selector) : selector;
    $coll.each(function () {
        var $el = $(this);
        if (!$el.is('select')) {
            return;
        }
        if ($el.is('[data-gdi-no-select2]') || $el.closest('[data-gdi-no-select2]').length) {
            if (window.GDI_DEBUG) {
                var host = $el.closest('[data-gdi-no-select2]').length ? $el.closest('[data-gdi-no-select2]').get(0) : $el.get(0);
                var reason = (host && host.getAttribute && host.getAttribute('data-gdi-no-select2')) || '';
                if (!String(reason).trim()) {
                    reason = '(data-gdi-no-select2 sem motivo textual — preencha o atributo)';
                }
                console.log('[gdi-select2] ignorado (data-gdi-no-select2):', reason, host);
            }
            return;
        }
        var lookupUrl = ($el.attr('data-gdi-lookup-url') || '').trim();
        var nOpts = gdiSelect2StaticOptionCount($el);
        if (!lookupUrl && nOpts <= 5 && $el.attr('data-gdi-select2-search') !== 'true') {
            gdiDestroySelect2IfAny($el);
            if (window.GDI_DEBUG) {
                console.log('[gdi-select2] lista curta (≤5 options), mantém <select> nativo. Forçar Select2: data-gdi-select2-search="true".', $el.get(0));
            }
            return;
        }
        var $resolvedParent = gdiSelect2ResolveDropdownParent($el, dropdownParent);
        gdiDestroySelect2IfAny($el);
        try {
            var buildFn = lookupUrl ? gdiSelect2BuildAjaxOptions : gdiSelect2BuildOptions;
            $el.select2(buildFn($el, $resolvedParent, extraOpts));
            gdiSyncSelect2ValidationToContainer($el);
        } catch (e) {
            if (window.GDI_DEBUG) {
                console.warn('[gdi-select2] init falhou', e, $el.get(0));
            }
        }
    });
}

/** Inicializa Select2 em todos os `<select>` do documento (após `jsInitForm`). */
function gdiInitSelect2Page() {
    gdiInitSelect2OnCollection($('select'), $(document.body));
}

/**
 * Select2 nos `<select>` do modal visível ou `#mainModal` com conteúdo carregado (dropdownParent = modal).
 * Scripts de partial em `#mainModal` podem correr antes de `.modal.show`; nesse caso usa-se `#mainModal` como âncora.
 */
function gdiInitSelect2Modal() {
    if (typeof $.fn.select2 !== 'function') {
        return;
    }
    var $m = $('.modal.show').last();
    if (!$m.length) {
        var $main = $('#mainModal');
        if ($main.length && $main.find('select').length) {
            $m = $main;
        }
    }
    /* Partial injectado: o <script> do HTML carregado pode correr antes de bootstrap.Modal.show();
       nesse caso ainda não há .modal.show — usa o último .modal que já contém <select> (ex.: #containerModalPedidoSeparacaoLotes). */
    if (!$m.length) {
        $m = $('.modal').filter(function () {
            return $(this).find('select').length > 0;
        }).last();
    }
    if (!$m.length) {
        return;
    }
    gdiInitSelect2OnCollection($m.find('select'), $m);
}

/**
 * Bootstrap 5 Modal regista FocusTrap em `document` (focusin em fase de bolha).
 * Se o dropdown do Select2 ficar fora do nó `.modal` ou houver corrida de foco,
 * o trap devolve o foco ao diálogo e o utilizador não consegue escrever na pesquisa.
 * Interromper a propagação neste alvo (após o foco já estar no input) evita que o handler do Modal execute.
 * @see https://github.com/select2/select2/issues/3125#issuecomment-149825873
 */
(function gdiBindBs5ModalSelect2SearchFocusinFixOnce() {
    if (window._gdiBs5ModalSelect2FocusinFix) {
        return;
    }
    window._gdiBs5ModalSelect2FocusinFix = true;
    document.addEventListener('focusin', function (e) {
        var t = e.target;
        if (!t || !t.classList || !t.classList.contains('select2-search__field')) {
            return;
        }
        /* Só com modal aberto: o FocusTrap BS5 é o que conflita com o foco na pesquisa. */
        if (!document.querySelector('.modal.show')) {
            return;
        }
        e.stopImmediatePropagation();
    }, false);
})();

$(function () {
    gdiBindSelect2ValidatorHooksOnce();
});
