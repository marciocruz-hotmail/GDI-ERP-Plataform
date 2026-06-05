# gc Movimentos — matriz método × contrato de erro (lotes A–G)

Referência compacta para auditoria e novas actions. Detalhe de implementação: `AI-CONTEXT.md` (secção «Padrões reutilizáveis — integridade MVC/Ajax/DataTables»).

| Método | Tipo | Contrato erro / sucesso |
|--------|------|-------------------------|
| `TryGetMovimentoModal` | privado | `null` se id inválido |
| `JsonPedidoNaoEncontrado` | privado | `{ success: false, msg }` |
| `JsonAjaxErro` / `JsonAjaxErroValidacao` / `JsonAjaxErroComItems` | privado | `{ success: false, msg }` (+ `items: []` no combo) |
| `JsonDataTableException` | privado | JSON DataTables: `errorMessage`, `severity`, `stackTrace`, `aaData: []` |
| **Lote A — workflow Ajax POST** | | |
| `AjaxSavePosVenda`, `AjaxModalPedidoAprovacao`, `AjaxModalPedidoSeparacao`, `AjaxModalPedidoEntrega`, `AjaxModalCancelarPedido` | Ajax | `JsonAjaxErro*` + `JsonPedidoNaoEncontrado` |
| `AjaxModalPedidoDuplicar`, `AjaxModalReabrirMovimento` | Ajax | guard `Find` → `JsonPedidoNaoEncontrado` + `JsonAjaxErro*` |
| `AjaxModalPedidoNotaFiscal`, `AjaxCancelNotaFiscal`, `AjaxPDF/XML/InfoNotaFiscal`, `AjaxGetXMLEnvioSefaz`, `AjaxModalPedidoCartaCorrecao` | Ajax NF | `JsonAjaxErro*` (+ `WebException` legado em NF/carta) |
| `AjaxModalPedidoExpedicao`, `AjaxModalNotificacaoCliente`, import Excel/TXT, duplicar/converter/reabrir/comissão/valor total, `AjaxFechamentoCompetenciaEstoque` | Ajax | `JsonAjaxErro*` |
| **Lote B — DataTables + sync** | | |
| `GetGedPedido`, `GetNotaFiscalPedido` | DataTables GET | `try/catch` → `JsonDataTableException` |
| `SincronizarStatusNotasFiscaisPedido` | Ajax POST | `JsonAjaxErro*`; side-effect e-Notas **fora** da listagem |
| `GetDadosModalItensComValor`, `GetDadosCartaCorrecao`, … (fases 9–12 gc) | DataTables | `JsonDataTableException` + `errorMessage` vazio no sucesso |
| **Lote C — qualidade (sem contrato novo)** | | |
| `AjaxModalPedidoAprovacao` (markup) | Ajax | mantém `JsonAjaxErro*`; markup: `FirstOrDefault` + `ItemCustoReais <= 0` → `Trace.TraceWarning` |
| `ModalPedidoAprovacao` (GET markup) | View prep | `LoadProdutosPedidoItens` + guard produto `null` |
| `SetCotacaoDollarDia`, `DeleteItemTemporario` | util | `Trace.TraceWarning`; cotação: fallback `CachePersister.userIdentity.CotacaoDollarDia` |
| **Lote D — integridade** | | |
| `AjaxCancelNotaFiscal` | Ajax NF | `null` → `return Json` imediato; sem acesso a propriedades |
| `AjaxModalPedidoAjustarComissao`, `AjaxModalPedidoAtualizarValorTotal` | Ajax | `JsonPedidoNaoEncontrado` + guard `baseComissao <= 0` |
| `ModalDuplicateItem` | GET item | `Find` null → `MsgBloqueio` + model vazio |
| `AjaxCarregarItensImportacao` | Ajax | guards `Find`/`FirstOrDefault` + `QtdIgnorados` + `JsonAjaxErro*` |
| `ModalPedidoNotaFiscal`, `ModalPedidoEntrega`, `ModalPedidoCartaCorrecao` | GET workflow | guards `RecordCliente`, `RecordUf`, `RecordCfopOperacao`, `gc_cfop.Find` |
| `EnviarEmailAprovacaoEspelhoDigital` | e-mail | `frete_observacoes` (não `obs_aprovacao`) |
| `ModalHistoricoMovimento`, `ModalPedidosLogs`, `GetDadosHistoricoMovimento` | modal + DT | `TryGetMovimentoModal`; `gc_movimentos_audit` EF; `JsonDataTableException` |
| **Lote E — contrato global DataTables + cliente** | | |
| `GetDadosPedidos`, `GetDadosItensPedido`, `GetDadosItensSeparacao` | DataTables | `param == null` + `JsonDataTableException` |
| `GetDadosCartaCorrecao` | DataTables | sem `foreach` Robo na listagem; sucesso com `errorMessage`/`stackTrace` vazios |
| `SincronizarStatusCartasCorrecaoPedido` | Ajax POST | `JsonAjaxErro*`; view `ModalViewCartaCorrecao` chama antes do DataTable |
| `AjaxVisualizarPedido`, `AjaxReportInvoicePDF`, `AjaxDadosProduto`, `AjaxGetPrecoVendaProduto`, `AjaxGetDetailsProduct`, histórico item, `AjaxDadosInfoComplementares`, `AjaxModalPedidoNotaFiscal`, `AjaxModalPedidoCartaCorrecao`, termo ICMS/Difal, `AjaxDadosClientesDestinatarios`, `AjaxComboComexImportacoesPedido` | Ajax POST | `JsonAjaxErro*` (combo: `JsonAjaxErroComItems`) |
| Import Excel/TXT, invoice PDF, NF GET, e-mail espelho, `GetAnexosPedido` | util / best-effort | `Trace.TraceWarning` (sem `catch { }` vazio) |
| **Views `Areas/gc/Views/Movimentos/*.cshtml`** | cliente | `$.ajax` → `GdiAjaxNotifyInconsistencias`; DataTables → `error.dt` + `xhr.dt` (`FormPedidoCreate` itens mantém `label_total_pedido`; `ModalViewNotasFiscais`, `ModalPedidoSeparacao`, `ModalPedidoViewAnexos`) |
| **Lote F — polish e documentação** | | |
| `ModalViewNotasFiscais`, `ModalViewCartaCorrecao` | GET visualização | `TryGetMovimentoModal` + model fallback; título «(não localizado)» |
| `ModalPedidoAprovacao`, `ModalPedidoSeparacao`, `ModalPedidoNotaFiscal`, `ModalPedidoEntrega` | GET workflow | `MsgHistorico` — todas OBS com `+=` (frete não sobrescreve pedido) |
| `FinanceiroLancamentosController` (gerar financeiro) | GET | `MsgHistorico` frete com `+=` (espelho Movimentos) |
| `IndexPedido.cshtml` — `jsModalConsultaPedidos` | cliente JS | prefixo `[jsModalConsultaPedidos]` no `catch` |

**Resumo lotes D/E (referência):** D = integridade NRE/guards/histórico audit; E = `JsonDataTableException` global, sync carta/NF dedicada, `xhr.dt`, `JsonAjaxErro*`, `Trace.TraceWarning`.

| **Lote G1 — integridade GET modal (Razor-safe)** | | |
| `ModalPedidoAprovacao` | GET workflow | guard `PedidoVenda == null` → fallback antes de `PreencherLookups*` |
| `ModalPedidoSeparacao`, `ModalPedidoExpedicao`, `ModalPedidoEntrega`, `ModalCancelarPedido`, `ModalPedidoViewAnexos`, `ModalNotificacaoCliente` | GET workflow | fallback `gc_movimentos { id_movimento }` + título «(não localizado)» |
| `ModalPedidoSeparacaoLotes` | GET item | guard item/produto `Find`; `ViewBag.MsgBloqueio`; view oculta form/salvar |
| `ModalNotificacaoCliente` | GET | guard `RecordCfopOperacao == null` |
| **Lote G2 — servidor** | | |
| `GetDadosItensPedido` | DataTables | `RecordMovimento != null` no badge cotação; `yesFilterOnOff` no JSON sucesso |
| `AjaxModalNotificacaoCliente`, `AjaxModalPedidoExpedicao` | Ajax POST | `TryGetMovimentoModal` + `JsonPedidoNaoEncontrado` early return |
| `ModalPedidoSeparacao` | GET workflow | guards `RecordCfopOperacao` / `RecordLocalEstoque` null → `MsgBloqueio` |
| **Lote G3 — cliente Ajax** | | |
| `Areas/gc/Views/Movimentos/*.cshtml` | cliente | `success/else` → `result.msg` (não `errorThrown`); script `2026_06_05_gdi_fix_movimentos_ajax_error_handlers.py` |
| `ModalPedidoNotaFiscal` — `AjaxDadosInfoComplementares` | Ajax | `result.success == true` antes de gravar `informacoes` |
| **Lote G4 — DataTables cliente** | | |
| `FormPedidoCreate` — GED / tarefas | DataTables | `xhr.dt` + `GdiDtNotifyJsonErrorMessage` |
| `ModalViewNotasFiscais`, `ModalViewCartaCorrecao` | sync + DT | `success` → `jsCreateTable()`; `error` notify (sem `complete`) |
| **Lote H — polish cliente (Movimentos)** | | |
| `ModalViewCartaCorrecao`, `ModalInvoicesItensEspelhoDigital`, `PainelPedidos` | hidden filtro | `yesFilterController` = nome da view |
| `IndexPedido.cshtml` | cliente JS | prefixo catch `[jsModalPedidoAtualizarValorTotal]`; removido `jsModalPedidoPosvenda` |

**Área gc completa (fora Movimentos):** lotes I–K — ver `.cursor/context/2026_06_05_gc-views-auditoria-inventario.md` e `AI-CONTEXT.md` (tabela scripts).

**Atualizar esta matriz** ao concluir cada lote J+ pontual em controller (sem reescrita em massa).
