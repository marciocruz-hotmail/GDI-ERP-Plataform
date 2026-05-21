# PERF-007 — Lote 2 (paginação DataTables em memória)

**Data:** 2026-05-20  
**Padrão:** `Lib/LibDataTableSqlPaging.cs` (`SqlCount` + `SqlPage` com `OFFSET/FETCH`), alinhado a `FinanceiroController.GetDados` (PERF-006).

## Lote 2 — corrigido

| Controller | Action(s) | Abordagem |
|------------|-----------|-----------|
| `ClassificacaoFinanceiraController` | `getDados` | EF `Count` + `Skip`/`Take` ou SQL paginado |
| `GedSGQController` | 4× `GetDados*` | `JsonGedSgqArquivosDataTable` + `LibDataTableSqlPaging` |
| `AtendimentosController` | `getDadosAtendimentos` | SQL paginado + dicts usuário/departamento só da página |
| `ComexImportacoesController` | `GetDadosItensImportacao`, `GetGedComex` | `query.Count()` + `Skip`/`Take` (EF) |
| `MovimentosController` | `GetDadosModalItensComValor`, `GetRelatorioConsultaPedidos`, `GetNotaFiscalPedido`, `GetDadosCartaCorrecao` | SQL paginado na grelha; robô e-Notas mantém query completa só para sync |

**Build:** Release|AnyCPU OK após correção de tipos em `getDadosAtendimentos` (`solicitacao_id_usuario` é `int`, não `int?`).

## Inventário automático

```text
python Scripts/2026_05_20_gdi_inventory_datatables_memory_paging.py
```

**Última execução (2026-05-20):**

| Status | Qtd | Significado |
|--------|-----|-------------|
| Lote 2 | 0 pendentes | Prioridade da Etapa 2.1 fechada |
| `PENDENTE` | 8 | Lote 3+ (ver tabela abaixo) |
| `ACEITE_DOC` | 1 | `GetDadosInvoicesItensEspelhoDigital` (DataRow/dedupe) |

### Pendentes (lote 3 — PERF-007b sugerido)

| Ficheiro | Action | Linha ~ |
|----------|--------|---------|
| `Areas/g/Controllers/AssistentesController.cs` | `GetDados` | 39 |
| `Areas/g/Controllers/CentrosCustosController.cs` | `getDados` | 58 |
| `Areas/g/Controllers/GedController.cs` | `GetDados` | 67 |
| `Areas/g/Controllers/NfeController.cs` | `GetDados` | 150 |
| `Areas/g/Controllers/AtendimentosController.cs` | `getDadosAtividades` | 613 |
| `Areas/g/Controllers/AtendimentosController.cs` | `getDadosAtendimentosLogs` | 869 |
| `Areas/gc/Controllers/EstoqueControleController.cs` | `GetDadosMedicoes` | 406 |
| `Areas/a/Controllers/ParametrosController.cs` | `GetDadosSistemas` | 35 |

### Aceite documentado (fora do lote 2)

| Ficheiro | Action | Motivo |
|----------|--------|--------|
| `MovimentosController.cs` | `GetDadosInvoicesItensEspelhoDigital` | `DataTable`/`DataRow`, filtro 60 dias, dedupe por `id_invoice_item` na projeção — refactor SQL dedicado na fase seguinte |

### Fora do grep `GetDados*` (manual)

| Ficheiro | Método ~L3940 | Nota |
|----------|---------------|------|
| `FinanceiroLancamentosController.cs` | GED anexo lançamento | `allRecords.ToList()` + `Skip`; incluir em PERF-007b |

## Aceite Etapa 2.1 (lote 2)

- [x] Prioridade da checklist corrigida com padrão PERF-006.
- [x] Script + esta nota listam restantes (8 + 1 aceite + 1 manual).
- [ ] Lote 3: zerar `PENDENTE` ou marcar aceite por action.
