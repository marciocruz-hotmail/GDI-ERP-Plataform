// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_FinanceiroLancamentos_*,g_FinanceiroLancamentos_Default")]
    public class FinanceiroLancamentosController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_FinanceiroLancamentos";

        public FinanceiroLancamentosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region AjaxDadosTituloFinanceiroEdicao
        public ActionResult AjaxDadosTituloFinanceiroEdicao(g_financeiro record_g_financeiro)
        {

            var dataSetDadosTituloFinanceiroEdicao = new cstDadosTituloFinanceiroEdicao();

            var allRecords = (from _financeiro in db.g_financeiro
                              where (_financeiro.id_cliente == record_g_financeiro.id_cliente && _financeiro.id_financeiro_status == 10)
                              select new { financeiro = _financeiro }).ToList();


            foreach (var record in allRecords)
            {
                dataSetDadosTituloFinanceiroEdicao.id_financeiro = record.financeiro.id_financeiro;
                dataSetDadosTituloFinanceiroEdicao.id_cliente = record.financeiro.id_cliente;
                dataSetDadosTituloFinanceiroEdicao.data_vencimento = Convert.ToString(record.financeiro.data_vencimento);
                dataSetDadosTituloFinanceiroEdicao.valor_encargos = record.financeiro.valor_encargos;
            }

            return Json(dataSetDadosTituloFinanceiroEdicao, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Index
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_FinanceiroLancamentos_*,g_FinanceiroLancamentos_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Financeiro - Lançamentos";
            PreencherLookupsIndex();
            cstFinanceiroLancamentosIndex record_cstFinanceiroLancamentosIndex = new cstFinanceiroLancamentosIndex();
            record_cstFinanceiroLancamentosIndex.LancamentosIndex_id_cliente = 0;
            record_cstFinanceiroLancamentosIndex.LancamentosIndex_data1 = LibDateTime.getPrimeiroDiaMesPassado();
            record_cstFinanceiroLancamentosIndex.LancamentosIndex_data2 = LibDateTime.getUltimoDiaMesPassado();
            return View(record_cstFinanceiroLancamentosIndex);
        }

        public void PreencherLookupsIndex()
        {
            var comboClientes = new List<SelectListItem>();
            try
            {
                IQueryable<g_clientes> listaDbClientes = null;
                if (CachePersister.userIdentity.IdPerfil == 1) { listaDbClientes = db.g_clientes.Where(p => p.ativo == true).OrderBy(p => p.id_cliente); }
                else { listaDbClientes = db.g_clientes.Where(p => p.ativo == true).OrderBy(p => p.id_cliente); };
                comboClientes.Add(new SelectListItem { Value = "0", Text = "[ TODOS ]" });
                foreach (g_clientes item1 in listaDbClientes)
                {
                    comboClientes.Add(new SelectListItem { Value = item1.id_cliente.ToString(), Text = item1.id_cliente.ToString("0000") + " - " + item1.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboClientes = comboClientes;
        }
        #endregion

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_FinanceiroLancamentos_*,g_FinanceiroLancamentos_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            String filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.g_financeiro_lancamentos>();
            var allRecordsFinanceiroStatus = db.g_financeiro_status.Select(f => new { f.id_financeiro_status, f.nome }).ToList();
            List<g_financeiro_origem> allRecordsFinanceiroOrigem = db.g_financeiro_origem.Where(f => f.id_financeiro_origem > 0).ToList();
            var allRecordsProdutos = db.g_produtos.Select(p => new { p.id_produto, p.nome }).ToList();
            DateTime DataField02 = new DateTime();
            DateTime DataField03 = new DateTime();
            DateTime.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField02);
            DateTime.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField03);

            if (param.yesFilterField.EmptyIfNull().ToString().Trim().Equals("*")) // Usuário realizou uma pesquisa
            {
                String SentencaSQL = string.Empty;
                if ((param.yesCustomField01.EmptyIfNull().ToString().Trim() != String.Empty)
                && (param.yesCustomField02.EmptyIfNull().ToString().Trim() != String.Empty)
                && (param.yesCustomField03.EmptyIfNull().ToString().Trim() != String.Empty))
                {
                    SentencaSQL = "select f.* from g_financeiro_lancamentos f where f.id_financeiro_status <= 2 ";
                    SentencaSQL += " and f.data_lancamento between '" + DataField02.ToString("yyyy-MM-dd") + " 00:00:00" + "' and '" + DataField03.ToString("yyyy-MM-dd") + " 23:59:59'";
                    if (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "0")
                    { SentencaSQL += " and f.id_cliente = " + param.yesCustomField01.EmptyIfNull().ToString().Trim(); }
                    LibDB.setFilterByUser(SentencaSQL, controllerName, true, db);
                    allRecords = db.g_financeiro_lancamentos.SqlQuery(SentencaSQL.ToString()).ToList();
                    filterOnOff = "1";
                }
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_financeiro_lancamentos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 0 && param.iSortingCols > 0 ? Convert.ToString(c.id_cliente) :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_cliente); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_cliente); }
                }
            }

            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {
                string nomeProduto = String.Empty;
                var recordProduto = allRecordsProdutos.Find(p => p.id_produto == c.id_produto_servico);
                if (recordProduto != null) { nomeProduto = recordProduto.nome.EmptyIfNull().ToString(); };

                String SiglaOrigem = string.Empty;
                g_financeiro_origem record_g_financeiro_origem = allRecordsFinanceiroOrigem.Where(o => o.id_financeiro_origem == c.id_financeiro_origem).FirstOrDefault();
                if (record_g_financeiro_origem != null) { SiglaOrigem = record_g_financeiro_origem.sigla.EmptyIfNull().ToString(); };

                var recordSt = allRecordsFinanceiroStatus.FirstOrDefault(s => s.id_financeiro_status == c.id_financeiro_status);
                String nomeStatus = recordSt != null ? recordSt.nome.ToString() : string.Empty;

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_financeiro_lancamento.ToString(),
                                    nomeStatus,
                                    c.id_cliente.ToString(),
                                    nomeProduto,
                                    c.qtd.ToString(),
                                    LibStringFormat.FormatarMoedaReais(c.valor_unit_bruto).Replace("R$ ","").Replace("R$","").Replace("$",""),
                                    LibStringFormat.FormatarMoedaReais(c.valor_total_bruto).Replace("R$ ","").Replace("R$","").Replace("$",""),
                                    LibStringFormat.FormatarMoedaReais(c.valor_faturado).Replace("R$ ","").Replace("R$","").Replace("$",""),
                                    c.data_lancamento.ToString("dd/MM/yy"),
                                    SiglaOrigem,
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

        #region ModalIncluirLancamentos
        public ActionResult ModalIncluirLancamentos(String idCliente)
        {
            ViewBag.Title = "Incluir Lançamentos";
            preencherCombosModalIncluirLancamentos(int.Parse(idCliente));
            g_financeiro_lancamentos record_g_financeiro_lancamentos = new g_financeiro_lancamentos();
            record_g_financeiro_lancamentos.data_lancamento = LibDateTime.getDataHoraBrasilia();
            record_g_financeiro_lancamentos.id_cliente = int.Parse(idCliente);
            record_g_financeiro_lancamentos.id_financeiro_faturamento = db.g_financeiro_faturamentos.Max(f => f.id_financeiro_faturamento);
            record_g_financeiro_lancamentos.qtd = 1;
            return View(record_g_financeiro_lancamentos);
        }

        public void preencherCombosModalIncluirLancamentos(int idCliente)
        {
            var comboClientes = new List<SelectListItem>();
            try
            {
                IQueryable<g_clientes> listaDbClientes = db.g_clientes.Where(c => c.id_cliente == idCliente);
                foreach (g_clientes item1 in listaDbClientes)
                {
                    comboClientes.Add(new SelectListItem { Value = item1.id_cliente.ToString(), Text = item1.id_cliente.ToString("0000") + " - " + item1.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboClientes = comboClientes;

            var comboProdutosServicos = new List<SelectListItem>();
            try
            {
                IQueryable<g_produtos> listaDbProdutos = db.g_produtos.Select(p => p).OrderBy(p => p.nome);
                foreach (g_produtos item1 in listaDbProdutos)
                {
                    comboProdutosServicos.Add(new SelectListItem { Value = item1.id_produto.ToString(), Text = item1.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboProdutosServicos = comboProdutosServicos;

            var comboFinanceiroFaturamentos = new List<SelectListItem>();
            try
            {
                IQueryable<g_financeiro_faturamentos> listaDbFinanceiroFaturamentos = db.g_financeiro_faturamentos.Select(p => p).OrderByDescending(p => p.id_financeiro_faturamento);
                foreach (g_financeiro_faturamentos item1 in listaDbFinanceiroFaturamentos)
                {
                    comboFinanceiroFaturamentos.Add(new SelectListItem { Value = item1.id_financeiro_faturamento.ToString(), Text = item1.id_financeiro_faturamento.ToString() + " - " + item1.descricao.ToString() });
                }
            }
            finally { }
            ViewBag.comboFinanceiroFaturamentos = comboFinanceiroFaturamentos;

        }

        [HttpPost]
        public ActionResult ajaxIncluirLancamentos(g_financeiro_lancamentos view_g_financeiro_lancamentos)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            g_financeiro_faturamentos record_g_financeiro_faturamento = db.g_financeiro_faturamentos.Find(view_g_financeiro_lancamentos.id_financeiro_faturamento);

            try
            {
                g_financeiro_lancamentos record_g_financeiro_lancamento = new g_financeiro_lancamentos();
                record_g_financeiro_lancamento.tipo_pag_rec = 2;  // Receber
                record_g_financeiro_lancamento.id_financeiro = null;
                record_g_financeiro_lancamento.id_financeiro_status = 1; // Aberto
                record_g_financeiro_lancamento.id_processamento = null;
                record_g_financeiro_lancamento.id_financeiro_origem = 4; // Lançamentos Manuais
                record_g_financeiro_lancamento.id_financeiro_faturamento = view_g_financeiro_lancamentos.id_financeiro_faturamento;
                record_g_financeiro_lancamento.id_produto_servico = view_g_financeiro_lancamentos.id_produto_servico;
                record_g_financeiro_lancamento.id_cliente = view_g_financeiro_lancamentos.id_cliente;
                record_g_financeiro_lancamento.data_lancamento = record_g_financeiro_faturamento.data_final;
                record_g_financeiro_lancamento.data_vencimento = record_g_financeiro_faturamento.data_final;
                record_g_financeiro_lancamento.data_vencimento_original = record_g_financeiro_faturamento.data_final;
                record_g_financeiro_lancamento.qtd = view_g_financeiro_lancamentos.qtd;
                record_g_financeiro_lancamento.valor_unit_bruto = (view_g_financeiro_lancamentos.valor_unit_liquido);
                record_g_financeiro_lancamento.valor_total_bruto = (view_g_financeiro_lancamentos.qtd * view_g_financeiro_lancamentos.valor_unit_liquido);
                record_g_financeiro_lancamento.valor_unit_liquido = (view_g_financeiro_lancamentos.valor_unit_liquido);
                record_g_financeiro_lancamento.valor_total_liquido = (view_g_financeiro_lancamentos.qtd * view_g_financeiro_lancamentos.valor_unit_liquido);
                record_g_financeiro_lancamento.valor_reembolso = 0;
                record_g_financeiro_lancamento.valor_descontos = 0;
                record_g_financeiro_lancamento.valor_acrescimos = 0;
                record_g_financeiro_lancamento.parcela_numero = 1;
                record_g_financeiro_lancamento.parcelas_qtd = 1;
                record_g_financeiro_lancamento.fechado = false;
                record_g_financeiro_lancamento.id_conta_caixa_geracao = 0;
                record_g_financeiro_lancamento.id_coligada = 1;
                record_g_financeiro_lancamento.id_filial = 1;
                record_g_financeiro_lancamento.datahora_cadastro = DataHoraAtual;
                record_g_financeiro_lancamento.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.g_financeiro_lancamentos.Add(record_g_financeiro_lancamento);
                db.SaveChanges();
                sucesso = true;
                msgRetorno += "Novo lançamento <b>Incluído</b> com sucesso!";
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
        #endregion

        #region modalEditarLancamentos
        public ActionResult modalEditarLancamentos(String idLancamento)
        {
            ViewBag.Title = "Editar Lançamentos";
            g_financeiro_lancamentos record_g_financeiro_lancamento = db.g_financeiro_lancamentos.Find(int.Parse(idLancamento));
            preencherCombosModalEditarLancamentos(record_g_financeiro_lancamento);
            return View(record_g_financeiro_lancamento);
        }

        public void preencherCombosModalEditarLancamentos(g_financeiro_lancamentos record_g_financeiro_lancamento)
        {
            var comboClientes = new List<SelectListItem>();
            try
            {
                IQueryable<g_clientes> listaDbClientes = db.g_clientes.Where(c => c.id_cliente == record_g_financeiro_lancamento.id_cliente);
                foreach (g_clientes item1 in listaDbClientes)
                {
                    comboClientes.Add(new SelectListItem { Value = item1.id_cliente.ToString(), Text = item1.id_cliente.ToString("0000") + " - " + item1.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboClientes = comboClientes;

            var comboProdutosServicos = new List<SelectListItem>();
            try
            {
                IQueryable<g_produtos> listaDbProdutos = db.g_produtos.Where(p => p.id_produto == record_g_financeiro_lancamento.id_produto_servico);
                foreach (g_produtos item2 in listaDbProdutos)
                {
                    comboProdutosServicos.Add(new SelectListItem { Value = item2.id_produto.ToString(), Text = item2.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboProdutosServicos = comboProdutosServicos;
        }

        [HttpPost]
        public ActionResult ajaxEditarLancamentos(g_financeiro_lancamentos view_g_financeiro_lancamentos)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                g_financeiro_lancamentos record_g_financeiro_lancamento = db.g_financeiro_lancamentos.Find(view_g_financeiro_lancamentos.id_financeiro_lancamento);
                record_g_financeiro_lancamento.qtd = view_g_financeiro_lancamentos.qtd;
                record_g_financeiro_lancamento.valor_unit_bruto = (view_g_financeiro_lancamentos.valor_unit_liquido);
                record_g_financeiro_lancamento.valor_total_bruto = (view_g_financeiro_lancamentos.qtd * view_g_financeiro_lancamentos.valor_unit_liquido);
                record_g_financeiro_lancamento.valor_unit_liquido = (view_g_financeiro_lancamentos.valor_unit_liquido);
                record_g_financeiro_lancamento.valor_total_liquido = (view_g_financeiro_lancamentos.qtd * view_g_financeiro_lancamentos.valor_unit_liquido);
                record_g_financeiro_lancamento.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_financeiro_lancamento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_financeiro_lancamento).State = EntityState.Modified;
                db.SaveChanges();
                sucesso = true;
                msgRetorno += "Lançamento <b>Alterado</b> com sucesso!";
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
        #endregion

        #region ModalGerarFaturamento
        public ActionResult ModalGerarFaturamento(String id)
        {
            cstFinanceiroLancamentos modal_cstFinanceiroLancamentos = null;
            modal_cstFinanceiroLancamentos = new cstFinanceiroLancamentos
            {
                data_vencimento = DateTime.Now
            };
            preencherCombosModalGerarFaturamento();
            ViewBag.Title = "Gerar Faturamento - Todos os Títulos";
            return View(modal_cstFinanceiroLancamentos);
        }

        public void preencherCombosModalGerarFaturamento()
        {
            var comboContasCaixas = new List<SelectListItem>();
            try
            {
                IQueryable<g_contas_caixas> listaDbContasCaixas = db.g_contas_caixas.Where(p => p.boleto_emissao == true).OrderBy(p => p.nome);
                foreach (g_contas_caixas itemContaCaixa in listaDbContasCaixas)
                {
                    comboContasCaixas.Add(new SelectListItem { Value = itemContaCaixa.id_conta_caixa.ToString(), Text = itemContaCaixa.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboContasCaixas = comboContasCaixas;

            var comboFinanceiroFaturamentos = new List<SelectListItem>();
            try
            {
                IQueryable<g_financeiro_faturamentos> listaDbFinanceiroFaturamentos = db.g_financeiro_faturamentos.Where(p => p.id_financeiro_status == 1).OrderByDescending(p => p.id_financeiro_faturamento);
                foreach (g_financeiro_faturamentos item1 in listaDbFinanceiroFaturamentos)
                {
                    comboFinanceiroFaturamentos.Add(new SelectListItem { Value = item1.id_financeiro_faturamento.ToString(), Text = item1.id_financeiro_faturamento.ToString() + " - " + item1.descricao.ToString() });
                }
            }
            finally { }
            ViewBag.comboFinanceiroFaturamentos = comboFinanceiroFaturamentos;
        }

        [HttpPost]
        public ActionResult AjaxSimularGerarFaturamento(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos)
        {
            return ProcessoGerarFaturamento(modal_cstFinanceiroLancamentos, true);
        }

        [HttpPost]
        public ActionResult AjaxExecutarGerarFaturamento(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos)
        {
            return ProcessoGerarFaturamento(modal_cstFinanceiroLancamentos, false);
        }

        public ActionResult ProcessoGerarFaturamento(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos, Boolean simulacao)
        {
            return null;
        }
        #endregion

        #region ModalFecharLancamentosAbertos
        public ActionResult ModalFecharLancamentosAbertos(String id)
        {
            preencherCombosModalFecharLancamentosAbertos();
            ViewBag.Title = "Gerar Título para o Cliente (Fechar Lançamentos - Criando Novo Título)";
            return View();
        }

        public void preencherCombosModalFecharLancamentosAbertos()
        {
            var comboContasCaixas = new List<SelectListItem>();
            try
            {
                IQueryable<g_contas_caixas> listaDbContasCaixas = db.g_contas_caixas.Where(p => p.boleto_emissao == true).OrderBy(p => p.nome);
                foreach (g_contas_caixas itemContaCaixa in listaDbContasCaixas)
                {
                    comboContasCaixas.Add(new SelectListItem { Value = itemContaCaixa.id_conta_caixa.ToString(), Text = itemContaCaixa.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboContasCaixas = comboContasCaixas;

            var comboFinanceiroFaturamentos = new List<SelectListItem>();
            try
            {
                IQueryable<g_financeiro_faturamentos> listaDbFinanceiroFaturamentos = db.g_financeiro_faturamentos.Select(p => p).OrderByDescending(p => p.id_financeiro_faturamento);
                foreach (g_financeiro_faturamentos item1 in listaDbFinanceiroFaturamentos)
                {
                    comboFinanceiroFaturamentos.Add(new SelectListItem { Value = item1.id_financeiro_faturamento.ToString(), Text = item1.id_financeiro_faturamento.ToString() + " - " + item1.descricao.ToString() });
                }
            }
            finally { }
            ViewBag.comboFinanceiroFaturamentos = comboFinanceiroFaturamentos;

            var comboClientes = new List<SelectListItem>();
            try
            {
                var allRecordsClientes = (from _clientes in db.g_clientes
                                          join _financeiroLancamentos in db.g_financeiro_lancamentos on _clientes.id_cliente equals _financeiroLancamentos.id_cliente
                                          where (_clientes.ativo == true && _financeiroLancamentos.id_financeiro_status == 1)
                                          select new { clientes = _clientes }).Distinct().OrderBy(c => c.clientes.id_cliente).ToList();
                comboClientes.Add(new SelectListItem { Value = "0", Text = "[ SELECIONE O CLIENTE ]" });
                foreach (var record in allRecordsClientes)
                {
                    comboClientes.Add(new SelectListItem { Value = record.clientes.id_cliente.ToString(), Text = record.clientes.id_cliente.ToString("0000") + " - " + record.clientes.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboClientes = comboClientes;
        }

        [HttpPost]
        public ActionResult AjaxSimularFecharLancamentosAbertos(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos)
        {
            return ProcessoFecharLancamentosAbertos(modal_cstFinanceiroLancamentos, true);
        }

        [HttpPost]
        public ActionResult AjaxExecutarFecharLancamentosAbertos(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos)
        {
            return ProcessoFecharLancamentosAbertos(modal_cstFinanceiroLancamentos, false);
        }

        public ActionResult ProcessoFecharLancamentosAbertos(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos, Boolean simulacao)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            int idRetorno = 0;
            int qtdInconsistencias = 0;
            decimal valorProdutosConsultas = 0;
            decimal valorOutrosProdutos = 0;
            decimal valorEncargos = modal_cstFinanceiroLancamentos.valor_despesas_cobranca;

            String SentencaSQL = string.Empty;
            var allClientesSelecionados = new List<Db.g_clientes>();
            var allRecordsProdutos = new List<Db.g_produtos>();
            var allRecordsLancamentosSelecionados = new List<Db.g_financeiro_lancamentos>();
            g_financeiro new_g_financeiro = new g_financeiro();

            try
            {
                if (modal_cstFinanceiroLancamentos.id_cliente > 0)
                {
                    SentencaSQL = " select f.* from g_financeiro_lancamentos f where f.id_financeiro_status = 1 " +
                                    " and f.id_cliente = " + modal_cstFinanceiroLancamentos.id_cliente.ToString() +
                                    " and f.id_financeiro_faturamento = " + modal_cstFinanceiroLancamentos.id_financeiro_faturamento.ToString();
                    allRecordsLancamentosSelecionados = db.g_financeiro_lancamentos.SqlQuery(SentencaSQL.ToString()).ToList();
                    allRecordsProdutos = db.g_produtos.Where(p => p.id_produto > 0).ToList();
                }

                // Consistências - Data Vencimento
                DateTime dataAtual = DateTime.Parse(LibDateTime.getDataHoraBrasilia().ToShortDateString());
                DateTime dataVencimentoView = DateTime.Parse(modal_cstFinanceiroLancamentos.data_vencimento.ToString());
                if (dataVencimentoView < dataAtual)
                {
                    qtdInconsistencias += 1;
                    msgRetorno += " - Data Vencimento [ " + dataVencimentoView.ToString("dd/MM/yyyy") + " ] menor que data atual. <br/>";
                }
                if (modal_cstFinanceiroLancamentos.id_cliente <= 0)
                {
                    qtdInconsistencias += 1;
                    msgRetorno += " - Selecione o Cliente!<br/>";
                }
                else if (allRecordsLancamentosSelecionados.Count() == 0)
                {
                    qtdInconsistencias += 1;
                    msgRetorno += " - Não foram encontrados lançamentos que atendam a pesquisa realizada!<br/>";
                }

                if (qtdInconsistencias == 0)
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                    // Totalizar os Lançamentos
                    foreach (g_financeiro_lancamentos record_g_financeiro_lancamentos in allRecordsLancamentosSelecionados)
                    {
                        if (allRecordsProdutos.Find(p => p.id_produto == record_g_financeiro_lancamentos.id_produto_servico).id_produto_tipo == 3) // Verificar se o produto é do tipo consulta
                        {
                            valorProdutosConsultas += record_g_financeiro_lancamentos.valor_total_bruto;
                        }
                        else
                        {
                            valorOutrosProdutos += record_g_financeiro_lancamentos.valor_total_bruto;
                        }
                    }

                    if (simulacao == false)
                    {
                        if ((valorProdutosConsultas + valorOutrosProdutos) > 0) // Se tiver valor a faturar
                        {
                            decimal valorTotalLiquido = valorProdutosConsultas + valorOutrosProdutos;
                            decimal valorTotalBruto = valorTotalLiquido;

                            g_clientes record_g_cliente = db.g_clientes.Find(modal_cstFinanceiroLancamentos.id_cliente);

                            // Calculo de impostos
                            cstFinanceiroImpostos record_FinanceiroImpostos = new cstFinanceiroImpostos();
                            record_FinanceiroImpostos = Lib.LibFinanceiro.CalcularImpostos(record_g_cliente, valorTotalBruto);
                            valorTotalBruto += valorEncargos;
                            valorTotalBruto += record_FinanceiroImpostos.iss_valor;
                            valorTotalBruto += record_FinanceiroImpostos.ir_valor;
                            valorTotalBruto += record_FinanceiroImpostos.pis_valor;
                            valorTotalBruto += record_FinanceiroImpostos.cofins_valor;
                            valorTotalBruto += record_FinanceiroImpostos.csll_valor;
                            valorTotalBruto += record_FinanceiroImpostos.pcc_valor;
                            valorTotalBruto += record_FinanceiroImpostos.inss_valor;


                            // Criar o título financeiro
                            new_g_financeiro.tipo_pag_rec = 2;               // Receber
                            new_g_financeiro.id_financeiro_status = 1;       // Aberto
                            new_g_financeiro.id_financeiro_origem = 8;       // Financeiro
                            new_g_financeiro.id_financeiro_faturamento = modal_cstFinanceiroLancamentos.id_financeiro_faturamento;
                            new_g_financeiro.id_cliente = modal_cstFinanceiroLancamentos.id_cliente;
                            new_g_financeiro.data_processamento = DataHoraAtual;
                            new_g_financeiro.data_vencimento = dataVencimentoView;
                            new_g_financeiro.descricao = "";
                            new_g_financeiro.valor_total_liquido = valorTotalLiquido;
                            new_g_financeiro.valor_total_bruto = valorTotalBruto;
                            new_g_financeiro.valor_encargos = valorEncargos;
                            new_g_financeiro.id_conta_caixa_geracao = modal_cstFinanceiroLancamentos.id_conta_caixa;
                            new_g_financeiro.id_coligada = 1;
                            new_g_financeiro.id_filial = 1;
                            new_g_financeiro.geracao_manual = true;
                            // Impostos
                            new_g_financeiro.iss_percentual = record_FinanceiroImpostos.iss_percentual;
                            new_g_financeiro.iss_display = record_FinanceiroImpostos.iss_display;
                            new_g_financeiro.iss_valor = record_FinanceiroImpostos.iss_valor;
                            new_g_financeiro.ir_percentual = record_FinanceiroImpostos.ir_percentual;
                            new_g_financeiro.ir_display = record_FinanceiroImpostos.ir_display;
                            new_g_financeiro.ir_valor = record_FinanceiroImpostos.ir_valor;
                            new_g_financeiro.pis_percentual = record_FinanceiroImpostos.pis_percentual;
                            new_g_financeiro.pis_display = record_FinanceiroImpostos.pis_display;
                            new_g_financeiro.pis_valor = record_FinanceiroImpostos.pis_valor;
                            new_g_financeiro.cofins_percentual = record_FinanceiroImpostos.cofins_percentual;
                            new_g_financeiro.cofins_display = record_FinanceiroImpostos.cofins_display;
                            new_g_financeiro.cofins_valor = record_FinanceiroImpostos.cofins_valor;
                            new_g_financeiro.csll_percentual = record_FinanceiroImpostos.csll_percentual;
                            new_g_financeiro.csll_display = record_FinanceiroImpostos.csll_display;
                            new_g_financeiro.csll_valor = record_FinanceiroImpostos.csll_valor;
                            new_g_financeiro.pcc_percentual = record_FinanceiroImpostos.pcc_percentual;
                            new_g_financeiro.pcc_display = record_FinanceiroImpostos.pcc_display;
                            new_g_financeiro.pcc_valor = record_FinanceiroImpostos.pcc_valor;
                            new_g_financeiro.inss_percentual = record_FinanceiroImpostos.inss_percentual;
                            new_g_financeiro.inss_display = record_FinanceiroImpostos.inss_display;
                            new_g_financeiro.inss_valor = record_FinanceiroImpostos.inss_valor;
                            new_g_financeiro.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            new_g_financeiro.datahora_cadastro = DataHoraAtual;
                            db.g_financeiro.Add(new_g_financeiro);
                            db.SaveChanges(); // Salvar o título financeiro

                            // Fechar os lancamentos financeiros
                            foreach (g_financeiro_lancamentos record_g_financeiro_lancamentos in allRecordsLancamentosSelecionados)
                            {
                                record_g_financeiro_lancamentos.id_financeiro = new_g_financeiro.id_financeiro;
                                record_g_financeiro_lancamentos.id_financeiro_faturamento = modal_cstFinanceiroLancamentos.id_financeiro_faturamento;
                                record_g_financeiro_lancamentos.data_vencimento_original = record_g_financeiro_lancamentos.data_vencimento;
                                record_g_financeiro_lancamentos.id_financeiro_status = 2; // Fechado
                                record_g_financeiro_lancamentos.valor_faturado = record_g_financeiro_lancamentos.valor_total_bruto;
                                db.Entry(record_g_financeiro_lancamentos).State = EntityState.Modified;
                            }
                            db.SaveChanges(); // Salvar os lançamentos financeiros
                        }
                    }

                    idRetorno = 1;
                    sucesso = true;

                    if (simulacao == true)
                    {
                        msgRetorno = "<b>Processo SIMULADO com sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" +
                                     "R$ Serviços - Consultas:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorProdutosConsultas) + "<br/>";
                        if (valorOutrosProdutos > 0) { msgRetorno += "R$ Serviços - Demais Produtos:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorOutrosProdutos) + "<br/>"; }
                        if (valorEncargos > 0) { msgRetorno += "R$ Encargos:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorEncargos) + "<br/>"; }
                        msgRetorno += "R$ Total:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorProdutosConsultas + valorOutrosProdutos + valorEncargos) + "<br/>";
                        msgRetorno += "Data Venc:" + LibStringFormat.GetTabHtml(1) + modal_cstFinanceiroLancamentos.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy");
                    }
                    else
                    {
                        msgRetorno = "<b>Processo EXECUTADO com sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        msgRetorno += "Título financeiro gerado - Id: " + LibStringFormat.GetTabHtml(1) + new_g_financeiro.id_financeiro.ToString() + "<br/>";
                        msgRetorno += "R$ Serviços - Consultas:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorProdutosConsultas) + "<br/>";
                        if (valorOutrosProdutos > 0) { msgRetorno += "R$ Serviços - Demais Produtos:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorOutrosProdutos) + "<br/>"; }
                        if (valorEncargos > 0) { msgRetorno += "R$ Encargos:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorEncargos) + "<br/>"; }
                        msgRetorno += "R$ Total:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorProdutosConsultas + valorOutrosProdutos + valorEncargos) + "<br/>";
                        msgRetorno += "Data Venc:" + LibStringFormat.GetTabHtml(1) + modal_cstFinanceiroLancamentos.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy");
                    }
                }
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

            return Json(new { success = sucesso, msg = msgRetorno, idFinanceiro = idRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalFinalizarEdicaoTitulo
        public ActionResult ModalFinalizarEdicaoTitulo(String id)
        {
            preencherCombosModalFinalizarEdicaoTitulo();
            ViewBag.Title = "Concluir edição do título do Cliente (Fechar Lançamentos - Reabrir Título Em Edição)";
            return View();
        }

        public void preencherCombosModalFinalizarEdicaoTitulo()
        {
            var comboContasCaixas = new List<SelectListItem>();
            try
            {
                IQueryable<g_contas_caixas> listaDbContasCaixas = db.g_contas_caixas.Where(p => p.boleto_emissao == true).OrderBy(p => p.nome);
                foreach (g_contas_caixas itemContaCaixa in listaDbContasCaixas)
                {
                    comboContasCaixas.Add(new SelectListItem { Value = itemContaCaixa.id_conta_caixa.ToString(), Text = itemContaCaixa.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboContasCaixas = comboContasCaixas;

            var comboFinanceiroFaturamentos = new List<SelectListItem>();
            try
            {
                IQueryable<g_financeiro_faturamentos> listaDbFinanceiroFaturamentos = db.g_financeiro_faturamentos.Select(p => p).OrderByDescending(p => p.id_financeiro_faturamento);
                foreach (g_financeiro_faturamentos item1 in listaDbFinanceiroFaturamentos)
                {
                    comboFinanceiroFaturamentos.Add(new SelectListItem { Value = item1.id_financeiro_faturamento.ToString(), Text = item1.descricao.ToString() });
                }
            }
            finally { }
            ViewBag.comboFinanceiroFaturamentos = comboFinanceiroFaturamentos;

            var comboClientes = new List<SelectListItem>();
            try
            {
                var allRecordsClientes = (from _clientes in db.g_clientes
                                          join _financeiroLancamentos in db.g_financeiro_lancamentos on _clientes.id_cliente equals _financeiroLancamentos.id_cliente
                                          join _financeiro in db.g_financeiro on _clientes.id_cliente equals _financeiro.id_cliente
                                          where (_financeiroLancamentos.id_financeiro_status == 1 && _financeiro.id_financeiro_status == 10)
                                          orderby _clientes.id_cliente
                                          select new { clientes = _clientes }).Distinct().ToList();
                comboClientes.Add(new SelectListItem { Value = "0", Text = "[ SELECIONE O CLIENTE ]" });
                foreach (var record in allRecordsClientes)
                {
                    comboClientes.Add(new SelectListItem { Value = record.clientes.id_cliente.ToString(), Text = record.clientes.id_cliente.ToString("0000") + " - " + record.clientes.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboClientes = comboClientes;
        }

        [HttpPost]
        public ActionResult AjaxSimularFinalizarEdicaoTitulo(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos)
        {
            return AjaxFecharFinalizarEdicaoTitulo(modal_cstFinanceiroLancamentos, true);
        }

        [HttpPost]
        public ActionResult AjaxExecutarFinalizarEdicaoTitulo(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos)
        {
            return AjaxFecharFinalizarEdicaoTitulo(modal_cstFinanceiroLancamentos, false);
        }

        public ActionResult AjaxFecharFinalizarEdicaoTitulo(cstFinanceiroLancamentos modal_cstFinanceiroLancamentos, Boolean simulacao)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            int idRetorno = 0;
            int qtdInconsistencias = 0;
            decimal valorProdutosConsultas = 0;
            decimal valorOutrosProdutos = 0;
            decimal valorEncargos = modal_cstFinanceiroLancamentos.valor_despesas_cobranca;

            String SentencaSQL = string.Empty;
            var allClientesSelecionados = new List<Db.g_clientes>();
            var allRecordsProdutos = new List<Db.g_produtos>();
            var allRecordsLancamentosSelecionados = new List<Db.g_financeiro_lancamentos>();
            g_financeiro edit_g_financeiro = new g_financeiro();

            try
            {
                g_financeiro_faturamentos record_g_financeiro_faturamento = db.g_financeiro_faturamentos.Find(modal_cstFinanceiroLancamentos.id_financeiro_faturamento);

                if (modal_cstFinanceiroLancamentos.id_cliente > 0)
                {
                    SentencaSQL = " select f.* from g_financeiro_lancamentos f where f.id_financeiro_status = 1 " +
                                    " and f.id_cliente = " + modal_cstFinanceiroLancamentos.id_cliente.ToString() +
                                    " and f.id_financeiro_faturamento = " + modal_cstFinanceiroLancamentos.id_financeiro_faturamento.ToString();
                    allRecordsLancamentosSelecionados = db.g_financeiro_lancamentos.SqlQuery(SentencaSQL.ToString()).ToList();
                    allRecordsProdutos = db.g_produtos.Where(p => p.id_produto > 0).ToList();
                }

                // Consistências - Data Vencimento
                DateTime dataAtual = DateTime.Parse(LibDateTime.getDataHoraBrasilia().ToShortDateString());
                DateTime dataVencimentoView = DateTime.Parse(modal_cstFinanceiroLancamentos.data_vencimento.ToString());
                if (dataVencimentoView < dataAtual)
                {
                    qtdInconsistencias += 1;
                    msgRetorno += " - Data Vencimento [ " + dataVencimentoView.ToString("dd/MM/yyyy") + " ] menor que data atual. <br/>";
                }
                if (modal_cstFinanceiroLancamentos.id_cliente <= 0)
                {
                    qtdInconsistencias += 1;
                    msgRetorno += " - Selecione o Cliente!<br/>";
                }
                else if (allRecordsLancamentosSelecionados.Count() == 0)
                {
                    qtdInconsistencias += 1;
                    msgRetorno += " - Não foram encontrados lançamentos que atendam a pesquisa realizada!<br/>";
                }
                else
                {
                    IQueryable<g_financeiro> listaTitulosCliente = db.g_financeiro.Where(p => p.id_cliente == modal_cstFinanceiroLancamentos.id_cliente && p.id_financeiro_status == 10).OrderBy(p => p.id_financeiro);
                    if (listaTitulosCliente.Count() == 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Não foram encontrados Títulos em Edição para esse cliente!<br/>";
                    }
                    else
                    {
                        edit_g_financeiro = listaTitulosCliente.FirstOrDefault();
                    }
                }

                if (qtdInconsistencias == 0)
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                    // Totalizar os Lançamentos
                    foreach (g_financeiro_lancamentos record_g_financeiro_lancamentos in allRecordsLancamentosSelecionados)
                    {
                        if (allRecordsProdutos.Find(p => p.id_produto == record_g_financeiro_lancamentos.id_produto_servico).id_produto_tipo == 3) // Verificar se o produto é do tipo consulta
                        {
                            valorProdutosConsultas += record_g_financeiro_lancamentos.valor_total_bruto;
                        }
                        else
                        {
                            valorOutrosProdutos += record_g_financeiro_lancamentos.valor_total_bruto;
                        }
                    }

                    if (simulacao == false)
                    {
                        if ((valorProdutosConsultas + valorOutrosProdutos) > 0) // Se tiver valor a faturar
                        {
                            decimal valorTotalLiquido = valorProdutosConsultas + valorOutrosProdutos;
                            decimal valorTotalBruto = valorTotalLiquido;
                            g_clientes record_g_cliente = db.g_clientes.Find(modal_cstFinanceiroLancamentos.id_cliente);

                            // Calculo de impostos
                            cstFinanceiroImpostos record_FinanceiroImpostos = new cstFinanceiroImpostos();
                            record_FinanceiroImpostos = Lib.LibFinanceiro.CalcularImpostos(record_g_cliente, valorTotalBruto);
                            valorTotalBruto += record_g_cliente.valor_despesas_cobranca;
                            valorTotalBruto += record_FinanceiroImpostos.iss_valor;
                            valorTotalBruto += record_FinanceiroImpostos.ir_valor;
                            valorTotalBruto += record_FinanceiroImpostos.pis_valor;
                            valorTotalBruto += record_FinanceiroImpostos.cofins_valor;
                            valorTotalBruto += record_FinanceiroImpostos.csll_valor;
                            valorTotalBruto += record_FinanceiroImpostos.pcc_valor;
                            valorTotalBruto += record_FinanceiroImpostos.inss_valor;

                            // Editar o título financeiro
                            edit_g_financeiro.tipo_pag_rec = 2;               // Receber
                            edit_g_financeiro.id_financeiro_status = 1;       // Aberto
                            edit_g_financeiro.id_financeiro_origem = 8;       // Financeiro
                            edit_g_financeiro.data_processamento = DataHoraAtual;
                            edit_g_financeiro.data_vencimento = dataVencimentoView;
                            edit_g_financeiro.descricao = "";
                            edit_g_financeiro.valor_total_liquido = valorTotalLiquido;
                            edit_g_financeiro.valor_total_bruto = valorTotalBruto;
                            edit_g_financeiro.valor_encargos = valorEncargos;
                            edit_g_financeiro.id_conta_caixa_geracao = modal_cstFinanceiroLancamentos.id_conta_caixa;
                            edit_g_financeiro.geracao_manual = true;
                            // Impostos
                            edit_g_financeiro.iss_percentual = record_FinanceiroImpostos.iss_percentual;
                            edit_g_financeiro.iss_display = record_FinanceiroImpostos.iss_display;
                            edit_g_financeiro.iss_valor = record_FinanceiroImpostos.iss_valor;
                            edit_g_financeiro.ir_percentual = record_FinanceiroImpostos.ir_percentual;
                            edit_g_financeiro.ir_display = record_FinanceiroImpostos.ir_display;
                            edit_g_financeiro.ir_valor = record_FinanceiroImpostos.ir_valor;
                            edit_g_financeiro.pis_percentual = record_FinanceiroImpostos.pis_percentual;
                            edit_g_financeiro.pis_display = record_FinanceiroImpostos.pis_display;
                            edit_g_financeiro.pis_valor = record_FinanceiroImpostos.pis_valor;
                            edit_g_financeiro.cofins_percentual = record_FinanceiroImpostos.cofins_percentual;
                            edit_g_financeiro.cofins_display = record_FinanceiroImpostos.cofins_display;
                            edit_g_financeiro.cofins_valor = record_FinanceiroImpostos.cofins_valor;
                            edit_g_financeiro.csll_percentual = record_FinanceiroImpostos.csll_percentual;
                            edit_g_financeiro.csll_display = record_FinanceiroImpostos.csll_display;
                            edit_g_financeiro.csll_valor = record_FinanceiroImpostos.csll_valor;
                            edit_g_financeiro.pcc_percentual = record_FinanceiroImpostos.pcc_percentual;
                            edit_g_financeiro.pcc_display = record_FinanceiroImpostos.pcc_display;
                            edit_g_financeiro.pcc_valor = record_FinanceiroImpostos.pcc_valor;
                            edit_g_financeiro.inss_percentual = record_FinanceiroImpostos.inss_percentual;
                            edit_g_financeiro.inss_display = record_FinanceiroImpostos.inss_display;
                            edit_g_financeiro.inss_valor = record_FinanceiroImpostos.inss_valor;
                            edit_g_financeiro.id_coligada = 1;
                            edit_g_financeiro.id_filial = 1;
                            edit_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            edit_g_financeiro.datahora_alteracao = DataHoraAtual;
                            db.Entry(edit_g_financeiro).State = EntityState.Modified;

                            // Fechar os lancamentos financeiros
                            foreach (g_financeiro_lancamentos record_g_financeiro_lancamentos in allRecordsLancamentosSelecionados)
                            {
                                record_g_financeiro_lancamentos.id_financeiro = edit_g_financeiro.id_financeiro;
                                record_g_financeiro_lancamentos.id_financeiro_faturamento = modal_cstFinanceiroLancamentos.id_financeiro_faturamento;
                                record_g_financeiro_lancamentos.data_vencimento_original = record_g_financeiro_lancamentos.data_vencimento;
                                record_g_financeiro_lancamentos.id_financeiro_status = 2; // Fechado
                                record_g_financeiro_lancamentos.valor_faturado = record_g_financeiro_lancamentos.valor_total_bruto;
                                db.Entry(record_g_financeiro_lancamentos).State = EntityState.Modified;
                            }
                            db.SaveChanges(); // Salvar os lançamentos financeiros
                        }
                    }

                    idRetorno = 1;
                    sucesso = true;

                    if (simulacao == true)
                    {
                        msgRetorno += "<b>Processo SIMULADO com sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>"
                                    + "R$ Serviços - Consultas:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorProdutosConsultas) + "<br/>";
                        if (valorOutrosProdutos > 0) { msgRetorno += "R$ Serviços - Demais Produtos:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorOutrosProdutos) + "<br/>"; }
                        if (valorEncargos > 0) { msgRetorno += "R$ Encargos:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorEncargos) + "<br/>"; }
                        msgRetorno += "R$ Total:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorProdutosConsultas + valorOutrosProdutos + valorEncargos) + "<br/>";
                        msgRetorno += "Data Venc:" + LibStringFormat.GetTabHtml(1) + modal_cstFinanceiroLancamentos.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy");
                    }
                    else
                    {
                        msgRetorno += "<b>Processo EXECUTADO com sucesso!</b>" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        msgRetorno += "Título financeiro atualizado - Id:" + LibStringFormat.GetTabHtml(1) + edit_g_financeiro.id_financeiro.ToString() + "<br/>";
                        msgRetorno += "R$ Serviços - Consultas:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorProdutosConsultas) + "<br/>";
                        if (valorOutrosProdutos > 0) { msgRetorno += "R$ Serviços - Demais Produtos:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorOutrosProdutos) + "<br/>"; }
                        if (valorEncargos > 0) { msgRetorno += "R$ Encargos:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorEncargos) + "<br/>"; }
                        msgRetorno += "R$ Total:" + LibStringFormat.GetTabHtml(1) + LibStringFormat.FormatarMoedaReais(valorProdutosConsultas + valorOutrosProdutos + valorEncargos) + "<br/>";
                        msgRetorno += "Data Venc.:" + LibStringFormat.GetTabHtml(1) + modal_cstFinanceiroLancamentos.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy");
                    }
                }
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

            return Json(new { success = sucesso, msg = msgRetorno, idFinanceiro = idRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalCancelarLancamentos
        public ActionResult ModalCancelarLancamentos(String idLancamento)
        {
            ViewBag.Title = "Cancelar Lançamentos";
            g_financeiro_lancamentos record_g_financeiro_lancamento = db.g_financeiro_lancamentos.Find(int.Parse(idLancamento));
            return View(record_g_financeiro_lancamento);
        }

        [HttpPost]
        public ActionResult AjaxCancelarLancamentos(g_financeiro_lancamentos view_g_financeiro_lancamentos)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                g_financeiro_lancamentos record_g_financeiro_lancamento = db.g_financeiro_lancamentos.Find(view_g_financeiro_lancamentos.id_financeiro_lancamento);
                record_g_financeiro_lancamento.id_financeiro_status = 3; // Cancelado
                record_g_financeiro_lancamento.motivo_cancelamento = view_g_financeiro_lancamentos.motivo_cancelamento;
                record_g_financeiro_lancamento.datahora_cancelamento = LibDateTime.getDataHoraBrasilia();
                record_g_financeiro_lancamento.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_financeiro_lancamento).State = EntityState.Modified;
                db.SaveChanges();
                sucesso = true;
                msgRetorno += "Lançamento CANCELADO com sucesso!";
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