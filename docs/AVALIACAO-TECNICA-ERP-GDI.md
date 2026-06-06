# Avaliação Técnica Profunda — ERP GDI Aviação (GDI-ERP-Plataform)

**Data da avaliação:** 2026-05-23  
**Modo:** somente leitura — nenhum arquivo de código-fonte foi alterado  
**Escopo:** monólito ASP.NET MVC .NET Framework 4.7.2, SQL Server, Bootstrap 5, AdminLTE 4  
**Autor da análise:** avaliação automatizada assistida (Cursor Agent)

---

## 1. Sumário executivo

### Estado geral do projeto

O **GDI-ERP-Plataform** é um monólito ASP.NET MVC maduro e em **modernização incremental ativa** (2026). A stack de UI foi migrada para **Bootstrap 5 + AdminLTE 4**; mensagens UX migraram para **SweetAlert2** (`LibMessage*`); lookups migraram de `LibDataSets` (removido) para **`ILookupQueryService`**; e há padronização progressiva de **DataTables/Ajax** (`GdiDt*`, JSON `errorMessage` no servidor).

O backend permanece em **.NET Framework 4.7.2** com **Entity Framework 6.5.2 (Database First / EDMX)** como ORM principal, coexistindo com SQL bruto via **`LibDB`** e **`Database.SqlQuery`**. Não há camada Repository; controllers concentram regra de negócio, acesso a dados e orquestração de integrações (`Robos/`).

**Contagens aproximadas (código-fonte, excl. `obj`, `bin`, `packages`, `_filestemp`):**

| Tipo | Quantidade |
|------|------------|
| Arquivos `.cs` | ~503 |
| Views `.cshtml` | ~257 |
| JS de runtime (`Scripts/` + `LibUI_*/plugins/`) | ~84 |
| Controllers principais | ~59 |
| Pacotes NuGet (`packages.config`) | 112 |

### Principais riscos

| Prioridade | Risco |
|------------|-------|
| **P0** | **Secrets hardcoded** no código (`RoboItauBolecode.cs`, `RelatoriosRegulamentacaoController.cs`) |
| **P0** | **Senha registrada em log** de falha de login (`UserIdentityController.cs`) |
| **P1** | **SQL por concatenação** com entrada de usuário (NF, produtos, filtros) — risco de SQL Injection |
| **P1** | **~344 POSTs sem antiforgery** explícito (536 `[HttpPost]` vs 192 atributos antiforgery) |
| **P1** | **Controllers sensíveis sem `[CustomAuthorize]`** (financeiro, GED, relatórios, audit) |
| **P1** | **`JobServerController`** anônimo; proteção por chave opcional |
| **P2** | **God controllers** (`MovimentosController` ~9.406 linhas; `FinanceiroLancamentosController` ~4.216) |
| **P2** | **Mistura EF + ADO/SQL bruto** no mesmo fluxo funcional |
| **P2** | **~540 `Html.Raw`** em 217 views — XSS onde conteúdo não é confiável |
| **P3** | Plataforma **.NET Framework 4.7.2** — **sem** migração ASP.NET Core (decisão 2026-05-25) |
| **P3** | Dependências legadas (`Rotativa`, `Zen.Barcode`, `WebGrease`, `Modernizr`) |

### Principais oportunidades

1. Externalizar e rotacionar todos os secrets; eliminar logging de credenciais.
2. Parametrizar SQL residual; consolidar padrão **EF ou ADO parametrizado por fluxo**.
3. Expandir **`[GdiValidateAntiForgeryToken]`** nos POSTs mutáveis Ajax.
4. Aplicar **`[CustomAuthorize]`** uniformemente ou filtro global complementar.
5. Extrair services de domínio dos controllers gigantes (pedidos, financeiro, COMEX).
6. Limpeza controlada de código morto (`PedidosVendaServices`, arquivos `*Copia*`, stubs EF).
7. Bump opcional **4.7.2 → 4.8.1** (mesmo monólito IIS) se a equipa decidir — **sem** trilha ASP.NET Core.

### Recomendações prioritárias (Top 5)

1. Remover secrets do repositório e do código; usar `App_Data/Secrets` + cofre/IIS.
2. Corrigir log de senha em `UserIdentityController` (P0 imediato).
3. Auditar e parametrizar pontos de SQL concatenado de **alto risco** (NF, portal, produtos).
4. Endurecer `JobServer` (chave obrigatória, IP allowlist, autenticação).
5. Padronizar autorização e CSRF nos módulos financeiro/GED/NFe.

---

## 2. Escopo analisado

### Pastas analisadas

| Pasta | Conteúdo analisado |
|-------|-------------------|
| `Areas/` (g, gc, crm, qa, a) | Controllers, Views, Models, Services |
| `Controllers/` | Controllers raiz (login, health, navbar, lookups) |
| `Db/` | EDMX, DbContext, entidades EF6 |
| `Lib/` | Helpers, Lookups, LibDB, LibLogger |
| `Robos/` | Integrações (e-Notas, Itaú, AWS, WhatsApp) |
| `Security/` | CustomAuthorize, antiforgery |
| `App_Start/` | Routes, bundles, filters, Web API, DI lookups |
| `Views/Shared/` | Layout, partials de scripts |
| `Scripts/` | JS runtime + ferramentas |
| `LibUI_AdminLTE-4.0.0/` | UI, plugins, `start.js` |
| `Content/` | CSS |
| `Tests/` | Testes de lookups |
| `Properties/PublishProfiles/` | Perfis de publish (avaliação) |
| `docs/`, raiz (`AI-CONTEXT.md`, `CHANGELOG-DEV.md`, etc.) | Documentação técnica |
| `Web.config`, `Web.Release.config`, `packages.config`, `.csproj` | Configuração |

### Tipos de arquivos analisados

`.cs`, `.cshtml`, `.js`, `.css`, `.config`, `.csproj`, `packages.config`, documentação Markdown técnica.

### Fora do escopo

- Alteração de código, config, schema SQL, publish remoto, instalação/atualização de pacotes.
- Análise profunda de `_filestemp/` (7173 arquivos, **gitignored** — mencionado como risco local).
- Comportamento em produção não evidenciado no código (IIS, firewall, backups, monitoramento externo).
- Análise exaustiva de **cada** função JS inline (~200+ views com `<script>`) — amostragem + limitações documentadas.

### Limitações da análise

1. **Código morto:** ausência de referência textual **não prova** não uso (rotas dinâmicas, menus DB, jobs, e-mail).
2. **Funções JS mortas:** análise estática parcial; funções inline em modals podem ser invocadas por eventos dinâmicos.
3. **Vulnerabilidades NuGet:** identificação por nome/versão; scan CVE formal não executado.
4. **Performance:** sem profiling runtime; achados baseados em padrões de código.
5. **Secrets locais:** arquivos `App_Data/Secrets/*.local.config` existem no disco de dev; valores **não** reproduzidos neste relatório.

---

## 3. Mapa geral de achados

| ID | Categoria | Achado | Gravidade | Probabilidade | Impacto | Prioridade | Arquivos envolvidos |
|----|-----------|--------|-----------|---------------|---------|------------|-------------------|
| A01 | Segurança | ClientSecret Itaú hardcoded (homolog + prod) | Crítica | Alta | Segurança | P0 | `Robos/Itau/RoboItauBolecode.cs` |
| A02 | Segurança | API key ANP/TextBox hardcoded | Crítica | Alta | Segurança | P0 | `Areas/gc/Controllers/RelatoriosRegulamentacaoController.cs` |
| A03 | Segurança | Senha em log de login falho | Crítica | Alta | Segurança | P0 | `Controllers/UserIdentityController.cs` |
| A04 | Segurança | SQL concatenado com entrada de usuário | Alta | Média | Segurança, BD | P1 | `MovimentosEntradasController.cs`, `ComexProdutosController.cs`, `PedidosController.cs` |
| A05 | Segurança | POSTs Ajax sem antiforgery (~64% dos HttpPost) | Alta | Alta | Segurança | P1 | Controllers diversos |
| A06 | Segurança | Controllers financeiros/GED sem CustomAuthorize | Alta | Média | Segurança | P1 | `FinanceiroLancamentosController.cs`, `GedController.cs`, `Relatorios*Controller.cs` |
| A07 | Segurança | JobServer anônimo, chave opcional | Alta | Média | Operacional, Segurança | P1 | `Controllers/JobServerController.cs` |
| A08 | Segurança | Html.Raw em conteúdo de sessão (navbar) | Alta | Baixa | Segurança, XSS | P1 | `Views/Shared/_Navbar.cshtml` |
| A09 | Segurança | SSL3 habilitado em envio de e-mail | Média | Média | Segurança | P2 | `Lib/LibEmail.cs`, `Robos/Aws/BotAwsEmail.cs` |
| A10 | Performance | Controller monolítico de pedidos (~9.4k linhas) | Alta | Alta | Performance, Manutenção | P2 | `MovimentosController.cs` |
| A11 | Acesso a dados | Mistura EF + LibDB/SqlQuery no mesmo controller | Média | Alta | Manutenção, BD | P2 | 17+ controllers (ver §7) |
| A12 | Arquitetura | Ausência de camada Repository/Service de domínio | Média | Alta | Manutenção, Evolução | P2 | Projeto inteiro |
| A13 | Dependências | .NET Framework 4.7.2 (EOL Microsoft) | Média | Alta | Evolução | P2 | `.csproj`, `Web.config` |
| A14 | Dependências | Rotativa / Zen.Barcode / WebGrease legados | Baixa | Média | Manutenção, Publish | P3 | `packages.config` |
| A15 | Código morto | `PedidosVendaServices` esqueleto sem uso | Baixa | Alta | Manutenção | P3 | `Areas/gc/Services/PedidosVendaServices.cs` |
| A16 | Publicação | CSP permissiva (`unsafe-inline`, `unsafe-eval`) | Média | Alta | Segurança | P2 | `Web.config` |
| A17 | Performance | `RoboEnotasNFE` carrega listas inteiras (`g_unidade_medida.ToList()`) | Média | Média | Performance | P2 | `Robos/ENotas/RoboEnotasNFE.cs` |
| A18 | Padronização | Dois arquivos EF stub (`ModelDbGdiPlataform1.cs`) | Baixa | Alta | Manutenção | P4 | `Db/ModelDbGdiPlataform1*.cs` |
| A19 | Manutenibilidade | Arquivos `*Copia*` no working tree | Baixa | Alta | Manutenção | P3 | `BACKLOG-DEV - Copia.md`, `sql-server.local - Copia.config` |
| A20 | Observabilidade | Logging não estruturado (LibLogger pontual) | Média | Alta | Operacional | P2 | `Lib/LibLogger.cs`, `Global.asax.cs` |

---

## 4. Tecnologias obsoletas ou com risco

| ID | Tecnologia / Dependência | Versão identificada | Arquivo | Evidência | Risco | Gravidade | Recomendação |
|----|--------------------------|---------------------|---------|-----------|-------|-----------|--------------|
| T01 | .NET Framework | 4.7.2 | `GDI-ERP-Plataform.csproj` L21 | `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` | Stack legada em manutenção; **sem** reescrita Core | Média | Monólito permanente IIS; bump 4.8.1 opcional em `docs/relatorio-migracao-netframework-472-481.md` |
| T02 | ASP.NET MVC | 5.3.0 | `packages.config` | `Microsoft.AspNet.Mvc` 5.3.0 | Framework legado; sem evolução | Média | Manter na fase atual; planejar migração incremental |
| T03 | Entity Framework | 6.5.2 | `packages.config` | EF6 Database First EDMX | EDMX frágil em refactors; tooling VS | Média | Consolidar regeneração EDMX; avaliar EF Core só em trilha separada |
| T04 | WebGrease + Optimization | 1.6.0 / 1.1.3 | `packages.config` | Bundling legado ASP.NET | Manutenção mínima | Baixa | Manter até migrar bundling; bundles atuais são pequenos |
| T05 | Modernizr | 2.8.3 | `packages.config` | Pacote presente | Sem referência em views analisadas | Baixa | Validar não uso; remover do `packages.config` em PR dedicado |
| T06 | Rotativa | 1.7.3 | `packages.config` | Wrapper wkhtmltopdf | Dependência externa frágil no IIS | Média | Documentar wkhtmltopdf no servidor; avaliar alternativa PDF |
| T07 | Zen.Barcode.Rendering.Framework | 3.1.10729.1 | `packages.config` | Timestamp ~2011 | Sem manutenção | Baixa | Monitorar; substituir se falhar em 4.8.1 |
| T08 | SSL3 | — | `Lib/LibEmail.cs` L30 | `SecurityProtocolType.Tls12 \| Ssl3` | Protocolo inseguro obsoleto | Média | Remover `Ssl3`; usar `Tls12` (ou `Tls13` se disponível) |
| T09 | jQuery (duplicado) | 3.7.1 | `Scripts/` + bundles | Bootstrap bundle em `Scripts/` e plugins em `LibUI_` | Duplicação potencial de assets | Baixa | Manter estratégia G-PERF-20; evitar carregar duas versões na mesma página |
| T10 | LibMessageDialog | legado | `start.js` L603 | Função mantida; 0 uso em views `Areas/**` | Confusão para devs | Baixa | Manter só em `start.js` conforme decisão 2026-05-20; não reintroduzir em views |
| T11 | System.Text.Json 10.0.7 | 10.0.7 | `packages.config` | Pacote moderno em projeto net472 | Compatibilidade binding redirects | Baixa | Validar redirects em publish Release |

**Nota positiva:** Bootstrap **5.3.8**, jQuery **3.7.1**, AdminLTE **4**, DataTables **2.3.2**, SweetAlert2 e Tempus Dominus **6.9.4** estão alinhados à stack declarada em `AI-CONTEXT.md`.

---

## 5. Riscos de segurança

| ID | Risco | Arquivo | Evidência | Gravidade | Probabilidade | Impacto | Recomendação |
|----|-------|---------|-----------|-----------|---------------|---------|--------------|
| S01 | OAuth ClientSecret hardcoded | `Robos/Itau/RoboItauBolecode.cs` | `ClientSecret = "8712be77-..."` (homolog) e `"e1035a69-..."` (prod) L37–43 | Crítica | Alta | Segurança | Mover para `appSettings.local.config` gitignored; rotacionar secrets no Itaú |
| S02 | API key hardcoded | `Areas/gc/Controllers/RelatoriosRegulamentacaoController.cs` | `TextBoxApiKey = "3A50E055-..."` L201 | Crítica | Alta | Segurança | Externalizar; usar config por ambiente |
| S03 | Password em log | `Controllers/UserIdentityController.cs` | `RecordUsuarioLoginLog.log = "... Senha: " + ...Password...` L505 | Crítica | Alta | Segurança | Logar apenas username + motivo; nunca password |
| S04 | SQL Injection (concatenação) | `Areas/gc/Controllers/MovimentosEntradasController.cs` | `"nf_numero = '" + _numeroNF + "'"` ~L1615 | Alta | Média | Segurança, BD | Parametrizar via `SqlParameter` ou LINQ EF |
| S05 | SQL Injection (concatenação) | `Areas/gc/Controllers/ComexProdutosController.cs` | `"codigo like '" + record...pn + "'"` ~L470 | Alta | Média | Segurança, BD | Parametrizar |
| S06 | SQL Injection (id numérico) | `Areas/crm/Controllers/PedidosController.cs` | `"where FL.id_lancamento = " + id` ~L307 | Média | Média | Segurança | Usar parâmetro `@idLancamento` (padrão já usado L42–63 no mesmo arquivo) |
| S07 | CSRF — POSTs sem token | Controllers diversos | 536 `[HttpPost]` vs 192 antiforgery attrs | Alta | Alta | Segurança | Expandir `[GdiValidateAntiForgeryToken]` + `GdiAjaxAntiForgeryHeaders` em Ajax mutável |
| S08 | Autorização inconsistente | `Areas/gc/Controllers/FinanceiroLancamentosController.cs` | Classe sem `[CustomAuthorize]` | Alta | Média | Segurança | Aplicar roles por action ou filtro global |
| S09 | Autorização inconsistente | `Areas/g/Controllers/GedController.cs` | Upload S3 sem CustomAuthorize na classe | Alta | Média | Segurança | `[CustomAuthorize]` + validação em `ServiceUploadFileGed` (já robusta) |
| S10 | Endpoint anônimo | `Controllers/JobServerController.cs` | `[AllowAnonymous]` + key opcional L24–65 | Alta | Média | Operacional | Tornar `JobServer:Key` obrigatório; restringir IP/rede |
| S11 | XSS via Html.Raw | `Views/Shared/_Navbar.cshtml` | `@Html.Raw(CachePersister.userIdentity.alerts_msg)` L35 | Alta | Baixa | Segurança | Sanitizar HTML ou usar encode; validar origem de `alerts_msg` |
| S12 | XSS potencial (volume) | 217 views | ~540 ocorrências `Html.Raw` | Média | Média | Segurança | Classificar: estático (OK) vs dinâmico (encode) |
| S13 | Upload fraco | `Areas/g/Controllers/NfeController.cs` | `ajaxImportarNfeLote` — sem whitelist extensão | Média | Média | Segurança | Replicar padrão `GedController.ServiceUploadFileGed` |
| S14 | Credenciais SQL local | `App_Data/Secrets/sql-server.local.config` | Plaintext, conta `sa` (dev local) | Crítica* | Média | Segurança | *Crítico se versionado ou em produção; usar least privilege |
| S15 | CSP fraca | `Web.config` | `'unsafe-inline'`, `'unsafe-eval'` documentados | Média | Alta | Segurança | Reduzir inline gradualmente; nonce/hash no longo prazo |
| S16 | Sem HSTS | `Web.config` | Ausente em `customHeaders` | Média | Média | Segurança | Adicionar `Strict-Transport-Security` em Release/IIS |
| S17 | customErrors Off (dev) | `Web.config` L27 | `mode="Off"` + `debug="true"` | Baixa* | Alta | Segurança | *Esperado em dev; Release transform corrige |

**Configuração positiva identificada:**

- Secrets externalizados via `configSource` / `appSettings file` (`Web.config` L11–18, L303).
- `.gitignore` ignora `sql-server.local.config`, `appSettings.local.config`, `_filestemp/`.
- Headers: `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, CSP (permissiva).
- `Web.Release.config`: `customErrors On`, compressão, cache estático.
- Upload GED: whitelist extensões + blocklist MIME (`GedController.cs` L247–261).

---

## 6. Riscos de performance

| ID | Problema | Arquivo | Evidência | Gravidade | Probabilidade | Impacto | Recomendação |
|----|----------|---------|-----------|-----------|---------------|---------|--------------|
| P01 | God controller — manutenção e hotspots | `MovimentosController.cs` | ~9.406 linhas, 49× `SaveChanges()` | Alta | Alta | Performance, Manutenção | Extrair services; revisar transações |
| P02 | God controller financeiro | `FinanceiroLancamentosController.cs` | ~4.216 linhas | Alta | Alta | Performance, Manutenção | Idem |
| P03 | Listas completas em memória (NFe) | `Robos/ENotas/RoboEnotasNFE.cs` | `db.g_unidade_medida.ToList()` L624; múltiplas queries por movimento | Média | Média | Performance | Cache estático de UM/NCM; reduzir round-trips |
| P04 | Loops com queries LINQ repetidas | `MovimentosEntradasController.cs` | `.Where(...).FirstOrDefault()` dentro de loops de importação NF L541–1588 | Média | Média | Performance, BD | Pré-carregar dicionários (padrão já usado em `FinanceiroController.GetDados` L275–282) |
| P05 | Paginação SQL parcial | Vários `GetDados*` | Nem todos usam `LibDataTableSqlPaging` | Média | Média | Performance | Inventariar `GetDados*` sem `OFFSET/FETCH` |
| P06 | Paginação implementada (positivo) | `FinanceiroController.cs` | `OFFSET ... FETCH NEXT` L261 | — | — | — | Replicar padrão |
| P07 | Paginação implementada (positivo) | `AtendimentosController.cs` | `LibDataTableSqlPaging.SqlCount/SqlPage` L77–78 | — | — | — | Manter |
| P08 | Scripts condicionais (positivo) | `_LayoutScriptsOptional.cshtml` | G-PERF-20: DT/Select2/Tempus sob demanda | — | — | — | Expandir flags para views pesadas |
| P09 | executionTimeout 600s | `Web.config` L25 | 10 min por request | Baixa | Baixa | Operacional | Monitorar requests longos; async onde possível (hoje **0** `async/await` em controllers) |
| P10 | maxRequestLength 100MB | `Web.config` | Upload grande | Média | Baixa | Operacional | Validar limites por endpoint |

---

## 7. Avaliação de acesso a dados

### Resumo por padrão

| Padrão | Onde | Evidência |
|--------|------|-----------|
| **Entity Framework 6** | Dominante | `GdiPlataformEntities` em `Db/ModelDbGdiPlataform.Context1.cs`; ~78 arquivos instanciam contexto |
| **EF SqlQuery** | DataTables, relatórios | `Database.SqlQuery`, `DbSet.SqlQuery` em ~24 arquivos |
| **LibDB (ADO via conexão EF)** | Relatórios, saldos, filtros | `Lib/LibDB.cs` — `GetDataTable`, `dbQueryExec`; referenciado em ~51 arquivos |
| **ADO.NET direto** | Raro | `LmsEvidenceController.cs` — `SqlConnection` + parâmetros |
| **Repository** | Ausente | 0 arquivos `*Repository*.cs` |

### Tabela de fluxos problemáticos

| ID | Fluxo / Área | Arquivos | Padrão encontrado | Problema | Risco | Recomendação |
|----|--------------|----------|-------------------|----------|-------|--------------|
| D01 | Pedidos de venda | `MovimentosController.cs` | EF + LibDB + SqlQuery concatenado | Mistura 3 caminhos; SQL `id_movimento = "+` | Alto | Service único; SQL parametrizado |
| D02 | Financeiro lançamentos | `FinanceiroLancamentosController.cs` | EF + LibDB | Mistura; controller 4k+ linhas | Alto | Extrair queries para partial/service |
| D03 | NF entrada | `MovimentosEntradasController.cs` | EF + StartDBGestaoComercial + SQL concat | NF número/série concatenados | Alto | Parametrizar |
| D04 | Portal cliente | `PedidosController.cs` (crm) | SqlQuery parametrizado **e** concat | Inconsistência no mesmo controller | Médio | Uniformizar parametrização |
| D05 | Lookups financeiros | `LookupQueryService.Financeiro.cs` | EF + SqlQuery `"id_usuario = " +` | Anti-padrão em serviço moderno | Médio | Parametrizar |
| D06 | Relatórios regulamentação | `RelatoriosRegulamentacaoController.cs` | SQL datas concatenadas + LibDB | Datas formatadas em string | Médio | Parâmetros `@dataIni/@dataFim` |
| D07 | Filtro genérico obsoleto | `Lib/LibStringFormat.cs` | `[Obsolete]` `SentencaSQLFiltroGenerico` L365 | Escape parcial de LIKE | Médio | Confirmar 0 uso; remover chamadas residuais |
| D08 | Paginação DataTables | `Lib/LibDataTableSqlPaging.cs` | COUNT + OFFSET/FETCH | Padrão correto | — | Expandir adoção |
| D09 | LMS QA | `LmsEvidenceController.cs` | ADO parametrizado | **Bom exemplo** | — | Usar como referência |
| D10 | COMEX importações | `ComexImportacoesController.cs` | EF + SqlQuery + LibDB | Mistura | Médio | Documentar fronteira por action |

### Onde há Entity Framework

Controllers, `Lib/Lookups/*`, `Robos/*`, entidades em `Db/*.cs` (~170 tabelas), `SaveChanges()` intensivo em fluxos transacionais.

### Onde há ADO.NET

`LibDB.cs` (principal), `LmsEvidenceController.cs` (direto), execução via `DbCommand` na conexão do EF.

### Onde há mistura inadequada

**17 controllers confirmados** com EF + LibDB/SqlQuery no mesmo arquivo, incluindo: `MovimentosController`, `FinanceiroLancamentosController`, `MovimentosEntradasController`, `FinanceiroController`, `ComexImportacoesController`, `EstoqueController`, `GerencialController`, `Relatorios*Controller`, `PedidosController` (crm), `AtendimentosController`.

### SQL por concatenação (amostra confirmada)

- Strings de usuário: NF, código produto, filtros texto.
- IDs numéricos: `id_movimento`, `id_cliente`, `id_lancamento` (risco menor, mas anti-padrão).

### Consultas candidatas a otimização

- `RoboEnotasNFE`: múltiplas listas completas por emissão NF.
- Importação NF (`MovimentosEntradasController`): loops com `FirstOrDefault` LINQ.
- Controllers sem `LibDataTableSqlPaging` nos `GetDados*` legados.

---

## 8. Arquivos mortos ou possivelmente não utilizados

| ID | Arquivo | Tipo | Classificação | Evidência | Confiança | Risco de remoção | Recomendação |
|----|---------|------|---------------|-----------|-----------|------------------|--------------|
| F01 | `Areas/gc/Services/PedidosVendaServices.cs` | Service C# | Morto provável | Classe só define `GetMsgProcessamento()`; **0 referências** fora do `.csproj` | Alta | Baixo | Validar histórico; remover ou implementar |
| F02 | `Db/ModelDbGdiPlataform1.cs` | EF stub | Morto provável | Arquivo auto-generated **vazio** (10 linhas); não referenciado em código | Alta | Baixo | Regenerar/limpar artefato EDMX duplicado |
| F03 | `Db/ModelDbGdiPlataform1.Designer.cs` | EF designer | Morto provável | Par de F02; só no `.csproj` | Alta | Baixo | Idem F02 |
| F04 | `BACKLOG-DEV - Copia.md` | Markdown | Morto provável | Cópia manual na raiz | Alta | Nulo | Remover após diff com `BACKLOG-DEV.md` |
| F05 | `App_Data/Secrets/sql-server.local - Copia.config` | Config | Morto provável | Duplicata de secrets local | Alta | Médio (dev) | Apagar cópia; manter só `.example` + gitignored |
| F06 | ~~`nul)`~~ | Arquivo acidental | **Removido** (2026-06-05) | Artefacto cmd `2>nul)` na raiz; gitignored | — | Nulo | Resolvido |
| F07 | `_filestemp/` (7173 arquivos) | Temp/backup | Legado preservado | Gitignored; backups de intervenções Cursor | Média | Nulo local | Manter gitignore; limpar disco local periodicamente |
| F08 | Views `FinanceiroFaturamentos/*` | Views | Removido (confirmado) | 0 arquivos; changelog registra remoção módulo | Alta | N/A | Já removido — validar menus DB |
| F09 | `PortalVendedorController` | Controller | Removido (confirmado) | 0 arquivos; refs só em docs/SQL scripts | Alta | N/A | Validar roles/menus SQL pendentes |
| F10 | `Modernizr` (pacote) | NuGet | Morto possível | Em `packages.config`; 0 refs em `.cshtml`/JS runtime | Média | Baixo | Confirmar bundle; desinstalar se unused |
| F11 | `Lib/LibStringFormat.cs` método obsoleto | Helper | Código legado | `[Obsolete]` L365 | Média | Médio | Grep usages antes de remover |
| F12 | `Scripts/gdi_inspect_dialog_html_title.ps1` (sem prefixo data) | Script | Morto possível | Duplicata de `2026_05_20_gdi_inspect_dialog_html_title.ps1` | Média | Baixo | Consolidar scripts conforme regra `AAAA_MM_DD_` |

**Nota:** arquivos em `Tests/GDI-ERP-Plataform.Lookups.Tests/` estão **ativos** (8 arquivos de teste de cache/lookups).

---

## 9. Funções mortas ou possivelmente não utilizadas em C#

| ID | Arquivo | Classe | Método / Função | Classificação | Evidência | Confiança | Risco de remoção | Recomendação |
|----|---------|--------|-----------------|---------------|-----------|-----------|------------------|--------------|
| C01 | `PedidosVendaServices.cs` | `PedidosVendaServices` | `GetMsgProcessamento()` | Função morta provável | Sem callers | Alta | Baixo | Remover classe ou implementar domínio |
| C02 | `LibStringFormat.cs` | `LibStringFormat` | `SentencaSQLFiltroGenerico` | Código legado obsoleto | `[Obsolete]` attribute | Alta | Médio | Inventário grep; remover calls |
| C03 | `PedidosVendaServices.cs` | ctor + campo `db` | Instancia EF sem uso | Função morta provável | `db` nunca usado | Alta | Baixo | Idem C01 |
| C04 | `start.js` (via arquitetura) | — | `LibMessageDialog` | Código legado | 0 calls em views; mantido em `start.js` | Alta | Médio | **Não remover** sem decisão — shim SweetAlert2 depende |
| C05 | Actions POST legados | Vários controllers | Actions de módulos removidos | Uso incerto | Changelog cita remoção Faturamentos/Requisicoes | Baixa | Alto | Mapear menus/roles DB antes |
| C06 | `StartDBGestaoComercial` | `LibDBGestaoComercial.cs` | `TotalizarMovimentosItens` | **Em uso** | Chamado 6× em `MovimentosEntradasController` L2475+ | Alta | Alto | **Não remover** — falso positivo inicial |

---

## 10. Funções mortas ou possivelmente não utilizadas em JavaScript

| ID | Arquivo | Função / Bloco | Local | Classificação | Evidência | Confiança | Risco de remoção | Recomendação |
|----|---------|----------------|-------|---------------|-----------|-----------|------------------|--------------|
| J01 | `start.js` | `LibMessageDialog` | Arquivo .js global | Código legado | Substituído por `LibMessageSuccess/Confirm`; 0 uso views | Alta | Médio | Manter até remover shim; documentado |
| J02 | `gdi-swal2-dialog-shim.js` | Ponte legada | Bundle swal-compat | Código legado | Referenciado por `LibMessageDialog` | Alta | Médio | Não remover sem migração completa |
| J03 | Funções `jsSendForm` duplicadas | ~50+ modals `.cshtml` | Script inline | Código duplicado | Padrão repetido por modal | Alta | Médio | Extrair helper comum (backlog) |
| J04 | `sessionInactivity.js` | (módulo) | Layout | **Em uso** | Referenciado `_Layout.cshtml` L126 | Alta | Alto | Não remover |
| J05 | `jsFileInputChange` | função global | `Scripts/jsFileInputChange.js` | **Em uso** | onchange em uploads + layout | Alta | Alto | Não remover |
| J06 | Handlers DataTables inline | `jsCreateOTable*` | Views Index | Uso incerto parcial | Nomes variam por tela | Baixa | Alto | Validar por view antes de limpar |

**Limitação:** inventário exaustivo de funções inline exigiria AST por view; recomenda-se script de inventário (`Scripts/2026_05_20_gdi_*`) antes de limpeza.

---

## 11. Duplicidades e inconsistências

| ID | Tipo | Arquivos envolvidos | Evidência | Risco | Recomendação |
|----|------|---------------------|-----------|-------|--------------|
| U01 | Controller responsabilidade excessiva | `MovimentosController.cs` | ~9.406 linhas | Alto | Partials `.Lookups.cs` existem; falta `.Services.cs` |
| U02 | Controller responsabilidade excessiva | `FinanceiroLancamentosController.cs` | ~4.216 linhas | Alto | Extrair boletos, anexos, baixas |
| U03 | Lógica duplicada | Modals `jsSendForm` | Dezenas de `.cshtml` | Médio | Helper JS compartilhado |
| U04 | Query duplicada | `RoboEnotasNFE.cs` | SqlQuery produtos/NCM repetido L619 vs L1568 | Médio | Método privado parametrizado |
| U05 | Padrão acesso dados | 17 controllers | EF + LibDB misturados | Alto | Definir fronteira por fluxo |
| U06 | Lookup duplicado | `ClientesLookupController` vs `MovimentosController.GetClientesLookup` | URLs diferentes em views | Baixo | Consolidar endpoints typeahead |
| U07 | Documentação duplicada | ~~`.md/`~~ removida (2026-06-05) | Conteúdo canónico em `docs/` | — | Resolvido — `docs/relatorio-migracao-netframework-472-481.md`, `docs/investigacao-timeout-sessao.md` |
| U08 | Scripts duplicados | `Scripts/gdi_*.ps1` vs `Scripts/2026_05_20_gdi_*.ps1` | Mesma função, nomes diferentes | Baixo | Padronizar prefixo data |
| U09 | Namespace inconsistente | `LibDBGestaoComercial.cs` | Classe `StartDBGestaoComercial` em namespace `Areas.gc.Controllers` mas arquivo em `Lib/` | Médio | Mover para `Services/` ou namespace `Lib` |
| U10 | Autorização inconsistente | Controllers com/sem `[CustomAuthorize]` | 14 controllers área + 6 raiz sem attr | Alto | Matriz roles uniforme |

---

## 12. Avaliação de arquitetura

### Principais pontos fortes

1. **Monólito coeso** adequado ao domínio ERP integrado (COMEX + comercial + financeiro + portal `crm`).
2. **Modernização UX documentada** — SweetAlert2, DataTables Fases 0–17, helpers centralizados em `start.js`.
3. **`ILookupQueryService`** substitui `LibDataSets` com cache, partials por domínio e testes (`Tests/GDI-ERP-Plataform.Lookups.Tests`).
4. **Segregação de áreas MVC** (`g`, `gc`, `crm`, `qa`, `a`) reflete módulos de negócio.
5. **Secrets externalizados** em dev via `App_Data/Secrets` + gitignore.
6. **Carregamento condicional de scripts** (G-PERF-20) reduz payload.
7. **Upload GED** com validação robusta (whitelist + MIME blocklist + S3).

### Principais fragilidades

1. **Controllers como “god objects”** — especialmente pedidos e financeiro.
2. **Ausência de camada de domínio** — regra de negócio, SQL e integração no mesmo arquivo.
3. **DbContext instanciado ad hoc** — `new GdiPlataformEntities(CachePersister.dataBase)` espalhado; DI piloto só em 2 controllers.
4. **Sessão estática `CachePersister`** — acoplamento global; dificulta testes e migração Core.
5. **EDMX Database First** — regeneração frágil; `ModelDbGdiPlataform1.cs` stub indica drift de tooling.
6. **Autorização por convenção** — depende de `[CustomAuthorize]` manual, não global.
7. **Zero async/await** — threads bloqueadas em I/O (HTTP robôs, S3, Graph).
8. **Testes automatizados mínimos** — só lookups (~8 arquivos); sem cobertura de controllers críticos.

### Acoplamento excessivo

- UI ↔ Controller ↔ EF/SQL ↔ Robôs externos sem interfaces.
- `ViewBag` intensivo vs ViewModels tipados.
- `VersionERP` como cache buster global acoplado a deploy.

### Oportunidades de Services / Repositories / ViewModels

| Área | Oportunidade |
|------|--------------|
| Pedidos (`MovimentosController`) | `PedidosVendaService` real (substituir stub) |
| Financeiro | `FinanceiroLancamentosService`, `BoletoService` |
| COMEX | `ComexImportacaoService`, `InvoiceService` |
| NFe | Isolar `RoboEnotasNFE` atrás de interface |
| Relatórios | `RelatorioQueryService` com SQL parametrizado |

### Dificuldade para testes

- Static `CachePersister`, `HttpContext`, EF direct — impede unit tests sem harness pesado.
- Lookups já testáveis — **modelo a replicar**.

### Migração ASP.NET Core — **cancelada** (2026-05-25)

O produto permanece em **ASP.NET MVC .NET Framework 4.7.2** no IIS. Não está planeado Strangler Fig nem reescrita para .NET 6+ / ASP.NET Core. Evolução via refactors internos, serviços de domínio e hardening de segurança no monólito actual.

---

## 13. Melhorias agrupadas por tipo

| Tipo de melhoria | Descrição | Benefício esperado | Risco se não fizer | Prioridade | Esforço |
|------------------|-----------|-------------------|-------------------|------------|---------|
| Segurança | Externalizar secrets; corrigir log de senha | Evita vazamento credenciais | Comprometimento integrações/SQL | P0 | Baixo |
| Segurança | Parametrizar SQL concat alto risco | Mitiga SQL Injection | Exfiltração/dano BD | P1 | Médio |
| Segurança | CSRF em POSTs mutáveis Ajax | Mitiga CSRF | Ações forged | P1 | Médio |
| Segurança | CustomAuthorize uniforme | Fecha bypass autorização | Acesso indevido financeiro/GED | P1 | Médio |
| Performance | Extrair services de controllers 9k/4k linhas | Manutenção + hotspots | Lentidão, bugs regressivos | P2 | Alto |
| Performance | Cache tabelas estáticas em robôs NFe | Menos RAM/CPU emissão | Timeout emissão NF | P2 | Baixo |
| Arquitetura | DI de `ILookupQueryService` para todos controllers | Testabilidade | Acoplamento | P2 | Médio |
| Acesso a dados | 1 padrão por fluxo (EF **ou** SQL parametrizado) | Previsibilidade | Bugs transacionais | P2 | Alto |
| UI e front-end | Reduzir Html.Raw dinâmico | Mitiga XSS | Session hijack / XSS | P1 | Médio |
| JavaScript | Consolidar `jsSendForm` modals | Menos duplicação | Inconsistência UX | P3 | Médio |
| Dependências | Remover Modernizr, SSL3 | Superfície menor | Exploit protocolo | P3 | Baixo |
| Código morto | Limpar stubs, cópias, PedidosVendaServices | Clareza repo | Confusão dev | P3 | Baixo |
| Padronização | Namespace `StartDBGestaoComercial` | Organização | Onboarding lento | P4 | Baixo |
| Observabilidade | Logs estruturados (JSON) + correlation id | Diagnóstico produção | MTTR alto | P2 | Médio |
| Publicação | HSTS + CSP gradual | Hardening IIS | Downgrade HTTPS | P2 | Médio |
| Manutenibilidade | Expandir testes além lookups | Regressão segura | Medo de refatorar | P2 | Alto |

---

## 14. Roadmap recomendado

### Fase 1: Segurança e estabilidade (P0–P1)

- [ ] Rotacionar e externalizar secrets Itaú, ANP, SQL (sem `sa`).
- [ ] Remover password de logs (`UserIdentityController`).
- [ ] Tornar `JobServer:Key` obrigatório + restricao rede.
- [ ] Parametrizar SQL crítico (NF entrada, produtos COMEX, portal).
- [ ] `[CustomAuthorize]` em `FinanceiroLancamentosController`, `GedController`, `Relatorios*`, `AuditController`.
- [ ] `[GdiValidateAntiForgeryToken]` nos POSTs Ajax de mutação (NFe cancel, financeiro, uploads).

### Fase 2: Performance e acesso a dados (P2)

- [ ] Inventário `GetDados*` sem paginação SQL → aplicar `LibDataTableSqlPaging`.
- [ ] Otimizar `RoboEnotasNFE` (cache UM/NCM; menos round-trips).
- [ ] Refatorar loops NF entrada para dicionários pré-carregados.
- [ ] Definir guideline **EF vs SQL** por módulo documentado em `AI-CONTEXT.md`.

### Fase 3: Código morto e padronização (P3)

- [ ] Validar e remover `PedidosVendaServices`, `ModelDbGdiPlataform1*`, `*Copia*` (~~`nul)`~~ removido 2026-06-05).
- [ ] Desinstalar Modernizr se confirmado unused.
- [ ] Consolidar scripts `Scripts/` com prefixo `2026_MM_DD_`.
- [ ] Mover `StartDBGestaoComercial` para namespace/pasta corretos.

### Fase 4: Arquitetura e modernização incremental (P2–P3)

- [ ] Extrair `PedidosVendaService` de `MovimentosController` (primeiro vertical slice).
- [ ] Expandir DI (`LookupDependencyConfig`) para controllers gc críticos.
- [ ] Trilha **.NET 4.8.1** (branch isolado).
- [ ] ViewModels tipados para telas complexas (pedido, financeiro).

### Fase 5: Observabilidade e governança (P2–P4)

- [ ] Padronizar logs (`LibLogger` → campos estruturados).
- [ ] Health check expandido (`HealthController` já existe).
- [ ] Checklist pre-publish automatizado (scripts existentes).
- [ ] Critérios de qualidade PR (antiforgery, authorize, SQL param).

---

## 15. Plano de validação manual antes de remover código morto

Checklist objetivo — **executar todos os itens aplicáveis** antes de qualquer remoção:

- [ ] Buscar referência textual no projeto (`grep`/IDE) ao símbolo/arquivo.
- [ ] Buscar referência em **rotas** (`RouteConfig`, `WebApiConfig`, atributos `[Route]`).
- [ ] Buscar referência em **views** (`@Url.Action`, links HTML, partials).
- [ ] Buscar referência em **bundles** (`BundleConfig`, `_Layout*.cshtml`).
- [ ] Buscar referência em **JavaScript** (`start.js`, inline handlers).
- [ ] Buscar referência em **menus** (DB, `NavbarController`, scripts SQL de menu).
- [ ] Buscar referência em **permissões/roles** (`g_*`, `gc_*`, SQL perfis).
- [ ] Buscar referência em **chamadas AJAX** (DataTables `sAjaxSource`, `$.ajax` URLs).
- [ ] Buscar referência em **configuração** (`Web.config`, transforms, job definitions).
- [ ] Buscar referência em **banco de dados** (SPs, jobs SQL Agent, triggers).
- [ ] Executar **fluxo funcional** relacionado em homologação.
- [ ] Validar com **usuário-chave** (comercial, financeiro, COMEX, portal cliente).
- [ ] Criar **backup ou branch** antes da remoção.
- [ ] Remover em **lote pequeno** (≤5 arquivos por PR).
- [ ] Testar **regressão** smoke (login, pedido, NF, boleto, portal).
- [ ] Registrar em **`CHANGELOG-DEV.md`**.

---

## 16. Recomendações finais

### Top 10 ações recomendadas

1. Externalizar e rotacionar secrets (Itaú, ANP, SQL) — **imediato**.
2. Eliminar log de senha no login falho.
3. Parametrizar SQL de NF/produtos/portal (top 5 pontos concat).
4. Obrigar antiforgery + autorização em financeiro/GED/NFe Ajax POSTs.
5. Endurecer `JobServer` (key + rede).
6. Plano de decomposição `MovimentosController` (primeiro service: save pedido).
7. Inventário paginação em todos `GetDados*`.
8. Sanitizar `Html.Raw` em conteúdo dinâmico (`alerts_msg`, ViewBag HTML).
9. Remover SSL3 de `LibEmail` / `BotAwsEmail`.
10. Executar trilha **4.8.1** em branch isolado (sem misturar refactors).

### Top 5 riscos mais graves

1. Secrets hardcoded no código fonte versionado.
2. SQL Injection via concatenação em fluxos COMEX/portal.
3. POSTs mutáveis sem CSRF (~344 endpoints).
4. Controllers financeiros/GED sem `[CustomAuthorize]` explícito.
5. Senha em log de autenticação.

### Top 5 ganhos rápidos (baixo esforço)

1. Corrigir log de senha (1 linha).
2. Remover arquivos `*Copia*` (~~`nul)`~~ removido 2026-06-05).
3. Remover `Ssl3` de email (2 arquivos).
4. Tornar `JobServer:Key` obrigatório (config + validação).
5. Cache `g_unidade_medida` / NCM no robô NFe.

### Itens que NÃO devem ser alterados sem validação funcional

- `MovimentosController` fluxos NF/boleto/separação/expedição.
- `RoboEnotasNFE` integração produção.
- `UserIdentityController` / portal `crm` (`AcessoPortal`).
- EDMX / regeneração entidades EF.
- Menus e roles em SQL Server (`g_perfis`, scripts deactivate portal).
- `_Layout.cshtml` ordem de scripts (`start.js`, swal-compat).

### Itens que exigem decisão de arquitetura

- Estratégia **EF6 vs SQL bruto** por módulo (política formal).
- Bump opcional **4.8.1** (TFM) vs manter **4.7.2** — **sem** ASP.NET Core.
- Filtro **global** de autorização vs `[CustomAuthorize]` por action.
- Política **CSRF** global para Web API + MVC Ajax.
- Substuição **Rotativa/wkhtmltopdf** vs serviço PDF externo.

---

## Apêndice A: Lista de evidências por arquivo

| Arquivo | Evidências encontradas | Categorias relacionadas | Severidade máxima |
|---------|------------------------|-------------------------|-------------------|
| `Robos/Itau/RoboItauBolecode.cs` | ClientSecret homolog+prod hardcoded | Segurança, Dependências | Crítica |
| `Areas/gc/Controllers/RelatoriosRegulamentacaoController.cs` | API key hardcoded; SQL datas concat | Segurança, Acesso a dados | Crítica |
| `Controllers/UserIdentityController.cs` | Password em log; SqlQuery concat login | Segurança, Acesso a dados | Crítica |
| `Controllers/JobServerController.cs` | AllowAnonymous; key opcional | Segurança, Arquitetura | Alta |
| `Areas/gc/Controllers/MovimentosController.cs` | ~9406 linhas; EF+LibDB; SQL concat; 49 SaveChanges | Arquitetura, Performance, Acesso a dados | Alta |
| `Areas/gc/Controllers/FinanceiroLancamentosController.cs` | ~4216 linhas; sem CustomAuthorize classe | Arquitetura, Segurança | Alta |
| `Areas/g/Controllers/GedController.cs` | Sem CustomAuthorize; upload robusto | Segurança (positivo+negativo) | Alta |
| `Areas/gc/Controllers/MovimentosEntradasController.cs` | SQL NF concat; loops LINQ | Segurança, Performance | Alta |
| `Areas/crm/Controllers/PedidosController.cs` | SQL mix param/concat | Acesso a dados, Segurança | Média |
| `Robos/ENotas/RoboEnotasNFE.cs` | ToList completos; SQL concat id_movimento | Performance, Acesso a dados | Média |
| `Lib/LibEmail.cs` | SSL3 + TLS12 | Dependências, Segurança | Média |
| `Views/Shared/_Navbar.cshtml` | Html.Raw alerts_msg | Segurança (XSS) | Alta |
| `Web.config` | CSP permissiva; customErrors Off dev; secrets externalized | Publicação, Segurança | Média |
| `Areas/gc/Services/PedidosVendaServices.cs` | Classe stub sem callers | Código morto | Baixa |
| `Db/ModelDbGdiPlataform1.cs` | Stub vazio EF | Código morto | Baixa |
| `Lib/LibDataTableSqlPaging.cs` | Paginação SQL correta | Performance (positivo) | — |
| `Lib/Lookups/ILookupQueryService.cs` | Substitui LibDataSets | Arquitetura (positivo) | — |
| `Security/GdiValidateAntiForgeryTokenAttribute.cs` | Antiforgery Ajax headers | Segurança (positivo) | — |
| `Views/Shared/_LayoutScriptsOptional.cshtml` | Scripts condicionais G-PERF-20 | Performance (positivo) | — |
| `Tests/GDI-ERP-Plataform.Lookups.Tests/*` | 8 testes lookups | Manutenibilidade (positivo) | — |
| `BACKLOG-DEV - Copia.md` | Duplicata documentação | Código morto | Baixa |
| `App_Data/Secrets/sql-server.local - Copia.config` | Duplicata secrets | Segurança, Código morto | Alta* |

---

## Apêndice B: Itens que precisam de confirmação humana

| Item | Motivo da dúvida | Quem deve confirmar | Risco de decisão incorreta |
|------|------------------|---------------------|----------------------------|
| Remoção `PedidosVendaServices.cs` | Pode ser placeholder de refactor planejado | Desenvolvedor responsável | Quebra compile futuro |
| `ModelDbGdiPlataform1.cs` vazio | Pode ser artefato de T4/EDMX necessário ao build | Desenvolvedor responsável | Falha regeneração EF |
| Controllers sem `[CustomAuthorize]` | Podem depender de check manual em action/base | Desenvolvedor responsável | Expor endpoints |
| POSTs Ajax sem antiforgery | Alguns podem ser read-only ou protegidos por sessão only | Responsável por segurança | CSRF falso negativo |
| Menus PortalVendedor / FinanceiroFaturamentos | Removidos no código; podem existir no BD | Analista funcional / DBA | Links mortos |
| `JobServer:Key` vazio em produção | Comportamento depende deploy | Responsável por infraestrutura | Jobs abertos |
| Uso real Modernizr | Pacote NuGet sem refs em views | Desenvolvedor responsável | Quebra bundle residual |
| Funções JS inline em modals | Podem ser invocadas dinamicamente | Desenvolvedor responsável | Quebra modal |
| Conta SQL `sa` em dev local | Pode ser intencional para restore | Responsável por infraestrutura | Vazamento credencial |
| `_filestemp/` backups locais | Gitignored mas ocupa disco | Gestor do ERP | Nulo funcional; confusão dev |
| Migração 4.8.1 timing | Documentada mas não executada | Gestor do ERP / Arquiteto | Bloqueio futuro |
| Html.Raw em ViewBag.Title | Maioria estática com HTML ícones | Desenvolvedor front-end | XSS baixo se controlado server-side |

---

*Fim do relatório. Documento gerado em modo somente leitura — nenhuma alteração foi aplicada ao código-fonte do ERP.*
