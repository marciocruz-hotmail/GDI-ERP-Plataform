# -*- coding: utf-8 -*-
from pathlib import Path
p = Path(r"c:\Marcio\Projetos\GDI-ERP-Plataform\Areas\g\Views\ContratosAviacao\Index.cshtml")
lines = p.read_text(encoding="utf-8").splitlines(keepends=True)
# remove lines 52-67 (1-based) => index 51-66
out = lines[:51] + lines[67:]
text = "".join(out)
text = text.replace(
    '<motion class="card-body m-0 p-1" style="max-width: 100%; overflow-x: auto; ">',
    '<div class="card-body m-0 p-1 gdi-index-contratos-dt" style="max-width: 100%; overflow-x: auto;">',
)
text = text.replace(
    '<div class="card-body m-0 p-1" style="max-width: 100%; overflow-x: auto; ">',
    '<motion class="card-body m-0 p-1 gdi-index-contratos-dt" style="max-width: 100%; overflow-x: auto;">',
)
bad = "m" + "otion"
text = text.replace("<" + bad, "<motion").replace("</" + bad + ">", "</motion>")
text = text.replace("<motion", "<div").replace("</motion>", "</div>")
# fix download render - add edit col 8
old_col = """                { "width": "5%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [7], data: null, defaultContent: "", render: function (data, type, row, meta) { return '<button type="button" name="btnDownloadFile" data-bs-toggle="tooltip" data-bs-placement="top" title="Download Arquivo Id: ' + data[1] + '" class="btn btn-outline-info btn-sm p-1 btnDownloadFile d-inline-flex align-items-center" onclick="jsDownloadFileContrato(' + data[1] + ');"><i class="fa-solid fa-cloud-download-alt"></i></button>' } }
            ],"""
new_col = """                { "width": "5%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [7], data: null, defaultContent: "", render: function (data, type, row, meta) { return '<button type="button" name="btnDownloadFile" data-bs-toggle="tooltip" data-bs-placement="top" title="Download Arquivo Id: ' + row[1] + '" class="btn btn-outline-info btn-sm p-1 btnDownloadFile d-inline-flex align-items-center" onclick="jsDownloadFileContrato(' + row[1] + ');"><i class="fa-solid fa-cloud-download-alt"></i></button>' } }
                @if (podeEditarContrato)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [8], data: null, defaultContent: "",
                    render: function (data, type, row) {
                        return '<button type="button" title="Editar contrato Id. ' + row[1] + '" class="btn btn-outline-warning btn-sm p-1 btnGContratosEditRow d-inline-flex align-items-center" onclick="jsGContratosEditRow(' + row[1] + ');"><i class="fa-regular fa-edit"></i></button>';
                    }
                }</text>
                }
            ],"""
if old_col in text:
    text = text.replace(old_col, new_col)
# inject filter JS if missing
if "function jsAjaxPesquisarContratos" not in text:
    inject = '''
    function keypressedContratos(e) { if (e.keyCode == 13) { e.preventDefault(); jsAjaxPesquisarContratos(); } }
    function jsContratosTemCriterioFiltro() { return !isEmpty($('#edit_id_contrato').val()) || !isEmpty($('#edit_descricao_contrato').val()); }
    function jsAtualizarIndicadorFiltroContratos(yesFilterOnOff) {
        try {
            var btn = document.getElementById('btnLimparFiltroContratos');
            if (!btn) return;
            var ativo = (yesFilterOnOff === 1 || yesFilterOnOff === '1');
            btn.classList.remove('btn-outline-secondary', 'btn-outline-warning');
            btn.classList.add(ativo ? 'btn-outline-warning' : 'btn-outline-secondary');
        } catch (err) { }
    }
    function jsAjaxPesquisarContratos() {
        if (!jsContratosTemCriterioFiltro()) { LibMessageAlert("Atenção", "Preencha ao menos um campo do <b>filtro</b> (Id. ou Descrição)."); return; }
        LibMessageProcessando('Pesquisando contratos...');
        document.getElementById("yesFilterField").value = "";
        $("#dtGContratos").DataTable().draw(false);
    }
    function jsLimparFiltroContratos() {
        $('#edit_id_contrato').val(''); $('#edit_descricao_contrato').val('');
        document.getElementById("yesFilterField").value = "*";
        LibMessageProcessando('Atualizando lista...');
        $("#dtGContratos").DataTable().draw(false);
    }
    function jsGContratosEditRow(idContrato) {
        var id = parseInt(idContrato, 10);
        if (!id || id <= 0) return;
        JsEditRecordDoubleClick(id, '@Url.Action("Edit", "ContratosAviacao", new { Area = "g" })');
    }
'''
    text = text.replace("    function jsCreateTable()", inject + "\n    function jsCreateTable()")
text = text.replace("btnFiltro(json.yesFilterOnOff)", "json.yesFilterOnOff !== undefined) { jsAtualizarIndicadorFiltroContratos(json.yesFilterOnOff); }")
text = text.replace("yesFilterOperador: $('#yesFilterOperador').val().toString(),\n                        yesFilterText: $('#yesFilterText').val().toString(),\n                        yesFilterAdvancedText: $('#yesFilterAdvancedText').val().toString(),",
                    "yesFilterController: $('#yesFilterController').val().toString(),\n                        yesCustomField01: emptyIfNull($('#edit_id_contrato').val()).toString(),\n                        yesCustomField02: emptyIfNull($('#edit_descricao_contrato').val()).toString()")
text = text.replace("processing: true,\n            bServerSide: true,", "deferLoading: true,\n            processing: true,\n            bServerSide: true,")
text = text.replace("jsCreateTable();\n            getValidationSummary", "jsCreateTable();\n            jsAtualizarIndicadorFiltroContratos('0');\n            gdiAplicarFoco('#edit_id_contrato', true);\n            getValidationSummary")
text = text.replace("$('#dtGContratos tbody').on('dblclick', 'tr td', function () {\n                var data = otableGContratos.row($(this).closest('tr')).data();\n                JsEditRecordDoubleClick(data[1],'@Url.Action(\"Edit\", \"ContratosAviacao\", new { Area = \"g\" })');\n            });",
                    "$('#dtGContratos tbody').on('dblclick', 'tr td:not(.dt-no-row-select)', function () {\n                var data = otableGContratos.row($(this).closest('tr')).data();\n                if (data) { jsGContratosEditRow(data[1]); }\n            });")
# remove filtro avancado functions
import re
text = re.sub(r"\n    function jsModalFiltroAvancado\(\)[\s\S]*?function jsRunFiltroAvancado[\s\S]*?\n    \}\n", "\n", text)
p.write_text(text, encoding="utf-8")
print("contratos patched")
