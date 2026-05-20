# Inspeciona views com icone FA fixo dentro de h5/h4/h3 modal-title
# seguido de @Html.Raw(ViewBag.Title) ou similar

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform"
$resultados = [System.Collections.Generic.List[object]]::new()

Get-ChildItem -Path "$root\Areas", "$root\Views" -Recurse -Filter "*.cshtml" | ForEach-Object {
    $path  = $_.FullName
    $lines = [System.IO.File]::ReadAllLines($path)

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Detecta linha com <i class="fa-... dentro de tag de titulo de modal
        if ($line -match '<i\s+class="fa-') {
            # Verifica contexto: linha atual ou proximas 3 contem modal-title ou ViewBag.Title
            $ctx = ($lines[$i..([Math]::Min($i+3, $lines.Count-1))] -join " ")
            $prev = if ($i -gt 0) { $lines[$i-1] } else { "" }
            $prevCtx = ($lines[[Math]::Max(0,$i-2)..$i] -join " ")

            if ($ctx -match "ViewBag\.Title|modal-title" -or $prevCtx -match "modal-title") {
                $resultados.Add([PSCustomObject]@{
                    File    = $_.Name
                    Path    = $path
                    Line    = $i + 1
                    Content = $line.Trim()
                })
            }
        }
    }
}

Write-Host ("Total encontrado: " + $resultados.Count + " ocorrencias`n")
$resultados | Group-Object File | ForEach-Object {
    Write-Host ("=== " + $_.Name + " ===")
    foreach ($r in $_.Group) {
        Write-Host ("  L" + $r.Line + ": " + $r.Content)
    }
    Write-Host ""
}
