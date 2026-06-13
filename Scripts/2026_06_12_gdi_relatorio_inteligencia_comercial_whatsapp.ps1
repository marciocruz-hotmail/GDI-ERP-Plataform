# =============================================================================
# GDI - Disparo do Relatório de Inteligência Comercial WhatsApp (por vendedor)
# Agendamento sugerido: segunda a sexta, 08h00
# Destino: JobServer /api/JobServer/Run -> EnviarRelatorioInteligenciaComercialWhatsApp
#
# Destinatários: vendedores com report_inteligencia_comercial = true (envio ao telefone_1).
# Parameters (opcional): informe um celular DDI 55 para enviar TODOS os relatórios a esse
#                        número (modo homologação). Vazio = envio normal por vendedor.
#
# DEPLOY: copiar para C:\Scripts\GDI\ no servidor de producao
# REGISTRO: ver 2026_06_12_gdi_register_task_relatorio_inteligencia_comercial.ps1
# =============================================================================

param(
    [string]$Url        = "https://homologacao.aeroflightx.com/api/JobServer/Run",
    [string]$JobKey     = "72b7c8d74d61ac3457795c57bb9be",
    [string]$Parameters = ""
)

$LogDir  = "C:\Scripts\GDI"
$LogFile = "$LogDir\relatorio-inteligencia-comercial-whatsapp.log"

if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null }

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $ts   = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts] [$Level] $Message"
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
    Write-Host $line
}

Write-Log "Iniciando disparo do relatorio de inteligencia comercial WhatsApp | Url: $Url"

try {
    $body = @{
        Key        = $JobKey
        JobName    = "EnviarRelatorioInteligenciaComercialWhatsApp"
        Parameters = $Parameters
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
