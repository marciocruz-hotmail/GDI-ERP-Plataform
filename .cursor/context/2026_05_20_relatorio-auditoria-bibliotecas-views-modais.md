# Relatório — Auditoria cruzada: componentes visuais vs bibliotecas (GDI-ERP-Plataform)

**Data:** 2026-05-21  
**Escopo:** 242 ficheiros `.cshtml` em `Areas/` e `Views/` (excl. partials `_Layout*` de infraestrutura)  
**Ferramentas:** `Scripts/2026_05_20_gdi_inventory_page_scripts.py`, `Scripts/2026_05_20_gdi_cross_audit_view_libraries.py`, `Scripts/2026_05_20_gdi_audit_tempus_loading.py`  
**Dados brutos:** `.cursor/context/2026_05_20_cross_audit_view_libraries.json`, `2026_05_20_layout-scripts-inventario.json`

---

## 1. Resumo executivo

A arquitetura **G-PERF-20** (carregamento condicional por `GdiPageScriptsFlags` + lazy-load em modais via `GdiMainModalLoad`) está **bem desenhada**, mas encontra-se num **estado intermédio incompleto**:

| Situação | Impacto |
|----------|---------|
| **Tempus** retirado do `_Layout` global | Correto para performance |
| Partials `_LayoutHead/ScriptsTempus` **criados mas em 0 views** | **14 hosts** quebram no `$(function(){ jsDatepicker* })` |
| Flag `TempusDominus` (8) **nunca aplicada** em `Resolve()` nem nos partials opcionais | Impossível ligar Tempus só por atributo/filter |
| **Área `a`** (`Parametros/Index`) com DataTables sem flag DT | Grelha não inicializa |
| **`qa/GedSGQ/IndexPops`** com jstree e preset `LayoutLite` | Árvore não inicializa |
| **28 modais** com datepicker | Dependem de **lazy-load**; funcionam se `GdiMainModalLoad`/`#mainModal.load` correr após `GdiEnsureScriptFlags` |

**Conclusão:** não é necessário repensar toda a stack; é necessário **fechar o ciclo G-PERF-20** — integrar Tempus ao mesmo mecanismo de flags que DT/S2/jstree **ou** aplicar partials em massa + verificação automática em CI.

---

## 2. Arquitetura atual (referência)

```
┌─────────────────────────────────────────────────────────────┐
│ _Layout.cshtml                                               │
│  ├─ _LayoutScriptsAuthenticated (jQuery, Swal, AdminLTE,   │
│  │    GdiPageScriptRegistry)                                 │
│  ├─ @RenderBody() ← view/modal fragment                     │
│  ├─ _LayoutScriptsOptional ← flags ViewBag.GdiPageScripts    │
│  │     • DataTables | Select2 | jstree | Toggle              │
│  │     • bootstrap.bundle se sem DT                          │
│  │     • (Tempus NÃO está aqui)                            │
│  ├─ start.js (LibMessage*, GdiDt*, jsDatepicker*, modais)    │
│  └─ sessão / sidebar                                         │
└─────────────────────────────────────────────────────────────┘

Modais (_Modal.cshtml): HTML+scripts injetados em #mainModal
  → GdiDetectScriptFlagsFromHtml → GdiEnsureScriptFlags (lazy)
```

| Componente visual | Biblioteca | Onde deve carregar |
|-------------------|------------|-------------------|
| Grelha Ajax `GetDados` / `.DataTable()` | DataTables bs5 + `gdi-datatables-defaults.js` | Layout flag **DataTables** (2) ou lazy modal |
| Filtro typeahead / `data-gdi-lookup-url` | Select2 + `gdi-select2.js` | Layout flag **Select2** (4) ou lazy modal |
| Árvore hierárquica | jstree | Layout flag **Jstree** (16) |
| Switch `bootstrapToggle` | bootstrap4-toggle | Layout flag **Toggle** (32) |
| Campos data `jsDatepicker*` | Tempus Dominus 6.9.4 + jQuery-provider | **Partial na view host** (desenho atual) **ou** flag **TempusDominus** (8) — **não implementado no layout** |
| Confirmações / erros | SweetAlert2 + `start.js` | **Core** (sempre) |
| Upload ficheiro | `jsFileInputChange.js` | Core (defer global) |
| Modais `#mainModal` | Herda documento + lazy registry | `GdiMainModalLoad` / patch `jQuery.fn.load` |

**Valores típicos `data-gdi-page-scripts` no `<body>`:**

| Valor | Flags |
|-------|-------|
| 39 | Core + DT + S2 + Toggle (default g/gc/qa) |
| 33 | Core + Toggle (LayoutLite / hubs relatório) |
| 49 | Core + DT + S2 + Toggle + Jstree |

---

## 3. Inventário quantitativo (242 views)

| Tipo de layout | Quantidade | Papel |
|----------------|------------|--------|
| **host** (`_Layout.cshtml`) | 96 | Página autenticada; define flags + partials |
| **modal** (`_Modal.cshtml` ou `/Modal*`) | 133 | Fragmento Ajax; herda/lazy libs |
| **blank** | 2 | Assets próprios |
| **other** | 11 | Legado / sem layout padrão |

| Componente detectado no `.cshtml` | Views |
|----------------------------------|-------|
| DataTables | 137 |
| Tempus (`jsDatepicker*`) | 42 |
| `#mainModal.load` / `GdiMainModalLoad` | 46 |
| LibMessage / GdiDt / GdiAjax | 207 |
| Upload ficheiro | 17 |
| jstree | 3 |
| bootstrap4-toggle | 4 |
| Select2 (regex estrita) | 3* |

\* O inventário por regex **subconta** Select2; typeahead em pedidos/clientes usa padrões que nem sempre batem na regex — na prática **IndexPedido** e vários CreateEdit precisam de S2 (já no default 39).

---

## 4. Cruzamento: necessidade vs o que o layout entrega

### 4.1 Hosts com gap **CRITICAL** (16)

Falha no **carregamento da página** (não só no modal):

| View | Precisa | Layout entrega | Gap |
|------|---------|----------------|-----|
| `Areas/a/Views/Parametros/Index.cshtml` | DT | Core + Toggle | **DT ausente** — área `a` fora do default g/gc/qa |
| `Areas/qa/Views/GedSGQ/IndexPops.cshtml` | Jstree | Core + Toggle (LayoutLite) | **Jstree ausente** |
| **14 views** (lista abaixo) | Tempus (+ DT na maioria) | DT+S2+Toggle, **sem Tempus** | **Partials Tempus não incluídos** |

**14 hosts Tempus sem partial** (alerta `[jsDatepicker*] Tempus Dominus não carregado`):

1. `Areas/g/Views/Clientes/CreateEdit.cshtml`
2. `Areas/g/Views/ContratosAviacao/CreateEdit.cshtml`
3. `Areas/g/Views/Financeiro/Index.cshtml`
4. `Areas/g/Views/Ged/Index.cshtml`
5. `Areas/g/Views/Nfe/CreateEdit.cshtml`
6. `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml`
7. `Areas/gc/Views/EstoqueControle/CreateEdit.cshtml`
8. `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml`
9. `Areas/gc/Views/Fretes/Index.cshtml`
10. `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
11. `Areas/gc/Views/Movimentos/IndexPedido.cshtml`
12. `Areas/qa/Views/GedSGQ/IndexAtasReunioes.cshtml`
13. `Areas/qa/Views/GedSGQ/IndexComunicados.cshtml`
14. `Areas/qa/Views/GedSGQ/IndexDocsSGQ.cshtml`

### 4.2 Hubs relatório / cadastro (sem datepicker no Index)

Preset `LayoutHubReport` (33): **sem DT, sem Tempus no layout**.

| Host | Modais com datas | Estado documentado | Risco |
|------|------------------|-------------------|--------|
| `RelatoriosComerciais/Index` | Sim | Deveria ter partial Tempus | Lazy-load no 1.º modal |
| `RelatoriosRegulamentacao/Index` | Sim | Idem | Idem |
| `RelatoriosFinanceiros/Index` | Sim (+ S2) | Idem | Idem |
| `RelatoriosCadastrais/Index` | Possível | C+TG | Modais simples |
| `Clientes/Index`, `Nfe/Index` | Modais com datas | Lista 22 hosts — **partial ausente** | Lazy-load |
| `Movimentos/PainelPedidos` | Expedição/entrega | Idem | Lazy-load |
| `ComexFinanceiro/Index` | Modal COMEX | Idem | Lazy-load |

Estes **não** aparecem nos 16 CRITICAL porque **não** chamam `jsDatepicker` no ficheiro do Index; o risco é **só no modal** (mitigado por `GdiMainModalLoad` se o patch estiver ativo).

### 4.3 Modais (133)

| Categoria | Qtd | Carregamento esperado |
|-----------|-----|------------------------|
| Com `jsDatepicker` | 28 | Lazy Tempus via registry |
| Com DataTables | ~120+ | Lazy DT se host sem flag (hubs) ou herança (host 39) |
| Sem libs opcionais | restantes | Core apenas |

**Regra do projeto:** modais **não** devem incluir partial Tempus; o **host** deve ter Tempus no documento **ou** o lazy-load deve completar **antes** de executar scripts inline do modal.

### 4.4 O que está correto

| Item | Estado |
|------|--------|
| DataTables + Select2 + Toggle no default g/gc/qa | OK na maior parte dos Index/CreateEdit com grelha |
| `[GdiPageScripts]` nos hubs relatório, jstree, portal, lite | OK nos controllers mapeados |
| `GdiPageScriptRegistry` + `GdiMainModalLoad` | OK em `start.js` |
| Core (jQuery, Swal, start.js) | OK em todas as páginas autenticadas |
| Login / `_Blank` com Tempus inline | OK |

---

## 5. Análise da aderência à arquitetura

### 5.1 Pontos fortes

1. **Separação Core vs opcionais** — reduz payload em hubs e portal.
2. **Lazy modal (20f)** — permite hubs sem DT no primeiro paint.
3. **Partials por biblioteca** — manutenção clara (`_LayoutScriptsDataTables`, etc.).
4. **Inventário automatizável** — scripts Python reproduzíveis.

### 5.2 Falhas de aderência (causa raiz)

| # | Problema | Porquê acontece |
|---|----------|-----------------|
| 1 | **Tempus “orfão”** | Removido do `_Layout` sem completar alternativa (partials não aplicados; flag 8 não ligada aos partials) |
| 2 | **Duas fontes de verdade** | Documentação lista 22 hosts; código tem 0 partials; `gdi_apply_tempus_partials.py` nunca corrido (ou revertido) |
| 3 | **Área `a` ignorada** | `Resolve()` só trata g/gc/qa/crm para DT |
| 4 | **LayoutLite vs conteúdo** | `IndexPops` em lite mas usa jstree |
| 5 | **Init síncrono vs defer** | `$(function(){ jsDatepicker })` na view corre **antes** de garantir libs se não houver partial/flag |
| 6 | **Verificação não bloqueante** | Inventário existe; não há gate de build que impeça publish com gaps |

### 5.3 Avaliação: ajustar arquitetura?

**Não recomendado** voltar ao monólito `FullAuthenticated` em todo o `_Layout` (perda de ganho G-PERF-20).

**Recomendado** evoluir a arquitetura actual com **uma única regra**:

> **Toda biblioteca opcional deve ser acionada por `GdiPageScriptsFlags` nos partials `_LayoutHead/ScriptsOptional`, incluindo TempusDominus.**

Partials manuais por view tornam-se **opcionais** (override fino) ou eliminam-se após migração para flags.

---

## 6. Sugestão de ajustes globais

### Fase 1 — Correção imediata (baixo risco)

1. **Executar** `python Scripts/2026_05_20_gdi_apply_tempus_partials.py` nos 22 hosts documentados (+ validar com `2026_05_20_gdi_audit_tempus_loading.py` → 0 hosts em falta).
2. **Corrigir excepções pontuais:**
   - `[GdiPageScripts(GdiPageScriptsFlags.DefaultGcG)]` ou `DataTables` em `Areas/a/Controllers/ParametrosController` action `Index`.
   - `[GdiPageScripts(GdiPageScriptsFlags.LayoutHubJstree)]` ou remover de `LayoutLiteActions` para `GedSGQ/IndexPops`.
3. **Smoke manual** (matriz em `2026_05_20_layout-scripts-matriz-modais.md`): IndexPedido, FinanceiroLancamentos, Ged, Relatórios ANP, Parametros.

### Fase 2 — Aderência estrutural (médio prazo)

| Alteração | Ficheiros | Benefício |
|-----------|-----------|-----------|
| Incluir **Tempus** em `_LayoutHeadOptionalScripts` e `_LayoutScriptsOptional` quando `(flags & TempusDominus)` | 2 partials + `GdiPageScripts.cs` | Uma só via de carga |
| Em `GdiPageScriptsDefaults.Resolve`, se view/controller está na lista **TEMPUS_HOSTS** (gerada pelo script de inventário), OR flag `TempusDominus` | `GdiPageScripts.cs` | Sem esquecer partial por view |
| Novo helper **`GdiPageScriptsForView(string path)`** ou filter que lê manifest JSON | `Lib/GdiPageScripts.cs` | Flags alinhadas ao conteúdo real |
| **`GdiInitPageScripts()`** em `start.js`: após DOM ready, se `data-gdi-defer-tempus` no body, `GdiEnsureScriptFlags(8, fn)` antes de init de datepickers globais | `start.js` | Hosts sem partial explícito ainda funcionam |

### Fase 3 — Governança (contínuo)

1. **`python Scripts/2026_05_20_gdi_cross_audit_view_libraries.py`** no checklist pré-publish; **exit code 1** se existir `critical_hosts`.
2. Atualizar `2026_05_20_layout-scripts-contrato-flags.md` — corrigir “Tempus: 3 views” (inventário real: **41–42**).
3. Documentar em `AI-CONTEXT.md` / `CLAUDE.md`: matriz componente → flag → verificação.
4. Opcional: atributo `data-gdi-require-scripts="8"` nos hosts com filtro de data no HTML estático.

### Fase 4 — Modais (reforço)

1. Auditar que **todos** os `$("#mainModal").load` passam pelo patch → `GdiMainModalLoad` (grep residual).
2. Modais críticos com muitas libs: `data-gdi-require-scripts="10"` (DT+Tempus) no `.modal-dialog` raiz.
3. Evitar `jsDatepicker` no `$(function(){})` do modal **antes** do callback de `GdiMainModalLoad` — padrão: init no callback após `.load` (já usado em várias views).

---

## 7. Matriz resumida — componente × responsabilidade

| Componente | Detecção na view | Quem carrega hoje | Gap | Ação global sugerida |
|------------|------------------|-------------------|-----|----------------------|
| DataTables | `.DataTable(`, `bServerSide` | Flag 2 no layout | Área `a` | Estender `Resolve` ou atributo no controller |
| Select2 | typeahead / `.select2` | Flag 4 (default gc/g) | Subdetecção regex | Manter default 39 em comercial |
| Tempus | `jsDatepicker*` | **Ninguém** (hosts) | **14 hosts** | Flag 8 + partials OU apply script |
| jstree | `.jstree(` | Flag 16 | IndexPops lite | Atributo `LayoutHubJstree` |
| Toggle | `bootstrapToggle` | Flag 32 | Raro | OK |
| Swal/LibMessage | `LibMessage*` | Core | — | OK |
| Modal DT/Tempus | scripts no partial | Lazy registry | Hubs | Tempus no host hub OU lazy OK |

---

## 8. Conclusões finais

1. O ERP **não** tem um problema de escolha de bibliotecas; tem um problema de **completude da migração G-PERF-20c-bis (Tempus)** e **duas rotas fora do default** (área `a`, `IndexPops`).
2. **96 hosts** e **133 modais** foram cruzados; **16 hosts** falham de forma determinística; **28 modais** com datas dependem de lazy-load — aceitável se Fase 1 Tempus nos hosts principais estiver feita.
3. A arquitetura por **flags + registry + lazy modal** deve **manter-se**; o ajuste global é **incluir Tempus no mesmo sistema de flags** e **verificação automática**, não voltar a carregar tudo no `_Layout`.
4. Os casos reportados (**IndexPedido**, **Financeiro Lançamentos**, **anexo** no mesmo ecrã) encaixam nos **14 hosts Tempus** — o botão anexo não chama Tempus; o alerta aparece no **filtro de datas do Index** ao carregar a página.

---

## 9. Artefatos gerados nesta auditoria

| Ficheiro | Uso |
|----------|-----|
| `Scripts/2026_05_20_gdi_cross_audit_view_libraries.py` | Reexecutar auditoria |
| `.cursor/context/2026_05_20_cross_audit_view_libraries.json` | Detalhe por view (`gaps`, `components`, `needs`) |
| `Scripts/2026_05_20_gdi_audit_tempus_loading.py` | Foco Tempus |
| `Scripts/2026_05_20_gdi_apply_tempus_partials.py` | Correção em massa (22 hosts) |

---

## 10. Registo sugerido CHANGELOG-DEV

| Data | Resumo |
|------|--------|
| 2026-05-21 | Auditoria cruzada 242 views: 16 hosts CRITICAL (14 Tempus sem partial, Parametros/Index sem DT, GedSGQ/IndexPops sem jstree); relatório e script `2026_05_20_gdi_cross_audit_view_libraries.py`; proposta integrar TempusDominus nos partials opcionais do layout. |
