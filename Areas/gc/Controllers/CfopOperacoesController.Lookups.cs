using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class CfopOperacoesController
    {
        private ILookupQueryService CfopOperacoesLookups => LookupQueryServiceAccessor.Current;

        private void PreencherLookupsCfop()
        {
            ViewBag.comboCFOP = CfopOperacoesLookups.GetComboGcCfop(db);
        }
    }
}
