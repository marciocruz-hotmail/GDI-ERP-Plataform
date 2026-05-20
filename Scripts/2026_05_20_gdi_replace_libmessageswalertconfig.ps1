# Substitui LibMessageSwalAlertConfig por LibMessageSuccess nas views
# Padrao exato (4 linhas eliminadas, callback preservado):
#   LibMessageSwalAlertConfig({
#       title: "...",
#       centerVertical: true,
#       message: EXPR,
#       callback: function ()
# ->
#   LibMessageSuccess("...", EXPR, {
#       callback: function ()

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$count = 0
$hits  = 0

$re = [regex]'(?m)^(\s*)LibMessageSwalAlertConfig\(\{\r?\n\s+title: "([^"]+)",\r?\n\s+centerVertical: true,\r?\n\s+message: ([^,\r\n]+),\r?\n(\s+callback: function \(\))'

Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $path = $_.FullName
    $c    = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

    $new = $re.Replace($c, {
        param($m)
        # Detecta linha-final usada no arquivo
        $nl = if ($m.Value -match "`r`n") { "`r`n" } else { "`n" }
        $callIndent = $m.Groups[1].Value   # indentacao da chamada
        $title      = $m.Groups[2].Value   # texto do title
        $msgExpr    = $m.Groups[4].Value   # "    callback: function ()" incluindo indentacao
        $msgVal     = $m.Groups[3].Value   # expressao do message (ex: result.msg)

        $callIndent + 'LibMessageSuccess("' + $title + '", ' + $msgVal + ', {' + $nl + $msgExpr
    })

    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($path, $new, [System.Text.Encoding]::UTF8)
        $matchCount = $re.Matches($c).Count
        $hits += $matchCount
        $count++
        Write-Host ("$matchCount ocorrencia(s): " + $path)
    }
}
Write-Host ""
Write-Host "=== Concluido: $hits substituicoes em $count arquivos ==="
