# Remove entradas de arquivos deletados do .csproj e os .map residuais do disco

$csproj = "C:\Marcio\Projetos\GDI-ERP-Plataform\GDI-ERP-Plataform.csproj"
$root   = "C:\Marcio\Projetos\GDI-ERP-Plataform"

# --- 1. Remover .map residuais do disco ---
$mapsToDelete = @(
    "Scripts\bootstrap.esm.js.map",
    "Scripts\bootstrap.esm.min.js.map",
    "Scripts\jquery-3.7.1.min.map",
    "Scripts\jquery-3.7.1.slim.min.map"
)
Write-Host "=== .map residuais ==="
foreach ($f in $mapsToDelete) {
    $p = "$root\$f"
    if (Test-Path $p) { Remove-Item $p -Force; Write-Host "  REMOVIDO: $f" }
    else              { Write-Host "  nao encontrado: $f" }
}

# --- 2. Remover linhas do .csproj ---
$patterns = @(
    'bootstrap-grid',
    'bootstrap-reboot',
    'bootstrap-utilities',
    'bootstrap\.rtl',
    'bootstrap-switch',
    'bootstrap\.esm',
    'jquery-3\.7',
    'modernizr-2\.8',
    'jquery\.validate-vsdoc'
)

Write-Host ""
Write-Host "=== .csproj ==="
$c = [System.IO.File]::ReadAllText($csproj, [System.Text.Encoding]::UTF8)
$lines = $c -split "`r?`n"
$antes = $lines.Count

$filtered = $lines | Where-Object {
    $line = $_
    $remove = $false
    foreach ($p in $patterns) {
        if ($line -match $p) { $remove = $true; break }
    }
    -not $remove
}

$depois = $filtered.Count
$removed = $antes - $depois

# Reescrever preservando terminador de linha original
$nl = if ($c -match "`r`n") { "`r`n" } else { "`n" }
$novo = $filtered -join $nl
[System.IO.File]::WriteAllText($csproj, $novo, [System.Text.Encoding]::UTF8)

Write-Host "  Linhas removidas do .csproj: $removed (de $antes para $depois)"
Write-Host ""
Write-Host "=== Concluido ==="
