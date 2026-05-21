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
| `Controllers/jQueryDataTableParamModel` | `yesFilterField`, `yesFilterController`, custom fields. **Removidos 2026-05-20:** `yesFilterAdvancedText`, `yesFilterOperador`, `yesFilterText`. |
| `Lib/LibStringFormat.SentencaSQLFiltroGenerico` | `[Obsolete]` — não chamado após G-FLT-05 |
| `Lib/LibDB.cs` | Persistência `g_filtros` (não remover sem revisão global) |

## Concluído (2026-05-20)

- FLT-1: `qa/GedSGQ`, `gc/EstoqueInventario` — ramos servidor `yesFilterAdvancedText` removidos.
- Propriedade `yesFilterAdvancedText` removida de `jQueryDataTableParamModel.cs`.

## Concluído (G-FLT-04…07, 2026-05-20)

- `LibDB.getFilterByUser(param, controllerName, db)` — sem ramo SQL genérico.
- `setFilterByUser(..., paramAdvanced, db)` — `paramAdvanced` grava `g_filtros.advanced` (filtros inline nas Index).
- Detalhe TempData/login/flash: `.cursor/context/2026_05_20_tempdata-legado-filtro-login.md`.
