using System.Collections.Generic;
using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Fase 4 LibDataSets P3 — lookups centralizados (atendimentos).</summary>
    public partial class AtendimentosController
    {
        private ILookupQueryService AtendimentosLookups => _lookupQueryService;

        #region PreencherLookups — atendimentos (Fase 4 P3)

        private void PreencherLookupsIndexAtendimentos()
        {
            var lk = AtendimentosLookups;
            ViewBag.ComboUsuariosAtendimentoSolicitante = lk.GetComboGUsuariosAtendimentoSolicitante(db);
            ViewBag.ComboUsuariosAtendimentoResponsavel = lk.GetComboGUsuariosAtendimentoResponsavel(db);
            ViewBag.ComboGDepartamentos = lk.GetComboGDepartamentos(db);
            ViewBag.ComboGAtendimentosCategorias = lk.GetComboGAtendimentosCategorias(db);
            ViewBag.ComboGAtendimentosStatus = lk.GetComboGAtendimentosStatus(db);
        }

        private static List<SelectListItem> ComboCategoriaPlaceholder()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ Selecione a Categoria ]" }
            };
        }

        private void PreencherLookupsAtendimentoFormulario()
        {
            var lk = AtendimentosLookups;
            ViewBag.ComboGDepartamentos = lk.GetComboGDepartamentos(db);
            ViewBag.ComboGAtendimentosCategorias = ComboCategoriaPlaceholder();
            ViewBag.ComboVendedores = lk.GetComboGVendedores(db);
            ViewBag.ComboClientes = LookupSearchQueries.ComboPlaceholderAtendimentoCliente();
            ViewBag.ComboProdutosServicos = lk.GetComboGcProdutosServicosTodos(db);
        }

        private void PreencherLookupsAtendimentoEdit(int idCliente)
        {
            var lk = AtendimentosLookups;
            ViewBag.ComboGDepartamentos = lk.GetComboGDepartamentos(db);
            ViewBag.ComboGAtendimentosStatus = lk.GetComboGAtendimentosStatus(db);
            ViewBag.ComboUsuariosAtendimentoResponsavel = lk.GetComboGUsuariosAtendimentoResponsavel(db);
            ViewBag.ComboGAtendimentosCategorias = ComboCategoriaPlaceholder();
            ViewBag.ComboVendedores = lk.GetComboGVendedores(db);
            ViewBag.ComboClientes = LookupSearchQueries.BuildComboClienteAtendimento(db, idCliente);
            ViewBag.ComboProdutosServicos = lk.GetComboGcProdutosServicosTodos(db);
        }

        #endregion
    }
}
