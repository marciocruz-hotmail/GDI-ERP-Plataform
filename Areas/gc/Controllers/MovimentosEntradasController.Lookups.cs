using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Lookups NF entrada — frete via serviço; tipos fixos por modal; produtos nacional via GetComboGcProdutosServicosTodos.</summary>
    public partial class MovimentosEntradasController
    {
        private ILookupQueryService MovimentosEntradasLookups => LookupQueryServiceAccessor.Current;

        private void PreencherLookupsFreteResponsavel()
        {
            ViewBag.comboFreteResponsavel = MovimentosEntradasLookups.GetComboGcFreteResponsavel(db);
        }

        /// <summary>Modal upload NF — único tipo por fluxo (sem contrato dedicado no serviço).</summary>
        private void PreencherLookupsComboMovimentoTipoFixo(string value, string text)
        {
            ViewBag.comboMovimentosTipos = new List<SelectListItem>
            {
                new SelectListItem { Value = value, Text = text }
            };
        }

        private void PreencherLookupsModalImportarNacional()
        {
            PreencherLookupsComboMovimentoTipoFixo("5", "1.1.1 - Entrada - Fornecedor - Nacional");
        }

        private void PreencherLookupsModalImportarDevolucao()
        {
            PreencherLookupsComboMovimentoTipoFixo("9", "1.1.4 - Devolução");
        }

        private void PreencherLookupsModalImportarImportacao()
        {
            PreencherLookupsComboMovimentoTipoFixo("6", "1.1.2 - Entrada - Fornecedor - Exterior");
        }

        /// <summary>Processar NF nacional — reutiliza GetComboGcProdutosServicosTodos (truncamento no serviço).</summary>
        private void PreencherLookupsComboProdutosEntradaNacional()
        {
            var combo = MovimentosEntradasLookups.GetComboGcProdutosServicosTodos(db);
            var result = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ SELECIONE ]" }
            };
            foreach (var item in combo)
            {
                if (item.Value != "-1" && !string.IsNullOrWhiteSpace(item.Value))
                    result.Add(item);
            }
            ViewBag.comboProdutos = result;
        }

        /// <summary>Processar NF devolução — itens do movimento de referência (lógica específica; sem Get* equivalente).</summary>
        private void PreencherLookupsComboProdutosDevolucao(int idMovimentoReferencia)
        {
            var comboProdutos = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ SELECIONE ]" }
            };
            const int sizeNomeItem = 100;
            string sentencaSql = "select mi.id_movimento_item, mi.sequencia, mi.id_produto, p.nome " +
                                " from gc_movimentos_itens mi " +
                                " left join g_produtos p on (mi.id_produto = p.id_produto) " +
                                " where mi.id_movimento = " + idMovimentoReferencia +
                                " order by mi.sequencia";
            DataTable tableItemReferencia = LibDB.GetDataTable(sentencaSql, db);
            foreach (var dsRow in tableItemReferencia.AsEnumerable())
            {
                string idProduto = dsRow["id_produto"].EmptyIfNull().ToString();
                string nomeProduto = dsRow["nome"].EmptyIfNull().ToString();
                if (nomeProduto.Length > sizeNomeItem)
                    nomeProduto = nomeProduto.Substring(0, sizeNomeItem) + "...";
                comboProdutos.Add(new SelectListItem { Value = idProduto, Text = nomeProduto });
            }
            ViewBag.comboProdutos = comboProdutos;
        }
    }
}
