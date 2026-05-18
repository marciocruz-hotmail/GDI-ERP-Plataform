# Pass 1b — variantes remanescentes com <i> sem &nbsp ou com wrapper <p>
# Padrao A: title: "<i class='fa-solid ...'></i><b>TEXT</b>"  (sem &nbsp)
# Padrao B: title: "<p ...><i class='fa-solid ...'></i><b>TEXT</b></p>"
$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$count = 0

# Regex A: <i[^>]+></i><b>TEXT</b>
$patternA = "title: ""<i[^>]+></i><b>([^<]+)</b>"""
$replacementA = 'title: "$1"'

# Regex B: <p[^>]*><i[^>]+></i><b>TEXT</b></p>
$patternB = "title: ""<p[^>]*><i[^>]+></i><b>([^<]+)</b></p>"""
$replacementB = 'title: "$1"'

Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $c = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    $new = [regex]::Replace($c, $patternA, $replacementA)
    $new = [regex]::Replace($new, $patternB, $replacementB)
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($_.FullName, $new, [System.Text.Encoding]::UTF8)
        $count++
        Write-Host $_.FullName
    }
}
Write-Host "Pass 1b completo: $count arquivos alterados"
