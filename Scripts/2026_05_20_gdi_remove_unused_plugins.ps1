$root = "C:\Marcio\Projetos\GDI-ERP-Plataform"
$total = 0

function Remove-Item-Log($path) {
    if (Test-Path $path) {
        Remove-Item $path -Force -Recurse -ErrorAction SilentlyContinue
        $script:total++
        Write-Host "  REMOVIDO: $path"
    } else {
        Write-Host "  nao encontrado: $path"
    }
}

Write-Host "=== 1. Plugin bootstrap-switch (pasta inteira) ==="
Remove-Item-Log "$root\LibUI_AdminLTE-4.0.0\plugins\bootstrap-switch"

Write-Host ""
Write-Host "=== 2. Scripts orfaos em Scripts\ ==="
$scripts = @(
    "jquery-3.7.0.intellisense.js",
    "jquery-3.7.1.intellisense.js",
    "jquery-3.7.1.js",
    "jquery-3.7.1.min.js",
    "jquery-3.7.1.slim.js",
    "jquery-3.7.1.slim.min.js",
    "jquery.validate-vsdoc.js",
    "modernizr-2.8.3.js",
    "bootstrap.esm.js",
    "bootstrap.esm.min.js"
)
foreach ($f in $scripts) {
    Remove-Item-Log "$root\Scripts\$f"
}

Write-Host ""
Write-Host "=== 3. CSS variantes nao usadas em Content\ ==="
$cssFiles = @(
    "bootstrap-grid.css", "bootstrap-grid.css.map",
    "bootstrap-grid.min.css", "bootstrap-grid.min.css.map",
    "bootstrap-grid.rtl.css", "bootstrap-grid.rtl.css.map",
    "bootstrap-grid.rtl.min.css", "bootstrap-grid.rtl.min.css.map",
    "bootstrap-reboot.css", "bootstrap-reboot.css.map",
    "bootstrap-reboot.min.css", "bootstrap-reboot.min.css.map",
    "bootstrap-reboot.rtl.css", "bootstrap-reboot.rtl.css.map",
    "bootstrap-reboot.rtl.min.css", "bootstrap-reboot.rtl.min.css.map",
    "bootstrap-utilities.css", "bootstrap-utilities.css.map",
    "bootstrap-utilities.min.css", "bootstrap-utilities.min.css.map",
    "bootstrap-utilities.rtl.css", "bootstrap-utilities.rtl.css.map",
    "bootstrap-utilities.rtl.min.css", "bootstrap-utilities.rtl.min.css.map",
    "bootstrap.rtl.css", "bootstrap.rtl.css.map",
    "bootstrap.rtl.min.css", "bootstrap.rtl.min.css.map"
)
foreach ($f in $cssFiles) {
    Remove-Item-Log "$root\Content\$f"
}

Write-Host ""
Write-Host "=== Concluido: $total itens removidos ==="
