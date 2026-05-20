using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class FretesController
    {
        private ILookupQueryService FretesLookups => LookupQueryServiceAccessor.Current;

        private void PreencherLookupsTransportadora()
        {
            ViewBag.comboTransportadora = FretesLookups.GetComboGcTransportadora(db);
        }
    }
}
