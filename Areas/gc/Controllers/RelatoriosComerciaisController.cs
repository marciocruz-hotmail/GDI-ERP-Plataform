using ClosedXML.Excel;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Windows.Forms;

namespace GdiPlataform.Areas.gc.Controllers
{
    public partial class RelatoriosComerciaisController : Controller
    {
        private GdiPlataformEntities db;
        private HSSFWorkbook _workbookCatalogo;
        public RelatoriosComerciaisController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [GdiPageScripts(GdiPageScriptsFlags.LayoutHubReport)]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatórios Comerciais";
            return View();
        }

        #region ModalRelatorioNotasFiscaisEmitidas
        public ActionResult ModalRelatorioNotasFiscaisEmitidas(int? id)
        {
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório - Notas Fiscais Emitidas";
            return View("ModalRelatorioNotasFiscaisEmitidas", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioNotasFiscaisEmitidas(CstModalRelatorio view_cstModalRelatorio)
        {

            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
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
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_notas_emitidas_gdi.xls");

            try
            {
                String TextSQL =
                    " SELECT " +
                    "   notasfiscais.id_movimento, " +
                    "   notasfiscais.nf_data_geracao, " +
                    "   clientes.nome AS cliente_nome, " +
                    "   notasfiscais.id_movimento_nf, " +
                    "   notasfiscais.nf_numero, " +
                    "   cfop.numero AS numero_cfop, " +
                    "   cfop.descricao AS desc_cfop, " +
                    "   nfstatus.descricao_resumida AS status_nf, " +
                    "   notasfiscais.nf_identificador, " +
                    "   notasfiscais.valor_total_liquido, " +
                    "   notasfiscais.frete_valor, " +
                    "   notasfiscais.valor_total_bruto, " +
                    "   vendedor.nome AS vendedor_nome, " +
                    "   notasfiscais.nf_chave_acesso, " +
                    "   notasfiscais.nf_url_pdf, " +
                    "   notasfiscais.nf_url_xml " +
                    " FROM gc_movimentos_nf notasfiscais " +
                    " LEFT JOIN g_nfe_status nfstatus " +
                    "        ON notasfiscais.id_nfe_status = nfstatus.id_nfe_status " +
                    " LEFT JOIN gc_cfop cfop " +
                    "        ON cfop.id_cfop = notasfiscais.id_cfop " +
                    " LEFT JOIN gc_movimentos movimento " +
                    "        ON movimento.id_movimento = notasfiscais.id_movimento " +
                    " LEFT JOIN g_clientes clientes " +
                    "        ON clientes.id_cliente = movimento.id_cliente " +
                    " LEFT JOIN g_vendedores vendedor " +
                    "        ON vendedor.id_vendedor = movimento.id_vendedor " +
                    " WHERE notasfiscais.nf_data_geracao BETWEEN '" + DataInicialSQL + "' AND '" + DataFinalSQL + "' " +
                    " ORDER BY " +
                    "   notasfiscais.nf_numero, " +
                    "   notasfiscais.id_movimento, " +
                    "   notasfiscais.id_movimento_nf ";

                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheet("Notas");

                if (allRecordsNotas.Count > 0)
                {
                    sheetCatalogo.GetCell(2, 1).SetCellValue("Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy"));
                    //sheetCatalogo.GetCell(1, 1).SetCellValue(sheetCatalogo.GetCell(1, 1).StringCellValue.Replace("[Período]", "Período: " + Convert.ToDateTime(view_cstModalRelatorio.Field_Data_01, new CultureInfo("en-US")).ToString("dd/MM/yy") + " à " + Convert.ToDateTime(view_cstModalRelatorio.Field_Data_02, new CultureInfo("en-US")).ToString("dd/MM/yy")));

                    foreach (var RowNota in allRecordsNotas)
                    {
                        String _NumeroNF = RowNota["nf_numero"].EmptyIfNull().ToString().Trim();

                        IndexLinha += 1;
                        sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(LibNumbers.ConvertInt(RowNota["id_movimento"].EmptyIfNull().ToString().Trim()));
                        sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(Convert.ToDateTime(RowNota["nf_data_geracao"]).ToString("dd/MM/yyyy HH:mm"));
                        sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(RowNota["cliente_nome"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue(RowNota["vendedor_nome"].EmptyIfNull().ToString().Trim());
                        if (_NumeroNF.Length > 0) { sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(LibNumbers.ConvertInt(_NumeroNF)); }
                        sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(RowNota["numero_cfop"].EmptyIfNull().ToString().Trim() + " - " + RowNota["desc_cfop"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(RowNota["status_nf"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_total_liquido"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["frete_valor"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        sheetCatalogo.GetCell(IndexLinha, 10).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_total_bruto"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_NotasEmitidas_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xls";
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
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalRelatorioItensComercializados
        public ActionResult ModalRelatorioItensComercializados(int? id)
        {
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório - Itens Comercializados";
            return View("ModalRelatorioItensComercializados", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioItensComercializados(CstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            Double PrecoMinimo = 0;
            Double PrecoMaximo = 0;
            Double DesvioPreco = 0;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            DateTime DataInicial = new DateTime();
            DateTime DataFinal = new DateTime();
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataInicial);
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataFinal);
            String DataInicialSQL = DataInicial.ToString("yyyy-MM-dd 00:00:00");
            String DataFinalSQL = DataFinal.ToString("yyyy-MM-dd 23:59:59");
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_comercial_itens_comercializados.xls");

            try
            {
                String TextSQL =
                    " SELECT " +
                    "   item.id_produto, " +
                    "   prod.nome, " +
                    "   SUM(item.quantidade) AS qtd, " +
                    "   SUM(item.valor_total) AS valor_total, " +
                    "   AVG(item.valor_unit) AS valor_avg, " +
                    "   MIN(item.valor_unit) AS valor_min, " +
                    "   MAX(item.valor_unit) AS valor_max " +
                    " FROM gc_movimentos_itens item " +
                    " JOIN gc_movimentos mov " +
                    "        ON mov.id_movimento = item.id_movimento " +
                    " LEFT JOIN g_produtos prod " +
                    "        ON prod.id_produto = item.id_produto " +
                    " WHERE EXISTS ( " +
                    "   SELECT 1 FROM gc_movimentos_nf nf " +
                    "   INNER JOIN gc_cfop_operacoes operacao " +
                    "           ON operacao.id_cfop_operacao = nf.id_cfop_operacao " +
                    "   WHERE nf.id_movimento = mov.id_movimento " +
                    "     AND nf.id_nfe_status IN (8, 17) " +
                    "     AND nf.nf_data_geracao BETWEEN '" + DataInicialSQL + "' AND '" + DataFinalSQL + "' " +
                    "     AND operacao.is_venda = 1 " +
                    " ) " +
                    " GROUP BY item.id_produto, prod.nome " +
                    " ORDER BY qtd;";

                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 4;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheet("Itens");

                if (allRecordsNotas.Count > 0)
                {
                    sheetCatalogo.GetCell(2, 1).SetCellValue("Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy"));
                    //sheetCatalogo.GetCell(1, 1).SetCellValue(sheetCatalogo.GetCell(1, 1).StringCellValue.Replace("[Período]", "Período: " + Convert.ToDateTime(view_cstModalRelatorio.Field_Data_01, new CultureInfo("en-US")).ToString("dd/MM/yy") + " à " + Convert.ToDateTime(view_cstModalRelatorio.Field_Data_02, new CultureInfo("en-US")).ToString("dd/MM/yy")));

                    foreach (var RowNota in allRecordsNotas)
                    {
                        PrecoMinimo = Double.Parse(RowNota["valor_min"].EmptyIfNull().ToString().Trim());
                        PrecoMaximo = Double.Parse(RowNota["valor_max"].EmptyIfNull().ToString().Trim());
                        DesvioPreco = ((PrecoMaximo * 100) / PrecoMinimo) - 100;

                        IndexLinha += 1;
                        sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(LibNumbers.ConvertInt(RowNota["id_produto"].EmptyIfNull().ToString().Trim()));
                        sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(RowNota["nome"].EmptyIfNull().ToString().Trim());
                        sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(double.Parse(RowNota["qtd"].EmptyIfNull().ToString().Trim()));
                        sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_total"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue("");
                        sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_min"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_max"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(DesvioPreco);
                        sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_avg"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_ItensComercializados_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xls";
                    String DirTempFiles = Server.MapPath("~/_filestemp");
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
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalRelatorioNotasFiscaisContabilidade
        public ActionResult ModalRelatorioNotasFiscaisContabilidade(int? id)
        {
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesPassado();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesPassado();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Arquivo Excel - Relatório Contabilidade";
            return View("ModalRelatorioNotasFiscaisContabilidade", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxRelatorioNotasFiscaisContabilidade(CstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
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
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_nf_contabilidade.xls");

            try
            {
                String TextSQL =
                    " SELECT " +
                    "   nf.id_movimento, " +
                    "   nf.id_movimento_nf, " +
                    "   nf.nf_numero, " +
                    "   cfop.numero, " +
                    "   cfop.descricao AS desc_cfop, " +
                    "   nfstatus.descricao_resumida, " +
                    "   nf.nf_data_geracao, " +
                    "   nf.nf_identificador, " +
                    "   nf.frete_valor, " +
                    "   nf.valor_total_liquido, " +
                    "   nf.valor_total_bruto, " +
                    "   nf.nf_chave_acesso, " +
                    "   nf.nf_url_pdf, " +
                    "   nf.nf_url_xml, " +
                    "   vendedor.nome AS vendedor_nome, " +
                    "   (case when movimento.id_filial = 1 then 'GDI BH' else 'GDI SP' end) as 'emitente' " +
                    " FROM gc_movimentos_nf nf " +
                    " LEFT JOIN g_nfe_status nfstatus " +
                    "        ON nf.id_nfe_status = nfstatus.id_nfe_status " +
                    " LEFT JOIN gc_cfop cfop " +
                    "        ON cfop.id_cfop = nf.id_cfop " +
                    " LEFT JOIN gc_movimentos movimento " +
                    "        ON nf.id_movimento = movimento.id_movimento " +
                    " LEFT JOIN g_vendedores vendedor " +
                    "        ON vendedor.id_vendedor = movimento.id_vendedor " +
                    " WHERE nf.nf_data_geracao BETWEEN '" + DataInicialSQL + "' AND '" + DataFinalSQL + "' " +
                    " ORDER BY nf.id_movimento_nf;";

                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 1;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheetAt(0);


                if (allRecordsNotas.Count > 0)
                {
                    foreach (var RowNota in allRecordsNotas)
                    {

                        IndexLinha += 1;
                        try { sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(LibNumbers.ConvertInt(RowNota["id_movimento"].EmptyIfNull().ToString().Trim())); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(RowNota["emitente"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(LibNumbers.ConvertInt(RowNota["nf_numero"].EmptyIfNull().ToString().Trim())); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue((RowNota["numero"].EmptyIfNull().ToString().Trim() + " - " + RowNota["desc_cfop"].EmptyIfNull().ToString().Trim())); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(RowNota["descricao_resumida"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(""); }
                        ;
                        DateTime DataHora = LibDateTime.GetDateTimeDataRow(RowNota, "nf_data_geracao");
                        try { sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue("'" + DataHora.ToString("dd/MM/yyyy HH:mm:ss")); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["frete_valor"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", ""))); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_total_liquido"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", ""))); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_total_bruto"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", ""))); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 10).SetCellValue("'" + RowNota["nf_chave_acesso"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 10).SetCellValue(""); }
                        ;
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_NotasFiscaisContabilidade_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xls";
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
                    record_g_processamento.detalhamento = "Relatório Notas Fiscais Contabilidade";
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

        #region ModalRelatorioNotasFiscaisMensais
        public ActionResult ModalRelatorioNotasFiscaisMensais(int? id)
        {
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesPassado();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesPassado();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Arquivo Excel - Notas Fiscais Mensais (Todas)";
            return View("ModalRelatorioNotasFiscaisMensais", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioNotasFiscaisMensais(CstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
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
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_nf_mensais.xls");

            try
            {
                String TextSQL = " SELECT nf.id_movimento, nf.id_movimento_nf, " +
                                    " nf.nf_numero, cfop.numero, cfop.descricao as \"desc_cfop\", nfstatus.descricao_resumida, nf.nf_data_geracao,  " +
                                    " nf.nf_identificador, nf.frete_valor, nf.valor_total_liquido, nf.valor_total_bruto, nf.nf_chave_acesso, nf.nf_url_pdf, nf.nf_url_xml, vendedor.nome " +
                                    " FROM gc_movimentos_nf nf " +
                                    " left join g_nfe_status nfstatus on(nf.id_nfe_status = nfstatus.id_nfe_status) " +
                                    " left join gc_cfop cfop on(cfop.id_cfop = nf.id_cfop) " +
                                    " left join gc_movimentos movimento on(nf.id_movimento = movimento.id_movimento) " +
                                    " left join g_vendedores vendedor on(vendedor.id_vendedor = movimento.id_vendedor) " +
                                    " where nf.nf_data_geracao between '" + DataInicialSQL + "' and '" + DataFinalSQL + "' " +
                                    " order by nf.id_movimento_nf ";


                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 1;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheetAt(0);


                if (allRecordsNotas.Count > 0)
                {
                    foreach (var RowNota in allRecordsNotas)
                    {
                        IndexLinha += 1;
                        try { sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(LibNumbers.ConvertInt(RowNota["id_movimento"].EmptyIfNull().ToString().Trim())); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(LibNumbers.ConvertInt(RowNota["id_movimento_nf"].EmptyIfNull().ToString().Trim())); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(LibNumbers.ConvertInt(RowNota["nf_numero"].EmptyIfNull().ToString().Trim())); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue((RowNota["numero"].EmptyIfNull().ToString().Trim() + " - " + RowNota["desc_cfop"].EmptyIfNull().ToString().Trim())); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(RowNota["descricao_resumida"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(""); }
                        ;
                        DateTime DataHora = LibDateTime.GetDateTimeDataRow(RowNota, "nf_data_geracao");
                        try { sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue("'" + DataHora.ToString("dd/MM/yyyy HH:mm:ss")); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(RowNota["nf_identificador"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["frete_valor"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", ""))); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_total_liquido"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", ""))); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 10).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Decimal.Parse(RowNota["valor_total_bruto"].EmptyIfNull().ToString().Trim())).Replace("R$ ", "").Replace("R$", "").Replace("$", ""))); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 10).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 11).SetCellValue("'" + RowNota["nf_chave_acesso"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 11).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 12).SetCellValue(RowNota["nf_url_pdf"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 12).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 13).SetCellValue(RowNota["nf_url_xml"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 13).SetCellValue(""); }
                        ;
                        try { sheetCatalogo.GetCell(IndexLinha, 14).SetCellValue(RowNota["nome"].EmptyIfNull().ToString().Trim()); } catch (Exception) { sheetCatalogo.GetCell(IndexLinha, 14).SetCellValue(""); }
                        ;
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_NotasFiscaisMensais_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xls";
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
                    record_g_processamento.detalhamento = "Relatório Notas Fiscais Mensais";
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

        #region ModalRelatorioVendedoresPedidos
        public ActionResult ModalRelatorioVendedoresPedidos(int? id)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DateTime DataInicio = LibDateTime.getPrimeiroDiaMesAtual();
            DateTime DataFim = LibDateTime.getUltimoDiaMesAtual();
            if (DataHoraAtual.Day <= 10)
            {
                DataInicio = DataInicio.AddMonths(-1);
                DataFim = DataFim.AddMonths(-1);
            }
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = DataInicio;
            view_cstModalRelatorio.Field_Data_02 = DataFim;
            bool gerencialPedidos = CachePersister.userIdentity.Roles.Contains("gc_RelatoriosComerciais_VendedoresComissoes_*")
                || CachePersister.userIdentity.Roles.Contains("gc_RelatoriosComerciais_VendedoresComissoes_Actionmanager");
            PreencherComboVendedoresRelatorio(view_cstModalRelatorio, gerencialPedidos);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório de Vendedores - Pedidos";
            return View("ModalRelatorioVendedoresPedidos", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioVendedoresPedidos(CstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            String MsgRetorno = String.Empty;
            String FileNameExportacao = String.Empty;
            String DirTempFiles = String.Empty;
            String IdProcessamentoGravado = "0";
            int IdVendedor = view_cstModalRelatorio.Field_Int_01;
            String SQLMovimentos = String.Empty;
            String NumerosNotasFiscais = String.Empty;
            String NomeCliente = String.Empty;
            String DescricaoOperacao = String.Empty;
            String PedidoNumero = String.Empty;
            DateTime DataInicial = new DateTime();
            DateTime DataFinal = new DateTime();
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataInicial);
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataFinal);
            String DataInicialSQL = DataInicial.ToString("yyyy-MM-dd 00:00:00");
            String DataFinalSQL = DataFinal.ToString("yyyy-MM-dd 23:59:59");
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_vendedores_pedidos.xlsx");

            List<Db.gc_movimentos> AllMovimentos = new List<Db.gc_movimentos>();
            List<Db.gc_movimentos_nf> AllMovimentosNF = new List<Db.gc_movimentos_nf>();
            List<Db.gc_movimentos_nf> NotasFiscaisMovimento = new List<Db.gc_movimentos_nf>();

            try
            {
                SQLMovimentos = " SELECT m.* ";
                SQLMovimentos += " FROM gc_movimentos m ";
                SQLMovimentos += " JOIN gc_cfop_operacoes cf ON cf.id_cfop_operacao = m.id_cfop_operacao ";
                SQLMovimentos += " JOIN g_vendedores v ON m.id_vendedor = v.id_vendedor ";
                SQLMovimentos += " WHERE m.movimento_aprovado = 1 ";
                SQLMovimentos += " AND (m.datahora_aprovacao BETWEEN '" + DataInicialSQL + "' AND '" + DataFinalSQL + "') ";
                if (IdVendedor > 0) { SQLMovimentos += " AND m.comissao1_vendedor = " + IdVendedor.ToString() + " "; }
                SQLMovimentos += " AND cf.is_venda = 1 ";
                SQLMovimentos += " AND EXISTS ( ";
                SQLMovimentos += "   SELECT 1 FROM gc_movimentos_nf nf ";
                SQLMovimentos += "   INNER JOIN gc_cfop_operacoes operacao ON operacao.id_cfop_operacao = nf.id_cfop_operacao ";
                SQLMovimentos += "   WHERE nf.id_movimento = m.id_movimento ";
                SQLMovimentos += "   AND nf.id_nfe_status IN (8, 17, 22) ";
                SQLMovimentos += "   AND operacao.is_venda = 1 ";
                SQLMovimentos += " ) ";
                SQLMovimentos += " ORDER BY m.datahora_aprovacao ";
                AllMovimentos = db.gc_movimentos.SqlQuery(SQLMovimentos).ToList();

                IndexLinha = 3;
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (AllMovimentos.Count > 0)
                {
                    List<int> ListaIdsMovimentos = AllMovimentos.Select(m => m.id_movimento).ToList();
                    var idsClientes = AllMovimentos.Select(m => m.id_cliente).Distinct().ToList();
                    var idsVendedoresComissao = AllMovimentos.Where(m => m.comissao1_vendedor > 0).Select(m => m.comissao1_vendedor).Distinct().ToList();
                    var AllClientes = db.g_clientes.Where(c => idsClientes.Contains(c.id_cliente)).Select(c => new { c.id_cliente, c.nome }).ToList();
                    var AllVendedores = db.g_vendedores.Where(v => idsVendedoresComissao.Contains(v.id_vendedor)).Select(v => new { v.id_vendedor, v.nome }).ToList();
                    var AllOperacao = db.gc_cfop_operacoes.Select(o => new { o.id_cfop_operacao, o.descricao, o.is_venda }).ToList();
                    var idsOperacaoVenda = AllOperacao.Where(o => o.is_venda).Select(o => o.id_cfop_operacao).ToList();

                    AllMovimentosNF = db.gc_movimentos_nf.Where(n => ListaIdsMovimentos.Contains(n.id_movimento)
                        && (n.id_nfe_status == 8 || n.id_nfe_status == 17 || n.id_nfe_status == 22)
                        && idsOperacaoVenda.Contains(n.id_cfop_operacao)).ToList();

                    var clientesPorId = AllClientes.ToDictionary(c => c.id_cliente, c => c.nome);
                    var vendedoresPorId = AllVendedores.ToDictionary(v => v.id_vendedor, v => v.nome);
                    var operacaoPorId = AllOperacao.ToDictionary(o => o.id_cfop_operacao, o => o.descricao);

                    foreach (gc_movimentos RecordMovimento in AllMovimentos)
                    {
                        NumerosNotasFiscais = String.Empty;
                        NomeCliente = String.Empty;
                        DescricaoOperacao = String.Empty;
                        PedidoNumero = RecordMovimento.id_movimento.EmptyIfNull().ToString();

                        NotasFiscaisMovimento = AllMovimentosNF.Where(n => n.id_movimento == RecordMovimento.id_movimento).ToList();

                        if (NotasFiscaisMovimento.Count > 0)
                        {
                            foreach (gc_movimentos_nf RecordNotasFiscais in NotasFiscaisMovimento)
                            {
                                if (RecordNotasFiscais != null)
                                {
                                    if (NumerosNotasFiscais.Trim().Length > 0) { NumerosNotasFiscais += " | "; };
                                    NumerosNotasFiscais += RecordNotasFiscais.nf_numero.EmptyIfNull().ToString();

                                    String descricaoOperacaoNf;
                                    if (operacaoPorId.TryGetValue(RecordNotasFiscais.id_cfop_operacao, out descricaoOperacaoNf))
                                    {
                                        if (DescricaoOperacao.Trim().Length > 0) { DescricaoOperacao += "\r\n"; };
                                        DescricaoOperacao += descricaoOperacaoNf.EmptyIfNull().ToString();
                                    };

                                    if (RecordMovimento.id_movimento_status == 1) { PedidoNumero += " (Aberto)"; }
                                    else if (RecordMovimento.id_movimento_status == 3) { PedidoNumero += " (Cancelado)"; }
                                    else if (RecordMovimento.id_movimento_status == 4) { PedidoNumero += " (Devolvido)"; };
                                }
                            }
                            String nomeClienteLookup;
                            if (clientesPorId.TryGetValue(RecordMovimento.id_cliente, out nomeClienteLookup)) { NomeCliente = nomeClienteLookup.EmptyIfNull().ToString(); };

                            IndexLinha += 1;
                            WorkSheet.Cell(IndexLinha, 1).Value = PedidoNumero;
                            WorkSheet.Cell(IndexLinha, 2).Value = NumerosNotasFiscais;
                            WorkSheet.Cell(IndexLinha, 3).Value = "'" + RecordMovimento.datahora_aprovacao.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm:ss");
                            WorkSheet.Cell(IndexLinha, 4).Value = "'" + NomeCliente;
                            WorkSheet.Cell(IndexLinha, 5).Value = DescricaoOperacao;
                            WorkSheet.Cell(IndexLinha, 6).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.valor_total_bruto).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                            WorkSheet.Cell(IndexLinha, 7).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.frete_valor).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                            WorkSheet.Cell(IndexLinha, 8).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.frete_gerencial).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                            WorkSheet.Cell(IndexLinha, 9).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (RecordMovimento.valor_total_bruto - RecordMovimento.frete_valor - RecordMovimento.frete_gerencial)).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                            if (RecordMovimento.comissao1_vendedor > 0)
                            {
                                String nomeVendedorComissao;
                                if (vendedoresPorId.TryGetValue(RecordMovimento.comissao1_vendedor, out nomeVendedorComissao))
                                {
                                    WorkSheet.Cell(IndexLinha, 10).Value = nomeVendedorComissao.EmptyIfNull().ToString();
                                }
                            }
                            NumeroRegistrosExportados += 1;
                        }
                    }

                  
                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_Vendedores_Pedidos_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    FileNameExportacao = Path.Combine(DirTempFiles, FileNameExportacao);

                    WorkSheet.Columns().AdjustToContents();
                    WorkBook.SaveAs(FileNameExportacao);
                    WorkBook.Dispose();


                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                    record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                    record_g_processamento.detalhamento = "Relatório Vendedores Pedidos";
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

        #region ModalRelatorioVendedoresComissoes
        public ActionResult ModalRelatorioVendedoresComissoes(int? id)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DateTime DataInicio = LibDateTime.getPrimeiroDiaMesAtual();
            DateTime DataFim = DataInicio.AddMonths(1).AddDays(-1);
            if (DataHoraAtual.Day <= 10)
            {
                DataInicio = DataInicio.AddMonths(-1);
                DataFim = DataInicio.AddMonths(1).AddDays(-1);
            }
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = DataInicio;
            view_cstModalRelatorio.Field_Data_02 = DataFim;
            bool gerencialComissoes = CachePersister.userIdentity.Roles.Contains("gc_RelatoriosComerciais_VendedoresComissoes_*")
                || CachePersister.userIdentity.Roles.Contains("gc_RelatoriosComerciais_VendedoresComissoes_Actionmanager");
            PreencherComboVendedoresRelatorio(view_cstModalRelatorio, gerencialComissoes);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Arquivo Excel - Relatório de Vendedores - Comissões";
            return View("ModalRelatorioVendedoresComissoes", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioVendedoresComissoes(CstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            bool CalculaComissao = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            int IdVendedor = view_cstModalRelatorio.Field_Int_01;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            String SQLFinanceiro = String.Empty;
            String NumerosNotasFiscais = String.Empty;
            String NomeCliente = String.Empty;
            String DescricaoOperacao = String.Empty;
            String PedidoNumero = String.Empty;
            DateTime DataInicial = new DateTime();
            DateTime DataFinal = new DateTime();
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataInicial);
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataFinal);
            DateTime DataInicialSQL = new DateTime(DataInicial.Year, DataInicial.Month, DataInicial.Day, 0, 0, 0);
            DateTime DataFinalSQL = new DateTime(DataFinal.Year, DataFinal.Month, DataFinal.Day, 23, 59, 59);
            DateTime DataInicioPagtoComissao = new DateTime(2025, 1, 1);
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_vendedores_comissoes.xlsx");
            Decimal ValorComissaoPedido = 0;
            Decimal ValorLiquidoPedido = 0;
            List<Db.gc_financeiro_lancamentos> ListaGeralGcFinanceiroLancamentos = null;
            ListaGeralGcFinanceiroLancamentos = new List<Db.gc_financeiro_lancamentos>();
            var AllClientes = db.g_clientes.Select(c => new { c.id_cliente, c.nome }).ToList();
            var AllVendedores = db.g_vendedores.Select(v => new { v.id_vendedor, v.nome }).ToList();
            var AllOperacao = db.gc_cfop_operacoes.Select(o => new { o.id_cfop_operacao, o.descricao }).ToList();
            var AllPagRecTipos = db.g_pagrec_tipos.Select(o => new { o.id_pagrec_tipo, o.descricao }).ToList();
            var AllFinanceiroStatus = db.gc_financeiro_status.Select(o => new { o.id_financeiro_status, o.nome }).ToList();

            try
            {
                ListaGeralGcFinanceiroLancamentos = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.tipo_pag_rec == 2 && l.id_movimento > 0
                                                                                            && l.is_provisao_imposto == false && l.is_difal == false
                                                                                            && ((l.data_vencimento >= DataInicialSQL && l.data_vencimento <= DataFinalSQL) || (l.data_pagamento >= DataInicialSQL && l.data_pagamento <= DataFinalSQL))
                                                                                            ).OrderBy(l => l.data_vencimento).ToList();

                IndexLinha = 6;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (ListaGeralGcFinanceiroLancamentos.Count > 0)
                {
                    List<int> ListaIdsMovimentos = new List<int>();
                    List<Db.gc_movimentos> ListaGeralGcMovimentos = new List<Db.gc_movimentos>();
                    List<Db.gc_movimentos_nf> ListaGeralGcMovimentosNf = new List<Db.gc_movimentos_nf>();
                    foreach (gc_financeiro_lancamentos RecordFinanceiro in ListaGeralGcFinanceiroLancamentos)
                    {
                        ListaIdsMovimentos.Add(RecordFinanceiro.id_movimento);
                    }
                    ListaGeralGcMovimentos = db.gc_movimentos.Where(m => ListaIdsMovimentos.Contains(m.id_movimento)).ToList();
                    ListaGeralGcMovimentosNf = db.gc_movimentos_nf.Where(n => ListaIdsMovimentos.Contains(n.id_movimento)).ToList();

                    foreach (gc_financeiro_lancamentos RecordFinanceiro in ListaGeralGcFinanceiroLancamentos)
                    {
                        List<Db.gc_movimentos_nf> ListaTempNotasFiscaisMovimento = null;
                        gc_movimentos RecordMovimento = ListaGeralGcMovimentos.Where(m => m.id_movimento == RecordFinanceiro.id_movimento).FirstOrDefault();

                        if (((IdVendedor == 0) || (RecordMovimento.comissao1_vendedor == IdVendedor)) && (RecordMovimento.datahora_aprovacao >= DataInicioPagtoComissao))
                        {
                            NumerosNotasFiscais = String.Empty;
                            NomeCliente = String.Empty;
                            CalculaComissao = false;
                            ValorLiquidoPedido = 0;
                            ValorComissaoPedido = 0;

                            if (RecordMovimento != null)
                            {
                                PedidoNumero = RecordMovimento.id_movimento.EmptyIfNull().ToString();
                                ListaTempNotasFiscaisMovimento = ListaGeralGcMovimentosNf.Where(n => n.id_movimento == RecordMovimento.id_movimento && (n.id_nfe_status == 8 || n.id_nfe_status == 17 || n.id_nfe_status == 22)).DefaultIfEmpty().ToList();
                            }
                            else
                            {
                                PedidoNumero = "Sem Pedido";
                                WorkSheet.Cell(IndexLinha, 1).Style.Fill.BackgroundColor = XLColor.CandyAppleRed;
                            }
                            ;

                            if (ListaTempNotasFiscaisMovimento.Count > 0)
                            {
                                foreach (gc_movimentos_nf RecordNotasFiscais in ListaTempNotasFiscaisMovimento)
                                {
                                    if (RecordNotasFiscais != null)
                                    {
                                        if (NumerosNotasFiscais.Trim().Length > 0) { NumerosNotasFiscais += " | "; }
                                        ;
                                        NumerosNotasFiscais += RecordNotasFiscais.nf_numero.EmptyIfNull().ToString();
                                    }
                                }
                            }
                            else
                            {
                                NumerosNotasFiscais += "Sem NF";
                                WorkSheet.Cell(IndexLinha, 2).Style.Fill.BackgroundColor = XLColor.CandyAppleRed;
                            }

                            var RecordCliente = AllClientes.Find(c => c.id_cliente == RecordFinanceiro.id_cliente);
                            if (RecordCliente != null) { NomeCliente = RecordCliente.nome.EmptyIfNull().ToString(); }
                            ;
                            WorkSheet.Cell(IndexLinha, 1).Value = PedidoNumero;
                            WorkSheet.Cell(IndexLinha, 2).Value = NumerosNotasFiscais;
                            WorkSheet.Cell(IndexLinha, 4).Value = NomeCliente;

                            if (RecordMovimento != null)
                            {
                                WorkSheet.Cell(IndexLinha, 3).Value = "'" + RecordMovimento.datahora_aprovacao.GetValueOrDefault().ToString("dd/MM/yyyy");
                                WorkSheet.Cell(IndexLinha, 5).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.valor_total_bruto).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                                WorkSheet.Cell(IndexLinha, 6).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.frete_valor).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                                WorkSheet.Cell(IndexLinha, 7).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.frete_gerencial).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                                WorkSheet.Cell(IndexLinha, 8).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (RecordMovimento.valor_total_bruto - RecordMovimento.frete_valor - RecordMovimento.frete_gerencial)).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                                if (RecordMovimento.comissao1_vendedor > 0)
                                {
                                    WorkSheet.Cell(IndexLinha, 10).Value = AllVendedores.Where(v => v.id_vendedor == RecordMovimento.comissao1_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString();
                                    ValorLiquidoPedido = RecordMovimento.valor_total_bruto - RecordMovimento.frete_valor - RecordMovimento.frete_gerencial;
                                    ValorComissaoPedido = ((ValorLiquidoPedido / 100) * RecordMovimento.comissao1_percentual);
                                    if (RecordMovimento.comissao1_valor != ValorComissaoPedido)
                                    {
                                        gc_movimentos record_gc_movimentos = db.gc_movimentos.Find(RecordMovimento.id_movimento);
                                        record_gc_movimentos.comissao1_valor = ValorComissaoPedido;
                                        RecordMovimento.comissao1_valor = ValorComissaoPedido;
                                    }
                                    CalculaComissao = true;
                                    WorkSheet.Cell(IndexLinha, 11).Value = RecordMovimento.comissao1_percentual;
                                    WorkSheet.Cell(IndexLinha, 12).Value = RecordMovimento.comissao1_valor;
                                }
                            }

                            WorkSheet.Cell(IndexLinha, 14).Value = RecordFinanceiro.id_lancamento.EmptyIfNull().ToString();
                            if ((RecordMovimento != null) && (RecordMovimento.id_pagrec_tipo > 0)) { WorkSheet.Cell(IndexLinha, 15).Value = AllPagRecTipos.Where(t => t.id_pagrec_tipo == RecordMovimento.id_pagrec_tipo).FirstOrDefault().descricao.EmptyIfNull().ToString(); }
                            ;
                            if (RecordFinanceiro.id_financeiro_status > 0) { WorkSheet.Cell(IndexLinha, 16).Value = AllFinanceiroStatus.Where(s => s.id_financeiro_status == RecordFinanceiro.id_financeiro_status).FirstOrDefault().nome.EmptyIfNull().ToString(); };

                            WorkSheet.Cell(IndexLinha, 17).Value = RecordFinanceiro.valor_total;
                            WorkSheet.Cell(IndexLinha, 18).Value = "'" + RecordFinanceiro.data_vencimento.ToString("dd/MM/yyyy");

                            if ((RecordFinanceiro.id_financeiro_status == 1) || (RecordFinanceiro.id_financeiro_status == 4))
                            {
                                Decimal ValorPago = RecordFinanceiro.valor_pago;
                                if ((ValorPago == 0) && (RecordFinanceiro.id_lancamento_adiantamento > 0)) { ValorPago = RecordFinanceiro.valor_total; }
                                ;
                                WorkSheet.Cell(IndexLinha, 19).Value = "'" + RecordFinanceiro.data_pagamento.ToString("dd/MM/yyyy");
                                WorkSheet.Cell(IndexLinha, 20).Value = ValorPago;
                                if ((CalculaComissao == true) && (ValorComissaoPedido > 0))
                                {
                                    Decimal PercentualComissionadoValorBruto = (ValorLiquidoPedido * 100) / RecordMovimento.valor_total_bruto;
                                    Decimal ValorComissionadoLancamento = (ValorPago / 100) * PercentualComissionadoValorBruto;
                                    Decimal ValorComissaoLancamento = (ValorComissionadoLancamento / 100) * RecordMovimento.comissao1_percentual;
                                    WorkSheet.Cell(IndexLinha, 23).Value = ValorComissaoLancamento;
                                }
                                WorkSheet.Cell(IndexLinha, 16).Style.Fill.BackgroundColor = XLColor.LightGreen;
                            }
                            else if (RecordFinanceiro.id_financeiro_status == 3)  // Aberto
                            {
                                Decimal ValortTitulo = RecordFinanceiro.valor_total;
                                if ((CalculaComissao == true) && (ValorComissaoPedido > 0))
                                {
                                    Decimal PercentualComissaoPedido = (ValorLiquidoPedido * 100) / RecordMovimento.valor_total_bruto;
                                    Decimal ValorComissionadoLancamento = (ValortTitulo / 100) * PercentualComissaoPedido;
                                    Decimal ValorComissaoLancamento = (ValorComissionadoLancamento / 100) * RecordMovimento.comissao1_percentual;
                                    if (RecordFinanceiro.data_vencimento < DateTime.Now.AddDays(-3))
                                    {
                                        WorkSheet.Cell(IndexLinha, 16).Value = "Atrasado";
                                        WorkSheet.Cell(IndexLinha, 16).Style.Fill.BackgroundColor = XLColor.CandyAppleRed;
                                        WorkSheet.Cell(IndexLinha, 21).Value = ValorComissaoLancamento; // Atrasado
                                    }
                                    else
                                    {
                                        WorkSheet.Cell(IndexLinha, 16).Value = "Previsto";
                                        WorkSheet.Cell(IndexLinha, 16).Style.Fill.BackgroundColor = XLColor.CanaryYellow;
                                        WorkSheet.Cell(IndexLinha, 22).Value = ValorComissaoLancamento; // Previsto
                                    }
                                }
                            }
                            NumeroRegistrosExportados += 1;
                            IndexLinha += 1;
                        }
                    }


                    if (NumeroRegistrosExportados > 0)
                    {
                        String TituloPlanilha = "Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy");
                        if (IdVendedor >= 0)
                        {
                            if (IdVendedor == 0) { TituloPlanilha += "   |   Vendedor: TODOS"; }
                            else
                            {
                                g_vendedores RecordVendedor = db.g_vendedores.Find(IdVendedor);
                                if (RecordVendedor != null) { TituloPlanilha += "   |   Vendedor: " + RecordVendedor.nome.EmptyIfNull().ToString(); }
                                ;
                            }
                        }
                        ;
                        WorkSheet.Cell(3, 1).Value = TituloPlanilha;

                        // Salvar o arquivo em disco
                        FileNameExportacao = "Relatório_Vendedores_Comissões_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                        String DirTempFiles = Server.MapPath("~/_filestemp");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "reports");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                        FileNameExportacao = Path.Combine(DirTempFiles, FileNameExportacao);

                        WorkSheet.Columns().AdjustToContents();
                        WorkBook.SaveAs(FileNameExportacao);
                        WorkBook.Dispose();


                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                        record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                        record_g_processamento.detalhamento = "Relatório Vendedores - Comissões";
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
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        try { WorkBook.Dispose(); } catch (Exception) { }
                        ;
                        Sucesso = false;
                        MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!" + view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim() + view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim();
                        Thread.Sleep(2000);
                    }
                }
                else
                {
                    Sucesso = false;
                    MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!" + view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim() + view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim();
                    Thread.Sleep(2000);
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

        [HttpPost]
        public ActionResult AjaxModalRelatorioVendedoresComissoesProjetado(CstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            bool CalculaComissao = false;
            int NumeroRegistrosExportados = 0;
            int IdVendedor = view_cstModalRelatorio.Field_Int_01;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            String SQLFinanceiro = String.Empty;

            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DateTime DataInicialPeriodo1 = LibDateTime.getPrimeiroDiaMesAtual();
            DateTime DataFinalPeriodo1 = LibDateTime.getUltimoDiaMesAtual();
            DateTime DataInicialPeriodo2 = DataInicialPeriodo1.AddMonths(1);
            DateTime DataFinalPeriodo2 = LibDateTime.getUltimoDiaMesReferencia(DataInicialPeriodo2);
            DateTime DataInicialPeriodo3 = DataInicialPeriodo2.AddMonths(1);
            DateTime DataFinalPeriodo3 = LibDateTime.getUltimoDiaMesReferencia(DataInicialPeriodo3);
            DateTime DataInicialPeriodo4 = DataInicialPeriodo3.AddMonths(1);
            DateTime DataFinalPeriodo4 = LibDateTime.getUltimoDiaMesReferencia(DataInicialPeriodo4);
            DateTime DataInicialPeriodo5 = DataInicialPeriodo4.AddMonths(1);
            DateTime DataFinalPeriodo5 = LibDateTime.getUltimoDiaMesReferencia(DataInicialPeriodo5);
            DateTime DataInicialPeriodo6 = DataInicialPeriodo5.AddMonths(1);
            DateTime DataFinalPeriodo6 = LibDateTime.getUltimoDiaMesReferencia(DataInicialPeriodo6);

            Decimal ValorComissaoConfirmadaPeriodo1 = 0;
            Decimal ValorComissaoConfirmadaPeriodo2 = 0;
            Decimal ValorComissaoConfirmadaPeriodo3 = 0;
            Decimal ValorComissaoConfirmadaPeriodo4 = 0;
            Decimal ValorComissaoConfirmadaPeriodo5 = 0;
            Decimal ValorComissaoConfirmadaPeriodo6 = 0;
            Decimal ValorComissaoPrevistaPeriodo1 = 0;
            Decimal ValorComissaoPrevistaPeriodo2 = 0;
            Decimal ValorComissaoPrevistaPeriodo3 = 0;
            Decimal ValorComissaoPrevistaPeriodo4 = 0;
            Decimal ValorComissaoPrevistaPeriodo5 = 0;
            Decimal ValorComissaoPrevistaPeriodo6 = 0;
            Decimal ValorComissaoAtrasadaPeriodo1 = 0;
            Decimal ValorComissaoAtrasadaPeriodo2 = 0;
            Decimal ValorComissaoAtrasadaPeriodo3 = 0;
            Decimal ValorComissaoAtrasadaPeriodo4 = 0;
            Decimal ValorComissaoAtrasadaPeriodo5 = 0;
            Decimal ValorComissaoAtrasadaPeriodo6 = 0;
            Decimal ValorComissaoAtrasadaAnterior = 0;

            DateTime DataInicialSQL = new DateTime(DataInicialPeriodo1.Year, DataInicialPeriodo1.Month, DataInicialPeriodo1.Day, 0, 0, 0);
            DateTime DataFinalSQL = new DateTime(DataFinalPeriodo6.Year, DataFinalPeriodo6.Month, DataFinalPeriodo6.Day, 23, 59, 59);
            DateTime DataInicioPagtoComissao = new DateTime(2025, 1, 1);

            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_vendedores_comissoes.xlsx");
            Decimal ValorComissaoPedido = 0;
            Decimal ValorLiquidoPedido = 0;
            List<Db.gc_financeiro_lancamentos> ListaGeralGcFinanceiroLancamentos = null;
            ListaGeralGcFinanceiroLancamentos = new List<Db.gc_financeiro_lancamentos>();
            List<Db.gc_financeiro_lancamentos> ListaGeralGcFinanceiroAtrasados = null;
            ListaGeralGcFinanceiroAtrasados = new List<Db.gc_financeiro_lancamentos>();
            var AllClientes = db.g_clientes.Select(c => new { c.id_cliente, c.nome }).ToList();
            var AllVendedores = db.g_vendedores.Select(v => new { v.id_vendedor, v.nome }).ToList();
            var AllOperacao = db.gc_cfop_operacoes.Select(o => new { o.id_cfop_operacao, o.descricao }).ToList();
            var AllPagRecTipos = db.g_pagrec_tipos.Select(o => new { o.id_pagrec_tipo, o.descricao }).ToList();
            var AllFinanceiroStatus = db.gc_financeiro_status.Select(o => new { o.id_financeiro_status, o.nome }).ToList();

            try
            {
                ListaGeralGcFinanceiroLancamentos = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.tipo_pag_rec == 2 && l.id_movimento > 0
                                                                                            && l.is_provisao_imposto == false && l.is_difal == false
                                                                                            && ((l.data_vencimento >= DataInicialSQL && l.data_vencimento <= DataFinalSQL) || (l.data_pagamento >= DataInicialSQL && l.data_pagamento <= DataFinalSQL))
                                                                                            ).OrderBy(l => l.id_cliente).ThenBy(l => l.id_movimento).ThenBy(l => l.data_vencimento).ToList();

                ListaGeralGcFinanceiroAtrasados = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.tipo_pag_rec == 2 && l.id_movimento > 0
                                                                                            && l.is_provisao_imposto == false && l.is_difal == false && l.id_financeiro_status == 3
                                                                                            && (l.data_vencimento >= DataInicioPagtoComissao && l.data_vencimento <= DataInicialPeriodo1)
                                                                                            ).OrderBy(l => l.id_cliente).ThenBy(l => l.id_movimento).ThenBy(l => l.data_vencimento).ToList();

                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (ListaGeralGcFinanceiroLancamentos.Count > 0)
                {
                    List<int> ListaIdsMovimentos = new List<int>();
                    List<Db.gc_movimentos> ListaGeralGcMovimentos = new List<Db.gc_movimentos>();
                    foreach (gc_financeiro_lancamentos RecordFinanceiro in ListaGeralGcFinanceiroLancamentos)
                    {
                        ListaIdsMovimentos.Add(RecordFinanceiro.id_movimento);
                    }
                    foreach (gc_financeiro_lancamentos RecordFinanceiro in ListaGeralGcFinanceiroAtrasados)
                    {
                        ListaIdsMovimentos.Add(RecordFinanceiro.id_movimento);
                    }
                    ListaGeralGcMovimentos = db.gc_movimentos.Where(m => ListaIdsMovimentos.Contains(m.id_movimento)).ToList();

                    foreach (gc_financeiro_lancamentos RecordFinanceiro in ListaGeralGcFinanceiroLancamentos)
                    {
                        gc_movimentos RecordMovimento = ListaGeralGcMovimentos.Where(m => m.id_movimento == RecordFinanceiro.id_movimento).FirstOrDefault();

                        if (((IdVendedor == 0) || (RecordMovimento.comissao1_vendedor == IdVendedor)) && (RecordMovimento.datahora_aprovacao >= DataInicioPagtoComissao))
                        {
                            CalculaComissao = false;
                            if (RecordMovimento != null)
                            {
                                if (RecordMovimento.comissao1_vendedor > 0)
                                {
                                    ValorLiquidoPedido = RecordMovimento.valor_total_bruto - RecordMovimento.frete_valor - RecordMovimento.frete_gerencial;
                                    ValorComissaoPedido = ((ValorLiquidoPedido / 100) * RecordMovimento.comissao1_percentual);
                                    if (RecordMovimento.comissao1_valor != ValorComissaoPedido)
                                    {
                                        gc_movimentos record_gc_movimentos = db.gc_movimentos.Find(RecordMovimento.id_movimento);
                                        record_gc_movimentos.comissao1_valor = ValorComissaoPedido;
                                        RecordMovimento.comissao1_valor = ValorComissaoPedido;
                                    }
                                    CalculaComissao = true;
                                }
                            }

                            if ((RecordFinanceiro.id_financeiro_status == 1) || (RecordFinanceiro.id_financeiro_status == 4))
                            {
                                Decimal ValorPago = RecordFinanceiro.valor_pago;
                                if ((ValorPago == 0) && (RecordFinanceiro.id_lancamento_adiantamento > 0)) { ValorPago = RecordFinanceiro.valor_total; }
                                ;
                                if ((CalculaComissao == true) && (ValorComissaoPedido > 0))
                                {
                                    Decimal PercentualComissionadoValorBruto = (ValorLiquidoPedido * 100) / RecordMovimento.valor_total_bruto;
                                    Decimal ValorComissionadoLancamento = (ValorPago / 100) * PercentualComissionadoValorBruto;
                                    Decimal ValorComissaoLancamento = (ValorComissionadoLancamento / 100) * RecordMovimento.comissao1_percentual;

                                    if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo1) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo1)) { ValorComissaoConfirmadaPeriodo1 += ValorComissaoLancamento; }
                                    else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo2) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo2)) { ValorComissaoConfirmadaPeriodo2 += ValorComissaoLancamento; }
                                    else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo3) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo3)) { ValorComissaoConfirmadaPeriodo3 += ValorComissaoLancamento; }
                                    else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo4) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo4)) { ValorComissaoConfirmadaPeriodo4 += ValorComissaoLancamento; }
                                    else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo5) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo5)) { ValorComissaoConfirmadaPeriodo5 += ValorComissaoLancamento; }
                                    else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo6) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo6)) { ValorComissaoConfirmadaPeriodo6 += ValorComissaoLancamento; }
                                    ;
                                }
                            }
                            else if (RecordFinanceiro.id_financeiro_status == 3)  // Aberto
                            {
                                Decimal ValortTitulo = RecordFinanceiro.valor_total;
                                if ((CalculaComissao == true) && (ValorComissaoPedido > 0))
                                {
                                    Decimal PercentualComissaoPedido = (ValorLiquidoPedido * 100) / RecordMovimento.valor_total_bruto;
                                    Decimal ValorComissionadoLancamento = (ValortTitulo / 100) * PercentualComissaoPedido;
                                    Decimal ValorComissaoLancamento = (ValorComissionadoLancamento / 100) * RecordMovimento.comissao1_percentual;
                                    if (RecordFinanceiro.data_vencimento < DateTime.Now.AddDays(-3))
                                    {
                                        if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo1) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo1)) { ValorComissaoAtrasadaPeriodo1 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo2) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo2)) { ValorComissaoAtrasadaPeriodo2 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo3) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo3)) { ValorComissaoAtrasadaPeriodo3 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo4) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo4)) { ValorComissaoAtrasadaPeriodo4 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo5) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo5)) { ValorComissaoAtrasadaPeriodo5 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo6) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo6)) { ValorComissaoAtrasadaPeriodo6 += ValorComissaoLancamento; }
                                        ;
                                    }
                                    else
                                    {
                                        if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo1) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo1)) { ValorComissaoPrevistaPeriodo1 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo2) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo2)) { ValorComissaoPrevistaPeriodo2 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo3) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo3)) { ValorComissaoPrevistaPeriodo3 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo4) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo4)) { ValorComissaoPrevistaPeriodo4 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo5) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo5)) { ValorComissaoPrevistaPeriodo5 += ValorComissaoLancamento; }
                                        else if ((RecordFinanceiro.data_vencimento >= DataInicialPeriodo6) && (RecordFinanceiro.data_vencimento <= DataFinalPeriodo6)) { ValorComissaoPrevistaPeriodo6 += ValorComissaoLancamento; }
                                        ;
                                    }
                                }
                            }
                            NumeroRegistrosExportados += 1;
                        }
                    }



                    // Atrasados
                    foreach (gc_financeiro_lancamentos RecordFinanceiro in ListaGeralGcFinanceiroAtrasados)
                    {
                        gc_movimentos RecordMovimento = ListaGeralGcMovimentos.Where(m => m.id_movimento == RecordFinanceiro.id_movimento).FirstOrDefault();

                        if (((IdVendedor == 0) || (RecordMovimento.comissao1_vendedor == IdVendedor)) && (RecordMovimento.datahora_aprovacao >= DataInicioPagtoComissao))
                        {
                            CalculaComissao = false;
                            if (RecordMovimento != null)
                            {
                                if (RecordMovimento.comissao1_vendedor > 0)
                                {
                                    ValorLiquidoPedido = RecordMovimento.valor_total_bruto - RecordMovimento.frete_valor - RecordMovimento.frete_gerencial;
                                    ValorComissaoPedido = ((ValorLiquidoPedido / 100) * RecordMovimento.comissao1_percentual);
                                    if (RecordMovimento.comissao1_valor != ValorComissaoPedido)
                                    {
                                        gc_movimentos record_gc_movimentos = db.gc_movimentos.Find(RecordMovimento.id_movimento);
                                        record_gc_movimentos.comissao1_valor = ValorComissaoPedido;
                                        RecordMovimento.comissao1_valor = ValorComissaoPedido;
                                    }
                                    CalculaComissao = true;
                                }
                            }

                            Decimal ValortTitulo = RecordFinanceiro.valor_total;
                            if ((CalculaComissao == true) && (ValorComissaoPedido > 0))
                            {
                                Decimal PercentualComissaoPedido = (ValorLiquidoPedido * 100) / RecordMovimento.valor_total_bruto;
                                Decimal ValorComissionadoLancamento = (ValortTitulo / 100) * PercentualComissaoPedido;
                                Decimal ValorComissaoLancamento = (ValorComissionadoLancamento / 100) * RecordMovimento.comissao1_percentual;
                                ValorComissaoAtrasadaAnterior += ValorComissaoLancamento;
                            }
                            NumeroRegistrosExportados += 1;
                        }
                    }

                    if (NumeroRegistrosExportados > 0)
                    {
                        // Atrasados
                        WorkSheet.Cell(3, 2).Value = "'Anterior";
                        WorkSheet.Cell(6, 2).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoAtrasadaAnterior).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                        // Período 1
                        WorkSheet.Cell(3, 3).Value = "'" + DataInicialPeriodo1.ToString("MM/yyyy");
                        WorkSheet.Cell(4, 3).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoConfirmadaPeriodo1).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(5, 3).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoPrevistaPeriodo1).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(6, 3).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoAtrasadaPeriodo1).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                        // Período 2
                        WorkSheet.Cell(3, 4).Value = "'" + DataInicialPeriodo2.ToString("MM/yyyy");
                        WorkSheet.Cell(4, 4).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoConfirmadaPeriodo2).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(5, 4).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoPrevistaPeriodo2).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(6, 4).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoAtrasadaPeriodo2).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                        // Período 3
                        WorkSheet.Cell(3, 5).Value = "'" + DataInicialPeriodo3.ToString("MM/yyyy");
                        WorkSheet.Cell(4, 5).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoConfirmadaPeriodo3).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(5, 5).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoPrevistaPeriodo3).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(6, 5).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoAtrasadaPeriodo3).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                        // Período 4
                        WorkSheet.Cell(3, 6).Value = "'" + DataInicialPeriodo4.ToString("MM/yyyy");
                        WorkSheet.Cell(4, 6).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoConfirmadaPeriodo4).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(5, 6).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoPrevistaPeriodo4).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(6, 6).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoAtrasadaPeriodo4).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                        // Período 5
                        WorkSheet.Cell(3, 7).Value = "'" + DataInicialPeriodo5.ToString("MM/yyyy");
                        WorkSheet.Cell(4, 7).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoConfirmadaPeriodo5).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(5, 7).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoPrevistaPeriodo5).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(6, 7).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoAtrasadaPeriodo5).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                        // Período 6
                        WorkSheet.Cell(3, 8).Value = "'" + DataInicialPeriodo6.ToString("MM/yyyy");
                        WorkSheet.Cell(4, 8).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoConfirmadaPeriodo6).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(5, 8).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoPrevistaPeriodo6).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                        WorkSheet.Cell(6, 8).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorComissaoAtrasadaPeriodo6).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                        String TituloPlanilha = "Período: " + Convert.ToDateTime(DataInicialPeriodo1, new CultureInfo("en-US")).ToString("dd/MM/yy") + " à " + Convert.ToDateTime(DataFinalPeriodo6, new CultureInfo("en-US")).ToString("dd/MM/yy");
                        if (IdVendedor >= 0)
                        {
                            if (IdVendedor == 0) { TituloPlanilha += "   |   Vendedor: TODOS"; }
                            else
                            {
                                g_vendedores RecordVendedor = db.g_vendedores.Find(IdVendedor);
                                if (RecordVendedor != null) { TituloPlanilha += "   |   Vendedor: " + RecordVendedor.nome.EmptyIfNull().ToString(); }
                                ;
                            }
                        }
                        ;
                        WorkSheet.Cell(2, 1).Value = TituloPlanilha;

                        // Salvar o arquivo em disco
                        FileNameExportacao = "Relatório_Vendedores_Comissões_Projetadas_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                        String DirTempFiles = Server.MapPath("~/_filestemp");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "reports");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        FileNameExportacao = Path.Combine(DirTempFiles, FileNameExportacao);

                        WorkSheet.Columns().AdjustToContents();
                        WorkBook.SaveAs(FileNameExportacao);
                        WorkBook.Dispose();


                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                        record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                        record_g_processamento.detalhamento = "Relatório Vendedores - Comissões";
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
                        MsgRetorno = "Relatório GERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "[" + NumeroRegistrosExportados.ToString() + " Lançamentos Financeiros]" + "<br/><br/>" + "O Download do relatório será iniciado automaticamente!";
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        try { WorkBook.Dispose(); } catch (Exception) { }
                        ;
                        Sucesso = false;
                        MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
                        Thread.Sleep(2000);
                    }
                }
                else
                {
                    Sucesso = false;
                    MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
                    Thread.Sleep(2000);
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

        #region ModalRelatorioVendedoresCarteira
        public ActionResult ModalRelatorioVendedoresCarteira(int? id)
        {
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            bool gerencialCarteira = CachePersister.userIdentity.Roles.Contains("gc_RelatoriosComerciais_VendedoresCarteira_*")
                || CachePersister.userIdentity.Roles.Contains("gc_RelatoriosComerciais_VendedoresCarteira_Actionmanager");
            PreencherComboVendedoresRelatorio(view_cstModalRelatorio, gerencialCarteira);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Arquivo Excel - Relatório de Vendedores - Carteira de Clientes";
            return View("ModalRelatorioVendedoresCarteira", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioVendedoresCarteira(CstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int QtdClientes = 0;
            int NumeroRegistrosExportados = 0;
            int IdVendedor = view_cstModalRelatorio.Field_Int_01;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            String SQLFinanceiro = String.Empty;
            String NumerosNotasFiscais = String.Empty;
            String NomeCliente = String.Empty;
            String DescricaoOperacao = String.Empty;
            String PedidoNumero = String.Empty;
            String SqlPedidos = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_vendedores_carteira.xlsx");
            List<Db.g_clientes> ListaGeralClientes = new List<Db.g_clientes>();
            List<Db.g_vendedores> ListaGeralVendedores = db.g_vendedores.Where(v => v.id_vendedor > 0).ToList();
            List<Db.gc_movimentos> ListaGeralPedidosRealizados = new List<Db.gc_movimentos>();
            var AllClientes = db.g_clientes.Select(c => new { c.id_cliente, c.nome, c.cpf, c.cnpj }).ToList();
            var AllVendedores = db.g_vendedores.Select(v => new { v.id_vendedor, v.nome }).ToList();

            try
            {
                if (view_cstModalRelatorio.Field_Int_01 == 0)
                {
                    ListaGeralClientes = db.g_clientes.Where(c => c.id_cliente > 0 && c.ativo == true && c.is_cliente == true).ToList();
                    SqlPedidos += " SELECT pedidos.* ";
                    SqlPedidos += " FROM gc_movimentos pedidos ";
                    SqlPedidos += " where pedidos.id_movimento_tipo = 4 ";
                    SqlPedidos += " and pedidos.id_movimento_status = 2 ";
                    SqlPedidos += " and pedidos.movimento_aprovado = 1 ";
                    ListaGeralPedidosRealizados = db.gc_movimentos.SqlQuery(SqlPedidos).ToList();
                }
                else
                {
                    ListaGeralClientes = db.g_clientes.Where(c => c.id_cliente > 0 && c.ativo == true && c.is_cliente == true && c.id_vendedor == view_cstModalRelatorio.Field_Int_01).ToList();
                    SqlPedidos += " SELECT pedidos.* ";
                    SqlPedidos += " FROM gc_movimentos pedidos ";
                    SqlPedidos += " join g_clientes cliente on (pedidos.id_cliente = cliente.id_cliente)";
                    SqlPedidos += " where pedidos.id_movimento_tipo = 4 ";
                    SqlPedidos += " and pedidos.id_movimento_status = 2 ";
                    SqlPedidos += " and pedidos.movimento_aprovado = 1 ";
                    SqlPedidos += " and cliente.id_vendedor =  " + view_cstModalRelatorio.Field_Int_01.EmptyIfNull().ToString();
                    ListaGeralPedidosRealizados = db.gc_movimentos.SqlQuery(SqlPedidos).ToList();
                }


                IndexLinha = 4;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (ListaGeralClientes.Count > 0)
                {
                    foreach (g_clientes RecordCliente in ListaGeralClientes)
                    {
                        int DiasSemComprar = 0;

                        String DocumentoCliente = string.Empty;
                        if (RecordCliente.cpf.EmptyIfNull().ToString().Length > 0) { DocumentoCliente = LibStringFormat.FormatarCPFCNPJ("1", RecordCliente.cpf.EmptyIfNull().ToString()); }
                        else if (RecordCliente.cnpj.EmptyIfNull().ToString().Length > 0) { DocumentoCliente = LibStringFormat.FormatarCPFCNPJ("2", RecordCliente.cnpj.EmptyIfNull().ToString()); }

                        String NomeConsultor = string.Empty;
                        g_vendedores RecordVendedor = ListaGeralVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor).FirstOrDefault();
                        if (RecordVendedor != null) { NomeConsultor = RecordVendedor.nome.EmptyIfNull().ToString(); }
                        ;

                        DateTime DataUltimaCompra;
                        String DataUltimaCompraString = string.Empty;
                        gc_movimentos RecorUltimoPedido = ListaGeralPedidosRealizados.Where(p => p.id_cliente == RecordCliente.id_cliente).OrderByDescending(p => p.datahora_aprovacao).FirstOrDefault();
                        if (RecorUltimoPedido != null)
                        {
                            DataUltimaCompra = RecorUltimoPedido.datahora_aprovacao.GetValueOrDefault();
                            DataUltimaCompraString = DataUltimaCompra.ToString("dd/MM/yyyy");
                            TimeSpan DiferencaDiasCompra = DateTime.Now.Subtract(DataUltimaCompra);
                            DiasSemComprar = DiferencaDiasCompra.Days;
                        }
                        else
                        {
                            DiasSemComprar = 9999;
                        }

                        WorkSheet.Cell(IndexLinha, 1).Value = RecordCliente.id_cliente.EmptyIfNull().ToString();
                        WorkSheet.Cell(IndexLinha, 2).Value = RecordCliente.nome.EmptyIfNull().ToString(); ;
                        WorkSheet.Cell(IndexLinha, 3).Value = DocumentoCliente;
                        WorkSheet.Cell(IndexLinha, 4).Value = NomeConsultor;
                        WorkSheet.Cell(IndexLinha, 5).Value = DataUltimaCompraString;

                        if (DiasSemComprar > 0)
                        {
                            WorkSheet.Cell(IndexLinha, 6).Value = DiasSemComprar;

                            if ((DiasSemComprar > 0) && (DiasSemComprar <= 30)) { WorkSheet.Cell(IndexLinha, 7).Value = "Até 30 Dias"; }
                            ;
                            if ((DiasSemComprar > 30) && (DiasSemComprar <= 60)) { WorkSheet.Cell(IndexLinha, 7).Value = "Entre 30 e 60 Dias"; }
                            ;
                            if ((DiasSemComprar > 60) && (DiasSemComprar <= 90)) { WorkSheet.Cell(IndexLinha, 7).Value = "Entre 60 e 90 Dias"; }
                            ;
                            if ((DiasSemComprar > 90) && (DiasSemComprar <= 180)) { WorkSheet.Cell(IndexLinha, 7).Value = "Entre 90 e 180 dias"; }
                            ;
                            if ((DiasSemComprar > 180) && (DiasSemComprar <= 365)) { WorkSheet.Cell(IndexLinha, 7).Value = "Entre 180 dias e 365 dias"; }
                            ;
                            if ((DiasSemComprar > 365) && (DiasSemComprar < 9999)) { WorkSheet.Cell(IndexLinha, 7).Value = "Maior que 1(um) ano"; }
                            ;
                            if (DiasSemComprar >= 9999) { WorkSheet.Cell(IndexLinha, 7).Value = "Não há registro de compras"; }
                            ;
                        }

                        NumeroRegistrosExportados += 1;
                        IndexLinha += 1;
                        QtdClientes += 1;
                    }


                    if (NumeroRegistrosExportados > 0)
                    {
                        String TituloPlanilha = string.Empty;
                        if (IdVendedor <= 0) { TituloPlanilha = "Vendedor: TODOS | " + QtdClientes.ToString() + " Clientes"; }
                        else
                        {
                            g_vendedores RecordVendedor = db.g_vendedores.Find(IdVendedor);
                            if (RecordVendedor != null) { TituloPlanilha += "Vendedor: " + RecordVendedor.nome.EmptyIfNull().ToString() + " | " + QtdClientes.ToString() + " Clientes"; }
                            ;
                        }
                        WorkSheet.Cell(2, 1).Value = TituloPlanilha;

                        // Salvar o arquivo em disco
                        FileNameExportacao = "Relatório_Vendedores_Carteira_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                        String DirTempFiles = Server.MapPath("~/_filestemp");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "reports");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                        FileNameExportacao = Path.Combine(DirTempFiles, FileNameExportacao);

                        WorkSheet.Columns().AdjustToContents();
                        WorkBook.SaveAs(FileNameExportacao);
                        WorkBook.Dispose();


                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                        record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                        record_g_processamento.detalhamento = "Relatório Vendedores - Carteira";
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
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        try { WorkBook.Dispose(); } catch (Exception) { }
                        ;
                        Sucesso = false;
                        MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
                        Thread.Sleep(2000);
                    }
                }
                else
                {
                    Sucesso = false;
                    MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
                    Thread.Sleep(2000);
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

        #region ModalRelatorioVendedoresAtrasados
        public ActionResult ModalRelatorioVendedoresAtrasados(int? id)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            bool gerencialAtrasados = CachePersister.userIdentity.Roles.Contains("gc_RelatoriosComerciais_VendedoresAtrasados_*")
                || CachePersister.userIdentity.Roles.Contains("gc_RelatoriosComerciais_VendedoresAtrasados_Actionmanager");
            PreencherComboVendedoresRelatorio(view_cstModalRelatorio, gerencialAtrasados);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Arquivo Excel - Relatório de Vendedores - Títulos Atrasados";
            return View("ModalRelatorioVendedoresAtrasados", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioVendedoresAtrasados(CstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            bool CalculaComissao = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            int IdVendedor = view_cstModalRelatorio.Field_Int_01;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            String SQLFinanceiro = String.Empty;
            String NumerosNotasFiscais = String.Empty;
            String NomeCliente = String.Empty;
            String DescricaoOperacao = String.Empty;
            String PedidoNumero = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia().AddDays(-3);
            DateTime DataFinal = Convert.ToDateTime(DataHoraAtual, new CultureInfo("en-US"));
            DateTime DataFinalSQL = new DateTime(DataHoraAtual.Year, DataHoraAtual.Month, DataFinal.Day, 23, 59, 59);
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_vendedores_comissoes.xlsx");
            Decimal ValorComissaoPedido = 0;
            Decimal ValorLiquidoPedido = 0;
            List<Db.gc_financeiro_lancamentos> ListaGeralGcFinanceiroLancamentos = null;
            ListaGeralGcFinanceiroLancamentos = new List<Db.gc_financeiro_lancamentos>();
            var AllClientes = db.g_clientes.Select(c => new { c.id_cliente, c.nome }).ToList();
            var AllVendedores = db.g_vendedores.Select(v => new { v.id_vendedor, v.nome }).ToList();
            var AllOperacao = db.gc_cfop_operacoes.Select(o => new { o.id_cfop_operacao, o.descricao }).ToList();
            var AllPagRecTipos = db.g_pagrec_tipos.Select(o => new { o.id_pagrec_tipo, o.descricao }).ToList();
            var AllFinanceiroStatus = db.gc_financeiro_status.Select(o => new { o.id_financeiro_status, o.nome }).ToList();

            try
            {
                ListaGeralGcFinanceiroLancamentos = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.tipo_pag_rec == 2 && l.id_movimento > 0
                                                                                                            && l.is_provisao_imposto == false && l.is_difal == false && l.id_financeiro_status == 3
                                                                                                            && (l.data_vencimento <= DataFinalSQL)
                                                                                                            ).OrderBy(l => l.data_vencimento).ToList();

                IndexLinha = 6;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (ListaGeralGcFinanceiroLancamentos.Count > 0)
                {
                    List<int> ListaIdsMovimentos = new List<int>();
                    List<Db.gc_movimentos> ListaGeralGcMovimentos = new List<Db.gc_movimentos>();
                    List<Db.gc_movimentos_nf> ListaGeralGcMovimentosNf = new List<Db.gc_movimentos_nf>();
                    foreach (gc_financeiro_lancamentos RecordFinanceiro in ListaGeralGcFinanceiroLancamentos)
                    {
                        ListaIdsMovimentos.Add(RecordFinanceiro.id_movimento);
                    }
                    ListaGeralGcMovimentos = db.gc_movimentos.Where(m => ListaIdsMovimentos.Contains(m.id_movimento)).ToList();
                    ListaGeralGcMovimentosNf = db.gc_movimentos_nf.Where(n => ListaIdsMovimentos.Contains(n.id_movimento)).ToList();

                    foreach (gc_financeiro_lancamentos RecordFinanceiro in ListaGeralGcFinanceiroLancamentos)
                    {
                        List<Db.gc_movimentos_nf> ListaTempNotasFiscaisMovimento = null;
                        gc_movimentos RecordMovimento = ListaGeralGcMovimentos.Where(m => m.id_movimento == RecordFinanceiro.id_movimento).FirstOrDefault();

                        if ((IdVendedor == 0) || (RecordMovimento.comissao1_vendedor == IdVendedor))
                        {
                            NumerosNotasFiscais = String.Empty;
                            NomeCliente = String.Empty;
                            CalculaComissao = false;
                            ValorLiquidoPedido = 0;
                            ValorComissaoPedido = 0;

                            if (RecordMovimento != null)
                            {
                                PedidoNumero = RecordMovimento.id_movimento.EmptyIfNull().ToString();
                                ListaTempNotasFiscaisMovimento = ListaGeralGcMovimentosNf.Where(n => n.id_movimento == RecordMovimento.id_movimento && (n.id_nfe_status == 8 || n.id_nfe_status == 17 || n.id_nfe_status == 22)).DefaultIfEmpty().ToList();
                            }
                            else
                            {
                                PedidoNumero = "Sem Pedido";
                                WorkSheet.Cell(IndexLinha, 1).Style.Fill.BackgroundColor = XLColor.CandyAppleRed;
                            }
                            ;

                            if (ListaTempNotasFiscaisMovimento.Count > 0)
                            {
                                foreach (gc_movimentos_nf RecordNotasFiscais in ListaTempNotasFiscaisMovimento)
                                {
                                    if (RecordNotasFiscais != null)
                                    {
                                        if (NumerosNotasFiscais.Trim().Length > 0) { NumerosNotasFiscais += " | "; }
                                        ;
                                        NumerosNotasFiscais += RecordNotasFiscais.nf_numero.EmptyIfNull().ToString();
                                    }
                                }
                            }
                            else
                            {
                                NumerosNotasFiscais += "Sem NF";
                                WorkSheet.Cell(IndexLinha, 2).Style.Fill.BackgroundColor = XLColor.CandyAppleRed;
                            }

                            var RecordCliente = AllClientes.Find(c => c.id_cliente == RecordFinanceiro.id_cliente);
                            if (RecordCliente != null) { NomeCliente = RecordCliente.nome.EmptyIfNull().ToString(); }
                            ;
                            WorkSheet.Cell(IndexLinha, 1).Value = PedidoNumero;
                            WorkSheet.Cell(IndexLinha, 2).Value = NumerosNotasFiscais;
                            WorkSheet.Cell(IndexLinha, 4).Value = NomeCliente;

                            if (RecordMovimento != null)
                            {
                                WorkSheet.Cell(IndexLinha, 3).Value = "'" + RecordMovimento.datahora_aprovacao.GetValueOrDefault().ToString("dd/MM/yyyy");
                                WorkSheet.Cell(IndexLinha, 5).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.valor_total_bruto).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                                WorkSheet.Cell(IndexLinha, 6).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.frete_valor).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                                WorkSheet.Cell(IndexLinha, 7).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.frete_gerencial).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));
                                WorkSheet.Cell(IndexLinha, 8).Value = Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (RecordMovimento.valor_total_bruto - RecordMovimento.frete_valor - RecordMovimento.frete_gerencial)).Replace("R$ ", "").Replace("R$", "").Replace("$", ""));

                                if (RecordMovimento.comissao1_vendedor > 0)
                                {
                                    WorkSheet.Cell(IndexLinha, 10).Value = AllVendedores.Where(v => v.id_vendedor == RecordMovimento.comissao1_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString();
                                    ValorLiquidoPedido = RecordMovimento.valor_total_bruto - RecordMovimento.frete_valor - RecordMovimento.frete_gerencial;
                                    ValorComissaoPedido = ((ValorLiquidoPedido / 100) * RecordMovimento.comissao1_percentual);
                                    if (RecordMovimento.comissao1_valor != ValorComissaoPedido)
                                    {
                                        gc_movimentos record_gc_movimentos = db.gc_movimentos.Find(RecordMovimento.id_movimento);
                                        record_gc_movimentos.comissao1_valor = ValorComissaoPedido;
                                        RecordMovimento.comissao1_valor = ValorComissaoPedido;
                                    }
                                    CalculaComissao = true;
                                    WorkSheet.Cell(IndexLinha, 11).Value = RecordMovimento.comissao1_percentual;
                                    WorkSheet.Cell(IndexLinha, 12).Value = RecordMovimento.comissao1_valor;
                                }
                            }

                            WorkSheet.Cell(IndexLinha, 14).Value = RecordFinanceiro.id_lancamento.EmptyIfNull().ToString();
                            if ((RecordMovimento != null) && (RecordMovimento.id_pagrec_tipo > 0)) { WorkSheet.Cell(IndexLinha, 15).Value = AllPagRecTipos.Where(t => t.id_pagrec_tipo == RecordMovimento.id_pagrec_tipo).FirstOrDefault().descricao.EmptyIfNull().ToString(); }
                            ;
                            if (RecordFinanceiro.id_financeiro_status > 0) { WorkSheet.Cell(IndexLinha, 16).Value = AllFinanceiroStatus.Where(s => s.id_financeiro_status == RecordFinanceiro.id_financeiro_status).FirstOrDefault().nome.EmptyIfNull().ToString(); }
                            ;
                            WorkSheet.Cell(IndexLinha, 17).Value = "'" + RecordFinanceiro.data_vencimento.ToString("dd/MM/yyyy");

                            Decimal ValortTitulo = RecordFinanceiro.valor_total;
                            if ((CalculaComissao == true) && (ValorComissaoPedido > 0))
                            {
                                Decimal PercentualComissaoPedido = (ValorLiquidoPedido * 100) / RecordMovimento.valor_total_bruto;
                                Decimal ValorComissionadoLancamento = (ValortTitulo / 100) * PercentualComissaoPedido;
                                Decimal ValorComissaoLancamento = (ValorComissionadoLancamento / 100) * RecordMovimento.comissao1_percentual;
                                WorkSheet.Cell(IndexLinha, 16).Value = "Atrasado";
                                WorkSheet.Cell(IndexLinha, 16).Style.Fill.BackgroundColor = XLColor.CandyAppleRed;
                                WorkSheet.Cell(IndexLinha, 20).Value = ValorComissaoLancamento; // Atrasado
                            }

                            NumeroRegistrosExportados += 1;
                            IndexLinha += 1;
                        }
                    }


                    if (NumeroRegistrosExportados > 0)
                    {
                        String TituloPlanilha = "Período: Títulos Vencidos até " + Convert.ToDateTime(DataFinalSQL, new CultureInfo("en-US")).ToString("dd/MM/yy");
                        if (IdVendedor >= 0)
                        {
                            if (IdVendedor == 0) { TituloPlanilha += "   |   Vendedor: TODOS"; }
                            else
                            {
                                g_vendedores RecordVendedor = db.g_vendedores.Find(IdVendedor);
                                if (RecordVendedor != null) { TituloPlanilha += "   |   Vendedor: " + RecordVendedor.nome.EmptyIfNull().ToString(); }
                                ;
                            }
                        }
                        ;
                        WorkSheet.Cell(3, 1).Value = TituloPlanilha;

                        // Salvar o arquivo em disco
                        FileNameExportacao = "Relatório_Vendedores_Títulos_Atrasados_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                        String DirTempFiles = Server.MapPath("~/_filestemp");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "reports");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        FileNameExportacao = Path.Combine(DirTempFiles, FileNameExportacao);

                        WorkSheet.Columns().AdjustToContents();
                        WorkBook.SaveAs(FileNameExportacao);
                        WorkBook.Dispose();


                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 40; // Exportação Lançamentos Financeiros
                        record_g_processamento.id_processamento_modulo = 2; // Relatório Financeiros/Gerenciais
                        record_g_processamento.detalhamento = "Relatório Vendedores - Comissões";
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
                        Thread.Sleep(2000);
                    }
                    else
                    {
                        try { WorkBook.Dispose(); } catch (Exception) { }
                        ;
                        Sucesso = false;
                        MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
                        Thread.Sleep(2000);
                    }
                }
                else
                {
                    Sucesso = false;
                    MsgRetorno = "Não há lançamentos que atendam à pesquisa realizada!";
                    Thread.Sleep(2000);
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

        #region ModalRelatorioTransportadorasFretes
        public ActionResult ModalRelatorioTransportadorasFretes(int? id)
        {
            CstModalRelatorio view_cstModalRelatorio = new CstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório - Transportadoras/Fretes";
            return View("ModalRelatorioTransportadorasFretes", view_cstModalRelatorio);
        }


        [HttpPost]
        public ActionResult AjaxModalRelatorioTransportadorasFretes(CstModalRelatorio view_cstModalRelatorio)
        {

            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            DateTime DataInicial = new DateTime();
            DateTime DataFinal = new DateTime();
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataInicial);
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataFinal);
            String DataInicialSQL = DataInicial.ToString("yyyy-MM-dd 00:00:00");
            String DataFinalSQL = DataFinal.ToString("yyyy-MM-dd 23:59:59");
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_transportadoras_fretes.xls");

            try
            {
                String TextSQL = " SELECT FORMAT(nf.nf_data_geracao, 'dd/MM/yyyy', 'pt-BR') as 'data_venda',   " +
                                " nf.nf_numero, nf.frete_valor as 'valor_frete',   " +
                                " movimento.frete_gerencial as 'frete_gerencial',   " +
                                " transportadora1.nome as 'transportadora1',  " +
                                " movimento.frete1_custo as 'frete1_custo',   " +
                                " movimento.frete1_transportadora as 'frete1_transportadora',   " +
                                " transportadora2.nome as 'transportadora2',  " +
                                " movimento.frete2_custo as 'frete2_custo',   " +
                                " movimento.frete2_transportadora as 'frete2_transportadora',   " +
                                " cliente.razao_social as 'cliente', vendedor.nome as 'vendedor'  " +
                                " FROM gc_movimentos_nf nf  " +
                                " left join g_nfe_status nfstatus on(nf.id_nfe_status = nfstatus.id_nfe_status)  " +
                                " left join gc_cfop cfop on (cfop.id_cfop = nf.id_cfop)  " +
                                " left join gc_movimentos movimento on (nf.id_movimento = movimento.id_movimento)  " +
                                " left join gc_cfop_operacoes operacao on (operacao.id_cfop_operacao = movimento.id_cfop_operacao) " +
                                " left join g_vendedores vendedor on (vendedor.id_vendedor = movimento.id_vendedor)  " +
                                " left join g_clientes cliente on (cliente.id_cliente = movimento.id_cliente)  " +
                                " left join g_clientes transportadora1 on (transportadora1.id_cliente = movimento.frete1_transportadora)  " +
                                " left join g_clientes transportadora2 on (transportadora2.id_cliente = movimento.frete2_transportadora)  " +
                                " where (nf.id_nfe_status = 8) and (operacao.is_venda = 1)  " +
                                " and (nf.nf_data_geracao between '" + DataInicialSQL + "' and '" + DataFinalSQL + "' ) " +
                                " order by nf.datahora_cadastro  ";


                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheet("Transportadoras");

                if (allRecordsNotas.Count > 0)
                {
                    sheetCatalogo.GetCell(2, 1).SetCellValue("Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy"));

                    foreach (var RowNota in allRecordsNotas)
                    {
                        Decimal ValorFreteCliente = 0;
                        Decimal ValorFreteGerencial = 0;
                        Decimal ValorFrete1Custo = 0;
                        Decimal ValorFrete2Custo = 0;


                        if (LibNumbers.ConvertInt(RowNota["frete1_transportadora"].EmptyIfNull().ToString().Trim()) > 0)
                        {
                            IndexLinha += 1;
                            Decimal.TryParse(RowNota["valor_frete"].EmptyIfNull().ToString().Trim(), out ValorFreteCliente);
                            Decimal.TryParse(RowNota["frete_gerencial"].EmptyIfNull().ToString().Trim(), out ValorFreteGerencial);
                            Decimal.TryParse(RowNota["frete1_custo"].EmptyIfNull().ToString().Trim(), out ValorFrete1Custo);
                            Decimal.TryParse(RowNota["frete2_custo"].EmptyIfNull().ToString().Trim(), out ValorFrete2Custo);
                            sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(LibNumbers.ConvertInt(RowNota["nf_numero"].EmptyIfNull().ToString().Trim()));
                            sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(Convert.ToDateTime(RowNota["data_venda"]).ToString("dd/MM/yyyy"));
                            sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(RowNota["cliente"].EmptyIfNull().ToString().Trim());
                            sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue("Principal");
                            sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(RowNota["transportadora1"].EmptyIfNull().ToString().Trim());
                            sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorFreteCliente + ValorFreteGerencial)).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                            sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorFrete1Custo)).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                            sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorFreteCliente + ValorFreteGerencial - ValorFrete1Custo - ValorFrete2Custo)).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                            sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(RowNota["vendedor"].EmptyIfNull().ToString().Trim());
                            NumeroRegistrosExportados += 1;
                        }

                        if (LibNumbers.ConvertInt(RowNota["frete2_transportadora"].EmptyIfNull().ToString().Trim()) > 0)
                        {
                            IndexLinha += 1;
                            sheetCatalogo.GetCell(IndexLinha, 1).SetCellValue(LibNumbers.ConvertInt(RowNota["nf_numero"].EmptyIfNull().ToString().Trim()));
                            sheetCatalogo.GetCell(IndexLinha, 2).SetCellValue(Convert.ToDateTime(RowNota["data_venda"]).ToString("dd/MM/yyyy"));
                            sheetCatalogo.GetCell(IndexLinha, 3).SetCellValue(RowNota["cliente"].EmptyIfNull().ToString().Trim());
                            sheetCatalogo.GetCell(IndexLinha, 4).SetCellValue("Adicional");
                            sheetCatalogo.GetCell(IndexLinha, 5).SetCellValue(RowNota["transportadora2"].EmptyIfNull().ToString().Trim());
                            sheetCatalogo.GetCell(IndexLinha, 6).SetCellValue(Double.Parse("0"));
                            sheetCatalogo.GetCell(IndexLinha, 7).SetCellValue(Double.Parse(string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorFrete2Custo)).Replace("R$ ", "").Replace("R$", "").Replace("$", "")));
                            sheetCatalogo.GetCell(IndexLinha, 8).SetCellValue(Double.Parse("0"));
                            sheetCatalogo.GetCell(IndexLinha, 9).SetCellValue(RowNota["vendedor"].EmptyIfNull().ToString().Trim());
                            NumeroRegistrosExportados += 1;
                        }


                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_Transportadoras_Fretes_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xls";
                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "reports");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyy"));
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("MM"));
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
                    record_g_processamento.detalhamento = "Relatório Transportadoras Fretes";
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