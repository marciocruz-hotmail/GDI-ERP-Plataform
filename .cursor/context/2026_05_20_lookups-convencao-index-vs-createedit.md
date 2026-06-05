# Lookups — convenção Index vs CreateEdit (Grupo 1.7)

**Data:** 2026-05-20  
**Relacionado:** `ILookupQueryService`, `*.Lookups.cs`, checklist `2026_05_22_checklist-pendencias-lookups-e-erp.md` §1.7.

---

## 1.7.1 — Regra de ouro

| Contexto | Onde montar o combo | Cache `LookupQueryServiceCache` |
|----------|---------------------|----------------------------------|
| **Index / filtro de listagem** | Query **local** na action ou método `PreencherLookups*` no controller principal | **Não** — evita RAM global e combos dependentes de ecrã/sessão (ex.: `DisplayScreenWidth`, hierarquia pai no próprio Index). |
| **CreateEdit / modal partilhado** | `ILookupQueryService` via `*.Lookups.cs` (`PreencherLookupsCreateEdit`, `LoadCombos`, etc.) | **Sim**, quando o combo é catálogo estável e reutilizado (`GetComboG*`, `GetComboGc*`). |
| **Exceção documentada** | Combo Index que replica catálogo global sem dependência de sessão | Pode usar serviço **sem cache** (ex.: `GetComboGcProdutosPosicaoEstoqueIndex`) — ver `EstoqueController`. |

**Partial do controller:** `Areas/{g|gc}/Controllers/{Nome}Controller.Lookups.cs` — classe `partial`, accessor `LookupQueryServiceAccessor.Current`, métodos privados `PreencherLookups*`.

**Não** voltar a `LibDataSets` nem duplicar LINQ de `Get*` já existentes no serviço.

---

## 1.7.2 — Inventário híbridos (`ViewBag.combo*` sem `*.Lookups.cs`)

Regenerar:

```powershell
python Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py
python Scripts/2026_05_20_gdi_inventory_combo_hybrid_controllers.py --markdown
```

### Área `g` (10 híbridos)

| Controller | ViewBags (resumo) |
|------------|-------------------|
| `CentrosCustosController` | `comboCentroCustoPai` |
| `ClassificacaoFinanceiraController` | `comboClassificacaoFinanceiraPai` |
| `ContasCaixasController` | `comboCidade`, `comboUF` |
| `FiliaisController` | `comboColigadas` |
| `FinanceiroController` | `comboClientes`, `comboContaCaixa`, `comboContasCaixas`, `comboFinanceiroStatus` |
| `ImportacoesBancariasController` | `comboContaCaixa` |
| `NfeController` | `comboCidade`, `comboUF` |
| `ProdutosNcmController` | CST/ICMS/IPI (7 combos fiscais) |
| `UsuariosController` | `comboColigada`, `comboFilial`, `comboLogomarca`, `comboPerfil` |
| `VendedoresController` | `comboRevenda` |

**Já com `*.Lookups.cs` (referência):** `Atendimentos`, `Clientes`, `ContratosAviacao`, `Ged`, `Produtos`.

### Área `gc` (4 híbridos)

| Controller | ViewBags (resumo) |
|------------|-------------------|
| `ComexFinanceiroController` | importações/invoices CRUD |
| `ComexImportacoesController` | `comboStatusImportacao` |
| `RelatoriosCadastraisController` | `comboOpcoes` |
| `RelatoriosComerciaisController` | `ComboVendedores` |

**Já com `*.Lookups.cs`:** `Movimentos*`, `Estoque*`, `FinanceiroLancamentos`, `ComexProdutos`, `Cfop*`, `Fretes`, `RelatoriosFinanceiros`, etc.

---

## 1.7.3 — Decisão por controller (lotes 1–2 por PR)

| Controller | Decisão | Próximo PR sugerido |
|------------|---------|---------------------|
| `CentrosCustosController` | **Manter local** | Pai hierárquico no CreateEdit; Index só DataTables. |
| `ClassificacaoFinanceiraController` | **Manter local** | Idem centros de custo. |
| `ContasCaixasController` | **Lookups.cs** | Reutilizar combos cidade/UF do serviço (se existir) ou novos `Get*` mínimos. |
| `FiliaisController` | **Lookups.cs** | `PreencherLookupsCreateEdit` → `GetCombo*` coligadas. |
| `FinanceiroController` (g) | **Lookups.cs** | Ligar `GetComboGClientes*`, `GetComboGContasCaixas`, `GetComboGcFinanceiroStatus`. |
| `ImportacoesBancariasController` | **Lookups.cs** (1 combo) | `GetComboGContasCaixas` ou variante. |
| `NfeController` | **Lookups.cs** | Cidade/UF CreateEdit. |
| `ProdutosNcmController` | **Lookups.cs** | Vários `GetComboGcIcms*` / NCM já no serviço. |
| `UsuariosController` | **Manter local** (fase 1) | Combos por tenant/coligada; avaliar `Get*` paramétricos antes de cache. |
| `VendedoresController` | **Lookups.cs** | `comboRevenda` → serviço ou query local documentada. |
| `ComexFinanceiroController` | **Lookups.cs** | COMEX: importações/invoices. |
| `ComexImportacoesController` | **Manter local** (Index) | Filtro `comboStatusImportacao` só na listagem. |
| `RelatoriosCadastraisController` | **Manter local** | Opções fixas de relatório. |
| `RelatoriosComerciaisController` | **Lookups.cs** | `GetComboGVendedores`. |

Ordem sugerida dos PRs: (1) `Filiais` + `Vendedores`; (2) `ContasCaixas` + `Nfe`; (3) `ProdutosNcm`; (4) `Financeiro` (g); (5) `ComexFinanceiro`.

---

## 1.8 — Organização `LookupQueryService` (decisão 2026-05-20)

**Escolhido:** partials por **domínio** (não unificar tudo em `LookupQueryService.cs`).

| Ficheiro | Conteúdo |
|----------|----------|
| `LookupQueryService.cs` | Núcleo: clientes, produtos, transportadora, estoque, GED, datasets |
| `LookupQueryService.Comercial.cs` | Movimentos, CFOP tela pedido, COMEX importações/produtos, contatos pedido |
| `LookupQueryService.Financeiro.cs` | PagRec, status financeiro, contas caixas gerencial, classificação |
| `LookupQueryService.CadastrosG.cs` | Produtos tipos/NCM, ICMS, atendimentos, departamentos |

`LookupQueryService.Wave6a.cs` **removido** (conteúdo repartido). Assinaturas em `ILookupQueryService` **inalteradas**.

Script one-off de split (histórico): `Scripts/2026_05_20_gdi_split_lookup_wave6a.py`.
