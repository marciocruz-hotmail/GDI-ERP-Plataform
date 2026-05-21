using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.g.Controllers
{
    public partial class ContasCaixasController
    {
        private ILookupQueryService ContasCaixasLookups => LookupQueryServiceAccessor.Current;

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_Actioncreate,g_ContasCaixas_Actionupdate")]
        public void PreencherLookupsCreateEdit()
        {
            ViewBag.comboCidade = ContasCaixasLookups.GetComboGCidadesAtivas(db);
            ViewBag.comboUF = ContasCaixasLookups.GetComboGUf(db);
        }
    }
}
