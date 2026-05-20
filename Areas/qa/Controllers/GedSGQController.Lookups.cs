using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.qa.Controllers
{
    /// <summary>Fase 5 LibDataSets — lookups GED SGQ (tipos por módulo).</summary>
    public partial class GedSGQController
    {
        private ILookupQueryService GedSGQLookups => LookupQueryServiceAccessor.Current;

        private void PreencherLookupsGedTiposFiltro(int idTipo, int idTipoPai)
        {
            ViewBag.ComboGedTiposFiltro = GedSGQLookups.GetComboGedArquivosTipos(db, idTipo, idTipoPai);
        }
    }
}
