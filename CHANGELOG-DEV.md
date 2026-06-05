# CHANGELOG-DEV.md

> Changelog **operacional** (~200 linhas).  
> **Histórico integral:** `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` (187 entradas)  
> **Contexto fixo:** `AI-CONTEXT.md` | **Pendências:** `BACKLOG-DEV.md` | **Índice temas:** `.cursor/context/2026_06_05_indice-memoria-ia.md`

**Última atualização:** 2026-06-05 — consolidação memória IA + lote prefixos legado

---

## Estado atual do projeto

Monólito ASP.NET MVC **4.7.2** (COMEX, comercial, estoque, financeiro, qualidade). Portal cliente em `crm` + `UserIdentity`. Modernização 2026: **`GdiMvcJsonResults`** + guards modal GET + handlers Ajax/DT homogéneos; lookups via **`ILookupQueryService`**; módulos legados removidos (Filtros, PortalVendedor, FinanceiroFaturamentos/Lancamentos em `g`, etc.).

**Build** Release OK nas intervenções recentes. **VersionERP:** incrementar após `start.js` / `start.css` / `gdi-select2.js`.

---

## Decisões técnicas ativas

- **.NET 4.7.2** nesta fase; migração **4.8.1** trilha isolada (`2026_05_20_migracao-472-481.md`).
- Stack UI fixa (Bootstrap 5, AdminLTE 4, DataTables bs5, SweetAlert2, Tempus Dominus).
- **Dois tipos de tabela:** DataTables (Ajax) vs MVC (`@for`) — não misturar CSS/contratos.
- **Arquitetura centralizada obrigatória:** `2026_06_05_arquitetura-centralizada-erp-gdi.md` + `Lib/GdiMvcJsonResults.cs`.
- **Lookups:** Index = combo local; CreateEdit = `ILookupQueryService` + `*.Lookups.cs`.
- Portal externo = `crm`; `g/PortalCliente` = legado interno.
- Agente: sem `git push` / publish remoto; português BR; registo só linha compacta aqui + `BACKLOG-DEV.md`.

---

## Últimas alterações relevantes

### 2026-06-05 — packages.config: auditoria e remoção de 26 pacotes mortos
- **Validado:** 112 → **86** pacotes; versões alinhadas ao `.csproj` e `targetFramework="net472"`.
- **Removidos:** bloco Microsoft.Graph/Kiota/Identity/Azure (zero uso em código); órfãos `jQuery`, `Modernizr`, `Microsoft.AspNet.WebApi` (metapacote), `SixLabors.ImageSharp` (só props).
- **Sincronizado:** `.csproj` (References/Import), `Web.config` (binding redirects), `using` mortos em `CustomPrincipal` e `FinanceiroLancamentosController`.
- **Build Release OK.** Candidatos futuros: `SkiaSharp.NativeAssets.Linux/macOS`, `ZString`, `bootstrap` NuGet (UI ativa = LibUI).

### 2026-06-05 — Web.config: auditoria e alinhamento de versões
- **7 ficheiros** revistos: raiz (`Web.config`, `Web.Debug.config`, `Web.Release.config`), `Views/Web.config`, áreas `a|g|gc|qa|crm`.
- **Correções:** `MvcWebRazorHostFactory` e assembly MVC em todas as Views → **5.3.0.0** (NuGet `Microsoft.AspNet.Mvc` 5.3.0); binding redirects IdentityModel (Tokens, Protocols, Jwt, JsonWebTokens, Logging) **8.15.0.0 → 8.18.0.0** alinhados a `packages.config`.
- **Validado:** `Scripts/2026_05_22_gdi_verify_web_release_transform.ps1` — Release sem `debug`, `customErrors On`, compressão PERF-012 OK; áreas `web.config` já no `.csproj`.

### 2026-06-05 — GDI-ERP-Plataform.csproj: auditoria e correções
- **Validação:** 1069 Includes, 0 paths em falta, 0 duplicados, 0 views `Gdi*` fora do csproj.
- **Correções:** SourceLink fallback 8.0.0 → 10.0.203; `ExcludeFoldersFromDeployment` + strip publish `.claude`; `None` para `AI-CONTEXT`/`CHANGELOG`/`BACKLOG`; `Content` templates `appSettings` + novo `sql-server.local.config.example`; script `2026_06_05_gdi_verify_csproj_includes.py`.

### 2026-06-05 — Remoção `nul)` acidental na raiz (F06)
- Artefacto cmd (redirecionamento `2>nul)` inválido); apagado com `Remove-Item -LiteralPath`; F06 resolvido em `docs/AVALIACAO-TECNICA-ERP-GDI.md`.

### 2026-06-05 — Remoção pasta `.md/` legada: csproj e refs
- Pasta **`.md/`** removida (duplicata de `docs/`); canónico: `docs/investigacao-timeout-sessao.md`, `docs/relatorio-migracao-netframework-472-481.md`.
- **`.csproj`:** removidos `Content` `.md\*`; `CLAUDE.md` e docs relevantes → `None Include` (versionam no Git, **não** vão ao IIS).
- **`roadmap.md`** / `0004 - auditoria-tecnica-relatorio.md` só existiam em `.md/` — recuperar do histórico Git se ainda forem necessários.

### 2026-06-05 — .gitignore: consolidar Git × IIS + corrigir ProdutosController
- **Reescrita** `.gitignore` (~543 → ~200 linhas): removida duplicata VisualStudio; removidos `*.dll`/`*.exe` globais; removida linha acidental `/Areas/g/Controllers/ProdutosController.cs`.
- **GDI:** `**/_filestemp/`, `App_Data/Secrets/*Copia*`, `.claude/worktrees/`, `nul`/`nul)`; exceção `!Rotativa/*.exe` (PDF no IIS).
- **`.csproj`:** `Rotativa\wkhtmltopdf.exe` e `wkhtmltoimage.exe` como `Content` para publish IIS.

### 2026-06-05 — Prompt genérico enxugar memória (reutilizável)
- **Novo:** `.cursor/context/2026_06_05_prompt-enxugar-memoria-projeto-generico.md` — prompt completo para auditoria/consolidação de memória em **qualquer** projeto (7 fases, hierarquia, anti-padrões).

### 2026-06-05 — Auditoria memória IA: centralizar, enxugar duplicatas
- **Índice único:** `.cursor/context/2026_06_05_indice-memoria-ia.md` — navegação por tema + mapa de duplicatas removidas.
- **`AI-CONTEXT.md` / `CLAUDE.md`:** reduzidos a ponteiro + regras mínimas; detalhe arquitetura/lotes N-* só em contextos datados.
- **`CHANGELOG-DEV.md`:** compactado (~200 linhas); entradas antigas → histórico arquivado.
- **Órfão removido:** `pascalcase-areas-renomeacao-lotes.md` (duplicata sem prefixo).
- **`sync_changelog_recent.py`:** fonte corrigida para `CHANGELOG-DEV.md` na raiz.

### 2026-06-05 — Lote legado 2026_05_20_*: renomear por CreationTime + refs
- 62 renomeações (`2026_05_21_`/`2026_05_22_`/`2026_05_15_`); 85 refs; script `2026_06_05_gdi_rename_legacy_2026_05_20_prefixes.py`; inventário prefixos exit 0.

### 2026-06-05 — G-PUB smoke + I-1b + convenção prefixos
- Smoke arquitetura 5 inventários exit 0; 18× `int.Parse` → `LibNumbers.ConvertInt` em relatórios/g; 35 ficheiros renomeados `2026_05_25_`/`2026_06_01_` → datas corretas.

### 2026-05-29 — Painel Indicadores Qualidade Pós-Venda (ISO 9001) Fases 1–3
- `IndicadoresQualidade` hub + `IndicadoresQualidadePosVenda` (KPIs, Flot, DataTables, Excel ClosedXML); pedido entregue = `id_movimento_posicao >= 6`.

### 2026-05-28 — Ambiente tenant + WhatsApp gerencial
- `CstTenant.ambiente`; `SetTenants()` público; `JobServerController` sem mapeamento duplicado host→DB.
- `RoboWhatsAppGerencial` — resumo diário 18h via Z-API + Task Scheduler.

### 2026-05-25 — Arquitetura centralizada ERP + ciclo N (servidor/cliente)
- **`GdiMvcJsonResults`** Lib; wrappers `JsonDataTableException`/`JsonAjaxErro*` delegam (31+ controllers).
- Modais GET gc/g com guards; remoção `GC.Collect` gc; inventários LibExceptions → **0** em a/crm/g/qa.
- Cliente: C-1/C-2 handlers Ajax homogéneos (gc + 115 views a/g/gc/qa); DT `xhr.dt`/`error.dt`.
- **Detalhe lotes N-A…N-V:** `2026_06_05_arquitetura-centralizada-erp-gdi.md` + histórico — não reexpandir aqui.

### 2026-06-01 — UI cadastros/gc + API pública lote-documento
- Cadastros carga inicial (exceto Produtos/Clientes deferLoading); yesFilterOnOff/Limpar amarelo corrigido (35 views auditadas).
- Select2 pesquisa local; Fretes/Cfop/PagRecCondicoes/Estoque alinhamentos Index.
- `LoteDocumentoPublicoController` — API `GET /api/public/lote-documento` migrada do portal descontinuado.

### 2026-05-20 — Baseline modernização (Fases 0–17)
- DataTables/Ajax padronização; lookups Onda 6; remoção módulos legados; PascalCase B1–B2d; CSRF/XSS fases iniciais.
- **Detalhe:** `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`.

*Entradas anteriores e blocos longos por ficheiro tocado:* arquivo histórico — **não** reabrir neste changelog operacional.

---

## Pendências abertas

Lista completa e critérios de aceite → **`BACKLOG-DEV.md`**.

| Prioridade | Resumo |
|------------|--------|
| Alta | Smoke pós-publish; NFe e-Notas homologação; Release/IIS/health |
| Média | 14 controllers híbridos `ViewBag.combo`; smoke Index cadastros |
| Baixa | Migração 4.8.1; filtro legado residual; índices SQL (DBA) |

---

## Alertas técnicos

| Alerta | Ação |
|--------|------|
| DataTables vs MVC | `2026_05_20_tabelas-datatables-vs-mvc.md` antes de CSS em `<table>` |
| Views `Gdi*` fora do `.csproj` | `python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py` |
| Cache `start.js` | Incrementar `VersionERP` |
| Publish `obj`/PackageTmp | Limpar `obj` se Access denied |
| Filtro Index `id > 1` na base | Pode excluir id=1 — padrão Filiais/Clientes |
| Portal pós-deploy | Novo login se cache `contextoModel_*` legado |
| UTF-8 BOM | `python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail` |

---

## Histórico completo

| Arquivo | Conteúdo |
|---------|----------|
| `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` | Snapshot integral — **não editar** para registo diário |
| `.cursor/context/2026_06_05_indice-memoria-ia.md` | Índice temas + scripts |
| `.cursor/CHANGELOG-DEV.md` | Redirecionamento para este ficheiro |

**Registar intervenção:** uma entrada compacta em «Últimas alterações» + `BACKLOG-DEV.md`; bloco longo só em contexto datado ou histórico.
