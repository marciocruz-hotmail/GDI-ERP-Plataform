$objPath = 'C:\Marcio\Projetos\GDI-ERP-Plataform\obj'
$files = @('ComexFinanceiroController.cs','ComexProdutosController.cs','ProdutosNcmController.cs','ImportacoesBancariasController.cs','RequisicoesController.cs','EstoqueInventarioController.cs')
foreach ($f in $files) {
    $found = Get-ChildItem -Path $objPath -Recurse -Filter $f -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { Write-Host "FOUND: $($found.FullName)" }
    else { Write-Host "NOT FOUND: $f" }
}
