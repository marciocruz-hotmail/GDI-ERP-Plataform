# CACHE-PROD — Remoção cache global de produtos

**Data:** 2026-05-20  
**Objetivo:** Plano incremental para eliminar combos/datasets com **todos** os `g_produtos` ativos (MemoryCache + HTML completo + dataset em RAM no servidor).  
**Referências:** `.cursor/context/2026_05_20_lookups-convencao-index-vs-createedit.md`, `.cursor/context/2026_05_22_lookups-typeahead-ajax-pedidos.md`, padrão CACHE-2 (clientes).

**Módulo `MovimentosCompras`:** **removido** do repositório em 2026-05-20 (controller, views, `GetComboGcTiposMovimentosCompras`). Import Excel SC permanece em `Movimentos/ModalImportarExcelSC`.

**Fase 0 (PROD-000):** **concluída** em 2026-05-20 — resultado detalhado: `.cursor/context/2026_05_20_prod000-baseline-resultado.json` | script: `Scripts/2026_05_22_gdi_prod000_baseline.py`.

---

## Mecanismos no serviço (`ILookupQueryService`)

| Método | MemoryCache | Problema típico |
|--------|-------------|-----------------|
| `GetComboGcProdutosServicosTodos` | Sim | `<select>` com todos ativos |
| `GetComboGcProdutosServicosImportados` | Sim | Subconjunto importados — ainda lista grande |
| `GetDatasetGcProdutosServicos` | Sim | Lista completa (preço/saldos/FOB) em RAM |
| `GetComboGcProdutosPosicaoEstoqueIndex` | **Não** | **Todos** os produtos no HTML **a cada** abertura do Index |

**Já com typeahead (referência):** `Movimentos/GetProdutosLookup` — modal item pedido, `ModalConsultaPedidos` (CACHE-2d).

---

## Fase 0 — PROD-000 (baseline P0) — CONCLUÍDA

### PROD-000.3 — Inventário de código (lista fechada)

| Método | P0 (migrar primeiro) | P1 | Excluído |
|--------|----------------------|-----|----------|
| `GetComboGcProdutosPosicaoEstoqueIndex` | `EstoqueController.Lookups` → `Estoque/Index.cshtml` | — | — |
| `GetDatasetGcProdutosServicos` | `MovimentosController.cs` (`AjaxDadosProduto`, `AjaxGetPrecoVendaProduto`); `EstoqueInventarioController` (`GetDadosInventarioItem` via `GetDatasetProdutosServicosLookup`) | — | — |
| `GetComboGcProdutosServicosImportados` | `EstoqueInventarioController.Lookups` → `FormInventarioItens` (filtro) | `EstoqueController` recebimento; `EstoqueControle` | — |
| `GetComboGcProdutosServicosTodos` | — | `EstoqueInventario` modal item; Atendimentos; COMEX; Lotes; NF entrada; Produtos desativar; Estoque controle | — |

**Confirmação typeahead (obrigatória):** OK

- `ModalPedidoInsertEditItem.cshtml` → `GetProdutosLookup`; `PreencherLookupsPedidoItemModal` = placeholder + 1 item em edição.
- `ModalConsultaPedidos.cshtml` → `GetProdutosLookup`; `ComboFiltroProdutoConsultaPedidos`.
- **Zero** `GetComboGcProdutosServicosTodos` nessas views.

**Correção de escopo inventário:** `FormInventarioItens` usa **`GetComboGcProdutosServicosImportados`** (cache), não `Todos`. O gargalo da grid é **`GetDatasetGcProdutosServicos`** (13 464 linhas em homologação) a cada `GetDadosInventarioItem`.

---

### PROD-000.2 — SQL (homologação `erp_gdi_homologacao`, 2026-05-20)

Equivalente ao Profiler nos 3 cenários P0:

| Cenário | Query EF / efeito | Rowcount medido |
|---------|-------------------|-----------------|
| 1 — Estoque/Index | `g_produtos` ativos, `OrderBy(nome)`, projeta `id_produto` + `nome` | **13 464** |
| 2 — Inventário filtro | `g_produtos` ativos **e** `importado = 1` (combo cache) | **8 080** |
| 3 — Dataset | `g_produtos` ativos, ~14 colunas → `List<CstDatasetProdutosServicos>` + MemoryCache `GcProdutosServicosDataset` | **13 464** |

Média `LEN(nome)` ativos: **82** caracteres.

**Ações Ajax Movimentos (cenário 3):** `AjaxDadosProduto`, `AjaxGetPrecoVendaProduto` — carregam dataset completo por request (cache hit após 1.ª chamada no app pool).

---

### PROD-000.1 — DevTools / HTML (estimativa + validação manual)

Medição automática via script (HTML do `<select>`); **validar no browser** com Network (Finish / transferred) em ambiente real.

| Fluxo | Options HTML estimadas | HTML select ~KB | Ajax lookup |
|-------|------------------------|-----------------|-------------|
| Estoque/Index (abrir) | 13 466 (+2 fixos) | **~2 130** | N/A |
| Inventário/FormInventarioItens (filtro) | 8 081 | **~1 278** | N/A |
| Inventário/GetDadosInventarioItem (1 pesquisa) | — | JSON página (até **1000** linhas/`pageLength`) + dataset 13 464 em RAM | POST `GetDadosInventarioItem` |
| Pedido/modal item (controle) | **1** placeholder | mínimo | `GetProdutosLookup` |
| Consulta pedidos (controle) | **1** placeholder | mínimo | `GetProdutosLookup` |

**Nota:** homologação ≠ produção; repetir contagens SQL em produção antes do publish se volumes divergirem.

---

### Registo da baseline

| Data | Medidor | Estoque Index | Inventário (HTML+Ajax) | Movimentos dataset | Controle pedido | Controle consulta |
|------|---------|---------------|------------------------|--------------------|-----------------|-------------------|
| 2026-05-20 | Script SQL + estimativa HTML (`gdi_prod000_baseline.py`) | 13 466 opt / ~2130 KB HTML | Filtro 8081 opt / ~1278 KB; grid + dataset 13 464 | 13 464 rows / cache `GcProdutosServicosDataset` | 1 opt + typeahead | 1 opt + typeahead |

---

## Ordem de implementação (próximo passo: PROD-005b)

| Fase | ID | Escopo | Notas |
|------|-----|--------|-------|
| ~~1~~ | ~~**PROD-002a**~~ | ~~`gc/Estoque/Index` → typeahead~~ | **Concluído** 2026-05-20 — `ComboFiltroProdutoPosicaoEstoqueIndex` + `Estoque/GetProdutosLookup` |
| 2 | **PROD-005b + PROD-002c** | Inventário: filtro typeahead + `GetDadosInventarioItem` sem dataset inteiro | `pageLength` 1000 — avaliar com PERF-007 |
| 3 | **PROD-005a** | `MovimentosController` — `AjaxDadosProduto` / `AjaxGetPrecoVendaProduto` | Query por `id_produto` |
| 4 | **PROD-003** | `GetComboGcProdutosServicosImportados` (recebimento) | P1 |
| 5 | **PROD-004** | Demais `GetComboGcProdutosServicosTodos` | Por PR pequeno |
| 6 | **PROD-006** | Deprecar métodos + `LookupCacheKeys` | Zero consumidores |
| 7 | **PROD-007** | `GetComboGcProdutosFamilia` / `Status` | Opcional |


---

## Ligação ao backlog

- **BACKLOG-DEV.md:** `CACHE-PROD` — PROD-000 fechado; iniciar PROD-002a.
- **CHANGELOG-DEV.md:** entrada 2026-05-20 PROD-000.
