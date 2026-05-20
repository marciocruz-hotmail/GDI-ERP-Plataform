using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (GED).</summary>
    public partial class GedController
    {
        private ILookupQueryService GedLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — GED (Fase 4 P3)

        private void PreencherLookupsGedTiposFiltro()
        {
            ViewBag.ComboGedTiposFiltro = GedLookups.GetComboGedArquivosTipos(db, 0, 0);
        }

        private void PreencherLookupsGedTipos(int idTipo, int idTipoPai)
        {
            ViewBag.ComboGedTipos = GedLookups.GetComboGedArquivosTipos(db, idTipo, idTipoPai);
        }

        #endregion
    }
}
