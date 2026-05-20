# Substitui yesMessageAlertSmall e yesMessageInfo pelas chamadas padrao LibMessageAlert
# yesMessageAlertSmall(T, M)   -> LibMessageAlert(T, M, { size: 'small' })
# yesMessageInfo(T, M)         -> LibMessageAlert(T, M, { icon: 'info'  })
# Escopo: Areas/ e start.js (exceto definicoes das funcoes — tratadas em separado)

$root    = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$startJs = "C:\Marcio\Projetos\GDI-ERP-Plataform\LibUI_AdminLTE-4.0.0\plugins\startprime\js\start.js"

$reSmall = [regex]'yesMessageAlertSmall\(("[^"]+"),\s*("[^"]+")\)'
$reInfo  = [regex]'yesMessageInfo\(("[^"]+"),\s*([^);\r\n]+)\)'

$totalSmall = 0
$totalInfo  = 0
$filesChanged = 0

function Process-File($path) {
    $c = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

    $new = $reSmall.Replace($c, {
        param($m)
        'LibMessageAlert(' + $m.Groups[1].Value + ', ' + $m.Groups[2].Value + ", { size: 'small' })"
    })
    $cntSmall = $reSmall.Matches($c).Count

    $new2 = $reInfo.Replace($new, {
        param($m)
        'LibMessageAlert(' + $m.Groups[1].Value + ', ' + $m.Groups[2].Value.TrimEnd() + ", { icon: 'info' })"
    })
    $cntInfo = $reInfo.Matches($new).Count

    if ($new2 -ne $c) {
        [System.IO.File]::WriteAllText($path, $new2, [System.Text.Encoding]::UTF8)
        $script:totalSmall += $cntSmall
        $script:totalInfo  += $cntInfo
        $script:filesChanged++
        Write-Host ("  small=$cntSmall info=$cntInfo : " + $path)
    }
}

Write-Host "=== Areas/ ==="
Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object { Process-File $_.FullName }

Write-Host ""
Write-Host "=== start.js (apenas chamadas, nao definicoes) ==="
Process-File $startJs

Write-Host ""
Write-Host "=== Concluido: small=$totalSmall info=$totalInfo substituicoes em $filesChanged arquivo(s) ==="
