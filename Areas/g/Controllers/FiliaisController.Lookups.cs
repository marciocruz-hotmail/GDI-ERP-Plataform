using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.g.Controllers
{
    public partial class FiliaisController
    {
        private ILookupQueryService FiliaisLookups => LookupQueryServiceAccessor.Current;

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actioncreate,g_Filiais_Actionupdate")]
        public void PreencherLookupsCreateEdit()
        {
            ViewBag.comboColigadas = FiliaisLookups.GetComboGColigadas(db);
        }
    }
}
