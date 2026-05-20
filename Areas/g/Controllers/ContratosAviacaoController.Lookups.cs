using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (contratos aviação).</summary>
    public partial class ContratosAviacaoController
    {
        private ILookupQueryService ContratosLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — contratos (Fase 4 P3)

        private void PreencherLookupsContratoCreateEdit()
        {
            ViewBag.comboClientes = ContratosLookups.GetComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]" });
            ViewBag.comboContratosTipos = ContratosLookups.GetComboGContratosTipos(db);
        }

        #endregion
    }
}
