# -*- coding: utf-8 -*-
from pathlib import Path
p = Path(r"c:\Marcio\Projetos\GDI-ERP-Plataform\Areas\g\Views\ContasCaixas\Index.cshtml")
t = p.read_text(encoding="utf-8")
excluir = """                @if ((CachePersister.userIdentity.Roles.Contains("g_ContasCaixas_*")) || (CachePersister.userIdentity.Roles.Contains("g_ContasCaixas_Actiondelete")))
                { <button type="button" name="btnRegistroExcluir" title="Excluir Registro" class="btn btn-outline-danger disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button> }
                else
                { <button type="button" name="btnRegistroExcluir" title="Excluir Registro - Não Liberado" class="btn btn-outline-secondary disabled d-inline-flex align-items-center gap-2"><i class="fa-regular fa-trash-alt me-2" aria-hidden="true"></i>Excluir</button> }
"""
needle = '                { <button type="button" name="btnRegistroNovo" title="Novo Registro - Não Liberado"'
idx = t.find(needle)
if idx >= 0:
    end = t.find('            </motion>', idx)
    if end < 0:
        end = t.find('            </div>', t.find('Novo</button> }', idx) + 10)
    # find closing div after novo else block
    pos = t.find('Novo</button> }', idx)
    pos = t.find('\n            </div>', pos)
    t = t[:pos] + '\n' + excluir + t[pos:]

t = t.replace('ContasCaixasIndex_descricao', 'ContasCaixasIndex_nome')
t = t.replace('edit_id_pagrec_condicao', 'edit_id_conta_caixa')
t = t.replace('edit_descricao_pagrec_condicao', 'edit_nome_conta_caixa')
t = t.replace('@Html.Label("Descrição"', '@Html.Label("Nome"')
t = t.replace('@placeholder = "Descrição"', '@placeholder = "Nome da conta"')
t = t.replace('Pesquisar condições', 'Pesquisar contas caixa')
t = t.replace('limpar e listar todas as condições', 'limpar e listar todas as contas caixa')
t = t.replace('Pesquisando condições...', 'Pesquisando contas caixa...')
t = t.replace('(Id. ou Descrição)', '(Id. ou Nome)')
t = t.replace('Informe Id. ou Descrição', 'Informe Id. ou Nome')
t = t.replace('Editar condição', 'Editar conta caixa')
t = t.replace('title="Editar condição Id.', 'title="Editar conta Id.')

thead_old = """                                    <th>Id.</th>
                                    <th>Ativo</th>
                                    <th>Descrição</th>
                                    <th>Pagamento</th>
                                    <th>Recebimento</th>
                                    <th>Qtd. Dias</th>
                                    <th>Qtd. Parcela</th>"""
thead_new = """                                    <th>Id.</th>
                                    <th>Nome</th>
                                    <th>Banco</th>
                                    <th>Agência</th>
                                    <th>Conta</th>
                                    <th>Emissão Boleto</th>"""
t = t.replace(thead_old, thead_new)

col_old = """                { "width": "6%", orderable: true, className: 'text-sm-center', targets: [1] },
                { "width": "6%", orderable: false, className: 'text-sm-center', targets: [2] },
                { "width": "30%", orderable: true, targets: [3] },
                { "width": "9%", orderable: false, className: 'text-sm-center', targets: [4] },
                { "width": "9%", orderable: false, className: 'text-sm-center', targets: [5] },
                { "width": "9%", orderable: false, className: 'text-sm-center', targets: [6] },
                { "width": "9%", orderable: false, className: 'text-sm-center', targets: [7] }
                @if (podeEditarContasCaixas)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [8],"""
col_new = """                { "width": "6%", orderable: true, className: 'text-sm-center', targets: [1] },
                { "width": "34%", orderable: true, targets: [2] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [3] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [4] },
                { "width": "10%", orderable: false, className: 'text-sm-center', targets: [5] },
                { "width": "16%", orderable: false, className: 'text-sm-center', targets: [6] }
                @if (podeEditarContasCaixas)
                {
                <text>,
                { "width": "4%", orderable: false, className: 'text-sm-center dt-no-row-select', targets: [7],"""
t = t.replace(col_old, col_new)

t = t.replace("return !isEmpty($('#edit_id_pagrec_condicao').val()) || !isEmpty($('#edit_descricao_pagrec_condicao').val());",
              "return !isEmpty($('#edit_id_conta_caixa').val()) || !isEmpty($('#edit_nome_conta_caixa').val());")
t = t.replace("gdiAplicarFoco('#edit_id_pagrec_condicao', true);", "gdiAplicarFoco('#edit_id_conta_caixa', true);")
t = t.replace("$('#edit_id_pagrec_condicao').val('');\n            $('#edit_descricao_pagrec_condicao').val('');",
              "$('#edit_id_conta_caixa').val('');\n            $('#edit_nome_conta_caixa').val('');")
t = t.replace("yesCustomField01: emptyIfNull($('#edit_id_pagrec_condicao').val()).toString(),\n                        yesCustomField02: emptyIfNull($('#edit_descricao_pagrec_condicao').val()).toString()",
              "yesCustomField01: emptyIfNull($('#edit_id_conta_caixa').val()).toString(),\n                        yesCustomField02: emptyIfNull($('#edit_nome_conta_caixa').val()).toString()")

p.write_text(t, encoding="utf-8")
print("done", p)
