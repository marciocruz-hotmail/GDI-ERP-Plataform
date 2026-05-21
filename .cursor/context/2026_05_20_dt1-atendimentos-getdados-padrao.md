# DT-1 — `GetDados*` em `Areas/g` com nomes fora do padrão

**Data:** 2026-05-20  
**Backlog:** G-DT-06 (concluído)

## Diagnóstico

O inventário `Scripts/2026_05_20_gdi_inventory_datatables_g_area.py` (versão antiga) só listava métodos `GetDados\w*` (PascalCase). Em **Fase 15** ficaram **4** endpoints em `AtendimentosController` com prefixo `getDados*` ou nome histórico — já com `try/catch` e `JsonDataTableException` no ficheiro, mas com **carga total em memória** (`ToList()` em tabelas inteiras) e bug de ordenação em `GetDadosGedAtendimento` (`iSortCol_0 == 2` duplicado).

## Decisão: não renomear actions

Renomear para `GetDadosAtendimentos` etc. **quebra** URLs nas views (`Areas/g/Views/Atendimentos/Index.cshtml`, `Edit.cshtml`) e bookmarks. Manter nomes legados; documentar em comentários XML no controller.

## Endpoints DT-1 (Atendimentos)

| Action | View | Alteração DT-1 |
|--------|------|------------------|
| `getDadosAtendimentos` | Index | `yesFilterOnOff = "1"` quando filtros custom; já usa `LibDataTableSqlPaging` |
| `getDadosAtividades` | Edit | `AsNoTracking`, `Count` + `Skip/Take`, dicts só da página |
| `getDadosAtendimentosLogs` | Edit | Idem; removido `g_usuarios.ToList()` / `g_departamentos.ToList()` |
| `GetDadosGedAtendimento` | Edit | Idem; correção sort col 3 = `descricao`; paginação EF |

## Fora de escopo DataTables (não entram no inventário)

- `FinanceiroController.getValoresConsolidados` — JSON consolidado, não `aaData`.
- `FinanceiroController.GetDadosGrafico` — contrato gráfico (`dataAberto`, etc.); catch inline com `errorMessage` (aceite).

## Verificação

```bash
python Scripts/2026_05_20_gdi_inventory_datatables_g_area.py
```

Exit 0 = todos os `(Get|get)Dados*` com `try`, `catch` → `JsonDataTableException` e helper no ficheiro.

## Referência de padrão

`Areas/g/Controllers/FiliaisController.cs` — `GetDados` + `JsonDataTableException`.
