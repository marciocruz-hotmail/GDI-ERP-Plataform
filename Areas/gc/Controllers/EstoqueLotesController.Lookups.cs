using System.Collections.Generic;
using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class EstoqueLotesController
    {
        private ILookupQueryService EstoqueLotesLookups => LookupQueryServiceAccessor.Current;

        private void LoadCombos()
        {
            PreencherLookupsProdutosTodos();
            PreencherLookupsComexImportacoes();
        }

        private void PreencherLookupsProdutosTodos()
        {
            ViewBag.comboProdutos = EstoqueLotesLookups.GetComboGcProdutosServicosTodos(db);
        }

        private void PreencherLookupsComexImportacoes()
        {
            var combo = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ IMPORTAÇÃO ]" }
            };
            foreach (var item in EstoqueLotesLookups.GetComboGcComexImportacoesTodas(db))
            {
                int id;
                if (int.TryParse(item.Value, out id) && id > 0)
                    combo.Add(item);
            }
            ViewBag.ComboComexImportacoes = combo;
        }
    }
}
