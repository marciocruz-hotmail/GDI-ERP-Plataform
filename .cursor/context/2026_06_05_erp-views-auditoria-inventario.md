# ERP completo — auditoria views Ajax/DataTables (2026-05-25)

Read-only. Critérios: lotes D–I (`AI-CONTEXT.md`), inventário gc já fechado (lotes I–K).  
Script: `Scripts/2026_06_05_gdi_inventory_erp_views_audit.py` (259 `.cshtml` em `Areas/**/Views` + `Views/**`).

---

## Resumo executivo

| Métrica | Valor | Notas |
|---------|-------|-------|
| Total views | **259** | Excl. `obj`, layouts partilhados em `Views/Shared` contam |
| `error: function (result)` | **0** (pós lote L) | era 43; `g`+`qa`+`a` migrados 2026-05-25 |
| `.DataTable({` sem `xhr.dt` | **0** (pós lote L) | era 2 em `g` |
| `.DataTable({` sem `error.dt` | **0** | — |
| Padrão unified `result.msg` | **93 ficheiros** | Quase só `gc/Movimentos` |
| `GdiAjaxNotifyInconsistencias` | **147 ficheiros** | — |
| `alert()` nativo | **0** | — |
| `error: function ()` vazio | **0** (pós micro-lote) | era 4 (qa indicadores + `g/Financeiro/DadosConsolidados`) |
| gc `GetDados*` sem contrato Fase 8+ | **0** (pós lote N-A) | era 4 — ver `2026_06_05_gdi-mvc-json-results-contrato.md` |
| `complete` + sync DT | **0** | Corrigido em gc (lote G4) |

---

## Estado por área MVC

| Área | Views c/ gaps | `error_fn_result` | DataTables | `xhr.dt` | Unified notify | Servidor DT |
|------|---------------|-------------------|------------|----------|----------------|-------------|
| **gc** | 0 crítico | **0** | 85 | 44 | 93 | Fases 9–12 + I–K **concluídas** |
| **g** | **fechado (L)** | **0** | 48 | 24 | 41 | Cliente lote L; servidor Fases 13–16 |
| **qa** | **fechado (L)** | **0** | 4+ | 4+ | 4 | GedSGQ Ajax migrado |
| **a** | **fechado (L)** | **0** | 1 | 1 | 1 | `Parametros/Index` |
| **crm** | mínimo | 0 | 0 | 0 | 0 | Portal MVC server-side (`Pedidos/Index`) — sem DataTables Ajax |
| **Views/** (raiz) | mínimo | 0 | 0 | 0 | 0 | Login `UserIdentity`, `Home` — fora do contrato DT |

---

## 1. `Areas/g` — detalhe (prioridade P1 global)

### 1.1 Ajax legado — `error: function (result)` (38 ficheiros)

| Pasta / módulo | Ficheiros |
|----------------|-----------|
| **Nfe** | `Index` + 9 modais (`ModalGerarNfe`, `ModalCancelarNfe`, `ModalSincronizarLotesNfe`, …) |
| **Financeiro** | `Index` + 6 modais (baixar, cancelar, editar, remessa, prorrogar, transferir) |
| **Clientes** | `CreateEdit` + 7 modais (contato, destinatário, limite, Sintegra, …) |
| **Produtos** | `CreateEdit`, 2 modais, `ModalViewFichaEstoqueProduto` (só DT) |
| **Atendimentos** | `Edit`, `ModalCreateEditAtividade`, `ModalCreateNewAtendimento` |
| **Ged** | `Index`, `ModalDesativarGed` |
| **ContratosAviacao** | `Index` (2×), `CreateEdit` |
| **Vendedores** | `Index`, `CreateEdit` |
| **Cidades** | `ModalCadastrarNovaCidade` |
| **ProdutosNcm** | `ModalAtualizarTabelaIBPT` |
| **Usuarios** | `ModalUsuarioTrocarSenha` |

**Index `g` com DataTables já migrado (`xhr.dt`) mas Ajax POST legado no mesmo ficheiro:**  
`Financeiro/Index`, `Nfe/Index`, `Ged/Index`, `ContratosAviacao/Index`, `Vendedores/Index`.

**Index `g` com `xhr.dt` e sem `error_fn_result` (referência):**  
`Clientes`, `Produtos`, `Filiais`, `Usuarios`, `Atendimentos`, `Cidades`, `UF`, `PagRec*`, `ContasCaixas`, `Perfis`, `ProdutosNcm`.

**Index sem DataTables Ajax:** `CentrosCustos`, `ClassificacaoFinanceira` (outro padrão UI).

### 1.2 DataTables — inits sem `xhr.dt` (2)

- `Areas/g/Views/Produtos/ModalViewFichaEstoqueProduto.cshtml`
- `Areas/g/Views/Vendedores/CreateEdit.cshtml`

### 1.3 Servidor

Controllers `g` com `JsonDataTableException` privado (Fase 13–16): Filiais, UF, PagRec*, Perfis, ContasCaixas, Clientes (parcial), Nfe, Atendimentos, Ged, Financeiro (parcial), ContratosAviacao, etc.

Controllers com `GetDados*` e **sem** helper unificado documentado: verificar `Produtos` (action existe na view; controller em partials), `Requisicoes`, `FinanceiroFaturamentos` se aplicável.

---

## 2. `Areas/qa` (prioridade P2)

| Ficheiro | `error_fn_result` | `xhr.dt` listagem |
|----------|-------------------|-------------------|
| `GedSGQ/IndexAtasReunioes` | sim | sim |
| `GedSGQ/IndexComunicados` | sim | sim |
| `GedSGQ/IndexDocsSGQ` | sim | sim |
| `GedSGQ/IndexPops` | sim | — (sem DT na mesma view) |
| `IndicadoresQualidadePosVenda/Index` | não | sim (Flot + DataTables) |
| `IndicadoresQualidade/Index` | não | hub |

**yesFilterController:** `IndexGed` em todas as listagens SGQ/Treinamentos — **padrão legado partilhado** (igual `Areas/g/Ged`); alterar só com inventário de filtros SQL.

---

## 3. `Areas/a` (prioridade P3)

- `Parametros/Index.cshtml` — 1× `error: function (result)`; DataTables com `xhr.dt`.

---

## 4. `Areas/crm` — portal cliente

- `Pedidos/Index.cshtml` — listagem **MVC** `@for`; downloads/boleto via links e JS simples.
- **Sem** contrato DataTables/Ajax workflow; **não** aplicar scripts gc em massa.

---

## 5. `Views/` (raiz)

- `UserIdentity/Index`, `Home/*`, `Shared/*` — login e layout; sem débito do inventário DT/Ajax.

---

## 6. `yesFilterController` — suspeitos globais

| Ficheiro | Valor | Nota |
|----------|-------|------|
| `gc/ComexFinanceiro`, `gc/FinanceiroLancamentos`, `gc/Gerencial`, `gc/Parametros` Index | `GcIndexFinanceiroLancamentos` | **Legado intencional** partilhado |
| `qa/GedSGQ/*`, `qa/Treinamentos/*` | `IndexGed` | **Legado** clone de `g/Ged` |
| `g/Ged/Index` | `IndexGed` | OK |

Pós lote K gc: **0** cópias `ModalViewNotasFiscais` incorretas.

---

## 7. Plano de lotes — estado (2026-05-25)

| Lote | Estado | Resultado |
|------|--------|-----------|
| **L-A / L-B** | **Concluído** | 46 views (`g` 41, `qa` 4, `a` 1) — `2026_06_05_gdi_fix_erp_views_ajax_error_handlers.py` |
| **L-C / L-D** | **Concluído** | 2 inits `g` (`ModalViewFichaEstoqueProduto`, `Vendedores/CreateEdit`) |
| **M** | Pendente smoke manual | Financeiro, Nfe, Ged, Contratos, Vendedores Index |
| **N** | Adiado | Servidor `g` — `errorMessage` inline já compatível com `xhr.dt` |
| **Polish** | Adiado | `yesFilterController` qa (`IndexGed` legado) |

**Pós-lote L (inventário ERP):** `error_fn_result` **0** | `dt_init` sem `xhr.dt` **0** | unified notify **139** ficheiros.

**Não aplicar** scripts em: `crm`, `Views/Shared`, `Report*PDF`, `Boleto*`.

---

## 8. Referências cruzadas

| Documento | Conteúdo |
|-----------|----------|
| `AI-CONTEXT.md` | Padrões D–I + tabela scripts |
| `.cursor/context/2026_06_05_gc-views-auditoria-inventario.md` | gc fechado |
| `.cursor/context/2026_06_05_movimentos-metodo-contrato-erro.md` | Matriz Movimentos + pointer gc |
| `Scripts/2026_06_05_gdi_fix_gc_views_ajax_error_handlers.py` | Generalizar path → todo `Areas/*/Views` |
| `Scripts/2026_06_05_gdi_inventory_erp_views_audit.py` | Re-run pós-lote L |
