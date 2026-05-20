using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (produtos controle).</summary>
    public partial class EstoqueControleController
    {
        private ILookupQueryService EstoqueControleLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — estoque controle (Fase 4 P3)

        private void PreencherLookupsProdutosControleCreate()
        {
            ViewBag.comboProdutosServicos = EstoqueControleLookups.GetComboGcProdutosServicosTodos(db);
            PreencherLookupsProdutosControleFamiliaStatus();
        }

        private void PreencherLookupsProdutosControleImportados()
        {
            ViewBag.comboProdutosServicos = EstoqueControleLookups.GetComboGcProdutosServicosImportados(db);
            PreencherLookupsProdutosControleFamiliaStatus();
        }

        private void PreencherLookupsProdutosControleFamiliaStatus()
        {
            ViewBag.comboProdutosFamilia = EstoqueControleLookups.GetComboGcProdutosFamilia(db);
            ViewBag.comboProdutosStatus = EstoqueControleLookups.GetComboGcProdutosStatus(db);
        }

        #endregion
    }
}
