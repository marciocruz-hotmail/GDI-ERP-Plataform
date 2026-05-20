# Reescreve com indent correto os blocos LibMessageError("Erro no Processamento", ...)
# onde o callback body ficou com indent relativo errado (4 espacos em vez de indent+4)

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$totalOcorrencias = 0
$totalArquivos = 0

Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $path  = $_.FullName
    $raw   = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $nl    = if ($raw -match "`r`n") { "`r`n" } else { "`n" }
    $lines = [System.Collections.Generic.List[string]]($raw -split "`r?`n")
    $changed = $false

    $i = 0
    while ($i -lt $lines.Count) {
        $line = $lines[$i]

        # Detecta: {INDENT}LibMessageError("Erro no Processamento", ...MSG..., {
        if ($line -match '^(\s+)LibMessageError\("Erro no Processamento"') {
            $indent = $Matches[1]  # ex: "                    " (20 spaces)
            $i4     = $indent + '    '   # 24 spaces
            $i8     = $indent + '        ' # 28 spaces

            # Verifica que a linha seguinte e o callback com indent errado
            # (se ja estiver correto, pula)
            if (($i + 1 -lt $lines.Count) -and
                ($lines[$i + 1] -match '^\s+callback: function')) {
                $cbIndent = if ($lines[$i + 1] -match '^(\s+)') { $Matches[1] } else { "" }
                if ($cbIndent -eq $i4) {
                    # Ja correto, pula
                    $i++
                    continue
                }
            }

            # Esperamos exatamente 6 linhas de callback apos LibMessageError:
            #  i+1: {??}    callback: function ()
            #  i+2: {??}    {
            #  i+3: {??}        document.querySelectorAll(...)...
            #  i+4: {??}        LibMessageHideAll();
            #  i+5: {??}    }
            #  i+6: {INDENT}})
            if ($i + 6 -lt $lines.Count) {
                $l1 = $lines[$i + 1]
                $l2 = $lines[$i + 2]
                $l3 = $lines[$i + 3]
                $l4 = $lines[$i + 4]
                $l5 = $lines[$i + 5]
                $l6 = $lines[$i + 6]

                if (($l1.TrimStart() -eq 'callback: function ()') -and
                    ($l2.TrimStart() -eq '{') -and
                    ($l3 -match "querySelectorAll") -and
                    ($l4.TrimStart() -eq 'LibMessageHideAll();') -and
                    ($l5.TrimStart() -eq '}') -and
                    ($l6.TrimStart() -eq '})')) {

                    $lines[$i + 1] = $i4    + 'callback: function ()'
                    $lines[$i + 2] = $i4    + '{'
                    $lines[$i + 3] = $i8    + "document.querySelectorAll('.modal').forEach(function(m){var i=bootstrap.Modal.getInstance(m);if(i)i.hide();});"
                    $lines[$i + 4] = $i8    + 'LibMessageHideAll();'
                    $lines[$i + 5] = $i4    + '}'
                    $lines[$i + 6] = $indent + '})'
                    $changed = $true
                    $script:totalOcorrencias++
                    $i += 7
                    continue
                }
            }
        }
        $i++
    }

    if ($changed) {
        $novo = $lines -join $nl
        if ($raw -match "(`r?`n)$") { $novo = $novo + $nl }
        [System.IO.File]::WriteAllText($path, $novo, [System.Text.Encoding]::UTF8)
        $script:totalArquivos++
        Write-Host ("  CORRIGIDO: " + $_.Name)
    }
}

Write-Host ""
Write-Host "=== Concluido: $totalOcorrencias blocos corrigidos em $totalArquivos arquivo(s) ==="
