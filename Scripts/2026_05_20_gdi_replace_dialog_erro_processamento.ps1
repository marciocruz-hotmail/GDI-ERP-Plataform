# Substitui LibMessageDialog com title HTML "Erro no Processamento" (botao unico OK)
# por LibMessageError("Erro no Processamento", MSG, { callback: function () { ... } })
# Usa contagem de braces para localizar o bloco completo.

$root  = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$totalOcorrencias = 0
$totalArquivos    = 0

Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $path  = $_.FullName
    $raw   = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $nl    = if ($raw -match "`r`n") { "`r`n" } else { "`n" }
    $lines = $raw -split "`r?`n"

    $out     = [System.Collections.Generic.List[string]]::new()
    $changed = $false
    $i       = 0

    while ($i -lt $lines.Count) {
        $line = $lines[$i]

        # Detecta LibMessageDialog({ com titulo Erro no Processamento na linha seguinte
        if (($line -match '^(\s+)LibMessageDialog\(\{') -and
            ($i + 1 -lt $lines.Count) -and
            ($lines[$i + 1] -match "<b>Erro no Processamento</b>")) {

            $indent = $Matches[1]   # indentacao do LibMessageDialog
            $i4     = $indent + '    '

            # Extrai expressao da mensagem varrendo as proximas linhas
            $msgExpr = $null
            for ($k = $i + 1; $k -lt [Math]::Min($i + 10, $lines.Count); $k++) {
                if ($lines[$k] -match '^\s+message:\s*([^,\r\n]+),') {
                    $msgExpr = $Matches[1].Trim()
                    break
                }
            }

            # Encontra fim do bloco via contagem de braces (comeca apos a linha LibMessageDialog({)
            $braceCount = 1
            $endIdx     = -1
            for ($k = $i + 1; $k -lt $lines.Count; $k++) {
                foreach ($ch in $lines[$k].ToCharArray()) {
                    if ($ch -eq '{') { $braceCount++ }
                    if ($ch -eq '}') {
                        $braceCount--
                        if ($braceCount -eq 0) { $endIdx = $k; break }
                    }
                }
                if ($endIdx -ge 0) { break }
            }

            if ($msgExpr -ne $null -and $endIdx -gt 0) {
                # Gera substituicao
                $out.Add($indent + 'LibMessageError("Erro no Processamento", ' + $msgExpr + ', {')
                $out.Add($i4    + 'callback: function ()')
                $out.Add($i4    + '{')
                $out.Add($i4    + "    document.querySelectorAll('.modal').forEach(function(m){var i=bootstrap.Modal.getInstance(m);if(i)i.hide();});")
                $out.Add($i4    + '    LibMessageHideAll();')
                $out.Add($i4    + '}')
                $out.Add($indent + '})')
                $i = $endIdx + 1
                $changed = $true
                $script:totalOcorrencias++
                continue
            }
        }

        $out.Add($line)
        $i++
    }

    if ($changed) {
        # Preserva terminador de linha original; nao adiciona \n extra no final
        $novo = $out -join $nl
        # Preserva ausencia/presenca de newline final do arquivo original
        if ($raw -match "(`r?`n)$") { $novo = $novo + $nl }
        [System.IO.File]::WriteAllText($path, $novo, [System.Text.Encoding]::UTF8)
        $script:totalArquivos++
        Write-Host ("  ALTERADO: " + $_.Name)
    }
}

Write-Host ""
Write-Host "=== Concluido: $totalOcorrencias substituicoes em $totalArquivos arquivo(s) ==="
