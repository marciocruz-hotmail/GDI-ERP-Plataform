# Verifica quais controllers alterados tem 0 ocorrencias de 0x56 (V) — sinal de corrupcao
$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$files = @(
    "g\Controllers\NfeController.cs",
    "g\Controllers\ProdutosController.cs",
    "g\Controllers\ProdutosNcmController.cs",
    "g\Controllers\ImportacoesBancariasController.cs",
    "g\Controllers\FinanceiroFaturamentosController.cs",
    "g\Controllers\RequisicoesController.cs",
    "g\Controllers\UsuariosController.cs",
    "gc\Controllers\FinanceiroLancamentosController.cs",
    "gc\Controllers\ComexInvoicesController.cs",
    "gc\Controllers\MovimentosEntradasController.cs",
    "gc\Controllers\ComexFinanceiroController.cs",
    "gc\Controllers\ComexProdutosController.cs",
    "gc\Controllers\EstoqueInventarioController.cs",
    "gc\Controllers\MovimentosController.cs"
)
foreach ($rel in $files) {
    $p = "$root\$rel"
    $bytes = [System.IO.File]::ReadAllBytes($p)
    $count56 = 0
    foreach ($b in $bytes) { if ($b -eq 0x56) { $count56++ } }
    $status = if ($count56 -eq 0) { "CORROMPIDO" } else { "OK ($count56 V's)" }
    Write-Host "$status  $rel"
}
