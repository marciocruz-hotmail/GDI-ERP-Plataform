# BACKLOG-DEV.md — Plano de implementação (checklist)

Pendências **ativas** do **GDI-ERP-Plataform**, consolidadas a partir de `.cursor/context/`, `CHANGELOG-DEV.md` e inventários em `Scripts/`. Itens já concluídos foram removidos desta lista (registo em `CHANGELOG-DEV.md` e `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`).

**Como usar:** marcar `[x]` só quando o critério de aceite estiver cumprido. Respeitar **Ordem** dentro do grupo. Um PR pequeno por item ou lote homogéneo. Sem `git push` nem publish remoto (regras do projeto).

**Índice de memória (temas):** `.cursor/context/2026_06_05_indice-memoria-ia.md`

**Referências transversais**

| Tema | Documento |
|------|-----------|
| Lookups / LibDataSets (histórico) | `.cursor/context/2026_05_22_checklist-pendencias-lookups-e-erp.md` |
| Performance (auditoria + etapas) | `.cursor/context/2026_05_22_checklist-performance-erp.md`, `.cursor/context/2026_05_22_performance-audit-erp.md` |
| Layout scripts G-PERF-20 | `.cursor/context/2026_05_22_layout-scripts-contrato-flags.md`, `2026_05_22_layout-scripts-matriz-modais.md`, `2026_05_21_layout-scripts-inventario.json` |
| Cache produtos | `.cursor/context/2026_05_22_checklist-cache-produtos.md` |
| DataTables vs MVC | `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` |
| **Arquitetura centralizada ERP GDI** | `.cursor/context/2026_06_05_arquitetura-centralizada-erp-gdi.md` |
| Convenção lookups Index/CreateEdit | `.cursor/context/2026_05_20_lookups-convencao-index-vs-createedit.md` |
| PERF-007 lote 3 | `.cursor/context/2026_05_22_perf007-lote2-inventario.md` |
| IIS / gzip / 304 | `.cursor/context/2026_05_22_perf012-iis-static-compression-cache.md` |
| IsTableUpdate TTL | `.cursor/context/2026_05_22_perf015-is-table-update-ttl.md` |
| NFe e-Notas | `.cursor/context/2026_05_26_nfe-enotas-arquitetura.md` |
| Migração 4.8.1 | `.cursor/context/2026_05_20_migracao-472-481.md` |
| Monitorização lookups pós-publish | `.cursor/context/2026_05_20_lookups-monitorizacao-pos-publish.md` |
| Health + PUB-1/PUB-2 | `.cursor/context/2026_05_22_health-endpoint-publish.md` |
| TempData legado (filtro/login) | `.cursor/context/2026_05_22_tempdata-legado-filtro-login.md` |

**Scripts úteis**

```powershell
cd c:\Marcio\Projetos\GDI-ERP-Plataform
python Scripts/2026_05_22_gdi_inventory_datatables_memory_paging.py
python Scripts/2026_05_22_gdi_audit_dt_scroll_host.py
python Scripts/2026_05_22_gdi_inventory_page_scripts.py
python Scripts/2026_05_22_gdi_inventory_page_scripts.py --markdown
python Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py --markdown
python Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py --fail
python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py
python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail
python Scripts/2026_06_05_gdi_inventory_gc_modal_get_guards.py
python Scripts/2026_06_05_gdi_inventory_ajax_hybrid_handlers.py
python Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py --areas all
python Scripts/2026_06_05_gdi_inventory_prefixed_file_dates.py --since 2026-06-01
python Scripts/2026_06_05_gdi_rename_legacy_2026_05_20_prefixes.py --dry-run
python Scripts/2026_06_05_gdi_smoke_architecture_inventories.py
```

---

## Legenda de grupos e IDs

| Grupo | Prefixo | Criticidade típica | Fase |
|-------|---------|-------------------|------|
| Publicação e ambiente | **G-PUB** | Alta | Deploy |
| Smoke / homologação | **G-SMK** | Alta | Validação manual |
| Performance — medição | **G-PERF-M** | Média | Baseline |
| Performance — código | **G-PERF** | Alta → Baixa | 1–4 (ver checklist performance) |
| Cache global produtos | **G-PROD** | Alta | PROD-005…007 |
| Lookups — controllers híbridos | **G-LKP** | Média | PRs por controller |
| DataTables — SQL em memória | **G-DT** | Alta | PERF-007b |
| NFe / e-Notas | **G-NFE** | Alta | Homologação |
| Filtro SQL legado | **G-FLT** | Baixa | Limpeza |
| Login / TempData | **G-LOGIN** | Baixa | UX tela de login |
| Flash messages PRG | **G-UX** | Baixa | Padronização |
| Higiene / inventário | **G-ENC** | Baixa | Contínuo |
| Plataforma (.NET) | **G-NET** | Baixa | Trilha separada |
| Arquitetura avançada | **G-ARC** | Baixa | Opcional |
| Operação pós-publish | **G-OPS** | Média | Após deploy |

**Criticidade:** Crítica | Alta | Média | Baixa  
**Prioridade de execução sugerida (global):** G-PUB → G-SMK (paralelo homologação) → G-PROD → G-DT → G-PERF (restantes) → G-LKP → G-FLT → G-NFE → G-OPS → G-NET / G-ARC.

---

## G-PUB — Publicação e ambiente IIS

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-PUB-01** | Publish Release sem debug | Alta | 1. Build `Release\|AnyCPU` no VS (0 erros). 2. Publicar com perfil Release. 3. `Scripts/2026_05_22_gdi_verify_web_release_transform.ps1`. 4. `customErrors mode="On"` no servidor. | App pool sem YSOD com stack em erro de utilizador |
| 2 | **G-PUB-02** | Validar transform `Web.Release.config` | Alta | Script acima + comparar `Web.config` publicado (`urlCompression`, `clientCache`). Ver `2026_05_22_perf012-iis-static-compression-cache.md`. | Secções PERF-012 presentes no site publicado |
| 3 | **G-PUB-03** | IIS — compressão estática/dinâmica | Média | IIS Manager → site ERP → Compression (ambos ativos). Instalar features se faltarem (doc PERF-012 §2.1). | `Content-Encoding: gzip` em `start.js` ou CSS com `?v=VersionERP` |
| 4 | **G-PUB-04** | Cache browser estáticos (304) | Média | Após publish, reload do mesmo asset com mesmo `?v=` → 304 ou cache válido (doc PERF-012 §4.2). | Segunda carga do mesmo JS/CSS mais leve |
| 5 | **G-PUB-05** | `VersionERP` / `ControlVersion` | Alta | **2026.51.03** (PUB-2). Repetir incremento se `start.js`/`start.css` mudarem antes do próximo publish. | Assets com `?v=` novo após deploy |
| 6 | **G-PUB-06** | Health check `/health` | Baixa | GET anónimo; doc `2026_05_22_health-endpoint-publish.md`; smoke `2026_05_22_gdi_smoke_health_login.ps1`. | 200 + `ok: true` + `version` no IIS |

---

## G-SMK — Smoke e homologação manual

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-SMK-01** | NFe e-Notas (gateway real) | Alta | Seguir secção «Smoke manual» em `.cursor/context/2026_05_26_nfe-enotas-arquitetura.md`: gerar, atualizar, cancelar, sincronizar em homologação. | Fluxos críticos NFe sem regressão |
| 2 | **G-SMK-02** | Smoke transversal pós-publish | Alta | Tabela abaixo (sub-itens 2.3.1–2.3.6 do checklist pendências). Novo login se cache `contextoModel_*` legado. | Cada área: fluxo principal OK ou defeito registado |
| 3 | **G-SMK-03** | Financeiro gc — cancelamento / senhas | Média | Exclusão faturamento (`AjaxFinanceiroCancelamento`); troca senha interno, portal, vendedor (`UsuariosController`). | POSTs respondem JSON esperado |
| 4 | **G-SMK-04** | Index cadastros — filtro inline | Média | Filiais (ou outro piloto): Enter, Limpar, paginação com filtro. Ver `2026_05_20_index-cadastros-primeiro-load.md`. | Grid não carrega dataset inteiro sem critério |
| 5 | **G-SMK-05** | Lookups — invalidação cadastral | Média | Procedimento em `2026_05_20_lookups-monitorizacao-pos-publish.md` (alterar cliente → refresh combo). | Combo reflete alteração após refresh/TTL |
| 6 | **G-SMK-06** | PERF-012 / PERF-015 em homologação | Média | Profiler: 2.º combo mesma tabela sem `MAX`; gzip em assets. | Evidência documentada no CHANGELOG ou nota publish |

**G-SMK-02 — sub-checklist**

- [ ] **G-SMK-02a** Identidade — login `SetTenants`, portal `AcessoPortal`, `crm/Pedidos/Index`
- [ ] **G-SMK-02b** Financeiro g — boletos, prorrogar, Excel, posição contas, robô Itaú
- [ ] **G-SMK-02c** Financeiro gc — faturamento, fechar/finalizar lançamentos, boletos e-mail, gestor franquia
- [ ] **G-SMK-02d** COMEX — importação, invoice PDF, espelho digital
- [ ] **G-SMK-02e** Comercial gc — entradas NF, carta correção, painel gerencial, relatórios
- [ ] **G-SMK-02f** GED — upload, download, tipos SGQ

---

## G-PROD — Cache global de produtos (CACHE-PROD)

> **Concluído:** PROD-000 (baseline), PROD-002a (Estoque Index typeahead). Ordem: **005b → 002c → 005a → 003 → 004 → 006 → 007**.

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-PROD-01** | Inventário filtro + grid inventário | Alta | 1. `EstoqueInventario/FormInventarioItens`: trocar filtro `GetComboGcProdutosServicosImportados` por typeahead (padrão PROD-002a). 2. `GetDadosInventarioItem`: **não** carregar `GetDatasetGcProdutosServicos` inteiro — resolver produto por linha/`id_produto` (EF/SQL). 3. Avaliar `pageLength` 1000 vs 50 (alinhar PERF-008). Ref: `2026_05_22_checklist-cache-produtos.md`, baseline JSON. | HTML filtro &lt;100 KB; Ajax grid sem 13k linhas em RAM por request |
| 2 | **G-PROD-02** | Ajax produto no pedido (005a) | Alta | `MovimentosController`: `AjaxDadosProduto`, `AjaxGetPrecoVendaProduto` — query por `id_produto` em vez de dataset completo. | 1 request por produto; sem cache `GcProdutosServicosDataset` na ação |
| 3 | **G-PROD-03** | Combo importados — recebimento estoque (003) | Média | `EstoqueController` recebimento: substituir `GetComboGcProdutosServicosImportados` por typeahead ou query local. | Abertura da tela sem 8k options no HTML |
| 4 | **G-PROD-04** | Demais `GetComboGcProdutosServicosTodos` (004) | Média | PRs pequenos: `EstoqueInventario` modal item, Atendimentos, COMEX, Lotes, NF entrada, Produtos desativar, Estoque controle. Inventário em checklist-cache-produtos §PROD-000.3. | Zero consumidores por método legado |
| 5 | **G-PROD-05** | Deprecar métodos + `LookupCacheKeys` (006) | Média | Após G-PROD-04: remover métodos órfãos do `ILookupQueryService` e chaves MemoryCache; `gdi_inventory_libdatasets_usage.py --fail`. | Build OK; inventário sem referências |
| 6 | **G-PROD-06** | Família/status produto (007) | Baixa | Opcional: `GetComboGcProdutosFamilia` / `Status` se ainda usados. | Documentar ou migrar |

---

## G-DT — DataTables (paginação SQL e índices)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-DT-01** | PERF-007b — lote 3 (8 actions) | Alta | Por controller (1 PR/área): aplicar `LibDataTableSqlPaging` ou EF `Count`+`Skip`/`Take`. Alvo: `Assistentes.GetDados`, `CentrosCustos.getDados`, `Ged.GetDados`, `Nfe.GetDados`, `Atendimentos.getDadosAtividades`, `getDadosAtendimentosLogs`, `EstoqueControle.GetDadosMedicoes`, `Parametros.GetDadosSistemas`. Script: `2026_05_22_gdi_inventory_datatables_memory_paging.py` → 0 `PENDENTE`. | Mesmo contrato JSON; sem `allRecords.ToList()` antes do total |
| 2 | **G-DT-02** | FinanceiroLancamentos GED anexo | Média | `FinanceiroLancamentosController` ~L3940: GED lançamento com `allRecords.ToList()`+`Skip` — paginar SQL ou EF. Incluir no inventário script (método pode não ser `GetDados*`). | Página de anexos com muitos registros não materializa tudo |
| 3 | **G-DT-03** | `GetDadosInvoicesItensEspelhoDigital` | Baixa | **Aceite documentado** — refactor `DataTable`/`DataRow` + dedupe; paginação SQL dedicada (doc perf007). Não bloquear lote 3. | Registado em `.cursor/context/2026_05_22_perf007-lote2-inventario.md` |
| 4 | **G-DT-04** | IndexPedido — filtro data (PERF-019) | Média | `MovimentosController.GetDados`: revisar `OR` em datas; preferir intervalo único se regra permitir. Validar plano SQL com DBA. | Menos scans; UX de filtro inalterada |
| 5 | **G-DT-05** | `deferLoading` módulos operacionais | Baixa | Financeiro g Index, Nfe, Ged, Atendimentos — fora escopo 2.8 Filiais; aplicar padrão `2026_05_20_index-cadastros-primeiro-load.md` se medição justificar. | 1.º paint sem `GetDados` até Pesquisar |
| 6 | **G-DT-07** | `NfeController.GetDados` paginação SQL | Alta | `Areas/g/Controllers/NfeController.cs` L127–158: hoje `ToList()` de `g_nfe` inteira + `Skip/Take` em memória. Aplicar `LibDataTableSqlPaging` ou EF `Count`+`Skip/Take` no SQL (padrão `AtendimentosController.getDadosAtendimentos`). Manter `JsonDataTableException` e `errorMessage`. | Ajax Index NFe: sem materializar tabela inteira (**validar** Profiler com filtro vazio e com filtro) |
| 7 | **G-DT-08** | Layout `gdi-dt-scroll-host` (overflow horizontal) | Média | **Lotes A, A+ e B concluídos** (modais + FormPedido, COMEX abas estreitas, Clientes, Atendimentos, ConsultaPedidos, DadosConsolidados, Nfe logs). **Pendente:** Lote C Index ≤10 col. Script `2026_05_22_gdi_audit_dt_scroll_host.py`; doc `.cursor/context/2026_05_22_datatables-gdi-dt-scroll-host-auditoria.md`. | Tabela vazia sem scroll fantasma; texto longo com ellipsis/`dt-wrap` |
| ~~6~~ | ~~**G-DT-06**~~ | ~~`GetDados*` g — nomes fora do padrão~~ | — | **Concluído 2026-05-20:** 4 actions em `AtendimentosController` (nomes legados); paginação EF + inventário por método. Ver `.cursor/context/2026_05_22_dt1-atendimentos-getdados-padrao.md`. | — |

---

## G-PERF-M — Performance (medição e validação)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-PERF-M01** | Contar SQL no full page load | Média | Extended Events / Profiler na 1.ª carga `IndexPedido` (navbar + corpo). Registar batches. | Número documentado no CHANGELOG ou context |
| 2 | **G-PERF-M02** | Meta pós-layout scripts | Média | **M02a concluído:** estimativa estática `Scripts/2026_05_22_gdi_perf_m02_network_baseline.py` → `2026_05_22_perf-m02-resultado.md`. **M02b pendente:** `--live` DevTools homologação (Finish &lt; 1,5 s; transferred &lt; 800 kB full page). | M02a OK (proxy layout); M02b operador |
| 3 | **G-PERF-M03** | Validar homologação = Release | Média | Confirmar perfil publish e `BundleTable.EnableOptimizations` ativo (#if !DEBUG). | Build servidor = Release local |

---

## G-PERF — Performance (código pendente)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-PERF-16** | Adiar `allYesProdutos` no login | Média | `UserIdentityController`: remover `ToList()` de `allYesProdutos` no login; grep consumidores; carregar lazy na 1.ª action que precisa. | Login mais rápido; YES intacto |
| 2 | **G-PERF-17** | Action filter de tempo | Baixa | Novo filter `PerfActionFilter` + `FilterConfig`; log `PERF\|area/controller/action\|ms` em `LibLogger` (amostragem ou &gt;2 s). | Linhas correlacionáveis em `App_Data/Logs/erp-*.log` |
| 3 | **G-PERF-18** | Índices SQL candidatos (DBA) | Baixa | Criar `.cursor/context/2026_05_20_performance-indices-candidatos.md` com tabelas da auditoria §6; **sem** script DDL sem DBA. | Documento entregue ao DBA |
| — | **G-PERF-20** | Layout — scripts condicionais (épico) | Alta | Ver sub-itens **G-PERF-20a…f** abaixo. Docs: `2026_05_22_layout-scripts-contrato-flags.md`, `2026_05_22_layout-scripts-matriz-modais.md`. | — |

### G-PERF-20 — Layout scripts (sub-itens)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| ~~1~~ | ~~**G-PERF-20a**~~ | Fase 0 — inventário + contrato | — | **Concluído 2026-05-20:** `Scripts/2026_05_22_gdi_inventory_page_scripts.py`, JSON inventário, matriz modais, `Lib/GdiPageScripts.cs` (sem filter no `FilterConfig`; layout inalterado). | — |
| ~~2~~ | ~~**G-PERF-20b**~~ | ~~Fase 1 — footer + `defer`~~ | — | **Concluído 2026-05-20:** `_Layout.cshtml`, `_LayoutScriptsAuthenticated.cshtml`; `VersionERP` **2026.51.04**. Smoke manual pendente (G-SMK). | — |
| ~~3~~ | ~~**G-PERF-20c**~~ | ~~Fase 2 — jstree/Tempus fora do global~~ | — | **Concluído 2026-05-20:** jstree 2 Index; Tempus 22 views (20c-bis); `VersionERP` 2026.51.06. Smoke: CentrosCustos, IndexPedido, Financeiro/Index, Clientes/Index modal limite. | — |
| ~~4~~ | ~~**G-PERF-20d**~~ | ~~Fase 3 — partials + filter~~ | — | **Concluído 2026-05-20:** partials DT/S2/Toggle/Jstree; filter em `FilterConfig`; opt-out DT 7 controllers; `VersionERP` 2026.51.07. | — |
| ~~5~~ | ~~**G-PERF-20e / Fase 4**~~ | ~~Opt-out por controller (lotes A+B+C)~~ | — | **Concluído 2026-05-20:** lotes A/B/C (`2026.51.08`–`2026.51.11`); doc `2026_05_22_layout-scripts-fase4-optout.md`. M02 homologação pendente. | — |
| ~~6~~ | ~~**G-PERF-20f**~~ | ~~Fase 5 — lazy load modal~~ | — | **Concluído 2026-05-20:** `GdiMainModalLoad` + patch `#mainModal` `.load`; registry partial; `VersionERP` **2026.51.12**; doc `2026_05_22_layout-scripts-fase5-lazy-modal.md`. Smoke manual pendente. | — |
| 5 | **G-PERF-21** | e-Notas/boletos assíncronos | Baixa | Não bloquear UI: fila JobServer ou polling Ajax; `NfeController`, `Robos/ENotas/`. Risco alto. | Smoke homologação definido |
| 6 | **G-PERF-22** | Cache cotação dólar | Baixa | `RoboCotacaoDolar` — MemoryCache diário por app pool. | ≤1 chamada externa/dia/pool |
| 7 | **G-PERF-23** | Desabilitar EF lazy loading | Baixa | `GdiPlataformEntities`; smoke amplo (pedidos, financeiro, lookups). Risco **alto**. | Inventário regressões = 0 |
| 8 | **G-PERF-24** | Serviço read-only Movimentos | Baixa | Extrair queries de leitura de `MovimentosController` — **não** misturar com correções cirúrgicas. | PR dedicado |
| 9 | **G-PERF-25** | Redis lookups multi-nó | Baixa | Só se farm IIS ≥2 nós; senão cancelar. | Decisão infra documentada |
| 10 | **G-PERF-26** | Connection string read-only relatórios | Baixa | `Relatorios*` → CS read-only. | Relatórios não bloqueiam OLTP |
| 11 | **G-PERF-27** | `GetGedPedido` — usuários sob demanda | Média | `MovimentosController.cs` `GetGedPedido` (~L638): remover `g_usuarios` completo; resolver login por `id_usuario_cadastro` só dos arquivos da página (dict EF/SQL). Opcional: paginação SQL em `ged_arquivos`. | Aba GED do pedido: 1 query usuários proporcional à página (**validar** modal pedido) |
| 12 | **G-PERF-28** | Cache totais BH/SP — Estoque Ajax | Média | `EstoqueController.GetDadosEstoque` L61–71: dois `Sum()` em todo `g_produtos` a cada draw. MemoryCache global `estoque_valor_totais_{database}` TTL 1–5 min ou invalidar em movimentação estoque. | 2.º draw em &lt;60 s sem 2× SUM catálogo (**validar** Profiler) |
| 13 | **G-PERF-29** | `LibLogger` fila assíncrona | Baixa | `Lib/LibLogger.cs`: evitar `File.AppendAllText` + `lock` na thread da request; fila + flush em background (ou amostragem). Não alterar contrato público `Error/Warn/Info`. | Pico de erros não aumenta P95 das actions (**validar** carga sintética) |
| 14 | **G-PERF-30** | COMEX CreateEdit — defer lookups | Média | `ComexImportacoesController` CreateEdit (~L747+): não carregar listas completas `g_produtos`/NCM/COMEX no primeiro paint; typeahead ou aba `shown.bs.tab`. Ref: auditoria §4 COMEX. | Abertura CreateEdit importação: HTML inicial &lt; baseline PROD-000 (**validar** DevTools) |

**Nota:** PERF-001 a PERF-015 e PERF-006/007/008/009/010/011/012/013 estão **concluídos** (ver `CHANGELOG-DEV.md` 2026-05-20). Auditoria completa: `.cursor/context/2026_05_22_performance-audit-erp.md` (2026-05-20, read-only).

---

## G-LKP — Lookups (controllers híbridos → `*.Lookups.cs`)

> Inventário: 14 controllers com `ViewBag.combo*` sem `*.Lookups.cs`. Decisões em `2026_05_20_lookups-convencao-index-vs-createedit.md` §1.7.3. **Um PR por controller** (ou lote de 2).

| Ordem | ID | Controller | Decisão doc | Instruções resumidas |
|------:|-----|------------|-------------|---------------------|
| 1 | **G-LKP-01** | `FiliaisController` | Lookups.cs | **Feito** — `FiliaisController.Lookups.cs` + `GetComboGColigadas`. |
| 2 | **G-LKP-02** | `VendedoresController` | Lookups.cs | **Feito** — `GetComboGRevendasVendedorForm`. |
| 3 | **G-LKP-03** | `ContasCaixasController` | Lookups.cs | **Feito** — `GetComboGCidadesAtivas` / `GetComboGUf`. |
| 4 | **G-LKP-04** | `NfeController` | Lookups.cs | **Feito** — `NfeController.Lookups.cs`. |
| 5 | **G-LKP-05** | `ProdutosNcmController` | Lookups.cs | **Feito** — combos fiscais no serviço. |
| 6 | **G-LKP-06** | `FinanceiroController` (g) | Lookups.cs | **Feito** — `GetComboGFinanceiroStatusTitulos` + boletos. |
| 7 | **G-LKP-07** | `ImportacoesBancariasController` | Lookups.cs | **Feito** — `GetComboGContasCaixasBoletoEmissao`. |
| 8 | **G-LKP-08** | `ComexFinanceiroController` | Lookups.cs | **Feito** — `ComexFinanceiroController.Lookups.cs` (saldo cambial). |
| 9 | **G-LKP-09** | `RelatoriosComerciaisController` | Lookups.cs | **Feito** — `GetComboGVendedoresRelatorioComercial` + partial. |
| — | **G-LKP-LOC** | CentrosCustos, ClassificacaoFinanceira, ComexImportacoes Index, RelatoriosCadastrais, Usuarios | Manter local | Decisão §1.7.3 — combos hierárquicos/paramétricos; sem `*.Lookups.cs`. |

**Aceite comum G-LKP-01…09 (feito):** build Release; smoke CreateEdit/Index do módulo; `python Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py` sem regressão de contagem híbrida.

---

## G-NFE — NFe e menu legado

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-NFE-01** | Smoke e-Notas | Alta | = **G-SMK-01** (não duplicar trabalho). | Ver G-SMK-01 |
| 2 | **G-NFE-02** | Menu Portal Vendedor (BD) | Baixa | Script `Scripts/2026_05_22_gdi_sql_deactivate_portal_vendedor_menu.sql` — **executar no SQL** homolog/prod. Código: roles troca senha → `g_Vendedores_*` (removido `g_PortalVendedor_*`). | Menu não aponta para 404 |

---

## G-FLT — Filtro genérico SQL (limpeza)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-FLT-01** | `qa/GedSGQ` | Baixa | ~~Remover `yesFilterAdvancedText`~~ **feito 2026-05-20**. | Grelhas SGQ só filtro custom fields / `g_filtros` |
| 2 | **G-FLT-02** | `gc/EstoqueInventario` | Baixa | ~~Idem~~ **feito 2026-05-20**. | Sem `yesFilterAdvancedText` em `GetDadosInventario` |
| 3 | **G-FLT-03** | `jQueryDataTableParamModel` | Baixa | ~~Remover `yesFilterAdvancedText`~~ **feito 2026-05-20**; ~~operador/texto~~ removidos em G-FLT-06. | Contrato: `yesFilterField` + custom fields |
| 4 | **G-CLI-01** | Clientes / perfil -800 | Baixa | ~~Bloqueio ERP + SQL menu opcional~~ **feito 2026-05-20**. Ver `2026_05_22_clientes-perfil-vendedor-removido.md`. | Vendedor sem cadastro Clientes no monólito |
| ~~5~~ | ~~**G-FLT-04**~~ | ~~TempData `yesFilter*` no login~~ | — | **Concluído 2026-05-20:** removidos 6× `TempData.Remove` em `UserIdentityController.Index`. | — |
| ~~6~~ | ~~**G-FLT-05**~~ | ~~LibDB ramo SQL genérico~~ | — | **Concluído 2026-05-20:** `getFilterByUser` sem `SentencaSQLFiltroGenerico`; `SentencaSQLFiltroGenerico` `[Obsolete]`. | — |
| ~~7~~ | ~~**G-FLT-06**~~ | ~~Modelo DataTables~~ | — | **Concluído 2026-05-20:** removidos `yesFilterOperador`/`yesFilterText` do modelo. | — |
| ~~8~~ | ~~**G-FLT-07**~~ | ~~`getFilterByUser` 3º parâmetro~~ | — | **Concluído 2026-05-20:** removido `paramAdvanced` de `getFilterByUser`; `setFilterByUser(..., advanced)` e `g_filtros.advanced` **mantidos** (filtros inline Index). | — |

---

## G-LOGIN — Tela de login (TempData)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| ~~1~~ | ~~**G-LOGIN-01**~~ | ~~Chrome login TempData~~ | — | **Concluído 2026-05-20:** `SaveLoginChromeToTempData` / `ApplyLoginChromeToViewBag` em `UserIdentityController`. | — |

---

## G-UX — Mensagens pós-redirect

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| ~~1~~ | ~~**G-UX-01**~~ | ~~TempData message PRG~~ | — | **Concluído 2026-05-20:** `Lib/LibFlashMessage.cs` (`SetModalMessage`, `SetError`, `SetInfo`); controllers + `ModalError.cshtml`; login/troca senha/logout. | — |

---

## G-ENC — Higiene e verificação de publish

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-ENC-01** | UTF-8 BOM antes de publish | Baixa | `python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail` — corrigir se &gt;0. | 0 ficheiros com BOM |
| 2 | **G-ENC-02** | Views `Gdi*` no `.csproj` | Baixa | Após alterar views com helpers: `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` → 0 lacunas. | Publish inclui todas as `.cshtml` |
| ~~3~~ | ~~**G-ENC-03**~~ | ~~Comentário TempData morto~~ | — | **Concluído 2026-05-20:** removido em `UsuariosController.cs`. | — |

---

## G-NET — Migração .NET 4.8.1 (trilha separada)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-NET-01** | Plano de migração | Baixa | Ler `.cursor/context/2026_05_20_migracao-472-481.md` integralmente. | Equipa alinhada |
| 2 | **G-NET-02** | Checklist próprio | Baixa | NuGet, TLS, `Web.config`, `ComexImportacoesController`, publish profile — **PR dedicado**. | **Não** misturar com G-PUB/G-DT/G-PROD na mesma branch |
| 3 | **G-NET-03** | NuGet — lote 2 limpeza | Baixa | Após auditoria 2026-06-05 (86 pacotes): avaliar remoção `SkiaSharp.NativeAssets.Linux/macOS` (IIS Win), `ZString`, `bootstrap` NuGet legado (`BundleConfig` vs LibUI), `System.Runtime.Caching` pacote vs GAC; smoke relatórios NPOI/ClosedXML. | Build Release + export Excel/PDF OK |

---

## G-INT — Integridade MVC/Ajax/DataTables (lotes servidor N-*)

**Concluído (2026-05-25):** lotes N-A…N-P.1 + memória arquitetura centralizada — ver `CHANGELOG-DEV.md` e `AI-CONTEXT.md`. **N-P.1:** inventário ERP (`2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py`) — 123 ocorrências classificadas (preservadas); LookupAjax **g** alinhado; `service_msg_append` em `EstoqueInventarioService`. **N-P:** gc (63) + LookupAjax gc + `GetDados` COMEX. **N-O:** varredura final exports + `AjaxFailure*Message`. Ciclo Ajax **g** e **gc** fechados (0 candidatos `ajax_json_*`); **N-Q (qa + a)** concluído; **N-R.1–R.5** (`Nfe`, `Financeiro`, `Ged`, `Clientes`, `Atendimentos`/`Produtos`/`ProdutosNcm`/`Usuarios`) concluídos.

**Concluído (2026-05-25):** **N-S.1–S.3** — `ComexProdutos` `idProcessamento`; onda `mvc_modelstate` **g** (50×); modais GET **gc** (10×). Inventário ERP: **59** preservadas (exit 0).

**Concluído (2026-05-25):** **N-T.1–T.3** — `id_processamento` **g**+**gc**; `mvc_modelstate` **gc** (12×); `Nfe` `modal_flash`. **`Areas/g` = 0** `LibExceptions` no inventário ERP.

**Concluído (2026-05-25):** **N-U.1–U.4** — uploads COMEX, `AjaxFailureWebMessage`, portal boleto **crm**.

**Concluído (2026-05-25):** **N-V…C-1** — ciclo N encerrado: residual gc → Lib; DT `DataTableSuccess*` (31 controllers); `GetFichaEstoqueProduto` Fase 8; `.csproj` `ModalTransferirContaCaixa`; cliente **gc/Movimentos** (24 views); `AjaxFailureIdProcessamento`. Inventário ERP: **0** `LibExceptions`; **verify_csproj:** 0 lacunas.

**Concluído (2026-05-25):** **Fase 18 C-2 + I-1** — 115 views Ajax homogéneas (262 linhas); `FinanceiroController` 4 modais GET com `TryParse`; inventário `inventory_ajax_hybrid_handlers.py` → exit 0.

**Concluído (2026-06-05):** **G-PUB/SMK** — `Scripts/2026_06_05_gdi_smoke_architecture_inventories.py` (5 inventários exit 0). **I-1b** — `Relatorios*` + `Ged.GetDados` → `LibNumbers.ConvertInt` (18×).

**Próximo lote sugerido:**
1. **I-1c (opcional):** `FinanceiroController` boleto/HTML + `ImportacoesBancarias` CNAB — `int.Parse` residual (7× em **g**).
2. **G-ARC / G-PERF:** trilhas separadas (cache combos, SQL em memória DT, `InvalidateForTable`).
3. **Smoke manual pós-publish:** pedido Movimentos, export relatório comercial, modal Financeiro.

---

## G-ARC — Arquitetura (opcional / alto risco)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-ARC-01** | Cache combos globais (clientes) fase 2+ | Baixa | Avaliar chaves compostas / serviço dedicado se reintroduzir cache global (histórico CHANGELOG). Hoje: typeahead (CACHE-2 concluído). | Decisão arquitetural escrita |
| 2 | **G-ARC-02** | Integrar `InvalidateForTable` em gravações | Média | Após save cliente/produto/etc., chamar `LookupCacheInvalidator.InvalidateForTable` onde aplicável (hoje depende de `IsTableUpdate`). | Combo atualiza sem esperar TTL |

---

## G-OPS — Operação pós-publish (registo)

| Ordem | ID | Item | Crit. | Instruções | Aceite |
|------:|-----|------|-------|-----------|--------|
| 1 | **G-OPS-01** | Registo publish lookups | Média | Após deploy: data, `VersionERP`, resultado G-SMK-05 e RAM w3wp (doc monitorização). | Linha em `CHANGELOG-DEV.md` |
| 2 | **G-OPS-02** | Menu MovimentosCompras BD | Baixa | Desativar entradas `/gc/MovimentosCompras/*` se existirem (módulo removido 2026-05-20). | Sem links mortos no menu |

---

## Resumo executivo (contagem)

| Grupo | Itens abertos (aprox.) | Prioridade imediata |
|-------|------------------------|-------------------|
| G-PUB | 6 | Publish homologação/produção |
| G-SMK | 6 + 6 sub-itens | Homologação funcional |
| G-PROD | 6 | Inventário + Ajax pedido (RAM) |
| G-DT | 7 | PERF-007b (6 grids) + Nfe GetDados (G-DT-07) |
| G-PERF-M | 3 | Medição |
| G-PERF | 14 | Login, telemetria, GED pedido, Estoque SUM, COMEX, logger |
| G-LKP | 8 PRs + 5 “manter local” | Média (por módulo) |
| G-NFE / G-FLT / G-ENC / G-NET / G-ARC / G-OPS | 2–10 | Baixa ou trilha separada |

**Total checklist ativo:** ~59 linhas acionáveis (exclui sub-itens SMK e itens “manter local” sem PR).

---

## Concluídos recentemente (não repetir no backlog)

Registo detalhado: **`CHANGELOG-DEV.md`** (tabela «Últimas alterações») e **`docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`**.

| Grupo | Exemplos concluídos (2026-05-20) |
|-------|----------------------------------|
| Lookups | LibDataSets → `ILookupQueryService`; typeahead clientes/produtos; CACHE-2; smoke 1.3 |
| DataTables | Fases g/gc 13–17; PERF-006 Financeiro; PERF-007 lote 2; mensagens GdiDt* |
| Performance UI | PERF-001–005 navbar/Index; PERF-008–011 FormPedido/Clientes; PERF-012–015 |
| UI / legado | Tabelas MVC; PascalCase B2; filtro g/ComexProdutos; MovimentosCompras removido |
| Produtos | PROD-000, PROD-002a |
| Outros | Google Analytics removido; UTF-8 BOM lote; Financeiro g POSTs órfãos |

*Atualizar este ficheiro ao fechar itens: marcar `[x]`, mover resumo para CHANGELOG, ajustar contagem no resumo executivo.*
