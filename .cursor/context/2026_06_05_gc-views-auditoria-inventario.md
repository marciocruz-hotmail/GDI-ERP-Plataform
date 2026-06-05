# Areas/gc/Views — auditoria e inventário (2026-05-25)

Read-only. Critérios espelhados da auditoria **Movimentos** (lotes D–H) + Fases 8–12 do ERP.  
Script de varredura: `Scripts/2026_06_05_gdi_inventory_gc_views_audit.py`.

**Escopo:** 129 ficheiros `.cshtml` em 21 pastas de controller (excl. `web.config`).  
**Movimentos:** concluído (lotes D–H) — referência; não reauditar salvo regressão.

---

## Resumo executivo

| Dimensão | Movimentos | Resto gc/Views |
|----------|------------|----------------|
| `error: function (result)` legado | 0 | **~66 ficheiros** |
| Padrão G3 `result.msg` (unified notify) | ~25 views | **0** |
| DataTables server-side | ~35 | ~70 |
| DataTables **sem** `xhr.dt` | poucos residuais | **32 modais** |
| DataTables **sem** `error.dt` | poucos | **29 modais** |
| `alert()` nativo | 0 | 0 |
| `yesFilterController` suspeito | corrigido (H) | **2** (`ModalViewFinanceiroMovimentos`, `ComexInvoices/ModalInvoice`) |
| Servidor `JsonDataTableException` | sim | parcial (ver §3) |

---

## 1. Cliente — Ajax POST (`$.ajax`)

### 1.1 Crítico — `error: function (result)` (Lote I-A)

Handler legado: trata falha HTTP como se fosse JSON `{ success, msg }`.  
**Correção replicável:** `Scripts/2026_06_05_gdi_fix_movimentos_ajax_error_handlers.py` estendido por pasta/módulo.

| Módulo | Ficheiros com `error_fn_result` |
|--------|--------------------------------|
| **FinanceiroLancamentos** | 10 — `Index`, `ModalBaixarLancamentos`, `ModalCancelarLancamentos`, `ModalCancelarMovimentoFinanceiro`, `ModalCreateEditLancamento`, `ModalFinanceiroViewAnexos`, `ModalGerarBoletoLancamentoAvulso`, `ModalGerarFinanceiroMovimentos`, `ModalRelatorioContaCaixaSaldoDiario`, `ModalViewFinanceiroMovimentos` |
| **RelatoriosComerciais** | 9 — todos os `ModalRelatorio*` |
| **MovimentosEntradas** | 8 — `Index`, 3× `FormProcessarNF*`, 4× `ModalNF*` / `ModalPedidoTransferirFilial` |
| **ComexImportacoes** | 7 — `CreateEdit`, `Index`, 5 modais workflow |
| **Estoque** | 7 — `Index`, 2× recebimento Index, 4 modais conferência/recebimento/transferência |
| **ComexProdutos** | 5 — 2× `FormProcessar*`, 3 modais |
| **EstoqueInventario** | 4 — `Index`, 3 modais |
| **RelatoriosRegulamentacao** | 4 — `ModalRelatorioANP/IBAMA/JogueLimpo/PF` |
| **ComexInvoices** | 3 — `ModalCambio/Cancelar/ImportarInvoiceXLS` |
| **ComexFinanceiro** | 2 — `ModalCreateEdit`, `ModalCancelar` |
| **EstoqueControle** | 2 — `CreateEdit`, `ModalCreateMedicao` |
| **CfopOperacoes / CfopParametros** | 1 cada — `ModalCreateEdit*` |
| **EstoqueLotes** | 1 — `ModalCreateEdit` |
| **RelatoriosCadastrais / RelatoriosFinanceiros** | 1 cada |

**Nota relatórios:** fluxo típico = validar datas → POST gerar ficheiro; risco menor que workflow, mas o handler legado impede mensagem de rede correta.

### 1.2 Alto — `success/else` sem `result.msg` (Lote I-B)

Após I-A, aplicar padrão unificado (já em Movimentos):

```javascript
GdiAjaxNotifyInconsistencias((typeof result !== 'undefined' && result && result.msg)
  ? result.msg : (errorThrown || textStatus || 'Verifique as inconsistências.'))
```

Módulos com `GdiAjaxNotifyInconsistencias` mas **sem** `typeof result`: todos exceto **Movimentos**.

Casos especiais (validar manualmente após script):
- Upload Excel/TXT: `LibMessageError("Erro no Processamento", result.msg)` em `error:` — **ComexImportacoes**, **ComexInvoices**, **MovimentosEntradas** (padrão upload próprio; não forçar G3 no `error:`).

---

## 2. Cliente — DataTables

### 2.1 Index / listagens principais — estado

| View | xhr.dt | error.dt | Notas |
|------|--------|----------|-------|
| Cfop/Index | sim | sim | servidor `JsonDataTableException` |
| CfopOperacoes/Index | sim | sim | idem |
| CfopParametros/Index | sim | sim | idem |
| ComexFinanceiro/Index | sim | sim | servidor sem helper privado padronizado |
| ComexImportacoes/Index | sim | sim | servidor Fase 10 |
| ComexImportacoes/CreateEdit | sim | sim | abas itens/GED |
| ComexInvoices/Index | sim | sim | Fase 12 |
| ComexProdutos/Index, ProdutosPre | sim | sim | `errorMessage` inline no controller |
| Estoque/Index + recebimentos | sim | sim | Fase 11 |
| EstoqueControle/Index | sim | sim | |
| EstoqueInventario/Index, FormInventarioItens | sim | sim | Fase 12 |
| EstoqueLotes/Index | sim | sim | |
| FinanceiroLancamentos/Index | sim | sim | Fase 11 |
| FinanceiroParametroDifal/Index | sim | sim | |
| Fretes/Index | sim | sim | |
| Gerencial/IndexPainelComercialGerencial | sim | sim | gráfico com guard `errorMessage` |
| MovimentosEntradas/Index | sim | sim | só 1 listagem com xhr.dt |
| Movimentos/IndexPedido, PainelPedidos, IndexEstoque | sim | sim | migrado |

### 2.2 Modais com DataTables **sem** `xhr.dt` (Lote I-C) — 32 ficheiros

Prioridade por uso em workflow:

1. **FinanceiroLancamentos** (4): `ModalCreateEditLancamento`, `ModalFinanceiroViewAnexos`, `ModalGerarBoleto*`, `ModalGerarFinanceiroMovimentos`
2. **ComexImportacoes** (6): modais itens/upload/cancelar/carregar
3. **ComexInvoices** (3), **ComexProdutos** (3)
4. **Estoque** (3), **EstoqueInventario** (3), **MovimentosEntradas** (4)
5. **EstoqueControle** (2), **EstoqueLotes** (1), **Cfop*** modais (2)

Padrão: `.on('error.dt', GdiDtNotifyLoadFailure).on('xhr.dt', GdiDtNotifyJsonErrorMessage)` antes de `.DataTable(`.

### 2.3 Modais com DataTables **sem** `error.dt` (Lote I-D) — 29 ficheiros

Subconjunto de I-C; alguns têm só `ajax.error` sem `error.dt` — alinhar ambos.

---

## 3. Servidor — controllers gc (pares view ↔ `GetDados*`)

| Controller | `GetDados*` | `param==null` | `JsonDataTableException` / `errorMessage` |
|------------|-------------|---------------|-------------------------------------------|
| MovimentosController | 9 | sim | helper privado — **referência** |
| ComexImportacoesController | 4 | sim | helper privado Fase 10 |
| Cfop/CfopOperacoes/CfopParametros | 1 cada | sim | helper privado Fase 13 |
| EstoqueControle/EstoqueLotes | 1–2 | sim | helper privado |
| FretesController | 1 | sim | `JsonDataTableExceptionFretes` |
| FinanceiroParametroDifal | 1 | sim | helper privado |
| **EstoqueController** | 5 | sim | `errorMessage` inline (Fase 11) — **sem** helper unificado |
| **FinanceiroLancamentosController** | 2 | sim | idem Fase 11 |
| **ComexInvoicesController** | 2 | sim | idem Fase 12 |
| **EstoqueInventarioController** | 2 | sim | idem Fase 12 |
| **MovimentosEntradasController** | 1 | sim | idem Fase 12 |
| **ComexProdutosController** | 2 | sim | `errorMessage` inline |
| **ComexFinanceiroController** | 1 | sim | `errorMessage` inline |
| GerencialController | gráfico Ajax | — | contrato gráfico (fora DataTables) |

**Lote J (servidor):** opcional unificar helpers `JsonDataTableException` nos controllers Fase 11–12 (não bloqueia cliente se `errorMessage` já preenchido).

---

## 4. Polish — `yesFilterController`

| Ficheiro | Valor atual | Sugestão |
|----------|-------------|----------|
| FinanceiroLancamentos/ModalViewFinanceiroMovimentos | `ModalViewNotasFiscais` | `ModalViewFinanceiroMovimentos` |
| ComexInvoices/ModalInvoice | `ModalViewNotasFiscais` | `ModalInvoice` |
| ComexImportacoes/ModalImportacoesLogs | `ModalPedidosLogs` | manter (alias documentado) |
| Movimentos/ModalHistoricoMovimento | `ModalPedidosLogs` | manter (rota legada) |
| ComexFinanceiro/Gerencial/Parametros/FinanceiroLancamentos Index | `GcIndexFinanceiroLancamentos` | legado partilhado — **não alterar** sem inventário global |

---

## 5. Módulos sem gaps relevantes (hub / PDF / MVC)

- **Relatorios*/Index.cshtml** — navegação; sem DataTables Ajax.
- **Report*PDF**, **BoletoPdfFebraban**, **Cfop/CreateEdit** — render servidor; fora do contrato Ajax/DT.
- **Parametros/Index** — hub financeiro.
- **ComexImportacoes/ModalUploadFile*** — upload; revisar só handlers `error:`.

---

## 6. Plano de lotes — estado (2026-05-25)

| Lote | Estado | Resultado |
|------|--------|-----------|
| **I-A / I-B** | **Concluído** | 69 views gc — `2026_06_05_gdi_fix_gc_views_ajax_error_handlers.py`; 0× `error: function (result)` |
| **I-C / I-D** | **Concluído** (inits reais) | 4 views com `.DataTable({` patchadas (`ModalCreateEditLancamento`, `ModalFinanceiroViewAnexos`, `EstoqueControle/CreateEdit`, `EstoqueLotes/ModalCreateEdit`); demais ficheiros da lista antiga só usam `.DataTable().draw` |
| **J** | **Adiado** | Servidor Fase 11–12 já devolve `errorMessage` — compatível com cliente; sem reescrita em massa |
| **K** | **Concluído** | `ModalViewFinanceiroMovimentos`, `ComexInvoices/ModalInvoice` |

**Smoke pós-lote:** Index Financeiro, Estoque, COMEX importações/invoices, MovimentosEntradas.

---

## 7. Referências

- Padrões globais: `AI-CONTEXT.md` (secção integridade D–H).
- Matriz Movimentos: `.cursor/context/2026_06_05_movimentos-metodo-contrato-erro.md`.
- Script correção Ajax: `Scripts/2026_06_05_gdi_fix_movimentos_ajax_error_handlers.py`.
- Script inventário: `Scripts/2026_06_05_gdi_inventory_gc_views_audit.py`.
