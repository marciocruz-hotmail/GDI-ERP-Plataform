# CHANGELOG-DEV.md

> Changelog **operacional** para Cursor / Claude Code (~200 linhas).  
> **Histórico integral (187 entradas, ~2900 linhas):** `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`  
> **Contexto fixo:** `AI-CONTEXT.md` | **Pendências:** `BACKLOG-DEV.md`

**Última atualização:** 2026-05-20 (pedido item sequência)

---

## Estado atual do projeto

O **GDI-ERP-Plataform** é um monólito ASP.NET MVC (.NET Framework **4.7.2**) para gestão COMEX, comercial, estoque, financeiro e qualidade da GDI Aviação. O portal público do cliente (**GDI-PortalCliente**) foi integrado neste repositório (área `crm`, `UserIdentity/AcessoPortal`).

A modernização em curso (2026) concentrou-se em: (1) substituição de **`LibDataSets`** por **`ILookupQueryService`** com cache e partials por domínio; (2) padronização **DataTables/Ajax** (`GdiDt*` / `GdiAjax*`, JSON `errorMessage` no servidor); (3) **Index** com filtro inline, paginação SQL e `deferLoading` onde aplicável; (4) remoção de módulos legados (Filtros genérico, PortalVendedor, FinanceiroFaturamentos/Lancamentos em `g`, Requisicoes, etc.); (5) higiene **PascalCase** (paths Git `Cst*`, views `Modal*`); (6) segurança incremental (**CSRF**, **XSS** em Atendimentos, `customErrors` Release); (7) documentação enxuta para IA (`AI-CONTEXT`, `BACKLOG`, este ficheiro + arquivo histórico).

**Build** Release|AnyCPU OK nas intervenções recentes. **VersionERP:** `2026.51.02` — incrementar após alterações em `start.js` / `start.css`. **UTF-8:** sem BOM no inventário de 2026-05-20.

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
