# =============================================================================
# GDI - Registra a tarefa agendada no Windows Task Scheduler
# Executar UMA ÚNICA VEZ no servidor de producao com privilégios de Administrador
#
# Pre-requisito: copiar 2026_05_28_gdi_relatorio_gerencial_whatsapp.ps1
#                para C:\Scripts\GDI\ no servidor de producao.
# URL de producao (param -Url do script de disparo): https://aeroflightx.com/api/JobServer/Run
# =============================================================================

$TaskFolder  = "GDI"
$TaskName    = "RelatorioGerencialWhatsApp"
$ScriptPath  = "C:\Scripts\GDI\2026_05_28_gdi_relatorio_gerencial_whatsapp.ps1"
$Horario     = "17:00"
$DiasUteis   = @("Monday","Tuesday","Wednesday","Thursday","Friday")

# --- Ação ---
$action = New-ScheduledTaskAction `
    -Execute  "powershell.exe" `
    -Argument "-NonInteractive -ExecutionPolicy Bypass -File `"$ScriptPath`""

# --- Trigger: semanal, segunda a sexta, 17:00 ---
$trigger = New-ScheduledTaskTrigger `
    -Weekly `
    -DaysOfWeek $DiasUteis `
    -At $Horario

# --- Configurações ---
$settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 5) `
    -RestartCount 1 `
    -RestartInterval (New-TimeSpan -Minutes 2) `
    -StartWhenAvailable `
    -RunOnlyIfNetworkAvailable

# --- Criar pasta GDI no Task Scheduler (se nao existir) ---
$scheduler = New-Object -ComObject "Schedule.Service"
$scheduler.Connect()
$rootFolder = $scheduler.GetFolder("\")
try {
    $rootFolder.GetFolder($TaskFolder) | Out-Null
} catch {
    $rootFolder.CreateFolder($TaskFolder) | Out-Null
    Write-Host "Pasta \$TaskFolder criada no Task Scheduler."
}

# --- Registrar tarefa ---
Register-ScheduledTask `
    -TaskPath    "\$TaskFolder\" `
    -TaskName    $TaskName `
    -Action      $action `
    -Trigger     $trigger `
    -Settings    $settings `
    -RunLevel    Highest `
    -User        "SYSTEM" `
    -Force

Write-Host ""
Write-Host "Tarefa registrada: \$TaskFolder\$TaskName"
Write-Host "Agendamento: segunda a sexta, $Horario"
Write-Host ""
Write-Host "Para testar imediatamente:"
Write-Host "  Start-ScheduledTask -TaskPath '\$TaskFolder\' -TaskName '$TaskName'"
