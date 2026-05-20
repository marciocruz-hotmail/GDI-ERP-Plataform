using System.Collections.Generic;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Contratos read-only dos lookups (MemoryCache; substitui LibDataSets removido na Onda 6b).</summary>
    public partial interface ILookupQueryService
    {
        List<SelectListItem> GetComboGcProdutosServicosTodos(GdiPlataformEntities db);
        List<SelectListItem> GetComboGClientesFornecedores(GdiPlataformEntities db);
        List<SelectListItem> GetComboGClientesFornecedoresComDoc(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcTransportadora(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcLocaisEstoqueOrders(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcClientesContatos(GdiPlataformEntities db, int idCliente);
        List<SelectListItem> GetComboGcClientesDestinatarios(GdiPlataformEntities db, int idCliente);
        List<SelectListItem> GetComboGVendedores(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcCfop(GdiPlataformEntities db);
        List<SelectListItem> GetComboSomenteGClientes(GdiPlataformEntities db);
        List<SelectListItem> GetComboGedArquivosTipos(GdiPlataformEntities db, int idTipo, int idTipoPai);
        List<SelectListItem> GetComboGcProdutosServicosImportados(GdiPlataformEntities db);
        /// <summary>Combo Index posição de estoque: [ TODOS OS PRODUTOS ], [ PRODUTOS COM SALDO ] e produtos ativos (truncamento por DisplayScreenWidth).</summary>
        List<SelectListItem> GetComboGcProdutosPosicaoEstoqueIndex(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcEntregasPrazos(GdiPlataformEntities db);
        List<SelectListItem> GetComboGProdutoCondicao(GdiPlataformEntities db);
        List<SelectListItem> GetComboGContasCaixas(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcMovimentosPosicao(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcFreteResponsavel(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcCfopOperacoesFaturamentoPedido(GdiPlataformEntities db, int idCfopOperacao);
        List<SelectListItem> GetComboGcEstoqueEnderecoArea(GdiPlataformEntities db, int idLocalEstoque);
        List<SelectListItem> GetComboGcEstoqueEnderecoSecao(GdiPlataformEntities db, int idLocalEstoque);
        List<SelectListItem> GetComboGcEstoqueEnderecoCorredor(GdiPlataformEntities db, int idLocalEstoque);
        List<SelectListItem> GetComboGcEstoqueEnderecoPrateleira(GdiPlataformEntities db, int idLocalEstoque);

        List<g_vendedores> GetDatasetGVendedores(GdiPlataformEntities db);
        List<CstDatasetProdutosServicos> GetDatasetGcProdutosServicos(GdiPlataformEntities db);
        List<g_clientes_destinatarios> GetDatasetGcClientesDestinatarios(int idCliente, GdiPlataformEntities db);

        // Onda 6a
        List<SelectListItem> GetComboGcTiposMovimentosVendas(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcTiposMovimentosCompras(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcTiposMovimentosCreateEdit(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcStatusMovimentos(GdiPlataformEntities db);
        List<SelectListItem> GetComboGMoedas(GdiPlataformEntities db);
        List<SelectListItem> GetComboPagRecCondicoesTodas(GdiPlataformEntities db);
        List<SelectListItem> GetComboPagRecCondicoesFaturaveis(GdiPlataformEntities db);
        List<SelectListItem> GetComboPagRecTiposFaturaveis(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcFinanceiroStatus(GdiPlataformEntities db);
        List<SelectListItem> GetComboFiltroFinanceiroStatus(GdiPlataformEntities db);
        List<SelectListItem> GetComboGContasCaixasGerencial(GdiPlataformEntities db);
        List<SelectListItem> GetComboViewDebitoCredito(GdiPlataformEntities db);
        List<SelectListItem> GetComboRowColors(GdiPlataformEntities db);
        List<SelectListItem> GetComboGClassificacaoFinanceira(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcCfopFinalidade(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcCfopOperacoesTelaPedido(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcComexImportacoesTodas(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcComexProdutosComId(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcClientesContatosTipos(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcClientesContatosPedido(GdiPlataformEntities db, int idCliente);
        List<CstDatasetClientesContatos> GetDatasetGcClientesContatos(GdiPlataformEntities db);
        List<SelectListItem> GetComboGProdutosTipos(GdiPlataformEntities db);
        List<SelectListItem> GetComboGProdutosNcm(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcIcmsUfIsento(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcIcmsCstSimples(GdiPlataformEntities db);
        List<SelectListItem> GetComboGUnidadeMedida(GdiPlataformEntities db);
        List<SelectListItem> GetComboGContratosTipos(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcProdutosFamilia(GdiPlataformEntities db);
        List<SelectListItem> GetComboGcProdutosStatus(GdiPlataformEntities db);
        List<SelectListItem> GetComboGUsuariosAtendimentoResponsavel(GdiPlataformEntities db);
        List<SelectListItem> GetComboGUsuariosAtendimentoSolicitante(GdiPlataformEntities db);
        List<SelectListItem> GetComboGDepartamentos(GdiPlataformEntities db);
        List<SelectListItem> GetComboGAtendimentosStatus(GdiPlataformEntities db);
        List<SelectListItem> GetComboGAtendimentosCategorias(GdiPlataformEntities db);
    }
}
