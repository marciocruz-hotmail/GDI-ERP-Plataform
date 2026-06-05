# Inventário ERP — LibExceptions residual (lotes N-P … N-V)

> **Script:** `Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py`  
> **Exit 0** = nenhuma ocorrência `LibExceptions` em `Areas/*/Controllers` (+ Services).

---

## Estado final (2026-05-25, pós N-V + N-W)

| Área | Total `LibExceptions` |
|------|----------------------|
| **a** | 0 |
| **crm** | 0 |
| **g** | 0 |
| **gc** | 0 |
| **qa** | 0 |
| **ERP** | **0** |

**Ciclo N encerrado.** Texto de exceção centralizado em `GdiMvcJsonResults.AjaxFailure*Message` / `DataTableError` / wrappers privados.

---

## Lotes de encerramento

| Lote | Escopo |
|------|--------|
| N-V.1 | `loop_partial` — `FinanceiroLancamentos` boleto + `Movimentos` WhatsApp |
| N-V.2 | `cleanup_financeiro` — rollback `AjaxModalGerarFinanceiroMovimentos` |
| N-V.3 | `upload_cleanup` — `MovimentosEntradas` NF XML temp |
| N-V.4 | `service_msg_append` — `EstoqueInventarioService` |
| N-W | `errorMessage = ""` → `DataTableSuccessErrorMessage` (31 controllers) |
| N-X | `ProdutosController.GetFichaEstoqueProduto` — try/catch Fase 8 |
| N-Y | `ModalTransferirContaCaixa.cshtml` no `.csproj` |
| C-1 | Cliente gc/Movimentos — handlers `error:` sem `result` (24 views) |
| L-1 | `GdiMvcJsonResults.AjaxFailureIdProcessamento` |

---

## Fase 18 — concluída (2026-05-25)

| Lote | Escopo |
|------|--------|
| C-2 | 115 views a/g/gc/qa — handlers Ajax homogéneos (`fix_ajax_error_handlers_erp.py`) |
| I-1 | `FinanceiroController` — 4 modais GET com `int.TryParse` |

**Inventário cliente:** `Scripts/2026_06_05_gdi_inventory_ajax_hybrid_handlers.py` → exit 0.

## G-PUB + I-1b (2026-06-05)

| Lote | Escopo |
|------|--------|
| G-PUB | `Scripts/2026_06_05_gdi_smoke_architecture_inventories.py` — 5 inventários exit 0 |
| I-1b | `Relatorios*` + `Ged.GetDados` — 18× `LibNumbers.ConvertInt` |

## Próxima etapa sugerida

1. **I-1c (opcional):** `FinanceiroController` + `ImportacoesBancarias` — `int.Parse` residual **g**.
2. Trilhas **G-PERF** / **G-ARC** conforme `BACKLOG-DEV.md`.
3. Smoke manual pós-publish (pedido, export Excel, modal financeiro).
