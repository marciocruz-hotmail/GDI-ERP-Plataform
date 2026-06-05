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
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.gc.Controllers
{
    public class RelatoriosCadastraisController : Controller
    {
        private GdiPlataformEntities db;
        private HSSFWorkbook _workbookCatalogo;

        public RelatoriosCadastraisController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [GdiPageScripts(GdiPageScriptsFlags.LayoutHubReport)]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatórios Cadastrais";
            return View();
        }

        #region ModalRelatorioClientesFornecedores
        public ActionResult ModalRelatorioClientesFornecedores(int? id)
        {
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório - Clientes/Fornecedores";
            ViewBag.comboOpcoes = new List<SelectListItem>();
            ViewBag.comboOpcoes.Add(new SelectListItem { Value = "-1", Text = "[ Todos ]" });
            ViewBag.comboOpcoes.Add(new SelectListItem { Value = "1", Text = "[ SIM ]" });
            ViewBag.comboOpcoes.Add(new SelectListItem { Value = "0", Text = "[ NÃO ]" });
            return View("ModalRelatorioClientesFornecedores", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioClientesFornecedores(CstModalRelatorio view_cstModalRelatorio)
        {

            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            String _IsAtivo = String.Empty;
            String _IsCliente = String.Empty;
            String _IsFornecedor = String.Empty;
            String _IsTransportadora = String.Empty;
            String _IsProdutorRural = String.Empty;
            String _IsConsultorAviacao = String.Empty;
            DateTime DataInicial = new DateTime();
            DateTime DataFinal = new DateTime();
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataInicial);
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataFinal);
            String DataInicialSQL = DataInicial.ToString("yyyy-MM-dd 00:00:00");
            String DataFinalSQL = DataFinal.ToString("yyyy-MM-dd 23:59:59");
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_clientesfornecedores.xls");

            try
            {
                String TextSQL = " select cliente.* from g_clientes cliente where id_cliente > 0 ";

                if (view_cstModalRelatorio.Field_Int_01 == 0) { TextSQL += " and cliente.ativo = 0 "; } else if (view_cstModalRelatorio.Field_Int_01 == 1) { TextSQL += " and cliente.ativo = 1 "; };
                if (view_cstModalRelatorio.Field_Int_02 == 0) { TextSQL += " and cliente.is_cliente = 0 "; } else if (view_cstModalRelatorio.Field_Int_02 == 1) { TextSQL += " and cliente.is_cliente = 1 "; };
                if (view_cstModalRelatorio.Field_Int_03 == 0) { TextSQL += " and cliente.is_fornecedor = 0 "; } else if (view_cstModalRelatorio.Field_Int_03 == 1) { TextSQL += " and cliente.is_fornecedor = 1 "; };
                if (view_cstModalRelatorio.Field_Int_04 == 0) { TextSQL += " and cliente.param_gc_transportadora = 0 "; } else if (view_cstModalRelatorio.Field_Int_04 == 1) { TextSQL += " and cliente.param_gc_transportadora = 1 "; };
                if (view_cstModalRelatorio.Field_Int_05 == 0) { TextSQL += " and cliente.gc_produtor_rural = 0 "; } else if (view_cstModalRelatorio.Field_Int_05 == 1) { TextSQL += " and cliente.gc_produtor_rural = 1 "; };
                if (view_cstModalRelatorio.Field_Int_06 == 0) { TextSQL += " and cliente.gc_consultor_aviacao = 0 "; } else if (view_cstModalRelatorio.Field_Int_06 == 1) { TextSQL += " and cliente.gc_consultor_aviacao = 1 "; };

                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsClientes = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheet("Cadastro");

                if (allRecordsClientes.Count > 0)
                {
                    foreach (var RowCliente in allRecordsClientes)
                    {
                        if (bool.Parse(RowCliente["ativo"].EmptyIfNull().ToString().Trim()) == true) { _IsAtivo = "Sim"; } else { _IsAtivo = "Não"; };
                        if (bool.Parse(RowCliente["is_cliente"].EmptyIfNull().ToString().Trim()) == true) { _IsCliente = "Sim"; } else { _IsCliente = "Não"; };
                        if (bool.Parse(RowCliente["is_fornecedor"].EmptyIfNull().ToString().Trim()) == true) { _IsFornecedor = "Sim"; } else { _IsFornecedor = "Não"; };
                        if (bool.Parse(RowCliente["param_gc_transportadora"].EmptyIfNull().ToString().Trim()) == true) { _IsTransportadora = "Sim"; } else { _IsTransportadora = "Não"; };
                        if (bool.Parse(RowCliente["gc_produtor_rural"].EmptyIfNull().ToString().Trim()) == true) { _IsProdutorRural = "Sim"; } else { _IsProdutorRural = "Não"; };
                        if (bool.Parse(RowCliente["gc_consultor_aviacao"].EmptyIfNull().ToString().Trim()) == true) { _IsConsultorAviacao = "Sim"; } else { _IsConsultorAviacao = "Não"; };
                        IndexLinha += 1;
                            
                        sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(_IsAtivo);
                        sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(RowCliente["nome"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue(RowCliente["razao_social"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(_IsCliente);
                        sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(_IsFornecedor);
                        sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(_IsTransportadora);
                        sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(_IsProdutorRural);
                        sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(_IsConsultorAviacao);
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    FileNameExportacao = Path.Combine(DirTempFiles, "Relatório_ClientesFornecedores_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xls");
                    FileStream fileStream = new FileStream(FileNameExportacao, FileMode.Create);
                    using (FileStream FileSaida = fileStream)
                    {
                        _workbookCatalogo.Write(FileSaida);
                        FileSaida.Close();
                        FileTemplate.Close();
                    }

                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 0; // Exportação Lançamentos Financeiros
                    record_g_processamento.id_processamento_modulo = 0; // Relatório Financeiros/Gerenciais
                    record_g_processamento.detalhamento = "Relatório Clientes/Fornecedores";
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
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
        }

    }
}