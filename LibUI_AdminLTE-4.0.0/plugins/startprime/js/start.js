/**
 * Substitui bootstrap-waitingfor (removido): expõe window.waitingDialog para LibMessageProcessando
 * e para cópias antigas de start.js ainda em cache.
 */
(function libInstallGdiWaitingDialog() {
    var depth = 0;
    var bodyOverflowPrev = '';

    function ensureOverlay() {
        var el = document.getElementById('gdi-busy-overlay');
        if (el) return el;
        el = document.createElement('div');
        el.id = 'gdi-busy-overlay';
        el.className = 'gdi-busy-overlay';
        el.setAttribute('aria-hidden', 'true');
        el.innerHTML =
            '<div class="gdi-busy-backdrop" aria-hidden="true"></div>' +
            '<div class="gdi-busy-panel" role="status">' +
            '<div class="spinner-border text-light" aria-hidden="true"></div>' +
            '<p class="gdi-busy-message text-light mt-3 mb-0"></p>' +
            '</div>';
        document.body.appendChild(el);
        return el;
    }

    window.waitingDialog = {
        show: function (msg, opts) {
            if (msg === undefined || msg == null) msg = 'Processando...';
            else if (String(msg).length === 0) msg = 'Processando...';
            depth++;
            var root = ensureOverlay();
            var msgEl = root.querySelector('.gdi-busy-message');
            if (msgEl) msgEl.textContent = msg;
            if (depth === 1) {
                bodyOverflowPrev = document.body.style.overflow;
                document.body.style.overflow = 'hidden';
                root.style.display = 'flex';
                root.setAttribute('aria-hidden', 'false');
            }
        },
        hide: function (cb) {
            depth = Math.max(0, depth - 1);
            if (depth > 0) {
                if (typeof cb === 'function') cb();
                return;
            }
            var root = document.getElementById('gdi-busy-overlay');
            if (root) {
                root.style.display = 'none';
                root.setAttribute('aria-hidden', 'true');
            }
            document.body.style.overflow = bodyOverflowPrev || '';
            if (typeof cb === 'function') cb();
        }
    };
})();

function JsGetSelectedRows(TableName, ColumnName) {
    try {
        // Get Column Header
        var idTable = -1;
        var rowHeader = TableName.$('tr')[0];
        var cellsHeader = rowHeader.cells;
        for (x = 0; x < TableName.columns().count(); x++) {
            if (TableName.column(x).header().textContent == ColumnName) {
                idTable = x;
            }
        }

        var listaId = "";
        var rowsSelected = TableName.rows('.selected').data();
        if ((rowsSelected.length > 0) && (idTable > -1)) {
            for (y = 0; y < rowsSelected.length; y++) {
                var rowSelected = String(rowsSelected[y]).split(',');
                listaId += rowSelected[idTable] + ',';
            }
            if (rowsSelected.length == 1) {
                listaId = listaId.substring(0, listaId.length - 1);
            }
            return listaId;
        }
        else {
            return "";
        }
    }
    catch (err) {
        return "";
    }
}

function JsNewRecord(urlNew) {
    try
    {
        LibMessageProcessando("Novo Registro . . .");
        $(window.document.location).attr('href', urlNew);
    }
    catch (err) {
        alert("[JsNewRecord] " + err.message.toString());
    }
}

function jsYesCancelRecord(urlNew) {
    try {
        $(window.document.location).attr('href', urlNew);
        LibMessageProcessando();
    }
    catch (err) {
        alert("[jsYesCancelRecord] " + err.message.toString());
    }
}

function JsEditRecord(TableName, columnName, urlEdit)
{
    try
    {
        var selectedIds = JsGetSelectedRows(TableName, columnName);
        if (!selectedIds || selectedIds.length == 0) {
            LibMessageAlert("Atenção", "Selecione o registro", { size: 'small' });
        }
        else {
            if (selectedIds.indexOf(',') == -1) {
                $(window.document.location).attr('href', urlEdit + selectedIds);
                LibMessageProcessando();
            }
            else {
                LibMessageAlert("Atenção", "Selecione apenas 1(um) registro!", { size: 'small' });
            }
        }
    }
    catch (err) {
        alert("[jsYesEditRecord] " + err.message.toString());
    }
}

function JsEditRecordDoubleClick(selectedId, urlEdit) {
    try {
        $(window.document.location).attr('href', urlEdit + "/" + selectedId);
        LibMessageProcessando();
    }
    catch (err) {
        alert("[jsYesEditRecordDoubleClick] " + err.message.toString());
    }
}


function JsNewWindow(urlNew) {
    try {
        LibMessageProcessando("Processando . . .");
        $(window.document.location).attr('href', urlNew);
    }
    catch (err) {
        alert("[JsNewWindow] " + err.message.toString());
    }
}


/** Tooltips BS5: suporta data-bs-toggle (novo) e data-toggle (legado). */
function gdiInitTooltips(container) {
    try {
        if (typeof bootstrap === 'undefined' || typeof bootstrap.Tooltip === 'undefined') return;
        var root = container && container.querySelectorAll ? container : document;
        root.querySelectorAll('[data-bs-toggle="tooltip"], [data-toggle="tooltip"]').forEach(function (el) {
            bootstrap.Tooltip.getOrCreateInstance(el);
        });
    }
    catch (err) { /* silencioso: telas sem bootstrap global */ }
}

function fnGetSelected(TableName) {
    try {
        var aReturn = [];
        if (!TableName) {
            return aReturn;
        }
        /* DataTables 2+: instância Api (rows / row); legado: retorno de .dataTable() com fnGetNodes */
        if (typeof TableName.rows === 'function' && typeof TableName.row === 'function') {
            TableName.rows({ search: 'applied' }).every(function () {
                var node = this.node();
                if (node && $(node).hasClass('selected')) {
                    aReturn.push(node);
                }
            });
            return aReturn;
        }
        if (typeof TableName.fnGetNodes === 'function') {
            var aTrs = TableName.fnGetNodes();
            for (var i = 0; i < aTrs.length; i++) {
                if ($(aTrs[i]).hasClass('selected')) {
                    aReturn.push(aTrs[i]);
                }
            }
        }
        return aReturn;
    }
    catch (err) {
        alert("[fnGetSelected] " + err.message.toString());
    }
}


function isEmpty(str) {
    return typeof str == 'string' && !str.trim() || typeof str == 'undefined' || str === null;
}

function emptyIfNull(val) {
    if (val === undefined || val == null) {
        val = "";
    }
    return val;
}

function JsFormatMoney(money) {
    try {
        var temp = parseFloat(money + "");
        temp = temp.toLocaleString('Pt-br', { style: "currency", currency: "BRL" });
        money = temp.replace("R$", "R$ ");
    }
    catch (err) {
        money = "Erro: " + err.message.toString();
    }
    return money;
}

function JsFormatValorSemSeparador(numero) {
    try {
        var temp = parseFloat(numero + "");
        temp = temp.toLocaleString('Pt-br', { minimumFractionDigits: 2 });
        temp = temp.replace(".", "");
        numero = temp;
    }
    catch (err) {
        numero = "-1";
    }
    return numero;
}

function JsFormatNumero(numero) {
    try {
        var temp = parseFloat(numero + "");
        temp = temp.toLocaleString('Pt-br', { minimumFractionDigits: 2 });
        numero = temp;
    }
    catch (err) {
        numero = "-1";
    }
    return numero;
}

function JsIsNumber(n) {
    try {
        if (n == null) return 0
        else if (n == undefined) return 0
        else {
            n = n.replace(",", ".");
            return Number(parseFloat(n)) == n;
        }
    }
    catch (err) {
        alert("[jsYesIsNumber] " + err.message.toString());
    }
}

function jsYesIsFloat(n) {
    if (n == null) return 0
    else if (n == undefined) return 0
    else return Number(n) === n && n % 1 !== 0;
}

function jsYesIsInt(n) {
    if (n == null) return 0
    else if (n == undefined) return 0
    return Number(n) === n && n % 1 === 0;
}

/**
 * Indicador visual de filtro ativo em botões de limpar/filtro (DataTables yesFilterOnOff).
 * @param {string|number} yesFilterOnOff - "0"|"1" do JSON do servidor
 * @param {object} [opcoes]
 * @param {string} [opcoes.btnId] - id de um botão
 * @param {string[]} [opcoes.btnIds] - ids adicionais (ex.: filtro avançado)
 * @param {string} [opcoes.tituloInativo]
 * @param {string} [opcoes.tituloAtivo]
 */
function GdiAtualizarIndicadorFiltro(yesFilterOnOff, opcoes) {
    try {
        opcoes = opcoes || {};
        var ids = [];
        if (opcoes.btnId) { ids.push(opcoes.btnId); }
        if (opcoes.btnIds && opcoes.btnIds.length) {
            for (var j = 0; j < opcoes.btnIds.length; j++) { ids.push(opcoes.btnIds[j]); }
        }
        var ativo = (yesFilterOnOff === 1 || yesFilterOnOff === '1');
        for (var i = 0; i < ids.length; i++) {
            var btn = document.getElementById(ids[i]);
            if (!btn) { continue; }
            btn.classList.remove('btn-outline-secondary', 'btn-outline-warning');
            btn.classList.add(ativo ? 'btn-outline-warning' : 'btn-outline-secondary');
            if (opcoes.tituloAtivo && opcoes.tituloInativo) {
                btn.title = ativo ? opcoes.tituloAtivo : opcoes.tituloInativo;
            }
        }
    }
    catch (e) { }
}

function getValidationSummary(message) {
    try {
        if ((message.length > 0) && (message.indexOf("display:none") == -1)) {
            LibMessageProcessandoHide();
            GdiAjaxNotifyInconsistencias(message);
        }
    }
    catch (err) {
        alert("[getValidationSummary] " + err.message.toString());
    }
};

function windowLoading(show, typeWindow) {
    try {
        var refWindow = "";
        if (typeWindow == 1) { refWindow = "#loading" }
        else if (typeWindow == 2) { refWindow = "#loadingModal" };
        if (show == true) {
            $(refWindow).fadeIn();
            var opts = {
                lines: 12, // The number of lines to draw
                length: 7, // The length of each line
                width: 4, // The line thickness
                radius: 10, // The radius of the inner circle
                color: '#000', // #rgb or #rrggbb
                speed: 3, // Rounds per second
                trail: 60, // Afterglow percentage
                shadow: false, // Whether to render a shadow
                hwaccel: false // Whether to use hardware acceleration
            };
            var target = document.getElementById(refWindow.replace("#", ""));
            var spinner = new Spinner(opts).spin(target);
        }
        else {
            $(refWindow).fadeOut();
        }
    }
    catch (err) {
        alert("[windowLoading] " + err.message.toString());
    }
};


/**
 * Helper interno: centraliza a chamada a GdiSwalCompat.alert com ícone variável.
 * Elimina triplicação de LibMessageAlert/Success/Error — não chamar diretamente das views.
 * opcoes: icon, size, backdrop, buttons.ok, callback, etc. (mesclados sobre cfg, icon de opcoes sobrepõe o padrão).
 */
function _gdiSwalAlert(icon, Title, msg, opcoes) {
    try {
        if (typeof GdiSwalCompat !== 'undefined' && GdiSwalCompat && typeof GdiSwalCompat.alert === 'function') {
            var cfg = { title: Title, message: msg, icon: icon };
            if (opcoes && typeof opcoes === 'object') {
                for (var k in opcoes) {
                    if (Object.prototype.hasOwnProperty.call(opcoes, k)) cfg[k] = opcoes[k];
                }
            }
            GdiSwalCompat.alert(cfg);
        } else {
            alert((Title ? Title + "\n\n" : "") + (msg || ""));
        }
    }
    catch (err) {
        alert("[_gdiSwalAlert] " + err.message.toString());
    }
}

/**
 * Mensagens globais (GDI): delegam a GdiSwalCompat (SweetAlert2, ver `startprime/js/gdi-swal2-dialog-shim.js`).
 * LibMessageAlert(Title, msg, opcoes?) — opcoes: icon, size, backdrop, buttons.ok, etc.
 */
function LibMessageAlert(Title, msg, opcoes)   { _gdiSwalAlert('warning', Title, msg, opcoes); }

/** Sucesso (ícone success). opcoes: mesmas extensões de GdiSwalCompat.alert. */
function LibMessageSuccess(Title, msg, opcoes) { _gdiSwalAlert('success', Title, msg, opcoes); }

/** Erro (ícone error). */
function LibMessageError(Title, msg, opcoes)   { _gdiSwalAlert('error',   Title, msg, opcoes); }

/**
 * Falha de carregamento/atualização de DataTables (evento error.dt ou callback ajax.error da grelha).
 * Centraliza LibMessageError + texto legado "Falha ao processar os dados <b>…</b>" e, por omissão, LibMessageProcessandoHide.
 * Não altera contrato servidor nem opções da API DataTables — apenas feedback ao utilizador.
 * @param {string} detail texto injectado no corpo HTML (negrito) como no legado (message do error.dt, errorThrown do jQuery.ajax).
 * @param {object} [opcoes] title (default "Atenção"); hideProcessando (default true).
 */
function GdiDtNotifyLoadFailure(detail, opcoes) {
    var o = opcoes || {};
    if (o.hideProcessando !== false && typeof LibMessageProcessandoHide === 'function') {
        try { LibMessageProcessandoHide(); } catch (e1) { }
    }
    var title = (o.title != null && o.title !== '') ? o.title : 'Atenção';
    var d = (detail !== undefined && detail !== null) ? String(detail) : '';
    var msg = 'Falha ao processar os dados <b>' + d + '</b>';
    if (typeof LibMessageError === 'function') {
        LibMessageError(title, msg);
    } else {
        alert((title ? title + '\n\n' : '') + 'Falha ao processar os dados ' + d);
    }
}

/**
 * xhr.dt: campo opcional errorMessage no mesmo JSON da resposta DataTables (não altera aaData, sEcho, totais).
 * Campo opcional severity: "warning" | "error" | "danger" — omissão = aviso (LibMessageAlert), compatível com o legado.
 * @returns {boolean} true se errorMessage não vazio e diálogo exibido
 */
function GdiDtNotifyJsonErrorMessage(json, opcoes) {
    var o = opcoes || {};
    if (!json || json.errorMessage === undefined || json.errorMessage === null) {
        return false;
    }
    var raw = json.errorMessage.toString().replace(/^\s+|\s+$/g, '');
    if (!raw.length) {
        return false;
    }
    if (json.stackTrace) {
        try { console.error(json.stackTrace); } catch (eSt) { }
    }
    var title = (o.title != null && o.title !== '') ? o.title : 'Atenção';
    var sev = (json.severity !== undefined && json.severity !== null) ? String(json.severity).toLowerCase().replace(/^\s+|\s+$/g, '') : '';
    var asError = (sev === 'error' || sev === 'danger' || sev === 'err');
    if (asError) {
        if (typeof LibMessageError === 'function') {
            LibMessageError(title, raw);
        } else {
            alert((title ? title + '\n\n' : '') + raw.replace(/<[^>]+>/g, ''));
        }
    } else {
        if (typeof LibMessageAlert === 'function') {
            LibMessageAlert(title, raw);
        } else {
            alert((title ? title + '\n\n' : '') + raw.replace(/<[^>]+>/g, ''));
        }
    }
    return true;
}

/**
 * Ajax / modais (fora do DataTables): título legado "Verifique as inconsistências" + corpo HTML.
 * Opcional: opcoes.severity "error"|"danger" → LibMessageError; omissão → LibMessageAlert (aviso).
 * Por omissão chama LibMessageProcessandoHide se existir (opcoes.hideProcessando === false para não esconder).
 */
function GdiAjaxNotifyInconsistencias(body, opcoes) {
    var o = opcoes || {};
    if (o.hideProcessando !== false && typeof LibMessageProcessandoHide === 'function') {
        try { LibMessageProcessandoHide(); } catch (eH) { }
    }
    var title = (o.title != null && o.title !== '') ? o.title : 'Verifique as inconsistências';
    var raw = (body !== undefined && body !== null) ? String(body) : '';
    raw = raw.replace(/^\s+|\s+$/g, '');
    if (!raw.length) {
        return false;
    }
    var sev = (o.severity !== undefined && o.severity !== null) ? String(o.severity).toLowerCase().replace(/^\s+|\s+$/g, '') : '';
    var asError = (sev === 'error' || sev === 'danger' || sev === 'err');
    if (asError) {
        if (typeof LibMessageError === 'function') {
            LibMessageError(title, raw);
        } else {
            alert((title ? title + '\n\n' : '') + raw.replace(/<[^>]+>/g, ''));
        }
    } else {
        if (typeof LibMessageAlert === 'function') {
            LibMessageAlert(title, raw);
        } else {
            alert((title ? title + '\n\n' : '') + raw.replace(/<[^>]+>/g, ''));
        }
    }
    return true;
}

/**
 * Helper parametrizável para validação e submit de formulários CreateEdit.
 * Encapsula o padrão repetido em jsSalvarDados() nas views — use-o como corpo
 * da função local, mantendo a assinatura jsSalvarDados() em cada view.
 *
 * config = {
 *   campos:        Array de { id: '#fieldId', label: 'Rótulo exibido' }  (opcional)
 *   formId:        Seletor jQuery do <form> a submeter                    (obrigatório se não usar onSalvar)
 *   onValidar:     function() { return { ok: bool, msg: 'HTML' }; }      (validação extra — checkboxes, condicionais)
 *   onAntesSalvar: function() { ... }                                    (ex.: habilitar campos disabled antes do submit)
 *   onSalvar:      function() { ... }                                    (substitui $(formId).submit() — ex.: AJAX)
 * }
 */
function GdiSalvarDados(config) {
    try {
        var campos = config.campos || [];
        var hasError = false;
        var msgError = 'Campos <b>Obrigatórios</b> deverão ser preenchidos!<br/><br/>';

        for (var i = 0; i < campos.length; i++) {
            var c = campos[i];
            var val = $(c.id).val();
            if (isEmpty(val == null ? '' : val.toString().trim())) {
                hasError = true;
                msgError += 'Campo <b>' + c.label + '</b> é de preenchimento obrigatório!<br/>';
            }
        }

        if (!hasError && typeof config.onValidar === 'function') {
            var extra = config.onValidar();
            if (!extra.ok) {
                hasError = true;
                msgError += extra.msg;
            }
        }

        if (hasError) {
            var msg = (campos.length === 1 && typeof config.onValidar !== 'function')
                ? 'Campo <b>' + campos[0].label + '</b> é de preenchimento obrigatório!'
                : msgError;
            LibMessageAlert('Atenção', msg);
        } else {
            if (typeof config.onAntesSalvar === 'function') config.onAntesSalvar();
            LibMessageProcessando('Salvando Dados . . .');
            if (typeof config.onSalvar === 'function') {
                config.onSalvar();
            } else {
                $(config.formId).submit();
            }
        }
    } catch (err) {
        LibMessageError('Atenção', '[jsSalvarDados] ' + (err && err.message ? err.message.toString() : String(err)));
    }
}

/**
 * Confirmação Sim/Cancelar (2 botões) — GdiSwal2.confirm com ícone e tema Bootstrap 5.
 * opcoes: icon ('question'|'warning'|…), size, backdrop, onEscape/allowEscapeKey, confirmLabel, cancelLabel,
 * onConfirm, onCancel, callback(isConfirmed), hideOnCancel (default true → LibMessageHideAll no cancelar).
 */
function LibMessageConfirm(Title, msg, opcoes) {
    try {
        if (typeof GdiSwalCompat !== 'undefined' && GdiSwalCompat && typeof GdiSwalCompat.confirm === 'function') {
            var o = opcoes || {};
            var cfg = {
                title: Title,
                message: msg,
                icon: o.icon || 'question',
                size: o.size,
                backdrop: o.backdrop,
                allowEscapeKey: o.allowEscapeKey,
                onEscape: o.onEscape,
                closeButton: o.closeButton,
                buttons: {
                    cancel: { label: o.cancelLabel || '<i class="fa fa-undo me-2" aria-hidden="true"></i>Cancelar' },
                    confirm: { label: o.confirmLabel || '<i class="fa-solid fa-check me-2" aria-hidden="true"></i>Confirmar' }
                },
                callback: function (isConfirmed) {
                    if (isConfirmed) {
                        if (typeof o.onConfirm === 'function') {
                            o.onConfirm();
                        } else if (typeof o.callback === 'function') {
                            o.callback(true);
                        }
                    } else {
                        if (typeof o.onCancel === 'function') {
                            o.onCancel();
                        } else if (o.hideOnCancel !== false && typeof LibMessageHideAll === 'function') {
                            LibMessageHideAll();
                        } else if (typeof o.callback === 'function') {
                            o.callback(false);
                        }
                    }
                }
            };
            GdiSwalCompat.confirm(cfg);
        }
    }
    catch (err) {
        alert('[LibMessageConfirm] ' + err.message.toString());
    }
}

/** Checklist / mensagem longa — LibMessageConfirm com width large. */
function LibMessageConfirmChecklist(Title, msg, opcoes) {
    var o = opcoes || {};
    o.size = o.size || 'large';
    if (!o.icon) { o.icon = 'question'; }
    LibMessageConfirm(Title, msg, o);
}

/** Desativar anexo GED/pedido/financeiro — texto e ícone warning unificados. */
function GdiConfirmDesativarAnexo(onConfirm) {
    LibMessageConfirm('Confirmação',
        'Deseja REALMENTE DESATIVAR esse arquivo anexo ?<br/><br/>' +
        '<b>Atenção:</b> O Log dessa operação ficará armazenado no banco de dados para futuras auditorias!',
        {
            icon: 'warning',
            onEscape: false,
            backdrop: true,
            confirmLabel: '<i class="fa-solid fa-save me-2" aria-hidden="true"></i>Confirmar',
            onConfirm: onConfirm
        });
}

/** Modal rico (footer HTML, vários botões) — repassa a GdiSwalCompat.dialog / SweetAlert2. */
function LibMessageDialog(options) {
    try {
        if (typeof GdiSwalCompat !== 'undefined' && GdiSwalCompat && typeof GdiSwalCompat.dialog === 'function') {
            return GdiSwalCompat.dialog(options);
        }
    }
    catch (err) {
        alert("[LibMessageDialog] " + err.message.toString());
    }
}

/** Fecha diálogos Swal abertos pelo shim (equivalente a GdiSwalCompat.hideAll). */
function LibMessageHideAll() {
    try {
        if (typeof GdiSwalCompat !== 'undefined' && GdiSwalCompat && typeof GdiSwalCompat.hideAll === 'function') {
            GdiSwalCompat.hideAll();
        } else if (typeof Swal !== 'undefined' && typeof Swal.close === 'function') {
            Swal.close();
        }
    }
    catch (err) {
        alert("[LibMessageHideAll] " + err.message.toString());
    }
}

function LibMessageProcessando(msg) {
    try {
        waitingDialog.show(msg, {});
    }
    catch (err) {
        alert("[yesMessageProcessando] " + err.message.toString());
    }
}

function LibMessageProcessandoHide() {
    try {
        waitingDialog.hide();
    }
    catch (err) {
        alert("[LibMessageProcessandoHide] " + err.message.toString());
    }
}

/**
 * DataTables (global): fecha LibMessageProcessando após xhr/draw da grelha.
 * Cobre LibMessageProcessando + DataTable().draw() sem hide local em cada view.
 * waitingDialog.hide é idempotente (contador de profundidade); GdiDtNotifyLoadFailure também chama hide em error.dt.
 */
(function gdiDataTablesProcessandoAutoHide() {
    function gdiDtTryHideProcessando() {
        if (typeof LibMessageProcessandoHide !== 'function') return;
        try { LibMessageProcessandoHide(); } catch (e) { }
    }
    if (typeof jQuery === 'undefined') return;
    jQuery(function () {
        if (!jQuery.fn.dataTable) return;
        jQuery(document).on('xhr.dt error.dt draw.dt', gdiDtTryHideProcessando);
    });
}());

/** Navegação MVC a partir do menu lateral: overlay "processando" antes do full load (mesmo padrão que JsNewRecord). Só .app-sidebar; ignora novo separador / modificadores / download / target extra. */
(function gdiSidebarNavProcessando() {
    document.addEventListener('click', function (e) {
        try {
            if (e.defaultPrevented || e.button !== 0) return;
            if (e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;
            var t = e.target;
            if (!t || !t.closest) return;
            var side = t.closest('.app-sidebar');
            if (!side) return;
            var a = t.closest('a[href]');
            if (!a || !side.contains(a)) return;
            var href = a.getAttribute('href');
            if (!href || href === '#' || /^\s*javascript:/i.test(href)) return;
            if (a.hasAttribute('download')) return;
            var tgt = (a.getAttribute('target') || '').trim().toLowerCase();
            if (tgt === '_blank') return;
            if (tgt && tgt !== '_self') return;
            e.preventDefault();
            if (typeof LibMessageProcessando === 'function') {
                LibMessageProcessando('Carregando . . .');
            }
            window.location.href = a.href;
        } catch (err1) { /* navegação nativa */ }
    }, false);
}());

function yesDestroyModal(selector)
{
    try
    {
        var $modal = $(selector);
        $modal.modal('hide');
        $modal.on('hidden.bs.modal', function () {
            $modal.off();               // limpa eventos customizados
            $modal.removeData();        // remove dados associados
            $modal.remove();            // remove do DOM
        });
    }
    catch (err) {
        alert("[yesDestroyModal] " + err.message.toString());
    }
}




var __gdiTdLocaleDone = false;
var __gdiTdAutoIg = 0;

function jsResolveDatepickerSelector(DatepickerName) {
    var s = (DatepickerName == null) ? '' : DatepickerName.toString().trim();
    if (!s) return s;
    if (s.charAt(0) === '#' || s.charAt(0) === '.' || s.charAt(0) === '[') return s;
    return '#' + s;
}

function jsEnsureGdiTempusLocale() {
    if (__gdiTdLocaleDone || typeof tempusDominus === 'undefined' || !tempusDominus.loadLocale) return;
    tempusDominus.loadLocale({
        name: 'pt-BR',
        localization: {
            today: 'Hoje',
            clear: 'Limpar seleção',
            close: 'Fechar',
            selectMonth: 'Selecionar mês',
            previousMonth: 'Mês anterior',
            nextMonth: 'Próximo mês',
            selectYear: 'Selecionar ano',
            previousYear: 'Ano anterior',
            nextYear: 'Próximo ano',
            selectDecade: 'Selecionar década',
            previousDecade: 'Década anterior',
            nextDecade: 'Próxima década',
            previousCentury: 'Século anterior',
            nextCentury: 'Próximo século',
            pickHour: 'Escolher hora',
            incrementHour: 'Aumentar hora',
            decrementHour: 'Diminuir hora',
            pickMinute: 'Escolher minuto',
            incrementMinute: 'Aumentar minuto',
            decrementMinute: 'Diminuir minuto',
            pickSecond: 'Escolher segundo',
            incrementSecond: 'Aumentar segundo',
            decrementSecond: 'Diminuir segundo',
            toggleMeridiem: 'Alternar AM/PM',
            selectTime: 'Selecionar horário',
            selectDate: 'Selecionar data',
            dayViewHeaderFormat: { month: 'long', year: 'numeric' },
            startOfTheWeek: 0,
            locale: 'pt-BR',
            dateFormats: {
                L: 'dd/MM/yyyy',
                LL: "d 'de' MMMM 'de' yyyy",
                LLL: "d 'de' MMMM 'de' yyyy HH:mm",
                LLLL: "EEEE, d 'de' MMMM 'de' yyyy HH:mm",
                LT: 'HH:mm',
                LTS: 'HH:mm:ss'
            },
            ordinal: function (n) { return n + 'º'; },
            format: 'L'
        }
    });
    tempusDominus.locale('pt-BR');
    __gdiTdLocaleDone = true;
}

function jsTdGetWrap(DatepickerName) {
    var $t = $(jsResolveDatepickerSelector(DatepickerName));
    if (!$t.length) return $();
    if ($t.hasClass('input-group')) return $t;
    var $p = $t.closest('.input-group');
    if ($p.length) return $p;
    return $t;
}

function jsTdEnsureMarkup($wrap) {
    if (!$wrap.length || !$wrap.is('.input-group')) return;
    if ($wrap.attr('data-td-target-input')) {
        $wrap.removeClass('date');
        return;
    }
    var id = $wrap.attr('id');
    if (!id) {
        __gdiTdAutoIg += 1;
        id = 'td_input_group_' + __gdiTdAutoIg;
        $wrap.attr('id', id);
    }
    var sel = '#' + id;
    $wrap.removeClass('date');
    $wrap.attr('data-td-target-input', 'nearest');
    $wrap.attr('data-td-target-toggle', 'nearest');
    var $inp = $wrap.find('input').first();
    if ($inp.length) {
        $inp.addClass('tempus-dominus-input');
        $inp.attr('data-td-target', sel);
    }
    $wrap.find('.input-group-text').each(function () {
        $(this).attr('data-td-target', sel).attr('data-td-toggle', 'datetimepicker');
    });
}

function jsTdDispose($wrap) {
    if (!$wrap || !$wrap.length) return;
    try {
        var inst = $wrap.data('td');
        if (inst && typeof inst.dispose === 'function') {
            inst.dispose();
        }
    } catch (e) { /* noop */ }
    try {
        $wrap.removeData('td');
    } catch (e2) { /* noop */ }
}

/** Atualiza a data após o init. Tempus Dominus v6 não expõe o método jQuery `date` (o provider quebraria com "No method named \"date\""). */
function jsTdSetPickerDateFromJsDate($wrap, jsDate) {
    if (!$wrap || !$wrap.length || !jsDate) {
        return;
    }
    if (typeof tempusDominus === 'undefined' || !tempusDominus.DateTime || typeof tempusDominus.DateTime.convert !== 'function') {
        return;
    }
    var inst = $wrap.data('td');
    if (!inst || !inst.dates || typeof inst.dates.setValue !== 'function') {
        return;
    }
    var loc = inst.optionsStore && inst.optionsStore.options && inst.optionsStore.options.localization
        ? inst.optionsStore.options.localization
        : undefined;
    var dt = loc
        ? tempusDominus.DateTime.convert(jsDate, 'pt-BR', loc)
        : tempusDominus.DateTime.convert(jsDate, 'pt-BR');
    inst.dates.setValue(dt, 0);
}

function jsTdOptsDateOnly(defaultDate) {
    var o = {
        localization: {
            locale: 'pt-BR',
            format: 'L'
        },
        display: {
            theme: 'light',
            components: {
                calendar: true,
                date: true,
                month: true,
                year: true,
                decades: true,
                clock: false,
                hours: false,
                minutes: false,
                seconds: false
            }
        }
    };
    if (defaultDate) {
        o.defaultDate = defaultDate;
    }
    return o;
}

function jsTdOptsMonthYear(defaultDate) {
    var o = {
        display: {
            theme: 'light',
            viewMode: 'months',
            components: {
                calendar: true,
                date: true,
                month: true,
                year: true,
                decades: true,
                clock: false,
                hours: false,
                minutes: false,
                seconds: false
            }
        }
    };
    if (defaultDate) {
        o.defaultDate = defaultDate;
    }
    return o;
}

/** Lê data do Tempus Dominus (instância em .input-group) ou faz parse dd/MM/yyyy ou MM/yyyy do input. Substitui $(sel).datepicker('getDate'). */
function jsDatepickerGetDate(selector) {
    try {
        var $start = $(jsResolveDatepickerSelector(selector));
        if (!$start.length) {
            return null;
        }
        var $wrap = $start.hasClass('input-group') ? $start : $start.closest('.input-group');
        if (!$wrap.length) {
            $wrap = $start;
        }
        var picker = $wrap.data('td');
        if (picker && picker.dates && picker.dates.lastPicked) {
            var dt = picker.dates.lastPicked;
            return new Date(dt.year, dt.month, dt.date);
        }
        var $inp = $wrap.find('input').first();
        if (!$inp.length && $start.is('input')) {
            $inp = $start;
        }
        var txt = ($inp.val() || '').trim();
        if (!txt) {
            return null;
        }
        var m = txt.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})/);
        if (m) {
            return new Date(parseInt(m[3], 10), parseInt(m[2], 10) - 1, parseInt(m[1], 10));
        }
        m = txt.match(/^(\d{1,2})\/(\d{4})$/);
        if (m) {
            return new Date(parseInt(m[2], 10), parseInt(m[1], 10) - 1, 1);
        }
        return null;
    } catch (err) {
        alert('[jsDatepickerGetDate] ' + err.message.toString());
        return null;
    }
}

function jsGetDateDDMMYYYY(data) {
    try {
        if (data == null) {
            return "";
        }
        else {
            var dia = data.getDate();
            if (dia < 10) { dia = "0" + dia; }
            var mes = data.getMonth() + 1;
            if (mes < 10) { mes = "0" + mes; }
            var ano = data.getFullYear();
            var resultado = dia + "/" + mes + "/" + ano;
            return resultado;
        }
    }
    catch (err) {
        alert("[jsGetDateDDMMYYYY] " + err.message.toString());
    }
}

/* Select2: ver gdi-select2.js (gdiInitSelect2OnCollection, gdiInitSelect2Page, gdiInitSelect2Modal, gdiDestroySelect2OnCollection, gdiIsSelect2Interacting, GDI_SELECT2_DEFAULTS). */


function jsInitForm() {
    try {
        LibMessageHideAll();
        LibMessageProcessandoHide();
        gdiInitTooltips(document);
        $('select').each(function () {
            var $el = $(this);
            if (!$el.hasClass('form-control')) {
                $el.addClass('form-control');
            }
        });
        if (typeof gdiInitSelect2Page === 'function') {
            gdiInitSelect2Page();
        }
        $("input[type=text]").keypress(function (e) {
            if (e.keyCode == 13) {
                e.preventDefault();
            }
        });
        /* Largura do container de tabelas: 100% (o legado 100 * .width sem () gerava NaN e ativava width:5000px no CSS). Mesmo alvo que .table-responsive.scroll-body-horizontal nas views. */
        $('.scroll-body-horizontal').css({ width: '100%', maxWidth: '100%', boxSizing: 'border-box' });
        $('.gdi-form-table-scroll, .scroll-body-horizontal').each(function () {
            if (this.querySelector && this.querySelector('table:not(.display):not(.dataTable)')) {
                this.scrollLeft = 0;
            }
        });

        // ---- FOCO NO PRIMEIRO EDITÁVEL VISÍVEL (páginas sem data-autofocus) ----
        if (!document.querySelector('[data-autofocus]')) {
            setTimeout(function () {
                var $scope = $('form').length ? $('form').first() : $(document);

                var $first = $scope.find(
                    'input:not([type=hidden]):not([disabled]):not([readonly]), ' +
                    'textarea:not([disabled]):not([readonly]), ' +
                    'select:not([disabled]):not([readonly]), ' +
                    '[contenteditable="true"]'
                ).filter(':visible').first();
                if (!$first.length) return;
                if ($first.is('select') && $first.hasClass('select2-hidden-accessible')) {
                    var $cont = $first.next('.select2-container');
                    if ($cont.length) {
                        $cont.find('.select2-selection').trigger('focus');
                    } else {
                        $first.focus();
                    }
                } else {
                    $first.focus();
                }
            }, 150);
        }
    }
    catch (err) {
        alert("[jsInitForm] " + err.message.toString());
    }
}

function jsInitModal()
{
    try
    {
        LibMessageHideAll();
        LibMessageProcessandoHide();
        var modalEl = document.querySelector('.modal.show');
        gdiInitTooltips(modalEl || document);
        $('select').each(function () {
            var $el = $(this);
            if (!$el.hasClass('form-control')) {
                $el.addClass('form-control');
            }
        });
        if (typeof gdiInitSelect2Modal === 'function') {
            gdiInitSelect2Modal();
        }
        $("input[type=text]").keypress(function (e) {
            if (e.keyCode == 13) {
                e.preventDefault();
            }
        });
        $('.scroll-modal-horizontal').css({ width: '100%', maxWidth: '100%', boxSizing: 'border-box' });

        /* Foco no modal: gdiAutoFocusModal (shown.bs.modal) no final deste arquivo. */

    }
    catch (err) {
        alert("[jsInitModal] " + err.message.toString());
    }
}

/**
 * Modal + Select2 (ex.: Movimentos/ModalPedidoInsertEditItem — PN/CD/Delivery):
 * evita dropdown cortado por overflow-y do .modal-body; callbacks existiam nas views sem implementação (ReferenceError).
 */
function jsHandleSelectOpen() {
    try {
        var $b = $('.modal.show .modal-body').first();
        if (!$b.length) {
            return;
        }
        if ($b.data('gdiSelOv') !== '1') {
            $b.data('gdiSelOv', '1');
            $b.data('gdiSelOvX', $b.css('overflow-x'));
            $b.data('gdiSelOvY', $b.css('overflow-y'));
        }
        $b.css({ overflow: 'visible', overflowX: 'visible', overflowY: 'visible' });
    } catch (err) { /* ignore */ }
}

function jsHandleSelectClose() {
    try {
        var $b = $('.modal.show .modal-body').first();
        if (!$b.length) {
            return;
        }
        if ($b.data('gdiSelOv') === '1') {
            $b.css({ overflow: '', overflowX: $b.data('gdiSelOvX'), overflowY: $b.data('gdiSelOvY') });
            $b.removeData('gdiSelOv');
            $b.removeData('gdiSelOvX');
            $b.removeData('gdiSelOvY');
        }
    } catch (err) { /* ignore */ }
}

(function gdiBindModalSelectOverflowCleanupOnce() {
    if (window._gdiModalSelectOverflowCleanup) {
        return;
    }
    window._gdiModalSelectOverflowCleanup = true;
    document.addEventListener('hidden.bs.modal', function () {
        try {
            $('.modal-body').each(function () {
                var $b = $(this);
                if ($b.data('gdiSelOv') === '1') {
                    $b.css({ overflow: '', overflowX: $b.data('gdiSelOvX'), overflowY: $b.data('gdiSelOvY') });
                    $b.removeData('gdiSelOv');
                    $b.removeData('gdiSelOvX');
                    $b.removeData('gdiSelOvY');
                }
            });
        } catch (e) { /* ignore */ }
    });
}());

/**
 * Modais de conferência de lotes (abas 01–50): após Select2 global, mantém só o painel ativo inicializado
 * e reinicia ao mudar de aba (evita Select2 em display:none).
 * @param {string} formSelector ex.: '#ModalConferenciaEstoqueItem'
 * @param {string} dropdownParentSelector ex.: '#mainModal' ou '#containerModalPedidoSeparacaoLotes'
 */
function gdiConferenciaLotesTabsSelect2Init(formSelector, dropdownParentSelector) {
    try {
        if (typeof jQuery === 'undefined' || typeof gdiDestroySelect2OnCollection !== 'function' || typeof gdiInitSelect2OnCollection !== 'function') {
            return;
        }
        var $form = $(formSelector);
        if (!$form.length) {
            return;
        }
        if (!$form.find('#crud-tabs').length || !$form.find('#crud-tabs-content').length) {
            return;
        }
        var $parent = $(dropdownParentSelector || '#mainModal');

        function refreshActivePaneSelect2() {
            $form.find('#crud-tabs-content .tab-pane:not(.active)').each(function () {
                gdiDestroySelect2OnCollection(this);
            });
            var $active = $form.find('#crud-tabs-content .tab-pane.active');
            if ($active.length) {
                gdiInitSelect2OnCollection($active.find('select'), $parent.length ? $parent : null);
            }
        }

        refreshActivePaneSelect2();

        /* shown.bs.tab dispara no trigger (âncora), não necessariamente no <ul> — bind direto nas abas. */
        $form.find('#crud-tabs a[data-bs-toggle="pill"], #crud-tabs a[data-bs-toggle="tab"]')
            .off('shown.bs.tab.gdiLotes')
            .on('shown.bs.tab.gdiLotes', function () {
                refreshActivePaneSelect2();
            });
    } catch (e) { /* ignore */ }
}


function jsYesShowNavigator() {
    try {
        var msg = "";
        try { msg += "<b>AppCodeName:</b> " + navigator.appCodeName + "<br/>" } catch (e) { };
        try { msg += "<b>AppName:</b> " + navigator.appName + "<br/>" } catch (e) { };
        try { msg += "<b>AppVersion:</b> " + navigator.appVersion + "<br/>" } catch (e) { };
        try { msg += "<b>Battery:</b> " + navigator.battery + "<br/>" } catch (e) { };
        try {
            var NetworkInformation = navigator.connection;
            if (NetworkInformation != null) {
                msg += "<b>Connection.downlink:</b> " + NetworkInformation.downlink + "<br/>";
                msg += "<b>Connection.downlinkMax:</b> " + NetworkInformation.downlinkMax + "<br/>";
                msg += "<b>Connection.effectiveType:</b> " + NetworkInformation.effectiveType + "<br/>";
                msg += "<b>Connection.rtt:</b> " + NetworkInformation.rtt + "<br/>";
                msg += "<b>Connection.saveData:</b> " + NetworkInformation.saveData + "<br/>";
                msg += "<b>Connection.type:</b> " + NetworkInformation.type + "<br/>";
            }
            else {
                msg += "<b>Connection:</b> null" + "<br/>"
            }
        } catch (e) { };
        try {
            var gps = navigator.geolocation.getCurrentPosition(
                function (position) {
                    var msgPosition = "";
                    for (key in position.coords) {
                        msgPosition += "<b>" + key + ":</b> " + position.coords[key] + "<br/>";
                    }
                    if (msgPosition.length > 0) { LibMessageAlert("GPS", msgPosition); };
                });
        } catch (e) { };
        try { msg += "<b>JavaEnabled:</b> " + navigator.javaEnabled + "<br/>" } catch (e) { };
        try { msg += "<b>OnLine:</b> " + navigator.onLine + "<br/>" } catch (e) { };
        try { msg += "<b>Oscpu:</b> " + navigator.oscpu + "<br/>" } catch (e) { };
        try { msg += "<b>Platform:</b> " + navigator.platform + "<br/>" } catch (e) { };
        try { msg += "<b>UserAgent:</b> " + navigator.userAgent + "<br/>" } catch (e) { };
        try { msg += "<b>Id:</b> " + navigator.id + "<br/>" } catch (e) { };
        try { msg += "<b>Vendor:</b> " + navigator.vendor + "<br/>" } catch (e) { };
        try { msg += "<b>VendorSub:</b> " + navigator.vendorSub + "<br/>" } catch (e) { };
        LibMessageAlert("Informações Gerais", msg)
    }
    catch (err) {
        alert("[jsYesShowNavigator] " + err.message.toString());
    }

}


function jsDatepicker(DatepickerName) {
    try {
        if (typeof window.tempusDominus === 'undefined' || typeof $.fn.tempusDominus !== 'function') {
            alert('[jsDatepicker] Tempus Dominus não carregado (tempus-dominus.min.js e jQuery-provider após jQuery).');
            return;
        }
        jsEnsureGdiTempusLocale();
        var $wrap = jsTdGetWrap(DatepickerName);
        if (!$wrap.length) {
            return;
        }
        jsTdEnsureMarkup($wrap);
        jsTdDispose($wrap);
        $wrap.tempusDominus(jsTdOptsDateOnly());
    }
    catch (err) {
        var msg = err.message.toString();
        if (msg.indexOf('datepicker') !== -1) {
            msg += ' — As telas usam Tempus Dominus (não jQuery UI datepicker). Isso costuma indicar cópia antiga de start.js em cache ou deploy desatualizado; tente Ctrl+F5 ou confirme a publicação de LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js.';
        }
        alert("[jsDatepicker] " + msg);
    }
}

function jsInitDateTimepicker(DatepickerName) {
    try {
        if (typeof $.fn.tempusDominus !== 'function') {
            alert('[jsInitDateTimepicker] Tempus Dominus não carregado.');
            return;
        }
        jsEnsureGdiTempusLocale();
        var $wrap = jsTdGetWrap(DatepickerName);
        if (!$wrap.length) {
            return;
        }
        jsTdEnsureMarkup($wrap);
        jsTdDispose($wrap);
        $wrap.tempusDominus({
            display: {
                theme: 'light',
                sideBySide: true,
                viewMode: 'calendar',
                components: {
                    calendar: true,
                    date: true,
                    month: true,
                    year: true,
                    decades: true,
                    clock: true,
                    hours: true,
                    minutes: true,
                    seconds: false
                }
            },
            localization: { format: 'L LT' }
        });
    }
    catch (err) {
        alert("[jsInitDateTimepicker] " + err.message.toString());
    }
}


function jsDatepickerToday(DatepickerName) {
    try {
        if (typeof $.fn.tempusDominus !== 'function') {
            alert('[jsDatepickerToday] Tempus Dominus não carregado.');
            return;
        }
        jsEnsureGdiTempusLocale();
        var $wrap = jsTdGetWrap(DatepickerName);
        if (!$wrap.length) {
            return;
        }
        jsTdEnsureMarkup($wrap);
        jsTdDispose($wrap);
        var now = new Date();
        $wrap.tempusDominus(jsTdOptsDateOnly(now));
        jsTdSetPickerDateFromJsDate($wrap, now);
    }
    catch (err) {
        alert("[jsDatepickerToday] " + err.message.toString());
    }
}

function jsDatepickerFirstDayMonth(DatepickerName) {
    try {
        if (typeof $.fn.tempusDominus !== 'function') {
            alert('[jsDatepickerFirstDayMonth] Tempus Dominus não carregado.');
            return;
        }
        jsEnsureGdiTempusLocale();
        var $wrap = jsTdGetWrap(DatepickerName);
        if (!$wrap.length) {
            return;
        }
        var d = new Date();
        var startDate = new Date(d.getFullYear(), d.getMonth(), 1);
        jsTdEnsureMarkup($wrap);
        jsTdDispose($wrap);
        $wrap.tempusDominus(jsTdOptsDateOnly(startDate));
        jsTdSetPickerDateFromJsDate($wrap, startDate);
    }
    catch (err) {
        alert("[jsDatepickerFirstDayMonth] " + err.message.toString());
    }
}

function jsDatepickerLastDayMonth(DatepickerName) {
    try {
        if (typeof $.fn.tempusDominus !== 'function') {
            alert('[jsDatepickerLastDayMonth] Tempus Dominus não carregado.');
            return;
        }
        jsEnsureGdiTempusLocale();
        var $wrap = jsTdGetWrap(DatepickerName);
        if (!$wrap.length) {
            return;
        }
        var d = new Date();
        var endDate = new Date(d.getFullYear(), d.getMonth() + 1, 0);
        jsTdEnsureMarkup($wrap);
        jsTdDispose($wrap);
        $wrap.tempusDominus(jsTdOptsDateOnly(endDate));
        jsTdSetPickerDateFromJsDate($wrap, endDate);
    }
    catch (err) {
        alert("[jsDatepickerLastDayMonth] " + err.message.toString());
    }
}


function jsDatepickerMonthAndYear(DatepickerName) {
    try {
        if (typeof $.fn.tempusDominus !== 'function') {
            alert('[jsDatepickerMonthAndYear] Tempus Dominus não carregado.');
            return;
        }
        jsEnsureGdiTempusLocale();
        var $wrap = jsTdGetWrap(DatepickerName);
        if (!$wrap.length) {
            return;
        }
        var d = new Date();
        var startDate = new Date(d.getFullYear(), d.getMonth(), 1);
        jsTdEnsureMarkup($wrap);
        jsTdDispose($wrap);
        $wrap.tempusDominus(jsTdOptsMonthYear(startDate));
        jsTdSetPickerDateFromJsDate($wrap, startDate);
    }
    catch (err) {
        alert("[jsDatepickerMonthAndYear] " + err.message.toString());
    }
}


/* G-TREE-01 — Wunderbaum (flag GdiPageScripts Jstree=16) */
function gdiHasWunderbaum() {
    return typeof window.mar10 !== 'undefined' && typeof window.mar10.Wunderbaum === 'function';
}

function GdiTreeNormalizeIcon(icon) {
    if (!icon) {
        return icon;
    }
    var s = String(icon);
    if (s.indexOf('<') >= 0) {
        return s;
    }
    if (/\.(png|jpe?g|gif|svg|webp|ico)(\?|$)/i.test(s) || (s.indexOf('/') >= 0 && !/\s/.test(s))) {
        return s;
    }
    if (/\bfa-[a-z0-9-]+\b/i.test(s)) {
        return '<i class="' + s + '" aria-hidden="true"></i>';
    }
    return s;
}

function GdiTreeMapNode(n) {
    if (!n) {
        return null;
    }
    var key = (n.id !== undefined && n.id !== null) ? String(n.id) : '';
    var node = {
        key: key,
        title: n.text || '',
        expanded: !!(n.state && n.state.opened)
    };
    if (n.icon) {
        node.icon = GdiTreeNormalizeIcon(n.icon);
    }
    if (key === '-1' || (n.state && n.state.disabled)) {
        node.unselectable = true;
    }
    var kids = n.children || [];
    if (kids.length) {
        node.children = kids.map(GdiTreeMapNode).filter(function (x) { return !!x; });
    }
    return node;
}

function GdiTreeNormalizeSource(data) {
    if (!data) {
        return [];
    }
    if (Array.isArray(data)) {
        return data.map(GdiTreeMapNode).filter(function (x) { return !!x; });
    }
    var one = GdiTreeMapNode(data);
    return one ? [one] : [];
}

function GdiTreeGetSelectedKey(treeRef) {
    if (!treeRef) {
        return '';
    }
    if (typeof treeRef.getSelectedNodes !== 'function') {
        return '';
    }
    var wbNodes = treeRef.getSelectedNodes();
    if (wbNodes && wbNodes.length) {
        return String(wbNodes[0].key);
    }
    /* Wunderbaum: clique define activeNode, não selected (sem checkbox). */
    var active = treeRef.activeNode;
    if (active) {
        if (typeof active.isRootNode === 'function' && active.isRootNode()) {
            return '';
        }
        if (active.key && String(active.key) !== '__root__') {
            return String(active.key);
        }
    }
    return '';
}

function GdiTreeInit(elementSelectorOrElem, jstreeSource, options) {
    var el = elementSelectorOrElem;
    if (typeof elementSelectorOrElem === 'string') {
        el = document.querySelector(elementSelectorOrElem);
    } else if (typeof jQuery !== 'undefined' && elementSelectorOrElem instanceof jQuery && elementSelectorOrElem.length) {
        el = elementSelectorOrElem[0];
    }
    if (!el) {
        throw new Error('Elemento da árvore não encontrado.');
    }
    if (!gdiHasWunderbaum()) {
        throw new Error('Wunderbaum indisponível.');
    }
    el.innerHTML = '';
    var opts = Object.assign({
        element: el,
        source: GdiTreeNormalizeSource(jstreeSource),
        selectMode: 'single'
    }, options || {});
    return new mar10.Wunderbaum(opts);
}

function jsYesEditRecordJsTree(JsTreeName, urlEdit) {
    var selectedKey = GdiTreeGetSelectedKey(JsTreeName);
    if (!selectedKey || selectedKey === '-1') {
        LibMessageAlert("Atenção", "Selecione o item");
    }
    else {
        if (selectedKey.indexOf('R') >= 0) {
            LibMessageAlert("Atenção", "Selecione o item permitido");
        }
        else if (selectedKey.indexOf(',') === -1) {
            $(window.document.location).attr('href', urlEdit + selectedKey);
        }
        else {
            LibMessageAlert("Atenção", "Para edição/visualização, selecione apenas 1(um) item!");
        }
    }
}

/* ============================================================
   gdiFocusElement — foco em input/textarea/select (incl. Select2)
   ============================================================ */
function gdiFocusElement(el, withSelect) {
    if (!el) { return; }
    try {
        if (typeof jQuery !== 'undefined' && el.tagName === 'SELECT' && $(el).hasClass('select2-hidden-accessible')) {
            var $cont = $(el).next('.select2-container');
            if ($cont.length) {
                $cont.find('.select2-selection').trigger('focus');
            } else {
                el.focus();
            }
        } else {
            el.focus();
            if (withSelect === true && typeof el.select === 'function') {
                el.select();
            }
        }
    } catch (e) {
        console.error('[gdiFocusElement] ' + e.message);
    }
}

/* ============================================================
   CSRF — token antiforgery em Ajax JSON / FormData (P1-06)
   Usar com [GdiValidateAntiForgeryToken] no servidor.
   ============================================================ */
function GdiGetAntiForgeryToken(container) {
    try {
        var root = null;
        if (container) {
            if (typeof container === 'string') {
                root = document.querySelector(container);
            } else if (typeof jQuery !== 'undefined' && container instanceof jQuery && container.length) {
                root = container[0];
            } else if (container.nodeType === 1) {
                root = container;
            }
        }
        var el = root && root.querySelector
            ? root.querySelector('input[name="__RequestVerificationToken"]')
            : null;
        if (!el) {
            el = document.querySelector('input[name="__RequestVerificationToken"]');
        }
        return el && el.value ? el.value : '';
    } catch (e) {
        console.error('[GdiGetAntiForgeryToken] ' + e.message);
        return '';
    }
}

function GdiAjaxAntiForgeryHeaders(container) {
    var token = GdiGetAntiForgeryToken(container);
    if (!token) {
        return {};
    }
    return {
        'RequestVerificationToken': token,
        '__RequestVerificationToken': token
    };
}

/* ============================================================
   gdiAutoFocusModal — foco automático em modais BS5 (shown.bs.modal)
   data-autofocus="true" | "select" (foco + select())
   Sem atributo: primeiro campo focável no modal.
   ============================================================ */
(function () {
    document.addEventListener('shown.bs.modal', function (e) {
        try {
            var modal = e.target;

            var alvo = modal.querySelector('[data-autofocus]');
            if (!alvo) {
                alvo = modal.querySelector(
                    'input:not([type="hidden"]):not([disabled]):not([readonly]),' +
                    'select:not([disabled]),' +
                    'textarea:not([disabled])'
                );
            }

            if (!alvo) { return; }

            var tipo = alvo.getAttribute('data-autofocus');
            var comSelect = (tipo === 'select');
            gdiFocusElement(alvo, comSelect);
        } catch (err) {
            console.error('[gdiAutoFocusModal] ' + err.message);
        }
    });
}());

/* ============================================================
   G-PERF-20f — Lazy load de libs no #mainModal (hubs layout lite)
   GdiLoadScriptOnce / GdiMainModalLoad + patch jQuery.fn.load
   ============================================================ */
(function gdiInstallMainModalLazyScripts() {
    if (window._gdiMainModalLazyScriptsInstalled) {
        return;
    }
    window._gdiMainModalLazyScriptsInstalled = true;

    var FLAG = { core: 1, dataTables: 2, select2: 4, tempusDominus: 8, jstree: 16 };
    var _scriptState = window._gdiLoadedScriptKeys || (window._gdiLoadedScriptKeys = {});
    var _scriptWaiters = window._gdiLoadedScriptWaiters || (window._gdiLoadedScriptWaiters = {});

    function gdiNormalizeScriptKey(src) {
        return String(src || '').split('?')[0].toLowerCase();
    }

    function gdiRegistry() {
        return window.GdiPageScriptRegistry || null;
    }

    function gdiFlagNames(missing) {
        var names = [];
        if (missing & FLAG.dataTables) { names.push('DataTables'); }
        if (missing & FLAG.select2) { names.push('Select2'); }
        if (missing & FLAG.tempusDominus) { names.push('Tempus'); }
        if (missing & FLAG.jstree) { names.push('Wunderbaum'); }
        return names;
    }

    function gdiHostPageScriptFlags() {
        var body = document.body;
        if (!body) {
            return 0;
        }
        var attr = body.getAttribute('data-gdi-page-scripts');
        if (!attr) {
            return 0;
        }
        var n = parseInt(attr, 10);
        return isNaN(n) ? 0 : n;
    }

    function gdiHostPageHasFlag(flagBit) {
        return (gdiHostPageScriptFlags() & flagBit) === flagBit;
    }

    function gdiHasDataTablesRelaxed() {
        if (typeof jQuery === 'undefined' || !jQuery.fn) {
            return false;
        }
        return typeof jQuery.fn.dataTable === 'function' || typeof jQuery.fn.DataTable === 'function';
    }

    function gdiHasDataTables() {
        if (!gdiHasDataTablesRelaxed()) {
            return false;
        }
        return typeof window.DataTable !== 'undefined';
    }

    function gdiHasSelect2() {
        return typeof jQuery !== 'undefined' && jQuery.fn && typeof jQuery.fn.select2 === 'function';
    }

    function gdiHasTempus() {
        return typeof window.tempusDominus !== 'undefined' &&
            typeof jQuery !== 'undefined' && jQuery.fn && typeof jQuery.fn.tempusDominus === 'function';
    }

    function gdiRuntimeMissingFlags(needed) {
        var missing = 0;
        if ((needed & FLAG.dataTables) && !gdiHasDataTables()) {
            if (!(gdiHostPageHasFlag(FLAG.dataTables) && gdiScriptTagPresent('datatables') && gdiHasDataTablesRelaxed())) {
                missing |= FLAG.dataTables;
            }
        }
        if ((needed & FLAG.select2) && !gdiHasSelect2()) {
            if (!(gdiHostPageHasFlag(FLAG.select2) && gdiScriptTagPresent('select2') && gdiHasSelect2())) {
                missing |= FLAG.select2;
            }
        }
        if ((needed & FLAG.tempusDominus) && !gdiHasTempus()) { missing |= FLAG.tempusDominus; }
        if ((needed & FLAG.jstree) && !gdiHasWunderbaum()) { missing |= FLAG.jstree; }
        return missing;
    }

    window.GdiDetectScriptFlagsFromHtml = function (html) {
        if (!html || typeof html !== 'string') {
            return FLAG.core;
        }
        var h = html.toLowerCase();
        var flags = FLAG.core;
        if (/\.datatable\s*\(|\.datatables\s*\(|bserverside\s*:\s*true|class\s*=\s*['"][^'"]*\bdisplay\b/.test(h)) {
            flags |= FLAG.dataTables;
        }
        if (/select2|gdi-select2|data-gdi-lookup|gdiinitselect2modal/.test(h)) {
            flags |= FLAG.select2;
        }
        if (/jsdatepicker|tempus-dominus|\.tempusdominus/.test(h)) {
            flags |= FLAG.tempusDominus;
        }
        if (/GdiTreeInit|gdiHasWunderbaum|mar10\.Wunderbaum/.test(h)) {
            flags |= FLAG.jstree;
        }
        /* Toggle (switch-success) já vem no layout Core; não lazy-load — evita falso positivo no modal ANP. */
        var attrMatch = html.match(/data-gdi-require-scripts\s*=\s*['"](\d+)['"]/i);
        if (attrMatch && attrMatch[1]) {
            var extra = parseInt(attrMatch[1], 10);
            if (!isNaN(extra)) {
                flags |= extra;
            }
        }
        return flags;
    };

    window.GdiLoadStylesOnce = function (hrefs, done) {
        if (!hrefs || !hrefs.length) {
            if (typeof done === 'function') { done(); }
            return;
        }
        var pending = 0;
        var failed = false;
        function check() {
            pending--;
            if (pending <= 0 && typeof done === 'function') {
                done(failed ? new Error('Falha ao carregar CSS do modal.') : null);
            }
        }
        for (var i = 0; i < hrefs.length; i++) {
            (function (href) {
                var key = gdiNormalizeScriptKey(href);
                var exists = false;
                var links = document.getElementsByTagName('link');
                for (var j = 0; j < links.length; j++) {
                    if (gdiNormalizeScriptKey(links[j].href) === key) {
                        exists = true;
                        break;
                    }
                }
                if (exists) {
                    return;
                }
                pending++;
                var link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = href;
                link.onload = function () { check(); };
                link.onerror = function () { failed = true; check(); };
                document.head.appendChild(link);
            })(hrefs[i]);
        }
        if (pending === 0 && typeof done === 'function') {
            done();
        }
    };

    function gdiFindScriptElByKey(key) {
        var scripts = document.getElementsByTagName('script');
        for (var i = 0; i < scripts.length; i++) {
            var src = scripts[i].src || scripts[i].getAttribute('src') || '';
            if (src && gdiNormalizeScriptKey(src) === key) {
                return scripts[i];
            }
        }
        return null;
    }

    function gdiScriptTagPresent(urlPart) {
        var needle = String(urlPart || '').toLowerCase();
        if (!needle) {
            return null;
        }
        var scripts = document.getElementsByTagName('script');
        for (var i = 0; i < scripts.length; i++) {
            var src = (scripts[i].src || scripts[i].getAttribute('src') || '').toLowerCase();
            if (src.indexOf(needle) >= 0) {
                return scripts[i];
            }
        }
        return null;
    }

    function gdiFlushScriptWaiters(key, err) {
        var waiters = _scriptWaiters[key];
        if (waiters && waiters.length) {
            delete _scriptWaiters[key];
            for (var w = 0; w < waiters.length; w++) {
                if (typeof waiters[w] === 'function') {
                    waiters[w](err || null);
                }
            }
        }
    }

    function gdiBindExistingScriptEl(el, key, done) {
        function finish(err) {
            if (err) {
                _scriptState[key] = false;
            } else {
                _scriptState[key] = true;
            }
            if (typeof done === 'function') {
                done(err || null);
            }
            gdiFlushScriptWaiters(key, err || null);
        }
        if (_scriptState[key] === true) {
            finish();
            return true;
        }
        if (_scriptState[key] === 'loading') {
            if (!_scriptWaiters[key]) {
                _scriptWaiters[key] = [];
            }
            if (typeof done === 'function') {
                _scriptWaiters[key].push(done);
            }
            return true;
        }
        _scriptState[key] = 'loading';
        if (el.readyState === 'complete' || el.readyState === 'loaded') {
            finish();
            return true;
        }
        el.addEventListener('load', function () { finish(); }, { once: true });
        el.addEventListener('error', function () {
            finish(new Error('Falha ao carregar script: ' + (el.src || key)));
        }, { once: true });
        if (typeof done === 'function') {
            if (!_scriptWaiters[key]) {
                _scriptWaiters[key] = [];
            }
            _scriptWaiters[key].push(done);
        }
        return true;
    }

    function gdiWaitForLibrary(isReady, done, attempt, maxAttempts) {
        attempt = attempt || 0;
        maxAttempts = maxAttempts || 80;
        if (isReady()) {
            if (typeof done === 'function') {
                done();
            }
            return;
        }
        if (attempt >= maxAttempts) {
            if (typeof done === 'function') {
                done(new Error('Timeout aguardando biblioteca do modal.'));
            }
            return;
        }
        setTimeout(function () {
            gdiWaitForLibrary(isReady, done, attempt + 1, maxAttempts);
        }, 50);
    }

    /** Libera overlay processando (contador waitingDialog pode estar > 1). */
    function gdiMainModalReleaseProcessando() {
        if (typeof LibMessageProcessandoHide !== 'function') {
            return;
        }
        try { LibMessageProcessandoHide(); } catch (e1) { }
        try { LibMessageProcessandoHide(); } catch (e2) { }
    }

    /** Evita lazy-load duplicado quando a view host já incluiu libs defer (DT, Select2, Tempus). */
    function gdiEnsureScriptFlagsForModal(needed, done) {
        function finish(err) {
            if (typeof done === 'function') {
                done(err || null);
            }
        }

        function ensureRemaining(flagsNeeded) {
            var still = gdiRuntimeMissingFlags(flagsNeeded);
            if (!still) {
                finish();
                return;
            }
            window.GdiEnsureScriptFlags(still, finish);
        }

        function waitHostLib(isReady, onDone) {
            gdiWaitForLibrary(isReady, function (waitErr) {
                if (!waitErr && !gdiRuntimeMissingFlags(needed)) {
                    finish();
                    return;
                }
                onDone(waitErr);
            });
        }

        var missing = gdiRuntimeMissingFlags(needed);
        if (!missing) {
            finish();
            return;
        }

        if ((missing & FLAG.dataTables) && gdiScriptTagPresent('datatables') && !gdiHasDataTables()) {
            waitHostLib(function () {
                return gdiHasDataTables() || (gdiHostPageHasFlag(FLAG.dataTables) && gdiHasDataTablesRelaxed());
            }, function (waitErr) {
                if (waitErr && !gdiHasDataTablesRelaxed()) {
                    finish(waitErr);
                    return;
                }
                ensureRemaining(needed);
            });
            return;
        }

        if ((missing & FLAG.select2) && gdiScriptTagPresent('select2') && !gdiHasSelect2()) {
            waitHostLib(gdiHasSelect2, function (waitErr) {
                if (waitErr && !gdiHasSelect2()) {
                    finish(waitErr);
                    return;
                }
                ensureRemaining(needed);
            });
            return;
        }

        if ((missing & FLAG.tempusDominus) && gdiScriptTagPresent('tempus-dominus')) {
            waitHostLib(gdiHasTempus, function (waitErr) {
                if (waitErr) {
                    ensureRemaining(missing);
                    return;
                }
                ensureRemaining(needed);
            });
            return;
        }

        window.GdiEnsureScriptFlags(missing, finish);
    }

    window.GdiLoadScriptOnce = function (src, done) {
        var key = gdiNormalizeScriptKey(src);
        if (_scriptState[key] === true) {
            if (typeof done === 'function') { done(); }
            return;
        }
        if (_scriptState[key] === 'loading') {
            if (!_scriptWaiters[key]) {
                _scriptWaiters[key] = [];
            }
            _scriptWaiters[key].push(done);
            return;
        }
        var existingEl = gdiFindScriptElByKey(key);
        if (existingEl && gdiBindExistingScriptEl(existingEl, key, done)) {
            return;
        }
        _scriptState[key] = 'loading';
        var script = document.createElement('script');
        script.src = src;
        script.async = false;
        script.onload = function () {
            _scriptState[key] = true;
            if (typeof done === 'function') { done(); }
            var waiters = _scriptWaiters[key];
            if (waiters && waiters.length) {
                delete _scriptWaiters[key];
                for (var w = 0; w < waiters.length; w++) {
                    if (typeof waiters[w] === 'function') { waiters[w](); }
                }
            }
        };
        script.onerror = function () {
            _scriptState[key] = false;
            var err = new Error('Falha ao carregar script: ' + src);
            if (typeof done === 'function') { done(err); }
            var waiters = _scriptWaiters[key];
            if (waiters && waiters.length) {
                delete _scriptWaiters[key];
                for (var w2 = 0; w2 < waiters.length; w2++) {
                    if (typeof waiters[w2] === 'function') { waiters[w2](err); }
                }
            }
        };
        document.head.appendChild(script);
    };

    function gdiLoadScriptsSequential(urls, done) {
        var idx = 0;
        function next(err) {
            if (err) {
                if (typeof done === 'function') { done(err); }
                return;
            }
            if (idx >= urls.length) {
                if (typeof done === 'function') { done(); }
                return;
            }
            window.GdiLoadScriptOnce(urls[idx++], next);
        }
        next();
    }

    window.GdiEnsureScriptFlags = function (missing, done) {
        if (!missing) {
            if (typeof done === 'function') { done(); }
            return;
        }
        var reg = gdiRegistry();
        if (!reg || !reg.bundles) {
            if (typeof done === 'function') {
                done(new Error('GdiPageScriptRegistry não definido no layout.'));
            }
            return;
        }
        var css = [];
        var js = [];
        function appendBundle(name, flagBit) {
            if (!(missing & flagBit)) {
                return;
            }
            var bundle = reg.bundles[name];
            if (!bundle) {
                return;
            }
            if (bundle.css) {
                css = css.concat(bundle.css);
            }
            if (bundle.js) {
                js = js.concat(bundle.js);
            }
        }
        appendBundle('dataTables', FLAG.dataTables);
        appendBundle('select2', FLAG.select2);
        appendBundle('tempusDominus', FLAG.tempusDominus);
        appendBundle('jstree', FLAG.jstree);

        window.GdiLoadStylesOnce(css, function (cssErr) {
            if (cssErr) {
                if (typeof done === 'function') { done(cssErr); }
                return;
            }
            gdiLoadScriptsSequential(js, function (jsErr) {
                if (jsErr) {
                    if (typeof done === 'function') { done(jsErr); }
                    return;
                }
                var still = gdiRuntimeMissingFlags(missing);
                if (still && typeof done === 'function') {
                    done(new Error('Bibliotecas não disponíveis após carregamento: ' + gdiFlagNames(still).join(', ')));
                    return;
                }
                if (typeof done === 'function') { done(); }
            });
        });
    };

    /**
     * Respostas Ajax com layout _Modal.cshtml (DOCTYPE/html/body): extrai só o corpo,
     * como o jQuery.fn.load legado (.children() após parse), para #mainModal receber
     * .modal-dialog como filho direto.
     */
    function gdiNormalizeModalAjaxHtml(html) {
        if (!html || typeof html !== 'string') {
            return html;
        }
        var trimmed = jQuery.trim(html);
        if (!/<\s*!doctype/i.test(trimmed) && !/<\s*html[\s>]/i.test(trimmed)) {
            return trimmed;
        }
        if (typeof DOMParser !== 'undefined') {
            try {
                var doc = new DOMParser().parseFromString(trimmed, 'text/html');
                if (doc && doc.body && jQuery.trim(doc.body.innerHTML || '').length) {
                    return doc.body.innerHTML;
                }
            } catch (eDom) { /* fallback jQuery */ }
        }
        var $wrap = jQuery('<div>').append(jQuery.parseHTML(trimmed, document, true));
        var $body = $wrap.find('body').first();
        if ($body.length) {
            return $body.html() || '';
        }
        var $htmlRoot = $wrap.children('html').first();
        if ($htmlRoot.length) {
            var $bodyInHtml = $htmlRoot.find('body').first();
            if ($bodyInHtml.length) {
                return $bodyInHtml.html() || '';
            }
        }
        return $wrap.children().map(function () {
            var el = this;
            return el.outerHTML || el.textContent || '';
        }).get().join('');
    }

    /** Exibe #mainModal (BS5); usar no callback de $("#mainModal").load / GdiMainModalLoad. */
    window.GdiMainModalShow = function () {
        var el = document.getElementById('mainModal');
        if (!el) {
            return;
        }
        if (typeof bootstrap === 'undefined' || !bootstrap.Modal) {
            if (typeof LibMessageError === 'function') {
                LibMessageError('Atenção', '[GdiMainModalShow] Bootstrap 5 JS não carregado nesta página.');
            }
            return;
        }
        var inst = bootstrap.Modal.getInstance(el);
        if (inst) {
            inst.dispose();
        }
        bootstrap.Modal.getOrCreateInstance(el).show();
    };

    /** Injeta HTML no #mainModal e executa <script> inline (paridade jQuery.fn.load). */
    function gdiMainModalInsertHtml($modal, html) {
        $modal.empty();
        if (!html) {
            return;
        }
        html = gdiNormalizeModalAjaxHtml(html);
        if (!html) {
            return;
        }
        var nodes = jQuery.parseHTML(html, document, true);
        if (!nodes || !nodes.length) {
            return;
        }
        var i;
        for (i = 0; i < nodes.length; i++) {
            $modal[0].appendChild(nodes[i]);
        }
        $modal.find('script').each(function () {
            var el = this;
            var src = el.src || el.getAttribute('src');
            if (src) {
                jQuery.ajax({ url: src, dataType: 'script', async: false, cache: true });
            } else {
                var code = el.text || el.textContent || '';
                if (code && jQuery.trim(code).length && jQuery.globalEval) {
                    jQuery.globalEval(code);
                }
            }
        });
    }

    window.GdiMainModalLoad = function (url, data, complete) {
        var $modal = jQuery('#mainModal');
        if (!$modal.length) {
            return jQuery('<div/>');
        }
        var ajaxOpts = {
            url: url,
            type: 'GET',
            dataType: 'text',
            cache: false,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        };
        if (data !== undefined && data !== null) {
            ajaxOpts.data = data;
            if (typeof data === 'object' || typeof data === 'string') {
                ajaxOpts.type = 'POST';
            }
        }
        jQuery.ajax(ajaxOpts)
            .done(function (html) {
                var bodyHtml = gdiNormalizeModalAjaxHtml(html);
                var needed = window.GdiDetectScriptFlagsFromHtml(bodyHtml || html);
                gdiEnsureScriptFlagsForModal(needed, function (err) {
                    if (err) {
                        gdiMainModalReleaseProcessando();
                        if (typeof LibMessageError === 'function') {
                            LibMessageError('Atenção', '[GdiMainModalLoad] ' + (err.message || String(err)));
                        }
                        return;
                    }
                    gdiMainModalInsertHtml($modal, html);
                    if (typeof complete === 'function') {
                        complete.call($modal[0], html, 'success', null);
                    } else if (typeof window.GdiMainModalShow === 'function') {
                        window.GdiMainModalShow();
                    }
                });
            })
            .fail(function (xhr, status, err) {
                gdiMainModalReleaseProcessando();
                if (typeof LibMessageError === 'function') {
                    var urlHint = ajaxOpts.url || '';
                    var httpInfo = (xhr && xhr.status) ? ('HTTP ' + xhr.status) : (status || 'error');
                    var detail = err || (xhr && xhr.statusText) || status || 'Erro ao carregar modal.';
                    LibMessageError('Atenção', '[GdiMainModalLoad] ' + httpInfo + (urlHint ? ' — ' + urlHint : '') + ' — ' + detail);
                }
            });
        return $modal;
    };

    if (typeof jQuery !== 'undefined' && jQuery.fn && jQuery.fn.load && !jQuery.fn._gdiMainModalLoadPatched) {
        var _gdiOrigFnLoad = jQuery.fn.load;
        jQuery.fn.load = function (url, param2, param3) {
            if (this.length === 1 && this[0] && this[0].id === 'mainModal' && typeof url === 'string') {
                var data;
                var complete;
                if (typeof param2 === 'function') {
                    complete = param2;
                } else {
                    data = param2;
                    complete = param3;
                }
                return window.GdiMainModalLoad(url, data, complete);
            }
            return _gdiOrigFnLoad.apply(this, arguments);
        };
        jQuery.fn._gdiMainModalLoadPatched = true;
    }
}());

/* ============================================================
   gdiAplicarFoco — páginas / callbacks (ex.: footerCallback DataTables)
   gdiAplicarFoco('#id', true) → foco + select()
   ============================================================ */
function gdiAplicarFoco(seletor, comSelect) {
    try {
        function focar() {
            if (typeof gdiIsSelect2Interacting === 'function' && gdiIsSelect2Interacting()) {
                return;
            }
            var el = typeof seletor === 'string'
                ? document.querySelector(seletor)
                : seletor;
            gdiFocusElement(el, comSelect === true);
        }

        if (typeof jQuery !== 'undefined' && jQuery.active > 0) {
            jQuery(document).one('ajaxStop', focar);
        } else {
            focar();
        }
    } catch (err) {
        console.error('[gdiAplicarFoco] ' + err.message);
    }
}

/* ============================================================
   Páginas normais: [data-autofocus] após jsInitForm/Select2 (defer 0)
   ou ajaxStop se houver Ajax pendente.
   ============================================================ */
$(document).ready(function () {
    try {
        if (!document.querySelector('[data-autofocus]')) { return; }

        function focarPagina() {
            var cur = document.querySelector('[data-autofocus]');
            if (!cur) { return; }
            var tipo = cur.getAttribute('data-autofocus');
            gdiFocusElement(cur, tipo === 'select');
        }

        if (typeof jQuery !== 'undefined' && jQuery.active > 0) {
            $(document).one('ajaxStop', focarPagina);
        } else {
            setTimeout(focarPagina, 0);
        }
    } catch (err) {
        console.error('[gdiAutoFocusPagina] ' + err.message);
    }
});
