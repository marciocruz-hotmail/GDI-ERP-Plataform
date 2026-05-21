# Smoke PUB-2 (parcial automatizado): health + versao no login.
# Uso: .\Scripts\2026_05_20_gdi_smoke_health_login.ps1 -BaseUrl https://localhost:44300
# Ajuste BaseUrl ao site local IIS Express / IIS.

param(
    [string]$BaseUrl = 'https://localhost:44300'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$cv = Get-Content (Join-Path $root 'ControlVersion.cs') -Raw
$expectedVersion = '?'
if ($cv -match 'getShortVersion\(\)[\s\S]*?return\s+"([^"]+)"') {
    $expectedVersion = $Matches[1]
}

$healthUrl = ($BaseUrl.TrimEnd('/')) + '/health'
Write-Host "GET $healthUrl (versao esperada: $expectedVersion)"

try {
    $r = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 30
    if ($r.StatusCode -ne 200) {
        Write-Error "Health HTTP $($r.StatusCode)"
    }
    $json = $r.Content | ConvertFrom-Json
    if (-not $json.ok) { Write-Error 'Health ok=false' }
    if ($json.version -ne $expectedVersion) {
        Write-Error "Versao health=$($json.version) != ControlVersion=$expectedVersion"
    }
    Write-Host "OK health: version=$($json.version) utc=$($json.utc)" -ForegroundColor Green
}
catch {
    Write-Host "FAIL health: $_" -ForegroundColor Red
    Write-Host 'Manual: login UserIdentity/Index — footer/navbar deve exibir versao' + $expectedVersion
    exit 1
}

$loginUrl = ($BaseUrl.TrimEnd('/')) + '/UserIdentity/Index'
try {
    $login = Invoke-WebRequest -Uri $loginUrl -UseBasicParsing -TimeoutSec 30
    if ($login.Content -notmatch [regex]::Escape($expectedVersion)) {
        Write-Host "WARN: versao $expectedVersion nao encontrada no HTML do login (cache ou layout sem ?v=)" -ForegroundColor Yellow
    }
    else {
        Write-Host "OK login HTML contem versao $expectedVersion" -ForegroundColor Green
    }
}
catch {
    Write-Host "WARN login nao testado: $_" -ForegroundColor Yellow
}

Write-Host @'

Smoke manual (navbar apos login):
  1. Login com utilizador de teste
  2. Confirmar menu lateral e overlay LibMessageProcessando em link MVC
  3. DevTools Network: start.js e start.css com ?v=2026.51.03

'@

exit 0
