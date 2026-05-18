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

function btnFiltro(yesFilterOnOff) {
    try { document.getElementById("btnFiltroDefault").innerHTML = document.getElementById("btnFiltroDefault").innerHTML.replace("Remover Filtro", "Sync...").replace("Filtro", "Sync...").replace("fa-solid fa-filter", "fa-solid fa-sync").replace("fa-solid fa-magnifying-glass-minus", "fa-solid fa-sync"); } catch (e) { };
    try { document.getElementById("btnFiltroAvancado").innerHTML = document.getElementById("btnFiltroAvancado").innerHTML.replace("Remover Filtro", "Sync...").replace("Filtro Avançado", "Sync...").replace("fa-solid fa-filter", "fa-solid fa-sync").replace("fa-solid fa-magnifying-glass-minus", "fa-solid fa-sync"); } catch (e) { };

    if (yesFilterOnOff == "0") {
        try { document.getElementById("btnFiltroDefault").innerHTML = document.getElementById("btnFiltroDefault").innerHTML.replace("Sync...", "Filtro").replace("fa-solid fa-sync", "fa-solid fa-filter"); } catch (e) { };
        try { document.getElementById("btnFiltroAvancado").innerHTML = document.getElementById("btnFiltroAvancado").innerHTML.replace("Sync...", "Filtro Avançado").replace("fa-solid fa-sync", "fa-solid fa-filter"); } catch (e) { };
    }
    else {
        try { document.getElementById("btnFiltroDefault").innerHTML = document.getElementById("btnFiltroDefault").innerHTML.replace("Sync...", "Remover Filtro").replace("fa-solid fa-sync", "fa-solid fa-magnifying-glass-minus"); } catch (e) { };
        try { document.getElementById("btnFiltroAvancado").innerHTML = document.getElementById("btnFiltroAvancado").innerHTML.replace("Sync...", "Remover Filtro").replace("fa-solid fa-sync", "fa-solid fa-magnifying-glass-minus"); } catch (e) { };
    }
    LibMessageHideAll();
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


function jsYesEditRecordJsTree(JsTreeName, urlEdit) {
    var selectedIds = JsTreeName.jstree('get_selected');
    if (!selectedIds || selectedIds.length == 0 || selectedIds.toString() == "-1") {
        LibMessageAlert("Atenção", "Selecione o item");
    }
    else {
        if (selectedIds.toString().indexOf('R') >= 0) {
            LibMessageAlert("Atenção", "Selecione o item permitido");
        }
        else if (selectedIds.toString().indexOf(',') == -1) {
            $(window.document.location).attr('href', urlEdit + selectedIds);
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
