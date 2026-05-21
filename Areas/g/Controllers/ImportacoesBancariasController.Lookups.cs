using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    public partial class ImportacoesBancariasController
    {
        private ILookupQueryService ImportacoesBancariasLookups => LookupQueryServiceAccessor.Current;

        public void PreencherLookupsImportacao()
        {
            ViewBag.comboContaCaixa = ImportacoesBancariasLookups.GetComboGContasCaixasBoletoEmissao(db);
        }
    }
}
