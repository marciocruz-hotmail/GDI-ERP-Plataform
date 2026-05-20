# Inventario LibDataSets (Fase 0)

**Diagnóstico completo e plano de ação (Fases 0–5):** [libdatasets-diagnostico-e-plano.md](./libdatasets-diagnostico-e-plano.md)

Gerado por `Scripts/gdi_inventory_libdatasets.py` a partir de `Lib/LibDataSets.cs`.
Stack: .NET Framework 4.7.2, EF6, cache em `CachePersister.contextoModel` (MemoryCache, chave por TokenId, sliding 15 min).

## Fase 1 — correcoes aplicadas (2026-05-20)

Metodos **parametricos** deixaram de gravar em slot global de `contextoModel` (consulta sempre por parametro; retorno via `CloneSelectList`).

| Metodo | Problema corrigido |
|--------|-------------------|
| `LoadComboGcClientesDestinatarios` | Cache global sobrescrevia ultimo `IdCliente` |
| `LoadDatasetGcClientesDestinatarios` | Lista global misturava destinatarios de clientes diferentes |
| `LoadComboGcCfopOperacoesFaturamentoPedido` | Gravava em `gc_comboGcCfopOperacoes` (compartilhado) |
| `LoadComboGcClientesContatos` | Cache global ignorava `IdCliente` |
| `LoadComboGcEstoqueEnderecoArea/Secao/Corredor/Prateleira` | So recarregava se `Count==0`, ignorava `IdLocalEstoque` |
| `LoadComboGedArquivosTipos` | Cache global ignorava `IdTipo` / `IdTipoPai` |

Helpers em `LibDataSets.cs`: `EnsureContextoModel()`, `CloneSelectList()`.

`LoadComboGcClientesContatosPedido` ja nao usava cache global (sem alteracao).

## Resumo

| Metrica | Valor |
|--------|-------|
| Metodos publicos Load* | 67 |
| Total chamadas em controllers | 227 |
| Com parametros alem de db | 10 |
| Risco cache parametrico (Fase 1) | 9 |

## Legenda

| Coluna | Significado |
|--------|-------------|
| Parametrico | Recebe IdCliente, IdLocalEstoque, IdTipo, etc. |
| Cache contextoModel | Propriedade em `Models/ContextoModel.cs` |
| IsTableUpdate | Invalidacao via `LibDB.IsTableUpdate(tabela, processo, db)` |
| Retorno | Clone JSON / direto do cache / lista local |
| Risco Fase 1 | CACHE_PARAMETRICO = slot unico com filtro por parametro; CACHE_SO_COUNT0 = so recarrega se Count==0 |

## Inventario metodo a metodo

| # | Metodo | Retorno | Parametros | Param. | Cache contextoModel | IsTableUpdate | Retorno | Chamadas | Risco Fase 1 |
|---|--------|---------|------------|--------|---------------------|---------------|---------|----------|--------------|
| 1 | `LoadComboFiltroDebitoCredito` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboFiltroDebitoCredito | - | JSON clone | 0 | - |
| 2 | `LoadComboFiltroFinanceiroStatus` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboFinanceiroFiltroStatus | - | JSON clone | 1 | - |
| 3 | `LoadComboGAtendimentosCategorias` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | g_comboAtendimentosCategorias | g_atendimentos_categorias (LoadComboGAtendimentosCategorias) | JSON clone | 1 | - |
| 4 | `LoadComboGAtendimentosStatus` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | g_comboAtendimentosStatus | g_atendimentos_status (LoadComboGAtendimentosStatus) | JSON clone | 2 | - |
| 5 | `LoadComboGClassificacaoFinanceira` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | g_comboGClassificacaoFinanceira | g_classificacao_financeira (LoadComboGClassificacaoFinanceira) | JSON clone | 1 | - |
| 6 | `LoadComboGClientesFornecedores` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGClientesFornecedores | g_clientes (LoadComboGClientesFornecedores) | JSON clone | 12 | - |
| 7 | `LoadComboGClientesFornecedoresComDoc` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGClientesFornecedoresComDoc | g_clientes (LoadComboGClientesFornecedoresComDoc) | JSON clone | 5 | - |
| 8 | `LoadComboGContasCaixas` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboContasCaixas | - | JSON clone | 5 | - |
| 9 | `LoadComboGContasCaixasGerencial` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboContasCaixasGerencial | - | JSON clone | 2 | - |
| 10 | `LoadComboGContratosTipos` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | g_comboContratosTipos | - | JSON clone | 4 | - |
| 11 | `LoadComboGDepartamentos` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | g_comboDepartamentos | g_departamentos (LoadComboGDepartamentos) | JSON clone | 3 | - |
| 12 | `LoadComboGMoedas` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboMoedas | - | JSON clone | 3 | - |
| 13 | `LoadComboGProdutoCondicao` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboProdutosCondicoes | g_produtos_condicoes (LoadComboGProdutoCondicao) | JSON clone | 6 | - |
| 14 | `LoadComboGProdutosNCM` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboProdutosNCM | g_produtos_ncm (LoadComboGProdutosNCM) | JSON clone | 1 | - |
| 15 | `LoadComboGProdutosTipos` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboProdutosTipos | g_produtos_tipos (LoadComboGProdutosTipos) | JSON clone | 1 | - |
| 16 | `LoadComboGUnidadeMedida` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | g_comboUnidadeMedida | g_unidade_medida (LoadComboGUnidadeMedida) | JSON clone | 1 | - |
| 17 | `LoadComboGUsuariosAtendimentoResponsavel` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | - | - | JSON clone | 2 | - |
| 18 | `LoadComboGUsuariosAtendimentoSolicitante` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | - | - | JSON clone | 1 | - |
| 19 | `LoadComboGVendedores` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboVendedores | g_vendedores (LoadComboGVendedores) | JSON clone | 8 | - |
| 20 | `LoadComboGcCfop` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGcCfop | gc_cfop (LoadComboGcCfop) | JSON clone | 8 | - |
| 21 | `LoadComboGcCfopFinalidade` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGcCfopFinalidade | - | JSON clone | 2 | - |
| 22 | `LoadComboGcCfopOperacoesFaturamentoPedido` | `List<SelectListItem>` | GdiPlataformEntities db, int IdCfopOperacao | Sim | gc_comboGcCfopOperacoes | - | JSON clone | 1 | CACHE_PARAMETRICO |
| 23 | `LoadComboGcCfopOperacoesTelaPedido` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGcCfop, gc_comboGcCfopOperacoesVendedor | gc_cfop_operacoes (LoadComboGcCfopOperacoesTelaPedido) | JSON clone | 2 | - |
| 24 | `LoadComboGcClientesContatos` | `List<SelectListItem>` | GdiPlataformEntities db, int IdCliente | Sim | gc_comboClientesContatos | - | JSON clone | 9 | CACHE_PARAMETRICO |
| 25 | `LoadComboGcClientesContatosPedido` | `List<SelectListItem>` | GdiPlataformEntities db, int IdCliente | Sim | - | - | JSON clone | 1 | - |
| 26 | `LoadComboGcClientesContatosTipos` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboClientesContatosTipos | - | JSON clone | 4 | - |
| 27 | `LoadComboGcClientesDestinatarios` | `List<SelectListItem>` | GdiPlataformEntities db, int IdCliente | Sim | gc_comboGcClientesDestinatarios | - | JSON clone | 1 | CACHE_PARAMETRICO |
| 28 | `LoadComboGcComexImportacoesAtivas` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGcComexImportacoesAtivas | gc_comex_importacoes (LoadComboGcComexImportacoesAtivas) | JSON clone | 0 | - |
| 29 | `LoadComboGcComexImportacoesTodas` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGcComexImportacoesTodas | gc_comex_importacoes (LoadComboGcComexImportacoesTodas) | JSON clone | 4 | - |
| 30 | `LoadComboGcComexProdutosComID` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGcComexProdutosComId | gc_comex_produtos (LoadComboGcComexProdutosComID) | JSON clone | 1 | - |
| 31 | `LoadComboGcEntregasPrazos` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboEntregasPrazos | gc_entregas_prazos (LoadComboGcEntregasPrazos) | JSON clone | 6 | - |
| 32 | `LoadComboGcEstoqueEnderecoArea` | `List<SelectListItem>` | GdiPlataformEntities db, int IdLocalEstoque | Sim | gc_comboEstoqueEnderecoArea | - | JSON clone | 4 | CACHE_PARAMETRICO, CACHE_SO_COUNT0 |
| 33 | `LoadComboGcEstoqueEnderecoCorredor` | `List<SelectListItem>` | GdiPlataformEntities db, int IdLocalEstoque | Sim | gc_comboEstoqueEnderecoCorredor | - | JSON clone | 4 | CACHE_PARAMETRICO, CACHE_SO_COUNT0 |
| 34 | `LoadComboGcEstoqueEnderecoPrateleira` | `List<SelectListItem>` | GdiPlataformEntities db, int IdLocalEstoque | Sim | gc_comboEstoqueEnderecoPrateleira | - | JSON clone | 4 | CACHE_PARAMETRICO, CACHE_SO_COUNT0 |
| 35 | `LoadComboGcEstoqueEnderecoSecao` | `List<SelectListItem>` | GdiPlataformEntities db, int IdLocalEstoque | Sim | gc_comboEstoqueEnderecoSecao | - | JSON clone | 4 | CACHE_PARAMETRICO, CACHE_SO_COUNT0 |
| 36 | `LoadComboGcFinanceiroStatus` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboFinanceiroStatus | - | JSON clone | 2 | - |
| 37 | `LoadComboGcFreteResponsavel` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboFreteResponsavel | gc_frete_responsavel (LoadComboGcFreteResponsavel) | JSON clone | 4 | - |
| 38 | `LoadComboGcIcmsCstSimples` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboIcmsCstSimples | gc_icms_cst (LoadComboGcIcmsCstSimples) | JSON clone | 1 | - |
| 39 | `LoadComboGcIcmsUfIsento` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboIcmsUfIsento | - | JSON clone | 1 | - |
| 40 | `LoadComboGcLocaisEstoqueOrders` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboLocaisEstoqueOrders | - | JSON clone | 11 | - |
| 41 | `LoadComboGcMovimentosPosicao` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGcMovimentosPosicao, gc_comboGcTransportadora | - | JSON clone | 5 | - |
| 42 | `LoadComboGcProdutosFamilia` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboProdutosFamilia | - | JSON clone | 4 | - |
| 43 | `LoadComboGcProdutosServicosImportados` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboProdutosServicosImportados | g_produtos (LoadComboGcProdutosServicosImportados) | JSON clone | 6 | - |
| 44 | `LoadComboGcProdutosServicosTodos` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboProdutosServicosTodos | g_produtos (LoadComboGcProdutosServicosTodos) | JSON clone | 14 | - |
| 45 | `LoadComboGcProdutosServicosTodosComId` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboProdutosServicosTodosComId | g_produtos (LoadComboGcProdutosServicosTodosComId) | JSON clone | 0 | - |
| 46 | `LoadComboGcProdutosStatus` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboProdutosStatus | - | JSON clone | 4 | - |
| 47 | `LoadComboGcStatusMovimentos` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboStatusMovimentos | - | JSON clone | 2 | - |
| 48 | `LoadComboGcTiposMovimentosCompras` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboTiposMovimentosCompras | - | JSON clone | 1 | - |
| 49 | `LoadComboGcTiposMovimentosCreateEdit` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboTiposMovimentosCreateEdit | - | JSON clone | 3 | - |
| 50 | `LoadComboGcTiposMovimentosVendas` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboTiposMovimentosVendas | - | JSON clone | 1 | - |
| 51 | `LoadComboGcTransportadora` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboGcTransportadora | g_clientes (LoadComboGcTransportadora) | JSON clone | 12 | - |
| 52 | `LoadComboGedArquivosTipos` | `List<SelectListItem>` | GdiPlataformEntities db, int IdTipo, int IdTipoPai | Sim | g_comboGedArquivosTipos | - | JSON clone | 7 | CACHE_PARAMETRICO |
| 53 | `LoadComboPagRecCondicoesFaturaveis` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboPagRecCondicoesFaturaveis | - | JSON clone | 1 | - |
| 54 | `LoadComboPagRecCondicoesTodas` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboPagRecCondicoesTodas | - | JSON clone | 3 | - |
| 55 | `LoadComboPagRecTiposFaturaveis` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboPagRecTiposFaturaveis | - | JSON clone | 3 | - |
| 56 | `LoadComboPagRecTiposTodos` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboPagRecTiposTodos | - | JSON clone | 0 | - |
| 57 | `LoadComboRowColors` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | a_comboRowsColors | - | JSON clone | 1 | - |
| 58 | `LoadComboSomenteGClientes` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboSomenteGClientes | g_clientes (LoadComboSomenteGClientes) | JSON clone | 7 | - |
| 59 | `LoadComboSomenteGClientesComDoc` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboSomenteGClientesComDoc | g_clientes (LoadComboSomenteGClientesComDoc) | JSON clone | 0 | - |
| 60 | `LoadComboSomenteGFornecedores` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboSomenteGFornecedores | g_clientes (LoadComboSomenteGFornecedores) | JSON clone | 0 | - |
| 61 | `LoadComboSomenteGFornecedoresComDoc` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | gc_comboSomenteGFornecedoresComDoc | g_clientes (LoadComboSomenteGFornecedoresComDoc) | JSON clone | 0 | - |
| 62 | `LoadComboViewDebitoCredito` | `List<SelectListItem>` | GdiPlataformEntities db | Nao | g_comboDebitoCredito | - | JSON clone | 1 | - |
| 63 | `LoadDatasetGVendedores` | `List<g_vendedores>` | GdiPlataformEntities db | Nao | gc_dataSetVendedores | g_vendedores (LoadDatasetGVendedores) | Direto cache | 6 | - |
| 64 | `LoadDatasetGcCfopOperacoes` | `List<gc_cfop_operacoes>` | GdiPlataformEntities db | Nao | gc_dataSetCfopOperacoes | gc_cfop_operacoes (LoadDatasetGcCfopOperacoes) | Direto cache | 0 | - |
| 65 | `LoadDatasetGcClientesContatos` | `List<CstDatasetClientesContatos>` | GdiPlataformEntities db | Nao | gc_dataSetClientesContatos | g_clientes_contatos (LoadDatasetGcClientesContatos) | Direto cache | 3 | - |
| 66 | `LoadDatasetGcClientesDestinatarios` | `List<g_clientes_destinatarios>` | int IdCliente, GdiPlataformEntities db | Sim | gc_dataSetClientesDestinatarios | - | JSON clone | 1 | CACHE_PARAMETRICO |
| 67 | `LoadDatasetGcProdutosServicos` | `List<CstDatasetProdutosServicos>` | GdiPlataformEntities db | Nao | gc_dataSetProdutosServicos | g_produtos (LoadDatasetGcProdutosServicos) | Direto cache | 9 | - |

## Metodos com correcao Fase 1 (cache parametrico)

- **LoadComboGcCfopOperacoesFaturamentoPedido** — CACHE_PARAMETRICO — chamadas: 1
- **LoadComboGcClientesContatos** — CACHE_PARAMETRICO — chamadas: 9
- **LoadComboGcClientesDestinatarios** — CACHE_PARAMETRICO — chamadas: 1
- **LoadComboGcEstoqueEnderecoArea** — CACHE_PARAMETRICO, CACHE_SO_COUNT0 — chamadas: 4
- **LoadComboGcEstoqueEnderecoCorredor** — CACHE_PARAMETRICO, CACHE_SO_COUNT0 — chamadas: 4
- **LoadComboGcEstoqueEnderecoPrateleira** — CACHE_PARAMETRICO, CACHE_SO_COUNT0 — chamadas: 4
- **LoadComboGcEstoqueEnderecoSecao** — CACHE_PARAMETRICO, CACHE_SO_COUNT0 — chamadas: 4
- **LoadComboGedArquivosTipos** — CACHE_PARAMETRICO — chamadas: 7
- **LoadDatasetGcClientesDestinatarios** — CACHE_PARAMETRICO — chamadas: 1

## Detalhe por metodo (consumo)

### `LoadComboFiltroFinanceiroStatus`
- Chamadas: 1
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`

### `LoadComboGAtendimentosCategorias`
- Chamadas: 1
  - `Areas\g\Controllers\AtendimentosController.cs`

### `LoadComboGAtendimentosStatus`
- Chamadas: 2
  - `Areas\g\Controllers\AtendimentosController.cs`

### `LoadComboGClassificacaoFinanceira`
- Chamadas: 1
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`

### `LoadComboGClientesFornecedores`
- Chamadas: 12
  - `Areas\g\Controllers\ContratosAviacaoController.cs`
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\RelatoriosFinanceirosController.cs`

### `LoadComboGClientesFornecedoresComDoc`
- Chamadas: 5
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`

### `LoadComboGContasCaixas`
- Chamadas: 5
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`
  - `Areas\gc\Controllers\RelatoriosFinanceirosController.cs`

### `LoadComboGContasCaixasGerencial`
- Chamadas: 2
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`

### `LoadComboGContratosTipos`
- Chamadas: 4
  - `Areas\g\Controllers\ContratosAviacaoController.cs`

### `LoadComboGDepartamentos`
- Chamadas: 3
  - `Areas\g\Controllers\AtendimentosController.cs`

### `LoadComboGMoedas`
- Chamadas: 3
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGProdutoCondicao`
- Chamadas: 6
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGProdutosNCM`
- Chamadas: 1
  - `Areas\g\Controllers\ProdutosController.cs`

### `LoadComboGProdutosTipos`
- Chamadas: 1
  - `Areas\g\Controllers\ProdutosController.cs`

### `LoadComboGUnidadeMedida`
- Chamadas: 1
  - `Areas\g\Controllers\ProdutosController.cs`

### `LoadComboGUsuariosAtendimentoResponsavel`
- Chamadas: 2
  - `Areas\g\Controllers\AtendimentosController.cs`

### `LoadComboGUsuariosAtendimentoSolicitante`
- Chamadas: 1
  - `Areas\g\Controllers\AtendimentosController.cs`

### `LoadComboGVendedores`
- Chamadas: 8
  - `Areas\g\Controllers\AtendimentosController.cs`
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcCfop`
- Chamadas: 8
  - `Areas\gc\Controllers\CfopOperacoesController.cs`
  - `Areas\gc\Controllers\CfopParametrosController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcCfopFinalidade`
- Chamadas: 2
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcCfopOperacoesFaturamentoPedido`
- Chamadas: 1
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcCfopOperacoesTelaPedido`
- Chamadas: 2
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcClientesContatos`
- Chamadas: 9
  - `Areas\g\Controllers\ClientesController.cs`
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcClientesContatosPedido`
- Chamadas: 1
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcClientesContatosTipos`
- Chamadas: 4
  - `Areas\g\Controllers\ClientesController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcClientesDestinatarios`
- Chamadas: 1
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcComexImportacoesTodas`
- Chamadas: 4
  - `Areas\g\Controllers\ProdutosController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcComexProdutosComID`
- Chamadas: 1
  - `Areas\gc\Controllers\ComexProdutosController.cs`

### `LoadComboGcEntregasPrazos`
- Chamadas: 6
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcEstoqueEnderecoArea`
- Chamadas: 4
  - `Areas\g\Controllers\ProdutosController.cs`
  - `Areas\gc\Controllers\EstoqueInventarioController.cs`

### `LoadComboGcEstoqueEnderecoCorredor`
- Chamadas: 4
  - `Areas\g\Controllers\ProdutosController.cs`
  - `Areas\gc\Controllers\EstoqueInventarioController.cs`

### `LoadComboGcEstoqueEnderecoPrateleira`
- Chamadas: 4
  - `Areas\g\Controllers\ProdutosController.cs`
  - `Areas\gc\Controllers\EstoqueInventarioController.cs`

### `LoadComboGcEstoqueEnderecoSecao`
- Chamadas: 4
  - `Areas\g\Controllers\ProdutosController.cs`
  - `Areas\gc\Controllers\EstoqueInventarioController.cs`

### `LoadComboGcFinanceiroStatus`
- Chamadas: 2
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`
  - `Areas\gc\Controllers\RelatoriosFinanceirosController.cs`

### `LoadComboGcFreteResponsavel`
- Chamadas: 4
  - `Areas\gc\Controllers\MovimentosController.cs`
  - `Areas\gc\Controllers\MovimentosEntradasController.cs`

### `LoadComboGcIcmsCstSimples`
- Chamadas: 1
  - `Areas\g\Controllers\ProdutosController.cs`

### `LoadComboGcIcmsUfIsento`
- Chamadas: 1
  - `Areas\g\Controllers\ProdutosController.cs`

### `LoadComboGcLocaisEstoqueOrders`
- Chamadas: 11
  - `Areas\gc\Controllers\EstoqueController.cs`
  - `Areas\gc\Controllers\EstoqueInventarioController.cs`
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcMovimentosPosicao`
- Chamadas: 5
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcProdutosFamilia`
- Chamadas: 4
  - `Areas\gc\Controllers\EstoqueControleController.cs`

### `LoadComboGcProdutosServicosImportados`
- Chamadas: 6
  - `Areas\gc\Controllers\EstoqueControleController.cs`
  - `Areas\gc\Controllers\EstoqueController.cs`
  - `Areas\gc\Controllers\EstoqueInventarioController.cs`

### `LoadComboGcProdutosServicosTodos`
- Chamadas: 14
  - `Areas\g\Controllers\AtendimentosController.cs`
  - `Areas\g\Controllers\ProdutosController.cs`
  - `Areas\gc\Controllers\ComexProdutosController.cs`
  - `Areas\gc\Controllers\EstoqueControleController.cs`
  - `Areas\gc\Controllers\EstoqueInventarioController.cs`
  - `Areas\gc\Controllers\EstoqueLotesController.cs`
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcProdutosStatus`
- Chamadas: 4
  - `Areas\gc\Controllers\EstoqueControleController.cs`

### `LoadComboGcStatusMovimentos`
- Chamadas: 2
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcTiposMovimentosCompras`
- Chamadas: 1
  - `Areas\gc\Controllers\MovimentosComprasController.cs`

### `LoadComboGcTiposMovimentosCreateEdit`
- Chamadas: 3
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcTiposMovimentosVendas`
- Chamadas: 1
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGcTransportadora`
- Chamadas: 12
  - `Areas\gc\Controllers\FretesController.cs`
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboGedArquivosTipos`
- Chamadas: 7
  - `Areas\g\Controllers\GedController.cs`
  - `Areas\qa\Controllers\GedSGQController.cs`

### `LoadComboPagRecCondicoesFaturaveis`
- Chamadas: 1
  - `Areas\gc\Controllers\MovimentosComprasController.cs`

### `LoadComboPagRecCondicoesTodas`
- Chamadas: 3
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboPagRecTiposFaturaveis`
- Chamadas: 3
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`
  - `Areas\gc\Controllers\RelatoriosFinanceirosController.cs`

### `LoadComboRowColors`
- Chamadas: 1
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`

### `LoadComboSomenteGClientes`
- Chamadas: 7
  - `Areas\g\Controllers\AtendimentosController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadComboViewDebitoCredito`
- Chamadas: 1
  - `Areas\gc\Controllers\FinanceiroLancamentosController.cs`

### `LoadDatasetGVendedores`
- Chamadas: 6
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadDatasetGcClientesContatos`
- Chamadas: 3
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadDatasetGcClientesDestinatarios`
- Chamadas: 1
  - `Areas\gc\Controllers\MovimentosController.cs`

### `LoadDatasetGcProdutosServicos`
- Chamadas: 9
  - `Areas\gc\Controllers\EstoqueInventarioController.cs`
  - `Areas\gc\Controllers\MovimentosComprasController.cs`
  - `Areas\gc\Controllers\MovimentosController.cs`

