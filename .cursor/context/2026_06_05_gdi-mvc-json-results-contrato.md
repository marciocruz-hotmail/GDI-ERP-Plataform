# GdiMvcJsonResults — contrato JSON MVC global (2026-05-25)

Fonte: `Lib/GdiMvcJsonResults.cs`. Cliente: `GdiDtNotifyJsonErrorMessage` / `GdiAjaxNotifyInconsistencias` (`start.js`).

---

## DataTables server-side (Fase 8+)

### Erro (`catch`)

```csharp
return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
```

| Propriedade | Valor |
|-------------|-------|
| `errorMessage` | `LibExceptions.getExceptionShortMessage(e)` |
| `severity` | `"error"` (const `SeverityError`) |
| `stackTrace` | `e.ToString()` |
| `yesFilterOnOff` | argumento ou `"0"` |
| `sEcho` | `param.sEcho` |
| `iTotalRecords` / `iTotalDisplayRecords` | `0` |
| `aaData` | `[]` |

### Sucesso

Incluir no `return Json(new { ... })`:

```csharp
errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
```

Mais: `sEcho`, `aaData`, totais, `yesFilterOnOff` quando a grelha já usa filtro.

### Esqueleto action

```csharp
if (param == null) { param = new jQueryDataTableParamModel(); }
string filterOnOff = "0";
try
{
    // lógica + guards FirstOrDefault (usuário, CFOP, …)
    return Json(new { errorMessage = ..., stackTrace = ..., yesFilterOnOff = filterOnOff, ... }, JsonRequestBehavior.AllowGet);
}
catch (Exception e)
{
    return Json(GdiMvcJsonResults.DataTableError(e, param, filterOnOff), JsonRequestBehavior.AllowGet);
}
```

### GED — template

Referência: `MovimentosController.GetGedPedido`, `ComexImportacoesController.GetGedComex`.  
Lote **N-A** (2026-05-25): `GetGedLancamentos`, `GetGedFinanceiro`, `GetGedEstoqueLotes`, `GetDadosMedicoes`.

---

## Ajax POST

| Método Lib | JSON |
|------------|------|
| `AjaxFailure(string msg)` | `{ success: false, msg }` |
| `AjaxFailure(Exception ex)` | `{ success: false, msg }` |
| `AjaxFailureValidation(DbEntityValidationException ex)` | `{ success: false, msg }` |
| `AjaxFailureMessage(Exception ex)` | só `msg` (helpers bool/string — **N-O**) |
| `AjaxFailureValidationMessage(DbEntityValidationException ex)` | só `msg` (helpers bool/string — **N-O**) |
| `AjaxFailureWithItems(Exception ex)` | `{ success: false, msg, items: [] }` |
| `AjaxFailureWebMessage(WebException ex)` | só `msg` (robô/API — body do response) |
| `AjaxFailureIdProcessamento(Exception ex, string id)` | `{ success: false, msg, idProcessamento }` — objeto pronto |
| `PedidoNaoEncontrado(int id)` | gc pedidos |
| `EntidadeNaoEncontrada(string rotulo, int? id)` | modais genéricos |

```csharp
catch (DbEntityValidationException ex) { return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet); }
catch (Exception e) { return Json(GdiMvcJsonResults.AjaxFailure(e), JsonRequestBehavior.AllowGet); }
```

### Wrapper privado Ajax (padrão N-D — manter nas actions)

```csharp
private JsonResult JsonAjaxErro(Exception ex)
{
    return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
}
```

### Modal GET — `ViewBag.MsgBloqueio` (texto alinhado ao Ajax)

```csharp
if (record == null || id.GetValueOrDefault() <= 0)
{
    ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Lançamento financeiro", id);
    return View(modelMinimo);
}
```

### Modal GET — id string (COMEX)

```csharp
int id = 0;
if (!int.TryParse(idImportacao, out id) || id <= 0) { /* MsgBloqueio + model mínimo */ }
var record = db.gc_comex_importacoes.Find(id);
if (record == null) { /* MsgBloqueio + model { id_importacao = id } */ }
```

### Export — sem `GC.Collect`

Após `WorkBook.Dispose()`, não chamar `GC.Collect()` / `WaitForPendingFinalizers` em actions MVC (script `2026_06_05_gdi_remove_gc_collect_gc_controllers.py`).
```

**Não alterar** contratos com `idProcessamento`, download relatório, etc.

---

## Migração incremental (lotes servidor)

| Lote | Escopo | Estado |
|------|--------|--------|
| **N-B** | Criar `Lib/GdiMvcJsonResults.cs` | ✅ 2026-05-25 |
| **N-A** | 4 gaps gc GED/medições | ✅ 2026-05-25 |
| **N-C** | Delegar `JsonDataTableException` privados → Lib (**31** controllers Areas a/g/gc/qa) | ✅ 2026-05-25 — `Scripts/2026_06_05_gdi_delegate_json_datatable_exception_to_lib.py` |
| **N-D** | Ajax wrappers `MovimentosController` + `*Mensagem` para modais GET; piloto `FinanceiroLancamentos` GED | ✅ 2026-05-25 |
| **N-E** | Modal GET COMEX/estoque/boleto + remoção `GC.Collect` gc | ✅ 2026-05-25 |
| **N-F** | Modal GET gc restante (CFOP, COMEX financeiro/invoices/uploads, produto desativar, `ModalEditarItem`) + 11 views | ✅ 2026-05-25 |
| **N-G** | `ComexProdutos.ModalCreateEdit`, `FinanceiroLancamentos.ModalCreateEditLancamento` + views | ✅ 2026-05-25 |
| **N-H** | Estoque conferência importação/estoque + inventário item + view inventário | ✅ 2026-05-25 |
| **N-I** | Ajax wrappers `FinanceiroLancamentos` + `ComexImportacoes` (7+3 actions `{ success, msg }`) | ✅ 2026-05-25 |
| **N-J** | `JsonDataTableException` em 7 controllers gc (`Estoque`, `ComexProdutos`, `EstoqueInventario`, `ComexInvoices`, `MovimentosEntradas`, `ComexFinanceiro`, `FinanceiroLancamentos`) | ✅ 2026-05-25 — `Scripts/2026_06_05_gdi_add_json_datatable_exception_gc_inline.py` |
| **N-K** | Ajax wrappers `ComexInvoices` + `ComexFinanceiro` + `ComexProdutos` (2+2+3 actions `{ success, msg }`) | ✅ 2026-05-25 |
| **N-L** | Ajax wrappers `Estoque` + `EstoqueInventario` + `MovimentosEntradas` (5+3+3 actions `{ success, msg }`) | ✅ 2026-05-25 |
| **N-M** | Ajax wrappers `EstoqueLotes` + `EstoqueControle` + `CfopOperacoes` + `CfopParametros` (1+3+1+1 actions) | ✅ 2026-05-25 |
| **N-N** | Ajax wrappers relatórios gc — 10+4+1 actions export (`catch` → Lib; sucesso com `idProcessamento`) | ✅ 2026-05-25 |
| **N-O** | Varredura final gc — 7 exports residuais + `AjaxFailureMessage`/`AjaxFailureValidationMessage` na Lib | ✅ 2026-05-25 |
| **N-P** | Inventário residual gc + LookupAjax + `GetDados` COMEX (`2026_06_05_gdi_inventory_gc_ajax_exceptions_residual.py`) | ✅ 2026-05-25 |
| **N-P.1** | Inventário ERP a/crm/g/qa/gc + `EstoqueInventarioService`; LookupAjax **g**; 56 candidatos N-Q+ (`2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py`) | ✅ 2026-05-25 |
| **N-Q** | `Areas/qa` indicadores — KPI/gráfico/DT/export catch → `GdiMvcJsonResults` | ✅ 2026-05-25 |
| **N-Q** | `Areas/a` `ParametrosController` — Ajax ativar/desativar sistema → `JsonAjaxErro*` | ✅ 2026-05-25 |
| **N-R.1** | `Areas/g` `NfeController` — Ajax e-Notas + `JsonAjaxErroIdProcessamento` (InnerException) | ✅ 2026-05-25 |
| **N-R.2** | `Areas/g` `FinanceiroController` — Ajax financeiro + gráfico Flot + DT sucesso | ✅ 2026-05-25 |
| **N-R.3** | `Areas/g` `GedController` — Ajax GED `ComTag`/`ComUrl` + `ged_partial` edit | ✅ 2026-05-25 |
| **N-R.4** | `Areas/g` `ClientesController` — Ajax modais + `JsonAjaxErroComContato` + ModelState | ✅ 2026-05-25 |
| **N-R.5** | `Areas/g` `Atendimentos`/`Produtos`/`ProdutosNcm`/`Usuarios` — Ajax + DT sucesso; modais GET Atendimentos | ✅ 2026-05-25 |
| **N-S.1** | `Areas/gc` `ComexProdutosController` — `JsonAjaxErro*IdProcessamento` + modal GET | ✅ 2026-05-25 |
| **N-S.2** | `Areas/g` onda `mvc_modelstate` — 12 cadastros Create/Edit | ✅ 2026-05-25 |
| **N-S.3** | `Areas/gc` modais GET — `AjaxFailureMessage` (10×) | ✅ 2026-05-25 |
| **N-T.1** | `Areas/g` `ContratosAviacao` + `ImportacoesBancarias` — `JsonAjaxErro*IdProcessamento` | ✅ 2026-05-25 |
| **N-T.1b** | `Areas/gc` `MovimentosEntradas` — `JsonAjaxErro*IdProcessamento` | ✅ 2026-05-25 |
| **N-T.2** | `Areas/gc` onda `mvc_modelstate` (12×) | ✅ 2026-05-25 |
| **N-T.3** | `Areas/g` `Nfe.Edit` POST `modal_flash` | ✅ 2026-05-25 |
| **N-U.1** | `Areas/gc` `ComexInvoices` upload xlsx → `JsonAjaxErro*` | ✅ 2026-05-25 |
| **N-U.2** | `Areas/gc` `ComexImportacoes` parse/clipboard → `AjaxFailure*Message` | ✅ 2026-05-25 |
| **N-U.3** | `AjaxFailureWebMessage(WebException)` + MovimentosEntradas/ComexImportacoes DT | ✅ 2026-05-25 |
| **N-U.4** | `Areas/crm` `PedidosController` → `JsonAjaxErro*IdProcessamento` | ✅ 2026-05-25 |
| **N-V** | Residual gc (12×) → `AjaxFailure*Message` em acumuladores/service | ✅ 2026-05-25 |
| **N-W** | DT sucesso literais → `DataTableSuccess*` (31 controllers) | ✅ 2026-05-25 |
| **N-X** | `GetFichaEstoqueProduto` Fase 8 | ✅ 2026-05-25 |
| **N-Y** | `.csproj` `ModalTransferirContaCaixa` | ✅ 2026-05-25 |
| **C-1** | Cliente gc/Movimentos `error:` Ajax (24 views) | ✅ 2026-05-25 |
| **L-1** | `AjaxFailureIdProcessamento` na Lib | ✅ 2026-05-25 |

**Inventário ERP:** `Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py --areas all` → **exit 0, 0 ocorrências**.

### Wrapper privado (padrão N-C — manter nas actions)

```csharp
private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
{
    return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
}
```

Variante sem `yesFilterOnOff` no método: passar `"0"` como terceiro argumento.  
**Não** remover o wrapper nas actions existentes — só o corpo delega à Lib.
