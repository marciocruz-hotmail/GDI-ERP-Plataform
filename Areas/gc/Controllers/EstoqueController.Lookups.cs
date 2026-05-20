using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (estoque).</summary>
    public partial class EstoqueController
    {
        private ILookupQueryService EstoqueLookups => _lookupQueryService;

        #region PreencherLookups — estoque (Fase 4 P3)

        /// <summary>Index posição de estoque — combo com [ TODOS OS PRODUTOS ] / [ PRODUTOS COM SALDO ] (Opção A, sem cache por largura de ecrã).</summary>
        private void PreencherLookupsEstoque()
        {
            ViewBag.comboProdutosServicos = EstoqueLookups.GetComboGcProdutosPosicaoEstoqueIndex(db);
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
