$path = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas\g\Controllers\FinanceiroController.cs"

# Check BOM
$bytes = [System.IO.File]::ReadAllBytes($path)
Write-Host "First 4 bytes: $($bytes[0]) $($bytes[1]) $($bytes[2]) $($bytes[3])"
if ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { Write-Host "  => UTF-8 BOM" }
elseif ($bytes[0] -eq 0xFF -and $bytes[1] -eq 0xFE) { Write-Host "  => UTF-16 LE BOM" }
else { Write-Host "  => No BOM - probably Windows-1252" }

# Bytes at "Baixar Títulos" location
$utf8 = [System.Text.Encoding]::UTF8.GetString($bytes)
$idx = $utf8.IndexOf('Baixar T')
if ($idx -ge 0) {
    $byteIdx = [System.Text.Encoding]::UTF8.GetByteCount($utf8.Substring(0, $idx)) + 8  # skip 'Baixar T'
    Write-Host "Byte after 'Baixar T': 0x$($bytes[$byteIdx].ToString('X2'))"
}

# Try Windows-1252 read + Replace
$cp1252 = [System.Text.Encoding]::GetEncoding(1252)
$raw1252 = [System.IO.File]::ReadAllText($path, $cp1252)
$target = 'ViewBag.Title = "Baixar T' + [char]0xED + 'tulos";'
Write-Host "1252 contains target: $($raw1252.Contains($target))"
# Check actual code point of char in file
$idx2 = $raw1252.IndexOf('Baixar T')
if ($idx2 -ge 0) {
    $charCode = [int]$raw1252[$idx2 + 8]
    Write-Host "Code point of char after 'Baixar T' in 1252 read: U+$($charCode.ToString('X4'))"
}

# Test Replace with Windows-1252
$oldStr = 'ViewBag.Title = "Baixar T' + [char]0xED + 'tulos";'
$newStr = 'ViewBag.Title = REPLACED;'
$result = $raw1252.Replace($oldStr, $newStr)
Write-Host "Replace worked: $($result -ne $raw1252)"

# Test with PS literal (UTF-8 í)
$psLiteral = 'ViewBag.Title = "Baixar Títulos";'
$result2 = $raw1252.Replace($psLiteral, 'REPLACED2')
Write-Host "Replace with PS literal worked: $($result2 -ne $raw1252)"
$charCodeI = [int]('í'[0])
Write-Host "Code point of 'í' in PS script: U+$($charCodeI.ToString('X4'))"
