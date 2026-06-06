# Índice de memória IA — GDI-ERP-Plataform

> **Fonte única de navegação** para contexto por tema. O agente lê **este índice** quando precisar de detalhe; não duplicar conteúdo em `AI-CONTEXT.md` / `CLAUDE.md`.

## Hierarquia (ordem de leitura)

| Prioridade | Ficheiro | Conteúdo |
|------------|----------|----------|
| 1 | `AI-CONTEXT.md` | Stack, regras, áreas sensíveis, resumo arquitetura |
| 2 | `CHANGELOG-DEV.md` | Estado atual + últimas ~15 alterações (compacto) |
| 3 | `BACKLOG-DEV.md` | Pendências ativas com IDs (G-PUB, G-DT, …) |
| 4 | **Este índice** | Detalhe por tema em `.cursor/context/` |
| 5 | `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` | Formato resposta, git/deploy, convenção nomes |
| 6 | `CLAUDE.md` | Entrada Claude Code + `@CHANGELOG-RECENT` |
| Arquivo | `docs/dev-history/CHANGELOG-DEV-HISTORICO-INICIAL.md` | **187 entradas** — não editar no dia a dia |

---

## Temas — onde está o detalhe

### Arquitetura e contratos (obrigatório em código novo)

| Tema | Ficheiro |
|------|----------|
| **Arquitetura centralizada** (modal GET, DT, Ajax) | `2026_06_05_arquitetura-centralizada-erp-gdi.md` |
| Contrato `GdiMvcJsonResults` | `2026_06_05_gdi-mvc-json-results-contrato.md` |
| Matriz método × erro (Movimentos) | `2026_06_05_movimentos-metodo-contrato-erro.md` |
| Inventário LibExceptions residual ERP | `2026_06_05_erp_ajax-exceptions-residual-inventory.md` |
| Inventário LibExceptions gc | `2026_06_05_gc_ajax-exceptions-residual-inventory.md` |
| Auditoria views gc / ERP | `2026_06_05_gc-views-auditoria-inventario.md`, `2026_06_05_erp-views-auditoria-inventario.md` |

### UI — tabelas, mensagens, layout

| Tema | Ficheiro |
|------|----------|
| **DataTables vs MVC** (classificar antes de `<table>`) | `2026_05_20_tabelas-datatables-vs-mvc.md` |
| Mensagens SweetAlert2 / `LibMessage*` | `2026_05_22_libmessage-confirm-arquitetura.md` |
| DataTables pt-BR, scroll host, filtro obrigatório | `2026_05_22_datatables-pt-br-global.md`, `2026_05_22_datatables-gdi-dt-scroll-host-auditoria.md`, `2026_05_22_datatables-filtro-obrigatorio-mensagem.md` |
| Layout scripts / page scripts (G-PERF-20) | `2026_05_22_layout-scripts-contrato-flags.md`, `2026_05_21_layout-scripts-inventario.json` |
| Index cadastros primeiro load | `2026_05_20_index-cadastros-primeiro-load.md` |

### Lookups e dados

| Tema | Ficheiro |
|------|----------|
| Convenção Index vs CreateEdit | `2026_05_20_lookups-convencao-index-vs-createedit.md` |
| LibDataSets → serviço (plano) | `2026_05_20_lookups-libdatasets.md` |
| Monitorização pós-publish | `2026_05_20_lookups-monitorizacao-pos-publish.md` |
| Typeahead pedidos Select2 | `2026_05_22_lookups-typeahead-ajax-pedidos.md` |
| Checklist pendências ERP | `2026_05_22_checklist-pendencias-lookups-e-erp.md` |

### Performance, publish, infra

| Tema | Ficheiro |
|------|----------|
| Checklist performance | `2026_05_22_checklist-performance-erp.md`, `2026_05_22_performance-audit-erp.md` |
| PERF-007 / DT memória | `2026_05_22_perf007-lote2-inventario.md` |
| IIS gzip / cache | `2026_05_22_perf012-iis-static-compression-cache.md` |
| Health endpoint publish | `2026_05_22_health-endpoint-publish.md` |
| Cache produtos | `2026_05_22_checklist-cache-produtos.md` |
| Plataforma (sem ASP.NET Core; bump 4.8.1 opcional) | `2026_05_20_migracao-472-481.md`, `docs/relatorio-migracao-netframework-472-481.md` |

### Domínios específicos

| Tema | Ficheiro |
|------|----------|
| NFe / e-Notas | `2026_05_26_nfe-enotas-arquitetura.md` |
| Portal vendedor removido | `2026_05_22_clientes-perfil-vendedor-removido.md` |
| PascalCase Areas | `2026_05_20_pascalcase-areas-renomeacao-lotes.md` |
| UTF-8 BOM | `2026_05_20_utf8-bom-lotes.md` |
| Filtro SQL legado | `2026_05_22_filtro-generico-legado-limpeza.md` |
| TempData legado login | `2026_05_22_tempdata-legado-filtro-login.md` |
| Atendimentos GetDados* | `2026_05_22_dt1-atendimentos-getdados-padrao.md` |

---

## Scripts de verificação (inventários)

```powershell
cd c:\Marcio\Projetos\GDI-ERP-Plataform
python Scripts/2026_06_05_gdi_smoke_architecture_inventories.py
python Scripts/2026_06_05_gdi_inventory_prefixed_file_dates.py --since 2026-06-01
python Scripts/2026_05_20_gdi_verify_csproj_gdi_helpers.py
python Scripts/2026_06_05_gdi_inventory_erp_ajax_exceptions_residual.py --areas all
python Scripts/2026_06_05_gdi_inventory_ajax_hybrid_handlers.py
python Scripts/2026_05_22_gdi_inventory_datatables_g_area.py
python Scripts/2026_05_20_gdi_inventory_utf8_bom.py --fail
python Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py --markdown
```

Smoke único: `2026_06_05_gdi_smoke_architecture_inventories.py` (5 inventários).

---

## O que NÃO guardar em memória ativa

| Conteúdo | Onde fica |
|----------|-----------|
| Lotes N-A…N-U, G1…G4, C-1/C-2 (detalhe por controller) | `CHANGELOG-DEV-HISTORICO-INICIAL.md` + contextos `2026_06_05_*` |
| Entradas changelog > 30 dias | `docs/dev-history/` |
| Fases DataTables 0–17 (lista longa) | Histórico + decisão em `CHANGELOG-DEV.md` § Decisões |
| Formato obrigatório de resposta ao utilizador | Só `.cursor/rules/2026_05_20_gdi-erp-plataform.mdc` §6 |
| Proibição git/publish | Só `.mdc` §3 e `CLAUDE.md` (ponteiro) |

---

## Duplicatas removidas nesta consolidação (2026-06-05)

| Informação | Estava duplicada em | Centralizado em |
|------------|---------------------|-----------------|
| Stack / identidade projeto | `AI-CONTEXT`, `CLAUDE.md`, `.mdc` §1 | `AI-CONTEXT.md` (`.mdc` mantém só referência rápida) |
| Arquitetura modal/DT/Ajax (lotes N-*) | `AI-CONTEXT` (~200 linhas), `CHANGELOG` (~60 entradas) | `2026_06_05_arquitetura-centralizada-erp-gdi.md` + índice |
| Fases DataTables 7–17 | `CLAUDE.md` (~15 parágrafos) | Índice + histórico; `CLAUDE.md` só ponteiro |
| Formato resposta 8 secções | `CLAUDE.md`, `.mdc` §6 | **Só** `.mdc` §6 |
| Regras git/deploy | `CLAUDE.md` §0, `.mdc` §3, `AI-CONTEXT` | **Só** `.mdc` §3; `AI-CONTEXT` uma linha |
| Ordem leitura docs | `AI-CONTEXT`, `.mdc` §2, `CLAUDE.md` | `AI-CONTEXT` + índice (`.mdc` §2 alinhado) |
| Scripts úteis | `AI-CONTEXT`, `BACKLOG`, `CLAUDE` | Índice § Scripts + `BACKLOG` (lista curta) |
| Pendências | `CHANGELOG` § Pendências, `BACKLOG` | **Só** `BACKLOG-DEV.md` |
| Alertas técnicos | `CHANGELOG`, `CLAUDE` armadilhas | `CHANGELOG-DEV.md` § Alertas + `CLAUDE.md` (3 bullets) |
| PascalCase inventário | `pascalcase-*.md` sem prefixo + `2026_05_20_*` | `2026_05_20_pascalcase-areas-renomeacao-lotes.md` (órfão removido) |

---

## Meta — reutilizar noutros projetos

| Ficheiro | Função |
|----------|--------|
| `2026_06_05_prompt-enxugar-memoria-projeto-generico.md` | Prompt copiável para auditoria/enxugamento de memória (**qualquer** stack) |

---

## Convenção de nomes (ficheiros novos)

Prefixo `AAAA_MM_DD_` = data de **criação** (*Today's date* da sessão). Inventário: `Scripts/2026_06_05_gdi_inventory_prefixed_file_dates.py`. Detalhe: `.mdc` §4.5.
