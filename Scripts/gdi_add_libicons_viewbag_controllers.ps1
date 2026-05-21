# Adiciona LibIcons.getIcon() ao ViewBag.Title de acoes de modal que ainda usam texto puro
# Executa substituicoes exatas (string literal) por controller

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$totalAlteracoes = 0

function Apply-Replacements($filePath, $replacements) {
    if (-not (Test-Path $filePath)) { Write-Host "  SKIP (nao encontrado): $filePath"; return }
    $raw = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
    $novo = $raw
    foreach ($r in $replacements) {
        $novo = $novo.Replace($r[0], $r[1])
    }
    if ($novo -ne $raw) {
        [System.IO.File]::WriteAllText($filePath, $novo, [System.Text.Encoding]::UTF8)
        $script:totalAlteracoes++
        Write-Host "  ALTERADO: $([System.IO.Path]::GetFileName($filePath))"
    } else {
        Write-Host "  SEM MUDANCA: $([System.IO.Path]::GetFileName($filePath))"
    }
}

# ---------------------------------------------------------------------------
# 1. FinanceiroController (g area)
# ---------------------------------------------------------------------------
Write-Host "`n=== FinanceiroController (g) ==="
Apply-Replacements "$root\g\Controllers\FinanceiroController.cs" @(
    @(
        'ViewBag.Title = "Baixar Títulos";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Baixar Títulos";'
    ),
    @(
        'ViewBag.Title = "Cancelar Título Financeiro";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "#cc0000", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar Título Financeiro";'
    ),
    @(
        'ViewBag.Title = "Editar Título - Reabrir Lançamentos";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Editar Título - Reabrir Lançamentos";'
    ),
    @(
        'ViewBag.Title = "Prorrogar Vencimento Título";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Prorrogar Vencimento Título";'
    ),
    @(
        'ViewBag.Title = "Gerar Remessa - Boletos Bancários (Títulos Abertos e Cancelados)";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Gerar Remessa - Boletos Bancários (Títulos Abertos e Cancelados)";'
    )
)

# ---------------------------------------------------------------------------
# 2. NfeController (g area)
# ---------------------------------------------------------------------------
Write-Host "`n=== NfeController (g) ==="
Apply-Replacements "$root\g\Controllers\NfeController.cs" @(
    @(
        'ViewBag.Title = "Enviar NF-e por e-mail";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Enviar NF-e por e-mail";'
    ),
    @(
        'ViewBag.Title = "Exportar dados NF-e (PDF)";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Exportar dados NF-e (PDF)";'
    ),
    @(
        'ViewBag.Title = "Gerar NF-e";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Gerar NF-e";'
    ),
    @(
        'ViewBag.Title = "Atualizar status NF-e";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Atualizar status NF-e";'
    ),
    @(
        'ViewBag.Title = "Enviar cancelamento NF-e";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Enviar cancelamento NF-e";'
    ),
    @(
        'ViewBag.Title = "Cancelar NF-e";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar NF-e";'
    ),
    @(
        'ViewBag.Title = "Transmitir/Receber NF-e";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Transmitir/Receber NF-e";'
    ),
    @(
        'ViewBag.Title = "Importar NF-e (lote)";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Importar NF-e (lote)";'
    )
)

# ---------------------------------------------------------------------------
# 3. ImportacoesBancariasController (g area)
# ---------------------------------------------------------------------------
Write-Host "`n=== ImportacoesBancariasController (g) ==="
Apply-Replacements "$root\g\Controllers\ImportacoesBancariasController.cs" @(
    @(
        'ViewBag.Title = "Importação Bancária - CNAB Boletos";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-import", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Importação Bancária - CNAB Boletos";'
    )
)

# ---------------------------------------------------------------------------
# 4. ProdutosController (g area)
# ---------------------------------------------------------------------------
Write-Host "`n=== ProdutosController (g) ==="
Apply-Replacements "$root\g\Controllers\ProdutosController.cs" @(
    @(
        'ViewBag.Title = "Produtos/Serviços - Atualizar Cadastro";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Produtos/Serviços - Atualizar Cadastro";'
    ),
    @(
        'ViewBag.Title = "Produto - Desativar Cadastro";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Produto - Desativar Cadastro";'
    ),
    @(
        'String Title = "<b>FICHA ESTOQUE</b>";',
        'String Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "<b>FICHA ESTOQUE</b>";'
    )
)

# ---------------------------------------------------------------------------
# 5. ProdutosNcmController (g area)
# ---------------------------------------------------------------------------
Write-Host "`n=== ProdutosNcmController (g) ==="
Apply-Replacements "$root\g\Controllers\ProdutosNcmController.cs" @(
    @(
        'ViewBag.Title = "Atualizar Tabela IBPTax";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Atualizar Tabela IBPTax";'
    )
)

# ---------------------------------------------------------------------------
# 6. g/FinanceiroLancamentosController
# ---------------------------------------------------------------------------
Write-Host "`n=== FinanceiroLancamentosController (g) ==="
Apply-Replacements "$root\g\Controllers\FinanceiroLancamentosController.cs" @(
    @(
        'ViewBag.Title = "Incluir Lançamentos";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Incluir Lançamentos";'
    ),
    @(
        'ViewBag.Title = "Gerar Faturamento - Todos os Títulos";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Gerar Faturamento - Todos os Títulos";'
    ),
    @(
        'ViewBag.Title = "Gerar Título para o Cliente (Fechar Lançamentos - Criando Novo Título)";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Gerar Título para o Cliente (Fechar Lançamentos - Criando Novo Título)";'
    ),
    @(
        'ViewBag.Title = "Concluir edição do título do Cliente (Fechar Lançamentos - Reabrir Título Em Edição)";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Concluir edição do título do Cliente (Fechar Lançamentos - Reabrir Título Em Edição)";'
    ),
    @(
        'ViewBag.Title = "Cancelar Lançamentos";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "#cc0000", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar Lançamentos";'
    )
)

# ---------------------------------------------------------------------------
# 7. gc/FinanceiroLancamentosController
# ---------------------------------------------------------------------------
Write-Host "`n=== FinanceiroLancamentosController (gc) ==="
Apply-Replacements "$root\gc\Controllers\FinanceiroLancamentosController.cs" @(
    @(
        'ViewBag.Title = "Baixar Lançamento Financeiro";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Baixar Lançamento Financeiro";'
    ),
    @(
        'ViewBag.Title = "Cancelar Lançamento Financeiro";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "#cc0000", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar Lançamento Financeiro";'
    ),
    @(
        'ViewBag.Title = "Cancelar Faturamento";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "#cc0000", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar Faturamento";'
    )
)

# ---------------------------------------------------------------------------
# 8. gc/ComexImportacoesController
# ---------------------------------------------------------------------------
Write-Host "`n=== ComexImportacoesController (gc) ==="
Apply-Replacements "$root\gc\Controllers\ComexImportacoesController.cs" @(
    @(
        'ViewBag.Title = "Cancelar Importação";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "#cc0000", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar Importação";'
    ),
    @(
        'ViewBag.Title = "Excluir Itens Importação";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Excluir Itens Importação";'
    ),
    @(
        'ViewBag.Title = "Fechamento Custos da Importação Nº " + record_gc_comex_importacoes.numero.EmptyIfNull().ToString();',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Fechamento Custos da Importação Nº " + record_gc_comex_importacoes.numero.EmptyIfNull().ToString();'
    )
)

# ---------------------------------------------------------------------------
# 9. gc/ComexInvoicesController
# ---------------------------------------------------------------------------
Write-Host "`n=== ComexInvoicesController (gc) ==="
Apply-Replacements "$root\gc\Controllers\ComexInvoicesController.cs" @(
    @(
        'ViewBag.Title = "<b>Itens da Invoice </b>" + record_gc_comex_invoices.invoice.EmptyIfNull().ToString();',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "<b>Itens da Invoice </b>" + record_gc_comex_invoices.invoice.EmptyIfNull().ToString();'
    ),
    @(
        'ViewBag.Title = "<b>Câmbio da Invoice </b>" + record_gc_comex_invoices.invoice.EmptyIfNull().ToString();',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "<b>Câmbio da Invoice </b>" + record_gc_comex_invoices.invoice.EmptyIfNull().ToString();'
    ),
    @(
        'ViewBag.Title = "Cancelar Invoice";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "#cc0000", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar Invoice";'
    )
)

# ---------------------------------------------------------------------------
# 10. gc/MovimentosEntradasController
# ---------------------------------------------------------------------------
Write-Host "`n=== MovimentosEntradasController (gc) ==="
Apply-Replacements "$root\gc\Controllers\MovimentosEntradasController.cs" @(
    @(
        'ViewBag.Title = "Cancelar Movimento NF Importada - Compra Exterior";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar Movimento NF Importada - Compra Exterior";'
    ),
    @(
        'ViewBag.Title = "Faturar NF Importação";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Faturar NF Importação";'
    )
)

# ---------------------------------------------------------------------------
# 11. g/FinanceiroFaturamentosController
# ---------------------------------------------------------------------------
Write-Host "`n=== FinanceiroFaturamentosController (g) ==="
Apply-Replacements "$root\g\Controllers\FinanceiroFaturamentosController.cs" @(
    @(
        'ViewBag.Title = "Sincronizar Faturamento - Gestor Franquia";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Sincronizar Faturamento - Gestor Franquia";'
    ),
    @(
        'ViewBag.Title = "Importação Faturamento - Gestor Franquia";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-import", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Importação Faturamento - Gestor Franquia";'
    ),
    @(
        'ViewBag.Title = "Comunicado de Faturamento - Email Clientes";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Comunicado de Faturamento - Email Clientes";'
    ),
    @(
        'ViewBag.Title = "Envio - Nota Fiscal Eletrônica Para Email Cliente";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-file-alt", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Envio - Nota Fiscal Eletrônica Para Email Cliente";'
    )
)

# ---------------------------------------------------------------------------
# 12. gc/ComexFinanceiroController (apenas ModalCancelarComexFinanceiro)
# ---------------------------------------------------------------------------
Write-Host "`n=== ComexFinanceiroController (gc) ==="
Apply-Replacements "$root\gc\Controllers\ComexFinanceiroController.cs" @(
    @(
        'ViewBag.Title = "Cancelar Pagamento Comex";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "#cc0000", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cancelar Pagamento Comex";'
    )
)

# ---------------------------------------------------------------------------
# 13. gc/ComexProdutosController
# ---------------------------------------------------------------------------
Write-Host "`n=== ComexProdutosController (gc) ==="
$comexProdCtrl = Get-ChildItem -Path $root -Recurse -Filter "ComexProdutosController.cs" | Select-Object -First 1
if ($comexProdCtrl) {
    Apply-Replacements $comexProdCtrl.FullName @(
        @(
            'ViewBag.Title = "Produto Comex - Desativar Cadastro";',
            'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Produto Comex - Desativar Cadastro";'
        )
    )
} else { Write-Host "  SKIP: ComexProdutosController.cs nao encontrado" }

# ---------------------------------------------------------------------------
# 14. gc/EstoqueInventarioController — ModalFinalizarInventario
#     TitleModal e atribuido em 2 pontos no mesmo metodo
# ---------------------------------------------------------------------------
Write-Host "`n=== EstoqueInventarioController (gc) ==="
Apply-Replacements "$root\gc\Controllers\EstoqueInventarioController.cs" @(
    @(
        'TitleModal = "Finalizar Inventário Nº " + RecordInventario.id_inventario.ToString();',
        'TitleModal = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Finalizar Inventário Nº " + RecordInventario.id_inventario.ToString();'
    )
)

# ---------------------------------------------------------------------------
# 15. g/RequisicoesController
# ---------------------------------------------------------------------------
Write-Host "`n=== RequisicoesController (g) ==="
$reqCtrl = Get-ChildItem -Path $root -Recurse -Filter "RequisicoesController.cs" | Select-Object -First 1
if ($reqCtrl) {
    Apply-Replacements $reqCtrl.FullName @(
        @(
            'ViewBag.Title = "Solicitação - Bloqueio de Logon";',
            'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Solicitação - Bloqueio de Logon";'
        )
    )
} else { Write-Host "  SKIP: RequisicoesController.cs nao encontrado" }

# ---------------------------------------------------------------------------
# 16. g/UsuariosController — modalUsuarioTrocarSenha (4 ramos condicionais)
# ---------------------------------------------------------------------------
Write-Host "`n=== UsuariosController (g) ==="
Apply-Replacements "$root\g\Controllers\UsuariosController.cs" @(
    @(
        'ViewBag.Title = "Usuário - Alterar Senha";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Usuário - Alterar Senha";'
    ),
    @(
        'ViewBag.Title = "Logon - Alterar Senha";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Logon - Alterar Senha";'
    ),
    @(
        'ViewBag.Title = "Cliente - Alterar Senha";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Cliente - Alterar Senha";'
    ),
    @(
        'ViewBag.Title = "Vendedor - Alterar Senha";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Vendedor - Alterar Senha";'
    )
)

# ---------------------------------------------------------------------------
# 17. gc/MovimentosController — ModalInvoicesItensEspelhoDigital e ModalPedidoSeparacaoLotes
# ---------------------------------------------------------------------------
Write-Host "`n=== MovimentosController (gc) ==="
Apply-Replacements "$root\gc\Controllers\MovimentosController.cs" @(
    @(
        'ViewBag.Title = "<b>Itens Importados</b>";',
        'ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "<b>Itens Importados</b>";'
    ),
    @(
        'String Title = "Informe o(s) lote(s) para o item:<br/>";',
        'String Title = LibIcons.getIcon("fa-solid fa-clipboard-list", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Informe o(s) lote(s) para o item:<br/>";'
    )
)

Write-Host "`n=== Concluido: $totalAlteracoes controladores alterados ==="
