# Funcoes partilhadas - backup GDI-ERP-Plataform (enxuto / offline).
# Scripts podem ficar em qualquer pasta; caminhos sempre absolutos.
# Copiar para a pasta externa: common.ps1, enxuto.ps1, offline.ps1 e gdi-backup-repositorio.path

Set-StrictMode -Version Latest

$script:GdiBackupProjetoMarker = "GDI-ERP-Plataform.csproj"

function Resolve-GdiBackupFullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [switch]$MustExist
    )
    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "Caminho vazio."
    }
    $full = if ([System.IO.Path]::IsPathRooted($Path)) {
        [System.IO.Path]::GetFullPath($Path)
    } else {
        [System.IO.Path]::GetFullPath((Join-Path (Get-Location).Path $Path))
    }
    if ($MustExist -and -not (Test-Path -LiteralPath $full)) {
        throw "Caminho nao encontrado: $full"
    }
    return $full
}

function Find-GdiBackupRepoRootFromPath {
    param([Parameter(Mandatory = $true)][string]$StartPath)
    $current = Resolve-GdiBackupFullPath $StartPath -MustExist
    if (-not (Test-Path -LiteralPath $current -PathType Container)) {
        $current = Split-Path -Parent $current
    }
    for ($i = 0; $i -lt 25; $i++) {
        if ([string]::IsNullOrWhiteSpace($current)) { break }
        $marker = Join-Path $current $script:GdiBackupProjetoMarker
        if (Test-Path -LiteralPath $marker) {
            return $current
        }
        $parent = Split-Path -Parent $current
        if ($parent -eq $current) { break }
        $current = $parent
    }
    return $null
}

function Read-GdiBackupConfigLine {
    param([Parameter(Mandatory = $true)][string]$ConfigFilePath)
    if (-not (Test-Path -LiteralPath $ConfigFilePath)) { return $null }
    $line = (Get-Content -LiteralPath $ConfigFilePath -ErrorAction SilentlyContinue | Select-Object -First 1)
    if ($null -eq $line) { return $null }
    $line = $line.Trim()
    if ($line.Length -eq 0 -or $line.StartsWith("#")) { return $null }
    return $line
}

function Get-GdiBackupRepoRoot {
    param(
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$ScriptDirectory
    )
    $scriptDir = Resolve-GdiBackupFullPath $ScriptDirectory -MustExist

    if ($RepoRoot) {
        $resolved = Resolve-GdiBackupFullPath $RepoRoot -MustExist
        $marker = Join-Path $resolved $script:GdiBackupProjetoMarker
        if (-not (Test-Path -LiteralPath $marker)) {
            throw "RepoRoot invalido (sem $script:GdiBackupProjetoMarker): $resolved"
        }
        return $resolved
    }

    $configRepo = Join-Path $scriptDir "gdi-backup-repositorio.path"
    $fromFile = Read-GdiBackupConfigLine $configRepo
    if ($fromFile) {
        return Get-GdiBackupRepoRoot -RepoRoot $fromFile -ScriptDirectory $scriptDir
    }

    if ($env:GDI_ERP_REPO_ROOT) {
        return Get-GdiBackupRepoRoot -RepoRoot $env:GDI_ERP_REPO_ROOT -ScriptDirectory $scriptDir
    }

    foreach ($start in @($scriptDir, (Get-Location).Path)) {
        $found = Find-GdiBackupRepoRootFromPath $start
        if ($found) { return $found }
    }

    throw @"
Raiz do repositorio GDI-ERP-Plataform nao encontrada.
Defina uma das opcoes:
  1) Ficheiro (mesma pasta dos scripts): gdi-backup-repositorio.path
     (copie de gdi-backup-repositorio.path.example)
  2) Parametro -RepoRoot 'C:\Marcio\Projetos\GDI-ERP-Plataform'
  3) Variavel de ambiente GDI_ERP_REPO_ROOT
"@
}

function Get-GdiBackupDestinoBase {
    param(
        [string]$Destino,
        [Parameter(Mandatory = $true)]
        [string]$Profile,
        [Parameter(Mandatory = $true)]
        [string]$ScriptDirectory
    )
    $scriptDir = Resolve-GdiBackupFullPath $ScriptDirectory -MustExist
    $perfilFolder = $Profile.ToLowerInvariant()

    if ($Destino) {
        $resolved = Resolve-GdiBackupFullPath $Destino
        if ($resolved -match '\.zip$') {
            $parent = Split-Path -Parent $resolved
            if ([string]::IsNullOrWhiteSpace($parent)) {
                throw "Caminho ZIP invalido: $resolved"
            }
            return (New-Item -ItemType Directory -Force -Path $parent).FullName
        }
        return (New-Item -ItemType Directory -Force -Path $resolved).FullName
    }

    $configDest = Join-Path $scriptDir "gdi-backup-destino.path"
    $fromFile = Read-GdiBackupConfigLine $configDest
    if ($fromFile) {
        $base = Resolve-GdiBackupFullPath $fromFile
        return (New-Item -ItemType Directory -Force -Path (Join-Path $base $perfilFolder)).FullName
    }

    $defaultBase = Resolve-GdiBackupFullPath "C:\Marcio\Backups\GDI-ERP-Plataform"
    return (New-Item -ItemType Directory -Force -Path (Join-Path $defaultBase $perfilFolder)).FullName
}

function Get-GdiBackupExcludeDirs {
    param(
        [ValidateSet("Enxuto", "Offline")]
        [string]$Profile
    )
    $common = @(
        "bin", "obj", ".vs", "TestResults",
        "[Dd]ebug", "[Rr]elease", "x64", "x86", "bld", "Out",
        "publish", "app.publish", "PackageTmp",
        "_filestemp", "App_Data\Logs",
        "node_modules", "__pycache__",
        "MigrationBackup", "_UpgradeReport_Files",
        "BenchmarkDotNet.Artifacts", "TestResults",
        ".localhistory", ".vshistory", "agent-transcripts"
    )
    if ($Profile -eq "Enxuto") {
        $common += "packages"
    }
    return $common
}

function Get-GdiBackupExcludeFiles {
    return @(
        "*.user", "*.suo", "*.userosscache", "*.sln.docstates",
        "Thumbs.db", "Desktop.ini",
        "*.cache", "*.log", "*.binlog",
        ".DS_Store"
    )
}

function Invoke-GdiProjetoBackup {
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("Enxuto", "Offline")]
        [string]$Profile,

        [Parameter(Mandatory = $true)]
        [string]$ScriptDirectory,

        [string]$RepoRoot,
        [string]$Origem,
        [string]$Destino,
        [string]$ZipPath,

        [switch]$Mirror,
        [switch]$IncluirGit = $true,
        [switch]$SemZip,
        [switch]$ManterPasta
    )

    $ScriptDirectory = Resolve-GdiBackupFullPath $ScriptDirectory -MustExist

    if ($Origem) {
        $Origem = Resolve-GdiBackupFullPath $Origem -MustExist
        $marker = Join-Path $Origem $script:GdiBackupProjetoMarker
        if (-not (Test-Path -LiteralPath $marker)) {
            throw "Origem invalida (sem $script:GdiBackupProjetoMarker): $Origem"
        }
    } else {
        $Origem = Get-GdiBackupRepoRoot -RepoRoot $RepoRoot -ScriptDirectory $ScriptDirectory
    }

    $stamp = Get-Date -Format "yyyy-MM-dd_HHmm"
    $perfilSuffix = if ($Profile -eq "Enxuto") { "enxuto" } else { "completo" }
    $zipFileName = "$stamp-GDI-ERP-Plataform_$perfilSuffix.zip"

    $DestinoBase = Get-GdiBackupDestinoBase -Destino $Destino -Profile $Profile -ScriptDirectory $ScriptDirectory

    if ($ZipPath) {
        $zipPath = Resolve-GdiBackupFullPath $ZipPath
        $DestinoBase = (New-Item -ItemType Directory -Force -Path (Split-Path -Parent $zipPath)).FullName
        $zipFileName = Split-Path -Leaf $zipPath
    } elseif ($Destino -and (Resolve-GdiBackupFullPath $Destino) -match '\.zip$') {
        $zipPath = Resolve-GdiBackupFullPath $Destino
        $DestinoBase = (New-Item -ItemType Directory -Force -Path (Split-Path -Parent $zipPath)).FullName
        $zipFileName = Split-Path -Leaf $zipPath
    } else {
        $zipPath = Join-Path $DestinoBase $zipFileName
    }
    $zipPath = Resolve-GdiBackupFullPath $zipPath

    $stagingStamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
    $DestinoStaging = if ($SemZip) {
        Resolve-GdiBackupFullPath (Join-Path $DestinoBase $stagingStamp)
    } else {
        Resolve-GdiBackupFullPath (Join-Path $DestinoBase ".staging-$stagingStamp")
    }

    $excludeDirs = Get-GdiBackupExcludeDirs -Profile $Profile
    if (-not $IncluirGit) {
        $excludeDirs += ".git"
    }
    $excludeFiles = Get-GdiBackupExcludeFiles

    if ($DestinoStaging.StartsWith($Origem, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Warning "Destino dentro da origem do repo. Use pasta de backup externa (ex.: C:\Marcio\Backups\...)."
    }

    New-Item -ItemType Directory -Force -Path $DestinoStaging | Out-Null

    $logFile = Join-Path $DestinoStaging "robocopy.log"
    $roboArgs = @(
        $Origem,
        $DestinoStaging,
        "/E", "/R:2", "/W:5",
        "/NFL", "/NDL", "/NP",
        "/LOG+:$logFile",
        "/TEE"
    )
    if ($Mirror) { $roboArgs += "/MIR" }
    foreach ($d in $excludeDirs) { $roboArgs += "/XD"; $roboArgs += $d }
    foreach ($f in $excludeFiles) { $roboArgs += "/XF"; $roboArgs += $f }

    Write-Host "=== GDI-ERP-Plataform - Backup $Profile ===" -ForegroundColor Cyan
    Write-Host "Scripts: $ScriptDirectory"
    Write-Host "Origem : $Origem"
    Write-Host "Staging: $DestinoStaging"
    if (-not $SemZip) { Write-Host "ZIP    : $zipPath" }
    Write-Host "Modo   : $(if ($Mirror) { 'MIR' } else { '/E' })"
    Write-Host ""

    if ($PSCmdlet.ShouldProcess($DestinoStaging, "Robocopy backup $Profile")) {
        & robocopy @roboArgs
        $roboExit = $LASTEXITCODE
        if ($roboExit -ge 8) {
            Write-Warning "Robocopy codigo $roboExit (ver $logFile)"
        } else {
            Write-Host "Robocopy concluido (codigo $roboExit)." -ForegroundColor Green
        }
        $global:LASTEXITCODE = [Math]::Min($roboExit, 7)
    }

    $manifest = @{
        projeto          = "GDI-ERP-Plataform"
        perfil           = $Profile
        dataHora         = (Get-Date).ToString("o")
        stamp            = $stamp
        perfilSuffix     = $perfilSuffix
        scriptDirectory  = $ScriptDirectory
        origem           = $Origem
        destinoStaging   = $DestinoStaging
        destinoBase      = $DestinoBase
        zipPath          = $zipPath
        mirror           = [bool]$Mirror
        excluiPackages   = ($Profile -eq "Enxuto")
        incluiGit        = [bool]$IncluirGit
        pastasExcluidas  = $excludeDirs
        compactadoZip    = (-not $SemZip)
    }

    $fileCount = 0
    $totalBytes = 0L
    Get-ChildItem -LiteralPath $DestinoStaging -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
        $fileCount++
        $totalBytes += $_.Length
    }
    $manifest["ficheiros"] = $fileCount
    $manifest["tamanhoMB"] = [Math]::Round($totalBytes / 1MB, 2)

    $secretsPath = Join-Path $DestinoStaging "App_Data\Secrets"
    $manifest["secretsPresentes"] = @()
    if (Test-Path -LiteralPath $secretsPath) {
        @(
            "sql-server.local.config",
            "appSettings.local.config",
            "aws-s3.local.json",
            "aws-ses-smtp.local.json"
        ) | ForEach-Object {
            if (Test-Path -LiteralPath (Join-Path $secretsPath $_)) {
                $manifest["secretsPresentes"] += $_
            }
        }
    }

    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $DestinoStaging "backup-manifest.json") -Encoding UTF8

    $readmeLines = @(
        "# Backup GDI-ERP-Plataform - $Profile",
        "Origem (absoluto): $Origem",
        "ZIP: $zipPath",
        "",
        "## Restore",
        "1. Extrair ZIP para pasta de desenvolvimento.",
        '2. Confirmar App_Data\Secrets\.',
        "3. nuget restore (perfil enxuto) se necessario.",
        "4. Build Release no Visual Studio.",
        "5. Restaurar SQL Server (backup DBA)."
    )
    Set-Content -LiteralPath (Join-Path $DestinoStaging "RESTORE-LEIA-ME.txt") -Value ($readmeLines -join "`r`n") -Encoding UTF8

    $manifestSidecar = Resolve-GdiBackupFullPath (Join-Path $DestinoBase "backup-manifest-$stamp-$perfilSuffix.json")
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestSidecar -Encoding UTF8

    if (-not $SemZip) {
        if ($PSCmdlet.ShouldProcess($zipPath, "Compactar ZIP")) {
            if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
            Write-Host "A compactar ZIP..." -ForegroundColor Cyan
            $itemsToZip = Get-ChildItem -LiteralPath $DestinoStaging -Force
            Compress-Archive -LiteralPath ($itemsToZip.FullName) -DestinationPath $zipPath -CompressionLevel Optimal -Force
            $zipInfo = Get-Item -LiteralPath $zipPath
            $manifest["zipTamanhoMB"] = [Math]::Round($zipInfo.Length / 1MB, 2)
            $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestSidecar -Encoding UTF8
            Write-Host ("ZIP: {0} ({1} MB)" -f $zipPath, $manifest.zipTamanhoMB) -ForegroundColor Green
            if (-not $ManterPasta) {
                Remove-Item -LiteralPath $DestinoStaging -Recurse -Force
            }
        }
    }

    Write-Host "Manifesto: $manifestSidecar" -ForegroundColor Cyan
    return $manifest
}
