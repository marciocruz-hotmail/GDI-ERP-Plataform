# CHANGELOG-DEV.md

> Changelog **operacional** para Cursor / Claude Code (~200 linhas).  
> **Histórico integral (187 entradas, ~2900 linhas):** `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`  
> **Contexto fixo:** `AI-CONTEXT.md` | **Pendências:** `BACKLOG-DEV.md`

**Última atualização:** 2026-05-20 (PUB-1/PUB-2 health + Release)

---

## Estado atual do projeto

O **GDI-ERP-Plataform** é um monólito ASP.NET MVC (.NET Framework **4.7.2**) para gestão COMEX, comercial, estoque, financeiro e qualidade da GDI Aviação. O portal público do cliente (**GDI-PortalCliente**) foi integrado neste repositório (área `crm`, `UserIdentity/AcessoPortal`).

A modernização em curso (2026) concentrou-se em: (1) substituição de **`LibDataSets`** por **`ILookupQueryService`** com cache e partials por domínio; (2) padronização **DataTables/Ajax** (`GdiDt*` / `GdiAjax*`, JSON `errorMessage` no servidor); (3) **Index** com filtro inline, paginação SQL e `deferLoading` onde aplicável; (4) remoção de módulos legados (Filtros genérico, PortalVendedor, FinanceiroFaturamentos/Lancamentos em `g`, Requisicoes, etc.); (5) higiene **PascalCase** (paths Git `Cst*`, views `Modal*`); (6) segurança incremental (**CSRF**, **XSS** em Atendimentos, `customErrors` Release); (7) documentação enxuta para IA (`AI-CONTEXT`, `BACKLOG`, este ficheiro + arquivo histórico).

**Build** Release|AnyCPU OK nas intervenções recentes. **VersionERP:** `2026.51.27` — incrementar após alterações em `start.js` / `start.css` / `gdi-select2.js`. **UTF-8:** sem BOM no inventário de 2026-05-20.

---

## Decisões técnicas ativas

### Plataforma e processo

- Manter **.NET Framework 4.7.2** nesta fase; migração **4.8.1** em trilha **separada** (não misturar PRs).
- Modernização **incremental**, commits/PRs pequenos, **baixo risco**.
- **Fonte de verdade:** código, `.csproj` e `Views/` prevalecem sobre documentação desatualizada.
- **Não** alterar schema SQL Server / SPs sem autorização explícita.
- **Não** remover funcionalidades, rotas ou POSTs sem mapeamento de uso.
- Agente: **sem** `git push` nem publish remoto; português BR nas respostas.

### UI e front-end

- Stack fixa: **Bootstrap 5**, **AdminLTE 4**, **DataTables bs5**, **SweetAlert2**, **Tempus Dominus** — sem substituir versões.
- Dois tipos de tabela: **DataTables** (Ajax, `GetDados*`, `scroll-body-horizontal`) vs **MVC** (`@for`, `gdi-form-table-*`) — não misturar CSS/contratos.
- Mensagens: `LibMessage*` / `GdiDt*` / `GdiAjax*` em `start.js`; evitar `alert()` nativo (Fase 7 concluída no histórico).
- Cache de assets: `?v=VersionERP` no layout.

### DataTables e Ajax

- Servidor `GetDados*`: `param` nulo, `try/catch`, JSON com `errorMessage`, `severity`, `stackTrace`, `yesFilterOnOff`, `aaData` vazio.
- Cliente: `error.dt` + `GdiDtNotifyJsonErrorMessage` + **`return`** antes de processar `aaData`.
- APIs **não-DataTables** com `{ ok, error }` (ex. LMS) mantêm contrato próprio.

### Lookups

- **`LibDataSets.cs` removido** (Onda 6b). Usar **`ILookupQueryService`** + `*.Lookups.cs`.
- **Index/filtro:** combo/query **local** na action (sem cache global).
- **CreateEdit/modal partilhado:** `PreencherLookups*` via serviço.
- Typeahead pedidos: `GetClientesLookup` / `GetProdutosLookup` (Select2 Ajax).

### Portal e áreas

- Portal **externo:** `crm` + `UserIdentity/AcessoPortal` (hosts `*.portalflightx.com`).
- `Areas/g/PortalCliente` = legado interno; **não** confundir com portal `crm`.

### Registo de mudanças

- Atualizar **tabela** «Últimas alterações» neste ficheiro + `BACKLOG-DEV.md`.
- Detalhe extenso → `.cursor/context/AAAA_MM_DD_*.md`, não reexpandir o changelog operacional.

---

## Últimas alterações relevantes

### 2026-05-25 — NF entrada nacional: Select2 ausente (LayoutLite) + scroll horizontal

- **Lookups vazios:** `FormProcessarNFCompraNacional` estava em `LayoutLiteActionsByController` (G-PERF-20) — **Select2 não carregava**; combos ficavam `<select>` nativo só com `[ SELECIONE ]`.
- **Correção:** `[GdiPageScripts(... | Select2)]` na action; placeholder vazio no combo (contrato Ajax); `gdi-select2.js` placeholder explícito; layout tabela sem `table-responsive` duplicado.
- **VersionERP:** `2026.51.29`.

### 2026-05-25 — NF entrada nacional: CS0122 LookupSearchQueries na view

- **Causa:** `FormProcessarNFCompraNacional.cshtml` chamava `LookupSearchQueries` (`internal`) — views Razor compilam em assembly separado.
- **Correção:** combos por linha montados no controller (`PreencherLookupsComboProdutosEntradaNacionalPorLinha` → `ViewBag.comboProdutosPorLinha`).

### 2026-05-25 — Cartas de Correção: COUNT DataTables com coluna duplicada

- **Causa:** `GetDadosCartaCorrecao` usava `SELECT *` com `JOIN gc_movimentos_nf` — ambas tabelas expõem `id_movimento_nf`; `LibDataTableSqlPaging.SqlCount` envolve em subquery `_cnt` e o SQL Server falha.
- **Correção:** `SELECT cr.*` (filtro por `nf.id_movimento` mantido no JOIN).

### 2026-05-25 — NF entrada nacional: lookup produto por linha (typeahead Ajax)

- **Causa:** `FormProcessarNFCompraNacional` repetia `GetComboGcProdutosServicosTodos` em cada linha da tabela MVC — Select2 estático com milhares de opções impedia seleção (performance/overflow).
- **Correção:** combo mínimo por linha (`BuildComboProdutoEntradaNacionalLinha`) + `GetProdutosLookup` em `MovimentosEntradasController.LookupAjax.cs` (roles `gc_MovimentosEntradas_*`); `nome_produto` ao auto-match por código externo.

### 2026-05-25 — GED upload: extensão `.csv` permitida em anexos

- **Causa:** `ServiceUploadFileGed` validava extensão via `_extensoesGedPermitidas` sem `.csv` — upload rejeitado antes do S3 em todos os modais que usam `AjaxUploadFileGed` (pedidos, financeiro, atendimentos, COMEX, etc.).
- **Correção:** inclusão de `.csv` na whitelist e mensagem de erro alinhada.

### 2026-05-21 — Lookup Ajax: sem modal em pedido abortado (digitação)

- **Causa:** ao digitar no Select2, o GET anterior é cancelado (`textStatus: abort`); `gdi-select2.js` tratava como erro e exibia *Não foi possível carregar os resultados do lookup* mesmo com o pedido seguinte OK (ex.: FinanceiroLancamentos Cli./For.).
- **Correção global:** `gdiSelect2IsLookupAjaxAbort` em `gdi-select2.js` — só notifica em falha real (404, 401/403, JSON erro). **VersionERP:** `2026.51.27`.

### 2026-05-21 — Auditoria lookups Ajax: `gc/Estoque/Index` produto

- **Lacuna:** `ComboFiltroProdutoPosicaoEstoqueIndex` sem `data-gdi-lookup-url` em `Estoque/Index` (único host typeahead produto/cliente em falta).
- **Correção:** `GetProdutosLookup` + `area = "gc"` + allowClear. Inventário completo em `.cursor/context/2026_05_20_lookups-typeahead-ajax-pedidos.md`.

### 2026-05-21 — Lookup Ajax: `area = ""` em `ClientesLookup` + erro Select2

- **Causa:** `Url.Action("…", "ClientesLookup")` em views de área gerava `/g/ClientesLookup/...` ou `/gc/ClientesLookup/...` (404) → Select2 *Os resultados não puderam ser carregados*.
- **Correção:** `new { area = "" }` em todas as views que apontam para `ClientesLookup`; `gdi-select2.js` — `transport` com mensagem legível em falha Ajax (404/401/403/JSON erro).

### 2026-05-21 — Lookup clientes: `data-gdi-lookup-url` nas views pós-CACHE-2

- **Causa:** combos migrados para placeholder (1 opção) sem atributos Ajax → `gdi-select2.js` não inicializa Select2 (`nOpts ≤ 5` sem `data-gdi-lookup-url`).
- **Correção:** atributos typeahead em `g/Clientes/Index`, `gc/FinanceiroLancamentos` (Index + modal ComDoc), `gc/Movimentos` (`IndexPedido`, `PainelPedidos`, `ModalConsultaPedidos`), `g/Atendimentos`, `g/Financeiro/Index`, `g/ContratosAviacao`, `gc/RelatoriosFinanceiros` modal.
- **Endpoints:** pedidos → `gc/Movimentos/GetClientesLookup`; cadastro/financeiro → `ClientesLookup/GetClientesFornecedores*`; atendimentos → `g/Atendimentos/GetClientesLookup`.
- **VersionERP:** `2026.51.25`. Detalhe: `.cursor/context/2026_05_20_lookups-typeahead-ajax-pedidos.md`.

### 2026-05-21 — G-PERF-20: TempusDominus global + correções flags (Parametros DT, IndexPops jstree)

- **`_LayoutHeadOptionalScripts` / `_LayoutScriptsOptional`:** carregam Tempus quando `ViewBag.GdiPageScripts` inclui `TempusDominus` (8).
- **`GdiPageScriptsDefaults`:** mapa de rotas com `jsDatepicker*` (actions MVC Create/Edit/IndexPedido, etc.); hubs `LayoutHubReport*` passam a incluir Tempus; área **`a`** + `Parametros` → DataTables.
- **`GedSGQ/IndexPops`:** `[GdiPageScripts(LayoutHubJstree)]`.
- **Governança:** `Scripts/2026_05_20_gdi_cross_audit_view_libraries.py` — exit **1** se hosts CRITICAL; relatório `.cursor/context/2026_05_20_relatorio-auditoria-bibliotecas-views-modais.md`.
- Corrige alerta `[jsDatepickerFirstDayMonth] Tempus Dominus não carregado` (IndexPedido, FinanceiroLancamentos, etc.) sem partials por view.

### 2026-05-20 — Mensagens: LibMessageConfirm + fim dos LibMessageDialog em views

- **`start.js`:** `LibMessageConfirm`, `LibMessageConfirmChecklist`, `GdiConfirmDesativarAnexo` (SweetAlert2 `confirm` com ícone, labels HTML, `size`).
- **`gdi-swal2-dialog-shim.js`:** `GdiSwal2.confirm` — `icon`, `size`→`width`, `backdrop`, `allowEscapeKey`, `html`.
- **17 blocos** (12 views) com 2 botões migrados de `LibMessageDialog` → novos helpers; **0** `LibMessageDialog` em `Areas/**/*.cshtml`.
- Views: pedidos (excluir item, anexos), clientes/vendedores, separação/expedição (checklist), medição, parâmetros ON/OFF, cancelar título financeiro.
- **`VersionERP`:** `2026.51.23`. Detalhe: `.cursor/context/2026_05_20_libmessage-confirm-arquitetura.md`.

### 2026-05-20 — Mensagens: LibMessageDialog (OK único) → LibMessageSuccess em massa

- **~102 blocos** em **90 views** `Areas/**` migrados de `LibMessageDialog` (confirmação, botão OK) para `LibMessageSuccess` + `callback` preservado.
- Scripts: `Scripts/2026_05_20_gdi_migrate_libmessage_dialog_single_ok.py`, `Scripts/2026_05_20_gdi_restore_and_migrate_libmessage.py` (restauração de backup `_filestemp` + migração com extração de callback por chaves).
- **Mantidos** `LibMessageDialog` com cancelar+confirmar (~15 blocos): exclusões, confirmações duplas, checklist separação, etc.
- Correção manual: `Financeiro/ModalTransferirContaCaixa.cshtml` (sem backup; forEach/callback).
- Piloto: `FormPedidoCreate` (`jsAjaxSavePedido` / `jsAjaxPosVendaPedido`).

### 2026-05-20 — Pedido CreateEdit: sucesso Ajax → LibMessageSuccess (piloto fase A)

- **`FormPedidoCreate.cshtml`:** `jsAjaxSavePedido` e `jsAjaxPosVendaPedido` — troca de `LibMessageDialog` (1× OK, sem ícone) por `LibMessageSuccess` + `callback` (padrão Atendimentos/Ged).
- **Inventário global:** `Scripts/2026_05_20_gdi_inventory_libmessage_dialog_single_ok.py` — candidatos fase A concluídos em `Areas/`.

### 2026-05-20 — Cotação/Pedido: AjaxSavePedido — g_produtos id_produto_substituto

- **Erro:** `data reader is incompatible with g_produtos` / coluna `id_produto_substituto` ausente no reader ao salvar cotação.
- **Causa:** `LoadProdutosPedidoItens` (PERF-010) usava `SqlQuery` com colunas parciais; o modelo EF exige todas as colunas mapeadas.
- **Correção:** `LoadProdutosPedidoItens` passa a usar EF `AsNoTracking` + filtro por itens do movimento (entidade completa, parametrizado).

### 2026-05-20 — DataTables: correção global `.draw()` (Limpar/Pesquisar Index)

- **Causa:** `gdi-datatables-defaults.js` tinha substituído `$.fn.DataTable` pelo mesmo wrapper de `$.fn.dataTable`; o **D maiúsculo** deve devolver **Api** (`.api()`), não jQuery — `$("#dt").DataTable().draw(false)` falhava em Produtos, Clientes e ~18 Index.
- **Correção:** restaurar `$.fn.DataTable = function(opts) { return $(this).dataTable(opts).api(); }`; helpers `GdiDataTableApi` / `GdiDataTableDraw`. **VersionERP** `2026.51.22`.

### 2026-05-20 — DataTables: filtro obrigatório — mensagem «aguardando filtro» (não «Carregando...»)

- **Causa:** com `deferLoading`, o DT usa `sLoadingRecords` enquanto `settings.json` é indefinido (`_emptyRow`).
- **Correção global:** `gdi-datatables-defaults.js` — hook em inits com `deferLoading` (sincroniza `sLoadingRecords` ← `sEmptyTable`/`gdiAwaitingFilter`; após 1.º `xhr.dt` limpa `sEmptyTable` para `sZeroRecords` em pesquisa vazia).
- **Telas:** ~19 Index (Clientes, Produtos, Cfop, etc.) sem alteração por view. **VersionERP** `2026.51.21`. Detalhe: `.cursor/context/2026_05_20_datatables-filtro-obrigatorio-mensagem.md`.

### 2026-05-20 — DataTables: PT-BR global (todas as grelhas)

- **`gdi-datatables-defaults.js`:** `oLanguage` completo (paginação, pesquisa, processando, select/aria, length «Todos»); expõe `window.GdiDataTablesPtBr`.
- **Carregamento:** `_LayoutScriptsDataTables` (já existente) + **`GdiPageScriptRegistry`** bundle `dataTables` (modais lazy) + **`_Blank.cshtml`**.
- **~170 inits** sem bloco `language:` nas views passam a herdar PT-BR dos defaults; blocos `language:` locais mantêm mensagens customizadas (`sEmptyTable` por tela).
- **VersionERP** `2026.51.20`.

### 2026-05-20 — DataTables: colspan «Nenhum registro encontrado» (auditoria global)

- **Causa:** `gdi-dt-scroll-host` + `table-layout: fixed` quebrava `colspan` da linha vazia (mensagem só na col. 1).
- **Correção global:** `start.css` — `table-layout: auto` quando `tbody td.dt-empty`; `td.dt-empty` com `nowrap`.
- **Grelhas ≤10 col.:** removido host (padrão Index) em 17 tabelas (Atendimentos, Clientes abas, COMEX, FormPedido, modais GED, Nfe logs, etc.) — script `2026_05_20_gdi_strip_host_narrow_datatables.py`.
- **Mantém host (só CSS):** IndexPedido, PainelPedidos, DadosConsolidados, Clientes/Contatos (11+ col.) — **VersionERP** `2026.51.19`.

### 2026-05-20 — Atendimentos/Edit Anexos: DataTable padrão Index (sem gdi-dt-scroll-host)

- **Causa:** `gdi-dt-scroll-host` + `table-layout: fixed` na grelha GED fazia «Nenhum registro encontrado» ficar só na coluna 1 (colspan quebrado).
- **Correção:** `#dtGAtendimentosGed` — markup e init alinhados a `Atendimentos/Index` (tabela no `card-body`, sem host/wrapper; sem `initComplete`/`dt-wrap`; `TableGed`).

### 2026-05-20 — DataTables: mensagem vazia PT-BR (`gdi-datatables-defaults.js`)

- **Causa:** DataTables 2.x ignora a chave `language` no init (só `oLanguage`); tabelas mostravam texto em inglês ou célula vazia sem «Nenhum registro encontrado» (ex.: Anexos em `Atendimentos/Edit`).
- **Correção:** `gdi-datatables-defaults.js` (defer após `datatables.min.js`); CSS `td.dt-empty` no host; `Atendimentos/Edit` GED com `oLanguage` + guard reinit. **VersionERP** `2026.51.18`.

### 2026-05-20 — DataTables: quebra de linha por omissão (`gdi-dt-scroll-host` / `dt-wrap`)

- **Causa:** `.dt-wrap` em `aoColumnDefs` não vencia a regra `nowrap` + `ellipsis` de `.gdi-dt-scroll-host` (especificidade CSS) — Audit Trail truncado em `Clientes/Edit`.
- **Correção:** `start.css` — padrão = quebra (`word-break`, `overflow-wrap`); opt-out `.dt-nowrap` (data/ícone); `.dt-wrap` com especificidade correta. **VersionERP** `2026.51.17`.
- **Views audit:** `dt-nowrap` na coluna Dt/Hora em Clientes, Produtos, ComexImportacoes, FormPedidoCreate.

### 2026-05-20 — ComexImportacoes/Edit: correção markup aba Itens (`CreateEdit`)

- **Causa:** na migração G-DT-08 faltou `</div>` do wrapper `table-responsive` na aba Itens — markup inválido (tab-pane sem fecho correto).
- **Correção:** `Areas/gc/Views/ComexImportacoes/CreateEdit.cshtml` — fecho do `scroll-body-horizontal` antes do botão «Carregar Itens».

### 2026-05-20 — G-DT-08 Lotes A+ e B: `gdi-dt-scroll-host` em abas CreateEdit críticas

- **A+:** `FormPedidoCreate` (itens + audit), `ComexImportacoes/CreateEdit` (itens, GED, invoices PDF, audit — invoices expandidas 13 col. mantém scroll largo).
- **B:** `Clientes/CreateEdit`, `Atendimentos/Edit`, `ModalConsultaPedidos`, `Financeiro/DadosConsolidados`, `Nfe/CreateEdit` (logs).
- Padrão: `gdi-dt-scroll-host min-w-0`, `initComplete` + `columns.adjust`, `dt-wrap` em colunas de texto.

### 2026-05-20 — G-DT-08 Lote A: modais anexos/GED com `gdi-dt-scroll-host`

- `ModalPedidoViewAnexos`, `ModalFinanceiroViewAnexos`, `ModalCreateEditLancamento` (aba GED), `EstoqueLotes/ModalCreateEdit` (documentos).

### 2026-05-20 — Auditoria DataTables vs `gdi-dt-scroll-host`

- Script `Scripts/2026_05_20_gdi_audit_dt_scroll_host.py`; relatório `.cursor/context/2026_05_20_datatables-gdi-dt-scroll-host-auditoria.md` (138 views DT, 3 com host, 38 candidatas ≤10 colunas).

### 2026-05-20 — EditPedido aba Anexos: DataTable sem scroll horizontal

- `FormPedidoCreate` tab GED: wrapper `gdi-dt-scroll-host min-w-0` (tabela 100% do card; evita `max-content` solto).
- `jsInitTableGcMovimentosGed`: `columns.adjust`, colunas Descrição/Arquivo com `dt-wrap`.

### 2026-05-20 — EditPedido aba Invoice/SO: AjaxComboComexImportacoesPedido

- Removido `[HttpPost]` indevido na action GET (erro genérico «Falha ao processar os dados error» ao abrir aba).
- `FormPedidoCreate`: feedback `success:false` e mensagem HTTP legível no Ajax do combo importações.

### 2026-05-20 — Atendimentos/Edit: acentuação no campo Categoria

- `ViewBag.MsgCategoria` sem `EncodeAtendimentoDisplay` (evita dupla codificação com `@ViewBag` na view).

### 2026-05-20 — Clientes/Index: erro de análise Razor (Tempus partial)

- Removido `@{` aninhado dentro do bloco `@{}` ao incluir `_LayoutHeadTempus` (linha 6).

### 2026-05-20 — GdiMainModalLoad: modal hub sem reload Tempus duplicado

- Hubs com `_LayoutScriptsTempus` (defer): `gdiEnsureScriptFlagsForModal` aguarda API Tempus já no DOM em vez de injetar de novo (DevTools Finish ~25 s no modal ANP com +3 kB).
- `GdiLoadScriptOnce` reutiliza `<script defer>` existente; deteção de flags no HTML normalizado do `<body>`.
- `VersionERP` **2026.51.16**.

### 2026-05-20 — Hubs relatório gc (4× Index): modais alinhados

- **Layout (todas):** `bootstrap.bundle.min.js` quando sem DataTables (`_LayoutScriptsOptional`); `GdiMainModalLoad` + corpo `_Modal.cshtml` (**2026.51.14–15**).
- **Index:** callbacks `$("#mainModal").load` → `GdiMainModalShow()` em Cadastrais, Comerciais, Financeiros, Regulamentação (9+4+1+1 funções).
- **Presets:** Cadastrais/Comerciais/Regulamentação `LayoutHubReport`; Financeiros `LayoutHubReportSelect2` + partials Tempus na Index.
- `VersionERP` **2026.51.15**.

### 2026-05-20 — GdiMainModalLoad: modais hubs (_Modal.cshtml)

- `gdiNormalizeModalAjaxHtml`: extrai conteúdo de `<body>` quando a action devolve documento completo (`_Modal.cshtml`); corrige modais que não abriam (ex. `RelatoriosComerciais`).
- `VersionERP` **2026.51.14**.

### 2026-05-20 — Hubs relatórios gc: layout Index (Cadastrais, Comerciais, Financeiros, Regulamentação)

- Removido `scroll-body-horizontal` em listas de botões MVC; grelha `row g-2` em Comerciais; `d-grid` nos hubs simples.
- Modais: `GdiMainModalLoad` + scripts inline (**2026.51.13**); estrutura Bootstrap exige **2026.51.14** para layout `_Modal.cshtml`.

### 2026-05-20 — Relatórios Regulamentação: layout + modal ANP

- `GdiMainModalLoad`: executa `<script>` do fragmento (paridade `jQuery.load`); deteção lazy sem falso positivo em `switch-success`.
- `RelatoriosRegulamentacao/Index`: removido `scroll-body-horizontal` indevido (lista de botões MVC).
- `VersionERP` **2026.51.13**.

### 2026-05-20 — Correção GdiPageScriptsActionFilter (ViewBag indexador)

- Removido `ViewBag[ViewBagKey]` (RuntimeBinderException); flags só em `ViewData` — `ViewBag.GdiPageScripts` na view continua válido.

### 2026-05-20 — G-PERF-M02 medição (estática + script live)

- Script `Scripts/2026_05_20_gdi_perf_m02_network_baseline.py` (`--static` / `--live` Playwright).
- Resultado proxy layout: hubs lite ~280–380 KB est.; telas DT+S2 ~510 KB est. (vs PERF-000 −450…−690 KB).
- Doc `2026_05_20_perf-m02-resultado.md`; JSON `.cursor/context/2026_05_20_perf-m02-resultado.json`.
- **Pendente:** M02b DevTools live em homologação (IIS local indisponível no agente).

### 2026-05-20 — G-PERF-20f Lazy load `#mainModal`

- `GdiPageScriptRegistry` + `GdiLoadScriptOnce` / `GdiMainModalLoad` em `start.js`; patch `$("#mainModal").load`.
- Partial `Views/Shared/_LayoutPageScriptRegistry.cshtml`; doc `2026_05_20_layout-scripts-fase5-lazy-modal.md`.
- `VersionERP` **2026.51.12**. Épico G-PERF-20 (layout scripts) concluído no código.

### 2026-05-20 — G-PERF-20 Fase 4 lote C (LayoutLite por action)

- `GdiPageScriptsDefaults`: mapa `LayoutLiteActionsByController` (Create/Edit cadastros `g`, forms `gc`, `GedSGQ/IndexPops`, treinamento vídeo).
- Script espelho: `Scripts/2026_05_20_gdi_layout_lite_actions.py`; inventário views: `2026_05_20_gdi_inventory_layout_no_datatables.py`.
- `Treinamentos` em `NoDataTablesControllers`; verify/smoke atualizados.
- `VersionERP` **2026.51.11**. Épico Fase 4 (opt-out) fechado no código; próximo opcional: **G-PERF-20f** (lazy modal).

### 2026-05-20 — G-PERF-20 Fase 3 (infra carregamento condicional — 1 PR)

- Partials agregados: `_LayoutHeadOptionalScripts`, `_LayoutScriptsOptional`; `_Layout` simplificado.
- `Lib/GdiPageScriptsView.cs` — leitura segura de flags; `data-gdi-page-scripts` no `<body>` (diagnóstico).
- Script: `Scripts/2026_05_20_gdi_verify_page_scripts_resolve.py`; doc `2026_05_20_layout-scripts-fase3-infra.md`.
- `VersionERP` **2026.51.09** (filter + flags 20d/20e mantidos).

### 2026-05-20 — G-PERF-20 Fase 4 lote B (opt-out Treinamentos + portal)

- `[GdiPageScripts]`: `qa/Treinamentos` (Index + IndexTreinamentoAviacao001) `LayoutLite`; `crm/Pedidos/Index` `LayoutPortalCliente`.
- Preset `LayoutPortalCliente`; doc lotes A/B/C em `2026_05_20_layout-scripts-fase4-optout.md`.
- Scripts: `2026_05_20_gdi_inventory_index_no_datatables.py`; verify/smoke atualizados.
- `VersionERP` **2026.51.10**.

### 2026-05-20 — G-PERF-20e Fase 4 lote A (hubs validados + smoke manifest)

- Presets: `LayoutHubReport`, `LayoutHubReportSelect2`, `LayoutHubJstree`, `LayoutLite`.
- `[GdiPageScripts]` em 7 controllers (Relatórios*, Parametros, CentrosCustos, ClassificacaoFinanceira).
- **Correção:** `RelatoriosFinanceiros` mantém **Select2** no layout (modal com lookup).
- Script smoke: `Scripts/2026_05_20_gdi_page_scripts_smoke_manifest.py`; doc `2026_05_20_layout-scripts-20e-validacao.md`.
- `VersionERP` **2026.51.08**.

### 2026-05-20 — G-PERF-20d Fase 3 (layout condicional + filter)

- `GdiPageScriptsActionFilter` registado em `FilterConfig`; `_Layout` carrega DT/Select2/Toggle/jstree via `ViewBag.GdiPageScripts`.
- Partials: `_LayoutHead/Scripts*DataTables|Select2|BootstrapToggle`; `_LayoutPageScriptsInit`.
- Opt-out DataTables+Select2: `Parametros`, `Relatorios*`, árvores `CentrosCustos`/`ClassificacaoFinanceira` (jstree via filter).
- `VersionERP` **2026.51.07**.

### 2026-05-20 — G-PERF-20c-bis Tempus fora do layout global

- `_Layout.cshtml`: removidos CSS/JS globais de **Tempus**; **22 views** com `_LayoutHeadTempus` + `_LayoutScriptsTempus` (15 com `jsDatepicker` + 7 hubs de modal com datas).
- Inventário: `Scripts/2026_05_20_gdi_inventory_tempus_hosts.py`, `Scripts/2026_05_20_gdi_apply_tempus_partials.py`; padrão `jsDatepicker` no inventário Fase 0.
- jstree (20c): inalterado — 2 Index `CentrosCustos` / `ClassificacaoFinanceira`.
- Login/`_Blank` mantêm Tempus próprio; `VersionERP` **2026.51.06**.

### 2026-05-20 — G-PERF-20c Fase 2 (jstree fora do layout)

- `_Layout.cshtml`: removidos CSS/JS globais de **jstree**.
- Partials jstree em `g/CentrosCustos/Index`, `g/ClassificacaoFinanceira/Index`.
- `VersionERP` **2026.51.05**.

### 2026-05-20 — G-PERF-20b Fase 1 (_Layout scripts defer)

- `_Layout.cshtml`: CSS permanece no `<head>`; scripts removidos do head.
- `_LayoutScriptsAuthenticated.cshtml`: núcleo síncrono (jQuery, OverlayScrollbars, swal-compat, spin, AdminLTE) no início do `<body>`.
- Fim do body: defer em Tempus, DataTables, jstree, Select2, `gdi-select2`, `jsFileInputChange`, toggle; `start.js` **sem** defer; `gdi-session-handler` após `start.js`.
- `ControlVersion` / `VersionERP` → **2026.51.04**.

### 2026-05-20 — G-PERF-20 Fase 0 (layout scripts — preparação)

- Script `Scripts/2026_05_20_gdi_inventory_page_scripts.py` + JSON `.cursor/context/2026_05_20_layout-scripts-inventario.json`.
- Docs: `2026_05_20_layout-scripts-contrato-flags.md`, `2026_05_20_layout-scripts-matriz-modais.md`.
- Arquitetura: `Lib/GdiPageScripts.cs` (enum, attribute, defaults, filter **sem** registo em `FilterConfig`); `_Layout` **sem** alteração de comportamento.
- Backlog: sub-itens **G-PERF-20a** concluído; **G-PERF-20b…f** abertos.

### 2026-05-20 — Auditoria performance ERP (read-only)

- Relatório técnico priorizado: `.cursor/context/2026_05_20_performance-audit-erp.md` (10 secções, Top 10, cache/SQL/front/IIS, plano em 3 fases).
- Inventário script: 6× `GetDados*` ainda com paginação em memória (`2026_05_20_gdi_inventory_datatables_memory_paging.py`).
- Backlog: novos **G-DT-07**, **G-PERF-27…30** em `BACKLOG-DEV.md`. Sem alteração de código.

### 2026-05-20 — G-FLT-04…07, G-LOGIN-01, G-UX-01, G-ENC-03 (filtro legado + flash + login)

- **FLT:** `getFilterByUser` 3 parâmetros; removidos `yesFilterOperador`/`yesFilterText` do modelo; `LibFlashMessage`; helpers login chrome; `SentencaSQLFiltroGenerico` obsoleto.
- **UX:** `LibFlashMessage.SetModalMessage` em Atendimentos, FinanceiroLancamentos, Nfe, Cfop*, ComexProdutos; `ModalError` usa chave constante.
- **LOGIN:** `SaveLoginChromeToTempData` / `ApplyLoginChromeToViewBag` no `UserIdentityController`.
- **ENC:** comentário `TempData IdColigada` removido em Usuarios.

### 2026-05-20 — G-FLT-04 (TempData filtro no login)

- Removidos `TempData.Remove` de `yesFilterField`, `yesFilterOperador`, `yesFilterText`, `yesFilterOn`, `yesFilterController`, `yesFilterControllerTemp` em `UserIdentityController.Index` (sem escritores no repo).

### 2026-05-20 — Inventário TempData legado (backlog)

- Documento `.cursor/context/2026_05_20_tempdata-legado-filtro-login.md`; novos itens **G-FLT-04…07**, **G-LOGIN-01**, **G-UX-01**, **G-ENC-03** em `BACKLOG-DEV.md`.

### 2026-05-20 — Filtro legado + perfil vendedor Clientes

- **`yesFilterAdvancedText`:** removida de `jQueryDataTableParamModel` (UI e servidor já sem uso desde FLT-1).
- **Clientes / perfil -800:** `ClientesController.OnActionExecuting` → 403; removidos ramos UI e filtro carteira vendedor; SQL opcional `Scripts/2026_05_20_gdi_sql_revoke_clientes_perfil_vendedor.sql`. Doc: `.cursor/context/2026_05_20_clientes-perfil-vendedor-removido.md`.

### 2026-05-20 — DT-1 (`GetDados*` g — Atendimentos, nomes legados)

- **G-DT-06:** quatro actions em `AtendimentosController` (`getDadosAtendimentos`, `getDadosAtividades`, `getDadosAtendimentosLogs`, `GetDadosGedAtendimento`) — contrato DataTables mantido; paginação `AsNoTracking` + dicts só da página; `yesFilterOnOff` na listagem; sort GED corrigido (col 3 = descrição).
- Inventário: `Scripts/2026_05_20_gdi_inventory_datatables_g_area.py` passa a incluir `getDados*` e validar `catch` → `JsonDataTableException` por método.
- Doc: `.cursor/context/2026_05_20_dt1-atendimentos-getdados-padrao.md`.

### 2026-05-20 — FLT-1, HYB-1, NFE-1 (filtro legado, lookups híbridos, Portal Vendedor)

- **FLT-1:** removido `yesFilterAdvancedText` em `GedSGQController` e `EstoqueInventarioController.GetDadosInventario`.
- **HYB-1:** 9 controllers migrados para `*.Lookups.cs` + novos `GetCombo*` em `LookupQueryService` (Filiais, Vendedores, ContasCaixas, Nfe, ProdutosNcm, Financeiro g, ImportacoesBancarias, ComexFinanceiro, RelatoriosComerciais). Mantidos locais: CentrosCustos, ClassificacaoFinanceira, ComexImportacoes Index, RelatoriosCadastrais, Usuarios.
- **NFE-1:** SQL desativar menu `g/PortalVendedor`; `UsuariosController` troca senha com `g_Vendedores_*` (sem roles legado portal).

### 2026-05-20 — PUB-1 / PUB-2 (Release, health, versão)

- **PUB-1:** `Web.Release.config` validado (sem `debug`, `customErrors On`, PERF-012); script `Scripts/2026_05_20_gdi_verify_web_release_transform.ps1`.
- **Health:** `HealthController` + rota `GET /health`; `CustomAuthorize` respeita `[AllowAnonymous]`.
- **PUB-2:** `ControlVersion` **2026.51.03**; smoke `Scripts/2026_05_20_gdi_smoke_health_login.ps1`; doc `.cursor/context/2026_05_20_health-endpoint-publish.md`.

### 2026-05-20 — BACKLOG-DEV consolidado (`.cursor/context`)

- Auditoria dos 25 ficheiros em `.cursor/context/` cruzada com `CHANGELOG-DEV.md`; itens concluídos removidos do backlog ativo.
- Novo esquema de IDs por **grupos** (G-PUB, G-SMK, G-PROD, G-DT, G-PERF, G-LKP, G-NFE, G-FLT, G-ENC, G-NET, G-ARC, G-OPS) com checklist executável, criticidade, fase e ordem.

### 2026-05-20 — Remoção Google Analytics

- Removidos scripts GA (`_GoogleAnalyticsGtag`), `GoogleTag`/`GoogleTagURL` (tenant/sessão), `GdiGoogleAnalytics.Enabled`, item Tag no navbar e PERF-014.

### 2026-05-20 — PERF-015 IsTableUpdate TTL (lookups cache)

- `IsTableUpdate`: skip `SELECT MAX` se verificação fresca (`DateTimeLastVerified`, TTL 5 min / 15 min `g_clientes`/`g_produtos`); `LookupQueryServiceCache` testa MemoryCache antes de `IsTableUpdate`; `ResetTableUpdateVerification` na invalidação.

### 2026-05-20 — PERF-012/013/014 infra assets (Etapa 3.1)

- **PERF-012:** doc IIS + `Web.Release.config` compressão/`clientCache` LibUI e Content.
- **PERF-013:** `BundleTable.EnableOptimizations = !Debugging.Enabled` em `Global.asax.cs`.

### 2026-05-20 — PERF-011 Clientes CreateEdit lazy DataTables

- `g/Clientes/CreateEdit`: contatos só no load (edição); destinatários e audit em `shown.bs.tab`; `error.dt`/`xhr.dt`/`GdiDtNotify*` preservados.

### 2026-05-20 — PERF-008/009/010 FormPedidoCreate (load pedido)

- **PERF-008:** DataTables Audit/GED em `shown.bs.tab`; grelha itens `pageLength` 50; mantidos `error.dt`/`xhr.dt`/`GdiDtNotify*`.
- **PERF-009:** `PreencherLookupsPedidoFormCore` sem combo COMEX completo nem warm `GetDatasetGVendedores`; `AjaxComboComexImportacoesPedido` na aba Invoice.
- **PERF-010:** `LoadProdutosPedidoItens` — `select` explícito de colunas de `g_produtos` (validação/gravação/markup).

### 2026-05-20 — PERF-007 lote 2 DataTables paginação SQL

- Helper `Lib/LibDataTableSqlPaging.cs` (`SqlCount`/`SqlPage`). Lote 2: `ClassificacaoFinanceiraController`, `GedSGQController` (4×), `AtendimentosController.getDadosAtendimentos`, `ComexImportacoesController` (itens/GED), `MovimentosController` (modal itens, relatório consulta, NF/carta correção display). Inventário: `Scripts/2026_05_20_gdi_inventory_datatables_memory_paging.py`; 8 actions lote 3 + aceite `GetDadosInvoicesItensEspelhoDigital`.

### 2026-05-20 — Remoção módulo MovimentosCompras (gc)

- Apagados `MovimentosComprasController` (+ `.Lookups`), views `Areas/gc/Views/MovimentosCompras/*`; `.csproj` e lookup `GetComboGcTiposMovimentosCompras` removidos. Excel SC mantido em `Movimentos/ModalImportarExcelSC`. **Menu BD:** desativar entradas `/gc/MovimentosCompras/*` manualmente se existirem.

### 2026-05-20 — CACHE-PROD PROD-002a Estoque Index typeahead

- `gc/Estoque/Index`: filtro produto com `ComboFiltroProdutoPosicaoEstoqueIndex` (2 opções fixas) + `Estoque/GetProdutosLookup`; removido `GetComboGcProdutosPosicaoEstoqueIndex` do primeiro paint (~13k options).

### 2026-05-20 — CACHE-PROD PROD-000 baseline P0 executada

- Script `Scripts/2026_05_20_gdi_prod000_baseline.py` + `2026_05_20_prod000-baseline-resultado.json`: homologação — 13 464 produtos ativos; Estoque Index ~2,1 MB HTML/select; inventário filtro importados 8 081 opt; dataset 13 464 linhas; typeahead pedido/consulta OK. **MovimentosCompras** fora. Próximo: **PROD-002a**.

### 2026-05-20 — CACHE-PROD: checklist Fase 0 (sem MovimentosCompras)

- `.cursor/context/2026_05_20_checklist-cache-produtos.md`: plano P0; compras excluído; ordem PROD-002a → 005b/002c → 005a.

### 2026-05-20 — CACHE-2 completo + PERF-006 Financeiro GetDados

- **CACHE-2b:** typeahead `ClientesLookupController` (`GetClientesFornecedoresLookup` / `ComDoc`) em FinanceiroLancamentos, Compras, Relatórios, Contratos.
- **CACHE-2c:** `g/Financeiro/Index` e `g/Clientes/Index` sem foreach em todos os `g_clientes`.
- **CACHE-2d:** produto em `ModalConsultaPedidos` via `GetProdutosLookup`.
- **CACHE-2e:** removidos `GetComboSomenteGClientes`, `GetComboGClientesFornecedores*` e chaves `LookupCacheKeys` associadas.
- **PERF-006:** `FinanceiroController.GetDados` — `COUNT` + `OFFSET/FETCH`; nomes de clientes só da página.

### 2026-05-20 — Performance: modal consulta N+1 SQL (PERF-005)

- `GetRelatorioConsultaPedidos`: uma query EF para itens da página (`IN id_movimento`); clientes/vendedores só dos ids da página; data fim lida de `yesCustomField04` (alinha ao Ajax do modal).

### 2026-05-20 — Lookups: Atendimentos cliente typeahead (CACHE-2a)

- Removido `GetComboSomenteGClientes` em Create/Edit atendimento; `GetClientesLookup` em `AtendimentosController.LookupAjax.cs`; placeholders `ComboPlaceholderAtendimentoCliente` / `BuildComboClienteAtendimento`; views Edit + `ModalCreateNewAtendimento` com Select2 Ajax. **Sem consumidores** de `GetComboSomenteGClientes` no HTML (método no serviço mantido até deprecação).

### 2026-05-20 — Performance Etapa 1: Painel + modal consulta cliente Ajax (PERF-004b)

- `PainelPedidos` / `ModalConsultaPedidos`: removido `GetComboSomenteGClientes` em `PreencherLookupsPainelPedidos` e `PreencherLookupsConsultaPedidos`; typeahead `GetClientesLookup`; helpers `ComboFiltroClienteTodosAtivos` / `ComboFiltroClienteSelecione` em `LookupSearchQueries.cs`.
- **Correção:** `PainelPedidos` Ajax enviava `yesCustomField03` de `#edit_cliente` (inexistente) — passou a `#id_cliente` (filtro cliente passou a funcionar no painel).
- Build: `using System` em `NavbarController.cs` (catch `Exception` da Etapa PERF-002).

### 2026-05-20 — Performance Etapa 1: IndexPedido filtro cliente Ajax (PERF-004)

- `IndexPedido`: removido `GetComboSomenteGClientes` no servidor; `#edit_cliente` typeahead `GetClientesLookup` (Select2 + `gdi-select2.js`); default `[ TODOS OS CLIENTES ]` (`-1`).

### 2026-05-20 — Performance Etapa 1: navbar sem duplicar footer (PERF-003)

- `NavbarFragmentCache.IsLoadedThisRequest` + flag por request; `IndexFooter` omite segunda `Apply` (listas já no `contextoModel` após `Index`).

### 2026-05-20 — Performance Etapa 1: cache navbar (PERF-002)

- `Security/NavbarFragmentCache.cs` — MemoryCache 60 s para mensagens/tarefas/atividades/alertas; `NavbarController` Index + IndexFooter; invalidação em `CachePersister.logout`.

### 2026-05-20 — Performance Etapa 1: navbar tasks (PERF-001)

- `Security/Contexto.cs` — `getNavbarItemsTask`: filtro `id_perfil` / `id_usuario` (como mensagens) + `Take(50)`; deixa de carregar todas as `g_tasks` ativas.

### 2026-05-20 — Baseline performance Etapa 0 (DevTools)

- Checklist: `.cursor/context/2026_05_20_checklist-performance-erp.md` — PERF-000.1 preenchido (IndexPedido, CreatePedido, Financeiro/Index, login redirect; 45–46 req, ~1–2,1 s Finish).

### 2026-05-20 — Plano de ação performance (checklist)

- Auditoria: `PERFORMANCE-AUDIT-ERP.md` (raiz).
- Checklist executável por etapas (crítica → opcional): `.cursor/context/2026_05_20_checklist-performance-erp.md` (PERF-000…026, PRs sugeridos).

### 2026-05-20 — Lookup clientes/fornecedores (sem filtro vendedor)

- `LookupSearchQueries.SearchClientes`: removido filtro por `IdVendedor`/perfil; mantém `ativo` + `is_cliente` (typeahead pedido).
- `ClientesController` índice: combo lookup lista todos `g_clientes` com `ativo = true`.
- Demais combos via `GetComboGClientesFornecedores` já consideram todos ativos; pedidos usam `GetComboSomenteGClientes` (`is_cliente`).

### 2026-05-20 — Notificar Cliente (pedido faturado)

- `ModalNotificacaoCliente` / `AjaxModalNotificacaoCliente`: deduplicação e validação de e-mails (`LibStringFormat.NormalizarListaEmails*`), telefone só dígitos + DDI 55 no WhatsApp, `id_contato` ao criar contato novo, título seguro se pedido inexistente, merge `email_notificacao` sem repetir no GET.

### 2026-05-20 — Backup projeto (scripts PowerShell)

- `Scripts/2026_05_20_gdi_backup_projeto_*.ps1` — ZIP por omissao `yyyy-MM-dd_HHmmss-GDI-ERP-Plataform.zip`; `-SemZip` / `-ManterPasta`.

### 2026-05-20 — Comercial pedidos

- **ModalPedidoInsertEditItem:** sequência incremental corrigida — `max(sequencia)` usava `id_movimento` 0 no insert; novo `ObterProximaSequenciaItemPedido` no modal e no `AjaxInsertEditItem`.

### 2026-05-20 — Documentação, checklist e higiene

- Arquitetura docs: `AI-CONTEXT.md`, `BACKLOG-DEV.md`, changelog compacto na raiz; histórico em `docs/dev-history/`.
- Regras Cursor alinhadas à ordem de leitura (AI-CONTEXT → CHANGELOG → BACKLOG).
- **Grupos 2.5–2.11:** Financeiro g (POSTs órfãos), UI tabelas MVC + sidebar portal, Index 1º load (Filiais), filtro SQL legado, órfãos ProdutosTipos N/A, UTF-8 BOM 0 ficheiros, verify csproj Gdi 0 lacunas.
- **Grupos 1.x:** smoke lookups OK; typeahead pedidos; EF6/DI piloto; convenção Index vs CreateEdit; partials domínio no serviço de lookups.
- **LibDataSets:** Ondas 6a/6b concluídas (classe removida).
- **NFe:** Fase 17 / arquitetura e-Notas documentada; Portal Vendedor N/A.
- **PascalCase:** paths Git models `Cst*` + views NFe `Modal*`.
- **UTF-8:** lote global 146 ficheiros (histórico); re-scan 0 BOM.
- Convenção `AAAA_MM_DD_` em `.cursor/context`, rules e Scripts.

### 2026-05-19 — Index, filtros e remoção de legado

- Padrão **Index modernizado:** filtro inline Id/Nome (e variantes), paginação SQL, Editar in-line, indicador «Limpar», remoção modal filtro avançado (`btnFiltro`, `yesFilter*`).
- **Produtos Index:** fases 1–5 (deferLoading, persistência filtro, sem carga automática inicial).
- Remoção módulos: `a/Filtros`, `g/PortalVendedor`, `g/PortalCliente`, `g/Requisicoes`, `g/FinanceiroFaturamentos`, `g/FinanceiroLancamentos`.
- **CSRF** fases 3A–3B (financeiro Ajax, Usuarios); **XSS** Fase A (Atendimentos).
- **PascalCase** lotes B1–B2d (views `modal*`, models `cst*` → `Cst*`).
- DataTables: `LibMessageProcessandoHide` global; filtro LIKE `%termo%` normalizado.
- Correções pontuais: Clientes/Perfis/Cidades Index (filtro id explícito vs `> 1` na base); Filiais CreateEdit; Estoque ficha in-line.

### 2026-05-14 — DataTables, NFe e UX global

- **Fases 17–18:** DataTables área `a` + auditoria cadastros `g`/`gc`.
- **e-Notas / NFS-e:** URLs, status 14 em falha, `porIdExterno`, NF produto vs serviço, alinhamento `RoboEnotasNFE`.
- UX: menu lateral `LibMessageProcessando`; login `UserIdentity` HTML válido + scroll; navbar/sidebar.
- Regras: linha de commit `AAAA_MM_DD - resumo` no relatório do agente.

### 2026-05-13 — Fases DataTables e mensagens (0–9+)

- Helpers `GdiDtNotifyLoadFailure`, `GdiDtNotifyJsonErrorMessage`, `GdiAjaxNotifyInconsistencias`.
- Substituição massiva `alert` → `LibMessageError` (Fase 7).
- `try/catch` + JSON erro em Atendimentos, GedSGQ, Movimentos (Fases 8–9).
- Script `gdi_verify_csproj_gdi_helpers.py` (gate publish views `Gdi*`).

*Entrada a entrada (ficheiros, causas, smoke): ver histórico arquivado.*

---

## Pendências abertas

Lista priorizada em **`BACKLOG-DEV.md`**. Resumo:

| Prioridade | Item |
|------------|------|
| Alta | Publish/IIS (Release, `customErrors`, health); smoke NFe e-Notas homologação; smoke transversal pós-publish |
| Média | Smoke Index cadastros (Filiais/Produtos); smoke Financeiro g; filtro legado `qa/GedSGQ` + `gc/EstoqueInventario`; 14 controllers híbridos `ViewBag.combo` |
| Análise | Migração .NET 4.8.1 (trilha isolada); ~4 `GetDados*` g fora do padrão `JsonDataTableException`; cache combos globais (Fase 2+ lookups) |

Checklist executável: `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md`.

---

## Alertas técnicos

| Alerta | Ação |
|--------|------|
| **Confundir DataTables com tabela MVC** | Ler `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` antes de CSS em `<table>` |
| **Views `Gdi*` fora do `.csproj`** | `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` antes do publish |
| **Cache `start.js` / `start.css`** | Incrementar `VersionERP` após alteração |
| **Publish: PackageTmp / Filtros** | Pasta obsoleta em `obj` pode gerar Access denied — limpar `obj` se necessário |
| **Filtro Index `id > 1` na query base** | Com filtro Id explícito pode excluir registo id=1 (corrigido em Perfis/Clientes/Difal — replicar padrão noutros Index) |
| **Lookup valor `0` no filtro** | Pode gerar LIKE em nome em vez de filtrar por id (Clientes — validar noutros) |
| **Portal / sessão** | Cache `contextoModel_*` antigo pode exigir novo login após deploy |
| **NFe homologação** | Smoke 2.2.4 requer gateway e-Notas real |
| **Menu legado Portal Vendedor** | Desativar em BD se ainda existir entrada |
| **UTF-8 BOM** | VS pode reintroduzir BOM — `python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail` |
| **TLS em código novo** | Preferir Tls12 \| Tls13; não SSL3 |

---

## Itens a revisar

- **FinanceiroLancamentos Index (g):** entrada no histórico sobre período mês corrente — módulo `g/FinanceiroLancamentos` foi **removido** em 2026-05-19; confirmar se a alteração migrou para `gc` ou ficou só como referência histórica.
- **Transferir conta caixa:** UI removida em 2.5 (2026-05-20); entradas de 2026-05-19 no histórico referem feature ainda não removida — estado atual = **removido**.
- Sincronizar `.claude/CHANGELOG-RECENT.md` com fonte `CHANGELOG-DEV.md` (raiz) se o script `sync_changelog_recent.py` for reativado.

---

## Histórico completo

| Arquivo | Conteúdo |
|---------|----------|
| **`docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`** | Snapshot **integral** (~2903 linhas, **187** blocos `### [data]`) — **não editar** para registo diário |
| `docs/dev-history/README.md` | Índice da pasta |
| `.cursor/context/*.md` | Contexto por tema (lookups, NFe, PascalCase, UTF-8, financeiro, etc.) |
| `CLAUDE.md` | Padrões longos, fases 0–18, armadilhas |
| `.cursor/CHANGELOG-DEV.md` | Redirecionamento para este ficheiro |

**Como registrar nova intervenção:** uma linha na secção «Últimas alterações relevantes» (mês/data) + atualizar `BACKLOG-DEV.md`; bloco longo só no histórico arquivado (se necessário, append datado em novo ficheiro `docs/dev-history/CHANGELOG-DEV-AAAA-MM-DD.md`, não no operacional).
