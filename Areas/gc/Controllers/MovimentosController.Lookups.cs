using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Areas.gc.Controllers
{
    /// <summary>Fase 4/6a — lookups centralizados via ILookupQueryService.</summary>
    public partial class MovimentosController
    {
        private ILookupQueryService MovimentosLookups => LookupQueryServiceAccessor.Current;

        #region PreencherLookups — pedidos / movimentos (Fase 4)

        /// <summary>IndexPedido — filtros da listagem (cliente via typeahead Ajax — PERF-004).</summary>
        private void PreencherLookupsIndexPedido()
        {
            ViewBag.comboClientes = LookupSearchQueries.ComboFiltroClienteTodosAtivos();
            ViewBag.comboTiposMovimentos = MovimentosLookups.GetComboGcTiposMovimentosVendas(db);
            ViewBag.comboStatusMovimentos = MovimentosLookups.GetComboGcStatusMovimentos(db);
            ViewBag.comboMovimentosPosicao = MovimentosLookups.GetComboGcMovimentosPosicao(db);
        }

        /// <summary>IndexEstoque — filtro produto: opções fixas + typeahead (mesmo contrato de Estoque/Index).</summary>
        private void PreencherLookupsIndexEstoque()
        {
            ViewBag.comboProdutosServicos = LookupSearchQueries.ComboFiltroProdutoPosicaoEstoqueIndex();
        }

        /// <summary>FormPedidoCreate — novo pedido/cotação/OS.</summary>
        private void PreencherLookupsPedidoFormCreate()
        {
            PreencherLookupsPedidoFormCore(0, idImportacaoSelecionada: 0, incluirMovimentosPosicao: true, incluirPlaceholderPagRec: true, carregarDestinatarios: false);
        }

        /// <summary>FormPedidoCreate — edição de pedido existente.</summary>
        private void PreencherLookupsPedidoFormEdit(int idCliente, int idImportacaoSelecionada)
        {
            PreencherLookupsPedidoFormCore(idCliente, idImportacaoSelecionada, incluirMovimentosPosicao: false, incluirPlaceholderPagRec: false, carregarDestinatarios: true);
        }

        /// <summary>Combo importação COMEX mínimo no HTML; lista completa via Ajax na aba Invoice (PERF-009).</summary>
        private List<SelectListItem> ComboComexImportacoesPedidoInicial(int idImportacaoSelecionada)
        {
            var combo = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ Selecione na aba Invoice / SO ]", Selected = idImportacaoSelecionada <= 0 }
            };
            if (idImportacaoSelecionada > 0)
            {
                var imp = db.gc_comex_importacoes.AsNoTracking()
                    .Where(c => c.id_importacao == idImportacaoSelecionada)
                    .Select(c => new { c.id_importacao, c.numero })
                    .FirstOrDefault();
                if (imp != null)
                {
                    combo.Add(new SelectListItem
                    {
                        Value = imp.id_importacao.ToString(),
                        Text = imp.numero.EmptyIfNull().ToString().Trim(),
                        Selected = true
                    });
                }
            }
            return combo;
        }

        private void PreencherLookupsPedidoFormCore(int idCliente, int idImportacaoSelecionada, bool incluirMovimentosPosicao, bool incluirPlaceholderPagRec, bool carregarDestinatarios)
        {
            var lk = MovimentosLookups;

            ViewBag.comboClientes = LookupSearchQueries.ComboPlaceholderCliente();
            if (idCliente > 0)
            {
                var item = LookupSearchQueries.GetClienteItem(db, idCliente);
                if (item != null)
                    ViewBag.comboClientes.Add(new SelectListItem { Value = item.id, Text = item.text, Selected = true });
            }

            ViewBag.comboTiposMovimentosCreateEdit = lk.GetComboGcTiposMovimentosCreateEdit(db);
            ViewBag.comboClientesContatos = lk.GetComboGcClientesContatos(db, idCliente);
            ViewBag.dataSetClientesContatos = lk.GetDatasetGcClientesContatos(db);

            var locaisEstoque = lk.GetComboGcLocaisEstoqueOrders(db);
            ViewBag.comboLocaisEstoque = locaisEstoque;
            ViewBag.comboLocaisEstoqueOrders = locaisEstoque;

            ViewBag.comboVendedores = lk.GetComboGVendedores(db);

            ViewBag.comboTransportadora = lk.GetComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar = lk.GetComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar.Insert(0, new SelectListItem { Value = "-1", Text = "[ SEM FRETE COMPLEMENTAR ]" });

            if (incluirMovimentosPosicao)
            {
                ViewBag.comboMovimentosPosicao = lk.GetComboGcMovimentosPosicao(db);
            }

            ViewBag.comboCfopFinalidade = lk.GetComboGcCfopFinalidade(db);
            ViewBag.comboFreteResponsavel = lk.GetComboGcFreteResponsavel(db);
            ViewBag.comboFreteResponsavel.Insert(0, new SelectListItem { Value = "-1", Text = "[ Frete ]" });
            ViewBag.ComboComexImportacoes = ComboComexImportacoesPedidoInicial(idImportacaoSelecionada);

            if (carregarDestinatarios && idCliente > 0)
            {
                ViewBag.comboClienteDestinatarios = lk.GetComboGcClientesDestinatarios(db, idCliente);
            }
            else
            {
                ViewBag.comboClienteDestinatarios = new List<SelectListItem>();
            }

            ViewBag.comboMoedas = lk.GetComboGMoedas(db);
            ViewBag.comboPagRecCondicoes = lk.GetComboPagRecCondicoesTodas(db);
            if (incluirPlaceholderPagRec)
            {
                ViewBag.comboPagRecCondicoes.Insert(0, new SelectListItem { Value = "-1", Text = "[ Condição Pagto. ]" });
            }

            ViewBag.comboGcCfopOperacao = lk.GetComboGcCfopOperacoesTelaPedido(db);
        }

        /// <summary>Modais inserir/editar/duplicar item do pedido (produto via Ajax — sem dataset/combo completo).</summary>
        private void PreencherLookupsPedidoItemModal(int idProdutoSelecionado = 0)
        {
            var lk = MovimentosLookups;
            ViewBag.comboProdutosServicos = LookupSearchQueries.ComboPlaceholderProduto();
            if (idProdutoSelecionado > 0)
            {
                var prod = LookupSearchQueries.GetProdutoItem(db, idProdutoSelecionado);
                if (prod != null)
                    ViewBag.comboProdutosServicos.Add(new SelectListItem { Value = prod.id, Text = prod.text, Selected = true });
            }
            ViewBag.comboProdutosCondicoes = lk.GetComboGProdutoCondicao(db);
            ViewBag.comboEntregasPrazos = lk.GetComboGcEntregasPrazos(db);
        }

        /// <summary>ModalPedidoAprovacao.</summary>
        private void PreencherLookupsPedidoAprovacao(int idCliente)
        {
            var lk = MovimentosLookups;
            lk.GetDatasetGVendedores(db);
            ViewBag.comboVendedores = lk.GetComboGVendedores(db);
            ViewBag.comboPagRecCondicoes = lk.GetComboPagRecCondicoesTodas(db);
            ViewBag.comboLocaisEstoqueOrders = lk.GetComboGcLocaisEstoqueOrders(db);
            ViewBag.comboTransportadora = lk.GetComboGcTransportadora(db);
            ViewBag.ComboGcClientesContatosTipos = lk.GetComboGcClientesContatosTipos(db);
            ViewBag.ComboClientesContatos = lk.GetComboGcClientesContatosPedido(db, idCliente);
        }

        /// <summary>Modais que só precisam do dataset/combo de vendedores.</summary>
        private void PreencherLookupsVendedoresModal()
        {
            var lk = MovimentosLookups;
            lk.GetDatasetGVendedores(db);
            ViewBag.comboVendedores = lk.GetComboGVendedores(db);
        }

        /// <summary>Modal faturamento NF — CFOP, frete, transportadora.</summary>
        private void PreencherLookupsFaturamentoPedido(int idCfopOperacao)
        {
            var lk = MovimentosLookups;
            ViewBag.comboCFOP = lk.GetComboGcCfop(db);
            ViewBag.comboCfopOperacoes = lk.GetComboGcCfopOperacoesFaturamentoPedido(db, idCfopOperacao);
            ViewBag.comboFreteResponsavel = lk.GetComboGcFreteResponsavel(db);
            ViewBag.comboTransportadora = lk.GetComboGcTransportadora(db);
            ViewBag.comboTransportadora.Insert(0, new SelectListItem { Value = "0", Text = "SEM TRANSPORTADORA" });
            ViewBag.ComboDestinatarios = new List<SelectListItem>();
        }

        /// <summary>ModalPedidoExpedicao e similares.</summary>
        private void PreencherLookupsTransportadoraExpedicao()
        {
            var lk = MovimentosLookups;
            ViewBag.comboTransportadora = lk.GetComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar = lk.GetComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar.Insert(0, new SelectListItem { Value = "-1", Text = "[ SEM FRETE COMPLEMENTAR ]" });
        }

        /// <summary>PainelPedidos — filtro cliente via typeahead (PERF-004).</summary>
        private void PreencherLookupsPainelPedidos()
        {
            var lk = MovimentosLookups;
            ViewBag.comboClientes = LookupSearchQueries.ComboFiltroClienteTodosAtivos();
            ViewBag.comboMovimentosPosicao = lk.GetComboGcMovimentosPosicao(db);
            var listaLocaisEstoque = lk.GetComboGcLocaisEstoqueOrders(db);
            listaLocaisEstoque.RemoveAt(0);
            listaLocaisEstoque.Add(new SelectListItem { Value = "-1", Text = "[ TODOS ]" });
            ViewBag.comboLocaisEstoque = listaLocaisEstoque;
        }

        /// <summary>ModalConsultaPedidos — cliente e produto typeahead (CACHE-2d).</summary>
        private void PreencherLookupsConsultaPedidos()
        {
            ViewBag.comboClientes = LookupSearchQueries.ComboFiltroClienteSelecione();
            ViewBag.comboProdutosServicos = LookupSearchQueries.ComboFiltroProdutoConsultaPedidos();
        }

        /// <summary>Contatos do cliente no contexto do movimento.</summary>
        private void PreencherLookupsClientesContatos(int idCliente)
        {
            ViewBag.comboClientesContatos = MovimentosLookups.GetComboGcClientesContatos(db, idCliente);
        }

        /// <summary>Importações COMEX no formulário de pedido.</summary>
        private void PreencherLookupsComexImportacoesPedido()
        {
            ViewBag.ComboComexImportacoes = MovimentosLookups.GetComboGcComexImportacoesTodas(db);
        }

        #endregion
    }
}
