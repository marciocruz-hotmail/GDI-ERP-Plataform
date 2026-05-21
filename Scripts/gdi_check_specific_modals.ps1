# Verifica ViewBag.Title para acoes especificas de modal nos controllers

$checks = @(
    @{ ctrl = "FinanceiroController.cs";          actions = @("ModalBoleto","ModalNotaDebito","ModalBaixarTitulos","ModalCancelarTitulos","ModalEditarTitulo","ModalProrrogarVencimentoTitulo","ModalTransferirContaCaixa","ModalGerarRemessaBoletosBancarios","ModalGerarFaturamento","ModalRelatorioContaCaixaSaldoDiario") },
    @{ ctrl = "ImportacoesBancariasController.cs"; actions = @("ModalImportarCnabBoletos") },
    @{ ctrl = "CidadesController.cs";             actions = @("ModalCadastrarNovaCidade") },
    @{ ctrl = "EstoqueController.cs";             actions = @("ModalConferenciaImportacaoItem","ModalTransferenciaGerencial","ModalRecebimentoComexLote","ModalRecebimentoComexObs") },
    @{ ctrl = "MovimentosController.cs";          actions = @("ModalPedidoAprovacao","ModalPedidoCancelar","ModalPedidoCartaCorrecao","ModalPedidoConverterMoeda","ModalPedidoDuplicar","ModalPedidoEntrega","ModalPedidoExpedicao","ModalPedidoSeparacao","ModalPedidoSeparacaoLotes","ModalPedidoNotaFiscal","ModalViewCartaCorrecao","ModalViewNotasFiscais","ModalConsultaPedidos","ModalHistoricoMovimento","ModalPedidoAtualizarValorTotal","ModalPedidoAjustarComissao","ModalReabrirMovimento","ModalUploadFilePedidos","ModalPedidoViewAnexos","ModalPedidoTransferirFilial") },
    @{ ctrl = "ComexImportacoesController.cs";    actions = @("ModalCarregarItensImportacao","ModalCadastrarAtualizarItensImportacao","ModalCancelarImportacaoComex","ModalExcluirItensImportacao","ModalFechamentoCustosImportacao","ModalUploadFileComexInvoicesPDF") },
    @{ ctrl = "ComexInvoicesController.cs";       actions = @("ModalInvoice","ModalCambioInvoice","ModalCancelarInvoice") },
    @{ ctrl = "EstoqueInventarioController.cs";   actions = @("ModalCreateInventario","ModalCreateEditInventarioItem","ModalFinalizarInventario") },
    @{ ctrl = "EstoqueLotesController.cs";        actions = @("ModalUploadAnexoEstoqueLotes") },
    @{ ctrl = "AtendimentosController.cs";        actions = @("ModalCreateNewAtendimento","ModalUploadFileAtendimentos","ModalCreateEditAtividade") },
    @{ ctrl = "GedController.cs";                 actions = @("ModalDesativarGed") },
    @{ ctrl = "FinanceiroLancamentosController.cs"; actions = @("ModalCreateEditLancamento","ModalBaixarLancamentos","ModalCancelarLancamentos","ModalGerarBoletoLancamentoAvulso","ModalGerarFinanceiroMovimentos","ModalFinanceiroViewAnexos","ModalUploadAnexoFinanceiro","ModalFecharLancamentosAbertos","ModalFinalizarEdicaoTitulo","ModalCancelarMovimentoFinanceiro") },
    @{ ctrl = "MovimentosEntradasController.cs";  actions = @("ModalNFEntradaCancelar","ModalNFEntradaGerarNF") },
    @{ ctrl = "MovimentosComprasController.cs";   actions = @("ModalInsertEditItemCompra","ModalCancelarPedido") },
    @{ ctrl = "FinanceiroFaturamentosController.cs"; actions = @("ModalAtualizarFaturamentoGestorFranquia","ModalImportarArquivoFaturamentoGestorFranquia","ModalEnviarEmailsClientes","ModalEnviarNFEmailCliente") }
)

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"

foreach ($chk in $checks) {
    $f = Get-ChildItem -Path $root -Recurse -Filter $chk.ctrl | Select-Object -First 1
    if (-not $f) { Write-Host "NAO ENCONTRADO: $($chk.ctrl)"; continue }
    $lines = [System.IO.File]::ReadAllLines($f.FullName)
    $content = $lines -join "`n"

    foreach ($action in $chk.actions) {
        # Encontra a action (public ActionResult <action>)
        $found = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "ActionResult\s+$action\s*\(") {
                # Escaneia ate 50 linhas para ViewBag.Title
                $vbTitle = "NAO ENCONTRADO"
                $tipo = "?"
                for ($k = $i; $k -lt [Math]::Min($i + 50, $lines.Count); $k++) {
                    if ($lines[$k] -match 'ViewBag\.Title\s*=') {
                        $vbTitle = $lines[$k].Trim()
                        if ($vbTitle -match 'getIcon|LibIcons') { $tipo = "LibIcons" }
                        elseif ($vbTitle -match '<i ') { $tipo = "HTML-icon" }
                        else { $tipo = "TextoPuro" }
                        break
                    }
                    # Para na proxima action
                    if ($k -gt $i -and $lines[$k] -match 'public.*ActionResult') { break }
                }
                Write-Host ("  [$tipo]".PadRight(15) + " $($chk.ctrl.Replace('Controller.cs','')) -> $action")
                if ($vbTitle -ne "NAO ENCONTRADO" -and $tipo -ne "TextoPuro") {
                    Write-Host ("            " + $vbTitle.Substring(0, [Math]::Min(100, $vbTitle.Length)))
                }
                $found = $true
                break
            }
        }
        if (-not $found) { Write-Host ("  [?]".PadRight(15) + " $($chk.ctrl.Replace('Controller.cs','')) -> $action NAO ENCONTRADA") }
    }
}
