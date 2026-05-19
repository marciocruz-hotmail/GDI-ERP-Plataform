$root = 'C:\Marcio\Projetos\GDI-ERP-Plataform\Areas'
$targets = @('ModalCadastrarAtualizarItensImportacao','ModalHistoricoMovimento','ModalInsertEditItemCompra','ModalRecebimentoComexLote','ModalRecebimentoComexObs')
foreach ($t in $targets) {
    Write-Host "=== $t ==="
    Get-ChildItem -Path $root -Recurse -Filter '*.cs' | ForEach-Object {
        $lines = [System.IO.File]::ReadAllLines($_.FullName)
        for ($i=0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "ActionResult\s+$t\s*\(") {
                Write-Host "  Found in: $($_.FullName) at L$($i+1)"
                for ($k=$i; $k -lt [Math]::Min($i+30,$lines.Count); $k++) {
                    if ($lines[$k] -match 'ViewBag\.Title') { Write-Host "  VB: $($lines[$k].Trim())"; break }
                    if ($k -gt $i+1 -and $lines[$k] -match 'public.*ActionResult') { break }
                }
            }
        }
    }
}
