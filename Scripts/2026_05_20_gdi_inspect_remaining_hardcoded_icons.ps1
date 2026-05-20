# Lista views que ainda tem icone FA hardcoded no modal-title
# (as que nao foram processadas na fase anterior)

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform"
$resultados = [System.Collections.Generic.List[object]]::new()

Get-ChildItem -Path "$root\Areas", "$root\Views" -Recurse -Filter "*.cshtml" | ForEach-Object {
    $path  = $_.FullName
    $lines = [System.IO.File]::ReadAllLines($path)

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '<i\s+class="fa-') {
            $ctx   = ($lines[$i..([Math]::Min($i+3, $lines.Count-1))] -join " ")
            $prev  = ($lines[[Math]::Max(0,$i-2)..$i] -join " ")
            if ($ctx -match "ViewBag\.Title|modal-title" -or $prev -match "modal-title") {
                $iconMatch = [regex]::Match($line, 'class="(fa-(?:solid|regular|brands)\s+[^"]+)"')
                $icon = if ($iconMatch.Success) { $iconMatch.Groups[1].Value.Trim() } else { "?" }
                $resultados.Add([PSCustomObject]@{
                    File    = $_.Name
                    Path    = $path
                    Line    = $i + 1
                    Icon    = $icon
                })
            }
        }
    }
}

Write-Host ("Total: " + $resultados.Count + " ocorrencias`n")
$resultados | Group-Object File | Sort-Object Name | ForEach-Object {
    Write-Host ("=== " + $_.Name + " ===")
    foreach ($r in $_.Group) {
        Write-Host ("  L" + $r.Line + "  icon: " + $r.Icon)
        Write-Host ("  path: " + $r.Path)
    }
    Write-Host ""
}
