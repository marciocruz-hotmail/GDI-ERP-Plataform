# -*- coding: utf-8 -*-
from pathlib import Path
ROOT = Path(r"c:\Marcio\Projetos\GDI-ERP-Plataform")
p = ROOT / "Areas/g/Views/Vendedores/Index.cshtml"
t = p.read_text(encoding="utf-8")
pairs = [
    ("CstPagRecCondicoesIndex", "CstVendedoresIndex"),
    ("g_PagRecCondicoes", "g_Vendedores"),
    ("PagRecCondicoes", "Vendedores"),
    ("pagreccondicoes", "vendedores"),
    ("GPagRecCondicoes", "GVendedores"),
    ("PagRecCondicoesIndex_id", "VendedoresIndex_id"),
    ("PagRecCondicoesIndex_descricao", "VendedoresIndex_nome"),
    ("edit_id_pagrec_condicao", "edit_id_vendedor"),
    ("edit_descricao_pagrec_condicao", "edit_nome_vendedor"),
    ("keypressedPagRecCondicoes", "keypressedVendedores"),
    ("jsPagRecCondicoes", "jsVendedores"),
    ("Pesquisar condições", "Pesquisar vendedores"),
    ("todas as condições", "todos os vendedores"),
    ("Pesquisando condições...", "Pesquisando vendedores..."),
    ("Editar condição", "Editar vendedor"),
    ('@Html.Label("Descrição"', '@Html.Label("Nome"'),
    ('placeholder = "Descrição"', 'placeholder = "Nome do vendedor"'),
    ("(Id. ou Descrição)", "(Id. ou Nome)"),
    ("Informe Id. ou Descrição", "Informe Id. ou Nome"),
]
for a, b in pairs:
    t = t.replace(a, b)
excluir = """                @if ((CachePersister.userIdentity.Roles.Contains("g_Vendedores_*")) || (CachePersister.userIdentity.Roles.Contains("g_Vendedores_Actiondelete")))
                {<button type="button" name="btnRegistroExcluir" title="Excluir Registro" class="btn btn-outline-danger disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button> }
                else
                { <button type="button" name="btnRegistroExcluir" title="Excluir Registro - Não Liberado" class="btn btn-outline-secondary disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button>}
"""
needle = '                { <button type="button" name="btnRegistroNovo" title="Novo Registro - Não Liberado"'
idx = t.find(needle)
if idx >= 0:
    pos = t.find("Novo</button> }", idx)
    pos = t.find("\n            </motion>", pos)
    if pos < 0:
        pos = t.find("\n            </motion>", pos)
    pos = t.find("\n            </" + "motion>", pos)
    if pos < 0:
        pos = t.find("\n            </" + "div>", t.find("Novo</button> }", idx))
    t = t[:pos] + "\n" + excluir + t[pos:]
thead_old = """                                    <th>Ativo</th>
                                    <th>Descrição</th>
                                    <th>Pagamento</th>
                                    <th>Recebimento</th>
                                    <th>Qtd. Dias</th>
                                    <th>Qtd. Parcela</th>"""
thead_new = """                                    <th>Ativo</th>
                                    <th>Nome</th>
                                    <th>Revenda</th>
                                    <th>E-mail</th>"""
t = t.replace(thead_old, thead_new)
col_old = """                { "width": "6%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "30%", orderable: true, targets: [3] },
                { "width": "9%", orderable: false, className: 'text-sm-center', targets: [4] },
                { "width": "9%", orderable: false, className: 'text-sm-center', targets: [5] },
                { "width": "9%", orderable: false, className: 'text-sm-center', targets: [6] },
                { "width": "9%", orderable: false, className: 'text-sm-center', targets: [7] }
                @if (podeEditarVendedores)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [8],"""
col_new = """                { "width": "8%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "24%", orderable: true, targets: [3] },
                { "width": "30%", orderable: false, className: 'text-sm-center', targets: [4] },
                { "width": "28%", orderable: false, className: 'text-sm-center', targets: [5] }
                @if (podeEditarVendedores)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [6],"""
t = t.replace(col_old, col_new)
bak = (ROOT / "Scripts/vendedores_index_extra.bak").read_text(encoding="utf-8")
start = bak.find("function jsModalCopiar")
if start >= 0:
    t = t[: t.rfind("</script>")] + "\n\n    " + bak[start:]
p.write_text(t, encoding="utf-8")
print("patched vendedores")
