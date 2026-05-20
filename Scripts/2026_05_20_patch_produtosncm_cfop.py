# -*- coding: utf-8 -*-
from pathlib import Path
ROOT = Path(r"c:\Marcio\Projetos\GDI-ERP-Plataform")

def patch_ncm():
    src = ROOT / "Areas/g/Views/PagRecTipos/Index.cshtml"
    dst = ROOT / "Areas/g/Views/ProdutosNcm/Index.cshtml"
    t = src.read_text(encoding="utf-8")
    pairs = [
        ("CstPagRecTiposIndex", "CstProdutosNcmIndex"),
        ("g_PagRecTipos", "g_ProdutosNcm"),
        ("PagRecTipos", "ProdutosNcm"),
        ("pagrectipos", "produtosncm"),
        ("GPagRecTipos", "GProdutosNcm"),
        ("PagRecTiposIndex_id", "ProdutosNcmIndex_id"),
        ("PagRecTiposIndex_descricao", "ProdutosNcmIndex_codigo_ncm"),
        ("edit_id_pagrec_tipo", "edit_id_produto_ncm"),
        ("edit_descricao_pagrec_tipo", "edit_codigo_ncm"),
        ("keypressedPagRecTipos", "keypressedProdutosNcm"),
        ("jsPagRecTipos", "jsProdutosNcm"),
        ("Pesquisar tipos", "Pesquisar NCM"),
        ("todos os tipos", "todos os NCM"),
        ("Pesquisando tipos...", "Pesquisando NCM..."),
        ("Editar tipo", "Editar NCM"),
        ('@Html.Label("Descrição"', '@Html.Label("Código NCM"'),
        ('placeholder = "Descrição"', '@placeholder = "Código NCM"'),
        ("(Id. ou Descrição)", "(Id. ou Código NCM)"),
        ("Informe Id. ou Descrição", "Informe Id. ou Código NCM"),
    ]
    for a, b in pairs:
        t = t.replace(a, b)
    excluir = """                @if ((CachePersister.userIdentity.Roles.Contains("g_ProdutosNcm_*")) || (CachePersister.userIdentity.Roles.Contains("g_ProdutosNcm_Actiondelete")))
                {<button type="button" name="btnRegistroExcluir" title="Excluir Registro" class="btn btn-outline-danger disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button> }
                else
                { <button type="button" name="btnRegistroExcluir" title="Excluir Registro - Não Liberado" class="btn btn-outline-secondary disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button>}
                <div class="btn-group" role="group">
                    <button id="btnGroupDrop1" type="button" class="btn btn-outline-info dropdown-toggle d-inline-flex align-items-center gap-2" data-bs-toggle="dropdown" aria-haspopup="true" aria-expanded="false" title="Processos">
                        <i class="fa-solid fa-cog me-2" aria-hidden="true"></i>Processos
                    </button>
                    <motion class="dropdown-menu" aria-labelledby="btnGroupDrop1">
                        <a class="d-flex align-items-center dropdown-item" href="#" onclick="jsModalAtualizarTabelaIBPT()"><i class="fa-solid fa-upload me-2" aria-hidden="true"></i>Atualizar Tabela IPBTax</a>
                    </motion>
                </motion>
"""
    excluir = excluir.replace("<" + "motion", "<" + "div").replace("</" + "motion>", "</" + "motion>")
    excluir = excluir.replace("</" + "motion>", "</" + "div>")
    idx = t.find('                { <button type="button" name="btnRegistroNovo" title="Novo Registro - Não Liberado"')
    if idx >= 0:
        pos = t.find("\n            </" + "motion>", t.find("Novo</button> }", idx))
        if pos < 0:
            pos = t.find("\n            </" + "div>", t.find("Novo</button> }", idx))
        t = t[:pos] + "\n" + excluir + t[pos:]
    thead_old = """                                    <th>Ativo</th>
                                    <th>Descrição</th>
                                    <th>Pagamento</th>
                                    <th>Recebimento</th>
                                    <th>Baixa Autómatica</th>"""
    thead_new = """                                    <th>Ativo</th>
                                    <th>Codigo NCM</th>"""
    t = t.replace(thead_old, thead_new)
    col_old = """                { "width": "8%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "34%", orderable: true, targets: [3] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [4] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [5] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [6] }
                @if (podeEditarProdutosNcm)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [7],"""
    col_new = """                { "width": "8%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "78%", orderable: true, targets: [3] }
                @if (podeEditarProdutosNcm)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [4],"""
    t = t.replace(col_old, col_new)
    ibpt = """
    function jsModalAtualizarTabelaIBPT() {
        $("#mainModal").load('@Url.Action("ModalAtualizarTabelaIBPT", "ProdutosNcm", new { Area = "g" })', function () {
            var e=document.getElementById('mainModal');if(e){bootstrap.Modal.getOrCreateInstance(e).show();}
        })
    }
"""
    t = t[: t.rfind("</script>")] + ibpt + "\n</script>\n"
    dst.write_text(t, encoding="utf-8")
    print("ncm ok")

def patch_cfop():
    src = ROOT / "Areas/g/Views/PagRecTipos/Index.cshtml"
    dst = ROOT / "Areas/gc/Views/Cfop/Index.cshtml"
    t = src.read_text(encoding="utf-8")
    pairs = [
        ("CstPagRecTiposIndex", "CstCfopIndex"),
        ("g_PagRecTipos", "gc_Cfop"),
        ("PagRecTipos", "Cfop"),
        ("pagrectipos", "cfcfop"),
        ("GPagRecTipos", "GcCfop"),
        ("PagRecTiposIndex_id", "CfopIndex_id"),
        ("PagRecTiposIndex_descricao", "CfopIndex_descricao"),
        ("edit_id_pagrec_tipo", "edit_id_cfop"),
        ("edit_descricao_pagrec_tipo", "edit_descricao_cfop"),
        ("keypressedPagRecTipos", "keypressedCfop"),
        ("jsPagRecTipos", "jsGcCfop"),
        ('new { Area = "g" }', 'new { Area = "gc" }'),
        ("Pesquisar tipos", "Pesquisar CFOP"),
        ("todos os tipos", "todos os CFOP"),
        ("Pesquisando tipos...", "Pesquisando CFOP..."),
        ("Editar tipo", "Editar CFOP"),
        ("(Id. ou Descrição)", "(Id. ou Descrição)"),
    ]
    for a, b in pairs:
        t = t.replace(a, b)
    t = t.replace("gdi-index-cfcfop-dt", "gdi-index-cfop-dt")
    thead_old = """                                    <th>Ativo</th>
                                    <th>Descrição</th>
                                    <th>Pagamento</th>
                                    <th>Recebimento</th>
                                    <th>Baixa Autómatica</th>"""
    thead_new = """                                    <th>Ativo</th>
                                    <th>Número</th>
                                    <th>Descrição</th>
                                    <th>Conta Contabil</th>
                                    <th>Código Contábil</th>"""
    t = t.replace(thead_old, thead_new)
    col_old = """                { "width": "8%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "34%", orderable: true, targets: [3] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [4] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [5] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [6] }
                @if (podeEditarCfop)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [7],"""
    col_new = """                { "width": "4%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [3] },
                { "width": "58%", orderable: false, className: 'text-sm-start', targets: [4] },
                { "width": "10%", orderable: false, className: 'text-sm-end', targets: [5] },
                { "width": "10%", orderable: false, className: 'text-sm-end', targets: [6] }
                @if (podeEditarCfop)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [7],"""
    t = t.replace(col_old, col_new)
    t = t.replace("var otableGcCfop;", "var otableGcCfop;")
    dst.write_text(t, encoding="utf-8")
    print("cfop ok")

if __name__ == "__main__":
    patch_ncm()
    patch_cfop()
