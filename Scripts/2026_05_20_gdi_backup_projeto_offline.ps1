#Requires -Version 5.1
<#
.SYNOPSIS
  Backup offline GDI-ERP-Plataform (com packages) -> ZIP yyyy-MM-dd_HHmm-GDI-ERP-Plataform_completo.zip

.DESCRIPTION
  Pode executar de qualquer pasta. Caminhos sempre absolutos.
  Na pasta dos scripts: gdi-backup-repositorio.path (ver .example).

.PARAMETER RepoRoot
  Caminho absoluto da raiz do repo.

.PARAMETER Origem
  Alias de RepoRoot.

.PARAMETER Destino
  Pasta absoluta ou caminho absoluto do .zip.

.PARAMETER ZipPath
  Caminho absoluto completo do .zip.

.EXAMPLE
  & "C:\Tools\GDI-Backup\2026_05_20_gdi_backup_projeto_offline.ps1"
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$RepoRoot,
    [string]$Origem,
    [string]$Destino,
    [string]$ZipPath,
    [switch]$Mirror,
    [switch]$SemGit,
    [switch]$SemZip,
    [switch]$ManterPasta
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
. (Join-Path $ScriptDir "2026_05_20_gdi_backup_projeto_common.ps1")

$repo = if ($Origem) { $Origem } else { $RepoRoot }
$null = Invoke-GdiProjetoBackup `
    -Profile Offline `
    -ScriptDirectory $ScriptDir `
    -RepoRoot $repo `
    -Destino $Destino `
    -ZipPath $ZipPath `
    -Mirror:$Mirror `
    -IncluirGit:(-not $SemGit) `
    -SemZip:$SemZip `
    -ManterPasta:$ManterPasta
