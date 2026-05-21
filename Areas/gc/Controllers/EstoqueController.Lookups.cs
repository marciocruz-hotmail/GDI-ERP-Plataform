using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (estoque).</summary>
    public partial class EstoqueController
    {
        private ILookupQueryService EstoqueLookups => _lookupQueryService;

        #region PreencherLookups — estoque (Fase 4 P3)

        /// <summary>Index posição de estoque — filtro produto: opções fixas + typeahead (PROD-002a).</summary>
        private void PreencherLookupsEstoque()
        {
            ViewBag.comboProdutosServicos = LookupSearchQueries.ComboFiltroProdutoPosicaoEstoqueIndex();
        }

        private void PreencherLookupsProdutosImportados()
        {
            ViewBag.comboProdutosServicos = EstoqueLookups.GetComboGcProdutosServicosImportados(db);
        }

        private void PreencherLookupsRecebimentoEstoque()
        {
            ViewBag.comboLocaisEstoque = EstoqueLookups.GetComboGcLocaisEstoqueOrders(db);
            ViewBag.comboProdutosServicos = EstoqueLookups.GetComboGcProdutosServicosImportados(db);
        }

        #endregion
    }
}
