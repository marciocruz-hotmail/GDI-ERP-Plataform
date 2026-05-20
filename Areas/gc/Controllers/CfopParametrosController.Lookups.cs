using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class CfopParametrosController
    {
        private ILookupQueryService CfopParametrosLookups => LookupQueryServiceAccessor.Current;

        private void PreencherLookupsCfop()
        {
            ViewBag.comboCFOP = CfopParametrosLookups.GetComboGcCfop(db);
        }
    }
}
