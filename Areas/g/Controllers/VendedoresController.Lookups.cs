using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    public partial class VendedoresController
    {
        private ILookupQueryService VendedoresLookups => LookupQueryServiceAccessor.Current;

        public void PreencherLookupsCreateEdit()
        {
            ViewBag.comboRevenda = VendedoresLookups.GetComboGRevendasVendedorForm(db);
        }
    }
}
