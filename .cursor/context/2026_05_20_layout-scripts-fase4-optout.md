# G-PERF-20 — Fase 4: Opt-out por controller (PRs pequenos)

**Data:** 2026-05-20  
**VersionERP:** 2026.51.11  
**Mecanismo:** `[GdiPageScripts(...)]` na action (ou fallback `NoDataTablesControllers` em `GdiPageScriptsDefaults`)

---

## Lotes (PRs)

### Lote A — Hubs relatório + jstree + parâmetros (`2026.51.08`)

| Área | Controller | Action | Preset |
|------|------------|--------|--------|
| gc | RelatoriosComerciais | Index | `LayoutHubReport` |
| gc | RelatoriosRegulamentacao | Index | `LayoutHubReport` |
| gc | RelatoriosCadastrais | Index | `LayoutHubReport` |
| gc | RelatoriosFinanceiros | Index | `LayoutHubReportSelect2` |
| gc | Parametros | Index | `LayoutLite` |
| g | CentrosCustos | Index | `LayoutHubJstree` |
| g | ClassificacaoFinanceira | Index | `LayoutHubJstree` |

**Tempus:** partial na view onde modais usam `jsDatepicker` (Relatórios Comercial/Regulamentação/Financeiros; ver `2026_05_20_layout-scripts-tempus-hosts.md`).

### Lote B — QA treinamentos + portal cliente (`2026.51.10`)

| Área | Controller | Action | Preset | Evidência |
|------|------------|--------|--------|-----------|
| qa | Treinamentos | Index | `LayoutLite` | Index sem DataTables |
| qa | Treinamentos | IndexTreinamentoAviacao001 | `LayoutLite` | Vídeo/PDF, sem grelha |
| crm | Pedidos | Index | `LayoutPortalCliente` | Lista MVC, sem DT/S2 |

### Lote C — Actions sem DT/S2 na view (`2026.51.11`)

Registo central em `GdiPageScriptsDefaults.LayoutLiteActionsByController` (Create/Edit e formulários validados no inventário).

| Área | Controller | Actions | Preset |
|------|------------|---------|--------|
| qa | GedSGQ | IndexPops | `LayoutLite` |
| qa | Treinamentos | IndexTreinamentoAviacao001 | `LayoutLite` |
| g | CentrosCustos, ClassificacaoFinanceira, Cidades, ContasCaixas, Filiais, PagRec*, Perfis, ProdutosNcm, UF, Usuarios | Create, Edit | `LayoutLite` |
| gc | Cfop, FinanceiroParametroDifal | Create, Edit | `LayoutLite` |
| gc | ComexProdutos | FormProcessarProdutosPre* | `LayoutLite` |
| gc | MovimentosEntradas | FormProcessarNF* | `LayoutLite` |

**Fora de escopo:** Index com grelha DataTables (`IndexPedido`, `PainelPedidos`, `ComexFinanceiro/Index`, etc.).

**Inventário:** `Scripts/2026_05_20_gdi_inventory_layout_no_datatables.py` (todas as views `_Layout` sem DT).

Novos opt-outs: atualizar mapa C# + verify script + smoke se rota crítica.

---

## Presets disponíveis

| Preset | Flags |
|--------|-------|
| `LayoutHubReport` | Core + Toggle |
| `LayoutHubReportSelect2` | Core + Toggle + Select2 |
| `LayoutHubJstree` | Core + Toggle + Jstree |
| `LayoutLite` | Core + Toggle |
| `LayoutPortalCliente` | Core + Toggle |
| *(default g/gc/qa)* | `DefaultGcGArea` = Core + DT + S2 + Toggle |

---

## Como acrescentar um controller

1. Confirmar na view: sem `.DataTable(` no HTML da página pai (modais herdam layout pai).
2. Confirmar libs dos modais (matriz `2026_05_20_layout-scripts-matriz-modais.md`).
3. Aplicar `[GdiPageScripts(GdiPageScriptsFlags.…)]` na **action** que devolve a view com `_Layout`.
4. Se modal usa `jsDatepicker`: incluir `_LayoutHead/ScriptsTempus` na view host.
5. Atualizar `Scripts/2026_05_20_gdi_page_scripts_smoke_manifest.py` e correr verify.

---

## Verificação

```bash
python Scripts/2026_05_20_gdi_verify_page_scripts_resolve.py
python Scripts/2026_05_20_gdi_inventory_index_no_datatables.py
```

Smoke DevTools: `data-gdi-page-scripts` no `<body>` — hub relatório `33`, portal `33`, lite `33`, default pedidos `39` (1+2+4+32).

---

## Referências

- Fase 3 infra: `2026_05_20_layout-scripts-fase3-infra.md`
- Tempus hosts: `2026_05_20_layout-scripts-tempus-hosts.md`
