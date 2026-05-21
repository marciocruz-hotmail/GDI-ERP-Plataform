$root = "C:\Marcio\Projetos\GDI-ERP-Plataform"

Get-ChildItem -Path "$root\Areas" -Recurse -Filter "*.cshtml" | ForEach-Object {
    $lines = [System.IO.File]::ReadAllLines($_.FullName)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        # Apenas linhas com title: "..." contendo HTML de icone dentro de LibMessageDialog
        if ($lines[$i] -match "^\s*title:\s*"".*fa-(solid|regular|brands).*<b>") {
            $name = $_.Name
            Write-Host ""
            Write-Host ("=== $name  L" + ($i+1) + " ===")
            $start = [Math]::Max(0, $i - 3)
            $end   = [Math]::Min($lines.Count - 1, $i + 18)
            for ($j = $start; $j -le $end; $j++) {
                Write-Host (($j+1).ToString().PadLeft(4) + "  " + $lines[$j])
            }
        }
    }
}
