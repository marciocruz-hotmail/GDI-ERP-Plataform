# G-PERF-20e — Opt-out hubs validados (Fase 4 lote A)

> Lotes completos Fase 4: **`2026_05_22_layout-scripts-fase4-optout.md`** (inclui lote B Treinamentos + portal).

**Data:** 2026-05-20  
**VersionERP:** 2026.51.08  
**Código:** `[GdiPageScripts]` em controllers hub + presets em `GdiPageScriptsFlags`

---

## Presets (enum)

| Flag composta | Valor | Uso |
|---------------|-------|-----|
| `LayoutHubReport` | Core + Toggle | Relatórios com modais só `jsDatepicker` |
| `LayoutHubReportSelect2` | Core + Toggle + Select2 | `RelatoriosFinanceiros` (lookup no modal) |
| `LayoutHubJstree` | Core + Toggle + Jstree | Centros de custo, classificação financeira |
| `LayoutLite` | Core + Toggle | `Parametros` |

**Tempus:** continua fora do `_Layout`; partials `_LayoutHead/ScriptsTempus` nas views host (ver `2026_05_22_layout-scripts-tempus-hosts.md`).

---

## Controllers com `[GdiPageScripts]` na action `Index`

| Controller | Preset |
|------------|--------|
| `gc/RelatoriosComerciais` | `LayoutHubReport` |
| `gc/RelatoriosRegulamentacao` | `LayoutHubReport` |
| `gc/RelatoriosCadastrais` | `LayoutHubReport` |
| `gc/RelatoriosFinanceiros` | `LayoutHubReportSelect2` |
| `gc/Parametros` | `LayoutLite` |
| `g/CentrosCustos` | `LayoutHubJstree` |
| `g/ClassificacaoFinanceira` | `LayoutHubJstree` |

`GdiPageScriptsDefaults.NoDataTablesControllers` mantido como **fallback** se o atributo for omitido.

---

## Smoke manual (G-PERF-M02 amostra)

Script: `python Scripts/2026_05_22_gdi_page_scripts_smoke_manifest.py --markdown`

1. Login ERP → DevTools Network (Disable cache).
2. **`gc/RelatoriosRegulamentacao/Index`**
   - **Não** carregar: `datatables.min.js`, `select2.min.js`
   - **Carregar:** `jstree` ausente; `tempus-dominus` via partial da view
   - Abrir modal ANP → datepicker funciona
   - Anotar **transferred** total (meta &lt; 800 kB vs baseline ~1,2 MB em telas cheias)
3. **`gc/RelatoriosFinanceiros/Index`** — deve incluir **select2**; modal lançamentos com combo cliente OK
4. **`gc/Movimentos/IndexPedido`** — regressão: DT + S2 presentes
5. **`g/CentrosCustos/Index`** — jstree presente; sem DataTables

---

## Correção crítica (20e)

`RelatoriosFinanceiros` usa `LayoutHubReportSelect2` (não `LayoutHubReport`) porque o modal `ModalRelatorioLancamentosFinanceiros` usa `data_gdi_select2` / lookup Ajax.

---

## Referências

- Matriz modais: `2026_05_22_layout-scripts-matriz-modais.md`
- Contrato: `2026_05_22_layout-scripts-contrato-flags.md`
- Checklist performance: `2026_05_22_checklist-performance-erp.md` § G-PERF-M02
