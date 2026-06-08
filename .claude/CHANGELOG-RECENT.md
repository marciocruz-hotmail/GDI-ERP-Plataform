<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: CHANGELOG-DEV.md (raiz) | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Gerado automaticamente. Histórico completo: `CHANGELOG-DEV.md` e `docs/dev-history/`.

---

## Últimas alterações (5)

### 2026-06-08 — Modais scrollable + DataTables: scroll horizontal residual (Lote A + start.css)
- `start.css` (`VersionERP` 2026.51.40): `.modal-dialog-scrollable .modal-body { overflow-x: clip; min-width: 0 }`; `.scroll-modal-horizontal` contido.
- **Lote A (≤10 col / GED/logs):** `ModalFinanceiroViewAnexos`, `ModalHistoricoMovimento`, `ModalViewCartaCorrecao`, `ModalImportacoesLogs`, `ModalConsultaPedidos`, `ModalInvoicesItensEspelhoDigital`, `EstoqueControle/ModalCreateEdit` (medições), `EstoqueLotes` (reforço CSS), `Produtos/ModalCreateEditProduto` (audit), `FinanceiroLancamentos/ModalCreateEditLancamento` (aba GED), `ModalViewFinanceiroMovimentos`, `ModalPedidoSeparacao`, `ModalPedidoExpedicao`, `ModalPedidoEntrega`; `ModalViewNotasFiscais` (14 col — host + scroll interno).
- Script inventário: `Scripts/2026_06_08_gdi_audit_modal_dt_scroll.py`.

---

### 2026-06-08 — gc/Movimentos/ModalPedidoViewAnexos: scroll horizontal residual (padrão EstoqueLotes)
- `ModalPedidoViewAnexos.cshtml`: CSS `#FormModalPedidoViewAnexos .modal-body { overflow-x: hidden }` + `#divDocsPedidoAnexos { min-width: 0 }`; remove wrappers extras; `.row` → `d-flex`; `modal-body p-1`; `drawCallback` zera `scrollLeft`.

---

### 2026-06-08 — gc/Movimentos/ModalPedidoViewAnexos: scroll horizontal desnecessário (gdi-dt-scroll-host)
- `ModalPedidoViewAnexos.cshtml`: padrão `EstoqueLotes/ModalCreateEdit` — `gdi-dt-scroll-host min-w-0` no host da tabela (8 col.); remove `scroll-body-horizontal` + `overflow-x` inline; botão upload fora do host; `drawCallback` + `columns.adjust()`.

---

### 2026-06-08 — gc/Movimentos/ModalPedidoViewAnexos: HTTP 500 — view + controller (arquitetura modal GET)
- `ModalPedidoViewAnexos.cshtml`: estrutura HTML alinhada a `ModalFinanceiroViewAnexos` (`_AlertMsg` + grelha só sem `MsgBloqueio`); corrige `@if`/`</div>` mal aninhados da modernização 2026-06-05; upload via `#id_movimento`.
- `MovimentosController.ModalPedidoViewAnexos`: guard `db`, `PedidoNaoEncontradoMensagem`, try/catch + `LibLogger`; `TryGetMovimentoModal` null-safe se `db` ausente.
- `_Modal.cshtml`: `VersionERP` null-safe (`ControlVersion` fallback).

---

### 2026-06-08 — gc/Movimentos/IndexPedido: anexos inline — confiar DT do host + erro HTTP detalhado
- `start.js` (`VersionERP` 2026.51.38): `gdiHostPageScriptFlags` / `gdiHasDataTablesRelaxed`; não recarrega DataTables/Select2 quando o host já declara o bundle (`data-gdi-page-scripts`); timeout do defer propaga erro; `GdiMainModalLoad` envia `X-Requested-With` e mensagem com HTTP + URL.
- `IndexPedido.cshtml`: botão anexo com `data-id-mov`; clique delegado namespaced; URL com `encodeURIComponent`; evita alerta duplicado (erro fica no `GdiMainModalLoad`).

---
