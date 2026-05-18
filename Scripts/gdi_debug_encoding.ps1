$file = 'C:\Marcio\Projetos\GDI-ERP-Plataform\Areas\g\Views\Cidades\ModalCadastrarNovaCidade.cshtml'
$bytes = [System.IO.File]::ReadAllBytes($file)
Write-Host ("BOM bytes: " + $bytes[0].ToString("X2") + " " + $bytes[1].ToString("X2") + " " + $bytes[2].ToString("X2"))

# Try both encodings
$contentUtf8 = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)
$contentDef  = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::Default)

$lines8 = $contentUtf8 -split "`r?`n"
foreach ($l in $lines8) {
    if ($l -match "fa-solid") {
        Write-Host ("UTF8 LINE: [" + $l + "]")
        # show hex of ç char
        $chars = $l.ToCharArray()
        $hexSeq = ""
        foreach ($ch in $chars) { $hexSeq += [int]$ch | ForEach-Object { $_.ToString("X4") } ; $hexSeq += " " }
        Write-Host ("HEX: " + $hexSeq.Substring(0, [Math]::Min(300, $hexSeq.Length)))
    }
}
