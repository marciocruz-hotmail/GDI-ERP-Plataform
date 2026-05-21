# BACKLOG-DEV.md

Pendências técnicas do **GDI-ERP-Plataform**. Atualizar quando itens forem concluídos ou re-priorizados.

**Checklist detalhado:** `.cursor/context/2026_05_20_checklist-pendencias-lookups-e-erp.md`  
**Cache global produtos (CACHE-PROD):** `.cursor/context/2026_05_20_checklist-cache-produtos.md`  
**Performance (auditoria 2026-05-20):** `PERFORMANCE-AUDIT-ERP.md` + `.cursor/context/2026_05_20_checklist-performance-erp.md`  
**Estado resumido:** `CHANGELOG-DEV.md`

---

## Alta prioridade

| ID | Item | Critério de aceite | Referência |
|----|------|-------------------|------------|
| PUB-1 | Publish / IIS Release (§2.6) | `Web.Release.config` sem debug; `customErrors On` em produção; health check validado | Checklist §2.6 |
| PUB-2 | Pós-publish | Incrementar `VersionERP` se `start.js`/`start.css` alterados; smoke breve login/navbar | `ControlVersion.cs` |
| SMK-1 | Smoke NFe e-Notas (§2.2.4) | Gerar/atualizar/cancelar/sincronizar em ambiente com gateway real | `.cursor/context/2026_05_20_nfe-enotas-arquitetura.md` |
| SMK-2 | Smoke transversal pós-publish (§2.3) | Identidade/portal, financeiro g/gc, COMEX, comercial, GED | Checklist §2.3 |

---

## Média prioridade

| ID | Item | Notas |
|----|------|-------|
| ~~PROD-002a~~ | Estoque Index — typeahead produto | **Concluído** 2026-05-20 |
| PERF-1 | Plano performance | Etapas 2 + 3.1–3.2 (PERF-012–015) fechadas; **PERF-007b** grids; validar gzip/304 e TTL IsTableUpdate em homologação |
| ~~CACHE-2~~ | Remover cache global clientes | **Concluído** (2a–2e + `ClientesLookupController`) |
| SMK-3 | Smoke Index cadastros (§2.8) | Enter, Limpar, paginação com filtro — Filiais, Produtos |
| SMK-4 | Smoke Financeiro g (§2.5) | Exclusão faturamento (`AjaxFinanceiroCancelamento`), troca senha |
| FLT-1 | Filtro legado — lote qa/gc (§2.9) | `qa/GedSGQ`, `gc/EstoqueInventario` ainda com `yesFilterAdvancedText` |
| HYB-1 | Controllers híbridos `ViewBag.combo` | 14 inventariados — migrar para `*.Lookups.cs` por PR pequeno (§1.7) |
| NFE-1 | Menu legado Portal Vendedor | Desativar em BD `/g/PortalVendedor/PortalFinanceiro` se existir |

---

## Baixa prioridade

| ID | Item | Notas |
|----|------|-------|
| IDX-1 | `deferLoading` em módulos operacionais | Financeiro, Nfe, Ged, Atendimentos — fora escopo 2.8 |
| ~~DOC-1~~ | ~~Atualizar referências `.cursor/CHANGELOG-DEV.md` → raiz~~ | Concluído 2026-05-20 — regras `.mdc`, `CLAUDE.md`, checklist |
| ENC-1 | Monitorizar UTF-8 BOM | `python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail` antes de publish |

---

## Itens em análise

| ID | Item | Estado |
|----|------|--------|
| NET-1 | Migração .NET 4.7.2 → 4.8.1 (§2.12) | Trilha separada — ler `.cursor/context/2026_05_20_migracao-472-481.md`; não misturar com itens 1.x/2.x |
| DT-1 | Endpoints `GetDados*` g sem nome padrão | Inventário mostrou ~4 fora do padrão `JsonDataTableException` — avaliar caso a caso |
| CACHE-1 | Combos globais (clientes/produtos) | Fase 2+ lookups: chaves compostas ou serviço dedicado (CHANGELOG histórico) |
| CACHE-PROD | Remover cache global **produtos** | PROD-000 + **PROD-002a** OK; próximo **PROD-005b/002c** inventário; ver checklist CACHE-PROD |

---

## Concluídos recentemente

| Data | Grupo | Resumo |
|------|-------|--------|
| 2026-05-20 | 2.11 | UTF-8 BOM: 0 ficheiros; inventário + scripts |
| 2026-05-20 | 2.10 | Órfãos ProdutosTipos N/A; verify csproj 0 lacunas |
| 2026-05-20 | 2.9 | Filtro SQL legado removido em `Areas/g` + ComexProdutos |
| 2026-05-20 | 2.8 | Filiais 1º load + inventário deferLoading |
| 2026-05-20 | 2.7 | Tabelas MVC + sidebar portal + VersionERP 2026.51.02 |
| 2026-05-20 | 2.5 | Financeiro g POSTs/views órfãos removidos |
| 2026-05-20 | 2.4 | PascalCase paths Git |
| 2026-05-20 | 2.1–2.2 | DataTables g + NFe Fase 17 |
| 2026-05-20 | 1.x | LibDataSets → ILookupQueryService; typeahead; smoke lookups OK |
| 2026-05-20 | PERF/CACHE | CACHE-2b–2e + PERF-006 Financeiro GetDados; ClientesLookup central |
| 2026-05-20 | PERF/CACHE | CACHE-2a Atendimentos typeahead; PERF-005 batch SQL modal consulta |
| 2026-05-20 | PERF | Etapa 1 navbar + PERF-004/004b typeahead clientes (gc pedidos) |
| 2026-05-20 | Docs | Arquitetura AI-CONTEXT / BACKLOG / CHANGELOG compacto + histórico |

*Detalhe completo das entradas antigas: `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md`.*
