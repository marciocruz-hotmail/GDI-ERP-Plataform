# Remove icone FA duplicado do header de modais cujo ViewBag.Title ja traz icone via LibIcons.getIcon()
# Aplica apenas nos arquivos confirmados (controller usa LibIcons no ViewBag.Title)
# NAO altera modais cujo ViewBag.Title e texto puro (NfeController, FinanceiroController, filtros, etc.)

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"

# Lista de nomes de arquivos (sem path) confirmados como "ViewBag.Title ja traz icone"
$removeList = @(
    "ModalConferenciaEstoqueItem.cshtml",
    "ModalConferenciaImportacaoItem.cshtml",
    "ModalTransferenciaGerencial.cshtml",
    "ModalDesativarGed.cshtml",
    "ModalUploadFileGed.cshtml",
    "ModalEditFileGed.cshtml",
    "ModalPedidoAprovacao.cshtml",
    "ModalPedidoInsertEditItem.cshtml",
    "ModalUploadFilePedidos.cshtml",
    "ModalPedidoViewAnexos.cshtml",
    "ModalNotificacaoCliente.cshtml",
    "ModalCarregarItensImportacao.cshtml",
    "ModalUploadFileComexDocs.cshtml",
    "ModalUploadFileComexInvoicesPDF.cshtml",
    "ModalImportacoesLogs.cshtml",
    "ModalImportarInvoiceXLS.cshtml",
    "ModalCreateEditContato.cshtml",
    "ModalDesativarContato.cshtml",
    "ModalAtualizarVendedorConsultor.cshtml",
    "ModalAtualizarLimiteCredito.cshtml",
    "ModalCreateEditDestinatario.cshtml",
    "ModalCreateNewAtendimento.cshtml",
    "ModalUploadFileAtendimentos.cshtml",
    "ModalCreateEditAtividade.cshtml",
    "ModalCreateInventario.cshtml",
    "ModalCreateEditInventarioItem.cshtml",
    "ModalUploadAnexoEstoqueLotes.cshtml",
    "ModalImportarExcelSC.cshtml",
    "ModalImportarTxtSC.cshtml",
    "ModalNFEntradaImportar.cshtml",
    "ModalCadastrarNovaCidade.cshtml",
    "ModalNovoCadastroRoboSintegra.cshtml",
    "ModalCreateEditOperacao.cshtml",
    "ModalCreateEditParametro.cshtml",
    "ModalCreateMedicao.cshtml",
    "ModalRelatorioANP.cshtml",
    "ModalRelatorioClientesFornecedores.cshtml",
    "ModalRelatorioIBAMA.cshtml",
    "ModalRelatorioItensComercializados.cshtml",
    "ModalRelatorioJogueLimpo.cshtml",
    "ModalRelatorioLancamentosFinanceiros.cshtml",
    "ModalRelatorioNotasFiscaisContabilidade.cshtml",
    "ModalRelatorioNotasFiscaisEmitidas.cshtml",
    "ModalRelatorioNotasFiscaisMensais.cshtml",
    "ModalRelatorioPF.cshtml",
    "ModalRelatorioTransportadorasFretes.cshtml",
    "ModalRelatorioVendedoresAtrasados.cshtml",
    "ModalRelatorioVendedoresCarteira.cshtml",
    "ModalRelatorioVendedoresComissoes.cshtml",
    "ModalRelatorioVendedoresPedidos.cshtml",
    "ModalUploadContratoAssinado.cshtml",
    "ModalViewFinanceiroMovimentos.cshtml",
    "ModalCancelarPedido.cshtml",
    "ModalPedidoSeparacao.cshtml",
    "ModalPedidoNotaFiscal.cshtml",
    "ModalPedidoExpedicao.cshtml",
    "ModalPedidoEntrega.cshtml",
    "ModalPedidoTransferirFilial.cshtml",
    "ModalCancelarCadastroProdutos.cshtml",
    "ModalCreateEdit.cshtml",
    "ModalPedidoInsertEditItemCompra.cshtml",
    "ModalConsultaPedidos.cshtml",
    "ModalCreateEditLancamento.cshtml",
    "ModalGerarFinanceiroMovimentos.cshtml",
    "ModalGerarBoletoLancamentoAvulso.cshtml",
    "ModalRelatorioContaCaixaSaldoDiario.cshtml",
    "ModalUploadAnexoFinanceiro.cshtml",
    "ModalFinanceiroViewAnexos.cshtml"
)

$removeSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($n in $removeList) { [void]$removeSet.Add($n) }

$totalOcorrencias = 0
$totalArquivos    = 0

Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | Where-Object { $removeSet.Contains($_.Name) } | ForEach-Object {
    $path  = $_.FullName
    $raw   = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $nl    = if ($raw -match "`r`n") { "`r`n" } else { "`n" }
    $lines = [System.Collections.Generic.List[string]]($raw -split "`r?`n")

    $changed = $false
    $i = 0

    while ($i -lt $lines.Count) {
        $line = $lines[$i]

        # Padrao A: linha standalone <i class="fa-..."> seguida de @Html.Raw(ViewBag.Title) ate 2 linhas adiante
        # A linha deve conter APENAS o icone (alem de whitespace)
        if ($line -match '^\s*<i\s+class="fa-(solid|regular|brands)\s+[^"]*"\s*(?:aria-hidden="true")?\s*(?:style="[^"]*")?\s*(?:aria-hidden="true")?></i>\s*$') {
            # Verifica contexto: esta em bloco modal-title?
            $prevLine = if ($i -gt 0) { $lines[$i - 1] } else { "" }
            $nextLine = if ($i + 1 -lt $lines.Count) { $lines[$i + 1] } else { "" }
            $nextLine2 = if ($i + 2 -lt $lines.Count) { $lines[$i + 2] } else { "" }

            $inModalContext = ($prevLine -match 'modal-title') -or
                              ($nextLine -match '@Html\.Raw.*ViewBag\.Title') -or
                              ($nextLine2 -match '@Html\.Raw.*ViewBag\.Title')

            if ($inModalContext) {
                $lines.RemoveAt($i)
                $changed = $true
                $script:totalOcorrencias++
                continue  # nao incrementa $i, a lista encolheu
            }
        }

        # Padrao B: icone inline na mesma linha que @Html.Raw(ViewBag.Title) dentro de h5/h4
        # Ex: <h5 class="modal-title ..."><i class="fa-..."></i>@Html.Raw(ViewBag.Title)</h5>
        if ($line -match '<i\s+class="fa-(solid|regular|brands)\s+[^"]*"[^>]*></i>' -and
            $line -match '@Html\.Raw.*ViewBag\.Title') {
            $novo = $line -replace '<i\s+class="fa-(solid|regular|brands)\s+[^"]*"[^>]*></i>', ''
            if ($novo -ne $line) {
                $lines[$i] = $novo
                $changed = $true
                $script:totalOcorrencias++
            }
        }

        $i++
    }

    if ($changed) {
        $novo = $lines -join $nl
        if ($raw -match "(`r?`n)$") { $novo = $novo + $nl }
        [System.IO.File]::WriteAllText($path, $novo, [System.Text.Encoding]::UTF8)
        $script:totalArquivos++
        Write-Host ("  ALTERADO: " + $_.Name + " (" + $path + ")")
    }
}

Write-Host ""
Write-Host "=== Concluido: $totalOcorrencias icones removidos em $totalArquivos arquivo(s) ==="
