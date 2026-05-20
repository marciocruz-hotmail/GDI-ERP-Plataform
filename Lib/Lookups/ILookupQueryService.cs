using System.Collections.Generic;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Contratos read-only dos lookups de maior uso (Fase 2 LibDataSets).</summary>
    public interface ILookupQueryService
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
    }
}
