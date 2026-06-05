# Arquitetura centralizada do ERP GDI (obrigatória)

> **Memória técnica permanente** — toda melhoria, correção ou feature nova deve **obedecer** estes padrões antes de introduzir exceções.  
> Implementação: `Lib/GdiMvcJsonResults.cs` | Cliente: `start.js` (`GdiDt*`, `GdiAjax*`, `LibMessage*`) | Resumo operacional: `AI-CONTEXT.md` § Arquitetura centralizada.

---

## 1. Princípios

| Princípio | Regra |
|-----------|--------|
| **Contrato único JSON** | Servidor usa `GdiMvcJsonResults`; cliente usa `GdiDtNotifyJsonErrorMessage` / `GdiAjaxNotifyInconsistencias` |
| **Cirurgia** | Guards e delegação à Lib; **sem** reescrita de actions legadas |
| **Mesmo texto GET/Ajax** | `ViewBag.MsgBloqueio` = `EntidadeNaoEncontradaMensagem` / `PedidoNaoEncontradoMensagem` |
| **Sem side-effect em GetDados*** | Sincronização robôs/NF → action dedicada antes do `draw` |
| **Sem GC.Collect** | Em actions MVC / export Excel |
| **ids string** | `int.TryParse`; nunca `int.Parse` em modais GET sem try |

---

## 2. Matriz por situação

### 2.1 DataTables servidor (`GetDados*`, `GetGed*`)

```csharp
if (param == null) { param = new jQueryDataTableParamModel(); }
string filterOnOff = "0";
try {
    // lógica + guards FirstOrDefault
    return Json(new {
        errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
        stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
        yesFilterOnOff = filterOnOff,
        sEcho = param.sEcho, aaData = list, iTotalRecords = ..., iTotalDisplayRecords = ...
    }, JsonRequestBehavior.AllowGet);
} catch (Exception e) {
    return Json(GdiMvcJsonResults.DataTableError(e, param, filterOnOff), JsonRequestBehavior.AllowGet);
    // ou: return JsonDataTableException(e, param, filterOnOff); // wrapper privado que delega
}
```

### 2.2 Modal GET — entidade ausente

```csharp
// Pedido gc:
var pedido = TryGetMovimentoModal(id);
if (pedido == null) {
    ViewBag.MsgBloqueio = GdiMvcJsonResults.PedidoNaoEncontradoMensagem(id);
    return View(new gc_movimentos { id_movimento = id.GetValueOrDefault() });
}
// Entidade genérica:
var record = db.Entidade.Find(id);
if (record == null) {
    ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Rótulo", id);
    ViewBag.Title = "... — (não localizado)";
    return View(new Entidade { id_* = id.GetValueOrDefault() });
}
```

**View:** `_AlertMsg` + `@if (string.IsNullOrEmpty(ViewBag.MsgBloqueio))` no form/grelha/DT/botão salvar.

### 2.3 Ajax POST

```csharp
catch (DbEntityValidationException ex) { return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet); }
catch (Exception e) { return Json(GdiMvcJsonResults.AjaxFailure(e), JsonRequestBehavior.AllowGet); }
```

Wrappers privados (`JsonAjaxErro*`) **delegam** à Lib (padrão `MovimentosController`).

### 2.4 Views JavaScript

| Contexto | Padrão |
|----------|--------|
| DataTables | `error.dt` → `GdiDtNotifyLoadFailure`; `xhr.dt` → `GdiDtNotifyJsonErrorMessage` |
| `$.ajax` rede | `error:` → `GdiAjaxNotifyInconsistencias(errorThrown \|\| textStatus \|\| 'Verifique as inconsistências.')` — **sem** referência a `result` |
| `success` com `success != true` | `GdiAjaxNotifyInconsistencias(result.msg \|\| 'Verifique as inconsistências.')` |

**Armadilha corrigida (C-1):** handler `error:` que usava `(typeof result !== 'undefined' && result.msg)` — `result` não existe no callback de rede. Piloto **gc/Movimentos** (24 views): `Scripts/2026_06_05_gdi_fix_ajax_error_handlers_movimentos.py`.

---

## 3. Referências ouro

| Fluxo | Ficheiro / método |
|-------|-------------------|
| Pedido + modais workflow | `MovimentosController` — `TryGetMovimentoModal`, `JsonAjaxErro*` |
| GED DataTables | `MovimentosController.GetGedPedido`, `ComexImportacoesController.GetGedComex` |
| Modal financeiro GED | `FinanceiroLancamentosController` — modais upload/anexos (N-D) |
| Contrato Lib | `.cursor/context/2026_06_05_gdi-mvc-json-results-contrato.md` |

---

## 4. Lotes servidor (incremental)

| Lote | Escopo | Estado |
|------|--------|--------|
| N-B | `GdiMvcJsonResults.cs` | ✅ |
| N-A | 4 gaps GED gc | ✅ |
| N-C | `JsonDataTableException` → Lib (31 controllers) | ✅ |
| N-D | Ajax wrappers Movimentos + modal financeiro GED | ✅ |
| N-E | Modal COMEX/estoque + sem `GC.Collect` gc | ✅ |
| N-F | Modal GET gc CFOP/COMEX/item pedido | ✅ |
| N-G | `ComexProdutos.ModalCreateEdit`, `FinanceiroLancamentos.ModalCreateEditLancamento` | ✅ |
| N-H | `EstoqueController.ModalConferencia*`, `EstoqueInventario.ModalCreateEditInventarioItem` | ✅ 2026-05-25 |
| N-I | Ajax wrappers `FinanceiroLancamentos` + `ComexImportacoes` | ✅ 2026-05-25 |
| N-J | DataTables inline → `JsonDataTableException` (7 controllers gc, 17 actions) | ✅ 2026-05-25 |
| N-K | Ajax wrappers `ComexInvoices` + `ComexFinanceiro` + `ComexProdutos` (7 actions) | ✅ 2026-05-25 |
| N-L | Ajax wrappers `Estoque` + `EstoqueInventario` + `MovimentosEntradas` (11 actions) | ✅ 2026-05-25 |
| N-M | Ajax wrappers `EstoqueLotes` + `EstoqueControle` + `CfopOperacoes` + `CfopParametros` (6 actions) | ✅ 2026-05-25 |
| N-N | Ajax wrappers relatórios gc — `catch` em 15 exports; sucesso mantém `idProcessamento` | ✅ 2026-05-25 |
| N-O | Varredura final gc — exports residuais + `AjaxFailure*Message` + `GerarAnexoBoletoPDF` | ✅ 2026-05-25 |
| N-P | Inventário residual Ajax gc + LookupAjax + `GetDados` COMEX | ✅ 2026-05-25 |
| N-P.1 | Inventário ERP (a/crm/g/qa/gc + Services); LookupAjax **g**; `service_msg_append` | ✅ 2026-05-25 |
| N-Q | Indicadores qualidade **qa** — JSON/KPI/DT → `GdiMvcJsonResults` | ✅ 2026-05-25 |
| N-Q | Parâmetros admin **a** — `AjaxAtivarSistema`/`AjaxDesativarSistema` → `JsonAjaxErro*` | ✅ 2026-05-25 |
| N-R.1 | **g** `NfeController` — Ajax e-Notas → `JsonAjaxErro*` / `JsonAjaxErroIdProcessamento` | ✅ 2026-05-25 |
| N-R.2 | **g** `FinanceiroController` — Ajax títulos/remessa + `GetDadosGrafico` → `GdiMvcJsonResults` | ✅ 2026-05-25 |
| N-R.3 | **g** `GedController` — Ajax upload/download GED + `ged_partial` edit | ✅ 2026-05-25 |
| N-R.4 | **g** `ClientesController` — modais Ajax + `ComContato` + ModelState Create/Edit | ✅ 2026-05-25 |
| N-R.5 | **g** `Atendimentos`/`Produtos`/`ProdutosNcm`/`Usuarios` — Ajax + DT sucesso; modais GET Atendimentos | ✅ 2026-05-25 |
| N-S.1 | **gc** `ComexProdutosController` — Ajax `idProcessamento` + modal GET | ✅ 2026-05-25 |
| N-S.2 | **g** onda `mvc_modelstate` — 12 cadastros (50×) | ✅ 2026-05-25 |
| N-S.3 | **gc** modais GET — CFOP/financeiro/COMEX (10×) | ✅ 2026-05-25 |
| N-T.1 | **g** `ContratosAviacao` + `ImportacoesBancarias` — Ajax `idProcessamento` | ✅ 2026-05-25 |
| N-T.1b | **gc** `MovimentosEntradas` — Ajax `idProcessamento` | ✅ 2026-05-25 |
| N-T.2 | **gc** onda `mvc_modelstate` (12×) | ✅ 2026-05-25 |
| N-T.3 | **g** `Nfe.Edit` POST `modal_flash` | ✅ 2026-05-25 |
| N-U.1 | **gc** `ComexInvoices` upload xlsx (6×) | ✅ 2026-05-25 |
| N-U.2 | **gc** `ComexImportacoes` parse + clipboard (10×) | ✅ 2026-05-25 |
| N-U.3 | **Lib** `AjaxFailureWebMessage` + DT/Ajax gc (7×) | ✅ 2026-05-25 |
| N-U.4 | **crm** `PedidosController` boleto `idProcessamento` | ✅ 2026-05-25 |
| N-V | Residual **gc** (12×) → `AjaxFailure*Message` | ✅ 2026-05-25 |
| N-W | DT sucesso — `DataTableSuccess*` (**31** controllers) | ✅ 2026-05-25 |
| N-X | **g** `GetFichaEstoqueProduto` — Fase 8 | ✅ 2026-05-25 |
| N-Y | `.csproj` `ModalTransferirContaCaixa` | ✅ 2026-05-25 |
| C-1 | Cliente **gc/Movimentos** — `error:` Ajax (25 views) | ✅ 2026-05-25 |
| C-2 | Cliente **ERP** — `error:`/`success` Ajax homogéneo (**115** views, 262 linhas) | ✅ 2026-05-25 |
| I-1 | **g** `FinanceiroController` — 4 modais GET `int.TryParse` | ✅ 2026-05-25 |
| G-PUB | Smoke 5 inventários arquitetura (`smoke_architecture_inventories.py`) | ✅ 2026-06-05 |
| I-1b | `Relatorios*` + `Ged.GetDados` — `LibNumbers.ConvertInt` (18×) | ✅ 2026-06-05 |
| L-1 | Lib `AjaxFailureIdProcessamento` | ✅ 2026-05-25 |

**Inventário modais GET gc:** `Scripts/2026_06_05_gdi_inventory_gc_modal_get_guards.py` (exit 0 = conforme).

**Inventário Ajax residual ERP:** `Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py` (`--areas all`) — **exit 0, 0 ocorrências**. Detalhe: `.cursor/context/2026_06_05_erp_ajax-exceptions-residual-inventory.md`.

**Publish views Gdi\*:** `Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` (exit 0 após N-Y).

**Cliente Ajax híbrido:** `Scripts/2026_06_05_gdi_inventory_ajax_hybrid_handlers.py` (exit 0 após C-2). Correção: `Scripts/2026_06_05_gdi_fix_ajax_error_handlers_erp.py`.

---

## 5. Variantes Ajax (wrappers privados — delegam à Lib)

| Variante | Quando | Campos extra |
|----------|--------|--------------|
| `JsonAjaxErro` | Ajax padrão | `{ success, msg }` |
| `JsonAjaxErroIdProcessamento` | Export/lote | `idProcessamento` |
| `JsonAjaxErroComTag` / `ComUrl` | GED **g** | `tag` / `url` |
| `JsonAjaxErroComContato` | Clientes | `idContato` |
| `AjaxFailureWithItems` | Typeahead falha | `items: []` |
| `AjaxFailureWebMessage` | `WebException` robô/API | só texto `msg` |

Objeto pronto: `GdiMvcJsonResults.AjaxFailureIdProcessamento(ex, id)`.

---

## 6. Preservados por design (texto via Lib)

| Categoria | Exemplo |
|-----------|---------|
| `loop_partial` | WhatsApp pedido, boleto on-line em loop |
| `cleanup_financeiro` | Rollback lançamentos órfãos |
| `upload_cleanup` | NF entrada XML temp |
| `service_msg_append` | `EstoqueInventarioService` |
| `mvc_modelstate` | Create/Edit server-side |
| `modal_flash` / `modal_get` | NFe Edit, modais legado |

---

## 7. Referências cruzadas

| Tema | Documento |
|------|-----------|
| DataTables vs MVC | `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` |
| Lookups Index vs CreateEdit | `.cursor/context/2026_05_20_lookups-convencao-index-vs-createedit.md` |
| Contrato Lib JSON | `.cursor/context/2026_06_05_gdi-mvc-json-results-contrato.md` |

---

## 8. Checklist antes de concluir intervenção

- [ ] Classifiquei tabela: DataTables vs MVC?
- [ ] Modal GET: `Find` + guard + model mínimo?
- [ ] DataTables: `param` nulo, try/catch, `DataTableSuccessErrorMessage`/`DataTableSuccessStackTrace` no sucesso?
- [ ] Ajax: `GdiMvcJsonResults` ou wrapper que delega?
- [ ] View: sem `alert()`; `error:` Ajax **sem** `result`; handlers `Gdi*`?
- [ ] `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py`?
- [ ] Registo em `CHANGELOG-DEV.md`?
