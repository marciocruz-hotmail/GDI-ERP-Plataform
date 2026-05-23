/**

 * GDI — idioma PT-BR global do DataTables 2.x + deferLoading (filtro obrigatório).

 * Carregar imediatamente após datatables.min.js.

 *

 * IMPORTANTE: $.fn.dataTable retorna jQuery; $.fn.DataTable (D maiúsculo) retorna a Api — nao substituir pelo mesmo wrapper.

 */

(function (window) {

    'use strict';

    if (typeof window.DataTable === 'undefined') {

        return;

    }

    var $ = window.jQuery;

    if (!$ || !$.extend) {

        return;

    }



    var gdiPtBr = {

        sEmptyTable: 'Nenhum registro encontrado',

        sZeroRecords: 'Nenhum registro encontrado',

        sAwaitingFilter: 'Informe os dados do filtro e clique em Pesquisar.',

        sInfo: 'Mostrando de _START_ ate _END_   [ _TOTAL_ registros total ]',

        sInfoEmpty: 'Mostrando 0 ate 0 de 0 registros',

        sInfoFiltered: ' (Filtrados de _MAX_ registros)',

        sInfoPostFix: '',

        sInfoThousands: '.',

        sDecimal: ',',

        sThousands: '.',

        sLengthMenu: '_MENU_ registros por pagina',

        sLoadingRecords: 'Carregando...',

        sProcessing: 'Processando...',

        sSearch: 'Pesquisar',

        sSearchPlaceholder: 'Pesquisar na tabela',

        oPaginate: {

            sFirst: '<<',

            sPrevious: '<',

            sNext: '>',

            sLast: '>>'

        },

        oAria: {

            sSortAscending: ': Ordenar colunas de forma ascendente',

            sSortDescending: ': Ordenar colunas de forma descendente',

            paginate: {

                first: 'Primeira',

                previous: 'Anterior',

                next: 'Proxima',

                last: 'Ultima'

            }

        },

        select: {

            rows: {

                0: '',

                1: ' [ %d registro selecionado ] ',

                _: ' [ %d registros selecionados ] '

            },

            aria: {

                rowCheckbox: 'Selecionar linha'

            }

        },

        lengthLabels: {

            '-1': 'Todos'

        }

    };



    $.extend(true, window.DataTable.defaults.oLanguage, gdiPtBr);

    window.GdiDataTablesPtBr = gdiPtBr;



    function gdiIsDeferLoading(opts) {

        if (!opts || typeof opts !== 'object' || $.isArray(opts)) {

            return false;

        }

        var d = opts.deferLoading;

        return d === true || d === 0 || (Array.isArray(d) && d.length > 0);

    }



    function gdiNormalizeAwaitingFilterOptions(options) {

        if (!gdiIsDeferLoading(options)) {

            return options;

        }

        options = $.extend(true, {}, options);

        var lang = $.extend(true, {}, options.language || {});

        var awaitingMsg = lang.gdiAwaitingFilter

            || lang.sEmptyTable

            || gdiPtBr.sAwaitingFilter;



        if (!lang.sLoadingRecords || lang.sLoadingRecords === gdiPtBr.sLoadingRecords) {

            lang.sLoadingRecords = awaitingMsg;

        }

        if (!lang.sZeroRecords) {

            lang.sZeroRecords = gdiPtBr.sZeroRecords;

        }

        delete lang.gdiAwaitingFilter;

        options.language = lang;



        var userInitComplete = options.initComplete;

        options.initComplete = function (settings, json) {

            var api = this.api ? this.api() : new $.fn.dataTable.Api(settings);

            api.one('xhr.dt', function () {

                if (settings.oLanguage) {

                    settings.oLanguage.sEmptyTable = '';

                }

                try {

                    if (!settings.aiDisplay || settings.aiDisplay.length === 0) {

                        api.draw(false);

                    }

                } catch (ignore) { }

            });

            if (typeof userInitComplete === 'function') {

                userInitComplete.call(this, settings, json);

            }

        };

        return options;

    }



    var originalDataTable = $.fn.dataTable;

    var originalDataTableCapital = $.fn.DataTable;

    if (typeof originalDataTable !== 'function') {

        return;

    }



    function gdiDataTableWrap() {

        if (arguments.length > 0 && arguments[0] !== null && typeof arguments[0] === 'object' && !$.isArray(arguments[0])) {

            arguments[0] = gdiNormalizeAwaitingFilterOptions(arguments[0]);

        }

        return originalDataTable.apply(this, arguments);

    }

    $.extend(gdiDataTableWrap, originalDataTable);

    $.fn.dataTable = gdiDataTableWrap;



    /** Preserva contrato oficial: Api com .draw(), nao jQuery. */

    $.fn.DataTable = function (opts) {

        return $(this).dataTable(opts).api();

    };

    $.each(originalDataTable, function (prop, val) {

        if ($.fn.DataTable[prop] === undefined) {

            $.fn.DataTable[prop] = val;

        }

    });

    if (originalDataTableCapital && typeof originalDataTableCapital === 'function') {

        $.each(originalDataTableCapital, function (prop, val) {

            if ($.fn.DataTable[prop] === undefined) {

                $.fn.DataTable[prop] = val;

            }

        });

    }



    /**

     * Api DataTables de um selector (uso em Pesquisar/Limpar e variaveis otable*).

     * @param {string|jQuery} selector

     * @returns {DataTables.Api|null}

     */

    window.GdiDataTableApi = function (selector) {

        var $el = selector && selector.jquery ? selector : $(selector);

        if (!$el || !$el.length) {

            return null;

        }

        if (!$.fn.DataTable.isDataTable($el)) {

            return null;

        }

        return $el.DataTable();

    };



    /**

     * Redesenha grelha server-side (ex.: apos Limpar filtro yesFilterField = *).

     * @param {string|jQuery} selector

     * @param {boolean} [resetPaging=false]

     * @returns {boolean}

     */

    window.GdiDataTableDraw = function (selector, resetPaging) {

        var api = window.GdiDataTableApi(selector);

        if (!api || typeof api.draw !== 'function') {

            return false;

        }

        api.draw(resetPaging === true);

        return true;

    };

})(typeof window !== 'undefined' ? window : this);


