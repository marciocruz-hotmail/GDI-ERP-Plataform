# Remove icones FA hardcoded do modal-title nos arquivos restantes
# Inclui: views que ja tinham LibIcons no controller (duplicatas perdidas na primeira passagem)
# E as views cujos controllers foram atualizados nesta sessao

$root = "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas"

$removeList = @(
    # Movimentos (gc) — controller ja tinha LibIcons, icone era duplicata
    "ModalPedidoCartaCorrecao.cshtml",
    "ModalPedidoConverterMoeda.cshtml",
    "ModalPedidoDuplicar.cshtml",
    "ModalPedidoAjustarComissao.cshtml",
    "ModalPedidoAtualizarValorTotal.cshtml",
    "ModalViewCartaCorrecao.cshtml",
    "ModalViewNotasFiscais.cshtml",
    "ModalReabrirMovimento.cshtml",
    "ModalPedidoSeparacaoLotes.cshtml",
    "ModalInvoicesItensEspelhoDigital.cshtml",
    # ComexFinanceiro (gc) — controller ja tinha LibIcons
    "ModalCreateEditGcComexFinanceiro.cshtml",
    # Controllers atualizados nesta sessao (g area)
    "ModalBaixarTitulos.cshtml",
    "ModalCancelarTitulos.cshtml",
    "ModalEditarTitulo.cshtml",
    "ModalProrrogarVencimentoTitulo.cshtml",
    "ModalGerarRemessaBoletosBancarios.cshtml",
    "ModalNfeEnviarPorEmailUnitario.cshtml",
    "ModalExportarDadosNfePDF.cshtml",
    "ModalGerarNfe.cshtml",
    "ModalAtualizarStatusNfe.cshtml",
    "ModalEnviarCancelamentoNfe.cshtml",
    "ModalCancelarNfe.cshtml",
    "ModalSincronizarLotesNfe.cshtml",
    "ModalImportarNfeLote.cshtml",
    "ModalImportarCnabBoletos.cshtml",
    "ModalAtualizarCadastroProdutos.cshtml",
    "ModalDesativarCadastroProduto.cshtml",
    "ModalViewFichaEstoqueProduto.cshtml",
    "ModalAtualizarTabelaIBPT.cshtml",
    "ModalIncluirLancamentos.cshtml",
    "ModalGerarFaturamento.cshtml",
    "ModalFecharLancamentosAbertos.cshtml",
    "ModalFinalizarEdicaoTitulo.cshtml",
    # Controllers atualizados nesta sessao (gc area)
    "ModalBaixarLancamentos.cshtml",
    "ModalCancelarMovimentoFinanceiro.cshtml",
    "ModalCancelarImportacaoComex.cshtml",
    "ModalExcluirItensImportacao.cshtml",
    "ModalFechamentoCustosImportacao.cshtml",
    "ModalInvoice.cshtml",
    "ModalCambioInvoice.cshtml",
    "ModalCancelarInvoice.cshtml",
    "ModalNFEntradaCancelar.cshtml",
    "ModalNFEntradaGerarNF.cshtml",
    "ModalAtualizarFaturamentoGestorFranquia.cshtml",
    "ModalImportarArquivoFaturamentoGestorFranquia.cshtml",
    "ModalEnviarEmailsClientes.cshtml",
    "ModalEnviarNFEmailCliente.cshtml",
    "ModalCancelarComexFinanceiro.cshtml",
    "ModalDesativarProdutoComex.cshtml",
    "ModalFinalizarInventario.cshtml",
    "ModalSolicitarBloqueioLogon.cshtml",
    "ModalUsuarioTrocarSenha.cshtml"
)

# ModalCancelarLancamentos aparece em DUAS areas (g e gc) — processar por path
$removePorPath = @(
    "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas\g\Views\FinanceiroLancamentos\ModalCancelarLancamentos.cshtml",
    "C:\Marcio\Projetos\GDI-ERP-Plataform\Areas\gc\Views\FinanceiroLancamentos\ModalCancelarLancamentos.cshtml"
)

$removeSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($n in $removeList) { [void]$removeSet.Add($n) }

$totalOcorrencias = 0
$totalArquivos    = 0

function ProcessFile($path) {
    $raw   = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $nl    = if ($raw -match "`r`n") { "`r`n" } else { "`n" }
    $lines = [System.Collections.Generic.List[string]]($raw -split "`r?`n")

    $changed = $false
    $i = 0

    while ($i -lt $lines.Count) {
        $line = $lines[$i]

        # Padrao A: linha standalone <i class="fa-..."> seguida de @Html.Raw(ViewBag.Title)
        if ($line -match '^\s*<i\s+class="fa-(solid|regular|brands)\s+[^"]*"\s*(?:aria-hidden="true")?\s*(?:style="[^"]*")?\s*(?:aria-hidden="true")?></i>\s*$') {
            $prevLine  = if ($i -gt 0) { $lines[$i - 1] } else { "" }
            $nextLine  = if ($i + 1 -lt $lines.Count) { $lines[$i + 1] } else { "" }
            $nextLine2 = if ($i + 2 -lt $lines.Count) { $lines[$i + 2] } else { "" }

            $inModalContext = ($prevLine  -match 'modal-title') -or
                              ($nextLine  -match '@Html\.Raw.*ViewBag\.Title') -or
                              ($nextLine2 -match '@Html\.Raw.*ViewBag\.Title')

            if ($inModalContext) {
                $lines.RemoveAt($i)
                $changed = $true
                $script:totalOcorrencias++
                continue
            }
        }

        # Padrao B: icone inline na mesma linha que @Html.Raw(ViewBag.Title)
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
        Write-Host ("  ALTERADO: " + [System.IO.Path]::GetFileName($path) + " (" + [System.IO.Path]::GetDirectoryName($path).Replace($root + "\", "") + ")")
    }
}

# Processa por nome de arquivo
Get-ChildItem -Path $root -Recurse -Filter "*.cshtml" | Where-Object { $removeSet.Contains($_.Name) } | ForEach-Object {
    ProcessFile $_.FullName
}

# Processa os dois ModalCancelarLancamentos por path exato
foreach ($p in $removePorPath) {
    if (Test-Path $p) { ProcessFile $p }
    else { Write-Host "  SKIP (nao encontrado): $p" }
}

Write-Host ""
Write-Host "=== Concluido: $totalOcorrencias icones removidos em $totalArquivos arquivo(s) ==="
