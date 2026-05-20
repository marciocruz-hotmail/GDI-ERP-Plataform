# Mapeia: controller + ViewBag.Title com LibIcons -> qual View() e retornada
# Para identificar quais modais tem icone duplicado (ViewBag.Title ja traz icone)

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$resultados = [System.Collections.Generic.List[object]]::new()

Get-ChildItem -Path $root -Recurse -Filter "*Controller.cs" | ForEach-Object {
    $path = $_.FullName
    $lines = [System.IO.File]::ReadAllLines($path)

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Linha com ViewBag.Title contendo LibIcons/getIcon
        if ($line -match 'ViewBag\.Title' -and $line -match 'getIcon|LibIcons') {
            # Busca return View() nas proximas 10 linhas
            for ($k = $i + 1; $k -lt [Math]::Min($i + 10, $lines.Count); $k++) {
                if ($lines[$k] -match 'return\s+View\s*\(\s*"?([^")\s]+)"?') {
                    $viewName = $Matches[1].Trim('"')
                    # Apenas modais
                    if ($viewName -match '^Modal|^modal') {
                        $resultados.Add([PSCustomObject]@{
                            Controller = $_.Name
                            Line       = $i + 1
                            ViewName   = $viewName
                        })
                    }
                    break
                }
            }
        }
    }
}

Write-Host ("Modais cujo ViewBag.Title usa LibIcons (icone duplicado na view): " + $resultados.Count)
Write-Host ""
$resultados | Sort-Object ViewName | ForEach-Object {
    Write-Host ("  " + $_.ViewName.PadRight(50) + " <- " + $_.Controller + " L" + $_.Line)
}
