$path = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas\g\Controllers\FinanceiroController.cs"

# Try UTF-8
$rawUtf8 = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
$hasMatch = $rawUtf8 -match 'Baixar T.tulos'
Write-Host "UTF-8 match (regex): $hasMatch"

# Try default
$rawDefault = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::Default)
$hasMatch2 = $rawDefault -match 'Baixar T.tulos'
Write-Host "Default match (regex): $hasMatch2"

# Check literal
$literal = 'ViewBag.Title = "Baixar T'
Write-Host "UTF-8 contains 'ViewBag.Title = Baixar T': $($rawUtf8.Contains($literal))"
Write-Host "Default contains 'ViewBag.Title = Baixar T': $($rawDefault.Contains($literal))"

# Show the actual bytes around that area
$bytes = [System.IO.File]::ReadAllBytes($path)
$txt = [System.Text.Encoding]::UTF8.GetString($bytes)
$idx = $txt.IndexOf('Baixar T')
if ($idx -ge 0) {
    Write-Host "Found at UTF-8 index: $idx"
    Write-Host "Surrounding text: $($txt.Substring([Math]::Max(0,$idx-20), 60))"
} else {
    Write-Host "NOT found in UTF-8 decoded text"
}

# Also try UTF-8 with BOM detection
$reader = New-Object System.IO.StreamReader($path, $true)
$content = $reader.ReadToEnd()
$reader.Close()
$hasMatchAuto = $content -match 'Baixar T.tulos'
Write-Host "Auto-detect encoding match: $hasMatchAuto"
$hasTitulos = $content.Contains('Baixar T')
Write-Host "Auto-detect contains 'Baixar T': $hasTitulos"
