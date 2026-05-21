using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.g.Controllers
{
    public partial class ProdutosNcmController
    {
        private ILookupQueryService ProdutosNcmLookups => LookupQueryServiceAccessor.Current;

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosNcm_*,g_ProdutosNcm_Actionread,g_ProdutosNcm_Actionupdate")]
        public void PreencherLookupsCreateEdit()
        {
            ViewBag.comboCstIcmsEntrada = ProdutosNcmLookups.GetComboGcIcmsCstNcm(db);
            ViewBag.comboCstIpiEntrada = ProdutosNcmLookups.GetComboGcTributosCstIpiEntrada(db);
            ViewBag.comboCstIpiSaida = ProdutosNcmLookups.GetComboGcTributosCstIpiSaida(db);
            ViewBag.comboCstPisEntrada = ProdutosNcmLookups.GetComboGcTributosCstPisEntrada(db);
            ViewBag.comboCstPisSaida = ProdutosNcmLookups.GetComboGcTributosCstPisSaida(db);
            ViewBag.comboCstCofinsEntrada = ProdutosNcmLookups.GetComboGcTributosCstCofinsEntrada(db);
            ViewBag.comboCstCofinsSaida = ProdutosNcmLookups.GetComboGcTributosCstCofinsSaida(db);
        }
    }
}
