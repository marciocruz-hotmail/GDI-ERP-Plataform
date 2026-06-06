# GDI-ERP — Normaliza Areas/*/Views: CRLF (Windows), linhas em branco excessivas.
param(
    [string[]]$Root = @(
        (Join-Path $PSScriptRoot "..\Areas\g\Views")
    )
)

$utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-GdiViewText {
    param([string]$Text)

    $normalized = $Text -replace "`r`n", "`n" -replace "`r", "`n"
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($normalized -split "`n", -1)) {
        if ($lines.Count -eq 0 -and $line.Length -eq 0) { continue }
        $lines.Add($line)
    }
    while ($lines.Count -gt 0 -and $lines[$lines.Count - 1].Length -eq 0) {
        $lines.RemoveAt($lines.Count - 1)
    }

    $emptyCount = 0
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { $emptyCount++ }
    }
    $emptyPct = if ($lines.Count -gt 0) { [double]$emptyCount / $lines.Count } else { 0 }

    if ($emptyPct -gt 0.30) {
        $deduped = [System.Collections.Generic.List[string]]::new()
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if ([string]::IsNullOrWhiteSpace($line)) {
                $prev = if ($i -gt 0) { $lines[$i - 1] } else { $null }
                $next = if ($i + 1 -lt $lines.Count) { $lines[$i + 1] } else { $null }
                $prevBlank = $deduped.Count -gt 0 -and [string]::IsNullOrWhiteSpace($deduped[$deduped.Count - 1])
                if (-not [string]::IsNullOrWhiteSpace($prev) -and -not [string]::IsNullOrWhiteSpace($next)) {
                    if ($prevBlank) { continue }
                    if ($prev -match '(</div>|</script>|\);\s*)$') {
                        $deduped.Add($line)
                        continue
                    }
                    continue
                }
            }
            $deduped.Add($line)
        }
        $lines = $deduped
    }

    $out = New-Object System.Collections.Generic.List[string]
    $blankRun = 0
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            $blankRun++
            if ($blankRun -le 1) { $out.Add("") }
        }
        else {
            $blankRun = 0
            $out.Add($line)
        }
    }

    return (($out -join "`r`n") + "`r`n")
}

function Normalize-GdiViewsFolder {
    param([string]$FolderRoot)

    $resolved = (Resolve-Path -LiteralPath $FolderRoot).Path
    $changed = New-Object System.Collections.Generic.List[string]
    $total = 0

    Get-ChildItem -Path $resolved -Recurse -File | ForEach-Object {
        $total++
        $path = $_.FullName
        $origBytes = [System.IO.File]::ReadAllBytes($path)
        $orig = [System.Text.Encoding]::UTF8.GetString($origBytes)
        $new = Normalize-GdiViewText $orig
        $newBytes = $utf8NoBom.GetBytes($new)
        $same = ($origBytes.Length -eq $newBytes.Length)
        if ($same) {
            for ($i = 0; $i -lt $origBytes.Length; $i++) {
                if ($origBytes[$i] -ne $newBytes[$i]) { $same = $false; break }
            }
        }
        if (-not $same) {
            [System.IO.File]::WriteAllText($path, $new, $utf8NoBom)
            $rel = $path.Substring($resolved.Length).TrimStart('\')
            $changed.Add($rel)
        }
    }

    return [PSCustomObject]@{
        Root    = $resolved
        Total   = $total
        Changed = $changed.Count
        Files   = $changed
    }
}

$results = New-Object System.Collections.Generic.List[object]
foreach ($r in $Root) {
    if (-not (Test-Path -LiteralPath $r)) {
        Write-Warning "Pasta não encontrada: $r"
        continue
    }
    $results.Add((Normalize-GdiViewsFolder $r))
}

$grandTotal = 0
$grandChanged = 0
foreach ($res in $results) {
    $grandTotal += $res.Total
    $grandChanged += $res.Changed
    Write-Output ""
    Write-Output "=== $($res.Root) ==="
    Write-Output "Ficheiros: $($res.Total) | Alterados: $($res.Changed)"
    $res.Files | ForEach-Object { Write-Output "  $_" }
}

Write-Output ""
Write-Output "TOTAL: $grandTotal ficheiros, $grandChanged alterados"
