using System.Web.Mvc;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.g.Controllers
{
    /// <summary>Fase 5 LibDataSets — lookups centralizados (produtos).</summary>
    public partial class ProdutosController
    {
        private ILookupQueryService ProdutosLookups => LookupQueryServiceAccessor.Current;

        private void PreencherLookupsProdutoCreateEdit()
        {
            var lk = ProdutosLookups;
            ViewBag.comboProdutosTipos = lk.GetComboGProdutosTipos(db);
            ViewBag.comboProdutosNCM = lk.GetComboGProdutosNcm(db);
            ViewBag.comboIcmsUfIsento = lk.GetComboGcIcmsUfIsento(db);
            ViewBag.comboIcmsCstSimples = lk.GetComboGcIcmsCstSimples(db);
            ViewBag.comboUnidadeMedida = lk.GetComboGUnidadeMedida(db);
            ViewBag.ComboComexImportacoes = lk.GetComboGcComexImportacoesTodas(db);
            ViewBag.ComboComexImportacoes.Insert(0, new SelectListItem { Value = "0", Text = "[ IMPORTAÇÃO ]" });
            ViewBag.comboEstoqueEnderecoAreaBH = lk.GetComboGcEstoqueEnderecoArea(db, 1);
            ViewBag.comboEstoqueEnderecoSecaoBH = lk.GetComboGcEstoqueEnderecoSecao(db, 1);
            ViewBag.comboEstoqueEnderecoCorredorBH = lk.GetComboGcEstoqueEnderecoCorredor(db, 1);
            ViewBag.comboEstoqueEnderecoPrateleiraBH = lk.GetComboGcEstoqueEnderecoPrateleira(db, 1);
            ViewBag.comboEstoqueEnderecoAreaSP = lk.GetComboGcEstoqueEnderecoArea(db, 3);
            ViewBag.comboEstoqueEnderecoSecaoSP = lk.GetComboGcEstoqueEnderecoSecao(db, 3);
            ViewBag.comboEstoqueEnderecoCorredorSP = lk.GetComboGcEstoqueEnderecoCorredor(db, 3);
            ViewBag.comboEstoqueEnderecoPrateleiraSP = lk.GetComboGcEstoqueEnderecoPrateleira(db, 3);
        }

        private void PreencherLookupsModalDesativarProduto()
        {
            ViewBag.comboProdutosServicos = ProdutosLookups.GetComboGcProdutosServicosTodos(db);
        }
    }
}
