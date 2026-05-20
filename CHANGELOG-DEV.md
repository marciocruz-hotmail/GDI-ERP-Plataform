# CHANGELOG-DEV.md

> Changelog **operacional** (curto, para contexto de IA).  
> Histórico completo preservado em `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`.  
> Contexto fixo: `AI-CONTEXT.md` | Pendências: `BACKLOG-DEV.md`

**Última atualização documental:** 2026-05-20

---

## Estado atual do projeto

| Tema | Estado |
|------|--------|
| **Framework** | ASP.NET MVC — .NET Framework **4.7.2** (migração 4.8.1 em trilha separada) |
| **UI** | Bootstrap 5, AdminLTE 4, DataTables bs5, SweetAlert2, Tempus Dominus 6.9.4 |
| **Lookups** | `LibDataSets.cs` **removido**; `ILookupQueryService` + partials + cache (`Ondas 6a/6b` concluídas) |
| **Portal cliente** | Integrado neste monólito — área `crm`, entrada `UserIdentity/AcessoPortal` |
| **VersionERP** | `2026.51.02` (`ControlVersion.cs`) — incrementar após mudanças em `start.js` / `start.css` |
| **Encoding** | Projeto sem UTF-8 BOM (inventário 2026-05-20); ver `Scripts/2026_05_20_gdi_inventory_utf8_bom.py` |
| **Build** | Release\|AnyCPU OK nas últimas intervenções documentadas |

**Grupos checklist (2026-05-20):** Grupo 1 (lookups) e Grupos 2.1–2.11 em grande parte **concluídos**; pendentes operacionais em `BACKLOG-DEV.md`.

---

## Decisões técnicas ativas

1. **Fonte de verdade:** código e `.csproj` prevalecem sobre documentação desatualizada na raiz.
2. **DataTables:** servidor `GetDados*` com `try/catch`, `errorMessage`, `severity`, `stackTrace`, `yesFilterOnOff`; cliente `error.dt` + `GdiDtNotifyJsonErrorMessage` + `return`.
3. **Ajax (não-DataTables):** `GdiAjaxNotifyInconsistencias` em modais; não forçar contrato DataTables em APIs `{ ok, error }`.
4. **Index vs CreateEdit:** combos de filtro **locais** na action; forms partilhados via `*.Lookups.cs` + `ILookupQueryService`.
5. **Tabelas:** classificar DataTables vs MVC antes de alterar CSS/JS (`gdi-form-table-*` vs `scroll-body-horizontal`).
6. **Alterações cirúrgicas:** sem refactor cosmético; sem `git push` / publish remoto pelo agente.
7. **Schema SQL:** não alterar sem autorização explícita; não remover funcionalidades sem confirmação.

---

## Últimas alterações relevantes

| Data | Resumo |
|------|--------|
| 2026-05-20 | **2.11** UTF-8 BOM: inventário 0 BOM; scripts validados |
| 2026-05-20 | **2.10** Órfãos ProdutosTipos N/A; verify csproj Gdi 0 lacunas |
| 2026-05-20 | **2.9** Filtro SQL legado: ramos mortos removidos em `Areas/g` + ComexProdutos |
| 2026-05-20 | **2.8** Filiais: `deferLoading` + gate 1º load; inventário Index |
| 2026-05-20 | **2.7** UI tabelas MVC + sidebar portal + VersionERP 2026.51.02 |
| 2026-05-20 | **2.5** Financeiro g: POSTs/views órfãos removidos |
| 2026-05-20 | **2.4** PascalCase paths Git (models + views NFe) |
| 2026-05-20 | **2.1–2.2** DataTables g + NFe Fase 17 documentada |
| 2026-05-20 | **1.x** LibDataSets → ILookupQueryService; typeahead pedidos; smoke OK |
| 2026-05-20 | **Arquitetura docs:** `AI-CONTEXT`, `BACKLOG`, `CHANGELOG` na raiz; histórico em `docs/dev-history/` |
| 2026-05-20 | **Regras Cursor:** `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` alinhada aos 3 ficheiros da raiz |

*Detalhe de cada entrada: `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` (buscar por data ou «Grupo»).*

---

## Pendências abertas

Ver **`BACKLOG-DEV.md`** (prioridades). Resumo:

- Smoke manual: NFe e-Notas (2.2.4), Index cadastros (2.8), Financeiro g (2.5), transversal pós-publish (2.3).
- Publish/IIS: `Web.Release.config`, `customErrors`, health check (2.6).
- Lote futuro: filtro legado em `qa/GedSGQ`, `gc/EstoqueInventario` (2.9).
- Migração **4.8.1** — trilha separada (2.12).

---

## Histórico completo

| Arquivo | Conteúdo |
|---------|----------|
| `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` | Snapshot integral do changelog antes da compactação (~2900 linhas, desde 2025) |
| `.cursor/context/*.md` | Contexto operacional por tema (lookups, NFe, PascalCase, tabelas, etc.) |
| `CLAUDE.md` | Padrões longos, fases DataTables, armadilhas |
| `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` | Regras do agente Cursor |

**Como registrar nova intervenção:** bloco curto no **topo** da secção «Últimas alterações relevantes» (tabela ou 5–10 linhas); se precisar de detalhe, ficheiro em `.cursor/context/` e linha na tabela. Entradas extensas antigas **não** voltam a este ficheiro — apenas no histórico arquivado ou em context docs.
