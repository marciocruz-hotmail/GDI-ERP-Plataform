$root = 'C:\Marcio\Projetos\GDI-ERP-Plataform'
$targets = @('ModalCadastrarAtualizarItensImportacao','ModalHistoricoMovimento','ModalInsertEditItemCompra','ModalRecebimentoComexLote','ModalRecebimentoComexObs')
foreach ($t in $targets) {
    Write-Host "=== $t ==="
    # Check in all .cs, .cshtml, .js files
    Get-ChildItem -Path $root -Recurse -Include "*.cs","*.cshtml","*.js" | ForEach-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        if ($content -match $t) {
            Write-Host "  Ref in: $($_.Name) ($($_.FullName))"
        }
    }
}
