# Inventário — LibExceptions residual em `Areas/gc` (lote N-P)

> **Script gc:** `Scripts/2026_06_05_gdi_inventory_gc_ajax_exceptions_residual.py` (atalho `--areas gc`)  
> **Script ERP:** `Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py` (`--areas all`)  
> **Exit 0** = todas as ocorrências classificadas (gc: 63 incl. `EstoqueInventarioService`).  
> **Exit 1** = `unclassified` ou lookup Ajax ainda com `LibExceptions` direto.  
> **Inventário completo ERP:** `.cursor/context/2026_06_05_erp_ajax-exceptions-residual-inventory.md`

---

## Categorias preservadas (não migrar para `JsonAjaxErro*`)

| Categoria | Qtd | Motivo |
|-----------|-----|--------|
| `upload_clipboard` | 4 | `ComexImportacoes` — xlsx + `Clipboard.SetText` no catch |
| `upload_xlsx` | 6 | `ComexInvoices` — import/reprocessar FOB/XLS |
| `upload_parse_loop` | 6 | parse célula/linha planilha — erro parcial em loop |
| `upload_cleanup` | 2 | `MovimentosEntradas.AjaxImportarNFEntrada` — `File.Delete` temp |
| `cleanup_financeiro` | 2 | `AjaxModalGerarFinanceiroMovimentos` — rollback + audit |
| `id_processamento` | 6 | lote com `idProcessamento` (catch externo já migrado N-N/O) |
| `webexception` | 7 | `WebException` / robô e-Notas / transferir filial |
| `loop_partial` | 4 | WhatsApp / boleto online — acumula em `MsgRetorno` |
| `modal_get` | 10 | modal GET → `LibFlashMessage` + `ModalError` |
| `mvc_modelstate` | 12 | POST MVC `Create`/`Edit` com `ModelState` |

---

## Alinhamentos N-P (código)

| Ficheiro | Alteração |
|----------|-----------|
| `*LookupAjax.cs` (3) | `JsonLookupError(GdiMvcJsonResults.AjaxFailureMessage(ex))` |
| `ComexImportacoesController.GetDados` | `errorMessage` via `AjaxFailure*Message`; `WebException` mantém `getWebException` |

---

## Serviço gc (N-P.1)

| Categoria | Qtd | Ficheiro |
|-----------|-----|----------|
| `service_msg_append` | 4 | `EstoqueInventarioService` — `MovimentarEstoque` / `ValidarEstoque` |

## Próximos lotes (N-Q+)

- Ver `.cursor/context/2026_06_05_erp_ajax-exceptions-residual-inventory.md` — candidatos em `g`, `qa`, `a`.
- Upload xlsx gc: avaliar `JsonAjaxErro` + `Clipboard` sem mudar contrato.
- `ComexImportacoes.GetDados`: wrapper `JsonDataTableException` unificado (hoje inline por `WebException`).
