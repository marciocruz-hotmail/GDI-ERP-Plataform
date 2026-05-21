# Auditoria de performance — GDI-ERP-Plataform

**Data da auditoria:** 2026-05-20  
**Tipo:** investigação estática (read-only) — sem alterações de código, schema ou IIS.  
**Stack:** ASP.NET MVC .NET Framework 4.7.2, SQL Server, EF6 + ADO.NET, IIS, Bootstrap 5 / AdminLTE 4 / DataTables 2.3.2.

**Documentos relacionados:** `2026_05_20_checklist-performance-erp.md` (etapas executáveis), `BACKLOG-DEV.md` (IDs G-PERF / G-DT / G-PROD), baseline DevTools no checklist §0.

**Legenda de confiança**

| Marcador | Significado |
|----------|-------------|
| **Confirmado** | Evidência direta no repositório |
| **Provável** | Padrão recorrente; impacto depende de volume e uso |
| **Validar runtime** | Requer medição (Profiler, DevTools, plano de execução SQL) |

---

## 1. Resumo executivo

### Estado geral

O ERP passou por um **ciclo relevante de otimização em 2026-05-20** (navbar, pedidos, estoque, publish, lookups TTL). Existem mecanismos maduros: `ILookupQueryService` + `MemoryCache`, `NavbarFragmentCache` (60 s), `LibDataTableSqlPaging`, typeahead produtos/clientes, `BundleTable.EnableOptimizations` em Release, compressão/cache estático em `Web.Release.config` (PERF-012/013), TTL em `IsTableUpdate` (PERF-015).

O gargalo dominante para a **percepção do usuário** continua sendo:

1. **Custo fixo por página autenticada** — `_Layout.cshtml` carrega ~15 CSS + ~12 JS (DataTables, Select2, jstree, Tempus, AdminLTE, `start.js`) em **todas** as telas, fora de bundles agressivos.
2. **Child actions Navbar** — ainda há pipeline MVC extra (`RenderAction` navbar + footer), mitigado por cache 60 s e deduplicação na mesma request, mas **primeira** navegação após TTL dispara 4 queries EF.
3. **Grids legados com paginação em memória** — inventário automatizado lista **6** `GetDados*` pendentes (`Assistentes`, `CentrosCustos`, `Ged`, `Nfe`, `EstoqueControle`, `Parametros`); **`Nfe.GetDados`** materializa **toda** `g_nfe` antes de `Skip/Take`.
4. **Formulários monolíticos** — `MovimentosController` (~9k linhas), `FormPedidoCreate.cshtml`, `ComexImportacoes/CreateEdit` com listas completas e integrações síncronas (e-Notas, S3, CPF/CNPJ).
5. **Login** — `a_yesprodutos.ToList()` + montagem de menu EF (`getNavbarItemsMenu`) no POST de autenticação.

### Principais causas prováveis de lentidão (atualizado)

| # | Causa | Status mitigação |
|---|--------|------------------|
| 1 | Assets globais no layout | **Pendente** (G-PERF-20) |
| 2 | Navbar queries por página | **Parcial** — cache 60 s + filtro tasks (PERF-001/002) |
| 3 | Paginação falsa em grids | **Parcial** — lote 2 PERF-007 feito; **6** actions pendentes |
| 4 | Combos/datasets volumosos (produtos) | **Parcial** — typeahead IndexPedido/Estoque; G-PROD-01…06 abertos |
| 5 | EF `LazyLoadingEnabled=true` | **Pendente** (G-PERF-23, alto risco) |
| 6 | APIs externas síncronas na request | **Pendente** (G-PERF-21) |

### Áreas com maior potencial de ganho (impacto × frequência)

| Prioridade | Área |
|------------|------|
| **P0** | Layout condicional de JS/CSS; concluir PERF-007b (6 grids) |
| **P0** | `Nfe.GetDados` + `GetGedPedido` (usuários completos) |
| **P1** | G-PROD (inventário, Ajax produto pedido, combos importados) |
| **P1** | Login (`allYesProdutos`); `Estoque/GetDadosEstoque` (SUM catálogo a cada Ajax) |
| **P2** | Instrumentação (G-PERF-17); lazy loading EF; Redis só se farm IIS ≥2 nós |
| **P2** | `LibLogger` síncrono; fila e-Notas |

### Recomendações prioritárias (ordem sugerida)

1. Executar **G-PERF-M01** (contar SQL no full page load) e **G-PERF-M02** (repetir baseline DevTools).
2. Fechar **G-DT-01** (PERF-007b): 6 controllers com `LibDataTableSqlPaging` ou EF `Count`+`Skip/Take` no SQL.
3. Corrigir **`NfeController.GetDados`** (carga total `g_nfe`) — impacto direto no módulo fiscal.
4. **`GetGedPedido`**: resolver usuário por `id_usuario` (dict), não `g_usuarios` inteiro.
5. Avançar **G-PROD-01** (inventário typeahead + grid sem dataset 13k).
6. **G-PERF-20**: scripts condicionais no layout (piloto em telas sem DataTables).
7. Medir antes/depois com telemetria mínima (**G-PERF-17**).

---

## 2. Top 10 oportunidades de melhoria

| Prioridade | Arquivo / local | Problema identificado | Impacto na UX | Recomendação | Severidade | Esforço | Risco |
|------------|-----------------|----------------------|---------------|--------------|------------|---------|-------|
| 1 | `Views/Shared/_Layout.cshtml` | ~15 CSS + ~12 JS no `<head>` sem `defer`; DataTables/Select2/jstree em toda página | Primeira pintura e TTI altos (~45 req, 1–2 MB transferido — baseline checklist) | `ViewBag`/partial por área; `defer` em scripts não críticos; bundles LibUI | **Alta** | Médio | Médio |
| 2 | `Areas/g/Controllers/NfeController.cs` `GetDados` L141–158 | `ToList()` de **toda** `g_nfe` (ou SQL sem página); `Skip/Take` só em memória | Grid NFe lenta com volume de notas | `SqlCount` + `SqlPage` / EF paginado; projeção de colunas | **Alta** | Médio | Médio |
| 3 | `Scripts/2026_05_20_gdi_inventory_datatables_memory_paging.py` | 6× `GetDados*` **PENDENTE** | Grids de cadastro/parâmetros degradam com dados | Aplicar `LibDataTableSqlPaging` (padrão Atendimentos L78) | **Alta** | Médio | Baixo |
| 4 | `MovimentosController.cs` `GetGedPedido` L638–656 | `g_usuarios` **todos** + tipos GED todos; paginação só em arquivos | Aba GED do pedido lenta | Dict usuários só IDs da página; paginação SQL em `ged_arquivos` | **Alta** | Baixo | Baixo |
| 5 | `Controllers/UserIdentityController.cs` L299–384 | `a_yesprodutos.ToList()` + menu EF no login | Login e redirect mais lentos | Adiar `allYesProdutos` (G-PERF-16) | **Média** | Baixo | Baixo |
| 6 | `Areas/gc/Controllers/EstoqueController.cs` `GetDadosEstoque` L61–71 | Dois `SUM` em **todo** `g_produtos` a **cada** draw Ajax | Posição estoque: spinner longo no filtro | Cache TTL 1–5 min dos totais BH/SP ou materialized snapshot | **Média** | Baixo | Baixo |
| 7 | `Areas/gc/Controllers/ComexImportacoesController.cs` CreateEdit ~L747+ | Listas completas produtos/NCM/COMEX no load | Abertura importação pesada | Typeahead + abas lazy (padrão FormPedido PERF-008) | **Média** | Médio | Médio |
| 8 | `Lib/LibLogger.cs` + `Robos/*` | Log e HTTP **síncronos** na thread ASP.NET | Picos de erro/travamento percebido | Fila log; e-Notas/S3 em background (G-PERF-21) | **Média** | Médio–Alto | Médio |
| 9 | `Db/ModelDbGdiPlataform.edmx` | `LazyLoadingEnabled="true"` | N+1 silencioso em fluxos EF legados | Desligar lazy load + smoke (G-PERF-23) | **Média** | Alto | **Alto** |
| 10 | `MovimentosController.cs` (monólito) | ~9k linhas; dezenas de `ToList()`; manutenção difícil | Regressões e otimizações pontuais caras | Serviço read-only extrato (G-PERF-24) — fase 3 | **Média** | Alto | Alto |

---

## 3. Achados detalhados por categoria

### 3.1 Banco de dados

#### BD-01 — Paginação falsa em DataTables (**Confirmado**, inventário 2026-05-20)

| Campo | Detalhe |
|-------|---------|
| **Pendentes (script)** | `AssistentesController.GetDados` L62; `CentrosCustosController.getDados` L80; `GedController.GetDados` L111; `NfeController.GetDados` L151; `EstoqueControleController.GetDadosMedicoes`; `ParametrosController.GetDadosSistemas` |
| **Evidência** | `python Scripts/2026_05_20_gdi_inventory_datatables_memory_paging.py` → 6 PENDENTE, exit 1 |
| **Por que lentidão** | SQL + app materializam conjunto inteiro; GC e tempo de resposta Ajax crescem com tabela |
| **Sugestão** | `LibDataTableSqlPaging.SqlCount` + `SqlPage` (referência: `AtendimentosController.getDadosAtendimentos` L78) |
| **Cuidados** | Manter JSON `errorMessage`/`yesFilterOnOff`; filtros `LibDB.getFilterByUser` na mesma query |
| **Teste** | `SET STATISTICS IO ON`; comparar reads antes/depois com filtro típico |

#### BD-02 — `Nfe.GetDados` carga total (**Confirmado**)

| Campo | Detalhe |
|-------|---------|
| **Arquivo** | `Areas/g/Controllers/NfeController.cs` L141–158 |
| **Evidência** | `allRecords = db.g_nfe...ToList()` ou `SqlQuery(sentenca).ToList()`; depois `Skip/Take` |
| **Sugestão** | Paginação SQL + `totalRecords` via `Count` |
| **Teste** | Modal/index NFe com &gt;1000 registros — tempo Ajax |

#### BD-03 — `GetRelatorioConsultaPedidos` (**Corrigido** 2026-05-20 — manter em testes)

| Campo | Detalhe |
|-------|---------|
| **Arquivo** | `MovimentosController.cs` L6172–6215 |
| **Evidência** | `SqlPage` + `itensPorMovimento` em batch (`movimentoIds.Contains`) — **sem** N+1 por linha |
| **Cuidado** | `select *` em movimentos+join; HTML de itens pode inchhar payload JSON |

#### BD-04 — `FinanceiroController.GetDados` (**Parcialmente corrigido**)

| Campo | Detalhe |
|-------|---------|
| **Arquivo** | `Areas/g/Controllers/FinanceiroController.cs` L240–262 |
| **Evidência** | Com filtro preenchido: `OFFSET/FETCH` + `SqlQuery<int>` count — **bom** |
| **Pendente** | Sem pesquisa (`yesFilterField != "*"`) retorna vazio — UX ok; consolidados/outros métodos podem usar `DataTable`+`Skip` em memória (**validar**) |

#### BD-05 — `GetGedPedido` (**Confirmado**)

| Campo | Detalhe |
|-------|---------|
| **Arquivo** | `MovimentosController.cs` L638–656 |
| **Evidência** | `ListaUsuarios = db.g_usuarios.Where(u => u.id_usuario > 0).ToList()` |
| **Sugestão** | Projeção `Where(id_usuario in (...))` dos IDs da página de arquivos |

#### BD-06 — `EstoqueController.GetDadosEstoque` agregados globais (**Confirmado**)

| Campo | Detalhe |
|-------|---------|
| **Arquivo** | L61–71 — dois `Sum()` em `g_produtos` por request Ajax |
| **Sugestão** | Cache MemoryCache chave `estoque_totais_{versão}` ou tabela snapshot |

#### BD-07 — EF lazy loading (**Confirmado** no modelo)

| Campo | Detalhe |
|-------|---------|
| **Arquivo** | `Db/ModelDbGdiPlataform.edmx` — `LazyLoadingEnabled="true"` |
| **Risco alteração** | **Alto** |

#### BD-08 — `IsTableUpdate` (**Parcialmente mitigado** PERF-015)

| Campo | Detalhe |
|-------|---------|
| **Arquivo** | `Lib/LibDB.cs` — TTL 5/15 min em verificação; ainda pode executar `MAX()` após TTL |
| **Sugestão** | Invalidação explícita em gravações (G-ARC-02) |

#### BD-09 — `SELECT *` / entidades completas (**Confirmado**)

| Campo | Detalhe |
|-------|---------|
| **Exemplos** | `MovimentosController` L3132 `select p.*`; `ComexProdutosController`; `GetRelatorioConsultaPedidos` L6151 |
| **Sugestão** | DTOs com colunas da grelha apenas |

---

### 3.2 Cache

#### CA-01 — MemoryCache sessão + lookups (**Confirmado** — ponto forte)

| Arquivo | `Security/CachePersister.cs`, `Lib/Lookups/LookupQueryServiceCache.cs` |
| Chaves por `TokenId`; sliding 15 min; invalidação no logout |
| **Limitação** | Não distribuído — farm IIS aquece por nó |

#### CA-02 — `NavbarFragmentCache` 60 s (**Confirmado** — implementado)

| Arquivo | `Security/NavbarFragmentCache.cs` |
| TTL absoluto 60 s; `IsLoadedThisRequest` evita 2ª ida DB navbar→footer na mesma request |

#### CA-03 — `CloneCombo` em hit de cache (**Confirmado**)

| Arquivo | `LookupQueryServiceCache.cs` |
| Aloca nova lista por leitura — impacto em combos grandes (**provável**) |

#### CA-04 — `BuildComboProdutosServicos` cacheia catálogo inteiro (**Confirmado**)

| Uso | Lookups CreateEdit; risco RAM — mitigar com G-PROD |

#### CA-05 — Sem Redis / OutputCache HTML (**Confirmado**)

| Exceção | `OutputCache(NoStore)` em `Areas/crm/PedidosController` |

---

### 3.3 Backend

#### BE-01 — Child actions Navbar (**Confirmado**, mitigado)

| `_Layout.cshtml` L122, L135 — `RenderAction` síncrono |
| Cache reduz queries repetidas; **primeira** carga após expiração ainda paga custo |

#### BE-02 — `MovimentosController` monolítico (**Confirmado**)

#### BE-03 — `LibDB.CloneTObject` via JSON (**Confirmado**)

#### BE-04 — Integrações síncronas (**Confirmado**)

| Robo | Padrão |
|------|--------|
| e-Notas | `HttpWebRequest.GetResponse()` |
| AWS S3 | `UploadAsync` + `.Wait()` |
| CPF/CNPJ | `Thread.Sleep` polling até 30s |
| Itaú boleto | `GetResponse()` síncrono |

#### BE-05 — Login pesado (**Confirmado**)

| `UserIdentityController.cs` L299–301, L370–384 |

#### BE-06 — `Contexto.getNavbarItemsMenu` (**Confirmado**)

| Query controllers×perfil `.ToList()` no login — cacheado em `contextoModel` 15 min |

---

### 3.4 Front-end

#### FE-01 — Assets globais (**Confirmado**)

| `_Layout.cshtml` L9–48 — sem Google Analytics (removido 2026-05-20) |

#### FE-02 — Bundles (**Parcial**)

| `BundleConfig` mínimo; `Global.asax.cs` L23–26 `EnableOptimizations` em Release |
| Maioria dos assets **fora** de bundles |

#### FE-03 — DataTables `pageLength` elevado (**Parcialmente corrigido**)

| PERF-008: itens pedido `pageLength` 50; validar outras views |

#### FE-04 — Lazy init abas (**Parcialmente corrigido**)

| PERF-008/011: FormPedido, Clientes CreateEdit — GED/Audit em `shown.bs.tab` |

#### FE-05 — `bServerSide: true` predominante (**Confirmado**)

| Grep views: padrão server-side; problema está no **servidor**, não no cliente |

---

### 3.5 IIS / Web.config

#### IIS-01 — `debug="true"` em dev (**Confirmado**)

| `Web.config`; Release remove via transform |

#### IIS-02 — Compressão/cache estáticos (**Implementado em Release**)

| `Web.Release.config` L23–41 — **validar** IIS features em homologação (G-PUB-03/04) |

#### IIS-03 — Request limits altos (**Confirmado**)

| Uploads grandes podem bloquear worker |

#### IIS-04 — Sessão 15 min (**Confirmado**, alinhado `sessionInactivity.js`)

---

### 3.6 Sessão / memória / buffer

#### SM-01 — `UserIdentity` + `ContextoModel` em MemoryCache (**Confirmado**)

#### SM-02 — `DataTable`/`DataSet` legado (**Confirmado**)

#### SM-03 — `GetRelatorioConsultaPedidos` itens na página (**Provável**)

| Batch de itens pode retornar muitas linhas por movimento — validar tamanho JSON |

---

### 3.7 Integrações externas

Ver tabela secção 2 item 8 e **G-PERF-21**, **G-PERF-22** (cotação dólar).

---

### 3.8 Logs / auditoria

#### LA-01 — `LibLogger` síncrono (**Confirmado**)

| `Lib/LibLogger.cs` L39–41 — `lock` + `File.AppendAllText` |

#### LA-02 — `Application_Error` → log disco (**Confirmado**)

| `Global.asax.cs` L30–37 |

---

### 3.9 Arquitetura

| ID | Achado | Sugestão |
|----|--------|----------|
| AR-01 | Controllers com query + comando misturados | CQRS leve / serviços read-only |
| AR-02 | Convenção lookups Index vs CreateEdit | G-LKP concluído; G-PROD pendente |
| AR-03 | Monólito OLTP + relatórios no mesmo DB | CS read-only relatórios (G-PERF-26) |
| AR-04 | Sem telemetria por action | G-PERF-17 |

---

## 4. Telas ou fluxos críticos

| Tela / fluxo | Controller / action / view | Motivo | Recomendações |
|--------------|----------------------------|--------|---------------|
| **Todas autenticadas** | `_Layout` + `Navbar/*` | ~45 requests, 2,4–2,8 MB resources (baseline) | G-PERF-20; manter NavbarFragmentCache |
| **Login** | `UserIdentity/Index` POST | `allYesProdutos` + menu EF | G-PERF-16 |
| **Pedidos — lista** | `gc/Movimentos/IndexPedido`, `GetDados` | Alta frequência; `Count()` complexo | G-DT-04 índices; medição SQL |
| **Pedido — formulário** | `FormPedidoCreate`, `PreencherLookupsPedidoFormCore` | Lookups + múltiplos DT | G-PROD-02; smoke PERF-008 |
| **Modal consulta pedidos** | `GetRelatorioConsultaPedidos` | Batch OK; `select *` | Reduzir colunas SQL |
| **NFe** | `g/Nfe/GetDados`, Robo e-Notas | Grid memória + API sync | G-DT-01 + G-PERF-21 |
| **Financeiro títulos** | `g/Financeiro/Index`, `GetDados` | OFFSET quando filtro OK | `deferLoading` G-DT-05 |
| **Posição estoque** | `gc/Estoque/Index`, `GetDadosEstoque` | SUM catálogo cada Ajax | G-PERF-28 |
| **COMEX importação** | `ComexImportacoes/CreateEdit` | Listas completas no load | G-PERF-30 |
| **GED pedido** | `GetGedPedido` | Todos usuários | G-PERF-27 |
| **Inventário** | `EstoqueInventario/FormInventarioItens` | Combo 8k produtos | G-PROD-01 |
| **Portal cliente** | `crm/Pedidos` | Mesmo layout stack | Herda melhorias de layout |

---

## 5. Oportunidades de cache

| Dado candidato | Local atual | Frequência | Volatilidade | Tipo recomendado | Chave sugerida | TTL | Riscos | Invalidação |
|----------------|-------------|------------|--------------|------------------|----------------|-----|--------|-------------|
| Fragmentos navbar | `NavbarFragmentCache` | Toda página | Média | MemoryCache (**já**) | `navbar_fragments_{token}` | 60 s | Atraso notificação | Logout; TTL |
| Menu lateral | Login → `contextoModel` | 1×/sessão | Baixa | MemoryCache (**já**) | `contextoModel_{token}` | 15 min sliding | Perfil alterado | Relogin / invalidar perfil |
| Combos lookups | `LookupQueryServiceCache` | Alta CRUD | Média | MemoryCache (**já**) | `LookupCacheKeys` + token | 15 min + IsTableUpdate | RAM; MAX() | `InvalidateForTable` (G-ARC-02) |
| Totais estoque BH/SP | `GetDadosEstoque` | Cada Ajax | Média | MemoryCache global | `estoque_valor_totais_{db}` | 1–5 min | Valor desatualizado | TTL / trigger estoque |
| Cotação dólar | `RoboCotacaoDolar` | Baixa | Diária | MemoryCache | `cotacao_usd_{data}` | 4–12 h | Câmbio velho | Data |
| Clientes/produtos busca | `LookupSearchQueries` | Digitação | Média | Por termo (não HTML) | `lk:cliente:{term}` | Curto | Segurança tenant | Versão tabela |
| Assets estáticos | IIS + `?v=VersionERP` | Sempre | Baixa com versão | HTTP cache (**Release**) | URL | 7 dias | Deploy sem bump versão | `ControlVersion` |
| `allYesProdutos` | Login | 1× | Baixa | MemoryCache (**já**) ou lazy | `allYesProdutos_{token}` | 15 min | — | Adiar carga (G-PERF-16) |
| Relatórios pesados | `RelatoriosComerciaisController` | Baixa | Snapshot | Cache resultado | `rpt:{hash}` | 5–30 min | Dado antigo | TTL |

**Não cachear:** saldos financeiros em tempo real sem TTL explícito; tokens; dados de outro tenant/usuário sem chave composta.

**Redis:** candidato se **≥2 nós IIS** (G-PERF-25); hoje só `MemoryCache.Default`.

---

## 6. Oportunidades no banco de dados

| Consulta / local | Problema | Impacto | Recomendação | Índice? | Paginação? | Refator? | Risco |
|------------------|----------|---------|--------------|---------|------------|----------|-------|
| `NfeController.GetDados` | Lista inteira em memória | Alto | SqlPage + Count | Candidato `(id_nfe desc, filtros status)` **validar plano** | Sim | Sim | Médio |
| 6× GetDados inventário | `SqlQuery().ToList()` antes Skip | Médio–Alto | PERF-007b | Por tabela **validar plano** | Sim | Sim | Baixo |
| `GetGedPedido` | Todos `g_usuarios` | Médio | IN (ids página) | PK existente | Parcial | Sim | Baixo |
| `GetDadosEstoque` SUM | Scan agregado catálogo | Médio cada filtro | Cache totais | Candidato saldos **validar** | Já tem página | Parcial | Baixo |
| `MovimentosController.GetDados` | `Count()` query complexa | Médio IndexPedido | Índices compostos | Candidato **validar plano** | Sim EF | Parcial | Baixo |
| `GetRelatorioConsultaPedidos` | `select *` join | Médio payload | Projeção colunas | Candidato datas/cliente | Sim | Parcial | Baixo |
| `Contexto.getNavbarItemsMenu` | Join grande no login | Médio 1× | Manter cache | FKs **validar** | N/A | Opcional | Baixo |
| `ComexImportacoes` processamento | Loops + SqlQuery listas | Alto batch | Batch queries | **validar** | N/A | Sim | Alto |
| `BuildComboProdutosServicos` | `ToList()` todos ativos | RAM | Typeahead only | `(ativo, nome)` candidato | N/A | Sim | Baixo |

---

## 7. Oportunidades no front-end

| View / script | Problema | Impacto | Recomendação | Sob demanda | Server-side | Reduzir JS/CSS |
|---------------|----------|---------|--------------|-------------|-------------|----------------|
| `_Layout.cshtml` | Pacote JS/CSS único | Toda navegação | Partials condicionais | Sim | — | **Sim** |
| Telas sem DT | Carregam datatables.min.js | Parse desnecessário | G-PERF-20 | Sim | — | Sim |
| `FormPedidoCreate.cshtml` | Múltiplos DT | Abertura pedido | Já lazy abas — validar restantes | Sim | Sim | — |
| `IndexPedido.cshtml` | Filtros | Melhor pós typeahead cliente | Manter Select2 Ajax | Sim | Sim | — |
| `EstoqueInventario/FormInventarioItens` | Combo 8k | Filtro inventário | G-PROD-01 typeahead | Sim | Sim | — |
| `start.js` | Global grande | Parse | Version bump; split futuro | Parcial | — | Difícil |
| Modais `Movimentos/*` | Vários inits DT | Modais pesados | Init no `shown.bs.modal` | Sim | Validar | — |

---

## 8. Recomendações de instrumentação

### Métricas mínimas por request

| Métrica | Captura sugerida |
|---------|------------------|
| Tempo action MVC | `PerfActionFilter` → `LibLogger` amostra 10% (G-PERF-17) |
| Contagem SQL | EF `Database.Log` dev; interceptor prod staging |
| Tempo navbar | Log separado `Navbar/Index` |
| Tamanho JSON `GetDados*` | Alerta &gt; 500 KB |
| Chamadas externas | Wrap `Robo*` com `Stopwatch` + timeout |

### Ferramentas

| Ferramenta | Uso |
|------------|-----|
| Chrome DevTools Network | Baseline §0 checklist-performance |
| SQL Server Extended Events | Reads/duration por tela |
| `2026_05_20_gdi_inventory_datatables_memory_paging.py` | Regressão paginação |
| MiniProfiler / PerfView | Dev/staging |

### Telemetria mínima (formato sugerido)

`PERF|{area}/{controller}/{action}|{ms}|sql={n}|jsonBytes={b}`

**Alertas:** P95 &gt; 3 s em `gc/Movimentos/GetDados`, `UserIdentity` login, `g/Nfe/GetDados`.

---

## 9. Plano de ação incremental

### Fase 1 — Ganhos rápidos e baixo risco (1–2 sprints)

| Item | ID backlog | Entrega esperada |
|------|------------|------------------|
| Fechar 6 grids paginação SQL | G-DT-01 | Script inventário 0 PENDENTE |
| Nfe.GetDados paginação real | G-DT-07 | Grid NFe escalável |
| GetGedPedido sem todos usuários | G-PERF-27 | Aba GED pedido mais rápida |
| Cache totais Estoque Ajax | G-PERF-28 | Menos SUM por draw |
| Adiar `allYesProdutos` login | G-PERF-16 | Login mais leve |
| Validar IIS gzip/304 homologação | G-PUB-03/04, G-SMK-06 | Assets comprimidos |

### Fase 2 — Melhorias estruturais (3–6 sprints)

| Item | ID | Entrega |
|------|-----|---------|
| G-PROD-01…04 | Inventário + Ajax pedido + combos |
| Layout scripts condicionais | G-PERF-20 | −MB JS em telas simples |
| Telemetria actions | G-PERF-17 | Baseline mensurável |
| `deferLoading` cadastros | G-DT-05 | 1º paint sem GetDados |
| ComexImportacoes defer lookups | G-PERF-30 | CreateEdit importação |
| Índices candidatos DBA | G-PERF-18 | Documento sem DDL automático |

### Fase 3 — Avançado

| Item | ID | Entrega |
|------|-----|---------|
| Fila e-Notas / boletos | G-PERF-21 | UI não bloqueia |
| EF lazy loading off | G-PERF-23 | Menos N+1 |
| Redis multi-nó | G-PERF-25 | Se infra exigir |
| Serviço read-only Movimentos | G-PERF-24 | Manutenção |
| CS read-only relatórios | G-PERF-26 | Menos contenção OLTP |

---

## 10. Backlog técnico priorizado (síntese — detalhe em BACKLOG-DEV.md)

| Código | Título | Esforço | Risco | Dependências |
|--------|--------|---------|-------|--------------|
| G-DT-01 | PERF-007b — 6 grids pendentes | Médio | Baixo | `LibDataTableSqlPaging` |
| G-DT-07 | Nfe.GetDados paginação SQL | Médio | Médio | G-DT-01 padrão |
| G-PERF-27 | GetGedPedido — usuários sob demanda | Baixo | Baixo | — |
| G-PERF-28 | Cache SUM totais Estoque | Baixo | Baixo | — |
| G-PERF-16 | Login sem allYesProdutos upfront | Baixo | Baixo | grep consumidores |
| G-PERF-17 | Action filter tempo | Médio | Baixo | — |
| G-PERF-20 | Layout scripts condicionais | Médio | Médio | Inventário views |
| G-PERF-29 | LibLogger fila assíncrona | Médio | Baixo | — |
| G-PERF-30 | ComexImportacoes CreateEdit lazy | Médio | Médio | G-PROD padrões |
| G-PROD-01…06 | Cache produtos / typeahead | Médio | Médio | baseline PROD-000 |
| G-PERF-M01/02 | Medição SQL + DevTools | Baixo | — | Antes de Fase 2 |

---

## Apêndice A — Melhorias já implementadas (2026-05-20, não reabrir)

| ID / tema | Entrega |
|-----------|---------|
| PERF-001/002 | Filtro tasks navbar + `NavbarFragmentCache` 60 s |
| PERF-003 / PROD-002a | Typeahead cliente/produto IndexPedido / Estoque |
| PERF-006 | Financeiro `GetDados` OFFSET/FETCH com filtro |
| PERF-007 lote 2 | SqlPaging em vários controllers gc/g |
| PERF-008–011 | Lazy DataTables FormPedido / Clientes |
| PERF-012/013 | IIS cache/compressão Release; bundle optimizations |
| PERF-015 | IsTableUpdate TTL |
| GetRelatorioConsultaPedidos | Batch itens (sem N+1 por linha) |
| G-DT-06 | Atendimentos GetDados* legados |
| GA | Removido do layout |

## Apêndice B — Padrões positivos (referência para novos `GetDados`)

- `MovimentosController.GetDados` (IndexPedido): EF `Skip/Take`, dicts só IDs da página.
- `AtendimentosController.getDadosAtendimentos`: `LibDataTableSqlPaging.SqlPage`.
- `ClientesController.GetDados` / `ProdutosController.GetDados`: filtro obrigatório + cap page size.
- `LookupSearchQueries` + Select2 typeahead.
- `FinanceiroLancamentosController.GetDadosLancamentos`: projeção + paginação EF.

## Apêndice C — Fora de escopo desta auditoria

- Planos de execução e índices físicos no servidor de produção.
- Configuração app pool / ARR / SSL offload.
- Testes de carga multi-usuário.

---

*Relatório gerado por investigação estática do repositório. Nenhum ficheiro de código, schema ou IIS foi alterado.*
