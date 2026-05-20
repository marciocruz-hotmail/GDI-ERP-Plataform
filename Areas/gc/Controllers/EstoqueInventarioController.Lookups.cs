using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets P2 — lookups centralizados (inventário estoque).</summary>
    public partial class EstoqueInventarioController
    {
        private ILookupQueryService InventarioLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — inventário (Fase 4 P2)

        private void PreencherLookupsIndexInventario()
        {
            ViewBag.comboLocaisEstoque = InventarioLookups.GetComboGcLocaisEstoqueOrders(db);
        }

        private void PreencherLookupsModalCreateInventario()
        {
            ViewBag.comboLocaisEstoque = InventarioLookups.GetComboGcLocaisEstoqueOrders(db);
        }

        /// <summary>FormInventarioItens — filtro por produto importado + endereço.</summary>
        private void PreencherLookupsFormInventarioItens(int idLocalEstoque)
        {
            var comboProdutos = InventarioLookups.GetComboGcProdutosServicosImportados(db);
            var idxTodos = comboProdutos.FindIndex(p => p.Value == "-1");
            if (idxTodos >= 0)
            {
                comboProdutos.RemoveAt(idxTodos);
            }
            comboProdutos.Insert(0, new SelectListItem { Value = "0", Text = "[ TODOS OS ITENS ]" });
            ViewBag.comboProdutosServicos = comboProdutos;
            PreencherLookupsEstoqueEndereco(idLocalEstoque);
        }

        /// <summary>ModalCreateEditInventarioItem — todos os produtos + endereço.</summary>
        private void PreencherLookupsModalInventarioItem(int idLocalEstoque)
        {
            ViewBag.comboProdutosServicos = InventarioLookups.GetComboGcProdutosServicosTodos(db);
            PreencherLookupsEstoqueEndereco(idLocalEstoque);
        }

        private void PreencherLookupsEstoqueEndereco(int idLocalEstoque)
        {
            var lk = InventarioLookups;
            ViewBag.comboEstoqueEnderecoArea = lk.GetComboGcEstoqueEnderecoArea(db, idLocalEstoque);
            ViewBag.comboEstoqueEnderecoSecao = lk.GetComboGcEstoqueEnderecoSecao(db, idLocalEstoque);
            ViewBag.comboEstoqueEnderecoCorredor = lk.GetComboGcEstoqueEnderecoCorredor(db, idLocalEstoque);
            ViewBag.comboEstoqueEnderecoPrateleira = lk.GetComboGcEstoqueEnderecoPrateleira(db, idLocalEstoque);
        }

        /// <summary>Dataset de produtos (DataTables / lookup em memória).</summary>
        private List<CstDatasetProdutosServicos> GetDatasetProdutosServicosLookup()
        {
            return InventarioLookups.GetDatasetGcProdutosServicos(db);
        }

        #endregion
    }
}
