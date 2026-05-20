# Inventario lookups (ILookupQueryService)

**Estado (Onda 6b + 1.8):** `Lib/LibDataSets.cs` removido. Contratos em `ILookupQueryService` + `LookupQueryService` (+ partials `Comercial`, `Financeiro`, `CadastrosG`).

Gerado por `Scripts/2026_05_20_gdi_inventory_libdatasets.py` a partir de `Lib/Lookups/ILookupQueryService.cs`.

## Resumo

| Metrica | Valor |
|--------|-------|
| Metodos Get* / GetDataset* | 59 |
| Total chamadas (grep `.GetX(`) | 146 |

## Metodos

| Metodo | Parametros | Parametrico | Chamadas | Consumidores |
|--------|------------|-------------|----------|--------------|
| `GetComboGcProdutosServicosTodos` | `GdiPlataformEntities db` | Nao | 10 | Areas\g\Controllers\AtendimentosController.Lookups.cs, Areas\g\Controllers\ProdutosController.Lookups.cs, Areas\gc\Controllers\ComexProdutosController.Lookups.cs, Areas\gc\Controllers\EstoqueControleController.Lookups.cs, Areas\gc\Controllers\EstoqueInventarioController.Lookups.cs (+3) |
| `GetComboGClientesFornecedores` | `GdiPlataformEntities db` | Nao | 4 | Areas\g\Controllers\ContratosAviacaoController.Lookups.cs, Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\RelatoriosFinanceirosController.Lookups.cs |
| `GetComboGClientesFornecedoresComDoc` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs |
| `GetComboGcTransportadora` | `GdiPlataformEntities db` | Nao | 8 | Areas\gc\Controllers\FretesController.Lookups.cs, Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcLocaisEstoqueOrders` | `GdiPlataformEntities db` | Nao | 7 | Areas\gc\Controllers\EstoqueController.Lookups.cs, Areas\gc\Controllers\EstoqueInventarioController.Lookups.cs, Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcClientesContatos` | `GdiPlataformEntities db, int idCliente` | Sim | 3 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcClientesDestinatarios` | `GdiPlataformEntities db, int idCliente` | Sim | 1 | Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGVendedores` | `GdiPlataformEntities db` | Nao | 6 | Areas\g\Controllers\AtendimentosController.Lookups.cs, Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcCfop` | `GdiPlataformEntities db` | Nao | 3 | Areas\gc\Controllers\CfopOperacoesController.Lookups.cs, Areas\gc\Controllers\CfopParametrosController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboSomenteGClientes` | `GdiPlataformEntities db` | Nao | 6 | Areas\g\Controllers\AtendimentosController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGedArquivosTipos` | `GdiPlataformEntities db, int idTipo, int idTipoPai` | Sim | 3 | Areas\g\Controllers\GedController.Lookups.cs, Areas\qa\Controllers\GedSGQController.Lookups.cs |
| `GetComboGcProdutosServicosImportados` | `GdiPlataformEntities db` | Nao | 4 | Areas\gc\Controllers\EstoqueControleController.Lookups.cs, Areas\gc\Controllers\EstoqueController.Lookups.cs, Areas\gc\Controllers\EstoqueInventarioController.Lookups.cs |
| `GetComboGcEntregasPrazos` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGProdutoCondicao` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGContasCaixas` | `GdiPlataformEntities db` | Nao | 3 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs, Areas\gc\Controllers\RelatoriosFinanceirosController.Lookups.cs |
| `GetComboGcMovimentosPosicao` | `GdiPlataformEntities db` | Nao | 5 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcFreteResponsavel` | `GdiPlataformEntities db` | Nao | 3 | Areas\gc\Controllers\MovimentosController.Lookups.cs, Areas\gc\Controllers\MovimentosEntradasController.Lookups.cs |
| `GetComboGcCfopOperacoesFaturamentoPedido` | `GdiPlataformEntities db, int idCfopOperacao` | Sim | 1 | Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcEstoqueEnderecoArea` | `GdiPlataformEntities db, int idLocalEstoque` | Sim | 3 | Areas\g\Controllers\ProdutosController.Lookups.cs, Areas\gc\Controllers\EstoqueInventarioController.Lookups.cs |
| `GetComboGcEstoqueEnderecoSecao` | `GdiPlataformEntities db, int idLocalEstoque` | Sim | 3 | Areas\g\Controllers\ProdutosController.Lookups.cs, Areas\gc\Controllers\EstoqueInventarioController.Lookups.cs |
| `GetComboGcEstoqueEnderecoCorredor` | `GdiPlataformEntities db, int idLocalEstoque` | Sim | 3 | Areas\g\Controllers\ProdutosController.Lookups.cs, Areas\gc\Controllers\EstoqueInventarioController.Lookups.cs |
| `GetComboGcEstoqueEnderecoPrateleira` | `GdiPlataformEntities db, int idLocalEstoque` | Sim | 3 | Areas\g\Controllers\ProdutosController.Lookups.cs, Areas\gc\Controllers\EstoqueInventarioController.Lookups.cs |
| `GetDatasetGVendedores` | `GdiPlataformEntities db` | Nao | 5 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs, Areas\gc\Controllers\MovimentosController.cs |
| `GetDatasetGcProdutosServicos` | `GdiPlataformEntities db` | Nao | 5 | Areas\gc\Controllers\EstoqueInventarioController.Lookups.cs, Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs, Areas\gc\Controllers\MovimentosController.cs |
| `GetDatasetGcClientesDestinatarios` | `int idCliente, GdiPlataformEntities db` | Sim | 1 | Areas\gc\Controllers\MovimentosController.cs |
| `GetComboGcTiposMovimentosVendas` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcTiposMovimentosCompras` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs |
| `GetComboGcTiposMovimentosCreateEdit` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcStatusMovimentos` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGMoedas` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboPagRecCondicoesTodas` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboPagRecCondicoesFaturaveis` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs |
| `GetComboPagRecTiposFaturaveis` | `GdiPlataformEntities db` | Nao | 3 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs, Areas\gc\Controllers\RelatoriosFinanceirosController.Lookups.cs |
| `GetComboGcFinanceiroStatus` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs, Areas\gc\Controllers\RelatoriosFinanceirosController.Lookups.cs |
| `GetComboFiltroFinanceiroStatus` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs |
| `GetComboGContasCaixasGerencial` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs |
| `GetComboViewDebitoCredito` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs |
| `GetComboRowColors` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs |
| `GetComboGClassificacaoFinanceira` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\FinanceiroLancamentosController.Lookups.cs |
| `GetComboGcCfopFinalidade` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcCfopOperacoesTelaPedido` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcComexImportacoesTodas` | `GdiPlataformEntities db` | Nao | 3 | Areas\g\Controllers\ProdutosController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcComexProdutosComId` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\ComexProdutosController.Lookups.cs |
| `GetComboGcClientesContatosTipos` | `GdiPlataformEntities db` | Nao | 2 | Areas\g\Controllers\ClientesController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGcClientesContatosPedido` | `GdiPlataformEntities db, int idCliente` | Sim | 1 | Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetDatasetGcClientesContatos` | `GdiPlataformEntities db` | Nao | 2 | Areas\gc\Controllers\MovimentosComprasController.Lookups.cs, Areas\gc\Controllers\MovimentosController.Lookups.cs |
| `GetComboGProdutosTipos` | `GdiPlataformEntities db` | Nao | 1 | Areas\g\Controllers\ProdutosController.Lookups.cs |
| `GetComboGProdutosNcm` | `GdiPlataformEntities db` | Nao | 1 | Areas\g\Controllers\ProdutosController.Lookups.cs |
| `GetComboGcIcmsUfIsento` | `GdiPlataformEntities db` | Nao | 1 | Areas\g\Controllers\ProdutosController.Lookups.cs |
| `GetComboGcIcmsCstSimples` | `GdiPlataformEntities db` | Nao | 1 | Areas\g\Controllers\ProdutosController.Lookups.cs |
| `GetComboGUnidadeMedida` | `GdiPlataformEntities db` | Nao | 1 | Areas\g\Controllers\ProdutosController.Lookups.cs |
| `GetComboGContratosTipos` | `GdiPlataformEntities db` | Nao | 1 | Areas\g\Controllers\ContratosAviacaoController.Lookups.cs |
| `GetComboGcProdutosFamilia` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\EstoqueControleController.Lookups.cs |
| `GetComboGcProdutosStatus` | `GdiPlataformEntities db` | Nao | 1 | Areas\gc\Controllers\EstoqueControleController.Lookups.cs |
| `GetComboGUsuariosAtendimentoResponsavel` | `GdiPlataformEntities db` | Nao | 2 | Areas\g\Controllers\AtendimentosController.Lookups.cs |
| `GetComboGUsuariosAtendimentoSolicitante` | `GdiPlataformEntities db` | Nao | 1 | Areas\g\Controllers\AtendimentosController.Lookups.cs |
| `GetComboGDepartamentos` | `GdiPlataformEntities db` | Nao | 3 | Areas\g\Controllers\AtendimentosController.Lookups.cs |
| `GetComboGAtendimentosStatus` | `GdiPlataformEntities db` | Nao | 2 | Areas\g\Controllers\AtendimentosController.Lookups.cs |
| `GetComboGAtendimentosCategorias` | `GdiPlataformEntities db` | Nao | 1 | Areas\g\Controllers\AtendimentosController.Lookups.cs |

