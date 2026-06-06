using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (COMEX produtos).</summary>
    public partial class ComexProdutosController
    {
        private ILookupQueryService ComexProdutosLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — COMEX produtos (Fase 4 P3)

        /// <summary>Modal CreateEdit — placeholder + produto vinculado (typeahead Ajax; sem GetComboGcProdutosServicosTodos).</summary>
        private void PreencherLookupsProdutoModal(int idProdutoSelecionado = 0)
        {
            ViewBag.comboProdutosServicos = LookupSearchQueries.ComboPlaceholderProduto();
            if (idProdutoSelecionado > 0)
            {
                var prod = LookupSearchQueries.GetProdutoItem(db, idProdutoSelecionado);
                if (prod != null)
                    ViewBag.comboProdutosServicos.Add(new System.Web.Mvc.SelectListItem { Value = prod.id, Text = prod.text, Selected = true });
            }
        }

        private void PreencherLookupsComexProdutosComId()
        {
            ViewBag.comboComexProdutos = ComexProdutosLookups.GetComboGcComexProdutosComId(db);
        }

        #endregion
    }
}
