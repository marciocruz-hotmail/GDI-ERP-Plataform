# TempData legado — inventário e modernização sugerida

**Data:** 2026-05-20  
**Inventário:** `grep TempData` no repo + cruzamento com `Scripts/2026_05_20_gdi_inventory_legacy_filter_server.py`

---

## 1. TempData do filtro genérico (morto)

**Onde:** `Controllers/UserIdentityController.cs` — action `Index` (GET login), linhas ~119–124.

| Chave TempData | Operação | Escritor ativo no repo |
|----------------|----------|------------------------|
| `yesFilterField` | `Remove` | **Nenhum** |
| `yesFilterOperador` | `Remove` | **Nenhum** |
| `yesFilterText` | `Remove` | **Nenhum** |
| `yesFilterOn` | `Remove` | **Nenhum** |
| `yesFilterController` | `Remove` | **Nenhum** |
| `yesFilterControllerTemp` | `Remove` | **Nenhum** |

**Contexto histórico:** o modal de filtro genérico (`Areas/a/Filtros`, removido) gravava estado em TempData entre redirects. A UI passou a usar apenas `yesFilterField` no Ajax DataTables + persistência em **`CachePersister.userIdentity.allFiltros`** (`LibDB.getFilterByUser` / `setFilterByUser`).

**Não confundir com:**

- `yesFilterOnOff` — propriedade do **JSON** DataTables (indicador visual via `GdiAtualizarIndicadorFiltro` em `start.js`).
- `yesFilterField` / `yesFilterController` — hiddens **vivos** nas views Index (enviados no `data` do DataTables, não em TempData).

**G-FLT-04 (concluído 2026-05-20):** removido o bloco de 6× `TempData.Remove` em `UserIdentityController.Index`. Opcional futuro: no `CachePersister.logout()` limpar `userIdentity.allFiltros` explicitamente.

---

## 2. Contrato `jQueryDataTableParamModel` — ramo LibDB inalcançável

**Onde:** `Lib/LibDB.cs` → `getFilterByUser`, primeiro `if` com `yesFilterOperador` + `yesFilterText` + `SentencaSQLFiltroGenerico`.

**Estado:** views e Ajax **não** enviam mais `yesFilterOperador` / `yesFilterText` (removidos em 2026-05-19). O ramo só seria acionado por binding manual ou ferramentas externas.

**Fluxo atual desejado:**

1. `yesFilterField == "*"` → limpar filtro em `allFiltros`.
2. Caso contrário → ler filtro persistido por `token` + `controllerName`.
3. Filtros inline → `yesCustomField*` / SQL dedicado no controller (ex.: `ClientesController`).

**Modernização (G-FLT-05 / G-FLT-06):**

1. Remover o ramo `SentencaSQLFiltroGenerico` em `getFilterByUser` (e limpeza de `param.yesFilterOperador` / `yesFilterText`).
2. Remover propriedades `yesFilterOperador` e `yesFilterText` do modelo.
3. Avaliar remoção ou `[Obsolete]` de `LibStringFormat.SentencaSQLFiltroGenerico` se grep = 0 após passo 1.

**Parâmetro `paramAdvanced`:** ainda passado em `getFilterByUser` / `setFilterByUser` (`g_filtros.advanced`). Hoje quase sempre `false`; revisar em **G-FLT-07** se o campo BD ainda tem significado.

---

## 3. TempData da tela de login (vivo, mas repetitivo)

**Onde:** `UserIdentityController` — chaves `WallPaper`, `SessionID`, `DeviceId`, `Version`.

**Padrão atual:** `Remove` → `TempData[key] = ViewBag` → `Keep` no GET Index; em falhas de login POST, `ViewBag` ← `TempData` + `Keep` repetido em ~5 ramos.

**Problema:** boilerplate alto; risco de divergência entre ViewBag e TempData.

**Modernização sugerida (G-LOGIN-01):**

- Opção A (mínima): método privado `PreserveLoginChrome(ViewBag)` que centraliza leitura/escrita TempData.
- Opção B: DTO `LoginChromeModel` (wallpaper, sessionId, deviceId, version) numa única chave TempData `LoginChrome`.
- Opção C: persistir wallpaper/device só em `Session` após primeiro GET (POST usa Session, não TempData).

**Fora de escopo:** troca de senha obrigatória já usa `TempData["Error"]` / `TempData["Info"]` — padrão PRG válido.

---

## 4. Outros TempData (não são filtro legado)

| Padrão | Exemplos | Sugestão backlog |
|--------|----------|------------------|
| Flash PRG `TempData["message"]` | `AtendimentosController`, `FinanceiroLancamentosController`, `NfeController`, `Cfop*` | **G-UX-01** — partial `_FlashMessages` + convenção de chaves (`Success`/`Error`) |
| Auth / sessão | `CustomAuthorizeAttribute` → `TempData["Info"]` timeout | Manter; documentar em contexto auth |
| Logout inatividade | `TempData["Info"]` em `Logout` | Manter |
| Comentário morto | `UsuariosController` `// TempData["IdColigada"]` | **G-ENC-03** — remover comentário |

---

## Verificação rápida

```powershell
# TempData yesFilter (só Remove no login)
rg "TempData.*yesFilter" --glob "*.cs"

# Propriedades modelo + LibDB
rg "yesFilterOperador|yesFilterText" --glob "*.cs"

# Filtros vivos (memória)
rg "allFiltros" --glob "*.cs"
```

---

## Implementado (2026-05-20)

| ID | Resultado |
|----|-----------|
| G-FLT-04 | Removido `TempData.Remove(yesFilter*)` no login |
| G-FLT-05–06 | `LibDB.getFilterByUser` simplificado; modelo sem operador/texto |
| G-FLT-07 | `paramAdvanced` removido só de `getFilterByUser`; `setFilterByUser(..., advanced)` + `recordFiltro.advanced` mantidos |
| G-LOGIN-01 | Helpers `SaveLoginChromeToTempData` / `ApplyLoginChromeToViewBag` |
| G-UX-01 | `Lib/LibFlashMessage.cs` |
| G-ENC-03 | Comentário `IdColigada` removido |
