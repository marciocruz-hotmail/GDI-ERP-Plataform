using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (contratos aviação).</summary>
    public partial class ContratosAviacaoController
    {
        private ILookupQueryService ContratosLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — contratos (Fase 4 P3)

        private void PreencherLookupsContratoCreateEdit(int idCliente)
        {
            ViewBag.comboClientes = LookupSearchQueries.ComboFiltroClienteFornecedorInforme();
            if (idCliente > 0)
            {
                var item = LookupSearchQueries.GetClienteFornecedorItem(db, idCliente, comDoc: false);
                if (item != null)
                    ViewBag.comboClientes.Add(new SelectListItem { Value = item.id, Text = item.text, Selected = true });
            }
            ViewBag.comboContratosTipos = ContratosLookups.GetComboGContratosTipos(db);
        }

        #endregion
    }
}
