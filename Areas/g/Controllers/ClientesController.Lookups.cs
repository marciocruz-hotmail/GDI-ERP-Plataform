using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups partilhados (clientes).</summary>
    public partial class ClientesController
    {
        private ILookupQueryService ClientesLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — clientes (Fase 4 P3)

        private void PreencherLookupsClientesContatosTipos()
        {
            ViewBag.ComboGcClientesContatosTipos = ClientesLookups.GetComboGcClientesContatosTipos(db);
        }

        #endregion
    }
}
