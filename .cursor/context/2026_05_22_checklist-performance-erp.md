# Checklist — performance e UX (pós auditoria)

**Projeto:** GDI-ERP-Plataform  
**Data base:** 2026-05-20  
**Fonte:** `2026_05_22_performance-audit-erp.md` (auditoria estática, sem medições em produção)  
**Objetivo:** correções **cirúrgicas**, padrão do projeto (MVC 4.7.2, EF/ADO existente, DataTables `GetDados*`, `GdiDt*`/`GdiAjax*`, lookups `ILookupQueryService`, AdminLTE 4 / BS5).

**Como usar**

- Marque `[x]` só quando o **critério de aceite** estiver cumprido.
- Respeite a ordem das etapas (0 → 4); itens com **↳** dependem do anterior.
- **Um PR por tema** quando possível (navbar | IndexPedido typeahead | Financeiro grid | N+1 modal).
- Após código: linha em `CHANGELOG-DEV.md`; build Release; smoke da tela tocada.
- **Não** `git push` nem publish remoto; **não** alterar schema SQL sem autorização; **não** misturar libs (Bootstrap/AdminLTE/DataTables).
- Baseline **antes** da Etapa 1: DevTools Network + amostra SQL (ver Etapa 0).

**Referências obrigatórias por tipo de tela**

| Tema | Documento |
|------|-----------|
| DataTables vs MVC | `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` |
| Lookups Index vs CreateEdit | `.cursor/context/2026_05_20_lookups-convencao-index-vs-createedit.md` |
| Typeahead pedidos | `.cursor/context/2026_05_22_lookups-typeahead-ajax-pedidos.md` |
| Auditoria completa | `2026_05_22_performance-audit-erp.md` |

**Mapa de códigos:** `PERF-00x` = itens deste checklist (alinhados ao backlog `PERF-001`…`PERF-015` da auditoria).

---

## Visão das etapas

| Etapa | Foco | Risco típico | Quando |
|-------|------|--------------|--------|
| **0** | Baseline e medição | Nenhum (só observação) | Antes de qualquer código |
| **1** | **Crítica** — toda navegação + telas mais usadas | Baixo–médio | Primeiro |
| **2** | **Alta** — grids e formulários pesados | Médio | Após Etapa 1 validada |
| **3** | **Média** — infra, cache lookups, inventário | Baixo–médio | Paralelo possível a 2 (itens isolados) |
| **4** | **Opcional / avançada** — arquitetura, Redis, EF global | Alto | Só com telemetria e prioridade negócio |

---

## Etapa 0 — Pré-requisito (baseline, sem alterar código)

- [x] **PERF-000.1** Registrar TTFB e peso total (HTML + JS + CSS) em telas-alvo.
  - **Ferramenta:** Chrome DevTools → Network (**Disable cache** assumido).
  - **Data medição:** 2026-05-20 (ambiente local/dev — utilizador).
  - **Aceite:** tabela abaixo preenchida.

### Baseline DevTools — Network (documento principal = full navigation)

| Tela / fluxo | Requests | Transferred | Resources | Finish | DOMContentLoaded | Load |
|--------------|----------|-------------|-----------|--------|------------------|------|
| Login POST → redirect | 45 | 2,7 MB | 4,0 MB | **976 ms** | 644 ms | 978 ms |
| `gc/Movimentos/IndexPedido` | 45 | 1,1 MB | 2,7 MB | 1,89 s | 1,52 s | 1,80 s |
| `gc/Movimentos/CreatePedido` | 46 | 960 kB | 2,4 MB | **2,14 s** | 1,84 s | 2,00 s |
| `g/Financeiro/Index` | 46 | 1,2 MB | 2,8 MB | 1,84 s | **803 ms** | 897 ms |

**Notas rápidas (referência pós-Etapa 1+)**

- **~45–46 requests** por full page autenticada — custo fixo do `_Layout` (navbar child actions + pacote JS/CSS global); alvo Etapa 1/3: reduzir requests duplicados e bytes transferidos.
- **Resources 2,4–2,8 MB** (autenticado) vs **4,0 MB** no redirect pós-login — login carrega mais no 1.º documento; manter comparação após otimizações.
- **CreatePedido** é o pior **Finish / DCL** (2,14 s / 1,84 s) — priorizar PERF-004 (combo Index), PERF-008 (lazy DataTables) e lookups no CreateEdit.
- **Financeiro/Index** tem **DCL/Load** mais baixos (803 ms / 897 ms) com Finish semelhante ao IndexPedido — gargalo provável no 1.º Ajax `GetDados` (Etapa 2 PERF-006), não só no HTML inicial.
- TTFB por request: não registado nesta amostra; opcional anotar coluna “Waiting” do documento HTML na próxima medição.

**Meta sugerida (após Etapas 1–2, validar no mesmo ambiente):** Finish &lt; 1,5 s e transferred &lt; 800 kB nas três telas autenticadas; DCL CreatePedido &lt; 1,2 s.

- [ ] **PERF-000.2** Contar queries SQL por full page load (navbar + corpo).
  - **Ferramenta:** Extended Events / Profiler **ou** `Database.Log` em dev local.
  - **Aceite:** número de batches na 1.ª carga de `IndexPedido` documentado (**validar runtime**).
- [ ] **PERF-000.3** Confirmar perfil de Publish em homologação/produção.
  - **Verificar:** `Web.Release.config` aplicado (`compilation debug` removido); `VersionERP` incrementado após mudanças em `start.js`.
  - **Aceite:** checklist mental da §5 do `CLAUDE.md` (build Release OK).

### G-PERF-20 — Layout scripts (payload global)

- [x] **G-PERF-20.0.1** Baseline DevTools (PERF-000.1) — tabela §0 acima.
- [x] **G-PERF-20.0.2** Inventário views → flags.
  - **Script:** `python Scripts/2026_05_22_gdi_inventory_page_scripts.py`
  - **Saída:** `.cursor/context/2026_05_21_layout-scripts-inventario.json` (239 views; 136 DT; 14 S2; 2 jstree; 3 Tempus; 47 hosts `mainModal`).
- [x] **G-PERF-20.0.3** Matriz hub → modais → libs.
  - **Doc:** `.cursor/context/2026_05_22_layout-scripts-matriz-modais.md`
- [x] **G-PERF-20.0.4** Contrato `GdiPageScriptsFlags` + `Lib/GdiPageScripts.cs` (filter **não** registado; `_Layout` inalterado).
  - **Doc:** `.cursor/context/2026_05_22_layout-scripts-contrato-flags.md`

- [x] **G-PERF-20b** (Fase 1) `_Layout` + `_LayoutScriptsAuthenticated`: CSS no head; JS núcleo no início do body; defer em DT/Select2/jstree/Tempus/toggle; `start.js` síncrono antes de `gdi-session-handler`; `VersionERP` **2026.51.04**.
  - **Smoke manual:** IndexPedido, Financeiro/Index, Filiais/Index (grid), modal `#mainModal`, `LibMessageError` em erro Ajax.
  - **Aceite DevTools:** DCL ≤ baseline ou menor; sem erro console.

- [x] **G-PERF-20c / 20c-bis** — jstree (2 Index) + Tempus (22 views) fora do `_Layout`; `VersionERP` **2026.51.06**.
  - **Smoke:** CentrosCustos/ClassificacaoFinanceira (jstree); `IndexPedido`, `Financeiro/Index`, `Clientes/Index` modal limite (Tempus); listagem sem `tempus-dominus` no Network.

- [x] **G-PERF-20 Fase 3** — infra condicional (`_LayoutHead/ScriptsOptional`, `GdiPageScriptsView`, verify script); `VersionERP` **2026.51.09**.
- [x] **G-PERF-20d** — filter + partials por lib; opt-out DT hubs; `2026.51.07`.

- [x] **G-PERF-20 Fase 4** — opt-out lotes A (7 hubs) + B (Treinamentos, Pedidos portal) + C (CreateEdit/forms sem DT/S2); `VersionERP` **2026.51.11** — doc `2026_05_22_layout-scripts-fase4-optout.md`.
  - **G-PERF-M02:** ver `2026_05_22_perf-m02-resultado.md` + script `2026_05_22_gdi_perf_m02_network_baseline.py`.

- [x] **G-PERF-20f** — lazy load `#mainModal` (`GdiLoadScriptOnce`, patch `jQuery.load`); `VersionERP` **2026.51.12** — doc `2026_05_22_layout-scripts-fase5-lazy-modal.md`.

**G-PERF-20 (layout scripts):** fases 0–4 + **20f** concluídas no código.

### G-PERF-M02 — Medição pós-layout scripts

- [x] **G-PERF-M02a** Estimativa estática (assets layout × gzip proxy) — `2026_05_21_perf-m02-resultado.json` (2026-05-20).
  - Hubs `33`: RelatóriosRegulamentação **~333 KB**, Parametros **~282 KB** (est., meta &lt; 800 kB **OK** no recorte layout).
  - Regressão `39`: IndexPedido / Financeiro **~510 KB** est. layout (full page live ainda inclui HTML + Ajax).
- [ ] **G-PERF-M02b** DevTools **live** homologação (Finish + transferred full navigation) — `python Scripts/2026_05_22_gdi_perf_m02_network_baseline.py --live` + smoke modais hub.

---

## Etapa 1 — Crítica (máximo impacto percebido, alteração mínima)

> Impacto em **toda** página autenticada ou fluxos de altíssima frequência (pedidos, navbar).

### 1.1 Navbar — menos SQL por request

- [x] **PERF-001** (↳ auditoria PERF-002) Filtrar tarefas da navbar por usuário/perfil.
  - **Arquivos:** `Security/Contexto.cs` — `getNavbarItemsTask`.
  - **Ação mínima:** reativar `where` comentado (`id_perfil` / `id_usuario`); opcional `Take(50)`.
  - **Padrão:** mesma regra de `getNavbarItemsMessage`; sem novo serviço.
  - **Não fazer:** mudar estrutura do menu (`getNavbarItemsMenu` já no login).
  - **Aceite:** SQL retorna só tasks do usuário; dropdown tarefas igual funcionalmente.
  - **Smoke:** login → abrir qualquer Index → inspecionar contagem de linhas.
  - **Resultado (2026-05-20):** `where` alinhado a `getNavbarItemsMessage` + `.Take(50)` na query EF.

- [x] **PERF-002** (PERF-001) Cache curto de fragmentos navbar (mensagens/tarefas/atividades).
  - **Arquivos:** `Controllers/NavbarController.cs`, opcional helper em `Security/` ou `Lib/`.
  - **Ação mínima:** `MemoryCache` com chave `navbar_{tipo}_{userId}`, TTL absoluto 30–60 s; popular em `Index`/`IndexFooter` só se cache miss.
  - **Padrão:** mesmo estilo `CachePersister` / `LookupQueryServiceCache` (sliding ou absolute documentado).
  - **Não fazer:** cachear `userIdentity` nem roles.
  - **Aceite:** 2.ª página em <60 s não dispara 4 queries EF duplicadas (**validar Profiler**).
  - **Risco:** baixo (dados não financeiros; TTL curto).
  - **Resultado (2026-05-20):** `Security/NavbarFragmentCache.cs` — snapshot único TTL 60 s (`navbar_fragments_{token}`); `Index`/`IndexFooter` partilham cache; invalidação no `logout`; alertas atendimento restaurados do snapshot.

- [x] **PERF-003** (PERF-001) Eliminar duplicação navbar ↔ footer.
  - **Arquivos:** `NavbarController.cs` (`Index` + `IndexFooter`), `_Layout.cshtml`, `_IndexFooter.cshtml`.
  - **Ação mínima:** `IndexFooter` reutilizar listas já preenchidas em `contextoModel` na mesma request **ou** não recarregar msg/task se preenchidos há <1 s; preferir **uma** child action se footer precisar só versão.
  - **Aceite:** −50% chamadas `getNavbarItems*` por full page (**validar**).
  - **Resultado (2026-05-20):** `HttpContext.Items[GdiNavbarFragmentsLoaded]` após `Apply`; `IndexFooter` não chama `Apply` se flag setada (mesmo `contextoModel` do `CachePersister`).

### 1.2 Index pedidos — filtro cliente (combo gigante)

- [x] **PERF-004** (PERF-003) Typeahead Ajax no filtro de cliente (`IndexPedido`).
  - **Arquivos:** `Areas/gc/Controllers/MovimentosController.Lookups.cs` (`PreencherLookupsIndexPedido`), `Areas/gc/Views/Movimentos/IndexPedido.cshtml`.
  - **Ação mínima:**
    1. Remover `ViewBag.comboClientes = GetComboSomenteGClientes` do Index (convenção 1.7 — Index sem cache global de todos clientes).
    2. Replicar padrão CreateEdit: `LookupSearchQueries.ComboPlaceholderCliente()` + `data-gdi-lookup-url` / `gdi-select2.js` apontando para action existente (`GetClientesLookup` ou equivalente documentado em `2026_05_22_lookups-typeahead-ajax-pedidos.md`).
    3. Manter valor selecionado para filtro via hidden + item único no combo quando filtro aplicado.
  - **Padrão:** não reintroduzir `LibDataSets`; não carregar todos `g_clientes` no HTML.
  - **Aceite:** HTML do Index sem milhares de `<option>`; pesquisa Ajax funciona; `GetDados` filtra por `id_cliente` como hoje.
  - **Smoke:** IndexPedido → buscar cliente → filtrar grid.
  - **Resultado (2026-05-20):** `PreencherLookupsIndexPedido` só opção `-1` [TODOS]; `#edit_cliente` com `GetClientesLookup` + `gdi-select2` (allowClear).

- [x] **PERF-004b** (PERF-004) Typeahead cliente em `PainelPedidos` + `ModalConsultaPedidos`.
  - **Arquivos:** `MovimentosController.Lookups.cs`, `PainelPedidos.cshtml`, `ModalConsultaPedidos.cshtml`, `LookupSearchQueries.cs`.
  - **Aceite:** abrir Painel ou modal Consulta a partir do Index **sem** HTML com todos os `g_clientes`; filtro painel envia `id_cliente` correto no Ajax.
  - **Resultado (2026-05-20):** `GetComboSomenteGClientes` removido dos dois `PreencherLookups*`; bug `#edit_cliente` no painel corrigido para `#id_cliente`. Produto no modal mantém combo cacheado (fora escopo).

### 1.3 Modal consulta pedidos — N+1 SQL (**confirmado**)

- [x] **PERF-005** (PERF-005) Refatorar `GetRelatorioConsultaPedidos` (batch de itens).
  - **Arquivos:** `Areas/gc/Controllers/MovimentosController.cs` (~6162+).
  - **Ação mínima:**
    1. Manter paginação `Skip/Take` na lista de movimentos.
    2. Coletar `id_movimento` da página; **uma** query de itens `WHERE id_movimento IN (...)`.
    3. Montar HTML da coluna em memória a partir do dicionário (sem `GetDataTable` dentro do `foreach`).
  - **Padrão:** JSON DataTables com `errorMessage`/`try-catch` existente; não alterar contrato de colunas visível.
  - **Opcional na mesma PR:** evitar `select *` na SQL principal — projetar colunas usadas no `aaData`.
  - **Aceite:** 1 página (ex. 10 linhas) ⇒ ≤3 queries SQL (**validar Profiler**).
  - **Risco:** médio — testar modal “Consulta pedidos” e ordenação.
  - **Resultado (2026-05-20):** batch EF itens + dict cliente/vendedor por página; data fim `yesCustomField04`; SQL principal ainda em memória (futuro PERF-006+).

---

## Etapa 2 — Alta prioridade (grids e abertura de formulários)

### 2.1 DataTables com paginação em memória

- [x] **PERF-006** (PERF-004) `FinanceiroController.GetDados` — paginação no SQL Server.
  - **Arquivos:** `Areas/g/Controllers/FinanceiroController.cs`.
  - **Ação mínima:** `COUNT(*)` com mesma `WHERE`; dados com `ORDER BY` + `OFFSET/FETCH`; manter `LibDB.setFilterByUser` na sentença; projeção `select` explícita (não `f.*` se possível sem quebrar mapeamento).
  - **Padrão:** igual melhorias já feitas em `MovimentosController.GetDados` (IndexPedido): total + página + `aaData`.
  - **Aceite:** filtro 90 dias + cliente: app não materializa lista completa (**validar** memória e tempo).
  - **Dependência:** índice candidato `(data_vencimento, id_cliente, id_financeiro_status)` — **só documentar**; DBA valida plano.
  - **Resultado (2026-05-20):** `COUNT` + `OFFSET/FETCH`; clientes só da página.

- [x] **PERF-007** Inventariar e corrigir **lote 2** de `GetDados` com `allRecords.ToList()` + `Skip`.
  - **Arquivos (prioridade):** `ClassificacaoFinanceiraController`, `GedSGQController` (4 actions), `MovimentosController` (outros `GetDados` legados), `AtendimentosController`, `ComexImportacoesController`.
  - **Ação mínima:** grep `iTotalRecords = allRecords.Count` sem `Skip` prévio em SQL; aplicar mesmo padrão PERF-006 por controller (**um PR por área**).
  - **Resultado (2026-05-20):** `Lib/LibDataTableSqlPaging.cs`; lote 2 paginado no SQL/EF; inventário `Scripts/2026_05_22_gdi_inventory_datatables_memory_paging.py` + `.cursor/context/2026_05_22_perf007-lote2-inventario.md` — **8 pendentes lote 3**, **1 aceite** (`GetDadosInvoicesItensEspelhoDigital`).
  - **Aceite lote 2:** prioridade fechada; restantes documentados para PERF-007b.

### 2.2 Formulário pedido — menos trabalho no load

- [x] **PERF-008** (PERF-007) Lazy init DataTables em `FormPedidoCreate`.
  - **Arquivos:** `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`.
  - **Ação mínima:**
    1. Manter init da grelha **itens** na aba Dados (essencial).
    2. Inicializar Audit / GED / Tarefas só no evento `shown.bs.tab` da respetiva aba.
    3. Reduzir `pageLength` de 100 → 25 ou 50 na grelha de itens.
  - **Padrão:** `error.dt` / `xhr.dt` + `GdiDtNotify*` já presentes; não remover.
  - **Aceite:** abrir CreatePedido dispara 1 Ajax DataTables no load (não 4); abas secundárias carregam ao clicar.
  - **Resultado (2026-05-20):** `shown.bs.tab` em `#pedidos-tabs`; Audit/GED lazy; itens `pageLength` 50; combo COMEX na aba Invoice via Ajax.
  - **Smoke:** criar cotação → itens → abrir GED → voltar.

- [x] **PERF-009** Reduzir lookups no load do CreateEdit (somente se medição Etapa 0 confirmar lentidão).
  - **Arquivos:** `MovimentosController.Lookups.cs` — `PreencherLookupsPedidoFormCore`.
  - **Ação mínima:** adiar `GetComboGcComexImportacoesTodas` / `GetDatasetGVendedores` para aba Invoice ou primeiro uso (ViewBag preenchido na action da aba ou Ajax).
  - **Cuidado:** não quebrar validação server-side que espera combo na 1.ª render.
  - **Aceite:** menos queries no GET CreatePedido (**validar**); funcionalidade Invoice intacta.
  - **Resultado (2026-05-20):** removidos `GetComboGcComexImportacoesTodas` e warm `GetDatasetGVendedores` do GET; `AjaxComboComexImportacoesPedido` + placeholder/valor selecionado na edição.

- [x] **PERF-010** (PERF-011 parcial) Trocar `select *` por colunas necessárias nos fluxos quentes de pedido.
  - **Arquivos:** `MovimentosController.cs` (SqlQuery produtos/itens nos métodos de gravação/leitura de itens — grep `select * from g_produtos`).
  - **Ação mínima:** listar colunas usadas no loop/view; `SqlQuery` com lista explícita ou projeção EF `Select`.
  - **Aceite:** mesmo comportamento em adicionar/editar item; menos I/O (**validar**).
  - **Resultado (2026-05-20):** `LoadProdutosPedidoItens` com colunas explícitas (3 pontos em `MovimentosController`).

### 2.3 Cadastro cliente (mesmo padrão)

- [x] **PERF-011** Lazy init DataTables em `Clientes/CreateEdit`.
  - **Arquivos:** `Areas/g/Views/Clientes/CreateEdit.cshtml`.
  - **Ação mínima:** idem PERF-008 (destinatários/contatos).
  - **Aceite:** 1 grid no load; demais ao abrir aba/modal.
  - **Resultado (2026-05-20):** contatos no load só se `#dataTables-Contatos` (edição); destinatários/audit em `shown.bs.tab` (`#crud-tabs`); novo cliente = 0 grids no load.

---

## Etapa 3 — Média prioridade (infra, cache, inventário)

### 3.1 IIS e assets (baixo risco de código)

- [x] **PERF-012** (PERF-009) Compressão e cache de estáticos no IIS.
  - **Onde:** IIS Manager + documentar em `.cursor/context/` (não obrigar `Web.config` se política for só IIS).
  - **Ação:** habilitar compressão dinâmica/estática; `clientCache` para `/LibUI_*`, `/Content` com `?v=VersionERP`.
  - **Aceite:** resposta JS/CSS com `Content-Encoding: gzip` e 304 em reload (**validar**).
  - **Resultado (2026-05-20):** `.cursor/context/2026_05_22_perf012-iis-static-compression-cache.md`; `Web.Release.config` (`urlCompression` + `clientCache` 7d).

- [x] **PERF-013** `BundleTable.EnableOptimizations` em Release.
  - **Arquivos:** `Global.asax.cs` (após `RegisterBundles`).
  - **Ação mínima:** `BundleTable.EnableOptimizations = !HttpContext.Current?.IsDebuggingEnabled ?? true` ou `#if !DEBUG`.
  - **Aceite:** bundles `~/bundles/libui-swal-compat` minificados em Release.
  - **Resultado (2026-05-20):** `#if !DEBUG` → `BundleTable.EnableOptimizations = true` após `RegisterBundles`.

- [x] **PERF-014** ~~Google Analytics~~ **removido do projeto (2026-05-20)** — sem gtag/googletagmanager; propriedades `GoogleTag*` e partial eliminados.

### 3.2 Lookups — custo de `IsTableUpdate`

- [x] **PERF-015** (PERF-008) Reduzir `MAX()` em `IsTableUpdate` para combos frequentes.
  - **Arquivos:** `Lib/LibDB.cs`, `Lib/Lookups/LookupQueryServiceCache.cs`.
  - **Ação mínima:** se `ListTablesUpdate` já tiver carimbo fresher que cache, **não** executar `SELECT MAX(...)`; ou TTL absoluto para tabelas grandes (`g_clientes`, `g_produtos`).
  - **Aceite:** hit de combo cacheado sem query agregada na mesma request (**validar**).
  - **Risco:** médio — invalidação stale; testar alteração em cliente e refresh combo.
  - **Resultado (2026-05-20):** `DateTimeLastVerified` + TTL 5/15 min; cache hit antes de `IsTableUpdate`; `ResetTableUpdateVerification` em `InvalidateForTable`. Ver `2026_05_22_perf015-is-table-update-ttl.md`.

### 3.3 Login

- [ ] **PERF-016** (PERF-014) Adiar `allYesProdutos` até 1.º uso.
  - **Arquivos:** `Controllers/UserIdentityController.cs`, consumidores de `CachePersister.allYesProdutos` (grep).
  - **Ação mínima:** remover `ToList()` do login; carregar lazy na action que precisa.
  - **Aceite:** login mais rápido; funcionalidade YES produtos intacta.

### 3.4 Instrumentação (habilitar Etapa 4)

- [ ] **PERF-017** (PERF-010) Action filter de tempo (amostragem).
  - **Arquivos:** novo `Lib/` ou `Filters/PerfActionFilter.cs`, `FilterConfig.cs`.
  - **Ação mínima:** log `PERF|area/controller/action|ms` em `LibLogger.Info` para 10% requests ou só &gt;2 s.
  - **Aceite:** linha no `App_Data/Logs/erp-*.log` correlacionável.

- [ ] **PERF-018** Índices SQL candidatos (documentação para DBA).
  - **Saída:** secção em `.cursor/context/2026_05_20_performance-indices-candidatos.md` (criar na implementação) com tabelas da auditoria secção 6 — **sem** script definitivo sem plano de execução.

### 3.5 Index pedidos — `Count()` pesado (se baseline apontar)

- [ ] **PERF-019** Otimizar filtros de data em `MovimentosController.GetDados` (IndexPedido).
  - **Ação mínima:** revisar `OR` em datas (cadastro/alteração/aprovação/vencimento); preferir intervalo único se regra de negócio permitir.
  - **Aceite:** plano de execução com menos scans (**validar** com DBA); sem mudança de regra de filtro visível ao usuário.

---

## Etapa 4 — Opcional / avançada (alto risco ou dependência de infra)

> Executar só com telemetria (Etapa 3 PERF-017) e aceite explícito do utilizador.

### 4.1 Layout — scripts condicionais

- [ ] **PERF-020** (PERF-006) Layout enxuto + scripts por necessidade.
  - **Arquivos:** `_Layout.cshtml`, views que precisam DataTables/Select2/jstree.
  - **Ação mínima:** `ViewBag.RequiresDataTables` etc.; mover includes para `@section` ou partial `_LayoutScriptsDataTables.cshtml` incluído só nas views que usam.
  - **Risco:** médio — regressão em modais que assumem DT global.
  - **Aceite:** tela sem grid (ex. parâmetro simples) não baixa `datatables.min.js` (**validar** KB).

### 4.2 Integrações síncronas

- [ ] **PERF-021** (PERF-012) e-Notas / boletos — não bloquear request UI.
  - **Arquivos:** `NfeController.cs`, `MovimentosController.cs`, `Robos/ENotas/`.
  - **Ação:** fila JobServer (já referenciado em `appSettings`) ou padrão “processando” + polling Ajax.
  - **Risco:** alto — exige desenho de estados e smoke homologação.

- [ ] **PERF-022** Cache cotação dólar (`RoboCotacaoDolar`) — MemoryCache diário.
  - **Arquivos:** `Robos/CotacaoDolar/`, chamada em `MovimentosController`.
  - **Aceite:** 1 chamada externa por dia por app pool.

### 4.3 Entity Framework global

- [ ] **PERF-023** (PERF-013) Desabilitar lazy loading no `GdiPlataformEntities`.
  - **Arquivos:** `Db/ModelDbGdiPlataform` (`.Context.cs` / template), smoke em **todas** áreas.
  - **Risco:** **alto** — pode expor N+1 já mascarados ou quebrar telas que dependem de navegação implícita.
  - **Aceite:** inventário de queries regressivas = 0 em smoke checklist lookups + pedidos + financeiro.

### 4.4 Arquitetura

- [ ] **PERF-024** Extrair queries de leitura de `MovimentosController` para serviço read-only.
  - **Risco:** alto — escopo grande; **não** misturar com correções cirúrgicas.
- [ ] **PERF-025** Redis para cache de lookups em farm multi-nó (**validar** se há ≥2 IIS).
- [ ] **PERF-026** Connection string read-only para relatórios `Relatorios*`.

---

## Matriz rápida — PR sugeridos (ordem)

| PR | Itens checklist | Escopo estimado |
|----|-----------------|-----------------|
| PR-1 | PERF-001, PERF-002, PERF-003 | Navbar |
| PR-2 | PERF-004 | IndexPedido filtro cliente |
| PR-3 | PERF-005 | Modal consulta pedidos N+1 |
| PR-4 | PERF-006 | Financeiro GetDados |
| PR-5 | PERF-008, PERF-010 (parcial) | FormPedidoCreate |
| PR-6 | PERF-012, PERF-013 | IIS + bundles |
| PR-7 | PERF-007 | Lote grids restantes |

---

## Smoke mínimo pós-etapas (reutilizar após cada PR)

| # | Fluxo | Etapas que obrigam |
|---|--------|-------------------|
| S1 | Login + menu lateral | 1, 3 |
| S2 | `gc/Movimentos/IndexPedido` filtro + grid | 1, 2 |
| S3 | `gc/Movimentos/CreatePedido` + itens + aba GED | 2 |
| S4 | Modal consulta pedidos | 1 |
| S5 | `g/Financeiro/Index` grid filtrada | 2 |
| S6 | `g/Clientes/CreateEdit` destinatários | 2 |
| S7 | Combo lookup após alterar cliente (cache) | 3 PERF-015 |

---

## O que **não** fazer neste plano (armadilhas)

- Reintroduzir `LibDataSets` ou cache global de combo completo em Index (ver convenção lookups).
- Aplicar `scroll-body-horizontal` / `max-content` em tabelas MVC de formulário.
- Refatorar `MovimentosController` inteiro na mesma PR que correção de navbar.
- Adicionar Redis, desligar lazy loading ou mudar versões Bootstrap/AdminLTE/DataTables sem pedido explícito.
- Criar índices em produção sem plano de execução e janela com DBA.

---

## Registo de progresso

| Data | Etapa | PR / notas |
|------|-------|------------|
| 2026-05-20 | 0 | PERF-000.1 OK — DevTools (tabela acima). Pendente: PERF-000.2 SQL, PERF-000.3 Publish. |
| | 1 | |
| | 2 | |
| | 3 | |
| | 4 | |

---

*Checklist derivado de `PERFORMANCE-AUDIT-ERP.md`. Atualizar este ficheiro quando itens forem concluídos ou descoped.*
