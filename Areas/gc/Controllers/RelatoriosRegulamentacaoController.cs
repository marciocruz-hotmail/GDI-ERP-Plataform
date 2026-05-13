using ClosedXML.Excel;
using ICSharpCode.SharpZipLib.Zip;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.gc.Controllers
{
    public class RelatoriosRegulamentacaoController : Controller
    {
        private GdiPlataformEntities db;
        public RelatoriosRegulamentacaoController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatórios Regulamentação";
            return View();
        }

        #region ModalRelatorioANP
        public ActionResult ModalRelatorioANP(int? id)
        {
            cstModalRelatorio view_cstModalRelatorio = new cstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório - ANP";
            return View("ModalRelatorioANP", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioANP(cstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameRelatorio = String.Empty;
            String IdProcessamentoGravado = "0";
            DateTime DataInicial = new DateTime();
            DateTime DataFinal = new DateTime();
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataInicial);
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataFinal);
            String DataInicialSQL = DataInicial.ToString("yyyy-MM-dd 00:00:00");
            String DataFinalSQL = DataFinal.ToString("yyyy-MM-dd 23:59:59");
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_regulamentacao.xlsx");
            List<String> ListaArquivosZip = new List<String>();

            try
            {
               if (view_cstModalRelatorio.Field_Bool_01 == false) // Relatório Excel
               {
                    String TextSQL = " SELECT nf.nf_numero, nf.nf_data_geracao,   " +
                                    " replace(replace(clientes.razao_social, ';', ''), ',', '') as 'razao_social',   " +
                                    " clientes.cnpj, clientes.cpf, itens.quantidade,   " +
                                    " replace(replace(produtos.descricao, ';', ''), ',', '') as 'descricao',   " +
                                    " cfop.numero as 'cfop_numero', cfop.descricao as 'cfop_descricao', nfstatus.descricao_resumida as 'status',  " +
                                    " tiposmov.is_entrada, tiposmov.is_saida  " +
                                    " FROM gc_movimentos_itens itens  " +
                                    " left join gc_movimentos movimentos on (movimentos.id_movimento = itens.id_movimento)  " +
                                    " left join gc_movimentos_tipos tiposmov on (tiposmov.id_movimento_tipo = movimentos.id_movimento_tipo)  " +
                                    " left join gc_movimentos_nf nf on (nf.id_movimento = itens.id_movimento)  " +
                                    " left join g_nfe_status nfstatus on (nf.id_nfe_status = nfstatus.id_nfe_status)  " +
                                    " left join gc_cfop cfop on (cfop.id_cfop = nf.id_cfop)  " +
                                    " left join g_produtos produtos on (produtos.id_produto = itens.id_produto)  " +
                                    " left join g_clientes clientes on (clientes.id_cliente = movimentos.id_cliente)  " +
                                    " where (nf.nf_data_geracao between '" + DataInicialSQL + "' and '" + DataFinalSQL + "')  " +
                                    " and ((nf.id_nfe_status = 8) or(nf.id_nfe_status = 17))  " +
                                    " and (itens.id_produto in (select distinct(id_produto) from g_produtos where item_regulado_anp = 1)) " +
                                    " and itens.quantidade > 0 order by id_movimento_nf;  ";

                    DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                    List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                    IndexLinha = 3;
                    FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                    XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                    IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                    if (allRecordsNotas.Count > 0)
                    {


                        WorkSheet.Cell(1, 1).Value = "Relatório Regulamentação da ANP (Agência Nacional do Petróleo)";
                        WorkSheet.Cell(2, 1).Value = "Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy");

                        foreach (var RowNota in allRecordsNotas)
                        {
                            IndexLinha += 1;

                            bool IsEntrada = Convert.ToBoolean(RowNota["is_entrada"].EmptyIfNull().ToString().Trim());
                            bool IsSaida = Convert.ToBoolean(RowNota["is_saida"].EmptyIfNull().ToString().Trim());
                            WorkSheet.Cell(IndexLinha, 1).Value = int.Parse(RowNota["nf_numero"].EmptyIfNull().ToString().Trim());
                            WorkSheet.Cell(IndexLinha, 2).Value = Convert.ToDateTime(RowNota["nf_data_geracao"]).ToString("dd/MM/yyyy HH:mm");
                            WorkSheet.Cell(IndexLinha, 3).Value = RowNota["razao_social"].EmptyIfNull().ToString().Trim();
                            if (RowNota["cnpj"].EmptyIfNull().ToString().Trim().Length > 0) { WorkSheet.Cell(IndexLinha, 4).Value = LibStringFormat.FormatarCPFCNPJ("J", RowNota["cnpj"].EmptyIfNull().ToString().Trim()); }
                            else if (RowNota["cpf"].EmptyIfNull().ToString().Trim().Length > 0) { WorkSheet.Cell(IndexLinha, 4).Value = LibStringFormat.FormatarCPFCNPJ("F", RowNota["cpf"].EmptyIfNull().ToString().Trim()); };
                            if (IsEntrada == true)
                            {
                                WorkSheet.Cell(IndexLinha, 5).Value = double.Parse(RowNota["quantidade"].EmptyIfNull().ToString().Trim());
                                WorkSheet.Cell(IndexLinha, 5).Style.Fill.BackgroundColor = XLColor.LightBlue;
                            }
                            else if (IsSaida == true)
                            {
                                WorkSheet.Cell(IndexLinha, 6).Value = double.Parse(RowNota["quantidade"].EmptyIfNull().ToString().Trim());
                                WorkSheet.Cell(IndexLinha, 6).Style.Fill.BackgroundColor = XLColor.Yellow;
                            }

                            WorkSheet.Cell(IndexLinha, 7).Value = RowNota["descricao"].EmptyIfNull().ToString().Trim();
                            WorkSheet.Cell(IndexLinha, 8).Value = RowNota["cfop_numero"].EmptyIfNull().ToString().Trim() + " - " + RowNota["cfop_descricao"].EmptyIfNull().ToString().Trim();
                            WorkSheet.Cell(IndexLinha, 9).Value = RowNota["status"].EmptyIfNull().ToString().Trim();
                            NumeroRegistrosExportados += 1;
                        }

                        // Salvar o arquivo em disco
                        FileNameRelatorio = "Relatório_ANP_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                        String DirTempFiles = Server.MapPath("~/_filestemp");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "reports");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                        FileNameRelatorio = Path.Combine(DirTempFiles, FileNameRelatorio);

                        WorkSheet.Columns().AdjustToContents();
                        WorkBook.SaveAs(FileNameRelatorio);
                        WorkBook.Dispose();

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 46; // Relatório Regulamentação ANP
                        record_g_processamento.id_processamento_modulo = 3; // Relatórios Regulamentação
                        record_g_processamento.detalhamento = "Relatório Regulamentação ANP";
                        record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                        record_g_processamento.datahora_inicio = DataHoraAtual;
                        record_g_processamento.datahora_final = DataHoraAtual;
                        record_g_processamento.qtd_registros = NumeroRegistrosExportados;
                        record_g_processamento.qtd_reg_ok = NumeroRegistrosExportados;
                        record_g_processamento.qtd_reg_erro = 0;
                        record_g_processamento.processando = false;
                        record_g_processamento.concluido = true;
                        record_g_processamento.pathfile = FileNameRelatorio;
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
                else
                {
                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "downloads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    String DirNotasFiscais = DirTempFiles;
                    String FileNameZIP = "NFe_ANP_" + DataHoraAtual.ToString("yyyyMMddHHmmss") + ".zip";
                    String FullFileNameZIP = Path.Combine(DirNotasFiscais, FileNameZIP);

                    int QtdDownloadPDF = 0;
                    int QtdDownloadXML = 0;
                    int QtdDownloadCancelamento = 0;
                    String TextBoxEmpresaID = "3A50E055-29EC-4574-83F5-DFF375550400";
                    String TextBoxApiKey = "3A50E055-29EC-4574-83F5-DFF375550400";

                    String SentencaSQLNotasFiscais = string.Empty;
                    SentencaSQLNotasFiscais += " SELECT nf.id_movimento, nf.id_movimento_nf,  ";
                    SentencaSQLNotasFiscais += " nf.nf_numero, cfop.numero, cfop.descricao as \"desc_cfop\", nfstatus.descricao_resumida, nf.nf_data_geracao,  ";
                    SentencaSQLNotasFiscais += " nf.nf_identificador, nf.frete_valor, nf.valor_total_liquido, nf.valor_total_bruto, nf.nf_chave_acesso, nf.nf_url_pdf, nf.nf_url_xml, vendedor.nome ";
                    SentencaSQLNotasFiscais += " FROM gc_movimentos_nf nf ";
                    SentencaSQLNotasFiscais += " left join g_nfe_status nfstatus on(nf.id_nfe_status = nfstatus.id_nfe_status) ";
                    SentencaSQLNotasFiscais += " left join gc_cfop cfop on(cfop.id_cfop = nf.id_cfop) ";
                    SentencaSQLNotasFiscais += " left join gc_movimentos movimento on(nf.id_movimento = movimento.id_movimento) ";
                    SentencaSQLNotasFiscais += " left join gc_movimentos_itens itens on(itens.id_movimento = movimento.id_movimento) ";
                    SentencaSQLNotasFiscais += " left join g_vendedores vendedor on(vendedor.id_vendedor = movimento.id_vendedor) ";
                    SentencaSQLNotasFiscais += " left join gc_movimentos_tipos tiposmov on (tiposmov.id_movimento_tipo = movimento.id_movimento_tipo) ";
                    SentencaSQLNotasFiscais += " left join g_produtos produtos on (produtos.id_produto = itens.id_produto) ";
                    SentencaSQLNotasFiscais += " where nf.nf_data_geracao between '" + DataInicialSQL + "' and '" + DataFinalSQL + "' ";
                    SentencaSQLNotasFiscais += " and ((nf.id_nfe_status = 8) or(nf.id_nfe_status = 17)) ";
                    SentencaSQLNotasFiscais += " and (itens.id_produto in (select distinct(id_produto) from g_produtos where item_regulado_anp = 1)) ";
                    SentencaSQLNotasFiscais += " and itens.quantidade > 0 ";
                    SentencaSQLNotasFiscais += " order by nf.id_movimento_nf ";
                    DataTable TableNotasFiscais = LibDB.GetDataTable(SentencaSQLNotasFiscais, db);
                    List<DataRow> AllNotasFiscais = TableNotasFiscais.AsEnumerable().ToList();
                    foreach (var dsRowNotaFiscal in AllNotasFiscais)
                    {
                        NumeroRegistrosExportados += 1;
                        String IdMovimentoNF = dsRowNotaFiscal["id_movimento_nf"].EmptyIfNull().ToString();
                        String NotaFiscalNumero = dsRowNotaFiscal["nf_numero"].EmptyIfNull().ToString();
                        String NotaFiscalStatus = dsRowNotaFiscal["descricao_resumida"].EmptyIfNull().ToString().ToUpperInvariant();
                        String NotaFiscalPDF = dsRowNotaFiscal["nf_url_pdf"].EmptyIfNull().ToString();
                        String NotaFiscalXML = dsRowNotaFiscal["nf_url_xml"].EmptyIfNull().ToString();
                        String NotaFiscalDescCFOP = dsRowNotaFiscal["desc_cfop"].EmptyIfNull().ToString().ToUpperInvariant();
                        String NotaFiscalIdentificador = dsRowNotaFiscal["nf_identificador"].EmptyIfNull().ToString();

                        // PDF
                        String FileNamePDFRemoto = NotaFiscalPDF;
                        String FileNamePDFLocal = "NF " + NotaFiscalNumero + " - " + NotaFiscalDescCFOP.Trim().Replace(".", "") + ".pdf";
                        if (NotaFiscalDescCFOP.IndexOf("COMPRA") >= 0)
                        { FileNamePDFLocal = Path.Combine(DirNotasFiscais, FileNamePDFLocal); }
                        else { FileNamePDFLocal = Path.Combine(DirNotasFiscais, FileNamePDFLocal); }
                        if (FileNamePDFRemoto.Length > 10)
                        {
                            if (!System.IO.File.Exists(FileNamePDFLocal))
                            {
                                try
                                {
                                    using (var client = new WebClient())
                                    {
                                        client.DownloadFile(FileNamePDFRemoto, FileNamePDFLocal);
                                        ListaArquivosZip.Add(FileNamePDFLocal);
                                    }
                                    QtdDownloadPDF += 1;
                                }
                                catch (Exception)
                                {
                                    /*GerarLogDownload("Erro ao efetuar Download PDF [ " + ex.Message.ToString() + " ]");
                                    GerarLogDownload("Arquivo Remoto [ " + FileNamePDFRemoto + " ]");
                                    GerarLogDownload("Arquivo Local[ " + FileNamePDFLocal + " ]");*/
                                }
                            }
                            else
                            {
                                //GerarLogDownload("Arquivo já existe [ " + FileNamePDFLocal.ToString() + " ]");
                            }
                        }

                        // XML
                        String FileNameXMLRemoto = NotaFiscalXML;
                        String FileNameXMLLocal = "NF " + NotaFiscalNumero + " - " + NotaFiscalDescCFOP.Trim().Replace(".", "") + ".xml";
                        if (NotaFiscalDescCFOP.IndexOf("COMPRA") >= 0)
                        { FileNameXMLLocal = Path.Combine(DirNotasFiscais, FileNameXMLLocal); }
                        else { FileNameXMLLocal = Path.Combine(DirNotasFiscais, FileNameXMLLocal); }
                        if (FileNameXMLRemoto.Length > 10)
                        {
                            if (!System.IO.File.Exists(FileNameXMLLocal))
                            {
                                try
                                {
                                    using (var client = new WebClient())
                                    {
                                        client.DownloadFile(FileNameXMLRemoto, FileNameXMLLocal);
                                        ListaArquivosZip.Add(FileNameXMLLocal);
                                    }
                                    QtdDownloadXML += 1;
                                }
                                catch (Exception)
                                {
                                    /*GerarLogDownload("Erro ao efetuar Download XML [ " + ex.Message.ToString() + " ]");
                                    GerarLogDownload("Arquivo Remoto [ " + FileNamePDFRemoto + " ]");
                                    GerarLogDownload("Arquivo Local[ " + FileNamePDFLocal + " ]");*/
                                }
                            }
                            else
                            {
                                //GerarLogDownload("Arquivo já existe [ " + FileNameXMLLocal.ToString() + " ]");
                            }
                        }

                        // Cancelamento
                        if (NotaFiscalStatus.Trim() == "CANCELADA")
                        {
                            string FileNameCancelamento = Path.Combine(DirNotasFiscais, "NF " + NotaFiscalNumero + " - Protocolo Cancelamento.xml");

                            if (!System.IO.File.Exists(FileNameCancelamento))
                            {
                                try
                                {
                                    string URLAuth = String.Empty;
                                    String textBoxDownload = String.Empty;
                                    HttpWebRequest webRequest;
                                    HttpWebResponse webResponse;
                                    StreamReader responseReader;
                                    string responseData;
                                    string dadosEnviar = String.Empty;
                                    dadosEnviar = "/v2/empresas/" + TextBoxEmpresaID + "/nfc-e/" + NotaFiscalIdentificador.Trim() + "/xmlCancelamento";
                                    URLAuth = "https://api.enotasgw.com.br" + dadosEnviar;

                                    System.Net.ServicePointManager.Expect100Continue = false;
                                    webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                                    webRequest.Method = "GET";
                                    webRequest.ContentType = "application/json";
                                    webRequest.Headers.Add("Authorization", "Basic " + TextBoxApiKey);

                                    responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                                    webResponse = (HttpWebResponse)webRequest.GetResponse();
                                    responseData = responseReader.ReadToEnd();
                                    responseReader.Close();
                                    webRequest.GetResponse().Close();
                                    if (webResponse.StatusCode == HttpStatusCode.OK)
                                    {
                                        responseData = responseData.Replace(@"\""", "'");
                                        textBoxDownload += responseData;
                                        System.IO.File.WriteAllText(FileNameCancelamento, textBoxDownload);
                                        QtdDownloadCancelamento += 1;
                                        ListaArquivosZip.Add(FileNameCancelamento);
                                        //GerarLogDownload("Download Protocolo Cancelamento [ " + FileNameCancelamento + " ]");
                                    }
                                    else
                                    {
                                        //GerarLogDownload("ERRO Download Protocolo Cancelamento [ " + responseData + " ]");
                                    }
                                    //this.Refresh();
                                    System.Threading.Thread.Sleep(250);
                                }
                                catch (Exception)
                                {
                                    //GerarLogDownload("Erro ao efetuar Download Cancelamento [ " + ex.Message.ToString() + " ]");
                                    //GerarLogDownload("Nota Fiscal [ " + NotaFiscalIdentificador + " ]");
                                }
                            }
                            else
                            {
                                //GerarLogDownload("Arquivo já existe [ " + FileNameCancelamento.ToString() + " ]");
                            }
                        }
                        //GerarLogDownload("Downloads Danfe e XMLs = PDFs: " + QtdDownloadPDF.ToString() + " | " + "XMLs: " + QtdDownloadXML.ToString() + " | " + "Cancelamentos: " + QtdDownloadCancelamento.ToString());
                    }

                    if (ListaArquivosZip.Count > 0)
                    {
                        // Gerar o arquivo Zip
                        using (ZipOutputStream s = new ZipOutputStream(System.IO.File.Create(FullFileNameZIP)))
                        {
                            s.SetLevel(9); // 0-9, 9 being the highest compression  

                            byte[] buffer = new byte[4096];

                            for (int i = 0; i < ListaArquivosZip.Count; i++)
                            {
                                ZipEntry entry = new ZipEntry(Path.GetFileName(ListaArquivosZip[i]));
                                entry.DateTime = DateTime.Now;
                                entry.IsUnicodeText = true;
                                s.PutNextEntry(entry);

                                using (FileStream fs = System.IO.File.OpenRead(ListaArquivosZip[i]))
                                {
                                    int sourceBytes;
                                    do
                                    {
                                        sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                        s.Write(buffer, 0, sourceBytes);
                                    } while (sourceBytes > 0);
                                }
                            }
                            s.Finish();
                            s.Flush();
                            s.Close();
                        }

                        // Apagar os arquivos PDF e XML
                        foreach (string FileName in ListaArquivosZip)
                        {
                            if (System.IO.File.Exists(FileName))
                            {
                                try { System.IO.File.Delete(FileName); } catch { };
                            }
                        }

                        byte[] finalResult = System.IO.File.ReadAllBytes(FullFileNameZIP);
                        if (finalResult == null || !finalResult.Any()) { throw new Exception(String.Format("Não há lançamentos que atendam à pesquisa realizada")); };

                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 46; // Relatório Regulamentação ANP
                        record_g_processamento.id_processamento_modulo = 3; // Relatórios Regulamentação
                        record_g_processamento.detalhamento = "Relatório Regulamentação ANP";
                        record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                        record_g_processamento.datahora_inicio = DataHoraAtual;
                        record_g_processamento.datahora_final = DataHoraAtual;
                        record_g_processamento.qtd_registros = NumeroRegistrosExportados;
                        record_g_processamento.qtd_reg_ok = NumeroRegistrosExportados;
                        record_g_processamento.qtd_reg_erro = 0;
                        record_g_processamento.processando = false;
                        record_g_processamento.concluido = true;
                        record_g_processamento.pathfile = FullFileNameZIP;
                        record_g_processamento.id_coligada = 1;
                        record_g_processamento.id_filial = 1;
                        db.g_processamento.Add(record_g_processamento);
                        db.SaveChanges();

                        Sucesso = true;
                        IdProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                        MsgRetorno = "Arquivo Compactado GERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "[" + NumeroRegistrosExportados.ToString() + " registros]" + "<br/><br/>" + "O Download do relatório será iniciado automaticamente!";
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
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = IdProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalRelatorioIBAMA
        public ActionResult ModalRelatorioIBAMA(int? id)
        {
            cstModalRelatorio view_cstModalRelatorio = new cstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório - IBAMA";
            return View("ModalRelatorioIBAMA", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioIBAMA(cstModalRelatorio view_cstModalRelatorio)
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
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_regulamentacao.xlsx");

            try
            {
                String TextSQL = " SELECT nf.nf_numero, nf.nf_data_geracao,   " +
                                " replace(replace(clientes.razao_social, ';', ''), ',', '') as 'razao_social',   " +
                                " clientes.cnpj, clientes.cpf, itens.quantidade,   " +
                                " replace(replace(produtos.descricao, ';', ''), ',', '') as 'descricao',   " +
                                " cfop.numero as 'cfop_numero', cfop.descricao as 'cfop_descricao', nfstatus.descricao_resumida as 'status',  " +
                                " tiposmov.is_entrada, tiposmov.is_saida  " +
                                " FROM gc_movimentos_itens itens  " +
                                " left join gc_movimentos movimentos on (movimentos.id_movimento = itens.id_movimento)  " +
                                " left join gc_movimentos_tipos tiposmov on (tiposmov.id_movimento_tipo = movimentos.id_movimento_tipo)  " +
                                " left join gc_movimentos_nf nf on (nf.id_movimento = itens.id_movimento)  " +
                                " left join g_nfe_status nfstatus on (nf.id_nfe_status = nfstatus.id_nfe_status)  " +
                                " left join gc_cfop cfop on (cfop.id_cfop = nf.id_cfop)  " +
                                " left join g_produtos produtos on (produtos.id_produto = itens.id_produto)  " +
                                " left join g_clientes clientes on (clientes.id_cliente = movimentos.id_cliente)  " +
                                " where (nf.nf_data_geracao between '" + DataInicialSQL + "' and '" + DataFinalSQL + "')  " +
                                " and ((nf.id_nfe_status = 8) or(nf.id_nfe_status = 17))  " +
                                " and (itens.id_produto in (select distinct(id_produto) from g_produtos where item_regulado_ibama = 1)) " +
                                " and itens.quantidade > 0 order by id_movimento_nf;  ";

                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (allRecordsNotas.Count > 0)
                {
                    WorkSheet.Cell(1, 1).Value = "Relatório Regulamentação do IBAMA";
                    WorkSheet.Cell(2, 1).Value = "Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy");

                    foreach (var RowNota in allRecordsNotas)
                    {
                        IndexLinha += 1;

                        bool IsEntrada = Convert.ToBoolean(RowNota["is_entrada"].EmptyIfNull().ToString().Trim());
                        bool IsSaida = Convert.ToBoolean(RowNota["is_saida"].EmptyIfNull().ToString().Trim());
                        WorkSheet.Cell(IndexLinha, 1).Value = int.Parse(RowNota["nf_numero"].EmptyIfNull().ToString().Trim());
                        WorkSheet.Cell(IndexLinha, 2).Value = Convert.ToDateTime(RowNota["nf_data_geracao"]).ToString("dd/MM/yyyy HH:mm");
                        WorkSheet.Cell(IndexLinha, 3).Value = RowNota["razao_social"].EmptyIfNull().ToString().Trim();
                        if (RowNota["cnpj"].EmptyIfNull().ToString().Trim().Length > 0) { WorkSheet.Cell(IndexLinha, 4).Value = LibStringFormat.FormatarCPFCNPJ("J", RowNota["cnpj"].EmptyIfNull().ToString().Trim()); }
                        else if (RowNota["cpf"].EmptyIfNull().ToString().Trim().Length > 0) { WorkSheet.Cell(IndexLinha, 4).Value = LibStringFormat.FormatarCPFCNPJ("F", RowNota["cpf"].EmptyIfNull().ToString().Trim()); };
                        if (IsEntrada == true)
                        {
                            WorkSheet.Cell(IndexLinha, 5).Value = double.Parse(RowNota["quantidade"].EmptyIfNull().ToString().Trim());
                            WorkSheet.Cell(IndexLinha, 5).Style.Fill.BackgroundColor = XLColor.LightBlue;
                        }
                        else if (IsSaida == true)
                        {
                            WorkSheet.Cell(IndexLinha, 6).Value = double.Parse(RowNota["quantidade"].EmptyIfNull().ToString().Trim());
                            WorkSheet.Cell(IndexLinha, 6).Style.Fill.BackgroundColor = XLColor.Yellow;
                        }

                        WorkSheet.Cell(IndexLinha, 7).Value = RowNota["descricao"].EmptyIfNull().ToString().Trim();
                        WorkSheet.Cell(IndexLinha, 8).Value = RowNota["cfop_numero"].EmptyIfNull().ToString().Trim() + " - " + RowNota["cfop_descricao"].EmptyIfNull().ToString().Trim();
                        WorkSheet.Cell(IndexLinha, 9).Value = RowNota["status"].EmptyIfNull().ToString().Trim();
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_IBAMA_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";
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

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 47; // Relatório Regulamentação IBAMA
                    record_g_processamento.id_processamento_modulo = 3; // Relatórios Regulamentação
                    record_g_processamento.detalhamento = "Relatório Regulamentação IBAMA";
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

        #region ModalRelatorioPF
        public ActionResult ModalRelatorioPF(int? id)
        {
            cstModalRelatorio view_cstModalRelatorio = new cstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório - PF";
            return View("ModalRelatorioPF", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioPF(cstModalRelatorio view_cstModalRelatorio)
        {
            bool Sucesso = false;
            int IndexLinha = 0;
            int NumeroRegistrosExportados = 0;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            String IdMovimentoAnterior = String.Empty;
            String IdMovimentoAtual = String.Empty;
            DateTime DataInicial = new DateTime();
            DateTime DataFinal = new DateTime();
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_01.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataInicial);
            DateTime.TryParse(view_cstModalRelatorio.Field_Data_02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataFinal);
            String DataInicialSQL = DataInicial.ToString("yyyy-MM-dd 00:00:00");
            String DataFinalSQL = DataFinal.ToString("yyyy-MM-dd 23:59:59");
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_regulamentacao.xlsx");

            try
            {
                String TextSQL = " SELECT nf.nf_numero, nf.nf_data_geracao,   " +
                                " replace(replace(clientes.razao_social, ';', ''), ',', '') as 'razao_social',   " +
                                " clientes.cnpj, clientes.cpf, itens.quantidade,   " +
                                " replace(replace(produtos.descricao, ';', ''), ',', '') as 'descricao',   " +
                                " cfop.numero as 'cfop_numero', cfop.descricao as 'cfop_descricao', nfstatus.descricao_resumida as 'status',  " +
                                " tiposmov.is_entrada, tiposmov.is_saida, movimentos.id_movimento  " +
                                " FROM gc_movimentos_itens itens  " +
                                " left join gc_movimentos movimentos on (movimentos.id_movimento = itens.id_movimento)  " +
                                " left join gc_movimentos_tipos tiposmov on (tiposmov.id_movimento_tipo = movimentos.id_movimento_tipo)  " +
                                " left join gc_movimentos_nf nf on (nf.id_movimento = itens.id_movimento)  " +
                                " left join g_nfe_status nfstatus on (nf.id_nfe_status = nfstatus.id_nfe_status)  " +
                                " left join gc_cfop cfop on (cfop.id_cfop = nf.id_cfop)  " +
                                " left join g_produtos produtos on (produtos.id_produto = itens.id_produto)  " +
                                " left join g_clientes clientes on (clientes.id_cliente = movimentos.id_cliente)  " +
                                " where (nf.nf_data_geracao between '" + DataInicialSQL + "' and '" + DataFinalSQL + "')  " +
                                " and ((nf.id_nfe_status = 8) or (nf.id_nfe_status = 17))  " +
                                " and (itens.id_produto in (select distinct(id_produto) from g_produtos where item_regulado_pf = 1)) " +
                                " and itens.quantidade > 0 order by movimentos.id_movimento, nf.id_movimento_nf;  ";

                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (allRecordsNotas.Count > 0)
                {
                    WorkSheet.Cell(1, 1).Value = "Relatório Regulamentação da PF";
                    WorkSheet.Cell(2, 1).Value = "Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy");

                    foreach (var RowNota in allRecordsNotas)
                    {
                        IdMovimentoAtual = RowNota["id_movimento"].EmptyIfNull().ToString().Trim();
                        if (IdMovimentoAtual != IdMovimentoAnterior)
                        {
                            IndexLinha += 1;
                            bool IsEntrada = Convert.ToBoolean(RowNota["is_entrada"].EmptyIfNull().ToString().Trim());
                            bool IsSaida = Convert.ToBoolean(RowNota["is_saida"].EmptyIfNull().ToString().Trim());
                            WorkSheet.Cell(IndexLinha, 1).Value = int.Parse(RowNota["nf_numero"].EmptyIfNull().ToString().Trim());
                            WorkSheet.Cell(IndexLinha, 2).Value = Convert.ToDateTime(RowNota["nf_data_geracao"]).ToString("dd/MM/yyyy HH:mm");
                            WorkSheet.Cell(IndexLinha, 3).Value = RowNota["razao_social"].EmptyIfNull().ToString().Trim();
                            if (RowNota["cnpj"].EmptyIfNull().ToString().Trim().Length > 0) { WorkSheet.Cell(IndexLinha, 4).Value = LibStringFormat.FormatarCPFCNPJ("J", RowNota["cnpj"].EmptyIfNull().ToString().Trim()); }
                            else if (RowNota["cpf"].EmptyIfNull().ToString().Trim().Length > 0) { WorkSheet.Cell(IndexLinha, 4).Value = LibStringFormat.FormatarCPFCNPJ("F", RowNota["cpf"].EmptyIfNull().ToString().Trim()); }
                            ;
                            if (IsEntrada == true)
                            {
                                WorkSheet.Cell(IndexLinha, 5).Value = double.Parse(RowNota["quantidade"].EmptyIfNull().ToString().Trim());
                                WorkSheet.Cell(IndexLinha, 5).Style.Fill.BackgroundColor = XLColor.LightBlue;
                            }
                            else if (IsSaida == true)
                            {
                                WorkSheet.Cell(IndexLinha, 6).Value = double.Parse(RowNota["quantidade"].EmptyIfNull().ToString().Trim());
                                WorkSheet.Cell(IndexLinha, 6).Style.Fill.BackgroundColor = XLColor.Yellow;
                            }

                            WorkSheet.Cell(IndexLinha, 7).Value = RowNota["descricao"].EmptyIfNull().ToString().Trim();
                            WorkSheet.Cell(IndexLinha, 8).Value = RowNota["cfop_numero"].EmptyIfNull().ToString().Trim() + " - " + RowNota["cfop_descricao"].EmptyIfNull().ToString().Trim();
                            WorkSheet.Cell(IndexLinha, 9).Value = RowNota["status"].EmptyIfNull().ToString().Trim();
                            NumeroRegistrosExportados += 1;
                        }
                        IdMovimentoAnterior = IdMovimentoAtual;
                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_PF_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";
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

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 47; // Relatório Regulamentação PF
                    record_g_processamento.id_processamento_modulo = 3; // Relatórios Regulamentação
                    record_g_processamento.detalhamento = "Relatório Regulamentação PF";
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

        #region ModalRelatorioJoguelimpo
        public ActionResult ModalRelatorioJogueLimpo(int? id)
        {
            cstModalRelatorio view_cstModalRelatorio = new cstModalRelatorio();
            view_cstModalRelatorio.Field_Data_01 = LibDateTime.getPrimeiroDiaMesAtual();
            view_cstModalRelatorio.Field_Data_02 = LibDateTime.getUltimoDiaMesAtual();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-print", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Relatório - Jogue Limpo";
            return View("ModalRelatorioJogueLimpo", view_cstModalRelatorio);
        }

        [HttpPost]
        public ActionResult AjaxModalRelatorioJogueLimpo(cstModalRelatorio view_cstModalRelatorio)
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
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_gc_relatorio_regulamentacao.xlsx");

            try
            {
                String TextSQL = " SELECT nf.nf_numero, nf.nf_data_geracao,   " +
                                " replace(replace(clientes.razao_social, ';', ''), ',', '') as 'razao_social',   " +
                                " clientes.cnpj, clientes.cpf, itens.quantidade,   " +
                                " replace(replace(produtos.descricao, ';', ''), ',', '') as 'descricao',   " +
                                " cfop.numero as 'cfop_numero', cfop.descricao as 'cfop_descricao', nfstatus.descricao_resumida as 'status',  " +
                                " tiposmov.is_entrada, tiposmov.is_saida  " +
                                " FROM gc_movimentos_itens itens  " +
                                " left join gc_movimentos movimentos on (movimentos.id_movimento = itens.id_movimento)  " +
                                " left join gc_movimentos_tipos tiposmov on (tiposmov.id_movimento_tipo = movimentos.id_movimento_tipo)  " +
                                " left join gc_movimentos_nf nf on (nf.id_movimento = itens.id_movimento)  " +
                                " left join g_nfe_status nfstatus on (nf.id_nfe_status = nfstatus.id_nfe_status)  " +
                                " left join gc_cfop cfop on (cfop.id_cfop = nf.id_cfop)  " +
                                " left join g_produtos produtos on (produtos.id_produto = itens.id_produto)  " +
                                " left join g_clientes clientes on (clientes.id_cliente = movimentos.id_cliente)  " +
                                " where (nf.nf_data_geracao between '" + DataInicialSQL + "' and '" + DataFinalSQL + "')  " +
                                " and ((nf.id_nfe_status = 8) or(nf.id_nfe_status = 17))  " +
                                " and (itens.id_produto in (select distinct(id_produto) from g_produtos where item_regulado_joguelimpo = 1)) " +
                                " and itens.quantidade > 0 order by id_movimento_nf;  ";

                DataTable tableRegistro = LibDB.GetDataTable(TextSQL, db);
                List<DataRow> allRecordsNotas = tableRegistro.AsEnumerable().ToList();

                IndexLinha = 3;
                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                XLWorkbook WorkBook = new XLWorkbook(FileNameTemplate);
                IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                if (allRecordsNotas.Count > 0)
                {


                    WorkSheet.Cell(1, 1).Value = "Relatório Regulamentação da JoqueLimpo";
                    WorkSheet.Cell(2, 1).Value = "Período: " + DataInicial.ToString("dd/MM/yy") + " à " + DataFinal.ToString("dd/MM/yy");

                    foreach (var RowNota in allRecordsNotas)
                    {
                        IndexLinha += 1;

                        bool IsEntrada = Convert.ToBoolean(RowNota["is_entrada"].EmptyIfNull().ToString().Trim());
                        bool IsSaida = Convert.ToBoolean(RowNota["is_saida"].EmptyIfNull().ToString().Trim());
                        WorkSheet.Cell(IndexLinha, 1).Value = int.Parse(RowNota["nf_numero"].EmptyIfNull().ToString().Trim());
                        WorkSheet.Cell(IndexLinha, 2).Value = Convert.ToDateTime(RowNota["nf_data_geracao"]).ToString("dd/MM/yyyy HH:mm");
                        WorkSheet.Cell(IndexLinha, 3).Value = RowNota["razao_social"].EmptyIfNull().ToString().Trim();
                        if (RowNota["cnpj"].EmptyIfNull().ToString().Trim().Length > 0) { WorkSheet.Cell(IndexLinha, 4).Value = LibStringFormat.FormatarCPFCNPJ("J", RowNota["cnpj"].EmptyIfNull().ToString().Trim()); }
                        else if (RowNota["cpf"].EmptyIfNull().ToString().Trim().Length > 0) { WorkSheet.Cell(IndexLinha, 4).Value = LibStringFormat.FormatarCPFCNPJ("F", RowNota["cpf"].EmptyIfNull().ToString().Trim()); };
                        if (IsEntrada == true)
                        {
                            WorkSheet.Cell(IndexLinha, 5).Value = double.Parse(RowNota["quantidade"].EmptyIfNull().ToString().Trim());
                            WorkSheet.Cell(IndexLinha, 5).Style.Fill.BackgroundColor = XLColor.LightBlue;
                        }
                        else if (IsSaida == true)
                        {
                            WorkSheet.Cell(IndexLinha, 6).Value = double.Parse(RowNota["quantidade"].EmptyIfNull().ToString().Trim());
                            WorkSheet.Cell(IndexLinha, 6).Style.Fill.BackgroundColor = XLColor.Yellow;
                        }

                        WorkSheet.Cell(IndexLinha, 7).Value = RowNota["descricao"].EmptyIfNull().ToString().Trim();
                        WorkSheet.Cell(IndexLinha, 8).Value = RowNota["cfop_numero"].EmptyIfNull().ToString().Trim() + " - " + RowNota["cfop_descricao"].EmptyIfNull().ToString().Trim();
                        WorkSheet.Cell(IndexLinha, 9).Value = RowNota["status"].EmptyIfNull().ToString().Trim();
                        NumeroRegistrosExportados += 1;
                    }

                    // Salvar o arquivo em disco
                    FileNameExportacao = "Relatório_JogueLimpo_" + DataHoraAtual.ToString("yyyyMMdd_HHmmss") + ".xlsx";
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

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Atualizar o registro do processamento
                    g_processamento record_g_processamento = new g_processamento();
                    record_g_processamento.id_processamento_tipo = 47; // Relatório Regulamentação JogueLimpo
                    record_g_processamento.id_processamento_modulo = 3; // Relatórios Regulamentação
                    record_g_processamento.detalhamento = "Relatório Regulamentação Jogue Limpo";
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