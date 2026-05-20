# CHANGELOG-DEV — histórico inicial (arquivo preservado)

> **Snapshot:** cópia integral de `.cursor/CHANGELOG-DEV.md` antes da reestruturação documental (2026-05-20).  
> **Não editar** para registo operacional diário — usar `CHANGELOG-DEV.md` na raiz do repositório.  
> **Índice vivo:** `AI-CONTEXT.md`, `BACKLOG-DEV.md`, `.cursor/context/*.md`

---

# CHANGELOG-DEV — GDI-ERP-Plataform
# Projeto: GDI Aviação | ERP Plataform
# Stack: ASP.NET MVC .NET Framework 4.7.2 + SQL Server + Bootstrap 5 + AdminLTE 4
#
# INSTRUÇÕES:
# - Novas entradas sempre no TOPO da seção de histórico
# - Formato obrigatório definido no .cursor/rules
# - Este arquivo é lido pelo Cursor antes de cada intervenção
# - Mantenha registros objetivos — sem prolixidade
# - Não apague entradas antigas; elas são memória do projeto

---
### [2026-05-20] — Grupo 2.11: UTF-8 BOM — inventário + validação scripts (0 conversões)
**Tipo:** Análise | Padronização
**Arquivos tocados:**
- `Scripts/2026_05_20_gdi_inventory_utf8_bom.py` (novo, read-only)
- `.cursor/context/2026_05_20_utf8-bom-lotes.md`, checklist §2.11
- `GDI-ERP-Plataform.csproj` — `<None Include>` dos 3 scripts UTF-8

**Problema / Demanda:** Checklist 2.11 — avaliar scripts `utf8_no_bom_*` e executar lote pequeno por área sem misturar com refactor funcional.

**O que foi feito:** Lote global `--project` (146 ficheiros) já registado no CHANGELOG. Inventário projeto inteiro → **0** BOM / UTF-16. Smoke: `utf8_no_bom_area_a.py`, `areas.py` em `Areas/g`, `Areas/gc`, `Lib` → 0 regravados. Scripts mantidos para regressões; doc com arquitetura inventário → conversão por pasta → re-scan.

**Evitado:** Reexecutar `--project` sem BOM (diff massivo desnecessário).

---
### [2026-05-20] — Grupo 2.10: Ficheiros órfãos — ProdutosTipos (N/A) + verify csproj Gdi helpers
**Tipo:** Análise | Documentação
**Arquivos tocados:**
- `.cursor/context/2026_05_20_ficheiros-orfaos-produtostipos.md` (novo)
- `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` — §2.10 marcado [x]

**Problema / Demanda:** Checklist 2.10 — confirmar `Areas/g/Views/ProdutosTipos/*` e `ProdutosTiposController.cs` (incluir no `.csproj` ou apagar); correr verify após mudanças em views.

**O que foi feito:** Pasta views e controller **já ausentes** do disco; `.csproj` sem `ProdutosTipos`; cadastro de tipos mantido via `GetComboGProdutosTipos` em `Produtos/CreateEdit`. Scan Areas: **0** controllers/views fora do csproj. `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` → 183 cshtml com Gdi*, **0** missing, exit 0.

**Evitado:** Recriar módulo MVC `ProdutosTipos` (funcionalidade coberta por combo/lookup).

---
### [2026-05-20] — Grupo 2.9: Filtro genérico SQL legado — ramos mortos servidor (g + ComexProdutos)
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/g/Controllers/AssistentesController.cs`, `GedController.cs`, `NfeController.cs`, `CentrosCustosController.cs`, `ClassificacaoFinanceiraController.cs`
- `Areas/gc/Controllers/ComexProdutosController.cs` — removido bloco `filterAdvanced`
- `Scripts/2026_05_20_gdi_inventory_legacy_filter_server.py`, `.cursor/context/2026_05_20_filtro-generico-legado-limpeza.md`

**Problema / Demanda:** Views já sem `yesFilterOperador`/`yesFilterText`/`yesFilterAdvancedText` (lote 2026-05-19); servidor ainda tinha ramos inalcançáveis.

**O que foi feito:** Grep em `Areas/g/*.cs` → zero referências aos campos legados. Removidos `filterAdvanced` e `SentencaSQLFiltroGenerico` onde a UI não envia mais dados; mantido `filterDb` via `g_filtros`. `jQueryDataTableParamModel` e `LibStringFormat` preservados (qa/gc). Build Release OK.

**Evitado:** Remover propriedades do `jQueryDataTableParamModel` ou `LibDB` (quebra contrato noutros módulos).

---
### [2026-05-20] — Grupo 2.8: Index cadastros — grelha vazia no 1.º load (Filiais)
**Tipo:** Correção | Performance
**Arquivos tocados:**
- `Areas/g/Controllers/FiliaisController.cs` — gate `listarTodosExplicito` / critério inline; `IQueryable` + paginação SQL
- `Areas/g/Views/Filiais/Index.cshtml` — filtro Id/Nome, `deferLoading`, Pesquisar/Limpar/Enter
- `Areas/g/Models/CstFiliaisIndex.cs`, `.csproj`
- `Scripts/2026_05_20_gdi_inventory_index_first_load.py`, `.cursor/context/2026_05_20_index-cadastros-primeiro-load.md`
- `Areas/g/Views/Produtos/Index.cshtml` — `LibMessageProcessandoHide` em `xhr.dt`

**Problema / Demanda:** Checklist 2.8 — evitar `GetDados` com lista completa sem critério (ex. Produtos no CHANGELOG; Filiais carregava todas as filiais ao abrir).

**O que foi feito:** Produtos já conforme (fase 2 CHANGELOG 2026-05-19). Filiais alinhado ao padrão Cidades/Perfis. Inventário documenta cadastros OK vs. módulos operacionais sem `deferLoading`.

**Atenção:** Smoke manual no browser (Enter, Limpar, paginação).

---
### [2026-05-20] — Grupo 2.7: UI tabelas MVC + sidebar portal + VersionERP
**Tipo:** Correção | Documentação
**Arquivos tocados:**
- `Areas/gc/Views/ComexProdutos/FormProcessarProdutosPreNovos.cshtml`, `FormProcessarProdutosPreAtualizar.cshtml` (já com `gdi-form-table-*`)
- `Areas/gc/Views/MovimentosEntradas/FormProcessarNFImportacao.cshtml`, `FormProcessarNFCompraNacional.cshtml`, `FormProcessarNFDevolucao.cshtml`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css`, `start.js` (`jsInitForm` scrollLeft)
- `Views/Shared/_Navbar.cshtml` — portal: ocultar faixa MS/AWS; logo link `crm/Pedidos/Index`
- `ControlVersion.cs` → **2026.51.02**; checklist §2.7; `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md`

**Problema / Demanda:** Checklist 2.7 — separar tabelas DataTables vs MVC; validar layout portal.

**O que foi feito:** Inventário Python: **0** views MVC com `scroll-body-horizontal` incorreto. Forms COMEX/entradas já migrados. Sidebar portal: só «Sair» no dropdown; parceiros MS/AWS ocultos; logo aponta a Pedidos. `VersionERP` incrementado para cache de `start.css`/`start.js`.

**Atenção:** ~60 Index/modais DataTables mantêm `scroll-body-horizontal` + `display` (intencional).

---
### [2026-05-20] — Grupo 2.5: Financeiro g — remoção transferir conta e POSTs órfãos
**Tipo:** Refatoração | Documentação
**Arquivos tocados:**
- `Areas/g/Controllers/FinanceiroController.cs` — removidos `ModalTransferirContaCaixa`, `AjaxTransferirContaCaixa`, `AjaxSaveFinanceiroAvulso`, `AjaxDownloadBoletos`, relatórios CSV órfãos (ex-Faturamentos)
- Removidos: `Areas/g/Views/Financeiro/ModalTransferirContaCaixa.cshtml`, `CstFinanceiroTransferirContaCaixa.cs`, `CstFinanceiroLancamentos.cs`
- `Areas/g/Views/Financeiro/Index.cshtml` — item menu e JS `modalTransferirContaCaixa`
- `.cursor/context/2026_05_20_financeiro-g-posts-mapeamento.md`, checklist §2.5, `Scripts/2026_05_20_gdi_check_specific_modals.ps1`, `.csproj`

**Problema / Demanda:** Limpar código morto após remoção de `FinanceiroFaturamentos`/`FinanceiroLancamentos`; transferir conta não usado em produção.

**O que foi feito:** Mapeamento e decisões documentados; mantidos POSTs com view (`AjaxCancelarTitulos`, remessa, e-mail, etc.) e `AjaxFinanceiroCancelamento` (gc). Build Release OK.

**Atenção:** Smoke manual §2.5 no doc de mapeamento.

---
### [2026-05-20] — Grupo 2.4: PascalCase — paths Git views modal* + models cst* (B1/B2)
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/g/Views/Nfe/ModalCancelarNfe.cshtml`, `ModalExportarDadosNfePDF.cshtml` (git rename)
- `Areas/g/Views/Usuarios/ModalUsuarioTrocarSenha.cshtml` (git rename — alinhamento csproj)
- **61** ficheiros `Areas/**/Models`, `Models/CstTenant.cs`, `Robos/SintegraWS/Models` — `cst*.cs` → `Cst*.cs` no índice Git
- `Areas/g/Controllers/NfeController.cs`, `GDI-ERP-Plataform.csproj`
- `.cursor/context/2026_05_20_pascalcase-areas-renomeacao-lotes.md`, checklist §2.4

**Problema / Demanda:** Classes já `Cst*` (B2 2026-05-19), mas paths no Git e 3 views NFe ainda em `modal*`/`cst*` — risco em FS case-sensitive e publish Linux.

**O que foi feito:** `git mv` em dois passos (Windows) para 3 views + 61 models; `return View("ModalExportarDadosNfePDF")`. `FinanceiroFaturamentos` N/A (controller/views removidos). MSBuild Release 0 erros.

**Smoke sugerido:** NFe cancelar/exportar PDF; troca senha vendedor (`ModalUsuarioTrocarSenha`).

---
### [2026-05-20] — Grupo 2.2: NFe / e-Notas — revisão Fase 17, arquitetura, Portal Vendedor N/A
**Tipo:** Análise | Documentação | Correção pontual
**Arquivos tocados:**
- `.cursor/context/2026_05_20_nfe-enotas-arquitetura.md` (novo) — fluxo `g_nfe`↔`gc_movimentos_nf`, mapa Ajax↔`RoboEnotasNFE`, smoke 2.2.4
- `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` — §2.2.1–2.2.3 concluídos; 2.2.4 smoke pendente homologação
- `Areas/g/Controllers/NfeController.cs` — view export: nome alinhado ao ficheiro `modalExportarDadosNfePDF.cshtml`
- `GDI-ERP-Plataform.csproj` — `Content Include` export PDF (casing disco)
- `Areas/g/Views/Nfe/ModalImportarNfeLote.cshtml` — erro Ajax: `GdiAjaxNotifyInconsistencias` (padrão modais NFe)

**Problema / Demanda:** Fechar checklist 2.2 sem reabrir Portal Vendedor (removido 2026-05-19).

**O que foi feito:** Confirmada integração Fase 17 (gerar/atualizar/cancelar/sincronizar via `RoboEnotasNFE`; clonar/e-mail/export/import locais). `GetDados*` já com contrato DataTables. Portal Vendedor: 2.2.2 N/A; roles `g_PortalVendedor_*` apenas troca senha vendedor. Correção casing view export para publish.

**Atenção:** Executar smoke §2.2.4 em ambiente com gateway e-Notas; desativar menu `/g/PortalVendedor/PortalFinanceiro` em BD se existir.

---
### [2026-05-20] — Grupo 2.1: DataTables cliente (lote 2) + servidor g validado
**Tipo:** Implementação | Análise
**Arquivos tocados:**
- `Areas/g/Views/Nfe/CreateEdit.cshtml` — logs NFe: `xhr.dt` com `return` após `GdiDtNotifyJsonErrorMessage`
- `Areas/g/Views/*/Index.cshtml` (13 cadastros) — `xhr.dt`: bloco vazio `{ }` → `{ return; }`
- `Areas/gc/Controllers/ComexProdutosController.cs` — `param` nulo em `GetDados` e `GetDadosProdutosPre`
- `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` — §2.1.1/2.1.2 marcados

**Problema / Demanda:** Fechar checklist 2.1 (lote CHANGELOG): views `GdiDt*` e contrato servidor `GetDados*` em cadastros `g`.

**O que foi feito:** Inventário `2026_05_20_gdi_inventory_datatables_g_area.py` (27 actions) — servidor `g` já com `try/catch`, `JsonDataTableException`, `errorMessage`/`stackTrace`/`yesFilterOnOff` (Fases 13–16). Cliente: Nfe Index/ComexProdutos já OK; corrigido `return` em 13 Index `g` + logs em Nfe CreateEdit. `ProdutosTipos/Index` inexistente (N/A). ComexProdutos servidor: guard `param == null`. `gdi_verify_csproj_gdi_helpers.py --fail` → 0 lacunas.

**Decisões:** CentrosCustos/ClassificacaoFinanceira usam jstree (sem DataTable Index); Assistentes sem pasta Views — fora do lote views.

**Atenção:** Após publish, smoke grelhas NFe, cadastros g (Index) e COMEX produtos/pré.

---
### [2026-05-20] — Grupo 1.6: typeahead Ajax clientes/produtos (form pedido)
**Tipo:** Implementação
**Arquivos tocados:**
- `Lib/Lookups/LookupAjaxContracts.cs`, `LookupSearchQueries.cs`
- `Areas/gc/Controllers/MovimentosController.LookupAjax.cs`, `MovimentosController.Lookups.cs`
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`, `ModalPedidoInsertEditItem.cshtml`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-select2.js`
- `.cursor/context/2026_05_20_lookups-typeahead-ajax-pedidos.md`

**O que foi feito:** GET `GetClientesLookup` / `GetProdutosLookup` com `{ items: [{ id, text }] }`; Select2 Ajax via `data-gdi-lookup-url`; form pedido e modal item sem carregar combos completos no primeiro paint; filtro vendedor em pesquisa de clientes.

---
### [2026-05-20] — Grupo 1.9: qualidade lookups (EF6, DI piloto, testes, monitorização)
**Tipo:** Refatoração | Testes | Documentação
**Arquivos tocados:**
- `Lib/Lookups/LookupQueryService.cs`, `.Comercial.cs`, `.Financeiro.cs`, `.CadastrosG.cs` — `.AsNoTracking()` + projeções em factories EF
- `Areas/gc/Controllers/EstoqueController.cs`, `EstoqueController.Lookups.cs` — ctor `ILookupQueryService` piloto
- `Areas/g/Controllers/AtendimentosController.cs`, `AtendimentosController.Lookups.cs` — idem
- `Tests/GDI-ERP-Plataform.Lookups.Tests/*` — 3 cenários novos (invalidator facade, registry isolamento, chaves contato pedido)
- `.cursor/context/2026_05_20_lookups-monitorizacao-pos-publish.md` (1.9.4)

**O que foi feito:** Leituras de combo sem tracking no DbContext do request; SqlQuery/Find mantidos onde já existiam. DI opcional em 2 controllers com fallback `LookupQueryServiceAccessor`. Testes exe ampliados; guia de smoke pós-publish para RAM e invalidação `g_clientes`.

**Exceções intencionais:** `SqlQuery` contas caixas gerencial; `Find` em CFOP faturamento; combos estáticos sem EF.

---
### [2026-05-20] — Grupo 1.7/1.8: convenção lookups + partials domínio no serviço
**Tipo:** Documentação | Refatoração
**Arquivos tocados:**
- `.cursor/context/2026_05_20_lookups-convencao-index-vs-createedit.md` (novo) — regra Index vs CreateEdit, inventário 14 híbridos, decisões 1.7.3
- `Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py` (novo), `Scripts/2026_05_20_gdi_split_lookup_wave6a.py` (one-off)
- `Lib/Lookups/LookupQueryService.Comercial.cs`, `.Financeiro.cs`, `.CadastrosG.cs` (novos); removido `LookupQueryService.Wave6a.cs`
- `GDI-ERP-Plataform.csproj`, `CLAUDE.md`, `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc`, checklist

**O que foi feito:** Documentada convenção (Index local sem cache; CreateEdit via serviço). Inventário automatizado de controllers sem `*.Lookups.cs`. Wave6a repartido em 3 partials por domínio; `ILookupQueryService` inalterado. Build Debug OK; `gdi_inventory_libdatasets_usage.py --fail` → 0 refs.

**Decisões:** Não unificar tudo em `LookupQueryService.cs` (ficheiro núcleo já ~600 linhas). Migração dos 14 híbridos fica para PRs de 1–2 controllers (ordem no contexto).

---
### [2026-05-20] — Grupo 1.5.2/1.5.3: EstoqueLotes + Estoque Index — lookups híbridos
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/gc/Controllers/EstoqueLotesController.Lookups.cs` — `LoadCombos` + `PreencherLookupsComexImportacoes` via `GetComboGcComexImportacoesTodas`
- `Areas/gc/Controllers/EstoqueLotesController.cs` — removido LINQ local de importações
- `Areas/gc/Controllers/EstoqueController.Lookups.cs` — `PreencherLookupsEstoque` no partial
- `Areas/gc/Controllers/EstoqueController.cs` — Index só chama partial; removido combo importados + `[ TODOS OS ITENS ]`
- `Lib/Lookups/ILookupQueryService.cs`, `Lib/Lookups/LookupQueryService.cs` — `GetComboGcProdutosPosicaoEstoqueIndex` (Opção A, sem cache)

**Problema / Demanda:** Controllers híbridos com LINQ duplicado; Index usava `comboProdutosServicos` de importados enquanto `PreencherLookupsEstoque` preenchia `comboProdutos` morto.

**O que foi feito:** Lotes: importações centralizadas com prefixo `[ IMPORTAÇÃO ]` e filtro `id > 0`. Estoque Index: combo alinhado à view (`comboProdutosServicos`) com entradas `[ TODOS OS PRODUTOS ]` / `[ PRODUTOS COM SALDO ]` e truncamento legado por largura de ecrã. Build Debug OK.

**Decisões técnicas relevantes:**
- Opção A sem MemoryCache neste combo (depende de `DisplayScreenWidth` por sessão).
- `GetComboGcComexImportacoesTodas` não filtra `id_importacao > 0`; filtro mantido no partial (paridade com legado).

**Atenção para próximas intervenções:** `GetDadosEstoque` ignora valores 0/-1 no filtro (só `idProduto > 0`); comportamento de grelha inalterado.

---
### [2026-05-20] — Grupo 1.5.1: MovimentosEntradasController — lookups no partial
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosEntradasController.Lookups.cs` — PreencherLookups* (frete, tipos modal, produtos nacional/devolução)
- `Areas/gc/Controllers/MovimentosEntradasController.cs` — removido LINQ/ViewBag.combo* inline

**O que foi feito:** `comboMovimentosTipos` (3 modais) e `comboProdutos` (nacional via `GetComboGcProdutosServicosTodos`; devolução via itens movimento ref.) centralizados no partial. Sem novos métodos no serviço. Build Debug OK.

---
### [2026-05-20] — Grupo 1.4: higiene documentação LibDataSets / lookups
**Tipo:** Documentação
**Arquivos tocados:**
- `.cursor/context/2026_05_20_libdatasets-diagnostico-e-plano.md` — Fases 2–5 e 6a/6b concluídas; arquitetura pós-6b; rollback via git
- `.cursor/context/2026_05_20_lookups-libdatasets.md` — regenerado (59 Get*)
- `.cursor/context/2026_05_20_lookups-libdatasets-inventario.md` — banner arquivado
- `Scripts/2026_05_20_gdi_libdatasets_obsolete_attrs.py` — legado; nao muta sem LibDataSets.cs
- `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` — 1.4 concluído

**O que foi feito:** Plano e referencias alinhados a `ILookupQueryService` apos remocao de `LibDataSets.cs`; script ObsoleteAttrs seguro (exit 0).

---
### [2026-05-20] — Grupo 1.3: smoke manual pós-Onda 6b (lookups) — concluído
**Tipo:** Validação / documentação
**Arquivos tocados:**
- `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` — secção 1.3 (1.3.1–1.3.20) marcada concluída

**O que foi feito:** Registado smoke manual **OK** em ambiente local com dados reais: login/navbar, Movimentos (pedidos/modais), compras, financeiro gc, inventário, produtos, atendimentos, contratos, estoque/controle/COMEX, clientes, relatórios financeiros, GED/SGQ, CFOP, fretes, lotes, NF importação. Sem defeito de lookup que exija commit de correção nesta onda.

**Atenção para publish:** Após deploy, validar 1.3.1 (novo login se cache `contextoModel_*` legado). Itens 1.5.x (controllers híbridos) permanecem pendentes no checklist.

---
### [2026-05-20] — Grupo 1.2: auditoria Get* (contrato vs *.Lookups.cs)
**Tipo:** Análise / validação
**Arquivos tocados:**
- `Scripts/2026_05_20_gdi_audit_lookup_get_names.py` (novo)
- `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` — itens 1.2.1–1.2.4

**O que foi feito:** Comparados 59 métodos em `ILookupQueryService` com 142 chamadas em 19 partials `*.Lookups.cs`: 0 fora do contrato, 0 divergência de casing (`GetComboGProdutosNcm` OK). `GetDatasetGcClientesDestinatarios` usado em `MovimentosController.cs` apenas. Build Debug OK; sem alteração C#.

---
### [2026-05-20] — Grupo 1.1: build Debug/Release + guardrails lookups (Onda 6b)
**Tipo:** Análise / validação
**Arquivos tocados:**
- `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md` — itens 1.1.1–1.1.4 marcados
- `.cursor/context/2026_05_20_lookups-libdatasets.md` — regenerado (59 métodos)

**O que foi feito:**
MSBuild `GDI-ERP-Plataform.csproj` `Debug|AnyCPU` e `Release|AnyCPU`: 0 erros, 0 warnings. `gdi_inventory_libdatasets_usage.py --fail`: `LibDataSets.cs` removido, zero refs `LibDataSets.*`. Inventário `ILookupQueryService` atualizado.

**Atenção:** Plataforma MSBuild CLI = `AnyCPU` (não `Any CPU`). Sem `.sln` na raiz — compilar pelo `.csproj`.

---
### [2026-05-20] — Scripts: máscara AAAA_MM_DD_ também em .ps1 (ferramentas Cursor)
**Tipo:** Refatoração (documentação / tooling)
**Arquivos tocados:**
- `Scripts/2026_05_20_gdi_*.ps1` (31 ficheiros PowerShell renomeados)
- `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` — §4.5: `Scripts/*.*` (ferramentas); exclusão JS de runtime
- `.cursor/context/2026_05_20_pascalcase-areas-renomeacao-lotes.md`, `CHANGELOG-DEV.md` (referências)

**O que foi feito:** Extensão da convenção a todos os scripts de repositório em `Scripts/` (`.py` já feitos; `.ps1` `gdi_*` agora com `2026_05_20_`). **Não** renomeados `bootstrap*`, `jquery.validate*`, `sessionInactivity.js`, `jsFileInputChange.js`, `gdi-session-handler.js`.

---
### [2026-05-20] — Convenção AAAA_MM_DD_ (substitui 2026-05-) em context, rules e Scripts
**Tipo:** Refatoração (documentação / tooling)
**Arquivos tocados:**
- `.cursor/context/*.md`, `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc`, `Scripts/*.py` — prefixo `2026_05_20_` (ficheiros vindos de `2026-05-`)
- `CLAUDE.md`, `.claude/commands/`, `.claude/settings.json`, `GDI-ERP-Plataform.csproj`, stub `gdi-erp-plataform.mdc`

**Problema / Demanda:** Ajustar máscara de `2026-05-` para `AAAA_MM_DD_` (ex.: `2026_05_20_`) com ano, mês e dia de criação.

**O que foi feito:** Renomeação física; referências ativas atualizadas; §4.5 da regra com formato `2026_05_20_*`. Ficheiros que tinham só `2026-05-` passaram a `2026_05_20_` (data de intervenção 20/05/2026).

**Atenção:** Entradas antigas do CHANGELOG podem citar `2026-05-*` — histórico preservado.

---
### [2026-05-20] — Convenção AAAA_MM_DD_ em context, rules e Scripts
**Tipo:** Refatoração (documentação / tooling)
**Arquivos tocados:**
- `.cursor/context/*.md` — renomeados com prefixo `2026-05-` (7 ficheiros)
- `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` — regra principal; §4.5 convenção de nomes
- `.cursor/rules/gdi-erp-plataform.mdc` — stub `alwaysApply: false` (caminho legado)
- `Scripts/*.py` — renomeados com prefixo `2026-05-` (20 ficheiros)
- `CLAUDE.md`, `.claude/commands/`, `.claude/settings.json`, `GDI-ERP-Plataform.csproj` — referências atualizadas

**Problema / Demanda:** Pedido do utilizador para mascarar ficheiros de contexto, regras Cursor e scripts Python com ano-mês de criação (`2026-05-`).

**O que foi feito:** Renomeação física + substituição de caminhos em documentação e comandos; prefixo derivado de `CreationTime` no Windows (todos `2026-05` neste ambiente). Symlink para regra antiga não foi possível (privilégios); mantido stub sem `alwaysApply`.

**Atenção:** Entradas antigas no CHANGELOG podem citar nomes sem prefixo — histórico preservado onde não foi reescrito. Novos ficheiros devem seguir §4.5 da regra.

---
### [2026-05-20] — Publish: aviso Access denied em Filtros (PackageTmp obsoleto)
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — target `GdiStripPackageTmpObsoletePathsBeforeWppCopy` (`.cursor`, `Areas\a\Views\Filtros`, `bootbox-compat`)

**Problema:** `Microsoft.Web.Publishing.targets(2693)` — *Access to the path 'Filtros' is denied* ao limpar `obj\...\PackageTmp` (pasta do modulo `a/Filtros` removido em 2026-05-19, resto de publish antigo, frequentemente só leitura).

**Solucao:** Mesmo padrao `context`/`bootbox-compat`: attrib + rmdir antes do WPP copy; workaround imediato apagar `obj\Release\Package` ou `obj` inteiro antes de publicar.

---
### [2026-05-20] — FinanceiroLancamentos Index: periodo mes corrente e filtro DataTable no load
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs` — `Field_Data_01`/`02` primeiro e ultimo dia do mes
- `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml` — `jsDatepickerFirstDayMonth` / `jsDatepickerLastDayMonth` antes do DataTable

**Problema:** Apos `@model CstModalRelatorio`, datas vazias no load; grelha sem filtro pelo periodo do mes.

**Solucao:** Padrao RelatoriosFinanceiros + Fretes/Pedidos: defaults no servidor e datepickers no `$(function)` antes de `jsCreateTableGcFinanceiroLancamentosIndex` (Ajax envia `yesCustomField09`/`10`).

---
### [2026-05-20] — LibDataSets Onda 6b: remocao da classe e slots ContextoModel
**Tipo:** Refatoração
**Arquivos tocados:**
- `Lib/LibDataSets.cs` — **removido**
- `GDI-ERP-Plataform.csproj` — sem compile LibDataSets
- `Models/ContextoModel.cs` — removidos ~50 slots `g_combo*` / `gc_combo*` / `gc_dataSet*` (shells legados)
- `Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py`, `2026_05_20_gdi_inventory_libdatasets.py`, `2026_05_20_gdi_prune_libdatasets_orphans.py`
- `.cursor/context/2026_05_20_lookups-libdatasets.md` — regenerado a partir de `ILookupQueryService`

**O que foi feito:**
Onda 6b cirurgica: ficheiro vazio pos-6a apagado; `ContextoModel` fica só navbar/sessao; lookups exclusivamente em `LookupQueryServiceCache` + `ILookupQueryService`. Guard `--fail`: 0 refs `LibDataSets.*`.

**Atenção:**
Utilizadores com `contextoModel_*` antigo em MemoryCache podem precisar novo login apos deploy (objeto serializado sem propriedades removidas — testar navbar).

---
### [2026-05-20] — LibDataSets Onda 6a: ILookupQueryService nos partials e remocao dos 34 Load*
**Tipo:** Refatoração
**Arquivos tocados:**
- `Lib/Lookups/LookupQueryService.Wave6a.cs` (novo), `ILookupQueryService.cs`, `LookupCacheKeys.cs`
- `GDI-ERP-Plataform.csproj` — compile Wave6a
- `*.Lookups.cs` (10): Movimentos, MovimentosCompras, FinanceiroLancamentos, Atendimentos, Produtos, RelatoriosFinanceiros, EstoqueControle, Clientes, ContratosAviacao, ComexProdutos — zero `LibDataSets.*`
- `Lib/LibDataSets.cs` — removidos 34 metodos orfaos (shell vazio)
- `Scripts/2026_05_20_gdi_prune_libdatasets_orphans.py` — regex corrigido

**Problema / Demanda:**
Onda 6a — migrar ultimo lote de combos/datasets para `ILookupQueryService` (MemoryCache) e eliminar fachadas `LibDataSets` nos partials.

**O que foi feito:**
Implementacao Wave6a no servico (factories usam `db` do request). Troca metodo a metodo nos `PreencherLookups*`. Poda automatica dos 34 `Load*`. Inventario `--fail`: 0 orfaos, 0 refs fora de Lookups.

**Atenção:**
`LibDataSets` permanece como classe vazia (helpers privados) — Onda 6b pode remover ficheiro e slots `ContextoModel` mortos. Validar build no VS (WebApplication targets).

---
### [2026-05-20] — LibDataSets Fase 5: deprecacao Obsolete, script --fail e ultimos Lookups
**Tipo:** Refatoração
**Arquivos tocados:**
- `Lib/LibDataSets.cs` — `[Obsolete]` em 34 metodos; removidas 9 fachadas orfas (delegacao sem refs)
- `Areas/g/Controllers/ProdutosController.Lookups.cs`, `Areas/qa/Controllers/GedSGQController.Lookups.cs` (novos)
- `ProdutosController.cs`, `GedSGQController.cs` — partial; zero `LibDataSets` fora de Lookups
- `Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py`, `2026_05_20_gdi_prune_libdatasets_orphans.py`, `2026_05_20_gdi_libdatasets_obsolete_attrs.py`
- `Models/ContextoModel.cs` — slots endereco estoque / produtos todos
- `.cursor/context/2026_05_20_lookups-libdatasets.md`, `2026_05_20_libdatasets-diagnostico-e-plano.md`, `GDI-ERP-Plataform.csproj`

**O que foi feito:**
Fase 5: deprecacao formal; guard `usage.py --fail` (0 orfaos, 0 nao migrados); ultimos controllers migrados para `*.Lookups.cs`. Build OK com CS0618 em Lookups ate troca por `ILookupQueryService`.

**Atenção:**
Warnings CS0618 sao esperados; suprimir ou migrar metodo a metodo para o servico.

---
### [2026-05-20] — LibDataSets Fase 4 P4: limpeza metodos orfaos e ContextoModel
**Tipo:** Refatoração
**Arquivos tocados:**
- `Lib/LibDataSets.cs` — removidos 24 metodos `Load*` sem referencia `LibDataSets.*` (fachadas pos-migracao P1–P3 + codigo morto)
- `Models/ContextoModel.cs` — 12 propriedades/listas mortas (combos/datasets sem `Load*` ativo)
- `Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py`, `Scripts/2026_05_20_gdi_prune_libdatasets_orphans.py` (novos)
- `.cursor/context/2026_05_20_lookups-libdatasets.md`
- `Areas/g/Controllers/AtendimentosController.cs` — comentario legado `LoadCacheComboCrmOperadores`

**Problema / Demanda:**
Fase 4 P4 — apos estrangulamento P1–P3, eliminar metodos `LibDataSets` orfaos (consumo ja em `LookupQueryServiceAccessor` / `*.Lookups.cs`).

**O que foi feito:**
Inventario automatizado (`2026_05_20_gdi_inventory_libdatasets_usage.py`): 24 orfaos. Remocao cirurgica via `2026_05_20_gdi_prune_libdatasets_orphans.py`. Slots mortos em `ContextoModel` (ex.: `gc_comboFiltroDebitoCredito`, `gc_dataSetCfopOperacoes`). Build Debug OK; 0 orfaos restantes no inventario.

**O que foi evitado:**
Remover propriedades `ContextoModel` ainda referenciadas por `Load*` ativos (ex. shells de combos em `ProdutosController`); Fase 5 `[Obsolete]`.

**Atenção:**
`ProdutosController` e cadastros `g` ainda chamam `LibDataSets` diretamente — proxima onda de migracao ou Fase 5.

---
### [2026-05-20] — LibDataSets Fase 4 P3: PreencherLookups Atendimentos, ContratosAviacao e demais gc/g
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/g/Controllers/*Controller.Lookups.cs` (novos): `Atendimentos`, `ContratosAviacao`, `Ged`, `Clientes`
- `Areas/gc/Controllers/*Controller.Lookups.cs` (novos): `EstoqueControle`, `RelatoriosFinanceiros`, `Estoque`, `ComexProdutos`, `Fretes`, `CfopOperacoes`, `CfopParametros`, `EstoqueLotes`, `MovimentosEntradas`
- Controllers correspondentes — `partial`; actions chamam `PreencherLookups*` (zero `LibDataSets` inline)
- `GDI-ERP-Plataform.csproj`, `.cursor/context/2026_05_20_lookups-libdatasets.md`

**Problema / Demanda:**
Fase 4 P3 — estrangulamento por módulo (volume médio): centralizar N× `LibDataSets` dispersos em partials, reutilizando `ILookupQueryService` onde já exposto.

**O que foi feito:**
13 controllers P3 com `*.Lookups.cs`; correção `LoadComboGAtendimentosCategorias` no Index de atendimentos; `EstoqueController.Index` mantém `PreencherLookupsEstoque()` legado (largura ecrã) + `PreencherLookupsProdutosImportados()` para combo importados. Build Debug OK.

**O que foi evitado:**
P4 (métodos órfãos); typeahead Ajax; alteração de views.

**Atenção:**
Validar: atendimentos Index/modal/Edit, contratos CreateEdit, estoque controle Create/Edit, relatório lançamentos financeiros, GED upload/edit, clientes contatos, estoque Index/transferência, COMEX produto modal, fretes Index, CFOP modais, lotes `LoadCombos`, faturar NF importação (frete responsável).

---
### [2026-05-20] — LibDataSets Fase 4 P2: PreencherLookups em FinanceiroLancamentos + EstoqueInventario
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/gc/Controllers/FinanceiroLancamentosController.Lookups.cs` (novo, partial)
- `Areas/gc/Controllers/EstoqueInventarioController.Lookups.cs` (novo, partial)
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs`, `EstoqueInventarioController.cs` — `partial`; zero `LibDataSets` inline nos actions
- `GDI-ERP-Plataform.csproj`, `.cursor/context/2026_05_20_lookups-libdatasets.md`, `CLAUDE.md`

**O que foi feito:**
P2: centralização de combos/datasets — Financeiro (`Index`, `ModalCreateEditLancamento`, `ModalGerarFinanceiroMovimentos`); Inventário (`Index`, `FormInventarioItens`, `ModalCreateEditInventarioItem`, `ModalCreateInventario`, `GetDadosInventarioItem` dataset). Serviço usado para contas, clientes com doc, locais estoque, endereço paramétrico, produtos/datasets migrados.

**Atenção:**
Validar: gestão financeira Index/modal lançamento, gerar financeiro do movimento, inventário (abrir, itens, modal item, grelha itens Ajax).

---
### [2026-05-20] — LibDataSets Fase 4 P1: PreencherLookups em Movimentos + MovimentosCompras
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.Lookups.cs` (novo, partial)
- `Areas/gc/Controllers/MovimentosComprasController.Lookups.cs` (novo, partial)
- `Areas/gc/Controllers/MovimentosController.cs`, `MovimentosComprasController.cs` — `partial`; actions chamam `PreencherLookups*`
- `GDI-ERP-Plataform.csproj`
- `.cursor/context/2026_05_20_lookups-libdatasets.md`

**O que foi feito:**
Estrangulamento P1: ~79+28 chamadas `LibDataSets` inline substituídas por metodos privados (`PreencherLookupsPedidoFormCreate/Edit`, `IndexPedido`, modais item/aprovacao/faturamento/painel/consulta, compras index/form/item). Lookups ja no `ILookupQueryService` passam por `LookupQueryServiceAccessor`; demais permanecem em `LibDataSets` dentro do partial (um ponto por tela).

**O que foi evitado:**
Typeahead Ajax clientes/produtos (avaliar P2+); alterar views; migrar P2 Financeiro/EstoqueInventario nesta entrega.

**Atenção:**
Validar manualmente: `FormPedidoCreate`, modais item, aprovacao, faturamento, `IndexPedido`, `PainelPedidos`, `IndexCompras`, `CreateCotacaoPedidoCompra`. P2: `FinanceiroLancamentosController`, `EstoqueInventarioController`.

---
### [2026-05-20] — Memória do projeto: dois tipos de tabela (DataTables vs MVC)
**Tipo:** Documentação | Decisão de arquitetura
**Arquivos tocados:**
- `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` (novo)
- `CLAUDE.md` — secção «Dois tipos de tabela no ERP»
- `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` — §2.1 e lembrança §7

**Problema / Demanda:**
Registrar permanentemente que o ERP usa **duas** famílias de tabelas; investigações e correções devem distinguir o tipo antes de alterar CSS/JS/views.

**O que foi feito:**
Documento de contexto com checklist, padrões de markup/wrapper/CSS/JS e scripts de inventário por tipo. Regra Cursor e `CLAUDE.md` referenciam o ficheiro. **Obrigatório** em intervenções com `<table>`: identificar DataTables (`display`, `GetDados*`, `GdiDt*`) vs MVC (`@for`, `gdi-form-table-*`).

**Decisões técnicas relevantes:**
- DataTables: manter `scroll-body-horizontal` + `max-content` onde aplicável.
- MVC formulário: `gdi-form-table-scroll` / `gdi-form-table-fixed`; não misturar contrato Ajax da grelha.

**Atenção para próximas intervenções:**
Consultar `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` antes de tocar em `start.css` / `start.js` / wrappers de tabela.

---
### [2026-05-20] — Tabelas MVC: scroll-body-horizontal (max-content) — inventário e correção em lote
**Tipo:** Correção | Análise
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css` — `max-content` só em `table.display` / DataTables; regras para tabelas MVC
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` — `scrollLeft = 0` em wrappers com tabela de formulário
- `Areas/gc/Views/MovimentosEntradas/FormProcessarNFCompraNacional.cshtml`, `FormProcessarNFDevolucao.cshtml`
- `Areas/gc/Views/ComexProdutos/FormProcessarProdutosPreNovos.cshtml`, `FormProcessarProdutosPreAtualizar.cshtml`
- `Scripts/2026_05_20_gdi_inventory_scroll_body_form_tables.py` (novo)
- (já na sessão anterior) `FormProcessarNFImportacao.cshtml`

**Problema / Demanda:**
Mesmo defeito de layout (primeira coluna cortada) em outras views com `scroll-body-horizontal` + tabela MVC sem classe `display`.

**O que foi feito:**
Inventário: ~60 views usam `scroll-body-horizontal` com DataTables (`table.display`) — mantidas. **4** views de formulário MVC corrigidas para `gdi-form-table-scroll` / `gdi-form-table-fixed`. CSS global: `width: max-content` deixa de aplicar a `> table` genérico; só DataTables. `jsInitForm` reposiciona scroll em formulários.

**O que foi evitado:**
Alterar dezenas de `Index.cshtml` DataTables (comportamento de scroll horizontal largo é intencional).

**Atenção:**
Incrementar `VersionERP` após publish (`start.css` + `start.js`). `Perfis/CreateEdit` tem tabelas MVC sem `scroll-body-horizontal` (width 100% inline) — fora do padrão problemático.

---
### [2026-05-20] — FormProcessarNFImportacao: tabela dentro do card (Seq./Produto visíveis)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/MovimentosEntradas/FormProcessarNFImportacao.cshtml`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css` — classes `.gdi-form-table-scroll` / `.gdi-form-table-fixed`

**Problema / Demanda:**
Na tela **Processar NF Importação**, a grelha estourava à esquerda: cabeçalho **Seq.** invisível, nomes de produto cortados (`...ERONAUTICO...`) e linhas sem texto aparente.

**O que foi feito:**
Removido `scroll-body-horizontal` (regra `width: max-content` em `start.css`). Wrapper `gdi-form-table-scroll` + tabela `table-layout: fixed` com colunas estreitas para CD; Seq./Produto/Qtd em texto (não `LabelFor`); `min-w-0` no card; `scrollLeft = 0` no init. Produto vazio exibe **—**; campos de post mantidos via `HiddenFor`.

**Decisões técnicas relevantes:**
- CSS reutilizável em `start.css` para outras tabelas de formulário MVC (não DataTables).
- Não alterado `FormProcessarProdutosPreNovos` (mesmo padrão antigo; fora do escopo).

**Atenção para próximas intervenções:**
Após publish, incrementar `VersionERP` ou hard-refresh para carregar `start.css`. Validar manualmente com muitos itens e nomes longos.

---
### [2026-05-20] — LibDataSets Fase 3: lookups só em MemoryCache + invalidação por tabela
**Tipo:** Implementação | Refatoração
**Arquivos tocados:**
- `Lib/Lookups/LookupCacheRegistry.cs`, `LookupCacheInvalidator.cs` (novos)
- `Lib/Lookups/LookupQueryServiceCache.cs`, `LookupQueryService.cs` — sem sync em ContextoModel
- `Lib/LibDB.cs` — invalidação lookup ao detectar tabela atualizada
- `Security/CachePersister.cs` — limpa lookups no logout
- `Areas/gc/Controllers/MovimentosController.cs` — dataset vendedores via serviço
- `Models/ContextoModel.cs` — nota shells legados
- `Tests/.../LookupCacheInvalidationTests.cs`, `Properties/AssemblyInfo.cs` (InternalsVisibleTo)

**O que foi feito:**
Fase 3: combos/datasets migrados deixam de duplicar listas em `contextoModel`; cache exclusivo com registo por tabela; `IsTableUpdate` remove entradas `lookup:{token}:...` associadas (ex. `g_clientes`).

**Atenção:**
`Load*` não migrados em `LibDataSets` ainda usam slots `contextoModel`. Fase 4 deve migrar ou mover para o serviço.

---
### [2026-05-20] — LibDataSets Fase 2: ILookupQueryService + cache por chave + fachada
**Tipo:** Implementação | Refatoração
**Arquivos tocados:**
- `Lib/Lookups/` — `ILookupQueryService`, `LookupQueryService`, `LookupCacheKeys`, `LookupQueryServiceCache`, `LookupQueryServiceAccessor`
- `App_Start/LookupDependencyConfig.cs`, `Global.asax.cs`
- `Lib/LibDataSets.cs` — 24 metodos delegam ao servico (top uso + parametricos)
- `Tests/GDI-ERP-Plataform.Lookups.Tests/` — testes chave/cache parametrico
- `GDI-ERP-Plataform.csproj`

**O que foi feito:**
Extraido servico testavel com cache MemoryCache `lookup:{token}:{nome}:{params}:v:{versao}`; mantidos slots `ContextoModel` para compatibilidade; controllers inalterados (`LibDataSets.Load*`).

**Validacao:**
Build Debug OK; `GDI-ERP-Plataform.Lookups.Tests.exe` passou. Manual: pedidos (contatos/destinatarios), inventario endereco, GED tipos.

---
### [2026-05-20] — LibDataSets: documento diagnostico completo e plano (Fases 0–5)
**Tipo:** Análise | Documentação
**Arquivos tocados:**
- `.cursor/context/2026_05_20_libdatasets-diagnostico-e-plano.md` (novo)
- `.cursor/context/2026_05_20_lookups-libdatasets.md` — link para o documento acima

**Problema / Demanda:**
Consolidar em ficheiro MD o diagnostico de arquitetura, riscos, boas praticas Microsoft e plano de modernizacao (ja executado parcialmente nas Fases 0/1).

**O que foi feito:**
Criado documento unico com fluxo, inicializacao, consumo por controllers, problemas, Fases 0–5 com criterios de aceite e validacao manual; referencia cruzada ao inventario metodo a metodo.

---
### [2026-05-20] — LibDataSets Fase 0/1: inventario lookups + cache parametrico
**Tipo:** Análise | Correção
**Arquivos tocados:**
- `.cursor/context/2026_05_20_lookups-libdatasets.md` (novo)
- `Scripts/2026_05_20_gdi_inventory_libdatasets.py` (novo)
- `Lib/LibDataSets.cs` — 9 metodos parametricos, `EnsureContextoModel`, `CloneSelectList`

**Problema:**
Combos/datasets com parametros (IdCliente, IdLocalEstoque, IdTipo) reutilizavam uma unica propriedade em `contextoModel`, devolvendo dados do ultimo filtro.

**Solucao:**
Fase 0: inventario 67 metodos. Fase 1: consulta sempre alinhada ao parametro; sem gravar em cache global nos metodos listados; copia defensiva sem JSON clone nesses fluxos.

**Atenção:**
Combos globais (clientes, produtos) mantem cache legado; Fase 2+ prevê chaves compostas ou servico de lookup.

---
### [2026-05-20] — Encoding: conversão em lote UTF-8 sem BOM (146 ficheiros)
**Tipo:** Implementação / Padronização
**Arquivos tocados:**
- `Scripts/2026_05_20_gdi_utf8_no_bom_areas.py --project` (146 ficheiros regravados)
- Principalmente `Areas/**/*.cshtml` (139), `Web.config`, `Web.Release.config`, `GDI-ERP-Plataform.csproj`, `ControlVersion.cs`, `Lib/LibIcons.cs`, `Models/UserIdentity.cs`, `LibUI_.../start.js`, `Views/Error/*.cshtml`

**Problema / Demanda:**
Auditoria indicou 146 ficheiros ainda com UTF-8 BOM (14,7% de 990 ficheiros de texto).

**O que foi feito:**
Executado `python Scripts/2026_05_20_gdi_utf8_no_bom_areas.py --project`. Removido BOM (`utf-8-sig` → UTF-8 sem BOM). Re-scan pós-conversão: 990 ficheiros de texto, 100% `utf-8-no-bom`; zero BOM/UTF-16/CP1252 nas extensões analisadas.

**Atenção para próximas intervenções:**
Novos `.cshtml`/`.cs` salvos pelo VS com BOM — repetir scan ou configurar editor para UTF-8 sem BOM.

---
### [2026-05-20] — Auditoria filtro inline Index: gc/FinanceiroParametroDifal (id > 1 na base)
**Tipo:** Correção | Análise
**Arquivos tocados:**
- `Areas/gc/Controllers/FinanceiroParametroDifalController.cs`

**Problema / Demanda:**
Varredura nas demais views com filtro inline (padrão Clientes/Perfis).

**Causa raiz (único outro caso igual a Perfis):**
`id_parametro_difal > 1` na base + filtro `id_parametro_difal = @id` impedia retorno do registro id 1.

**O que foi feito:**
Mesmo padrão Perfis: sem `> 1` quando há Id. explícito; `TryParseIdParametroDifalFiltro`; persistência de sigla normalizada. Demais Index com `AplicarFiltro*` usam `id > 0` — OK. Lookups com valor `0` ignorados no servidor (Atendimentos, ComexFinanceiro) ou corrigidos (Clientes).

**Atenção:**
`g/Financeiro/Index` exige quatro campos preenchidos (legado) — fora do padrão inline Id./Nome; não alterado.

---
### [2026-05-20] — g/Perfis Index: filtro por Id. sem resultado (base `id_perfil > 1` vs id explícito)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/g/Controllers/PerfisController.cs` — `GetDados`, `AplicarFiltroPerfisNaQuery`, `TryParseIdPerfilFiltro`

**Problema / Demanda:**
Mesma situação de Clientes: Id. válido + Pesquisar não retornava linhas.

**Causa raiz:**
`baseQuery` fixava `id_perfil > 1` antes do filtro. Id. 1 (admin/sistema) ou negativos (ex. -800) com `c.id_perfil = @id` geravam interseção vazia.

**O que foi feito:**
Com Id. numérico explícito (`TryParseIdPerfilFiltro`), a query não aplica `> 1`; lista geral e Limpar mantêm ocultar id <= 1. Filtro id via helper único; persistência do nome normalizada (`NormalizarTermoBuscaTexto`); `hasInline` com `IsNullOrWhiteSpace`.

**Decisões técnicas relevantes:**
- Id. = igualdade; Nome = LIKE %termo% (inalterado); sem lookup na tela.

**O que foi evitado e por quê:**
- Alteração na view — envio Ajax já correto.

**Impactos conhecidos:**
- Pesquisar Id. 1 ou perfil negativo passa a exibir o registro; Limpar continua listando só `id_perfil > 1`.

**Atenção para próximas intervenções:**
- Replicar padrão “restrição de listagem vs busca por PK” em outros cadastros com `id > 1` na base.

---
### [2026-05-20] — g/Clientes Index: filtro por Id. sem resultado (lookup "0" gerava LIKE nome)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/g/Controllers/ClientesController.cs` — `AplicarFiltroClientesInline`

**Problema / Demanda:**
Ao informar Id. válido e Pesquisar, a grade não retornava registros.

**Causa raiz:**
Com o combo Cliente em `[ SELECIONE ]` (`yesCustomField02` = `"0"`), o servidor entrava no ramo `else` do lookup e aplicava `c.nome LIKE '%0%'` em **AND** com `c.id_cliente = @idCliente`, excluindo a maioria dos clientes.

**O que foi feito:**
Lookup só aplica igualdade quando valor numérico `> 0` e diferente de `"0"`; removido LIKE em `nome` a partir do valor do combo. Id. exige `> 0`. CPF/CNPJ e razão legada (LIKE) inalterados no contrato.

**Decisões técnicas relevantes:**
- Combo lookup = id exato (não texto livre); strings LIKE apenas em `razaoLegado` persistido.

**O que foi evitado e por quê:**
- Alteração na view/JS — critérios já enviados corretamente; bug só no SQL montado no servidor.

**Impactos conhecidos:**
- Pesquisa só por Id. + lookup vazio volta a funcionar; múltiplos critérios continuam em AND.

**Atenção para próximas intervenções:**
- Perfis não-Admin ainda restringem `id_coligada`/`id_filial` (e vendedor em -800); cliente fora do escopo do perfil pode não aparecer mesmo com Id. correto.

---

## CONTEXTO GERAL DO PROJETO

**Descrição:** ERP customizado da GDI Importação e Comércio de Peças Aeronáuticas Ltda. (GDI Aviação), desenvolvido para gerenciar os processos de importação, comercialização e distribuição de peças e componentes aeronáuticos.

**Unidades atendidas:**
- Belo Horizonte, MG (matriz)
- São Paulo, SP (filial)

**Módulos principais do sistema:**
- COMEX — Gestão de Importações
- Comercial — Cotações e Pedidos de Venda
- Estoque — Cadastro, Recebimento e Separação
- Expedição — Separação, Embalagem e Rastreamento
- Financeiro — Monitoramento de Pedidos e Satisfação de Clientes
- Qualidade — Não Conformidades e Registros do SGQ

**Padrões estabelecidos no projeto:**
- (preencher conforme forem sendo definidos ao longo do desenvolvimento)

**Arquivos críticos / sensíveis identificados:**
- (preencher conforme forem sendo mapeados)

**Decisões de arquitetura já tomadas:**
- (preencher conforme forem sendo registradas)

**Armadilhas conhecidas:**
- Publish Web (VS/MSBuild): pasta obsoleta `obj\...\PackageTmp\...\plugins\bootbox-compat` herdada de build antigo pode ficar **somente leitura** (`d-r---`) e gerar *Warning: Access to the path 'bootbox-compat' is denied* em `Microsoft.Web.Publishing.targets`. **Solução:** apagar `obj` (ou só `obj\Release\Package`) antes de publicar; o projeto fonte não inclui mais `bootbox-compat` (usa `sweetalert2\`).
- Mesmo ficheiro de targets: *Access to the path 'context' is denied* ao limpar `PackageTmp` — restos de `.cursor\context` ou exclusão incompleta de `.cursor`. O `.csproj` define `ExcludeFoldersFromDeployment` + `ExcludeFromPackageFolders` para `.cursor`; se o aviso persistir, apagar `obj\...\Package\PackageTmp` antes de publicar.

---

## HISTÓRICO DE INTERVENÇÕES

---
### [2026-05-19] — Lote 1: remoção hiddens legados yesFilterOperador/Text/AdvancedText
**Tipo:** Refatoração
**Arquivos tocados:**
- 40 views `Areas/**` (g, gc, qa, crm) — removidos `<input hidden>` e chaves ajax DataTables
- `Scripts/2026_05_20_gdi_remove_legacy_filter_hiddens_lote1.py` (script reutilizável)

**Problema / Demanda:** Campos do modal genérico `a/Filtros` (já removido) permaneciam nas views sem JS que os preenchesse.

**O que foi feito:** Remoção mecânica de `yesFilterOperador`, `yesFilterText` e `yesFilterAdvancedText` no markup e no `data` do DataTables. **Mantido** `yesFilterField` (sentinela `""` / `"*"` para Pesquisar/Limpar e `LibDB.getFilterByUser`).

**O que foi evitado:** Alterar `jQueryDataTableParamModel`, `LibDB.getFilterByUser` e controllers (fase 2).

**Atenção para próximas intervenções:** Propriedades no model e ramos servidor de filtro genérico SQL permanecem; limpeza opcional na fase 2.

---
### [2026-05-19] — Remoção módulo legado `a/Filtros` (modal genérico)
**Tipo:** Refatoração
**Arquivos tocados:**
- Removidos: `Areas/a/Controllers/FiltrosController.cs`, `Areas/a/Models/CstFiltroModel.cs`, `Areas/a/Views/Filtros/ModalFiltroGenericoView.cshtml`
- `GDI-ERP-Plataform.csproj` — entradas Compile/Content correspondentes

**Problema / Demanda:** Modal de filtro genérico na área `a` sem consumidores após descontinuação de `g/ProdutosTipos` no projeto (fora do `.csproj`).

**O que foi feito:** Remoção completa do controller, model e view; área `a` mantém `Parametros` e `Audit`. Filtros persistidos em sessão (`LibDB.getFilterByUser`, `UserIdentity.allFiltros`) **preservados** — usados pelos índices com filtro inline.

**O que foi evitado:** Remover `LibDB` / `allFiltros`; remover área `a` inteira.

**Atenção para próximas intervenções:** Ficheiros órfãos `Areas/g/Views/ProdutosTipos/*` e `ProdutosTiposController.cs` podem ainda existir no disco sem estar no `.csproj` — limpeza opcional separada.

---
### [2026-05-19] — jsAtualizarIndicadorFiltro*: wrappers para GdiAtualizarIndicadorFiltro (start.js)
**Tipo:** Refatoração
**Arquivos tocados:**
- 17 índices `Areas/g` e `Areas/gc` (Cidades, Clientes, Produtos, Cfop*, Usuarios, Vendedores, etc.)
- `Scripts/2026_05_20_gdi_refactor_indicador_filtro.py`

**O que foi feito:** Corpos duplicados das funções `jsAtualizarIndicadorFiltro{Modulo}` passam a delegar em `GdiAtualizarIndicadorFiltro` (mantidos nomes locais nos `xhr.dt`).

---
### [2026-05-19] — Migração btnFiltro lote 2: removido de start.js + 4 views finais
**Tipo:** Refatoração
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` — `GdiAtualizarIndicadorFiltro`; removido `btnFiltro`
- `Areas/g/Views/ProdutosTipos/Index.cshtml` — Filtro modal + Limpar; `GdiAtualizarIndicadorFiltro` no `xhr.dt`
- `Areas/g/Views/Nfe/Index.cshtml`, `Areas/gc/Views/ComexProdutos/Index.cshtml`, `ProdutosPre.cshtml` — `xhr.dt` sem `btnFiltro`

**Decisões:** ProdutosTipos deixa de alternar “Filtro/Remover” via `innerHTML`; botões separados. **Publish:** incrementar `VersionERP` (alteração em `start.js`).

---
### [2026-05-19] — Migração btnFiltro lote 1 (views sem #btnFiltroDefault)
**Tipo:** Refatoração
**Arquivos tocados:**
- ~21 views em `Areas/g`, `Areas/gc`, `Areas/qa` (DataTables `xhr.dt`)
- `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml` (ordem notify → `jsUpdateDataView`)
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (comentário `@deprecated` em `btnFiltro`)
- `Scripts/2026_05_20_gdi_remove_btnfiltro_lote1.py` (verificação)

**O que foi feito:** Removido `btnFiltro(json.yesFilterOnOff)` do `xhr.dt`; padrão `if (GdiDtNotifyJsonErrorMessage(json)) { return; }`. Spinner via `gdiDataTablesProcessandoAutoHide`. **Pendente lote 2:** `g/Nfe`, `g/ProdutosTipos`, `gc/ComexProdutos/Index`, `gc/ComexProdutos/ProdutosPre`.

---
### [2026-05-19] — qa/GedSGQ IndexDocsSGQ: botão Pesquisar sem função JS
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/qa/Views/GedSGQ/IndexDocsSGQ.cshtml`

**Problema:** `onclick="jsAjaxPesquisarGedArquivos()"` sem definição na view (existe só em `g/Ged/Index.cshtml`) → ReferenceError e grelha não atualiza.

**Solução:** Copiado `jsAjaxPesquisarGedArquivos` do padrão GED; download na coluna usa `row[1]` em vez de `data[1]`.

**Atenção:** `IndexComunicados`, `IndexAtasReunioes` e `IndexPops` têm o mesmo gap — corrigir se reportarem.

---
### [2026-05-19] — gc/Estoque Index: Ficha de estoque in-line por linha
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Estoque/Index.cshtml`

**Problema:** Botão toolbar "Produtos - Ficha Estoque" exigia seleção na grelha.

**Solução:** Removido botão do `card-header`; coluna **Ficha** (última) com botão por linha (`jsGcEstoqueFichaEstoque`), padrão `g/Produtos/Index` (sem exigir seleção na grelha).

---
### [2026-05-19] — g/Produtos Index: colunas Ficha e Editar invertidas
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/g/Views/Produtos/Index.cshtml`

**Problema:** Coluna Editar na penúltima posição e Ficha na última; pedido para inverter.

**Solução:** `<th>` e `aoColumnDefs` reordenados — Ficha (índice 8) antes de Editar (índice 9 quando ambas visíveis).

---
### [2026-05-19] — g/Filiais CreateEdit: layout tab-pane abaixo do título
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/g/Views/Filiais/CreateEdit.cshtml`

**Problema:** `form-horizontal` envolvia `card-title`, `card-header` e `card-body`, colocando abas/conteúdo na mesma linha do título.

**Solução:** Estrutura alinhada a `PagRecTipos`/`UF`: filhos diretos do `card` (`card-title` → `card-header` → `card-body` → `card-footer`); `tab-content` só dentro de `card-body`.

---
### [2026-05-19] — g/Filiais CreateEdit: título duplicado, Create POST e Edit seguro
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/g/Controllers/FiliaisController.cs`
- `Areas/g/Views/Filiais/CreateEdit.cshtml`

**Problema:** Título Edit repetia o nome (`GDI BH - GDI BH`); tag `<b>Filial</b` sem fechar; Create POST forçava `id_filial=1` e `id_coligada=1`; Edit com `EntityState.Modified` zerava campos fora do formulário.

**Solução:** `MontarTituloCreateEditFilial` com `id_filial - nome`; removido hardcode no Create; Edit atualiza entidade carregada do banco; validação JS de Coligada no save.

---
### [2026-05-19] — g/Filiais Index: Editar in-line por linha (padrão Produtos/Perfis)
**Tipo:** Implementação (UX)
**Arquivos tocados:**
- `Areas/g/Views/Filiais/Index.cshtml`

**O que foi feito:** Removido botão toolbar «Editar» (`JsEditRecord` + seleção); coluna ação com `btnGFiliaisEditRow` + `jsGFiliaisEditRow` → `JsEditRecordDoubleClick`; `dt-no-row-select`; duplo clique na linha; roles `g_Filiais_*` / `Actionupdate`; `drawCallback` tooltips; `gdi-dt-zebra`. Hide do modal processando via listener global `start.js` (`xhr.dt`/`draw.dt`).

---
### [2026-05-19] — DataTables global: LibMessageProcessandoHide após xhr/draw/error.dt
**Tipo:** Correção
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` — `gdiDataTablesProcessandoAutoHide()` em `$(document).on('xhr.dt error.dt draw.dt', …)`

**Problema:** Dezenas de views chamam `LibMessageProcessando` antes de `DataTable().draw()` sem `LibMessageProcessandoHide()` ao concluir o Ajax/redraw (ex.: índices modernizados Perfis, CFOP, Clientes, Financeiro).

**Solução:** Listener global DataTables (todas as grelhas do ERP que disparam eventos dt) fecha o overlay ao terminar xhr, erro ou draw. `waitingDialog.hide` usa contador de profundidade — múltiplos hide são seguros.

**Atenção publish:** incrementar `VersionERP` / cache-bust de `start.js` no `_Layout` após deploy.

---
### [2026-05-19] — Padronização filtro LIKE %termo% normalizado em GetDados (LibStringFormat + controllers)
**Tipo:** Correção / Refatoração
**Arquivos tocados:**
- `Lib/LibStringFormat.cs` — `NormalizarTermoBuscaTexto`, `NormalizarTermoBuscaCodigo`, `MontarPadraoLikeContem`, `TryMontarPadraoLikeContemTexto/Codigo`
- `Areas/g/Controllers`: Cidades, Perfis, Usuarios, ContasCaixas, Vendedores, PagRecTipos, PagRecCondicoes, ContratosAviacao, UF, ProdutosNcm, Produtos, Clientes, Nfe
- `Areas/gc/Controllers`: Cfop, CfopOperacoes, CfopParametros, FinanceiroParametroDifal, EstoqueControle, EstoqueLotes, FinanceiroLancamentos (2 trechos), ComexProdutos

**Problema:** Vários `GetDados` com filtro inline usavam `Contains` sem normalização alinhada ao cadastro; comportamento inconsistente (ex.: BELO não encontrava BELO HORIZONTE).

**Solução:** Helpers centralizados; `DbFunctions.Like` com `%termo%` e escape de curingas; texto via `FormatarTextoSimples`, códigos/PN/serial via maiúsculas sem acento.

**Fora do escopo:** filtros só por id, datas, SQL genérico `g_filtros`, listas `Contains(id)` em coleções.

---
### [2026-05-19] — g/Cidades GetDados: filtro nome LIKE %termo% e inclusão id_cidade 1
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/g/Controllers/CidadesController.cs`

**Problema:** Pesquisa por «BELO» não retornava «BELO HORIZONTE»; base usava `id_cidade > 1` (excluía id 1) e filtro de nome sem normalização explícita tipo `LIKE '%…%'`.

**Solução:** `id_cidade > 0`; termo normalizado com `FormatarTextoSimples`; filtro `DbFunctions.Like` com padrão `%termo%` e escape de curingas SQL.

---
### [2026-05-19] — g/Cidades Index: modal «Pesquisando cidades…» não fechava após pesquisa
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/g/Views/Cidades/Index.cshtml`

**Problema:** `jsAjaxPesquisarCidades` / `jsLimparFiltroCidades` chamavam `LibMessageProcessando` antes de `DataTable().draw(false)`, mas não havia `LibMessageProcessandoHide()` ao concluir o redraw (padrão já usado em `Produtos/Index` via `footerCallback`).

**Solução:** `LibMessageProcessandoHide()` no `drawCallback`, no `xhr.dt` (após resposta Ajax) e nos `catch` de pesquisa/limpar.

---
### [2026-05-19] — Remoção módulo g/PortalVendedor (controller + view PortalFinanceiro)
**Tipo:** Refatoração
**Arquivos tocados:**
- Removidos: `Areas/g/Controllers/PortalVendedorController.cs`, `Areas/g/Views/PortalVendedor/PortalFinanceiro.cshtml`
- `GDI-ERP-Plataform.csproj` — retirados Compile/Content

**Preservado:** roles `g_PortalVendedor_*` em `UsuariosController` (`ModalUsuarioTrocarSenha` / `AjaxUsuarioTrocarSenha` — logons vendedor `TokenAcesso` "V"); portal externo `crm` + `UserIdentity`; `NfeController`.

**Evitado:** remover roles de `UsuariosController` — não são rota do portal, apenas autorização de troca de senha.

**Atenção:** desativar no menu/BD eventual item `/g/PortalVendedor/PortalFinanceiro` em produção.

---
### [2026-05-19] — Remoção legado g/PortalCliente (view PortalFinanceiro + models)
**Tipo:** Refatoração
**Arquivos tocados:**
- Removidos: `Areas/g/Views/PortalCliente/PortalFinanceiro.cshtml`, `Areas/g/Models/CstPortalClienteFinanceiro.cs`, `Areas/g/Models/CstViewPortalClienteFinanceiro.cs`
- `GDI-ERP-Plataform.csproj` — retirados Compile/Content

**Nota:** `Areas/g/Controllers/PortalClienteController.cs` **não existia** no repositório (já ausente ou nunca commitado).

**Preservado (portal externo integrado):** `UserIdentity` (`AcessoPortal`, `CompletePortalClienteLogin` → `crm/Pedidos`), `Areas/crm/*`, role `gc_PortalCliente_PortalFinanceiro`, `Areas/g/Views/PortalVendedor` (vendedor interno).

**Atenção:** Não confundir com portal público em **crm**; apenas o ecrã legado **g/PortalCliente/PortalFinanceiro** foi removido.

---
### [2026-05-19] — Remoção módulo g/Requisicoes (controller, views, referências Clientes e navbar)
**Tipo:** Refatoração
**Arquivos tocados:**
- Removidos: `Areas/g/Controllers/RequisicoesController.cs`, `Areas/g/Views/Requisicoes/*` (Index, CreateEdit, ModalSolicitarBloqueioLogon)
- `GDI-ERP-Plataform.csproj` — retirados Compile/Content
- `Areas/g/Views/Clientes/Index.cshtml` — removida função `jsModalSolicitarBloqueioLogon` (modal em Requisicoes)
- `Security/Contexto.cs` — `getNavbarItemsAlert` sem links para `/g/Requisicoes/Edit`

**Preservado:** tabelas `g_requisicoes` / `g_requisicoes_tipos` no EF; `GetDadosRequisicoesPedidos` em **gc/Tarefas** (não é o módulo g).

**Atenção:** Remover menu `/g/Requisicoes` no SQL/IIS; alertas de requisições abertas deixam de aparecer na navbar.

---
### [2026-05-19] — Remoção módulo g/FinanceiroLancamentos (controller, views, CstFinanceiroLancamentosIndex)
**Tipo:** Refatoração
**Arquivos tocados:**
- Removidos: `Areas/g/Controllers/FinanceiroLancamentosController.cs`, `Areas/g/Models/CstFinanceiroLancamentosIndex.cs`, `Areas/g/Views/FinanceiroLancamentos/*` (6 views)
- `GDI-ERP-Plataform.csproj` — retirados Compile/Content do módulo **g** (área **gc** `FinanceiroLancamentos` intacta)

**Preservado:** `Areas/g/Models/CstFinanceiroLancamentos.cs` — usado por `FinanceiroController.AjaxSaveFinanceiroAvulso`.

**Atenção:** Remover menu `/g/FinanceiroLancamentos` no SQL/IIS; lançamentos **gc** (`/gc/FinanceiroLancamentos`) não foram alterados.

---
### [2026-05-19] — Remoção módulo g/FinanceiroFaturamentos (controller, views, model)
**Tipo:** Refatoração
**Arquivos tocados:**
- Removidos: `Areas/g/Controllers/FinanceiroFaturamentosController.cs`, `Areas/g/Models/CstFinanceiroFaturamentosEnviarNFe.cs`, `Areas/g/Views/FinanceiroFaturamentos/*` (5 views)
- `GDI-ERP-Plataform.csproj` — retirados Compile/Content do módulo
- `Areas/g/Views/Nfe/Index.cshtml` — dropdown Processos sem roles `g_FinanceiroFaturamentos_*`

**Problema / Demanda:** Descontinuar tela MVC `g/FinanceiroFaturamentos` mantendo tabela `g_financeiro_faturamentos` para lançamentos.

**O que foi feito:** Apagados controller, views e model; limpeza do `.csproj`; Nfe/Index com autorização só por roles `g_Nfe_*`.

**Atenção:** Remover entrada de menu `/g/FinanceiroFaturamentos` no SQL/IIS se existir; envio NF-e em lote por faturamento deixa de existir na UI.

---
### [2026-05-19] — gc/EstoqueControle: Index modernizado (filtro inline, GetDados SQL)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Models/CstEstoqueControleIndex.cs` (novo)
- `Areas/gc/Controllers/EstoqueControleController.cs`
- `Areas/gc/Views/EstoqueControle/Index.cshtml`
- `GDI-ERP-Plataform.csproj`

**Problema / Demanda:**
Lista de controles/aferições carregava todos os registros em memória (`ToList()` em `g_produtos_controle`, `g_produtos`, `g_produtos_status`), sem filtro inline nem grade vazia ao abrir; botão Editar na toolbar com handler incorreto na coluna edit.

**O que foi feito:**
- Model `CstEstoqueControleIndex` (Id + Serial); `Index()` restaura filtro `g_filtros` (`gc_EstoqueControle`, formato `id;serial`).
- `GetDados`: `AsNoTracking`, filtro vazio → grade vazia; Pesquisar/Limpar (`yesFilterField=*`); paginação `Skip`/`Take` no SQL (máx. 100); lookups batch de produto/status só da página.
- View: filtro inline, `deferLoading`, indicador no Limpar, coluna Editar in-line (`jsGcEstoqueControleEditRow`), toolbar só Novo; duplo clique em `td:not(.dt-no-row-select)`.

**Decisões técnicas relevantes:**
- Critérios de ativos mantidos: `id_produto > 0 && ativo == true`.
- Ordenação server-side nas colunas Id. (1) e Serial (2).

**O que foi evitado e por quê:**
- `CreateEdit.cshtml` / `GetDadosMedicoes` — fora do escopo da listagem Index.

**Impactos conhecidos:**
- Comportamento alinhado a Cfop/Cidades; persistência de filtro por utilizador.

**Atenção para próximas intervenções:**
- Validar pesquisa por serial parcial e Limpar com volume grande em produção; compilar no VS antes do publish.

---
### [2026-05-19] — Index modernizado (lote cadastros g/gc): UF, PagRecTipos/Condicoes, ContasCaixas, Vendedores, ProdutosNcm, ContratosAviacao, Cfop
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/UFController.cs`, `PagRecTiposController.cs`, `PagRecCondicoesController.cs`, `ContasCaixasController.cs`, `VendedoresController.cs`, `ProdutosNcmController.cs`, `ContratosAviacaoController.cs`
- `Areas/gc/Controllers/CfopController.cs`
- Views Index correspondentes (filtro inline + `yesCustomField01/02` + `deferLoading`)

**O que foi feito:** Padrão Cidades/Perfis em `Index`/`GetDados`: `IQueryable` + paginação, filtro `id;campo2` persistido, grid vazia sem critério, `*` para listar todos. `controllerName` onde faltava. Vendedores/ContratosAviacao: lookup revendas/clientes/tipos só da página.

---

### [2026-05-19] — Index modernizado: Usuarios, Cidades, CfopOperacoes, CfopParametros, FinanceiroParametroDifal
**Tipo:** Implementação (UX)
**Arquivos tocados:**
- `Areas/g/Views/Usuarios/Index.cshtml`, `Areas/g/Controllers/UsuariosController.cs`, `Areas/g/Models/CstUsuariosIndex.cs`
- `Areas/g/Views/Cidades/Index.cshtml`, `Areas/g/Controllers/CidadesController.cs`, `Areas/g/Models/CstCidadesIndex.cs`
- `Areas/g/Views/Cidades/ModalFiltroAvancadoView.cshtml` (removido)
- `Areas/gc/Views/CfopOperacoes/Index.cshtml`, `Areas/gc/Controllers/CfopOperacoesController.cs`, `Areas/gc/Models/CstCfopOperacoesIndex.cs`
- `Areas/gc/Views/CfopParametros/Index.cshtml`, `Areas/gc/Controllers/CfopParametrosController.cs`, `Areas/gc/Models/CstCfopParametrosIndex.cs`
- `Areas/gc/Views/FinanceiroParametroDifal/Index.cshtml`, `Areas/gc/Controllers/FinanceiroParametroDifalController.cs`, `Areas/gc/Models/CstFinanceiroParametroDifalIndex.cs`
- `GDI-ERP-Plataform.csproj`

**O que foi feito:**
Padrão Perfis/Clientes: filtro inline (Id + texto), Pesquisar/Limpar, `deferLoading`, gate sem critério, persistência `id;texto` em `g_filtros`, `GetDados` com `IQueryable` + paginação SQL, coluna Editar in-line (modal nos CFOP), toolbar Editar removida; Cidades sem modal de filtro avançado.

---
### [2026-05-19] — Perfis Index: filtro inline Id/Nome; GetDados paginação SQL; Editar in-line
**Tipo:** Implementação (UX)
**Arquivos tocados:**
- `Areas/g/Views/Perfis/Index.cshtml`
- `Areas/g/Controllers/PerfisController.cs` — `Index`, `GetDados`, helpers
- `Areas/g/Models/CstPerfisIndex.cs` (novo)
- `GDI-ERP-Plataform.csproj`

**O que foi feito:**
Filtro inline Id + Nome; Pesquisar/Limpar; `deferLoading` e gate sem critério; persistência `id;nome` em `g_filtros` (`g_Perfis`); `GetDados` com `IQueryable` + `Skip`/`Take` no SQL (sem `ToList` da tabela inteira); coluna Editar in-line; botão Editar da toolbar removido; correção HTML (tags inválidas `<motion>` substituídas por `<div>`).

**Atenção para próximas intervenções:**
Validar no browser: abrir Index vazio → Pesquisar → Limpar → editar na linha / duplo clique.

---
### [2026-05-19] — Clientes Index: coluna Editar in-line; removido botão Editar da toolbar
**Tipo:** Implementação (UX)
**Arquivos tocados:**
- `Areas/g/Views/Clientes/Index.cshtml`

**O que foi feito:**
Coluna 7 com botão `jsGClientesEditRow` (`dt-no-row-select`, roles `g_Clientes_*` / `Actionupdate`); toolbar Editar removida; duplo clique delega à mesma função; tooltips no `drawCallback`.

---
### [2026-05-19] — Clientes Index: filtro inline (Id, lookup Nome, CPF, CNPJ) padrão Produtos
**Tipo:** Implementação (UX)
**Arquivos tocados:**
- `Areas/g/Views/Clientes/Index.cshtml`
- `Areas/g/Controllers/ClientesController.cs` — `Index`, `GetDados`, helpers
- `Areas/g/Models/cstClientesIndex.cs` (novo)
- `Areas/g/Views/Clientes/ModalFiltroAvancadoView.cshtml` (removido)
- `GDI-ERP-Plataform.csproj`

**O que foi feito:**
Painel inline Id + `DropDownList` cliente (Financeiro), CPF, CNPJ; Pesquisar/Limpar; `deferLoading`; `yesCustomField01–04`; persistência `id;idLookup;;cpf;cnpj`; razão social só servidor; Filtro Avançado removido.

---
### [2026-05-19] — Modal Ficha Estoque: alinhamento do título em duas linhas
**Tipo:** Correção (UX)
**Arquivos tocados:**
- `Areas/g/Views/Produtos/ModalViewFichaEstoqueProduto.cshtml`
- `Areas/g/Controllers/ProdutosController.cs` — `ModalViewFichaEstoqueProduto`

**Problema / Demanda:**
Título com `<br/>` desalinhado (escada): `d-flex` no `h5` quebrava quebra de linha; `GetTabHtml` gerava indentação extra.

**O que foi feito:**
Ícone em `ViewBag.TitleIcon`; linhas em `TitleLinha1`/`TitleLinha2`; layout ícone + texto com `align-items-start`, `text-break`, `flex-grow-1` no `modal-title`.

---
### [2026-05-19] — Produtos Index: coluna in-line Ficha de Estoque (padrão IndexPedido)
**Tipo:** Implementação (UX)
**Arquivos tocados:**
- `Areas/g/Views/Produtos/Index.cshtml`

**Problema / Demanda:**
Ficha de estoque exigia seleção de linha + menu Relatórios; padrão desejado: botão por linha como `gc/Movimentos/IndexPedido`.

**O que foi feito:**
Coluna 8 com botão `warehouse` (`dt-no-row-select`), `jsGProdutosFichaEstoque(id)` carrega `ModalViewFichaEstoqueProduto`; menu Relatórios mantido e delega à mesma função; roles `g_Produtos_*` / `g_Produtos_Actionread`; tooltips no `drawCallback`; CSS `gdi-index-produtos-dt`. Sem alteração em controller/modal.

**Decisões técnicas relevantes:**
- Coluna só renderizada no cliente quando há role de leitura (Razor `podeVerFichaEstoque`).
- `GetDados` inalterado — coluna de ação é `data: null` no DataTables.

**O que foi evitado e por quê:**
- Remoção do item em Relatórios → compatibilidade com fluxo anterior.
- Alteração em `GetFichaEstoqueProduto` (ToList em memória) → fora do escopo UX.

**Impactos conhecidos:**
- Largura coluna Produto 59% → 56% quando coluna Ficha visível.

**Atenção para próximas intervenções:**
- Otimizar paginação SQL em `GetFichaEstoqueProduto` em tarefa separada.

---
### [2026-05-19] — Produtos Index fase 5: indicador visual de filtro ativo (Limpar)
**Tipo:** Implementação (UX)
**Arquivos tocados:**
- `Areas/g/Views/Produtos/Index.cshtml`

**O que foi feito:**
`jsAtualizarIndicadorFiltroProdutos` no `xhr.dt` conforme `yesFilterOnOff` do servidor — botão Limpar passa a `btn-outline-warning` e título “Filtro ativo…” sem usar `btnFiltro` global. Encerra o lote de melhorias do índice de Produtos (fases 1–5).

---
### [2026-05-19] — Produtos Index fase 4: remoção modal filtro avançado legado
**Tipo:** Refatoração / Limpeza
**Arquivos tocados:**
- `Areas/g/Controllers/ProdutosController.cs` — removido `ModalFiltroAvancadoView`; `GetDados` sem ramo `yesFilterAdvancedText`
- `Areas/g/Views/Produtos/Index.cshtml` — hiddens/ajax legados removidos
- `Areas/g/Views/Produtos/ModalFiltroAvancadoView.cshtml` — apagado
- `GDI-ERP-Plataform.csproj`

**O que foi feito:**
Limpeza pós-migração para filtro inline; filtro persistido continua via `g_filtros` / parse 5 campos (aux/descrição legados na sessão). Build Release OK.

---
### [2026-05-19] — Produtos Index fase 3: persistência e restauração do filtro inline
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/ProdutosController.cs`
- `Areas/g/Views/Produtos/Index.cshtml`

**Problema / Demanda:**
`yesFilterField='*'` em Pesquisar acionava `getFilterByUser` e **apagava** o filtro gravado; `filterDb` nunca era aplicado à query.

**O que foi feito:**
Pesquisar deixa de usar `*` (só **Limpar** usa `*` para limpar cache e listar todos). `setFilterByUser` grava critério `id;pn;;nome;` após pesquisa; `GetDados` reutiliza filtro da sessão se os campos vierem vazios; `Index` pré-preenche campos e dispara pesquisa se houver filtro salvo. Helpers `ObterFiltroPersistidoUsuario`, `TryParseFiltroProdutosSemicolon`, `AplicarFiltroProdutosNaQuery`. Compatível com filtro legado 5 campos (aux/descrição). Build Release OK.

---
### [2026-05-19] — Produtos Index fase 2: sem carga automática da grade no 1.º load
**Tipo:** Correção / Performance
**Arquivos tocados:**
- `Areas/g/Views/Produtos/Index.cshtml`
- `Areas/g/Controllers/ProdutosController.cs`

**Problema / Demanda:**
Após filtro inline, o DataTables ainda disparava `GetDados` ao abrir a página e carregava todos os produtos ativos.

**O que foi feito:**
`deferLoading: true` + mensagem em `sEmptyTable`; **Limpar** define `yesFilterField='*'` para listar todos. `GetDados`: retorno vazio se não houver critério inline/avançado nem `yesFilterField='*'`; teto `iDisplayLength` 100. Build Release OK.

**Atenção:** Pesquisar exige ao menos um campo; Limpar lista todos os ativos paginados.

---
### [2026-05-19] — Produtos Index: filtro inline Id/PN/Nome (padrão IndexPedido)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Views/Produtos/Index.cshtml`
- `Areas/g/Controllers/ProdutosController.cs`

**Problema / Demanda:**
Filtro só via modal “Filtro Avançado”; alinhar UX ao painel inline + Pesquisar de `gc/Movimentos/IndexPedido` com campos Id., PN e Nome.

**O que foi feito:**
Painel acima da grelha (`edit_id_produto`, `edit_codigo`, `edit_nome`), `jsAjaxPesquisarProdutos` / `jsLimparFiltroProdutos`, Ajax com `yesCustomField01–03`. `GetDados` aplica filtros em LINQ (`Contains` / id); PN normalizado (`RemoverEspacos`, prefixo `PN:`). Removido botão/modal da Index (action/view legado mantidos). Build Release OK.

**O que foi evitado:**
- Alterar `ModalFiltroAvancadoView.cshtml` / paginação existente; `filterDb`/`sql_filtro` (ainda não aplicado à query).

**Atenção para próximas intervenções:**
1.º load continua listando todos os ativos até Pesquisar; validar Enter e paginação com filtro ativo.

---
### [2026-05-19] — P2-05 FinanceiroFaturamentos GetDados: paginação SQL sem alterar filtro avançado
**Tipo:** Correção / Performance
**Arquivos tocados:**
- `Areas/g/Controllers/FinanceiroFaturamentosController.cs`

**Problema / Demanda:**
`GetDados` carregava `g_financeiro_faturamentos` inteiro em memória (`ToList`) e aplicava `Skip/Take` no servidor; Admin sem filtro pior caso. Totalizador fazia `GROUP BY` em `g_financeiro` + `DataTable.Select` por linha.

**O que foi feito:**
Sem filtro: EF `AsNoTracking` + `Count` + `Skip/Take`. Filtro simples e avançado (5 campos): mesma montagem SQL (concat inalterada) + `COUNT` subquery + `OFFSET/FETCH`. Avançado inválido: `SELECT *` paginado em vez de `ToList` total. Totalizador só para IDs da página (`IN`). Helpers privados: `TryGetFaturamentosPageSemFiltro`, `TryBuildFaturamentosInnerSelect`, `BuildFaturamentosOrderBy`, `NormalizeDisplayLength` (máx. 100), `LoadQtdTitulosPorFaturamento`, `MapFaturamentosToAaData`. Build Release OK.

**O que foi evitado e por quê:**
- Parametrizar filtro avançado (injection) — fase posterior.
- Alterar `Index.cshtml` ou contrato JSON DataTables.

**Atenção para próximas intervenções:**
Validar em SQL Profiler: sem `SELECT` sem limite no primeiro load Admin. Testar filtro simples e avançado 5 campos vs. comportamento anterior na 1ª página.

---
### [2026-05-19] — Financeiro Index: menu Processos — Transferir Conta Caixa
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Views/Financeiro/Index.cshtml`
- `Scripts/2026_05_20_gdi_check_specific_modals.ps1`

**O que foi feito:**
Item no dropdown Processos (`g_Financeiro_*` ou `g_Financeiro_GerarRemessaBoletosBancarios`), antes de Gerar Remessa; chama `modalTransferirContaCaixa()`.

**Atenção:** Pesquisar com status Em Aberto antes de transferir (validação no servidor).

---
### [2026-05-19] — B3 Financeiro: view ModalTransferirContaCaixa e filtro em AjaxTransferirContaCaixa
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Views/Financeiro/ModalTransferirContaCaixa.cshtml` (novo)
- `Areas/g/Controllers/FinanceiroController.cs` — `ModalTransferirContaCaixa`, `AjaxTransferirContaCaixa` + `LibDB.getFilterByUser`
- `Areas/g/Views/Financeiro/Index.cshtml` — `Url.Action` PascalCase
- `GDI-ERP-Plataform.csproj`

**Problema / Demanda:**
Lote B3 — action `modalTransferirContaCaixa` sem view; POST usava `SentencaSQLTemp` sempre vazio (transferência nunca executava).

**O que foi feito:**
View modal alinhada a `ModalProrrogarVencimentoTitulo` (conta caixa, vencimento, juros/multa); aviso se filtro da grade não foi gravado; leitura do SQL do filtro persistido (`g_Financeiro`) no POST; anti-forgery no Ajax. Build Release OK.

**Atenção para próximas intervenções:**
- Função JS `modalTransferirContaCaixa()` em `Index` ainda sem item de menu visível — expor no dropdown Processos se o fluxo for usado em produção. Filtro deve incluir `id_financeiro_status = 1` (Em Aberto).

---
### [2026-05-19] — PascalCase Lote B2d: cstTenant + Robos Sintegra (raiz do projeto)
**Tipo:** Refatoração
**Arquivos tocados:**
- `Models/CstTenant.cs` (ex-`cstTenant`)
- `Robos/SintegraWS/Models/CstRetornoSintegraWS.cs`, `CstRetornoReceitaCPF.cs`
- `Controllers/UserIdentityController.cs`, `Areas/g/Controllers/ClientesController.cs`
- `GDI-ERP-Plataform.csproj`

**Problema / Demanda:**
Fechar sub-lote B2d — últimos view models `cst*` fora de `Areas` (tenant multi-domínio e retornos Sintegra).

**O que foi feito:**
Rename classe + ficheiro; 8 ficheiros atualizados. **Zero** `public class cst*` restantes no repositório. Build Release OK.

**Atenção para próximas intervenções:**
- Smoke: login por subdomínio (`SetTenants`), portal cliente (`AcessoPortal`), cadastro cliente via robô Sintegra. Plano PascalCase `cst*`/`modal*` em Areas **concluído**.

---
### [2026-05-19] — PascalCase Lote B2c: models cst* em Areas/gc/Models (32 ficheiros)
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/gc/Models/Cst*.cs` (32 renomeados de `cst*`; 5 já tinham ficheiro `Cst*` — classes alinhadas)
- Controllers/views em `Areas/gc` + referências cruzadas (Movimentos, Entradas, COMEX, relatórios, Gerencial)
- `GDI-ERP-Plataform.csproj`
- Caso especial: `cstNfeICMSTotal.cs` → `CstNfeIcmsTotal.cs` (classe `CstNfeIcmsTotal`)

**Problema / Demanda:**
Sub-lote B2c — padronizar view models `cst*` da área `gc` para PascalCase.

**O que foi feito:**
Substituição de tipos em ~149 ficheiros; rename de ficheiros (2 passos Windows); `.csproj` atualizado. Build Release OK.

**Atenção para próximas intervenções:**
- Smoke: entradas NF, importação COMEX, relatórios comerciais, painel gerencial, carta correção, invoice PDF. Próximo: **B2d** (`Models/cstTenant`, `Robos`, `Lib` raiz).

---
### [2026-05-19] — PascalCase Lote B2b: models cst* em Areas/g/Models (29 ficheiros)
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/g/Models/Cst*.cs` (29 — ex-`cst*`, classe + ficheiro)
- Controllers/views em `Areas/g`, `Areas/gc`, `Areas/crm` que referenciam `GdiPlataform.Areas.g.Models.*`
- `Areas/g/Models/Lib/LibFinanceiro.cs`
- `GDI-ERP-Plataform.csproj` (`Compile Include` → `Cst*`)

**Problema / Demanda:**
Sub-lote B2b — padronizar view models `cst*` da área `g` para PascalCase (`Cst*`).

**O que foi feito:**
Rename de ficheiro (2 passos Windows) + substituição de tipo em ~133 ficheiros (controllers, Razor, Lib). Build Release OK. Área `gc` models `cst*` **não** alterados (B2c).

**Atenção para próximas intervenções:**
- Smoke: Financeiro (Index, boletos, prorrogar), GED upload, NFe exportar PDF, portal `crm` boleto, filtros avançados `gc`/g. Próximo: **B2c** (`Areas/gc/Models`).

---
### [2026-05-19] — PascalCase Lote B2a: models cst* em Areas/crm e Areas/a
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/crm/Models/CstDadosPedidoPortal.cs`, `CstListaPedidosPortal.cs` (ex-`cst*`)
- `Areas/crm/Controllers/PedidosController.cs`, `Areas/crm/Views/Pedidos/Index.cshtml`
- `Areas/a/Models/CstFiltroModel.cs` (classe `CstFiltroModel`), `Areas/a/Views/Filtros/ModalFiltroGenericoView.cshtml`
- `GDI-ERP-Plataform.csproj`

**Problema / Demanda:**
Sub-lote B2a do plano PascalCase — alinhar ficheiro + classe dos view models `cst*` nas áreas `crm` e `a`.

**O que foi feito:**
Rename de classes e ficheiros `crm`; `a` já tinha ficheiro `CstFiltroModel.cs` — só a classe foi corrigida. Build Release OK.

**Atenção para próximas intervenções:**
- Smoke: portal cliente (`/crm/Pedidos/Index`), modal filtro genérico (`a/Filtros`). Próximo: **B2b** (`Areas/g/Models`, 28 ficheiros).

---
### [2026-05-19] — PascalCase Lote B1: últimas 3 views modal* em Areas/g
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/g/Views/FinanceiroFaturamentos/ModalAtualizarFaturamentoGestorFranquia.cshtml` (renomeado)
- `Areas/g/Views/Nfe/ModalCancelarNfe.cshtml`, `ModalExportarDadosNfePDF.cshtml` (renomeados)
- `Areas/g/Controllers/NfeController.cs` — `return View("ModalCancelarNfe")`, `ModalExportarDadosNfePDF`
- `GDI-ERP-Plataform.csproj`, `Scripts/2026_05_20_gdi_remove_remaining_modal_icons.ps1`, `Scripts/2026_05_20_gdi_check_specific_modals.ps1`

**Problema / Demanda:**
Concluir padronização PascalCase das views `modal*` em `Areas` (Lote B1 do plano).

**O que foi feito:**
Rename em dois passos (Windows); views Nfe com nome explícito no controller alinhado ao ficheiro; Faturamentos mantém `return View()` implícito (action já `ModalAtualizarFaturamentoGestorFranquia`). Build Release OK.

**O que foi evitado e por quê:**
- Lote B2 (`cst*` → `Cst*`) — alto impacto; próximo passo documentado (B2a: `crm` + `a`).

**Atenção para próximas intervenções:**
- Smoke: Faturamentos (sincronizar gestor franquia), Nfe (cancelar, exportar PDF). Zero ficheiros `modal*.cshtml` restantes em `Areas`.

---
### [2026-05-19] — Planeamento PascalCase Areas: inventário views modal* e models cst*
**Tipo:** Análise
**Arquivos tocados:**
- `.cursor/context/2026_05_20_pascalcase-areas-renomeacao-lotes.md` (novo)

**Problema / Demanda:**
Padronizar views `modal*` e models `cst*` em `Areas` (PascalCase); preparar lote com inventário e impacto.

**O que foi feito:**
Varredura em `Areas/**/Controllers|Models|Views`. Documento com 3 views `modal*` pendentes (Nfe×2, FinanceiroFaturamentos×1), 126 views `Modal*` já conformes, 52 models `cst*` a renomear + 6 `Cst*` híbridos; sub-lotes B1–B3, referências (`csproj`, controllers, Razor, Lib, UserIdentity) e checklist.

**Atenção para próximas intervenções:**
- Implementar **Lote B1** primeiro (baixo risco). Models `cst*` exigem rename de símbolo em massa (B2a→B2d).

---
### [2026-05-19] — Padronização PascalCase: view Usuarios ModalUsuarioTrocarSenha
**Tipo:** Refatoração
**Arquivos tocados:**
- `Areas/g/Views/Usuarios/ModalUsuarioTrocarSenha.cshtml` (renomeado de `modalUsuarioTrocarSenha.cshtml`)
- `Areas/g/Controllers/UsuariosController.cs`
- `GDI-ERP-Plataform.csproj`
- `Scripts/2026_05_20_gdi_remove_remaining_modal_icons.ps1`

**Problema / Demanda:**
Views do módulo Usuários em `Areas` devem iniciar com maiúscula (PascalCase), alinhado à action `ModalUsuarioTrocarSenha`.

**O que foi feito:**
Renomeação da view; `return View("ModalUsuarioTrocarSenha")` explícito; correção `onclick="jsAjaxTrocarSenha()()"` → `jsAjaxTrocarSenha()`; atualização `.csproj` e script de inventário.

**Decisões técnicas relevantes:**
- `UsuariosController`, `Index.cshtml`, `CreateEdit.cshtml` já estavam corretos. Sem Models locais em `Areas/g/Models` para usuários (entidade `g_usuarios` em `Db`).
- Models `cst*` em Areas mantidos (convenção histórica do projeto). Outras views `modal*` fora de Usuários (Nfe, FinanceiroFaturamentos) não alteradas neste lote.

**Atenção para próximas intervenções:**
- Em servidor Linux/IIS case-sensitive, publish deve incluir o novo nome de ficheiro.

---
### [2026-05-19] — P1-06 CSRF Fase 3B: excluir faturamento (g) e troca de senha (Usuarios)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/FinanceiroFaturamentosController.cs`
- `Areas/g/Views/FinanceiroFaturamentos/Index.cshtml`
- `Areas/g/Controllers/UsuariosController.cs`
- `Areas/g/Views/Usuarios/modalUsuarioTrocarSenha.cshtml`

**Problema / Demanda:**
P1-06 — fechar lote financeiro sensível: `AjaxDeleteFaturamentoCompleto` (JSON na Index faturamentos) e `AjaxUsuarioTrocarSenha` (navbar, JSON) sem validação CSRF.

**O que foi feito:**
`[GdiValidateAntiForgeryToken]` nas duas actions; `GdiAjaxAntiForgeryHeaders` nas views; removido `@Html.AntiForgeryToken()` duplicado na modal de senha (mantido no `BeginForm`).

**Decisões técnicas relevantes:**
- POSTs `FinanceiroController` sem referência em views (relatórios, download boletos, transferir conta, avulso) e `AjaxRelComissaoRegra2Tabela` — **não** alterados (sem cliente mapeado; evitar quebra de integração oculta).

**O que foi evitado e por quê:**
- Filtro global; converter `AjaxFinanceiroCancelamento` (gc) de GET para POST.

**Impactos conhecidos:**
- Excluir faturamento e trocar senha passam a exigir token; token já na Index faturamentos e no form da modal.

**Atenção para próximas intervenções:**
- Testar exclusão de faturamento e troca de senha (interno, portal, vendedor). Pendentes documentados: views órfãs `g/Financeiro` (transferir conta sem `.cshtml` no repo), relatórios POST sem JS.

---
### [2026-05-19] — P1-06 CSRF Fase 3A.3: Ajax JSON gc/FinanceiroLancamentos (Index + modal movimentos)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs`
- `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml`
- `Areas/gc/Views/FinanceiroLancamentos/ModalViewFinanceiroMovimentos.cshtml`

**Problema / Demanda:**
P1-06 — POSTs JSON na lista gc de lançamentos financeiros (export Excel, posição conta caixa, robô Itaú, boleto PDF) sem validação CSRF apesar de token na Index/modal.

**O que foi feito:**
`[GdiValidateAntiForgeryToken]` em `AjaxExportarLancamentosExcel`, `AjaxPosicaoAtualContaCaixa`, `AjaxRoboItau`, `AjaxFinanceiroBoletoGCPDF`. Views com `headers: GdiAjaxAntiForgeryHeaders()` (Index) ou seletor do form da modal.

**Decisões técnicas relevantes:**
- Reutiliza atributo e helpers da 3A.2; sem filtro global.
- `ModalGerarFinanceiroMovimentos` chama boleto via GET (fora do POST protegido). Ged/anexos (upload, download S3) — fase posterior.

**O que foi evitado e por quê:**
- Alterar `GedController` e DataTables `GetDados*` do financeiro gc.

**Impactos conhecidos:**
- Processos do menu Processos/Relatórios na Index gc passam a exigir token; token já renderizado na página.

**Atenção para próximas intervenções:**
- Testar export Excel, posição contas, robô Itaú e boleto PDF (Index e modal movimentos). Próximo: 3B `g/Financeiro` POSTs restantes (relatórios, transferir conta, avulso) e/ou Usuarios troca senha.

---
### [2026-05-19] — P1-06 CSRF Fase 3A.2: JSON/FormData financeiro + GdiValidateAntiForgeryToken
**Tipo:** Implementação
**Arquivos tocados:**
- `Security/GdiValidateAntiForgeryTokenAttribute.cs` (novo)
- `GDI-ERP-Plataform.csproj`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` — `GdiGetAntiForgeryToken`, `GdiAjaxAntiForgeryHeaders`
- `Areas/g/Controllers/FinanceiroController.cs`, `FinanceiroLancamentosController.cs`, `FinanceiroFaturamentosController.cs`
- Views: `ModalGerarFaturamento`, `ModalFecharLancamentosAbertos`, `ModalFinalizarEdicaoTitulo`, `ModalIncluirLancamentos`, `Financeiro/Index`, `ModalImportarArquivoFaturamentoGestorFranquia`

**Problema / Demanda:**
P1-06 — POSTs com `application/json` ou `FormData` sem token no corpo; `[ValidateAntiForgeryToken]` padrão só lê `request.Form`.

**O que foi feito:**
Atributo `GdiValidateAntiForgeryToken` (form + cabeçalhos `RequestVerificationToken` / `__RequestVerificationToken` / `X-RequestVerificationToken`). Helpers em `start.js`. Dez actions financeiras protegidas; views Ajax JSON com `headers: GdiAjaxAntiForgeryHeaders(...)`. Importação faturamento: `FormData` a partir do formulário (inclui token oculto).

**Decisões técnicas relevantes:**
- 3A.1 mantém `[ValidateAntiForgeryToken]`; 3A.2 usa `[GdiValidateAntiForgeryToken]` apenas onde o cliente não envia `form.serialize()`.
- `gc` FinanceiroLancamentos Index (export Excel, robô Itaú, etc.) fora do lote — 3A.3.

**O que foi evitado e por quê:**
- Filtro global / `ajaxSetup` — risco de quebrar DataTables e centenas de POSTs legados.
- Substituir todos os `[ValidateAntiForgeryToken]` existentes no ERP.

**Impactos conhecidos:**
- Após publish, incrementar `VersionERP` se browsers cachearem `start.js` antigo sem os helpers.

**Atenção para próximas intervenções:**
- Testar executar faturamento, fechar/finalizar lançamentos, incluir lançamento, enviar boletos e-mail, importar arquivo gestor franquia. Próximo: 3A.3 `gc` Index + demais POSTs JSON financeiros.

---
### [2026-05-19] — P1-06 CSRF Fase 3A.1: ValidateAntiForgeryToken em Ajax financeiro (form.serialize)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/FinanceiroController.cs`
- `Areas/g/Controllers/FinanceiroLancamentosController.cs`
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs`
- `Areas/g/Controllers/FinanceiroFaturamentosController.cs`

**Problema / Demanda:**
P1-06 — dezenas de POSTs financeiros sem `[ValidateAntiForgeryToken]` apesar de modais já emitirem `@Html.AntiForgeryToken()` e enviarem o token via `form.serialize()`.

**O que foi feito:**
Fase 3A.1: atributo `[ValidateAntiForgeryToken]` em 24 actions Ajax de mutação financeira (títulos `g`, lançamentos `g`/`gc`, faturamentos) cujo cliente já envia `__RequestVerificationToken` no corpo do POST. Em `gc`, `AjaxCreateEditLancamento` e `AjaxLiquidarLancamento` passaram a `[HttpPost]` explícito. `ajaxSimularEnviarNFEmailCliente` / `ajaxProcessarEnviarNFEmailCliente` idem.

**Decisões técnicas relevantes:**
- Escopo restrito a POST com `form.serialize()` — sem alterar `start.js` nem Ajax JSON (`contentType: application/json`) nesta fase (3A.2).

**O que foi evitado e por quê:**
- `AjaxImportarArquivoFaturamentoGestorFranquia` (FormData sem token), DataTables `GetDados*`, `ajaxEnviarBoletosEmail`, export Excel — exigem envio de token no cliente (fases seguintes).

**Impactos conhecidos:**
- Pedidos forjados sem token passam a falhar com erro antiforgery (comportamento desejado). Fluxos que já tinham token na modal mantêm funcionamento.

**Atenção para próximas intervenções:**
- Testar manualmente: cancelar/baixar título, prorrogar vencimento, remessa boletos, simular faturamento (g), baixar/cancelar lançamento (gc), e-mail NF faturamento. Fase 3A.2: JSON + header/helper em `start.js` para `AjaxExecutarGerarFaturamento`, fechar lançamentos, etc.

---
### [2026-05-19] — P1-05: Roles "*" — Usuarios, Filtros por módulo e Dispose
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/UsuariosController.cs`
- `Areas/a/Controllers/FiltrosController.cs`
- `Areas/g/Controllers/CentrosCustosController.cs`
- `Areas/g/Controllers/ClassificacaoFinanceiraController.cs`
- `Areas/g/Controllers/NfeController.cs`
- `Areas/g/Controllers/PortalVendedorController.cs`

**Problema / Demanda:**
P1-05 — `CustomAuthorize(Roles = "*")` em controllers sensíveis; portal -900 não deve aceder a gestão de utilizadores/filtros de outros módulos.

**O que foi feito:**
Removido `Roles = "*"` da classe `UsuariosController`; troca de senha com roles explícitas (incl. portal/vendedor). `FiltrosController`: base `SuperAdmin,Admin,*,Home` + `UserCanUseFiltro(id)` por prefixo de módulo. Removido `[CustomAuthorize]` dos métodos `Dispose`.

**Decisões técnicas relevantes:**
- Filtros legados (`gdc_*`, `gts_*`, `g_Revendas_Index`) só SuperAdmin/Admin (sem mapeamento de role).

**O que foi evitado e por quê:**
- Alterar `Contexto.cs` (remover `"*"` global) — impacto transversal; fora do escopo.

**Atenção para próximas intervenções:**
- Testar filtro avançado em Clientes/Produtos com perfil sem permissão ao módulo; troca de senha no navbar (interno e portal).

---
### [2026-05-19] — P1-04 XSS Fase A: encoding em Atendimentos (views, DataTables, logs)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Views/Atendimentos/Edit.cshtml`
- `Areas/g/Views/Atendimentos/ModalCreateEditAtividade.cshtml`
- `Areas/g/Controllers/AtendimentosController.cs`

**Problema / Demanda:**
P1-04 — `@Html.Raw` em `solicitacao`/`descricao` e dados do utilizador expostos em títulos, grelhas e logs.

**O que foi feito:**
Removido `Html.Raw` dos campos de texto; Razor encoda com `white-space: pre-wrap`. `ViewBag.MsgCategoria` sem Raw. Controller: `EncodeAtendimentoDisplay` / `EncodeForLogHtml` em `getDados`, `getDadosAtividades`, `getDadosAtendimentosLogs`, `ViewBag.Title`, gravação de logs e mensagens de retorno com atividades.

**Decisões técnicas relevantes:**
- Logs novos gravam HTML estrutural do servidor com valores encode; registos antigos no BD podem ainda conter HTML não encode.

**O que foi evitado e por quê:**
- Fases B–D (boletos, inventário global, NuGet sanitizador) — fora do escopo pedido.

**Impactos conhecidos:**
- Quebras de linha em descrição na tela via CSS; em logs via `<br/>` após encode.

**Atenção para próximas intervenções:**
- Testar atendimento com `<script>alert(1)</script>` na solicitação; Fase B para `EMensagemCaixa`.
---
### [2026-05-19] — P1-02 Web.config: customErrors On em Release + aviso dev no Web.config base
**Tipo:** Implementação
**Arquivos tocados:**
- `Web.config`
- `Web.Release.config`

**Problema / Demanda:**
Auditoria P1-02 — `debug="true"` e `customErrors mode="Off"` no Web.config versionado; endurecer produção e documentar que o ficheiro base é só para dev local.

**O que foi feito:**
`Web.Release.config`: `customErrors mode="On"` (antes `RemoteOnly`). `Web.config`: comentários explícitos de que debug/Off são dev e que produção depende de Publish Release + transformação.

**Decisões técnicas relevantes:**
- `mode="On"` evita yellow screen mesmo em pedidos locais no servidor IIS.

**O que foi evitado e por quê:**
- Alterar valores no Web.config base para RemoteOnly/debug false — preserva diagnóstico em F5/IIS Express.

**Impactos conhecidos:**
- Após próximo publish Release, validar `Web.config` no servidor sem `debug="true"` e com `customErrors mode="On"`.

**Atenção para próximas intervenções:**
- `MSBuild /t:TransformWebConfig /p:Configuration=Release` para validar transformação antes do deploy.
---

> As entradas mais recentes ficam sempre no TOPO desta seção.

---

### [2026-05-14] — Fase 18: DataTables g e gc — audit e correção de controllers/views faltantes
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/CfopController.cs`
- `Areas/gc/Controllers/CfopOperacoesController.cs`
- `Areas/gc/Controllers/CfopParametrosController.cs`
- `Areas/gc/Controllers/EstoqueControleController.cs`
- `Areas/gc/Controllers/EstoqueLotesController.cs`
- `Areas/gc/Controllers/FinanceiroParametroDifalController.cs`
- `Areas/gc/Controllers/ComexFinanceiroController.cs`
- `Areas/gc/Views/EstoqueControle/Index.cshtml`

**Problema / Demanda:**
Audit das áreas g e gc revelou 7 controllers gc que não tinham o padrão DataTables completo (Fases 8–16 não os cobriram): 6 totalmente legados (sem param null, sem try/catch, sem errorMessage/stackTrace) e 1 com inline pattern mas sem null guard.

**O que foi feito:**
- `CfopController`, `CfopOperacoesController`, `CfopParametrosController`: param null guard + try/catch + `JsonDataTableException` privado + `errorMessage`/`stackTrace`/`yesFilterOnOff` no JSON de sucesso.
- `EstoqueControleController`: mesmo padrão + proteção NRE com `?.` nas chamadas `Find(...).descricao` (produto/status podem estar ausentes na lista).
- `EstoqueLotesController`: mesmo padrão + `yesFilterOnOff` computado pelos 4 filtros customizados (`yesCustomField01–04`).
- `FinanceiroParametroDifalController`: param null guard + try/catch + `JsonDataTableException` + `errorMessage`/`stackTrace` no sucesso (yesFilterOnOff já existia).
- `ComexFinanceiroController.GetDadosPagamentos`: apenas param null guard (inline pattern com errorMessage/stackTrace/severity já estava correto).
- `EstoqueControle/Index.cshtml`: adicionado `xhr.dt` → `GdiDtNotifyJsonErrorMessage` no `dtGcProdutosControle` (view estava sem, único gap de view nesta fase).

**Decisões técnicas relevantes:**
- `EstoqueLotes`: yesFilterOnOff calculado por `filtroCodigoLote || filtroSerialLote || filtroProduto || filtroImportacao` (a view não usa `btnFiltro`, mas o contrato fica consistente).
- Areas g: todas conformes após análise (ClientesController.GetDados usa método longo com catch no fim — 260 linhas; NfeController, AtendimentosController, FinanceiroController confirmados).

**O que foi evitado e por quê:**
- Não alteradas views Cfop/Index, CfopOperacoes/Index, CfopParametros/Index, EstoqueLotes/Index, FinanceiroParametroDifal/Index, ComexFinanceiro/Index — já tinham `xhr.dt` correto.
- Não alterado EstoqueControle/CreateEdit — `otableAfericoes` usa data source diferente de `GetDados`.
- Não alterado `GedSGQController` (qa) — já coberto como "baixa prioridade" na Fase 17.

**Impactos conhecidos:**
- `EstoqueControleController.GetDados`: o `?.` no `Find()` retorna `""` em vez de lançar NRE quando produto/status não está na lista — comportamento mais seguro em produção.
- Migração DataTables agora completa em todas as áreas: g, gc, crm, qa, a.

**Atenção para próximas intervenções:**
- `GetDadosPops` (qa/GedSGQController) — código morto confirmado; candidato a remoção.
- GedSGQController JSONs de sucesso sem `errorMessage`/`stackTrace` — inócuo funcionalmente, cosmético.

---

### [2026-05-14] — Fase 17: DataTables área `a` — AuditController + ParametrosController + 5 views
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/a/Controllers/AuditController.cs`
- `Areas/a/Controllers/ParametrosController.cs`
- `Areas/g/Views/Clientes/CreateEdit.cshtml`
- `Areas/g/Views/Produtos/CreateEdit.cshtml`
- `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml`
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `Areas/a/Views/Parametros/Index.cshtml`

**Problema / Demanda:**
Área `a` era a única com controllers DataTables sem try/catch, sem JsonDataTableException e sem errorMessage/stackTrace. `GetAuditTrail` é consumido por 4 views em áreas g e gc já na fase completa, mas sem xhr.dt — erro de servidor seria silencioso.

**O que foi feito:**
- `AuditController.GetAuditTrail`: param null guard + envolvimento em try/catch + JSON sucesso com `errorMessage: ""` / `stackTrace: ""` + método privado `JsonDataTableException`.
- `ParametrosController.GetDadosSistemas`: mesmo padrão + `yesFilterOnOff: "0"` no sucesso (ausente antes).
- 5 views: adicionado `.on('xhr.dt', function (e, settings, json, xhr) { GdiDtNotifyJsonErrorMessage(json); })` nas inicializações `otableGClientesAudit`, `otableGProdutosAudit`, `otableGcComexAudit`, `otableGcMovimentosAudit`, `otableAParametros`.

**Decisões técnicas relevantes:**
- `JsonDataTableException` em `AuditController` sem parâmetro `yesFilterOnOff` (método não tem filtro ativo — fixo em `"0"`).
- `try/catch` envolve todo o corpo do método; os `try/catch` internos para NRE de `NomeUsuario` foram preservados intactos.

**O que foi evitado e por quê:**
- Não alterado `GedSGQController` (qa) — já tem try/catch e JsonDataTableException corretos; ausência de errorMessage/stackTrace no sucesso é inócua funcionalmente.
- Não alterado `GetDadosPops` (qa) — método sem consumidor nas views (código morto); remover seria fora do escopo.
- Não alterado `LmsEvidenceController` — contrato `{ok, error}` próprio, não DataTables.

**Impactos conhecidos:**
- `GetAuditTrail` é usado em Clientes, Produtos, ComexImportacoes e FormPedidoCreate: comportamento de sucesso inalterado; erros agora chegam ao `GdiDtNotifyJsonErrorMessage` via `xhr.dt`.
- Área `crm`: sem GetDados DataTables — não é alvo de fase.

**Atenção para próximas intervenções:**
- `GetDadosPops` em `GedSGQController` é código morto — nenhuma view o chama. Candidato à remoção futura com validação.
- JSONs de sucesso em `GedSGQController` (qa) sem `errorMessage`/`stackTrace` — cosmético, baixa prioridade.
- Fases DataTables agora completas em todas as áreas mapeadas (g, gc, crm, qa, a).

---

### [2026-05-14] — Remoção de `GerarNFPImportacaoByMovimentoNF_OLD` (código morto)
**Tipo:** Refatoração
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Método legado não referenciado em nenhum ponto do repositório.

**O que foi feito:**
- Removida a região e o método **`GerarNFPImportacaoByMovimentoNF_OLD`** de **`RoboEnotasNFE`**. O fluxo ativo continua a ser **`GerarNFPImportacaoByMovimentoNF`** (ex.: `MovimentosEntradasController`).

**O que foi evitado e por quê:**
- Sem alteração ao método V2 nem ao controller.

**Impactos conhecidos:**
- Nenhum consumidor no código; comportamento de produção inalterado.

**Atenção para próximas intervenções:**
- Se existir referência externa ao `_OLD` (scripts, outro repo), ajustar para o V2.

---

### [2026-05-14] — eNotas: falha de transmissão JSON → `gc_movimentos_nf.id_nfe_status = 14` + `g_nfe_logs`
**Tipo:** Correção
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Em falhas de rede/IO ao enviar JSON à API eNotas (ex.: servidor remoto recusou conexão), o movimento NF e o log não refletiam o status de erro nos dados (**14**).

**O que foi feito:**
- Método **`PersistirFalhaTransmissaoJsonEnotasMovimentoNf`**: atualiza **`gc_movimentos_nf.id_nfe_status = 14`**, insere **`g_nfe_logs`** com mensagem truncada (via `LibExceptions` / `WebException`), campos obrigatórios do log preenchidos a partir do NF; erros secundários ao persistir são ignorados.
- **`GerarNFPVendaByMovimentoNF`** e **`GerarNFServicoByMovimentoNF`**: no `catch` antes de relançar, chamada ao helper quando **`id_movimento_nf > 0`** (já gravado antes do POST).
- **`GerarNFPImportacaoByMovimentoNF`** (V2): **`gc_movimentos_nf`** passa a ser gravado **antes** do HTTP (mesmos campos que no sucesso), para existir `id_movimento_nf` em falha de transmissão; `catch` chama o helper; ramo de sucesso deixa de duplicar `Add`/`SaveChanges` da NF.

**Decisões técnicas relevantes:**
- **`id_nfe_status`** existe em **`gc_movimentos_nf`**, não na entidade **`g_nfe_logs`**; o log descreve a falha de transmissão.

**O que foi evitado e por quê:**
- Não alterar outros `catch` do robô (consulta status, etc.) fora do escopo de emissão POST produto/serviço.

**Impactos conhecidos:**
- Importação: registo **`gc_movimentos_nf`** criado antes da resposta OK da eNotas (antes só após sucesso); em sucesso o movimento pedido continua a ser atualizado como antes.

**Atenção para próximas intervenções:**
- Se a API devolver HTTP não OK mas com corpo (ex.: validação), o fluxo atual continua a lançar **`Exception(responseData)`** sem passar pelo helper de transmissão (conexão estabelecida).

---
**Tipo:** Correção
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`

**Problema / Demanda:**
eNotas negou NFS-e (**GW3001**): série do RPS deve conter **apenas números** e estar entre **1 e 49.999**. O envio usava **`serieRps` = `"RPS"`** (letras).

**O que foi feito:**
- `NormalizarSerieRpsEnotas`: valida dígitos e intervalo; inválido ou vazio → **`"1"`**.
- `GerarNFServicoByMovimentoNF`: `serieRps` passa a usar **`g_nfe_gateway.key3`** da filial do gateway (quando preenchido com número válido); caso contrário **`"1"`**.

**O que foi evitado e por quê:**
- Sem alteração de schema SQL: `key3` já existia na entidade e não era usada no código do robô.

**Impactos conhecidos:**
- Filiais que precisem de série específica (ex.: 2, 10) devem gravar esse valor em **`g_nfe_gateway.key3`** para o registo do gateway da filial usado na NFS-e.

**Atenção para próximas intervenções:**
- Se `key3` for reutilizado noutro contexto no futuro, extrair coluna dedicada ou documentar convenção no `CLAUDE.md`.

---

### [2026-05-14] — `AtualizarStatusNFPbyMovimentoNF`: consulta NFS-e em `/v1/nfes/porIdExterno` (evita GEN002)
**Tipo:** Correção
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`
- `Robos/ENotas/Nfe/NFSe.cs`

**Problema / Demanda:**
Ao atualizar status de **NF de serviços** (`GetNotaFiscalPedido` → `AtualizarStatusNFPbyMovimentoNF`), a API eNotas devolvia **GEN002** (erro genérico): o código fazia **GET** `.../v2/empresas/{id}/nf-e/{nf_identificador}` pensando em **NF-e produto** (identificador GUID da nota), mas em NFS-e o `nf_identificador` é o **`idExterno`** enviado na emissão, não a chave da rota `nf-e`.

**O que foi feito:**
- Deteção de payload NFS-e: `xml_erp` JSON com **`servico`** na raiz e sem **`itens`** (`IsJsonEnvioNfseServico`).
- Nesse caso: **GET** `https://api.enotasgw.com.br/v1/empresas/{Key2}/nfes/porIdExterno/{Uri.EscapeDataString(nf_identificador)}` (alinhado ao Yes-ERP-Cloud e ao download XML já existente no robô).
- Deserialização com **`DataNFSe`**: PDF via **`linkDownloadPDF`**, demais campos mapeados para as mesmas variáveis usadas na atualização do `gc_movimentos_nf` (fluxo unificado com NF-e produto).
- **`NFSe`**: propriedades opcionais para resposta da consulta (`numero`, `dataCompetenciaRps`, `chaveAcesso`, datas em string).
- **`catch (WebException)`** neste método: uso de **`LibExceptions.getWebException`** (evita NRE se `Response` for nulo).

**O que foi evitado e por quê:**
- Não alterar `AtualizarStatusG_nfePorId` (usa `nfe_key` / NF-e avulsa) nem cancelamento.

**Impactos conhecidos:**
- Notas de serviço emitidas pelo `GerarNFServicoByMovimentoNF` (com `xml_erp` típico) passam a consultar o endpoint correto; NF-e produto mantém **`/v2/.../nf-e/...`**.

**Atenção para próximas intervenções:**
- Registos antigos sem `xml_erp` ou com JSON atípico continuam no fluxo **NF-e** (v2); se existirem, avaliar outro critério (ex.: CFOP operação serviço).

---

### [2026-05-14] — NFS-e eNotas: URL emissão `/v1/.../nfes` + `getWebException` sem `Response`
**Tipo:** Correção
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`
- `Lib/LibExceptions.cs`

**Problema / Demanda:**
`GerarNFServicoByMovimentoNF` chamava `POST .../v2/empresas/{id}/nfes`; em ambiente real surgiu **404 Não Localizado**, `WebException` e o modal (`AjaxModalPedidoNotaFiscal`) não recebia JSON útil se `getWebException` falhasse com `Response` nulo.

**O que foi feito:**
- URL de emissão NFS-e alinhada à documentação oficial da eNotas (**POST** `https://api.enotasgw.com.br/v1/empresas/{empresaId}/nfes`), coerente com o próprio projeto (`/v1/.../nfes/porIdExterno/.../xml`).
- `LibExceptions.getWebException`: guardas para `ex`/`Response` nulos e corpo vazio → devolve `ex.Message` em vez de lançar.

**O que foi evitado e por quê:**
- Não alterar endpoints **NF-e produto** (`/v2/.../nf-e`) nem outros fluxos eNotas.

**Impactos conhecidos:**
- Emissão NFS-e passa a usar a mesma versão de path que a doc eNotas de emissão de nota fiscal (NFS-e).

**Atenção para próximas intervenções:**
- Se a eNotas descontinuar `v1/nfes`, rever doc do provedor e ajustar URL.

---

### [2026-05-14] — `AjaxModalPedidoNotaFiscal`: NF produto vs serviço (`gc_cfop_operacoes.is_servico`, eNotas)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs`

**Problema / Demanda:**
Emissão eNotas no modal de NF do pedido chamava sempre `GerarNFPVendaByMovimentoNF`, ignorando operação de serviço.

**O que foi feito:**
- Com `IdGateway == 1` e `record_gc_cfop_operacao.is_servico == true` → **`GerarNFServicoByMovimentoNF`**; caso contrário → **`GerarNFPVendaByMovimentoNF`** (após validações existentes e com `record_gc_cfop_operacao` já garantido por `QtdErros == 0`).
- `Sucesso` passa a refletir o **retorno bool** do robo; mensagem de sucesso distingue serviços vs pedido produto; em falha genérica sem exceção, mensagem orientando ver logs.

**O que foi evitado e por quê:**
- Ramo WebMania (`IdGateway == 2`) mantém-se comentado; sem alterar outros gateways.

**Impactos conhecidos:**
- Operações com `is_servico` na CFOP emissão pelo mesmo modal usam fluxo NFS-e (regras já no `RoboEnotasNFE`).

---

### [2026-05-14] — `RoboEnotasNFE.GerarNFServicoByMovimentoNF`: alinhamento fase 1 à NF venda (gateway/filial/status)
**Tipo:** Correção
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs`

**Problema / Demanda:**
NFS-e serviço (escopo BH, 1 item) com persistência e status iniciais desalinhados de `GerarNFPVendaByMovimentoNF`.

**O que foi feito:**
- `id_nfe_status` inicial **1 → 14** (igual venda, pré-envio “Erro nos dados”).
- **`id_coligada`** e **`id_filial`** a partir de `RecordMovimento` antes do `Add`.
- **`id_nfe_gateway`** por filial (1/2) com fallback 1, mesmo bloco que venda (incl. atribuição inicial `= 1`).
- Mensagem de validação: typo **Seviços** → **Serviços**; prefixo **NF-e** no texto do item único.

**O que foi evitado e por quê:**
- Sem alterar TLS, URL eNotas, `GerarNFPVendaByMovimentoNF` ou outros métodos do robo.

**Impactos conhecidos:**
- Registos `gc_movimentos_nf` de serviço passam a gravar coligada/filial e gateway como na venda; status inicial 14 até resposta OK (continua a definir `1` após sucesso, como já estava).

---

### [2026-05-14] — Regras Cursor + `CLAUDE.md`: linha de commit Git no fim do relatório
**Tipo:** Implementação
**Arquivos tocados:**
- `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc`
- `CLAUDE.md`

**Problema / Demanda:**
Padronizar mensagem de commit numa linha ao fim de cada relatório de implementação/ajuste.

**O que foi feito:**
- §6 do `.mdc`: nova ordem **8 — Linha de commit (Git)** e subsecção **6.1** com formato `AAAA_MM_DD - resumo` (data com `_`), PT-BR, só quando houve alteração de ficheiros.
- `CLAUDE.md`: alinhamento do formato de resposta com remissão à §6.1.
- Tabela §0.2: referência à linha de commit no formato de resposta.

**Impactos conhecidos:**
- Respostas futuras do agente devem incluir a linha após o bloco do CHANGELOG quando aplicável.

---

### [2026-05-14] — Menu lateral: `LibMessageProcessando` antes da navegação MVC
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js`

**Problema / Demanda:**
Clique em opção do menu esquerdo sem feedback visual até a nova página carregar.

**O que foi feito:**
- Listener delegado em `click`: só âncoras dentro de **`.app-sidebar`** com `href` navegável; ignora `#`, `javascript:`, **Ctrl/Cmd/Shift/Alt**, botão não primário, `download`, `target="_blank"` (e outros `target` que não `_self`).
- `preventDefault` + `LibMessageProcessando('Carregando . . .')` + `location.href` — alinhado a `JsNewRecord` / `JsNewWindow`.

**O que foi evitado e por quê:**
- Sem alterar `_Navbar.cshtml` nem links; sem handler global fora do sidebar.

**Impactos conhecidos:**
- Incrementar **`VersionERP`** no fluxo habitual se o browser mantiver `start.js` em cache.

---

### [2026-05-14] — Sidebar logo GDI: imagem maior dentro da mesma faixa (sem alterar `gdi-sidebar-brand-logo`)
**Tipo:** Correção
**Arquivos tocados:**
- `Views/Shared/_Navbar.cshtml`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css`

**Problema / Demanda:**
Logo ainda pequeno face ao espaço útil da faixa; manter bloco `.gdi-sidebar-brand-logo` (min-height / height) inalterado.

**O que foi feito:**
- `max-height` da imagem **72px → 88px** (proporção com `width/height: auto` + `object-fit: contain`).
- Link do logo: **`py-2` → `py-0`** para libertar **1rem** de padding vertical dentro da mesma altura mínima da faixa, evitando corte sem mexer nas regras do contentor.

**O que foi evitado e por quê:**
- Sem alterar `.app-sidebar .sidebar-brand.gdi-sidebar-brand-logo { … }` (min-height / height / box-sizing).

**Impactos conhecidos:**
- Área clicável vertical do link fica só na altura do logo (aceitável nesta faixa).

---

### [2026-05-14] — Sidebar logo GDI: contentor + imagem (corte `overflow` / `3.5rem`)
**Tipo:** Correção
**Arquivos tocados:**
- `Views/Shared/_Navbar.cshtml`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css`

**Problema / Demanda:**
Com `max-height` maior no `<img>` sem aumentar a faixa `.sidebar-brand`, o AdminLTE (`height: 3.5rem`, `overflow: hidden`) cortava o logo (ex.: texto inferior).

**O que foi feito:**
- 2.º `sidebar-brand`: classe **`gdi-sidebar-brand-logo`**; imagem com classe **`gdi-sidebar-brand-logo-img`** (estilos retirados do inline).
- Em **`start.css`**: só `.app-sidebar .sidebar-brand.gdi-sidebar-brand-logo` com `height: auto`, `min-height: calc(72px + 1rem + 1.625rem)` (72px logo + `py-2` + padding vertical padrão da faixa); regras do `<img>` com `max-height: 72px`, `object-fit: contain`, `flex-shrink: 0`.
- Primeira faixa (Microsoft/AWS) e `adminlte.css` inalterados.

**O que foi evitado e por quê:**
- Sem editar `adminlte.min.css`; override mínimo e escopado.

**Impactos conhecidos:**
- `start.css` carrega com `?v=VersionERP` no layout — se o browser mantiver CSS antigo, incrementar `VersionERP` no fluxo habitual do projeto.

---

### [2026-05-14] — Sidebar: logo GDI (`max-height` no `_Navbar`)
**Tipo:** Correção
**Arquivos tocados:**
- `Views/Shared/_Navbar.cshtml`

**Problema / Demanda:**
Logo `logoGdi.png` (222×110) na segunda faixa `sidebar-brand` aparecia pequeno por `max-height: 48px` no `<img>`.

**O que foi feito:**
- `max-height` do `<img>` do GDI de **48px → 64px**; mantidos `max-width: 100%`, `width`/`height: auto`, `object-fit: contain` (proporção ~2:1).

**O que foi evitado e por quê:**
- Sem alterar `adminlte.css` nem altura global de `.sidebar-brand` — ajuste pontual só no markup do logo.

**Impactos conhecidos:**
- Se em algum zoom/viewport notar corte, avaliar classe só nesse `sidebar-brand` ou reduzir para 56px (ainda dentro do intervalo sugerido).

---

### [2026-05-14] — Navbar `user-menu`: indicador visual de dropdown
**Tipo:** Implementação
**Arquivos tocados:**
- `Views/Shared/_Navbar.cshtml`

**Problema / Demanda:**
O trigger do menu do utilizador não mostrava indício de submenu; o AdminLTE remove o `::after` de `.navbar-nav > .user-menu > .nav-link`.

**O que foi feito:**
- Ícone **`fa-chevron-down`** (Font Awesome, `aria-hidden="true"`) no fim do `<a class="nav-link dropdown-toggle">`, com `ms-1 small` — só no bloco `user-menu`, sem CSS global nem alteração a `adminlte.css`.

**O que foi evitado e por quê:**
- Restaurar o pseudo-elemento `::after` via override — duplicaria o caret do Bootstrap e competiria com regras do tema.

**Impactos conhecidos:**
- Apenas o link do utilizador na navbar; outros dropdowns inalterados.

---

### [2026-05-14] — Login `UserIdentity`: HTML válido (`BeginForm` só no `body`) + scroll residual
**Tipo:** Correção
**Arquivos tocados:**
- `Views/UserIdentity/Index.cshtml`
- `Views/UserIdentity/TrocaObrigatoriaSenha.cshtml`

**Problema / Demanda:**
Barra de scroll vertical residual na página de login apesar de `100dvh` / padding em `box-sizing`.

**O que foi feito:**
- **`Html.BeginForm` deixou de envolver `<!DOCTYPE>` / `<html>` / `<head>`** — o `<form>` fica só dentro de `<body>`, envolvendo `.login-page-wrap` (DOM válido; evita normalização estranha do browser).
- Removido o bloco `<style>` de `.app-content` (irrelevante com `Layout = null`).
- CSS: cadeia **`html` → `body` → `#UserIdentity` / `#formTrocaObrigatoriaSenha` → `.login-page-wrap`** com `height`/`min-height`/`max-height` + `overflow-y: auto` só no wrapper (documento sem crescer por “fantasma”).
- **`TrocaObrigatoriaSenha`:** mesmo arranjo de documento; **`<form>` interno aninhado** trocado por `<div class="troca-senha-fields">` para um único POST no `BeginForm` MVC; scripts antes de `</body>`.

**O que foi evitado e por quê:**
- `overflow: hidden` em `html`/`body` — poderia interferir com overlays (ex.: SweetAlert2); o scroll extra fica confinado ao `.login-page-wrap` quando necessário.
- Sem alterações a `start.css`, `Login.css`, `_Layout`.

**Impactos conhecidos:**
- Troca de senha: o botão **Alterar senha** passa a submeter o formulário MVC externo (comportamento pretendido; antes o `<form>` interno podia isolar o submit).

**Atenção para próximas intervenções:**
- Outras views autónomas com `BeginForm` à volta do documento inteiro: mesmo padrão.

---

### [2026-05-14] — Login `UserIdentity`: scroll vertical fantasma (viewport + `100dvh` + fundo)
**Tipo:** Correção
**Arquivos tocados:**
- `Views/UserIdentity/Index.cshtml`
- `Views/UserIdentity/TrocaObrigatoriaSenha.cshtml`

**Problema / Demanda:**
Página de login a forçar scroll vertical sem necessidade de conteúdo extra.

**O que foi feito:**
- Meta viewport sem `height=device-height` (só `width=device-width, initial-scale=1`).
- `min-height`: fallback **`100vh`** + **`100dvh`** em `body` e `.login-page-wrap`; **`box-sizing: border-box`** explícito; **`background-attachment: scroll`** em vez de `fixed` (evita inflação da altura do documento em alguns browsers).
- `html { height: 100%; }` no bloco inline (coerente com `Login.css` nestas páginas isoladas).

**O que foi evitado e por quê:**
- Sem alterações a `start.css`, `Login.css`, `_Layout` — resto do ERP inalterado.

---

### [2026-05-14] — Reversão: bloco `form-switch` / `form-switch-lg` / `switch-success` em `start.css`
**Tipo:** Correção (reversão)
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css`

**O que foi feito:**
- Desfeitas as alterações do comando anterior: restaurado o CSS de `switch-success` / `form-switch-lg` ao estado imediatamente anterior (sem `:focus` extra, sem wrapper `padding-left`/`margin-left` ampliados, sem variantes `form-check-reverse`).

---

### [2026-05-14] — UI global: `.section-header` — `font-size` fixo **1rem** (`start.css`)
**Tipo:** Correção
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css`

**Problema / Demanda:**
Refinar o cabeçalho de secção após o aumento para 1.275rem: passar a **`font-size: 1rem`** (raiz típica BS5) e alinhar comentário de manutenção.

**O que foi feito:**
- `font-size`: **1.275rem** → **1rem**; comentário do bloco atualizado (histórico 0.85rem → 1.275rem → 1rem).
- `padding`, `gap`, `font-weight` e restantes propriedades de `.section-header` mantidos — já adequados a 1rem.

---

### [2026-05-14] — UI global: `.section-header` — `font-size` +50% (`start.css`)
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/css/start.css`

**Problema / Demanda:**
Aumentar em 50% o tamanho da fonte dos cabeçalhos de secção (`.section-header`), de forma centralizada.

**O que foi feito:**
- `font-size`: **0.85rem** → **1.275rem** (0.85 × 1.5); restantes propriedades da classe inalteradas.

**Atenção para próximas intervenções:**
- O layout referencia `start.css?v=@VersionERP`; após publish, utilizadores com sessão antiga podem precisar de refresh forçado ou nova versão em `ControlVersion` para invalidar cache do CSS.

---

### [2026-05-14] — Botões `name="btnSair"` em footer: `btn-info` → `btn-primary`
**Tipo:** Refatoração (UI mínima)
**Arquivos tocados:**
- `Areas/gc/Views/Estoque/FormRecebimentoItensEstoque.cshtml` — `card-footer`
- `Areas/gc/Views/Estoque/FormRecebimentoItensImportacao.cshtml` — `card-footer`
- `Areas/gc/Views/EstoqueInventario/FormInventarioItens.cshtml` — `card-footer`
- `Areas/gc/Views/Movimentos/ModalPedidoViewAnexos.cshtml` — `modal-footer`
- `Areas/gc/Views/FinanceiroLancamentos/ModalFinanceiroViewAnexos.cshtml` — `modal-footer`
- `Areas/gc/Views/EstoqueLotes/ModalCreateEdit.cshtml` — `modal-footer` (ramo bloqueado)

**Problema / Demanda:**
Padronizar cor do «Sair» (`btnSair`) em rodapés: trocar `btn-info` por `btn-primary` (Bootstrap 5 / AdminLTE 4).

**O que foi feito:**
- Apenas o botão `name="btnSair"` em `card-footer` / `modal-footer`; sem alterar `onclick` / `data-bs-dismiss`.

**O que foi evitado e por quê:**
- `FormPedidoCreate` já estava `btn-primary`; CRM `Pedidos/Index` — `btnSair` em `row` (não footer semântico) e `outline-secondary`; `ModalViewFichaEstoqueProduto` — `btn-secondary` (fora do pedido `btn-info`).

---

### [2026-05-14] — Views: `text-sm-start` em colunas com checkbox + `<label class="form-check-label">` sob `text-sm-end`
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml` — aba Fornecedor Cotação (cinco colunas com switches + label)
- `Areas/gc/Views/Estoque/ModalConferenciaImportacaoItem.cshtml` — coluna «Lote nn Conferido?» (5 repetições)
- `Areas/gc/Views/Estoque/ModalConferenciaEstoqueItem.cshtml` — idem
- `Areas/gc/Views/Movimentos/ModalPedidoSeparacaoLotes.cshtml` — idem

**Problema / Demanda:**
Mesmo padrão do switch «Benefício Aviação»: linha com `text-sm-end` empurra o texto do label para longe do input quando checkbox e label estão na mesma `form-check`.

**O que foi feito:**
- `text-sm-start` na `div` coluna que envolve `form-check` + label (Bootstrap 5), alinhado ao que já existia em `ModalPedidoExpedicao` / checklist pós-venda em `FormPedidoCreate`.

**Impactos conhecidos:**
- Varredura por `form-check-label`: restantes usam `text-sm-start` na coluna, ou `span` + `d-inline-flex` (sem o mesmo bug).

---

### [2026-05-14] — Pedido (`FormPedidoCreate`): switch «Benefício Aviação» — `text-sm-start` na coluna
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`

**Problema / Demanda:**
Label do switch afastado do toggle: a linha usa `text-sm-end` (alinhamento de rótulos); o bloco do switch herdava `text-align: end`.

**O que foi feito:**
- `text-sm-start` na `div.col-sm-2` do switch (igual ao padrão já usado nos switches da checklist pós-venda na mesma view); neutraliza `text-sm-end` da linha.

---

### [2026-05-14] — Sidebar: `gdi-sidebar-nav.css` — folga ícone↔texto (1.125rem + `p` 0.25rem)
**Tipo:** Correção
**Arquivos tocados:**
- `Content/gdi-sidebar-nav.css`

**Problema / Demanda:**
Após afinação máxima (0.875rem + 0.125rem), ícones ficaram colados ao texto — aumentar um pouco o espaçamento.

**O que foi feito:**
- Nível 1 e `.nav-treeview`: coluna `.nav-icon` **1.125rem**; `p` com **0.25rem** `padding-left` (antes 0.875rem / 0.125rem).

---

### [2026-05-14] — Sidebar nível 1: `gdi-sidebar-nav.css` (mesma coluna 0.875rem + `p` 0.125rem)
**Tipo:** Implementação
**Arquivos tocados:**
- `Content/gdi-sidebar-nav.css`

**Problema / Demanda:**
Afinar também o espaço ícone↔texto nos itens de **nível 1** do menu lateral (antes só `.nav-treeview`).

**O que foi feito:**
- Overrides com `> .nav-item > .nav-link` a partir de `ul.sidebar-menu` (só ramo nível 1; subitens mantêm regras `.nav-treeview` existentes).
- Mesmos valores do submenu: coluna `.nav-icon` **0.875rem**; `p` **0.125rem** `padding-left`.

**O que foi evitado e por quê:**
- Seletores sem `>` em `.sidebar-menu` para nível 1 → evitar afetar outros `nav-link` fora da árvore principal.

**Atenção para próximas intervenções:**
- Se algum ícone FA de nível 1 parecer apertado, avaliar `min-width`/`max-width` ligeiramente acima de 0.875rem só nesse bloco.

---

### [2026-05-14] — Sidebar submenu: `gdi-sidebar-nav.css` (coluna ícone 0.875rem, só `.nav-treeview`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Content/gdi-sidebar-nav.css` (novo)
- `Views/Shared/_Layout.cshtml` — `<link>` após `gdi-section-cards.css`
- `GDI-ERP-Plataform.csproj` — `Content Include`

**Problema / Demanda:**
Reduzir espaço entre ícone (`nav-icon`) e texto nos itens do submenu lateral (AdminLTE 4 usa 1.5rem + `padding-left` 0.5rem no `p`).

**O que foi feito:**
- Override cirúrgico só em `.sidebar-menu .nav-treeview > .nav-item > .nav-link`: coluna `.nav-icon` **0.875rem**; `p` com **0.125rem** `padding-left` (o pedido de 0.875rem aplica-se à coluna do ícone).

---

### [2026-05-14] — Documentação: portal integrado no ERP; fim de referências ao repo GDI-PortalCliente-Plataform
**Tipo:** Refatoração (docs + comentários)
**Arquivos tocados:**
- `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` — §0 reescrita (repositório único; mapa `UserIdentity` / `Areas/crm`); §§2 e 8 sem segundo repo; frontmatter
- `CLAUDE.md` — secção «Portal do cliente (integrado neste ERP)»
- `Controllers/UserIdentityController.cs` — comentários `SetTenants` / `AcessoPortal`
- `Areas/gc/Controllers/MovimentosController.cs` — comentário `[LinkPortalDireto]`

**Problema / Demanda:**
O projeto **GDI-PortalCliente-Plataform** foi excluído; funcionalidades migradas para este ERP — remover ligações e instruções que pressupunham repositório irmão.

**O que foi feito:**
- Regras Cursor e `CLAUDE.md` passam a descrever o portal **só** dentro deste monólito; histórico de migração remete ao `CHANGELOG-DEV`.
- Comentários em código alinhados (URLs públicas = mesma app ERP).

**O que foi evitado e por quê:**
- Reescrever entradas antigas do `CHANGELOG-DEV` (mantêm-se como arquivo histórico).

---

### [2026-05-14] — `_Navbar`: menu utilizador portal só «Sair» + deteção por role
**Tipo:** Correção
**Arquivos tocados:**
- `Views/Shared/_Navbar.cshtml` — `isPortalClienteNav` via `CustomPrincipal.IsInRole("gc_PortalCliente_PortalFinanceiro")`; ramo portal com única opção «Sair do Sistema»

**Problema / Demanda:**
Perfil portal ainda via Ambiente/Empresa/Filial/Perfil/Versão no dropdown; deve ver **apenas** «Sair do Sistema».

**O que foi feito:**
- Deteção alinhada ao `[CustomAuthorize(Roles = "gc_PortalCliente_PortalFinanceiro")]` da área `crm`. Conteúdo do ramo portal reduzido a um único `<li>`.

---

### [2026-05-14] — `_Navbar`: portal (menu utilizador + sidebar) via `IdCliente`; menu lateral Pedidos
**Tipo:** Correção
**Arquivos tocados:**
- `Views/Shared/_Navbar.cshtml` — `isPortalClienteNav` (`Model.userIdentity.IdCliente > 0`); dropdown portal com Ambiente/Empresa/Filial/Perfil/Versão + Sair (sem Trocar Senha/Device)
- `Controllers/UserIdentityController.cs` — `PerfilNome` no login portal; após `allNavbarItemMenu.Clear()` itens sintéticos grupo «Portal do Cliente» + «Pedidos» (`crm`)

**Problema / Demanda:**
`ViewBag.PortalClienteLogin` não existe no `RenderAction` do Navbar → ramo portal do dropdown não aplicado como previsto; com `Clear()` do menu a sidebar ficava sem entradas (só «uma opção» ou vazio).

**O que foi feito:**
- Deteção de portal autenticado pela sessão (`IdCliente`), alinhada ao `CompletePortalClienteLogin`.
- Menu lateral mínimo para voltar a `Pedidos/Index` na área `crm`.

---

### [2026-05-14] — Portal cliente título: CS0103 em `_ViewStart` (`ViewBag` indisponível)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/crm/Views/_ViewStart.cshtml` — removido `ViewBag` (StartPage não expõe `ViewBag` em runtime/compilação)
- `Areas/crm/Views/Pedidos/Index.cshtml` — `ViewBag.PortalClienteBrowserTitle = true` no topo da view (WebViewPage)

**Problema / Demanda:**
Produção: `CS0103: The name 'ViewBag' does not exist in the current context` em `Areas/crm/Views/_ViewStart.cshtml`.

**O que foi feito:**
- `_ViewStart` só define `Layout`. O sinal para `<title>` do portal fica em `Pedidos/Index.cshtml` (executa antes do `_Layout` e tem `ViewBag`). Novas views `crm` com `_Layout` devem repetir a linha ou definir no respetivo controller.

---

### [2026-05-14] — Portal cliente: `<title>GDI - Portal do Cliente` (área `crm` + login portal)
**Tipo:** Implementação
**Arquivos tocados:**
- `Views/Shared/_Layout.cshtml` — `<title>` condicional com `ViewBag.PortalClienteBrowserTitle`
- `Areas/crm/Views/Pedidos/Index.cshtml` — `ViewBag.PortalClienteBrowserTitle = true` (após correção CS0103; não usar `ViewBag` em `_ViewStart`)
- `Views/UserIdentity/Index.cshtml` — título do documento quando `portalClienteLogin`
- `Areas/crm/Views/Pedidos/BoletoPdfFebraban.cshtml` — `<title>` (view `Layout = null`)

**Problema / Demanda:**
No perfil portal, o título do browser era o mesmo do staff (`aeroflightx.com - versão`); deve ser `GDI - Portal do Cliente` só nas páginas desse perfil, sem alteração global desnecessária.

**O que foi feito:**
- `_Layout` altera apenas a linha do `<title>` quando `ViewBag.PortalClienteBrowserTitle` está ativo na view que corre antes do layout.
- Login portal (`UserIdentity/Index` com `ViewBag.PortalClienteLogin`) e PDF boleto (`Layout = null`) com título explícito.

**O que foi evitado e por quê:**
- Duplicar `_Layout` inteiro só para o portal.

---

### [2026-05-14] — Publish Web: target `GdiStripPackageTmpDotCursorBeforeWppCopy` (aviso `context` / RemoveEmptyDirectories)
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — target MSBuild `BeforeTargets="CopyAllFilesToSingleFolderForMsdeploy;CopyAllFilesToSingleFolderForPackage"` (Windows): remove `$(WPPAllFilesInSingleFolder)\.cursor` com `attrib` + `rmdir`

**Problema / Demanda:**
Aviso persistente `Microsoft.Web.Publishing.targets(2693,5)` — *Access to the path 'context' is denied* em `RemoveEmptyDirectories`, mesmo com `ExcludeFoldersFromDeployment` / `ExcludeFromPackageFolders` para `.cursor`.

**O que foi feito:**
- Restos em `obj\...\PackageTmp\.cursor` (ex. `context` só leitura ou bloqueado pelo IDE) impedem apagar pastas vazias. O target corre **antes** da cópia WPP e força remoção recursiva da pasta `.cursor` no `PackageTmp` em `Windows_NT`.

**O que foi evitado e por quê:**
- Não alterar targets Microsoft; extensão só no `.csproj` do projeto.

**Atenção para próximas intervenções:**
- Build/publish em não-Windows: o target não corre (`OS` ≠ `Windows_NT`); manter limpeza manual de `PackageTmp` se necessário.

---

### [2026-05-14] — Análise: timeout / logout → login portal vs staff (`UserIdentity/Index`)
**Tipo:** Análise
**Arquivos tocados:**
- (nenhum — apenas verificação de código)

**Problema / Demanda:**
Confirmar se, em timeout de sessão, utilizador no **contexto portal do cliente** volta ao **login de portal** e não ao login staff.

**O que foi verificado:**
- `CustomAuthorizeAttribute` (não-Ajax) e `UserIdentityController.Logout` (inatividade via `sessionInactivity.js`) redirecionam para **`UserIdentity/Index`** no **mesmo host** da requisição.
- O GET `UserIdentityController.Index()` define `ViewBag.PortalClienteLogin = IsPortalClienteHost(GetHostWithoutPort(Request))`, logo em FQDN reconhecido (ex.: `*.portalflightx.com`, regras `portalflightx`/`.local`) a view `Views/UserIdentity/Index.cshtml` já apresenta o fluxo de portal.
- AJAX 401: `gdi-session-handler.js` usa `window.GDI_LoginUrl` (`_Layout`: `Url.Action("Index","UserIdentity")`) — URL relativa ao origin atual; mesmo comportamento.
- **Limite:** se o portal for acedido por host **não** coberto por `IsPortalClienteHost` (ex.: `localhost` puro, conforme decisão já registada), o login continua o de staff — por desenho. `X-Forwarded-Host` não está tratado no código; atrás de proxy que altere `Host`, validar cabeçalhos no IIS.

---

### [2026-05-14] — CRM `Pedidos/Index`: callout verde + UI alinhada AdminLTE 4 / área `g`
**Tipo:** Correção + Refatoração (UI)
**Arquivos tocados:**
- `Areas/crm/Views/Pedidos/Index.cshtml`

**Problema / Demanda:**
`callout callout-success` com `h-100` pintava todo o painel com fundo verde (AdminLTE 4 aplica `--lte-callout-bg` em todo o bloco). Modernizar cabeçalhos, cartões e botões ao padrão já usado (ex.: `PortalFinanceiro`).

**O que foi feito:**
- Substituídos callouts por `card card-outline card-secondary` (dados) e `card card-outline card-primary` (documentos): destaque só na borda superior, corpo neutro.
- Removidos estilos inline `#EAEDED` / `#aeb6bf`; uso de `bg-body-secondary`, `bg-body-tertiary`, `text-secondary`, `text-muted`, `border`, `shadow-sm`, espaçamento `g-*` / `mb-3`.
- Botões: `btn-outline-secondary` + `d-inline-flex align-items-center gap-2` para NF/XML; `btn-outline-info` para boletos; `btn-outline-secondary` no Sair (antes `btn-info`).

**O que foi evitado e por quê:**
- Manter `callout-success` com overrides CSS locais — preferível usar componentes AdminLTE 4 já previstos (`card-outline`).

**Impactos conhecidos:**
- Apenas visual/semântica de classes; JS e rotas inalterados.

---

### [2026-05-14] — Publish Web: excluir pasta `.cursor` do pacote (aviso `context` / RemoveEmptyDirectories)
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — `ExcludeFoldersFromDeployment` = `.cursor`; `ItemGroup` `ExcludeFromPackageFolders` para `.cursor`

**Problema / Demanda:**
Aviso em `Microsoft.Web.Publishing.targets(2693,5)`: *Access to the path 'context' is denied* (tarefa `RemoveEmptyDirectories` sobre `PackageTmp`, frequentemente ligado a `.cursor\context`).

**O que foi feito:**
- Exclusão explícita da pasta `.cursor` do pipeline de packaging Web (alinhado a `ProcessItemToExcludeFromDeployment` / `ExcludeFilesFromPackage` nos targets Microsoft), para não copiar nem deixar subpastas problemáticas no `PackageTmp`.

**Atenção para próximas intervenções:**
- Se o aviso continuar após um publish antigo, apagar `obj\<Config>\Package` (ou `PackageTmp`) e voltar a publicar.

---

### [2026-05-14] — Publish Web: `.cursor` em `Content` causava falha ao copiar `PackageTmp\.cursor\rules`
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — `.cursor\CHANGELOG-DEV.md`, `.cursor\context\2026_05_20_migracao-472-481.md` e regra Cursor passam de `Content` para `None`; `.cursor\rules` substituído por `.cursor\rules\2026_05_20_gdi-erp-plataform.mdc`

**Problema / Demanda:**
`Copying file .cursor\rules to obj\Release\Package\PackageTmp\.cursor\rules failed. Access to the path '.cursor\rules' is denied.`

**O que foi feito:**
- Ficheiros só de desenvolvimento não devem ir como `Content` para o pacote IIS. `None` mantém-os no projeto no Visual Studio sem os copiar no publish.

**Atenção para próximas intervenções:**
- Não voltar a marcar `.cursor\**` como `Content` salvo requisito explícito de entrega no site publicado.

---

### [2026-05-14] — Antlr3: `Antlr3 (1).Runtime.dll` em `bin` (FileLoadException 0x80131040)
**Tipo:** Correção
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj` — target `RemoveMisnamedAntlrDuplicatesFromBin` após `CopyFilesToOutputDirectory`
- `bin\Antlr3 (1).Runtime.dll` / `.pdb` — removidos na cópia de trabalho (cópia duplicada do Explorer)

**Problema / Demanda:**
IIS Express / ASP.NET falha ao iniciar: não carrega `Antlr3 (1).Runtime` — manifesto incompatível com a referência (HRESULT 0x80131040).

**O que foi feito:**
- Causa raiz: ficheiros `Antlr3 (1).Runtime.dll` (e `.pdb`) em `bin` ao lado de `Antlr3.Runtime.dll` (nome típico de “cópia (1)” no Windows). O runtime tenta carregar todos os `.dll` de `bin`; o nome do ficheiro não corresponde ao assembly identity interno.
- Target MSBuild remove padrões `Antlr3.Runtime (*).dll` e `Antlr3 (*).Runtime*.dll` após cada build para evitar recorrência.

**Atenção para próximas intervenções:**
- Se o erro persistir, apagar também a pasta temporária do ASP.NET para este site em `%LOCALAPPDATA%\Temp\Temporary ASP.NET Files\` (subpasta do VS/IIS correspondente).

---

### [2026-05-14] — Unificação portal do cliente: área `crm`, `AcessoPortal`, tenant `portalflightx`
**Tipo:** Implementação
**Arquivos tocados:**
- `Controllers/UserIdentityController.cs` — `AcessoPortal` (GET), `CompletePortalClienteLogin`, `SetTenants` (+ `portalflightx` → `GdiPlataformEntities_gdi_producao`), deteção `IsPortalClienteHost` / `GetHostWithoutPort`, POST `Index` com ramo portal em hosts `*.portalflightx.com` (e `portalflightx`/`.local` para dev)
- `Views/UserIdentity/Index.cshtml` — formulário portal (código + CPF/CNPJ) vs staff; `LibMessage*` no portal
- `Models/UserIdentity.cs` — `IdCliente`, `ClienteIdentificador`, `ClienteCpfCnpj`
- `Security/CustomPrincipal.cs` — `Roles` nulo não quebra `IsInRole`
- `Areas/crm/**` — `crmAreaRegistration`, `PedidosController`, `GlobalController`, models `cstListaPedidosPortal` / `cstDadosPedidoPortal`, views `Pedidos/Index`, `Pedidos/BoletoPdfFebraban` (modelo `Areas.g.Models.cstFinanceiroBoletos`), `Views/web.config`, `_ViewStart`
- `GDI-ERP-Plataform.csproj` — `Compile` + `Content` da área `crm`

**Problema / Demanda:**
Unificar o portal do cliente no monólito ERP (URLs públicas `UserIdentity/AcessoPortal`, `/crm/Pedidos/Index`, AJAX boleto, download), modernizar UI/JS ao padrão AdminLTE 4 + BS5 + `start.js`, e mapear tenant `portalflightx` com as mesmas connection names usadas no repositório Portal.

**O que foi feito:**
- Área MVC `crm` com paridade de rotas (`crm/{controller}/{action}`), autorização `[CustomAuthorize(Roles = "gc_PortalCliente_PortalFinanceiro")]` em `Pedidos` e `Global`.
- `AjaxFinanceiroBoletoGCPDF` e PDF via Rotativa alinhados ao código do Portal; modelo de boleto reutiliza `GdiPlataform.Areas.g.Models.cstFinanceiroBoletos` e `LibFinanceiroBoletos` em `Areas.g.Lib`.
- `GlobalController.AjaxGetFileProcessamento` com guard de `db` nulo (padrão área `g`).
- Login portal: hosts cujo FQDN termina em `portalflightx.com`, ou contém `portalflightx` e termina em `.local` / host `portalflightx` (IIS Express / hosts file); **não** força login portal em `localhost` puro (mantém login staff local).

**Decisões técnicas relevantes:**
- `homologacao.portalflightx.com` continua a resolver primeiro segmento `homologacao` → `GdiPlataformEntities_gdi_homologacao` (já existente no ERP), alinhado ao DNS unificado staff/portal no mesmo IIS.
- Sessão portal: `IdPerfil = -900`, `TokenAcesso` = `C{idCliente}`, role `gc_PortalCliente_PortalFinanceiro`, `GoogleTag` / `GoogleTagURL` corretos (corrige dupla atribuição ao campo `GoogleTag` do Portal legado).

**O que foi evitado e por quê:**
- Não alterar `MovimentosController` (link `LinkPortalDireto` já aponta para `UserIdentity/AcessoPortal`); não duplicar `cstFinanceiroBoletos` na área `crm`.

**Impactos conhecidos:**
- Staff em `aeroflightx` / `localhost` inalterado; build Debug OK.

**Atenção para próximas intervenções:**
- **Segurança:** `AcessoPortal` e validações por CPF/CNPJ + pedidos continuam passíveis de enumeração; rate limit fora de escopo.
- Publicar: confirmar bindings IIS para `portalflightx.com` e `homologacao.portalflightx.com` na mesma app; incrementar versão se alterar `start.js` (não alterado nesta entrega).

---

### [2026-05-14] — Regras Cursor: relação ERP ↔ Portal + manutenção do `CLAUDE.md`
**Tipo:** Implementação
**Arquivos tocados:**
- `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alinhar o ficheiro de regras do ERP ao trabalho já feito no repositório **GDI-PortalCliente-Plataform** (`.cursor/rules`): caminhos absolutos dos dois produtos, separação estrita de stack por repo, e instruções de atualização do `CLAUDE.md` equivalentes às do Portal.

**O que foi feito:**
- Nova secção **§0** com tabela de pastas (`C:\Marcio\Projetos\GDI-ERP-Plataform` vs `GDI-PortalCliente-Plataform`), aviso de não misturar tecnologias, onde vive cada `.mdc`/`CLAUDE.md`, manutenção sem drift e tabela de aplicabilidade (o que nas regras do Portal **não** vale para o ERP).
- **§2** alargada com ponto **4** sobre manutenção do `CLAUDE.md` (memória longa, `#` no Claude Code, não copiar secções longas do Portal; CHANGELOG para intervenções pontuais).
- **§8** com remessa explícita à stack do Portal apenas quando o trabalho for na pasta do Portal.
- `description` do frontmatter atualizada para mencionar a relação com o Portal.

**Decisões técnicas relevantes:**
- Espelhar simetria conceitual ao ficheiro `2026_05_20_gdi-erp-plataform.mdc` **dentro** do repo Portal (contexto cruzado), sem duplicar listas técnicas do Portal no ERP.

**O que foi evitado e por quê:**
- Não editar ficheiros no diretório do Portal a partir desta intervenção → fora do escopo e da política de workspace do ERP.

**Impactos conhecidos:**
- Agentes em workspace só-ERP ficam com caminho canónico do irmão e regra clara de não importar stack do Portal.

**Atenção para próximas intervenções:**
- Se a política comum (CHANGELOG / ambiente) mudar num produto, replicar manualmente no outro conforme §0.1.

---

### [2026-05-14] — Reestruturação de `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` (frontmatter + deduplicação)
**Tipo:** Refatoração
**Arquivos tocados:**
- `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc`

**Problema / Demanda:**
O ficheiro de regras do Cursor não seguia o formato `.mdc` com frontmatter YAML; repetia instruções (CHANGELOG, metodologia); o nome do repositório na restrição de workspace estava incorreto (`GDI-PortalCliente-Plataform`).

**O que foi feito:**
- Adicionado frontmatter (`description`, `alwaysApply: true`).
- Reorganizado em secções numeradas: identidade, ordem de leitura, restrições, metodologia, registo CHANGELOG, formato de resposta, lembretes, limites da regra.
- Corrigido o nome do repositório nas restrições de workspace; tabela única para git/deploy/infra.
- Reduzida duplicação com `CLAUDE.md` (referência explícita em vez de repetir fases DataTables); checklist longo de publish fundido na metodologia §4.5.
- Secção 6 alinhada a `CLAUDE.md` (tabela de ordem das respostas).

**Decisões técnicas relevantes:**
- Manter `alwaysApply: true` para regra global do monorepo ERP.
- Não duplicar template completo do CHANGELOG no `.mdc` — remeter ao formato já documentado no próprio `CHANGELOG-DEV.md`.

**O que foi evitado e por quê:**
- Não copiar para o `.mdc` listas extensas de fases/controllers já em `CLAUDE.md` → tokens e drift entre ficheiros.

**Impactos conhecidos:**
- Agentes passam a carregar metadados da regra no picker do Cursor; conteúdo mais curto favorece aderência.

**Atenção para próximas intervenções:**
- Se `CLAUDE.md` e este `.mdc` divergirem, atualizar ambos ou concentrar detalhe só num deles e manter o outro como índice.

---

### [2026-05-13] — S3: atualização de credenciais em `aws-s3.local.json` (gitignored)
**Tipo:** Implementação
**Arquivos tocados:**
- `App_Data/Secrets/aws-s3.local.json` (gitignored; não versionado)

**Problema / Demanda:**
Atualizar access key / secret access key do IAM S3 no ficheiro local de credenciais.

**O que foi feito:**
- Preenchido `aws-s3.local.json` com as novas chaves; região e buckets mantidos (`sa-east-1`, `bucket-erp-gdi`, `bucket-gdi-public-files`).

**O que foi evitado e por quê:**
- Não alterar `aws-s3.local.json.example` (continua só com placeholders no repositório).

---

### [2026-05-13] — SES: um ficheiro de runtime + modelo `aws-ses-smtp.template.json` (remove `.local.json.example`)
**Tipo:** Refatoração
**Arquivos tocados:**
- `App_Data/Secrets/aws-ses-smtp.template.json` (novo, versionado, sem segredos)
- Removido `App_Data/Secrets/aws-ses-smtp.local.json.example` (confundia com o ficheiro gitignored)
- `GDI-ERP-Plataform.csproj`, `.gitignore`, `Robos/Aws/GdiAwsSesSmtpCredentials.cs`, `.cursor/CHANGELOG-DEV.md` (referências históricas alinhadas)

**Problema / Demanda:**
Dois nomes SES pareciam ambos “credenciais”; clarificar: só `aws-ses-smtp.local.json` tem segredos em runtime.

**O que foi feito:**
- Modelo renomeado para **`aws-ses-smtp.template.json`**; runtime continua **`aws-ses-smtp.local.json`** (gitignored).

---

### [2026-05-13] — SES SMTP: credenciais locais + região São Paulo (`sa-east-1`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsSesSmtpCredentials.cs` — constante `AwsSesSmtpRegionSaoPaulo` = `sa-east-1`; `ResolveSmtpHost`; env opcional `AWS_SES_SMTP_REGION`; campo JSON opcional `SmtpRegion`
- `App_Data/Secrets/aws-ses-smtp.local.json` (gitignored, runtime)

**Problema / Demanda:**
Credenciais SES e região São Paulo; endpoint omissão `email-smtp.sa-east-1.amazonaws.com`.

**O que foi feito:**
- Resolução via variáveis de ambiente ou JSON local; modelo versionado posteriormente renomeado para `aws-ses-smtp.template.json` (ver entrada acima).

**Atenção para próximas intervenções:**
- Se a conta SES estiver noutra região, definir `SmtpHost` ou `SmtpRegion` / `AWS_SES_SMTP_REGION` em conformidade.

---

### [2026-05-13] — S3: regras por bucket (ERP vs público) centralizadas e aplicadas no GED/BotAws
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsS3BucketRules.cs` (novo) — lista branca de buckets (`ResolveBucketErp` / `ResolveBucketPublicFiles`); GED privado não grava no bucket público; validação de `public_url`; consistência `ged_arquivos.bucket` vs `ged_arquivos_tipos.bucket_s3`
- `Robos/Aws/BotAwsS3.cs` — `ValidateGedUpload` no upload; `ThrowIfBucketNotAllowed` em `BuildPublicObjectUrl`
- `Areas/g/Controllers/GedController.cs` — downloads contrato/arquivo: validações antes de presign / devolução de URL pública
- `Areas/qa/Controllers/TreinamentosController.cs` — presign LMS só após validação do bucket ERP
- `GDI-ERP-Plataform.csproj`

**Problema / Demanda:**
Garantir regras explícitas de leitura/gravação por bucket (`bucket-erp-gdi` vs `bucket-gdi-public-files`).

**O que foi feito:**
- **Gravação:** `BotAwsS3.UploadStreamS3` chama `ValidateGedUpload` — anexo privado (`publicRead == false`) recusa bucket público.
- **Leitura GED:** presigned só com bucket na lista branca; `public_url` só aceita HTTPS virtual-hosted (e variante dualstack) para um dos dois buckets; deteta divergência tipo vs registo.
- **LMS:** bucket de presign validado contra a lista branca (usa `ResolveBucketErp()`).

**O que foi evitado e por quê:**
- Não alterar dezenas de URLs estáticas nas views (bucket público já fixo em HTML); escopo mantém-se no SDK/presign/GED.

**Atenção para próximas intervenções:**
- URLs `public_url` fora do padrão virtual-hosted (ex.: CloudFront) exigem alargar `TryValidateStoredPublicUrl`.

---

### [2026-05-13] — S3: buckets GDI no modelo local + `ResolveBucketErp` / `ResolveBucketPublicFiles`
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsS3Credentials.cs` — campos opcionais `BucketErp` / `BucketPublicFiles` no JSON; resolução por `AWS_S3_BUCKET_ERP` / `AWS_S3_BUCKET_PUBLIC`; omissões = `bucket-erp-gdi` / `bucket-gdi-public-files`
- `App_Data/Secrets/aws-s3.local.json.example` — alinhado ao IAM **GDI-User-S3-ERP-AppAndPublicFiles-Access** e buckets oficiais (sem segredos no repo)
- `Areas/qa/Controllers/TreinamentosController.cs` — bucket do presign via `ResolveBucketErp()`

**Problema / Demanda:**
Documentar e centralizar buckets do utilizador S3 dedicado ao ERP e ficheiros públicos, mantendo access/secret só em ficheiro local ou env.

**O que foi evitado e por quê:**
- Não gravar access key / secret no repositório nem no chat em ficheiros versionados.

---

### [2026-05-13] — AWS SES SMTP: credenciais fora do código (`aws-ses-smtp.local.json` + env)
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsSesSmtpCredentials.cs` (novo)
- `Robos/Aws/BotAwsEmail.cs` — lê credenciais via resolver; `UseDefaultCredentials = false` para autenticação explícita
- `App_Data/Secrets/aws-ses-smtp.template.json` (modelo versionado; nome final após refactor — antes `.example`)
- `.gitignore` — `App_Data/Secrets/aws-ses-smtp.local.json`
- `Lib/LibEmail.cs` — bloco comentado: removidos segredos em texto claro (referência ao resolver)
- `GDI-ERP-Plataform.csproj`

**Problema / Demanda:**
Credenciais SES SMTP não podem permanecer hardcoded; alinhar ao padrão já usado para S3.

**O que foi feito:**
- **`GdiAwsSesSmtpCredentials.Resolve()`**: `AWS_SES_SMTP_USERNAME` / `AWS_SES_SMTP_PASSWORD` (opcionais `AWS_SES_SMTP_HOST`, `AWS_SES_SMTP_PORT`, `AWS_SES_SMTP_REGION`) ou JSON local **`App_Data/Secrets/aws-ses-smtp.local.json`** com `SmtpHost`, `SmtpPort`, `SmtpUsername`, `SmtpPassword`, `SmtpRegion` opcional.

**Atenção para próximas intervenções:**
- S3 e SES podem ser IAM users distintos: ficheiros **`aws-s3.local.json`** e **`aws-ses-smtp.local.json`** separados.

---

### [2026-05-13] — AWS S3: credenciais fora do código (env + `aws-s3.local.json` gitignored)
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/Aws/GdiAwsS3Credentials.cs` (novo)
- `Robos/Aws/BotAwsS3.cs`
- `Areas/g/Controllers/GedController.cs`
- `Areas/qa/Controllers/TreinamentosController.cs` (mesmas chaves duplicadas no repositório)
- `App_Data/Secrets/aws-s3.local.json.example` (novo, versionado)
- `.gitignore` — `App_Data/Secrets/aws-s3.local.json`
- `GDI-ERP-Plataform.csproj` — compile + Content do `.example`

**Problema / Demanda:**
Chaves AWS hardcoded em ficheiros que não podem ir ao GitHub; solução alinhada ao mercado com ficheiro local.

**O que foi feito:**
- Classe **`GdiAwsS3Credentials`**: lê **`AWS_ACCESS_KEY_ID`** / **`AWS_SECRET_ACCESS_KEY`** (e opcionalmente **`AWS_REGION`** / **`AWS_DEFAULT_REGION`**); se faltarem, lê **`~/App_Data/Secrets/aws-s3.local.json`** (JSON com `AccessKeyId`, `SecretAccessKey`, `Region` opcional; omissão de região → `sa-east-1`).
- **`BotAwsS3`** e **`GedController`**: usam `CreateS3Client()` / `ResolveRegion()`; modelo `.example` no repositório; ficheiro real gitignored.
- **`TreinamentosController`**: removidas constantes duplicadas; mesmo resolvedor (evita secrets no `qa`).

**Decisões técnicas relevantes:**
- Prioridade env → ficheiro local (padrão AWS CLI + secrets locais comuns em ASP.NET).

**O que foi evitado e por quê:**
- Novas dependências NuGet; uso de `Newtonsoft.Json` já referenciado.

**Impactos conhecidos:**
- Dev/IIS: criar `aws-s3.local.json` a partir do `.example` **ou** definir variáveis de ambiente no pool IIS.
- Chaves que já estiveram no Git: **revogar/rodar no IAM** e substituir por novas.

**Atenção para próximas intervenções:**
- Em produção preferir env vars ou perfil IAM na máquina em vez de JSON em disco quando possível.

---

### [2026-05-13] — Select2 lotes em modais (conferência importação, separação pedido, abas + `start.js`)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/Estoque/ModalConferenciaImportacaoItem.cshtml`
- `Areas/gc/Views/Estoque/ModalConferenciaEstoqueItem.cshtml`
- `Areas/gc/Views/Movimentos/ModalPedidoSeparacaoLotes.cshtml`
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js`

**Problema / Demanda:**
Mesmo contexto do fix já testado em `ModalConferenciaEstoqueItem`: `ComboEstoqueLotes` + Select2 + abas + modal; replicar e evitar Select2 em painéis ocultos.

**O que foi feito:**
- `ModalConferenciaImportacaoItem`: CSS `overflow-x` no form, mesmos atributos nos 5 `DropDownList`, `gdiConferenciaLotesTabsSelect2Init(..., '#mainModal')` após `jsInitModal`.
- `ModalConferenciaEstoqueItem`: chamada `gdiConferenciaLotesTabsSelect2Init('#ModalConferenciaEstoqueItem', '#mainModal')` após `jsInitModal`.
- `ModalPedidoSeparacaoLotes`: CSS no form, `data_select2_dropdown_parent = "#containerModalPedidoSeparacaoLotes"`, handlers e `gdiConferenciaLotesTabsSelect2Init` com esse parent.
- `start.js`: função `gdiConferenciaLotesTabsSelect2Init` — destrói Select2 nas `.tab-pane` inativas, reinicia no painel ativo; `shown.bs.tab` ligado às âncoras `pill`/`tab`.

**Decisões técnicas relevantes:**
- Evento de aba nas âncoras (não delegação no `<ul>`), pois `shown.bs.tab` pode não propagar.

**O que foi evitado e por quê:**
- Alteração em massa de outros modais com Select2 sem o mesmo combo de lotes/abas.

**Impactos conhecidos:**
- Depende de `gdiDestroySelect2OnCollection` / `gdiInitSelect2OnCollection` (`gdi-select2.js`), já carregados nas telas que usam estes partials.

**Atenção para próximas intervenções:**
- Novo modal com o mesmo padrão de abas de lotes: passar o seletor do form e o `dropdownParent` correto (`#mainModal` vs container dedicado).

---

### [2026-05-13] — Estoque: `ModalConferenciaEstoqueItem` — Select2/modal (dropdown lotes)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/Estoque/ModalConferenciaEstoqueItem.cshtml`

**Problema / Demanda:**
DropDownList de lotes no modal de conferência não permitia seleção/busca (Select2 + `modal-dialog-scrollable` / overflow), padrão já usado em `ModalPedidoInsertEditItem`.

**O que foi feito:**
- Nos 5 blocos de `DropDownList` de lote: `data-select2-dropdown-parent="#mainModal"`, `jsHandleSelectOpen` / `jsHandleSelectClose` / `onmousedown`, classe `select-modal-fix`, `data-container="body"`.
- CSS `#ModalConferenciaEstoqueItem .modal-body { overflow-x: visible; }`.

**O que foi evitado e por quê:**
- Extensão imediata a importação/separação — feita em entrada seguinte do histórico após validação do padrão.

**Impactos conhecidos:**
- Conteúdo continua a ser carregado em `#mainModal` (`FormRecebimentoItensEstoque`).

---

### [2026-05-13] — Fase 17: Ajax NFe + RoboEnotasNFE, `GetDadosGedAtendimento`, modais com `id_nfe`, export/import/processamento
**Tipo:** Implementação
**Arquivos tocados:**
- `Robos/ENotas/RoboEnotasNFE.cs` — `AtualizarStatusNFPbyMovimentoNFId`, `CancelarNFPbyMovimentoNFId`, `GerarNFServicoByMovimentoNFId`, `AtualizarStatusG_nfePorId`, `CancelarG_nfePorId` (gateway e-Notas para `g_nfe` com `nfe_key` ou vínculo via `gc_movimentos_nf`)
- `Areas/g/Controllers/NfeController.cs` — Ajax: clonar `g_nfe`, cancelar / enviar cancelamento, e-mail unitário (CSV + `g_processamento` alinhado a `FinanceiroFaturamentos`), export período, gerar serviço, atualizar status, sincronizar lote (máx. 200), import arquivo; helpers `ResolverMovimentoNfParaGNfe`, `SincronizarGNfeComMovimentoNf`
- `Areas/g/Views/Nfe/Index.cshtml`, `ModalGerarNfe.cshtml`, `ModalAtualizarStatusNfe.cshtml`, `ModalEnviarCancelamentoNfe.cshtml`, `modalCancelarNfe.cshtml` — seleção única na grade + `id_nfe` nos POSTs
- `Areas/g/Controllers/AtendimentosController.cs` — action **`GetDadosGedAtendimento`** (ex-`GetGedAtendimento`)
- `Areas/g/Views/Atendimentos/Edit.cshtml` — URL DataTables GED
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 17: acionar Ajax NFe com `RoboEnotasNFE` / padrão de processamento de `FinanceiroFaturamentos`; alinhar nome do endpoint GED em atendimentos ao contrato `GetDados*`; validar publish/deps e plano de migração.

**O que foi feito:**
- **Robô:** wrappers por `id_movimento_nf` (mesmo contexto EF do Robô) e métodos para **atualizar/cancelar** usando `g_nfe.nfe_key` quando não há `gc_movimentos_nf` resolvido pelo financeiro.
- **NfeController:** fluxos Ajax reais com `try/catch` e mensagens; e-mail unitário e export geram arquivo em `_filestemp` + `g_processamento` (tipos **49** export, **50** import lote); import grava cópia do upload e relatório texto (integração XML futura).
- **Atendimentos:** renomeação para **`GetDadosGedAtendimento`** mantendo JSON DataTables.
- **Modais NFe:** `ModalExportarDadosNfePDF` passa `cstExportacaoDadosNFEModel` (corrige modelo da view).

**Decisões técnicas relevantes:**
- Prioridade: `g_financeiro.id_financeiro_movimento` → último `gc_movimentos_nf` do movimento; senão API só com `g_nfe.nfe_key`.
- Lote sincroniza no máximo **200** `id_nfe` mais recentes com `nfe_key` preenchido.

**O que foi evitado e por quê:**
- Não implementar parser XML de import em lote sem especificação — apenas recepção e registro em processamento.

**Impactos conhecidos:**
- `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` → **0** lacunas.
- `python Scripts/2026_05_20_gdi_inventory_datatables_g_area.py` → **32** métodos `GetDados*` (inclui `GetDadosGedAtendimento`).

**Atenção para próximas intervenções:**
- Homologar em ambiente real: `GerarNFServico` exige item de serviço único no movimento; validar `id_processamento_tipo` 49/50 na tabela de tipos se houver FK/restrição.
- **Fase 18 (opcional):** testes manuais e-Notas + ajuste de mensagens/roles `g_PortalVendedor_*`; opcional `param` nulo explícito em `Nfe.GetDados` para heurística do script.

---

### [2026-05-13] — Fase 16: controllers ausentes `Nfe` + `PortalVendedor`, `xhr.dt` logs, `JsonDataTableException` em `Atendimentos`, correção `.csproj` Lib `g`
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/NfeController.cs` — **novo**: `Index`, `Edit` (GET/POST), `GetDados`, `GetDadosNfeLogs`, `ModalFiltroAvancadoView`, modais NFe, Ajax com resposta JSON explícita (integração e-Notas pendente de gateway)
- `Areas/g/Controllers/PortalVendedorController.cs` — **novo**: `PortalFinanceiro`, `GetDados` (carteira vendedor `IdPerfil == -800`), `JsonDataTableException`
- `Areas/g/Controllers/AtendimentosController.cs` — `JsonDataTableException` privado; `catch` dos endpoints DataTables delegando ao método
- `Areas/g/Views/PortalVendedor/PortalFinanceiro.cshtml` — **`xhr.dt`** + `GdiDtNotifyJsonErrorMessage`
- `Areas/g/Views/Nfe/CreateEdit.cshtml` — **`xhr.dt`** na tabela de logs
- `GDI-ERP-Plataform.csproj` — `<Compile Include>` de **`NfeController`**, **`PortalVendedorController`**; caminhos **`Areas\g\Models\Lib\LibFinanceiro*.cs`** (ficheiros existentes; corrigem build quebrado por paths antigos `Areas\g\Lib\`)
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 16: eliminar rotas 404 das views **`Nfe`** e **`PortalVendedor`**; alinhar notificação JSON nas grids tocadas; desbloquear compilação MSBuild por referências erradas a `LibFinanceiro`.

**O que foi feito:**
- **`NfeController`**: listagem `g_nfe` com filtros (persistido `getFilterByUser`, avançado 8 campos, genérico), colunas compatíveis com `Index.cshtml` (incl. rótulos **Autorizada** / **Cancelada** e sanitização de vírgulas para `JsGetSelectedRows`); logs `g_nfe_logs`; modais devolvem views existentes; Ajax devolve `success: false` com mensagem até integração com Robô/gateway.
- **`PortalVendedorController`**: `g_financeiro` + `g_clientes` com filtro opcional por vendedor; filtro genérico com `setFilterByUser` alinhado a `g_Financeiro`.
- **`AtendimentosController`**: DRY no erro DataTables.
- **Publish:** `.csproj` atualizado para novos controllers e paths reais de `LibFinanceiro`.

**Decisões técnicas relevantes:**
- Ajax NFe não invoca `RoboEnotasNFE` sem revisão funcional — evita efeitos colaterais em produção; mensagem JSON orienta suporte/faturamento.

**O que foi evitado e por quê:**
- Não duplicar centenas de linhas de integração e-Notas nesta fase.

**Impactos conhecidos:**
- `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` → **0** lacunas após alteração em `CreateEdit` NFe.
- `python Scripts/2026_05_20_gdi_inventory_datatables_g_area.py` → **31** métodos `GetDados*` (inclui `Nfe` + `PortalVendedor` + `GetDadosNfeLogs`); `GetGedAtendimento` fora do padrão de nome.

**Atenção para próximas intervenções:**
- **Fase 17:** ligar Ajax NFe (`AjaxClonarNfe`, cancelamento, e-mail, etc.) a `RoboEnotasNFE` / regras já usadas em `FinanceiroFaturamentos`; revisar roles reais de `g_PortalVendedor_*` em produção; opcional `param` nulo explícito em `Nfe.GetDados` (heurística script).

---

### [2026-05-13] — Fase 15: servidor DataTables — `Produtos`, `CentrosCustos`, `ClassificacaoFinanceira`, `Atendimentos` (atividades/logs/GED)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/ProdutosController.cs` — `GetDados`: `param` nulo, `try/catch`, JSON sucesso com `errorMessage`/`stackTrace` vazios, **`JsonDataTableException`**
- `Areas/g/Controllers/CentrosCustosController.cs` — `getDados`: idem + `yesFilterOnOff` quando filtro genérico; nome do pai sem NRE (`Find` nulo)
- `Areas/g/Controllers/ClassificacaoFinanceiraController.cs` — `getDados`: idem + nome do pai sem NRE
- `Areas/g/Controllers/AtendimentosController.cs` — `getDadosAtendimentos` / **`getDadosAtividades`** / **`getDadosAtendimentosLogs`** / **`GetGedAtendimento`**: sucesso com `errorMessage`/`stackTrace` vazios; `yesFilterOnOff` em atividades; NRE evitada (`FirstOrDefault` usuário GED, `Find` operador atividade); logs com `filterOnOff` no catch (**`JsonDataTableException`** consolidado na **Fase 16**).
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 15 (planejada na Fase 14): fechar o lote `GetDados`/`getDados*` pendente em cadastros e atendimentos, alinhando contrato DataTables no servidor.

**O que foi feito:**
- Padrão Fases 13–14 aplicado onde faltava; **`JsonDataTableException`** em `Produtos`, `CentrosCustos`, `ClassificacaoFinanceira` (métodos `getDados` ainda usados por integrações/API; telas atuais são jsTree).
- `AtendimentosController` — contrato de sucesso alinhado + correções de NRE; método **`JsonDataTableException`** na **Fase 16**.

**Decisões técnicas relevantes:**
- Controllers **`Nfe`** / **`PortalVendedor`** ausentes nesta entrega — entregues na **Fase 16**.

**O que foi evitado e por quê:**
- Não criar **`NfeController`** / **`PortalVendedorController`** sem inventário completo de actions — Fase 16.

**Impactos conhecidos:**
- `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` → **0** lacunas (sem `.cshtml` novo nesta fase).
- `python Scripts/2026_05_20_gdi_inventory_datatables_g_area.py` → **28** métodos `GetDados*` na data da Fase 15.

**Atenção para próximas intervenções:**
- **Fase 16** — ver entrada no topo do histórico.

---

### [2026-05-13] — Fase 14: servidor DataTables — `Areas/g` (lote ampliado) + `Financeiro` consolidados + `Clientes` abas
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/` — `AssistentesController`, `PagRecCondicoesController`, `PerfisController`, `ContasCaixasController`, `ProdutosTiposController`, `CidadesController`, `ProdutosNcmController`, `UsuariosController`, `RequisicoesController`, `VendedoresController`, `ContratosAviacaoController` (correção tipo contrato + cliente nulos), `GedController`, `FinanceiroLancamentosController`, `FinanceiroFaturamentosController`, `FinanceiroController` (`GetDados`, `getValoresConsolidados`, `GetDadosGrafico`), `ClientesController` (`GetDados` + `GetDadosContatos` + `GetDadosDestinatarios` + `JsonDataTableException`)
- `Areas/g/Views/` — `Perfis/Index`, `ContasCaixas/Index`, `PagRecCondicoes/Index`, `Requisicoes/Index`, `Usuarios/Index`, `FinanceiroFaturamentos/Index`, `Financeiro/DadosConsolidados`, `Clientes/CreateEdit` (destinatários)
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 14: continuar padronização `GetDados*` / financeiro lista + dados consolidados + abas de cliente.

**O que foi feito:**
- `param` nulo, `try/catch`, JSON sucesso com `errorMessage`/`stackTrace`/`yesFilterOnOff` onde aplicável; **`JsonDataTableException`** por controller (ou compartilhado em `Clientes`); correções de NRE (`Usuarios` perfil, `Requisicoes` tipo/vendedor, `Financeiro` status, `Ged` tipo arquivo + usuário, `Vendedores` revenda, `ContratosAviacao` tipo/cliente); `GetDadosGrafico` com JSON de erro e listas vazias + guard na view.
- `getValoresConsolidados` e `DadosConsolidados` com `xhr.dt` + contrato alinhado.

**Decisões técnicas relevantes:**
- `GetDadosGrafico` não usa shape DataTables; erro devolve as mesmas chaves de séries vazias + `errorMessage` para o `$.ajax` não quebrar o Flot após alerta.

**O que foi evitado e por quê:**
- `ProdutosController`, `AtendimentosController`, `CentrosCustosController`, `ClassificacaoFinanceiraController` — Fase **15**; controllers **`Nfe`** / **`PortalVendedor`** — Fase **16**.

**Impactos conhecidos:**
- `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` → 0 lacunas; inventário `g` mostra **4** endpoints ainda sem `JsonDataTableException` ao nível de ficheiro (nomes fora do padrão `GetDados*` ou não cobertos).

**Atenção para próximas intervenções:**
- **Fase 15** concluída; **`Nfe`** / **`PortalVendedor`** resolvidos na **Fase 16** (ver topo do histórico).

---

### [2026-05-13] — Fase 13: servidor DataTables — cadastros `Areas/g` (Filiais, UF, PagRecTipos) + inventário `g`
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/FiliaisController.cs` — `GetDados`: `param` nulo, `try/catch`, sucesso com `errorMessage`/`stackTrace`/`yesFilterOnOff`, **`JsonDataTableException`**; coligada inexistente sem NRE
- `Areas/g/Controllers/UFController.cs` — `GetDados`: idem + sucesso com `errorMessage`/`stackTrace` vazios
- `Areas/g/Controllers/PagRecTiposController.cs` — `GetDados`: idem (`yesFilterOnOff` fixo `"0"`)
- `Areas/g/Views/Filiais/Index.cshtml`, `Areas/g/Views/PagRecTipos/Index.cshtml` — **`xhr.dt`** + `GdiDtNotifyJsonErrorMessage`
- `Scripts/2026_05_20_gdi_inventory_datatables_g_area.py` — inventário `GetDados*` em `Areas/g/Controllers`
- `GDI-ERP-Plataform.csproj` — `<None Include>` do script
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 13: alinhar cadastros `g` ao contrato DataTables (servidor + cliente) e ferramenta de inventário para o restante da área.

**O que foi feito:**
- Três controllers representativos padronizados; views com consumo de `errorMessage` no retorno JSON; script para mapear próximos `GetDados*`.

**Decisões técnicas relevantes:**
- `JsonDataTableException` privado por controller (paridade com `gc`).

**O que foi evitado e por quê:**
- Não varrer todos os `GetDados` de `Areas/g` num único PR (risco de regressão); lote inicial + inventário.

**Impactos conhecidos:**
- `UF/Index` já tinha `xhr.dt`; sem alteração nesta entrega.

**Atenção para próximas intervenções:**
- **Fase 14:** demais controllers em `Areas/g` conforme saída de `2026_05_20_gdi_inventory_datatables_g_area.py` (priorizar `ClientesController`, `FinanceiroController`, etc.).

---

### [2026-05-13] — Fase 12: servidor DataTables — `ComexInvoicesController` + `MovimentosEntradasController` + `EstoqueInventarioController` + views
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/ComexInvoicesController.cs` — `GetDadosViewImportacao`, `GetDadosViewInvoicesItens`: `param` nulo; JSON com **`yesFilterOnOff`** em sucesso/erro (importação)
- `Areas/gc/Controllers/MovimentosEntradasController.cs` — `GetDadosMovimentosEntradas`: `param` nulo; sucesso e resposta sem linhas com `errorMessage`/`stackTrace` vazios
- `Areas/gc/Controllers/EstoqueInventarioController.cs` — `GetDadosInventario`, `GetDadosInventarioItem`: `param` nulo; inventário inválido com **`severity`**
- `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml` — aba invoices: `xhr.dt` com `GdiDtNotifyJsonErrorMessage` + atualização condicional dos totais
- `Areas/gc/Views/ComexInvoices/ModalInvoice.cshtml` — `xhr.dt` + `GdiDtNotifyJsonErrorMessage`
- `Areas/gc/Views/EstoqueInventario/FormInventarioItens.cshtml` — `error.dt` + `xhr.dt` + `GdiDtNotifyJsonErrorMessage` + `btnFiltro`
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 12: fechar lacunas em COMEX invoices (view importação + itens no modal), listagem de entradas NF e inventário — contrato JSON e consumo `xhr.dt`.

**O que foi feito:**
- `MovimentosEntradas/Index` e `EstoqueInventario/Index` já tinham `xhr.dt`; `FormInventarioItens` e `ModalInvoice` completados.

**Decisões técnicas relevantes:**
- Labels de totais na aba invoices do `CreateEdit`: só atualizar quando não há `errorMessage` (evita `undefined` nos elementos).

**O que foi evitado e por quê:**
- Não varrer `Areas/g` (cadastros) nesta fase (escopo Fase 13 ou inventário).

**Impactos conhecidos:**
Erros de servidor nas grelhas citadas passam a SweetAlert2 quando o JSON traz `errorMessage`.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados` em `ClientesController`, `Produtos*`, etc.) ou script de inventário global.

---

### [2026-05-13] — Fase 11: servidor DataTables — compras (`GetDadosCompras`) + financeiro + estoque + views Gerencial / recebimento itens estoque
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosComprasController.cs` — `GetDadosCompras`: `param` nulo, `try/catch`, sucesso com `errorMessage`/`stackTrace` vazios; **`JsonDataTableException`**
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs` — `GetDadosLancamentos`, `GetDadosLancamentosByMovimento`: `param` nulo
- `Areas/gc/Controllers/EstoqueController.cs` — `param` nulo e JSON de sucesso alinhado (`errorMessage`/`stackTrace`) em `GetDadosEstoque`, `GetDadosRecebimentoImportacao`, `GetDadosRecebimentoItensImportacao`, `GetDadosRecebimentoEstoque`, `GetDadosRecebimentoItensEstoque`
- `Areas/gc/Views/Estoque/FormRecebimentoItensEstoque.cshtml` — `error.dt` + `xhr.dt` + `GdiDtNotifyJsonErrorMessage`
- `Areas/gc/Views/Gerencial/IndexPainelComercialGerencial.cshtml` — `xhr.dt`: `GdiDtNotifyJsonErrorMessage` antes de `jsUpdateDataView`
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 11: lacunas da Fase 10 (financeiro/compras/estoque) — `GetDadosCompras` sem contrato de exceção; `param` nulo em endpoints já com try/catch; respostas de sucesso COMEX/recebimento sem `stackTrace` explícito; painel gerencial e form de itens de recebimento sem consumo uniforme de `errorMessage` no `xhr.dt`.

**O que foi feito:**
- `IndexCompras`, `FinanceiroLancamentos/Index` e grelhas Estoque já tinham `xhr.dt` em vários casos — foco em servidor + lacunas de view.

**Decisões técnicas relevantes:**
- `JsonDataTableException` em `MovimentosComprasController` (paridade com Movimentos/COMEX).

**O que foi evitado e por quê:**
- Não duplicar `JsonDataTableException` em `EstoqueController`/`FinanceiroLancamentosController` (já devolvem JSON de erro estruturado).

**Impactos conhecidos:**
Falhas nas grelhas citadas mostram mensagem via SweetAlert2 quando o JSON traz `errorMessage`.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados*`) ou inventário automatizado global.

---

### [2026-05-13] — Fase 10: servidor DataTables — `GetRelatorioConsultaPedidos` + `ComexImportacoesController` + views COMEX
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs` — `GetRelatorioConsultaPedidos`: `param` nulo, `try/catch`, sucesso com `errorMessage`/`stackTrace` vazios
- `Areas/gc/Controllers/ComexImportacoesController.cs` — `GetDados`: `param` nulo; JSON de erro com `yesFilterOnOff`; `GetDadosItensImportacao`, `GetDadosImportacoesLogs`, `GetGedComex`, `GetGedInvoicesComex`: `try/catch`, sucesso com `errorMessage`/`stackTrace` vazios onde aplicável; método privado **`JsonDataTableException`**
- `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml` — `xhr.dt` + `GdiDtNotifyJsonErrorMessage` (itens, GED, invoices PDF); guard em **`jsDisplayFooterItensImportacao`**
- `Areas/gc/Views/ComexImportacoes/ModalImportacoesLogs.cshtml` — `xhr.dt` + `GdiDtNotifyJsonErrorMessage`
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 10: fechar lacunas do contrato DataTables em relatório de consulta de pedidos e em grelhas COMEX (importação) ainda sem `try/catch` padronizado ou sem consumo `xhr.dt`.

**O que foi feito:**
- `ModalConsultaPedidos.cshtml` já tinha `xhr.dt` — apenas servidor alinhado.
- `ComexImportacoes/Index` já tratava `GetDados` com `xhr.dt` — reforço no JSON de erro e `param` nulo.

**Decisões técnicas relevantes:**
- `JsonDataTableException` no `ComexImportacoesController` (ficheiro extenso; evita `catch` duplicados).
- Footer da aba itens: não atualizar labels quando `errorMessage` preenchido (evita `undefined` nos campos custom).

**O que foi evitado e por quê:**
- Não varrer todos os controllers `GetDados*` do repositório nesta fase (escopo COMEX + um endpoint Movimentos).

**Impactos conhecidos:**
Erros de SQL/EF nas grelhas citadas passam a SweetAlert2 via `GdiDtNotifyJsonErrorMessage` quando aplicável.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados*`) ou inventário automatizado global.

---

### [2026-05-13] — Fase 9: servidor DataTables — `MovimentosController` (`GetDados*`) + `xhr.dt` nas views
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs` — `GetDadosModalItensComValor`, `GetDadosCartaCorrecao`, `GetDadosPainelPedidos`, `GetDadosInvoicesItensEspelhoDigital`: `param` nulo, `try/catch`, JSON de erro (`errorMessage`, `severity`, `stackTrace`, `yesFilterOnOff`, `sEcho`, `aaData` vazio); sucesso alinhado com `errorMessage`/`stackTrace` vazios onde aplicável; método privado **`JsonDataTableException`**
- `Areas/gc/Views/Movimentos/ModalPedidoEntrega.cshtml`, `ModalPedidoExpedicao.cshtml`, `ModalPedidoAprovacao.cshtml`, `ModalViewCartaCorrecao.cshtml`, `ModalInvoicesItensEspelhoDigital.cshtml` — encadeamento **`xhr.dt`** + `GdiDtNotifyJsonErrorMessage`
- `CLAUDE.md`, `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 9: alargar o contrato de falha DataTables ao módulo comercial **Movimentos** (`gc`), complementando a Fase 8.

**O que foi feito:**
- Quatro actions que ainda não tinham tratamento de exceção estruturado para a grelha.
- `PainelPedidos.cshtml` já tratava `xhr.dt` com `GdiDtNotifyJsonErrorMessage` + `btnFiltro` — sem alteração.

**Decisões técnicas relevantes:**
- Reutilizar o mesmo shape JSON da Fase 5c/8; helper privado no controller (ficheiro muito grande — evita duplicação de quatro blocos `catch` idênticos).

**O que foi evitado e por quê:**
- Não alterar `GetDadosPedidos` / `GetDadosItensPedido` / `GetDadosItensSeparacao` (já com `try/catch`).

**Impactos conhecidos:**
Erros de SQL/EF nas grelhas dos modais e painel passam a mensagem legível via SweetAlert2 quando a view consome `xhr.dt`.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados*`) ou inventário automatizado global.

---

### [2026-05-13] — Fase 8: servidor DataTables — `try/catch` + JSON `errorMessage` (Atendimentos + GedSGQ)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Controllers/AtendimentosController.cs` — `getDadosAtividades`: `try/catch`, `param` nulo, JSON de erro com `errorMessage`/`severity`/`stackTrace`/`yesFilterOnOff`/`sEcho`/`aaData` vazio; `GetGedAtendimento`: idem (removidos `filterDb`/`filterAdvanced`/`SentencaSQL` não usados para evitar CS0219)
- `Areas/qa/Controllers/GedSGQController.cs` — `GetDadosDocsSGQ`, `GetDadosPops`, `GetDadosComunicados`, `GetDadosAtasReunioes`: `filterOnOff` + `param` nulo + `try/catch` + método privado `JsonDataTableException`
- `Areas/g/Views/Atendimentos/Edit.cshtml` — `errMode` + `xhr.dt`/`GdiDtNotifyJsonErrorMessage` em **Atividades** e **GED** (paridade com logs)
- `CLAUDE.md` — nota Fase 8 (contrato servidor)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 8: extensão do contrato JSON de falha aos endpoints DataTables ainda sem `try/catch`, para consumo por `GdiDtNotifyJsonErrorMessage` nas views já preparadas.

**O que foi feito:**
- Padrão alinhado ao `getDadosAtendimentos` / `getDadosAtendimentosLogs` (Fase 5c).
- Qualidade (`qa`): quatro listagens GED SGQ passam a devolver JSON estruturado em exceção (views já tinham `xhr.dt`).

**Decisões técnicas relevantes:**
- `JsonDataTableException` centralizado em `GedSGQController` para evitar quatro blocos `catch` idênticos.

**O que foi evitado e por quê:**
- Não varrer todos os `GetDados*` do `gc`/`g` num único commit (volume e regressão).

**Impactos conhecidos:**
Erros de servidor nas grelhas citadas mostram mensagem via SweetAlert2 em vez de falha genérica / HTML parse.

**Atenção para próximas intervenções:**
Fase 13 sugerida: cadastros `Areas/g` (`GetDados*`) ou inventário automatizado global.

---

### [2026-05-13] — Fase 7: `alert(` nativo em `.cshtml` → `LibMessageError`
**Tipo:** Implementação
**Arquivos tocados:**
- `Scripts/2026_05_20_gdi_replace_alert_libmessage.py` — substituição mecânica (regex com `(?<!\.)alert` para não tocar em `GdiSwal2.alert`) dos padrões `+"…"+ err.message` / `e.message` / `err.message` / `err.toString` / `Erro [tag]` + `err.message`
- `Scripts/2026_05_20_gdi_dedupe_libmessage_if_else.py` — remoção de `if (typeof LibMessageError === "function") { LibMessageError(...); } else { LibMessageError(...); }` quando ambos os ramos são equivalentes (472 ocorrências em 168 ficheiros)
- ~237 ficheiros `.cshtml` tocados pelo primeiro script; ajustes manuais: `ClassificacaoFinanceira/CreateEdit`, `UserIdentity/Index`, `ModalPedidoNotaFiscal`, `Atendimentos/Index`, `ComexProdutos/Index`, `ProdutosPre`, `Produtos/Index`; `FormPedidoCreate` + `FinanceiroLancamentos/Index` (ramos `else` não idênticos)
- `GDI-ERP-Plataform.csproj` — `<None Include>` para `2026_05_20_gdi_replace_alert_libmessage.py` e `2026_05_20_gdi_dedupe_libmessage_if_else.py`
- `CLAUDE.md` — nota Fase 7 + scripts
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 7 do plano de modernização UX: eliminar `alert(` nativo nas views em favor de SweetAlert2 via `LibMessageError`, alinhado ao restante do ERP.

**O que foi feito:**
- Script executado uma vez; revisão grep: restam apenas `GdiSwal2.alert` e comentário `// alert(options)`.
- `gdi-session-handler.js` e fallbacks internos em `start.js` mantidos (alert como último recurso).
- Após o primeiro script, correção em massa de `if/else` duplicado com `2026_05_20_gdi_dedupe_libmessage_if_else.py`.

**Decisões técnicas relevantes:**
- Não alterar `GdiSwal2.alert({...})` (confirmações com callback).
- `confirm(` nativo: inexistente em `.cshtml` no âmbito pesquisado; `LibMessageConfirm` já usado via `start.js` quando Swal indisponível.

**O que foi evitado e por quê:**
- Não reescrever dezenas de `if (typeof LibMessageError)… else alert` para uma linha onde o script já substituiu o ramo `alert` equivalente em outros ficheiros; casos remanescentes tratados à mão.

**Impactos conhecidos:**
Mensagens de exceção em `catch` passam pelo mesmo estilo visual que o resto do sistema.

**Atenção para próximas intervenções:**
Fase 10 sugerida: continuar `try/catch` + JSON `errorMessage` nos restantes `GetDados*` / COMEX / financeiro ou script de inventário.

---

### [2026-05-13] — Fase 6: documentação + verificação automática views `Gdi*` vs `.csproj`
**Tipo:** Implementação | Análise
**Arquivos tocados:**
- `Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` — script Python (regex `Gdi(Ajax|Dt)\w*` em `Areas/` e `Views/` vs `<Content Include="…cshtml" />` no `GDI-ERP-Plataform.csproj`)
- `GDI-ERP-Plataform.csproj` — `<None Include="Scripts\2026_05_20_gdi_verify_csproj_gdi_helpers.py" />` (ferramenta de repo; não publicável como recurso do site)
- `CLAUDE.md` — padrões helpers `GdiDt*` / `GdiAjax*`, contrato JSON, ficheiros críticos, armadilhas publish/cache
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fechar risco de novos `.cshtml` com helpers `GdiAjax*` / `GdiDt*` fora do `<Content Include>` do `.csproj` (publish incompleto) e consolidar documentação da migração de mensagens.

**O que foi feito:**
- Script executável antes do publish; exit code 1 se existir lacuna.
- Execução na máquina de dev: `cshtml_with_GdiAjax_or_GdiDt: 200`, `missing_in_csproj: 0`.

**Decisões técnicas relevantes:**
- Critério de deteção alinhado aos nomes reais dos helpers (`GdiAjax*`, `GdiDt*`).

**O que foi evitado e por quê:**
- Não incluir o `.py` como `<Content>` — evita tratar como ficheiro estático do site; `None` mantém-o no projeto MSBuild.

**Impactos conhecidos:**
Documentação para agentes e humanos; gate manual ou CI possível com o mesmo comando.

**Atenção para próximas intervenções:**
Fase 7 sugerida: substituir `alert(` / `confirm(` nativos por `LibMessageError` / `LibMessageConfirm` (por módulo) ou alargar `try/catch` + `errorMessage` em outros `GetDados*` fora dos já cobertos.

---

### [2026-05-13] — Fase 5b (ampla) + 5c: `GdiAjaxNotifyInconsistencias` global + Atendimentos DataTables servidor
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (`getValidationSummary` → `GdiAjaxNotifyInconsistencias(message)` em vez de `LibMessageAlert` duplicado)
- ~152 ficheiros `.cshtml` em `Areas/**` (+ `Views/**` se existir) com `LibMessageAlert("Verifique as inconsistências", …)` → `GdiAjaxNotifyInconsistencias(…)`; variantes upload/erro com `opcoes.title`; `ModalIncluirLancamentos` título com `!`
- `Areas/g/Controllers/AtendimentosController.cs` — `getDadosAtendimentos` e `getDadosAtendimentosLogs`: `try/catch`, JSON de falha com `errorMessage`, `severity`, `stackTrace`, `yesFilterOnOff`; sucesso de logs inclui `yesFilterOnOff = "0"` para simetria com outras grelhas
- `Areas/g/Views/Atendimentos/Edit.cshtml` — tabela de logs: `errMode` + `xhr.dt` com `GdiDtNotifyJsonErrorMessage(json)`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Extender 5b a todo o projeto e concluir 5c com contrato DataTables nos endpoints de listagem de atendimentos.

**O que foi feito:**
- Script de substituição em massa nos `.cshtml` (padrão + literais "Erro no Upload…" / "Error: Verifique…" + correção `result.msg,)` → `result.msg)`).
- Servidor: exceções passam a JSON consumível por `GdiDtNotifyJsonErrorMessage` na view de logs.

**Decisões técnicas relevantes:**
- Mantidos `LibMessageAlert("Atenção", "Verifique o período…")` e similares (outro título/corpo).

**O que foi evitado e por quê:**
- `LmsEvidenceController` e outros `Json { ok, error }` não-DataTables sem alteração.

**Impactos conhecidos:**
Feedback Ajax unificado para a frase legado "Verifique as inconsistências"; atendimentos lista/logs mostram erro de servidor na grelha quando aplicável.

**Atenção para próximas intervenções:**
Revisão visual em QA; opcional alargar `GdiAjaxNotifyInconsistencias` a títulos totalmente customizados; documentar no `CLAUDE.md` o trio de helpers (`GdiDt*`, `GdiAjax*`).

---

### [2026-05-13] — Fase 5b (piloto): Ajax/modais — `GdiAjaxNotifyInconsistencias` + pasta `g/Financeiro`
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (`GdiAjaxNotifyInconsistencias` — título legado por omissão; `severity` opcional; `LibMessageProcessandoHide` por omissão)
- `Areas/g/Views/Financeiro/Index.cshtml`
- `Areas/g/Views/Financeiro/ModalBaixarTitulos.cshtml`, `ModalBoleto.cshtml`, `ModalCancelarTitulos.cshtml`, `ModalEditarTitulo.cshtml`, `ModalGerarRemessaBoletosBancarios.cshtml`, `ModalNotaDebito.cshtml`, `ModalProrrogarVencimentoTitulo.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Iniciar padronização de feedback Ajax fora do DataTables (texto "Verifique as inconsistências" repetido em dezenas de views).

**O que foi feito:**
- Helper único que delega em `LibMessageAlert` ou `LibMessageError` conforme `opcoes.severity`.
- Piloto: substituição mecânica de `LibMessageAlert("Verifique as inconsistências", …)` por `GdiAjaxNotifyInconsistencias(…)` em toda a pasta `Areas/g/Views/Financeiro`.

**Decisões técnicas relevantes:**
- Sem novo pacote NuGet; mesmo bundle que já inclui `start.js`.

**O que foi evitado e por quê:**
- Não varrer todas as áreas num único commit (risco de revisão); restantes `gc`/`qa`/outros módulos `g` ficam para extensões da 5b.

**Impactos conhecidos:**
Telas financeiras `g` listadas: mesmo texto e ícone de aviso que antes; possível `LibMessageProcessandoHide` extra em caminhos que já escondiam processando (efeito colateral benigno).

**Atenção para próximas intervenções:**
Estender `GdiAjaxNotifyInconsistencias` às outras pastas (`gc`, `qa`, restantes `g`); opcional Fase 5c (`getDadosAtendimentos` com try/catch + JSON).

---

### [2026-05-13] — Fase 5a: `Atendimentos/Index` — `error.dt` + `errMode` (paridade Fase 2)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Views/Atendimentos/Index.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
A listagem de atendimentos era a única em `g` com DataTables sem `error.dt` e sem `$.fn.dataTable.ext.errMode = 'none'`, apesar de já ter `ajax.error` com `GdiDtNotifyLoadFailure`.

**O que foi feito:**
- Antes do `.DataTable({`: `errMode = 'none'` e `.on('error.dt', … GdiDtNotifyLoadFailure(message))` encadeado antes do `xhr.dt` existente.

**Decisões técnicas relevantes:**
- Alinhamento ao padrão de `Assistentes/Index` e restantes índices `g`.

**O que foi evitado e por quê:**
- Não alterar `getDadosAtendimentos` (sem `try/catch` explícito; falhas seguem pelo pipeline Ajax / erro DataTables).

**Impactos conhecidos:**
Erros de parsing/rendering da grelha passam a notificar via o mesmo helper que as outras listagens.

**Atenção para próximas intervenções:**
Fase 5b: padronização de modais/Ajax (`LibMessageAlert` genérico); opcional endurecimento de `getDadosAtendimentos` com `try/catch` + JSON `errorMessage` alinhado à Fase 4.

---

### [2026-05-13] — Fase 4: JSON DataTables — `error` → `errorMessage` + `severity` (servidor)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Controllers/EstoqueController.cs` (`GetDados` recebimento importação lista, `GetDadosRecebimentoItensImportacao`, `GetDadosRecebimentoItensEstoque` / índice associado: catches e validações com `aaData`)
- `Areas/gc/Controllers/MovimentosEntradasController.cs` (`GetDados` — catches)
- `Areas/g/Controllers/ClientesController.cs` (guard `db == null` em `GetDados`)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Payloads DataTables ainda expunham a propriedade `error`, incompatível com `GdiDtNotifyJsonErrorMessage` (que lê `errorMessage`).

**O que foi feito:**
- Substituição por `errorMessage`, `severity = "error"` e `stackTrace` quando há exceção em `catch` (`e.ToString()`); validações sem exceção sem `stackTrace` ou com `stackTrace = ""` no ramo agregado pós-catch onde a exceção não está em scope.
- Retorno antecipado `db == null` em `ClientesController.GetDados` alinhado ao mesmo contrato.

**Decisões técnicas relevantes:**
- `Areas/qa/Controllers/LmsEvidenceController.cs` mantém `ok`/`error` — contrato Ajax não-DataTables, fora deste passo.

**O que foi evitado e por quê:**
- Não alterar todos os `Json` da solução: apenas actions identificadas com `aaData` + `error`.

**Impactos conhecidos:**
Grelhas de recebimento importação/estoque e entradas de NF: mensagens de falha passam pelo helper no cliente com ícone de erro.

**Atenção para próximas intervenções:**
Paridade Fase 2 em `g/Atendimentos/Index`; padronização de modais/Ajax (`LibMessageAlert` genérico); outros `Json` legados fora de DataTables.

---

### [2026-05-13] — Fase 3b: ordem `xhr.dt` — `GdiDtNotifyJsonErrorMessage` antes de `btnFiltro` (`gc`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Estoque/FormRecebimentoItensImportacao.cshtml`
- `Areas/gc/Views/CfopOperacoes/Index.cshtml`
- `Areas/gc/Views/ComexProdutos/Index.cshtml`
- `Areas/gc/Views/MovimentosEntradas/Index.cshtml`
- `Areas/gc/Views/Movimentos/IndexEstoque.cshtml`
- `Areas/gc/Views/EstoqueInventario/Index.cshtml`
- `Areas/gc/Views/Estoque/IndexRecebimentoImportacao.cshtml`
- `Areas/gc/Views/Estoque/IndexRecebimentoEstoque.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alinhar à convenção do plano: no `xhr.dt`, notificar `errorMessage` antes de atualizar o botão de filtro.

**O que foi feito:**
- Troca mecânica `{ btnFiltro(...); GdiDtNotifyJsonErrorMessage(...) }` → `{ GdiDtNotifyJsonErrorMessage(...); btnFiltro(...) }` nos 8 ficheiros.

**Decisões técnicas relevantes:**
- `ComexFinanceiro/Index.cshtml` mantém `if (GdiDtNotifyJsonErrorMessage(json)) { } else if (...) { btnFiltro(...) }` — não alterado (padrão condicional distinto).

**O que foi evitado e por quê:**
- Escopo limitado a `Areas/gc/Views`; sem mudanças em controllers ou `start.js`.

**Impactos conhecidos:**
Recebimento, inventário, entradas, estoque em movimentos, CFOP operações e listagem COMEX produtos: ordem igual às restantes grelhas já padronizadas.

**Atenção para próximas intervenções:**
Fase 4 (servidor DataTables) concluída para os pontos mapeados — ver entrada acima. Seguinte: `Atendimentos/Index` (`error.dt`) e/ou modais Ajax.

---

### [2026-05-13] — Fase 3 (fecho): `xhr.dt` → `GdiDtNotifyJsonErrorMessage` nas lacunas `gc`
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Fretes/Index.cshtml`
- `Areas/gc/Views/CfopParametros/Index.cshtml`
- `Areas/gc/Views/FinanceiroParametroDifal/Index.cshtml`
- `Areas/gc/Views/FinanceiroLancamentos/ModalViewFinanceiroMovimentos.cshtml`
- `Areas/gc/Views/MovimentosCompras/IndexCompras.cshtml`
- `Areas/gc/Views/Movimentos/ModalConsultaPedidos.cshtml`
- `Areas/gc/Views/ComexProdutos/ProdutosPre.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Completar o padrão Fase 3 nas grelhas `gc` que ainda só chamavam `btnFiltro` no `xhr.dt`.

**O que foi feito:**
- Handler unificado: `GdiDtNotifyJsonErrorMessage(json);` antes de `btnFiltro(json.yesFilterOnOff)` (inclui `ProdutosPre` com cadeia `.css(...).DataTable`).

**Decisões técnicas relevantes:**
- Mesma assinatura de função já usada noutras views; sem alteração de bundles ou `start.js`.

**O que foi evitado e por quê:**
- Na mesma data, a reordenação `GdiDtNotify` → `btnFiltro` nos ficheiros que ainda invertiam a ordem ficou registada em **Fase 3b** (entrada imediatamente acima no histórico).

**Impactos conhecidos:**
Fretes, CFOP parâmetros, DIFAL, modal de lançamentos por movimento, compras, modal histórico de pedidos e pré-listagem COMEX produtos passam a exibir mensagem de `errorMessage` no payload DataTables quando existir.

**Atenção para próximas intervenções:**
Auditoria servidor `error` vs `errorMessage` em actions DataTables (Fase 4).

---

### [2026-05-13] — Fase 3 (extensão): `xhr.dt` → `GdiDtNotifyJsonErrorMessage` + contrato JSON (áreas `g` e `qa`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/g/Views/**` (16 `.cshtml`: todas as grelhas com `xhr.dt` + `btnFiltro` passam a chamar `GdiDtNotifyJsonErrorMessage(json)` antes do filtro)
- `Areas/qa/Views/GedSGQ/IndexAtasReunioes.cshtml`, `IndexComunicados.cshtml`, `IndexDocsSGQ.cshtml` (mesmo padrão)
- `Areas/g/Controllers/ClientesController.cs` (`GetDados` / catch: `error` → `errorMessage` + `severity` + `stackTrace`)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Replicar a Fase 3 nas áreas `g` e `qa` onde só existia `btnFiltro` no `xhr.dt`, e alinhar o JSON de falha do DataTables em `ClientesController` ao helper (`errorMessage` em vez de `error`).

**O que foi feito:**
- Views `g` e `qa` (GED SGQ): `GdiDtNotifyJsonErrorMessage(json)` no início do handler `xhr.dt`, antes de `btnFiltro`, sem alterar `error.dt` / `ajax.error`.
- `ClientesController`: no `catch` de `GetDados`, resposta JSON com `errorMessage`, `severity = "error"`, `stackTrace = e.ToString()` e demais campos DataTables inalterados.

**Decisões técnicas relevantes:**
- Manter `stackTrace` no JSON como em outros controllers gc para `console.error` no helper; em produção pode ser endurecido depois com log só no servidor.

**O que foi evitado e por quê:**
- Não acrescentar `error.dt` em `Atendimentos/Index` (já tinha só `xhr.dt`); escopo limitado ao padrão Fase 3 acordado.

**Impactos conhecidos:**
Listagens g (clientes, produtos, NCM, financeiro, NFe, GED, etc.) e índices GED SGQ em `qa`: se o servidor devolver `errorMessage` preenchido no JSON da grelha, o utilizador vê SweetAlert coerente com `severity`.

**Atenção para próximas intervenções:**
Outros actions em `g` que ainda devolvam apenas `error` no JSON do DataTables não são cobertos pelo helper até alinharem o contrato.

---

### [2026-05-13] — Fase 3 (extensão): `xhr.dt` → `GdiDtNotifyJsonErrorMessage` + `severity` nos JSON de erro (gc)
**Tipo:** Implementação
**Arquivos tocados:**
- ~15 ficheiros `.cshtml` em `Areas/gc/Views` (substituição do bloco `json.errorMessage` + `LibMessageAlert`/`stackTrace` manual pelo helper; `EstoqueLotes/Index` e `Movimentos/PainelPedidos` multilinha à mão por CRLF)
- `Areas/gc/Controllers/ComexProdutosController.cs`, `ComexInvoicesController.cs`, `ComexImportacoesController.cs`, `ComexFinanceiroController.cs`, `EstoqueController.cs` (ramo catch com `stackTrace`), `EstoqueInventarioController.cs`, `FinanceiroLancamentosController.cs` (`GetDadosLancamentosByMovimento`), `MovimentosController.cs` (dois `return Json` com `errorMessage,` + `stackTrace` shorthand)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alargar o piloto Fase 3: mesmo tratamento de `errorMessage`/`severity` em todas as grelhas gc identificadas com o padrão legado no `xhr.dt`.

**O que foi feito:**
- Views: `GdiDtNotifyJsonErrorMessage(json)` preservando ordem (`btnFiltro`, `jsUpdateDataView`, labels) onde já existia.
- Controllers: propriedade opcional `severity = "error"` nos `Json` de exceção que já expunham `errorMessage` + `stackTrace` (ou shorthand `errorMessage,` / `stackTrace,`).

**Decisões técnicas relevantes:**
- Omissão de `severity` no servidor mantém ícone de aviso (`LibMessageAlert`) no helper — sem alterar payloads de sucesso.

**O que foi evitado e por quê:**
- Não alterar actions que devolvem `error` em vez de `errorMessage` (ex.: ramos catch em `EstoqueController` já existentes).

**Impactos conhecidos:**
Listagens COMEX, estoque (índices/recebimento), CFOP, faturas, financeiro COMEX, inventário e entradas: mensagem de exceção do servidor pode aparecer como erro (vermelho) quando o JSON inclui `severity`.

**Atenção para próximas intervenções:**
Área `g` (ex.: `ClientesController` com `errorMessage`) e restantes `catch` sem `stackTrace` podem seguir o mesmo critério sob revisão.

---

### [2026-05-13] — Fase 3 (piloto): `GdiDtNotifyJsonErrorMessage` + `severity` opcional no JSON
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (função `GdiDtNotifyJsonErrorMessage` — campo opcional `severity`; omissão mantém `LibMessageAlert` como no legado)
- `Areas/gc/Views/Movimentos/IndexPedido.cshtml` (`xhr.dt` → helper)
- `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml` (`xhr.dt` → helper)
- `Areas/gc/Controllers/MovimentosController.cs` (`GetDadosPedidos`: JSON de exceção com `severity = "error"`)
- `Areas/gc/Controllers/FinanceiroLancamentosController.cs` (`GetDadosLancamentos`: JSON de exceção com `severity = "error"`)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase 3 mínima: mensagens de negócio via `errorMessage` no mesmo payload DataTables, com distinção opcional aviso vs erro, sem alterar campos obrigatórios do contrato (`aaData`, `sEcho`, totais, etc.).

**O que foi feito:**
- Helper `GdiDtNotifyJsonErrorMessage(json)`: se `errorMessage` vazio → no-op; `stackTrace` só em `console.error`; `severity` `error`/`danger`/`err` → `LibMessageError`; caso contrário → `LibMessageAlert` (comportamento legado quando `severity` ausente).
- Piloto em duas telas e respetivos endpoints de listagem: ramo `catch` passa a incluir propriedade opcional `severity = "error"` para falhas de servidor.

**Decisões técnicas relevantes:**
- Respostas de sucesso inalteradas; `severity` só no JSON de erro já existente.

**O que foi evitado e por quê:**
- Propagar a todas as views/controllers num único passo — outras grelhas podem adoptar o helper sem mudar servidor até ser desejado `LibMessageError` via `severity`.

**Impactos conhecidos:**
Em `IndexPedido` e índice de lançamentos financeiros (gc), mensagem de exceção do servidor passa a ícone de erro quando `severity` é `error`.

**Atenção para próximas intervenções:**
Alargar `GdiDtNotifyJsonErrorMessage` às outras `xhr.dt` com `errorMessage`; usar `severity = "warning"` no servidor para avisos não bloqueantes.

---

### [2026-05-13] — Fase 2: `GdiDtNotifyLoadFailure` em ~79 views (`error.dt` / `ajax.error` DataTables)
**Tipo:** Implementação
**Arquivos tocados:**
- ~79 ficheiros `.cshtml` em `Areas/**` (lista gerada por substituição mecânica; ex.: índices `g`/`gc`/`qa`, modais com grelha server-side)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alargar a Fase 1: mesma mensagem legada e `LibMessageError` centralizado via `GdiDtNotifyLoadFailure` em todos os handlers `error.dt` e `ajax.error` que usavam `LibMessageAlert`/`LibMessageError` com o texto «Falha ao processar os dados <b>…</b>».

**O que foi feito:**
- Substituição literal dos quatro padrões (`message` / `errorThrown` × Alert/Error) por `GdiDtNotifyLoadFailure(message)` ou `GdiDtNotifyLoadFailure(errorThrown)` sem alterar URLs, `ajax.data`, colunas, `xhr.dt` nem contrato JSON.

**Decisões técnicas relevantes:**
- Exceção intencional: `Areas/g/Views/Financeiro/DadosConsolidados.cshtml` mantém `LibMessageAlert` com variável `url` (padrão diferente; fora dos quatro literais).

**O que foi evitado e por quê:**
- Mudar payloads ou actions; não tocar em `json.errorMessage` nos `xhr.dt` (Fase 3 / confirmação explícita para evolução de API).

**Impactos conhecidos:**
Em falhas de grelha, `LibMessageProcessandoHide` pode ser invocado via helper onde antes só existia alerta (comportamento alinhado ao piloto `FormPedidoCreate`).

**Atenção para próximas intervenções:**
Fase 3: opcional `errorMessage`/`severity` no JSON + extensão do helper, com revisão por tela para avisos não bloqueantes.

---

### [2026-05-13] — Fase 0/1: helper `GdiDtNotifyLoadFailure` + piloto em `FormPedidoCreate`
**Tipo:** Implementação
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js`
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Centralizar feedback de falha em DataTables (`error.dt` / `ajax.error` da grelha) com `LibMessageError` + `LibMessageProcessandoHide` opcional, sem alterar contrato JSON/URLs/colunas nem avaliação global controller↔DataTables.

**O que foi feito:**
- Nova função global `GdiDtNotifyLoadFailure(detail, opcoes)` em `start.js`: mensagem legada `Falha ao processar os dados <b>…</b>`, título omissão `Atenção`, `hideProcessando` omissão `true`, fallback `alert` se `LibMessageError` ausente.
- Piloto só em `FormPedidoCreate.cshtml`: os quatro `error.dt` e quatro `ajax.error` das tabelas passam a chamar o helper; AJAX de destinatários que tinha `error` vazio passa a `GdiDtNotifyLoadFailure(..., { hideProcessando: false })` (não é grelha — evita `hide` desnecessário).

**Decisões técnicas relevantes:**
- Nenhuma alteração em `ajax.data`, `dataSrc`, actions MVC ou resposta esperada; apenas substituição do corpo dos callbacks de erro já existentes.

**O que foi evitado e por quê:**
- Migração em massa noutras views e mudança de `xhr.dt` / payload — exige confirmação explícita para evolução de API de comunicação.

**Impactos conhecidos:**
Em falha de rede na grelha, `LibMessageProcessandoHide()` passa a ser invocado também via helper (alinhado ao comportamento já documentado para esta página).

**Atenção para próximas intervenções:**
Replicar `GdiDtNotifyLoadFailure` noutros `.cshtml` com `error.dt`/`ajax.error` em PRs pequenos; opcional estender opções (ex. título) sem tocar no servidor.

---

### [2026-05-13] — Remoção do plugin toastr (não utilizado; padrão SweetAlert2)
**Tipo:** Refatoração / Limpeza
**Arquivos tocados:**
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_Blank.cshtml`
- `Views/UserIdentity/Index.cshtml`
- `Views/UserIdentity/TrocaObrigatoriaSenha.cshtml`
- `Views/UserIdentity/OldTrocaObrigatoriaSenha.cshtml`
- `Views/UserIdentity/BackupIndex.cshtml`
- `GDI-ERP-Plataform.csproj`
- Pasta `LibUI_AdminLTE-4.0.0/plugins/toastr-3/` (ficheiros apagados)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
O ERP não chamava a API `toastr.*`; o plugin era apenas carregado. Remover referências e ficheiros para alinhar ao padrão SweetAlert2 e reduzir superfície de manutenção.

**O que foi feito:**
- Removidos `<link>` e `<script>` do toastr nos layouts e páginas de identidade.
- Removidas entradas `<Content Include>` do `.csproj`.
- Eliminados `toastr.css`, `toastr.min.css`, `toastr.min.js`, `toastr.js.map` e a pasta `toastr-3`.

**Decisões técnicas relevantes:**
- Nenhum substituto funcional necessário: não havia chamadas a toastr; mensagens continuam via `LibMessage*` / Swal2.

**O que foi evitado e por quê:**
- Introduzir helper novo tipo toast no Swal2 — fora do escopo (só remoção).

**Impactos conhecidos:**
Publish: se existir `PackageTmp` antigo com cópia de `toastr-3`, seguir prática já documentada (limpar `obj` antes de publicar se surgir aviso de ficheiro em falta).

**Atenção para próximas intervenções:**
Se no futuro forem desejadas notificações não modais, usar modo toast do SweetAlert2 ou helper dedicado.

---

### [2026-05-13] — Fase E convergência: `Views` raiz (Error, UserIdentity) — `alert` → `LibMessageError`
**Tipo:** Implementação
**Arquivos tocados:**
- `Views/Error/Index.cshtml`
- `Views/Error/ModalError.cshtml`
- `Views/UserIdentity/Index.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase E: alinhar mensagens em `catch` ao padrão SweetAlert2 via `LibMessageError` + fallback `alert`, após validar ordem de scripts.

**O que foi feito:**
- `Error/Index` e `Error/ModalError`: mesmo padrão `[jsInitForm]` + `LibMessageError("Atenção", …)` com `else { alert(…) }` (páginas com `_Layout` ou modal carregado no ERP já têm `libui-swal-compat` + `start.js`).
- `UserIdentity/Index`: `catch` de `jsValidarAcesso` com `LibMessageError` para a mensagem de exceção; comentário Razor junto a `@Scripts.Render("~/bundles/libui-swal-compat")` documentando dependência antes de `start.js`.

**Decisões técnicas relevantes:**
- Não alterar `TrocaObrigatoriaSenha` / `BackupIndex` — sem `alert` encontrado.

**O que foi evitado e por quê:**
- Duplicar carregamento de scripts; apenas comentário documental no login.

**Impactos conhecidos:**
Login e ecrã de erro usam o mesmo critério visual que o resto do ERP quando `LibMessageError` está disponível.

**Atenção para próximas intervenções:**
Qualquer nova view “standalone” com `Layout = null` deve incluir jQuery → `libui-swal-compat` → `start.js` antes de `LibMessage*`.

---

### [2026-05-13] — Fase D convergência (hotspots): `alert` → `LibMessageError` + erros DataTables com `LibMessageError`
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `Areas/gc/Views/Movimentos/IndexPedido.cshtml`
- `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml`
- `Areas/g/Views/Clientes/CreateEdit.cshtml`
- `Areas/g/Views/Nfe/Index.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase D: ficheiros com muitas ocorrências de `alert`; padronizar mensagens de erro em criação/atualização DataTables (SweetAlert2) sem alterar opções/comportamento do DataTables.net.

**O que foi feito:**
- Todos os `alert("[contexto]" + err|e.message…)` e `alert(err.message)` / `alert(err.toString())` nestes cinco ficheiros passaram ao padrão `LibMessageError("Atenção", …)` com fallback `else { alert(…) }`.
- Onde já existia `LibMessageAlert("Atenção", "Falha ao processar os dados …")` em handlers `error.dt` ou `ajax.error` das tabelas, substituído por **`LibMessageError`** (mesmo texto, ícone de erro via helper central — sem mudar assinaturas `.DataTable()` / `.dataTable()` nem callbacks além da chamada de mensagem).

**Decisões técnicas relevantes:**
- Não introduzir `LibMessageSuccess` novo em fluxos que já usavam apenas `LibMessageAlert`/`LibMessageProcessando` em sucesso; escopo limitado a erros visíveis ao utilizador e falhas de rede/tabela.

**O que foi evitado e por quê:**
- Refatoração de `columns`, `ajax`, `drawCallback`, etc.; apenas troca da função de exibição da mensagem.

**Impactos conhecidos:**
Linhas longas nos `catch`; comportamento de dados inalterado.

**Atenção para próximas intervenções:**
Outros índices com >3 `alert` (ex.: `Financeiro/Index`, `MovimentosEntradas/Index`) podem seguir o mesmo padrão numa extensão da Fase D.

---

### [2026-05-13] — Fase B convergência: `alert` em catch → `LibMessageError` (views com 1–3 ocorrências nativas)
**Tipo:** Implementação
**Arquivos tocados:**
- ~161 ficheiros `.cshtml` em `Areas` (contagem nativa `(?<!\.)alert\(` entre 1 e 3 antes da alteração), mais correção pontual em `Areas/g/Views/ClassificacaoFinanceira/CreateEdit.cshtml` (caso `alert("Erro [jsSalvarDados] (" + e + ")")`).
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Fase B do plano: substituir feedback ao utilizador em `catch` por `LibMessageError("Atenção", …)` com fallback `else { alert(…) }` quando `LibMessageError` não existir.

**O que foi feito:**
- Substituição automática dos padrões `alert("[tag]" + err.message.toString());` e equivalente com `e.message`, por bloco `if (typeof LibMessageError === "function") { LibMessageError("Atenção", "[tag]" + (…)); } else { alert(…); }`.
- Regex com lookbehind `(?<!\.)` para **não** alterar `GdiSwal2.alert`.
- Caso excecional em `ClassificacaoFinanceira/CreateEdit.cshtml` convertido manualmente.

**Decisões técnicas relevantes:**
- Mantém-se `alert` apenas no ramo `else` (ambiente sem helper).
- Ficheiros com mais de 3 `alert` nativos por ficheiro ficam para fases seguintes (C/D).

**O que foi evitado e por quê:**
- Alterações em `start.js`, `Views` raiz fora de `Areas`, e views com >3 ocorrências.

**Impactos conhecidos:**
Algumas linhas ficaram longas (if/else numa linha); comportamento funcional inalterado salvo canal visual (Swal).

**Atenção para próximas intervenções:**
Quebrar linhas nos ficheiros mais lidos se a equipa preferir legibilidade; continuar Fase C por área funcional.

---

### [2026-05-13] — Fase A convergência: sessão expirada (AJAX 401) via `LibMessageError` + checklist ordem scripts
**Tipo:** Implementação
**Arquivos tocados:**
- `Scripts/gdi-session-handler.js`
- `Views/Shared/_Layout.cshtml` (comentário de ordem de scripts)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Executar Fase A do plano de convergência: substituir `alert` de feedback ao utilizador em `gdi-session-handler.js` por `LibMessageError` (SweetAlert2), com fallback nativo se Swal não existir; documentar checklist de ordem de carregamento.

**O que foi feito:**
- `notifySessionExpiredThenRedirect`: usa `LibMessageError('Sessão', msg, { callback: redirectToLogin })` quando `LibMessageError` e `GdiSwalCompat.alert` existem; caso contrário `alert` + redirecionamento.
- Redirecionamento para login **após** fechar o Swal (callback do `GdiSwal2.alert`), evitando redirecionar antes do utilizador ver a mensagem (comportamento diferente do `alert` bloqueante, corrigido de forma explícita).
- Cabeçalho JSDoc no `.js` com checklist: jQuery → bundle `libui-swal-compat` → `start.js` → `gdi-session-handler.js`.
- Comentário Razor em `_Layout.cshtml` junto ao include do script.

**Decisões técnicas relevantes:**
- Não alterar outras views nem outros `alert` do ERP nesta fase.

**O que foi evitado e por quê:**
- Mudança de posição dos `<script>` no layout: a ordem já satisfaz o checklist; apenas documentação.

**Impactos conhecidos:**
Páginas que não carreguem `start.js` / Swal mas incluam este handler isoladamente caem no ramo `alert` (fallback).

**Atenção para próximas intervenções:**
Se `_Blank.cshtml` ou outro layout passar a usar `gdi-session-handler.js`, replicar a mesma ordem de scripts ou o fallback cobre.

---

### [2026-05-13] — Cancelamento NF: `jsCancelamentoNF_Step4` com `Swal.fire` + input (protocolo SIARE)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/ModalViewNotasFiscais.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alinhar o pedido de protocolo SIARE ao mesmo modelo visual e de validação já usado em `jsCancelamentoNF_Step3` (`Swal.fire`, botões Bootstrap, `target` no modal).

**O que foi feito:**
`jsCancelamentoNF_Step4` deixou de usar `GdiSwal2.prompt` e passou a `Swal.fire` com `title` + `html` para reproduzir as duas linhas de texto originais, `inputPlaceholder`, `inputValidator` (valor obrigatório após trim — aceita `0`), e encadeamento a `jsCancelamentoNF_Step5` com `result.value.trim()` em `isConfirmed`.

**Decisões técnicas relevantes:**
- Validação apenas de não vazio; não restringir a dígitos para não bloquear formatos de protocolo eventualmente alfanuméricos.

**O que foi evitado e por quê:**
- Alterações noutros ficheiros ou no shim `GdiSwal2.prompt`.

**Impactos conhecidos:**
Comportamento equivalente ao `Step3`: cancelar não chama `Step5`; confirmar sem texto mostra mensagem de validação.

**Atenção para próximas intervenções:**
Fluxo de cancelamento NF neste modal fica todo em `Swal.fire` até ao AJAX do `Step5`.

---

### [2026-05-13] — Cancelamento NF: `jsCancelamentoNF_Step3` com `Swal.fire` + input (substitui `GdiSwal2.prompt`)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/ModalViewNotasFiscais.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Modernizar o pedido de justificativa (`GdiSwal2.prompt`) para `Swal.fire` nativo com `input`, botões Bootstrap (`buttonsStyling: false`, `customClass`), `reverseButtons`, `target` no modal visível, validação de campo vazio, mantendo o título original do ERP e o fluxo para `jsCancelamentoNF_Step4`.

**O que foi feito:**
`jsCancelamentoNF_Step3` passou a usar `Swal.fire` com `input: "text"`, `inputPlaceholder`, `inputValidator` (trim), `result.isConfirmed` e `result.value.trim()` antes de chamar `Step4`. `jsCancelamentoNF_Step4` permanece com `GdiSwal2.prompt` (fora do exemplo fornecido).

**Decisões técnicas relevantes:**
- Título mantido literalmente: `"Informe a Justificatica do Cancelamento"` (texto já usado no sistema).
- `target: document.querySelector(".modal.show") || document.body` para empilhar o Swal corretamente sobre o Bootstrap Modal.

**O que foi evitado e por quê:**
- Migração de outros `GdiSwal2.prompt` do projeto e de `Step4` — escopo limitado ao caso definido no pedido.

**Impactos conhecidos:**
Comportamento mais rigoroso que o `prompt` antigo sem `required`: não aceita confirmação com valor em branco (alinhado ao modelo com `inputValidator`).

**Atenção para próximas intervenções:**
`Step4` (protocolo SIARE) pode seguir o mesmo padrão com `title` em `html` se for desejado alinhar todo o fluxo.

---

### [2026-05-13] — Cancelamento NF: confirmações em `Swal.fire` (substitui `GdiSwal2.dialog` de 2 botões)
**Tipo:** Implementação
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/ModalViewNotasFiscais.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Modernizar diálogos de confirmação com exatamente dois botões (confirmar / desistir) para o padrão nativo SweetAlert2 (`Swal.fire`), mantendo o texto e a ordem dos botões do modelo acordado, sem alterar o restante ERP.

**O que foi feito:**
`jsCancelamentoNF_Step1` e `jsCancelamentoNF_Step2` passaram a usar `Swal.fire` com `html`, `icon: "question"`, `showCancelButton`, `reverseButtons`, `buttonsStyling: false`, `customClass` com classes Bootstrap e `allowEscapeKey: false` (equivalente a `onEscape: false`). Mensagens equivalentes às anteriores; `</br>` normalizado para `<br>`.

**Decisões técnicas relevantes:**
- Apenas o par de confirmações do fluxo de cancelamento neste modal; `GdiSwal2.prompt` (passos seguintes) mantido.
- `Step2` alinhado ao mesmo modelo que `Step1` para UX consistente no fluxo.

**O que foi evitado e por quê:**
- Migração em massa de todos os `GdiSwal2.dialog` do projeto — fora do escopo “cirúrgico” definido.

**Impactos conhecidos:**
Requer `Swal` global (já carregado com o bundle SweetAlert2 nas páginas que abrem este modal).

**Atenção para próximas intervenções:**
Outros modais com o mesmo padrão de 2 botões podem ser migrados caso a caso com o mesmo template.

---

### [2026-05-13] — SweetAlert2: popup sempre claro (anular dark de `prefers-color-scheme`)
**Tipo:** Correção
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/sweetalert2/gdi-swal2-overrides.css`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Com OS/browser em modo escuro, o tema oficial `bootstrap-5.min.css` do SweetAlert2 aplicava fundo escuro ao popup; desejável manter apenas o aspeto claro do diálogo, sem mudar JS nem outras funções.

**O que foi feito:**
Em `gdi-swal2-overrides.css` (já carregado após o tema), adicionado `@media (prefers-color-scheme: dark)` que repõe as variáveis CSS alteradas por esse ficheiro para os valores do tema claro `bootstrap-5`.

**Decisões técnicas relevantes:**
- Ajuste só em CSS centralizado; não tocar em `bootstrap-5.min.css` (vendor) nem no shim JS.

**O que foi evitado e por quê:**
- Desativar `prefers-color-scheme` noutras partes do ERP ou alterar tema AdminLTE.

**Impactos conhecidos:**
Utilizadores em dark mode continuam com app escura, mas alertas/confirmações Swal2 com fundo claro.

**Atenção para próximas intervenções:**
Se no futuro se quiser popup escuro alinhado ao OS, remover este bloco ou condicionar a uma classe no `html`.

---

### [2026-05-13] — Padronização: `bootbox` → `GdiSwal2` + ficheiro `gdi-swal2-dialog-shim.js`
**Tipo:** Refatoração
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-swal2-dialog-shim.js` (novo; substitui `gdi-swal-bootbox-shim.js`)
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (comentário)
- `App_Start/BundleConfig.cs`
- `GDI-ERP-Plataform.csproj`
- Todas as `.cshtml` / `.js` do repo que invocavam `bootbox.alert|confirm|dialog|hideAll|prompt` (substituídas por `GdiSwal2.*`)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Identificar o componente como API GDI sobre SweetAlert2, removendo o nome legado `bootbox` em chamadas, globais e nome de ficheiro.

**O que foi feito:**
1. Global **`GdiSwal2`** (métodos `alert`, `confirm`, `prompt`, `dialog`, `hideAll`) + `window.GdiSwal2`; `GdiSwalCompat` delega para `GdiSwal2`.  
2. Renomeado o módulo para **`gdi-swal2-dialog-shim.js`** e atualizado bundle/`.csproj`.  
3. Substituição em massa de **`bootbox.`** por **`GdiSwal2.`** nas views e scripts (sem manter alias `bootbox`).

**Decisões técnicas relevantes:**
- Não usar o identificador global `SweetAlert2` (reservado à lib / confuso); **`GdiSwal2`** indica stack Swal2 + camada GDI sem colidir com `Swal`.

**O que foi evitado e por quê:**
- Alias `window.bootbox = GdiSwal2` — manter dois nomes dilui a padronização.

**Impactos conhecidos:**
Qualquer script externo ou bookmarklet que ainda chamasse `bootbox` deixa de funcionar até migrar para `GdiSwal2`.

**Atenção para próximas intervenções:**
Novos modais devem usar `GdiSwal2.dialog` / `LibMessageDialog` conforme o padrão da view.

---

### [2026-05-13] — Remover pasta `plugins/bootbox-compat` (obsoleta)
**Tipo:** Refatoração
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/bootbox-compat/` (pasta removida do repositório)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Eliminar a pasta `LibUI_AdminLTE-4.0.0\plugins\bootbox-compat` após migração do shim para `startprime/js/gdi-swal-bootbox-shim.js`.

**O que foi feito:**
Remoção recursiva da pasta no disco de trabalho; o `.csproj` e o `BundleConfig` já não referenciam este caminho.

**Decisões técnicas relevantes:**
- Nenhum ficheiro fonte restante na pasta; evita confusão com artefactos antigos de publish.

**O que foi evitado e por quê:**
- Alterar histórico antigo do CHANGELOG (entradas de 2026-05-12) — mantidas como arquivo.

**Impactos conhecidos:**
Nenhum em runtime; o bundle continua a servir `gdi-swal-bootbox-shim.js`.

**Atenção para próximas intervenções:**
Se `PackageTmp` ainda contiver cópia read-only de `bootbox-compat`, limpar `obj` antes do publish (já documentado na secção «Armadilhas»).

---

### [2026-05-13] — Shim Bootbox/Swal: mover para `startprime/js/gdi-swal-bootbox-shim.js`
**Tipo:** Refatoração
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/gdi-swal-bootbox-shim.js` (novo; conteúdo equivalente ao antigo shim)
- `LibUI_AdminLTE-4.0.0/plugins/bootbox-compat/bootbox-compat.js` (removido)
- `App_Start/BundleConfig.cs` (`~/bundles/libui-swal-compat`)
- `GDI-ERP-Plataform.csproj` (`Content` do novo JS; removido `bootbox-compat`)
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (comentário JSDoc)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Centralizar no startprime as funções globais necessárias ao ERP (`bootbox` + `GdiSwalCompat` sobre SweetAlert2), referenciadas pelo bundle, em vez de `plugins/bootbox-compat/`.

**O que foi feito:**
1. Criado `gdi-swal-bootbox-shim.js` com a mesma API (`GdiSwal2.alert|confirm|prompt|dialog|hideAll` e `window.GdiSwalCompat`).  
2. Bundle `libui-swal-compat`: SweetAlert2 → novo ficheiro (ordem inalterada em relação ao `start.js` no layout).  
3. Removido o ficheiro antigo e entrada `Content` no `.csproj`.

**Decisões técnicas relevantes:**
- Manter o nome global `bootbox` para não alterar dezenas de `.cshtml` com `GdiSwal2.dialog` / `hideAll`.

**O que foi evitado e por quê:**
- Renomear chamadas nas views para `Swal.fire` — fora do escopo; shim preserva contrato.

**Impactos conhecidos:**
Publish antigo em `PackageTmp` pode ainda referenciar `plugins\bootbox-compat` read-only (histórico conhecido); apagar `obj\Release\Package` se o aviso MSBuild voltar.

**Atenção para próximas intervenções:**
Editar o shim apenas em `startprime/js/gdi-swal-bootbox-shim.js`.

---

### [2026-05-13] — `GdiSwalCompat` ausente: `LibMessageAlert` caía no `alert()` nativo
**Tipo:** Correção
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/bootbox-compat/bootbox-compat.js`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Após restauro de backup, chamadas como `LibMessageAlert("Verifique as inconsistências", result.msg)` pareciam «desconfiguradas»: título/corpo sem SweetAlert2, HTML de `result.msg` (`<br/>`, `<b>`) não renderizado.

**O que foi feito:**
Definido `window.GdiSwalCompat` no fim de `bootbox-compat.js`, delegando `alert`, `confirm`, `prompt`, `dialog` e `hideAll` ao shim `bootbox` já existente (que usa SweetAlert2). `start.js` passa a encontrar `GdiSwalCompat` e a usar o fluxo previsto.

**Decisões técnicas relevantes:**
- Implementação no mesmo ficheiro do bundle `~/bundles/libui-swal-compat` (ordem: Swal → bootbox-compat → `GdiSwalCompat` → start.js), sem novo ficheiro nem dependência.

**O que foi evitado e por quê:**
- Alterar centenas de views com `LibMessageAlert(...)` — a causa era global (objeto em falta).

**Impactos conhecidos:**
`LibMessageConfirm`, `LibMessageDialog`, `LibMessagePrompt` (quando usam o ramo Swal) também passam a usar o shim; `prompt` continua com as limitações já existentes em `GdiSwal2.prompt` (não mostra `message` como HTML — comportamento anterior ao ramo `GdiSwalCompat` era fallback incompleto).

**Atenção para próximas intervenções:**
Se existir outro script que defina `GdiSwalCompat`, carregar antes de `bootbox-compat.js` ou remover duplicado.

---

### [2026-05-13] — FormPedidoCreate: `LibMessageProcessando` preso após `draw` na grelha de itens
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
`jsPesquisarGcMovimentosCreatePedidoItensPedido()` chama `LibMessageProcessando("")` e `DataTable().draw(false)` mas nunca `LibMessageProcessandoHide()`; o `waitingDialog` (start.js) permanece aberto («processando») e bloqueia a UI. Evidente ao chamar só o redraw (ex.: botão de teste) sem modal.

**O que foi feito:**
1. `drawCallback` no DataTable de `#dtGcMovimentosCreatePedido` para `LibMessageProcessandoHide()` ao concluir qualquer desenho (inclui redraw após `draw(false)`).  
2. `error.dt` e `ajax.error` passam a chamar `LibMessageProcessandoHide()` antes do alerta.  
3. `xhr.dt`: atualização de `label_total_pedido` só se `json` e propriedade existirem (evita exceção JS que interrompe o fluxo).

**Decisões técnicas relevantes:**
- Centralizar o fecho do overlay no `drawCallback` em vez de duplicar em cada chamada a `draw`, cobrindo também fluxos existentes (ex.: modal item) que reabriam o processando sem fecho garantido após o redraw.

**O que foi evitado e por quê:**
- Remover o botão de teste `btnInserirItem2` — não solicitado; a causa era o overlay global, não o botão.

**Impactos conhecidos:**
Qualquer `draw` na tabela de itens passa a invocar `LibMessageProcessandoHide()`; chamadas a `hide` sem `show` prévio devem ser toleradas por `waitingDialog.hide()` (já encapsulado em try/catch em start.js).

**Atenção para próximas intervenções:**
Se no futuro for necessário manter o `waitingDialog` aberto durante um redraw desta tabela, usar flag ou não acionar `LibMessageProcessando` antes desse `draw`.

---

### [2026-05-12] — Importação SC: não apagar itens ao abrir modal + URL com `idMovimento`
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs` (`ModalImportarExcelSC`, `ModalImportarTxtSC`)
- `Areas/gc/Controllers/MovimentosComprasController.cs` (`ModalImportarExcelSC`)
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `Areas/gc/Views/MovimentosCompras/CreateCotacaoPedidoCompra.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Ao abrir «Importar Lista Itens» / «Importar Excel SC», `DeleteItemTemporario()` apagava todos os `gc_movimentos_itens` do `id_movimento` temporário (negativo), invalidando itens já incluídos antes do modal. A URL usava `?id=0` (não ligava ao parâmetro `idMovimento` da action).

**O que foi feito:**
1. Removida a chamada a `DeleteItemTemporario()` nos GET dos modais de importação (vendas e compras); o arranque «limpo» do pedido continua em `CreateCotacaoPedidoOS` / `CreateCotacaoPedidoCompra` via `DeleteItemTemporario()` já existente.  
2. `ViewBag.idMovimento` nos modais: se `idMovimento` na query for não nulo e ≠ 0, usa-se esse valor; senão mantém-se o negativo do utilizador (comportamento anterior).  
3. `FormPedidoCreate` e `CreateCotacaoPedidoCompra`: `load` do modal com `@Url.Action(..., new { area = "gc", idMovimento = ViewBag.idMovimento })`.

**Decisões técnicas relevantes:**
- Alinhar compras a vendas no mesmo critério de URL e de não limpar ao abrir importação.

**O que foi evitado e por quê:**
- Remover `DeleteItemTemporario()` do create inicial do pedido — alteraria o contrato de «nova ficha vazia».

**Impactos conhecidos:**
Reabrir o modal de importação já não zera a lista temporária; utilizadores que dependiam desse efeito colateral perdem-no (substituído por comportamento explícito).

**Atenção para próximas intervenções:**
`AjaxModalImportarTxtSC` / Excel continuam a gravar itens com `id_movimento` negativo do utilizador na lógica atual — independentemente do `ViewBag` passado ao modal (só o cabeçalho do modal fica coerente com a URL).

---

### [2026-05-12] — Remover sourceMappingURL de `popper.min.js` (Tempus Dominus)
**Tipo:** Correção
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/tempus-dominus-6.9.4/js/popper.min.js`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Ferramentas de debug / browser pediam `popper.min.js.map` (404 IIS); mensagem confundida com «erro de compilação» — não é MSBuild.

**O que foi feito:**
Removida a linha final `//# sourceMappingURL=popper.min.js.map` do `popper.min.js` bundle do Tempus Dominus, pois o `.map` não está versionado no repositório.

**Decisões técnicas relevantes:**
- Não adicionar ficheiro `.map` volumoso só para debug; remoção da diretiva é padrão quando não se distribuem maps.

**O que foi evitado e por quê:**
- Alterar `_Layout` / `_Modal` — desnecessário.

**Impactos conhecidos:**
Stack traces de `popper.min.js` no DevTools deixam de resolver ao fonte original (aceitável em produção).

**Atenção para próximas intervenções:**
Se ao atualizar Tempus Dominus o ficheiro for substituído, a diretiva pode voltar — repetir ou incluir o `.map` no deploy.

---

### [2026-05-12] — DataTables: aviso coluna 1 após «Importar Lista Itens» (pedido gc)
**Tipo:** Correção
**Arquivos tocados:**
- `Areas/gc/Controllers/MovimentosController.cs` (`GetDadosItensPedido`)
- `Areas/gc/Views/Movimentos/FormPedidoCreate.cshtml`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Alerta DataTables `Requested unknown parameter '1' for row 0, column 1` na tabela `dtGcMovimentosCreatePedido` após processar o modal «Importar Lista Itens».

**O que foi feito:**
1. `GetDadosItensPedido`: **LEFT JOIN** em `g_produtos` para incluir itens com `id_produto = 0` (comum após import SC antes da vinculação ao ERP); `ProdutoNome` com `p == null ? null : p.nome`.  
2. Só anexar textos de «Cotação Compra» quando `RecordMovimento != null` (evita `NullReferenceException` com `id_movimento` temporário negativo sem cabeçalho em `gc_movimentos`).  
3. `FormPedidoCreate.cshtml`: `ajax.dataSrc: 'aaData'` no DataTable para alinhar à resposta legado do servidor.

**Decisões técnicas relevantes:**
- Manter formato `aaData` / `iTotalRecords` já usado pelo controlador.

**O que foi evitado e por quê:**
- Refatorar todo o DataTable para API 2.x pura — fora do escopo.

**Impactos conhecidos:**
Itens sem produto GDI passam a aparecer na grelha (coluna produto pode ficar vazia até vinculação).

**Atenção para próximas intervenções:**
Replicar padrão LEFT JOIN noutras queries de itens se o mesmo sintoma aparecer.

---

### [2026-05-12] — Excluir `.cursor`, `.md` e `CLAUDE.md` do Web Publish (manter no Git)
**Tipo:** Implementação
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Pastas `.cursor\`, `.md\` e ficheiro `CLAUDE.md` devem permanecer no repositório mas **não** ser copiados na publicação IIS.

**O que foi feito:**
Substituídos os `Content Include` pontuais por `None Include`: glob `.cursor\**\*`, glob `.md\**\*`, e `CLAUDE.md`. Em projetos Web ASP.NET, `Content` é empacotado no publish; `None` não é implantado por omissão.

**Decisões técnicas relevantes:**
- Um único bloco com comentário no `.csproj` para futuros ficheiros sob `.cursor` / `.md` herdarem o mesmo comportamento.

**O que foi evitado e por quê:**
- `wpp.targets` / exclusões por perfil — desnecessário enquanto os itens não forem `Content`.

**Impactos conhecidos:**
Publish (File System / FTP / Web Deploy) deixa de incluir esses caminhos no `PackageTmp`/destino.

**Atenção para próximas intervenções:**
Se algo em `.md` ou `.cursor` for marcado como `Content` noutro `ItemGroup`, voltará a publicar — rever ao adicionar.

---

### [2026-05-12] — Remoção segura do pacote NuGet SixLabors.ImageSharp (P3)
**Tipo:** Refatoração
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj`
- `packages.config`
- `.md/relatorio-migracao-netframework-472-481.md` (tabela de dependências)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Plano P3: o projeto só importava `SixLabors.ImageSharp.props` e validava existência com `Error`; não havia `Reference` a `SixLabors.ImageSharp.dll` nem uso no código.

**O que foi feito:**
Removidos o `<Import ... SixLabors.ImageSharp.props />` no início do `.csproj`, a linha `Error` correspondente em `EnsureNuGetPackageBuildImports` e o pacote em `packages.config`. Atualizada a tabela no relatório de migração. `MSBuild /t:Restore,Build /p:Configuration=Release` concluído com sucesso.

**Decisões técnicas relevantes:**
- Manter `SixLabors.Fonts` (referência explícita; cadeia ClosedXML).

**O que foi evitado e por quê:**
- Remover `SixLabors.Fonts` → ainda necessário.

**Impactos conhecidos:**
Pasta local `packages\SixLabors.ImageSharp.3.1.12` pode permanecer até limpeza manual.

**Atenção para próximas intervenções:**
Se for necessário processamento de imagem com ImageSharp, reinstalar o pacote e adicionar `Reference` explícita se o SDK props não bastar em net472.

---

### [2026-05-12] — Remoção segura do pacote NuGet SkiaSharp e NativeAssets (P2)
**Tipo:** Refatoração
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj`
- `packages.config`
- `.md/relatorio-migracao-netframework-472-481.md` (tabela de dependências)
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Plano P2: remover SkiaSharp e pacotes `SkiaSharp.NativeAssets.*` (sem uso no código; barcodes via `System.Drawing` / Zen.Barcode).

**O que foi feito:**
Removida referência `SkiaSharp`, três linhas `Error` e três `Import` de targets no `EnsureNuGetPackageBuildImports` / final do projeto; removidas quatro entradas em `packages.config`. Build Release com MSBuild concluído com sucesso.

**Decisões técnicas relevantes:**
- Utilizador confirmou ausência de DLLs de terceiros que carreguem SkiaSharp por reflexão.

**O que foi evitado e por quê:**
- Alterar `SixLabors.ImageSharp` / `SixLabors.Fonts` → fora do escopo P2.

**Impactos conhecidos:**
Pastas `packages\SkiaSharp*` podem permanecer localmente até limpeza; deploy IIS deixa de incluir `SkiaSharp.dll` e nativos.

**Atenção para próximas intervenções:**
Se no futuro se integrar renderização via Skia, reintroduzir pacote + targets.

---

### [2026-05-12] — Remoção segura do pacote NuGet ZString (P1)
**Tipo:** Refatoração
**Arquivos tocados:**
- `GDI-ERP-Plataform.csproj`
- `packages.config`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Executar plano P1: excluir dependência não utilizada (`ZString`), alinhando projeto e restore.

**O que foi feito:**
Removida a entrada `<Reference Include="ZString" ...>` do `.csproj` e a linha `<package id="ZString" ...>` do `packages.config`. Validação: `MSBuild /t:Restore,Build /p:Configuration=Release` concluído com sucesso.

**Decisões técnicas relevantes:**
- Nenhum `using` ou tipo `ZString` no código-fonte — remoção sem substituto.

**O que foi evitado e por quê:**
- Remoção de outros pacotes (SkiaSharp, ImageSharp, etc.) → fora do escopo P1; exigem análise transitiva.

**Impactos conhecidos:**
Nenhum em runtime; pasta local `packages\ZString.2.6.0` pode permanecer até limpeza manual ou `nuget locals clear`.

**Atenção para próximas intervenções:**
Em máquinas de dev: opcionalmente apagar `packages\ZString.2.6.0` e correr restore.

---

### [2026-05-12] — Remoção de `_filestemp` do disco e reforço no `.gitignore`
**Tipo:** Implementação
**Arquivos tocados:**
- `_filestemp\` (pasta apagada do working tree — não estava rastreada pelo Git)
- `.gitignore`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Eliminar fisicamente `_filestemp` e subpastas; garantir que conteúdo gerado em runtime não volte a ser commitado.

**O que foi feito:**
`Remove-Item -Recurse -Force` na raiz do projeto. Confirmado `git ls-files` sem entradas em `_filestemp` (pasta já era ignorada; artefactos eram locais). No `.gitignore`: comentário alinhado a `Server.MapPath("~/_filestemp")`, mantido `_filestemp/` e acrescentado `**/_filestemp/`; removida linha acidental `/Areas/g/Controllers/ProdutosController.cs` que seguia o bloco (ruído, sem efeito útil no padrão de paths do repo).

**Decisões técnicas relevantes:**
- Não criar `.gitkeep`: a aplicação recria ficheiros sob `_filestemp` quando necessário (vários controllers).

**O que foi evitado e por quê:**
- `git rm` em massa → não havia paths rastreados com prefixo `_filestemp`.

**Impactos conhecidos:**
Primeira execução após deploy em servidor limpo: pastas filhas são criadas sob demanda pelo código existente.

**Atenção para próximas intervenções:**
Em IIS, garantir permissão de escrita na raiz da app para `_filestemp` quando relatórios/uploads forem gerados.

---

### [2026-05-12] — Publish VS: aviso Access denied em `bootbox-compat` (PackageTmp)
**Tipo:** Análise
**Arquivos tocados:**
- `.cursor/CHANGELOG-DEV.md`
- Removido localmente: `obj\Release\Package\PackageTmp\LibUI_AdminLTE-4.0.0\plugins\bootbox-compat` (artefato de build, não versionado)

**Problema / Demanda:**
Na publicação, MSBuild emitiu: `Warning : Access to the path 'bootbox-compat' is denied` (`Microsoft.Web.Publishing.targets` ao copiar para `PackageTmp`).

**O que foi feito:**
Confirmado que **não** existe `LibUI_AdminLTE-4.0.0\plugins\bootbox-compat` no código fonte (substituído por `sweetalert2\`). Em `PackageTmp` existia pasta residual `...\plugins\bootbox-compat` com atributo **somente leitura**, impedindo o pipeline de empacotamento de atualizar/remover o caminho. Pasta removida com `Remove-Item -Recurse -Force` (e tentativa de `attrib -R`).

**Decisões técnicas relevantes:**
- Tratar como problema de **cache de build** (`obj`), não de referência no `.csproj`.

**O que foi evitado e por quê:**
- Reintroduzir `bootbox-compat` no repositório → desnecessário e contrário à stack atual (SweetAlert2).

**Impactos conhecidos:**
Outros devs/CI: se o aviso voltar, limpar `obj` antes do publish.

**Atenção para próximas intervenções:**
Fechar VS, apagar pasta `obj` inteira e republicar se permissões persistirem; checar antivírus/OneDrive sobre a pasta do projeto.

---

### [2026-05-12] — Verificação de referências a bootbox-compat
**Tipo:** Análise
**Arquivos tocados:**
- `.cursor/CHANGELOG-DEV.md` (apenas este registro)

**Problema / Demanda:**
Verificar e corrigir referências ao componente `bootbox-compat` no código.

**O que foi feito:**
Varredura em todo o workspace (`.cshtml`, `.js`, `.cs`, `.csproj`, `.md`, `.cursor`): **nenhuma** ocorrência de `bootbox-compat`, `bootbox`, `libui-bootbox` ou arquivo `*bootbox*`. O padrão atual é SweetAlert2 + `gdi-swal-compat.js` registrado em `BundleConfig.cs` como `~/bundles/libui-swal-compat`, referenciado em `_Layout.cshtml`, `_Modal.cshtml`, `_Blank.cshtml` e views de identidade. Nenhuma correção de código foi necessária.

**Decisões técnicas relevantes:**
- Manter `libui-swal-compat` como camada de diálogos (substitui Bootbox historicamente).

**O que foi evitado e por quê:**
- Criar shim `bootbox-compat` → desnecessário: não há referências quebradas no repositório.

**Impactos conhecidos:**
Nenhum — apenas confirmação de estado.

**Atenção para próximas intervenções:**
Se aparecer 404 a `bootbox-compat` no navegador, checar cache/CDN ou HTML gerado fora deste repo; no código fonte atual o bundle correto é `~/bundles/libui-swal-compat`.

---

### [YYYY-MM-DD] — Inicialização do CHANGELOG-DEV
**Tipo:** Configuração
**Arquivos tocados:**
- `.cursor/rules`
- `.cursor/CHANGELOG-DEV.md`

**Problema / Demanda:**
Configuração inicial do ambiente de desenvolvimento com Cursor AI, definindo regras de comportamento do assistente e estrutura de memória do projeto.

**O que foi feito:**
Criados os arquivos `.cursor/rules` e `.cursor/CHANGELOG-DEV.md` para padronizar o comportamento do Cursor em todas as intervenções futuras e manter histórico estruturado da evolução do projeto.

**Decisões técnicas relevantes:**
- O Cursor deve ler este arquivo antes de qualquer intervenção para manter contexto acumulado
- Registros devem ser mínimos e objetivos — foco em decisões e alertas, não em descrição de código
- Novas entradas sempre no topo da seção de histórico

**O que foi evitado e por quê:**
- Uso de ferramentas externas de changelog → mantido dentro do próprio repositório para garantir acesso imediato pelo Cursor

**Impactos conhecidos:**
Nenhum — arquivos de configuração apenas.

**Atenção para próximas intervenções:**
- Sempre atualizar este arquivo ao final de cada sessão de trabalho
- Registrar padrões de código descobertos na seção "Padrões estabelecidos no projeto" do contexto geral acima
- Registrar arquivos críticos ou sensíveis na seção correspondente acima conforme forem identificados

---

<!-- PRÓXIMAS ENTRADAS ACIMA DESTA LINHA -->
