using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4 LibDataSets P2 — lookups centralizados (financeiro).</summary>
    public partial class FinanceiroLancamentosController
    {
        private ILookupQueryService FinanceiroLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — financeiro (Fase 4 P2)

        /// <summary>Index — filtros da listagem de lançamentos.</summary>
        private void PreencherLookupsIndexFinanceiro()
        {
            var lk = FinanceiroLookups;
            ViewBag.comboContasCaixa = lk.GetComboGContasCaixas(db);
            ViewBag.comboContasCaixaGerencial = lk.GetComboGContasCaixasGerencial(db);
            ViewBag.comboFinanceiroFiltroStatus = lk.GetComboFiltroFinanceiroStatus(db);
            ViewBag.comboClientes = lk.GetComboGClientesFornecedoresComDoc(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]" });
        }

        /// <summary>ModalCreateEditLancamento — combos comuns (novo/edição).</summary>
        private void PreencherLookupsModalLancamento()
        {
            var lk = FinanceiroLookups;
            ViewBag.comboClientes = lk.GetComboGClientesFornecedoresComDoc(db);
            ViewBag.comboContasCaixa = lk.GetComboGContasCaixas(db);
            ViewBag.comboContasCaixaGerencial = lk.GetComboGContasCaixasGerencial(db);
            ViewBag.comboPagRecTipos = lk.GetComboPagRecTiposFaturaveis(db);
            ViewBag.comboFinanceiroStatus = lk.GetComboGcFinanceiroStatus(db);
            ViewBag.comboDebitoCredito = lk.GetComboViewDebitoCredito(db);
            ViewBag.comboRowColors = lk.GetComboRowColors(db);
            ViewBag.comboGClassificacaoFinanceira = lk.GetComboGClassificacaoFinanceira(db);
        }

        /// <summary>ModalGerarFinanceiroMovimentos.</summary>
        private void PreencherLookupsGerarFinanceiroMovimentos()
        {
            ViewBag.comboPagRecTipos = FinanceiroLookups.GetComboPagRecTiposFaturaveis(db);
        }

        #endregion
    }
}
