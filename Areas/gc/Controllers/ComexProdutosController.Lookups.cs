using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (COMEX produtos).</summary>
    public partial class ComexProdutosController
    {
        private ILookupQueryService ComexProdutosLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — COMEX produtos (Fase 4 P3)

        private void PreencherLookupsProdutosServicosTodos()
        {
            ViewBag.comboProdutosServicos = ComexProdutosLookups.GetComboGcProdutosServicosTodos(db);
        }

        private void PreencherLookupsComexProdutosComId()
        {
            ViewBag.comboComexProdutos = ComexProdutosLookups.GetComboGcComexProdutosComId(db);
        }

        #endregion
    }
}
