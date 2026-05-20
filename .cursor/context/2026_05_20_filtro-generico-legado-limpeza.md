# Filtro genérico SQL legado — limpeza 2.9 (2026-05-20)

**Inventário:** `python Scripts/2026_05_20_gdi_inventory_legacy_filter_server.py`

## UI (lote 2026-05-19)

- Removidos hiddens `yesFilterOperador`, `yesFilterText`, `yesFilterAdvancedText` de **todas** as views `.cshtml` / Ajax DataTables.
- Mantido `yesFilterField` (`""` / `"*"` para Pesquisar/Limpar e `g_filtros`).
- Área `a/Filtros` (modal genérico) e models `CstFiltroModel` / `ModalFiltroAvancadoView` já removidos do repo.

## Servidor — limpeza 2.9 (`Areas/g` + `ComexProdutos`)

| Controller | Removido | Mantido |
|------------|----------|---------|
| `AssistentesController.GetDados` | `filterAdvanced` / `yesFilterAdvancedText` | `filterDb` (`g_filtros`) |
| `GedController.GetDados` | `filterAdvanced` | Filtro inline `yesCustomField01–03` |
| `NfeController.GetDados` | `filterAdvanced` + `SentencaSQLFiltroGenerico` (operador/text) | `filterDb` ou lista padrão |
| `CentrosCustosController.getDados` | `SentencaSQLFiltroGenerico` | `filterDb` (`g_CentrosCustos`) — método legado (Index = jsTree) |
| `ClassificacaoFinanceiraController.getDados` | idem | idem |
| `ComexProdutosController.GetDados` | bloco `filterAdvanced` (5 campos `;`) | `filterDb` + listagem LINQ |

**`Areas/g`:** grep em `*.cs` → **0** referências a `yesFilterOperador` / `yesFilterAdvancedText` / `SentencaSQLFiltroGenerico`.

## Mantido de propósito (fora 2.9)

| Local | Motivo |
|-------|--------|
| `Controllers/jQueryDataTableParamModel` | Contrato compartilhado; ainda usado em `qa/GedSGQ`, `gc/EstoqueInventario` |
| `Lib/LibStringFormat.SentencaSQLFiltroGenerico` | Helper; pode ser usado por `LibDB` / módulos não migrados |
| `Lib/LibDB.cs` | Persistência `g_filtros` (não remover sem revisão global) |

## Próximo lote (opcional)

- `Areas/qa/Controllers/GedSGQController.cs` — vários `GetDados*` com `filterAdvanced`.
- `Areas/gc/Controllers/EstoqueInventarioController.cs` — `yesFilterAdvancedText`.
