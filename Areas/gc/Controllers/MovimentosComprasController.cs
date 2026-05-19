using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using ClosedXML.Excel;
using System.Data.Entity;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_MovimentosCompras_*,gc_MovimentosCompras_Default")]
    public class MovimentosComprasController : Controller
    {
        private GdiPlataformEntities db;
        public MovimentosComprasController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region IndexCompras
        public ActionResult IndexCompras()
        {
            ViewBag.comboClientes = LibDataSets.LoadComboGClientesFornecedores(db); ;
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]" });
            ViewBag.comboTiposMovimentos = LibDataSets.LoadComboGcTiposMovimentosCompras(db);
            ViewBag.comboStatusMovimentos = LibDataSets.LoadComboGcStatusMovimentos(db);
            ViewBag.comboMovimentosPosicao = LibDataSets.LoadComboGcMovimentosPosicao(db);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-clipboard-list", "", "#008000", "fa-lg") + "&nbsp;" + LibIcons.getIcon("fa-solid fa-boxes", "", "#008000", "fa-lg") + "  Painel de Compras  -  Cotações / Pedidos";
            //LibStringFormat.GetTabHtml(2)
            return View();
        }
        #endregion

        #region CreateCotacaoPedidoCompra
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_MovimentosCompras_*,gc_MovimentosCompras_Index")]
        public ActionResult CreateCotacaoCompra()
        {
            DeleteItemTemporario();
            return CreateCotacaoPedidoCompra("Nova Cotação", "Cotação", 3);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_MovimentosCompras_*,gc_MovimentosCompras_Index")]
        public ActionResult CreatePedidoCompra()
        {
            DeleteItemTemporario();
            return CreateCotacaoPedidoCompra("Novo Pedido", "Pedido", 4);
        }

        public ActionResult CreateCotacaoPedidoCompra(String Titulo, String TipoSolicitacao, int IdMovimentoStatus)
        {
            gc_movimentos record_gc_movimento = new Db.gc_movimentos();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>" + Titulo + "</b>";
            ViewBag.tipoSolicitacao = TipoSolicitacao;
            record_gc_movimento.id_movimento_tipo = IdMovimentoStatus; // Ordem de Serviço
            record_gc_movimento.id_movimento_status = 1; // Aberto
            record_gc_movimento.data_vencimento = DateTime.Now.AddDays(10);
            record_gc_movimento.param_reducao_bc = false;
            record_gc_movimento.param_zerar_difal = false;
            record_gc_movimento.has_beneficio_aviacao = false;
            record_gc_movimento.id_vendedor = -1;
            record_gc_movimento.id_local_estoque = -1;
            if (CachePersister.userIdentity.IdVendedor > 0) { record_gc_movimento.id_vendedor = CachePersister.userIdentity.IdVendedor; }; // Set Vendedor logado            
            ViewBag.idMovimento = (CachePersister.userIdentity.IdUsuario * -1).ToString(); // O Id, será o negativo do id do usuário;
            ViewBag.comboClientes = LibDataSets.LoadComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ SELECIONE ]" });
            ViewBag.comboTiposMovimentosCreateEdit = LibDataSets.LoadComboGcTiposMovimentosCreateEdit(db);
            ViewBag.comboClientesContatos = LibDataSets.LoadComboGcClientesContatos(db, 0);
            ViewBag.dataSetClientesContatos = LibDataSets.LoadDatasetGcClientesContatos(db);
            ViewBag.comboLocaisEstoque = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
            ViewBag.comboLocaisEstoqueOrders = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
            ViewBag.comboVendedores = LibDataSets.LoadComboGVendedores(db);
            ViewBag.comboVendedores.Insert(0, new SelectListItem { Value = "0", Text = "[ ESTOQUE ]" });
            ViewBag.comboTransportadora = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.comboMovimentosPosicao = LibDataSets.LoadComboGcMovimentosPosicao(db);
            LibDataSets.LoadDatasetGVendedores(db);
            ViewBag.comboMoedas = LibDataSets.LoadComboGMoedas(db);
            ViewBag.comboPagRecCondicoes = LibDataSets.LoadComboPagRecCondicoesFaturaveis(db);
            return View("CreateCotacaoPedidoCompra", record_gc_movimento);
        }

        public ActionResult ModalInserirItemCompra(int? idMovimento)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-dice-d6", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Inserir Novo Item</b>";
            ViewBag.idMovimento = idMovimento;
            gc_movimentos_itens record_gc_movimento_item = new Db.gc_movimentos_itens();
            record_gc_movimento_item.id_movimento = idMovimento.GetValueOrDefault();
            record_gc_movimento_item.id_movimento_item = -1;
            record_gc_movimento_item.quantidade = 1;
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosTodos(db);
            ViewBag.comboProdutosCondicoes = LibDataSets.LoadComboGProdutoCondicao(db);
            ViewBag.comboEntregasPrazos = LibDataSets.LoadComboGcEntregasPrazos(db);
            ViewBag.dataSetProdutosServicos = LibDataSets.LoadDatasetGcProdutosServicos(db);
            return View("ModalPedidoInsertEditItemCompra", record_gc_movimento_item);
        }

        public ActionResult ModalEditarItemCompra(int? IdItem)
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Editar Item";
            gc_movimentos_itens record_gc_movimento_item = db.gc_movimentos_itens.Find(IdItem);
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosTodos(db);
            ViewBag.comboProdutosCondicoes = LibDataSets.LoadComboGProdutoCondicao(db);
            ViewBag.comboEntregasPrazos = LibDataSets.LoadComboGcEntregasPrazos(db);
            ViewBag.dataSetProdutosServicos = LibDataSets.LoadDatasetGcProdutosServicos(db);
            return View("ModalPedidoInsertEditItemCompra", record_gc_movimento_item);
        }


        [HttpPost]
        public ActionResult AjaxInsertEditItemCompra(gc_movimentos_itens record_gc_movimento_item)
        {
            bool sucesso = false;
            int qtdInconsistencias = 0;
            String msgRetorno = "";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                if (ModelState.IsValid)
                {
                    if (record_gc_movimento_item.quantidade <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [Quantidade] não pode ser menor ou igual a zero!<br/>";
                    }
                    if (record_gc_movimento_item.valor_unit < 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [R$ Unit] não pode ser menor que zero!<br/>";
                    }
                    if ((record_gc_movimento_item.valor_unit == 0) && (record_gc_movimento_item.obs.Trim().Length == 0))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Para [R$ Unit] R$ 0,00 o campo [Obs] é obrigatório!<br/>";
                    }

                    if ((record_gc_movimento_item.id_produto == 0) || (record_gc_movimento_item.id_produto == -1))
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - [Produto] não foi informado!<br/>";
                    }
                }
                else
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }

                if (qtdInconsistencias == 0)
                {
                    if (record_gc_movimento_item.id_movimento_item == -1)
                    {
                        // Novo Item

                        if (record_gc_movimento_item.sequencia == 0)
                        {
                            decimal Sequencia = 0;
                            string _Sequencia = LibDB.dbQueryValue("select max(sequencia) from gc_movimentos_itens m where m.id_movimento = " + record_gc_movimento_item.id_movimento.ToString(), db);
                            decimal.TryParse(_Sequencia, out Sequencia);
                            Sequencia += 1;
                            record_gc_movimento_item.sequencia = Sequencia;
                        }
                        gc_movimentos_itens new_record_gc_movimentos_itens = new Db.gc_movimentos_itens();
                        new_record_gc_movimentos_itens.id_movimento = record_gc_movimento_item.id_movimento;
                        new_record_gc_movimentos_itens.id_produto = record_gc_movimento_item.id_produto;
                        new_record_gc_movimentos_itens.id_produto_condicao = record_gc_movimento_item.id_produto_condicao;
                        new_record_gc_movimentos_itens.id_entrega_prazo = record_gc_movimento_item.id_entrega_prazo;
                        new_record_gc_movimentos_itens.sequencia = record_gc_movimento_item.sequencia;
                        new_record_gc_movimentos_itens.quantidade = Math.Truncate(record_gc_movimento_item.quantidade);
                        new_record_gc_movimentos_itens.valor_unit = Math.Round(record_gc_movimento_item.valor_unit, 2);
                        new_record_gc_movimentos_itens.valor_total = Math.Round(record_gc_movimento_item.quantidade * record_gc_movimento_item.valor_unit, 2);
                        new_record_gc_movimentos_itens.valor_unit_corecharge = Math.Round(record_gc_movimento_item.valor_unit_corecharge, 2);
                        new_record_gc_movimentos_itens.valor_total_corecharge = Math.Round((record_gc_movimento_item.quantidade * record_gc_movimento_item.valor_unit_corecharge), 2);
                        new_record_gc_movimentos_itens.obs = record_gc_movimento_item.obs;
                        new_record_gc_movimentos_itens.obs_nf = record_gc_movimento_item.obs_nf;
                        new_record_gc_movimentos_itens.serial = record_gc_movimento_item.serial;
                        new_record_gc_movimentos_itens.lote01_identificador = record_gc_movimento_item.lote01_identificador;
                        db.gc_movimentos_itens.Add(new_record_gc_movimentos_itens);
                    }
                    else
                    {
                        // Editar Item
                        gc_movimentos_itens edit_record_gc_movimento_item = db.gc_movimentos_itens.Find(record_gc_movimento_item.id_movimento_item);
                        edit_record_gc_movimento_item.id_produto = record_gc_movimento_item.id_produto;
                        edit_record_gc_movimento_item.id_produto_condicao = record_gc_movimento_item.id_produto_condicao;
                        edit_record_gc_movimento_item.id_entrega_prazo = record_gc_movimento_item.id_entrega_prazo;
                        edit_record_gc_movimento_item.sequencia = record_gc_movimento_item.sequencia;
                        edit_record_gc_movimento_item.quantidade = Math.Truncate(record_gc_movimento_item.quantidade);
                        edit_record_gc_movimento_item.valor_unit = Math.Round(record_gc_movimento_item.valor_unit, 2);
                        edit_record_gc_movimento_item.valor_total = Math.Round(record_gc_movimento_item.quantidade * record_gc_movimento_item.valor_unit, 2);
                        edit_record_gc_movimento_item.valor_unit_corecharge = Math.Round(record_gc_movimento_item.valor_unit_corecharge, 2);
                        edit_record_gc_movimento_item.valor_total_corecharge = Math.Round((record_gc_movimento_item.quantidade * record_gc_movimento_item.valor_unit_corecharge), 2);
                        edit_record_gc_movimento_item.obs = record_gc_movimento_item.obs;
                        edit_record_gc_movimento_item.obs_nf = record_gc_movimento_item.obs_nf;
                        edit_record_gc_movimento_item.serial = record_gc_movimento_item.serial;
                        edit_record_gc_movimento_item.datahora_alteracao = DataHoraAtual;
                        edit_record_gc_movimento_item.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(edit_record_gc_movimento_item).State = EntityState.Modified;
                    }
                    db.SaveChanges();
                    sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                sucesso = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ModalDuplicateItemCompra(int? idMovimentoItem)
        {
            gc_movimentos_itens record_item_original = db.gc_movimentos_itens.Find(idMovimentoItem);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-dice-d6", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Inserir Novo Item</b>";
            ViewBag.idMovimento = record_item_original.id_movimento;
            ViewBag.ActionDuplicate = "1";
            gc_movimentos_itens record_gc_movimento_item = LibDB.CloneTObject(record_item_original);
            record_gc_movimento_item.id_movimento = record_item_original.id_movimento;
            record_gc_movimento_item.id_movimento_item = -1;
            record_gc_movimento_item.quantidade = 1;
            record_gc_movimento_item.serial = string.Empty;
            record_gc_movimento_item.tag = true;
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosTodos(db);
            ViewBag.comboProdutosCondicoes = LibDataSets.LoadComboGProdutoCondicao(db);
            ViewBag.comboEntregasPrazos = LibDataSets.LoadComboGcEntregasPrazos(db);
            ViewBag.dataSetProdutosServicos = LibDataSets.LoadDatasetGcProdutosServicos(db);
            return View("ModalPedidoInsertEditItemCompra", record_gc_movimento_item);
        }

        [HttpPost]
        public ActionResult AjaxRemoverItemCompra(gc_movimentos_itens record_gc_movimento_item)
        {
            bool sucesso = false;
            String msgRetorno = "";
            try
            {
                String SqlDelete = "delete from  gc_movimentos_itens where id_movimento_item = " + record_gc_movimento_item.id_movimento_item.ToString(); // O Id, será o negativo do id do usuário;
                DataTable tableRegistroDeleteExtratos = LibDB.GetDataTable(SqlDelete, db);
                sucesso = true;
            }
            catch (DbEntityValidationException ex)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetDadosCompras(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            String filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.gc_movimentos>();
            var allRecordsClientes = db.g_clientes.Select(g => new { g.id_cliente, g.nome }).ToList();
            var allRecordsVendedores = db.g_vendedores.Select(v => new { v.id_vendedor, v.apelido }).ToList();
            var allRecordsLocaisEstoqueOrders = db.gc_locais_estoque.Select(e => new { e.id_local_estoque, e.sigla }).ToList();
            String SentencaSQL = string.Empty;
            DateTime DataField02 = new DateTime();
            DateTime DataField03 = new DateTime();
            DateTime.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField02);
            DateTime.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField03);

            SentencaSQL = " select * from gc_movimentos m where (m.id_movimento_tipo = 12 or m.id_movimento_tipo = 15) ";
            if (param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) // Localizar pelo número da cotação ou pelo número da nota fiscal (* na frente)
            {
                String TermoPesquisa = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                if (TermoPesquisa.StartsWith("*"))
                {
                    SentencaSQL += " and m.id_movimento in (select distinct id_movimento from gc_movimentos_nf where nf_numero = '" + TermoPesquisa.Substring(1) + "')";
                }
                else
                {
                    SentencaSQL += " and m.id_movimento = " + param.yesCustomField01.EmptyIfNull().ToString().Trim();
                }
            }
            else
            {
                if ((param.yesCustomField02.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField03.EmptyIfNull().ToString().Trim().Length > 0))
                { SentencaSQL += " and m.datahora_alteracao between '" + DataField02.ToString("yyyy-MM-dd") + " 00:00:00" + "' and '" + DataField03.ToString("yyyy-MM-dd") + " 23:59:59'"; }

                if ((param.yesCustomField04.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField04.EmptyIfNull().ToString().Trim() != "-1") && (param.yesCustomField04.EmptyIfNull().ToString().Trim() != "0"))
                { SentencaSQL += " and m.id_cliente = " + param.yesCustomField04.EmptyIfNull().ToString().Trim(); }

                if ((param.yesCustomField05.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField05.EmptyIfNull().ToString().Trim() != "-1"))
                { SentencaSQL += " and m.id_movimento_posicao = " + param.yesCustomField05.EmptyIfNull().ToString().Trim(); }

                if ((param.yesCustomField06.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField06.EmptyIfNull().ToString().Trim() != "-1"))
                { SentencaSQL += " and m.id_movimento_tipo = " + param.yesCustomField06.EmptyIfNull().ToString().Trim(); }

                if ((param.yesCustomField07.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField07.EmptyIfNull().ToString().Trim() != "-1"))
                { SentencaSQL += " and m.id_movimento_status = " + param.yesCustomField07.EmptyIfNull().ToString().Trim(); }
            }

            SentencaSQL += " order by id_movimento desc ";

            allRecords = db.gc_movimentos.SqlQuery(SentencaSQL).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.gc_movimentos, string> orderingFunction = (c => param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_movimento) : "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_movimento); }
                    else if (param.iSortCol_0 == 7) { displayedRecords = displayedRecords.OrderByDescending(c => c.datahora_cadastro); }
                    else if (param.iSortCol_0 == 10) { displayedRecords = displayedRecords.OrderByDescending(c => c.valor_total_bruto); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_movimento); }
                    else if (param.iSortCol_0 == 7) { displayedRecords = displayedRecords.OrderBy(c => c.datahora_cadastro); }
                    else if (param.iSortCol_0 == 10) { displayedRecords = displayedRecords.OrderBy(c => c.valor_total_bruto); }
                }
            }

            List<string[]> list = new List<string[]>();
            foreach (var m in displayedRecords)
            {
                var arrayCliente = allRecordsClientes.Find(f => f.id_cliente == m.id_cliente);
                var arrayVendedor = allRecordsVendedores.Find(v => v.id_vendedor == m.id_vendedor);

                string iconeTipo = String.Empty;
                if (m.id_movimento_tipo == 12) { iconeTipo = LibIcons.getIcon("fa-solid fa-clipboard-list", "Cotação", "#CACFD2", "fa-lg"); }
                else if (m.id_movimento_tipo == 15) { iconeTipo = LibIcons.getIcon("fa-solid fa-boxes", "Pedido", "#008000", "fa-lg"); }

                string iconeStatus = String.Empty;
                if (m.id_movimento_status == 1) { iconeStatus = LibIcons.getIcon("fa-solid fa-shopping-cart", "Aberto", "#CACFD2", "fa-lg"); }
                else if (m.id_movimento_status == 2) { iconeStatus = LibIcons.getIcon("fa-solid fa-lock", "Fechado", "#008000", "fa-lg"); }
                else if (m.id_movimento_status == 3) { iconeStatus = LibIcons.getIcon("fa-regular fa-thumbs-down", "Cancelado", "cc0000", "fa-lg"); }

                string IconeStatusPrazos = String.Empty;
                string LabelIconeStatusPrazos = String.Empty;
                if (m.id_movimento_status == 1) // Aberto
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia().Date;
                    if (m.data_vencimento == DataHoraAtual)
                    {
                        string dataVencimento = m.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy");
                        if (m.id_movimento_tipo == 12) { LabelIconeStatusPrazos = "Orçamento VENCENDO Hoje " + dataVencimento; }
                        else if (m.id_movimento_tipo == 15) { LabelIconeStatusPrazos = "Pedido VENCENDO Hoje " + dataVencimento; }
                        IconeStatusPrazos = LibIcons.getIcon("fa-solid fa-calendar-day", LabelIconeStatusPrazos, "#ffbb00", "fa-lg");

                    }
                    else if (m.data_vencimento < DataHoraAtual)
                    {
                        string dataVencimento = m.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy");
                        if (m.id_movimento_tipo == 12) { LabelIconeStatusPrazos = "Orçamento VENCIDO em " + dataVencimento; }
                        else if (m.id_movimento_tipo == 15) { LabelIconeStatusPrazos = "Pedido VENCIDO em " + dataVencimento; }
                        IconeStatusPrazos = LibIcons.getIcon("fa-regular fa-calendar-times", LabelIconeStatusPrazos, "#cc0000", "fa-lg");
                    }
                    else
                    {
                        string dataVencimento = m.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy");
                        if (m.id_movimento_tipo == 12) { LabelIconeStatusPrazos = "Orçamento a vencer em " + dataVencimento; }
                        else if (m.id_movimento_tipo == 15) { LabelIconeStatusPrazos = "Pedido a vencer em " + dataVencimento; }
                        IconeStatusPrazos = LibIcons.getIcon("fa-regular fa-calendar-check", LabelIconeStatusPrazos, "#008000", "fa-lg");
                    }
                }
                else if (m.id_movimento_status == 2) // Fechado
                {
                    if (m.id_movimento_tipo == 12) { LabelIconeStatusPrazos = "Orçamento Fechado"; }
                    else if (m.id_movimento_tipo == 15) { LabelIconeStatusPrazos = "Pedido Fechado"; }
                    IconeStatusPrazos = LibIcons.getIcon("fa-solid fa-calendar-check", LabelIconeStatusPrazos, "#008000", "fa-lg");
                }

                string iconePosicao = String.Empty;
                string TipoMovimento = String.Empty;
                if (m.id_movimento_tipo == 12) { TipoMovimento = "Orçamento"; } else if (m.id_movimento_tipo == 15) { TipoMovimento = "Pedido"; };

                string formatoMoeda = string.Empty;
                if (m.id_moeda == 1) { formatoMoeda = "pt-BR"; }
                else if (m.id_moeda == 2) { formatoMoeda = "en-US"; }
                string valorFormatado = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", m.valor_total_bruto);
                if (m.id_moeda == 2) { valorFormatado = valorFormatado.Replace("$", "$ "); };

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    m.id_movimento.ToString(),
                                    iconeTipo,
                                    iconeStatus,
                                    iconePosicao,
                                    ((arrayCliente != null) ? arrayCliente.nome.EmptyIfNull().ToString() : String.Empty),
                                    ((arrayVendedor != null) ? arrayVendedor.apelido.EmptyIfNull().ToString() : String.Empty),
                                    m.datahora_alteracao.GetValueOrDefault().ToString("dd/MM/yy"),
                                    IconeStatusPrazos,
                                    m.qtd_itens.EmptyIfNull().ToString(),
                                    valorFormatado,
                                    "", // Botão Editar
                                });
            }

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count(),
                iTotalDisplayRecords = allRecords.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }
        #endregion

        public void DeleteItemTemporario()
        {
            try
            {
                String SqlDelete = " delete from  gc_movimentos_itens where id_movimento = " + (CachePersister.userIdentity.IdUsuario * -1).ToString(); // O Id, será o negativo do id do usuário;
                DataTable tableRegistroDeleteExtratos = LibDB.GetDataTable(SqlDelete, db);
            }
            catch (Exception) { };
        }

        #region ModalImportarExcelSC
        public ActionResult ModalImportarExcelSC(int? idMovimento)
        {
            DeleteItemTemporario();
            CstUploadFiles record_cstUploadFiles = new CstUploadFiles();
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-file-excel", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Importar Excel - Southern Cross";
            var comboMovimentosTipos = new List<SelectListItem>();
            comboMovimentosTipos.Add(new SelectListItem { Value = "12", Text = "1.2 - Cotação - Fornecedor" });
            ViewBag.comboMovimentosTipos = comboMovimentosTipos;
            record_cstUploadFiles.IdMovimentoTipo = 12;
            ViewBag.idMovimento = (CachePersister.userIdentity.IdUsuario * -1).ToString(); // O Id, será o negativo do id do usuário;
            return View("ModalImportarExcelSC", record_cstUploadFiles);
        }

        [HttpPost]
        public ActionResult AjaxModalImportarExcelSC(CstUploadFiles record_cstUploadFiles)
        {
            bool Processado = false;
            bool ErroProcessamento = false;
            int QtdProdutosCadastrados = 0;
            int QtdItensImportados = 0;
            string FileUploadXLXS = string.Empty;
            string FileNameXlsxLocal = string.Empty;
            string FileNameXlsxUpload = string.Empty;
            String MsgRetorno = String.Empty;
            String ResultadoProcessamento = String.Empty;
            String IdProcessamentoGravado = "0";

            int QtdNovosProdutosCadastrados = 0;
            int QtdProdutosVinculados = 0;
            int QtdProdutosNaoVinculados = 0;

            if (record_cstUploadFiles.FilesourceXLXS != null) 
            { 
                if (record_cstUploadFiles.FilesourceXLXS.FileName.EmptyIfNull().ToString().Length > 0) 
                {
                    if (record_cstUploadFiles.FilesourceXLXS.FileName.EmptyIfNull().ToString().ToLowerInvariant().IndexOf("xlsx") <= 0)
                    {
                        MsgRetorno += "O Layout do arquivo XLSX informado não foi identificado pelo ERP" + "<br/>";
                        ErroProcessamento = true;
                    }
                }
                else
                {
                    MsgRetorno += "O Arquivo XLSX não foi informado!" + "<br/>";
                    ErroProcessamento = true;
                }
            }
            else
            {
                MsgRetorno += "O Arquivo XLSX não foi informado!" + "<br/>";
                ErroProcessamento = true;
            }


            if ((ErroProcessamento == false) && (record_cstUploadFiles.FilesourceXLXS.ContentLength > 0))
            {
                try
                {
                    String ColunaCabecalho = string.Empty;
                    XLWorkbook WorkBook = null;
                    int IndexItem = -1;
                    int IndexDescription = -1;
                    int IndexOrdered = -1;
                    int IndexCD = -1;
                    int IndexT = -1;
                    int IndexUOM = -1;
                    int IndexQty = -1;
                    int IndexUnitPrice = -1;
                    int IndexTotalPrice = -1;
                    bool LeituraAtiva = false;
                    List<CstModelSalesOrderSC> ListaItensSO = new List<CstModelSalesOrderSC>();
                    List<gc_movimentos_itens> ListaItensMovimentoCompra = new List<gc_movimentos_itens>();
                    List<String> ListaColunas = new List<String>();
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                    var fileNameOrigem = Path.GetFileName(record_cstUploadFiles.FilesourceXLXS.FileName);

                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    var FileNameExcel = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_ExcelSC_" + fileNameOrigem);
                    record_cstUploadFiles.FilesourceXLXS.SaveAs(FileNameExcel);
                    //String FileNameExcel = record_cstUploadFiles.FilesourceXLXS.FileName.EmptyIfNull().ToString();
                    WorkBook = new XLWorkbook(FileNameExcel);

                    for (int Planilha = 1; Planilha <= WorkBook.Worksheets.Count(); Planilha++)
                    {
                        LeituraAtiva = false;
                        IXLWorksheet WorkSheet = WorkBook.Worksheet(Planilha);

                        for (int IndexRow = 1; IndexRow <= (WorkSheet.RowsUsed().Count()); IndexRow++)
                        {
                            if (WorkSheet.Row(IndexRow).Cell(1).Value.IsBlank == false)
                            {
                                ColunaCabecalho = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(1).Value);
                                if (ColunaCabecalho == "ITEM")
                                {
                                    for (int IndexCol = 1; IndexCol <= (WorkSheet.CellsUsed().Count()); IndexCol++)
                                    {
                                        ColunaCabecalho = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCol).Value);
                                        if (ColunaCabecalho == "ITEM") { IndexItem = IndexCol; }
                                        else if (ColunaCabecalho.IndexOf("PART NUMBER") == 0) { IndexDescription = IndexCol; }
                                        else if (ColunaCabecalho == "ORDERED") { IndexOrdered = IndexCol; }
                                        else if (ColunaCabecalho == "CD") { IndexCD = IndexCol; }
                                        else if (ColunaCabecalho == "QTY") { IndexQty = IndexCol; }
                                        else if (ColunaCabecalho == "T") { IndexT = IndexCol; }
                                        else if (ColunaCabecalho == "UOM") { IndexUOM = IndexCol; }
                                        else if (ColunaCabecalho == "UNIT PRICE") { IndexUnitPrice = IndexCol; }
                                        else if ((ColunaCabecalho == "TOTAL AMOUNT") || (ColunaCabecalho == "TOTAL PRICE") || (ColunaCabecalho.StartsWith("TOTAL")))  { IndexTotalPrice = IndexCol; }
                                    }
                                    LeituraAtiva = true;
                                }

                                if (LeituraAtiva == true)
                                {
                                    CstModelSalesOrderSC ItemSO = new CstModelSalesOrderSC();
                                    if (IndexItem >= 0) { ItemSO.String_Item = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexItem).Value); }
                                    if (IndexDescription >= 0) { ItemSO.String_Description = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDescription).Value); }
                                    if (IndexOrdered >= 0) { ItemSO.String_Ordered = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexOrdered).Value); }
                                    if (IndexCD >= 0) { ItemSO.String_CD = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCD).Value); }
                                    if (IndexQty >= 0) { ItemSO.String_Qty = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexQty).Value); }
                                    if (IndexT >= 0) { ItemSO.String_T = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexT).Value); }
                                    if (IndexUOM >= 0) { ItemSO.String_UOM = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexUOM).Value); }
                                    if (IndexUnitPrice >= 0) { ItemSO.String_UnitPrice = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexUnitPrice).Value); }
                                    if (IndexTotalPrice >= 0) { ItemSO.String_TotalPrice = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexTotalPrice).Value); }
                                    if (ItemSO.IsValidItem())
                                    {
                                        ListaItensSO.Add(ItemSO);
                                    }
                                }
                            }
                        }
                    }

                    if (ListaItensSO.Count > 0)
                    {
                        List<gc_comex_produtos> ListaComexProdutos = db.gc_comex_produtos.Where(p => p.ativo == true).ToList();
                        List<g_produtos> ListaProdutosGDI = db.g_produtos.Where(p => p.ativo == true).ToList();
                        foreach (var ItemSO in ListaItensSO)
                        {
                            if ((ItemSO.Int_Ordered >= 0) || (ItemSO.Int_Qty >= 0))
                            {
                                g_produtos record_g_produtos = ListaProdutosGDI.Where(p => p.codigo == ItemSO.String_PN).FirstOrDefault();
                                gc_comex_produtos record_gc_comex_produtos = ListaComexProdutos.Where(p => p.pn == ItemSO.String_PN).FirstOrDefault();
                                gc_movimentos_itens NovoItemMovimentoCompra = new gc_movimentos_itens();
                                NovoItemMovimentoCompra.id_movimento = int.Parse((CachePersister.userIdentity.IdUsuario * -1).ToString()); // O Id, será o negativo do id do usuário;
                                NovoItemMovimentoCompra.id_produto_condicao = 1;
                                NovoItemMovimentoCompra.id_entrega_prazo = 1;
                                NovoItemMovimentoCompra.sequencia = 0;
                                NovoItemMovimentoCompra.valor_unit_corecharge = 0;
                                NovoItemMovimentoCompra.valor_total_corecharge = 0;

                                int IdProdutoGDI = 0;
                                if (record_g_produtos != null) { IdProdutoGDI = record_g_produtos.id_produto; };
                                if (record_gc_comex_produtos == null)
                                {
                                    gc_comex_produtos new_record_gc_comex_produtos = new gc_comex_produtos();
                                    new_record_gc_comex_produtos.id_produto = IdProdutoGDI;
                                    new_record_gc_comex_produtos.ativo = true;
                                    new_record_gc_comex_produtos.pn = ItemSO.String_PN;
                                    new_record_gc_comex_produtos.pn_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemSO.String_PN);
                                    new_record_gc_comex_produtos.pn_variacao1 = new_record_gc_comex_produtos.pn_auxiliar.Replace("0", "O");
                                    new_record_gc_comex_produtos.pn_variacao2 = new_record_gc_comex_produtos.pn_auxiliar.Replace("O", "0");
                                    new_record_gc_comex_produtos.description = ItemSO.String_Description;
                                    new_record_gc_comex_produtos.fob1_dollar = Math.Round(ItemSO.Decimal_UnitPrice);
                                    new_record_gc_comex_produtos.id_coligada = 0;
                                    new_record_gc_comex_produtos.id_filial = 0;
                                    new_record_gc_comex_produtos.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                                    new_record_gc_comex_produtos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                    db.gc_comex_produtos.Add(new_record_gc_comex_produtos);
                                    db.SaveChanges();
                                    LibAudit.SaveAudit(db, true,"gc_comex_produtos", new_record_gc_comex_produtos.id_comex_produto, "Novo Produto Comex | Upload xlxs sc");
                                    QtdNovosProdutosCadastrados += 1;
                                    
                                    if (IdProdutoGDI > 0)
                                    {
                                        QtdProdutosVinculados += 1;
                                        LibAudit.SaveAudit(db, true,"gc_comex_produtos", new_record_gc_comex_produtos.id_comex_produto, "Vinculação ao Produto ERP | id: " + IdProdutoGDI.ToString());
                                    }
                                    else
                                    {
                                        QtdProdutosNaoVinculados += 1;
                                    }

                                    NovoItemMovimentoCompra.id_produto = new_record_gc_comex_produtos.id_produto;
                                    NovoItemMovimentoCompra.quantidade = ItemSO.Int_Qty;
                                    NovoItemMovimentoCompra.valor_unit = Math.Round(ItemSO.Decimal_UnitPrice, 2) * 2;
                                    NovoItemMovimentoCompra.valor_total = NovoItemMovimentoCompra.quantidade * NovoItemMovimentoCompra.valor_unit;
                                    ListaItensMovimentoCompra.Add(NovoItemMovimentoCompra);
                                }
                                else
                                {
                                    NovoItemMovimentoCompra.id_produto = record_gc_comex_produtos.id_produto;
                                    NovoItemMovimentoCompra.quantidade = ItemSO.Int_Qty;
                                    NovoItemMovimentoCompra.valor_unit = Math.Round(ItemSO.Decimal_UnitPrice, 2) * 2;
                                    NovoItemMovimentoCompra.valor_total = NovoItemMovimentoCompra.quantidade * NovoItemMovimentoCompra.valor_unit;

                                    if (record_gc_comex_produtos.id_produto == 0)
                                    {
                                        if (IdProdutoGDI > 0)
                                        {
                                            NovoItemMovimentoCompra.id_produto = IdProdutoGDI;
                                            record_gc_comex_produtos.id_produto = IdProdutoGDI;
                                            record_gc_comex_produtos.datahora_alteracao = DataHoraAtual;
                                            record_gc_comex_produtos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                            db.Entry(record_gc_comex_produtos).State = EntityState.Modified;
                                            db.SaveChanges();
                                            QtdProdutosVinculados += 1;
                                            LibAudit.SaveAudit(db, true,"gc_comex_produtos", record_gc_comex_produtos.id_comex_produto, "Vinculação ao Produto ERP | id: " + IdProdutoGDI.ToString());
                                        }
                                        else
                                        {
                                            QtdProdutosNaoVinculados += 1;
                                            NovoItemMovimentoCompra.id_produto = IdProdutoGDI;
                                        }

                                    }
                                    ListaItensMovimentoCompra.Add(NovoItemMovimentoCompra);
                                }
                            }
                        }

                        if (QtdProdutosNaoVinculados == 0)
                        {
                            QtdItensImportados = 0;
                            foreach (var NovoItemMovimentoCompra in ListaItensMovimentoCompra)
                            {
                                QtdItensImportados += 1;
                                NovoItemMovimentoCompra.sequencia = QtdItensImportados;
                                db.Entry(NovoItemMovimentoCompra).State = EntityState.Added;
                                db.SaveChanges();
                            }
                            Processado = true;
                            MsgRetorno += "Arquivo Processado com Sucesso" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                            MsgRetorno += QtdItensImportados.ToString() + LibStringFormat.GetTabHtml(1) + "Itens(s) Importados para o movimento" + "<br/><br/>";
                            if (QtdProdutosCadastrados > 0) { MsgRetorno += QtdProdutosCadastrados.ToString() + LibStringFormat.GetTabHtml(1) + "Novos Produtos(s) Cadastrados" + "<br/>"; };
                            if (QtdProdutosVinculados > 0) { MsgRetorno += QtdProdutosVinculados.ToString() + LibStringFormat.GetTabHtml(1) + "Produtos(s) Novos Vinculados aos produtos GDI" + "<br/>"; };
                            if (QtdProdutosNaoVinculados > 0) { MsgRetorno += QtdProdutosNaoVinculados.ToString() + LibStringFormat.GetTabHtml(1) + "Produtos(s) Novos NÃO Vinculados aos produtos GDI" + "<br/>"; };
                        }
                        else
                        {
                            Processado = false;
                            MsgRetorno += "Arquivo Não Processado" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "") + "<br/><br/>";
                            if (QtdProdutosCadastrados > 0) { MsgRetorno += QtdProdutosCadastrados.ToString() + LibStringFormat.GetTabHtml(1) + "Novos Produtos(s) Cadastrados" + "<br/>"; };
                            if (QtdProdutosVinculados > 0) { MsgRetorno += QtdProdutosVinculados.ToString() + LibStringFormat.GetTabHtml(1) + "Produtos(s) Novos Vinculados aos produtos GDI" + "<br/>"; };
                            if (QtdProdutosNaoVinculados > 0) { MsgRetorno += QtdProdutosNaoVinculados.ToString() + LibStringFormat.GetTabHtml(1) + "Produtos(s) Novos NÃO Vinculados aos produtos GDI" + "<br/>"; };
                            MsgRetorno += "<br/>";
                            MsgRetorno += "Obs: Foram indentificados " + QtdProdutosNaoVinculados + " Novos Produtos sem a vinculação ao cadastro de Produtos Principal GDI, execute a conferência/vinculação dos novos produtos no menu [Cadastros Comercial > Produtos (Novos)]!";
                        }
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    Processado = false;
                    MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
                    try { System.IO.File.Delete(FileNameXlsxUpload); } catch { };
                }
                catch (Exception e)
                {
                    Processado = false;
                    MsgRetorno = LibExceptions.getExceptionShortMessage(e);
                    try { System.IO.File.Delete(FileNameXlsxUpload); } catch { };
                }
            }
            return Json(new { success = Processado, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region CadastrarProdutoNCM
        public g_produtos CadastrarNovoProduto(CstModelSalesOrderSC ItemSO)
        {
            g_produtos record_g_produto = new g_produtos();
            record_g_produto.id_produto_substituto = 0;
            record_g_produto.ativo = true;
            record_g_produto.bloqueado = false;
            record_g_produto.importado = true;
            record_g_produto.id_produto_tipo = 1;
            record_g_produto.id_produto_ncm = 0;
            record_g_produto.id_icms_cst = 0;
            record_g_produto.icms_isento_uf = false;
            record_g_produto.is_servico = false;
            record_g_produto.has_corecharge = true;
            record_g_produto.nome = ItemSO.String_Description;
            record_g_produto.descricao = ItemSO.String_Description;
            record_g_produto.codigo = LibStringFormat.ClienteGDIGetPartNumber(record_g_produto.nome);
            record_g_produto.codigo_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(record_g_produto.codigo);
            record_g_produto.codigo = "";
            record_g_produto.codigo_auxiliar = "";
            record_g_produto.valor_base = ItemSO.Decimal_UnitPrice;
            record_g_produto.controla_estoque = true;
            record_g_produto.id_unidade_medida_compra = 2;
            record_g_produto.id_unidade_medida_venda = 2;
            record_g_produto.fator_conversao = 1;
            //record_g_produto.custo_medio = ItemSO.Decimal_UnitPrice;
            //record_g_produto.custo_ultima_entrada = ItemSO.Decimal_UnitPrice;
            record_g_produto.preco_venda = ItemSO.Decimal_UnitPrice * 2;
            record_g_produto.peso = 0;
            record_g_produto.id_produto_grupo = 1;
            record_g_produto.id_coligada = 0;
            record_g_produto.id_filial = 0;
            record_g_produto.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
            record_g_produto.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
            db.g_produtos.Add(record_g_produto);
            db.SaveChanges();
            return record_g_produto;
        }
        #endregion

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = yesFilterOnOff ?? "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
    }
}