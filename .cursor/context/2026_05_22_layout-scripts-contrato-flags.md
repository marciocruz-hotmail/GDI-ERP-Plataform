# G-PERF-20 — Contrato de flags de scripts no layout

**Data:** 2026-05-20  
**Fase:** 0–4 + **20f** concluídas (`2026.51.12`); `2026_05_22_layout-scripts-fase5-lazy-modal.md`; Fase 4 `2026_05_22_layout-scripts-fase4-optout.md`  
**Código:** `Lib/GdiPageScripts.cs`  
**Inventário:** `2026_05_21_layout-scripts-inventario.json` (gerado por `Scripts/2026_05_22_gdi_inventory_page_scripts.py`)

---

## Objetivo

Permitir que `_Layout.cshtml` inclua apenas as bibliotecas necessárias por rota, sem quebrar modais Ajax (`#mainModal.load`) que reutilizam o documento da página pai.

---

## Enum `GdiPageScriptsFlags`

| Flag | Valor | Assets no _Layout (Fase 3+) |
|------|-------|-----------------------------|
| **Core** | 1 | jQuery, OverlayScrollbars, AdminLTE, SweetAlert2 bundle, spin, `start.js`, `jsFileInputChange`, `sessionInactivity`, `gdi-session-handler`, CSS base + `start.css` + `gdi-sidebar-nav` |
| **DataTables** | 2 | `datatables.min.css/js`, `gdi-datatables-bs5.css` |
| **Select2** | 4 | select2 CSS/JS, pt-BR, `gdi-select2.js` |
| **TempusDominus** | 8 | tempus CSS, popper, tempus JS, jQuery-provider (`_LayoutHead/ScriptsOptional`, 2026-05-21) |
| **Jstree** | 16 | jstree CSS/JS |
| **BootstrapToggle** | 32 | bootstrap4-toggle CSS/JS |

Combinações nomeadas:

- `DefaultGcG` = Core \| DataTables \| Select2  
- `FullAuthenticated` = todas (comportamento atual do _Layout)

---

## Regras de resolução (`GdiPageScriptsDefaults.Resolve`)

1. **Core** é sempre aplicado.  
2. Áreas **`g`** e **`gc`**: default `DefaultGcG` + `BootstrapToggle` (modais e forms).  
3. **Jstree** só em `CentrosCustos`, `ClassificacaoFinanceira` (inventário: 2 views).  
4. **TempusDominus** — flag no `_Layout` + mapa de actions em `GdiPageScriptsDefaults` (inventário 2026-05-21); partials por view opcionais/legado.  
5. **`[GdiPageScripts(...)]`** na action ou controller **sobrescreve** o default.  
6. Páginas **hub** com `#mainModal.load` sem DataTables na view: o inventário força sugestão `DataTables|Select2` no default do controller (ver JSON `modal_hosts`).

---

## ViewBag / filter

| Item | Uso |
|------|-----|
| Chave | `ViewBag.GdiPageScripts` (`GdiPageScriptsActionFilter.ViewBagKey`) |
| Filter | `GdiPageScriptsActionFilter` — registado em `FilterConfig` (G-PERF-20d) |
| Layout | `_LayoutPageScriptsInit` → `_LayoutHeadOptionalScripts` + `_LayoutScriptsOptional` |

---

## Partials planejados (Fase 3)

| Partial | Conteúdo |
|---------|----------|
| `_LayoutHead/ScriptsDataTables` | DataTables |
| `_LayoutHead/ScriptsSelect2` | Select2 + gdi-select2 |
| `_LayoutHead/ScriptsBootstrapToggle` | bootstrap4-toggle |
| `_LayoutHead/ScriptsJstree` | jstree (filter `Jstree`) |
| `_LayoutHead/ScriptsTempus` | Tempus (só nas 22 views host, 20c-bis) |

Ordem de scripts no body (Fase 1+): jQuery → Swal bundle → AdminLTE → **partials opcionais** → `start.js` → handlers sessão.

---

## Dependências que não podem sair do Core

- `start.js` — `LibMessage*`, `GdiDt*`, `GdiAjax*`, menu, busy overlay  
- `~/bundles/libui-swal-compat` — antes de `start.js`  
- `gdi-session-handler.js` — após jQuery + swal + start (comentário no _Layout)

---

## Inventário Fase 0 (resumo)

| Métrica | Valor |
|---------|-------|
| Views analisadas | 239 |
| Com DataTables | 136 |
| Com Select2 (typeahead) | 14 |
| Com jstree | 2 |
| Com Tempus | 3 |
| Host `mainModal.load` | 47 |

**Candidatos “layout lite” real** (sem DT/S2/jstree/tempus/modal no grep): poucos (`_ViewStart`, área Shared) — hubs de relatório **precisam** DT/S2 por causa dos modais.

---

## Critérios de aceite por fase

| Fase | Aceite |
|------|--------|
| **0** | Script + JSON + este contrato + `GdiPageScripts.cs`; layout inalterado |
| **1** | `_Layout` + `_LayoutScriptsAuthenticated` (2026.51.04): defer no fim; `start.js` síncrono antes de sessão |
| **2** | Partials `_LayoutHead/Scripts*Jstree|Tempus` em CentrosCustos, ClassificacaoFinanceira, Clientes/CreateEdit |
| **3** | Partials + filter ativo + default g/gc |
| **4** | `[GdiPageScripts]` + presets `LayoutHub*` (lotes A–C, `2026.51.11`) |
| **5 (20f)** | Lazy load modal: `GdiMainModalLoad` / patch `#mainModal.load` (`2026.51.12`) |
