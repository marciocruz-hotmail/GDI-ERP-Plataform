# Corrige indentacao das chamadas LibMessageError("Erro no Processamento", ...)
# que ficaram sem indent apos a substituicao do LibMessageDialog

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$totalOcorrencias = 0
$totalArquivos = 0

Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $path = $_.FullName
    $lines = [System.IO.File]::ReadAllLines($path)
    $changed = $false

    for ($i = 0; $i -lt $lines.Count; $i++) {
        # Detecta LibMessageError no inicio da linha (sem indent)
        if ($lines[$i] -match '^LibMessageError\("Erro no Processamento"') {
            # Determina indent a partir da linha anterior (ex: else { ou if { )
            $indent = ""
            for ($k = $i - 1; $k -ge [Math]::Max(0, $i - 5); $k--) {
                if ($lines[$k] -match '^(\s+)\S') {
                    $indent = $Matches[1]
                    break
                }
            }
            if ($indent -eq "") { continue }  # nao ha como determinar indent

            # Aplica indent as linhas do bloco (LibMessageError ate })
            $j = $i
            while ($j -lt $lines.Count) {
                if ($lines[$j] -notmatch '^\s') {
                    $lines[$j] = $indent + $lines[$j]
                }
                if ($lines[$j].TrimEnd() -eq "})") { break }
                $j++
            }
            $changed = $true
            $script:totalOcorrencias++
            $i = $j  # pula bloco ja processado
        }
    }

    if ($changed) {
        $raw = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
        $nl = if ($raw -match "`r`n") { "`r`n" } else { "`n" }
        $novo = $lines -join $nl
        if ($raw -match "(`r?`n)$") { $novo = $novo + $nl }
        [System.IO.File]::WriteAllText($path, $novo, [System.Text.Encoding]::UTF8)
        $script:totalArquivos++
        Write-Host ("  CORRIGIDO: " + $_.Name)
    }
}

Write-Host ""
Write-Host "=== Concluido: $totalOcorrencias blocos reindentados em $totalArquivos arquivo(s) ==="
