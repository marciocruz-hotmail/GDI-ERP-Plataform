using ClosedXML.Excel;
using GdiPlataform.Areas.g.Controllers;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Areas.gc.Reports;
using GdiPlataform.Areas.gc.Services;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Robos;
using GdiPlataform.Robos.Aws;
using GdiPlataform.Robos.CotacaoDolar;
using GdiPlataform.Robos.ENotas;
using GdiPlataform.Robos.Whatsapp;
using GdiPlataform.Security;
using MathNet.Numerics;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Script.Serialization;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Linq;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Movimentos_*,gc_Movimentos_Default")]
    public class MovimentosController : Controller
    {
        public string MsgGeral = string.Empty;

        private GdiPlataformEntities db;
        public MovimentosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region Pedido - Create/Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Movimentos_*,gc_Movimentos_IndexPedido")]
        public ActionResult CreateCotacao()
        {
            return CreateCotacaoPedidoOS("Nova Cotação", "Cotação", 3);
        }   

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Movimentos_*,gc_Movimentos_IndexPedido")]
        public ActionResult CreatePedido()
        {
            return CreateCotacaoPedidoOS("Novo Pedido", "Pedido", 4);
        }
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Movimentos_*,gc_Movimentos_IndexPedido")]
        public ActionResult CreateOS()
        {
            return CreateCotacaoPedidoOS("Nova Ordem de Serviço", "Ordem de Serviço", 8);
        }

        public ActionResult CreateCotacaoPedidoOS(String Titulo, String TipoSolicitacao, int IdMovimentoTipo)
        {
            DeleteItemTemporario();
            CachePersister.userIdentity.FormNameActive = "GcMovimentosFormPedidoCreate";
            gc_movimentos record_gc_movimento = new Db.gc_movimentos();
            record_gc_movimento.cotacao_dolar_venda = SetCotacaoDollarDia();
            record_gc_movimento.cotacao_dolar_oficial_venda = record_gc_movimento.cotacao_dolar_venda;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>" + Titulo + "</b>";
            ViewBag.tipoSolicitacao = TipoSolicitacao;
            record_gc_movimento.id_movimento_tipo = IdMovimentoTipo;
            record_gc_movimento.id_movimento_status = 1; // Aberto
            record_gc_movimento.data_vencimento = DateTime.Now.AddDays(10);
            record_gc_movimento.param_reducao_bc = false;
            record_gc_movimento.param_zerar_difal = false;
            record_gc_movimento.has_beneficio_aviacao = false;
            record_gc_movimento.id_vendedor = -1;
            record_gc_movimento.id_local_estoque = -1;
                if (CachePersister.userIdentity.GcParamLocalEstoquePedidos.EmptyIfNull().ToString() == "1") { record_gc_movimento.id_local_estoque = 1; }
                else if (CachePersister.userIdentity.GcParamLocalEstoquePedidos.EmptyIfNull().ToString() == "3") { record_gc_movimento.id_local_estoque = 3; }
            record_gc_movimento.id_cfop_finalidade = 1; // Padrão é consumidor final
            record_gc_movimento.id_frete_responsavel = 0;
            record_gc_movimento.frete2_transportadora = -1;
            record_gc_movimento.id_cliente_destinatario = 0;
            record_gc_movimento.id_cfop_operacao = 0;
            if (CachePersister.userIdentity.IdVendedor > 0) { record_gc_movimento.id_vendedor = CachePersister.userIdentity.IdVendedor; }; // Set Vendedor logado            
            ViewBag.idMovimento = (CachePersister.userIdentity.IdUsuario * -1).ToString(); // O Id, será o negativo do id do usuário;
            ViewBag.comboClientes = LibDataSets.LoadComboSomenteGClientes(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]" });
            ViewBag.comboTiposMovimentosCreateEdit = LibDataSets.LoadComboGcTiposMovimentosCreateEdit(db);
            ViewBag.comboClientesContatos = LibDataSets.LoadComboGcClientesContatos(db, 0);
            ViewBag.dataSetClientesContatos = LibDataSets.LoadDatasetGcClientesContatos(db);
            ViewBag.comboLocaisEstoque = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
            ViewBag.comboLocaisEstoqueOrders = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
            ViewBag.comboVendedores = LibDataSets.LoadComboGVendedores(db);
            ViewBag.comboTransportadora = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar.Insert(0, new SelectListItem { Value = "-1", Text = "[ SEM FRETE COMPLEMENTAR ]" });
            ViewBag.comboMovimentosPosicao = LibDataSets.LoadComboGcMovimentosPosicao(db);
            ViewBag.comboCfopFinalidade = LibDataSets.LoadComboGcCfopFinalidade(db);
            ViewBag.comboFreteResponsavel = LibDataSets.LoadComboGcFreteResponsavel(db);
            ViewBag.ComboComexImportacoes = LibDataSets.LoadComboGcComexImportacoesTodas(db);
            ViewBag.comboClienteDestinatarios = new List<SelectListItem>();
            ViewBag.comboFreteResponsavel.Insert(0, new SelectListItem { Value = "-1", Text = "[ Frete ]" });
            LibDataSets.LoadDatasetGVendedores(db);
            ViewBag.comboMoedas = LibDataSets.LoadComboGMoedas(db);
            ViewBag.comboPagRecCondicoes = LibDataSets.LoadComboPagRecCondicoesTodas(db);
            ViewBag.comboPagRecCondicoes.Insert(0, new SelectListItem { Value = "-1", Text = "[ Condição Pagto. ]" });
            ViewBag.comboGcCfopOperacao = LibDataSets.LoadComboGcCfopOperacoesTelaPedido(db);
            ViewBag.MsgInfo = String.Empty;
            return View("FormPedidoCreate", record_gc_movimento);
        }


        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Movimentos_*,gc_Movimentos_IndexPedido")]
        public ActionResult EditPedido(int? id)
        {
            CachePersister.userIdentity.FormNameActive = "GcMovimentosFormPedidoCreate";
            String MsgInfo = string.Empty;
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            if (record_gc_movimento == null)
            {
                return RedirectToAction("Index");
            }
            else
            {
                if (record_gc_movimento.cotacao_dolar_venda <= 0) { record_gc_movimento.cotacao_dolar_venda = SetCotacaoDollarDia(); };
                string tipoSolicitacao = String.Empty;
                if (record_gc_movimento.id_movimento_tipo == 3) { tipoSolicitacao = LibIcons.getIcon("fa-solid fa-clipboard-list", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Alteração de Cotação - Nº " + record_gc_movimento.id_movimento.ToString() + "</b>"; }
                else if (record_gc_movimento.id_movimento_tipo == 4) { tipoSolicitacao = LibIcons.getIcon("fa-solid fa-boxes", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Alteração de Pedido - Nº " + record_gc_movimento.id_movimento.ToString() + "</b>"; }
                else if (record_gc_movimento.id_movimento_tipo == 8) { tipoSolicitacao = LibIcons.getIcon("fa-solid fa-tools", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Alteração de OS - Nº " + record_gc_movimento.id_movimento.ToString() + "</b>"; }
                else if (record_gc_movimento.id_movimento_tipo == 19) { tipoSolicitacao = LibIcons.getIcon("fa-solid fa-tools", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Transferência Filiais - Nº " + record_gc_movimento.id_movimento.ToString() + "</b>"; };
                int QtdNotasAtivas = LibDB.dbQueryCount("select count(*) from gc_movimentos_nf nf left join g_nfe_status status on (status.id_nfe_status = nf.id_nfe_status) where nf.id_movimento = " + record_gc_movimento.id_movimento.ToString() + " and status.nf_ativa = 1", db);
                if (QtdNotasAtivas > 0) { record_gc_movimento.movimento_nf = true; } else { record_gc_movimento.movimento_nf = false; }
                ViewBag.Title = tipoSolicitacao;
                ViewBag.idMovimento = record_gc_movimento.id_movimento.ToString();
                if (record_gc_movimento.param_reducao_bc == true) { record_gc_movimento.has_beneficio_aviacao = true; } else { record_gc_movimento.has_beneficio_aviacao = false; }
                if (record_gc_movimento.id_movimento_status == 1) // Aberto
                {
                    Decimal CotacaoDolarDia = SetCotacaoDollarDia();
                    if (record_gc_movimento.cotacao_dolar_oficial_venda != CotacaoDolarDia) { record_gc_movimento.cotacao_dolar_oficial_venda = CotacaoDolarDia; };
                }
                ViewBag.comboClientes = LibDataSets.LoadComboSomenteGClientes(db);
                ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]" });
                ViewBag.comboTiposMovimentosCreateEdit = LibDataSets.LoadComboGcTiposMovimentosCreateEdit(db);
                ViewBag.comboClientesContatos = LibDataSets.LoadComboGcClientesContatos(db, record_gc_movimento.id_cliente);
                ViewBag.dataSetClientesContatos = LibDataSets.LoadDatasetGcClientesContatos(db);
                ViewBag.comboLocaisEstoque = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
                ViewBag.comboLocaisEstoqueOrders = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
                ViewBag.comboVendedores = LibDataSets.LoadComboGVendedores(db);
                ViewBag.comboTransportadora = LibDataSets.LoadComboGcTransportadora(db);
                ViewBag.comboTransportadoraComplementar = LibDataSets.LoadComboGcTransportadora(db);
                ViewBag.comboTransportadoraComplementar.Insert(0, new SelectListItem { Value = "-1", Text = "[ SEM FRETE COMPLEMENTAR ]" });
                ViewBag.comboCfopFinalidade = LibDataSets.LoadComboGcCfopFinalidade(db);
                LibDataSets.LoadDatasetGVendedores(db);
                ViewBag.comboMoedas = LibDataSets.LoadComboGMoedas(db);
                ViewBag.comboPagRecCondicoes = LibDataSets.LoadComboPagRecCondicoesTodas(db);
                ViewBag.comboGcCfopOperacao = LibDataSets.LoadComboGcCfopOperacoesTelaPedido(db);
                ViewBag.comboFreteResponsavel = LibDataSets.LoadComboGcFreteResponsavel(db);
                ViewBag.comboFreteResponsavel.Insert(0, new SelectListItem { Value = "-1", Text = "[ Frete ]" });
                ViewBag.comboClienteDestinatarios = LibDataSets.LoadComboGcClientesDestinatarios(db, record_gc_movimento.id_cliente);
                ViewBag.ComboComexImportacoes = LibDataSets.LoadComboGcComexImportacoesTodas(db);
                if (record_gc_movimento.id_movimento_posicao > 0) { MsgInfo = "<b>Atenção</b> Pedido somente para visualização!"; };
                ViewBag.MsgInfo = MsgInfo;
                return View("FormPedidoCreate", record_gc_movimento);
            }
        }
        public void DeleteItemTemporario()
        {
            try
            {
                String SqlDelete = " delete from  gc_movimentos_itens where id_movimento = " + (CachePersister.userIdentity.IdUsuario * -1).ToString(); // O Id, será o negativo do id do usuário;
                DataTable tableRegistroDeleteExtratos = LibDB.GetDataTable(SqlDelete, db);
            }
            catch (Exception) { };
        }
        public decimal SetCotacaoDollarDia()
        {
            Decimal CotacaoDolarDia = CachePersister.userIdentity.CotacaoDollarDia;
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia().Date;
            g_cotacoes record_g_cotacoes = null;  
            try
            {
                if (CotacaoDolarDia == 0)
                {
                    RoboCotacaoDolar BotCotacaoDolar = new RoboCotacaoDolar();
                    //CotacaoDolarDia = Convert.ToDecimal(BotCotacaoDolar.GetCotacaoDolarDia());
                    CotacaoDolarDia = BotCotacaoDolar.GetCotacaoDolarDia();
                    if (CotacaoDolarDia > 0)
                    {
                        try
                        {
                            record_g_cotacoes = db.g_cotacoes.Where(c => c.id_moeda == 2 && c.cotacao_data == DataAtual).FirstOrDefault();
                            if (record_g_cotacoes == null)
                            {
                                record_g_cotacoes = new g_cotacoes();
                                record_g_cotacoes.id_moeda = 2;
                                record_g_cotacoes.cotacao_data = DataAtual;
                                record_g_cotacoes.cotacao_ultimo_valor = CotacaoDolarDia;
                                record_g_cotacoes.cotacao_menor_valor = CotacaoDolarDia;
                                record_g_cotacoes.cotacao_maior_valor = CotacaoDolarDia;
                                db.g_cotacoes.Add(record_g_cotacoes);
                                db.SaveChanges();
                            }
                            else
                            {
                                record_g_cotacoes.cotacao_ultimo_valor = CotacaoDolarDia;
                                if (CotacaoDolarDia < record_g_cotacoes.cotacao_menor_valor) { record_g_cotacoes.cotacao_menor_valor = CotacaoDolarDia; };
                                if (CotacaoDolarDia > record_g_cotacoes.cotacao_maior_valor) { record_g_cotacoes.cotacao_maior_valor = CotacaoDolarDia; };
                                db.Entry(record_g_cotacoes).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                        }
                        catch (Exception) { };
                    }
                    CachePersister.userIdentity.CotacaoDollarDia = CotacaoDolarDia;
                }
            }
            catch (Exception) 
            { 
            
            };
            return CotacaoDolarDia;
        }


        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Movimentos_*,gc_Movimentos_IndexPedido")]
        public ActionResult IndexPedido()
        {
            CachePersister.userIdentity.FormNameActive = "GcMovimentosIndexPedido";
            ViewBag.comboClientes = LibDataSets.LoadComboSomenteGClientes(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]" });
            ViewBag.comboTiposMovimentos = LibDataSets.LoadComboGcTiposMovimentosVendas(db);
            ViewBag.comboStatusMovimentos = LibDataSets.LoadComboGcStatusMovimentos(db);
            ViewBag.comboMovimentosPosicao = LibDataSets.LoadComboGcMovimentosPosicao(db);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-clipboard-list", "", "#008000", "fa-lg") + "&nbsp;Cotações" + LibStringFormat.GetTabHtml(2) + LibIcons.getIcon("fa-solid fa-boxes", "", "#008000", "fa-lg") + "&nbsp;Pedidos" + LibStringFormat.GetTabHtml(2) + LibIcons.getIcon("fa-solid fa-tools", "", "#008000", "fa-lg") + "&nbsp;OS";
            return View();
        }

        public ActionResult GetDadosPedidos(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            string errorMessage = "";
            string stackTrace = "";
            try
            {
                // Parse filtros
                DateTime dataIni, dataFim;
                DateTime.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out dataIni);
                DateTime.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out dataFim);
                string termo = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string idClienteStr = param.yesCustomField04.EmptyIfNull().ToString().Trim();
                string posicaoStr = param.yesCustomField05.EmptyIfNull().ToString().Trim();
                string valorStr = param.yesCustomField06.EmptyIfNull().ToString().Trim();
                string statusStr = param.yesCustomField07.EmptyIfNull().ToString().Trim();

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // Base query (somente tipos do módulo)
                var movimentos = db.gc_movimentos.AsNoTracking().Where(m => m.id_movimento_tipo == 3 || m.id_movimento_tipo == 4 || m.id_movimento_tipo == 8 || m.id_movimento_tipo == 19);

                // -------- Filtro: termo (id_movimento OU nf_numero) --------
                if (!string.IsNullOrWhiteSpace(termo))
                {
                    filterOnOff = "1";
                    string termoLimpo = termo.StartsWith("*") ? termo.Substring(1) : termo;

                    int idMov = 0;
                    int.TryParse(termoLimpo, out idMov);

                    movimentos = movimentos.Where(m =>
                        (idMov > 0 && m.id_movimento == idMov) ||
                        db.gc_movimentos_nf.Any(nf => nf.id_movimento == m.id_movimento && nf.nf_numero == termoLimpo)
                    );
                }
                else
                {
                    // -------- Filtro: período --------
                    if (!string.IsNullOrWhiteSpace(param.yesCustomField02) && !string.IsNullOrWhiteSpace(param.yesCustomField03))
                    {
                        filterOnOff = "1";
                        var dtIni = dataIni.Date;
                        var dtFim = dataFim.Date.AddDays(1).AddTicks(-1);

                        movimentos = movimentos.Where(m =>
                            (m.datahora_cadastro >= dtIni && m.datahora_cadastro <= dtFim) ||
                            (m.datahora_alteracao != null && m.datahora_alteracao >= dtIni && m.datahora_alteracao <= dtFim) ||
                            (m.datahora_aprovacao != null && m.datahora_aprovacao >= dtIni && m.datahora_aprovacao <= dtFim) ||
                            (m.data_vencimento != null && m.data_vencimento >= dataIni.Date && m.data_vencimento <= dataFim.Date)
                        );
                    }

                    // -------- Filtro: cliente --------
                    if (int.TryParse(idClienteStr, out int idCliente) && idCliente > 0)
                    {
                        filterOnOff = "1";
                        movimentos = movimentos.Where(m => m.id_cliente == idCliente);
                    }

                    // -------- Filtro: posição --------
                    if (int.TryParse(posicaoStr, out int idPosicao) && idPosicao > 0)
                    {
                        filterOnOff = "1";
                        movimentos = movimentos.Where(m => m.id_movimento_posicao == idPosicao);
                    }

                    // -------- Filtro: valor --------
                    if (!string.IsNullOrWhiteSpace(valorStr))
                    {
                        if (decimal.TryParse(valorStr, NumberStyles.Any, new CultureInfo("pt-BR"), out decimal valor) ||
                            decimal.TryParse(valorStr, NumberStyles.Any, CultureInfo.InvariantCulture, out valor))
                        {
                            filterOnOff = "1";
                            movimentos = movimentos.Where(m => m.valor_total_bruto == valor || m.valor_total_produtos == valor);
                        }
                    }

                    // -------- Filtro: status --------
                    if (int.TryParse(statusStr, out int idStatus) && idStatus > 0)
                    {
                        filterOnOff = "1";
                        movimentos = movimentos.Where(m => m.id_movimento_status == idStatus);
                    }
                }

                // -------- Regra: grupo vendedor --------
                string grp = CachePersister.userIdentity.GcParamGrupoVendedor.EmptyIfNull().ToString().Trim();

                if (grp == "0")
                {
                    movimentos = movimentos.Where(m => m.id_vendedor == 0);
                }
                else if (grp == "99999")
                {
                    movimentos = movimentos.Where(m => m.id_vendedor > 0);
                }
                else
                {
                    var ids = grp.Split(',')
                        .Select(s => s.Trim())
                        .Select(s => int.TryParse(s, out var x) ? (int?)x : null)
                        .Where(x => x.HasValue)
                        .Select(x => x.Value)
                        .Distinct()
                        .ToList();

                    if (ids.Count == 0) movimentos = movimentos.Where(m => false);
                    else movimentos = movimentos.Where(m => ids.Contains(m.id_vendedor));
                }

                // Totais
                int totalRecords = movimentos.Count();
                int totalDisplayRecords = totalRecords;

                IOrderedQueryable<Db.gc_movimentos> ordered = movimentos.OrderByDescending(m => m.id_movimento);

                // ✅ Pagina primeiro
                var pageMov = ordered
                    .Skip(start)
                    .Take(length)
                    .Select(m => new
                    {
                        m.id_movimento,
                        m.id_movimento_tipo,
                        m.id_movimento_status,
                        m.movimento_cancelado,
                        m.movimento_aprovado,
                        m.movimento_separado,
                        m.movimento_faturado,
                        m.movimento_nf,
                        m.movimento_nf_autorizada,
                        m.movimento_notificado,
                        m.movimento_expedido,
                        m.movimento_entregue,
                        m.movimento_posvenda,
                        m.datahora_cadastro,
                        m.datahora_aprovacao,
                        m.fornecedor_cotacao_solicitar,
                        m.fornecedor_cotacao_solicitada,
                        m.fornecedor_cotacao_respondida,
                        m.fornecedor_cotacao_aprovada,
                        m.data_vencimento,
                        m.qtd_itens,
                        m.valor_total_bruto,
                        m.valor_total_produtos,
                        m.id_moeda,
                        m.id_cliente,
                        m.id_vendedor,
                        m.id_local_estoque,
                        m.id_cfop_operacao
                    })
                    .ToList();

                // Lookups somente para IDs da página
                var idsClientes = pageMov.Select(x => x.id_cliente).Distinct().ToList();
                var idsVendedores = pageMov.Select(x => x.id_vendedor).Distinct().ToList();
                var idsLocais = pageMov.Select(x => x.id_local_estoque).Distinct().ToList();
                var idsOperacoes = pageMov.Select(x => x.id_cfop_operacao).Distinct().ToList();

                var clientes = db.g_clientes.AsNoTracking()
                    .Where(c => idsClientes.Contains(c.id_cliente))
                    .Select(c => new { c.id_cliente, c.nome })
                    .ToList()
                    .ToDictionary(x => x.id_cliente, x => x.nome);

                var vendedores = db.g_vendedores.AsNoTracking()
                    .Where(v => idsVendedores.Contains(v.id_vendedor))
                    .Select(v => new { v.id_vendedor, v.apelido })
                    .ToList()
                    .ToDictionary(x => x.id_vendedor, x => x.apelido);

                var locais = db.gc_locais_estoque.AsNoTracking()
                    .Where(le => idsLocais.Contains(le.id_local_estoque))
                    .Select(le => new { le.id_local_estoque, le.sigla })
                    .ToList()
                    .ToDictionary(x => x.id_local_estoque, x => x.sigla);

                var operacoes = db.gc_cfop_operacoes.AsNoTracking()
                    .Where(op => idsOperacoes.Contains(op.id_cfop_operacao))
                    .Select(op => new
                    {
                        op.id_cfop_operacao,
                        op.descricao_tv_monitor,
                        op.is_venda,
                        op.is_remessa,
                        op.is_devolucao,
                        op.is_servico,
                        op.is_baixa,
                        op.has_aprovacao,
                        op.has_separacao,
                        op.has_financeiro,
                        op.has_nfe,
                        op.has_notifica_email,
                        op.has_expedicao,
                        op.has_entrega,
                        op.has_posvenda
                    })
                    .ToList()
                    .ToDictionary(x => x.id_cfop_operacao);

                // Montagem do aaData
                var list = new List<string[]>(pageMov.Count);

                foreach (var m in pageMov)
                {
                    DateTime dataHoraPedido = m.datahora_cadastro;
                    if (m.datahora_aprovacao != null) dataHoraPedido = m.datahora_aprovacao.Value;

                    string corIcone;
                    string tipoMovimento;

                    if (m.movimento_cancelado)
                    {
                        corIcone = "#cc0000";
                        tipoMovimento =
                            m.id_movimento_tipo == 3 ? "Orçamento Cancelado" :
                            m.id_movimento_tipo == 4 ? "Pedido Cancelado" :
                            m.id_movimento_tipo == 8 ? "OS Cancelada" :
                            "Transferência Cancelada";
                    }
                    else
                    {
                        corIcone = (m.id_movimento_status == 1) ? "#CACFD2" : "#008000";
                        tipoMovimento =
                            m.id_movimento_tipo == 3 ? "Orçamento" :
                            m.id_movimento_tipo == 4 ? "Pedido" :
                            m.id_movimento_tipo == 8 ? "OS" :
                            "Transferência";
                    }

                    string iconeTipo = "";
                    if (m.id_movimento_tipo == 3) iconeTipo = LibIcons.getIcon("fa-solid fa-clipboard-list", "Cotação", corIcone, "");
                    else if (m.id_movimento_tipo == 4) iconeTipo = LibIcons.getIcon("fa-solid fa-boxes", "Pedido", corIcone, "");
                    else if (m.id_movimento_tipo == 8) iconeTipo = LibIcons.getIcon("fa-solid fa-tools", "OS", corIcone, "");
                    else if (m.id_movimento_tipo == 19) iconeTipo = LibIcons.getIcon("fa-solid fa-truck-arrow-right", "Transferência", corIcone, "");

                    string iconeStatus = "";
                    if (m.id_movimento_status == 1) iconeStatus = LibIcons.getIcon("fa-solid fa-lock-open", "Aberto", corIcone, "");
                    else if (m.id_movimento_status == 2) iconeStatus = LibIcons.getIcon("fa-solid fa-lock", "Fechado", corIcone, "");
                    else if (m.id_movimento_status == 3) iconeStatus = LibIcons.getIcon("fa-solid fa-thumbs-down", "Cancelado", corIcone, "");

                    string iconeStatusPrazos = "";
                    if (m.id_movimento_status == 1)
                    {
                        DateTime hoje = LibDateTime.getDataHoraBrasilia().Date;
                        DateTime? venc = m.data_vencimento;

                        if (venc.HasValue)
                        {
                            string dv = venc.Value.ToString("dd/MM/yy");
                            string labelPrazos;

                            if (venc.Value.Date == hoje)
                            {
                                labelPrazos = $"{tipoMovimento} VENCENDO Hoje {dv}";
                                iconeStatusPrazos = LibIcons.getIcon("fa-solid fa-calendar-day", labelPrazos, "#ffbb00", "fa-lg");
                            }
                            else if (venc.Value.Date < hoje)
                            {
                                labelPrazos = $"{tipoMovimento} VENCIDO em {dv}";
                                iconeStatusPrazos = LibIcons.getIcon("fa-regular fa-calendar-times", labelPrazos, "#cc0000", "fa-lg");
                            }
                            else
                            {
                                labelPrazos = $"{tipoMovimento} a vencer em {dv}";
                                iconeStatusPrazos = LibIcons.getIcon("fa-regular fa-calendar-check", labelPrazos, "#008000", "fa-lg");
                            }
                        }
                    }
                    else if (m.id_movimento_status == 2)
                    {
                        string labelPrazos = $"{tipoMovimento} Fechado";
                        iconeStatusPrazos = LibIcons.getIcon("fa-solid fa-calendar-check", labelPrazos, "#008000", "fa-lg");
                    }

                    string iconeOperacao = "";
                    string iconePosicao = "";
                    string textoResumoOperacao = "";
                    string cor_cinza = "#CACFD2";
                    string cor_amarelo = "#8B8000";
                    string cor_laranja = "#ff9a00";
                    string cor_verde = "#008000";


                    if (operacoes.TryGetValue(m.id_cfop_operacao, out var op) && !string.IsNullOrWhiteSpace(op.descricao_tv_monitor))
                    {
                        textoResumoOperacao = op.descricao_tv_monitor;
                        int idx = textoResumoOperacao.IndexOf(" - ");
                        if (idx > 0) textoResumoOperacao = textoResumoOperacao.Substring(idx + 3);
                        textoResumoOperacao = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style='font-size: 75%;'>[ " + textoResumoOperacao.Trim() + " ]</span>";

                        if (op.is_venda) iconeOperacao = LibIcons.getIcon("fa-solid fa-cart-shopping", op.descricao_tv_monitor, corIcone, "");
                        else if (op.is_remessa) iconeOperacao = LibIcons.getIcon("fa-solid fa-truck-arrow-right", op.descricao_tv_monitor, corIcone, "");
                        else if (op.is_devolucao) iconeOperacao = LibIcons.getIcon("fa-solid fa-reply-all", op.descricao_tv_monitor, corIcone, "");
                        else if (op.is_servico) iconeOperacao = LibIcons.getIcon("fa-solid fa-wrench", op.descricao_tv_monitor, corIcone, "");
                        else if (op.is_baixa) iconeOperacao = LibIcons.getIcon("fa-solid fa-trash", op.descricao_tv_monitor, corIcone, "");

                        if (m.fornecedor_cotacao_aprovada == true) { iconePosicao += LibIcons.getIcon("fa-solid fa-boxes-packing", "Cotação Compra - Aprovada", cor_verde, "fa-xs"); }
                        else if (m.fornecedor_cotacao_respondida == true) { iconePosicao += LibIcons.getIcon("fa-solid fa-boxes-packing", "Cotação Compra - Respondida", cor_laranja, "fa-xs"); }
                        else if (m.fornecedor_cotacao_solicitada == true) { iconePosicao += LibIcons.getIcon("fa-solid fa-boxes-packing", "Cotação Compra - Solicitada", cor_amarelo, "fa-xs"); }
                        else if (m.fornecedor_cotacao_solicitar == true) { iconePosicao += LibIcons.getIcon("fa-solid fa-boxes-packing", "Cotação Compra - Solicitar", cor_amarelo, "fa-xs"); };

                        if (m.id_movimento_status == 2)
                        {
                            if (op.has_aprovacao) iconePosicao += LibIcons.getIcon("fa-solid fa-clipboard-check", m.movimento_aprovado ? "Aprovado" : "Não Aprovado", m.movimento_aprovado ? cor_verde : cor_cinza, "fa-xs") ;
                            if (op.has_separacao) iconePosicao += LibIcons.getIcon("fa-solid fa-dolly", m.movimento_separado ? "Separado" : "Não Separado", m.movimento_separado ? cor_verde : cor_cinza, "fa-xs") ;
                            if (op.has_financeiro) iconePosicao += LibIcons.getIcon("fa-solid fa-credit-card", m.movimento_faturado ? "Faturado" : "Não Faturado", m.movimento_faturado ? cor_verde : cor_cinza, "fa-xs");

                            if (op.has_nfe)
                            {
                                if (m.movimento_nf)
                                {
                                    iconePosicao += LibIcons.getIcon("fa-solid fa-file-invoice",
                                        m.movimento_nf_autorizada ? "NF Autorizada" : "NF Gerada",
                                        m.movimento_nf_autorizada ? cor_verde : cor_amarelo, "fa-xs") ;
                                }
                                else
                                {
                                    iconePosicao += LibIcons.getIcon("fa-solid fa-file-invoice", "NFe não emitida", cor_cinza, "fa-xs") ;
                                }
                            }

                            if (op.has_notifica_email) iconePosicao += LibIcons.getIcon("fa-solid fa-envelope-circle-check", m.movimento_notificado ? "Notificado" : "Não Notificado", m.movimento_notificado ? cor_verde : cor_cinza, "fa-xs");
                            if (op.has_expedicao) iconePosicao += LibIcons.getIcon("fa-solid fa-truck", m.movimento_expedido ? "Expedido" : "Não Expedido", m.movimento_expedido ? cor_verde : cor_cinza, "fa-xs");
                            if (op.has_entrega) iconePosicao += LibIcons.getIcon("fa-solid fa-people-carry", m.movimento_entregue ? "Entregue" : "Não Entregue", m.movimento_entregue ? cor_verde : cor_cinza, "fa-xs");
                            if (op.has_posvenda) { iconePosicao += LibIcons.getIcon("fa-solid fa-medal", m.movimento_posvenda ? "Pós-Venda Realizado!" : "Pós-Venda NÃO realizado!", m.movimento_posvenda ? cor_verde : cor_cinza, "fa-xs"); };
                        }

                        if (iconePosicao.EmptyIfNull().Trim().Length > 0) { iconePosicao = "<span style='display: inline-flex; align-items: center; justify-content: flex-start; gap: 2px;'>" + iconePosicao + "</span>"; };
                    }

                    string culturaMoeda = (m.id_moeda == 2) ? "en-US" : "pt-BR";
                    string valorFormatado = string.Format(CultureInfo.GetCultureInfo(culturaMoeda), "{0:C}", m.valor_total_bruto);
                    if (m.id_moeda == 2) valorFormatado = valorFormatado.Replace("$", "$ ");

                    string nomeCliente = clientes.TryGetValue(m.id_cliente, out var cn) ? cn : "";
                    nomeCliente += textoResumoOperacao;

                    string vendedorApelido = vendedores.TryGetValue(m.id_vendedor, out var va) ? va : "";
                    string localSigla = locais.TryGetValue(m.id_local_estoque, out var ls) ? ls : "";

                    list.Add(new[]
                    {
                        "", // seleção
                        m.id_movimento.ToString(),
                        iconeTipo,
                        iconeStatus,
                        iconeOperacao,
                        iconePosicao,
                        nomeCliente,
                        vendedorApelido,
                        dataHoraPedido.ToString("dd/MM/yy"),
                        iconeStatusPrazos,
                        m.qtd_itens.EmptyIfNull().ToString(),
                        localSigla,
                        valorFormatado,
                        "", // anexo
                        ""  // editar
                    });
                }

                return Json(new
                {
                    errorMessage = "",      // ✅ inclui sempre
                    stackTrace = "",        // opcional (deixe vazio em produção)
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException ex)
            {
                errorMessage = LibExceptions.getDbEntityValidationException(ex);
                stackTrace = ex.ToString();
            }
            catch (WebException ex)
            {
                errorMessage = LibExceptions.getWebException(ex);
                stackTrace = ex.ToString();
            }
            catch (Exception ex)
            {
                errorMessage = LibExceptions.getExceptionShortMessage(ex);
                stackTrace = ex.ToString();
            }

            // ✅ Retorna DataTables “vazio”, mas com errorMessage real
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error", // opcional: GdiDtNotifyJsonErrorMessage → LibMessageError (ícone erro)
                stackTrace = stackTrace, // se não quiser expor, retorne "" e faça log no servidor
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetGedPedido(jQueryDataTableParamModel param)
        {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            int IdTable = 0;
            int.TryParse(param.yesCustomIdPK, out IdTable);
            List<g_usuarios> ListaUsuarios = db.g_usuarios.Where(u => u.id_usuario > 0).ToList();
            List<ged_arquivos> ListaArquivosGed = db.ged_arquivos.Where(g => g.ativo == true && g.id_gc_movimento == IdTable).ToList();
            List<ged_arquivos_tipos> ListaArquivosGedTipos = db.ged_arquivos_tipos.Where(t => t.id_arquivo_tipo > 0).ToList();
            List<string[]> list = new List<string[]>();

            var displayedRecords = ListaArquivosGed.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.ged_arquivos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_arquivo) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.filename :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.descricao :
                                     "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            foreach (var RecordGed in displayedRecords)
            {
                String DataReferencia = String.Empty;
                String DescricaoTipoArquivo = String.Empty;
                String NomeUsuario = ListaUsuarios.Where(u => u.id_usuario == RecordGed.id_usuario_cadastro).FirstOrDefault().login.EmptyIfNull().ToString();
                if (RecordGed.datahora_cadastro != null) { DataReferencia = RecordGed.datahora_cadastro.GetValueOrDefault().ToString("dd/MM/yy"); };
                if (RecordGed.id_arquivo_tipo > 0)
                {
                    ged_arquivos_tipos RecordArquivoTipo = ListaArquivosGedTipos.Where(t => t.id_arquivo_tipo == RecordGed.id_arquivo_tipo).FirstOrDefault();
                    if (RecordArquivoTipo != null) { DescricaoTipoArquivo = RecordArquivoTipo.descricao.EmptyIfNull().ToString(); };
                }

                list.Add(new[] {
                                    RecordGed.id_arquivo.ToString(),
                                    "", // Botão Desativar
                                    DescricaoTipoArquivo.ToString(),
                                    RecordGed.descricao.ToString(),
                                    RecordGed.filename.ToString(),
                                    DataReferencia,
                                    NomeUsuario,
                                    "" // Botão Download
                                });
            }

            String filterOnOff = "0";
            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; };

            return Json(new
            {
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = ListaArquivosGed.Count(),
                iTotalDisplayRecords = ListaArquivosGed.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult AjaxVisualizarPedido(gc_movimentos view_record_gc_movimentos) // Criar movimento temporário somente para visualização
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            int qtdInconsistencias = 0;
            decimal valorTotalMovimento = 0;
            decimal valorTotalCoreCharge = 0;
            decimal qtdItensMovimento = 0;
            int idMovimentoTemp = (CachePersister.userIdentity.IdUsuario * -1);
            int idMovimentoGravado = 0;
            int idMovimentoVisualizacao = 0;
            if (view_record_gc_movimentos.id_movimento > 0) { idMovimentoGravado = view_record_gc_movimentos.id_movimento; };

            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                String SqlDeleteItem = "delete from gc_movimentos_itens where id_movimento in (select distinct(id_movimento) from gc_movimentos where id_movimento_tipo = 1 and id_usuario_cadastro = " + CachePersister.userIdentity.IdUsuario.ToString() + ")";
                String SqlDeleteMovimento = "delete from gc_movimentos where id_movimento_tipo = 1 and id_usuario_cadastro = " + CachePersister.userIdentity.IdUsuario.ToString();
                DataTable tableRegistroDeleteItem = LibDB.GetDataTable(SqlDeleteItem, db);
                DataTable tableRegistroDeleteMovimento = LibDB.GetDataTable(SqlDeleteMovimento, db);

                if (view_record_gc_movimentos.id_cliente <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Cliente/Fornecedor não localizado/informado!<br/>";
                }
                if (view_record_gc_movimentos.data_vencimento.GetValueOrDefault() < DateTime.Parse(DateTime.Now.ToShortDateString()))
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Data de vencimento não pode ser menor que a data atual!<br/>";
                }

                if (qtdInconsistencias == 0)
                {
                    var allItensTemp = db.gc_movimentos_itens.Where(i => i.id_movimento == idMovimentoTemp).ToList();
                    var allItensGravados = db.gc_movimentos_itens.Where(i => i.id_movimento == idMovimentoGravado).ToList();
                    var listItens = new List<gc_movimentos_itens>();
                    listItens.AddRange(allItensTemp);
                    listItens.AddRange(allItensGravados);

                    if (listItens.Count() <= 0)
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += " - Não há itens!<br/>";
                    }
                    else
                    {
                        foreach (gc_movimentos_itens itemVisualizacao in listItens)
                        {
                            valorTotalMovimento += itemVisualizacao.valor_total;
                            valorTotalCoreCharge += itemVisualizacao.valor_total_corecharge;
                            qtdItensMovimento += 1;
                        }
                        gc_movimentos new_record_gc_movimentos = new Db.gc_movimentos();
                        new_record_gc_movimentos.id_movimento_tipo = 1; // 1 = Visualização
                        new_record_gc_movimentos.id_movimento_status = 1;   // Aberto
                        new_record_gc_movimentos.id_movimento_posicao = 0;  // Incluído
                        new_record_gc_movimentos.id_local_estoque = view_record_gc_movimentos.id_local_estoque;
                        new_record_gc_movimentos.data_vencimento = view_record_gc_movimentos.data_vencimento;
                        new_record_gc_movimentos.movimento_faturado = false;
                        new_record_gc_movimentos.id_cliente = view_record_gc_movimentos.id_cliente;
                        new_record_gc_movimentos.id_contato = view_record_gc_movimentos.id_contato;
                        new_record_gc_movimentos.id_moeda = view_record_gc_movimentos.id_moeda;
                        new_record_gc_movimentos.id_vendedor = view_record_gc_movimentos.id_vendedor;
                        new_record_gc_movimentos.id_pagrec_condicao = view_record_gc_movimentos.id_pagrec_condicao;
                        new_record_gc_movimentos.id_frete_responsavel = view_record_gc_movimentos.id_frete_responsavel;
                        new_record_gc_movimentos.obs = view_record_gc_movimentos.obs;
                        new_record_gc_movimentos.obs_negociacao = view_record_gc_movimentos.obs_negociacao;
                        new_record_gc_movimentos.aeronave_prefixo = view_record_gc_movimentos.aeronave_prefixo;
                        new_record_gc_movimentos.aeronave_modelo = view_record_gc_movimentos.aeronave_modelo;
                        new_record_gc_movimentos.aeronave_serie = view_record_gc_movimentos.aeronave_serie;
                        new_record_gc_movimentos.aeronave_registro = view_record_gc_movimentos.aeronave_registro;
                        new_record_gc_movimentos.notifica_contatos_emails = view_record_gc_movimentos.notifica_contatos_emails;
                        new_record_gc_movimentos.notifica_contatos_ids = view_record_gc_movimentos.notifica_contatos_ids;
                        new_record_gc_movimentos.qtd_itens = allItensTemp.Count() + allItensGravados.Count();
                        new_record_gc_movimentos.qtd_produtos = allItensTemp.Count() + allItensGravados.Count();
                        new_record_gc_movimentos.nf_numero = "0";
                        new_record_gc_movimentos.nf_serie = "0";
                        new_record_gc_movimentos.frete_valor = view_record_gc_movimentos.frete_valor;
                        new_record_gc_movimentos.valor_total_produtos = valorTotalMovimento;
                        new_record_gc_movimentos.valor_total_liquido = valorTotalMovimento; 
                        new_record_gc_movimentos.valor_total_bruto = valorTotalMovimento + new_record_gc_movimentos.frete_valor;
                        new_record_gc_movimentos.valor_total_corecharge = valorTotalCoreCharge;
                        new_record_gc_movimentos.datahora_cadastro = DataHoraAtual;
                        new_record_gc_movimentos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        new_record_gc_movimentos.datahora_alteracao = DataHoraAtual;
                        new_record_gc_movimentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.gc_movimentos.Add(new_record_gc_movimentos);
                        db.SaveChanges();
                        idMovimentoVisualizacao = new_record_gc_movimentos.id_movimento;

                        foreach (gc_movimentos_itens itemVisualizacao in listItens)
                        {
                            // Novo Item
                            gc_movimentos_itens new_record_gc_movimentos_itens = new Db.gc_movimentos_itens();
                            new_record_gc_movimentos_itens.id_movimento = new_record_gc_movimentos.id_movimento;
                            new_record_gc_movimentos_itens.id_produto = itemVisualizacao.id_produto;
                            new_record_gc_movimentos_itens.id_produto_condicao = itemVisualizacao.id_produto_condicao;
                            new_record_gc_movimentos_itens.id_entrega_prazo = itemVisualizacao.id_entrega_prazo;
                            new_record_gc_movimentos_itens.sequencia = itemVisualizacao.sequencia;
                            new_record_gc_movimentos_itens.serial = itemVisualizacao.serial;
                            new_record_gc_movimentos_itens.lote01_identificador = itemVisualizacao.lote01_identificador;
                            new_record_gc_movimentos_itens.quantidade = itemVisualizacao.quantidade;
                            new_record_gc_movimentos_itens.valor_unit = itemVisualizacao.valor_unit;
                            new_record_gc_movimentos_itens.valor_total = itemVisualizacao.quantidade * itemVisualizacao.valor_unit;
                            new_record_gc_movimentos_itens.valor_unit_corecharge = itemVisualizacao.valor_unit_corecharge;
                            new_record_gc_movimentos_itens.valor_total_corecharge = itemVisualizacao.quantidade * itemVisualizacao.valor_unit_corecharge;
                            new_record_gc_movimentos_itens.obs = itemVisualizacao.obs;
                            new_record_gc_movimentos_itens.obs_nf = itemVisualizacao.obs_nf;
                            db.gc_movimentos_itens.Add(new_record_gc_movimentos_itens);
                        }
                        db.SaveChanges();
                        Sucesso = true;
                        MsgRetorno += "Visualização concluída com Sucesso!";
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idPedido = idMovimentoVisualizacao }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxSavePedido(gc_movimentos view_record_gc_movimentos)
        {
            String MsgRetorno = String.Empty;
            String MsgInconsistencia = String.Empty;
            String MsgAlerta = String.Empty;
            bool Sucesso = false;
            bool DifalPresenteOperacao = false;
            bool DifalObrigatorioOperacao = false;
            bool IsMarkupGeralCalculado = true;
            bool NovoPedido = false;
            bool IsNfeServicos = false;
            int QtdItensGrupo1Pecas = 0;
            int QtdItensGrupo2CombustivelLubrificante = 0;
            int QtdItensGrupo3Servicos = 0;
            int QtdItensGrupo99NaoClassificados = 0;
            int idMovimento = 0;
            int qtdInconsistencias = 0;
            int idMovimentoTemp = (CachePersister.userIdentity.IdUsuario * -1);
            decimal valorTotalProdutos = 0;
            decimal valorTotalCoreCharge = 0;
            decimal qtdItensMovimento = 0;
            decimal MarkupPedido = 0;
            String MsgTributariaIcmsDifal = string.Empty;
            String MsgObsFinanceira = string.Empty;
            g_clientes RecordCliente = null;
            g_uf RecordUfDestinatarioICMS = null;
            gc_parametros_difal RecordParametrosDifal = null;
            gc_movimentos record_pedido_gc_movimento = new Db.gc_movimentos();
            gc_movimentos record_old_gc_movimento = new Db.gc_movimentos();
            gc_cfop_operacoes RecordCfopOperacoes = null;
            cstPosicaoFinanceiraCliente PosicaoFinanceiraCliente = null;
            if (view_record_gc_movimentos.id_movimento > 0) { idMovimentoTemp = view_record_gc_movimentos.id_movimento; };
            List<gc_movimentos_itens> ListaItensPedido = new List<gc_movimentos_itens>();
            List<g_produtos> ListaProdutosPedido = new List<g_produtos>();
            List<Decimal> ListaSequenciaItens = new List<Decimal>();

            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia().Date;
            try
            {
                // Validação Vendedor
                if (view_record_gc_movimentos.id_cliente <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Cliente/Fornecedor não localizado/informado!<br/>";
                }
                else if (view_record_gc_movimentos.id_vendedor <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Vendedor/Consultor não informado!<br/>";
                }
                else if ((view_record_gc_movimentos.id_vendedor > 0) && (view_record_gc_movimentos.id_cliente > 0))
                {
                    RecordCliente = db.g_clientes.Find(view_record_gc_movimentos.id_cliente);
                    if ((view_record_gc_movimentos.id_vendedor != RecordCliente.id_vendedor) && (view_record_gc_movimentos.id_vendedor != RecordCliente.id_vendedor2) && (view_record_gc_movimentos.id_vendedor != RecordCliente.id_vendedor3))
                    {
                        qtdInconsistencias += 1;
                        int IdVendedor = view_record_gc_movimentos.id_vendedor;
                        MsgInconsistencia += " - Vendedor informado não associado ao cliente " + RecordCliente.nome.EmptyIfNull().ToString() + "<br/>[Vendedores associados: ";
                        MsgInconsistencia += CachePersister.contextoModel.gc_dataSetVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString();
                        if ((RecordCliente.id_vendedor2 > 0) && (RecordCliente.id_vendedor2 != RecordCliente.id_vendedor)) { MsgInconsistencia += ", " + CachePersister.contextoModel.gc_dataSetVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor2).FirstOrDefault().nome.EmptyIfNull().ToString(); };
                        if ((RecordCliente.id_vendedor3 > 0) && (RecordCliente.id_vendedor3 != RecordCliente.id_vendedor)) { MsgInconsistencia += ", " + CachePersister.contextoModel.gc_dataSetVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor3).FirstOrDefault().nome.EmptyIfNull().ToString(); };
                        MsgInconsistencia += "]!<br/>"; ;
                    }
                }

                // Validação de Operação
                if (view_record_gc_movimentos.id_cfop_operacao <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Operação não informada!<br/>";
                }
                else if ((view_record_gc_movimentos.id_cfop_operacao == 3) && (view_record_gc_movimentos.id_cfop_operacao != 17) && (view_record_gc_movimentos.id_cliente_destinatario <= 0))
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Destinatário não informado para operação triangular!<br/>";
                }

                if (((view_record_gc_movimentos.id_movimento_tipo == 19) && (view_record_gc_movimentos.id_cfop_operacao != 25)) || ((view_record_gc_movimentos.id_movimento_tipo != 19) && (view_record_gc_movimentos.id_cfop_operacao == 25))) // Transferência Entre Filiais
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Para movimentos de transferência informe o Tipo [Transferência] e informe a operação [Transferência]!" + "<br/>";
                }
                else if ((view_record_gc_movimentos.id_movimento_tipo == 19) && (view_record_gc_movimentos.id_cfop_operacao == 25)) // Transferência Entre Filiais
                {
                    if ((view_record_gc_movimentos.id_local_estoque == 1) && (view_record_gc_movimentos.id_cliente != 3637))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Operações de Transferência de Estoque Matriz x Filial, o cliente deverá ser GDI SP!" + "<br/>";
                    }
                    else if ((view_record_gc_movimentos.id_local_estoque == 3) && (view_record_gc_movimentos.id_cliente != 704))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Operações de Transferência de Estoque Filial x Matriz, o cliente deverá ser GDI BH!" + "<br/>";
                    }
                    if (((view_record_gc_movimentos.id_local_estoque != 1) && (view_record_gc_movimentos.id_local_estoque != 3)) || ((view_record_gc_movimentos.id_cliente != 704) && (view_record_gc_movimentos.id_cliente != 3637)))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Erro na Operação de Transferência, verifique Local de Estoque e Cliente!" + "<br/>";
                    }
                }

                if (view_record_gc_movimentos.id_cfop_operacao > 0)
                {
                    RecordCfopOperacoes = db.gc_cfop_operacoes.Find(view_record_gc_movimentos.id_cfop_operacao);
                    if (RecordCfopOperacoes.is_servico == true) { IsNfeServicos = true; };
                    if ((RecordCfopOperacoes.has_financeiro == true) && (view_record_gc_movimentos.id_pagrec_condicao == 30)) // Sem Faturamento
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Para a Operação selecionada é obrigatório informar a condição de pagamento!" + "<br/>";
                    }
                    else if ((RecordCfopOperacoes.has_financeiro == false) && (view_record_gc_movimentos.id_pagrec_condicao != 30)) // Sem Faturamento
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Para a Operação selecionada é obrigatório informar a condição [Sem Faturamento]!" + "<br/>";
                    }
                }

                // Local Estoque - Pelo local de estoque vamos saber a filial
                if ((view_record_gc_movimentos.id_local_estoque != 1) && (view_record_gc_movimentos.id_local_estoque != 3))
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Local de Estoque é de preenchimento obrigatório!" + "<br/>";
                }

                // Validação Data de Vencimento
                if (view_record_gc_movimentos.data_vencimento.GetValueOrDefault() < DateTime.Parse(DateTime.Now.ToShortDateString()))
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Data de vencimento não pode ser menor que a data atual!<br/>";
                }
                // Validação Finalidade
                if (view_record_gc_movimentos.id_cfop_finalidade <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Finalidade não informada!<br/>";
                }
                // Validação Benefício Aviação
                if ((view_record_gc_movimentos.has_beneficio_aviacao != false) && (view_record_gc_movimentos.has_beneficio_aviacao != true))
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Benefício de ICMS da aviação não informado!<br/>";
                }
                // Validação Benefício Aviação
                if (view_record_gc_movimentos.id_pagrec_condicao <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgInconsistencia += " - Condição de pagamento é de preenchimento obrigatório!<br/>";
                }


                if (IsNfeServicos == false)
                {
                    // Validação de Frete
                    if (view_record_gc_movimentos.id_frete_responsavel <= 0)
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Frete (Responsável) não informado!<br/>";
                    }

                    if ((view_record_gc_movimentos.id_frete_responsavel == 1) && (view_record_gc_movimentos.id_vendedor != 6) && (view_record_gc_movimentos.frete_valor == 0) && (view_record_gc_movimentos.frete_gerencial == 0)) // Responsável pelo frete é GDI, o vendedor não é o paulo, e não foi informado valor de frete
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Valor do Frete (Normal ou Gerencial) é obrigatório quando a GDI é responsável pelo frete!<br/>";
                    }
                    else if ((view_record_gc_movimentos.frete1_transportadora > 0) && (view_record_gc_movimentos.frete_valor == 0) && (view_record_gc_movimentos.frete_gerencial == 0) && (view_record_gc_movimentos.frete_observacoes.EmptyIfNull().Length <= 10))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Valor do Frete (Normal ou Gerencial) deverá ser informado, ou deverá ser justificado a isenção de Frete!<br/>";
                    }

                    if ((view_record_gc_movimentos.frete1_custo + view_record_gc_movimentos.frete2_custo) > (view_record_gc_movimentos.frete_valor + view_record_gc_movimentos.frete_gerencial))
                    {
                        if (view_record_gc_movimentos.frete_observacoes.EmptyIfNull().Length <= 10)
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Valor do Custo do Frete é Maior do que o valor do Frete da operação, deverá ser justificado a diferença de valores!<br/>";
                        }
                    }
                    if ((view_record_gc_movimentos.frete_valor > 0) && (view_record_gc_movimentos.frete_gerencial > 0))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Valor do Frete (Normal e Gerencial) não poderão ser informados simultaneamente!<br/>";
                    }
                    if (((view_record_gc_movimentos.frete_valor + view_record_gc_movimentos.frete_gerencial) > 0) && (view_record_gc_movimentos.frete1_custo == 0))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Valor do Custo do Frete deverá ser informado!<br/>";
                    }
                    if ((view_record_gc_movimentos.frete2_transportadora > 0) && (view_record_gc_movimentos.frete2_custo == 0))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Valor do Custo do Frete (Complementar) deverá ser informado!<br/>";
                    }
                    if ((view_record_gc_movimentos.frete2_custo > 0) && (view_record_gc_movimentos.frete2_transportadora <= 0))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Transportadora Complementar deverá ser informada!<br/>";
                    }

                    if (view_record_gc_movimentos.id_frete_responsavel == 2) // Responsável pelo frete é o cliente
                    {
                        if ((view_record_gc_movimentos.frete_valor > 0)
                        || (view_record_gc_movimentos.frete_gerencial > 0)
                        || (view_record_gc_movimentos.frete1_custo > 0)
                        || (view_record_gc_movimentos.frete2_custo > 0)
                        || (view_record_gc_movimentos.frete3_custo > 0)
                        )
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Dados do Custo do Frete não poderão ser informados o frete é por conta do cliente!<br/>";
                        }
                    }
                    else if ((view_record_gc_movimentos.id_frete_responsavel == 1) && (view_record_gc_movimentos.frete1_transportadora == 0)) // Responsável pelo frete é GDI e não há transportadoras
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Transportadora deverá ser informada!<br/>";
                    }
                    else if ((view_record_gc_movimentos.id_frete_responsavel == 1) && (view_record_gc_movimentos.frete1_transportadora > 0))        // Responsável pelo frete é GDI // Verificar qual GDI
                    {
                        if ((view_record_gc_movimentos.id_local_estoque == 1) && (view_record_gc_movimentos.frete1_transportadora == 3637 ))         // GDI BH
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Para pedidos de BH com transporte por conta da GDI a transportadora GDI SP não é permitida!<br/>";
                        }
                        else if ((view_record_gc_movimentos.id_local_estoque == 3) && (view_record_gc_movimentos.frete1_transportadora == 704))    // GDI SP
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Para pedidos de SP com transporte por conta da GDI a transportadora GDI BH não é permitida!<br/>";
                        }
                    }
                }
                {
                    if (view_record_gc_movimentos.id_frete_responsavel <= 0) { view_record_gc_movimentos.id_frete_responsavel = 2; };
                    if (view_record_gc_movimentos.id_local_estoque <= 0) { view_record_gc_movimentos.id_local_estoque = 1; };
                }

                // Validação dos Itens
                if (qtdInconsistencias == 0)
                {
                    ListaItensPedido = db.gc_movimentos_itens.Where(i => i.id_movimento == idMovimentoTemp).OrderBy(i => i.id_movimento_item).ToList();
                    String SqlListaProdutosPedido = "select * from g_produtos where id_produto in (select distinct(id_produto) from gc_movimentos_itens where id_movimento = " + idMovimentoTemp.ToString() + ")";
                    ListaProdutosPedido = db.g_produtos.SqlQuery(SqlListaProdutosPedido).ToList();
                    if (ListaItensPedido.Count <= 0)
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Não há itens!<br/>";
                    }
                    else
                    {
                        foreach (gc_movimentos_itens ItemPedido in ListaItensPedido)
                        {
                            ListaItensPedido[ListaItensPedido.IndexOf(ItemPedido)].tag = false;
                            g_produtos RecordProduto = db.g_produtos.Find(ItemPedido.id_produto);

                            if ((view_record_gc_movimentos.id_cfop_finalidade == 2) && (RecordProduto.item_revenda == false)) // REVENDA
                            {
                                qtdInconsistencias += 1;
                                MsgInconsistencia += " - Item [" + RecordProduto.nome.EmptyIfNull().ToString() + "] Não permitido para operação de Revenda!" + "<br/>";
                            }

                            if (RecordProduto.is_servico == true) { QtdItensGrupo3Servicos += 1; }
                            else if (RecordProduto.id_produto_grupo == 1) { QtdItensGrupo1Pecas += 1; }
                            else if (RecordProduto.id_produto_grupo == 2) { QtdItensGrupo2CombustivelLubrificante += 1; }
                            else { QtdItensGrupo99NaoClassificados += 1; }
                        }
                    }

                    if (IsNfeServicos == true)
                    {
                        if ((QtdItensGrupo3Servicos != 1) || ((QtdItensGrupo1Pecas + QtdItensGrupo2CombustivelLubrificante + QtdItensGrupo99NaoClassificados) > 0))
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Para operações de Serviços, só poderá ser registrado 1(um) item do tipo Serviço!" + "<br/>";
                        }
                    }
                    else
                    {
                        if (QtdItensGrupo3Servicos > 0)
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Itens do tipo [Serviço] somente podem ser informados em pedidos de Serviços!" + "<br/>";
                        }
                    }
                }


                // Validação Obrigatoriedade da Ordem de Compra
                if (qtdInconsistencias == 0)
                {
                    if (RecordCliente.param_gc_pedidos_oc == true)
                    {
                        if (view_record_gc_movimentos.oc_numero.EmptyIfNull().ToString().Length == 0)
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Número da Ordem de Compra é de preenchimento obrigatório!" + "<br/>";
                        }
                        else
                        {
                            gc_movimentos MovimentoMesmaOC = db.gc_movimentos.Where(m => m.id_movimento > 0 && m.id_movimento_status == 2 && m.id_cliente == RecordCliente.id_cliente && m.oc_numero == view_record_gc_movimentos.oc_numero.EmptyIfNull().ToString()).FirstOrDefault();
                            if (MovimentoMesmaOC != null)
                            {
                                List<gc_movimentos_itens> ListaItensMovimentoMesmaOC = db.gc_movimentos_itens.Where(i => i.id_movimento == MovimentoMesmaOC.id_movimento).ToList();
                                foreach (gc_movimentos_itens ItemMovimentoAnterior in ListaItensMovimentoMesmaOC)
                                {
                                    gc_movimentos_itens ItemMovimentoAtual = ListaItensPedido.Where(i => i.id_produto == ItemMovimentoAnterior.id_produto).FirstOrDefault();
                                    if (ItemMovimentoAtual != null)
                                    {
                                        g_produtos ProdutoAtual = db.g_produtos.Find(ItemMovimentoAtual.id_produto);
                                        qtdInconsistencias += 1;
                                        MsgInconsistencia += " - Produto [" + ProdutoAtual.nome.EmptyIfNull().ToString() + "] está em dupliciade no pedido [" + ItemMovimentoAtual.id_movimento.EmptyIfNull().ToString() + "] para a Ordem de Compra Nº [" + view_record_gc_movimentos.oc_numero.EmptyIfNull().ToString() + "] desse mesmo cliente!" + "<br/>";
                                    }
                                }
                            }
                        }
                    }
                }

                // Validações Financeiras
                if (qtdInconsistencias == 0)
                {
                    // Validar Limite de Crédito e Títulos em aberto
                    PosicaoFinanceiraCliente = GetPosicaoFinanceiraCliente(view_record_gc_movimentos.id_cliente);
                    if (view_record_gc_movimentos.has_beneficio_aviacao == true) { view_record_gc_movimentos.param_reducao_bc = true; } else { view_record_gc_movimentos.param_reducao_bc = false; }
                    ListaSequenciaItens.Clear();
                    foreach (gc_movimentos_itens ItemPedido in ListaItensPedido)
                    {
                        if (ItemPedido.sequencia > 0) { ListaSequenciaItens.Add(ItemPedido.sequencia); };
                        valorTotalProdutos += ItemPedido.valor_total;
                        valorTotalCoreCharge += ItemPedido.valor_total_corecharge;
                        qtdItensMovimento += 1;
                    }

                    // Valor do adiantamento é maior que o valor dos movimento
                    if ((view_record_gc_movimentos.valor_total_adiantamento) > (view_record_gc_movimentos.frete_valor + valorTotalProdutos))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Valor do Adiantamento não pode ser maior que o valor do movimento!<br/>";
                    }
                }

                if (qtdInconsistencias == 0)
                {

                    if ((view_record_gc_movimentos.id_cliente_destinatario > 0) && ((view_record_gc_movimentos.id_cfop_operacao == 3) || (view_record_gc_movimentos.id_cfop_operacao == 17)))
                    {
                        g_clientes_destinatarios RecordClienteDestinatario = db.g_clientes_destinatarios.Find(view_record_gc_movimentos.id_cliente_destinatario);
                        if (RecordClienteDestinatario != null)
                        {
                            if (RecordClienteDestinatario.id_uf_com > 0)
                            {
                                RecordUfDestinatarioICMS = db.g_uf.Find(RecordClienteDestinatario.id_uf_com);
                                if (RecordCliente.id_indicador_ie == 3) 
                                {
                                    if ((view_record_gc_movimentos.id_local_estoque == 1) && (RecordUfDestinatarioICMS.id_uf != 11)) // Aplica os percentuais de MG
                                    {
                                        DifalPresenteOperacao = true;
                                        RecordParametrosDifal = db.gc_parametros_difal.Where(p => p.id_uf == RecordUfDestinatarioICMS.id_uf).FirstOrDefault();
                                    }
                                    else if ((view_record_gc_movimentos.id_local_estoque == 3) && (RecordUfDestinatarioICMS.id_uf != 26)) // Aplica os percentuais de SP
                                    {
                                        DifalPresenteOperacao = true;
                                        RecordParametrosDifal = db.gc_parametros_difal.Where(p => p.id_uf == RecordUfDestinatarioICMS.id_uf).FirstOrDefault();
                                    }; 
                                }
                            }
                            else
                            {
                                qtdInconsistencias += 1;
                                MsgInconsistencia += " - UF do Destinatário não localizada!<br/>";
                            }
                        }
                        else
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Destinatário não localizado!<br/>";
                        }
                    }
                    else
                    {
                        RecordUfDestinatarioICMS = db.g_uf.Find(RecordCliente.id_uf_com);
                        if (RecordCliente.id_indicador_ie == 3)
                        {
                            if ((view_record_gc_movimentos.id_local_estoque == 1) && (RecordUfDestinatarioICMS.id_uf != 11)) // Aplica os percentuais de MG
                            {
                                DifalPresenteOperacao = true;
                                RecordParametrosDifal = db.gc_parametros_difal.Where(p => p.id_uf == RecordUfDestinatarioICMS.id_uf).FirstOrDefault();
                            }
                            else if ((view_record_gc_movimentos.id_local_estoque == 3) && (RecordUfDestinatarioICMS.id_uf != 26)) // Aplica os percentuais de SP
                            {
                                DifalPresenteOperacao = true;
                                RecordParametrosDifal = db.gc_parametros_difal.Where(p => p.id_uf == RecordUfDestinatarioICMS.id_uf).FirstOrDefault();
                            };
                        }
                    }
                }

                // Validação venda complementar (valor)
                if (qtdInconsistencias == 0)
                {
                    if (view_record_gc_movimentos.id_cfop_operacao == 2)
                    {
                        if (view_record_gc_movimentos.nf_chave_referenciada.EmptyIfNull().ToString().Length == 0)
                        {
                            qtdInconsistencias += 1;
                            MsgInconsistencia += " - Campo [Chave NFe Referência] é de preenchimento obrigatório para operações de Venda Complementar!" + "<br/>";
                        }
                        else
                        {
                            view_record_gc_movimentos.nf_chave_referenciada = LibStringFormat.SomenteNumeros(view_record_gc_movimentos.nf_chave_referenciada);

                            gc_movimentos_nf MovimentoNFReferencia = db.gc_movimentos_nf.Where(nf => nf.nf_chave_acesso == view_record_gc_movimentos.nf_chave_referenciada).FirstOrDefault();
                            if (MovimentoNFReferencia != null)
                            {
                                gc_movimentos MovimentoReferencia = db.gc_movimentos.Find(MovimentoNFReferencia.id_movimento);

                                if (MovimentoReferencia != null)
                                {
                                    if (MovimentoReferencia.id_cliente == view_record_gc_movimentos.id_cliente)
                                    {
                                        List<gc_movimentos_itens> ListaItensMovimentoReferencia = db.gc_movimentos_itens.Where(i => i.id_movimento == MovimentoReferencia.id_movimento).ToList();
                                        foreach (gc_movimentos_itens ItemMovimentoAtual in ListaItensPedido)
                                        {
                                            gc_movimentos_itens ItemMovimentoReferencia = ListaItensMovimentoReferencia.Where(i => i.id_produto == ItemMovimentoAtual.id_produto).FirstOrDefault();

                                            if (ItemMovimentoReferencia != null)
                                            {
                                                if (ItemMovimentoAtual.quantidade != ItemMovimentoReferencia.quantidade)
                                                {
                                                    g_produtos ProdutoAtual = db.g_produtos.Find(ItemMovimentoAtual.id_produto);
                                                    qtdInconsistencias += 1;
                                                    MsgInconsistencia += " - Quantidade do produto [" + ProdutoAtual.codigo.EmptyIfNull().ToString() + "] está divergente da NF Referência!" + "<br/>";
                                                }
                                            }
                                            else
                                            {
                                                g_produtos ProdutoAtual = db.g_produtos.Find(ItemMovimentoAtual.id_produto);
                                                qtdInconsistencias += 1;
                                                MsgInconsistencia += " - Produto [" + ProdutoAtual.codigo.EmptyIfNull().ToString() + "] não foi localizado na NF Referência!" + "<br/>";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        g_clientes ClienteNFReferenciada = db.g_clientes.Find(MovimentoReferencia.id_cliente);
                                        qtdInconsistencias += 1;
                                        MsgInconsistencia += " - A NFe Referência pertence ao cliente [" + ClienteNFReferenciada.nome.EmptyIfNull().ToString() + "]!" + "<br/>";
                                    }
                                }
                            }
                            else
                            {
                                qtdInconsistencias += 1;
                                MsgInconsistencia += " - Chave NFe Referência informada não foi localizada no banco de dados!" + "<br/>";
                            }
                        }
                    }
                }

                // Solicitação de Cotação ao Fornecedor
                if (qtdInconsistencias == 0)
                {
                    if ((view_record_gc_movimentos.fornecedor_cotacao_solicitar == true) && (view_record_gc_movimentos.fornecedor_cotacao_email.EmptyIfNull().ToString().Trim().Length == 0))
                    {
                        qtdInconsistencias += 1;
                        MsgInconsistencia += " - Campo [Email do Fornecedor para Cotação de Compra] é de preenchimento obrigatório para operações de Cotação de Compra!" + "<br/>";
                    }
                }

                if (qtdInconsistencias == 0)
                {
                    // Validacao Fob
                    bool AlertaFob = false;
                    bool ValorTotalAlterado = false;
                    foreach (gc_movimentos_itens ItemMovimentoAtual in ListaItensPedido)
                    {
                        Decimal _FobItem = 0;
                        Decimal ProdutoCustoReais = 0;
                        Decimal ItemCustoReais = 0;
                        g_produtos RecordProduto = ListaProdutosPedido.Where(p => p.id_produto == ItemMovimentoAtual.id_produto).FirstOrDefault();
                        _FobItem = RecordProduto.fob1_dollar;
                        if ((_FobItem > 0) && (CachePersister.userIdentity.CotacaoDollarDia > 0))
                        {
                            if (view_record_gc_movimentos.id_moeda == 1) 
                            { 
                                ItemCustoReais = _FobItem * CachePersister.userIdentity.CotacaoDollarDia * ItemMovimentoAtual.quantidade;
                                ProdutoCustoReais = _FobItem * CachePersister.userIdentity.CotacaoDollarDia;
                            }
                            else if (view_record_gc_movimentos.id_moeda == 2) 
                            { 
                                ItemCustoReais = _FobItem * ItemMovimentoAtual.quantidade;
                                ProdutoCustoReais = _FobItem;
                            }
                            if (ItemMovimentoAtual.valor_total < ItemCustoReais)
                            {
                                if (AlertaFob == false)
                                {
                                    AlertaFob = true;
                                    MsgAlerta += "<br/>" + LibIcons.getIcon("fa-solid fa-scale-unbalanced-flip", "", "red", "fa-sm") + LibStringFormat.GetTabHtml(1) + "<b style=\'color:#cc0000'><u>Markup / Custo FOB</u></b>" + "<br/>";
                                }
                                MsgAlerta += " - Item [" + RecordProduto.codigo.EmptyIfNull().ToString() + "]";
                                MsgAlerta += " está com o valor de venda [" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ItemMovimentoAtual.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "] ";
                                MsgAlerta += " MENOR que o valor FOB [" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ItemCustoReais).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "]!" + "<br/>";
                            }

                            // Cálculo do valor do item para movimento de transferência
                            if (view_record_gc_movimentos.id_cfop_operacao == 25)
                            {
                                if (ItemMovimentoAtual.valor_unit != ItemCustoReais)
                                {
                                    ListaItensPedido[ListaItensPedido.IndexOf(ItemMovimentoAtual)].valor_unit = ProdutoCustoReais;
                                    ListaItensPedido[ListaItensPedido.IndexOf(ItemMovimentoAtual)].valor_total = ProdutoCustoReais * ItemMovimentoAtual.quantidade;
                                    ValorTotalAlterado = true;
                                }
                            }
                        }
                    }
                    if (ValorTotalAlterado == true)
                    {
                        valorTotalProdutos = 0;
                        foreach (gc_movimentos_itens ItemPedido in ListaItensPedido) { valorTotalProdutos += ItemPedido.valor_total; };
                    }
                }



                if (qtdInconsistencias == 0)
                {
                    if (idMovimentoTemp <= 0) { NovoPedido = true; }; // Novo Movimento

                    if (NovoPedido == true)
                    {
                        record_pedido_gc_movimento.id_movimento_status = 1;   // Aberto
                        record_pedido_gc_movimento.id_movimento_posicao = 0;  // Incluído
                        record_pedido_gc_movimento.movimento_faturado = false;
                        record_pedido_gc_movimento.nf_numero = "0";
                        record_pedido_gc_movimento.nf_serie = "0";
                        record_pedido_gc_movimento.datahora_cadastro = DataHoraAtual;
                        record_pedido_gc_movimento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        record_pedido_gc_movimento.id_coligada = 1; // Grupo GDI
                    }
                    else
                    {
                        record_pedido_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimentos.id_movimento);
                        record_old_gc_movimento = LibDB.CloneTObject(record_pedido_gc_movimento);
                        record_pedido_gc_movimento.datahora_alteracao = DataHoraAtual;
                        record_pedido_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        record_pedido_gc_movimento.id_coligada = 1; // Grupo GDI
                    }

                    record_pedido_gc_movimento.id_movimento_tipo = view_record_gc_movimentos.id_movimento_tipo; // 3 = Orçamento | 4 = Pedido | 8 = OS | 19 - Transferência entre Filiais Saída
                    record_pedido_gc_movimento.data_vencimento = view_record_gc_movimentos.data_vencimento;
                    record_pedido_gc_movimento.id_cliente = view_record_gc_movimentos.id_cliente;
                    record_pedido_gc_movimento.id_contato = view_record_gc_movimentos.id_contato;
                    record_pedido_gc_movimento.id_moeda = view_record_gc_movimentos.id_moeda;
                    record_pedido_gc_movimento.id_vendedor = view_record_gc_movimentos.id_vendedor;
                    record_pedido_gc_movimento.id_pagrec_condicao = view_record_gc_movimentos.id_pagrec_condicao;
                    record_pedido_gc_movimento.id_cfop_finalidade = view_record_gc_movimentos.id_cfop_finalidade;
                    record_pedido_gc_movimento.id_cliente_destinatario = view_record_gc_movimentos.id_cliente_destinatario;
                    record_pedido_gc_movimento.id_cfop_operacao = view_record_gc_movimentos.id_cfop_operacao;
                    record_pedido_gc_movimento.cotacao_dolar_venda = view_record_gc_movimentos.cotacao_dolar_venda;
                    record_pedido_gc_movimento.cotacao_dolar_oficial_venda = view_record_gc_movimentos.cotacao_dolar_oficial_venda;

                    // Frete
                    record_pedido_gc_movimento.id_frete_responsavel = view_record_gc_movimentos.id_frete_responsavel;
                    record_pedido_gc_movimento.frete_valor = view_record_gc_movimentos.frete_valor;
                    record_pedido_gc_movimento.frete_gerencial = view_record_gc_movimentos.frete_gerencial;
                    record_pedido_gc_movimento.frete1_custo = view_record_gc_movimentos.frete1_custo;
                    record_pedido_gc_movimento.frete1_transportadora = view_record_gc_movimentos.frete1_transportadora;
                    record_pedido_gc_movimento.frete1_documento = view_record_gc_movimentos.frete1_documento;
                    record_pedido_gc_movimento.frete1_rastreio = view_record_gc_movimentos.frete1_rastreio;
                    record_pedido_gc_movimento.frete_observacoes = view_record_gc_movimentos.frete_observacoes;
                    record_pedido_gc_movimento.frete2_custo = view_record_gc_movimentos.frete2_custo;
                    record_pedido_gc_movimento.frete2_transportadora = view_record_gc_movimentos.frete2_transportadora;
                    record_pedido_gc_movimento.frete2_documento = view_record_gc_movimentos.frete2_documento;
                    record_pedido_gc_movimento.frete2_rastreio = view_record_gc_movimentos.frete2_rastreio;

                    record_pedido_gc_movimento.nf_chave_referenciada = view_record_gc_movimentos.nf_chave_referenciada;
                    record_pedido_gc_movimento.id_local_estoque = view_record_gc_movimentos.id_local_estoque;
                    record_pedido_gc_movimento.obs = view_record_gc_movimentos.obs;
                    record_pedido_gc_movimento.obs_negociacao = view_record_gc_movimentos.obs_negociacao;
                    record_pedido_gc_movimento.informacoes_complementares_nf = view_record_gc_movimentos.informacoes_complementares_nf;
                    record_pedido_gc_movimento.aeronave_prefixo = view_record_gc_movimentos.aeronave_prefixo;
                    record_pedido_gc_movimento.aeronave_modelo = view_record_gc_movimentos.aeronave_modelo;
                    record_pedido_gc_movimento.aeronave_serie = view_record_gc_movimentos.aeronave_serie;
                    record_pedido_gc_movimento.aeronave_registro = view_record_gc_movimentos.aeronave_registro;
                    record_pedido_gc_movimento.oc_numero = view_record_gc_movimentos.oc_numero;
                    record_pedido_gc_movimento.notifica_contatos_emails = view_record_gc_movimentos.notifica_contatos_emails;
                    record_pedido_gc_movimento.notifica_contatos_ids = view_record_gc_movimentos.notifica_contatos_ids;
                    record_pedido_gc_movimento.qtd_itens = ListaItensPedido.Count();
                    record_pedido_gc_movimento.qtd_produtos = ListaItensPedido.Count();
                    record_pedido_gc_movimento.valor_total_produtos = valorTotalProdutos;
                    record_pedido_gc_movimento.valor_total_liquido = valorTotalProdutos;
                    record_pedido_gc_movimento.valor_total_bruto = valorTotalProdutos + record_pedido_gc_movimento.frete_valor;
                    record_pedido_gc_movimento.valor_total_corecharge = valorTotalCoreCharge;
                    record_pedido_gc_movimento.valor_total_adiantamento = view_record_gc_movimentos.valor_total_adiantamento;
                    record_pedido_gc_movimento.param_reducao_bc = view_record_gc_movimentos.param_reducao_bc;
                    record_pedido_gc_movimento.id_estoque_cd = view_record_gc_movimentos.id_local_estoque;
                    record_pedido_gc_movimento.icms_difal_calculado = view_record_gc_movimentos.icms_difal_calculado;

                    record_pedido_gc_movimento.has_beneficio_aviacao = view_record_gc_movimentos.has_beneficio_aviacao;
                    record_pedido_gc_movimento.invoice1_cliente = view_record_gc_movimentos.invoice1_cliente;
                    record_pedido_gc_movimento.id_importacao = view_record_gc_movimentos.id_importacao;
                    record_pedido_gc_movimento.invoice1_numero = view_record_gc_movimentos.invoice1_numero;
                    record_pedido_gc_movimento.salesorder1_numero = view_record_gc_movimentos.salesorder1_numero;
                    record_pedido_gc_movimento.invoice2_numero = view_record_gc_movimentos.invoice2_numero;
                    record_pedido_gc_movimento.salesorder2_numero = view_record_gc_movimentos.salesorder2_numero;
                    record_pedido_gc_movimento.invoice3_numero = view_record_gc_movimentos.invoice3_numero;
                    record_pedido_gc_movimento.salesorder3_numero = view_record_gc_movimentos.salesorder3_numero;
                    record_pedido_gc_movimento.valor_fob_scross = view_record_gc_movimentos.valor_fob_scross;
                    record_pedido_gc_movimento.valor_fob_brasil = view_record_gc_movimentos.valor_fob_brasil;
                    record_pedido_gc_movimento.cotacao_dolar_compra = view_record_gc_movimentos.cotacao_dolar_compra;

                    record_pedido_gc_movimento.fornecedor_cotacao_solicitar = view_record_gc_movimentos.fornecedor_cotacao_solicitar;
                    record_pedido_gc_movimento.fornecedor_cotacao_solicitada = view_record_gc_movimentos.fornecedor_cotacao_solicitada;
                    record_pedido_gc_movimento.fornecedor_cotacao_respondida = view_record_gc_movimentos.fornecedor_cotacao_respondida;
                    record_pedido_gc_movimento.fornecedor_cotacao_aprovada = view_record_gc_movimentos.fornecedor_cotacao_aprovada;
                    record_pedido_gc_movimento.fornecedor_cotacao_email = view_record_gc_movimentos.fornecedor_cotacao_email;
                    record_pedido_gc_movimento.fornecedor_cotacao_obs = view_record_gc_movimentos.fornecedor_cotacao_obs;

                    if (view_record_gc_movimentos.id_local_estoque == 1) { record_pedido_gc_movimento.id_filial = 1; }
                    else if (view_record_gc_movimentos.id_local_estoque == 3) { record_pedido_gc_movimento.id_filial = 2; };

                    if (NovoPedido == true) { db.gc_movimentos.Add(record_pedido_gc_movimento); } else { db.Entry(record_pedido_gc_movimento).State = EntityState.Modified; }
                    db.SaveChanges();

                    // Cálculo do Markup
                    Decimal PedidoValorTotalVenda = 0;
                    Decimal PedidoCustoTotalVenda = 0;
                    Decimal ProdutoCustoDollar = 0;
                    Decimal ItemCustoReais = 0;
                    Decimal MarkupItem = 0;
                    IsMarkupGeralCalculado = true;
                    foreach (gc_movimentos_itens ItemPedido in ListaItensPedido)
                    {
                        ProdutoCustoDollar = ListaProdutosPedido.Where(p => p.id_produto == ItemPedido.id_produto).FirstOrDefault().fob1_dollar;
                        if ((ProdutoCustoDollar > 0) && (CachePersister.userIdentity.CotacaoDollarDia > 0))
                        {
                            PedidoValorTotalVenda += ItemPedido.valor_total;
                            ItemCustoReais = ProdutoCustoDollar * CachePersister.userIdentity.CotacaoDollarDia * ItemPedido.quantidade;
                            if (view_record_gc_movimentos.id_moeda == 2) { ItemCustoReais = ProdutoCustoDollar * ItemPedido.quantidade; }; // Moeda Dollar
                            PedidoCustoTotalVenda += ItemCustoReais;
                            MarkupItem = ((ItemPedido.valor_total * 100) / ItemCustoReais) - 100;
                            ListaItensPedido[ListaItensPedido.IndexOf(ItemPedido)].markup = Math.Round(MarkupItem, 2);
                            ListaItensPedido[ListaItensPedido.IndexOf(ItemPedido)].fob_unit_dollar = ProdutoCustoDollar;
                            ListaItensPedido[ListaItensPedido.IndexOf(ItemPedido)].fob_unit_reais_venda = ProdutoCustoDollar * CachePersister.userIdentity.CotacaoDollarDia;
                            ListaItensPedido[ListaItensPedido.IndexOf(ItemPedido)].tag = true;
                            MarkupPedido = ((PedidoValorTotalVenda * 100) / PedidoCustoTotalVenda) - 100;
                        }
                    }
                    record_pedido_gc_movimento.markup = MarkupPedido;

                    // Atualizar os itens do pedido
                    if (ListaItensPedido.Count() > 0)
                    {
                        Decimal SequenciaAtual = 1;
                        foreach (gc_movimentos_itens record_gc_movimentos_itens in ListaItensPedido)
                        {
                            if (record_gc_movimentos_itens.sequencia == 0)
                            {
                                while (ListaSequenciaItens.IndexOf(SequenciaAtual) >= 0)
                                {
                                    SequenciaAtual += 1;
                                }
                                record_gc_movimentos_itens.sequencia = SequenciaAtual;
                                SequenciaAtual += 1;
                            }
                            record_gc_movimentos_itens.tag = false;
                            record_gc_movimentos_itens.id_movimento = record_pedido_gc_movimento.id_movimento;
                            record_gc_movimentos_itens.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            record_gc_movimentos_itens.datahora_alteracao = DataHoraAtual;
                            db.Entry(record_gc_movimentos_itens).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        String SqlUpdate = " update gc_movimentos_itens set id_movimento = " + record_pedido_gc_movimento.id_movimento.ToString() + " where id_movimento = " + idMovimentoTemp;
                        DataTable tableTemp = LibDB.GetDataTable(SqlUpdate, db);
                    }
                    String StatusPedido = String.Empty;
                    if (NovoPedido == true) { StatusPedido = "REGISTRADO"; } else { StatusPedido = "ALTERADO"; };
                    
                    // Cabeçalho da MSG
                    if (record_pedido_gc_movimento.id_movimento_tipo == 3) { MsgRetorno += "<b>Orçamento " + StatusPedido + " com Sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>"; }
                    else if (record_pedido_gc_movimento.id_movimento_tipo == 4) { MsgRetorno += "<b>Pedido " + StatusPedido + " com Sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>"; }
                    else if (record_pedido_gc_movimento.id_movimento_tipo == 8) { MsgRetorno += "<b>OS " + StatusPedido + " com Sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>"; }
                    else if (record_pedido_gc_movimento.id_movimento_tipo == 19) { MsgRetorno += "<b>Transferência " + StatusPedido + " com Sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>"; }
                    MsgRetorno += "<b>Número:</b> " + record_pedido_gc_movimento.id_movimento.ToString() + "   |   ";
                    MsgRetorno += "<b>Total:</b> " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (record_pedido_gc_movimento.valor_total_bruto));
                    if ((CachePersister.userIdentity.Roles.Contains("gc_Movimentos_*")) || (CachePersister.userIdentity.Roles.Contains("gc_Movimentos_VisualizarMarkup")))
                    {
                        if (IsMarkupGeralCalculado == true) { MsgRetorno += "  (" + MarkupPedido.ToString("0.00") + " %)"; }
                    }
                    MsgRetorno += "   |    ";
                    MsgRetorno += "<b>Data Venc:</b> " + record_pedido_gc_movimento.data_vencimento.GetValueOrDefault().ToString("dd/MM/yyyy") + "<br/>";
                    if (record_pedido_gc_movimento.notifica_contatos_ids.EmptyIfNull().Trim().Length > 0)
                    {
                        AjaxReportInvoicePDF(record_pedido_gc_movimento.id_movimento, "email");
                        MsgRetorno += "Notificações enviadas por email!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                    }

                    // Análise Tributaria ICMS / Difal
                    if (DifalPresenteOperacao == true)
                    {
                        String UFOrigem = string.Empty;
                        if ((RecordParametrosDifal.difal_geral_calcular == true) && (QtdItensGrupo1Pecas > 0)) { DifalObrigatorioOperacao = true; }
                        else if ((RecordParametrosDifal.difal_comb_calcular == true) && (QtdItensGrupo2CombustivelLubrificante > 0)) { DifalObrigatorioOperacao = true; }

                        if (view_record_gc_movimentos.id_local_estoque == 1) { UFOrigem = "MG"; }
                        else if (view_record_gc_movimentos.id_local_estoque == 3) { UFOrigem = "SP"; };

                        if (DifalObrigatorioOperacao == true)
                        {
                            MsgTributariaIcmsDifal += "<b style=\"color:#cc0000\">DIFAL: OPERAÇÃO COM DIFAL OBRIGATÓRIO: De: " + UFOrigem + " para " + RecordParametrosDifal.sigla.EmptyIfNull().ToString() + "</b><br/>";
                        }
                        else
                        {
                            MsgTributariaIcmsDifal += "<b style=\"color:#cc6600\">DIFAL: O Valor do DIFAL não será informado nessa operação: De: " + UFOrigem + " para " + RecordParametrosDifal.sigla.EmptyIfNull().ToString() + " a autorização da NFe estará condicionada às regras de cada Sefaz Estadual!</b><br/>";
                        }
                    }

                    // Análise Tributaria ICMS / Difal
                    if ((record_pedido_gc_movimento.param_reducao_bc == true) || (record_pedido_gc_movimento.param_reducao_bc == false) || (DifalPresenteOperacao == true))
                    {
                        Decimal IcmsBaseReducao = 0;
                        Decimal IcmsPercentuaInterno = 0;
                        Decimal IcmsPercentuaInterestadual = 0;

                        if (record_pedido_gc_movimento.id_filial == 2)
                        {
                            IcmsBaseReducao = RecordUfDestinatarioICMS.basesp_icms_base_reducao;
                            IcmsPercentuaInterno = RecordUfDestinatarioICMS.basesp_icms_percentual_interno;
                            IcmsPercentuaInterestadual = RecordUfDestinatarioICMS.basesp_icms_interestadual;
                        }
                        else if ((record_pedido_gc_movimento.id_filial == 1) || (record_pedido_gc_movimento.id_filial == 0))
                        {
                            IcmsBaseReducao = RecordUfDestinatarioICMS.basemg_icms_base_reducao;
                            IcmsPercentuaInterno = RecordUfDestinatarioICMS.basemg_icms_percentual_interno;
                            IcmsPercentuaInterestadual = RecordUfDestinatarioICMS.basemg_icms_interestadual;
                        }

                        Decimal ValorDiferencaIcms = 0;
                        Decimal ValorDiferencaTributos = 0;
                        Decimal PercentualReducaoBcAviacao = Math.Round(((record_pedido_gc_movimento.valor_total_produtos / 100) * IcmsBaseReducao), 2);
                        Decimal IcmsBaseCalculoNormal = Math.Round((record_pedido_gc_movimento.valor_total_produtos) + record_pedido_gc_movimento.frete_valor, 2);
                        Decimal IcmsBaseCalculoReduzida = Math.Round((record_pedido_gc_movimento.valor_total_produtos - PercentualReducaoBcAviacao) + record_pedido_gc_movimento.frete_valor, 2);
                        Decimal ValorIcmsDifalNormal = Math.Round(((IcmsBaseCalculoNormal / 100) * (IcmsPercentuaInterno - IcmsPercentuaInterestadual)), 2);
                        Decimal ValorIcmsDifalReduzido = Math.Round(((IcmsBaseCalculoReduzida / 100) * (IcmsPercentuaInterno - IcmsPercentuaInterestadual)), 2);
                        Decimal ValorIcmsNormalAviacao = Math.Round(((IcmsBaseCalculoNormal / 100) * (IcmsPercentuaInterestadual)), 2);
                        Decimal ValorIcmsReduzidoAviacao = Math.Round(((IcmsBaseCalculoReduzida / 100) * IcmsPercentuaInterestadual), 2);
                        Decimal DifalPercentual = Math.Round(IcmsPercentuaInterno - IcmsPercentuaInterestadual, 2);
                        Decimal DifalNotaCheiaNormal = Math.Round(((IcmsBaseCalculoNormal * 100) / (100 - DifalPercentual)) - IcmsBaseCalculoNormal, 2);
                        Decimal DifalNotaCheiaReduzida = Math.Round(((IcmsBaseCalculoReduzida * 100) / (100 - DifalPercentual)) - IcmsBaseCalculoReduzida, 2);
                        DifalNotaCheiaReduzida = (((DifalNotaCheiaReduzida - ValorIcmsDifalReduzido) / 100) * (100 - IcmsBaseReducao)) + ValorIcmsDifalReduzido;

                        if (record_pedido_gc_movimento.param_reducao_bc == true)
                        {
                            MsgTributariaIcmsDifal += "<b>Redução ICMS:</b> Para o direito à Redução do ICMS informado nessa operação, deverão ser atendidas todas as exigências legais!" + "<br/>";
                        }
                        if (DifalPresenteOperacao == true)
                        {
                            if (record_pedido_gc_movimento.param_reducao_bc == true)
                            {
                                MsgTributariaIcmsDifal += "<b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorIcmsDifalReduzido)) + "</b> - Valor DIFAL Reduzido para essa operação, para NOTA CHEIA considerar <b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (DifalNotaCheiaReduzida)) + "</b></br>";
                                ValorDiferencaTributos += ValorIcmsDifalReduzido;
                                view_record_gc_movimentos.icms_difal_calculado = ValorIcmsDifalReduzido;
                            }
                            else if (record_pedido_gc_movimento.param_reducao_bc == false)
                            {
                                MsgTributariaIcmsDifal += "<b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorIcmsDifalNormal)) + "</b> - Valor DIFAL Normal para essa operação, para NOTA CHEIA considerar <b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (DifalNotaCheiaNormal)) + "</b></br>";
                                ValorDiferencaTributos += ValorIcmsDifalNormal;
                                view_record_gc_movimentos.icms_difal_calculado = ValorIcmsDifalNormal;
                            }
                        }
                        if (record_pedido_gc_movimento.param_reducao_bc == false)
                        {
                            ValorDiferencaIcms = ValorIcmsNormalAviacao - ValorIcmsReduzidoAviacao;
                            if (ValorDiferencaIcms > 0)
                            {
                                MsgTributariaIcmsDifal += "<b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorDiferencaIcms)) + "</b> - Valor diferença ICMS (Normal x Reduzido)<br/>";
                                ValorDiferencaTributos += ValorDiferencaIcms;
                            }
                        }
                        if (ValorDiferencaTributos > 0)
                        {
                            MsgTributariaIcmsDifal += "<b> ---------------------------------------- </b><br/>";
                            MsgTributariaIcmsDifal += "<b>" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorDiferencaTributos)) + "</b> - Valor <b>TOTAL</b> da diferença de Tributação<br/>";
                        }
                    }

                    if (MsgAlerta.EmptyIfNull().ToString().Length > 0)
                    {
                        MsgRetorno += MsgAlerta;
                    }

                    if (MsgTributariaIcmsDifal.EmptyIfNull().ToString().Length > 0)
                    {
                        MsgRetorno += "<br/>" + LibIcons.getIcon("fa-solid fa-receipt", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b><u>Observações Tributárias (ICMS / DIFAL)</u></b><br/>";
                        MsgRetorno += MsgTributariaIcmsDifal;
                    }

                    if (RecordCfopOperacoes.has_financeiro == true) // Análise Financeira do Cliente
                    {
                        MsgRetorno += "<br/>" + LibIcons.getIcon("fa-solid fa-credit-card", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b><u>Análise Financeira do Cliente</u></b><br/>";
                        MsgRetorno += "<b>Limites crédito:</b>   total   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "   |   utilizado   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoUtilizado).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "   |   disponível   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoRestante).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                        if (PosicaoFinanceiraCliente.TitulosAVencerQtd > 0) { MsgRetorno += "<b>Títulos à vencer:</b>   " + "qtd:   " + PosicaoFinanceiraCliente.TitulosAVencerQtd.EmptyIfNull().ToString() + "   |   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.TitulosAVencerValor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>"; }
                        if (PosicaoFinanceiraCliente.TitulosVencidosQtd > 0) { MsgRetorno += "<b>Títulos vencidos:</b>   " + "qtd:   " + PosicaoFinanceiraCliente.TitulosVencidosQtd.EmptyIfNull().ToString() + "   |   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.TitulosVencidosValor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>"; }
                        if (PosicaoFinanceiraCliente.TitulosNegociacaoQtd > 0) { MsgRetorno += "<b>Títulos em negociação:</b>   " + "qtd:   " + PosicaoFinanceiraCliente.TitulosNegociacaoQtd.EmptyIfNull().ToString() + "   |   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.TitulosNegociacaoValor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>"; }
                        if (PosicaoFinanceiraCliente.TitulosVencidosQtd > 0) { MsgRetorno += "<b style=\"color:#cc0000\">Atenção: O Pedido NÃO será faturado, existem títulos vencidos para esse cliente!</b>" + "<br/>"; }
                        else if ((PosicaoFinanceiraCliente.LimiteCreditoRestante < record_pedido_gc_movimento.valor_total_bruto)) // Faturado
                        {
                            if (record_pedido_gc_movimento.id_pagrec_condicao >= 3) { MsgRetorno += "<b style=\"color:#cc0000\">Atenção: O Pedido NÃO será faturado à Crédito, não há limite de crédito disponível!</b>" + "<br/>"; } // Crédito
                            else { MsgRetorno += "<b style=\"color:#cc6600\">Atenção: O Pedido somente será faturado nas seguintes condições (à vista / antecipado), não há limite de crédito disponível!</b>" + "<br/>"; } // à Vista / Antecipado
                        }
                        if (RecordCliente.obs_financeira.EmptyIfNull().ToString().Length > 0)
                        {
                            MsgRetorno += "<br/>" + LibIcons.getIcon("fa-regular fa-edit", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b><u>Observações de Faturamento</u></b><br/>";
                            MsgRetorno += RecordCliente.obs_financeira.EmptyIfNull().ToString() + "<br/>";
                        }

                        if ((record_pedido_gc_movimento.id_pagrec_condicao == 2) || (record_pedido_gc_movimento.valor_total_adiantamento > 0))
                        {
                            // Cálculo do saldo de adiantamento real do cliente, considerando os lançamentos de adiantamento que não estão vinculados a nenhum pedido ou nota fiscal, ou estão vinculados a pedidos ou notas fiscais que ainda estão em aberto
                            Decimal ValorAdiantamentoInformado = record_pedido_gc_movimento.valor_total_adiantamento;
                            Decimal SaldoAdiantamentoReal = 0;
                            List<gc_financeiro_lancamentos> listAdiantamentos = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.is_adiantamento == true && l.tipo_pag_rec == 2 && l.id_financeiro_status == 1 && l.id_cliente == record_pedido_gc_movimento.id_cliente).ToList();
                            foreach (gc_financeiro_lancamentos Adiantamento in listAdiantamentos)
                            {
                                gc_financeiro_lancamentos LancamentoAdiantamentoVinculado = db.gc_financeiro_lancamentos.Where(l => l.id_lancamento_adiantamento == Adiantamento.id_lancamento || l.id_lancamento_adiantamento2 == Adiantamento.id_lancamento || l.id_lancamento_adiantamento3 == Adiantamento.id_lancamento).FirstOrDefault();
                                if (LancamentoAdiantamentoVinculado == null) // Esse adiantamento não está vinculado a nenhum pedido ou nota fiscal, então o valor total do adiantamento pode ser considerado para o saldo
                                {
                                    SaldoAdiantamentoReal += Adiantamento.valor_total;
                                }
                            }
                            if (record_pedido_gc_movimento.id_pagrec_condicao == 2)
                            {
                                if (SaldoAdiantamentoReal == 0)
                                {
                                    MsgRetorno += "<b style=\"color:#cc0000\">Não foi encontrado Saldo de Adiantamento para faturar o pedido nas condições A Vista ou Antecipado!" + "</b><br/>";
                                }
                                else if ((SaldoAdiantamentoReal > 0) && (SaldoAdiantamentoReal < record_pedido_gc_movimento.valor_total_bruto))
                                {
                                    MsgRetorno += "<b style=\"color:#cc0000\">Saldo de adiantamento do cliente [ " + SaldoAdiantamentoReal.ToString("###,###,###,##0.00") + "] é insuficiente para faturar o pedido " + record_pedido_gc_movimento.valor_total_bruto.ToString("###,###,###,##0.00") + " nas condições A Vista ou Antecipado!<br/>" + "</b><br/>";
                                }
                            }
                            else if ((ValorAdiantamentoInformado > 0) && (ValorAdiantamentoInformado > SaldoAdiantamentoReal))
                            {
                                MsgRetorno += "<b style=\"color:#cc0000\">Valor do adiantamento informado no pedido [ " + ValorAdiantamentoInformado.ToString("###,###,###,##0.00") + " ], é MAIOR do que o saldo de adiantamento encontrado  [ " + SaldoAdiantamentoReal.ToString("###,###,###,##0.00") + "]" + "</b><br/>";
                            }
                        }
                    }

                    if (record_pedido_gc_movimento.cotacao_dolar_venda < record_pedido_gc_movimento.cotacao_dolar_oficial_venda)
                    {
                        MsgRetorno += "<br/>" + LibIcons.getIcon("fa-solid fa-dollar-sign", "", "green", "fa-sm") + LibStringFormat.GetTabHtml(1) + "<b><u>Cotação Oficial do Dolar</u></b><br/>";
                        MsgRetorno += "A Cotação do Dolar informado no pedido " + record_pedido_gc_movimento.cotacao_dolar_venda.ToString("##0.00000") + " é menor do que a cotação do dia " + record_pedido_gc_movimento.cotacao_dolar_oficial_venda.ToString("##0.00000") + "<br/>";
                    }

                    // Difal 
                    if (view_record_gc_movimentos.icms_difal_calculado > 0)
                    {
                        record_pedido_gc_movimento.icms_difal_calculado = view_record_gc_movimentos.icms_difal_calculado;
                        record_pedido_gc_movimento.datahora_alteracao = DataHoraAtual;
                        record_pedido_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_pedido_gc_movimento).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    // Verificar se o movimento está aberto, e se há lançamentos financeiros ou nfe para ele
                    if ((NovoPedido == false) && (record_pedido_gc_movimento.id_movimento_status == 1) && (record_pedido_gc_movimento.reaberto == true))
                    {
                        List<gc_movimentos_nf> ListaGCMovimentosNF = db.gc_movimentos_nf.Where(n => n.id_movimento == record_pedido_gc_movimento.id_movimento && n.id_nfe_status == 8).ToList();
                        List<gc_financeiro_lancamentos> ListaGCFinanceiroLancamentos = db.gc_financeiro_lancamentos.Where(l => l.id_movimento == record_pedido_gc_movimento.id_movimento && l.ativo == true).ToList();
                        if ((ListaGCMovimentosNF.Count > 0) || (ListaGCFinanceiroLancamentos.Count > 0))
                        {
                            record_pedido_gc_movimento.id_movimento_status = 2;
                            record_pedido_gc_movimento.datahora_alteracao = DataHoraAtual;
                            record_pedido_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_pedido_gc_movimento).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }

                    if (NovoPedido == true)
                    {
                        //String LogAudit = LibDB.CompareDataTable(new Db.gc_movimentos(), record_pedido_gc_movimento);
                        String LogAudit = LibDB.CompareGcMovimentos(new Db.gc_movimentos(), record_pedido_gc_movimento, db);
                        if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_pedido_gc_movimento.id_movimento, "Nova Cotação/Pedido [ " + LogAudit + " ]"); };
                    }
                    else
                    {
                        //String LogAudit = LibDB.CompareDataTable(record_old_gc_movimento, record_pedido_gc_movimento);
                        String LogAudit = LibDB.CompareGcMovimentos(record_old_gc_movimento, record_pedido_gc_movimento, db);
                        if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_pedido_gc_movimento.id_movimento, "Atualização Cotação/Pedido [ " + LogAudit + " ]"); };
                    }
                    Sucesso = true;
                }
                else
                {
                    MsgRetorno = MsgInconsistencia;
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idPedido = idMovimento }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult AjaxSavePosVenda(gc_movimentos view_record_gc_movimentos)
        {
            String MsgRetorno = String.Empty;
            bool Sucesso = false;
            int idMovimento = view_record_gc_movimentos.id_movimento;
            gc_movimentos record_pedido_gc_movimento = new Db.gc_movimentos();
            gc_movimentos record_old_gc_movimento = new Db.gc_movimentos();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                record_pedido_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimentos.id_movimento);
                record_old_gc_movimento = LibDB.CloneTObject(record_pedido_gc_movimento);
                record_pedido_gc_movimento.id_contato = view_record_gc_movimentos.id_contato;
                record_pedido_gc_movimento.contato_nome = view_record_gc_movimentos.contato_nome;
                record_pedido_gc_movimento.contato_telefone = view_record_gc_movimentos.contato_telefone;
                record_pedido_gc_movimento.contato_email = view_record_gc_movimentos.contato_email;
                if (view_record_gc_movimentos.posvenda_datahora_contato.EmptyIfNull().ToString().Length == 0) { record_pedido_gc_movimento.posvenda_datahora_contato = DataHoraAtual; } else { record_pedido_gc_movimento.posvenda_datahora_contato = view_record_gc_movimentos.posvenda_datahora_contato; };
                record_pedido_gc_movimento.posvenda_pedido_recebido = view_record_gc_movimentos.posvenda_pedido_recebido;
                record_pedido_gc_movimento.posvenda_recebimento_conforme_combinado = view_record_gc_movimentos.posvenda_recebimento_conforme_combinado;
                record_pedido_gc_movimento.posvenda_mercadoria_estado_fisico_ok = view_record_gc_movimentos.posvenda_mercadoria_estado_fisico_ok;
                record_pedido_gc_movimento.posvenda_itens_correspondem_pedido_nf = view_record_gc_movimentos.posvenda_itens_correspondem_pedido_nf;
                record_pedido_gc_movimento.posvenda_informacoes_tecnicas_ok = view_record_gc_movimentos.posvenda_informacoes_tecnicas_ok;
                record_pedido_gc_movimento.posvenda_documentacao_correta = view_record_gc_movimentos.posvenda_documentacao_correta;
                record_pedido_gc_movimento.posvenda_houve_dificuldade_documentacao = view_record_gc_movimentos.posvenda_houve_dificuldade_documentacao;
                record_pedido_gc_movimento.posvenda_observacao_recebimento_pedido = view_record_gc_movimentos.posvenda_observacao_recebimento_pedido;
                record_pedido_gc_movimento.posvenda_nota_avaliacao_prazo_entrega = view_record_gc_movimentos.posvenda_nota_avaliacao_prazo_entrega;
                record_pedido_gc_movimento.posvenda_nota_avaliacao_clareza_informacoes_comerciais = view_record_gc_movimentos.posvenda_nota_avaliacao_clareza_informacoes_comerciais;
                record_pedido_gc_movimento.posvenda_nota_avaliacao_atendimento_equipe = view_record_gc_movimentos.posvenda_nota_avaliacao_atendimento_equipe;
                record_pedido_gc_movimento.posvenda_nota_avaliacao_geral = view_record_gc_movimentos.posvenda_nota_avaliacao_geral;
                record_pedido_gc_movimento.posvenda_pedido_nao_conforme = view_record_gc_movimentos.posvenda_pedido_nao_conforme;
                record_pedido_gc_movimento.posvenda_cliente_sugeriu_melhoria = view_record_gc_movimentos.posvenda_cliente_sugeriu_melhoria;
                record_pedido_gc_movimento.posvenda_descricao_nao_conformidade = view_record_gc_movimentos.posvenda_descricao_nao_conformidade;
                record_pedido_gc_movimento.posvenda_descricao_sugestao_melhoria = view_record_gc_movimentos.posvenda_descricao_sugestao_melhoria;
                record_pedido_gc_movimento.posvenda_observacoes_internas = view_record_gc_movimentos.posvenda_observacoes_internas;
                record_pedido_gc_movimento.movimento_posvenda = true;
                record_pedido_gc_movimento.datahora_posvenda = DataHoraAtual;
                record_pedido_gc_movimento.id_usuario_posvenda = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_pedido_gc_movimento).State = EntityState.Modified;
                db.SaveChanges();

                MsgRetorno += "<b>Dados da Pós-Venda do Pedido foram registrados com Sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                String LogAudit = LibDB.CompareGcMovimentos(record_old_gc_movimento, record_pedido_gc_movimento, db);
                if (LogAudit.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_pedido_gc_movimento.id_movimento, "Pós-Venda Pedido [ " + LogAudit + " ]"); };
                Sucesso = true;
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idPedido = idMovimento }, JsonRequestBehavior.AllowGet);
        }




        public ActionResult GetDadosItensPedido(jQueryDataTableParamModel param)
        {
            string errorMessage = "";
            string stackTrace = "";

            try
            {
                // -------------------------
                // 1) Params
                // -------------------------
                int idMovimento = 0;
                int.TryParse(param.yesCustomIdPK, out idMovimento);
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(idMovimento);

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 50 : param.iDisplayLength);

                bool podeVerMarkup =
                    (CachePersister.userIdentity.Roles.Contains("gc_Movimentos_*")) ||
                    (CachePersister.userIdentity.Roles.Contains("gc_Movimentos_VisualizarMarkup"));

                // -------------------------
                // 2) Base query (sem ToList antes / sem trazer tudo)
                // -------------------------
                var baseQuery =
                    from mi in db.gc_movimentos_itens.AsNoTracking()
                    join p in db.g_produtos.AsNoTracking() on mi.id_produto equals p.id_produto
                    where mi.id_movimento == idMovimento
                    select new
                    {
                        // item
                        mi.id_movimento_item,
                        mi.sequencia,
                        mi.quantidade,
                        mi.valor_unit,
                        mi.valor_total,
                        mi.markup,
                        mi.serial,
                        mi.fornecedor_cotacao_solicitar,

                        // produto
                        ProdutoNome = p.nome
                    };

                // total de registros
                int totalRecords = baseQuery.Count();
                int totalDisplayRecords = totalRecords;

                // -------------------------
                // 3) Total do pedido (SUM no banco)
                // -------------------------
                decimal valorTotalPedido = 0m;
                try
                {
                    valorTotalPedido = db.gc_movimentos_itens
                        .AsNoTracking()
                        .Where(x => x.id_movimento == idMovimento)
                        .Select(x => (decimal?)x.valor_total)
                        .Sum() ?? 0m;
                }
                catch
                {
                    valorTotalPedido = 0m; // fallback seguro
                }

                // -------------------------
                // 4) Ordenação + Paginação (OrderBy ANTES do Skip)
                // -------------------------
                var page = baseQuery
                    .OrderBy(x => x.sequencia)
                    .ThenBy(x => x.id_movimento_item)
                    .Skip(start)
                    .Take(length)
                    .ToList();

                // -------------------------
                // 5) Monta aaData
                // -------------------------
                var list = new List<string[]>(page.Count);

                foreach (var l in page)
                {
                    string nomeProduto = (l.ProdutoNome ?? "").Trim();
                    string serial = (l.serial ?? "").Trim();
                    if (serial.Length > 0) nomeProduto += " [Serial: " + serial + "]";

                    if (l.fornecedor_cotacao_solicitar == true)
                    {
                        if (RecordMovimento.fornecedor_cotacao_aprovada == true) { nomeProduto += "<br/><span style='font-size: 75%;'>[ Cotação Compra - Aprovada ]</span>"; }
                        else if (RecordMovimento.fornecedor_cotacao_respondida == true) { nomeProduto += "<br/><span style='font-size: 75%;'>[ Cotação Compra - Respondida ]</span>"; }
                        else if (RecordMovimento.fornecedor_cotacao_solicitada == true) { nomeProduto += "<br/><span style='font-size: 75%;'>[ Cotação Compra - Solicitada ]</span>"; }
                        else if (RecordMovimento.fornecedor_cotacao_solicitar == true) { nomeProduto += "<br/><span style='font-size: 75%;'>[ Cotação Compra - Solicitar ]</span>"; };
                    }
                    string valorUnit = string
                        .Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.valor_unit)
                        .Replace("R$ ", "").Replace("R$", "").Replace("$", "");

                    string valorTotalColuna = "";
                    if (podeVerMarkup)
                    {
                        string valorTot = string
                            .Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.valor_total)
                            .Replace("R$ ", "").Replace("R$", "").Replace("$", "");

                        decimal mk = l.markup;
                        valorTotalColuna = valorTot + "<br/>" + "(" + mk.ToString("0.00") + " %)";
                    }

                    list.Add(new[]
                    {
                        "", // Seleção
                        l.id_movimento_item.ToString(),
                        l.sequencia.ToString().Replace(".00","").Replace(",00",""),
                        l.quantidade.ToString().Replace(",000","").Replace(",00",""),
                        nomeProduto,
                        valorUnit,
                        valorTotalColuna,
                        "", // Botão Duplicate
                        "", // Botão Editar
                        ""  // Botão Excluir
                    });
                }

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesDisplayField01 = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotalPedido).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalDisplayRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException ex)
            {
                errorMessage = LibExceptions.getDbEntityValidationException(ex);
                stackTrace = ex.ToString();
            }
            catch (WebException ex)
            {
                errorMessage = LibExceptions.getWebException(ex);
                stackTrace = ex.ToString();
            }
            catch (Exception ex)
            {
                errorMessage = LibExceptions.getExceptionShortMessage(ex);
                stackTrace = ex.ToString();
            }

            return Json(new
            {
                errorMessage,
                severity = "error",
                stackTrace, // em produção você pode retornar ""
                yesDisplayField01 = "0,00",
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ModalInserirItem(int? idMovimento, int? IdMoeda, int? IdCliente)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-dice-d6", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Inserir Novo Item</b>";
            ViewBag.idMovimento = idMovimento;
            gc_movimentos_itens record_gc_movimento_item = new Db.gc_movimentos_itens();
            record_gc_movimento_item.id_movimento = idMovimento.GetValueOrDefault();
            record_gc_movimento_item.id_movimento_item = -1;
            record_gc_movimento_item.quantidade = 1;
            record_gc_movimento_item.tag1_int = IdMoeda.GetValueOrDefault();
            record_gc_movimento_item.id_coligada = IdCliente.GetValueOrDefault();
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosTodos(db);
            ViewBag.comboProdutosCondicoes = LibDataSets.LoadComboGProdutoCondicao(db);
            ViewBag.comboEntregasPrazos = LibDataSets.LoadComboGcEntregasPrazos(db);
            ViewBag.dataSetProdutosServicos = LibDataSets.LoadDatasetGcProdutosServicos(db);
            return View("ModalPedidoInsertEditItem", record_gc_movimento_item);
        }

        public ActionResult ModalDuplicateItem(int? idMovimentoItem)
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
            return View("ModalPedidoInsertEditItem", record_gc_movimento_item);
        }


        [HttpPost]
        public ActionResult AjaxInsertEditItem(gc_movimentos_itens view_record_gc_movimento_item)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String MsgRetorno = "";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (ModelState.IsValid)
                {
                    if (view_record_gc_movimento_item.quantidade <= 0)
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += " - [Quantidade] não pode ser menor ou igual a zero!<br/>";
                    }
                    if (view_record_gc_movimento_item.valor_unit < 0)
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += " - [R$ Unit] não pode ser menor que zero!<br/>";
                    }
                    if ((view_record_gc_movimento_item.valor_unit == 0) && (view_record_gc_movimento_item.obs.Trim().Length == 0))
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += " - Para [R$ Unit] R$ 0,00 o campo [Obs] é obrigatório!<br/>";
                    }

                    if ((view_record_gc_movimento_item.id_produto == 0) || (view_record_gc_movimento_item.id_produto == -1))
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += " - [Produto] não foi informado!<br/>";
                    }
                }
                else
                {
                    MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }

                if (qtdInconsistencias == 0)
                {
                    gc_movimentos_itens record_gc_movimentos_itens = new Db.gc_movimentos_itens();

                    if (view_record_gc_movimento_item.id_movimento_item == -1)
                    {
                        if (view_record_gc_movimento_item.sequencia == 0)
                        {
                            decimal Sequencia = 0;
                            string _Sequencia = LibDB.dbQueryValue("select max(sequencia) from gc_movimentos_itens m where m.id_movimento = " + record_gc_movimentos_itens.id_movimento.ToString(), db);
                            decimal.TryParse(_Sequencia, out Sequencia);
                            Sequencia += 1;
                            view_record_gc_movimento_item.sequencia = Sequencia;
                        }
                        record_gc_movimentos_itens.id_movimento = view_record_gc_movimento_item.id_movimento;
                    }
                    else
                    {
                        record_gc_movimentos_itens = db.gc_movimentos_itens.Find(view_record_gc_movimento_item.id_movimento_item);
                    }
                    record_gc_movimentos_itens.id_produto = view_record_gc_movimento_item.id_produto;
                    record_gc_movimentos_itens.id_produto_condicao = view_record_gc_movimento_item.id_produto_condicao;
                    record_gc_movimentos_itens.id_entrega_prazo = view_record_gc_movimento_item.id_entrega_prazo;
                    record_gc_movimentos_itens.sequencia = view_record_gc_movimento_item.sequencia;
                    record_gc_movimentos_itens.quantidade = Math.Truncate(view_record_gc_movimento_item.quantidade);
                    record_gc_movimentos_itens.valor_unit = Math.Round(view_record_gc_movimento_item.valor_unit, 2);
                    record_gc_movimentos_itens.valor_total = Math.Round(view_record_gc_movimento_item.quantidade * view_record_gc_movimento_item.valor_unit, 2);
                    record_gc_movimentos_itens.valor_unit_corecharge = Math.Round(view_record_gc_movimento_item.valor_unit_corecharge, 2);
                    record_gc_movimentos_itens.valor_total_corecharge = Math.Round((view_record_gc_movimento_item.quantidade * view_record_gc_movimento_item.valor_unit_corecharge), 2);
                    record_gc_movimentos_itens.obs = view_record_gc_movimento_item.obs;
                    record_gc_movimentos_itens.obs_nf = view_record_gc_movimento_item.obs_nf;
                    record_gc_movimentos_itens.serial = view_record_gc_movimento_item.serial;
                    record_gc_movimentos_itens.lote01_identificador = view_record_gc_movimento_item.lote01_identificador;
                    record_gc_movimentos_itens.fornecedor_cotacao_solicitar = view_record_gc_movimento_item.fornecedor_cotacao_solicitar;

                    // Cálculo do Markup
                    g_produtos RecordProduto = db.g_produtos.Find(record_gc_movimentos_itens.id_produto);
                    if (RecordProduto != null)
                    {
                        Decimal ItemCustoReais = 0;
                        if ((RecordProduto.fob1_dollar > 0) && (CachePersister.userIdentity.CotacaoDollarDia > 0))
                        {
                            try
                            {
                                gc_movimentos RecordMovimento = db.gc_movimentos.Find(view_record_gc_movimento_item.id_movimento);
                                ItemCustoReais = RecordProduto.fob1_dollar * CachePersister.userIdentity.CotacaoDollarDia * record_gc_movimentos_itens.quantidade;
                                if (RecordMovimento != null)
                                {
                                    if (RecordMovimento.id_moeda == 2) { ItemCustoReais = RecordProduto.fob1_dollar * record_gc_movimentos_itens.quantidade; } // Moeda Dollar
                                }
                                else if (view_record_gc_movimento_item.tag1_int == 2) // Moeda Dollar
                                {
                                    ItemCustoReais = RecordProduto.fob1_dollar * record_gc_movimentos_itens.quantidade;
                                }
                                record_gc_movimentos_itens.markup = Math.Round(((record_gc_movimentos_itens.valor_total * 100) / ItemCustoReais) - 100, 2);
                                record_gc_movimentos_itens.fob_unit_dollar = RecordProduto.fob1_dollar;
                                record_gc_movimentos_itens.fob_unit_reais_venda = RecordProduto.fob1_dollar * CachePersister.userIdentity.CotacaoDollarDia;
                            }
                            catch (Exception) { };
                        };
                    };
                    
                    if (view_record_gc_movimento_item.id_movimento_item == -1)
                    {
                        record_gc_movimentos_itens.datahora_cadastro = DataHoraAtual;
                        record_gc_movimentos_itens.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        db.gc_movimentos_itens.Add(record_gc_movimentos_itens);
                    }
                    else
                    {
                        record_gc_movimentos_itens.datahora_alteracao = DataHoraAtual;
                        record_gc_movimentos_itens.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_movimentos_itens).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ModalEditarItem(int? IdItem)
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Editar Item";
            gc_movimentos_itens record_gc_movimento_item = db.gc_movimentos_itens.Find(IdItem);
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosTodos(db);
            ViewBag.comboProdutosCondicoes = LibDataSets.LoadComboGProdutoCondicao(db);
            ViewBag.comboEntregasPrazos = LibDataSets.LoadComboGcEntregasPrazos(db);
            ViewBag.dataSetProdutosServicos = LibDataSets.LoadDatasetGcProdutosServicos(db);
            return View("ModalPedidoInsertEditItem", record_gc_movimento_item);
        }

        [HttpPost]
        public ActionResult AjaxRemoverItem(gc_movimentos_itens record_gc_movimento_item)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            try
            {
                String SqlDelete = "delete from  gc_movimentos_itens where id_movimento_item = " + record_gc_movimento_item.id_movimento_item.ToString(); // O Id, será o negativo do id do usuário;
                DataTable tableRegistroDeleteExtratos = LibDB.GetDataTable(SqlDelete, db);
                Sucesso = true;
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AjaxReportInvoicePDF(int? id, string saida)
        {
            bool Sucesso = false;
            String MsgRetorno = string.Empty;
            String bodyEmail = string.Empty;
            String idProcessamentoGravado = "0";
            String paramFromEmail = string.Empty;
            String paramFromNome = string.Empty;
            String FileNamePDFReport = string.Empty;
            String notificacoesEmails = string.Empty;
            String notificacoesContatos = string.Empty;

            int notificacoesQtd = 0;
            var pdf = new ViewAsPdf();

            try
            {
                DateTime dataAtual = LibDateTime.getDataHoraBrasilia();
                String dirExportacaoPDF = String.Empty;
                String formatoMoeda = String.Empty;
                dirExportacaoPDF = Server.MapPath("~/_filestemp");
                if (!Directory.Exists(dirExportacaoPDF)) { Directory.CreateDirectory(dirExportacaoPDF); }
                dirExportacaoPDF = Path.Combine(dirExportacaoPDF, "reports");
                if (!Directory.Exists(dirExportacaoPDF)) { Directory.CreateDirectory(dirExportacaoPDF); }
                dirExportacaoPDF = Path.Combine(dirExportacaoPDF, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                if (!Directory.Exists(dirExportacaoPDF)) { Directory.CreateDirectory(dirExportacaoPDF); }
                LibFilesDisk.DeleteFilesInDirectory(dirExportacaoPDF); // Apagar todos os arquivos que estiveremno diretório do usuario
                int id_movimento = id.GetValueOrDefault();
                gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
                cstInvoice record_cstInvoice = new cstInvoice();
                if (record_gc_movimento != null)
                {
                    var listMovimento = (from _m in db.gc_movimentos
                                         join _c in db.g_clientes on _m.id_cliente equals _c.id_cliente
                                         where _m.id_movimento == id_movimento
                                         select new { tableMovimento = _m, tableCliente = _c }).ToList();

                    var listItens = (from _i in db.gc_movimentos_itens
                                     join _p in db.g_produtos on _i.id_produto equals _p.id_produto
                                     where _i.id_movimento == id_movimento
                                     orderby _i.sequencia, _i.id_movimento_item
                                     select new { tableItens = _i, nomeProduto = _p.nome, codigoProduto = _p.codigo }).ToList();

                    var listIdsContatosTemp = listMovimento.FirstOrDefault().tableMovimento.notifica_contatos_ids.EmptyIfNull().ToString().Split(';');
                    List<int> listIdsContatos = new List<int>();
                    if (listIdsContatosTemp.Length > 0)
                    {
                        for (int i = 0; i <= listIdsContatosTemp.Length; i++)
                        {
                            try
                            {
                                string idTemp = listIdsContatosTemp[i];
                                if (idTemp.Trim().Length > 0) { listIdsContatos.Add(int.Parse(listIdsContatosTemp[i])); };
                            }
                            catch { };
                        }
                    }
                    List<g_clientes_contatos> listContatos = db.g_clientes_contatos.Where(c => listIdsContatos.Contains(c.id_contato)).ToList();
                    g_vendedores record_g_vendedores = db.g_vendedores.Find(listMovimento.FirstOrDefault().tableMovimento.id_vendedor);
                    gc_parametros record_gc_parametros = db.gc_parametros.Find(1);
                    g_pagrec_condicoes record_g_pagrec_condicoes = db.g_pagrec_condicoes.Find(listMovimento.FirstOrDefault().tableMovimento.id_pagrec_condicao);
                    g_moedas record_g_moedas = db.g_moedas.Find(listMovimento.FirstOrDefault().tableMovimento.id_moeda);
                    List<g_produtos_condicoes> allProdutosCondicoes = db.g_produtos_condicoes.Where(c => (c.id_produto_condicao > 0)).ToList();
                    List<gc_entregas_prazos> allEntregasPrazos = db.gc_entregas_prazos.Where(p => (p.id_entrega_prazo > 0)).ToList();

                    if (record_g_moedas != null)
                    {
                        record_cstInvoice.invoice_moeda_nome = record_g_moedas.descricao.EmptyIfNull().ToString();
                        if (record_g_moedas.id_moeda == 1)
                        {
                            record_cstInvoice.invoice_moeda_flag = "flag_brazil.png";
                            formatoMoeda = "pt-BR";

                        }
                        else if (record_g_moedas.id_moeda == 2)
                        {
                            record_cstInvoice.invoice_moeda_flag = "flag_usa.png";
                            formatoMoeda = "en-US";
                        }
                    }

                    if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 3) { record_cstInvoice.invoice_tipo = "Cotação"; }
                    else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 4) { record_cstInvoice.invoice_tipo = "Pedido"; }
                    else if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 8) { record_cstInvoice.invoice_tipo = "Ordem de Serviço"; }
                    if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 1) { record_cstInvoice.invoice_numero = "####"; }
                    else { record_cstInvoice.invoice_numero = listMovimento.FirstOrDefault().tableMovimento.id_movimento.EmptyIfNull().ToString(); }

                    if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo == 8)
                    {
                        record_cstInvoice.invoice_numero += DateTime.Now.ToString("ss");
                        record_cstInvoice.invoice_data = listMovimento.FirstOrDefault().tableMovimento.datahora_cadastro.ToString("MM/dd/yyyy");
                    }
                    else
                    {
                        record_cstInvoice.invoice_data = listMovimento.FirstOrDefault().tableMovimento.datahora_alteracao.GetValueOrDefault().ToString("MM/dd/yyyy");
                    }

                    record_cstInvoice.invoice_data_vencimento = listMovimento.FirstOrDefault().tableMovimento.data_vencimento.GetValueOrDefault().ToString("MM/dd/yyyy");
                    record_cstInvoice.cliente_nome = listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString();
                    record_cstInvoice.invoice_aeronave_prefixo = listMovimento.FirstOrDefault().tableMovimento.aeronave_prefixo.EmptyIfNull().ToString();
                    record_cstInvoice.invoice_aeronave_modelo = listMovimento.FirstOrDefault().tableMovimento.aeronave_modelo.EmptyIfNull().ToString();
                    record_cstInvoice.invoice_aeronave_serie = listMovimento.FirstOrDefault().tableMovimento.aeronave_serie.EmptyIfNull().ToString();
                    record_cstInvoice.invoice_aeronave_registro = listMovimento.FirstOrDefault().tableMovimento.aeronave_registro.EmptyIfNull().ToString();
                    record_cstInvoice.cliente_endereco1 = listMovimento.FirstOrDefault().tableCliente.endereco_com.EmptyIfNull().ToString() + ", "+ listMovimento.FirstOrDefault().tableCliente.endereco_com_numero.EmptyIfNull().ToString() + " " + listMovimento.FirstOrDefault().tableCliente.endereco_com_complemento.EmptyIfNull().ToString() + " - " + listMovimento.FirstOrDefault().tableCliente.bairro_com.EmptyIfNull().ToString();
                    record_cstInvoice.invoice_subtotal_value = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.valor_total_liquido);
                    if (listMovimento.FirstOrDefault().tableMovimento.frete_valor > 0) { record_cstInvoice.invoice_total_freight = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.frete_valor); } else { record_cstInvoice.invoice_total_freight = string.Empty; };
                    if (listMovimento.FirstOrDefault().tableMovimento.valor_total_corecharge > 0) { record_cstInvoice.invoice_total_corecharges = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.valor_total_corecharge); } else { record_cstInvoice.invoice_total_corecharges = string.Empty; };
                    record_cstInvoice.invoice_grantotal_value = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.valor_total_liquido + listMovimento.FirstOrDefault().tableMovimento.frete_valor + listMovimento.FirstOrDefault().tableMovimento.valor_total_corecharge);

                    if (record_g_vendedores != null)
                    {
                        record_cstInvoice.vendedor_nome = record_g_vendedores.nome.EmptyIfNull().ToString();
                        record_cstInvoice.vendedor_telefone = record_g_vendedores.telefone_1.EmptyIfNull().ToString();
                        record_cstInvoice.vendedor_email = record_g_vendedores.email.EmptyIfNull().ToString();

                        if (record_g_vendedores.email_autenticado == true)
                        {
                            paramFromEmail = record_g_vendedores.email.EmptyIfNull().Trim();
                            paramFromNome = "GDI Aviação - " + record_g_vendedores.nome.EmptyIfNull().Trim();
                        }
                        else
                        {
                            paramFromNome = "GDI Aviação - Atendimento Comercial";
                        }
                    }
                    else
                    {
                        paramFromNome = "GDI Aviação - Atendimento Comercial";
                    }

                    if ((listMovimento.FirstOrDefault().tableCliente.email_principal.EmptyIfNull().ToString().Length > 0) && (listIdsContatos.Count > 0) && (listIdsContatos[0] == 0)) // Contato Principal do Cadastro
                    {
                        notificacoesContatos += record_cstInvoice.cliente_nome + ";";
                        notificacoesEmails += listMovimento.FirstOrDefault().tableCliente.email_principal.EmptyIfNull().ToString() + ";";
                        notificacoesQtd += 1;
                    }
                    if (listContatos != null)
                    {
                        record_cstInvoice.contato_nome = String.Empty;
                        record_cstInvoice.contato_telefone = String.Empty;
                        record_cstInvoice.contato_email = String.Empty;
                        for (int i = 0; i < listContatos.Count(); i++)
                        {
                            record_cstInvoice.contato_nome += listContatos[i].contato.EmptyIfNull().ToString() + " / ";
                            record_cstInvoice.contato_telefone += listContatos[i].telefone.EmptyIfNull().ToString() + " / ";
                            record_cstInvoice.contato_email += listContatos[i].email.EmptyIfNull().ToString() + " / ";
                            notificacoesContatos += listContatos[i].contato.EmptyIfNull().ToString() + ";";
                            notificacoesEmails += listContatos[i].email.EmptyIfNull().ToString() + ";";
                            notificacoesQtd += 1;
                        }
                        try
                        {
                            record_cstInvoice.contato_nome = record_cstInvoice.contato_nome.Substring(0, record_cstInvoice.contato_nome.LastIndexOf("/"));
                            record_cstInvoice.contato_telefone = record_cstInvoice.contato_telefone.Substring(0, record_cstInvoice.contato_telefone.LastIndexOf("/"));
                            record_cstInvoice.contato_email = record_cstInvoice.contato_email.Substring(0, record_cstInvoice.contato_email.LastIndexOf("/"));
                        }
                        catch (Exception) { };
                    }

                    // Observações sobre o pedido
                    record_cstInvoice.invoice_obs = String.Empty;
                    if (listMovimento.FirstOrDefault().tableMovimento.icms_difal_calculado > 0)
                    {
                        try
                        {
                            String ValorDifal = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.icms_difal_calculado);
                            record_cstInvoice.invoice_obs += "<b>Difal:</b> Caso o cliente não possua Inscrição Estadual,<br/>O pedido será acrescido de " + ValorDifal + " referente à diferença da aliquota do ICMS!<br/><br/>";
                        }
                        catch (Exception) { };
                    }
                    if (listMovimento.FirstOrDefault().tableMovimento.obs.EmptyIfNull().ToString().Length > 0) { record_cstInvoice.invoice_obs += listMovimento.FirstOrDefault().tableMovimento.obs.EmptyIfNull().Replace("\r\n", "<br/>") + "<br/>"; };
                    if (listMovimento.FirstOrDefault().tableMovimento.obs_negociacao.EmptyIfNull().ToString().Length > 0) { record_cstInvoice.invoice_obs += listMovimento.FirstOrDefault().tableMovimento.obs_negociacao.EmptyIfNull().Replace("\r\n", "<br/>") + "<br/>"; };

                    if (record_gc_parametros != null)
                    {
                        string Template1 = string.Empty;
                        if (listMovimento.FirstOrDefault().tableMovimento.id_movimento_tipo != 8) // 8 = Ordem de Serviço
                        {
                            g_templates RecordTemplateObsCotacaoPedido = null;
                            if (record_gc_movimento.id_local_estoque == 1) { RecordTemplateObsCotacaoPedido = db.g_templates.Where(t => t.localizador == "GcMovimentosObservacoesCotacaoPedidoBH").FirstOrDefault(); }
                            else if (record_gc_movimento.id_local_estoque == 3) { RecordTemplateObsCotacaoPedido = db.g_templates.Where(t => t.localizador == "GcMovimentosObservacoesCotacaoPedidoSP").FirstOrDefault(); }
                            if (RecordTemplateObsCotacaoPedido != null) { Template1 = RecordTemplateObsCotacaoPedido.template.EmptyIfNull().Trim(); }
                            record_cstInvoice.invoice_obs_general = Template1;
                        }
                        bodyEmail = Template1;
                    }
                    if (record_g_pagrec_condicoes != null) { record_cstInvoice.invoice_condicao_pagto = record_g_pagrec_condicoes.descricao.EmptyIfNull().ToString().Trim(); }

                    // Verificar sequenciamento
                    bool hasSequencia = true;
                    foreach (var item in listItens)
                    {
                        if (item.tableItens.sequencia == 0) { hasSequencia = false; };
                    }

                    int indexItem = 0;
                    int PaginaAtual = 1;
                    int ProximaQuebraPagina = 15;
                    foreach (var item in listItens)
                    {
                        indexItem += 1;
                        cstInvoiceItem record_cstInvoiceItem = new cstInvoiceItem();

                        // Numero da Página
                        record_cstInvoiceItem.pagina = PaginaAtual;
                        if (indexItem == ProximaQuebraPagina)
                        {
                            PaginaAtual += 1;
                            ProximaQuebraPagina += 23;
                        };

                        if (hasSequencia == true)
                        {
                            record_cstInvoiceItem.indexItem = item.tableItens.sequencia.ToString("N1").Replace(",", ".").Replace(".0", "");
                        }
                        else
                        {
                            record_cstInvoiceItem.indexItem = indexItem.ToString();
                        };

                        record_cstInvoiceItem.qtd = item.tableItens.quantidade.EmptyIfNull().ToString();
                        String NomeProduto = item.nomeProduto.EmptyIfNull().ToString();
                        if (NomeProduto.Length > 200) { NomeProduto = NomeProduto.Substring(0, 200) + "..."; };
                        
                        if (record_cstInvoiceItem.produto_codigo.EmptyIfNull().ToString().Trim().Length > 0)
                        {
                            record_cstInvoiceItem.produto_nome = record_cstInvoiceItem.produto_codigo.ToString().Trim() + " | " + NomeProduto;
                        }
                        else
                        {
                            record_cstInvoiceItem.produto_nome = NomeProduto;
                        };
                        
                        if (item.tableItens.serial.EmptyIfNull().ToString().Trim().Length > 0) { record_cstInvoiceItem.produto_nome += "|Serial:" + item.tableItens.serial.EmptyIfNull().ToString().Trim(); };
                        if (item.tableItens.lote01_identificador.EmptyIfNull().ToString().Trim().Length > 0) { record_cstInvoiceItem.produto_nome += "|Lote:" + item.tableItens.lote01_identificador.EmptyIfNull().ToString().Trim(); };
                        if (item.tableItens.obs.EmptyIfNull().ToString().Trim().Length > 0) { record_cstInvoiceItem.produto_nome += "|Obs:" + item.tableItens.obs.EmptyIfNull().ToString().Trim(); };
                        if (item.tableItens.valor_total_corecharge > 0) { record_cstInvoiceItem.produto_nome += "<br/>" + "Core Charge - Standard Exchange   " + string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", item.tableItens.valor_total_corecharge); };

                        record_cstInvoiceItem.valor_unit = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", item.tableItens.valor_unit);
                        record_cstInvoiceItem.valor_total = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", item.tableItens.valor_total);
                        record_cstInvoiceItem.cd = allProdutosCondicoes.Find(c => (c.id_produto_condicao == item.tableItens.id_produto_condicao)).sigla.EmptyIfNull().ToString();
                        record_cstInvoiceItem.delivery = allEntregasPrazos.Find(p => (p.id_entrega_prazo == item.tableItens.id_entrega_prazo)).sigla.EmptyIfNull().ToString();
                        record_cstInvoice.AllItens.Add(record_cstInvoiceItem);
                    }
                    record_cstInvoice.invoice_qtd_paginas = PaginaAtual;
                }

                ViewBag.ImgLogoSubdominio = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/logoGdi.png";
                FileNamePDFReport = record_cstInvoice.invoice_tipo + "_" + record_cstInvoice.invoice_numero + ".pdf";
                FileNamePDFReport = Path.Combine(dirExportacaoPDF, FileNamePDFReport);
                if (System.IO.File.Exists(FileNamePDFReport)) System.IO.File.Delete(FileNamePDFReport);
                pdf = new ViewAsPdf
                {
                    ViewName = "ReportInvoicePDF",
                    Model = record_cstInvoice
                };
                byte[] applicationPDFData_BL = pdf.BuildFile(ControllerContext);
                var fileStream_BL = new FileStream(FileNamePDFReport, FileMode.Create, FileAccess.Write);
                fileStream_BL.Write(applicationPDFData_BL, 0, applicationPDFData_BL.Length);
                fileStream_BL.Close();
                Sucesso = true;

                if (saida.Equals("email"))
                {
                    // Envio Email
                    idProcessamentoGravado = "0";
                    String tituloEmail = "GDI Aviação - " + record_cstInvoice.invoice_tipo.EmptyIfNull().ToString() + " Nº #" + record_cstInvoice.invoice_numero.EmptyIfNull().ToString();
                    if (bodyEmail.EmptyIfNull().ToString().Trim().Length == 0)
                    {
                        bodyEmail = "Arquivo Anexo - " + record_cstInvoice.invoice_tipo.EmptyIfNull().ToString() + " Nº #" + record_cstInvoice.invoice_numero.EmptyIfNull().ToString();
                    }
                    else
                    {
                        string taginvoice = record_cstInvoice.invoice_tipo.EmptyIfNull().ToString() + " Nº #" + record_cstInvoice.invoice_numero.EmptyIfNull().ToString();
                        bodyEmail = bodyEmail.Replace("#taginvoice", taginvoice);
                    }
                    List<string> ListaAnexos = new List<string>();
                    ListaAnexos.Add(FileNamePDFReport);
                    BotAwsEmail RoboAwsEmail = new BotAwsEmail();
                    RoboAwsEmail.EnviarEmailAWS(paramFromEmail, paramFromNome, notificacoesEmails, notificacoesContatos, tituloEmail, bodyEmail, ListaAnexos);
                    MsgRetorno = record_cstInvoice.invoice_tipo.EmptyIfNull().ToString() + " Digital gerado com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" +
                                    "Notificações: " + notificacoesQtd.ToString() + "<br/><br/>" +
                                    "Contatos: " + notificacoesContatos.ToString() + "<br/><br/>";
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = idProcessamentoGravado, url = FileNamePDFReport }, JsonRequestBehavior.AllowGet);
        }

        public cstPosicaoFinanceiraCliente GetPosicaoFinanceiraCliente(int IdCliente)
        {
            cstPosicaoFinanceiraCliente PosicaoFinanceiraCliente = new cstPosicaoFinanceiraCliente();
            try
            {
                DateTime DataAtual = LibDateTime.getDataHoraBrasilia();

                // Limite de Crédito Cadastral
                g_clientes RecordCliente = db.g_clientes.Find(IdCliente);
                PosicaoFinanceiraCliente.LimiteCreditoTotal = RecordCliente.gc_limite_credito;

                // Títulos Vencidos
                List<gc_financeiro_lancamentos> ListaLancamentosFinanceirosCliente = db.gc_financeiro_lancamentos.Where(p => p.ativo == true && p.tipo_pag_rec == 2 && p.id_financeiro_status == 3 && p.id_cliente == IdCliente).ToList();
                foreach (gc_financeiro_lancamentos Lancamento in ListaLancamentosFinanceirosCliente)
                {
                    if (Lancamento.data_vencimento.Date < DataAtual.AddDays(-3).Date)
                    {
                        if ((Lancamento.negociacao == true) && (Lancamento.negociacao_data_limite > DataAtual))
                        {
                            PosicaoFinanceiraCliente.TitulosNegociacaoQtd += 1;
                            PosicaoFinanceiraCliente.TitulosNegociacaoValor += Lancamento.valor_total;
                        }
                        else
                        {
                            PosicaoFinanceiraCliente.TitulosVencidosQtd += 1;
                            PosicaoFinanceiraCliente.TitulosVencidosValor += Lancamento.valor_total;
                        }
                    }
                    else
                    {
                        PosicaoFinanceiraCliente.TitulosAVencerQtd += 1;
                        PosicaoFinanceiraCliente.TitulosAVencerValor += Lancamento.valor_total;
                    }
                }
                PosicaoFinanceiraCliente.LimiteCreditoUtilizado = PosicaoFinanceiraCliente.TitulosNegociacaoValor + PosicaoFinanceiraCliente.TitulosVencidosValor + PosicaoFinanceiraCliente.TitulosAVencerValor;
                PosicaoFinanceiraCliente.LimiteCreditoRestante = PosicaoFinanceiraCliente.LimiteCreditoTotal - PosicaoFinanceiraCliente.LimiteCreditoUtilizado;
            }
            finally { }
            return PosicaoFinanceiraCliente;
        }

        public ActionResult AjaxDadosProduto(g_produtos view_g_produtos)
        {
            bool Sucesso = true;
            String MsgRetorno = "";
            String PrecoVenda = "0";
            try
            {
                List<cstDatasetProdutosServicos> ListaProdutosServicos = LibDataSets.LoadDatasetGcProdutosServicos(db);
                cstDatasetProdutosServicos record_cstDatasetProdutosServicos = ListaProdutosServicos.Where(l => l.id_produto_servico == view_g_produtos.id_produto).FirstOrDefault();
                if (record_cstDatasetProdutosServicos != null)
                {
                    PrecoVenda = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_cstDatasetProdutosServicos.preco_venda).Replace("R$ ", "").Replace("R$", "").Replace("$", "").Replace(".", "");
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, valor = PrecoVenda }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxGetPrecoVendaProduto(gc_movimentos_itens view_gc_movimentos_itens)
        {
            bool Sucesso = false;
            string MsgRetorno = String.Empty;
            decimal PrecoVendaCalculado = 0;
            string PrecoVenda = "0";
            string Fob1Dollar = "0";
            string SaldoBHDisponivel = "0";
            string SaldoSPDisponivel = "0";

            try
            {
                List<cstDatasetProdutosServicos> ListaProdutosServicos = LibDataSets.LoadDatasetGcProdutosServicos(db);
                cstDatasetProdutosServicos RecordProduto = ListaProdutosServicos.Where(p => p.id_produto_servico == view_gc_movimentos_itens.id_produto).FirstOrDefault();
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(view_gc_movimentos_itens.id_movimento);
                if (RecordProduto != null)
                {
                    SaldoBHDisponivel = RecordProduto.saldo_01_disponivel.ToString().Replace(",000", "").Replace(",00", "");
                    SaldoSPDisponivel = RecordProduto.saldo_03_disponivel.ToString().Replace(",000", "").Replace(",00", "");
                    if (RecordProduto.fob1_dollar > 0)
                    {
                        Fob1Dollar = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordProduto.fob1_dollar).Replace("R$ ", "").Replace("R$", "").Replace("$", "").Replace(".", "");
                        if ((RecordProduto.fob1_dollar > 0) && (CachePersister.userIdentity.CotacaoDollarDia > 0)) { PrecoVendaCalculado = (RecordProduto.fob1_dollar * CachePersister.userIdentity.CotacaoDollarDia * 2); } // Markup 100% 
                        else if (RecordProduto.preco_venda > 0) { PrecoVendaCalculado = RecordProduto.preco_venda; };
                        if (RecordMovimento != null)
                        {
                            if (RecordMovimento.id_moeda == 2)
                            {
                                if ((RecordProduto.fob1_dollar > 0) && (CachePersister.userIdentity.CotacaoDollarDia > 0)) { PrecoVendaCalculado = (RecordProduto.fob1_dollar * 2); } // Markup 100% 
                                else if (RecordProduto.preco_venda > 0) { PrecoVendaCalculado = RecordProduto.preco_venda / CachePersister.userIdentity.CotacaoDollarDia; };
                            }
                        }
                        PrecoVenda = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PrecoVendaCalculado).Replace("R$ ", "").Replace("R$", "").Replace("$", "").Replace(".", "");

                    }
                }
                Sucesso = true;
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, preco_venda = PrecoVenda, fob1_dollar = Fob1Dollar, saldo_01_disponivel = SaldoBHDisponivel, saldo_03_disponivel = SaldoSPDisponivel }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxGetDetailsProduct(g_produtos view_g_produtos)
        {
            bool sucesso = false;
            string msgRetorno = "";
            decimal precoVendaCalc = 0m;
            string precoVenda = "0";

            try
            {
                int idProduto = view_g_produtos?.id_produto ?? 0;
                if (idProduto <= 0)
                    return Json(new { success = false, msg = "Produto inválido.", preco_venda = "0" }, JsonRequestBehavior.AllowGet);

                // 1) Carrega somente o produto necessário (evita dataset gigante em memória)
                g_produtos produto = db.g_produtos.Find(idProduto);

                if (produto == null)
                    return Json(new { success = false, msg = "Produto não encontrado.", preco_venda = "0" }, JsonRequestBehavior.AllowGet);

                // 2) Lookups (NCM / Unidade) - NoTracking
                var ncm = (produto.id_produto_ncm > 0)
                    ? db.g_produtos_ncm.AsNoTracking().FirstOrDefault(n => n.id_produto_ncm == produto.id_produto_ncm)
                    : null;

                var unidade = (produto.id_unidade_medida_venda > 0)
                    ? db.g_unidade_medida.AsNoTracking().FirstOrDefault(u => u.id_unidade_medida == produto.id_unidade_medida_venda)
                    : null;

                // 3) Importações referenciadas (somente as 3 IDs do produto)
                var importIds = new[] { produto.fob1_id_importacao, produto.fob2_id_importacao, produto.fob3_id_importacao }
                    .Where(x => x > 0)
                    .Distinct()
                    .ToList();

                var importacoes = (importIds.Count == 0)
                    ? new Dictionary<int, gc_comex_importacoes>()
                    : db.gc_comex_importacoes.AsNoTracking()
                        .Where(i => i.ativo && importIds.Contains(i.id_importacao))
                        .ToDictionary(i => i.id_importacao);

                // 4) Itens das importações desse produto (para Qtd) - 1 query só
                var itensPorImportacao = (importIds.Count == 0)
                    ? new Dictionary<int, gc_comex_importacoes_itens>()
                    : db.gc_comex_importacoes_itens.AsNoTracking()
                        .Where(ii => ii.id_produto == produto.id_produto && importIds.Contains(ii.id_importacao))
                        .GroupBy(ii => ii.id_importacao)
                        .Select(g => g.OrderByDescending(x => x.id_importacao_item).FirstOrDefault()) // pega 1 registro por importação (ajuste se quiser somar)
                        .ToList()
                        .Where(x => x != null)
                        .ToDictionary(x => x.id_importacao);

                // Helpers (evita Replace duplicado)
                string MoedaPtBr(decimal v) =>
                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", v)
                        .Replace("R$ ", "").Replace("R$", "").Replace("$", "").Replace(".", "");

                string EstoqueFmt(decimal v) =>
                    v.ToString().Replace(",000", "").Replace(",00", "");

                // 5) Mensagem (StringBuilder)
                var sb = new System.Text.StringBuilder();

                sb.Append("<b>Produto:</b> ").Append(produto.descricao).Append("<br/><br/>");

                sb.Append("<b>Estoque BH:</b> ").Append(EstoqueFmt(produto.saldo_01_disponivel))
                  .Append(" | <b>Estoque SP:</b> ").Append(EstoqueFmt(produto.saldo_03_disponivel))
                  .Append("<br/><br/>");

                sb.Append("<b>Id:</b> ").Append(produto.id_produto)
                  .Append(" | <b>Cód</b>: ").Append(produto.codigo);

                if (produto.has_corecharge) sb.Append(" | <b>Core Charge</b>");
                if (ncm != null) sb.Append(" | <b>NCM:</b> ").Append(ncm.codigo_ncm.EmptyIfNull().ToString());
                if (unidade != null) sb.Append(" | <b>Unidade:</b> ").Append(unidade.descricao.EmptyIfNull().ToString());

                sb.Append("<br/><br/>");

                // 6) Preço venda (mesma regra do seu código, só organizada)
                if (produto.fob1_dollar > 0)
                {
                    if (CachePersister.userIdentity.CotacaoDollarDia > 0)
                        precoVendaCalc = produto.fob1_dollar * CachePersister.userIdentity.CotacaoDollarDia * 2m; // markup 100%
                    else if (produto.preco_venda > 0)
                        precoVendaCalc = produto.preco_venda;

                    precoVenda = MoedaPtBr(precoVendaCalc);
                }

                // 7) FOBs (sem repetir código)
                void AppendFob(int idx, decimal fobDollar, int idImportacao)
                {
                    if (fobDollar <= 0) return;

                    sb.Append("<b>Fob").Append(idx).Append(":</b> ").Append(MoedaPtBr(fobDollar));

                    if (idImportacao > 0 && importacoes.TryGetValue(idImportacao, out var imp))
                    {
                        sb.Append(" | ").Append(imp.numero.EmptyIfNull().ToString())
                          .Append("  (").Append(imp.data_registro.ToString("dd/MM/yyyy")).Append(")");

                        if (itensPorImportacao.TryGetValue(idImportacao, out var itemImp) && itemImp != null)
                            sb.Append(" | Qtd: ").Append(itemImp.quantidade.ToString());
                    }

                    sb.Append("<br/>");
                }

                AppendFob(1, produto.fob1_dollar, produto.fob1_id_importacao);
                AppendFob(2, produto.fob2_dollar, produto.fob2_id_importacao);
                AppendFob(3, produto.fob3_dollar, produto.fob3_id_importacao);

                sucesso = true;
                msgRetorno = sb.ToString();
            }
            catch (DbEntityValidationException ex)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception ex)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(ex);
            }

            return Json(new
            {
                success = sucesso,
                msg = msgRetorno,
                preco_venda = precoVenda
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxDadosHistoricoComercialItemGeral(g_produtos view_g_produtos)
        {
            bool sucesso = false;
            string msgRetorno = "";

            try
            {
                int idProduto = view_g_produtos?.id_produto ?? 0;
                if (idProduto <= 0)
                    return Json(new { success = false, msg = "Produto inválido." }, JsonRequestBehavior.AllowGet);

                // ✅ 1) Uma consulta só (sem string SQL / sem UNION), parametrizada via LINQ
                // Regra original:
                // - Tipos: 3,4,8
                // - Status: pega TOP 5 de status=2 e TOP 5 de status=1 (por datahora_alteracao desc)
                // Aqui eu trago os dois TOPs separadamente (2 queries leves) e monto a saída.
                var baseQuery =
                    from item in db.gc_movimentos_itens.AsNoTracking()
                    join mov in db.gc_movimentos.AsNoTracking() on item.id_movimento equals mov.id_movimento
                    join cli in db.g_clientes.AsNoTracking() on mov.id_cliente equals cli.id_cliente
                    join ven in db.g_vendedores.AsNoTracking() on mov.id_vendedor equals ven.id_vendedor
                    where (mov.id_movimento_tipo == 3 || mov.id_movimento_tipo == 4 || mov.id_movimento_tipo == 8)
                          && item.id_produto == idProduto
                          && mov.datahora_alteracao != null
                    select new
                    {
                        item.quantidade,
                        item.valor_unit,
                        item.valor_total,
                        mov.id_movimento,
                        mov.id_movimento_status,
                        mov.datahora_alteracao,
                        mov.id_moeda,
                        Cliente = cli.nome,
                        Vendedor = ven.nome
                    };

                var topPedidos = baseQuery
                    .Where(x => x.id_movimento_status == 2)
                    .OrderByDescending(x => x.datahora_alteracao)
                    .Take(5)
                    .ToList();

                var topCotacoes = baseQuery
                    .Where(x => x.id_movimento_status == 1)
                    .OrderByDescending(x => x.datahora_alteracao)
                    .Take(5)
                    .ToList();

                var allItens = topCotacoes.Concat(topPedidos).ToList();

                if (allItens.Count == 0)
                {
                    msgRetorno = "<font size='6'>Não foram localizados Pedidos/Cotações com o item selecionado!</font><br/>";
                    sucesso = true;
                    return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
                }

                // ✅ 2) Cálculos (min/max/média) sem parse de DataRow
                decimal valorMin = 0m;
                decimal valorMax = 0m;
                decimal totalValores = 0m;
                decimal totalQtd = 0m;

                string TrimCliente(string s)
                {
                    s = (s ?? "").Trim();
                    return s.Length > 20 ? (s.Substring(0, 20) + "...") : s;
                }

                string PrimeiroNome(string s)
                {
                    s = (s ?? "").Trim();
                    int idx = s.IndexOf(" ");
                    return idx > 0 ? s.Substring(0, idx).Trim() : s;
                }

                string Num2(decimal v) => v.ToString("###,###,##0.00");
                string Dt(DateTime? d) => (d ?? DateTime.MinValue).ToString("dd/MM/yy");

                var sb = new System.Text.StringBuilder();

                sb.Append("<font size='2px'>");
                sb.Append("<b>Id.</b> | Data | Vendedor | Cliente | Qtd x R$ Unit<br/>");
                sb.Append("<b>---------- Cotações ----------</b><br/>");

                foreach (var r in topCotacoes)
                {
                    decimal qtd = r.quantidade;
                    decimal unit = r.valor_unit;
                    decimal tot = r.valor_total;

                    totalValores += tot;
                    totalQtd += qtd;

                    if (valorMin == 0m && unit > 0m) valorMin = unit;
                    if (valorMax == 0m && unit > 0m) valorMax = unit;
                    if (unit > 0m && unit < valorMin) valorMin = unit;
                    if (unit > valorMax) valorMax = unit;

                    string formatoMoedaCot = "pt-BR";
                    if (r.id_moeda == 2) { formatoMoedaCot = "en-US"; }
                    string unitFormatCot = string.Format(CultureInfo.GetCultureInfo(formatoMoedaCot), "{0:C}", unit);
                    if (r.id_moeda == 2) { unitFormatCot = unitFormatCot.Replace("$", "$ "); }

                    sb.Append("<b>").Append(r.id_movimento).Append("</b> | ");
                    sb.Append(Dt(r.datahora_alteracao)).Append(" | ");
                    sb.Append(PrimeiroNome(r.Vendedor)).Append(" | ");
                    sb.Append(TrimCliente(r.Cliente)).Append(" | ");
                    sb.Append(decimal.Truncate(qtd)).Append(" x ").Append(unitFormatCot);
                    sb.Append("<br/>");
                }

                sb.Append("<b>---------- Pedidos ----------</b><br/>");

                foreach (var r in topPedidos)
                {
                    decimal qtd = r.quantidade;
                    decimal unit = r.valor_unit;
                    decimal tot = r.valor_total;

                    totalValores += tot;
                    totalQtd += qtd;

                    if (valorMin == 0m && unit > 0m) valorMin = unit;
                    if (valorMax == 0m && unit > 0m) valorMax = unit;
                    if (unit > 0m && unit < valorMin) valorMin = unit;
                    if (unit > valorMax) valorMax = unit;

                    string formatoMoedaPed = "pt-BR";
                    if (r.id_moeda == 2) { formatoMoedaPed = "en-US"; }
                    string unitFormatPed = string.Format(CultureInfo.GetCultureInfo(formatoMoedaPed), "{0:C}", unit);
                    if (r.id_moeda == 2) { unitFormatPed = unitFormatPed.Replace("$", "$ "); }

                    sb.Append("<b>").Append(r.id_movimento).Append("</b> | ");
                    sb.Append(Dt(r.datahora_alteracao)).Append(" | ");
                    sb.Append(PrimeiroNome(r.Vendedor)).Append(" | ");
                    sb.Append(TrimCliente(r.Cliente)).Append(" | ");
                    sb.Append(decimal.Truncate(qtd)).Append(" x ").Append(unitFormatPed);
                    sb.Append("<br/>");
                }

                decimal valorMed = (totalQtd > 0m) ? Math.Round(totalValores / totalQtd, 2) : 0m;

                sb.Append("<b>------------------------------</b><br/>");
                sb.Append("<b>")
                  .Append("R$ Mín ").Append(Num2(valorMin))
                  .Append("   |   R$ Máx ").Append(Num2(valorMax))
                  .Append("   |   R$ Méd ").Append(Num2(valorMed))
                  .Append("</b>");

                sb.Append("</font>");

                msgRetorno = sb.ToString();
                sucesso = true;
            }
            catch (DbEntityValidationException ex)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception ex)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(ex);
            }

            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult AjaxDadosHistoricoComercialItemCliente(g_produtos view_g_produtos)
        {
            bool sucesso = false;
            string msgRetorno = string.Empty;

            try
            {
                // 🔒 Parse seguro (evita SQL Injection / strings inválidas)
                int idProduto = view_g_produtos.id_produto;
                int idCliente = view_g_produtos.id_coligada; // (no seu código está sendo usado como id_cliente)

                if (idProduto <= 0 || idCliente <= 0)
                    return Json(new { success = false, msg = "Parâmetros inválidos (Produto/Cliente)." }, JsonRequestBehavior.AllowGet);

                // ✅ Sem SQL string: LINQ + joins + TOP + ORDER
                var itens = (
                    from item in db.gc_movimentos_itens.AsNoTracking()
                    join mov in db.gc_movimentos.AsNoTracking() on item.id_movimento equals mov.id_movimento
                    join prod in db.g_produtos.AsNoTracking() on item.id_produto equals prod.id_produto
                    join cli in db.g_clientes.AsNoTracking() on mov.id_cliente equals cli.id_cliente
                    join ven in db.g_vendedores.AsNoTracking() on mov.id_vendedor equals ven.id_vendedor
                    where (mov.id_movimento_tipo == 3 || mov.id_movimento_tipo == 4 || mov.id_movimento_tipo == 8)
                       && item.id_produto == idProduto
                       && mov.id_cliente == idCliente
                       && (mov.id_movimento_status == 1 || mov.id_movimento_status == 2)
                    orderby mov.datahora_alteracao descending
                    select new
                    {
                        mov.id_movimento,
                        mov.id_movimento_status,
                        mov.datahora_alteracao,
                        mov.id_moeda,
                        item.quantidade,
                        item.valor_unit,
                        item.valor_total,
                        Cliente = cli.nome,
                        Vendedor = ven.nome,
                        PnProduto = prod.codigo
                    }
                )
                .Take(10)
                .ToList();

                if (itens.Count == 0)
                {
                    msgRetorno = "<font size='6'>Não foram localizados Pedidos/Cotações com o item/cliente selecionado!</font><br/>";
                    sucesso = true;
                    return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
                }

                // ✅ Cálculos e montagem (sem try/parse em DataRow)
                decimal valorMin = 0m;
                decimal valorMax = 0m;
                decimal totalValores = 0m;
                decimal totalQtd = 0m;

                string nomeCliente = (itens[0].Cliente ?? "").Trim();
                if (nomeCliente.Length > 20) nomeCliente = nomeCliente.Substring(0, 20) + "...";

                string pn = (itens[0].PnProduto ?? "").Trim();

                var sb = new System.Text.StringBuilder();
                sb.Append("<b>---------- Histórico Cotações/Pedidos por cliente ----------</b><br/>");
                sb.Append("<b>Cliente: ").Append(nomeCliente).Append("</b><br/>");
                sb.Append("<b>Produto PN: ").Append(pn).Append("</b><br/>");

                sb.Append("Data").Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1));
                sb.Append("Cotação/Venda").Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1));
                sb.Append("Id.").Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1));
                sb.Append("Vendedor").Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1));
                sb.Append("Qtd x R$ Unit<br/>");

                foreach (var r in itens)
                {
                    var qtd = r.quantidade;
                    var vUnit = r.valor_unit;
                    var vTot = r.valor_total;

                    totalValores += vTot;
                    totalQtd += qtd;

                    if (vUnit > 0)
                    {
                        if (valorMin == 0 || vUnit < valorMin) valorMin = vUnit;
                        if (valorMax == 0 || vUnit > valorMax) valorMax = vUnit;
                    }

                    string vendedor = (r.Vendedor ?? "").Trim();
                    int idxEspaco = vendedor.IndexOf(' ');
                    if (idxEspaco > 0) vendedor = vendedor.Substring(0, idxEspaco).Trim();

                    string statusPedido =
                        r.id_movimento_status == 1 ? "Cotação" :
                        r.id_movimento_status == 2 ? "Venda OK" :
                        "Venda Cancelada";

                    string dataStr = (r.datahora_alteracao ?? DateTime.MinValue).ToString("dd/MM/yy");

                    // Formatação do valor unitário com símbolo de moeda do movimento
                    string formatoMoeda = "pt-BR";
                    if (r.id_moeda == 2) { formatoMoeda = "en-US"; }
                    string valorUnitFormatado = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", vUnit);
                    if (r.id_moeda == 2) { valorUnitFormatado = valorUnitFormatado.Replace("$", "$ "); }

                    sb.Append(dataStr).Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1));
                    sb.Append(statusPedido).Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1));
                    sb.Append(r.id_movimento).Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1));
                    sb.Append(vendedor).Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1));
                    sb.Append(decimal.Truncate(qtd)).Append(" x ").Append("<b>").Append(valorUnitFormatado).Append("</b><br/>");
                }

                decimal valorMed = (totalQtd > 0 ? Math.Round(totalValores / totalQtd, 2) : 0m);

                sb.Append("<br/><b>")
                  .Append("R$ Mín ").Append(valorMin.ToString("###,###,##0.00"))
                  .Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1))
                  .Append("R$ Máx ").Append(valorMax.ToString("###,###,##0.00"))
                  .Append(LibStringFormat.GetTabHtml(1)).Append("|").Append(LibStringFormat.GetTabHtml(1))
                  .Append("R$ Méd ").Append(valorMed.ToString("###,###,##0.00"))
                  .Append("</b>");

                msgRetorno = "<font size='2px'>" + sb.ToString() + "</font>";
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


        [HttpPost]
        public ActionResult AjaxDadosInfoComplementares(gc_cfop_operacoes view_gc_cfop_operacoes)
        {
            bool Sucesso = false;
            string MsgRetorno = String.Empty;
            string InformacoesComplementares = String.Empty;
            try
            {
                List<gc_cfop_operacoes> ListaCfopOperacoes = db.gc_cfop_operacoes.Where(o => o.id_cfop_operacao > 0).OrderBy(p => p.ordem).ToList(); 
                gc_cfop_operacoes record_gc_cfop_operacoes = ListaCfopOperacoes.Where(c => c.id_cfop_operacao == view_gc_cfop_operacoes.id_cfop_operacao).FirstOrDefault();
                if (record_gc_cfop_operacoes != null)
                {
                    if (view_gc_cfop_operacoes.ativo == true) // // gc_cfop_operacoes.ativo = gc_movimentos.param_reducao_bc
                    {
                        InformacoesComplementares = record_gc_cfop_operacoes.info_nfe_beneficio.EmptyIfNull().ToString();
                    }
                    else
                    {
                        InformacoesComplementares = record_gc_cfop_operacoes.info_nfe.EmptyIfNull().ToString();
                    }
                    int IdMovimento = int.Parse(view_gc_cfop_operacoes.tag1);
                    gc_movimentos record_gc_movimento = db.gc_movimentos.Find(IdMovimento);
                    g_clientes record_g_cliente = db.g_clientes.Find(record_gc_movimento.id_cliente);
                    
                    String TagAeronavePrefixo = string.Empty;
                    if (record_gc_movimento.aeronave_prefixo.EmptyIfNull().ToString().Length > 0) { TagAeronavePrefixo += " da Aeronave Prefixo: " + record_gc_movimento.aeronave_prefixo.EmptyIfNull().ToString() + ", "; }
                    else if (record_g_cliente.identificador.EmptyIfNull().ToString().Length > 0) { TagAeronavePrefixo += " de Aeronaves Prefixo: " + record_g_cliente.identificador.EmptyIfNull().ToString() + ", "; }
                    else { TagAeronavePrefixo += " de Aeronaves, "; }
                    InformacoesComplementares = InformacoesComplementares.Replace("[aeronave_prefixo]", TagAeronavePrefixo);
                    InformacoesComplementares = InformacoesComplementares.Replace("[prefixo]", TagAeronavePrefixo);

                    String TagAeronaveModelo = string.Empty;
                    if (record_gc_movimento.aeronave_modelo.EmptyIfNull().ToString().Length > 0) { TagAeronaveModelo += " Modelo: " + record_gc_movimento.aeronave_modelo.EmptyIfNull().ToString() + ", "; }
                    InformacoesComplementares = InformacoesComplementares.Replace("[aeronave_modelo]", TagAeronaveModelo);

                    String TagAeronaveSerie = string.Empty;
                    if (record_gc_movimento.aeronave_serie.EmptyIfNull().ToString().Length > 0) { TagAeronaveSerie += " Série: " + record_gc_movimento.aeronave_serie.EmptyIfNull().ToString() + ", "; }
                    InformacoesComplementares = InformacoesComplementares.Replace("[aeronave_serie]", TagAeronaveSerie);

                    String TagAeronaveRegistro = string.Empty;
                    if (record_gc_movimento.aeronave_registro.EmptyIfNull().ToString().Length > 0) { TagAeronaveRegistro += " Registro: " + record_gc_movimento.aeronave_registro.EmptyIfNull().ToString() + ", "; }
                    InformacoesComplementares = InformacoesComplementares.Replace("[aeronave_registro]", TagAeronaveRegistro);

                    if (record_gc_movimento.oc_numero.EmptyIfNull().ToString().Length > 0) { InformacoesComplementares = "Ordem de Compra nº  " + record_gc_movimento.oc_numero.EmptyIfNull().ToString() + " | " + InformacoesComplementares; };
                    if (record_gc_movimento.informacoes_complementares_nf.EmptyIfNull().ToString().Length > 0) { InformacoesComplementares = "Obs: " + record_gc_movimento.informacoes_complementares_nf.EmptyIfNull().ToString() + " | " + InformacoesComplementares; };

                    if ((record_gc_movimento.id_cliente_destinatario > 0) && (record_gc_movimento.id_cfop_operacao != 3) && (record_gc_movimento.id_cfop_operacao != 17)) // Destinatário diferente, porém na mesma UF
                    {
                        String TextoEnderecoEntregaMesmaUF = string.Empty;
                        g_clientes_destinatarios record_g_clientes_destinatarios = db.g_clientes_destinatarios.Find(record_gc_movimento.id_cliente_destinatario);
                        TextoEnderecoEntregaMesmaUF += LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.nome).ToUpperInvariant() + " - ";
                        TextoEnderecoEntregaMesmaUF += LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.endereco_com).ToUpperInvariant() + ", " + LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.endereco_com_numero).ToUpperInvariant() + " " + LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.endereco_com_complemento).ToUpperInvariant() + " - ";
                        TextoEnderecoEntregaMesmaUF += LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.bairro_com).ToUpperInvariant() + " - ";
                        g_cidades RecordCidadeEntregaMesmaUF = db.g_cidades.Find(record_g_clientes_destinatarios.id_cidade_com);
                        TextoEnderecoEntregaMesmaUF += LibStringFormat.SomenteAlfabetoSefaz(RecordCidadeEntregaMesmaUF.nome.EmptyIfNull().ToString()).ToUpperInvariant() + " - ";
                        g_uf RecordUFEntregaMesmaUF = db.g_uf.Find(record_g_clientes_destinatarios.id_uf_com);
                        TextoEnderecoEntregaMesmaUF += LibStringFormat.SomenteAlfabetoSefaz(RecordUFEntregaMesmaUF.nome.EmptyIfNull().ToString()).ToUpperInvariant() + " - ";
                        TextoEnderecoEntregaMesmaUF += "CEP: " + LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.cep_com);
                        InformacoesComplementares = "Entregar em: " + TextoEnderecoEntregaMesmaUF + " | " + InformacoesComplementares;
                    };

                    // Número da Cotação na Transportadora
                    if (record_gc_movimento.frete1_documento.EmptyIfNull().ToString().Length > 0) { InformacoesComplementares = "Nº Cotação: " + record_gc_movimento.frete1_documento.EmptyIfNull().ToString() + " | " + InformacoesComplementares; };

                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, informacoes = InformacoesComplementares }, JsonRequestBehavior.AllowGet);
        }
        #endregion 

        #region Pedido - Aprovação
        public ActionResult ModalPedidoAprovacao(int? id)
        {
            Decimal CotacaoDolarDia = SetCotacaoDollarDia();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            String MsgObservacoes = string.Empty;
            String MsgBloqueio = string.Empty;
            String MsgHistorico = string.Empty;
            gc_movimentos PedidoVenda = db.gc_movimentos.Find(id);
            g_clientes RecordCliente = db.g_clientes.Find(PedidoVenda.id_cliente);
            gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(PedidoVenda.id_cfop_operacao);
            List<gc_movimentos_itens> ListaItensPedido = db.gc_movimentos_itens.Where(i => i.id_movimento == PedidoVenda.id_movimento).ToList();
            List<g_produtos> ListaProdutosPedido = db.g_produtos.SqlQuery("select p.* from g_produtos p where p.id_produto in (select distinct i.id_produto from gc_movimentos_itens i where id_movimento = "+ PedidoVenda.id_movimento.ToString() + ")").ToList();

            if (PedidoVenda != null)
            {
                if (PedidoVenda.movimento_aprovado == true) { TitleModal = LibIcons.getIcon("fa-solid fa-clipboard-check", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Pedido Nº " + PedidoVenda.id_movimento.ToString() + " já aprovado!"; }
                else { TitleModal = LibIcons.getIcon("fa-solid fa-clipboard-check", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Aprovação do Pedido Nº " + PedidoVenda.id_movimento.ToString(); }

                // Verificar Markup Item
                int QtdItensSemMarkup = 0;
                Decimal _FobItem = 0;
                Decimal ItemCustoReais = 0;
                String MsgItemSemFob = string.Empty;
                foreach (gc_movimentos_itens ItemPedido1 in ListaItensPedido)
                {
                    g_produtos ProdutoPedido1 = ListaProdutosPedido.Where(p => p.id_produto == ItemPedido1.id_produto).FirstOrDefault();
                    _FobItem = ProdutoPedido1.fob1_dollar;

                    if ((_FobItem > 0) && (CachePersister.userIdentity.CotacaoDollarDia > 0))
                    {
                        if (PedidoVenda.id_moeda == 1) { ItemCustoReais = _FobItem * CachePersister.userIdentity.CotacaoDollarDia * ItemPedido1.quantidade; }
                        else if (PedidoVenda.id_moeda == 2) { ItemCustoReais = _FobItem * ItemPedido1.quantidade; }
                        if (ItemPedido1.valor_total < ItemCustoReais)
                        {
                            MsgItemSemFob += " - Item [" + ProdutoPedido1.codigo.EmptyIfNull().ToString() + "] abaixo do Markup, ";
                            MsgItemSemFob += " Venda/Fob [" + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ItemPedido1.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " / ";
                            MsgItemSemFob += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ItemCustoReais).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "]!" + "<br/>";
                            QtdItensSemMarkup += 1;
                        }
                    }
                }

                if ((PedidoVenda.id_cfop_operacao != 2) && (PedidoVenda.id_cfop_operacao != 25)) // Não entra nessa regra Transferências = Custo, e Complemento de valor
                {
                    if (QtdItensSemMarkup > 0)
                    {
                        MsgBloqueio = "<b> ---------- ITENS ABAIXO DO MARKUP ----------</b>" + "<br/>" + MsgItemSemFob;
                    }
                }

                // Verificar itens importados
                int QtdItensTemporarios = 0;
                String PnsItensTemporarios = string.Empty;
                foreach (gc_movimentos_itens ItemPedidoVenda in ListaItensPedido)
                {
                    g_produtos ProdutoPedido = ListaProdutosPedido.Where(p => p.id_produto == ItemPedidoVenda.id_produto).FirstOrDefault();
                    if (ProdutoPedido.importado == false)
                    {
                        QtdItensTemporarios += 1;
                        PnsItensTemporarios += ProdutoPedido.codigo.EmptyIfNull().ToString()+"; ";
                    }
                }
                if (QtdItensTemporarios > 0)
                {
                    MsgBloqueio += "<b> ---------- ITENS TEMPORÁRIOS ----------</b>" + "<br/>";
                    MsgBloqueio += "Não é possível confirmar o pedido com itens temporários [" + PnsItensTemporarios.EmptyIfNull().ToString().Trim() + "]<br/>";
                    MsgBloqueio += "Ajuste a cotação/pedido substituindo pelos itens corretos!" + "<br/>";
                }

                if (RecordCfopOperacao.has_financeiro == true) // Verificar se a operação tem financeiro
                {
                    // Cálculo do saldo de adiantamento real do cliente, considerando os lançamentos de adiantamento que não estão vinculados a nenhum pedido ou nota fiscal, ou estão vinculados a pedidos ou notas fiscais que ainda estão em aberto
                    if ((PedidoVenda.id_pagrec_condicao == 2) || (PedidoVenda.valor_total_adiantamento > 0))
                    {
                        String ListaIdsAdiantamentos = string.Empty;
                        Decimal ValorAdiantamentoInformado = PedidoVenda.valor_total_adiantamento;
                        Decimal SaldoAdiantamentoReal = 0;
                        List<gc_financeiro_lancamentos> listAdiantamentos = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.is_adiantamento == true && l.tipo_pag_rec == 2 && l.id_financeiro_status == 1 && l.id_cliente == PedidoVenda.id_cliente).ToList();
                        foreach (gc_financeiro_lancamentos Adiantamento in listAdiantamentos)
                        {
                            gc_financeiro_lancamentos LancamentoAdiantamentoVinculado = db.gc_financeiro_lancamentos.Where(l => l.id_lancamento_adiantamento == Adiantamento.id_lancamento || l.id_lancamento_adiantamento2 == Adiantamento.id_lancamento || l.id_lancamento_adiantamento3 == Adiantamento.id_lancamento).FirstOrDefault();
                            if (LancamentoAdiantamentoVinculado == null) // Esse adiantamento não está vinculado a nenhum pedido ou nota fiscal, então o valor total do adiantamento pode ser considerado para o saldo
                            {
                                SaldoAdiantamentoReal += Adiantamento.valor_total;
                                ListaIdsAdiantamentos += Adiantamento.id_lancamento.EmptyIfNull().ToString() + ";";
                            }
                        }
                        if (PedidoVenda.id_pagrec_condicao == 2)
                        {
                            if (SaldoAdiantamentoReal == 0)
                            {
                                MsgBloqueio += "<b> ---------- NÃO HÁ SALDO DE ADIANTAMENTO ----------</b><br/>";
                                MsgBloqueio += " - Não foi encontrado Saldo de Adiantamento para faturar o pedido nas condições A Vista ou Antecipado!";
                            }
                            else if ((SaldoAdiantamentoReal > 0) && (SaldoAdiantamentoReal < PedidoVenda.valor_total_bruto))
                            {
                                MsgBloqueio += "<b> ---------- SALDO DE ADIANTAMENTO INSUFICIENTE ----------</b><br/>";
                                MsgBloqueio += " - Saldo de adiantamento do cliente [ " + SaldoAdiantamentoReal.ToString("###,###,###,##0.00") + "] é insuficiente para faturar o pedido " + PedidoVenda.valor_total_bruto.ToString("###,###,###,##0.00") + " nas condições A Vista ou Antecipado!<br/>";
                            }
                        }
                        else if ((ValorAdiantamentoInformado > 0) && (ValorAdiantamentoInformado > SaldoAdiantamentoReal))
                        {
                            MsgBloqueio += "<b> ---------- SALDO DE ADIANTAMENTO MENOR QUE ADIANTAMENTO INFORMADO ----------</b><br/>";
                            MsgBloqueio += " - Valor do adiantamento informado no pedido [ " + ValorAdiantamentoInformado.ToString("###,###,###,##0.00") + " ], é MAIOR do que o saldo de adiantamento encontrado  [ " + SaldoAdiantamentoReal.ToString("###,###,###,##0.00") + "]" + "<br/>";
                        }
                    }

                    // Posição Financeira do Cliente
                    cstPosicaoFinanceiraCliente PosicaoFinanceiraCliente = GetPosicaoFinanceiraCliente(PedidoVenda.id_cliente);
                    if (PedidoVenda.valor_total_bruto > PosicaoFinanceiraCliente.LimiteCreditoRestante)
                    {
                        if (PedidoVenda.id_pagrec_condicao >= 3) // Pedido à Crédito
                        {
                            MsgBloqueio = "<b> ---------- PEDIDO BLOQUEADO PARA VENDA À CRÉDITO ----------</b>" + "<br/>";
                            MsgBloqueio += " - Limite de crédito insuficiente para confirmação do pedido:   Pedido  " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PedidoVenda.valor_total_bruto).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                            MsgBloqueio += " - Limites do cliente:   total   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "   |   utilizado   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoUtilizado).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "   |   disponível   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoRestante).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                        }
                        else // Venda à vista
                        {
                            MsgObservacoes += "<b> ---------- PEDIDO LIBERADO SOMENTE PARA VENDA À VISTA/ANTECIPADO ----------</b>" + "<br/>";
                            MsgObservacoes += " - Limite de crédito insuficiente para confirmação do pedido à crédito:   Pedido  " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PedidoVenda.valor_total_bruto).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                            MsgObservacoes += " - Limites do cliente:   total   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "   |   utilizado   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoUtilizado).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "   |   disponível   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoRestante).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                        }
                    }
                    if (PosicaoFinanceiraCliente.TitulosVencidosQtd > 0)
                    {
                        MsgBloqueio = "<b> ---------- PEDIDO BLOQUEADO, EXISTEM TÍTULOS VENCIDOS ----------</b>" + "<br/>";
                        MsgBloqueio += " - " + PosicaoFinanceiraCliente.TitulosVencidosQtd.EmptyIfNull().ToString() + " título(s) vencido(s) em aberto, Total: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.TitulosVencidosValor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                        if (PosicaoFinanceiraCliente.TitulosNegociacaoQtd > 0)
                        {
                            MsgBloqueio += " - " + PosicaoFinanceiraCliente.TitulosNegociacaoQtd.EmptyIfNull().ToString() + " título(s) vencido(s) em negociação, Total: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.TitulosNegociacaoValor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                            MsgBloqueio += " - " + (PosicaoFinanceiraCliente.TitulosVencidosQtd + PosicaoFinanceiraCliente.TitulosNegociacaoQtd).EmptyIfNull().ToString() + " título(s) vencido(s), Total: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.TitulosVencidosValor + PosicaoFinanceiraCliente.TitulosNegociacaoValor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                        }
                    }
                    if ((PosicaoFinanceiraCliente.TitulosVencidosQtd == 0) && (PosicaoFinanceiraCliente.TitulosNegociacaoQtd > 0))
                    {
                        MsgObservacoes = "<b> ---------- ATENÇÃO: EXISTEM TÍTULOS VENCIDOS EM NEGOCIAÇÃO ----------</b>" + "<br/>";
                        MsgObservacoes += " - " + PosicaoFinanceiraCliente.TitulosNegociacaoQtd.EmptyIfNull().ToString() + " título(s) vencido(s) em negociação, Total: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.TitulosNegociacaoValor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                    }
                }

                if (PedidoVenda.obs.EmptyIfNull().ToString().Length > 0) { MsgHistorico = "OBS Pedido: " + PedidoVenda.obs.EmptyIfNull().ToString() + "\r\n"; };
                if (PedidoVenda.frete_observacoes.EmptyIfNull().ToString().Length > 0) { MsgHistorico = "OBS Frete: " + PedidoVenda.frete_observacoes.EmptyIfNull().ToString() + "\r\n"; };

                if (RecordCfopOperacao.has_aprovacao == false) 
                { 
                    MsgBloqueio += "<b> ---------- O TIPO DE PEDIDO [" + RecordCfopOperacao.descricao.EmptyIfNull().ToString().Trim() + "] NÃO POSSUI ATIVIDADE DE APROVAÇÃO ----------</b>" + "<br/>"; 
                }
                else 
                {
                    //if ((RecordCfopOperacao.has_aprovacao == true) && (PedidoVenda.movimento_aprovado == true)) { }
                    if ((RecordCfopOperacao.has_separacao == true) && (PedidoVenda.movimento_separado == true)) { MsgBloqueio += " - Pedido já foi SEPARADO!<br/>"; }
                    if ((RecordCfopOperacao.has_financeiro == true) && (PedidoVenda.movimento_faturado == true)) { MsgBloqueio += " - Pedido já foi FATURADO!<br/>"; }
                    if ((RecordCfopOperacao.has_nfe == true) && (PedidoVenda.movimento_nf_autorizada == true)) { MsgBloqueio += " - Pedido já possui NFe Autorizada!<br/>"; }
                    //if ((RecordCfopOperacao.has_notifica_email == true) && (PedidoVenda.movimento_notificado == true)) { MsgBloqueio += " - Pedido já foi NOTIFICADO!<br/>"; }
                    if ((RecordCfopOperacao.has_expedicao == true) && (PedidoVenda.movimento_expedido == true)) { MsgBloqueio += " - Pedido já foi EXPEDIDO!<br/>"; }
                    if ((RecordCfopOperacao.has_entrega == true) && (PedidoVenda.movimento_entregue == true)) { MsgBloqueio += " - Pedido já foi ENTREGUE!<br/>"; }
                };

                if (PedidoVenda.obs.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Pedido: " + PedidoVenda.obs.EmptyIfNull().ToString(); };
                PedidoVenda.cotacao_dolar_oficial_venda = CotacaoDolarDia;
                if ((PedidoVenda.movimento_aprovado == false) && (PedidoVenda.cotacao_dolar_venda < PedidoVenda.cotacao_dolar_oficial_venda))
                {
                    MsgObservacoes = "<b> ---------- ATENÇÃO: COTAÇÃO OFICIAL DO DOLAR ----------</b>" + "<br/>";
                    MsgObservacoes += " - A Cotação do Dolar informado no pedido " + PedidoVenda.cotacao_dolar_venda.ToString("##0.00000") + " é menor do que a cotação do dia " + PedidoVenda.cotacao_dolar_oficial_venda.ToString("##0.00000") + "<br/>";
                }
            }
            else
            {
                MsgBloqueio = " - Pedido Nº " + id.ToString() + " não localizado no ERP";
            }
            ViewBag.Title = TitleModal;
            LibDataSets.LoadDatasetGVendedores(db);
            ViewBag.comboVendedores = LibDataSets.LoadComboGVendedores(db);
            ViewBag.comboPagRecCondicoes = LibDataSets.LoadComboPagRecCondicoesTodas(db);
            ViewBag.comboLocaisEstoqueOrders = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
            ViewBag.comboTransportadora = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.ComboGcClientesContatosTipos = LibDataSets.LoadComboGcClientesContatosTipos(db);
            ViewBag.ComboClientesContatos = LibDataSets.LoadComboGcClientesContatosPedido(db, PedidoVenda.id_cliente);
            //PedidoVenda.notifica_contatos_emails = RecordCliente.email_notificacao;

            ViewBag.MsgBloqueio = MsgBloqueio;
            ViewBag.MsgObservacoes = MsgObservacoes;
            ViewBag.MsgHistorico = MsgHistorico;
            if (ListaItensPedido.Count() <= 1) { ViewBag.QtdItensPedido = ListaItensPedido.Count() + " Item no Pedido"; } else { ViewBag.QtdItensPedido = ListaItensPedido.Count() + " Itens no Pedido"; }
            if (PedidoVenda.comissao1_vendedor <= 0) { PedidoVenda.comissao1_vendedor = PedidoVenda.id_vendedor; };
            return View("ModalPedidoAprovacao", PedidoVenda);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoAprovacao(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String LogAlteracoes = string.Empty;
            String MsgRetorno = "";
            String ListaLancamentosAdiantamentos = string.Empty;
            try
            {
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);
                gc_movimentos record_old_gc_movimento = LibDB.CloneTObject(record_gc_movimento);
                gc_cfop_operacoes RecordCfopOperacoes = db.gc_cfop_operacoes.Find(record_gc_movimento.id_cfop_operacao);

                if (ModelState.IsValid == false)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }

                if (qtdInconsistencias == 0)
                {
                    if (view_record_gc_movimento.frete1_transportadora < 0)
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "Campo [Transportadora] é de preenchimento obrigatório!" + "<br/>";
                    }
                    if (view_record_gc_movimento.movimento_aprovado == true)
                    {
                        if (RecordCfopOperacoes.is_venda == true)
                        {
                            if (view_record_gc_movimento.contato_nome.EmptyIfNull().ToString().Length == 0)
                            {
                                qtdInconsistencias += 1;
                                MsgRetorno += "Campo [Pessoa de Contato] é de preenchimento obrigatório!" + "<br/>";
                            }
                            if (view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Length == 0)
                            {
                                qtdInconsistencias += 1;
                                MsgRetorno += "Campo [Celular de Contato] é de preenchimento obrigatório!" + "<br/>";
                            }
                            else
                            {
                                String Telefone = LibStringFormat.SomenteNumeros(view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Trim());
                                if ((Telefone.Length != 10) && (Telefone.Length != 11))
                                {
                                    qtdInconsistencias += 1;
                                    MsgRetorno += "Campo <b>Celular/Telefone</b> deverá conter a seguinte formatação DDNNNNNNNNN onde os 2 primeiros dígitos deverão ser o DDD e os dígitos seguintes o número do telefone ou celular com 8 ou 9 dígitos!";
                                }
                            }
                            if (view_record_gc_movimento.contato_email.EmptyIfNull().ToString().Length == 0)
                            {
                                qtdInconsistencias += 1;
                                MsgRetorno += "Campo [Email de Contato] é de preenchimento obrigatório!" + "<br/>";
                            }
                            else
                            {
                                if (LibStringValidate.ValidarEmail(view_record_gc_movimento.contato_email.EmptyIfNull().ToString()) == false)
                                {
                                    qtdInconsistencias += 1;
                                    MsgRetorno += "Campo [Email de Contato] contém um email inválido!" + "<br/>";
                                }
                            }
                        }
                        else
                        {
                            if (view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Length == 0) { view_record_gc_movimento.contato_telefone = "0"; };
                        }
                    }
                    if (view_record_gc_movimento.id_local_estoque <= 0)
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "Campo [Estoque] é de preenchimento obrigatório!" + "<br/>";
                    }
                    if ((record_gc_movimento.id_movimento_posicao != 0) && (record_gc_movimento.id_movimento_posicao != 1))
                    {
                        qtdInconsistencias += 1;
                        String PosicaoAtual = string.Empty;
                        if (record_gc_movimento.id_movimento_posicao == 0) { PosicaoAtual = "Aberto"; } else { PosicaoAtual = db.gc_movimentos_posicao.Find(record_gc_movimento.id_movimento_posicao).posicao.EmptyIfNull().ToString(); };
                        MsgRetorno += "O Pedido se encontra na posição [" + PosicaoAtual + "], não é possível Aprovar ou Reprovar!" + "<br/>";
                    }
                    if ((record_gc_movimento.movimento_aprovado == true) && (view_record_gc_movimento.movimento_aprovado == false) && (view_record_gc_movimento.tag_string.EmptyIfNull().ToString().Length == 0))
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "Para CANCELAR a aprovação, informe o motivo!" + "<br/>";
                    }
                }

                if ((view_record_gc_movimento.id_pagrec_condicao == 2) || (record_gc_movimento.valor_total_adiantamento > 0))
                {
                    ListaLancamentosAdiantamentos = string.Empty;
                    Decimal ValorAdiantamentoInformado = record_gc_movimento.valor_total_adiantamento;
                    Decimal SaldoAdiantamentoReal = 0;
                    List<gc_financeiro_lancamentos> listAdiantamentos = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.is_adiantamento == true && l.tipo_pag_rec == 2 && l.id_financeiro_status == 1 && l.id_cliente == record_gc_movimento.id_cliente).ToList();
                    foreach (gc_financeiro_lancamentos Adiantamento in listAdiantamentos)
                    {
                        ListaLancamentosAdiantamentos += "Id: " + Adiantamento.id_lancamento.EmptyIfNull().ToString() + " R$ " + Adiantamento.valor_total.ToString("###,###,###,##0.00") + ", ";
                        SaldoAdiantamentoReal += Adiantamento.valor_total;
                    }

                    if (view_record_gc_movimento.id_pagrec_condicao == 2)
                    {
                        if (SaldoAdiantamentoReal == 0)
                        {
                            qtdInconsistencias += 1;
                            MsgRetorno += "Não foi encontrado Saldo de Adiantamento para faturar o pedido nas condições A Vista ou Antecipado!";
                        }
                        else if ((SaldoAdiantamentoReal > 0) && (SaldoAdiantamentoReal < view_record_gc_movimento.valor_total_bruto))
                        {
                            qtdInconsistencias += 1;
                            MsgRetorno += "Saldo de adiantamento do cliente [ " + SaldoAdiantamentoReal.ToString("###,###,###,##0.00") + "] é insuficiente para faturar o pedido " + view_record_gc_movimento.valor_total_bruto.ToString("###,###,###,##0.00") + " nas condições A Vista ou Antecipado!<br/>";
                        }
                    }
                    else if ((ValorAdiantamentoInformado > 0) && (ValorAdiantamentoInformado > SaldoAdiantamentoReal))
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "Valor do adiantamento informado no pedido [ " + ValorAdiantamentoInformado.ToString("###,###,###,##0.00") + " ], é MAIOR do que o saldo de adiantamento encontrado  [ " + SaldoAdiantamentoReal.ToString("###,###,###,##0.00") + "]" + "<br/>";
                    }
                }

                if (qtdInconsistencias == 0)
                {
                    cstPosicaoFinanceiraCliente PosicaoFinanceiraCliente = GetPosicaoFinanceiraCliente(view_record_gc_movimento.id_cliente);
                    if ((view_record_gc_movimento.valor_total_bruto > PosicaoFinanceiraCliente.LimiteCreditoRestante) && (view_record_gc_movimento.id_pagrec_condicao >= 3))
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "<b>Limite de crédito insuficiente para confirmação do pedido:</b>   Pedido  " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.valor_total_bruto).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                        MsgRetorno += "<b>Limites do cliente:</b>   total   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "   |   utilizado   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoUtilizado).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "   |   disponível   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.LimiteCreditoRestante).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                    }
                    if (PosicaoFinanceiraCliente.TitulosVencidosQtd > 0)
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "<b>Títulos vencidos:</b>   " + "qtd:   " + PosicaoFinanceiraCliente.TitulosVencidosQtd.EmptyIfNull().ToString() + "   |   " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PosicaoFinanceiraCliente.TitulosVencidosValor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>";
                    }
                }

                if (RecordCfopOperacoes.verifica_estoque_aprovacao == true)
                {
                    // Validação Estoque na Aprovação
                    if ((qtdInconsistencias == 0) && (view_record_gc_movimento.movimento_aprovado == true))
                    {
                        int TipoMovimentoEstoque = 11; // Saída - Separação Pedido
                        if (record_gc_movimento.id_movimento_tipo == 19) { TipoMovimentoEstoque = 5; } // Saída - Transferência
                        EstoqueInventarioService ServicoEstoqueInventario = new EstoqueInventarioService();
                        if (ServicoEstoqueInventario.ValidarEstoque(record_gc_movimento.id_movimento, TipoMovimentoEstoque, db) == false)
                        {
                            qtdInconsistencias += 1;
                            MsgRetorno += ServicoEstoqueInventario.GetMsgProcessamento(); ;
                        }
                    }
                }

                if (qtdInconsistencias == 0)
                {
                    if (view_record_gc_movimento.movimento_aprovado == true)
                    {
                        ////////// Cálculo do Markup - Início //////////
                        Decimal PedidoAprovadoValorTotalVenda = 0;
                        Decimal PedidoAprovadoCustoTotalVenda = 0;
                        Decimal ProdutoCustoDollar = 0;
                        Decimal ItemCustoReais = 0;
                        Decimal MarkupPedido = 0;
                        Decimal MarkupItem = 0;
                        String SqlListaProdutosPedido = "select * from g_produtos where id_produto in (select distinct(id_produto) from gc_movimentos_itens where id_movimento = " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + ")";
                        var ListaProdutosPedido = db.g_produtos.SqlQuery(SqlListaProdutosPedido).ToList();
                        String SqlListaItensPedido = "select * from gc_movimentos_itens where id_movimento = " + record_gc_movimento.id_movimento.EmptyIfNull().ToString();
                        var ListaItensPedido = db.gc_movimentos_itens.SqlQuery(SqlListaItensPedido).ToList();
                        List<gc_movimentos_itens> ListaItensPedidosAtualizar = new List<gc_movimentos_itens>();

                        foreach (gc_movimentos_itens ItemPedido in ListaItensPedido)
                        {
                            ItemCustoReais = 0;
                            MarkupPedido = 0;
                            MarkupItem = 0;

                            ProdutoCustoDollar = ListaProdutosPedido.Where(p => p.id_produto == ItemPedido.id_produto).FirstOrDefault().fob1_dollar;
                            if ((ProdutoCustoDollar > 0) && (CachePersister.userIdentity.CotacaoDollarDia > 0))
                            {
                                PedidoAprovadoValorTotalVenda += ItemPedido.valor_total;
                                ItemCustoReais = ProdutoCustoDollar * CachePersister.userIdentity.CotacaoDollarDia * ItemPedido.quantidade;
                                if (record_gc_movimento.id_moeda == 2) { ItemCustoReais = ProdutoCustoDollar * ItemPedido.quantidade; } // Moeda Dollar
                                MarkupItem = ((ItemPedido.valor_total * 100) / ItemCustoReais) - 100;
                                MarkupItem = Math.Round(MarkupItem, 2);
                                PedidoAprovadoCustoTotalVenda += ItemCustoReais;
                                MarkupPedido = ((PedidoAprovadoValorTotalVenda * 100) / PedidoAprovadoCustoTotalVenda) - 100;

                                if ((MarkupItem > 0) && (MarkupItem != ItemPedido.markup))
                                {
                                    ItemPedido.markup = Math.Round(MarkupItem, 2);
                                    ItemPedido.fob_unit_dollar = ProdutoCustoDollar;
                                    ItemPedido.fob_unit_reais_venda = ItemPedido.valor_unit;
                                    ListaItensPedidosAtualizar.Add(ItemPedido);
                                }
                            }
                        }
                        if (ListaItensPedidosAtualizar.Count > 0)
                        {
                            foreach (gc_movimentos_itens Item in ListaItensPedidosAtualizar)
                            {
                                Item.datahora_alteracao = DataHoraAtual;
                                Item.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(Item).State = EntityState.Modified;
                            }
                        }
                        record_gc_movimento.markup = MarkupPedido;
                        ////////// Cálculo do Markup - Fim //////////

                        // Dados do Contato
                        record_gc_movimento.notifica_contatos_emails = view_record_gc_movimento.notifica_contatos_emails.EmptyIfNull().ToString().ToLowerInvariant(); // Todos os outros emails a serem notificados
                        record_gc_movimento.id_contato = view_record_gc_movimento.id_contato;
                        record_gc_movimento.contato_nome = view_record_gc_movimento.contato_nome.EmptyIfNull().ToString().ToUpperInvariant();
                        record_gc_movimento.contato_telefone = view_record_gc_movimento.contato_telefone;
                        record_gc_movimento.contato_email = view_record_gc_movimento.contato_email.EmptyIfNull().ToString().ToLowerInvariant();

                        if (view_record_gc_movimento.id_contato > 0) // Usuário Selecionou o Contato
                        {
                            g_clientes_contatos RecordContato = db.g_clientes_contatos.Find(view_record_gc_movimento.id_contato);

                            if ((RecordContato.telefone != view_record_gc_movimento.contato_telefone) || (RecordContato.email != view_record_gc_movimento.contato_email))
                            {
                                String LogAlteracao = string.Empty;
                                LogAlteracao = "Alteração dados do contato | " + LogAlteracao;
                                LogAlteracao += "Contato: " + RecordContato.contato.EmptyIfNull().ToString().Trim() + " | ";
                                if (RecordContato.telefone.EmptyIfNull().ToString().Trim() != view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Telefone: " + RecordContato.telefone.EmptyIfNull().ToString().Trim() + " > " + view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Trim() + " | "; };
                                if (RecordContato.email.EmptyIfNull().ToString().Trim() != view_record_gc_movimento.contato_email.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Email: " + RecordContato.email.EmptyIfNull().ToString().Trim() + " > " + view_record_gc_movimento.contato_email.EmptyIfNull().ToString().Trim() + " | "; };
                                LibAudit.SaveAudit(db, false, "g_clientes", record_gc_movimento.id_cliente, LogAlteracao.EmptyIfNull());
                                RecordContato.contato = view_record_gc_movimento.contato_nome;
                                RecordContato.telefone = view_record_gc_movimento.contato_telefone;
                                RecordContato.email = view_record_gc_movimento.contato_email;
                                RecordContato.datahora_alteracao = DataHoraAtual;
                                RecordContato.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(RecordContato).State = EntityState.Modified;
                            }
                        }
                        else
                        {
                            g_clientes_contatos RecordContatoNovo = new g_clientes_contatos();
                            RecordContatoNovo.id_cliente = record_gc_movimento.id_cliente;
                            RecordContatoNovo.ativo = true;
                            RecordContatoNovo.contato = view_record_gc_movimento.contato_nome.EmptyIfNull().ToString().ToUpperInvariant();
                            RecordContatoNovo.telefone = view_record_gc_movimento.contato_telefone;
                            RecordContatoNovo.email = view_record_gc_movimento.contato_email.EmptyIfNull().ToString().ToLowerInvariant();
                            RecordContatoNovo.id_contato_tipo = 1;
                            RecordContatoNovo.id_coligada = 0; // Global
                            RecordContatoNovo.id_filial = 0; // Global
                            RecordContatoNovo.datahora_cadastro = DataHoraAtual;
                            RecordContatoNovo.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordContatoNovo).State = EntityState.Added;
                            db.SaveChanges();
                            record_gc_movimento.id_contato = RecordContatoNovo.id_contato;

                            // Audit Novo Contato
                            String TipoContato = String.Empty;
                            List<Db.g_clientes_contatos_tipos> AllContatosTipos = db.g_clientes_contatos_tipos.Where(c => c.ativo == true).ToList();
                            g_clientes_contatos_tipos RecordTipoContato = AllContatosTipos.Where(t => t.id_contato_tipo == RecordContatoNovo.id_contato_tipo).FirstOrDefault();
                            if (RecordTipoContato != null) { TipoContato = RecordTipoContato.nome.EmptyIfNull().ToString(); };
                            String LogAlteracaoContato = "Novo contato | ";
                            LogAlteracaoContato += "Tipo: " + TipoContato + " | ";
                            LogAlteracaoContato += "Contato: " + RecordContatoNovo.contato.EmptyIfNull().ToString().Trim().ToUpperInvariant() + " | ";
                            LogAlteracaoContato += "Setor: " + RecordContatoNovo.setor.EmptyIfNull().ToString().Trim().ToUpperInvariant() + " | ";
                            LogAlteracaoContato += "Telefone: " + RecordContatoNovo.telefone.EmptyIfNull().ToString().Trim() + " | ";
                            LogAlteracaoContato += "Email: " + RecordContatoNovo.email.EmptyIfNull().ToString().Trim().ToLowerInvariant() + " | ";
                            if (LogAlteracaoContato.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true, "g_clientes", RecordContatoNovo.id_cliente, LogAlteracaoContato.EmptyIfNull()); };
                        }

                        List<g_vendedores> AllVendedores = db.g_vendedores.Where(v => v.ativo == true).OrderBy(p => p.nome).ToList();
                        if (record_gc_movimento.id_movimento_posicao == 0) // Movimento está aberto
                        {
                            if ((record_gc_movimento.comissao1_percentual == 0) && (record_gc_movimento.comissao1_valor == 0) && (record_gc_movimento.comissao2_percentual == 0) && (record_gc_movimento.comissao2_valor == 0)) // Se todos os percentuais estiverm vazios
                            {
                                record_gc_movimento.comissao1_vendedor = record_gc_movimento.id_vendedor;
                                record_gc_movimento.comissao1_percentual = 5;
                                record_gc_movimento.comissao1_valor = (((record_gc_movimento.valor_total_liquido - record_gc_movimento.frete_gerencial) / 100) * (record_gc_movimento.comissao1_percentual));
                                record_gc_movimento.comissao2_vendedor = 0;
                                record_gc_movimento.comissao2_percentual = 0;
                                record_gc_movimento.comissao2_valor = 0;
                            }
                            if ((record_gc_movimento.comissao1_vendedor <= 0) && (record_gc_movimento.comissao1_vendedor <= 0)) // Não foi configurado o vendedor 1 | Configurar o vendedor do pedido
                            {
                                record_gc_movimento.comissao1_vendedor = record_gc_movimento.id_vendedor;
                            }
                            if ((record_gc_movimento.comissao2_percentual == 0) && (record_gc_movimento.comissao2_valor == 0) && (record_gc_movimento.comissao2_vendedor > 0)) // Se a comissão do vendedor 2 está vazio, não informar o vendedor 2
                            {
                                record_gc_movimento.comissao2_vendedor = 0;
                            }
                            LogAlteracoes += "Aprovação do Pedido | ";
                            if (ListaLancamentosAdiantamentos.EmptyIfNull().ToString().Length > 0) { LogAlteracoes += "Lançamentos de Adiantamento: " + ListaLancamentosAdiantamentos.EmptyIfNull().Trim() + " | "; };
                            LogAlteracoes += "Pessoa de Contato: " + record_gc_movimento.contato_nome.EmptyIfNull().ToString().ToUpperInvariant() + " | ";
                            LogAlteracoes += "Telefone do Contato: " + record_gc_movimento.contato_telefone.EmptyIfNull().ToString() + " | ";
                            LogAlteracoes += "Email do Contato: " + record_gc_movimento.contato_email.EmptyIfNull().ToString().ToLowerInvariant() + " | ";
                            LogAlteracoes += "Email Notificações: " + record_gc_movimento.notifica_contatos_emails.EmptyIfNull().ToString().ToLowerInvariant() + " | ";
                            if (view_record_gc_movimento.frete1_transportadora > 0) { LogAlteracoes += "Transportadora: " + db.g_clientes.Find(view_record_gc_movimento.frete1_transportadora).nome.EmptyIfNull().ToString() + " | "; } else { LogAlteracoes += "Transportadora: Cliente Retira | "; };
                            LogAlteracoes += "Local Estoque: " + db.gc_locais_estoque.Find(view_record_gc_movimento.id_local_estoque).sigla.EmptyIfNull().ToString() + " | ";
                            LogAlteracoes += "US$ Cotação Oficial: " + record_gc_movimento.cotacao_dolar_oficial_venda.ToString("0.0000") + " | ";
                            LogAlteracoes += "US$ Cotação Venda: " + record_gc_movimento.cotacao_dolar_venda.ToString("0.0000") + " | ";
                            if (view_record_gc_movimento.comissao1_vendedor > 0)
                            {
                                LogAlteracoes += "Vendedor(1): " + AllVendedores.Where(v => v.id_vendedor == view_record_gc_movimento.comissao1_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + ", ";
                                LogAlteracoes += "Part(%): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.comissao1_percentual).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + ", ";
                                LogAlteracoes += "Part(R$): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.comissao1_valor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " | ";
                            }
                            if (view_record_gc_movimento.comissao2_vendedor > 0)
                            {
                                LogAlteracoes += "Vendedor(2): " + AllVendedores.Where(v => v.id_vendedor == view_record_gc_movimento.comissao2_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + ", ";
                                LogAlteracoes += "Part(%): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.comissao2_percentual).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + ", ";
                                LogAlteracoes += "Part(R$): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.comissao2_valor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " | ";
                            }
                            if (view_record_gc_movimento.frete_gerencial > 0)
                            {
                                LogAlteracoes += "Frete (Gerencial): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.frete_gerencial).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + ", ";
                            }
                        }
                        else
                        {
                            LogAlteracoes += "Atualização da Aprovação Pedido | ";
                            if (ListaLancamentosAdiantamentos.EmptyIfNull().ToString().Length > 0) { LogAlteracoes += "Lançamentos de Adiantamento: " + ListaLancamentosAdiantamentos.EmptyIfNull().Trim() + " | "; };
                            if (record_gc_movimento.contato_nome != view_record_gc_movimento.contato_nome) { LogAlteracoes += "Contato: " + record_gc_movimento.contato_nome.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.contato_telefone != view_record_gc_movimento.contato_telefone) { LogAlteracoes += "Telefone: " + record_gc_movimento.contato_telefone.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.contato_email != view_record_gc_movimento.contato_email) { LogAlteracoes += "Email: " + record_gc_movimento.contato_email.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.frete1_transportadora != view_record_gc_movimento.frete1_transportadora) { if (view_record_gc_movimento.frete1_transportadora > 0) { LogAlteracoes += "Transportadora: " + db.g_clientes.Find(view_record_gc_movimento.frete1_transportadora).nome.EmptyIfNull().ToString() + " | "; } else { LogAlteracoes += "Transportadora: Cliente Retira | "; }; };
                            if (record_gc_movimento.id_local_estoque != view_record_gc_movimento.id_local_estoque) { LogAlteracoes += "Local Estoque: " + db.gc_locais_estoque.Find(view_record_gc_movimento.id_local_estoque).sigla.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.cotacao_dolar_oficial_venda != view_record_gc_movimento.cotacao_dolar_oficial_venda) { LogAlteracoes += "US$ Cotação Oficial: " + record_gc_movimento.cotacao_dolar_oficial_venda.ToString("0.0000") + " | "; };
                            if (record_gc_movimento.cotacao_dolar_venda != view_record_gc_movimento.cotacao_dolar_venda) { LogAlteracoes += "US$ Cotação Venda: " + record_gc_movimento.cotacao_dolar_venda.ToString("0.0000") + " | "; };
                            if (record_gc_movimento.frete_gerencial != view_record_gc_movimento.frete_gerencial) { LogAlteracoes += "Frete (Gerencial): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.frete_gerencial).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " | "; };
                        };

                        if (record_gc_movimento.id_movimento_tipo == 3) { record_gc_movimento.id_movimento_tipo = 4; } // Se for cotação vai evoluir para pedido
                        record_gc_movimento.id_movimento_status = 2; // Fechado
                        record_gc_movimento.movimento_aprovado = true;
                        record_gc_movimento.frete1_transportadora = view_record_gc_movimento.frete1_transportadora;
                        record_gc_movimento.id_local_estoque = view_record_gc_movimento.id_local_estoque;
                        record_gc_movimento.comissao1_abate_nf = false;
                        record_gc_movimento.comissao2_abate_nf = false;
                        record_gc_movimento.frete_gerencial = view_record_gc_movimento.frete_gerencial;
                        record_gc_movimento.obs_aprovacao = view_record_gc_movimento.obs_aprovacao;
                        record_gc_movimento.id_usuario_aprovacao = CachePersister.userIdentity.IdUsuario;
                        record_gc_movimento.datahora_aprovacao = DataHoraAtual;
                        record_gc_movimento.datahora_alteracao = DataHoraAtual;
                        if (record_gc_movimento.id_movimento_posicao == 0) { record_gc_movimento.id_movimento_posicao = 1; } // Aprovado
                        record_gc_movimento.datahora_alteracao = DataHoraAtual;
                        record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_movimento).State = EntityState.Modified;
                        db.SaveChanges();
                        if (CachePersister.userIdentity.AmbienteDatabase != "Homologação") { EnviarEmailAprovacaoEspelhoDigital(record_old_gc_movimento, record_gc_movimento); }; // Somente enviar o email em produção
                        MsgRetorno += "Status do pedido " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>APROVADO</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                        Sucesso = true;
                    }
                    else if (view_record_gc_movimento.movimento_aprovado == false) // ########## REPROVAÇÃO DO PEDIDO ##########
                    {
                        if (record_gc_movimento.id_movimento_posicao == 1) // Pedido está aprovado
                        {
                            LogAlteracoes += "Reprovação do Pedido | Motivo: " + view_record_gc_movimento.tag_string.EmptyIfNull().ToString();
                            if (record_gc_movimento.id_movimento_tipo == 3) { record_gc_movimento.id_movimento_tipo = 4; } // Pedido
                            record_gc_movimento.id_movimento_status = 1; // Aberto
                            record_gc_movimento.movimento_aprovado = false;
                            record_gc_movimento.comissao1_abate_nf = false;
                            record_gc_movimento.comissao2_abate_nf = false;
                            record_gc_movimento.obs_aprovacao = null;
                            record_gc_movimento.id_usuario_aprovacao = 0;
                            record_gc_movimento.datahora_aprovacao = null;
                            record_gc_movimento.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento.id_movimento_posicao = 0;
                            record_gc_movimento.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento).State = EntityState.Modified;
                            db.SaveChanges();

                            if (CachePersister.userIdentity.AmbienteDatabase != "Homologação") { EnviarEmailAprovacaoEspelhoDigital(record_old_gc_movimento, record_gc_movimento); }; // Somente enviar o email em produção
                            MsgRetorno += "Status do pedido " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>NÃO APROVADO</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "");
                            Sucesso = true;
                        }
                        else
                        {
                            String PosicaoAtual = string.Empty;
                            if (record_gc_movimento.id_movimento_posicao == 0) { PosicaoAtual = "Aberto"; } else { PosicaoAtual = db.gc_movimentos_posicao.Find(record_gc_movimento.id_movimento_posicao).posicao.EmptyIfNull().ToString(); };
                            MsgRetorno += "Não é possível retirar a APROVAÇÃO do Pedido Nº " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + ", o pedido está [<b>" + PosicaoAtual + "</b>]!";
                        }
                    }
                    // Log de Alterações
                    if (Sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_gc_movimento.id_movimento, LogAlteracoes); }; };
                    db.SaveChanges();
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetDadosModalItensComValor(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            int IdMovimento = -1;
            int.TryParse(param.yesCustomIdPK, out IdMovimento);
            var allRecords = (from _m in db.gc_movimentos_itens
                              join _p in db.g_produtos on _m.id_produto equals _p.id_produto
                              where _m.id_movimento == IdMovimento
                              orderby _m.sequencia, _m.id_movimento_item
                              select new { item = _m, produto = _p }).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            List<string[]> list = new List<string[]>();
            foreach (var l in displayedRecords)
            {
                String ValorTotal = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.item.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                String NomeProduto = l.produto.nome.EmptyIfNull().ToString().Trim();
                if (l.item.serial.EmptyIfNull().ToString().Trim().Length > 0) { NomeProduto += " [Serial: " + l.item.serial.EmptyIfNull().ToString().Trim() + "]"; };
                list.Add(new[] {
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.item.quantidade).Replace("R$ ","").Replace("R$","").Replace("$","").Replace(".00","").Replace(",00",""),
                                    NomeProduto,
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", l.item.valor_unit).Replace("R$ ","").Replace("R$","").Replace("$",""),
                                    ValorTotal,
                                }); ;
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

        #region Pedido - Separação
        public ActionResult ModalPedidoSeparacao(int? id)
        {
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            String MsgBloqueio = string.Empty;
            String MsgHistorico = string.Empty;
            String MsgObservacoes = string.Empty;
            String MsgCompetenciaBloqueada = string.Empty;
            DateTime DataInicioCompetencia;
            DateTime DataFimCompetencia;
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(id);
            List<gc_movimentos_itens> ListaItensPedido = new List<gc_movimentos_itens>();
            List<g_produtos> ListaProdutosPedido = new List<g_produtos>();
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia().Date;

            if (RecordMovimento != null)
            {
                if (RecordMovimento.movimento_separado == true) { TitleModal = LibIcons.getIcon("fa-solid fa-dolly", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Pedido Nº " + RecordMovimento.id_movimento.ToString() + " já Separado!"; }
                else { TitleModal = LibIcons.getIcon("fa-solid fa-dolly", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Separação do Pedido Nº " + RecordMovimento.id_movimento.ToString(); }

                gc_estoque_competencia RecordEstoqueCompetencia = db.gc_estoque_competencia.Where(e => e.status == "A").FirstOrDefault();
                if (RecordEstoqueCompetencia != null)
                {
                    DataInicioCompetencia = RecordEstoqueCompetencia.data_inicio;
                    DataFimCompetencia = RecordEstoqueCompetencia.data_fim;

                    if ((DataAtual >= DataInicioCompetencia) && (DataAtual <= DataFimCompetencia))
                    {
                        TitleModal += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style='font-size: 75%;'>[ " + DataInicioCompetencia.ToString("MMM/yyyy", new CultureInfo("pt-BR")) + " ]</span>";
                    }
                    else
                    {
                        MsgBloqueio += "<b> ---------- COMPETÊNCIA ATUAL DO ESTOQUE "+ DataInicioCompetencia.ToString("MMM/yyyy", new CultureInfo("pt-BR")).ToUpperInvariant() + " É DIFERENTE DA DATA ATUAL ----------</b>" + "<br/>";
                        MsgCompetenciaBloqueada += "<b> ---------- COMPETÊNCIA ATUAL DO ESTOQUE " + DataInicioCompetencia.ToString("MMM/yyyy", new CultureInfo("pt-BR")) + " É DIFERENTE DA DATA ATUAL ----------</b>" + "<br/>";
                    }
                }
                else
                {
                    MsgBloqueio += "<b> ---------- COMPETÊNCIA DO ESTOQUE NÃO CONFIGURADA ----------</b>" + "<br/>";
                }

                if (RecordMovimento.obs.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Pedido: " + RecordMovimento.obs.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.frete_observacoes.EmptyIfNull().ToString().Length > 0) { MsgHistorico = "OBS Frete: " + RecordMovimento.frete_observacoes.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.obs_aprovacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Aprovação: " + RecordMovimento.obs_aprovacao.EmptyIfNull().ToString() + "\r\n"; };

                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);
                if (RecordCfopOperacao.has_separacao == false)
                {
                    MsgBloqueio += "<b> ---------- O TIPO DE PEDIDO [" + RecordCfopOperacao.descricao.EmptyIfNull().ToString().Trim() + "] NÃO POSSUI ATIVIDADE DE SEPARAÇÃO ----------</b>" + "<br/>";
                }
                else
                {
                    gc_locais_estoque RecordLocalEstoque = db.gc_locais_estoque.Find(RecordMovimento.id_local_estoque);
                    if (RecordLocalEstoque.inventario_aberto == true)
                    {
                        MsgBloqueio += "<b> ---------- ESTOQUE EM PROCESSO DE INVENTÁRIO ----------</b>" + "<br/>";
                    }
                    else if (RecordCfopOperacao.has_separacao == false)
                    {
                        MsgBloqueio += "<b> ---------- O TIPO DE PEDIDO [" + RecordCfopOperacao.descricao.EmptyIfNull().ToString().Trim() + "] NÃO POSSUI ATIVIDADE DE SEPARAÇÃO ----------</b>" + "<br/>";
                    }
                    else
                    {
                        if ((RecordCfopOperacao.has_aprovacao == true) && (RecordMovimento.movimento_aprovado == false)) { MsgBloqueio += " - Pedido não foi APROVADO!<br/>"; }
                        if ((RecordCfopOperacao.has_separacao == true) && (RecordMovimento.movimento_separado == true)) { MsgBloqueio += " - Pedido já foi SEPARADO!<br/>"; }
                        if ((RecordCfopOperacao.has_financeiro == true) && (RecordMovimento.movimento_faturado == true)) { MsgBloqueio += " - Pedido já foi FATURADO!<br/>"; }
                        if ((RecordCfopOperacao.has_nfe == true) && (RecordMovimento.movimento_nf_autorizada == true)) { MsgBloqueio += " - Pedido já possui NFe Autorizada!<br/>"; }
                        if ((RecordCfopOperacao.has_notifica_email == true) && (RecordMovimento.movimento_notificado == true)) { MsgBloqueio += " - Pedido já foi NOTIFICADO!<br/>"; }
                        if ((RecordCfopOperacao.has_expedicao == true) && (RecordMovimento.movimento_expedido == true)) { MsgBloqueio += " - Pedido já foi EXPEDIDO!<br/>"; }
                        if ((RecordCfopOperacao.has_entrega == true) && (RecordMovimento.movimento_entregue == true)) { MsgBloqueio += " - Pedido já foi ENTREGUE!<br/>"; }
                    }
                }

                if (!CachePersister.userIdentity.Roles.Contains("gc_Movimentos_IndexPedido_*"))
                {
                    if (RecordMovimento.id_filial != CachePersister.userIdentity.id_filial)
                    {
                        MsgBloqueio += "<b> ---------- O PEDIDO NÃO PERTENCE AO LOCAL DE ESTOQUE DO USUÁRIO ----------</b>" + "<br/>";
                    }
                }
            }
            else
            {
                MsgBloqueio += "<b> ---------- PEDIDO BLOQUEADO PARA SEPARAÇÃO ----------</b>" + "<br/>";
                MsgBloqueio += " - Pedido Nº " + id.ToString() + " não localizado no ERP" + "<br/>";
            }
            ViewBag.Title = TitleModal;
            ViewBag.MsgBloqueio = MsgBloqueio;
            ViewBag.MsgHistorico = MsgHistorico;
            ViewBag.MsgObservacoes = MsgObservacoes;
            ViewBag.MsgCompetenciaBloqueada = MsgCompetenciaBloqueada;

            if (ListaItensPedido.Count() <= 1) { ViewBag.QtdItensPedido = ListaItensPedido.Count() + " Item no Pedido"; } else { ViewBag.QtdItensPedido = ListaItensPedido.Count() + " Itens no Pedido"; }
            return View(RecordMovimento);
        }

        public ActionResult ModalPedidoSeparacaoLotes(int idMovimento, int idMovimentoItem)
        {
            String Title = "Informe o(s) lote(s) para o item:<br/>";
            CstPedidoSeparacaoLote RecordCstPedidoSeparacaoLote = new CstPedidoSeparacaoLote();
            RecordCstPedidoSeparacaoLote.id_movimento = idMovimento;
            RecordCstPedidoSeparacaoLote.id_movimento_item = idMovimentoItem;

            gc_movimentos_itens RecordMovimentoItem = db.gc_movimentos_itens.Find(idMovimentoItem);
            if (RecordMovimentoItem != null)
            {
                String NomeProduto = db.g_produtos.Find(RecordMovimentoItem.id_produto).nome.EmptyIfNull().ToString();
                if (NomeProduto.Length > 50) { NomeProduto = NomeProduto.Substring(0, 50) + "..."; };
                Title += RecordMovimentoItem.quantidade.ToString("N0") + " x " + NomeProduto.EmptyIfNull().ToString();

                RecordCstPedidoSeparacaoLote.id_movimento = RecordMovimentoItem.id_movimento;
                RecordCstPedidoSeparacaoLote.id_movimento_item = RecordMovimentoItem.id_movimento_item;
                RecordCstPedidoSeparacaoLote.item_nome = RecordMovimentoItem.produto_externo_nome.EmptyIfNull().ToString();
                RecordCstPedidoSeparacaoLote.item_quantidade = RecordMovimentoItem.quantidade;

                var tipoSeparacao = RecordCstPedidoSeparacaoLote.GetType();
                var tipoMovimento = RecordMovimentoItem.GetType();
                for (int i = 1; i <= 50; i++)
                {
                    string loteNum = i.ToString("D2");
                    string propId = $"id_estoque_lote_{loteNum}";
                    string propQtd = $"lote{loteNum}_qtd";

                    var idValue = tipoMovimento.GetProperty(propId)?.GetValue(RecordMovimentoItem);
                    var qtdValue = tipoMovimento.GetProperty(propQtd)?.GetValue(RecordMovimentoItem);

                    tipoSeparacao.GetProperty(propId)?.SetValue(RecordCstPedidoSeparacaoLote, idValue);
                    tipoSeparacao.GetProperty(propQtd)?.SetValue(RecordCstPedidoSeparacaoLote, qtdValue);
                }

                List<gc_estoque_lotes> ListaLotes = new List<gc_estoque_lotes>();
                ListaLotes = db.gc_estoque_lotes.Where(l => l.ativo == true && l.id_produto == RecordMovimentoItem.id_produto).OrderBy(l => l.codigo_lote).ToList();
                RecordCstPedidoSeparacaoLote.ComboEstoqueLotes.Add(new SelectListItem { Value = "0", Text = "Selecione um lote" });

                foreach (var lote in ListaLotes)
                {
                    String TextoLote = lote.codigo_lote.EmptyIfNull().ToString().Trim();
                    if (lote.codigo_serial != null) { TextoLote += " | Serial.: " + lote.codigo_serial.EmptyIfNull().ToString(); }
                    if (lote.data_validade != null) { TextoLote += " | Venc.: " + lote.data_validade.Value.ToString("dd/MM/yyyy"); }
                    RecordCstPedidoSeparacaoLote.ComboEstoqueLotes.Add(new SelectListItem { Value = lote.id_estoque_lote.EmptyIfNull().ToString(), Text = TextoLote.EmptyIfNull().ToString() });
                }
            }
            ViewBag.Title = Title;
            return View("ModalPedidoSeparacaoLotes", RecordCstPedidoSeparacaoLote);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoSeparacaoLotes(CstPedidoSeparacaoLote RecordPedidoSeparacaoLote)
        {
            bool Sucesso = false;
            string MsgRetorno = string.Empty;
            int QtdInconsistencias = 0;
            try
            {
                gc_movimentos_itens RecordMovimentoItem = db.gc_movimentos_itens.Find(RecordPedidoSeparacaoLote.id_movimento_item);
                if (RecordMovimentoItem == null)
                {
                    QtdInconsistencias += 1;
                    MsgRetorno += "Item do pedido não localizado.";
                }

                if (ModelState.IsValid == false)
                {
                    QtdInconsistencias += 1;
                    if (MsgRetorno.Length > 0) { MsgRetorno += "<br/>"; }
                    MsgRetorno += String.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage));
                }

                // Conferido?
                if (QtdInconsistencias == 0)
                {
                    var tipo = RecordPedidoSeparacaoLote.GetType();

                    for (int i = 1; i <= 50; i++)
                    {
                        string loteNum = i.ToString("D2");
                        string propId = $"id_estoque_lote_{loteNum}";
                        string propQtd = $"lote{loteNum}_qtd";
                        string propConferido = $"lote{loteNum}_conferido";

                        var idEstoqueLote = Convert.ToInt32(tipo.GetProperty(propId)?.GetValue(RecordPedidoSeparacaoLote) ?? 0);
                        var qtd = Convert.ToDecimal(tipo.GetProperty(propQtd)?.GetValue(RecordPedidoSeparacaoLote) ?? 0);
                        var conferido = Convert.ToBoolean(tipo.GetProperty(propConferido)?.GetValue(RecordPedidoSeparacaoLote) ?? false);

                        if (idEstoqueLote > 0 && (qtd == 0 || conferido == false))
                        {
                            QtdInconsistencias += 1;
                            MsgRetorno += $"Informe [Qtd] e [Conferido] para o Lote {loteNum}<br/>";
                        }
                    }
                }

                // Soma total
                if (QtdInconsistencias == 0)
                {
                    var tipo = RecordPedidoSeparacaoLote.GetType();
                    decimal somaLotes = 0;

                    for (int i = 1; i <= 50; i++)
                    {
                        string loteNum = i.ToString("D2");
                        string propQtd = $"lote{loteNum}_qtd";

                        somaLotes += Convert.ToDecimal(tipo.GetProperty(propQtd)?.GetValue(RecordPedidoSeparacaoLote) ?? 0);
                    }

                    if (RecordMovimentoItem.quantidade != somaLotes)
                    {
                        QtdInconsistencias += 1;
                        MsgRetorno += "A Soma das quantidades informadas nos lotes separados deve ser igual a quantidade do item!<br/>";
                    }
                }

                // Gravar dados
                if (QtdInconsistencias == 0)
                {
                    var tipoSeparacao = RecordPedidoSeparacaoLote.GetType();
                    var tipoMovimento = RecordMovimentoItem.GetType();
                    for (int i = 1; i <= 50; i++)
                    {
                        string loteNum = i.ToString("D2");
                        string propId = $"id_estoque_lote_{loteNum}";
                        string propQtd = $"lote{loteNum}_qtd";
                        var idEstoqueLote = Convert.ToInt32(tipoSeparacao.GetProperty(propId)?.GetValue(RecordPedidoSeparacaoLote) ?? 0);
                        {
                            var qtd = tipoSeparacao.GetProperty(propQtd)?.GetValue(RecordPedidoSeparacaoLote);
                            tipoMovimento.GetProperty(propId)?.SetValue(RecordMovimentoItem, idEstoqueLote);
                            tipoMovimento.GetProperty(propQtd)?.SetValue(RecordMovimentoItem, qtd);
                        }
                    }
                    db.Entry(RecordMovimentoItem).State = EntityState.Modified;
                    db.SaveChanges();
                    Sucesso = true;
                    MsgRetorno = "Lote gravado com sucesso.";
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(ex);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno, id_movimento = RecordPedidoSeparacaoLote.id_movimento }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoSeparacao(gc_movimentos RecordViewMovimento)
        {
            bool Sucesso = false;
            bool EstoqueMovimentado = false;
            int QtdInconsistencias = 0;
            String MsgRetorno = string.Empty;
            try
            {
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(RecordViewMovimento.id_movimento);
                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);
                List<gc_movimentos_itens> ListaItensMovimento = db.gc_movimentos_itens.Where(i => i.id_movimento == RecordMovimento.id_movimento).ToList();

                if (ModelState.IsValid == false)
                {
                    MsgRetorno += String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    QtdInconsistencias += 1;

                }
                
                if (QtdInconsistencias == 0)
                {
                    if ((RecordMovimento.id_movimento_posicao != 1) && (RecordMovimento.id_movimento_posicao != 2))
                    {
                        String PosicaoAtual = string.Empty;
                        if (RecordMovimento.id_movimento_posicao == 0) { PosicaoAtual = "Aberto"; } else { PosicaoAtual = db.gc_movimentos_posicao.Find(RecordMovimento.id_movimento_posicao).posicao.EmptyIfNull().ToString(); };
                        MsgRetorno += "O Pedido está na posição [" + PosicaoAtual + "], não é possível Confirmar/Reprovar a Separação!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                }


                if (QtdInconsistencias == 0)
                {
                    // Validação dos Pesos
                    int QtdInfoPeso = 0;
                    if (RecordViewMovimento.frete_pesol > 0) { QtdInfoPeso += 1; };
                    if (RecordViewMovimento.frete_pesob > 0) { QtdInfoPeso += 1; };
                    if ((QtdInfoPeso > 0) && (QtdInfoPeso < 2))
                    {
                        MsgRetorno += "Quando informar Pesos, deve-se informar Peso Liquido e Peso Bruto!" + "<br/>";
                        QtdInconsistencias += 1;
                    }

                    // Validação dos Pesos
                    int QtdInfoMedidas = 0;
                    if (RecordViewMovimento.frete_dimensao_altura > 0) { QtdInfoMedidas += 1; };
                    if (RecordViewMovimento.frete_dimensao_largura > 0) { QtdInfoMedidas += 1; };
                    if (RecordViewMovimento.frete_dimensao_profundidade > 0) { QtdInfoMedidas += 1; };
                    if ((QtdInfoMedidas > 0) && (QtdInfoMedidas < 3))
                    {
                        MsgRetorno += "Quando informar Medidas, deve-se informar Altura, Largura e Comprimento!" + "<br/>";
                        QtdInconsistencias += 1;
                    }
                }

                if (QtdInconsistencias == 0)
                {
                    int Index = 0;
                    foreach (var ItemMovimento in ListaItensMovimento)
                    {
                        Index += 1;
                        var tipo = ItemMovimento.GetType();
                        decimal somaLotes = 0;

                        for (int i = 1; i <= 50; i++)
                        {
                            string loteNum = i.ToString("D2");
                            string propId = $"id_estoque_lote_{loteNum}";
                            string propQtd = $"lote{loteNum}_qtd";

                            var idEstoqueLote = Convert.ToInt32(tipo.GetProperty(propId)?.GetValue(ItemMovimento) ?? 0);
                            var qtd = Convert.ToDecimal(tipo.GetProperty(propQtd)?.GetValue(ItemMovimento) ?? 0);

                            somaLotes += qtd;

                            if (idEstoqueLote > 0 && qtd == 0)
                            {
                                QtdInconsistencias += 1;
                                MsgRetorno += $"Informe [Qtd] para o Item [{Index}] Lote {loteNum}<br/>";
                            }
                        }

                        if (somaLotes == 0)
                        {
                            QtdInconsistencias += 1;
                            MsgRetorno += $"Item [{Index}] NÃO separado!<br/>";
                        }
                        else if (ItemMovimento.quantidade != somaLotes)
                        {
                            QtdInconsistencias += 1;
                            MsgRetorno += $"Erro nas quantidades informadas no item [{Index}]!<br/>";
                        }
                    }
                }


                if (RecordViewMovimento.movimento_separado == true)
                {
                    // Validação Estoque na Separação - Somente se não tiver separado antes
                    if (RecordMovimento.id_movimento_posicao == 1) // Aprovado
                    {
                        int TipoMovimentoEstoque = 11; // Saída - Separação Pedido
                        if (RecordMovimento.id_movimento_tipo == 19) { TipoMovimentoEstoque = 5; } // Saída - Transferência
                        EstoqueInventarioService ServicoEstoqueInventario = new EstoqueInventarioService();
                        if (ServicoEstoqueInventario.ValidarEstoque(RecordMovimento.id_movimento, TipoMovimentoEstoque, db) == false)
                        {
                            QtdInconsistencias += 1;
                            MsgRetorno += ServicoEstoqueInventario.GetMsgProcessamento();
                        }
                    }
                }

                String ListaIdsLotes = string.Empty;
                if (QtdInconsistencias == 0)
                {
                    foreach (var ItemMovimento in ListaItensMovimento)
                    {
                        var tipo = ItemMovimento.GetType();

                        for (int i = 1; i <= 50; i++)
                        {
                            string propId = $"id_estoque_lote_{i.ToString("D2")}";
                            var idEstoqueLote = Convert.ToInt32(tipo.GetProperty(propId)?.GetValue(ItemMovimento) ?? 0);

                            if (idEstoqueLote > 0)
                                ListaIdsLotes += idEstoqueLote.ToString() + ",";
                        }
                    }

                    if (ListaIdsLotes.EmptyIfNull().Trim().Length > 0)
                    {
                        ListaIdsLotes = ListaIdsLotes.Trim().TrimEnd(',');
                    }

                    // Documentos associados aos lotes anteriores
                    var arquivos = db.ged_arquivos
                        .Where(g => g.id_arquivo_origem == 1 && g.id_gc_movimento == RecordMovimento.id_movimento)
                        .ToList();
                    arquivos.ForEach(g => g.ativo = false);
                    db.SaveChanges();

                    // Documentos associados aos lotes atuais, associar ao movimento
                    int[] Ids = ListaIdsLotes
                        .Split(',')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(int.Parse)
                        .ToArray();

                    List<ged_arquivos> ListaArquivosGed = db.ged_arquivos
                        .Where(x => Ids.Contains(x.id_estoque_lote))
                        .ToList();

                    foreach (var ArquivoGedMovimento in ListaArquivosGed)
                    {
                        ged_arquivos NewRecordGed = LibDB.CloneTObject(ArquivoGedMovimento);
                        NewRecordGed.id_arquivo = 0;
                        NewRecordGed.id_estoque_lote = 0;
                        NewRecordGed.id_gc_movimento = RecordMovimento.id_movimento;
                        NewRecordGed.id_arquivo_origem = 1;
                        db.ged_arquivos.Add(NewRecordGed);
                    }

                    db.SaveChanges();
                }

                if (QtdInconsistencias == 0)
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                    if (RecordViewMovimento.movimento_separado == true)
                    {
                        if (RecordMovimento.id_movimento_posicao == 1) // Aprovado
                        {
                            // MOVIMENTO ESTAVA APROVADO E FOI SEPARADO - VERIFICAR O TIPO DE OPERAÇÃO SE BAIXA O ESTOQUE
                            if (RecordCfopOperacao.estoque_saida == true)
                            {
                                EstoqueInventarioService ServicoEstoqueBaixa = new EstoqueInventarioService();
                                int TipoMovimentoEstoque = 11; // Saída - Separação Pedido
                                if (RecordMovimento.id_movimento_tipo == 19) { TipoMovimentoEstoque = 5; } // Saída - Transferência
                                EstoqueMovimentado = ServicoEstoqueBaixa.MovimentarEstoque(RecordMovimento.id_movimento, TipoMovimentoEstoque, db, false); // Saída do Estoque - Separação
                                if (EstoqueMovimentado == false)
                                {
                                    QtdInconsistencias += 1;
                                    MsgRetorno += ServicoEstoqueBaixa.GetMsgProcessamento(); ;
                                }
                            }
                        }

                        if (QtdInconsistencias == 0)
                        {
                            if (RecordMovimento.id_movimento_tipo == 3) { RecordMovimento.id_movimento_tipo = 4; } // Pedido
                            RecordMovimento.movimento_separado = true;
                            RecordMovimento.movimento_estoque_saida = true;
                            RecordMovimento.movimento_estoque_entrada = false;
                            RecordMovimento.frete_qvol = RecordViewMovimento.frete_qvol;
                            RecordMovimento.frete_esp = RecordViewMovimento.frete_esp;
                            RecordMovimento.frete_marca = RecordViewMovimento.frete_marca;
                            RecordMovimento.frete_nvol = RecordViewMovimento.frete_nvol;
                            RecordMovimento.frete_pesol = RecordViewMovimento.frete_pesol;
                            RecordMovimento.frete_pesob = RecordViewMovimento.frete_pesob;
                            RecordMovimento.frete_dimensao_altura = RecordViewMovimento.frete_dimensao_altura;
                            RecordMovimento.frete_dimensao_largura = RecordViewMovimento.frete_dimensao_largura;
                            RecordMovimento.frete_dimensao_profundidade = RecordViewMovimento.frete_dimensao_profundidade;
                            RecordMovimento.obs_separacao = RecordViewMovimento.obs_separacao;
                            RecordMovimento.id_movimento_status = 2; // Fechado
                            RecordMovimento.id_usuario_separacao = CachePersister.userIdentity.IdUsuario;
                            RecordMovimento.datahora_separacao = DataHoraAtual;
                            if (RecordMovimento.id_movimento_posicao < 2) 
                            {
                                RecordMovimento.id_movimento_posicao = 2;
                                MsgRetorno += "Status do pedido " + RecordMovimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>SEPARADO</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" + "<b>Atenção: Os itens do pedido foram BAIXADOS do Estoque Físico</b>";
                            }  // Separado                            {
                            else if (RecordMovimento.id_movimento_posicao == 2)
                            {
                                RecordMovimento.id_movimento_posicao = 2;
                                MsgRetorno += "Dados da SEPARAÇÃO do pedido nº " + RecordMovimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>ATUALIZADOS</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                            }  // Separado                            {

                            RecordMovimento.datahora_alteracao = DataHoraAtual;
                            RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordMovimento).State = EntityState.Modified;
                            Sucesso = true;
                        }
                    }
                    else if (RecordViewMovimento.movimento_separado == false)
                    {
                        if (RecordMovimento.id_movimento_posicao == 2)
                        {
                            // MOVIMENTO ESTAVA SEPARADO E FOI RETIRADA A SEPARAÇÃO - VERIFICAR SE O TIPO DE OPERAÇÃO FAZ BAIXA DE ESTOQUE
                            if (RecordCfopOperacao.estoque_saida == true)
                            {
                                EstoqueInventarioService ServicoEstoqueEntrada = new EstoqueInventarioService();
                                EstoqueMovimentado = ServicoEstoqueEntrada.MovimentarEstoque(RecordMovimento.id_movimento, 10, db, false); // Entrada - Cancelamento Separação
                                if (EstoqueMovimentado == false)
                                {
                                    QtdInconsistencias += 1;
                                    MsgRetorno += ServicoEstoqueEntrada.GetMsgProcessamento(); ;
                                }
                            }


                            if (QtdInconsistencias == 0)
                            {

                                RecordMovimento.movimento_separado = false;
                                RecordMovimento.movimento_estoque_saida = false;
                                RecordMovimento.movimento_estoque_entrada = true;
                                RecordMovimento.movimento_estoque_devolucao = true;
                                RecordMovimento.frete_qvol = 0;
                                RecordMovimento.frete_esp = string.Empty;
                                RecordMovimento.frete_marca = string.Empty;
                                RecordMovimento.frete_nvol = string.Empty;
                                RecordMovimento.frete_pesol = 0;
                                RecordMovimento.frete_pesob = 0;
                                RecordMovimento.frete_dimensao_altura = 0;
                                RecordMovimento.frete_dimensao_largura = 0;
                                RecordMovimento.frete_dimensao_profundidade = 0;
                                RecordMovimento.obs_separacao = string.Empty;
                                RecordMovimento.id_usuario_separacao = 0;
                                RecordMovimento.datahora_separacao = null;
                                RecordMovimento.id_movimento_posicao = 1; // Aprovado
                                MsgRetorno += "Status do pedido " + RecordMovimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>NÃO SEPARADO</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "") + "<br/><br/>" + "<b>Atenção: Os itens do pedido foram DEVOLVIDOS para o Estoque Físico</b>";
                                RecordMovimento.datahora_alteracao = DataHoraAtual;
                                RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(RecordMovimento).State = EntityState.Modified;
                                Sucesso = true;
                            }
                        }
                        else
                        {
                            String PosicaoAtual = string.Empty;
                            if (RecordMovimento.id_movimento_posicao == 0) { PosicaoAtual = "Aberto"; } else { PosicaoAtual = db.gc_movimentos_posicao.Find(RecordMovimento.id_movimento_posicao).posicao.EmptyIfNull().ToString(); };
                            MsgRetorno += "Não é possível excluir os dados da SEPARAÇÃO do Pedido " + RecordMovimento.id_movimento.EmptyIfNull().ToString() + ", o pedido está [<b>" + PosicaoAtual + "</b>]!";
                        }
                    }

                    if (QtdInconsistencias == 0)
                    {
                        // Log de Alterações
                        String LogAlteracoes = String.Empty;
                        List<g_vendedores> AllVendedores = db.g_vendedores.Where(v => v.ativo == true).OrderBy(p => p.nome).ToList();
                        if (RecordViewMovimento.movimento_separado == true) { LogAlteracoes += "Separação do Pedido | "; } else { LogAlteracoes += "Cancelamento da Separação Pedido | "; };
                        if (RecordMovimento.frete_qvol > 0) { LogAlteracoes += "Volumes: " + RecordMovimento.frete_qvol.ToString() + " | "; };
                        if (RecordMovimento.frete_esp.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Espécie: " + RecordMovimento.frete_esp.EmptyIfNull().ToString().Trim() + " | "; };
                        if (RecordMovimento.frete_marca.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Marca: " + RecordMovimento.frete_marca.EmptyIfNull().ToString().Trim() + " | "; };
                        if (RecordMovimento.frete_nvol.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Numeração: " + RecordMovimento.frete_nvol.EmptyIfNull().ToString().Trim() + " | "; };
                        if (RecordMovimento.frete_pesol > 0) { LogAlteracoes += "Peso Liq.: " + RecordMovimento.frete_pesol.ToString("0.000") + " | "; };
                        if (RecordMovimento.frete_pesob > 0) { LogAlteracoes += "Peso Bruto: " + RecordMovimento.frete_pesob.ToString("0.000") + " | "; };
                        if (RecordMovimento.frete_dimensao_altura > 0) { LogAlteracoes += "Medida-A: " + RecordMovimento.frete_dimensao_altura.ToString("0.000") + " | "; };
                        if (RecordMovimento.frete_dimensao_largura > 0) { LogAlteracoes += "Medida-L: " + RecordMovimento.frete_dimensao_largura.ToString("0.000") + " | "; };
                        if (RecordMovimento.frete_dimensao_profundidade > 0) { LogAlteracoes += "Medida-C: " + RecordMovimento.frete_dimensao_profundidade.ToString("0.000") + " | "; };
                        if (RecordMovimento.obs_separacao.EmptyIfNull().ToString().Length > 0) { LogAlteracoes += "Obs: " + RecordMovimento.obs_separacao.ToString() + " | "; };

                        if (Sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", RecordMovimento.id_movimento, LogAlteracoes); }; };
                        db.SaveChanges();
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno += LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno += LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AjaxGetXMLEnvioSefaz(gc_movimentos_nf view_record_gc_movimento_nf)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String MsgRetorno = "";
            String BodyXML = string.Empty;
            String FileNameXml = String.Empty;
            String idProcessamentoGravado = "0";

            //String idProcessamentoGravado = "0";
            var pdf = new ViewAsPdf();
            try
            {
                if (view_record_gc_movimento_nf == null)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else if (view_record_gc_movimento_nf.id_movimento_nf <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else
                {
                    gc_movimentos_nf record_gc_movimentos_nf = db.gc_movimentos_nf.Find(view_record_gc_movimento_nf.id_movimento_nf);
                    RoboEnotasNFE BotEnotas = new RoboEnotasNFE();
                    BodyXML = BotEnotas.GetXMLEnvioSefaz(record_gc_movimentos_nf);

                    if (BodyXML.EmptyIfNull().ToString().Length > 0)
                    {
                        if (BodyXML.StartsWith("#ERRO#"))
                        {
                            Sucesso = false;
                            MsgRetorno = "Não foi possível realizar o download do XML Sefaz!" + "\r\n" + BodyXML.EmptyIfNull().ToString();
                        }
                        else
                        {
                            String DirTempFiles = Server.MapPath("~/_filestemp");
                            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                            DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                            DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                            LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                            FileNameXml = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMdd") + "_NF" + record_gc_movimentos_nf.nf_numero.EmptyIfNull().ToString() + ".xml");
                            LibCache.LiberarMemoria();
                            using (StreamWriter w = new StreamWriter(FileNameXml, true, Encoding.UTF8)) { w.Write(BodyXML); w.Flush(); w.Close(); w.Dispose(); }

                            g_processamento record_g_processamento = new g_processamento();
                            record_g_processamento.id_processamento_tipo = 17; // WebService NFE
                            record_g_processamento.id_processamento_modulo = 3; // Modulo Comercial
                            record_g_processamento.detalhamento = "XML Envio Sefaz";
                            record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                            record_g_processamento.datahora_inicio = LibDateTime.getDataHoraBrasilia();
                            record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                            record_g_processamento.qtd_registros = 1;
                            record_g_processamento.qtd_reg_ok = 1;
                            record_g_processamento.qtd_reg_erro = 0;
                            record_g_processamento.processando = false;
                            record_g_processamento.concluido = true;
                            record_g_processamento.pathfile = FileNameXml;
                            record_g_processamento.id_coligada = 0; // Global
                            record_g_processamento.id_filial = 0; // Global
                            db.g_processamento.Add(record_g_processamento);
                            db.SaveChanges();
                            Sucesso = true;
                            idProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                            MsgRetorno = "XML Envio Sefaz recuperado com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" + "Obs: O Download será iniciado automaticamente na sequência";
                        }
                    }
                    else
                    {
                        Sucesso = false;
                        MsgRetorno = "Não foi possível realizar o download do XML Sefaz!";
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Notas Fiscais
        public ActionResult ModalPedidoNotaFiscal(int? id)
        {
            int IdMovimentoTemp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            String MsgBloqueio = string.Empty;
            String MsgInfoDifal = string.Empty;
            String MsgCalculoDifal = string.Empty;
            String MsgLancamentosFinanceiros = string.Empty;
            String ClienteNome = string.Empty;
            String MsgHistorico = string.Empty;

            gc_movimentos_nf record_gc_movimento_nf = new gc_movimentos_nf();
            record_gc_movimento_nf.id_movimento = IdMovimentoTemp;
            gc_cfop_operacoes RecordCfopOperacoes = null;
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(IdMovimentoTemp);
            if (RecordMovimento != null)
            {
                if (RecordMovimento.obs.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Pedido: " + RecordMovimento.obs.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.frete_observacoes.EmptyIfNull().ToString().Length > 0) { MsgHistorico = "OBS Frete: " + RecordMovimento.frete_observacoes.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.obs_aprovacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Aprovação: " + RecordMovimento.obs_aprovacao.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.obs_separacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Separação: " + RecordMovimento.obs_separacao.EmptyIfNull().ToString() + "\r\n"; };
                RecordCfopOperacoes = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);
            }
            g_clientes RecordCliente = db.g_clientes.Find(RecordMovimento.id_cliente);
            g_uf RecordUfDestinatarioICMS = db.g_uf.Find(RecordCliente.id_uf_com);
            gc_parametros_difal record_gc_parametros_difal = db.gc_parametros_difal.Where(p => p.id_uf == RecordCliente.id_uf_com).FirstOrDefault();
            if (RecordMovimento.id_filial == 1) 
            { 
                if (RecordCliente.id_uf_com == 11) 
                { 
                    record_gc_movimento_nf.id_cfop = 2; } else { record_gc_movimento_nf.id_cfop = 1; 
                }
                if ((RecordCliente.id_indicador_ie == 3) && (RecordUfDestinatarioICMS.id_uf != 11)) { record_gc_movimento_nf.id_lancamento_financeiro_difal = 1; } // Tem difal nessa NF
            }
            else if (RecordMovimento.id_filial == 2) 
            { 
                if (RecordCliente.id_uf_com == 26) 
                { 
                    record_gc_movimento_nf.id_cfop = 2; } else { record_gc_movimento_nf.id_cfop = 1; 
                }
                if ((RecordCliente.id_indicador_ie == 3) && (RecordUfDestinatarioICMS.id_uf != 26)) { record_gc_movimento_nf.id_lancamento_financeiro_difal = 1; } // Tem difal nessa NF
            };
            record_gc_movimento_nf.id_transportadora = RecordMovimento.frete1_transportadora;
            record_gc_movimento_nf.frete_valor = RecordMovimento.frete_valor;
            record_gc_movimento_nf.frete_qvol = RecordMovimento.frete_qvol;
            record_gc_movimento_nf.frete_esp = RecordMovimento.frete_esp;
            record_gc_movimento_nf.frete_marca = RecordMovimento.frete_marca;
            record_gc_movimento_nf.frete_nvol = RecordMovimento.frete_nvol;
            record_gc_movimento_nf.frete_pesol = RecordMovimento.frete_pesol;
            record_gc_movimento_nf.frete_pesob = RecordMovimento.frete_pesob;
            record_gc_movimento_nf.frete_dimensao_altura = RecordMovimento.frete_dimensao_altura;
            record_gc_movimento_nf.frete_dimensao_largura = RecordMovimento.frete_dimensao_largura;
            record_gc_movimento_nf.frete_dimensao_profundidade = RecordMovimento.frete_dimensao_profundidade;
            record_gc_movimento_nf.param_reducao_bc = RecordMovimento.param_reducao_bc;
            record_gc_movimento_nf.param_zerar_difal = RecordMovimento.param_zerar_difal;
            record_gc_movimento_nf.icms_difal_calculado = RecordMovimento.icms_difal_calculado;
            record_gc_movimento_nf.id_frete_responsavel = RecordMovimento.id_frete_responsavel;
            record_gc_movimento_nf.id_ambiente_sefaz = 0;
            record_gc_movimento_nf.transportadora_cotacao = RecordMovimento.frete1_documento;
            record_gc_movimento_nf.id_cfop_operacao = RecordMovimento.id_cfop_operacao;
            record_gc_movimento_nf.nf_chave_referenciada = RecordMovimento.nf_chave_referenciada;
            record_gc_movimento_nf.id_coligada = RecordMovimento.id_coligada;
            record_gc_movimento_nf.id_filial = RecordMovimento.id_filial;

            // Zerar o Difal - Obedecer os parâmetros por grupo
            if (record_gc_parametros_difal != null)
            {
                if ((record_gc_parametros_difal.difal_geral_calcular == false) && (record_gc_parametros_difal.difal_comb_calcular == false))
                { record_gc_movimento_nf.param_zerar_difal = true; }
                else { record_gc_movimento_nf.param_zerar_difal = false; }

                MsgCalculoDifal = string.Empty;
                if (record_gc_parametros_difal.difal_geral_calcular == true) { MsgCalculoDifal += "Difal Geral (Calcular) | "; }
                else if (record_gc_parametros_difal.difal_geral_zerar == true) { MsgCalculoDifal += "Difal Geral (Zerar) | "; }
                else if (record_gc_parametros_difal.difal_geral_naoinformar == true) { MsgCalculoDifal += "Difal Geral (Não Informar) | "; }

                if (record_gc_parametros_difal.difal_comb_calcular == true) { MsgCalculoDifal += "Difal Comb. (Calcular) | "; }
                else if (record_gc_parametros_difal.difal_comb_zerar == true) { MsgCalculoDifal += "Difal Comb. (Zerar) | "; }
                else if (record_gc_parametros_difal.difal_comb_naoinformar == true) { MsgCalculoDifal += "Difal Comb. (Não Informar) | "; }
            }

            TitleModal = LibIcons.getIcon("fa-solid fa-file-invoice", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Gerar Nota Fiscal para o Pedido Nº " + record_gc_movimento_nf.id_movimento.ToString();
            ViewBag.Title = TitleModal;
            ViewBag.ClienteNome = string.Empty;
            ViewBag.ClienteEndereco1 = string.Empty;
            try
            {
                ClienteNome += RecordCliente.nome.EmptyIfNull().ToString();
                if (RecordMovimento.id_cfop_finalidade == 2) { ClienteNome += "   (REVENDA - "; } else { ClienteNome += "   (CONSUMIDOR FINAL - "; }
                if (RecordMovimento.param_reducao_bc == true) { ClienteNome += " ICMS REDUZIDO"; } else { ClienteNome += " ICMS NORMAL)"; };
                ViewBag.ClienteNome = ClienteNome;
            }
            catch (Exception) { };
            try
            {
                ViewBag.ClienteEndereco1 += RecordCliente.endereco_com.EmptyIfNull().ToUpper().ToString();
                ViewBag.ClienteEndereco1 += " - " + RecordCliente.bairro_com.EmptyIfNull().ToUpper().ToString();
                ViewBag.ClienteEndereco1 += " - " + RecordCliente.cep_com.EmptyIfNull().ToUpper().ToString();
                ViewBag.ClienteEndereco1 += " - " + db.g_cidades.Find(RecordCliente.id_cidade_com).nome.EmptyIfNull().ToUpper().ToString();
                ViewBag.ClienteEndereco1 += " - " + db.g_uf.Find(RecordCliente.id_uf_com).nome.EmptyIfNull().ToUpper().ToString();
            }
            catch (Exception) { };


            if (RecordCfopOperacoes.has_financeiro == true)
            {
                int QtdLancamentosFinanceiros = LibDB.dbQueryCount("select count(*) from gc_financeiro_lancamentos where tipo_pag_rec = 2 and id_movimento = " + RecordMovimento.id_movimento.EmptyIfNull().ToString(), db);
                if (QtdLancamentosFinanceiros == 0) { MsgLancamentosFinanceiros = "<b>ATENÇÃO:</b> Não foram localizados Lançamentos Financeiros (Contas à Receber) referente à esse pedido. Para CFOPs faturáveis, a geração de contas à receber é obrigatório antes da geração da NF!"; };
            }

            if (record_gc_movimento_nf.id_lancamento_financeiro_difal == 1)
            {
                string ValorDifal = "R$ " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_movimento_nf.icms_difal_calculado).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                if (record_gc_movimento_nf.param_zerar_difal == true)
                {
                    MsgInfoDifal += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b style=\"color:#cc0000\">" + ValorDifal + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + MsgCalculoDifal + "!</b>";

                }
                else if (record_gc_movimento_nf.param_zerar_difal == false)
                {
                    MsgInfoDifal += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<b style=\"color:#008000\">" + ValorDifal + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + MsgCalculoDifal + "!</b>";
                }
            }

            ViewBag.MsgInfoDifal = MsgInfoDifal;
            ViewBag.MsgHistorico = MsgHistorico;
            ViewBag.MsgLancamentosFinanceiros = MsgLancamentosFinanceiros;
            ViewBag.comboCFOP = LibDataSets.LoadComboGcCfop(db);
            ViewBag.comboCfopOperacoes = LibDataSets.LoadComboGcCfopOperacoesFaturamentoPedido(db, RecordMovimento.id_cfop_operacao);
            ViewBag.comboFreteResponsavel = LibDataSets.LoadComboGcFreteResponsavel(db);
            ViewBag.comboTransportadora = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.comboTransportadora.Insert(0, new SelectListItem { Value = "0", Text = "SEM TRANSPORTADORA" });
            ViewBag.ComboDestinatarios = new List<SelectListItem>();

            if (RecordMovimento.id_cliente_destinatario > 0)
            {
                g_clientes_destinatarios RecordDestinatario = db.g_clientes_destinatarios.Where(c => c.id_cliente_destinatario == RecordMovimento.id_cliente_destinatario).FirstOrDefault();
                if (RecordDestinatario != null) { ViewBag.ComboDestinatarios.Add(new SelectListItem { Value = RecordDestinatario.id_cliente_destinatario.ToString(), Text = RecordDestinatario.nome.ToString() }); }
                record_gc_movimento_nf.id_cliente_destinatario = RecordMovimento.id_cliente_destinatario;
            }
            else
            {
                ViewBag.ComboDestinatarios.Add(new SelectListItem { Value = "0", Text = "[ O PRÓPRIO CLIENTE ]" });
            }

            gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);

            if (RecordCfopOperacao.has_nfe == false)
            {
                MsgBloqueio += "<b> ---------- O TIPO DE PEDIDO [" + RecordCfopOperacao.descricao.EmptyIfNull().ToString().Trim() + "] NÃO POSSUI ATIVIDADE DE NOTA FISCAL ----------</b>" + "<br/>";
            }
            else
            {
                if ((RecordCfopOperacao.has_aprovacao == true) && (RecordMovimento.movimento_aprovado == false)) { MsgBloqueio += " - Pedido não foi APROVADO!<br/>"; }
                if ((RecordCfopOperacao.has_separacao == true) && (RecordMovimento.movimento_separado == false)) { MsgBloqueio += " - Pedido não foi SEPARADO!<br/>"; }
                if ((RecordCfopOperacao.has_financeiro == true) && (RecordMovimento.movimento_faturado == false)) { MsgBloqueio += " - Pedido não foi FATURADO!<br/>"; }
                //if ((RecordCfopOperacao.has_notifica_email == true) && (RecordMovimento.movimento_notificado == true)) { MsgBloqueio += " - Pedido já foi NOTIFICADO!<br/>"; }
                //if ((RecordCfopOperacao.has_nfe == true) && (RecordMovimento.movimento_nf_autorizada == true)) { MsgBloqueio += " - Pedido já possui NFe Autorizada!<br/>"; }
                //if ((RecordCfopOperacao.has_expedicao == true) && (RecordMovimento.movimento_expedido == true)) { MsgBloqueio += " - Pedido já foi EXPEDIDO!<br/>"; }
                //if ((RecordCfopOperacao.has_entrega == true) && (RecordMovimento.movimento_entregue == true)) { MsgBloqueio += " - Pedido já foi ENTREGUE!<br/>"; }
            }

            //ViewBag.Prefixo = ComplementoPrefixo;
            ViewBag.MsgBloqueio = MsgBloqueio;
            record_gc_movimento_nf.id_cfop = 0;
            return View("ModalPedidoNotaFiscal", record_gc_movimento_nf);
        }
        public ActionResult ModalViewNotasFiscais(int? id)
        {
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            if (record_gc_movimento != null)
            {
                TitleModal = LibIcons.getIcon("fa-solid fa-file-invoice", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Notas Fiscais - Movimento Nº " + record_gc_movimento.id_movimento.ToString();
            }
            ViewBag.Title = TitleModal;
            return View("ModalViewNotasFiscais", record_gc_movimento);
        }

        public ActionResult GetNotaFiscalPedido(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";

            var allRecords = new List<Db.gc_movimentos_nf>();
            var allRecordsAtualizarNFE = new List<Db.gc_movimentos_nf>();
            var allRecordsNfeStatus = db.g_nfe_status.Select(s => new { s.id_nfe_status, s.processamento, s.descricao_resumida }).ToList();
            var allRecordsCfop = db.gc_cfop.Select(c => new { c.id_cfop, c.numero }).ToList();
            String SentencaSQL = string.Empty;

            SentencaSQL = " select * from gc_movimentos_nf nf ";
            if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1"))
            { SentencaSQL += " where nf.id_movimento = " + param.yesCustomField01.EmptyIfNull().ToString().Trim(); }
            SentencaSQL += " order by nf.id_movimento_nf desc";

            allRecordsAtualizarNFE = db.gc_movimentos_nf.SqlQuery(SentencaSQL).ToList();
            foreach (var record_gc_movimento_nf in allRecordsAtualizarNFE)
            {
                // Atualização de Status das Notas
                if (allRecordsNfeStatus.Find(s => s.id_nfe_status == record_gc_movimento_nf.id_nfe_status).processamento == true)
                {
                    if ((record_gc_movimento_nf.id_nfe_gateway == 1) || (record_gc_movimento_nf.id_nfe_gateway == 2)) // Enotas
                    {
                        RoboEnotasNFE _RoboFaturarNFP = new RoboEnotasNFE();
                        _RoboFaturarNFP.AtualizarStatusNFPbyMovimentoNF(record_gc_movimento_nf);
                    }
                }
            }

            allRecords = db.gc_movimentos_nf.SqlQuery(SentencaSQL).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.gc_movimentos_nf, string> orderingFunction = (c => param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_movimento) : "");

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
            foreach (var nf in displayedRecords)
            {
                String CfopNumero = String.Empty;
                if (nf.id_cfop > 0)
                {
                    CfopNumero += allRecordsCfop.Find(c => c.id_cfop == nf.id_cfop).numero.EmptyIfNull().ToString();
                }
                else
                {
                    if (nf.id_cfop_grupo1 > 0) { CfopNumero += allRecordsCfop.Find(c => c.id_cfop == nf.id_cfop_grupo1).numero.EmptyIfNull().ToString(); }
                    if (nf.id_cfop_grupo2 > 0) { CfopNumero += "/" + allRecordsCfop.Find(c => c.id_cfop == nf.id_cfop_grupo2).numero.EmptyIfNull().ToString(); }
                    if (nf.id_cfop_grupo3 > 0) { CfopNumero += "/" + allRecordsCfop.Find(c => c.id_cfop == nf.id_cfop_grupo3).numero.EmptyIfNull().ToString(); }
                    if (nf.id_cfop_grupo4 > 0) { CfopNumero += "/" + allRecordsCfop.Find(c => c.id_cfop == nf.id_cfop_grupo4).numero.EmptyIfNull().ToString(); }
                    if (nf.id_cfop_grupo5 > 0) { CfopNumero += "/" + allRecordsCfop.Find(c => c.id_cfop == nf.id_cfop_grupo5).numero.EmptyIfNull().ToString(); }
                }
                String DataHoraNF = nf.datahora_cadastro.ToString("dd/MM/yy HH:mm"); ;
                if ((nf.nf_data_autorizacao != null) && (nf.nf_data_autorizacao.GetValueOrDefault().Year > 2000)) { DataHoraNF = nf.nf_data_autorizacao.GetValueOrDefault().ToString("dd/MM/yy HH:mm"); }
                else if ((nf.nf_data_emissao != null) && (nf.nf_data_emissao.GetValueOrDefault().Year > 2000)) { DataHoraNF = nf.nf_data_emissao.GetValueOrDefault().ToString("dd/MM/yy HH:mm"); }
                else if ((nf.nf_data_criacao != null) && (nf.nf_data_criacao.GetValueOrDefault().Year > 2000)) { DataHoraNF = nf.nf_data_criacao.GetValueOrDefault().ToString("dd/MM/yy HH:mm"); }
                else if ((nf.nf_data_geracao != null) && (nf.nf_data_geracao.GetValueOrDefault().Year > 2000)) { DataHoraNF = nf.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy HH:mm"); };
                var arrayStatusNfe = allRecordsNfeStatus.Find(s => s.id_nfe_status == nf.id_nfe_status);
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    nf.id_movimento_nf.ToString(),
                                    ((arrayStatusNfe != null) ? arrayStatusNfe.descricao_resumida.EmptyIfNull().ToString() : String.Empty),
                                    CfopNumero,
                                    nf.nf_numero.EmptyIfNull().ToString(),
                                    DataHoraNF,
                                    nf.qtd_itens.ToString(),
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", nf.valor_total_bruto),
                                    "", // Botão XML
                                    "", // Botão PDF
                                    "", // Botão Informações
                                    "", // Botão Termo Convênio ICMS
                                    "", // Botão XML Envio Sefaz
                                    "" // Botão Cancelar
                                }); ;
            }

            return Json(new
            {
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count(),
                iTotalDisplayRecords = allRecords.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoNotaFiscal(gc_movimentos_nf viewrecord_gc_movimento_nf)
        {
            bool Sucesso = false;
            String MsgRetorno = String.Empty;
            int IdGateway = db.gc_parametros.Find(1).id_nfe_gateway;
            gc_movimentos record_gc_movimento = new Db.gc_movimentos();
            gc_cfop_operacoes record_gc_cfop_operacao = null;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            int QtdErros = 0;
            try
            {
                record_gc_cfop_operacao = db.gc_cfop_operacoes.Find(viewrecord_gc_movimento_nf.id_cfop_operacao);

                if (record_gc_cfop_operacao == null)
                {
                    MsgRetorno += " - Operação Não identificada" + "<br/>";
                    QtdErros += 1;
                }
                else
                {
                    if (record_gc_cfop_operacao.has_financeiro == true)
                    {
                        // Validações
                        int QtdLancamentosFinanceiros = LibDB.dbQueryCount("select count(*) from gc_financeiro_lancamentos where ativo = 1 and tipo_pag_rec = 2 and id_movimento = " + viewrecord_gc_movimento_nf.id_movimento.EmptyIfNull().ToString(), db);
                        if (QtdLancamentosFinanceiros == 0)
                        {
                            MsgRetorno += " - Para a Operação " + record_gc_cfop_operacao.descricao.EmptyIfNull().ToString() + " é obrigatório a geração dos Lançamentos Financeiros (Contas à Receber) antes da geração da Nota Fiscal!" + "<br/>";
                            QtdErros += 1;
                        }
                    }

                    if (record_gc_cfop_operacao.has_nfe_referenciada == true)
                    {
                        if (viewrecord_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Length > 0)
                        {
                            viewrecord_gc_movimento_nf.nf_chave_referenciada = LibStringFormat.RemoverEspacos(viewrecord_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Trim());
                        }

                        if (viewrecord_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Length == 0)
                        {
                            MsgRetorno += " - Para a Operação " + record_gc_cfop_operacao.descricao.EmptyIfNull().ToString() + " é obrigatório o preenchimento da Chave NFe Referência!" + "<br/>";
                            QtdErros += 1;
                        }
                        else if (viewrecord_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Length != 44)
                        {
                            MsgRetorno += " - Chave NFe Referência não possui 44 dígitos!" + "<br/>";
                            QtdErros += 1;
                        }
                        else
                        {
                            String ChaveAcessoInformada = viewrecord_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString();
                            gc_movimentos_nf record_gc_movimentos_nf = db.gc_movimentos_nf.Where(n => n.nf_chave_acesso == ChaveAcessoInformada).FirstOrDefault();
                            if (record_gc_movimentos_nf == null)
                            {
                                MsgRetorno += " - Chave NFe Referência não informada não foi localizada no ERP!" + "<br/>";
                                QtdErros += 1;
                            }
                        }
                    }

                    // Consistir Movimento
                    record_gc_movimento = db.gc_movimentos.Find(viewrecord_gc_movimento_nf.id_movimento);
                    if (record_gc_movimento == null)
                    {
                        MsgRetorno += " - Pedido não localizado!" + "<br/>";
                        QtdErros += 1;
                    }

                    // Consistir CFOP Dentro/Fora UF
                    int IdUfDestinatario = 0;
                    if (viewrecord_gc_movimento_nf.id_cliente_destinatario > 0) { IdUfDestinatario = db.g_clientes_destinatarios.Find(viewrecord_gc_movimento_nf.id_cliente_destinatario).id_uf_com.GetValueOrDefault(); }
                    else IdUfDestinatario = db.g_clientes.Find(record_gc_movimento.id_cliente).id_uf_com.GetValueOrDefault();
                    if (IdUfDestinatario == 0)
                    {
                        MsgRetorno += " - UF do Destinatário NÃO localizada!" + "<br/>";
                        QtdErros += 1;
                    }
                }
                if (QtdErros == 0)
                {
                    if (record_gc_movimento.id_movimento_tipo == 3) { record_gc_movimento.id_movimento_tipo = 4; } // Pedido
                    record_gc_movimento.datahora_alteracao = DataHoraAtual;
                    record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_movimento).State = EntityState.Modified;
                    db.SaveChanges();
                    if (IdGateway == 1) // ENotas
                    {
                        RoboEnotasNFE _RoboFaturarNFP = new RoboEnotasNFE();
                        bool okNf;
                        if (record_gc_cfop_operacao.is_servico == true)
                        {
                            okNf = _RoboFaturarNFP.GerarNFServicoByMovimentoNF(viewrecord_gc_movimento_nf);
                        }
                        else
                        {
                            okNf = _RoboFaturarNFP.GerarNFPVendaByMovimentoNF(viewrecord_gc_movimento_nf);
                        }
                        Sucesso = okNf;
                        if (Sucesso)
                        {
                            if (record_gc_cfop_operacao.is_servico == true)
                            {
                                MsgRetorno = "Nota Fiscal de Serviços — Pedido Nº [<b>" + viewrecord_gc_movimento_nf.id_movimento.EmptyIfNull().ToString() + "</b>] transmitida com sucesso!";
                            }
                            else
                            {
                                MsgRetorno = "Nota Fiscal Pedido Nº  [<b>" + viewrecord_gc_movimento_nf.id_movimento.EmptyIfNull().ToString() + "</b>] transmitida com Sucesso!";
                            }
                        }
                        else
                        {
                            MsgRetorno = "Falha na transmissão da nota fiscal. Verifique os logs da NF-e.";
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getWebException(ex);
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxPDFNotaFiscal(gc_movimentos_nf view_record_gc_movimento_nf)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            int qtdInconsistencias = 0;
            try
            {
                if (view_record_gc_movimento_nf == null)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else if (view_record_gc_movimento_nf.id_movimento_nf <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else
                {
                    gc_movimentos_nf record_gc_movimento_nf = db.gc_movimentos_nf.Find(view_record_gc_movimento_nf.id_movimento_nf);

                    if (record_gc_movimento_nf.nf_url_pdf.EmptyIfNull().ToString().Length > 10)
                    {
                        MsgRetorno = record_gc_movimento_nf.nf_url_pdf.EmptyIfNull().ToString();
                    }
                    else
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += " - NF não disponível!<br/>";
                    }
                }
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            if (qtdInconsistencias == 0) { Sucesso = true; } else { Sucesso = false; }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxPDFCartaCorrecao(g_nfe_carta_correcao view_record_g_nfe_carta_correcao)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            String chaveAcesso = "";
            int qtdInconsistencias = 0;
            try
            {
                g_nfe_carta_correcao record_g_nfe_carta_correcao = db.g_nfe_carta_correcao.Find(view_record_g_nfe_carta_correcao.id_nfe_carta_correcao);
                if (view_record_g_nfe_carta_correcao == null)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a Carta Correção!<br/>";
                }
                else if (view_record_g_nfe_carta_correcao.id_nfe_carta_correcao <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else
                {
                    gc_movimentos_nf record_gc_movimento_nf = db.gc_movimentos_nf.Find(record_g_nfe_carta_correcao.id_movimento_nf);
                    chaveAcesso = LibStringFormat.SomenteNumeros(record_gc_movimento_nf.nf_chave_acesso.EmptyIfNull().ToString());

                    // ENotas MG ou ENotas SP
                    if (record_gc_movimento_nf.id_nfe_gateway == 1) { MsgRetorno = "http://nfe.fazenda.mg.gov.br/portalnfe/sistema/consultaarg.xhtml";}
                    else if (record_gc_movimento_nf.id_nfe_gateway == 2) { MsgRetorno = "https://www.nfe.fazenda.sp.gov.br/ConsultaNFe/consulta/publica/ConsultarNFe.aspx"; };
                }
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            if (qtdInconsistencias == 0) { Sucesso = true; } else { Sucesso = false; }
            return Json(new { success = Sucesso, msg = MsgRetorno, chaveacesso = chaveAcesso }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxXMLCartaCorrecao(g_nfe_carta_correcao view_record_g_nfe_carta_correcao)
        {
            bool Sucesso = false;
            String MsgRetorno = string.Empty;
            String chaveAcesso = string.Empty;
            String ArquivoSaida = String.Empty;
            String fileNameXML = String.Empty;
            int qtdInconsistencias = 0;
            String idProcessamentoGravado = "0";
            try
            {
                g_nfe_carta_correcao record_g_nfe_carta_correcao = db.g_nfe_carta_correcao.Find(view_record_g_nfe_carta_correcao.id_nfe_carta_correcao);
                if (view_record_g_nfe_carta_correcao == null)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a Carta Correção!<br/>";
                }
                else if (view_record_g_nfe_carta_correcao.id_nfe_carta_correcao <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else
                {
                    RoboEnotasNFE _RoboFaturarNFP = new RoboEnotasNFE();
                    ArquivoSaida = _RoboFaturarNFP.GetXMLCartaCorrecao(record_g_nfe_carta_correcao);


                    if (ArquivoSaida.Trim().Length > 0)
                    {
                        XmlDocument XMLdoc = (XmlDocument)Newtonsoft.Json.JsonConvert.DeserializeXmlNode(ArquivoSaida, "XmlResult");
                        ArquivoSaida = XMLdoc.InnerXml.EmptyIfNull().ToString();

                        fileNameXML = "Carta_Correção_NF_" + db.gc_movimentos_nf.Find(record_g_nfe_carta_correcao.id_movimento_nf).nf_numero.EmptyIfNull().ToString() + ".xml";
                        String DirTempFiles = Server.MapPath("~/_filestemp");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "reports");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                        fileNameXML = Path.Combine(DirTempFiles, fileNameXML);
                        using (StreamWriter w = new StreamWriter(fileNameXML, true, Encoding.UTF8))
                        {
                            w.Write(ArquivoSaida); // Write the text)
                            w.Flush(); w.Close(); w.Dispose();
                        }
                        // Inserir o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 33; // Exportação Comissão Vendedor
                        record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                        record_g_processamento.datahora_inicio = LibDateTime.getDataHoraBrasilia();
                        record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                        record_g_processamento.qtd_registros = 1;
                        record_g_processamento.qtd_reg_ok = 1;
                        record_g_processamento.qtd_reg_erro = 0;
                        record_g_processamento.processando = false;
                        record_g_processamento.concluido = true;
                        record_g_processamento.pathfile = fileNameXML;
                        record_g_processamento.id_coligada = 0; // Global
                        record_g_processamento.id_filial = 0; // Global
                        db.g_processamento.Add(record_g_processamento);
                        db.SaveChanges();

                        Sucesso = true;
                        idProcessamentoGravado = record_g_processamento.id_processamento.EmptyIfNull().ToString().Trim();

                        MsgRetorno = "XML gerado com Sucesso " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" + "Obs: O Download será iniciado automaticamente na sequência";
                    }
                    else
                    {
                        Sucesso = false;
                        MsgRetorno = "Não foi possível gerar o arquivo XML!";
                    }
                }
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            if (qtdInconsistencias == 0) { Sucesso = true; } else { Sucesso = false; }
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxXMLNotaFiscal(gc_movimentos_nf view_record_gc_movimento_nf)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            int qtdInconsistencias = 0;
            try
            {
                if (view_record_gc_movimento_nf == null)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar o XML!<br/>";
                }
                else if (view_record_gc_movimento_nf.id_movimento_nf <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar o XML!<br/>";
                }
                else
                {
                    gc_movimentos_nf record_gc_movimento_nf = db.gc_movimentos_nf.Find(view_record_gc_movimento_nf.id_movimento_nf);

                    if (record_gc_movimento_nf.nf_url_xml.EmptyIfNull().ToString().Length > 10)
                    {
                        MsgRetorno = record_gc_movimento_nf.nf_url_xml.EmptyIfNull().ToString();
                    }
                    else
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += " - XML não disponível!<br/>";
                    }
                }
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            if (qtdInconsistencias == 0) { Sucesso = true; } else { Sucesso = false; }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxInfoNotaFiscal(gc_movimentos_nf view_record_gc_movimento_nf)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            int qtdInconsistencias = 0;
            try
            {
                if (view_record_gc_movimento_nf == null)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else if (view_record_gc_movimento_nf.id_movimento_nf <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else
                {
                    gc_movimentos_nf record_gc_movimento_nf = db.gc_movimentos_nf.Find(view_record_gc_movimento_nf.id_movimento_nf);
                    if (record_gc_movimento_nf != null)
                    {
                        var allLogs = db.g_nfe_logs.Select(l => new { l.id_nfe_log, l.id_movimento_nf, l.datahora_cadastro, l.retorno, l.log }).Where(l => (l.id_movimento_nf == record_gc_movimento_nf.id_movimento_nf && l.retorno == true)).ToList();
                        String StatusNF = string.Empty;
                        String UsuarioNF = string.Empty;
                        g_usuarios record_g_usuario = db.g_usuarios.Find(record_gc_movimento_nf.nf_id_usuario_geracao);
                        if ((record_g_usuario != null) && (record_g_usuario.nome.EmptyIfNull().ToString().Length > 0)) { UsuarioNF = record_g_usuario.nome.EmptyIfNull().ToString(); };

                        foreach (var item1 in allLogs)
                        {
                            StatusNF += item1.datahora_cadastro.ToString("dd/MM/yy HH:mm") + " - " + item1.log.EmptyIfNull().ToString() + "<br/>";
                        }
                        MsgRetorno = "<b>Informações da Nota Fiscal</b><br/><br/>";
                        MsgRetorno += "----- Log dos Status -----<br/>" + StatusNF + "<br/>";
                        MsgRetorno += "Gerada em: " + record_gc_movimento_nf.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy HH:mm") + "<br/>";
                        MsgRetorno += "Usuário: " + UsuarioNF + "<br/>";
                        MsgRetorno += "Chave: " + record_gc_movimento_nf.nf_chave_acesso.EmptyIfNull().ToString() + "<br/>";
                        MsgRetorno += "Identificador: " + record_gc_movimento_nf.nf_identificador.EmptyIfNull().ToString() + "<br/>";
                    }
                    else
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += " - NF não disponível!<br/>";
                    }
                }
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            if (qtdInconsistencias == 0) { Sucesso = true; } else { Sucesso = false; }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxCancelNotaFiscal(gc_movimentos_nf view_record_gc_movimento_nf)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            int qtdInconsistencias = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (view_record_gc_movimento_nf == null)
                {
                    Sucesso = false;
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                if (view_record_gc_movimento_nf.id_movimento_nf <= 0)
                {
                    Sucesso = false;
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                if (view_record_gc_movimento_nf.justificativa_cancelamento.EmptyIfNull().ToString().Trim().Length <= 0)
                {
                    Sucesso = false;
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Justificativa do Cancelamento não preenchida!<br/>";
                }
                if (view_record_gc_movimento_nf.protocolo_cancelamento_siare.EmptyIfNull().ToString().Trim().Length <= 10)
                {
                    if (view_record_gc_movimento_nf.protocolo_cancelamento_siare.EmptyIfNull().ToString().Trim() != "0")
                    {
                        Sucesso = false;
                        qtdInconsistencias += 1;
                        MsgRetorno += " - Protocolo Siare inválido!<br/>";
                    }
                }
                if (qtdInconsistencias == 0)
                {
                    gc_movimentos_nf record_gc_movimento_nf = db.gc_movimentos_nf.Find(view_record_gc_movimento_nf.id_movimento_nf);
                    TimeSpan TempoAutorizacao = DateTime.Now - record_gc_movimento_nf.nf_data_autorizacao.GetValueOrDefault();
                    Double DiasAutorizacao = TempoAutorizacao.TotalDays;

                    if (record_gc_movimento_nf != null)
                    {
                        if (db.g_nfe_status.Find(record_gc_movimento_nf.id_nfe_status).nf_autorizada == false)
                        {
                            Sucesso = false;
                            qtdInconsistencias += 1;
                            MsgRetorno += " - Somente notas fiscais AUTORIZADAS podem ser canceladas!<br/>";
                        }
                        if ((DiasAutorizacao >= 1) && (view_record_gc_movimento_nf.protocolo_cancelamento_siare.EmptyIfNull().ToString().Trim().Length <= 10))
                        {
                            Sucesso = false;
                            qtdInconsistencias += 1;
                            MsgRetorno += " - Protocolo Siare obrigatório para esse Cancelamento!<br/>";

                        }
                        if (qtdInconsistencias == 0)
                        {
                            record_gc_movimento_nf.justificativa_cancelamento = view_record_gc_movimento_nf.justificativa_cancelamento;
                            record_gc_movimento_nf.protocolo_cancelamento_siare = view_record_gc_movimento_nf.protocolo_cancelamento_siare;
                            record_gc_movimento_nf.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento_nf.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento_nf).State = EntityState.Modified;
                            db.SaveChanges();

                            if ((record_gc_movimento_nf.id_nfe_gateway == 1) || (record_gc_movimento_nf.id_nfe_gateway == 2)) // Enotas
                            {
                                RoboEnotasNFE _RoboFaturarNFP = new RoboEnotasNFE();
                                _RoboFaturarNFP.CancelarNFPbyMovimentoNF(record_gc_movimento_nf);
                                Sucesso = true;
                                MsgRetorno = "Cancelamento da Nota Fiscal Nº  <b>" + record_gc_movimento_nf.nf_identificador.EmptyIfNull().ToString() + "</b> solicitado!";
                            }
                        }
                    }
                    else
                    {
                        Sucesso = false;
                        qtdInconsistencias += 1;
                        MsgRetorno += " - NF não disponível!<br/>";
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                qtdInconsistencias = 1;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                qtdInconsistencias = 1;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Nfe - Carta Correção
        public ActionResult ModalViewCartaCorrecao(int? id)
        {
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            if (record_gc_movimento != null)
            {
                TitleModal = LibIcons.getIcon("fa-solid fa-envelope", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Cartas de Correção - Pedido Nº " + record_gc_movimento.id_movimento.ToString();
            }
            ViewBag.Title = TitleModal;
            return View("ModalViewCartaCorrecao", record_gc_movimento);
        }

        public ActionResult GetDadosCartaCorrecao(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {

            var allRecords = new List<Db.g_nfe_carta_correcao>();
            var allRecordsAtualizarCartaCorrecao = new List<Db.g_nfe_carta_correcao>();
            var allRecordsNfeStatus = db.g_nfe_status.Select(s => new { s.id_nfe_status, s.descricao_resumida }).ToList();
            var allRecordsCfop = db.gc_cfop.Select(c => new { c.id_cfop, c.numero }).ToList();
            String SentencaSQL = string.Empty;

            SentencaSQL = " select * from g_nfe_carta_correcao cr ";
            SentencaSQL += " left join gc_movimentos_nf nf on (nf.id_movimento_nf = cr.id_movimento_nf) ";

            if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1"))
            { SentencaSQL += " where nf.id_movimento = " + param.yesCustomField01.EmptyIfNull().ToString().Trim(); }

            SentencaSQL += " order by cr.id_nfe_carta_correcao desc";


            allRecordsAtualizarCartaCorrecao = db.g_nfe_carta_correcao.SqlQuery(SentencaSQL).ToList();
            foreach (var record_g_nfe_carta_correcao in allRecordsAtualizarCartaCorrecao)
            {
                if (record_g_nfe_carta_correcao.status.ToString() == "Enviada")
                {
                    // Atualizar Status
                    RoboEnotasNFE _RoboNotaMercantil = new RoboEnotasNFE();
                    _RoboNotaMercantil.AtualizarStatusCartaCorrecao(record_g_nfe_carta_correcao);
                }
            }

            allRecords = db.g_nfe_carta_correcao.SqlQuery(SentencaSQL).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_nfe_carta_correcao, string> orderingFunction = (c => param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_nfe_carta_correcao) : "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_nfe_carta_correcao); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_nfe_carta_correcao); }
                }
            }

            List<string[]> list = new List<string[]>();
            foreach (var nf in displayedRecords)
            {
                String Status = nf.status.EmptyIfNull().ToString();
                String Obs = nf.correcao.EmptyIfNull().ToString();
                if ((Status != "Enviada") && (Status != "Autorizada")) { Obs = nf.motivo_status.EmptyIfNull().ToString(); }
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    nf.id_nfe_carta_correcao.EmptyIfNull().ToString(),
                                    nf.identificador.EmptyIfNull().ToString(),
                                    Status,
                                    nf.datahora_cadastro.ToString("dd/MM/yy"),
                                    Obs,
                                    "", // Botão PDF
                                    "" // Botão XML
                                });
            }
            return Json(new
            {
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

        public ActionResult ModalPedidoCartaCorrecao(int? id)
        {
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            TitleModal = LibIcons.getIcon("fa-solid fa-eraser", "", "#008000", "fa-sm") + LibStringFormat.GetEspacesHtml(3) + "Carta de Correção";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            cstNfeCartaCorrecao RecordCstNfeCartaCorrecao = new cstNfeCartaCorrecao();
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            if (record_gc_movimento != null)
            {
                TitleModal = LibIcons.getIcon("fa-solid fa-eraser", "", "#008000", "fa-sm") + LibStringFormat.GetEspacesHtml(3) + "Carta de Correção do Pedido Nº " + record_gc_movimento.id_movimento.ToString();
                string SqlConsulta = "select * from gc_movimentos_nf m left join g_nfe_status s on(s.id_nfe_status = m.id_nfe_status) where m.id_movimento = " + record_gc_movimento.id_movimento.ToString() + " and s.nf_autorizada = 1";
                List<gc_movimentos_nf> ListaGNFE = db.gc_movimentos_nf.SqlQuery(SqlConsulta).ToList();
                if (ListaGNFE.Count > 0)
                {
                    RecordCstNfeCartaCorrecao.CartaCorrecaoLiberada = true;
                    var comboNfeCartaCorrecao = new List<SelectListItem>();
                    try
                    {
                        foreach (gc_movimentos_nf MovimentoNF in ListaGNFE)
                        {
                            string CFOP = string.Empty;
                            CFOP = db.gc_cfop.Find(MovimentoNF.id_cfop).numero.EmptyIfNull().ToString();
                            comboNfeCartaCorrecao.Add(new SelectListItem { Value = MovimentoNF.id_movimento_nf.EmptyIfNull().ToString(), Text = "Pedido: " + MovimentoNF.id_movimento.EmptyIfNull().ToString() + " - CFOP: " + CFOP });
                        }
                    }
                    finally { }
                    ViewBag.comboNfeCartaCorrecao = comboNfeCartaCorrecao;
                }
                else
                {
                    RecordCstNfeCartaCorrecao.CartaCorrecaoLiberada = false;
                    RecordCstNfeCartaCorrecao.msg = "Não há notas fiscais Autorizadas para esse movimento";
                }
            }
            else
            {
                RecordCstNfeCartaCorrecao.CartaCorrecaoLiberada = false;
                RecordCstNfeCartaCorrecao.msg = "Não há notas fiscais Autorizadas para esse movimento";
            }
            ViewBag.Title = TitleModal;
            return View("ModalPedidoCartaCorrecao", RecordCstNfeCartaCorrecao);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoCartaCorrecao(cstNfeCartaCorrecao view_record_cstNfeCartaCorrecao)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String MsgRetorno = "";
            try
            {
                if (view_record_cstNfeCartaCorrecao.correcao.EmptyIfNull().ToString().Length == 0)
                {
                    Sucesso = false;
                    qtdInconsistencias += 1;
                    MsgRetorno += "Dados da correção NÃO informados!" + "<br/>";
                }
                if ((qtdInconsistencias == 0) && (ModelState.IsValid == false))
                {
                    Sucesso = false;
                    qtdInconsistencias += 1;
                    MsgRetorno += String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }
                if (qtdInconsistencias == 0)
                {
                    int IdGatewayNfe = db.gc_movimentos_nf.Find(view_record_cstNfeCartaCorrecao.id_movimento_nf).id_nfe_gateway;
                    g_nfe_carta_correcao record_g_nfe_carta_correcao = new g_nfe_carta_correcao();
                    record_g_nfe_carta_correcao.id_movimento_nf = view_record_cstNfeCartaCorrecao.id_movimento_nf;
                    record_g_nfe_carta_correcao.correcao = LibStringFormat.SomenteAlfabetoSefaz(view_record_cstNfeCartaCorrecao.correcao);
                    if (IdGatewayNfe == 1 || IdGatewayNfe == 2) // Enotas BH ou Enotas SP
                    {
                        RoboEnotasNFE _RoboNotaMercantil = new RoboEnotasNFE();
                        _RoboNotaMercantil.GerarCartaCorrecaoNFPbyMovimentoNF(record_g_nfe_carta_correcao);
                        Sucesso = true;
                        MsgRetorno = "Carta Correção transmitida com Sucesso!";
                    }
                }
            }
            catch (WebException ex)
            {
                Sucesso = false;
                qtdInconsistencias = 1;
                MsgRetorno = LibExceptions.getWebException(ex);
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                qtdInconsistencias = 1;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception ex)
            {
                Sucesso = false;
                qtdInconsistencias = 1;
                MsgRetorno = LibExceptions.getExceptionShortMessage(ex);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Expedição (Modal)
        public ActionResult ModalPedidoExpedicao(int? id)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            String MsgBloqueio = string.Empty;
            String MsgHistorico = string.Empty;
            String ParamMovimentoExpedido = string.Empty;
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(id);
            List<gc_movimentos_itens> ListaItensPedido = db.gc_movimentos_itens.Where(i => i.id_movimento == RecordMovimento.id_movimento).ToList();

            if (RecordMovimento != null)
            {
                if (RecordMovimento.movimento_expedido == true) 
                {
                    ParamMovimentoExpedido = "1";
                    TitleModal = LibIcons.getIcon("fa-solid fa-truck", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Pedido Nº " + RecordMovimento.id_movimento.ToString() + " já Expedido!";
                }
                else 
                {
                    ParamMovimentoExpedido = "0";
                    TitleModal = LibIcons.getIcon("fa-solid fa-truck", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Expedição do Pedido Nº " + RecordMovimento.id_movimento.ToString(); 
                }

                if (RecordMovimento.obs.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Pedido: " + RecordMovimento.obs.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.frete_observacoes.EmptyIfNull().ToString().Length > 0) { MsgHistorico = "OBS Frete: " + RecordMovimento.frete_observacoes.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.obs_aprovacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Aprovação: " + RecordMovimento.obs_aprovacao.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.obs_separacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Separação: " + RecordMovimento.obs_separacao.EmptyIfNull().ToString() + "\r\n"; };

                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);
                if (RecordCfopOperacao.has_separacao == false)
                {
                    MsgBloqueio += "<b> ---------- O TIPO DE PEDIDO [" + RecordCfopOperacao.descricao.EmptyIfNull().ToString().Trim() + "] NÃO POSSUI ATIVIDADE DE EXPEDIÇÃO ----------</b>" + "<br/>";
                }
                else
                {
                    if ((RecordCfopOperacao.has_aprovacao == true) && (RecordMovimento.movimento_aprovado == false)) { MsgBloqueio += " - Pedido não foi APROVADO!<br/>"; }
                    if ((RecordCfopOperacao.has_separacao == true) && (RecordMovimento.movimento_separado == false)) { MsgBloqueio += " - Pedido não foi SEPARADO!<br/>"; }
                    if ((RecordCfopOperacao.has_financeiro == true) && (RecordMovimento.movimento_faturado == false)) { MsgBloqueio += " - Pedido não foi FATURADO!<br/>"; }
                    if ((RecordCfopOperacao.has_nfe == true) && (RecordMovimento.movimento_nf_autorizada == false)) { MsgBloqueio += " - Pedido não possui NFe Autorizada!<br/>"; }
                    if ((RecordCfopOperacao.has_notifica_email == true) && (RecordMovimento.movimento_notificado == false)) { MsgBloqueio += " - Pedido não foi NOTIFICADO!<br/>"; }
                    if ((RecordCfopOperacao.has_expedicao == true) && (RecordMovimento.movimento_expedido == true)) { MsgBloqueio += " - Pedido já foi EXPEDIDO!<br/>"; }
                    if ((RecordCfopOperacao.has_entrega == true) && (RecordMovimento.movimento_entregue == true)) { MsgBloqueio += " - Pedido já foi ENTREGUE!<br/>"; }
                }

                if (MsgBloqueio.EmptyIfNull().ToString().Trim().Length == 0) 
                {
                    RecordMovimento.datahora_expedicao = DataHoraAtual;
                    RecordMovimento.datahora_entrega_previsao = DataHoraAtual.AddDays(5);
                }
            }
            else
            {
                MsgBloqueio = " - Pedido Nº " + id.ToString() + " não localizado no ERP";
            }

            if (!CachePersister.userIdentity.Roles.Contains("gc_Movimentos_IndexPedido_*"))
            {
                if (RecordMovimento.id_filial != CachePersister.userIdentity.id_filial)
                {
                    MsgBloqueio += "<b> ---------- O PEDIDO NÃO PERTENCE AO LOCAL DE ESTOQUE DO USUÁRIO ----------</b>" + "<br/>";
                }
            }

            ViewBag.Title = TitleModal;
            ViewBag.MsgBloqueio = MsgBloqueio;
            ViewBag.MsgHistorico = MsgHistorico;
            ViewBag.ParamMovimentoExpedido = ParamMovimentoExpedido;
            ViewBag.comboTransportadora = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar.Insert(0, new SelectListItem { Value = "-1", Text = "[ SEM FRETE COMPLEMENTAR ]" });
            if (ListaItensPedido.Count() <= 1) { ViewBag.QtdItensPedido = ListaItensPedido.Count() + " Item no Pedido"; } else { ViewBag.QtdItensPedido = ListaItensPedido.Count() + " Itens no Pedido"; }
            return View("ModalPedidoExpedicao", RecordMovimento);
        }


        [HttpPost]
        public ActionResult AjaxModalPedidoExpedicao(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String MsgRetorno = "";
            String LogFrete = String.Empty;
            String NfeAutorizadaNumero = String.Empty;
            String NfeAutorizadaSerie = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);

                if (ModelState.IsValid == false)
                {
                    MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }

                if (qtdInconsistencias == 0)
                {
                    if ((record_gc_movimento.id_movimento_posicao != 4) && (record_gc_movimento.id_movimento_posicao != 5))
                    {
                        String PosicaoAtual = string.Empty;
                        if (record_gc_movimento.id_movimento_posicao == 0) { PosicaoAtual = "Aberto"; } else { PosicaoAtual = db.gc_movimentos_posicao.Find(record_gc_movimento.id_movimento_posicao).posicao.EmptyIfNull().ToString(); };
                        MsgRetorno += "O Pedido está na posição [" + PosicaoAtual + "], não é possível Confirmar/Reprovar a Expedição!" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                }

                if (qtdInconsistencias == 0)
                {
                    if (view_record_gc_movimento.movimento_expedido == true)
                    {
                        if (qtdInconsistencias == 0)
                        {
                            record_gc_movimento.movimento_expedido = true;
                            record_gc_movimento.id_movimento_status = 2; // Fechado
                            record_gc_movimento.datahora_expedicao = view_record_gc_movimento.datahora_expedicao;
                            record_gc_movimento.datahora_entrega_previsao = view_record_gc_movimento.datahora_entrega_previsao;
                            record_gc_movimento.obs_expedicao = view_record_gc_movimento.obs_expedicao;
                            record_gc_movimento.id_usuario_expedicao = CachePersister.userIdentity.IdUsuario;

                            // Frete
                            if (record_gc_movimento.frete1_transportadora != view_record_gc_movimento.frete1_transportadora) { LogFrete += "Transportadora(1): " + record_gc_movimento.frete1_transportadora.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete1_transportadora.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.frete1_documento != view_record_gc_movimento.frete1_documento) { LogFrete += "Documento(1): " + record_gc_movimento.frete1_documento.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete1_documento.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.frete1_rastreio != view_record_gc_movimento.frete1_rastreio) { LogFrete += "Rastreio(1): " + record_gc_movimento.frete1_rastreio.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete1_rastreio.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.frete1_custo != view_record_gc_movimento.frete1_custo) { LogFrete += "Custo(1): " + record_gc_movimento.frete1_custo.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete1_custo.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.frete2_transportadora != view_record_gc_movimento.frete2_transportadora) { LogFrete += "Transportadora(2): " + record_gc_movimento.frete2_transportadora.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete2_transportadora.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.frete2_documento != view_record_gc_movimento.frete2_documento) { LogFrete += "Documento(2): " + record_gc_movimento.frete2_documento.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete2_documento.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.frete2_rastreio != view_record_gc_movimento.frete2_rastreio) { LogFrete += "Rastreio(2): " + record_gc_movimento.frete2_rastreio.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete2_rastreio.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento.frete2_custo != view_record_gc_movimento.frete2_custo) { LogFrete += "Custo(2): " + record_gc_movimento.frete2_custo.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete2_custo.EmptyIfNull().ToString() + " | "; };

                            record_gc_movimento.frete1_transportadora = view_record_gc_movimento.frete1_transportadora;
                            record_gc_movimento.frete1_documento = view_record_gc_movimento.frete1_documento;
                            record_gc_movimento.frete1_rastreio = view_record_gc_movimento.frete1_rastreio;
                            record_gc_movimento.frete1_custo = view_record_gc_movimento.frete1_custo;
                            record_gc_movimento.frete2_transportadora = view_record_gc_movimento.frete2_transportadora;
                            record_gc_movimento.frete2_documento = view_record_gc_movimento.frete2_documento;
                            record_gc_movimento.frete2_rastreio = view_record_gc_movimento.frete2_rastreio;
                            record_gc_movimento.frete2_custo = view_record_gc_movimento.frete2_custo;

                            if (record_gc_movimento.id_movimento_posicao < 5) { record_gc_movimento.id_movimento_posicao = 5; }  // Expedido
                            MsgRetorno += "Status do pedido " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>EXPEDIDO</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                            record_gc_movimento.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento).State = EntityState.Modified;
                            Sucesso = true;


                            if ((record_gc_movimento.id_movimento_tipo == 19) && (record_gc_movimento.movimento_transferido_filial == false))
                            {
                                #region Criar Movimento de Recebimento da Transferência
                                gc_movimentos RecordMovimentoRecebimentoTransferencia = LibDB.CloneTObject(record_gc_movimento);
                                RecordMovimentoRecebimentoTransferencia.id_movimento = 0;
                                RecordMovimentoRecebimentoTransferencia.id_movimento_tipo = 18; // Entrada - Transferência entre Filiais
                                RecordMovimentoRecebimentoTransferencia.id_movimento_ref = record_gc_movimento.id_movimento;

                                RecordMovimentoRecebimentoTransferencia.id_coligada = 1; // GDI BH

                                if (record_gc_movimento.id_local_estoque == 1)
                                {
                                    RecordMovimentoRecebimentoTransferencia.id_local_estoque = 3; // SP
                                    RecordMovimentoRecebimentoTransferencia.id_estoque_cd = 3; // SP
                                    RecordMovimentoRecebimentoTransferencia.id_filial = 2; // SP
                                }
                                else if (record_gc_movimento.id_local_estoque == 3)
                                {
                                    RecordMovimentoRecebimentoTransferencia.id_local_estoque = 1;
                                    RecordMovimentoRecebimentoTransferencia.id_estoque_cd = 1;
                                    RecordMovimentoRecebimentoTransferencia.id_filial = 1;
                                }
                                RecordMovimentoRecebimentoTransferencia.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                RecordMovimentoRecebimentoTransferencia.datahora_cadastro = DataHoraAtual;
                                db.gc_movimentos.Add(RecordMovimentoRecebimentoTransferencia);
                                db.SaveChanges();

                                List<gc_movimentos_itens> ListaItensMovimentoTransferencia = db.gc_movimentos_itens.Where(i => i.id_movimento == record_gc_movimento.id_movimento).ToList();
                                foreach (var RecordItemOrigem in ListaItensMovimentoTransferencia)
                                {
                                    gc_movimentos_itens RecordItemTransferencia = LibDB.CloneTObject(RecordItemOrigem);
                                    RecordItemTransferencia.id_movimento_item = 0;
                                    RecordItemTransferencia.id_movimento_ref = 0;
                                    RecordItemTransferencia.id_movimento = RecordMovimentoRecebimentoTransferencia.id_movimento;
                                    RecordItemTransferencia.id_coligada = RecordMovimentoRecebimentoTransferencia.id_coligada; // GDI BH
                                    RecordItemTransferencia.id_filial = RecordMovimentoRecebimentoTransferencia.id_filial; // SP
                                    RecordItemTransferencia.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                    RecordItemTransferencia.datahora_cadastro = DataHoraAtual;
                                    RecordItemTransferencia.id_usuario_alteracao = 0;
                                    RecordItemTransferencia.datahora_alteracao = null;
                                    db.gc_movimentos_itens.Add(RecordItemTransferencia);
                                }
                                db.SaveChanges();

                                // Copiar as notas Fiscais
                                List<gc_movimentos_nf> ListaNotasFiscais = db.gc_movimentos_nf.Where(n => n.id_movimento == record_gc_movimento.id_movimento).ToList();
                                foreach (gc_movimentos_nf RecordNotaFiscal in ListaNotasFiscais)
                                {
                                    gc_movimentos_nf RecordCopiaNotaFiscal = LibDB.CloneTObject(RecordNotaFiscal);
                                    RecordCopiaNotaFiscal.id_movimento = RecordMovimentoRecebimentoTransferencia.id_movimento;
                                    if (RecordCopiaNotaFiscal.id_nfe_status == 8) { NfeAutorizadaNumero = RecordCopiaNotaFiscal.nf_numero; NfeAutorizadaSerie = RecordCopiaNotaFiscal.nf_serie; };
                                    db.gc_movimentos_nf.Add(RecordCopiaNotaFiscal);
                                }
                                RecordMovimentoRecebimentoTransferencia.nf_numero = NfeAutorizadaNumero;
                                RecordMovimentoRecebimentoTransferencia.nf_serie = NfeAutorizadaSerie;
                                db.Entry(RecordMovimentoRecebimentoTransferencia).State = EntityState.Modified;

                                record_gc_movimento.movimento_transferido_filial = true;
                                record_gc_movimento.id_movimento_transferencia = RecordMovimentoRecebimentoTransferencia.id_movimento;
                                record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                record_gc_movimento.datahora_alteracao = DataHoraAtual;
                                db.Entry(record_gc_movimento).State = EntityState.Modified;
                                db.SaveChanges();

                                #endregion
                            }
                        }
                    }
                    else if (view_record_gc_movimento.movimento_expedido == false)
                    {
                        if (record_gc_movimento.id_movimento_posicao == 5)
                        {
                            record_gc_movimento.id_movimento_posicao = 4;
                            record_gc_movimento.movimento_expedido = false;
                            record_gc_movimento.datahora_expedicao = null;
                            record_gc_movimento.datahora_entrega_previsao = null;
                            record_gc_movimento.frete1_rastreio = null;
                            record_gc_movimento.obs_expedicao = null;
                            record_gc_movimento.id_usuario_expedicao = 0;
                            MsgRetorno += "Status do pedido " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>NÃO EXPEDIDO</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "");
                            record_gc_movimento.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento).State = EntityState.Modified;
                            Sucesso = true;
                        }
                        else
                        {
                            String PosicaoAtual = string.Empty;
                            if (record_gc_movimento.id_movimento_posicao == 0) { PosicaoAtual = "Aberto"; } else { PosicaoAtual = db.gc_movimentos_posicao.Find(record_gc_movimento.id_movimento_posicao).posicao.EmptyIfNull().ToString(); };
                            MsgRetorno += "Não é possível excluir os dados da EXPEDIÇÃO do Pedido Nº " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + ", o pedido está [<b>" + PosicaoAtual + "</b>]!";
                        }
                    }

                    if (qtdInconsistencias == 0)
                    {
                        // Log de Alterações
                        String LogAlteracoes = string.Empty;
                        if (view_record_gc_movimento.movimento_expedido == true) { LogAlteracoes += "Expedição do Pedido | "; } else { LogAlteracoes += "Cancelamento da Expedição do Pedido | "; };
                        if (record_gc_movimento.datahora_expedicao != null) { LogAlteracoes += "Data Expedição: " + record_gc_movimento.datahora_expedicao.GetValueOrDefault().ToString("dd/MM/yy") + " | "; };
                        if (record_gc_movimento.datahora_entrega_previsao != null) { LogAlteracoes += "Data Previsão Entrega: " + record_gc_movimento.datahora_entrega_previsao.GetValueOrDefault().ToString("dd/MM/yy") + " | "; };
                        if (record_gc_movimento.frete1_rastreio.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Código Rastreio: " + record_gc_movimento.frete1_rastreio.EmptyIfNull().ToString().Trim() + " | "; };
                        if (record_gc_movimento.obs_expedicao.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Obs Expedição: " + record_gc_movimento.obs_expedicao.EmptyIfNull().ToString().Trim() + " | "; };
                        if (LogFrete.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Dados do Frete: " + LogFrete; };
                        if (Sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_gc_movimento.id_movimento, LogAlteracoes); }; };
                        db.SaveChanges();
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno += LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno += LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Entrega (Modal)
        public ActionResult ModalPedidoEntrega(int? id)
        {
            int temp = id.GetValueOrDefault();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String TitleModal = string.Empty;
            String MsgBloqueio = string.Empty;
            String MsgHistorico = string.Empty;
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(id);
            List<gc_movimentos_itens> ListaItensPedido = db.gc_movimentos_itens.Where(i => i.id_movimento == RecordMovimento.id_movimento).ToList();
            if (RecordMovimento != null)
            {
                if (RecordMovimento.movimento_entregue == true) { TitleModal = LibIcons.getIcon("fa-solid fa-people-carry", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Pedido Nº " + RecordMovimento.id_movimento.ToString() + " já Entregue!"; }
                else { TitleModal = LibIcons.getIcon("fa-solid fa-people-carry", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Entrega do Pedido Nº " + RecordMovimento.id_movimento.ToString(); }

                if (RecordMovimento.obs.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Pedido: " + RecordMovimento.obs.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.frete_observacoes.EmptyIfNull().ToString().Length > 0) { MsgHistorico = "OBS Frete: " + RecordMovimento.frete_observacoes.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.obs_aprovacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Aprovação: " + RecordMovimento.obs_aprovacao.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.obs_separacao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Separação: " + RecordMovimento.obs_separacao.EmptyIfNull().ToString() + "\r\n"; };
                if (RecordMovimento.obs_expedicao.EmptyIfNull().ToString().Length > 0) { MsgHistorico += "OBS Expedição: " + RecordMovimento.obs_expedicao.EmptyIfNull().ToString() + "\r\n"; };

                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);
                if (RecordCfopOperacao.has_entrega == false)
                {
                    MsgBloqueio += "<b> ---------- O TIPO DE PEDIDO [" + RecordCfopOperacao.descricao.EmptyIfNull().ToString().Trim() + "] NÃO POSSUI ATIVIDADE DE ENTREGA ----------</b>" + "<br/>";
                }
                else
                {
                    if ((RecordCfopOperacao.has_aprovacao == true) && (RecordMovimento.movimento_aprovado == false)) { MsgBloqueio += " - Pedido NÃO foi APROVADO!<br/>"; }
                    if ((RecordCfopOperacao.has_separacao == true) && (RecordMovimento.movimento_separado == false)) { MsgBloqueio += " - Pedido NÃO foi SEPARADO!<br/>"; }
                    if ((RecordCfopOperacao.has_financeiro == true) && (RecordMovimento.movimento_faturado == false)) { MsgBloqueio += " - Pedido NÃO foi FATURADO!<br/>"; }
                    if ((RecordCfopOperacao.has_nfe == true) && (RecordMovimento.movimento_nf_autorizada == false)) { MsgBloqueio += " - Pedido NÃO possui NFe Autorizada!<br/>"; }
                    if ((RecordCfopOperacao.has_notifica_email == true) && (RecordMovimento.movimento_notificado == false)) { MsgBloqueio += " - Pedido NÃO foi NOTIFICADO!<br/>"; }
                    if ((RecordCfopOperacao.has_expedicao == true) && (RecordMovimento.movimento_expedido == false) ) { MsgBloqueio += " - Pedido NÃO foi EXPEDIDO!<br/>"; }
                    //if ((RecordCfopOperacao.has_entrega == true) && (RecordMovimento.movimento_entregue == false)) { MsgBloqueio += " - Pedido NÃO foi ENTREGUE!<br/>"; }
                }

                if (MsgBloqueio.EmptyIfNull().ToString().Length == 0) { RecordMovimento.datahora_entrega = DataHoraAtual; };
            }
            else
            {
                MsgBloqueio = " - Pedido Nº " + id.ToString() + " não localizado no ERP";
            }
            ViewBag.comboTransportadora = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar = LibDataSets.LoadComboGcTransportadora(db);
            ViewBag.comboTransportadoraComplementar.Insert(0, new SelectListItem { Value = "-1", Text = "[ SEM FRETE COMPLEMENTAR ]" });
            ViewBag.Title = TitleModal;
            ViewBag.MsgBloqueio = MsgBloqueio;
            ViewBag.MsgHistorico = MsgHistorico;
            if (ListaItensPedido.Count() <= 1) { ViewBag.QtdItensPedido = ListaItensPedido.Count() + " Item no Pedido"; } else { ViewBag.QtdItensPedido = ListaItensPedido.Count() + " Itens no Pedido"; }
            return View("ModalPedidoEntrega", RecordMovimento);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoEntrega(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String MsgRetorno = String.Empty;
            String LogFrete = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);

                if (ModelState.IsValid == false)
                {
                    MsgRetorno += String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }

                if (qtdInconsistencias == 0)
                {
                    if ((record_gc_movimento.id_movimento_posicao != 5) && (record_gc_movimento.id_movimento_posicao != 6))
                    {
                        String PosicaoAtual = string.Empty;
                        if (record_gc_movimento.id_movimento_posicao == 0) { PosicaoAtual = "Aberto"; } else { PosicaoAtual = db.gc_movimentos_posicao.Find(record_gc_movimento.id_movimento_posicao).posicao.EmptyIfNull().ToString(); };
                        MsgRetorno += "O Pedido está na posição [" + PosicaoAtual + "], não é possível Confirmar/Reprovar a Expedição!" + "<br/>";
                        qtdInconsistencias += 1;
                    }
                }

                if (qtdInconsistencias == 0)
                {

                    if (view_record_gc_movimento.movimento_entregue == true)
                    {
                        record_gc_movimento.movimento_entregue = true;
                        record_gc_movimento.datahora_entrega = view_record_gc_movimento.datahora_entrega;
                        record_gc_movimento.nome_recebedor_entrega = view_record_gc_movimento.nome_recebedor_entrega;
                        record_gc_movimento.documento_recebedor_entrega = view_record_gc_movimento.documento_recebedor_entrega;
                        record_gc_movimento.obs_entrega = view_record_gc_movimento.obs_entrega;
                        record_gc_movimento.id_movimento_status = 2; // Fechado
                        record_gc_movimento.id_usuario_entrega = CachePersister.userIdentity.IdUsuario;

                        // Frete
                        if (record_gc_movimento.frete1_transportadora != view_record_gc_movimento.frete1_transportadora) { LogFrete += "Transportadora(1): " + record_gc_movimento.frete1_transportadora.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete1_transportadora.EmptyIfNull().ToString() + " | "; };
                        if (record_gc_movimento.frete1_documento != view_record_gc_movimento.frete1_documento) { LogFrete += "Documento(1): " + record_gc_movimento.frete1_documento.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete1_documento.EmptyIfNull().ToString() + " | "; };
                        if (record_gc_movimento.frete1_rastreio != view_record_gc_movimento.frete1_rastreio) { LogFrete += "Rastreio(1): " + record_gc_movimento.frete1_rastreio.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete1_rastreio.EmptyIfNull().ToString() + " | "; };
                        if (record_gc_movimento.frete1_custo != view_record_gc_movimento.frete1_custo) { LogFrete += "Custo(1): " + record_gc_movimento.frete1_custo.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete1_custo.EmptyIfNull().ToString() + " | "; };
                        if (record_gc_movimento.frete2_transportadora != view_record_gc_movimento.frete2_transportadora) { LogFrete += "Transportadora(2): " + record_gc_movimento.frete2_transportadora.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete2_transportadora.EmptyIfNull().ToString() + " | "; };
                        if (record_gc_movimento.frete2_documento != view_record_gc_movimento.frete2_documento) { LogFrete += "Documento(2): " + record_gc_movimento.frete2_documento.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete2_documento.EmptyIfNull().ToString() + " | "; };
                        if (record_gc_movimento.frete2_rastreio != view_record_gc_movimento.frete2_rastreio) { LogFrete += "Rastreio(2): " + record_gc_movimento.frete2_rastreio.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete2_rastreio.EmptyIfNull().ToString() + " | "; };
                        if (record_gc_movimento.frete2_custo != view_record_gc_movimento.frete2_custo) { LogFrete += "Custo(2): " + record_gc_movimento.frete2_custo.EmptyIfNull().ToString() + " > " + view_record_gc_movimento.frete2_custo.EmptyIfNull().ToString() + " | "; };

                        record_gc_movimento.frete1_transportadora = view_record_gc_movimento.frete1_transportadora;
                        record_gc_movimento.frete1_documento = view_record_gc_movimento.frete1_documento;
                        record_gc_movimento.frete1_rastreio = view_record_gc_movimento.frete1_rastreio;
                        record_gc_movimento.frete1_custo = view_record_gc_movimento.frete1_custo;
                        record_gc_movimento.frete2_transportadora = view_record_gc_movimento.frete2_transportadora;
                        record_gc_movimento.frete2_documento = view_record_gc_movimento.frete2_documento;
                        record_gc_movimento.frete2_rastreio = view_record_gc_movimento.frete2_rastreio;
                        record_gc_movimento.frete2_custo = view_record_gc_movimento.frete2_custo;

                        if (record_gc_movimento.id_movimento_posicao < 6) { record_gc_movimento.id_movimento_posicao = 6; }  // Entregue
                        MsgRetorno += "Status do pedido " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>ENTREGUE</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                        record_gc_movimento.datahora_alteracao = DataHoraAtual;
                        record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_movimento).State = EntityState.Modified;
                        Sucesso = true;
                    }
                    else if (view_record_gc_movimento.movimento_entregue == false)
                    {
                        if (record_gc_movimento.id_movimento_posicao == 6)
                        {
                            record_gc_movimento.id_movimento_posicao = 5;
                            record_gc_movimento.movimento_entregue = false;
                            record_gc_movimento.datahora_entrega = null;
                            record_gc_movimento.nome_recebedor_entrega = null;
                            record_gc_movimento.documento_recebedor_entrega = null;
                            record_gc_movimento.obs_entrega = null;
                            record_gc_movimento.id_movimento_status = 2; // Fechado
                            record_gc_movimento.id_usuario_entrega = 0;
                            MsgRetorno += "Status do pedido " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + LibStringFormat.GetTabHtml(1) + "<b>NÃO ENTREGUE</b> " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "");
                            record_gc_movimento.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento).State = EntityState.Modified;
                            Sucesso = true;
                        }
                        else
                        {
                            String PosicaoAtual = string.Empty;
                            if (record_gc_movimento.id_movimento_posicao == 0) { PosicaoAtual = "Aberto"; } else { PosicaoAtual = db.gc_movimentos_posicao.Find(record_gc_movimento.id_movimento_posicao).posicao.EmptyIfNull().ToString(); };
                            MsgRetorno += "Não é possível excluir os dados da ENTREGA do Pedido Nº " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + ", o pedido está [<b>" + PosicaoAtual + "</b>]!";
                        }
                    }

                    // Log de Alterações
                    String LogAlteracoes = string.Empty;
                    if (view_record_gc_movimento.movimento_entregue == true) { LogAlteracoes += "Entrega do Pedido | "; } else { LogAlteracoes += "Cancelamento da Entrega do Pedido | "; };
                    if (record_gc_movimento.datahora_entrega != null) { LogAlteracoes += "Data Entrega: " + record_gc_movimento.datahora_entrega.GetValueOrDefault().ToString("dd/MM/yy") + " | "; };
                    if (record_gc_movimento.datahora_entrega_previsao != null) { LogAlteracoes += "Data Previsão Entrega: " + record_gc_movimento.datahora_entrega_previsao.GetValueOrDefault().ToString("dd/MM/yy") + " | "; };
                    if (record_gc_movimento.nome_recebedor_entrega.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Recebedor (Nome): " + record_gc_movimento.nome_recebedor_entrega.EmptyIfNull().ToString().Trim() + " | "; };
                    if (record_gc_movimento.documento_recebedor_entrega.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Recebedor (Documento): " + record_gc_movimento.documento_recebedor_entrega.EmptyIfNull().ToString().Trim() + " | "; };
                    if (record_gc_movimento.obs_entrega.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Obs Entrega: " + record_gc_movimento.obs_entrega.EmptyIfNull().ToString().Trim() + " | "; };
                    if (LogFrete.EmptyIfNull().Trim().Length > 0) { LogAlteracoes += "Dados do Frete: " + LogFrete; };

                    if (Sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_gc_movimento.id_movimento, LogAlteracoes); }; };
                    db.SaveChanges();
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno += LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno += LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Painel de Pedidos
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Movimentos_*,gc_Movimentos_PainelPedidos")]
        public ActionResult PainelPedidos()
        {
            CachePersister.userIdentity.FormNameActive = "GcMovimentosPainelPedidos";
            ViewBag.comboClientes = LibDataSets.LoadComboSomenteGClientes(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]" });
            ViewBag.comboMovimentosPosicao = LibDataSets.LoadComboGcMovimentosPosicao(db); //
            List<SelectListItem> ListaLocaisEstoque = LibDataSets.LoadComboGcLocaisEstoqueOrders(db);
            ListaLocaisEstoque.RemoveAt(0);
            ListaLocaisEstoque.Add(new SelectListItem { Value = "-1", Text = "[ TODOS ]" });
            ViewBag.comboLocaisEstoque = ListaLocaisEstoque;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-desktop", "", "#008000", "fa-lg") + "&nbsp;Painel de Pedidos";
            gc_movimentos RecordGcMovimentos = new gc_movimentos();
            RecordGcMovimentos.id_cliente = -1;
            RecordGcMovimentos.id_movimento_posicao = -1;
            RecordGcMovimentos.id_local_estoque = -1;
            if (CachePersister.userIdentity.GcParamLocalEstoquePedidos.EmptyIfNull().ToString() == "1") { RecordGcMovimentos.id_local_estoque = 1; }
            else if (CachePersister.userIdentity.GcParamLocalEstoquePedidos.EmptyIfNull().ToString() == "3") { RecordGcMovimentos.id_local_estoque = 3; }
            return View(RecordGcMovimentos);
        }

        public ActionResult GetDadosPainelPedidos(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {

            DateTime dataCorte = new DateTime(2023, 4, 17);

            IQueryable<gc_movimentos> movimentos = db.gc_movimentos
                .AsNoTracking()
                .Where(m =>
                    (m.id_movimento_tipo == 3 || m.id_movimento_tipo == 4 || m.id_movimento_tipo == 8) &&
                    m.id_movimento_status == 2 &&
                    (m.id_movimento_posicao == 1 || m.id_movimento_posicao == 2 || m.id_movimento_posicao == 3 || m.id_movimento_posicao == 4 || m.id_movimento_posicao == 5) &&
                    m.datahora_aprovacao != null &&
                    m.datahora_aprovacao >= dataCorte
                );

            // Busca por termo (id_movimento ou NF)
            string termo = param.yesCustomField01.EmptyIfNull().ToString().Trim();
            if (!string.IsNullOrWhiteSpace(termo))
            {
                filterOnOff = "1";
                if (termo.StartsWith("*")) termo = termo.Substring(1);

                int idMov;
                int.TryParse(termo, out idMov);

                movimentos = movimentos.Where(m =>
                    (idMov > 0 && m.id_movimento == idMov) ||
                    db.gc_movimentos_nf.Any(nf => nf.id_movimento == m.id_movimento && nf.nf_numero == termo)
                );
            }
            else
            {
                if (int.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), out int idPos) && idPos > 0)
                {
                    filterOnOff = "1";
                    movimentos = movimentos.Where(m => m.id_movimento_posicao == idPos);
                }

                if (int.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), out int idCli) && idCli > 0)
                {
                    filterOnOff = "1";
                    movimentos = movimentos.Where(m => m.id_cliente == idCli);
                }

                if (int.TryParse(param.yesCustomField04.EmptyIfNull().ToString().Trim(), out int idLoc) && idLoc > 0)
                {
                    filterOnOff = "1";
                    movimentos = movimentos.Where(m => m.id_local_estoque == idLoc);
                }
            }

            // Regra de vendedor
            string grp = CachePersister.userIdentity.GcParamGrupoVendedor.EmptyIfNull().ToString().Trim();
            if (grp == "0")
            {
                movimentos = movimentos.Where(m => m.id_vendedor == 0);
            }
            else if (grp == "99999")
            {
                movimentos = movimentos.Where(m => m.id_vendedor > 0);
            }
            else
            {
                var idsVend = grp.Split(',')
                    .Select(s => s.Trim())
                    .Select(s => int.TryParse(s, out var x) ? (int?)x : null)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .Distinct()
                    .ToList();

                movimentos = (idsVend.Count == 0)
                    ? movimentos.Where(m => false)
                    : movimentos.Where(m => idsVend.Contains(m.id_vendedor));
            }

            int totalRecords = movimentos.Count();
            int totalDisplayRecords = totalRecords;

            int start = param.iDisplayStart;
            int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

            // Query final (JOINs + subqueries), com ORDER BY aplicado aqui (antes do Skip/Take)
            var query =
                from m in movimentos

                join cli in db.g_clientes.AsNoTracking() on m.id_cliente equals cli.id_cliente into cliJoin
                from cli in cliJoin.DefaultIfEmpty()

                join vend in db.g_vendedores.AsNoTracking() on m.id_vendedor equals vend.id_vendedor into vendJoin
                from vend in vendJoin.DefaultIfEmpty()

                join loc in db.gc_locais_estoque.AsNoTracking() on m.id_local_estoque equals loc.id_local_estoque into locJoin
                from loc in locJoin.DefaultIfEmpty()

                join pos in db.gc_movimentos_posicao.AsNoTracking() on m.id_movimento_posicao equals pos.id_movimento_posicao into posJoin
                from pos in posJoin.DefaultIfEmpty()

                join op in db.gc_cfop_operacoes.AsNoTracking() on m.id_cfop_operacao equals op.id_cfop_operacao into opJoin
                from op in opJoin.DefaultIfEmpty()

                select new
                {
                    // movimento
                    m.id_movimento,
                    m.id_movimento_posicao,
                    m.id_cfop_operacao,
                    m.id_moeda,
                    m.valor_total_bruto,
                    m.qtd_itens,
                    m.datahora_aprovacao,

                    // joins
                    ClienteNome = cli.nome,
                    VendedorApelido = vend.apelido,
                    LocalSigla = loc.sigla,
                    PosicaoNome = pos.posicao,
                    ProximaAtividade = pos.proxima_atividade,

                    // operação
                    OpDescricao = op.descricao_tv_monitor,
                    op.is_venda,
                    op.is_remessa,
                    op.is_devolucao,
                    op.is_servico,
                    op.is_baixa,

                    // NF numero
                    NfNumero = db.gc_movimentos_nf
                        .Where(nf => nf.id_movimento == m.id_movimento &&
                                    (nf.id_nfe_status == 8 || nf.id_nfe_status == 17 || nf.id_nfe_status == 22))
                        .OrderBy(nf => nf.id_movimento_nf)
                        .Select(nf => nf.nf_numero)
                        .FirstOrDefault(),

                    // DIFAL existe?
                    HasDifal = db.gc_financeiro_lancamentos.Any(fl =>
                        fl.id_movimento == m.id_movimento &&
                        fl.id_pag_rec_tipo == 1 &&
                        fl.is_difal == true
                    )
                };

            // ✅ Ordenação determinística ANTES do Skip
            var page = query
                .OrderByDescending(x => x.datahora_aprovacao)
                .ThenByDescending(x => x.id_movimento) // garante determinismo quando datahora empata
                .Skip(start)
                .Take(length)
                .ToList();

            // Cores fixas (mantidas)
            string ColorMovimentoAprovado = "#F5B7B1";
            string ColorMovimentoSeparado = "#EDBB99";
            string ColorMovimentoFaturado = "#FAD7A0";
            string ColorMovimentoEmitidoNF = "#F9E79F";
            string ColorMovimentoExpedido = "#AED6F1";
            string ColorMovimentoEntregue = "#ABEBC6";
            string ColorMovimentoPlanoAcao = "#DC143C";
            string ColorMovimentoQualificado = "#32CD32";

            var list = new List<string[]>(page.Count);

            foreach (var m in page)
            {
                string colorMovimento = "";
                string iconePosicaoAtual = "";
                string iconeProximaAtividade = "";

                if (m.id_movimento_posicao == 1)
                {
                    iconePosicaoAtual = LibIcons.getIcon("fa-solid fa-clipboard-check", "Aprovado", "#008000", "fa-lg");
                    iconeProximaAtividade = LibIcons.getIcon("fa-solid fa-dolly", "Separar", "#008000", "fa-lg");
                    colorMovimento = ColorMovimentoAprovado;
                }
                else if (m.id_movimento_posicao == 2)
                {
                    iconePosicaoAtual = LibIcons.getIcon("fa-solid fa-dolly", "Separado", "#008000", "fa-sm");
                    iconeProximaAtividade = LibIcons.getIcon("fa-solid fa-credit-card", "Faturar", "#008000", "fa-sm");
                    colorMovimento = ColorMovimentoSeparado;
                }
                else if (m.id_movimento_posicao == 3)
                {
                    iconePosicaoAtual = LibIcons.getIcon("fa-solid fa-credit-card", "Faturado", "#008000", "fa-sm");
                    iconeProximaAtividade = LibIcons.getIcon("fa-solid fa-file-invoice", "Emitir NF", "#008000", "fa-sm");
                    colorMovimento = ColorMovimentoFaturado;
                }
                else if (m.id_movimento_posicao == 4)
                {
                    iconePosicaoAtual = LibIcons.getIcon("fa-solid fa-file-invoice", "NF Emitida", "#008000", "fa-sm");
                    iconeProximaAtividade = LibIcons.getIcon("fa-solid fa-truck", "Expedir", "#008000", "fa-sm");
                    colorMovimento = ColorMovimentoEmitidoNF;
                }
                else if (m.id_movimento_posicao == 5)
                {
                    iconePosicaoAtual = LibIcons.getIcon("fa-solid fa-truck", "Expedido", "#008000", "fa-sm");
                    iconeProximaAtividade = LibIcons.getIcon("fa-solid fa-people-carry", "Entregar", "#008000", "fa-sm");
                    colorMovimento = ColorMovimentoExpedido;
                }
                else if (m.id_movimento_posicao == 6)
                {
                    iconePosicaoAtual = LibIcons.getIcon("fa-solid fa-people-carry", "Entregue", "#008000", "fa-sm");
                    iconeProximaAtividade = LibIcons.getIcon("fa-regular fa-face-smile", "Qualificar", "#008000", "fa-sm");
                    colorMovimento = ColorMovimentoEntregue;
                }
                else if (m.id_movimento_posicao == 7)
                {
                    iconePosicaoAtual = LibIcons.getIcon("fa-solid fa-list-ol", "Plano de Ação", "#008000", "fa-sm");
                    iconeProximaAtividade = LibIcons.getIcon("fa-solid fa-certificate", "Certificado", "#008000", "fa-sm");
                    colorMovimento = ColorMovimentoPlanoAcao;
                }
                else if (m.id_movimento_posicao == 8)
                {
                    iconePosicaoAtual = "&nbsp;&nbsp;" + LibIcons.getIcon("fa-solid fa-certificate", "Qualificado", "#008000", "fa-sm");
                    iconeProximaAtividade = "";
                    colorMovimento = ColorMovimentoQualificado;
                }

                // Operação (ícone)
                string iconeOperacao = "";
                if (!string.IsNullOrWhiteSpace(m.OpDescricao))
                {
                    if (m.is_venda) iconeOperacao = LibIcons.getIcon("fa-solid fa-cart-shopping", m.OpDescricao, "#008000", "fa-lg");
                    else if (m.is_remessa) iconeOperacao = LibIcons.getIcon("fa-solid fa-truck-arrow-right", m.OpDescricao, "#008000", "fa-lg");
                    else if (m.is_devolucao) iconeOperacao = LibIcons.getIcon("fa-solid fa-reply-all", m.OpDescricao, "#008000", "fa-lg");
                    else if (m.is_servico) iconeOperacao = LibIcons.getIcon("fa-solid fa-wrench", m.OpDescricao, "#008000", "fa-lg");
                    else if (m.is_baixa) iconeOperacao = LibIcons.getIcon("fa-solid fa-trash", m.OpDescricao, "#008000", "fa-lg");
                }

                string nomeCliente = (m.ClienteNome ?? "");

                if (!string.IsNullOrWhiteSpace(m.OpDescricao))
                {
                    if (m.HasDifal)
                        nomeCliente += "<br/>" + "<b><font color=\"#cc0000\">[ ATENÇÃO: " + m.OpDescricao + " COM DIFAL ]</font></b>";
                    else
                        nomeCliente += "<br/>" + "[ Operação: " + m.OpDescricao + " ]";
                }

                // Moeda
                string culturaMoeda = (m.id_moeda == 2) ? "en-US" : "pt-BR";
                string valorFormatado = string.Format(CultureInfo.GetCultureInfo(culturaMoeda), "{0:C}", m.valor_total_bruto);
                if (m.id_moeda == 2) valorFormatado = valorFormatado.Replace("$", "$ ");

                list.Add(new[]
                {
            colorMovimento,
            "",
            m.id_movimento.ToString(),
            iconeOperacao,
            (m.NfNumero ?? ""),
            (!string.IsNullOrWhiteSpace(m.PosicaoNome) ? iconePosicaoAtual + "<br/>" + m.PosicaoNome : ""),
            (!string.IsNullOrWhiteSpace(m.ProximaAtividade) ? iconeProximaAtividade + "<br/>" + m.ProximaAtividade : ""),
            nomeCliente,
            (m.VendedorApelido ?? ""),
            m.datahora_aprovacao.GetValueOrDefault().ToString("dd/MM/yy") + "<br/>" + m.datahora_aprovacao.GetValueOrDefault().ToString("HH:mm"),
            m.qtd_itens.EmptyIfNull().ToString(),
            (m.LocalSigla ?? ""),
            valorFormatado,
            "", "", "", "", ""
        });
            }

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = totalRecords,
                iTotalDisplayRecords = totalDisplayRecords,
                aaData = list
            }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }
        #endregion

        #region Pedidos - Consultar Pedidos
        public ActionResult ModalConsultaPedidos()
        {
            cstModalRelatorio view_cstModalRelatorio = new cstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual().AddMonths(-6);
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            view_cstModalRelatorio.Field_Int_01 = -1;
            view_cstModalRelatorio.Field_Int_02 = -2;
            ViewBag.comboClientes = LibDataSets.LoadComboSomenteGClientes(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "0", Text = "[ TODOS OS CLIENTES ]" });
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ SELECIONE O CLIENTE ]" });
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosTodos(db);
            ViewBag.comboProdutosServicos.Insert(0, new SelectListItem { Value = "0", Text = "[ TODOS OS PRODUTOS ]" });
            ViewBag.comboProdutosServicos.Insert(0, new SelectListItem { Value = "-1", Text = "[ SELECIONE O PRODUTO ]" });
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-chart-column", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Consulta de Pedidos</b>";
            return View(view_cstModalRelatorio);
        }

        public ActionResult GetRelatorioConsultaPedidos(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            int IdProduto = 0;
            DateTime DataField03 = new DateTime();
            DateTime DataField04 = new DateTime();
            DateTime.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField03);
            DateTime.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField04);

            var allRecords = new List<Db.gc_movimentos>();
            var allRecordsClientes = db.g_clientes.Select(g => new { g.id_cliente, g.nome }).ToList();
            var allRecordsVendedores = db.g_vendedores.Select(v => new { v.id_vendedor, v.apelido }).ToList();
            var allRecordsLocaisEstoqueOrders = db.gc_locais_estoque.Select(e => new { e.id_local_estoque, e.sigla }).ToList();
            String SentencaSQL = string.Empty;

            if ((param.yesCustomField01.EmptyIfNull().ToString().Trim() == "-1") && (param.yesCustomField02.EmptyIfNull().ToString().Trim() == "-1"))
            {
                SentencaSQL += " select * from gc_movimentos movimento where (movimento.id_movimento < 0) ";
            }
            else
            {
                SentencaSQL += " select * from gc_movimentos movimento ";
                SentencaSQL += " join gc_movimentos_itens item on (item.id_movimento = movimento.id_movimento) ";
                SentencaSQL += " join g_produtos produto on (produto.id_produto = item.id_produto) ";
                SentencaSQL += " where (movimento.id_movimento_tipo = 3 or movimento.id_movimento_tipo = 4 or movimento.id_movimento_tipo = 8) ";
                if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1")) { SentencaSQL += " and movimento.id_cliente = " + param.yesCustomField01.EmptyIfNull().ToString().Trim(); } // Id Cliente
                if ((param.yesCustomField02.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField02.EmptyIfNull().ToString().Trim() != "-1") && (param.yesCustomField02.EmptyIfNull().ToString().Trim() != "0")) { int.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), out IdProduto); } // Id Produto
                if (IdProduto > 0) SentencaSQL += " and item.id_produto = " + IdProduto.ToString();
                SentencaSQL += " and movimento.datahora_alteracao between '" + DataField03.ToString("yyyy-MM-dd") + " 00:00:00" + "' and '" + DataField04.ToString("yyyy-MM-dd") + " 23:59:59'";
                SentencaSQL += " order by movimento.datahora_alteracao desc ";
            }
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
                String LabelClienteProduto = string.Empty;
                var arrayCliente = allRecordsClientes.Find(f => f.id_cliente == m.id_cliente);
                var arrayVendedor = allRecordsVendedores.Find(v => v.id_vendedor == m.id_vendedor);
                if (arrayCliente != null) { LabelClienteProduto = "<b>" + arrayCliente.nome.EmptyIfNull().ToString() + "   [Id: " + arrayCliente.id_cliente.EmptyIfNull().ToString() + "]" + "</b>"; };

                string iconeTipo = String.Empty;
                if (m.id_movimento_status == 1) { iconeTipo = LibIcons.getIcon("fa-solid fa-clipboard-list", "Cotação (Aberta)", "#CACFD2", "fa-lg"); }
                else if (m.id_movimento_status == 2) { iconeTipo = LibIcons.getIcon("fa-solid fa-boxes", "Pedido (Fechado)", "#008000", "fa-lg"); }
                else if (m.id_movimento_status == 3) { iconeTipo = LibIcons.getIcon("fa-regular fa-thumbs-down", "Pedido(Cancelado)", "cc0000", "fa-lg"); }

                string formatoMoeda = string.Empty;
                if (m.id_moeda == 1) { formatoMoeda = "pt-BR"; }
                else if (m.id_moeda == 2) { formatoMoeda = "en-US"; }
                string valorFormatado = string.Format(CultureInfo.GetCultureInfo(formatoMoeda), "{0:C}", m.valor_total_bruto);
                if (m.id_moeda == 2) { valorFormatado = valorFormatado.Replace("$", "$ "); };

                String SqlItens = string.Empty;
                SqlItens += "select movimento.id_movimento, item.id_produto, produto.descricao, item.quantidade, item.valor_unit, item.valor_total from gc_movimentos movimento ";
                SqlItens += "join gc_movimentos_itens item on(movimento.id_movimento = item.id_movimento) ";
                SqlItens += "join g_produtos produto on (item.id_produto = produto.id_produto) ";
                SqlItens += "where movimento.id_movimento = " + m.id_movimento.ToString() + " ";
                if (IdProduto > 0) SqlItens += "and item.id_produto = " + IdProduto.ToString();
                DataTable TableItem = LibDB.GetDataTable(SqlItens, db);
                List<DataRow> AllItens = TableItem.AsEnumerable().ToList();
                foreach (var dsRowItem in AllItens)
                {
                    Decimal QtdProduto = 0;
                    Decimal ValorUnitProduto = 0;
                    String NomeProduto = string.Empty;
                    NomeProduto = dsRowItem["descricao"].EmptyIfNull().ToString().Trim();
                    if (NomeProduto.Length > 120) { NomeProduto = NomeProduto.Substring(0, 120) + "..."; }
                    decimal.TryParse(dsRowItem["quantidade"].EmptyIfNull().ToString(), out QtdProduto);
                    decimal.TryParse(dsRowItem["valor_unit"].EmptyIfNull().ToString(), out ValorUnitProduto);
                    QtdProduto = decimal.Truncate(QtdProduto);
                    NomeProduto += "     |  <b>" + QtdProduto.ToString("0") + "  x  " + ValorUnitProduto.ToString("###,###,##0.00") + "</b>";
                    LabelClienteProduto += "<br/>" + NomeProduto;
                }

                if (AllItens.Count > 0)
                {
                    list.Add(new[] {
                                    m.id_movimento.ToString(),
                                    iconeTipo,
                                    LabelClienteProduto,
                                    ((arrayVendedor != null) ? arrayVendedor.apelido.EmptyIfNull().ToString() : String.Empty),
                                    m.datahora_alteracao.GetValueOrDefault().ToString("dd/MM/yy"),
                                    valorFormatado,
                                });
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        list.Add(new[] {
                                    "X",
                                    "",
                                    "",
                                    "",
                                    "",
                                    "",
                                });
                    }
                }
            }

            if (displayedRecords.Count() == 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    list.Add(new[] {
                                    "X",
                                    "",
                                    "",
                                    "",
                                    "",
                                    "",
                                });
                }
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

        #region Pedido - Importar Excel SC
        public ActionResult ModalImportarExcelSC(int? idMovimento)
        {
            DeleteItemTemporario();
            cstUploadFiles record_cstUploadFiles = new cstUploadFiles();
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-file-excel", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Importar Excel - Southern Cross";
            var comboMovimentosTipos = new List<SelectListItem>();
            comboMovimentosTipos.Add(new SelectListItem { Value = "12", Text = "1.2 - Cotação - Fornecedor" });
            ViewBag.comboMovimentosTipos = comboMovimentosTipos;
            record_cstUploadFiles.IdMovimentoTipo = 12;
            ViewBag.idMovimento = (CachePersister.userIdentity.IdUsuario * -1).ToString(); // O Id, será o negativo do id do usuário;
            return View("ModalImportarExcelSC", record_cstUploadFiles);
        }

        [HttpPost]
        public ActionResult AjaxModalImportarExcelSC(cstUploadFiles record_cstUploadFiles)
        {
            bool Processado = false;
            bool ProdutoComexAtualizado = false;
            bool ErroProcessamento = false;
            int QtdProdutosCadastrados = 0;
            int QtdItensImportados = 0;
            string FileUploadXLXS = string.Empty;
            string FileNameXlsxLocal = string.Empty;
            string FileNameXlsxUpload = string.Empty;
            String MsgRetorno = String.Empty;
            String ResultadoProcessamento = String.Empty;
            String IdProcessamentoGravado = "0";
            String ListaProdutosNaoVinculados = string.Empty;

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
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                    List<cstModelSalesOrderSC> ListaItensSO = new List<cstModelSalesOrderSC>();
                    List<gc_movimentos_itens> ListaItensPedido = new List<gc_movimentos_itens>();
                    List<String> ListaColunas = new List<String>();

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
                                        else if ((ColunaCabecalho == "TOTAL AMOUNT") || (ColunaCabecalho == "TOTAL PRICE") || (ColunaCabecalho.StartsWith("TOTAL"))) { IndexTotalPrice = IndexCol; }
                                    }
                                    LeituraAtiva = true;
                                }

                                if (LeituraAtiva == true)
                                {
                                    cstModelSalesOrderSC ItemSO = new cstModelSalesOrderSC();
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

                    if ((ErroProcessamento == false) && (ListaItensSO.Count > 0))
                    {
                        List<gc_comex_produtos> ListaComexProdutos = db.gc_comex_produtos.Where(p => p.ativo == true).ToList();
                        List<g_produtos> ListaProdutosGDI = db.g_produtos.Where(p => p.ativo == true).ToList();
                        foreach (var ItemSO in ListaItensSO)
                        {
                            // CADASTRO DE PRODUTOS - 2
                            String ListaPnsSimilares = string.Empty;

                            String PNOficial = ItemSO.String_PN.EmptyIfNull().ToString();
                            String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                            String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                            String PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                            g_produtos ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                            try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };// Buscar pelo PN Auxiliar

                            gc_comex_produtos ProdutoComex = ListaComexProdutos.Where(p => p.pn == PNOficial).FirstOrDefault();
                            try { if (ProdutoComex == null) { ProdutoComex = ListaComexProdutos.Where(p => p.pn_auxiliar == PNAuxiliar || p.pn_variacao1 == PNCuringaOH || p.pn_variacao2 == PNCuringaZERO).FirstOrDefault(); };} catch (Exception) { };

                            gc_movimentos_itens NovoItemPedido = new gc_movimentos_itens();
                            NovoItemPedido.id_movimento = int.Parse((CachePersister.userIdentity.IdUsuario * -1).ToString()); // O Id, será o negativo do id do usuário;
                            NovoItemPedido.id_produto_condicao = 1;
                            NovoItemPedido.id_entrega_prazo = 1;
                            NovoItemPedido.sequencia = 0;
                            NovoItemPedido.valor_unit_corecharge = 0;
                            NovoItemPedido.valor_total_corecharge = 0;

                            if (ProdutoComex == null) // NOVO PRODUTO COMEX - NEW GC_COMEX_PRODUTOS
                            {
                                ProdutoComex = new gc_comex_produtos();

                                if (ProdutoGDI != null)
                                {
                                    ProdutoComex.id_produto = ProdutoGDI.id_produto;
                                    ProdutoComex.item_cadastro_novo = false;
                                    QtdProdutosVinculados += 1;
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Vinculação ao Produto ERP id: " + ProdutoGDI.id_produto.ToString());
                                }
                                else
                                {
                                    ProdutoComex.id_produto = 0;
                                    ProdutoComex.item_cadastro_novo = true;
                                    if (ListaProdutosNaoVinculados.IndexOf(ItemSO.String_PN + ",") == -1)
                                    {
                                        ListaProdutosNaoVinculados += ItemSO.String_PN + ", ";
                                        QtdProdutosNaoVinculados += 1;
                                    }
                                }
                                ProdutoComex.ativo = true;
                                ProdutoComex.pn = ItemSO.String_PN;
                                ProdutoComex.pn_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemSO.String_PN);
                                ProdutoComex.pn_variacao1 = ProdutoComex.pn_auxiliar.Replace("0", "O");
                                ProdutoComex.pn_variacao2 = ProdutoComex.pn_auxiliar.Replace("O", "0");
                                ProdutoComex.fob1_dollar = ItemSO.Decimal_UnitPrice;
                                ProdutoComex.description = ItemSO.String_Description;
                                ProdutoComex.item_cadastro_similaridade = false;
                                ProdutoComex.id_coligada = 0; // Global
                                ProdutoComex.id_filial = 0; // Global
                                ProdutoComex.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                                ProdutoComex.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                db.gc_comex_produtos.Add(ProdutoComex);
                                db.SaveChanges();
                                LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Novo Produto Comex | Upload Lista Itens");
                                ListaComexProdutos.Add(ProdutoComex); // Atualizar a lista de produtos Comex
                                QtdNovosProdutosCadastrados += 1;

                                NovoItemPedido.id_produto = ProdutoComex.id_produto;
                                NovoItemPedido.quantidade = ItemSO.Int_Qty;
                                NovoItemPedido.valor_unit = Math.Round(ItemSO.Decimal_UnitPrice, 2) * 2;
                                NovoItemPedido.valor_total = NovoItemPedido.quantidade * NovoItemPedido.valor_unit;
                                ListaItensPedido.Add(NovoItemPedido);
                            }
                            else    // Produto Comex Existente
                            {
                                if (ProdutoComex.id_produto == 0)
                                {
                                    if (ProdutoGDI != null)
                                    {
                                        NovoItemPedido.id_produto = ProdutoGDI.id_produto;
                                        QtdProdutosVinculados += 1;
                                    }
                                    else
                                    {
                                        if (ListaProdutosNaoVinculados.IndexOf(ItemSO.String_PN + ",") == -1)
                                        {
                                            ListaProdutosNaoVinculados += ItemSO.String_PN + ", ";
                                            QtdProdutosNaoVinculados += 1;
                                        }
                                        NovoItemPedido.id_produto = 0;
                                    }
                                }
                                NovoItemPedido.id_produto = ProdutoComex.id_produto;
                                NovoItemPedido.quantidade = ItemSO.Int_Qty;
                                NovoItemPedido.valor_unit = Math.Round(ItemSO.Decimal_UnitPrice, 2) * 2;
                                NovoItemPedido.valor_total = NovoItemPedido.quantidade * NovoItemPedido.valor_unit;
                                ListaItensPedido.Add(NovoItemPedido);
                            }


                            // Atualizacao do Produto Comex
                            if (ProdutoComex != null)
                            {
                                ProdutoComexAtualizado = false;
                                if (ProdutoComex.fob1_dollar == 0)
                                {
                                    ProdutoComex.fob1_dollar = ItemSO.Decimal_UnitPrice;
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Atualização Fob US$: " + ProdutoComex.fob1_dollar.ToString("0.00000"));
                                    ProdutoComexAtualizado = true;
                                };
                                if ((ProdutoComex.id_produto == 0) && (ProdutoGDI != null))
                                {
                                    ProdutoComex.id_produto = ProdutoGDI.id_produto;
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Vinculação ao Produto ERP id: " + ProdutoGDI.id_produto.ToString());
                                    ProdutoComexAtualizado = true;
                                }
                                if (ProdutoComexAtualizado == true)
                                {
                                    ProdutoComex.datahora_alteracao = DataHoraAtual;
                                    ProdutoComex.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(ProdutoComex).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }

                            // Atualização do Produto GDI
                            if (ProdutoGDI != null)
                            {
                                bool ProdutoGDIAtualizado = false;
                                if (ProdutoGDI.fob1_dollar == 0)
                                {
                                    ProdutoGDI.fob1_dollar = ItemSO.Decimal_UnitPrice;
                                    ProdutoGDI.fob1_id_importacao = 0;
                                    LibAudit.SaveAudit(db, false, "gc_produtos", ProdutoGDI.id_produto, "Atualização Fob US$: " + ProdutoGDI.fob1_dollar.ToString("0.00000"));
                                    ProdutoGDIAtualizado = true;
                                }
                                if (ProdutoGDIAtualizado == true)
                                {
                                    ProdutoGDI.datahora_alteracao = DataHoraAtual;
                                    ProdutoGDI.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(ProdutoGDI).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }
                        }

                        if (QtdProdutosNaoVinculados == 0)
                        {
                            QtdItensImportados = 0;
                            foreach (var NovoItemMovimentoCompra in ListaItensPedido)
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
                            if (QtdProdutosNaoVinculados > 0) { MsgRetorno += QtdProdutosNaoVinculados.ToString() + LibStringFormat.GetTabHtml(1) + "Produtos(s) Novos NÃO Vinculados aos produtos GDI" + "<br/><br/>"; };
                            MsgRetorno += "Obs: Foram indentificados " + QtdProdutosNaoVinculados + " Novos Produtos sem a vinculação ao cadastro de Produtos Principal GDI, execute a conferência/vinculação dos novos produtos no menu [Cadastros Comercial > Produtos (Novos)]!" + "<br/><br/>";
                            MsgRetorno += "PNs Identificados: " + ListaProdutosNaoVinculados.EmptyIfNull().ToString();
                        }
                    }
                    else
                    {
                        MsgRetorno += "Não há itens válidos a serem processados!" + "<br/>";
                        ErroProcessamento = true;
                        Processado = false;
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

        #region Pedido - Importar Txt SC
        public ActionResult ModalImportarTxtSC(int? idMovimento)
        {
            DeleteItemTemporario();
            cstUploadList record_cstUploadList = new cstUploadList();
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-file-excel", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Importar Lista Itens";
            ViewBag.idMovimento = (CachePersister.userIdentity.IdUsuario * -1).ToString(); // O Id, será o negativo do id do usuário;
            return View("ModalImportarTxtSC", record_cstUploadList);
        }

        [HttpPost]
        public ActionResult AjaxModalImportarTxtSC(cstUploadList record_cstUploadList)
        {
            bool Processado = false;
            bool ErroImpeditivo = false;
            bool ProdutoComexAtualizado = false;
            int QtdProdutosCadastrados = 0;
            int QtdItensImportados = 0;
            string FileUploadXLXS = string.Empty;
            string FileNameXlsxLocal = string.Empty;
            string FileNameXlsxUpload = string.Empty;
            String MsgRetorno = String.Empty;
            String ResultadoProcessamento = String.Empty;
            String IdProcessamentoGravado = "0";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            if (record_cstUploadList.List1.EmptyIfNull().ToString().Length == 0)
            {
                ErroImpeditivo = true;
                MsgRetorno += "Não há itens a serem processados!" + "<br/>";
            }

            int QtdNovosProdutosCadastrados = 0;
            int QtdProdutosVinculados = 0;
            int QtdProdutosNaoVinculados = 0;

            if (ErroImpeditivo == false)
            {
                try
                {
                    String ColunaCabecalho = string.Empty;
                    String ListaProdutosNaoVinculados = string.Empty;
                    String _ItemTemp = string.Empty;
                    String _FobTemp = string.Empty;
                    List<cstModelSalesOrderSC> ListaItensSO = new List<cstModelSalesOrderSC>();
                    List<gc_movimentos_itens> ListaItensPedido = new List<gc_movimentos_itens>();
                    List<String> ListaColunas = new List<String>();
                    String[] ListaItens = null;
                    String[] ListaFob = null;

                    try { ListaItens = record_cstUploadList.List1.EmptyIfNull().ToString().Split('\r'); } catch (Exception) { };
                    try { ListaFob = record_cstUploadList.List2.EmptyIfNull().ToString().Split('\r'); } catch (Exception) { };


                    if (ListaItens.Count() == 0)
                    {
                        ErroImpeditivo = true;
                        ErroImpeditivo = true;
                        MsgRetorno += "Não há itens válidos a serem processados!" + "<br/>";
                    }
                    else
                    {
                        for (int index = 0; index < ListaItens.Count(); index++)
                        {
                            cstModelSalesOrderSC ItemSO = new cstModelSalesOrderSC();
                            ItemSO.String_Qty = "1";

                            _ItemTemp = string.Empty;
                            _FobTemp = string.Empty;

                            try { _ItemTemp = ListaItens[index].EmptyIfNull().ToString().Trim().Replace("\r", "").Replace("\n", "").Replace("\t", ""); } catch (Exception) { };
                            try { _FobTemp = ListaFob[index].EmptyIfNull().ToString().Trim().Replace("\r", "").Replace("\n", "").Replace("\t", ""); } catch (Exception) { };
                           

                            if ((_ItemTemp.EmptyIfNull().ToString().Length > 0) && (_FobTemp.EmptyIfNull().ToString().Length > 0))
                            {
                                // Descrição do item
                                if (_ItemTemp.Length == 0)
                                {
                                    ErroImpeditivo = true;
                                    MsgRetorno += "Item [" + (index + 1).ToString() + "] sem informações do item!" + "<br/>";
                                }
                                else
                                {
                                    if (_ItemTemp.IndexOf("  ") <= 3)
                                    {
                                        ErroImpeditivo = true;
                                        MsgRetorno += "Item [" + (index + 1).ToString() + "] formatação incorreta!" + "<br/>";
                                    }
                                    else
                                    {
                                        ItemSO.String_Description = _ItemTemp;
                                    }
                                }

                                // Fob do item
                                if (_FobTemp.Length > 0)
                                {
                                    ItemSO.String_UnitPrice = _FobTemp;
                                }
                                else
                                {
                                    ErroImpeditivo = true;
                                    MsgRetorno += "Item [" + (index + 1).ToString() + "] sem valor do Fob!" + "<br/>";
                                }

                                if (ItemSO.IsValidItemSimpleLista())
                                {
                                    ListaItensSO.Add(ItemSO);
                                }
                                else
                                {
                                    ErroImpeditivo = true;
                                    MsgRetorno += "Item [" + (index + 1).ToString() + "] Erros nos dados!" + "<br/>";
                                }

                                if (ErroImpeditivo)
                                {
                                    break;
                                };
                            }
                        }
                    }


                    if ((ErroImpeditivo == false) && (ListaItensSO.Count > 0))
                    {
                        List<gc_comex_produtos> ListaComexProdutos = db.gc_comex_produtos.Where(p => p.ativo == true).ToList();
                        List<g_produtos> ListaProdutosGDI = db.g_produtos.Where(p => p.ativo == true).ToList();
                        foreach (var ItemSO in ListaItensSO)
                        {
                            // CADASTRO DE PRODUTOS - 1
                            String ListaPnsSimilares =  string.Empty;

                            String PNOficial = ItemSO.String_PN.EmptyIfNull().ToString().Trim().ToUpperInvariant();
                            String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                            String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                            String PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                            g_produtos ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                            try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar

                            gc_comex_produtos ProdutoComex = ListaComexProdutos.Where(p => p.pn == PNOficial).FirstOrDefault();
                            try { if (ProdutoComex == null) { ProdutoComex = ListaComexProdutos.Where(p => p.pn_auxiliar == PNAuxiliar || p.pn_variacao1 == PNCuringaOH || p.pn_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };

                            gc_movimentos_itens NovoItemPedido = new gc_movimentos_itens();
                            NovoItemPedido.id_movimento = int.Parse((CachePersister.userIdentity.IdUsuario * -1).ToString()); // O Id, será o negativo do id do usuário;
                            NovoItemPedido.id_produto_condicao = 1;
                            NovoItemPedido.id_entrega_prazo = 1;
                            NovoItemPedido.sequencia = 0;
                            NovoItemPedido.valor_unit_corecharge = 0;
                            NovoItemPedido.valor_total_corecharge = 0;

                            if (ProdutoComex == null) // NOVO PRODUTO COMEX - NEW GC_COMEX_PRODUTOS
                            {
                                ProdutoComex = new gc_comex_produtos();
                                
                                if (ProdutoGDI != null)
                                {
                                    ProdutoComex.id_produto = ProdutoGDI.id_produto;
                                    ProdutoComex.item_cadastro_novo = false;
                                    QtdProdutosVinculados += 1;
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Vinculação ao Produto ERP id: " + ProdutoGDI.id_produto.ToString());
                                }
                                else 
                                {
                                    ProdutoComex.id_produto = 0;
                                    ProdutoComex.item_cadastro_novo = true;
                                    if (ListaProdutosNaoVinculados.IndexOf(ItemSO.String_PN + ",") == -1)
                                    {
                                        ListaProdutosNaoVinculados += ItemSO.String_PN + ", ";
                                        QtdProdutosNaoVinculados += 1;
                                    }
                                }
                                ProdutoComex.ativo = true;
                                ProdutoComex.pn = ItemSO.String_PN;
                                ProdutoComex.pn_auxiliar = PNAuxiliar;
                                ProdutoComex.pn_variacao1 = ProdutoComex.pn_auxiliar.Replace("0", "O");
                                ProdutoComex.pn_variacao2 = ProdutoComex.pn_auxiliar.Replace("O", "0");
                                ProdutoComex.fob1_dollar = ItemSO.Decimal_UnitPrice;
                                ProdutoComex.description = ItemSO.String_Description;
                                ProdutoComex.item_cadastro_similaridade = false;
                                ProdutoComex.id_coligada = 0; // Global
                                ProdutoComex.id_filial = 0; // Global
                                ProdutoComex.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                                ProdutoComex.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                db.gc_comex_produtos.Add(ProdutoComex);
                                db.SaveChanges();
                                LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Novo Produto Comex | Upload Lista Itens");
                                ListaComexProdutos.Add(ProdutoComex); // Atualizar a lista de produtos Comex
                                QtdNovosProdutosCadastrados += 1;

                                NovoItemPedido.id_produto = ProdutoComex.id_produto;
                                NovoItemPedido.quantidade = ItemSO.Int_Qty;
                                NovoItemPedido.valor_unit = Math.Round(ItemSO.Decimal_UnitPrice, 2) * 2;
                                NovoItemPedido.valor_total = NovoItemPedido.quantidade * NovoItemPedido.valor_unit;
                                ListaItensPedido.Add(NovoItemPedido);
                            }
                            else    // Produto Comex Existente
                            {
                                if (ProdutoComex.id_produto == 0)
                                {
                                    if (ProdutoGDI != null)
                                    {
                                        NovoItemPedido.id_produto = ProdutoGDI.id_produto;
                                        QtdProdutosVinculados += 1;
                                    }
                                    else
                                    {
                                        if (ListaProdutosNaoVinculados.IndexOf(ItemSO.String_PN + ",") == -1)
                                        {
                                            ListaProdutosNaoVinculados += ItemSO.String_PN + ", ";
                                            QtdProdutosNaoVinculados += 1;
                                        }
                                        NovoItemPedido.id_produto = 0;
                                    }
                                }
                                NovoItemPedido.id_produto = ProdutoComex.id_produto;
                                NovoItemPedido.quantidade = ItemSO.Int_Qty;
                                NovoItemPedido.valor_unit = Math.Round(ItemSO.Decimal_UnitPrice, 2) * 2;
                                NovoItemPedido.valor_total = NovoItemPedido.quantidade * NovoItemPedido.valor_unit;
                                ListaItensPedido.Add(NovoItemPedido);
                            }


                            // Atualizacao do Produto Comex
                            if (ProdutoComex != null)
                            {
                                ProdutoComexAtualizado = false;
                                if (ProdutoComex.fob1_dollar == 0) 
                                {
                                    ProdutoComexAtualizado = true;
                                    ProdutoComex.fob1_dollar = ItemSO.Decimal_UnitPrice;
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Atualização Fob US$: " + ProdutoComex.fob1_dollar.ToString("0.00000"));
                                };
                                if ((ProdutoComex.id_produto == 0) && (ProdutoGDI != null))
                                {
                                    ProdutoComexAtualizado = true;
                                    ProdutoComex.id_produto = ProdutoGDI.id_produto;
                                    LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Vinculação ao Produto ERP id: " + ProdutoGDI.id_produto.ToString());
                                }
                                if (ProdutoComexAtualizado == true)
                                {
                                    ProdutoComex.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    ProdutoComex.datahora_alteracao = DataHoraAtual;
                                    db.Entry(ProdutoComex).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }

                            // Atualização do Produto GDI
                            if (ProdutoGDI != null)
                            {
                                bool ProdutoGDIAtualizado = false;
                                if (ProdutoGDI.fob1_dollar == 0)
                                {
                                    ProdutoGDI.fob1_dollar = ItemSO.Decimal_UnitPrice;
                                    ProdutoGDI.fob1_id_importacao = 0;
                                    LibAudit.SaveAudit(db, false, "gc_produtos", ProdutoGDI.id_produto, "Atualização Fob US$: " + ProdutoGDI.fob1_dollar.ToString("0.00000"));
                                    ProdutoGDIAtualizado = true;
                                }
                                if (ProdutoGDIAtualizado == true)
                                {
                                    ProdutoGDI.datahora_alteracao = DataHoraAtual;
                                    ProdutoGDI.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(ProdutoGDI).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }
                        }

                        if (QtdProdutosNaoVinculados == 0)
                        {
                            QtdItensImportados = 0;
                            foreach (var NovoItemMovimentoCompra in ListaItensPedido)
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
                            if (QtdProdutosNaoVinculados > 0) { MsgRetorno += QtdProdutosNaoVinculados.ToString() + LibStringFormat.GetTabHtml(1) + "Produtos(s) Novos NÃO Vinculados aos produtos GDI" + "<br/><br/>"; };
                            MsgRetorno += "Obs: Foram indentificados " + QtdProdutosNaoVinculados + " Novos Produtos sem a vinculação ao cadastro de Produtos Principal GDI, execute a conferência/vinculação dos novos produtos no menu [Cadastros Comercial > Produtos (Novos)]!" + "<br/><br/>";
                            MsgRetorno += "PNs Identificados: " + ListaProdutosNaoVinculados.EmptyIfNull().ToString();
                        }
                    }
                    else
                    {
                        ErroImpeditivo = true;
                        MsgRetorno += "Não há itens válidos a serem processados!" + "<br/>";
                        Processado = false;
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

            if (ErroImpeditivo == true) { Processado = false; } else { Processado = true; }
            return Json(new { success = Processado, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Cancelar
        public ActionResult ModalCancelarPedido(int? id)
        {
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            String MsgBloqueio = string.Empty;
            String MsgInfo = string.Empty;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            TitleModal = LibIcons.getIcon("fa-solid fa-ban", "", "red", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Cancelamento do Pedido Nº " + record_gc_movimento.id_movimento.ToString();
            if (record_gc_movimento != null)
            {
                if (record_gc_movimento.movimento_aprovado == false) { MsgBloqueio = "<b> ---------- PEDIDO NÃO ESTÁ APROVADO ----------</b>" + "<br/>"; }
            }
            else
            {
                MsgBloqueio = "Movimento [" + id.ToString() + "] não localizado no ERP";
            }

            List<gc_financeiro_lancamentos> ListaFinanceiro = db.gc_financeiro_lancamentos.Where(f => f.ativo == true && f.tipo_pag_rec == 2 && f.id_financeiro_status == 3 && f.id_movimento == record_gc_movimento.id_movimento).ToList();
            List<gc_movimentos_nf> ListaNF = db.gc_movimentos_nf.Where(n => n.id_nfe_status == 8 && n.id_movimento == record_gc_movimento.id_movimento).ToList();

            if (ListaFinanceiro.Count() > 0)
            {
                MsgInfo += "<b>" + ListaFinanceiro.Count() + " Lançamentos Financeiros Abertos" + "<b/><br/>";
                foreach (var RecordFinanceiro in ListaFinanceiro)
                {
                    MsgInfo += " | Id: " + RecordFinanceiro.id_lancamento.EmptyIfNull().ToString() + " | Venc: " + RecordFinanceiro.data_vencimento.ToString("dd/MM/yyyy") + " | " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordFinanceiro.valor_total) + "<br/>";
                }
            }
            if (ListaNF.Count() > 0)
            {
                MsgInfo += "<b>" + ListaNF.Count() + " Notas Fiscais Autorizadas" + "<b/><br/>";
                foreach (var RecordNF in ListaNF)
                {
                    MsgInfo += " | Nota Fisal Nº: " + RecordNF.nf_numero.ToString() + "<br/>";
                }
            }
            ViewBag.Title = TitleModal;
            LibDataSets.LoadDatasetGVendedores(db);
            ViewBag.comboVendedores = LibDataSets.LoadComboGVendedores(db);
            ViewBag.MsgBloqueio = MsgBloqueio;
            ViewBag.MsgInfo = MsgInfo;
            return View("ModalCancelarPedido", record_gc_movimento);
        }

        [HttpPost]
        public ActionResult AjaxModalCancelarPedido(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int QtdInconsistencias = 0;
            int QtdNotasFiscaisAtivas = 0;
            String LogAlteracoes = string.Empty;
            String MsgRetorno = "";
            try
            {
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);
                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);

                if (view_record_gc_movimento.obs_cancelamento.EmptyIfNull().ToString().Length <= 5)
                {
                    QtdInconsistencias += 1;
                    MsgRetorno += "Preencha corretamente o campo Motivo do Cancelamento!" + "<br/>";
                }


                if (QtdInconsistencias == 0)
                {
                    List<gc_financeiro_lancamentos> ListaFinanceiro = db.gc_financeiro_lancamentos.Where(f => f.ativo == true && f.tipo_pag_rec == 2 && f.id_financeiro_status == 3 && f.id_movimento == RecordMovimento.id_movimento).ToList();
                    List<gc_movimentos_nf> ListaNotasFiscais = db.gc_movimentos_nf.Where(n => n.id_movimento == RecordMovimento.id_movimento).ToList();

                    if (ListaFinanceiro.Count() > 0)
                    {
                        MsgRetorno += "<b>" + ListaFinanceiro.Count() + " Lançamentos Financeiros Abertos" + "<b/><br/>";
                        foreach (var RecordFinanceiro in ListaFinanceiro)
                        {
                            MsgRetorno += "Id: " + RecordFinanceiro.id_lancamento.EmptyIfNull().ToString() + " | Venc: " + RecordFinanceiro.data_vencimento.ToString("dd/MM/yyyy") + " | " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordFinanceiro.valor_total) + "<br/>";
                        }
                        QtdInconsistencias += 1;
                    }
                    if (ListaNotasFiscais.Count() > 0)
                    {
                        List<g_nfe_status> ListaStatusNotasFiscais = db.g_nfe_status.Where(s => s.id_nfe_status > 0).ToList();
                        foreach (gc_movimentos_nf RecordNotaFiscal in ListaNotasFiscais)
                        {
                            g_nfe_status RecordStatusNf = ListaStatusNotasFiscais.Where(e => e.id_nfe_status == RecordNotaFiscal.id_nfe_status).FirstOrDefault();
                            if (RecordStatusNf.nf_ativa == true)
                            {
                                QtdNotasFiscaisAtivas += 1;
                            }
                        }

                        if (QtdNotasFiscaisAtivas > 0)
                        {
                            MsgRetorno += "<b>" + QtdNotasFiscaisAtivas.ToString() + " Notas Fiscais Ativas" + "<b/><br/>";
                            QtdInconsistencias += 1;
                        }
                    }
                }
                if (QtdInconsistencias == 0)
                {
                    // MOVIMENTO ESTAVA SEPARADO E FOI RETIRADA A SEPARAÇÃO - VERIFICAR SE O TIPO DE OPERAÇÃO FAZ BAIXA DE ESTOQUE
                    if (RecordCfopOperacao.estoque_saida == true)
                    {
                        EstoqueInventarioService ServicoEstoqueBaixa = new EstoqueInventarioService();
                        bool EstoqueMovimentado = ServicoEstoqueBaixa.MovimentarEstoque(RecordMovimento.id_movimento, 9, db, false); // Entrada - Cancelamento Pedido
                        if (EstoqueMovimentado == false)
                        {
                            QtdInconsistencias += 1;
                            MsgRetorno += ServicoEstoqueBaixa.GetMsgProcessamento(); ;
                        }
                    }
                }

                if (QtdInconsistencias == 0)
                {
                    RecordMovimento.movimento_estoque_saida = false;
                    RecordMovimento.movimento_estoque_entrada = true;
                    RecordMovimento.movimento_estoque_devolucao = true;
                    RecordMovimento.id_movimento_status = 3;
                    RecordMovimento.movimento_cancelado = true;
                    RecordMovimento.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                    RecordMovimento.datahora_cancelamento = DataHoraAtual;
                    RecordMovimento.obs_cancelamento = view_record_gc_movimento.obs_cancelamento;
                    RecordMovimento.datahora_alteracao = DataHoraAtual;
                    RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordMovimento).State = EntityState.Modified;

                    LogAlteracoes += "Cancelamento do Pedido | Motivo: " + view_record_gc_movimento.obs_cancelamento.EmptyIfNull().ToString();
                    if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, false, "gc_movimentos", RecordMovimento.id_movimento, LogAlteracoes); };

                    db.SaveChanges();
                    Sucesso = true;
                    MsgRetorno = "Cancelamento do Pedido " + view_record_gc_movimento.id_movimento.EmptyIfNull().ToString() + " executado com Sucesso!";
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Espelho Digital - Itens Invoices 
        public ActionResult ModalInvoicesItensEspelhoDigital(String idMovimento)
        {
            int id_movimento = 0;
            int.TryParse(idMovimento, out id_movimento);
            PreencherLookupsEspelhoDigital();
            ViewBag.Title = "<b>Itens Importados</b>";
            ViewBag.ComboComexImportacoes = LibDataSets.LoadComboGcComexImportacoesTodas(db);
            ViewBag.ComboComexImportacoes.Insert(0, new SelectListItem { Value = "0", Text = "[ TODAS AS IMPORTAÇÕES ]" });
            gc_movimentos record_gc_movimento = new Db.gc_movimentos();
            record_gc_movimento.id_movimento = id_movimento;
            record_gc_movimento.id_importacao = 0;
            return View(record_gc_movimento);
        }

        public ActionResult GetDadosInvoicesItensEspelhoDigital(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            String SentencaSQL = string.Empty;
            String IdItemView = string.Empty;
            DataTable TableItens = new DataTable();
            List<DataRow> RowsItens = new List<DataRow>();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            var allRecords = new List<DataRow>();

            if ((param.yesCustomField01.EmptyIfNull().ToString() == "0") && (param.yesCustomField02.EmptyIfNull().ToString() == "0"))
            {
                // Não faz nada
            }
            else
            {
                SentencaSQL = " select ItemInvoice.id_invoice_item, ItemInvoice.ativo,  ItemInvoice.item_qty, ItemInvoice.pn, ItemInvoice.description,  " +
                                " ItemInvoice.item_unit_price, ItemInvoice.item_total_price, ItemInvoice.note, ItemInvoice.customer, ItemInvoice.serial_number, " +
                                " ItemImportacao.descricao, ItemImportacao.valor_unit, ItemImportacao.valor_total, ItemImportacao.valor_fob " +
                                " from gc_comex_invoices_itens ItemInvoice " +
                                " left join gc_comex_importacoes_itens ItemImportacao on (ItemImportacao.id_comex_produto = ItemInvoice.id_comex_produto and ItemImportacao.id_importacao = ItemInvoice.id_importacao) " +
                                " left join gc_comex_importacoes ComexImportacoes on (ComexImportacoes.id_importacao = ItemInvoice.id_importacao) " +
                                " left join gc_comex_invoices ComexInvoices on (ComexInvoices.id_invoice = ItemInvoice.id_invoice) " +
                                " left join gc_comex_produtos ComexProduto on (ComexProduto.id_comex_produto = ItemInvoice.id_comex_produto) " +
                                " where (ItemInvoice.ativo = 1) and (ItemInvoice.id_comex_produto > 0) and (ItemImportacao.ativo = true) and (ComexImportacoes.ativo = true) and (ComexInvoices.ativo = true) " +
                                " and ComexImportacoes.data_registro >= '" + DataHoraAtual.AddDays(-60).ToString("yyyy-MM-dd") + " 00:00:00" + "'";
                if (param.yesCustomField01.EmptyIfNull().ToString().Length > 1) { SentencaSQL += " and (ItemInvoice.customer = '" + param.yesCustomField01.EmptyIfNull().ToString() + "') "; }
                if (param.yesCustomField02.EmptyIfNull().ToString().Length > 1) { SentencaSQL += " and (ItemInvoice.note = '" + param.yesCustomField02.EmptyIfNull().ToString() + "') "; }
                SentencaSQL += " order by ItemInvoice.id_invoice_item ";
                TableItens = LibDB.GetDataTable(SentencaSQL, db);
                RowsItens = TableItens.AsEnumerable().ToList();
                allRecords = TableItens.AsEnumerable().ToList();
            }
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);

            List<string[]> list = new List<string[]>();
            foreach (var RowItem in displayedRecords)
            {
                if (IdItemView != RowItem["id_invoice_item"].EmptyIfNull().ToString())
                {
                    IdItemView = RowItem["id_invoice_item"].EmptyIfNull().ToString();
                    Decimal ItemQty = 0;
                    Decimal ItemUnitPrice = 0;
                    Decimal ItemValorUnit = 0;
                    Decimal ItemValorTotal = 0;
                    Decimal.TryParse(RowItem["item_qty"].EmptyIfNull().ToString(), out ItemQty);
                    Decimal.TryParse(RowItem["item_unit_price"].EmptyIfNull().ToString(), out ItemUnitPrice);
                    Decimal.TryParse(RowItem["valor_unit"].EmptyIfNull().ToString(), out ItemValorUnit);
                    Decimal.TryParse(RowItem["valor_total"].EmptyIfNull().ToString(), out ItemValorTotal);
                    String ItemDescricao = RowItem["descricao"].EmptyIfNull().ToString();
                    if (RowItem["description"].EmptyIfNull().ToString().Length > 0) { ItemDescricao += "   (" + RowItem["description"].EmptyIfNull().ToString() + ")"; };
                    if (RowItem["serial_number"].EmptyIfNull().ToString().Length > 0) { ItemDescricao += "<br/>[Serial: " + RowItem["serial_number"].EmptyIfNull().ToString() + "]"; };
                    String Cliente = RowItem["customer"].EmptyIfNull().ToString().Replace("GDI IMPORTACAO E COMERCIO DE PECAS AERONAUTICAS", "GDI");
                    if (RowItem["note"].EmptyIfNull().ToString().Length > 0) { Cliente += "<br/>[OS: " + RowItem["note"].EmptyIfNull().ToString() + "]"; };
                    String ItemPreco = "US$ " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ItemUnitPrice).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/>R$ " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ItemValorUnit).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                    list.Add(new[] {
                                    "", // Botão seleção
                                    RowItem["id_invoice_item"].EmptyIfNull().ToString(),
                                    ItemQty.EmptyIfNull().ToString().Replace(",00",""),
                                    ItemDescricao,
                                    ItemPreco,
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ItemValorTotal).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                                    Cliente
                                });
                }
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

        public void PreencherLookupsEspelhoDigital()
        {
            String SentencaSQL = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            SentencaSQL = " select distinct(ItemInvoice.note)  " +
                            " from gc_comex_invoices_itens ItemInvoice " +
                            " left join gc_comex_importacoes_itens ItemImportacao on (ItemImportacao.id_comex_produto = ItemInvoice.id_comex_produto) " +
                            " left join gc_comex_importacoes ComexImportacoes on (ComexImportacoes.id_importacao = ItemInvoice.id_importacao and ItemImportacao.id_importacao = ItemInvoice.id_importacao) " +
                            " left join gc_comex_invoices ComexInvoices on (ComexInvoices.id_invoice = ItemInvoice.id_invoice) " +
                            " left join gc_comex_produtos ComexProduto on (ComexProduto.id_comex_produto = ItemInvoice.id_comex_produto) " +
                            " where (ItemInvoice.ativo = 1) and (ItemInvoice.id_comex_produto > 0) and (ItemImportacao.ativo = true) and (ComexImportacoes.ativo = true) and (ComexInvoices.ativo = true) " +
                            " and ComexImportacoes.data_registro >= '" + DataHoraAtual.AddDays(-60).ToString("yyyy-MM-dd") + " 00:00:00" + "'" +
                            " order by ItemInvoice.note ";
            DataTable TableNotes = LibDB.GetDataTable(SentencaSQL, db);
            List<DataRow> RowsNotes = TableNotes.AsEnumerable().ToList();
            var comboNotes = new List<SelectListItem>();
            comboNotes.Add(new SelectListItem { Value = "0", Text = "[ ORDER ]" });
            foreach (var RowNote in RowsNotes)
            {
                String Note = RowNote["note"].EmptyIfNull().ToString().Trim();
                comboNotes.Add(new SelectListItem { Value = Note, Text = Note });
            }
            ViewBag.comboNotes = comboNotes;

            SentencaSQL = SentencaSQL.Replace("ItemInvoice.note", "ItemInvoice.customer");
            DataTable TableClientes = LibDB.GetDataTable(SentencaSQL, db);
            List<DataRow> RowsClientes = TableClientes.AsEnumerable().ToList();
            var comboClientes = new List<SelectListItem>();
            comboClientes.Add(new SelectListItem { Value = "0", Text = "[ CLIENTE ]" });
            foreach (var RowClientes in RowsClientes)
            {
                String Cliente = RowClientes["customer"].EmptyIfNull().ToString().Trim();
                if (Cliente.Length > 3) { comboClientes.Add(new SelectListItem { Value = Cliente, Text = Cliente }); }
            }
            ViewBag.comboClientes = comboClientes;
        }

        [HttpPost]
        public ActionResult AjaxCarregarItensImportacao(gc_movimentos record_g_movimento)
        {
            bool Sucesso = false;
            int QtdItensCadastrados = 0;
            string MsgRetorno = String.Empty;
            string[] ListaIds = null;
            string[] ListaSeriais = null;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                try { ListaIds = record_g_movimento.informacoes_adicionais.EmptyIfNull().ToString().Split(','); } catch (Exception) { ListaIds = new string[1] { "" }; };
                if (ListaIds.Count() > 0)
                {
                    foreach (String Indentificador in ListaIds)
                    {
                        if (Indentificador.EmptyIfNull().ToString().Length > 0)
                        {
                            int IdInvoiceItem = 0;
                            int.TryParse(Indentificador, out IdInvoiceItem);
                            int IdProduto = 0;

                            if (IdInvoiceItem > 0)
                            {
                                gc_comex_invoices_itens record_gc_comex_invoices_itens = db.gc_comex_invoices_itens.Find(IdInvoiceItem);
                                IdProduto = db.gc_comex_produtos.Find(record_gc_comex_invoices_itens.id_comex_produto).id_produto;
                                gc_comex_importacoes_itens record_gc_comex_importacoes_itens = db.gc_comex_importacoes_itens.Where(i => i.id_comex_produto == record_gc_comex_invoices_itens.id_comex_produto && i.id_importacao == record_gc_comex_invoices_itens.id_importacao).FirstOrDefault();
                                if (IdProduto > 0)
                                {
                                    //try { ListaSeriais = record_gc_comex_invoices_itens.serial_number.EmptyIfNull().ToString().Split(','); } catch (Exception) { ListaSeriais = new string[1] {"0"}; };
                                    if (record_gc_comex_invoices_itens.serial_number.EmptyIfNull().ToString().Length > 0) { ListaSeriais = record_gc_comex_invoices_itens.serial_number.EmptyIfNull().ToString().Split(','); } else { ListaSeriais = new string[1] { "0" }; };
                                    foreach (String SerialNumber in ListaSeriais)
                                    {
                                        if (SerialNumber.Trim().Length > 0)
                                        {
                                            gc_movimentos_itens record_gc_movimentos_itens = new Db.gc_movimentos_itens();
                                            record_gc_movimentos_itens.id_movimento = record_g_movimento.id_movimento;
                                            record_gc_movimentos_itens.id_produto = IdProduto;
                                            record_gc_movimentos_itens.id_produto_condicao = 1;
                                            record_gc_movimentos_itens.id_entrega_prazo = 1;
                                            record_gc_movimentos_itens.sequencia = 1;
                                            // Tratativa do número serial
                                            if ((SerialNumber.Trim().Length > 0) && (SerialNumber.Trim() != "0"))
                                            {
                                                record_gc_movimentos_itens.serial = SerialNumber;
                                                record_gc_movimentos_itens.quantidade = 1;
                                            }
                                            else
                                            {
                                                record_gc_movimentos_itens.serial = null;
                                                record_gc_movimentos_itens.quantidade = Math.Truncate(record_gc_comex_importacoes_itens.quantidade);
                                            };
                                            record_gc_movimentos_itens.valor_unit = Math.Round(record_gc_comex_importacoes_itens.valor_unit, 2);
                                            record_gc_movimentos_itens.valor_total = Math.Round(record_gc_movimentos_itens.quantidade * record_gc_movimentos_itens.valor_unit, 2);
                                            record_gc_movimentos_itens.valor_unit_corecharge = 0;
                                            record_gc_movimentos_itens.valor_total_corecharge = 0;
                                            record_gc_movimentos_itens.obs = null;
                                            record_gc_movimentos_itens.obs_nf = false;

                                            record_gc_movimentos_itens.lote01_identificador = null;
                                            db.gc_movimentos_itens.Add(record_gc_movimentos_itens);
                                            QtdItensCadastrados += 1;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (QtdItensCadastrados > 0)
                    {
                        db.SaveChanges();
                        Sucesso = true;
                        MsgRetorno += QtdItensCadastrados.ToString() + " Itens carregados com Sucesso!";
                    }
                    else
                    {
                        Sucesso = false;
                        MsgRetorno += "Nenhum item carregado!";
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Anexos - Modal UploadFile
        public ActionResult ModalUploadFilePedidos(int? IdMovimento)
        {
            cstUploadGed record_cstUploadGed = new cstUploadGed();
            {
                record_cstUploadGed.isCotacaoPedido = true;
                record_cstUploadGed.id_gc_movimento = IdMovimento.GetValueOrDefault();
                var ComboGedTipos = new List<SelectListItem>();
                ComboGedTipos.Add(new SelectListItem { Value = "0", Text = "[ SELECIONE O TIPO DO ANEXO ]" });
                List<ged_arquivos_tipos> ListaGedTipos = db.ged_arquivos_tipos.Where(g => g.ativo == true && g.link_pedido == true).OrderBy(p => p.descricao).ToList();
                foreach (var RecordGedTipo in ListaGedTipos) { ComboGedTipos.Add(new SelectListItem { Value = RecordGedTipo.id_arquivo_tipo.ToString(), Text = RecordGedTipo.descricao }); };
                ViewBag.ComboGedTipos = ComboGedTipos;
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Documentos</b>";
            }
            return View(record_cstUploadGed);
        }
        #endregion

        #region Pedido - Anexos - View (Modal)
        public ActionResult ModalPedidoViewAnexos(int? id, string tag)
        {
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(id);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-paperclip", "" , "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Anexos do Pedido Nº " + RecordMovimento.id_movimento.EmptyIfNull().ToString()+ "</b>";
            return View(RecordMovimento);            
        }
        #endregion

        #region Pedido - Notificação Cliente
        public ActionResult ModalNotificacaoCliente(int? id)
        {
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            String MsgBloqueio = string.Empty;
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(id);
            if (RecordMovimento != null)
            {
                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(RecordMovimento.id_cfop_operacao);
                if (RecordCfopOperacao.has_notifica_email == false)  
                { 
                    MsgBloqueio += "<b> ---------- O TIPO DE PEDIDO ["+ RecordCfopOperacao.descricao.EmptyIfNull().ToString().Trim() + "] NÃO POSSUI PARAMETRIZAÇÃO DE NOTIFICAÇÃO ----------</b>" + "<br/>"; 
                } 
                else
                {
                    if ((RecordCfopOperacao.has_aprovacao == true) && (RecordMovimento.movimento_aprovado == false)) { MsgBloqueio += " - Pedido não foi APROVADO!<br/>"; }
                    if ((RecordCfopOperacao.has_separacao == true) && (RecordMovimento.movimento_separado == false)) { MsgBloqueio += " - Pedido não foi SEPARADO!<br/>"; }
                    if ((RecordCfopOperacao.has_financeiro == true) && (RecordMovimento.movimento_faturado == false)) { MsgBloqueio += " - Pedido não foi FATURADO!<br/>"; }
                    if ((RecordCfopOperacao.has_nfe == true) && (RecordMovimento.movimento_nf_autorizada == false)) { MsgBloqueio += " - Pedido não possui NFe Autorizada!<br/>"; }
                }

                g_clientes RecordCliente = db.g_clientes.Find(RecordMovimento.id_cliente);
                if (RecordCliente != null)
                {
                    if (RecordCliente.email_notificacao.EmptyIfNull().ToString().Trim().Length > 0)
                    {
                        if (RecordMovimento.notifica_contatos_emails.EmptyIfNull().ToString().Trim().Length > 0) { RecordMovimento.notifica_contatos_emails += ";"; };
                        RecordMovimento.notifica_contatos_emails += RecordCliente.email_notificacao.EmptyIfNull().ToString().Trim();
                    }
                }
            }
            else
            {
                MsgBloqueio = "Movimento [" + id.ToString() + "] não localizado no ERP";
            }
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-paper-plane", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>" + "Notificar Cliente - Pedido Nº " + RecordMovimento.id_movimento.ToString() + "</b>";
            ViewBag.comboClientesContatos = LibDataSets.LoadComboGcClientesContatos(db, RecordMovimento.id_cliente);
            ViewBag.MsgBloqueio = MsgBloqueio;
            return View("ModalNotificacaoCliente", RecordMovimento);
        }

        [HttpPost]
        public ActionResult AjaxModalNotificacaoCliente(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String LogAlteracoes = string.Empty;
            String MsgRetorno = string.Empty;
            try
            {
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);

                if (view_record_gc_movimento.contato_nome.EmptyIfNull().ToString().Length == 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += "Campo [Pessoa de Contato] é de preenchimento obrigatório!" + "<br/>";
                }
                if (view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Length == 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += "Campo [Celular de Contato] é de preenchimento obrigatório!" + "<br/>";
                }
                else
                {
                    String Telefone = LibStringFormat.SomenteNumeros(view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Trim());
                    if ((Telefone.Length != 10) && (Telefone.Length != 11))
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "Campo <b>Celular/Telefone</b> deverá conter a seguinte formatação DDNNNNNNNNN onde os 2 primeiros dígitos deverão ser o DDD e os dígitos seguintes o número do telefone ou celular com 8 ou 9 dígitos!";
                    }
                }
                if (view_record_gc_movimento.contato_email.EmptyIfNull().ToString().Length == 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += "Campo [Email de Contato] é de preenchimento obrigatório!" + "<br/>";
                }
                else
                {
                    if (LibStringValidate.ValidarEmail(view_record_gc_movimento.contato_email.EmptyIfNull().ToString()) == false)
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "Campo [Email de Contato] contém um email inválido!" + "<br/>";
                    }
                }

                if (qtdInconsistencias == 0)
                {

                    // Dados do Contato
                    RecordMovimento.notifica_contatos_emails = view_record_gc_movimento.notifica_contatos_emails;
                    RecordMovimento.contato_nome = view_record_gc_movimento.contato_nome;
                    RecordMovimento.contato_telefone = view_record_gc_movimento.contato_telefone;
                    RecordMovimento.contato_email = view_record_gc_movimento.contato_email;

                    if (view_record_gc_movimento.id_contato > 0) // Usuário Selecionou o Contato
                    {
                        g_clientes_contatos RecordContato = db.g_clientes_contatos.Find(view_record_gc_movimento.id_contato);
                        RecordMovimento.id_contato = view_record_gc_movimento.id_contato;

                        if ((RecordContato.telefone != view_record_gc_movimento.contato_telefone) || (RecordContato.email != view_record_gc_movimento.contato_email))
                        {
                            String LogAlteracao = string.Empty;
                            LogAlteracao = "Alteração dados do contato | " + LogAlteracao;
                            LogAlteracao += "Contato: " + RecordContato.contato.EmptyIfNull().ToString().Trim() + " | ";
                            if (RecordContato.telefone.EmptyIfNull().ToString().Trim() != view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Telefone: " + RecordContato.telefone.EmptyIfNull().ToString().Trim() + " > " + view_record_gc_movimento.contato_telefone.EmptyIfNull().ToString().Trim() + " | "; };
                            if (RecordContato.email.EmptyIfNull().ToString().Trim() != view_record_gc_movimento.contato_email.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Email: " + RecordContato.email.EmptyIfNull().ToString().Trim() + " > " + view_record_gc_movimento.contato_email.EmptyIfNull().ToString().Trim() + " | "; };
                            LibAudit.SaveAudit(db, false, "g_clientes", RecordMovimento.id_cliente, LogAlteracao.EmptyIfNull());
                            RecordContato.contato = view_record_gc_movimento.contato_nome;
                            RecordContato.telefone = view_record_gc_movimento.contato_telefone;
                            RecordContato.email = view_record_gc_movimento.contato_email;
                            RecordContato.datahora_alteracao = DataHoraAtual;
                            RecordContato.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordContato).State = EntityState.Modified;
                            db.SaveChanges();
                            RecordMovimento.id_contato = view_record_gc_movimento.id_contato;
                        }
                    }
                    else
                    {
                        g_clientes_contatos RecordContatoNovo = new g_clientes_contatos();
                        RecordContatoNovo.id_cliente = RecordMovimento.id_cliente;
                        RecordContatoNovo.ativo = true;
                        RecordContatoNovo.contato = view_record_gc_movimento.contato_nome;
                        RecordContatoNovo.telefone = view_record_gc_movimento.contato_telefone;
                        RecordContatoNovo.email = view_record_gc_movimento.contato_email;
                        RecordContatoNovo.id_contato_tipo = 1;
                        RecordContatoNovo.id_coligada = 0; // Global
                        RecordContatoNovo.id_filial = 0; // Global
                        RecordContatoNovo.datahora_cadastro = DataHoraAtual;
                        RecordContatoNovo.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        db.Entry(RecordContatoNovo).State = EntityState.Added;

                        // Audit Novo Contato
                        String TipoContato = String.Empty;
                        List<Db.g_clientes_contatos_tipos> AllContatosTipos = db.g_clientes_contatos_tipos.Where(c => c.ativo == true).ToList();
                        g_clientes_contatos_tipos RecordTipoContato = AllContatosTipos.Where(t => t.id_contato_tipo == RecordContatoNovo.id_contato_tipo).FirstOrDefault();
                        if (RecordTipoContato != null) { TipoContato = RecordTipoContato.nome.EmptyIfNull().ToString(); };
                        String LogAlteracaoContato = "Novo contato | ";
                        LogAlteracaoContato += "Tipo: " + TipoContato + " | ";
                        LogAlteracaoContato += "Contato: " + RecordContatoNovo.contato.EmptyIfNull().ToString().Trim() + " | ";
                        LogAlteracaoContato += "Setor: " + RecordContatoNovo.setor.EmptyIfNull().ToString().Trim() + " | ";
                        LogAlteracaoContato += "Telefone: " + RecordContatoNovo.telefone.EmptyIfNull().ToString().Trim() + " | ";
                        LogAlteracaoContato += "Email: " + RecordContatoNovo.email.EmptyIfNull().ToString().Trim() + " | ";
                        if (LogAlteracaoContato.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true, "g_clientes", RecordContatoNovo.id_cliente, LogAlteracaoContato.EmptyIfNull()); };
                    }

                    RecordMovimento.datahora_alteracao = DataHoraAtual;
                    RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordMovimento).State = EntityState.Modified;
                    db.SaveChanges();


                    // Anexos do Email
                    List<string> ListaAnexosEmail = GetAnexosPedido(RecordMovimento.id_movimento);

                    // Notificações por Email
                    g_clientes RecordCliente = db.g_clientes.Find(RecordMovimento.id_cliente);
                    String BodyEmail = string.Empty;
                    String EmailsNotificar = string.Empty;
                    String ClienteNome = string.Empty;
                    String ClienteCodigo = RecordMovimento.id_cliente.EmptyIfNull().ToString().Trim();
                    String ClienteDocumento = string.Empty;
                    String PedidoNumero = RecordMovimento.id_movimento.EmptyIfNull().ToString();
                    String LinkPortalDireto = string.Empty;
                    String TituloEmail = "GDI Aviação - Faturamento do Pedido Nº " + RecordMovimento.id_movimento.EmptyIfNull().ToString();
                    if (RecordCliente != null) { ClienteNome = RecordCliente.razao_social.EmptyIfNull().ToString().Trim().ToUpperInvariant();  TituloEmail += " - " + ClienteNome;  };
                    g_templates RecordTemplate = db.g_templates.Where(t => t.localizador == "GcMovimentoPedidoFaturadoCliente").FirstOrDefault();
                    if (RecordTemplate != null) { BodyEmail = RecordTemplate.template.EmptyIfNull().ToString(); }
                    else { MsgRetorno += "Template de email não localizado!" + "<br/>"; qtdInconsistencias += 1; };
                    if (RecordCliente.cpf.EmptyIfNull().ToString().Length > 0) { ClienteDocumento = LibStringFormat.SomenteNumeros(RecordCliente.cpf.EmptyIfNull().ToString()); }
                    else if (RecordCliente.cnpj.EmptyIfNull().ToString().Length > 0) { ClienteDocumento = LibStringFormat.SomenteNumeros(RecordCliente.cnpj.EmptyIfNull().ToString()); }

                    BodyEmail = BodyEmail.Replace("[NumeroPedido]", PedidoNumero);
                    BodyEmail = BodyEmail.Replace("[CodigoCliente]", ClienteCodigo);
                    BodyEmail = BodyEmail.Replace("[NomeCliente]", ClienteNome);
                    BodyEmail = BodyEmail.Replace("[DocumentoCliente]", ClienteDocumento);
                    // Link para o portal do cliente servido por este mesmo ERP (GDI-PortalCliente-Plataform descontinuado)
                    LinkPortalDireto = "https://portalflightx.com/UserIdentity/AcessoPortal?codigocliente=" + ClienteCodigo + "&documentocliente=" + ClienteDocumento;
                    BodyEmail = BodyEmail.Replace("[LinkPortalDireto]", LinkPortalDireto);

                    RecordMovimento.notifica_contatos_emails = view_record_gc_movimento.notifica_contatos_emails;
                    RecordMovimento.contato_nome = view_record_gc_movimento.contato_nome;
                    RecordMovimento.contato_telefone = view_record_gc_movimento.contato_telefone;
                    RecordMovimento.contato_email = view_record_gc_movimento.contato_email;

                    EmailsNotificar += RecordMovimento.notifica_contatos_emails.EmptyIfNull().ToString().Trim().ToLowerInvariant();

                    if (RecordMovimento.contato_email.EmptyIfNull().ToString().Trim().Length > 0)
                    {
                        RecordMovimento.contato_email = RecordMovimento.contato_email.ToLowerInvariant();
                        if (EmailsNotificar.EmptyIfNull().ToString().Trim().IndexOf(RecordMovimento.contato_email.EmptyIfNull().ToString().Trim()) < 0)
                        {
                            if (EmailsNotificar.EmptyIfNull().ToString().Trim().Length > 0) { EmailsNotificar += ";"; };
                            EmailsNotificar += RecordMovimento.contato_email.EmptyIfNull().ToString().Trim();
                        }
                    }

                    if (RecordMovimento.id_vendedor > 0)
                    {
                        g_vendedores RecordVendedor = db.g_vendedores.Find(RecordMovimento.id_vendedor);
                        if (RecordVendedor != null)
                        {
                            if (RecordVendedor.email.EmptyIfNull().ToString().Trim().Length > 0)
                            {
                                if (EmailsNotificar.EmptyIfNull().ToString().Trim().Length > 0) { EmailsNotificar += ";"; };
                                EmailsNotificar += RecordVendedor.email.EmptyIfNull().ToString().Trim();
                            }
                        }
                    }
                    if (CachePersister.userIdentity.AmbienteDatabase == "Homologação") { EmailsNotificar = "informatica@gdiaviacao.com"; };

                    if (EmailsNotificar.EmptyIfNull().ToString().Trim().Length == 0) 
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "Não foram informados Emails a serem notificados!" + "<br/>";
                    }
                    else
                    {
                        String ParamFromEmail = "faturamento@gdiaviacao.com";
                        String ParamFromNome = "GDI Aviação - Departamento Financeiro";
                        BotAwsEmail RoboAwsEmail = new BotAwsEmail();
                        RoboAwsEmail.EnviarEmailAWS(ParamFromEmail, ParamFromNome, EmailsNotificar, EmailsNotificar, TituloEmail, BodyEmail, ListaAnexosEmail);
                        Sucesso = true;

                        MsgRetorno = string.Empty;
                        MsgRetorno += "Cliente <b>" + ClienteNome + "</b> notificado com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        MsgRetorno += "<b>Contato notificado:</b> " + RecordMovimento.contato_nome.EmptyIfNull().ToString() + "<br/>";
                        MsgRetorno += "<b>Emails notificados:</b> " + EmailsNotificar.Replace(";","<br/>" ) + "<br/>";

                        LogAlteracoes += "Notificação Faturamento Pedido | ";
                        LogAlteracoes += "Pessoa de contato: " + RecordMovimento.contato_nome.EmptyIfNull().ToString() + " | ";
                        LogAlteracoes += "Telefone do contato: " + RecordMovimento.contato_telefone.EmptyIfNull().ToString() + " | ";
                        LogAlteracoes += "Email do contato: " + RecordMovimento.contato_email.EmptyIfNull().ToString() + " | ";
                        LogAlteracoes += "Email notificações: " + RecordMovimento.notifica_contatos_emails.EmptyIfNull().ToString() + " | ";

                        // Notificação WhatsApp
                        if (RecordMovimento.contato_telefone.EmptyIfNull().ToString().Trim().Length > 0)
                        {
                            String NotificacaoCelular = RecordMovimento.contato_telefone.EmptyIfNull().ToString().Trim();
                            String NotificacaoMensagem = "Prezado(a) " + RecordMovimento.contato_nome.ToUpperInvariant() + ", seu pedido nº " + PedidoNumero + " foi faturado com sucesso!" + "\r\n\r\n" + "Clique no link dessa mensagem e acesse a Nota Fiscal e Boletos" + "\r\n\r\n";
                            String NotificacaoTitle = "✈️ GDI Aviação - Faturamento do Pedido";
                            try
                            {
                                RoboWhatsApp RoboWhatsapp = new RoboWhatsApp();
                                RoboWhatsapp.EnviarTextoLinkWhatsApp(NotificacaoCelular, NotificacaoMensagem, LinkPortalDireto, NotificacaoTitle);
                                LogAlteracoes += "WhatsAPP notificado: " + RecordMovimento.contato_telefone.EmptyIfNull().ToString().Trim()  + "  | ";
                                MsgRetorno += "<b>WhatsAPP notificado:</b> " + RecordMovimento.contato_telefone.EmptyIfNull().ToString().Trim() + "<br/><br/>";
                            }
                            catch (Exception ex)
                            {
                                LogAlteracoes += "Erro notificação WhatsAPP Nº  " + RecordMovimento.contato_telefone.EmptyIfNull().ToString().Trim() + " - "  + LibExceptions.getExceptionShortMessage(ex) + " | ";
                                MsgRetorno += "Erro notificação WhatsAPP Nº  " + RecordMovimento.contato_telefone.EmptyIfNull().ToString().Trim() + " - " + LibExceptions.getExceptionShortMessage(ex) + "<br/>";
                            }
                        }

                        if (ListaAnexosEmail.Count > 0)
                        {
                            LogAlteracoes += "Arquivos anexados no e-mail: ";
                            MsgRetorno += "<b>Arquivos anexados no e-mail:</b> ";
                            foreach (String FileNameAnexo in ListaAnexosEmail)
                            {
                                String FileName = Path.GetFileName(FileNameAnexo);
                                LogAlteracoes += FileName + ", ";
                                MsgRetorno += FileName + "<br/>";
                            }
                            LogAlteracoes += " | ";
                        }

                        if (Sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", RecordMovimento.id_movimento, LogAlteracoes); }; };

                        RecordMovimento.movimento_notificado = true;
                        RecordMovimento.id_usuario_notificacao = CachePersister.userIdentity.IdUsuario;
                        RecordMovimento.datahora_notificacao = DataHoraAtual;
                        RecordMovimento.datahora_alteracao = DataHoraAtual;
                        RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(RecordMovimento).State = EntityState.Modified;
                        db.SaveChanges();

                        LibCache.LiberarMemoria();
                        // Apagar arquivos temporários do disco
                        if (ListaAnexosEmail != null)
                        {
                            if (ListaAnexosEmail.Count > 0)
                            {
                                foreach (String FileNameAnexo in ListaAnexosEmail)
                                {
                                    if (System.IO.File.Exists(FileNameAnexo))
                                    {
                                        try { System.IO.File.Delete(FileNameAnexo); } catch(Exception ex) { String texto = LibExceptions.getExceptionShortMessage(ex); };
                                    }
                                }
                            }
                        }
                    }

                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Função - Duplicar Movimento
        public ActionResult ModalPedidoDuplicar(int? id)
        {
            int temp = id.GetValueOrDefault();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String TitleModal = string.Empty;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            if (record_gc_movimento != null)
            {
                TitleModal = LibIcons.getIcon("fa-solid fa-copy", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Duplicar Movimento Cotação/Pedido/OS Nº " + record_gc_movimento.id_movimento.ToString();
            }
            ViewBag.Title = TitleModal;
            return View("ModalPedidoDuplicar", record_gc_movimento);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoDuplicar(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String MsgRetorno = "";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (qtdInconsistencias == 0)
                {
                    gc_movimentos record_gc_movimento_origem = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);
                    List<gc_movimentos_itens> ListaGCMovimentosItens = db.gc_movimentos_itens.Where(i => i.id_movimento == view_record_gc_movimento.id_movimento).OrderBy(i => i.sequencia).ToList();

                    gc_movimentos record_gc_movimento_destino = new gc_movimentos();
                    record_gc_movimento_destino.id_movimento_status = 1;
                    record_gc_movimento_destino.id_local_estoque = record_gc_movimento_origem.id_local_estoque;
                    record_gc_movimento_destino.id_movimento_tipo = record_gc_movimento_origem.id_movimento_tipo;
                    record_gc_movimento_destino.id_moeda = record_gc_movimento_origem.id_moeda;
                    record_gc_movimento_destino.id_cfop_finalidade = record_gc_movimento_origem.id_cfop_finalidade;
                    record_gc_movimento_destino.id_cfop_operacao = record_gc_movimento_origem.id_cfop_operacao;
                    record_gc_movimento_destino.id_cliente = record_gc_movimento_origem.id_cliente;
                    record_gc_movimento_destino.id_contato = record_gc_movimento_origem.id_contato;
                    record_gc_movimento_destino.contato_nome = record_gc_movimento_origem.contato_nome;
                    record_gc_movimento_destino.contato_telefone = record_gc_movimento_origem.contato_telefone;
                    record_gc_movimento_destino.contato_email = record_gc_movimento_origem.contato_email;
                    record_gc_movimento_destino.id_cliente_destinatario = record_gc_movimento_origem.id_cliente_destinatario;
                    record_gc_movimento_destino.id_vendedor = record_gc_movimento_origem.id_vendedor;
                    record_gc_movimento_destino.id_frete_responsavel = record_gc_movimento_origem.id_frete_responsavel;
                    record_gc_movimento_destino.data_vencimento = DataHoraAtual.AddDays(7);
                    record_gc_movimento_destino.id_pagrec_condicao = record_gc_movimento_origem.id_pagrec_condicao;
                    record_gc_movimento_destino.documento_numero = record_gc_movimento_origem.documento_numero;
                    record_gc_movimento_destino.nf_numero = "0";
                    record_gc_movimento_destino.nf_serie = "0";
                    record_gc_movimento_destino.oc_numero = record_gc_movimento_origem.oc_numero;
                    record_gc_movimento_destino.icms_difal_calculado = record_gc_movimento_origem.icms_difal_calculado;
                    record_gc_movimento_destino.icms_difal_pagar = record_gc_movimento_origem.icms_difal_pagar;
                    record_gc_movimento_destino.frete_valor = record_gc_movimento_origem.frete_valor;
                    record_gc_movimento_destino.frete_gerencial = record_gc_movimento_origem.frete_gerencial;
                    record_gc_movimento_destino.frete_observacoes = record_gc_movimento_origem.frete_observacoes;
                    record_gc_movimento_destino.frete1_custo = record_gc_movimento_origem.frete1_custo;
                    record_gc_movimento_destino.frete1_transportadora = record_gc_movimento_origem.frete1_transportadora;
                    record_gc_movimento_destino.frete1_rastreio = record_gc_movimento_origem.frete1_rastreio;
                    record_gc_movimento_destino.frete1_documento = record_gc_movimento_origem.frete1_documento;
                    record_gc_movimento_destino.frete2_custo = record_gc_movimento_origem.frete2_custo;
                    record_gc_movimento_destino.frete2_transportadora = record_gc_movimento_origem.frete2_transportadora;
                    record_gc_movimento_destino.frete2_rastreio = record_gc_movimento_origem.frete2_rastreio;
                    record_gc_movimento_destino.frete2_documento = record_gc_movimento_origem.frete2_documento;
                    record_gc_movimento_destino.qtd_itens = record_gc_movimento_origem.qtd_itens;
                    record_gc_movimento_destino.qtd_produtos = record_gc_movimento_origem.qtd_produtos;
                    record_gc_movimento_destino.valor_total_produtos = record_gc_movimento_origem.valor_total_produtos;
                    record_gc_movimento_destino.valor_total_bruto = record_gc_movimento_origem.valor_total_bruto;
                    record_gc_movimento_destino.valor_total_liquido = record_gc_movimento_origem.valor_total_liquido;
                    record_gc_movimento_destino.markup = record_gc_movimento_origem.markup;
                    record_gc_movimento_destino.valor_total_corecharge = record_gc_movimento_origem.valor_total_corecharge;
                    record_gc_movimento_destino.valor_total_adiantamento = record_gc_movimento_origem.valor_total_adiantamento;
                    record_gc_movimento_destino.transportadora_cotacao = record_gc_movimento_origem.transportadora_cotacao;
                    record_gc_movimento_destino.cotacao_dolar_venda = record_gc_movimento_origem.cotacao_dolar_venda;
                    record_gc_movimento_destino.cotacao_dolar_oficial_venda = record_gc_movimento_origem.cotacao_dolar_oficial_venda;
                    record_gc_movimento_destino.comissao1_vendedor = record_gc_movimento_origem.comissao1_vendedor;
                    record_gc_movimento_destino.comissao1_percentual = record_gc_movimento_origem.comissao1_percentual;
                    record_gc_movimento_destino.comissao1_valor = record_gc_movimento_origem.comissao1_valor;
                    record_gc_movimento_destino.aeronave_prefixo = record_gc_movimento_origem.aeronave_prefixo;
                    record_gc_movimento_destino.has_beneficio_aviacao = record_gc_movimento_origem.has_beneficio_aviacao;
                    record_gc_movimento_destino.param_reducao_bc = record_gc_movimento_origem.param_reducao_bc;
                    record_gc_movimento_destino.id_coligada = record_gc_movimento_origem.id_coligada;
                    record_gc_movimento_destino.id_filial = record_gc_movimento_origem.id_filial;
                    record_gc_movimento_destino.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    record_gc_movimento_destino.datahora_cadastro = DataHoraAtual;
                    record_gc_movimento_destino.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    record_gc_movimento_destino.datahora_alteracao = DataHoraAtual;
                    db.gc_movimentos.Add(record_gc_movimento_destino);
                    db.SaveChanges();

                    // Criar o log do Movimento
                    String LogAlteracoes = "Clone Cotação/Pedido Nº " + record_gc_movimento_origem.id_movimento.ToString() + " para a Cotação/Pedido Nº " + record_gc_movimento_destino.id_movimento.ToString();
                    LibAudit.SaveAudit(db, true, "gc_movimentos", record_gc_movimento_origem.id_movimento, LogAlteracoes);
                    LibAudit.SaveAudit(db, true, "gc_movimentos", record_gc_movimento_destino.id_movimento, LogAlteracoes);

                    foreach (gc_movimentos_itens ItemMovimento in ListaGCMovimentosItens)
                    {
                        gc_movimentos_itens record_gc_movimento_item = new gc_movimentos_itens();
                        record_gc_movimento_item = LibDB.CloneTObject(ItemMovimento);
                        record_gc_movimento_item.id_movimento_item = 0;
                        record_gc_movimento_item.id_movimento = record_gc_movimento_destino.id_movimento;
                        record_gc_movimento_item.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        record_gc_movimento_item.datahora_cadastro = DataHoraAtual;
                        db.gc_movimentos_itens.Add(record_gc_movimento_item);
                    }
                    db.SaveChanges();

                    MsgRetorno = "Novo Movimento Nº <b> " + record_gc_movimento_destino.id_movimento.EmptyIfNull().ToString() + "</b> copiado com Sucesso" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Função - Converter Moeda Movimento
        public ActionResult ModalPedidoConverterMoeda(int? id)
        {
            int temp = id.GetValueOrDefault();
            String MsgBloqueio = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String TitleModal = string.Empty;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            if (record_gc_movimento != null)
            {
                TitleModal = LibIcons.getIcon("fa-solid fa-dollar-sign", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Converter Moeda - Cotação/Pedido/OS Nº " + record_gc_movimento.id_movimento.ToString();
                if ((record_gc_movimento.id_moeda != 1) && (record_gc_movimento.id_moeda != 2))
                {
                    MsgBloqueio = "Moeda do pedido não Identificada!";
                };
            }
            ViewBag.Title = TitleModal;
            ViewBag.MsgBloqueio = MsgBloqueio;

            record_gc_movimento.cotacao_dolar_venda = SetCotacaoDollarDia();

            return View("ModalPedidoConverterMoeda", record_gc_movimento);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoConverterMoeda(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            int IdMoedaOrigem = 0;
            String MsgRetorno = "";
            Decimal ValorTotalProdutos = 0;
            Decimal ValorTotalCoreCharge = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if ((view_record_gc_movimento.cotacao_dolar_venda <= 1) || (view_record_gc_movimento.cotacao_dolar_venda > 100))
                {
                    MsgRetorno = "Valor da cotação fora da faixa!";
                    qtdInconsistencias += 1;
                }
                if (qtdInconsistencias == 0)
                {
                    gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);
                    gc_movimentos OldMovimento = LibDB.CloneTObject(record_gc_movimento);

                    List<gc_movimentos_itens> ListaGCMovimentosItens = db.gc_movimentos_itens.Where(i => i.id_movimento == view_record_gc_movimento.id_movimento).OrderBy(i => i.sequencia).ToList();

                    IdMoedaOrigem = record_gc_movimento.id_moeda;

                    foreach (gc_movimentos_itens record_gc_movimento_item in ListaGCMovimentosItens)
                    {
                        if (IdMoedaOrigem == 1) // Moeda Real converter para Dolar
                        {
                            record_gc_movimento_item.valor_unit = (record_gc_movimento_item.valor_unit / view_record_gc_movimento.cotacao_dolar_venda);
                            record_gc_movimento_item.valor_total = (record_gc_movimento_item.quantidade * record_gc_movimento_item.valor_unit);
                            record_gc_movimento_item.valor_unit_corecharge = (record_gc_movimento_item.valor_unit_corecharge / view_record_gc_movimento.cotacao_dolar_venda);
                            record_gc_movimento_item.valor_total_corecharge = (record_gc_movimento_item.quantidade * record_gc_movimento_item.valor_unit_corecharge);
                        }
                        else if (IdMoedaOrigem == 2) // Moeda Dolar converter para Real
                        {
                            record_gc_movimento_item.valor_unit = (record_gc_movimento_item.valor_unit * view_record_gc_movimento.cotacao_dolar_venda);
                            record_gc_movimento_item.valor_total = (record_gc_movimento_item.quantidade * record_gc_movimento_item.valor_unit);
                            record_gc_movimento_item.valor_unit_corecharge = (record_gc_movimento_item.valor_unit_corecharge * view_record_gc_movimento.cotacao_dolar_venda);
                            record_gc_movimento_item.valor_total_corecharge = (record_gc_movimento_item.quantidade * record_gc_movimento_item.valor_unit_corecharge);
                        }
                        record_gc_movimento_item.datahora_alteracao = DataHoraAtual;
                        record_gc_movimento_item.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        ValorTotalProdutos += record_gc_movimento_item.valor_total;
                        ValorTotalCoreCharge += record_gc_movimento_item.valor_total_corecharge;
                        record_gc_movimento_item.datahora_alteracao = DataHoraAtual;
                        record_gc_movimento_item.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;

                        db.Entry(record_gc_movimento_item).State = EntityState.Modified;
                    }

                    if (IdMoedaOrigem == 1) { record_gc_movimento.id_moeda = 2; } // Moeda Real converter para Dolar
                    else if (IdMoedaOrigem == 2) { record_gc_movimento.id_moeda = 1; } // Moeda Dolar converter para Real
                    record_gc_movimento.valor_total_produtos = ValorTotalProdutos;
                    record_gc_movimento.valor_total_liquido = ValorTotalProdutos;
                    record_gc_movimento.valor_total_bruto = ValorTotalProdutos;
                    record_gc_movimento.valor_total_corecharge = ValorTotalCoreCharge;
                    record_gc_movimento.cotacao_dolar_venda = view_record_gc_movimento.cotacao_dolar_venda;
                    record_gc_movimento.datahora_alteracao = DataHoraAtual;
                    record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_movimento).State = EntityState.Modified;

                    // Log do Movimento
                    String LogAlteracoes = string.Empty;
                    if (IdMoedaOrigem == 1) { LogAlteracoes += "Converter moeda (R$ > US$): Cotação - " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_movimento.cotacao_dolar_venda).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " | "; }
                    else if (IdMoedaOrigem == 2) { LogAlteracoes += "Converter moeda (US$ > R$): Cotação - " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_gc_movimento.cotacao_dolar_venda).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " | "; }
                    LogAlteracoes += LibDB.CompareGcMovimentos(OldMovimento, record_gc_movimento, db);

                    if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_gc_movimento.id_movimento, LogAlteracoes); };
                    db.SaveChanges();

                    MsgRetorno = "Movimento Nº <b> " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + "</b> atualizado com Sucesso" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Função - Reabrir Movimento
        public ActionResult ModalReabrirMovimento(int? id)
        {
            int temp = id.GetValueOrDefault();
            String MsgAdvertencia = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String TitleModal = string.Empty;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            if (record_gc_movimento != null)
            {
                if (record_gc_movimento.id_movimento_status != 2)
                {
                    MsgAdvertencia += "Movimento NÃO está Fechado!";
                }
                else
                {
                    List<gc_financeiro_lancamentos> ListaGCFinanceiroLancamentos = db.gc_financeiro_lancamentos.Where(l => l.id_movimento == record_gc_movimento.id_movimento && l.ativo == true).ToList();
                    List<gc_movimentos_nf> ListaGCMovimentosNF = db.gc_movimentos_nf.Where(n => n.id_movimento == record_gc_movimento.id_movimento && n.id_nfe_status == 8).ToList();
                    if (ListaGCFinanceiroLancamentos.Count > 0) { MsgAdvertencia += "  " + ListaGCFinanceiroLancamentos.Count.ToString() + " Lançamento(s) Financeiro(s) encontrado(s).  "; };
                    if (ListaGCMovimentosNF.Count > 0) { MsgAdvertencia += "  " + ListaGCMovimentosNF.Count.ToString() + " NFe encontrada(s).  "; };
                }
                TitleModal = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Reabrir Movimento Cotação/Pedido/OS Nº " + record_gc_movimento.id_movimento.ToString();
            }
            ViewBag.MsgAdvertencia = MsgAdvertencia;
            ViewBag.Title = TitleModal;
            return View("ModalReabrirMovimento", record_gc_movimento);
        }

        [HttpPost]
        public ActionResult AjaxModalReabrirMovimento(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String MsgRetorno = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);
                gc_movimentos OldMovimento = LibDB.CloneTObject(record_gc_movimento);

                if (record_gc_movimento.id_movimento_status != 2)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += "Movimento NÃO está Fechado!";
                }
                if (qtdInconsistencias == 0)
                {
                    record_gc_movimento.id_movimento_status = 1;
                    record_gc_movimento.reaberto = true;
                    record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    record_gc_movimento.datahora_alteracao = DataHoraAtual;
                    db.Entry(record_gc_movimento).State = EntityState.Modified;

                    // Log do Movimento

                    String LogAlteracoes = "Reabrir Cotação/Pedido | ";

                    db.SaveChanges();
                    MsgRetorno += "Movimento Nº <b> " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + "</b> Reaberto com Sucesso" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");

                    List<gc_movimentos_nf> ListaGCMovimentosNF = db.gc_movimentos_nf.Where(n => n.id_movimento == record_gc_movimento.id_movimento && n.id_nfe_status == 8).ToList();
                    List<gc_financeiro_lancamentos> ListaGCFinanceiroLancamentos = db.gc_financeiro_lancamentos.Where(l => l.id_movimento == record_gc_movimento.id_movimento && l.ativo == true).ToList();
                    if ((ListaGCMovimentosNF.Count > 0) || (ListaGCFinanceiroLancamentos.Count > 0))
                    {
                        MsgRetorno += "<br/><br/>---------- ATENÇÃO USUÁRIO ----------<br/>";
                        MsgRetorno += "Foram localizados os itens abaixo associados ao movimento<br/>";
                    }
                    if (ListaGCFinanceiroLancamentos.Count > 0)
                    {
                        MsgRetorno += ListaGCFinanceiroLancamentos.Count.ToString() + " Lançamentos Financeiros<br/>";
                        LogAlteracoes += " | " + ListaGCFinanceiroLancamentos.Count.ToString() + " Lançamentos Financeiros associados | ";
                    }
                    if (ListaGCMovimentosNF.Count > 0)
                    {
                        MsgRetorno += ListaGCMovimentosNF.Count.ToString() + " NFe Autorizadas<br/>";
                        LogAlteracoes += " | " + ListaGCMovimentosNF.Count.ToString() + " NFe Autorizadas | ";
                    }

                    if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_gc_movimento.id_movimento, LogAlteracoes); };
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Função - Enviar Email Espelho Digital
        public void EnviarEmailAprovacaoEspelhoDigital(gc_movimentos record_old_gc_movimento, gc_movimentos record_gc_movimento)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                bool EnviarEmail = true;
                bool PedidoAlterado = false;

                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(record_old_gc_movimento.id_cfop_operacao);
                if (RecordCfopOperacao != null) { if (RecordCfopOperacao.has_notifica_email == true) { EnviarEmail = true; }; }; // Verifica se essa operação está parametrizada para notificação por email

                if (EnviarEmail == true)
                {
                    if ((record_gc_movimento.movimento_aprovado == true) && (record_old_gc_movimento.movimento_aprovado == true)) // Movimento já aprovado anteriormente
                    {
                        if (record_gc_movimento.frete1_transportadora != record_old_gc_movimento.frete1_transportadora) { PedidoAlterado = true; }
                        if (record_gc_movimento.id_local_estoque != record_old_gc_movimento.id_local_estoque) { PedidoAlterado = true; }
                        if (record_gc_movimento.cotacao_dolar_oficial_venda != record_old_gc_movimento.cotacao_dolar_oficial_venda) { PedidoAlterado = true; }
                        if (record_gc_movimento.cotacao_dolar_venda != record_old_gc_movimento.cotacao_dolar_venda) { PedidoAlterado = true; }
                        if (record_gc_movimento.obs_aprovacao != record_old_gc_movimento.obs_aprovacao) { PedidoAlterado = true; }
                        if (PedidoAlterado == false) { EnviarEmail = false; };
                    }
                }

                if (EnviarEmail == true)
                {
                    String TituloEmailOperacional = string.Empty;
                    String TituloEmailGerencial = string.Empty;
                    String RemetenteOperacionalNome = string.Empty;
                    String RemetenteGerencialNome = string.Empty;

                    gc_parametros record_gc_parametros = db.gc_parametros.Find(1);
                    String paramFromEmail = "informatica@gdiaviacao.com";

                    g_usuarios_grupos record_g_usuarios_grupos = db.g_usuarios_grupos.Find(1);
                    String DestinatariosOperacionalNomes = "Espelho Digital";
                    String DestinatariosOperacionalEmails = "espelhodigital@gdiaviacao.com";
                    String DestinatariosGerencialNomes = "Espelho Digital Gerencial";
                    String DestinatariosGerencialEmails = "espelhodigitalgerencial@gdiaviacao.com";
                    String BodyEmailEspelhoDigitalOperacional = string.Empty;
                    String BodyEmailEspelhoDigitalGerencial = string.Empty;
                    g_templates record_g_templates = db.g_templates.Where(t => t.localizador == "GcMovimentosEmailAprovacaoPedido").FirstOrDefault();
                    if (record_g_templates != null) { BodyEmailEspelhoDigitalOperacional = record_g_templates.template.EmptyIfNull(); }
                    String ObservacoesPedido = string.Empty;

                    // Configurar o Body
                    String BodyListaOperacionalItens = String.Empty;
                    String BodyListaGerencialItens = String.Empty;
                    String BodyItemOperacional = string.Empty;
                    String BodyItemGerencial = string.Empty;
                    String BodyItemHtml = BodyEmailEspelhoDigitalOperacional.Substring(BodyEmailEspelhoDigitalOperacional.IndexOf("</html>") + 7);
                    BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Substring(0, BodyEmailEspelhoDigitalOperacional.IndexOf("</html>") + 7);

                    var listMovimento = (from _m in db.gc_movimentos
                                         join _c in db.g_clientes on _m.id_cliente equals _c.id_cliente
                                         where _m.id_movimento == record_gc_movimento.id_movimento
                                         select new { tableMovimento = _m, tableCliente = _c }).ToList();

                    var listItens = (from _i in db.gc_movimentos_itens
                                     join _p in db.g_produtos on _i.id_produto equals _p.id_produto
                                     where _i.id_movimento == record_gc_movimento.id_movimento
                                     orderby _i.sequencia, _i.id_movimento_item
                                     select new { tableItens = _i, nomeProduto = _p.nome, codigoProduto = _p.codigo }).ToList();

                    g_vendedores record_g_vendedores = db.g_vendedores.Find(listMovimento.FirstOrDefault().tableMovimento.id_vendedor);

                    // Colocar o vendedor na copia do espelho digital
                    if (record_g_vendedores != null)
                    {
                        if (record_g_vendedores.email.EmptyIfNull().ToString().Length > 0)
                        {
                            if (!DestinatariosGerencialEmails.EndsWith(";")) { DestinatariosGerencialEmails += ";"; };
                            DestinatariosGerencialEmails += record_g_vendedores.email.EmptyIfNull().ToString() + ";";
                        }
                    }
                    g_pagrec_condicoes record_g_pagrec_condicoes = db.g_pagrec_condicoes.Find(listMovimento.FirstOrDefault().tableMovimento.id_pagrec_condicao);
                    g_moedas record_g_moedas = db.g_moedas.Find(listMovimento.FirstOrDefault().tableMovimento.id_moeda);
                    List<g_produtos_condicoes> allProdutosCondicoes = db.g_produtos_condicoes.Where(c => (c.id_produto_condicao > 0)).ToList();
                    List<gc_entregas_prazos> allEntregasPrazos = db.gc_entregas_prazos.Where(p => (p.id_entrega_prazo > 0)).ToList();
                    g_clientes record_g_transportadora = db.g_clientes.Find(listMovimento.FirstOrDefault().tableMovimento.frete1_transportadora);
                    g_pagrec_condicoes record_pagrec_condicoes = db.g_pagrec_condicoes.Find(listMovimento.FirstOrDefault().tableMovimento.id_pagrec_condicao);
                    g_cidades record_g_cidades = db.g_cidades.Find(listMovimento.FirstOrDefault().tableCliente.id_cidade_com);
                    g_uf record_g_uf = db.g_uf.Find(listMovimento.FirstOrDefault().tableCliente.id_uf_com);
                    gc_locais_estoque record_locais_estoque = db.gc_locais_estoque.Find(listMovimento.FirstOrDefault().tableMovimento.id_local_estoque);
                    String NomeCliente = listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString();
                    if (listMovimento.FirstOrDefault().tableCliente.cnpj.EmptyIfNull().ToString().Trim().Length > 0) { NomeCliente += "   (CNPJ: " + LibStringFormat.FormatarCPFCNPJ("J", listMovimento.FirstOrDefault().tableCliente.cnpj.EmptyIfNull().ToString()) + ")"; }
                    else if (listMovimento.FirstOrDefault().tableCliente.cpf.EmptyIfNull().ToString().Trim().Length > 0) { NomeCliente += "   (CPF: " + LibStringFormat.FormatarCPFCNPJ("F", listMovimento.FirstOrDefault().tableCliente.cpf.EmptyIfNull().ToString()) + ")"; }
                    BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[cliente]", NomeCliente);

                    String EnderecoCliente = listMovimento.FirstOrDefault().tableCliente.endereco_com.EmptyIfNull().ToString().Trim() + ", " + listMovimento.FirstOrDefault().tableCliente.endereco_com_numero.EmptyIfNull().ToString().Trim() + " " + listMovimento.FirstOrDefault().tableCliente.endereco_com_complemento.EmptyIfNull().ToString().Trim() + " - CEP: " + LibStringFormat.FormatarCEP(listMovimento.FirstOrDefault().tableCliente.cep_com.EmptyIfNull().ToString());
                    if (record_g_cidades != null) { EnderecoCliente += " - " + record_g_cidades.nome.EmptyIfNull().ToString(); };
                    if (record_g_uf != null) { EnderecoCliente += " - " + record_g_uf.sigla.EmptyIfNull().ToString(); };
                    BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[endereco]", EnderecoCliente.ToUpperInvariant());

                    if (listMovimento.FirstOrDefault().tableMovimento.frete_valor > 0) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[valor_frete]", string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.frete_valor).Replace("R$", "").Trim()); }
                    else { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[valor_frete]", "0,00"); };

                    if (listMovimento.FirstOrDefault().tableMovimento.id_frete_responsavel == 1) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[frete_responsavel]", "  [Emitente]"); }
                    else if (listMovimento.FirstOrDefault().tableMovimento.id_frete_responsavel == 2) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[frete_responsavel]", "  [Destinatário]"); }
                    else if (listMovimento.FirstOrDefault().tableMovimento.id_frete_responsavel == 3) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[frete_responsavel]", "  [Sem Frete]"); }
                    else { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[frete_responsavel]", ""); };

                    if (listMovimento.FirstOrDefault().tableMovimento.frete_valor > 0) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[frete_tipo]", "R$ Frete (Destacado)"); }
                    else if (listMovimento.FirstOrDefault().tableMovimento.frete_gerencial > 0) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[frete_tipo]", "R$ Frete (Gerencial)"); }
                    else { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[frete_tipo]", "R$ Frete "); };

                    BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[vendedor]", record_g_vendedores.nome.EmptyIfNull().ToString());
                    if (record_g_transportadora != null) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[transportadora]", record_g_transportadora.nome.EmptyIfNull().ToString()); } else { if (listMovimento.FirstOrDefault().tableMovimento.frete1_transportadora == 0) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[transportadora]", "CLIENTE RETIRA"); } else { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[transportadora]", "Sem Transportadora"); } }
                    if (record_pagrec_condicoes != null) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[condicao_pagamento]", record_pagrec_condicoes.descricao.EmptyIfNull().ToString()); } else { { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[condicao_pagamento]", "Não Informada"); } }
                    if (record_locais_estoque != null) { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[estoque]", record_locais_estoque.sigla.EmptyIfNull().ToString()); } else { { BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[estoque]", "NÃO INFORMADO"); } }

                    int IndexItem = 0;
                    foreach (var item in listItens)
                    {
                        IndexItem += 1;
                        String NomeProduto = item.nomeProduto.EmptyIfNull().ToString();
                        if (NomeProduto.Length > 200) { NomeProduto = NomeProduto.Substring(0, 200) + "..."; };
                        if (item.tableItens.serial.EmptyIfNull().ToString().Trim().Length > 0) { NomeProduto += "|Serial:" + item.tableItens.serial.EmptyIfNull().ToString().Trim(); }
                        if (item.tableItens.lote01_identificador.EmptyIfNull().ToString().Trim().Length > 0) { NomeProduto += "|Lote:" + item.tableItens.lote01_identificador.EmptyIfNull().ToString().Trim(); }
                        if (item.tableItens.obs.EmptyIfNull().ToString().Trim().Length > 0) { NomeProduto += "|Obs:" + item.tableItens.obs.EmptyIfNull().ToString().Trim(); }

                        BodyItemOperacional = BodyItemHtml;
                        BodyItemOperacional = BodyItemOperacional.Replace("[item_numero]", item.tableItens.sequencia.ToString("N1").Replace(",", ".").Replace(".0", ""));
                        BodyItemOperacional = BodyItemOperacional.Replace("[item_descricao]", NomeProduto);
                        BodyItemOperacional = BodyItemOperacional.Replace("[item_qtd]", item.tableItens.quantidade.EmptyIfNull().ToString().Replace(",000", "").Trim());
                        BodyItemOperacional = BodyItemOperacional.Replace("[item_valor_unit]", string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.tableItens.valor_unit).Replace("R$", "").Trim());
                        BodyItemOperacional = BodyItemOperacional.Replace("[item_valor_total]", string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.tableItens.valor_total).Replace("R$", "").Trim() + "[markupitem]");
                        if ((IndexItem % 2) == 1) { BodyItemOperacional = BodyItemOperacional.Replace("[colorrow]", "#FFFFFF"); } else { BodyItemOperacional = BodyItemOperacional.Replace("[colorrow]", "#D5DBDB"); }

                        BodyItemGerencial = BodyItemOperacional.Replace("[markupitem]", "<br/>(" + Math.Round(item.tableItens.markup, 2).ToString("0.00") + " %)");
                        BodyListaGerencialItens += BodyItemGerencial;

                        BodyItemOperacional = BodyItemOperacional.Replace("[markupitem]", "");
                        BodyListaOperacionalItens += BodyItemOperacional;
                    }
                    if (listMovimento.FirstOrDefault().tableMovimento.obs.EmptyIfNull().ToString().Length > 0) { ObservacoesPedido += "OBS. PEDIDO: " + listMovimento.FirstOrDefault().tableMovimento.obs.EmptyIfNull().Replace("\r\n", "<br/>") + "<br/>"; }
                    if (listMovimento.FirstOrDefault().tableMovimento.obs_aprovacao.EmptyIfNull().ToString().Length > 0) { ObservacoesPedido += "OBS APROVAÇÃO: " + listMovimento.FirstOrDefault().tableMovimento.obs_aprovacao.EmptyIfNull().Replace("\r\n", "") + "<br/>"; }
                    if (listMovimento.FirstOrDefault().tableMovimento.frete_observacoes.EmptyIfNull().ToString().Length > 0) { ObservacoesPedido += "OBS FRETE: " + listMovimento.FirstOrDefault().tableMovimento.obs_aprovacao.EmptyIfNull().Replace("\r\n", "") + "<br/>"; }
                    try { if (listMovimento.FirstOrDefault().tableMovimento.frete2_transportadora > 0) { ObservacoesPedido += "OBS. Transportadora Complementar: " + db.g_clientes.Find(listMovimento.FirstOrDefault().tableMovimento.frete2_transportadora).nome.EmptyIfNull().ToString() + "<br/>"; }; } catch (Exception) { };
                    BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[observacoes]", ObservacoesPedido);

                    if (record_gc_movimento.movimento_aprovado == true)
                    {
                        //if (record_gc_movimento.id_movimento_posicao == 0)
                        if (record_old_gc_movimento.movimento_aprovado == false) // Movimento já aprovado anteriormente
                        {
                            string IconeAprovado = new string(new char[] { '\u2705' });
                            RemetenteOperacionalNome = "GDI - Pedido " + record_gc_movimento.id_movimento.ToString() + " Aprovado";
                            RemetenteGerencialNome = "GDI Gerencial - Pedido " + record_gc_movimento.id_movimento.ToString() + " Aprovado";
                            TituloEmailOperacional = IconeAprovado + " GDI - Pedido " + record_gc_movimento.id_movimento.ToString() + " Aprovado - " + LibStringFormat.GetRazaoSocialAbreviada(listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString()) + " (Vendedor: " + LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(record_g_vendedores.nome.EmptyIfNull().ToString())) + ")";
                            TituloEmailGerencial = IconeAprovado + " GDI Gerencial - Pedido " + record_gc_movimento.id_movimento.ToString() + " Aprovado - " + LibStringFormat.GetRazaoSocialAbreviada(listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString()) + " (Vendedor: " + LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(record_g_vendedores.nome.EmptyIfNull().ToString())) + ")";
                        }
                        else // Aprovado
                        {
                            string IconeAlterado = new string(new char[] { '\u26a0' });
                            RemetenteOperacionalNome = "GDI - Pedido " + record_gc_movimento.id_movimento.ToString() + " Alterado";
                            RemetenteGerencialNome = "GDI Gerencial - Pedido " + record_gc_movimento.id_movimento.ToString() + " Alterado";
                            TituloEmailOperacional = IconeAlterado + " GDI - Pedido " + record_gc_movimento.id_movimento.ToString() + " Alterado - " + LibStringFormat.GetRazaoSocialAbreviada(listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString()) + " (Vendedor: " + LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(record_g_vendedores.nome.EmptyIfNull().ToString())) + ")";
                            TituloEmailGerencial = IconeAlterado + " GDI Gerencial - Pedido " + record_gc_movimento.id_movimento.ToString() + " Alterado - " + LibStringFormat.GetRazaoSocialAbreviada(listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString()) + " (Vendedor: " + LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(record_g_vendedores.nome.EmptyIfNull().ToString())) + ")";
                            BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("#DDEEBC", "#F9E79F"); // Cor de Atenção
                        }
                        record_gc_movimento.datahora_alteracao = DataHoraAtual;
                        record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_gc_movimento).State = EntityState.Modified;
                    }
                    else if (record_gc_movimento.movimento_aprovado == false)
                    {
                        if (record_old_gc_movimento.movimento_aprovado == true) // Pedido estava aprovado anteriormente
                        {
                            string IconeCancelado = new string(new char[] { '\u274c' });
                            RemetenteOperacionalNome = "GDI - Pedido " + record_gc_movimento.id_movimento.ToString() + " Cancelado";
                            RemetenteGerencialNome = "GDI Gerencial - Pedido " + record_gc_movimento.id_movimento.ToString() + " Cancelado";
                            TituloEmailOperacional = IconeCancelado + " GDI - Pedido " + record_gc_movimento.id_movimento.ToString() + " Cancelado - " + LibStringFormat.GetRazaoSocialAbreviada(listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString()) + " (Vendedor: " + LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(record_g_vendedores.nome.EmptyIfNull().ToString())) + ")";
                            TituloEmailGerencial = IconeCancelado + " GDI Gerencial - Pedido " + record_gc_movimento.id_movimento.ToString() + " Cancelado - " + LibStringFormat.GetRazaoSocialAbreviada(listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString()) + " (Vendedor: " + LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(record_g_vendedores.nome.EmptyIfNull().ToString())) + ")";
                            BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("#DDEEBC", "#F5B7B1"); // Cor de Reprovação
                        }
                    }
                    BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[numero_pedido]", record_gc_movimento.id_movimento.ToString() + " - " + LibStringFormat.GetRazaoSocialAbreviada(listMovimento.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString()));

                    BodyEmailEspelhoDigitalGerencial = BodyEmailEspelhoDigitalOperacional;
                    BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[lista-itens]", BodyListaOperacionalItens);
                    BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[valor_total]", string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.valor_total_liquido + listMovimento.FirstOrDefault().tableMovimento.frete_valor + listMovimento.FirstOrDefault().tableMovimento.valor_total_corecharge).Replace("R$", "").Trim());

                    BodyEmailEspelhoDigitalGerencial = BodyEmailEspelhoDigitalGerencial.Replace("[lista-itens]", BodyListaGerencialItens);
                    BodyEmailEspelhoDigitalGerencial = BodyEmailEspelhoDigitalGerencial.Replace("[valor_total]", string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", listMovimento.FirstOrDefault().tableMovimento.valor_total_liquido + listMovimento.FirstOrDefault().tableMovimento.frete_valor + listMovimento.FirstOrDefault().tableMovimento.valor_total_corecharge).Replace("R$", "").Trim() + "<br/>(" + Math.Round(listMovimento.FirstOrDefault().tableMovimento.markup, 2).ToString("0.00") + " %)");

                    if (listMovimento.FirstOrDefault().tableMovimento.cotacao_dolar_venda > 0)
                    {
                        BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[cotacaodolar]", "Cotação US$ " + listMovimento.FirstOrDefault().tableMovimento.cotacao_dolar_venda.ToString("0.0000"));
                        BodyEmailEspelhoDigitalGerencial = BodyEmailEspelhoDigitalGerencial.Replace("[cotacaodolar]", "Cotação US$ " + listMovimento.FirstOrDefault().tableMovimento.cotacao_dolar_venda.ToString("0.0000"));
                    }
                    else
                    {
                        BodyEmailEspelhoDigitalOperacional = BodyEmailEspelhoDigitalOperacional.Replace("[cotacaodolar]", "");
                        BodyEmailEspelhoDigitalGerencial = BodyEmailEspelhoDigitalGerencial.Replace("[cotacaodolar]", "");
                    }

                    if (CachePersister.userIdentity.IdUsuario <= 1)
                    {
                        DestinatariosOperacionalNomes = "Márcio";
                        DestinatariosOperacionalEmails = "informatica@gdiaviacao.com";
                        DestinatariosGerencialNomes = "Márcio";
                        DestinatariosGerencialEmails = "informatica@gdiaviacao.com";
                    }

                    if (DestinatariosOperacionalEmails.Trim().Length > 0) 
                    {
                        BotAwsEmail RoboAwsEmail1 = new BotAwsEmail();
                        RoboAwsEmail1.EnviarEmailAWS(paramFromEmail, RemetenteOperacionalNome, DestinatariosOperacionalEmails, DestinatariosOperacionalNomes, TituloEmailOperacional, BodyEmailEspelhoDigitalOperacional, null); 
                    };
                    if (DestinatariosGerencialEmails.Trim().Length > 0) 
                    {
                        BotAwsEmail RoboAwsEmail2 = new BotAwsEmail();
                        RoboAwsEmail2.EnviarEmailAWS(paramFromEmail, RemetenteGerencialNome, DestinatariosGerencialEmails, DestinatariosGerencialNomes, TituloEmailGerencial, BodyEmailEspelhoDigitalGerencial, null); 
                    };
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
        #endregion

        #region Função - Ajustes Da Comissão
        public ActionResult ModalPedidoAjustarComissao(int? id)
        {
            int temp = id.GetValueOrDefault();
            String TitleModal = string.Empty;
            String MsgBloqueio = string.Empty;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            TitleModal = LibIcons.getIcon("fa-solid fa-clipboard-check", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Ajustar Comissão - Pedido Nº " + record_gc_movimento.id_movimento.ToString();
            if (record_gc_movimento != null)
            {
                if (record_gc_movimento.movimento_aprovado == false) { MsgBloqueio = "<b> ---------- PEDIDO NÃO ESTÁ APROVADO ----------</b>" + "<br/>"; }
            }
            else
            {
                MsgBloqueio = "Movimento [" + id.ToString() + "] não localizado no ERP";
            }
            ViewBag.Title = TitleModal;
            LibDataSets.LoadDatasetGVendedores(db);
            ViewBag.comboVendedores = LibDataSets.LoadComboGVendedores(db);
            ViewBag.MsgBloqueio = MsgBloqueio;
            return View("ModalPedidoAjustarComissao", record_gc_movimento);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoAjustarComissao(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            String LogAlteracoes = string.Empty;
            String MsgRetorno = "";
            try
            {
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                gc_movimentos record_gc_movimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);

                if ((view_record_gc_movimento.comissao1_percentual + view_record_gc_movimento.comissao2_percentual) > 50)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += "A Soma dos percentuais das comissões não poderá ultrapassar 50%!" + "<br/>";
                }
                else
                {
                    if (view_record_gc_movimento.comissao1_percentual > 0) { view_record_gc_movimento.comissao1_valor = (((record_gc_movimento.valor_total_liquido - record_gc_movimento.frete_gerencial) / 100) * view_record_gc_movimento.comissao1_percentual); } else if (view_record_gc_movimento.comissao1_valor > 0) { view_record_gc_movimento.comissao1_percentual = ((view_record_gc_movimento.comissao1_valor * 100) / (record_gc_movimento.valor_total_liquido - record_gc_movimento.frete_gerencial)); };
                    if (view_record_gc_movimento.comissao2_percentual > 0) { view_record_gc_movimento.comissao2_valor = (((record_gc_movimento.valor_total_liquido - record_gc_movimento.frete_gerencial) / 100) * view_record_gc_movimento.comissao2_percentual); } else if (view_record_gc_movimento.comissao2_valor > 0) { view_record_gc_movimento.comissao2_percentual = ((view_record_gc_movimento.comissao2_valor * 100) / (record_gc_movimento.valor_total_liquido - record_gc_movimento.frete_gerencial)); };
                    if ((view_record_gc_movimento.comissao1_percentual + view_record_gc_movimento.comissao2_percentual) > 50)
                    {
                        qtdInconsistencias += 1;
                        MsgRetorno += "A Soma dos percentuais das comissões não poderá ultrapassar 50%!" + "<br/>";
                    }
                    if ((view_record_gc_movimento.comissao1_percentual > 0) || (view_record_gc_movimento.comissao1_valor > 0))
                    {
                        if (view_record_gc_movimento.comissao1_vendedor <= 0)
                        {
                            qtdInconsistencias += 1;
                            MsgRetorno += "Vendedor(1) não informado!" + "<br/>";
                        }
                    }
                    if ((view_record_gc_movimento.comissao2_percentual > 0) || (view_record_gc_movimento.comissao2_valor > 0))
                    {
                        if (view_record_gc_movimento.comissao2_vendedor <= 0)
                        {
                            qtdInconsistencias += 1;
                            MsgRetorno += "Vendedor(2) não informado!" + "<br/>";
                        }
                    }
                    if ((view_record_gc_movimento.comissao1_percentual == 0) && (view_record_gc_movimento.comissao1_valor == 0)) { view_record_gc_movimento.comissao1_vendedor = 0; }
                    if ((view_record_gc_movimento.comissao2_percentual == 0) && (view_record_gc_movimento.comissao2_valor == 0)) { view_record_gc_movimento.comissao2_vendedor = 0; }
                }

                if (qtdInconsistencias == 0)
                {
                    if (view_record_gc_movimento.comissao1_vendedor <= 0) { view_record_gc_movimento.comissao1_vendedor = record_gc_movimento.id_vendedor; };
                    List<g_vendedores> AllVendedores = db.g_vendedores.Where(v => v.ativo == true).OrderBy(p => p.nome).ToList();

                    LogAlteracoes += "Ajuste de Comissão | ";
                    if (view_record_gc_movimento.comissao1_vendedor > 0)
                    {
                        LogAlteracoes += "Vendedor(1): " + AllVendedores.Where(v => v.id_vendedor == view_record_gc_movimento.comissao1_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + " - ";
                        LogAlteracoes += "Part(%): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.comissao1_percentual).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " - ";
                        LogAlteracoes += "Part(R$): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.comissao1_valor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " | ";
                    }
                    if (view_record_gc_movimento.comissao2_vendedor > 0)
                    {
                        LogAlteracoes += "Vendedor(2): " + AllVendedores.Where(v => v.id_vendedor == view_record_gc_movimento.comissao2_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + " - ";
                        LogAlteracoes += "Part(%): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.comissao2_percentual).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " - ";
                        LogAlteracoes += "Part(R$): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", view_record_gc_movimento.comissao2_valor).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " | ";
                    }

                    // Alteração da Comissão
                    record_gc_movimento.comissao1_vendedor = view_record_gc_movimento.comissao1_vendedor;
                    record_gc_movimento.comissao1_percentual = view_record_gc_movimento.comissao1_percentual;
                    record_gc_movimento.comissao1_valor = view_record_gc_movimento.comissao1_valor;
                    record_gc_movimento.comissao2_vendedor = view_record_gc_movimento.comissao2_vendedor;
                    record_gc_movimento.comissao2_percentual = view_record_gc_movimento.comissao2_percentual;
                    record_gc_movimento.comissao2_valor = view_record_gc_movimento.comissao2_valor;
                    MsgRetorno = "Comissão do Pedido Nº " + record_gc_movimento.id_movimento.EmptyIfNull().ToString() + " <b>ALTERADA</b> com Sucesso!" + LibStringFormat.GetTabHtml(2) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                    record_gc_movimento.datahora_alteracao = DataHoraAtual;
                    record_gc_movimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_movimento).State = EntityState.Modified;

                    db.SaveChanges();
                    Sucesso = true;

                    if (Sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", record_gc_movimento.id_movimento, LogAlteracoes); }; };
                }
            }
            catch (DbEntityValidationException ex)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                qtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Função - Atualizar Valor Total Cotação
        public ActionResult ModalPedidoAtualizarValorTotal(int? id)
        {
            int temp = id.GetValueOrDefault();
            String MsgBloqueio = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String TitleModal = string.Empty;
            gc_movimentos record_gc_movimento = db.gc_movimentos.Find(id);
            if (record_gc_movimento != null) { TitleModal = LibIcons.getIcon("fa-solid fa-dollar-sign", "", "#008000", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Atualizar Valor Total - Cotação/Pedido/OS Nº " + record_gc_movimento.id_movimento.ToString(); };
            ViewBag.Title = TitleModal;
            ViewBag.MsgBloqueio = MsgBloqueio;
            return View("ModalPedidoAtualizarValorTotal", record_gc_movimento);
        }

        [HttpPost]
        public ActionResult AjaxModalPedidoAtualizarValorTotal(gc_movimentos view_record_gc_movimento)
        {
            bool Sucesso = false;
            int QtdErros = 0;
            String MsgRetorno = "";
            Decimal ValorTotalFobProdutos = 0;
            Decimal ValorTotalPrevisto = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            gc_movimentos RecordMovimento = db.gc_movimentos.Find(view_record_gc_movimento.id_movimento);
            List<gc_movimentos_itens> ListaItens = db.gc_movimentos_itens.Where(i => i.id_movimento == RecordMovimento.id_movimento).OrderBy(i => i.quantidade).ToList();
            List<gc_movimentos_itens> ListaItensAtualizar = new List<gc_movimentos_itens>();
            List<g_produtos> ListaProdutos = db.g_produtos.SqlQuery("select * from g_produtos where id_produto in (select distinct id_produto from gc_movimentos_itens where id_movimento = "+ RecordMovimento.id_movimento.EmptyIfNull().ToString() + ")").ToList();
            try
            {
                // Validações
                if (RecordMovimento.cotacao_dolar_venda <= 0)
                {
                    QtdErros += 1;
                    MsgRetorno += " - Cotação Dolar do pedido não foi definido!" + "<br/>";
                }

                if (RecordMovimento.valor_total_bruto <= 0)
                {
                    QtdErros += 1;
                    MsgRetorno += " - R$ Total atualizado deverá ser informado!" + "<br/>";
                }
                else
                {
                    ValorTotalPrevisto = view_record_gc_movimento.valor_total_bruto;
                }
                
                foreach (gc_movimentos_itens Item in ListaItens)
                {
                    g_produtos RecordProduto = ListaProdutos.Where(p => p.id_produto == Item.id_produto).FirstOrDefault();
                    if (RecordProduto.fob1_dollar <= 0)
                    {
                        QtdErros += 1;
                        MsgRetorno += " - Fob item ["+ RecordProduto.codigo + "] não foi definido!" + "<br/>";
                    }
                }

                // Cálculo dos Valores dos Itens
                if (QtdErros == 0)
                {
                    ValorTotalFobProdutos = 0;
                    foreach (gc_movimentos_itens Item in ListaItens)
                    {
                        g_produtos RecordProduto = ListaProdutos.Where(p => p.id_produto == Item.id_produto).FirstOrDefault();
                        Item.valor_unit = RecordProduto.fob1_dollar * RecordMovimento.cotacao_dolar_venda;
                        Item.valor_total = Item.valor_unit * Item.quantidade;
                        ValorTotalFobProdutos += Item.valor_total;
                    }
                    if ((ValorTotalFobProdutos + RecordMovimento.frete_valor) > ValorTotalPrevisto)
                    {
                        QtdErros += 1;
                        MsgRetorno += " - R$ Total Fob dos produtos + R$ Frete é maior do que o Valor Total Informado!" + "<br/>";
                    }
                }

                if (QtdErros == 0)
                {
                    int SequencialItem = 0;
                    Decimal PercentualTotalItem = 0;
                    Decimal PercentualUnitItem = 0;
                    Decimal ItemCustoReais = 0;
                    Decimal MarkupItem = 0;
                    Decimal MarkupPedido = 0;
                    Decimal PedidoCustoTotalVenda = 0;
                    Decimal PedidoValorTotalVendaSemFrete = 0;
                    Decimal PedidoValorTotalVendaComFrete = 0;

                    Decimal ValorTotalPrevistoSemFrete = ValorTotalPrevisto - RecordMovimento.frete_valor; ;
                    Decimal ValorTotalPrevistoSemFreteRestante = ValorTotalPrevistoSemFrete;
                    foreach (gc_movimentos_itens Item in ListaItens)
                    {
                        SequencialItem += 1;

                        if (SequencialItem == ListaItens.Count) // Último Item ou 1 único item
                        {
                            Item.valor_unit = LibNumbers.TruncateDecimal((ValorTotalPrevistoSemFreteRestante / Item.quantidade), 2);
                            Item.valor_total = Item.valor_unit * Item.quantidade;
                        }
                        else
                        {
                            PercentualTotalItem = ((Item.valor_total * 100) / ValorTotalFobProdutos);
                            PercentualUnitItem = (PercentualTotalItem / Item.quantidade);
                            Item.valor_unit = LibNumbers.TruncateDecimal((PercentualUnitItem * (ValorTotalPrevistoSemFrete / 100)), 2);
                            Item.valor_total = Item.valor_unit * Item.quantidade;
                        }
                        PedidoValorTotalVendaSemFrete += Item.valor_total;
                        ValorTotalPrevistoSemFreteRestante -= Item.valor_total;

                        g_produtos RecordProduto = ListaProdutos.Where(p => p.id_produto == Item.id_produto).FirstOrDefault();
                        if ((RecordProduto.fob1_dollar > 0) && (RecordMovimento.cotacao_dolar_oficial_venda > 0))
                        {
                            ItemCustoReais = RecordProduto.fob1_dollar * RecordMovimento.cotacao_dolar_oficial_venda * Item.quantidade;
                            PedidoCustoTotalVenda += ItemCustoReais;
                            MarkupItem = ((Item.valor_total * 100) / ItemCustoReais) - 100;
                            Item.markup = LibNumbers.TruncateDecimal(MarkupItem, 2);
                            MarkupPedido = ((PedidoValorTotalVendaSemFrete * 100) / PedidoCustoTotalVenda) - 100;
                        }

                        ListaItensAtualizar.Add(Item);
                    }

                    foreach (gc_movimentos_itens ItemAtualizar in ListaItensAtualizar)
                    {
                        ItemAtualizar.datahora_alteracao = DataHoraAtual;
                        ItemAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(ItemAtualizar).State = EntityState.Modified;
                    }

                    RecordMovimento.valor_total_produtos = PedidoValorTotalVendaSemFrete;
                    RecordMovimento.valor_total_liquido = PedidoValorTotalVendaSemFrete;
                    RecordMovimento.valor_total_bruto = PedidoValorTotalVendaSemFrete + RecordMovimento.frete_valor;
                    PedidoValorTotalVendaComFrete = PedidoValorTotalVendaSemFrete + RecordMovimento.frete_valor;
                    RecordMovimento.markup = MarkupPedido;
                    RecordMovimento.datahora_alteracao = DataHoraAtual;
                    RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(RecordMovimento).State = EntityState.Modified;
                    db.SaveChanges();

                    MsgRetorno += "Cotação/Pedido Nº <b> " + RecordMovimento.id_movimento.EmptyIfNull().ToString() + "</b> atualizado com Sucesso" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                    MsgRetorno += "Valor Informado: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorTotalPrevisto) + "<br/>";
                    MsgRetorno += "Valor Total Pedido (Atualizado): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", PedidoValorTotalVendaComFrete) + "<br/><br/>";
                    if (PedidoValorTotalVendaComFrete == ValorTotalPrevisto) { MsgRetorno += "Obs: O Valor total do pedido é <b>Igual</b> ao valor informado!" + "<br/><br/>"; }
                    else { MsgRetorno += "Obs: Houve uma diferença de <b>R$ " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorTotalPrevisto- PedidoValorTotalVendaComFrete) + "</b><br/>" + "Entre o Valor Total Informado e o Valor Total do Pedido!" + "<br/>"; }
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdErros = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdErros = 1;
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Relatório - Termo Convenio ICMS PDF
        public ActionResult AjaxReportTermoConvenioICMSPDF(gc_movimentos_nf view_record_gc_movimento_nf)
        {
            bool Sucesso = false;
            int qtdInconsistencias = 0;
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia();
            String MsgRetorno = "";
            String bodyHTML = string.Empty;
            String idProcessamentoGravado = "0";
            String TipoSaida = "Pdf";
            var pdf = new ViewAsPdf();
            try
            {
                if (view_record_gc_movimento_nf == null)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else if (view_record_gc_movimento_nf.id_movimento_nf <= 0)
                {
                    qtdInconsistencias += 1;
                    MsgRetorno += " - Não foi possível localizar a NF!<br/>";
                }
                else
                {
                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    gc_movimentos_nf record_gc_movimentos_nf = db.gc_movimentos_nf.Find(view_record_gc_movimento_nf.id_movimento_nf);
                    gc_movimentos record_gc_movimento = db.gc_movimentos.Find(record_gc_movimentos_nf.id_movimento);
                    cstReportHTML record_cstReportHTML = new cstReportHTML();
                    if (record_gc_movimento != null)
                    {
                        var ListDados = (from _nf in db.gc_movimentos_nf
                                         join _m in db.gc_movimentos on _nf.id_movimento equals _m.id_movimento
                                         join _c in db.g_clientes on _m.id_cliente equals _c.id_cliente
                                         join _cfop in db.gc_cfop on _nf.id_cfop equals _cfop.id_cfop
                                         where _nf.id_movimento_nf == record_gc_movimentos_nf.id_movimento_nf
                                         select new { tableCfop = _cfop, tableMovimentoNF = _nf, tableMovimento = _m, tableCliente = _c }).ToList();

                        record_cstReportHTML.Identificador = record_gc_movimento.id_movimento;
                        g_templates record_g_templates = db.g_templates.Where(t => t.localizador == "GcMovimentosTermoIcmsDifalAviacao").FirstOrDefault();
                        if (record_g_templates != null) { bodyHTML = record_g_templates.template.EmptyIfNull(); }
                        if (bodyHTML.IndexOf("<body>") > 0) { bodyHTML = bodyHTML.Substring(bodyHTML.IndexOf("<body>") + 6); }
                        if (bodyHTML.IndexOf("</body>") > 0) { bodyHTML = bodyHTML.Substring(0, bodyHTML.IndexOf("</body>")); }
                        bodyHTML = bodyHTML.Replace("[NomeRazaoSocial]", ListDados.FirstOrDefault().tableCliente.nome.EmptyIfNull().ToString());
                        bodyHTML = bodyHTML.Replace("[NumeroNotaFiscal]", ListDados.FirstOrDefault().tableMovimentoNF.nf_numero.EmptyIfNull().ToString());
                        bodyHTML = bodyHTML.Replace("[NaturezaOperacao]", ListDados.FirstOrDefault().tableCfop.numero.EmptyIfNull().ToString() + " - " + ListDados.FirstOrDefault().tableCfop.descricao.EmptyIfNull().ToString());
                        bodyHTML = bodyHTML.Replace("[ValorTotal]", LibStringFormat.FormatarMoedaReais(ListDados.FirstOrDefault().tableMovimento.valor_total_bruto));
                        bodyHTML = bodyHTML.Replace("[IcmsBaseCalculo]", LibStringFormat.FormatarMoedaReais(ListDados.FirstOrDefault().tableMovimento.icms_vbc));
                        bodyHTML = bodyHTML.Replace("[IcmsValorTotal]", LibStringFormat.FormatarMoedaReais(ListDados.FirstOrDefault().tableMovimento.icms_vicms));
                    }
                    record_cstReportHTML.BodyHTML = bodyHTML;

                    String FileNamePDF = "Termo Convênio ICMS_" + record_cstReportHTML.Identificador.ToString();
                    if (TipoSaida.Equals("View")) { FileNamePDF = String.Empty; };

                    pdf = new ViewAsPdf
                    {
                        ViewName = "ReportTermoConvenioICMSPDF",
                        Model = record_cstReportHTML,
                        FileName = FileNamePDF
                    };

                    if ((TipoSaida.Equals("Email")) || (TipoSaida.Equals("Pdf")))
                    {
                        byte[] applicationPDFData_BL = pdf.BuildFile(ControllerContext);
                        string fileNamePDF = string.Empty;
                        if (TipoSaida.Equals("Email")) { fileNamePDF = FileNamePDF + ".pdf"; }
                        else if (TipoSaida.Equals("Pdf")) { fileNamePDF = FileNamePDF + "_download.pdf"; }
                        fileNamePDF = Path.Combine(DirTempFiles, fileNamePDF);
                        var fileStream_BL = new FileStream(fileNamePDF, FileMode.Create, FileAccess.Write);
                        fileStream_BL.Write(applicationPDFData_BL, 0, applicationPDFData_BL.Length);
                        fileStream_BL.Close();
                        Sucesso = true;

                        if (TipoSaida.Equals("Pdf"))
                        {
                            g_processamento record_g_processamento = new g_processamento();
                            record_g_processamento.id_processamento_tipo = 37; // PDF
                            record_g_processamento.id_processamento_modulo = 3; // Modulo Comercial
                            record_g_processamento.detalhamento = "Termo ICSMS/Difal";
                            record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                            record_g_processamento.datahora_inicio = LibDateTime.getDataHoraBrasilia();
                            record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                            record_g_processamento.qtd_registros = 1;
                            record_g_processamento.qtd_reg_ok = 1;
                            record_g_processamento.qtd_reg_erro = 0;
                            record_g_processamento.processando = false;
                            record_g_processamento.concluido = true;
                            record_g_processamento.pathfile = fileNamePDF;
                            record_g_processamento.id_coligada = 0; // Global
                            record_g_processamento.id_filial = 0; // Global
                            db.g_processamento.Add(record_g_processamento);
                            db.SaveChanges();
                            Sucesso = true;
                            idProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                            MsgRetorno = "Termo ICMS/Difal gerado com Sucesso em PDF" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" + "Obs: O Download será iniciado automaticamente na sequência";
                        }
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            if (TipoSaida.Equals("Email") || TipoSaida.Equals("Pdf"))
            {
                return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
            }
            else if (TipoSaida.Equals("View"))
            {
                return pdf;
            }
            else
            {
                return null;
            }
        }
        #endregion

        [HttpPost]
        public ActionResult AjaxDadosClientesDestinatarios(g_clientes view_g_clientes)
        {
            bool sucesso = false;
            string msgRetorno = String.Empty;
            string PrecoVenda = String.Empty;
            string DescricaoLonga = String.Empty;
            string IdProduto = String.Empty;
            string CodigoProduto = String.Empty;
            string HasCoreCharge = String.Empty;
            string QtdEstoque1 = String.Empty;
            string QtdEstoque2 = String.Empty;
            string QtdEstoque3 = String.Empty;
            string QtdEstoque4 = String.Empty;
            List<g_clientes_destinatarios> ListaClientesDestinatarios = new List<g_clientes_destinatarios>();
            ListaClientesDestinatarios.Clear();
            try
            {
                ListaClientesDestinatarios = LibDataSets.LoadDatasetGcClientesDestinatarios(view_g_clientes.id_cliente, db);
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
            return Json(new { success = sucesso, msg = msgRetorno, dataSetClientesDestinatarios = ListaClientesDestinatarios }, JsonRequestBehavior.AllowGet);
        }
        public List<string> GetAnexosPedido(int IdMovimento)
        {
            List<string> ListaAnexos = new List<string>();
            string SqlNotasFiscais = string.Empty;
            string SqlBoletos = string.Empty;
            string DirTempFiles = Server.MapPath("~/_filestemp");
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, "downloads");
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
            LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }

            try
            {
                // Notas Fiscais
                SqlNotasFiscais += " select nf.* from gc_movimentos_nf nf ";
                SqlNotasFiscais += " where nf.id_movimento = " + IdMovimento.ToString() + " ";
                SqlNotasFiscais += " and nf.id_nfe_status in (8,17)";
                List<Db.gc_movimentos_nf> ListaMovimentosNF = db.gc_movimentos_nf.SqlQuery(SqlNotasFiscais).ToList();
                foreach (var RecordMovimentoNF in ListaMovimentosNF)
                {
                    if (RecordMovimentoNF != null)
                    {
                        // PDF
                        String NamePDFLocal = "Danfe - GDI Aviação - NFe nº " + RecordMovimentoNF.nf_numero.EmptyIfNull().ToString().Trim() + " - Pedido nº " + IdMovimento.ToString() + ".pdf";
                        String FileNamePDFLocal = Path.Combine(DirTempFiles, NamePDFLocal);
                        String FileNamePDFRemoto = RecordMovimentoNF.nf_url_pdf.EmptyIfNull().ToString();

                        if (FileNamePDFRemoto.Length > 10)
                        {
                            if (System.IO.File.Exists(FileNamePDFLocal)) { System.IO.File.Delete(FileNamePDFLocal); };
                            try
                            {
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile(FileNamePDFRemoto, FileNamePDFLocal);
                                    ListaAnexos.Add(FileNamePDFLocal);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Erro ao anexar documento Danfe [ " + ex.Message.ToString() + " ]");
                            }
                        }

                        // PDF
                        String NameXMLLocal = "XML - GDI Aviação - NFe nº " + RecordMovimentoNF.nf_numero.EmptyIfNull().ToString().Trim() + " - Pedido nº " + IdMovimento.ToString() + ".xml";
                        String FileNameXMLLocal = Path.Combine(DirTempFiles, NameXMLLocal);
                        String FileNameXMLRemoto = RecordMovimentoNF.nf_url_xml.EmptyIfNull().ToString();
                        if (FileNameXMLRemoto.Length > 10)
                        {
                            if (System.IO.File.Exists(FileNameXMLLocal)) { System.IO.File.Delete(FileNameXMLLocal); };
                            try
                            {
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile(FileNameXMLRemoto, FileNameXMLLocal);
                                    ListaAnexos.Add(FileNameXMLLocal);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Erro ao anexar documento XML [ " + ex.Message.ToString() + " ]");
                            }
                        }
                    }
                }

                // Boletos
                SqlBoletos += " select * from gc_financeiro_lancamentos l ";
                SqlBoletos += " where ativo = 1 ";
                SqlBoletos += " and id_financeiro_status = 3 "; // Aberto
                SqlBoletos += " and id_pag_rec_tipo = 3 ";  // Boleto
                SqlBoletos += " and l.id_movimento = " + IdMovimento.ToString() + " ";
                List<Db.gc_financeiro_lancamentos> ListaBoletos = db.gc_financeiro_lancamentos.SqlQuery(SqlBoletos).ToList();
                foreach (var RecordBoleto in ListaBoletos)
                {
                    if (RecordBoleto != null)
                    {
                        String NameBoletoLocal = "Boleto - GDI Aviação - Pedido nº "+ IdMovimento.ToString() + " - Venc " + RecordBoleto.data_vencimento.ToString("dd-MM-yyyy") + ".pdf";
                        String FileNameBoletoLocal = Path.Combine(DirTempFiles, NameBoletoLocal);
                        if (System.IO.File.Exists(FileNameBoletoLocal)) { System.IO.File.Delete(FileNameBoletoLocal); };
                        if (GerarAnexoBoletoPDF(RecordBoleto.id_lancamento, FileNameBoletoLocal) == true)
                        {
                            ListaAnexos.Add(FileNameBoletoLocal);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return ListaAnexos;
        }
        public bool GerarAnexoBoletoPDF(int IdLancamento, String FileNameBoletoLocal)
        {
            bool Sucesso = false;
            String MsgRetorno = "AjaxFinanceiroBoletoGCPDF";
            String SentencaSQLFinanceiro = String.Empty;
            g_contas_caixas RecordContaCaixaBoleto = new Db.g_contas_caixas();

            try
            {
                gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(IdLancamento);

                if (record_gc_financeiro_lancamentos == null)
                {
                    Sucesso = false;
                    MsgRetorno = "Lançamento financeiro [" + IdLancamento.ToString() + "] não encontrado!";
                }
                else if (record_gc_financeiro_lancamentos.cnab_linha_digitavel.EmptyIfNull().ToString().Length == 0)
                {
                    Sucesso = false;
                    MsgRetorno = "Boleto [" + IdLancamento.ToString() + "] não encontrado!";
                }
                else
                {
                    DateTime dataAtual = LibDateTime.getDataHoraBrasilia();

                    // Contas Caixas
                    String SentencaSQLContasCaixas = String.Empty;
                    DataTable tableContasCaixas = null;
                    List<DataRow> allContasCaixas = null;
                    SentencaSQLContasCaixas = " select CC.*, CI.nome " +
                                              " from g_contas_caixas CC " +
                                              " join g_cidades CI on (CI.id_cidade = CC.id_cidade_com)";
                    tableContasCaixas = LibDB.GetDataTable(SentencaSQLContasCaixas, db);
                    allContasCaixas = tableContasCaixas.AsEnumerable().ToList();

                    if (record_gc_financeiro_lancamentos.boleto_banco == "237") { RecordContaCaixaBoleto = db.g_contas_caixas.Find(1); }
                    else if (record_gc_financeiro_lancamentos.boleto_banco == "341") { RecordContaCaixaBoleto = db.g_contas_caixas.Find(7); }

                    // Financeiro
                    DataTable TableFinanceiroLancamentos = null;
                    List<DataRow> AllFinanceiroLancamentos = null;
                    SentencaSQLFinanceiro = "select FL.*, " +
                                            "CL.id_cliente as 'cliente.id_cliente', CL.nome as 'cliente.nome', CL.cpf as 'cliente.cpf', CL.cnpj as 'cliente.cnpj', " +
                                            "CL.endereco_com as 'cliente.endereco_com', CL.endereco_com_numero as 'cliente.endereco_com_numero', CL.endereco_com_complemento as 'cliente.endereco_com_complemento', CL.bairro_com as 'cliente.bairro_com', CI.nome as 'cliente.cidade_com', " +
                                            "CL.cep_com as 'cliente.cep_com', UF.sigla as 'cliente.uf_com' " +
                                            "from gc_financeiro_lancamentos FL " +
                                            "join g_clientes CL on (CL.id_cliente = FL.id_cliente) " +
                                            "join g_cidades CI on (CI.id_cidade = CL.id_cidade_com) " +
                                            "join g_uf UF on (UF.id_uf = CL.id_uf_com) ";
                    if (IdLancamento > 0)
                    {
                        SentencaSQLFinanceiro += "where FL.id_lancamento = " + IdLancamento.EmptyIfNull().ToString();
                    }
                    TableFinanceiroLancamentos = LibDB.GetDataTable(SentencaSQLFinanceiro, db);
                    AllFinanceiroLancamentos = TableFinanceiroLancamentos.AsEnumerable().ToList();

                    if (AllFinanceiroLancamentos.Count > 0)
                    {
                        foreach (var dsRowFinanceiro in AllFinanceiroLancamentos)
                        {
                            cstFinanceiroBoletos record_cstFinanceiroBoletos = new cstFinanceiroBoletos();
                            record_cstFinanceiroBoletos.idFinanceiro = IdLancamento;
                            int idContaCaixa = 0;
                            int.TryParse(dsRowFinanceiro["id_conta_caixa"].EmptyIfNull().ToString(), out idContaCaixa);
                            var dsRowContaCaixa = tableContasCaixas.Select("id_conta_caixa = " + idContaCaixa).FirstOrDefault();

                            // Cabeçalho
                            record_cstFinanceiroBoletos.EDadosCabecalho1 = "FATURA - PEDIDO Nº " + dsRowFinanceiro["id_movimento"].EmptyIfNull().ToString().ToUpper();
                            record_cstFinanceiroBoletos.ECedenteNome = RecordContaCaixaBoleto.razao_social.EmptyIfNull().ToString().ToUpper();
                            record_cstFinanceiroBoletos.ECedenteComplemento1 = RecordContaCaixaBoleto.endereco_com.EmptyIfNull().ToString().ToUpper() + " " + RecordContaCaixaBoleto.bairro_com.EmptyIfNull().ToString().ToUpper() + " CEP: " + RecordContaCaixaBoleto.cep_com.EmptyIfNull().ToString().ToUpper();
                            record_cstFinanceiroBoletos.ECedenteComplemento2 = "BELO HORIZONTE - MG";

                            // Cliente
                            record_cstFinanceiroBoletos.EClienteNome = dsRowFinanceiro["cliente.nome"].EmptyIfNull().ToString().ToUpper();
                            if (dsRowFinanceiro["cliente.cpf"].EmptyIfNull().ToString().Trim() != String.Empty) { record_cstFinanceiroBoletos.EClienteDocumento = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("F", dsRowFinanceiro["cliente.cpf"].EmptyIfNull().ToString().Trim()); }
                            else { record_cstFinanceiroBoletos.EClienteDocumento = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("J", dsRowFinanceiro["cliente.cnpj"].EmptyIfNull().ToString().Trim()); }
                            record_cstFinanceiroBoletos.EClienteCodigo = dsRowFinanceiro["cliente.id_cliente"].EmptyIfNull().ToString();
                            String _clienteEndereco = string.Empty;
                            String _clienteEnderecoCidadeUF = string.Empty;
                            _clienteEndereco += dsRowFinanceiro["cliente.endereco_com"].EmptyIfNull().ToString().ToUpper() + ", " + dsRowFinanceiro["cliente.endereco_com_numero"].EmptyIfNull().ToString().ToUpper() + " " + dsRowFinanceiro["cliente.endereco_com_complemento"].EmptyIfNull().ToString().ToUpper() + " ";
                            _clienteEndereco += dsRowFinanceiro["cliente.bairro_com"].EmptyIfNull().ToString().ToUpper() + " ";
                            _clienteEnderecoCidadeUF += dsRowFinanceiro["cliente.cidade_com"].EmptyIfNull().ToString().ToUpper() + " ";
                            _clienteEnderecoCidadeUF += dsRowFinanceiro["cliente.uf_com"].EmptyIfNull().ToString().ToUpper() + " ";
                            _clienteEnderecoCidadeUF += "CEP " + GdiPlataform.Lib.LibStringFormat.FormatarCEP(dsRowFinanceiro["cliente.cep_com"].EmptyIfNull().ToString().ToUpper());

                            record_cstFinanceiroBoletos.EClienteEndereco = _clienteEndereco;
                            record_cstFinanceiroBoletos.EClienteEnderecoCidadeUF = _clienteEnderecoCidadeUF;

                            decimal ValorTotal = 0;
                            DateTime DataVencimento = LibDateTime.getDataHoraBrasilia();
                            DateTime DataProcessamento = DataVencimento;

                            g_produtos extratoProdutoConsumoMinimo = new Db.g_produtos();
                            g_produtos extratoProdutoConsolidador = new Db.g_produtos();
                            decimal.TryParse(dsRowFinanceiro["valor_total"].EmptyIfNull().ToString(), out ValorTotal);
                            DateTime.TryParse(dsRowFinanceiro["data_vencimento"].EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataVencimento);

                            // SubTotal
                            //record_cstFinanceiroBoletos.EClienteMensagem = dsRowContaCaixa["mensagem_cliente"].EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.EClienteMensagem = RecordContaCaixaBoleto.mensagem_cliente.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.EValorLiquido = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowFinanceiro["valor_total"].EmptyIfNull().ToString()));
                            record_cstFinanceiroBoletos.EValorBruto = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowFinanceiro["valor_total"].EmptyIfNull().ToString()));

                            // MensagemCaixa
                            decimal ValorMulta = ((ValorTotal / 100) * 2);
                            decimal ValorJuros = ((ValorTotal / 100) * 1);
                            String MsgCaixa = String.Empty;
                            MsgCaixa += "Sr(a). Cliente pague esse título até o vencimento em toda a rede bancária, <br/> casas lotéricas, caixas eletrônicos, internet banking ou aplicativo do seu banco" + "<br><br>";
                            MsgCaixa += "O Não pagamento até o vencimento <b>" + DataVencimento.ToString("dd/MM/yy") + "</b> acarretará a cobrança de multa no valor de " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorMulta) + ",<br>"; ;
                            MsgCaixa += "Acrescido de juros diários de " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorJuros) + "." + "<br>";

                            // Boleto
                            if (RecordContaCaixaBoleto.banco.EmptyIfNull().ToString() == "341") { record_cstFinanceiroBoletos.ECodBanco = "341-7"; }
                            else if (RecordContaCaixaBoleto.banco.EmptyIfNull().ToString() == "237") { record_cstFinanceiroBoletos.ECodBanco = "237-0"; }
                            else { record_cstFinanceiroBoletos.ECodBanco = RecordContaCaixaBoleto.banco.EmptyIfNull().ToString(); }
                            record_cstFinanceiroBoletos.ELogoBanco = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/banco" + record_gc_financeiro_lancamentos.boleto_banco.EmptyIfNull().ToString() + ".png";
                            record_cstFinanceiroBoletos.ELogoBoletoBanco = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/LogoBoleto" + record_gc_financeiro_lancamentos.boleto_banco.EmptyIfNull().ToString() + ".png";
                            record_cstFinanceiroBoletos.EDataVencimento = DataVencimento.ToString("dd/MM/yy");
                            record_cstFinanceiroBoletos.ECedenteNome = RecordContaCaixaBoleto.razao_social.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.ECedenteComplemento1 = RecordContaCaixaBoleto.endereco_com.EmptyIfNull().ToString().ToUpper() + " " + RecordContaCaixaBoleto.bairro_com.EmptyIfNull().ToString().ToUpper();
                            record_cstFinanceiroBoletos.ECedenteComplemento2 = " CEP: " + RecordContaCaixaBoleto.cep_com.EmptyIfNull().ToString().ToUpper() + " CNPJ: " + GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("J", RecordContaCaixaBoleto.cnpj.EmptyIfNull().ToString().ToUpper());
                            record_cstFinanceiroBoletos.EAgenciaCodCedente = GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularAgenciaCodigoCedente(RecordContaCaixaBoleto.banco.EmptyIfNull().ToString(), RecordContaCaixaBoleto.agencia.EmptyIfNull().ToString(), RecordContaCaixaBoleto.dv_agencia.EmptyIfNull().ToString(), RecordContaCaixaBoleto.conta.EmptyIfNull().ToString(), RecordContaCaixaBoleto.dv_conta.EmptyIfNull().ToString(), RecordContaCaixaBoleto.codigo_convenio.EmptyIfNull().ToString());
                            record_cstFinanceiroBoletos.EDataDocumento = DataProcessamento.ToString("dd/MM/yy");
                            record_cstFinanceiroBoletos.ENossoNumeroDV = dsRowFinanceiro["cnab_nosso_numero"].EmptyIfNull().ToString();
                            if (dsRowFinanceiro["numero_documento"].EmptyIfNull().ToString().Length > 0) { record_cstFinanceiroBoletos.ENumeroDocumento = "NF " + dsRowFinanceiro["numero_documento"].EmptyIfNull().ToString(); } else { record_cstFinanceiroBoletos.ENumeroDocumento = dsRowFinanceiro["descricao"].EmptyIfNull().ToString(); }
                            record_cstFinanceiroBoletos.EEspecieDoc = "NS";
                            record_cstFinanceiroBoletos.EAceite = "N";
                            record_cstFinanceiroBoletos.EDataProcessamento = DataProcessamento.ToString("dd/MM/yy");
                            record_cstFinanceiroBoletos.ENossoNumeroDV = dsRowFinanceiro["cnab_nosso_numero"].EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.ECarteira = RecordContaCaixaBoleto.carteira.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.EEspecieMoeda = RecordContaCaixaBoleto.especie_moeda.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.EValorTotal = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorTotal);
                            record_cstFinanceiroBoletos.EMensagemCaixa = MsgCaixa;
                            record_cstFinanceiroBoletos.ENomeSacado = dsRowFinanceiro["cliente.nome"].EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.EEnderecoSacado = dsRowFinanceiro["cliente.endereco_com"].EmptyIfNull().ToString().Trim() + "," + dsRowFinanceiro["cliente.endereco_com_numero"].EmptyIfNull().ToString().Trim() + " " + dsRowFinanceiro["cliente.endereco_com_complemento"].EmptyIfNull().ToString().Trim() + " " + dsRowFinanceiro["cliente.bairro_com"].EmptyIfNull().ToString().Trim();
                            record_cstFinanceiroBoletos.ECidadeSacado = dsRowFinanceiro["cliente.cidade_com"].EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.ECepSacado = GdiPlataform.Lib.LibStringFormat.FormatarCEP(dsRowFinanceiro["cliente.cep_com"].EmptyIfNull().ToString());
                            record_cstFinanceiroBoletos.EUFSacado = dsRowFinanceiro["cliente.uf_com"].EmptyIfNull().ToString();
                            if (dsRowFinanceiro["cliente.cpf"].EmptyIfNull().ToString().Trim() != String.Empty)
                            { record_cstFinanceiroBoletos.EDocSacado = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("F", dsRowFinanceiro["cliente.cpf"].EmptyIfNull().ToString().Trim()); }
                            else { record_cstFinanceiroBoletos.EDocSacado = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("J", dsRowFinanceiro["cliente.cnpj"].EmptyIfNull().ToString().Trim()); }
                            record_cstFinanceiroBoletos.ELinhaDigitavel = GdiPlataform.Lib.LibStringFormat.FormatarLinhaDigitavel(dsRowFinanceiro["cnab_linha_digitavel"].EmptyIfNull().ToString());
                            record_cstFinanceiroBoletos.ECodigoBarras = GdiPlataform.Lib.LibCnabBancario.GetCodigoBarras(record_cstFinanceiroBoletos.ELinhaDigitavel);
                            record_cstFinanceiroBoletos.EImgBarCode = LibBoletos.Generate_barcode(record_cstFinanceiroBoletos.ECodigoBarras, DateTime.Parse(record_cstFinanceiroBoletos.EDataVencimento), Server.MapPath("~/_filestemp"));
                            if (dsRowFinanceiro["pix_base64"].EmptyIfNull().ToString().Trim().Length > 0)
                            {
                                record_cstFinanceiroBoletos.HasPix = true;
                                record_cstFinanceiroBoletos.EPixEMV = dsRowFinanceiro["pix_emv"].EmptyIfNull().ToString().Trim().Replace(" ", "&nbsp;");
                                record_cstFinanceiroBoletos.EImgPixQrCode = LibBoletos.Generate_PixQrCode(record_cstFinanceiroBoletos.idFinanceiro, dsRowFinanceiro["pix_base64"].EmptyIfNull().ToString().Trim(), DateTime.Parse(record_cstFinanceiroBoletos.EDataVencimento), Server.MapPath("~/_filestemp"));
                            };
                            ViewBag.Title = "Boleto - " + record_cstFinanceiroBoletos.EClienteNome.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.printPDF = true;

                            //String TemplateBoleto = "BoletoPDF";
                            String TemplateBoleto = "BoletoPdfFebraban";

                            var pdf = new ViewAsPdf
                            {
                                //ViewName = "BoletoPDF",
                                ViewName = TemplateBoleto,
                                Model = record_cstFinanceiroBoletos
                            };
                            // Criar o PDF
                            byte[] applicationPDFData = pdf.BuildFile(ControllerContext);
                            var fileStream = new FileStream(FileNameBoletoLocal, FileMode.Create, FileAccess.Write);
                            fileStream.Write(applicationPDFData, 0, applicationPDFData.Length);
                            fileStream.Close();

                            MsgRetorno = "Boleto Gerado com Sucesso!";
                            Sucesso = true;
                        }
                    }
                    else
                    {
                        Sucesso = false;
                        MsgRetorno = "Boleto [" + IdLancamento + "] não encontrado!";
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Sucesso;
        }

        [HttpPost]
        public ActionResult AjaxFechamentoCompetenciaEstoque(CstPedidoSeparacao RecordViewPedidoSeparacao)
        {
            bool Sucesso = false;
            int QtdInconsistencias = 0;
            DateTime DataInicioCompetenciaAberta;
            DateTime DataFimCompetenciaAberta;
            DateTime DataInicioCompetenciaAtual = LibDateTime.getPrimeiroDiaMesAtual();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String MsgRetorno = string.Empty;

            try
            {
                gc_estoque_competencia RecordEstoqueCompetenciaAtual = db.gc_estoque_competencia.Where(e => e.status == "A").FirstOrDefault();
                if (RecordEstoqueCompetenciaAtual != null)
                {
                    DataInicioCompetenciaAberta = RecordEstoqueCompetenciaAtual.data_inicio;
                    DataFimCompetenciaAberta = RecordEstoqueCompetenciaAtual.data_fim;

                    if (DataInicioCompetenciaAberta.AddMonths(1) != DataInicioCompetenciaAtual)
                    {
                        MsgRetorno += "Competências divergentes, Atual " + DataInicioCompetenciaAtual.ToString("MMM/yyyy", new CultureInfo("pt-BR")).ToUpperInvariant()  + " Aberta: "+ DataInicioCompetenciaAberta.ToString("MMM/yyyy", new CultureInfo("pt-BR")).ToUpperInvariant() + "<br/>";
                        QtdInconsistencias += 0;
                    }
                    else
                    {
                        String SqlDeleteSaldo = "delete from gc_estoque_competencia_saldo where id_estoque_competencia = "+ RecordEstoqueCompetenciaAtual.id_estoque_competencia + " and id_estoque_competencia_saldo > 0";
                        int QtdRowsDeleteSaldo = LibDB.dbQueryExec(SqlDeleteSaldo, db);

                        String SqlAtualizaSaldo = "INSERT INTO gc_estoque_competencia_saldo " +
                                                    "( " +
                                                    "    id_estoque_competencia, " +
                                                    "    id_produto, " +
                                                    "    saldo_01_disponivel, " +
                                                    "    saldo_01_consignado, " +
                                                    "    saldo_01_reservado, " +
                                                    "    saldo_01_separado, " +
                                                    "    saldo_01_quarentena, " +
                                                    "    saldo_03_disponivel, " +
                                                    "    saldo_03_consignado, " +
                                                    "    saldo_03_reservado, " +
                                                    "    saldo_03_separado, " +
                                                    "    saldo_03_quarentena, " +
                                                    "    fob1_dollar) " +
                                                    "SELECT " +
                                                         RecordEstoqueCompetenciaAtual.id_estoque_competencia.EmptyIfNull().ToString() + ", " + 
                                                    "    id_produto, " +
                                                    "    ISNULL(saldo_01_disponivel, 0), " +
                                                    "    ISNULL(saldo_01_consignado, 0), " +
                                                    "    ISNULL(saldo_01_reservado, 0), " +
                                                    "    ISNULL(saldo_01_separado, 0), " +
                                                    "    ISNULL(saldo_01_quarentena, 0), " +
                                                    "    ISNULL(saldo_03_disponivel, 0), " +
                                                    "    ISNULL(saldo_03_consignado, 0), " +
                                                    "    ISNULL(saldo_03_reservado, 0), " +
                                                    "    ISNULL(saldo_03_separado, 0), " +
                                                    "    ISNULL(saldo_03_quarentena, 0), " +
                                                    "    fob1_dollar " +
                                                    "FROM g_produtos WHERE(g_produtos.saldo_01_disponivel > 0 or g_produtos.saldo_03_disponivel > 0)";
                        int QtdRowsAtualizado = LibDB.dbQueryExec(SqlAtualizaSaldo, db);
                        MsgRetorno += QtdRowsAtualizado.ToString() + " Produtos com saldo atualizados!" + "<br/>";


                        RecordEstoqueCompetenciaAtual.status = "F";
                        RecordEstoqueCompetenciaAtual.id_usuario_fechamento = CachePersister.userIdentity.IdUsuario;
                        RecordEstoqueCompetenciaAtual.datahora_fechamento = DataHoraAtual;
                        db.Entry(RecordEstoqueCompetenciaAtual).State = EntityState.Modified;

                        gc_estoque_competencia RecordEstoqueNovaCompetencia = new Db.gc_estoque_competencia();
                        RecordEstoqueNovaCompetencia.ano = DataHoraAtual.Year;
                        RecordEstoqueNovaCompetencia.mes= DataHoraAtual.Month;
                        RecordEstoqueNovaCompetencia.data_inicio = LibDateTime.getPrimeiroDiaMesAtual().Date;
                        RecordEstoqueNovaCompetencia.data_fim = LibDateTime.getUltimoDiaMesAtual().Date;
                        RecordEstoqueNovaCompetencia.status = "A";
                        RecordEstoqueNovaCompetencia.id_usuario_atualizacao = CachePersister.userIdentity.IdUsuario;
                        RecordEstoqueNovaCompetencia.datahora_atualizacao = DataHoraAtual;
                        db.gc_estoque_competencia.Add(RecordEstoqueNovaCompetencia);
                        db.SaveChanges();

                        Sucesso = true;
                        MsgRetorno += "Fechamento de Estoque da Competência Anterior " + DataInicioCompetenciaAberta.ToString("MMM/yyyy", new CultureInfo("pt-BR")).ToUpperInvariant() + " EXECUTADO com sucesso!";
                    }
                }
                else
                {
                    MsgRetorno += "Competência Aberta não parametrizada!" + "<br/>";
                    QtdInconsistencias += 0;
                }
            }
            catch (DbEntityValidationException ex)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno += LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                QtdInconsistencias = 1;
                Sucesso = false;
                MsgRetorno += LibExceptions.getExceptionShortMessage(e);
            }

            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetDadosItensSeparacao(jQueryDataTableParamModel param)
        {
            string errorMessage = string.Empty;
            string stackTrace = string.Empty;
            string IconeSeparado = string.Empty;

            try
            {
                int idMovimento = 0;
                int.TryParse(param.yesCustomIdPK, out idMovimento);

                var baseQuery =
                    from mi in db.gc_movimentos_itens.AsNoTracking()
                    join p in db.g_produtos.AsNoTracking() on mi.id_produto equals p.id_produto
                    where mi.id_movimento == idMovimento
                    select new
                    {
                        // item
                        mi.id_movimento_item,
                        mi.sequencia,
                        mi.quantidade,
                        mi.serial,
                        mi.id_estoque_lote_01,
                        mi.id_estoque_lote_02,
                        mi.id_estoque_lote_03,
                        mi.id_estoque_lote_04,
                        mi.id_estoque_lote_05,
                        mi.id_estoque_lote_06,
                        mi.id_estoque_lote_07,
                        mi.id_estoque_lote_08,
                        mi.id_estoque_lote_09,
                        mi.id_estoque_lote_10,
                        mi.id_estoque_lote_11,
                        mi.id_estoque_lote_12,
                        mi.id_estoque_lote_13,
                        mi.id_estoque_lote_14,
                        mi.id_estoque_lote_15,
                        mi.id_estoque_lote_16,
                        mi.id_estoque_lote_17,
                        mi.id_estoque_lote_18,
                        mi.id_estoque_lote_19,
                        mi.id_estoque_lote_20,
                        mi.id_estoque_lote_21,
                        mi.id_estoque_lote_22,
                        mi.id_estoque_lote_23,
                        mi.id_estoque_lote_24,
                        mi.id_estoque_lote_25,
                        mi.id_estoque_lote_26,
                        mi.id_estoque_lote_27,
                        mi.id_estoque_lote_28,
                        mi.id_estoque_lote_29,
                        mi.id_estoque_lote_30,
                        mi.id_estoque_lote_31,
                        mi.id_estoque_lote_32,
                        mi.id_estoque_lote_33,
                        mi.id_estoque_lote_34,
                        mi.id_estoque_lote_35,
                        mi.id_estoque_lote_36,
                        mi.id_estoque_lote_37,
                        mi.id_estoque_lote_38,
                        mi.id_estoque_lote_39,
                        mi.id_estoque_lote_40,
                        mi.id_estoque_lote_41,
                        mi.id_estoque_lote_42,
                        mi.id_estoque_lote_43,
                        mi.id_estoque_lote_44,
                        mi.id_estoque_lote_45,
                        mi.id_estoque_lote_46,
                        mi.id_estoque_lote_47,
                        mi.id_estoque_lote_48,
                        mi.id_estoque_lote_49,
                        mi.id_estoque_lote_50,
                        mi.lote01_qtd,
                        mi.lote02_qtd,
                        mi.lote03_qtd,
                        mi.lote04_qtd,
                        mi.lote05_qtd,
                        mi.lote06_qtd,
                        mi.lote07_qtd,
                        mi.lote08_qtd,
                        mi.lote09_qtd,
                        mi.lote10_qtd,
                        mi.lote11_qtd,
                        mi.lote12_qtd,
                        mi.lote13_qtd,
                        mi.lote14_qtd,
                        mi.lote15_qtd,
                        mi.lote16_qtd,
                        mi.lote17_qtd,
                        mi.lote18_qtd,
                        mi.lote19_qtd,
                        mi.lote20_qtd,
                        mi.lote21_qtd,
                        mi.lote22_qtd,
                        mi.lote23_qtd,
                        mi.lote24_qtd,
                        mi.lote25_qtd,
                        mi.lote26_qtd,
                        mi.lote27_qtd,
                        mi.lote28_qtd,
                        mi.lote29_qtd,
                        mi.lote30_qtd,
                        mi.lote31_qtd,
                        mi.lote32_qtd,
                        mi.lote33_qtd,
                        mi.lote34_qtd,
                        mi.lote35_qtd,
                        mi.lote36_qtd,
                        mi.lote37_qtd,
                        mi.lote38_qtd,
                        mi.lote39_qtd,
                        mi.lote40_qtd,
                        mi.lote41_qtd,
                        mi.lote42_qtd,
                        mi.lote43_qtd,
                        mi.lote44_qtd,
                        mi.lote45_qtd,
                        mi.lote46_qtd,
                        mi.lote47_qtd,
                        mi.lote48_qtd,
                        mi.lote49_qtd,
                        mi.lote50_qtd,                        // produto
                        ProdutoNome = p.nome
                    };
                int totalRecords = baseQuery.Count();
                var ListItens = baseQuery
                    .OrderBy(x => x.sequencia)
                    .ThenBy(x => x.id_movimento_item)
                    .ToList();
                var list = new List<string[]>(totalRecords);

                foreach (var item in ListItens)
                {
                    IconeSeparado = string.Empty;
                    string InfoLotes = string.Empty;
                    string NomeProduto = (item.ProdutoNome ?? "").Trim();
                    if (NomeProduto.Length > 100) { NomeProduto = NomeProduto.Substring(0, 100) + "..."; };
                    string serial = (item.serial ?? "").Trim();
                    if (serial.Length > 0) NomeProduto += " [Serial: " + serial + "]";

                    var tipoItem = item.GetType();
                    decimal somaLotes = 0;

                    for (int i = 1; i <= 50; i++)
                    {
                        string loteNum = i.ToString("D2");
                        string propId = $"id_estoque_lote_{loteNum}";
                        string propQtd = $"lote{loteNum}_qtd";
                        var qtd = Convert.ToDecimal(tipoItem.GetProperty(propQtd)?.GetValue(item) ?? 0);
                        somaLotes += qtd;
                        var idEstoqueLote = Convert.ToInt32(tipoItem.GetProperty(propId)?.GetValue(item) ?? 0);
                        if (idEstoqueLote > 0)
                        {
                            gc_estoque_lotes RecordLote = db.gc_estoque_lotes.Find(idEstoqueLote);
                            if (RecordLote != null)
                            {
                                if (InfoLotes.Length > 0) { InfoLotes += " | "; }

                                InfoLotes += "Qtd: " + qtd.ToString("N0")
                                           + "  Lote: " + RecordLote.codigo_lote.EmptyIfNull()
                                           + "  Venc: " + RecordLote.data_validade.GetValueOrDefault().ToString("dd/MM/yy");

                                if (RecordLote.codigo_serial.EmptyIfNull().Length > 0)
                                    InfoLotes += "  Serial: " + RecordLote.codigo_serial.EmptyIfNull();

                                InfoLotes = InfoLotes.Trim();
                            }
                        }
                    }
                    if (InfoLotes.Length > 0) NomeProduto += "<br/><span style='color:#008000;'>Lotes Separados [ " + InfoLotes + " ]</span>";
                    if (item.quantidade == somaLotes) IconeSeparado = LibIcons.getIcon("fa-solid fa-dolly", "Separado", "#008000", "");
                    list.Add(new[]
                    {
                        item.id_movimento_item.ToString(),
                        item.quantidade.ToString("N0"),
                        NomeProduto,
                        IconeSeparado,
                        ""  // Botão Lote
                    });
                }

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException ex)
            {
                errorMessage = LibExceptions.getDbEntityValidationException(ex);
                stackTrace = ex.ToString();
            }
            catch (WebException ex)
            {
                errorMessage = LibExceptions.getWebException(ex);
                stackTrace = ex.ToString();
            }
            catch (Exception ex)
            {
                errorMessage = LibExceptions.getExceptionShortMessage(ex);
                stackTrace = ex.ToString();
            }

            return Json(new
            {
                errorMessage,
                severity = "error",
                stackTrace, // em produção você pode retornar ""
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }

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




