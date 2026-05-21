# Valida Web.Release.config (PUB-1): debug removido, customErrors On, PERF-012.
# Uso: .\Scripts\2026_05_20_gdi_verify_web_release_transform.ps1
# Requer MSBuild (Visual Studio).

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$csproj = Join-Path $root 'GDI-ERP-Plataform.csproj'
$transformed = Join-Path $root 'obj\Release\TransformWebConfig\transformed\Web.config'

$msbuild = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $msbuild) {
    Write-Error 'MSBuild nao encontrado. Abra o projeto no Visual Studio ou instale Build Tools.'
}

& $msbuild $csproj /p:Configuration=Release /t:TransformWebConfig /v:minimal | Out-Host
if (-not (Test-Path $transformed)) {
    Write-Error "Transform falhou: $transformed nao existe."
}

[xml]$xml = Get-Content -LiteralPath $transformed -Encoding UTF8
$compilation = $xml.configuration.'system.web'.compilation
$customErrors = $xml.configuration.'system.web'.customErrors
$fail = $false

if ($compilation.debug -eq 'true') {
    Write-Host 'FAIL: compilation debug=true no Web.config transformado' -ForegroundColor Red
    $fail = $true
}
else {
    Write-Host 'OK: compilation sem debug=true' -ForegroundColor Green
}

if ($customErrors.mode -ne 'On') {
    Write-Host "FAIL: customErrors mode=$($customErrors.mode) (esperado On)" -ForegroundColor Red
    $fail = $true
}
else {
    Write-Host 'OK: customErrors mode=On' -ForegroundColor Green
}

$urlCompression = $xml.configuration.'system.webServer'.urlCompression
if ($urlCompression -and $urlCompression.doStaticCompression -eq 'true') {
    Write-Host 'OK: urlCompression estatica ativa (PERF-012)' -ForegroundColor Green
}
else {
    Write-Host 'WARN: urlCompression nao encontrado no transformado' -ForegroundColor Yellow
}

if ($fail) { exit 1 }
Write-Host "Transform validado: $transformed"
exit 0
