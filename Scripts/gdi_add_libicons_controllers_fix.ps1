# Corrige os 3 controllers que falharam por encoding (UTF-8 sem BOM lido como Windows-1252 pelo PS 5.x)
# Usa GetEncoding(1252) para leitura e escrita — os bytes UTF-8 das strings acentuadas
# passam intocados (round-trip correto) porque script e arquivo sofrem o mesmo decode

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$enc1252 = [System.Text.Encoding]::GetEncoding(1252)
$totalAlteracoes = 0

function Apply1252($filePath, $replacements) {
    if (-not (Test-Path $filePath)) { Write-Host "  SKIP (nao encontrado): $filePath"; return }
    $raw = [System.IO.File]::ReadAllText($filePath, $enc1252)
    $novo = $raw
    foreach ($r in $replacements) {
        $novo = $novo.Replace($r[0], $r[1])
    }
    if ($novo -ne $raw) {
        [System.IO.File]::WriteAllText($filePath, $novo, $enc1252)
        $script:totalAlteracoes++
        Write-Host "  ALTERADO: $([System.IO.Path]::GetFileName($filePath))"
    } else {
        Write-Host "  SEM MUDANCA: $([System.IO.Path]::GetFileName($filePath))"
    }
}

# ---------------------------------------------------------------------------
# FinanceiroController (g)
# ---------------------------------------------------------------------------
Write-Host "`n=== FinanceiroController (g) ==="
Apply1252 "$root\g\Controllers\FinanceiroController.cs" @(
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
# g/FinanceiroLancamentosController
# ---------------------------------------------------------------------------
Write-Host "`n=== FinanceiroLancamentosController (g) ==="
Apply1252 "$root\g\Controllers\FinanceiroLancamentosController.cs" @(
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
# gc/ComexImportacoesController
# ---------------------------------------------------------------------------
Write-Host "`n=== ComexImportacoesController (gc) ==="
Apply1252 "$root\gc\Controllers\ComexImportacoesController.cs" @(
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

Write-Host "`n=== Concluido: $totalAlteracoes controllers alterados ==="
