# Inspeciona ViewBag.Title em actions de modal nos controllers das areas g e gc
# Classifica: LibIcons (icone embutido) vs TextoPuro

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$controllers = @(
    "FinanceiroController.cs",
    "MovimentosController.cs",
    "EstoqueController.cs",
    "GedController.cs",
    "ImportacoesBancariasController.cs",
    "MovimentosEntradasController.cs",
    "ComexImportacoesController.cs",
    "ComexInvoicesController.cs",
    "ClientesController.cs",
    "EstoqueInventarioController.cs",
    "EstoqueLotesController.cs",
    "ComexFinanceiroController.cs",
    "FinanceiroFaturamentosController.cs",
    "AtendimentosController.cs",
    "UsuariosController.cs",
    "ProdutosController.cs",
    "RequisicoesSCController.cs"
)

foreach ($ctrl in $controllers) {
    $f = Get-ChildItem -Path $root -Recurse -Filter $ctrl | Select-Object -First 1
    if (-not $f) { continue }

    $lines = [System.IO.File]::ReadAllLines($f.FullName)
    $hasOutput = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match 'ViewBag\.Title\s*=') {
            $tipo = if ($line -match "LibIcons|getIcon|<i |<b>|style=|&nbsp") { "LibIcons" } else { "TextoPuro" }
            if ($tipo -eq "LibIcons") {
                if (-not $hasOutput) { Write-Host "=== $ctrl ==="; $hasOutput = $true }
                Write-Host ("  L" + ($i+1) + " [" + $tipo + "]: " + $line.Trim())
            }
        }
    }
}

Write-Host ""
Write-Host "=== Concluido ==="
