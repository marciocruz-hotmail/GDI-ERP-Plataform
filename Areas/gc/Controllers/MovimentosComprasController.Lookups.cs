using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets — lookups centralizados para compras.</summary>
    public partial class MovimentosComprasController
    {
        private ILookupQueryService ComprasLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — compras (Fase 4)

        private void PreencherLookupsIndexCompras()
        {
            ViewBag.comboClientes = ComprasLookups.GetComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]" });
            ViewBag.comboTiposMovimentos = ComprasLookups.GetComboGcTiposMovimentosCompras(db);
            ViewBag.comboStatusMovimentos = ComprasLookups.GetComboGcStatusMovimentos(db);
            ViewBag.comboMovimentosPosicao = ComprasLookups.GetComboGcMovimentosPosicao(db);
        }

        private void PreencherLookupsCompraPedidoForm()
        {
            var lk = ComprasLookups;
            ViewBag.comboClientes = lk.GetComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ SELECIONE ]" });
            ViewBag.comboTiposMovimentosCreateEdit = lk.GetComboGcTiposMovimentosCreateEdit(db);
            ViewBag.comboClientesContatos = lk.GetComboGcClientesContatos(db, 0);
            ViewBag.dataSetClientesContatos = lk.GetDatasetGcClientesContatos(db);
            var locaisEstoque = lk.GetComboGcLocaisEstoqueOrders(db);
            ViewBag.comboLocaisEstoque = locaisEstoque;
            ViewBag.comboLocaisEstoqueOrders = locaisEstoque;
            ViewBag.comboVendedores = lk.GetComboGVendedores(db);
            ViewBag.comboVendedores.Insert(0, new SelectListItem { Value = "0", Text = "[ ESTOQUE ]" });
            ViewBag.comboTransportadora = lk.GetComboGcTransportadora(db);
            ViewBag.comboMovimentosPosicao = lk.GetComboGcMovimentosPosicao(db);
            lk.GetDatasetGVendedores(db);
            ViewBag.comboMoedas = lk.GetComboGMoedas(db);
            ViewBag.comboPagRecCondicoes = lk.GetComboPagRecCondicoesFaturaveis(db);
        }

        private void PreencherLookupsCompraItemModal()
        {
            var lk = ComprasLookups;
            ViewBag.comboProdutosServicos = lk.GetComboGcProdutosServicosTodos(db);
            ViewBag.comboProdutosCondicoes = lk.GetComboGProdutoCondicao(db);
            ViewBag.comboEntregasPrazos = lk.GetComboGcEntregasPrazos(db);
            ViewBag.dataSetProdutosServicos = lk.GetDatasetGcProdutosServicos(db);
        }

        #endregion
    }
}
