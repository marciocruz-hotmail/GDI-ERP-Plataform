using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
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
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    public class RelatoriosFinanceirosController : Controller
    {
        private GdiPlataformEntities db;
        private HSSFWorkbook _workbookCatalogo;
        public RelatoriosFinanceirosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatórios Financeiros";
            return View();
        }


        #region ModalRelatorioNotasFiscaisEmitidas
        public ActionResult ModalRelatorioLancamentosFinanceiros(int? id)
        {
            cstModalRelatorio view_cstModalRelatorio = new cstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            view_cstModalRelatorio.Field_Int_01 = 0;
            view_cstModalRelatorio.Field_Int_02 = 0;
            ViewBag.comboClientes = LibDataSets.LoadComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]" });
            ViewBag.comboContasCaixas = LibDataSets.LoadComboGContasCaixas(db);
            ViewBag.comboContasCaixas.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS AS CONTAS ]" });
            ViewBag.comboTipoPagRec = LibDataSets.LoadComboPagRecTiposFaturaveis(db);
            ViewBag.comboGcFinanceiroStatus = LibDataSets.LoadComboGcFinanceiroStatus(db);
            ViewBag.comboGcFinanceiroStatus.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS ]" });
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório Financeiro - Lançamentos";
            return View("ModalRelatorioLancamentosFinanceiros", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioLancamentosFinanceiros(cstModalRelatorio view_cstModalRelatorio)
        {

            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            String TituloRelatorio = string.Empty;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String DirTempFiles = String.Empty;
            String IdProcessamentoGravado = "0";
            DateTime DataInicial = new DateTime();
            DateTime DataFinal = new DateTime();
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataInicial);
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataFinal);
            String DataInicialSQL = DataInicial.ToString("yyyy-MM-dd 00:00:00");
            String DataFinalSQL = DataFinal.ToString("yyyy-MM-dd 23:59:59");
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_financeiro_lancamentos.xls");
            try
            {
                TituloRelatorio = "Relatório Financeiro";

                String TextSQL = " SELECT fl.id_lancamento, " +
                                    " contas.nome as 'conta_caixa', " +
                                    " status.nome as 'status', " +
                                    " FORMAT(fl.data_pagamento, 'dd/MM/yyyy', 'pt-BR') as 'data_pagamento', " +
                                    " case tipo_pag_rec " +
                                    "     when 1 then 'Pagamento' " +
                                    "     when 2 then 'Recebimento' " +
                                    "     Else 'Não Identificado' " +
                                    " end as 'pag_rec', pagrec.descricao as 'tipo',  " +
                                    " CASE WHEN fl.parcela_atual IS NULL OR fl.parcela_total IS NULL THEN NULL ELSE CAST(fl.parcela_atual AS VARCHAR) + '/' + CAST(fl.parcela_total AS VARCHAR) END as 'parcela', cliente.nome as 'cliente', " +
                                    " fl.descricao, fl.numero_documento, fl.valor_total, fl.valor_pago, vendedores.nome as 'nome_vendedor' " +
                                    " FROM gc_financeiro_lancamentos fl " +
                                    " left join g_clientes cliente on (cliente.id_cliente = fl.id_cliente) " +
                                    " left join g_pagrec_tipos pagrec on (pagrec.id_pagrec_tipo = fl.id_pag_rec_tipo) " +
                                    " left join gc_financeiro_status status on (status.id_financeiro_status = fl.id_financeiro_status) " +
                                    " left join g_contas_caixas contas on (contas.id_conta_caixa = fl.id_conta_caixa) " +
                                    " left join gc_movimentos movimentos on (fl.id_movimento = movimentos.id_movimento) " +
                                    " left join g_vendedores vendedores on (movimentos.id_vendedor = vendedores.id_vendedor) " +
                                    " where (fl.id_usuario_cadastro > 0) and (fl.ativo = 1) " +
                                    " and (fl.data_pagamento between '" + DataInicialSQL + "' and '" + DataFinalSQL + "') ";

                if (view_cstModalRelatorio.Field_Int_01 > 0) // Tipo Pag/Rec
                {
                    TextSQL += " and fl.tipo_pag_rec = " + view_cstModalRelatorio.Field_Int_01.ToString() + " ";
                    if (view_cstModalRelatorio.Field_Int_01 == 1) { TituloRelatorio += " | Débito"; }
                    else if (view_cstModalRelatorio.Field_Int_01 == 2) { TituloRelatorio += " | Crédito"; }
                }
                else
                {
                    TituloRelatorio += " | Débito & Crédito";
                }

                if (view_cstModalRelatorio.Field_Int_02 > 0) // Financeiro Status
                {
                    TextSQL += " and fl.id_financeiro_status = " + view_cstModalRelatorio.Field_Int_02.ToString() + " ";
                    TituloRelatorio += " | Status - " + db.gc_financeiro_status.Find(view_cstModalRelatorio.Field_Int_02).nome.EmptyIfNull().ToString();
                }

                if (view_cstModalRelatorio.Field_Int_03 > 0) // conta caixa
                {
                    TextSQL += " and fl.id_conta_caixa = " + view_cstModalRelatorio.Field_Int_03.ToString() + " ";
                    g_contas_caixas_acessos record_g_contas_caixas_acessos = db.g_contas_caixas_acessos.Where(a => a.id_usuario == CachePersister.userIdentity.IdUsuario && a.id_conta_caixa == view_cstModalRelatorio.Field_Int_03).FirstOrDefault();
                    if (record_g_contas_caixas_acessos.has_gerencial == false) { TextSQL += " and fl.gerencial = 0 "; }
                    TituloRelatorio += " | Conta Caixa - " + db.g_contas_caixas.Find(view_cstModalRelatorio.Field_Int_03).nome.EmptyIfNull().ToString(); 
                }
                else
                {
                    TituloRelatorio += " | Conta Caixa - TODAS";
                }

                if (view_cstModalRelatorio.Field_Int_04 > 0) // Cliente
                {
                    TextSQL += " and fl.id_cliente = " + view_cstModalRelatorio.Field_Int_04.ToString() + " ";
                }
                else
                {
                    TituloRelatorio += " | Clientes - TODOS";
                }

                TextSQL += " order by fl.id_conta_caixa, fl.data_pagamento, fl.ordem_pagamento ";
                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheet("Lançamentos");

                if (allRecordsNotas.Count > 0)
                {
                    sheetCatalogo.GetCell(1, 1).SetCellValue(TituloRelatorio);
                    sheetCatalogo.GetCell(2, 1).SetCellValue("Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy"));

                    foreach (var RowNota in allRecordsNotas)
                    {
                        IndexLinha += 1;
                        sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(int.Parse(RowNota["id_lancamento"].EmptyIfNull().ToString().Trim()));
                        sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(RowNota["conta_caixa"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(RowNota["status"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue(Convert.ToDateTime(RowNota["data_pagamento"]).ToString("dd/MM/yyyy"));
                        sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(RowNota["pag_rec"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(RowNota["tipo"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(RowNota["parcela"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(RowNota["cliente"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(RowNota["descricao"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 10).SetCellValue(RowNota["numero_documento"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 11).SetCellValue(RowNota["nome_vendedor"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 12).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_total"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        sheetCatalogo.GetCell(IndexLinha, 13).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_pago"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_Lançamento_Financeiros_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xls";
                    DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    FileNameExportacao = Path.Combine(DirTempFiles, FileNameExportacao);
                    FileStream fileStream = new FileStream(FileNameExportacao, FileMode.Create);
                    using (FileStream FileSaida = fileStream)
                    {
                        _workbookCatalogo.Write(FileSaida);
                        FileSaida.Close();
                        FileTemplate.Close();
                    }

                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                    record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                    record_g_processamento.detalhamento = "Relatório Lançamentos Financeiros";
                    record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                    record_g_processamento.datahora_inicio = DataHoraAtual;
                    record_g_processamento.datahora_final = DataHoraAtual;
                    record_g_processamento.qtd_registros = NumeroRegistrosExportados;
                    record_g_processamento.qtd_reg_ok = NumeroRegistrosExportados;
                    record_g_processamento.qtd_reg_erro = 0;
                    record_g_processamento.processando = false;
                    record_g_processamento.concluido = true;
                    record_g_processamento.pathfile = FileNameExportacao;
                    record_g_processamento.id_coligada = 1;
                    record_g_processamento.id_filial = 1;
                    db.g_processamento.Add(record_g_processamento);
                    db.SaveChanges();

                    Sucesso = true;
                    IdProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                    MsgRetorno = "Relatório GERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "[" + NumeroRegistrosExportados.ToString() + " registros]" + "<br/><br/>" + "O Download do relatório será iniciado automaticamente!";
                }
                else
                {
                    Sucesso = false;
                    MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
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
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

    }
}