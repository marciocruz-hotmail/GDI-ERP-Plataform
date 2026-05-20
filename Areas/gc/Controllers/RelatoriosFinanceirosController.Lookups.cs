using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (relatórios financeiros).</summary>
    public partial class RelatoriosFinanceirosController
    {
        private ILookupQueryService RelatoriosFinanceirosLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — relatórios financeiros (Fase 4 P3)

        private void PreencherLookupsModalLancamentosFinanceiros()
        {
            var lk = RelatoriosFinanceirosLookups;
            ViewBag.comboClientes = lk.GetComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]" });
            ViewBag.comboContasCaixas = lk.GetComboGContasCaixas(db);
            ViewBag.comboContasCaixas.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS AS CONTAS ]" });
            ViewBag.comboTipoPagRec = lk.GetComboPagRecTiposFaturaveis(db);
            ViewBag.comboGcFinanceiroStatus = lk.GetComboGcFinanceiroStatus(db);
            ViewBag.comboGcFinanceiroStatus.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS ]" });
        }

        #endregion
    }
}
