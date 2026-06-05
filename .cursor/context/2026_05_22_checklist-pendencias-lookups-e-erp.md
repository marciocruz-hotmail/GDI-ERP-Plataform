# Checklist executável — pendências pós LibDataSets (Grupo 1 + Grupo 2)

**Projeto:** GDI-ERP-Plataform  
**Data base:** 2026-05-20  
**Estado de referência:** Fases 0–5 e Ondas **6a/6b** concluídas (`Lib/LibDataSets.cs` removido; lookups em `ILookupQueryService` + `LookupQueryServiceCache`).

> **Pendências ativas (2026-05-20):** consolidadas em **`BACKLOG-DEV.md`** (IDs **G-PUB**, **G-SMK**, **G-LKP**, **G-DT**, etc.). Este ficheiro mantém o histórico detalhado dos grupos 1–2; marcar `[x]` aqui **e** no backlog quando fechar um item.

**Como usar**

- Marque `[x]` apenas quando o critério de aceite estiver cumprido.
- Respeite a ordem numérica dentro de cada grupo (dependências indicadas com ↳).
- Uma intervenção de código = entrada em `CHANGELOG-DEV.md` (raiz); detalhe extenso em `.cursor/context/` ou histórico em `docs/dev-history/`.
- **Não** executar `git push` nem publish remoto (regras do projeto).

**Scripts úteis (lookups)**

```powershell
cd c:\Marcio\Projetos\GDI-ERP-Plataform
python Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py --fail
python Scripts/2026_05_20_gdi_inventory_libdatasets.py
python Scripts/2026_05_20_gdi_audit_lookup_get_names.py --fail
```

**Build (Visual Studio)**

- Compilar solução `GDI-ERP-Plataform` em **Debug** e **Release** no VS (MSBuild CLI local pode falhar sem `Microsoft.WebApplication.targets`).

---

## Grupo 1 — LibDataSets / lookups (`ILookupQueryService`)

### 1.1 Build e guardrails

- [x] **1.1.1** Compilar solução sem erros (Debug).
  - **Aceite:** 0 erros CS; warnings pré-existentes documentados se novos.
  - **Resultado (2026-05-20):** MSBuild `Debug|AnyCPU` — 0 erros, 0 warnings.
- [x] **1.1.2** Compilar solução sem erros (Release).
  - **Aceite:** igual a 1.1.1.
  - **Resultado (2026-05-20):** MSBuild `Release|AnyCPU` — 0 erros, 0 warnings.
- [x] **1.1.3** Executar inventário lookups.
  - **Comando:** `python Scripts/2026_05_20_gdi_inventory_libdatasets_usage.py --fail`
  - **Aceite:** `LibDataSets.cs (removido)`; `(nenhuma)` em referências `LibDataSets.*`.
  - **Resultado (2026-05-20):** exit 0; `(nenhuma)` referência.
- [x] **1.1.4** Regenerar inventário `ILookupQueryService` (opcional após alterações no serviço).
  - **Comando:** `python Scripts/2026_05_20_gdi_inventory_libdatasets.py`
  - **Aceite:** `.cursor/context/2026_05_20_lookups-libdatasets.md` atualizado.
  - **Resultado (2026-05-20):** 59 métodos `Get*`; 146 chamadas.

### 1.2 Auditoria de nomes `Get*` (contrato vs partials)

- [x] **1.2.1** Listar métodos em `Lib/Lookups/ILookupQueryService.cs`.
  - **Resultado (2026-05-20):** **59** métodos `Get*` / `GetDataset*` no contrato.
- [x] **1.2.2** Grep em `**/*.Lookups.cs` por chamadas `\.Get\w+\(` e comparar com a interface.
  - **Comando:** `python Scripts/2026_05_20_gdi_audit_lookup_get_names.py --fail`
  - **Resultado:** **58** métodos distintos, **142** chamadas em **19** partials; **0** fora do contrato.
- [x] **1.2.3** Corrigir divergências de casing (ex.: `GetComboGProdutosNcm` ≠ `GetComboGProdutosNCM`).
  - **Resultado:** **nenhuma correção necessária** — `ProdutosController.Lookups.cs` já usa `GetComboGProdutosNcm` (alinhado à interface).
  - `GetDatasetGcClientesDestinatarios` só em `MovimentosController.cs` (não no partial); válido.
- [x] **1.2.4** Recompilar após correções (↳ 1.1.1).
  - **Resultado:** MSBuild `Debug|AnyCPU` — 0 erros, 0 warnings (sem alteração de código C#).

### 1.3 Smoke manual pós-Onda 6b (funcional) — **concluído**

**Estado (2026-05-20):** Smoke manual validado em ambiente local com dados reais, pós-Onda 6b (`ILookupQueryService`). **Resultado global: OK** — combos/datasets e fluxos abaixo sem defeito a corrigir no código nesta onda; sem entrada de bugfix no CHANGELOG (apenas registo de conclusão).

Validar combos/datasets após deploy ou ambiente local com dados reais.

| # | Fluxo | O que verificar |
|---|--------|-----------------|
| [x] **1.3.1** | Login / navbar | Menu, mensagens, tarefas; **novo login** se cache `contextoModel_*` antigo causar erro |
| [x] **1.3.2** | Pedidos vendas | `Movimentos` — Index, `FormPedidoCreate`/Edit, trocar cliente → contatos/destinatários |
| [x] **1.3.3** | Modais pedido | Item, aprovação, faturamento NF, painel, consulta pedidos |
| [x] **1.3.4** | Compras | ~~`MovimentosCompras`~~ **módulo removido** 2026-05-20 |
| [x] **1.3.5** | Financeiro gc | `FinanceiroLancamentos` — Index, modal lançamento, gerar financeiro do movimento |
| [x] **1.3.6** | Inventário | `EstoqueInventario` — Index, itens, modal item, grelha Ajax |
| [x] **1.3.7** | Produtos g | `Produtos` CreateEdit — tipos, NCM, ICMS, unidade, importações COMEX |
| [x] **1.3.8** | Atendimentos | Index + formulário + Edit — departamentos, categorias, status, responsável |
| [x] **1.3.9** | Contratos | `ContratosAviacao` CreateEdit |
| [x] **1.3.10** | Estoque | `Estoque` Index (combo produtos legado largura ecrã), transferência/recebimento |
| [x] **1.3.11** | Estoque controle | Create/Edit importados e não importados |
| [x] **1.3.12** | COMEX produtos | Modal com combo com Id |
| [x] **1.3.13** | Clientes | Tipos de contato |
| [x] **1.3.14** | Rel. financeiros | Modal lançamentos — tipos PagRec, status |
| [x] **1.3.15** | GED / SGQ | Tipos por módulo (`Ged`, `GedSGQ`) |
| [x] **1.3.16** | CFOP | Operações / parâmetros / tela pedido (roles vendedor vs adm) |
| [x] **1.3.17** | Fretes | Index |
| [x] **1.3.18** | Lotes estoque | `EstoqueLotes` — `LoadCombos` |
| [x] **1.3.19** | NF importação | `MovimentosEntradas` — frete responsável e formulários processar NF |

- [x] **1.3.20** Registar resultado do smoke (OK / falhas + ecrã) em nota de publish ou CHANGELOG se houver defeito corrigido.
  - **Resultado:** **OK** — 19 fluxos validados; sem falhas que exijam correção de código nesta sessão. Nota de publish: repetir smoke breve após deploy IIS se `contextoModel_*` em cache de sessão antiga (↳ 1.3.1).

### 1.4 Documentação e scripts (higiene) — **concluído**

- [x] **1.4.1** Atualizar `.cursor/context/2026_05_20_libdatasets-diagnostico-e-plano.md`:
  - [x] Tabela de status: Fases 2–4 e Ondas 6a/6b como **concluídas**
  - [x] Referências a `Lib/LibDataSets.cs` como **histórico** (~1.806 linhas, removido); arquitetura §2.1 pós-6b
  - [x] Secção rollback: `ILookupQueryService` + git (não `LibDataSets.cs`)
  - [x] Referências §9: `ILookupQueryService` / `LookupQueryService*.cs` / `LookupQueryServiceCache`
- [x] **1.4.2** Confirmar `.cursor/context/2026_05_20_lookups-libdatasets.md` alinhado (gerado por script ou revisão manual).
  - **Resultado (2026-05-20):** regenerado — **59** métodos, **146** chamadas.
- [x] **1.4.3** `Scripts/2026_05_20_gdi_libdatasets_obsolete_attrs.py` — **legado/obsoleto**; exit 0 sem mutar ficheiros.
- [x] **1.4.4** `.cursor/context/2026_05_20_lookups-libdatasets-inventario.md` — aviso de arquivo + link para inventário `ILookupQueryService`.

### 1.5 Código — alinhar controllers ainda híbridos

#### 1.5.1 `MovimentosEntradasController` — **concluído**

- [x] Mapear todas as atribuições `ViewBag.combo*` no ficheiro principal (`Areas/gc/Controllers/MovimentosEntradasController.cs`).
  - **Mapeamento (2026-05-20):** `comboMovimentosTipos` ×3 modais; `comboProdutos` ×2 forms (nacional, devolução); `comboFreteResponsavel` já no partial.
- [x] Mover para `MovimentosEntradasController.Lookups.cs` com `MovimentosEntradasLookups.Get*` onde existir contrato.
  - `GetComboGcFreteResponsavel` (existente); `GetComboGcProdutosServicosTodos` + `[ SELECIONE ]` (nacional); tipos fixos em `PreencherLookupsComboMovimentoTipoFixo`; devolução mantém SQL por `id_movimento_ref` no partial (sem novo `Get*`).
- [x] Criar métodos no serviço **somente** se a lógica for nova (evitar duplicar LINQ já coberto por outro `Get*`).
  - **Nenhum** método novo em `ILookupQueryService`.
- [x] Garantir `partial` + chamadas `PreencherLookups*` nas actions afetadas.
  - `ModalImportarNFCompraNacional/Devolucao/Importacao`, `FormProcessarNFCompraNacional`, `FormProcessarNFDevolucao`, `ModalFaturarNFImportacao` (frete).
- [x] Smoke: processar NF compra/nacional/devolução/importação (↳ 1.3.19).
  - Coberto pelo smoke 1.3 já registado como OK.

#### 1.5.2 `EstoqueLotesController`

- [x] Refatorar `LoadCombos()` — usar `GetComboGcComexImportacoesTodas` (ou equivalente) em vez de LINQ local duplicado.
- [x] Manter `PreencherLookupsProdutosTodos()` do partial existente.
- [x] Smoke: lotes (↳ 1.3.18).

#### 1.5.3 `EstoqueController.PreencherLookupsEstoque`

- [x] Decisão registrada no CHANGELOG:
  - **Opção A:** `GetComboGcProdutosPosicaoEstoqueIndex` (sem cache — truncamento por `DisplayScreenWidth` por utilizador).
- [x] Implementar opção escolhida.
- [x] Smoke: Index posição estoque (↳ 1.3.10).

### 1.6 Typeahead Ajax (clientes / produtos) — evolução

> Adiado nas Fases P1–P4; maior impacto em RAM e UX em pedidos.

- [x] **1.6.1** Contrato JSON + actions — ver `.cursor/context/2026_05_22_lookups-typeahead-ajax-pedidos.md`
- [x] **1.6.2** `GetClientesLookup` / `GetProdutosLookup` + `LookupSearchQueries` (limite, `q`, filtro vendedor).
- [x] **1.6.3** `FormPedidoCreate` + `ModalPedidoInsertEditItem` + `gdi-select2.js` (Ajax).
- [x] **1.6.4** Form/modal item sem combo completo no primeiro paint.
- [x] **1.6.5** Smoke: registado no doc typeahead (vendedor vs adm — filtro em `SearchClientes`).

### 1.7 Convenção e cadastros `g` (combo local)

- [x] **1.7.1** Documentar em `CLAUDE.md` ou `.cursor/rules`:
  - Index/filtro → query local na action (sem cache global).
  - Form CreateEdit compartilhado → `ILookupQueryService` + `*.Lookups.cs`.
  - Detalhe: `.cursor/context/2026_05_20_lookups-convencao-index-vs-createedit.md`
- [x] **1.7.2** Inventariar controllers `g`/`gc` com `ViewBag.combo` **sem** `*.Lookups.cs` (14 híbridos):
  - Script: `Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py`
  - Tabela no contexto acima.
- [x] **1.7.3** Por controller: decisão **manter local** vs **Lookups.cs** documentada no contexto (PRs 1–2 controllers: ver ordem sugerida no `.md`).

### 1.8 Organização do serviço de lookups

- [x] **1.8.1** Decidir estrutura: **partials por domínio** (`Comercial`, `Financeiro`, `CadastrosG`) — não unificar num único `.cs`.
- [x] **1.8.2** Aplicar refactor sem alterar assinaturas públicas de `ILookupQueryService` (`Wave6a` removido).
- [x] **1.8.3** Build + `2026_05_20_gdi_inventory_libdatasets_usage.py --fail` (↳ 1.1) OK.

### 1.9 Qualidade técnica (EF6, DI, testes, operação)

- [x] **1.9.1** Revisar factories em `LookupQueryService*.cs`: adicionar `.AsNoTracking()` e projeção onde ainda houver materialização desnecessária.
- [x] **1.9.2** (Opcional) Injetar `ILookupQueryService` via construtor em `EstoqueController` e `AtendimentosController`; `LookupQueryServiceAccessor` como fallback.
- [x] **1.9.3** Ampliar `Tests/GDI-ERP-Plataform.Lookups.Tests` — invalidação por tabela, combo paramétrico, chaves de cache.
- [x] **1.9.4** Monitorização pós-publish: `.cursor/context/2026_05_20_lookups-monitorizacao-pos-publish.md`

---

## Grupo 2 — Demais pendências do ERP (CHANGELOG / diagnóstico)

### 2.1 DataTables — cliente (`GdiDt*` / `xhr.dt` / `error.dt`)

#### 2.1.1 Lote 2 — views pendentes (mencionado no CHANGELOG)

- [x] `Areas/g/Views/Nfe/Index.cshtml` (e CreateEdit/logs se aplicável)
- [x] `Areas/g/Views/ProdutosTipos/Index.cshtml` — **N/A** (sem pasta/view no repo; tipos via combo em `Produtos/CreateEdit`)
- [x] `Areas/gc/Views/ComexProdutos/Index.cshtml`
- [x] `Areas/gc/Views/ComexProdutos/ProdutosPre.cshtml` (ou `FormProcessarProdutosPre*`)
- [x] Padrão em cada view: `error.dt` + `if (GdiDtNotifyJsonErrorMessage(json)) return;` antes de processar `aaData`
- [x] `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` → 0 lacunas (2026-05-20)

#### 2.1.2 Servidor — `GetDados*` / `getDados*` em cadastros `g`

- [x] Inventário: `python Scripts/2026_05_22_gdi_inventory_datatables_g_area.py` — 27 métodos; Fases 13–16 já aplicadas
- [x] Por controller pendente: `param` nulo, `try/catch`, JSON com `errorMessage`, `severity`, `stackTrace`, `yesFilterOnOff`, `aaData` vazio
- [x] Consolidar `JsonDataTableException` onde ainda faltar (padrão Fases 13–16)
- [x] Views correspondentes: `xhr.dt` + `GdiDtNotifyJsonErrorMessage` (+ `return` no lote Index g)
- [x] **Extra gc:** `ComexProdutosController` — `param` nulo em `GetDados` / `GetDadosProdutosPre`

### 2.2 NFe (Fase 16 / 17) — Portal Vendedor **fora de escopo** (módulo removido)

- [x] **2.2.1** Revisar `NfeController` + `RoboEnotasNFE` — Fase 17 concluída; mapa em `.cursor/context/2026_05_26_nfe-enotas-arquitetura.md`
- [x] **2.2.2** Roles `g_PortalVendedor_*` — **N/A** módulo Portal Vendedor; roles mantidas só em troca de senha (`TokenAcesso` **V**) em `UsuariosController`; desativar menu legado em produção
- [x] **2.2.3** `Nfe.GetDados` / `GetDadosNfeLogs` — `param` nulo + `JsonDataTableException` + views `xhr.dt` com `return`
- [ ] **2.2.4** Smoke manual — checklist na secção «Smoke manual» de `2026_05_26_nfe-enotas-arquitetura.md` (homologação e-Notas real)

### 2.3 Smoke transversal (pós-publish)

| [ ] | Área | Casos |
|-----|------|--------|
| [ ] **2.3.1** | Identidade | Login subdomínio `SetTenants`, portal `UserIdentity/AcessoPortal`, `crm/Pedidos/Index` |
| [ ] **2.3.2** | Financeiro g | Boletos, prorrogar vencimento, export Excel, posição contas, robô Itaú |
| [ ] **2.3.3** | Financeiro gc | Faturamento, fechar/finalizar lançamentos, boletos e-mail, gestor franquia |
| [ ] **2.3.4** | COMEX | Importação, invoice PDF, espelho digital |
| [ ] **2.3.5** | Comercial gc | Entradas NF, carta correção, painel gerencial, relatórios comerciais |
| [ ] **2.3.6** | GED | Upload, download, tipos SGQ |

### 2.4 PascalCase e views `modal*`

- [x] Consultar `.cursor/context/2026_05_20_pascalcase-areas-renomeacao-lotes.md`
- [x] **B2a** — `crm` (2 models) — paths Git `Cst*` (2026-05-20)
- [x] **B2a** — `a` — sem `Models/cst*` no repo (N/A)
- [x] **B2b** — `Areas/g/Models` — 29 paths Git renomeados
- [x] **B2c** — `Areas/gc/Models` — 28 paths Git renomeados
- [x] **B2d** — `Models/CstTenant`, `Robos/SintegraWS` — paths Git renomeados
- [x] Views `modal*`: Nfe×2 (`ModalCancelarNfe`, `ModalExportarDadosNfePDF`); **FinanceiroFaturamentos N/A** (módulo removido); extra `Usuarios/ModalUsuarioTrocarSenha` (csproj já PascalCase)
- [x] `NfeController` + `.csproj` alinhados; classes/refs já `Cst*` desde 2026-05-19
- [x] Build **Release|AnyCPU** OK (2026-05-20)

### 2.5 Financeiro `g` — POSTs e views órfãs

- [x] Mapear POSTs em `FinanceiroController` — `.cursor/context/2026_05_20_financeiro-g-posts-mapeamento.md`
- [x] Decisão por endpoint: manter fluxos com UI; remover transferir conta + POSTs órfãos (ex-`FinanceiroFaturamentos` / `FinanceiroLancamentos`)
- [x] Remover `modalTransferirContaCaixa()` (não usado em produção)
- [x] Remover view/action `ModalTransferirContaCaixa` / `AjaxTransferirContaCaixa` + models dedicados
- [ ] Smoke: exclusão faturamento (`AjaxFinanceiroCancelamento` gc), troca senha (interno, portal, vendedor)

### 2.6 Publish / IIS / Release

- [x] `Web.Release.config` — transformação sem `debug="true"` (script `Scripts/2026_05_22_gdi_verify_web_release_transform.ps1`)
- [x] `customErrors mode="On"` no Web.config transformado Release (validar no IIS após publish)
- [x] `MSBuild /t:TransformWebConfig /p:Configuration=Release` — automatizado no script acima
- [x] Health check — `GET /health` (`HealthController`); doc `.cursor/context/2026_05_22_health-endpoint-publish.md`
- [x] `ControlVersion` **2026.51.03** (PUB-2 cache-bust); smoke `Scripts/2026_05_22_gdi_smoke_health_login.ps1`
- [ ] Smoke manual pós-deploy homologação/produção (login/navbar no IIS remoto)

### 2.7 UI — tabelas MVC e layout

- [x] Consultar `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` antes de alterar tabelas
- [x] Corrigir views MVC: `FormProcessarProdutosPreNovos`, `FormProcessarProdutosPreAtualizar`, `FormProcessarNF*` → `gdi-form-table-scroll` / `gdi-form-table-fixed`
- [x] Script inventário: `python Scripts/2026_05_20_gdi_inventory_scroll_body_form_tables.py` → **0** views MVC incorretas
- [x] Sidebar portal: dropdown só «Sair»; faixa MS/AWS oculta; logo → `crm/Pedidos/Index`
- [x] `ControlVersion` **2026.51.03** (`start.css` / `start.js` cache-bust no publish; ver PUB-2)

### 2.8 Index cadastros — primeiro load da grelha

- [x] Inventário: `Scripts/2026_05_20_gdi_inventory_index_first_load.py` + `.cursor/context/2026_05_20_index-cadastros-primeiro-load.md`
- [x] Cadastros DataTables alinhados (Produtos já em 2026-05-19); **Filiais** migrado 2026-05-20
- [ ] Smoke manual: Enter, Limpar, paginação com filtro (browser)
- [ ] Fora escopo 2.8: Financeiro, Nfe, Ged, Atendimentos (operacionais, sem deferLoading)

### 2.9 Filtro genérico SQL (limpeza opcional)

- [x] Revisar models/propriedades — sem `CstModalFiltro*` / `ModalFiltro*` no disco; `jQueryDataTableParamModel` mantido (qa/gc)
- [x] **FLT-1 (2026-05-20):** `qa/GedSGQ` + `gc/EstoqueInventario` — removido ramo `yesFilterAdvancedText` nos `GetDados*`
- [x] Remover ramos mortos em `Areas/g` + `ComexProdutos.GetDados` — doc `.cursor/context/2026_05_22_filtro-generico-legado-limpeza.md`
- [ ] Lote futuro: `qa/GedSGQ`, `gc/EstoqueInventario` (ainda referenciam `yesFilterAdvancedText`)

### 2.10 Ficheiros órfãos no disco

- [x] `Areas/g/Views/ProdutosTipos/*` e `ProdutosTiposController.cs` — **inexistentes** no disco; `.csproj` sem entradas; tipos via combo `Produtos/CreateEdit` (doc `.cursor/context/2026_05_20_ficheiros-orfaos-produtostipos.md`)
- [x] `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` → **0** lacunas (183 views Gdi*, 2026-05-20)

### 2.11 UTF-8 BOM (opcional / lote)

- [x] Scripts avaliados — válidos; inventário `Scripts/2026_05_20_gdi_inventory_utf8_bom.py` (doc `.cursor/context/2026_05_20_utf8-bom-lotes.md`)
- [x] Lote global já aplicado (146 ficheiros, CHANGELOG 2026-05-20); re-scan **0** BOM; smoke `area_a` + `Areas/g|gc` + `Lib` → **0** convertidos nesta sessão

### 2.12 Migração .NET 4.7.2 → 4.8.1 (trilha separada)

- [ ] Ler `.cursor/context/2026_05_20_migracao-472-481.md`
- [ ] Checklist próprio: NuGet, TLS, `Web.config`, `ComexImportacoesController`, publish profile
- [ ] **Não** misturar com itens 1.x/2.x sem necessidade

---

## Registo de conclusão por grupo

| Grupo | Itens totais (aprox.) | Concluídos | Data conclusão |
|-------|------------------------|------------|----------------|
| 1 — LibDataSets / lookups | §1.1–1.9 | | |
| 2 — Demais ERP | §2.1–2.12 | | |

**Responsável / notas:**

```
(preencher durante execução)
```

---

## Referências

| Documento | Caminho |
|-----------|---------|
| Diagnóstico LibDataSets | `.cursor/context/2026_05_20_libdatasets-diagnostico-e-plano.md` |
| Inventário lookups | `.cursor/context/2026_05_20_lookups-libdatasets.md` |
| CHANGELOG | `CHANGELOG-DEV.md` (raiz); histórico `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` |
| DataTables vs MVC | `.cursor/context/2026_05_20_tabelas-datatables-vs-mvc.md` |
| PascalCase | `.cursor/context/2026_05_20_pascalcase-areas-renomeacao-lotes.md` |
| Migração 4.8.1 | `.cursor/context/2026_05_20_migracao-472-481.md` |
| Regras agente | `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` |
