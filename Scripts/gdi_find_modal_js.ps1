$root = 'C:\Marcio\Projetos\GDI-ERP-Plataform'
$targets = @('ModalCadastrar','ModalRecebimento')
foreach ($t in $targets) {
    Write-Host "=== $t ==="
    Get-ChildItem -Path $root -Recurse -Include "*.cshtml","*.js" | Where-Object { $_.FullName -notmatch "\\obj\\" } | ForEach-Object {
        $content = [System.IO.File]::ReadAllText($_.FullName)
        if ($content -match $t) {
            Write-Host "  Ref in: $($_.Name)"
        }
    }
}
