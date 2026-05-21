using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;
using LookupSearchQueries = GdiPlataform.Lib.Lookups.LookupSearchQueries;

namespace GdiPlataform.Areas.g.Controllers
{
    public partial class FinanceiroController
    {
        private ILookupQueryService FinanceiroLookups => LookupQueryServiceAccessor.Current;

        public void PreencherLookupsIndex()
        {
            ViewBag.comboClientes = LookupSearchQueries.ComboFiltroClienteFinanceiroTodos();
            ViewBag.comboFinanceiroStatus = FinanceiroLookups.GetComboGFinanceiroStatusTitulos(db);
        }

        public void preencherCombosModalGerarRemessaBoletosBancarios()
        {
            ViewBag.comboContasCaixas = FinanceiroLookups.GetComboGContasCaixasBoletoEmissao(db);
        }
    }
}
