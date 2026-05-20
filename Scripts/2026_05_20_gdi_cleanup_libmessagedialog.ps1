# gdi_cleanup_libmessagedialog.ps1
# Limpeza de LibMessageDialog: strip HTML do title, remove className btn-info, corrige label
$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"

# Pass 1 — strip HTML do title (fa-solid fa-check + <b>Confirmação</b>)
$count1 = 0
Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $c = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    $old = "<i class='fa-solid fa-check' style='color:#008000'>&nbsp&nbsp</i><b>Confirmação</b>"
    $new = $c -replace [regex]::Escape($old), "Confirmação"
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($_.FullName, $new, [System.Text.Encoding]::UTF8)
        $count1++
        Write-Host "P1: $($_.FullName)"
    }
}
Write-Host "Pass 1 completo: $count1 arquivos alterados"

# Pass 2 — remover linha "                className: 'btn btn-info',"
$count2 = 0
Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $c = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    $new = $c -replace "(?m)^\s*className: 'btn btn-info',\r?\n", ""
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($_.FullName, $new, [System.Text.Encoding]::UTF8)
        $count2++
        Write-Host "P2: $($_.FullName)"
    }
}
Write-Host "Pass 2 completo: $count2 arquivos alterados"

# Pass 3 — label: '&nbsp;OK', -> label: 'OK',
$count3 = 0
Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $c = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    $new = $c -replace [regex]::Escape("label: '&nbsp;OK',"), "label: 'OK',"
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($_.FullName, $new, [System.Text.Encoding]::UTF8)
        $count3++
        Write-Host "P3: $($_.FullName)"
    }
}
Write-Host "Pass 3 completo: $count3 arquivos alterados"

Write-Host ""
Write-Host "=== TOTAL: P1=$count1  P2=$count2  P3=$count3 ==="
