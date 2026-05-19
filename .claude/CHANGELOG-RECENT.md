<!-- GERADO AUTOMATICAMENTE por .claude/scripts/sync_changelog_recent.py -->
<!-- Fonte: .cursor/CHANGELOG-DEV.md | Últimas 5 entradas -->

# CHANGELOG-DEV — Entradas Recentes

> Arquivo gerado automaticamente. Para o histórico completo, consulte `.cursor/CHANGELOG-DEV.md`.

---

## HISTÓRICO DE INTERVENÇÕES (últimas 5)

### [2026-05-19] — Lote 1: remoção hiddens legados yesFilterOperador/Text/AdvancedText
**Tipo:** Refatoração
**Arquivos tocados:**
- 40 views `Areas/**` (g, gc, qa, crm) — removidos `<input hidden>` e chaves ajax DataTables
- `Scripts/gdi_remove_legacy_filter_hiddens_lote1.py` (script reutilizável)

**Problema / Demanda:** Campos do modal genérico `a/Filtros` (já removido) permaneciam nas views sem JS que os preenchesse.

**O que foi feito:** Remoção mecânica de `yesFilterOperador`, `yesFilterText` e `yesFilterAdvancedText` no markup e no `data` do DataTables. **Mantido** `yesFilterField` (sentinela `""` / `"*"` para Pesquisar/Limpar e `LibDB.getFilterByUser`).

**O que foi evitado:** Alterar `jQueryDataTableParamModel`, `LibDB.getFilterByUser` e controllers (fase 2).

**Atenção para próximas intervenções:** Propriedades no model e ramos servidor de filtro genérico SQL permanecem; limpeza opcional na fase 2.

---

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

---

### [2026-05-19] — jsAtualizarIndicadorFiltro*: wrappers para GdiAtualizarIndicadorFiltro (start.js)
**Tipo:** Refatoração
**Arquivos tocados:**
- 17 índices `Areas/g` e `Areas/gc` (Cidades, Clientes, Produtos, Cfop*, Usuarios, Vendedores, etc.)
- `Scripts/gdi_refactor_indicador_filtro.py`

**O que foi feito:** Corpos duplicados das funções `jsAtualizarIndicadorFiltro{Modulo}` passam a delegar em `GdiAtualizarIndicadorFiltro` (mantidos nomes locais nos `xhr.dt`).

---

---

### [2026-05-19] — Migração btnFiltro lote 2: removido de start.js + 4 views finais
**Tipo:** Refatoração
**Arquivos tocados:**
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` — `GdiAtualizarIndicadorFiltro`; removido `btnFiltro`
- `Areas/g/Views/ProdutosTipos/Index.cshtml` — Filtro modal + Limpar; `GdiAtualizarIndicadorFiltro` no `xhr.dt`
- `Areas/g/Views/Nfe/Index.cshtml`, `Areas/gc/Views/ComexProdutos/Index.cshtml`, `ProdutosPre.cshtml` — `xhr.dt` sem `btnFiltro`

**Decisões:** ProdutosTipos deixa de alternar “Filtro/Remover” via `innerHTML`; botões separados. **Publish:** incrementar `VersionERP` (alteração em `start.js`).

---

---

### [2026-05-19] — Migração btnFiltro lote 1 (views sem #btnFiltroDefault)
**Tipo:** Refatoração
**Arquivos tocados:**
- ~21 views em `Areas/g`, `Areas/gc`, `Areas/qa` (DataTables `xhr.dt`)
- `Areas/gc/Views/FinanceiroLancamentos/Index.cshtml` (ordem notify → `jsUpdateDataView`)
- `LibUI_AdminLTE-4.0.0/plugins/startprime/js/start.js` (comentário `@deprecated` em `btnFiltro`)
- `Scripts/gdi_remove_btnfiltro_lote1.py` (verificação)

**O que foi feito:** Removido `btnFiltro(json.yesFilterOnOff)` do `xhr.dt`; padrão `if (GdiDtNotifyJsonErrorMessage(json)) { return; }`. Spinner via `gdiDataTablesProcessandoAutoHide`. **Pendente lote 2:** `g/Nfe`, `g/ProdutosTipos`, `gc/ComexProdutos/Index`, `gc/ComexProdutos/ProdutosPre`.

---

---
