<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: CHANGELOG-DEV.md (raiz) | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Gerado automaticamente. Histórico completo: `CHANGELOG-DEV.md` e `docs/dev-history/`.

---

## Últimas alterações (5)

### 2026-05-25 — gc/EstoqueControle: coluna Validade só com data (sem hora)
- `EstoqueControleController.GetDados`: `data_validade` formatada `dd/MM/yyyy` (alinhado a `EstoqueLotes`).

---

### 2026-05-25 — gc/EstoqueLotes: scroll horizontal desnecessário no DataTable Documentos
- `ModalCreateEdit.cshtml`: bloco GED com `gdi-dt-scroll-host min-w-0` (remove `overflow-x: auto` inline); botão «Incluir Anexos» fora do host; `drawCallback` com `columns.adjust()`; `#divDocsEstoqueLote` com `min-width: 0`.

---

### 2026-05-25 — gc/EstoqueControle: LibMessageProcessando ao abrir modal CreateEdit
- `Index.cshtml`: `LibMessageProcessandoHide` duplo no callback do `#mainModal.load` + erro de load; `drawCallback` da grelha com hide; `ModalCreateEditEstoqueControle`: `jsInitModal` antes dos datepickers + hide no `catch` (contador `waitingDialog`).

---

### 2026-05-25 — gc/EstoqueLotes: modal CreateEdit em `modal-full`
- `ModalCreateEdit.cshtml`: `modal-xl` → `modal-full modal-dialog-scrollable` (~90% largura, `start.css`); scroll no body do modal.

---

### 2026-05-25 — gc/EstoqueControle: CRUD modal CreateEdit
- `ModalCreateEditEstoqueControle` + Index (`jsModalCreateEditEstoqueControle`); GET `Create`/`Edit` → `Index`; reutiliza `AjaxSaveRecord`; `DataRowInUseSerialized` na edição; abas Dados/Aferições em `modal-full`.

---
