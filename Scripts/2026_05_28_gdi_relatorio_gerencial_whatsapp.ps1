# =============================================================================
# GDI - Disparo do Relatório Gerencial WhatsApp
# Agendamento: segunda a sexta, 17h00
# Destino: JobServer /api/JobServer/Run -> EnviarResumoGerencialWhatsApp
#
# DEPLOY: copiar para C:\Scripts\GDI\ no servidor de producao
# REGISTRO: ver 2026_05_28_gdi_register_task_relatorio_gerencial.ps1
# =============================================================================

param(
    [string]$Url    = "https://aeroflightx.com/api/JobServer/Run",
    [string]$JobKey = "72b7c8d74d61ac3457795c57bb9be"
)

$LogDir  = "C:\Scripts\GDI"
$LogFile = "$LogDir\relatorio-gerencial-whatsapp.log"

if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $ts   = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts] [$Level] $Message"
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
    Write-Host $line
}

Write-Log "Iniciando disparo do relatorio gerencial WhatsApp | Url: $Url"

try {
    $body = @{
        Key        = $JobKey
        JobName    = "EnviarResumoGerencialWhatsApp"
        Parameters = ""
    } | ConvertTo-Json -Compress

    # Ignora certificado autoassinado em ambiente local/dev se necessario
    [Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    $response = Invoke-RestMethod `
        -Uri         $Url `
        -Method      Post `
        -Body        $body `
        -ContentType "application/json" `
        -ErrorAction Stop

    Write-Log "Resposta recebida | JobId: $($response.jobId) | Mensagem: $($response.message)"
}
catch {
    Write-Log "ERRO ao chamar JobServer: $_" "ERROR"
    exit 1
}
