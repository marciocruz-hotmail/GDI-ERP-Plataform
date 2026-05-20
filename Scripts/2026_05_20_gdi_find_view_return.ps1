$root = 'C:\Marcio\Projetos\GDI-ERP-Plataform\Areas'
$targets = @('ModalCadastrarAtualizarItensImportacao','ModalHistoricoMovimento','ModalInsertEditItemCompra','ModalRecebimentoComexLote','ModalRecebimentoComexObs')
foreach ($t in $targets) {
    Write-Host "=== $t ==="
    Get-ChildItem -Path $root -Recurse -Filter '*.cs' | ForEach-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        if ($content -match "View\(`"$t`"\)|PartialView\(`"$t`"\)") {
            $lines = [System.IO.File]::ReadAllLines($_.FullName)
            for ($i=0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match "View\(`"$t`"\)|PartialView\(`"$t`"\)") {
                    # find enclosing action
                    for ($k=$i; $k -ge 0; $k--) {
                        if ($lines[$k] -match 'public.*ActionResult\s+(\w+)\s*\(') {
                            Write-Host "  In: $($_.FullName)"
                            Write-Host "  Action: $($Matches[1]) => returns $t (L$($i+1))"
                            for ($j=$k; $j -lt [Math]::Min($k+40,$lines.Count); $j++) {
                                if ($lines[$j] -match 'ViewBag\.Title') { Write-Host "  VB: $($lines[$j].Trim())"; break }
                                if ($j -gt $k+1 -and $lines[$j] -match 'public.*ActionResult') { break }
                            }
                            break
                        }
                    }
                }
            }
        }
    }
}
