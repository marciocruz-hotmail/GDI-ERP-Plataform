using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class RelatoriosComerciaisController
    {
        private ILookupQueryService RelatoriosComerciaisLookups => LookupQueryServiceAccessor.Current;

        /// <summary>Combo vendedores para modais de relatório (gerencial vs vendedor logado).</summary>
        private void PreencherComboVendedoresRelatorio(CstModalRelatorio view, bool gerencial)
        {
            int fieldDefault;
            ViewBag.ComboVendedores = RelatoriosComerciaisLookups.GetComboGVendedoresRelatorioComercial(
                db, gerencial, CachePersister.userIdentity.IdVendedor, out fieldDefault);
            view.Field_Int_01 = fieldDefault;
        }
    }
}
