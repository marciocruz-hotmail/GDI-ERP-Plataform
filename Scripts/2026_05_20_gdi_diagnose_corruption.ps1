$path = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas\gc\Controllers\ComexFinanceiroController.cs"
$bytes = [System.IO.File]::ReadAllBytes($path)

# Find "iewBag" and show surrounding bytes
$target = [System.Text.Encoding]::UTF8.GetBytes("iewBag")
for ($i = 0; $i -lt $bytes.Length - $target.Length; $i++) {
    $match = $true
    for ($j = 0; $j -lt $target.Length; $j++) {
        if ($bytes[$i + $j] -ne $target[$j]) { $match = $false; break }
    }
    if ($match) {
        $from = [Math]::Max(0, $i - 4)
        $to   = [Math]::Min($bytes.Length - 1, $i + $target.Length + 3)
        $hexStr = ""
        $chrStr = ""
        for ($k = $from; $k -le $to; $k++) {
            $hexStr += "0x" + $bytes[$k].ToString("X2") + " "
            $c = [char]$bytes[$k]
            $chrStr += if ($bytes[$k] -ge 0x20 -and $bytes[$k] -lt 0x7F) { $c } else { "." }
        }
        Write-Host "Pos $i : $hexStr"
        Write-Host "        $chrStr"
        break
    }
}

# Check first occurrence of byte 0x56 (V) in the file
$first56 = -1
for ($i = 0; $i -lt $bytes.Length; $i++) {
    if ($bytes[$i] -eq 0x56) { $first56 = $i; break }
}
Write-Host "First 0x56 (V) byte at: $first56"
if ($first56 -ge 0) {
    $ctx = [System.Text.Encoding]::UTF8.GetString($bytes, [Math]::Max(0,$first56-5), 20)
    Write-Host "  Context: $ctx"
}

# Count total 0x56 vs 0x69 bytes
$count56 = 0; $count69 = 0
foreach ($b in $bytes) {
    if ($b -eq 0x56) { $count56++ }
    if ($b -eq 0x69) { $count69++ }
}
Write-Host "Total 0x56 (V): $count56"
Write-Host "Total 0x69 (i): $count69"

# Show BOM
Write-Host "First 4 bytes: 0x$($bytes[0].ToString('X2')) 0x$($bytes[1].ToString('X2')) 0x$($bytes[2].ToString('X2')) 0x$($bytes[3].ToString('X2'))"
