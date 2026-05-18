# Pass 1 — strip HTML do title (todos os icones fa-solid)
# Padrao: title: "<i class='fa-solid ...' style='color:...'>&nbsp&nbsp</i><b>TEXTO</b>"
# Resultado: title: "TEXTO"
# Nota: sem chars especiais portugueses no padrao — extrai o texto do <b>...</b>
$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"
$count = 0

$pattern = "title: ""<i class='fa-solid [^']+' style='color:[^']+'>&nbsp(&nbsp)*</i><b>([^<]+)</b>"""
# Captura grupo 2 = texto dentro do <b>
$replacement = 'title: "$2"'

Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | ForEach-Object {
    $c = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    $new = [regex]::Replace($c, $pattern, $replacement)
    if ($new -ne $c) {
        [System.IO.File]::WriteAllText($_.FullName, $new, [System.Text.Encoding]::UTF8)
        $count++
        Write-Host $_.FullName
    }
}
Write-Host "Pass 1 completo: $count arquivos alterados"
