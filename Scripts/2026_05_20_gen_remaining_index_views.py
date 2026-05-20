# -*- coding: utf-8 -*-
"""Generate remaining modernized Index.cshtml views."""
from pathlib import Path

ROOT = Path(r"c:\Marcio\Projetos\GDI-ERP-Plataform")

def read_tpl():
    return (ROOT / "Areas/g/Views/PagRecTipos/Index.cshtml").read_text(encoding="utf-8")

def write_view(rel_path, content):
    p = ROOT / rel_path
    p.write_text(content, encoding="utf-8")
    print("wrote", rel_path)

def mk_vendedores():
    t = read_tpl()
    rep = [
        ("CstPagRecTiposIndex", "CstVendedoresIndex"),
        ("PagRecTipos", "Vendedores"),
        ("pagrectipos", "vendedores"),
        ("GPagRecTipos", "GVendedores"),
        ("PagRecTiposIndex_id", "VendedoresIndex_id"),
        ("PagRecTiposIndex_descricao", "VendedoresIndex_nome"),
        ("edit_id_pagrec_tipo", "edit_id_vendedor"),
        ("edit_descricao_pagrec_tipo", "edit_nome_vendedor"),
        ("keypressedPagRecTipos", "keypressedVendedores"),
        ("jsPagRecTipos", "jsVendedores"),
        ("PagRecTipos", "Vendedores"),
        ("Pesquisar tipos", "Pesquisar vendedores"),
        ("todos os tipos", "todos os vendedores"),
        ("Pesquisando tipos...", "Pesquisando vendedores..."),
        ("Editar tipo", "Editar vendedor"),
        ('@Html.Label("Descrição"', '@Html.Label("Nome"'),
        ('@placeholder = "Descrição"', '@placeholder = "Nome do vendedor"'),
        ("(Id. ou Descrição)", "(Id. ou Nome)"),
        ("Informe Id. ou Descrição", "Informe Id. ou Nome"),
    ]
    for a, b in rep:
        t = t.replace(a, b)
    excluir = """                @if ((CachePersister.userIdentity.Roles.Contains("g_Vendedores_*")) || (CachePersister.userIdentity.Roles.Contains("g_Vendedores_Actiondelete")))
                {<button type="button" name="btnRegistroExcluir" title="Excluir Registro" class="btn btn-outline-danger disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button> }
                else
                { <button type="button" name="btnRegistroExcluir" title="Excluir Registro - Não Liberado" class="btn btn-outline-secondary disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button>}
"""
    t = t.replace(
        '                { <button type="button" name="btnRegistroNovo" title="Novo Registro - Não Liberado" class="btn btn-outline-secondary disabled d-inline-flex align-items-center gap-2"><i class="fa fa-plus me-2" aria-hidden="true"></i>Novo</button>}\n            </' + "motion>",
        '                { <button type="button" name="btnRegistroNovo" title="Novo Registro - Não Liberado" class="btn btn-outline-secondary disabled d-inline-flex align-items-center gap-2"><i class="fa fa-plus me-2" aria-hidden="true"></i>Novo</button>}\n' + excluir + '            </' + "motion>",
    )
    # fix botched replace - use div
    t = t.replace("</" + "motion>", "</" + "div>").replace("<" + "motion ", "<" + "motion ")
    thead_old = """                                    <th>Ativo</th>
                                    <th>Descrição</th>
                                    <th>Pagamento</th>
                                    <th>Recebimento</th>
                                    <th>Baixa Autómatica</th>"""
    thead_new = """                                    <th>Ativo</th>
                                    <th>Nome</th>
                                    <th>Revenda</th>
                                    <th>E-mail</th>"""
    t = t.replace(thead_old, thead_new)
    col_old = """                { "width": "8%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "34%", orderable: true, targets: [3] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [4] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [5] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [6] }
                @if (podeEditarVendedores)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [7],"""
    col_new = """                { "width": "8%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "24%", orderable: true, targets: [3] },
                { "width": "30%", orderable: false, className: 'text-sm-center', targets: [4] },
                { "width": "28%", orderable: false, className: 'text-sm-center', targets: [5] }
                @if (podeEditarVendedores)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [6],"""
    t = t.replace(col_old, col_new)
    extra = Path(ROOT / "Areas/g/Views/Vendedores/Index.cshtml.backup_extra.js")
    # append vendedores process JS from original - skip for now, read original file tail
    orig = (ROOT / "Areas/g/Views/Vendedores/Index.cshtml")
    if orig.exists():
        ot = orig.read_text(encoding="utf-8")
        start = ot.find("function jsModalCopiarPrecosDeOutraTabelaVendedor")
        if start > 0:
            extra_js = ot[start:]
            t = t[: t.rfind("</script>")] + "\n\n    " + extra_js + "\n</script>\n"
    write_view("Areas/g/Views/Vendedores/Index.cshtml", t)

def mk_produtos_ncm():
    t = read_tpl()
    rep = [
        ("CstPagRecTiposIndex", "CstProdutosNcmIndex"),
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
        ('@placeholder = "Descrição"', '@placeholder = "Código NCM"'),
        ("(Id. ou Descrição)", "(Id. ou Código NCM)"),
        ("Informe Id. ou Descrição", "Informe Id. ou Código NCM"),
    ]
    for a, b in rep:
        t = t.replace(a, b)
    excluir_novo = """                @if ((CachePersister.userIdentity.Roles.Contains("g_ProdutosNcm_*")) || (CachePersister.userIdentity.Roles.Contains("g_ProdutosNcm_Actioncreate")))
                {<button type="button" name="btnRegistroNovo" title="Novo Registro" class="btn btn-outline-success d-inline-flex align-items-center gap-2" onclick="JsNewRecord('@Url.Action("Create","ProdutosNcm",new { Area = "g" })')"><i class="fa fa-plus me-2" aria-hidden="true"></i>Novo</button> }
                else
                { <button type="button" name="btnRegistroNovo" title="Novo Registro - Não Liberado" class="btn btn-outline-secondary disabled d-inline-flex align-items-center gap-2"><i class="fa fa-plus me-2" aria-hidden="true"></i>Novo</button>}
                @if ((CachePersister.userIdentity.Roles.Contains("g_ProdutosNcm_*")) || (CachePersister.userIdentity.Roles.Contains("g_ProdutosNcm_Actionupdate")))
                {<button type="button" name="btnRegistroExcluir" title="Excluir Registro" class="btn btn-outline-danger disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button> }
                else
                { <button type="button" name="btnRegistroExcluir" title="Excluir Registro - Não Liberado" class="btn btn-outline-secondary disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button>}
                <div class="btn-group" role="group">
                    <button id="btnGroupDrop1" type="button" class="btn btn-outline-info dropdown-toggle d-inline-flex align-items-center gap-2" data-bs-toggle="dropdown" aria-haspopup="true" aria-expanded="false" title="Processos">
                        <i class="fa-solid fa-cog me-2" aria-hidden="true"></i>Processos
                    </button>
                    <div class="dropdown-menu" aria-labelledby="btnGroupDrop1">
                        <a class="d-flex align-items-center dropdown-item" href="#" onclick="jsModalAtualizarTabelaIBPT()"><i class="fa-solid fa-upload me-2" aria-hidden="true"></i>Atualizar Tabela IPBTax</a>
                    </div>
                </div>
"""
    # replace header block - fragile; use simpler insert for processos after novo block
    thead = """                                    <th>Ativo</th>
                                    <th>Codigo NCM</th>"""
    t = t.replace("""                                    <th>Ativo</th>
                                    <th>Descrição</th>
                                    <th>Pagamento</th>
                                    <th>Recebimento</th>
                                    <th>Baixa Autómatica</th>""", thead)
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
    write_view("Areas/g/Views/ProdutosNcm/Index.cshtml", t)

if __name__ == "__main__":
    # save vendedores backup first
    v = ROOT / "Areas/g/Views/Vendedores/Index.cshtml"
    if v.exists():
        (ROOT / "Scripts/vendedores_index_extra.bak").write_text(
            v.read_text(encoding="utf-8")[v.read_text(encoding="utf-8").find("function jsModalCopiar"):], encoding="utf-8"
        )
    mk_vendedores()
    mk_produtos_ncm()
    print("done")
