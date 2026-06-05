// Migrado em 2020_07_15

using DocumentFormat.OpenXml.Spreadsheet;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using ICSharpCode.SharpZipLib.Zip;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Financeiro_*,g_Financeiro_Default,gc_Movimentos_*,gc_Movimentos_Default")]
    public partial class FinanceiroController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Financeiro";

        private static readonly string[] DateFormats = new[]
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "dd/MM/yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss.fff"
        };
        
        protected string RenderPartialViewToString(string viewName, object model)
        {
            string toReturn;
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewData.Model = model;

            using (StringWriter sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                toReturn = sw.GetStringBuilder().ToString();
                return toReturn;
            }
        }

        public static string RenderPartialViewToString(Controller controller, string viewName, object model)
        {
            controller.ViewData.Model = model;
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
                    ViewContext viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, sw);
                    viewResult.View.Render(viewContext, sw);

                    return sw.GetStringBuilder().ToString();
                }
            }
            catch (Exception ex)
            {
                return GdiMvcJsonResults.AjaxFailureMessage(ex);
            }
        }

        public long ToJavascriptTimestamp(DateTime input)
        {
            TimeSpan span = new TimeSpan(new DateTime(1970, 1, 1, 0, 0, 0).Ticks);
            DateTime time = input.Subtract(span);
            return (long)(time.Ticks / 10000);
        }

        public FinanceiroController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Financeiro_*,g_Financeiro_Actionread")]
        public ActionResult Index()
        {
            PreencherLookupsIndex();
            CstFinanceiroIndex record_cstFinanceiroIndex = new CstFinanceiroIndex
            {
                FinanceiroIndex_id_cliente = 0,
                FinanceiroIndex_data1 = LibDateTime.getPrimeiroDiaMesAtual(),
                FinanceiroIndex_data2 = LibDateTime.getUltimoDiaMesAtual()
            };
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Títulos Financeiros";
            return View(record_cstFinanceiroIndex);
        }

        #region GravarLogFinanceiro
        public bool GravarLogFinanceiro(int id_financeiro, String campo, String conteudo_anterior, String conteudo_novo)
        {
            bool gravado = false;
            try
            {
                g_financeiro_logs record_g_financeiro_logs = new g_financeiro_logs
                {
                    id_financeiro = id_financeiro,
                    campo = campo,
                    conteudo_anterior = conteudo_anterior,
                    conteudo_novo = conteudo_novo,
                    id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                };
                record_g_financeiro_logs.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                db.g_financeiro_logs.Add(record_g_financeiro_logs);
                gravado = true;
            }
            catch (Exception)
            {

            }
            return gravado;
        }
        #endregion

        #region DownloadZipFile
        public FileResult DownloadZipFile()
        {

            var fileName = string.Format("{0}_ImageFiles.zip", DateTime.Today.Date.ToString("dd-MM-yyyy") + "_1");
            var tempOutPutPath = Server.MapPath(Url.Content("/_filestemp/")) + fileName;

            using (ZipOutputStream s = new ZipOutputStream(System.IO.File.Create(tempOutPutPath)))
            {
                s.SetLevel(9); // 0-9, 9 being the highest compression  

                byte[] buffer = new byte[4096];

                var ImageList = new List<string>
                {
                    Server.MapPath("/_filestemp/01.jpg"),
                    Server.MapPath("/_filestemp/02.jpg")
                };


                for (int i = 0; i < ImageList.Count; i++)
                {
                    ZipEntry entry = new ZipEntry(Path.GetFileName(ImageList[i]));
                    entry.DateTime = DateTime.Now;
                    entry.IsUnicodeText = true;
                    s.PutNextEntry(entry);

                    using (FileStream fs = System.IO.File.OpenRead(ImageList[i]))
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

            byte[] finalResult = System.IO.File.ReadAllBytes(tempOutPutPath);
            if (System.IO.File.Exists(tempOutPutPath))
                System.IO.File.Delete(tempOutPutPath);

            if (finalResult == null || !finalResult.Any())
                throw new Exception(String.Format("No Files found with Image"));

            return File(finalResult, "application/zip", fileName);
        }
        #endregion

        #region gerarBoleto
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Financeiro_*,g_Financeiro_Actionread")]
        //public ActionResult gerarBoleto(int tipo, int? idFinanceiro, int idConsultor, string fileNamePDF)
        public ActionResult gerarBoleto(int tipoSaida, int idFinanceiro, int idVendedor, int idFaturamento)
        {
            return null;
        }


        public ActionResult ModalBoleto(int? id)
        {
            return gerarBoleto(1, id.GetValueOrDefault(), 0, 0);
        }

        public ActionResult GerarBoletoPDF(int? id)
        {
            return gerarBoleto(2, id.GetValueOrDefault(), 0, 0);
        }

        public ActionResult ModalNotaDebito(int? id)
        {
            return gerarBoleto(11, id.GetValueOrDefault(), 0, 0);
        }

        public ActionResult GerarNotaDebitoPDF(int? id)
        {
            return gerarBoleto(12, id.GetValueOrDefault(), 0, 0);
        }

        #endregion

        #region GetDados
        // 2023-05-23 customAuthorize busca em g_FinanceiroFaturamentos mesmo, pois essa classe pertencer a esse controller
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_FinanceiroFaturamentos_*,g_FinanceiroFaturamentos_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            String filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.g_financeiro>();
            var allRecordsFinanceiroStatus = db.g_financeiro_status.AsNoTracking()
                .Select(f => new { f.id_financeiro_status, f.nome }).ToList();
            DateTime DataField02 = new DateTime();
            DateTime DataField03 = new DateTime();
            DateTime.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField02);
            DateTime.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField03);
            int totalRecords = 0;
            int start = param.iDisplayStart;
            int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;

            if (param.yesFilterField.EmptyIfNull().ToString().Trim().Equals("*")) // Usuário realizou uma pesquisa
            {
                if ((param.yesCustomField01.EmptyIfNull().ToString().Trim() != String.Empty)
                && (param.yesCustomField02.EmptyIfNull().ToString().Trim() != String.Empty)
                && (param.yesCustomField03.EmptyIfNull().ToString().Trim() != String.Empty)
                && (param.yesCustomField04.EmptyIfNull().ToString().Trim() != String.Empty))
                {
                    string sqlWhere = " from g_financeiro f where f.id_financeiro > 0 ";
                    sqlWhere += " and f.data_vencimento between '" + DataField02.ToString("yyyy-MM-dd") + " 00:00:00" + "' and '" + DataField03.ToString("yyyy-MM-dd") + " 23:59:59'";
                    if (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "0")
                        sqlWhere += " and f.id_cliente = " + param.yesCustomField01.EmptyIfNull().ToString().Trim();
                    if (param.yesCustomField04.EmptyIfNull().ToString().Trim() != "0")
                        sqlWhere += " and f.id_financeiro_status = " + param.yesCustomField04.EmptyIfNull().ToString().Trim();

                    string sqlData = "select f.* " + sqlWhere;
                    LibDB.setFilterByUser(sqlData, controllerName, true, db);

                    string sqlCount = "select count(*) " + sqlWhere;
                    totalRecords = db.Database.SqlQuery<int>(sqlCount).FirstOrDefault();
                    filterOnOff = "1";

                    string sqlPage = sqlData + " order by f.id_financeiro desc OFFSET " + start + " ROWS FETCH NEXT " + length + " ROWS ONLY";
                    allRecords = db.g_financeiro.SqlQuery(sqlPage).ToList();
                }
            }

            IEnumerable<Db.g_financeiro> displayedRecords = allRecords;
            if (param.iSortingCols > 0 && param.iSortCol_0 == 1)
            {
                displayedRecords = param.sSortDir_0 == "asc"
                    ? allRecords.OrderBy(c => c.id_cliente)
                    : allRecords.OrderByDescending(c => c.id_cliente);
            }

            var pageList = displayedRecords.ToList();
            var clienteIds = pageList.Select(c => c.id_cliente).Distinct().ToList();
            var clientesDict = clienteIds.Count > 0
                ? db.g_clientes.AsNoTracking()
                    .Where(g => clienteIds.Contains(g.id_cliente) && g.ativo)
                    .Select(g => new { g.id_cliente, g.nome })
                    .ToList()
                    .ToDictionary(g => g.id_cliente, g => g.nome)
                : new Dictionary<int, string>();

            List<string[]> list = new List<string[]>();
            foreach (var c in pageList)
            {
                string nomeCliente;
                clientesDict.TryGetValue(c.id_cliente, out nomeCliente);
                var stFin = allRecordsFinanceiroStatus.FirstOrDefault(e => e.id_financeiro_status == c.id_financeiro_status);
                String nomeStatusFin = stFin != null ? stFin.nome.ToString() : String.Empty;

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_financeiro.ToString(),
                                    nomeStatusFin,
                                    c.id_cliente.ToString(),
                                    nomeCliente.EmptyIfNull().ToString(),
                                    c.data_processamento.ToString("dd/MM/yy"),
                                    c.data_vencimento.ToString("dd/MM/yy"),
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", c.valor_total_bruto).Replace("R$ ","").Replace("R$","").Replace("$",""),
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", c.valor_total_liquido).Replace("R$ ","").Replace("R$","").Replace("$",""),
                                    //((c.geracao_manual == true) ? LibIcons.getIcon("fa-solid fa-check", "Gerado Manualmente", "", "fa-lg") : "")
                                    ((c.geracao_manual == true) ? "Avulsa" : "Faturamento")
                                });
            }

            return Json(new
            {
                errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = totalRecords,
                iTotalDisplayRecords = totalRecords,
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

        #region ViewDadosConsolidados
        public ActionResult ViewDadosConsolidados()
        {
            return View("DadosConsolidados");
        }

        public ActionResult getValoresConsolidados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            const string filterOnOff = "0";
            try
            {

            DataTable tableConsolidado = LibDB.GetDataTable("select month(f.data_vencimento) as 'mes', year(f.data_vencimento) as 'ano',  count(f.id_financeiro) as 'qtd_total', " +
                                                        " sum(f.valor_original) as 'valor_total', " +
                                                        " sum(case when id_financeiro_status = 1 then 1 else 0 end) as 'qtd_status_1', " +
                                                        " sum(case when id_financeiro_status = 2 then 1 else 0 end) as 'qtd_status_2', " +
                                                        " sum(case when id_financeiro_status = 3 then 1 else 0 end) as 'qtd_status_3', " +
                                                        " sum(case when id_financeiro_status = 4 then 1 else 0 end) as 'qtd_status_4', " +
                                                        " sum(case when id_financeiro_status = 1 then valor_original else 0 end) as 'total_status_1', " +
                                                        " sum(case when id_financeiro_status = 2 then valor_original else 0 end) as 'total_status_2', " +
                                                        " sum(case when id_financeiro_status = 3 then valor_original else 0 end) as 'total_status_3', " +
                                                        " sum(case when id_financeiro_status = 4 then valor_original else 0 end) as 'total_status_4' " +
                                                        " from g_financeiro f where f.id_financeiro > 0 " + 
                                                        " group by year(f.data_vencimento), month(f.data_vencimento) " +
                                                        " order by year(f.data_vencimento), month(f.data_vencimento) ", db);

            List<DataRow> allRecords = tableConsolidado.AsEnumerable().ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);

            List<string[]> list = new List<string[]>();
            foreach (var linha in displayedRecords)
            {
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    linha["mes"].ToString()+"/"+linha["ano"].ToString(),
                                    linha["qtd_status_2"].ToString(),
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(linha["total_status_2"].ToString())), // Quitado
                                    linha["qtd_status_1"].ToString(),
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(linha["total_status_1"].ToString())), // Aberto
                                    linha["qtd_status_4"].ToString(),
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(linha["total_status_4"].ToString())), // Transferido
                                    linha["qtd_total"].ToString(),
                                    string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(linha["valor_total"].ToString())) // Totais
                                });
            }

            return Json(new
            {
                errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = list.Count,
                iTotalDisplayRecords = tableConsolidado.Rows.Count,
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

        #region GetDadosGrafico
        public ActionResult GetDadosGrafico(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            try
            {
            DataTable tableConsolidado = LibDB.GetDataTable("select month(f.data_vencimento) as 'mes', year(f.data_vencimento) as 'ano',  count(f.id_financeiro) as 'qtd_total', " +
                                                        " sum(f.valor_original) as 'valor_total', " +
                                                        " sum(case when id_financeiro_status = 1 then 1 else 0 end) as 'qtd_status_1', " +
                                                        " sum(case when id_financeiro_status = 2 then 1 else 0 end) as 'qtd_status_2', " +
                                                        " sum(case when id_financeiro_status = 3 then 1 else 0 end) as 'qtd_status_3', " +
                                                        " sum(case when id_financeiro_status = 4 then 1 else 0 end) as 'qtd_status_4', " +
                                                        " sum(case when id_financeiro_status = 1 then valor_original else 0 end) as 'total_status_1', " +
                                                        " sum(case when id_financeiro_status = 2 then valor_original else 0 end) as 'total_status_2', " +
                                                        " sum(case when id_financeiro_status = 3 then valor_original else 0 end) as 'total_status_3', " +
                                                        " sum(case when id_financeiro_status = 4 then valor_original else 0 end) as 'total_status_4' " +
                                                        " from g_financeiro f where f.id_financeiro > 0 " +
                                                        " group by year(f.data_vencimento), month(f.data_vencimento) " +
                                                        " order by year(f.data_vencimento), month(f.data_vencimento) ", db);

            List<string[]> listQuitado = new List<string[]>();
            List<string[]> listAberto = new List<string[]>();
            List<string[]> listTransferido = new List<string[]>();
            List<string[]> listTotal = new List<string[]>();
            List<string[]> listTicks = new List<string[]>();
            int index = 0;
            foreach (DataRow linha in tableConsolidado.Rows)
            {
                index += 1;

                String mes = linha["mes"].ToString();
                if (mes.Length == 1) { mes = "0" + mes; }
                String ano = linha["ano"].ToString();
                if (ano.Length == 4) { ano = ano.Substring(2); }

                listTicks.Add(new[] {
                                    index.ToString(),
                                    mes+"/"+ano
                                });

                listAberto.Add(new[] {
                                    linha["mes"].ToString(),
                                    linha["total_status_1"].ToString().Replace(",",".")
                                });

                listQuitado.Add(new[] {
                                    linha["mes"].ToString(),
                                    linha["total_status_2"].ToString().Replace(",",".")
                                });

                listTransferido.Add(new[] {
                                    linha["mes"].ToString(),
                                    linha["total_status_4"].ToString().Replace(",",".")
                                });

                listTotal.Add(new[] {
                                    linha["mes"].ToString(),
                                    linha["valor_total"].ToString().Replace(",",".")
                                });

            }

            return Json(new
            {
                errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                dataAberto = listAberto,
                dataTransferido = listTransferido,
                dataQuitado = listQuitado,
                dataTotal = listTotal,
                dataTicks = listTicks
            },
            JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.AjaxFailureMessage(e),
                    severity = GdiMvcJsonResults.SeverityError,
                    stackTrace = e.ToString(),
                    dataAberto = new List<string[]>(),
                    dataTransferido = new List<string[]>(),
                    dataQuitado = new List<string[]>(),
                    dataTotal = new List<string[]>(),
                    dataTicks = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }
        }
        #endregion

        #region ModalGerarRemessaBoletosBancarios
        public ActionResult ModalGerarRemessaBoletosBancarios(String id)
        {
            preencherCombosModalGerarRemessaBoletosBancarios();
            ViewBag.Title = "Gerar Remessa - Boletos Bancários (Títulos Abertos e Cancelados)";
            jQueryDataTableParamModel param = new jQueryDataTableParamModel();
            string filterSQL = String.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, db);
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0)
            { filterSQL = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); }
            g_contas_caixas record_g_contas_caixas = new g_contas_caixas();
            if (filterSQL.Trim().Length == 0) { record_g_contas_caixas.id_processamento = -1; } else { record_g_contas_caixas.id_processamento = 0; };
            return View(record_g_contas_caixas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxGerarRemessaBoletosBancarios(g_contas_caixas view_record_g_contas_caixas)
        {
            bool sucesso = true;
            String msgRetorno = String.Empty;
            String layoutRemessa = String.Empty;
            String nomeBanco = String.Empty;
            String arquivoSaida = String.Empty;
            String fileNameDestino = String.Empty;
            String fileNameExportacao = String.Empty;
            String idProcessamentoGravado = "0";
            String SentencaSQL = String.Empty;
            String filterSQL = String.Empty;
            int qtdRegistros = 0;
            int numeroRegistro = 1;
            decimal valorTotal = 0;
            DateTime datahora = LibDateTime.getDataHoraBrasilia();
            DataTable tableConsolidado = null;
            List<DataRow> allFinanceiro = null;
            g_financeiro_remessas record_g_financeiro_remessas = null;
            var allCidades = new List<g_cidades>();
            var allUF = new List<g_uf>();
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                // Verificar o Filtro
                jQueryDataTableParamModel param = new jQueryDataTableParamModel();

                g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, db);
                if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0)
                { filterSQL = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); }

                if (filterSQL.Trim().Length > 0)
                {
                    int pos1 = filterSQL.IndexOf("f.data_vencimento ");
                    filterSQL = filterSQL.Substring(pos1);
                    int pos2 = filterSQL.IndexOf("and f.id_financeiro_status");
                    if (pos2 > 0)
                    {
                        if ((filterSQL.IndexOf("and f.id_financeiro_status = 1") > 0) || (filterSQL.IndexOf("and f.id_financeiro_status = 3") > 0)) // 1 - Aberto ou 3 = Em Cancelamento OK
                        {

                        }
                        else if (filterSQL.IndexOf("and f.id_financeiro_status = 0") > 0) // 0 - Todos
                        {
                            filterSQL = filterSQL.Replace("and f.id_financeiro_status = 0", "and (f.id_financeiro_status = 1 or f.id_financeiro_status = 3)");
                        }
                        else // Outros Status, invalidar o Filtro, não retornar nada
                        {
                            filterSQL = filterSQL.Substring(0, pos2);
                            filterSQL += " and f.id_financeiro_status = 0 ";
                        }
                    }
                    else
                    {
                        filterSQL += " and (f.id_financeiro_status = 1 or f.id_financeiro_status = 3) ";
                    }

                    SentencaSQL = "select f.id_financeiro, f.id_financeiro_status, f.valor_total_bruto, f.cnab_nosso_numero, f.data_vencimento, f.data_processamento, " +
                                    "c.cpf, c.cnpj, c.id_cidade_com, c.id_uf_com, c.nome, c.endereco_com, c.bairro_com, c.cep_com " +
                                    "from g_financeiro f " +
                                    "left join g_clientes c on(c.id_cliente = f.id_cliente) " +
                                    "where f.id_conta_caixa_geracao = " + view_record_g_contas_caixas.id_conta_caixa.EmptyIfNull().ToString() + " " +
                                    "and f.tipo_pag_rec = 2 and f.id_financeiro_remessa is null and " + filterSQL;

                    try
                    {
                        tableConsolidado = LibDB.GetDataTable(SentencaSQL, db);
                        allFinanceiro = tableConsolidado.AsEnumerable().ToList();
                    }
                    catch (Exception e)
                    {
                        sucesso = false;
                        msgRetorno = GdiMvcJsonResults.AjaxFailureMessage(e);
                    };

                    if ((sucesso == true) && (allFinanceiro != null) && (allFinanceiro.Count() > 0))
                    {
                        g_contas_caixas record_conta_caixa = record_conta_caixa = db.g_contas_caixas.Find(view_record_g_contas_caixas.id_conta_caixa);
                        var allRecordsClientes = db.g_clientes.Select(g => new { g.id_cliente, g.nome }).ToList();
                        allCidades = (from _cidades in db.g_cidades select _cidades).ToList();
                        allUF = (from _UF in db.g_uf select _UF).ToList();


                        ////////// Totalizadores //////////
                        foreach (var dsRow in allFinanceiro)
                        {
                            qtdRegistros += 1;
                            valorTotal += Decimal.Parse(dsRow["valor_total_bruto"].EmptyIfNull().ToString().Trim());

                        }

                        ////////// Criação do registro da remessa //////////
                        record_g_financeiro_remessas = new g_financeiro_remessas();
                        record_g_financeiro_remessas.id_financeiro_remessa_tipo = 1; // CNAB Bancário
                        record_g_financeiro_remessas.id_financeiro_remessa_status = 1; // Aberto
                        record_g_financeiro_remessas.id_conta_caixa = view_record_g_contas_caixas.id_conta_caixa;
                        record_g_financeiro_remessas.remessa_numero = record_conta_caixa.boleto_cnab_remessas + 1;
                        record_g_financeiro_remessas.qtd_registros = qtdRegistros;
                        record_g_financeiro_remessas.valor_processado = valorTotal;
                        record_g_financeiro_remessas.valor_liquidado = 0;
                        record_g_financeiro_remessas.id_usuario_remessa = CachePersister.userIdentity.IdUsuario;
                        record_g_financeiro_remessas.datahora_remessa = datahora;
                        record_g_financeiro_remessas.id_coligada = 1;
                        record_g_financeiro_remessas.id_filial = 1;
                        record_g_financeiro_remessas.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        record_g_financeiro_remessas.datahora_cadastro = datahora;
                        db.g_financeiro_remessas.Add(record_g_financeiro_remessas);
                        record_conta_caixa.boleto_cnab_remessas++; // Atualização da Remessa na Conta Caixa
                        record_conta_caixa.datahora_alteracao = DataHoraAtual;
                        record_conta_caixa.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_conta_caixa).State = EntityState.Modified;
                        db.SaveChanges(); // Salvar os dados
                        sucesso = true;

                        ////////// Criação do arquivo de remessa //////////
                        if (record_conta_caixa.banco == "033") // Layout Santander
                        {
                            nomeBanco = "Santander";
                            layoutRemessa = "Layout: Banco 033 - Santander";
                            arquivoSaida = String.Empty;

                            // REGISTRO HEADER DO ARQUIVO REMESSA
                            arquivoSaida += "033";                                                                  // Código do Banco na compensação
                            arquivoSaida += "0000";                                                                 // Lote de serviço
                            arquivoSaida += "0";                                                                    // Tipo de registro
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 8);                   // Reservado (uso Banco)
                            arquivoSaida += "2";                                                                    // Tipo de inscrição da empresa
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.cnpj.EmptyIfNull().ToString().Trim(), 15);  // Nº de inscrição da empresa
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa("347700002440059", 15);            // Código de Transmissão
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 25);                  // Reservado (uso Banco)
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(record_conta_caixa.razao_social.EmptyIfNull().ToString().Trim(), 30);    // Nome do Beneficiário: vide planilha "Contracapa" deste arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa("BANCO SANTANDER", 30);             // Nome do Banco
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 10);                  // Reservado (uso Banco)
                            arquivoSaida += "1";                                                                    // Código remessa | 1 = Remessa
                            arquivoSaida += LibDateTime.getDataHoraBrasilia().ToString("ddMMyyyy");                 // Data de geração do arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 6);                   // Reservado (uso Banco)
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_g_financeiro_remessas.remessa_numero.EmptyIfNull().ToString().Trim(), 6); // Nº seqüencial do arquivo
                            arquivoSaida += "040";                                                                  // Nº da versão do layout do arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 74);                  // Reservado (uso Banco)
                            arquivoSaida += "\r\n";

                            // REGISTRO HEADER DO LOTE REMESSA
                            arquivoSaida += "033";                                                                  // Código do Banco na compensação
                            arquivoSaida += "0000";                                                                 // Lote de serviço
                            arquivoSaida += "1";                                                                    // Tipo de registro
                            arquivoSaida += "R";                                                                    // Tipo de operação - R - Remessa
                            arquivoSaida += "01";                                                                   // Tipo de Serviço - Cobrança
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 2);                   // Reservado (uso Banco)
                            arquivoSaida += "030";                                                                  // Nº da versão do layout do lote
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1);                   // Reservado (uso Banco)
                            arquivoSaida += "2";                                                                    // Tipo de inscrição da empresa
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.cnpj.EmptyIfNull().ToString().Trim(), 15);  // Nº de inscrição da empresa
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 20);                  // Reservado (uso Banco)
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa("347700002440059", 15);            // Código de Transmissão
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 5);                   // Reservado (uso Banco)
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(record_conta_caixa.razao_social.EmptyIfNull().ToString().Trim(), 30);    // Nome do Beneficiário: vide planilha "Contracapa" deste arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 40);                  // Mensagem 1
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 40);                  // Mensagem 2
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_g_financeiro_remessas.remessa_numero.EmptyIfNull().ToString().Trim(), 8); // Número Remessa/Retorno
                            arquivoSaida += LibDateTime.getDataHoraBrasilia().ToString("ddMMyyyy");                 // Data de geração do arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 41);                  // Reservado (uso Banco)
                            arquivoSaida += "\r\n";

                            // Registro
                            foreach (var dsRow in allFinanceiro)
                            {
                                String _tipoInscricaoPagador = String.Empty;
                                String _documentoPagador = String.Empty;
                                String _valorTitulo = (Decimal.Parse(dsRow["valor_total_bruto"].EmptyIfNull().ToString().Trim()) * 100).ToString("000000000000000");
                                String _nossoNumero = LibStringFormat.FormatarNumeroSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 12);
                                _nossoNumero += GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularDigitoNossoNumero(record_conta_caixa.banco.EmptyIfNull().ToString().Trim(), record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), record_conta_caixa.carteira.EmptyIfNull().ToString().Trim(), _nossoNumero, record_conta_caixa.codigo_empresa.EmptyIfNull().ToString().Trim());

                                Decimal valorMultaFixo = 0;
                                if (record_conta_caixa.multa_fixo.EmptyIfNull().ToString().Trim() != String.Empty)
                                { Decimal.TryParse(record_conta_caixa.multa_fixo.EmptyIfNull().ToString().Trim(), out valorMultaFixo); }
                                String _valorMultaFixo = (valorMultaFixo * 100).ToString("000000");

                                Decimal valorMultaDia = 0;
                                if (record_conta_caixa.multa_dia.EmptyIfNull().ToString().Trim() != String.Empty)
                                { Decimal.TryParse(record_conta_caixa.multa_dia.EmptyIfNull().ToString().Trim(), out valorMultaDia); }
                                //valorMultaDia = 123456789;
                                //String _valorMultaDia = (valorMultaDia * 100).ToString().Replace(".", "").Replace(",", "").Replace(" ", "");
                                String _valorMultaDia = (valorMultaDia * 100).ToString("000000000000000");

                                if (dsRow["cpf"].EmptyIfNull().ToString().Trim() != String.Empty)
                                {
                                    _tipoInscricaoPagador = "1";
                                    _documentoPagador = LibStringFormat.FormatarNumeroSerasa(dsRow["cpf"].EmptyIfNull().ToString().Trim(), 15);
                                }
                                else
                                {
                                    _tipoInscricaoPagador = "2";
                                    _documentoPagador = LibStringFormat.FormatarNumeroSerasa(dsRow["cnpj"].EmptyIfNull().ToString().Trim(), 15);
                                };
                                String nomeCidadePagador = String.Empty;
                                if (dsRow["id_cidade_com"].EmptyIfNull().ToString().Trim() != string.Empty)
                                {
                                    nomeCidadePagador = allCidades.Find(c => c.id_cidade == int.Parse(dsRow["id_cidade_com"].EmptyIfNull().ToString().Trim())).nome.ToString();
                                }
                                String nomeUFPagador = String.Empty;
                                if (dsRow["id_uf_com"].EmptyIfNull().ToString().Trim() != string.Empty)
                                {
                                    nomeUFPagador = allUF.Find(c => c.id_uf == int.Parse(dsRow["id_uf_com"].EmptyIfNull().ToString().Trim())).sigla.ToString();
                                }

                                // REGISTRO DETALHE SEGMENTO P
                                arquivoSaida += "033";                                                                      // Código do Banco na compensação
                                arquivoSaida += "0000";                                                                     // Lote de serviço
                                arquivoSaida += "3"; // Tipo de Registro
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(numeroRegistro.ToString(), 5); // Número sequencial do registro no lote
                                arquivoSaida += "P"; // Cód. Segmento do Registro Detalhe: "P"
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                                arquivoSaida += "01"; // Entrada de Títulos
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), 4);                        // Prefixo da Cooperativa: vide planilha "Contracapa" deste arquivo
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.dv_agencia.EmptyIfNull().ToString().Trim(), 1); // Dígito Verificador do Prefixo: Preencher com espaços em branco
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), 9);  // Conta Corrente: vide planilha "Contracapa" deste arquivo
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.dv_conta.EmptyIfNull().ToString().Trim(), 1);
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 9);  // Conta Cobrança
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 1);
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 2); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                                arquivoSaida += _nossoNumero;
                                arquivoSaida += "5"; // Tipo de Cobrança - 5 = Cobrança Simples (Rápida com Registro)
                                arquivoSaida += "1"; // Forma Cadastramento '1' = Cobrança Registrada(Rápida e Eletrônica com Registro)
                                arquivoSaida += "1"; // Tipo Documento - 1- Tradicional , 2- Escritural
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 15);
                                arquivoSaida += _valorTitulo; // Valor do Titulo 
                                arquivoSaida += "0000"; // Agência Encarregada da Cobrança: "00000"
                                arquivoSaida += "0"; // Dígito Verificador da Agência: Preencher com espaços em branco
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                                arquivoSaida += "04"; // Espécie do Título - 04 DS - DUPLICATA DE SERVICO
                                arquivoSaida += "N"; // Não Aceito
                                arquivoSaida += DateTime.Parse(dsRow["data_processamento"].EmptyIfNull().ToString().Trim()).ToString("ddMMyyyy"); // Data da Emissão do Título
                                if (valorMultaDia > 0)
                                {
                                    arquivoSaida += "2";  // Juros - Taxa Mensal
                                    arquivoSaida += DateTime.Parse(dsRow["data_vencimento"].EmptyIfNull().ToString().Trim()).ToString("ddMMyyyy"); // Data dos Juros de Mora
                                }
                                else
                                {
                                    arquivoSaida += "3";  // Juros - Isento
                                    arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 8); // Data dos Juros de Mora
                                }
                                arquivoSaida += _valorMultaDia; // Taxa de Multa Dia

                                arquivoSaida += "0";// Código do Desconto - Não Conceder Desconto 
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 8); // Data do Desconto
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 15); // Valor percentual do desconto
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 15); // Valor IOF
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 15); // Valor Abatimento

                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 25);
                                arquivoSaida += "0";// Protesto - Não Protestar
                                arquivoSaida += "00";// Prazo Protesto
                                arquivoSaida += "0";// Código para Baixa/Devolução: "0"
                                arquivoSaida += "0";// Zero Fixo
                                arquivoSaida += "00";// Prazo Baixa/Devolução
                                arquivoSaida += "00"; // "Código da Moeda: '00' = Real"
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 11); // Brancos
                                arquivoSaida += "\r\n";
                                numeroRegistro += 1;

                                // REGISTRO DETALHE - SEGMENTO Q REMESSA
                                arquivoSaida += "033";                                                                      // Código do Banco na compensação
                                arquivoSaida += "0000";                                                                     // Lote de serviço
                                arquivoSaida += "3";                                                                        // Tipo de registro
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(numeroRegistro.ToString(), 5);        // Seqüência do Registro 
                                arquivoSaida += "Q";                                                                        // Cód. segmento do registro detalhe
                                arquivoSaida += " ";                                                                        // Reservado (uso Banco)
                                arquivoSaida += "01";                                                                       // Código de movimento remessa
                                arquivoSaida += _tipoInscricaoPagador;                                                                                          // Inscrição Pagador
                                arquivoSaida += _documentoPagador;
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["nome"].EmptyIfNull().ToString().Trim(), 40);                                // Nome Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["endereco_com"].EmptyIfNull().ToString().Trim(), 40);                        // Endereco Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["bairro_com"].EmptyIfNull().ToString().Trim(), 15);                          // Bairro Pagador
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(dsRow["cep_com"].EmptyIfNull().ToString().Trim(), 8);                             // CEP Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(nomeCidadePagador, 15);                                                     // Cidade Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(nomeUFPagador, 2);                                                          // UF Pagador
                                arquivoSaida += "0";                                                                                                            // Tipo de inscrição sacador/avalista
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(String.Empty, 15);                                                                  // Nº de inscrição sacador/avalista
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 40);                                                          // Nome do sacador/avalista
                                arquivoSaida += "000";                                                                                                          // Identificador de carne
                                arquivoSaida += "000";                                                                                                          // Seqüencial da Parcela ou número - inicial da parcela
                                arquivoSaida += "000";                                                                                                          // Quantidade total de parcelas
                                arquivoSaida += "000";                                                                                                          // Número do plano
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 19);                                                          // Reservado (uso Banco)
                                arquivoSaida += "\r\n";
                                numeroRegistro += 1;

                                // Atualização do título
                                g_financeiro record_g_financeiro = db.g_financeiro.Find(dsRow["id_financeiro"]);
                                record_g_financeiro.id_financeiro_remessa = record_g_financeiro_remessas.id_financeiro_remessa;
                                record_g_financeiro.id_financeiro_status = 11; // Cobrança Enviada
                                record_g_financeiro.datahora_alteracao = DataHoraAtual;
                                record_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(record_g_financeiro).State = EntityState.Modified;

                                // Criar o histórico do titulo para ratreabilidade
                                g_financeiro_historicos record_g_financeiro_historicos = new g_financeiro_historicos();
                                record_g_financeiro_historicos.id_financeiro = record_g_financeiro.id_financeiro;
                                record_g_financeiro_historicos.id_financeiro_origem = 7; //Remessa Bancária
                                record_g_financeiro_historicos.id_financeiro_status_inicial = 1; // Aberto                                
                                record_g_financeiro_historicos.id_financeiro_status_final = record_g_financeiro.id_financeiro_status; // Cob. Enviada
                                record_g_financeiro_historicos.id_financeiro_remessa = record_g_financeiro_remessas.id_financeiro_remessa;
                                //record_g_financeiro_historicos.id_conta_caixa = record_g_financeiro.id_conta_caixa_liquidacao.GetValueOrDefault();
                                record_g_financeiro_historicos.id_conta_caixa = view_record_g_contas_caixas.id_conta_caixa;
                                record_g_financeiro_historicos.id_pagamento_recebimento_tipo = record_g_financeiro.id_pagamento_recebimento_tipo.GetValueOrDefault();
                                record_g_financeiro_historicos.historico = "GERAÇÃO DE REM. BANC. (SANTANDER - 033)";
                                record_g_financeiro_historicos.id_coligada = 1;
                                record_g_financeiro_historicos.id_filial = 1;
                                record_g_financeiro_historicos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                record_g_financeiro_historicos.datahora_cadastro = datahora;
                                db.g_financeiro_historicos.Add(record_g_financeiro_historicos);
                            }

                            // TRAILER DE LOTE REMESSA
                            arquivoSaida += "033";                                                                      // Código do Banco na compensação
                            arquivoSaida += "0000";                                                                     // Lote de serviço
                            arquivoSaida += "5";                                                                        // tipo de registro
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa("0", 9);                               // Brancos
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa((numeroRegistro - 1).ToString(), 6);   // Quantidade de registros do lote
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 217);                     // Filler
                            arquivoSaida += "\r\n";

                            // TRAILER DE ARQUIVO REMESSA
                            arquivoSaida += "033";                                                                      // Código do Banco na compensação
                            arquivoSaida += "0000";                                                                     // Lote de serviço
                            arquivoSaida += "9";                                                                        // tipo de registro
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa("0", 9);                               // Brancos
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa("1", 6);                               // Quantidade de lotes do arquivo
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa((numeroRegistro - 1).ToString(), 6);    // Quantidade de registros do arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 211);                     // Filler
                            arquivoSaida += "\r\n";

                            // Salvar os dados
                            db.SaveChanges();
                        }
                        if (record_conta_caixa.banco == "341") // Itaú
                        {
                            nomeBanco = "Itaú";
                            layoutRemessa = "Layout: Banco 341 - Itaú";
                            arquivoSaida = String.Empty;

                            // REGISTRO HEADER DO ARQUIVO REMESSA
                            arquivoSaida += "0";                                                                                                        // IDENTIFICAÇÃO DO REGISTRO HEADER
                            arquivoSaida += "1";                                                                                                        // TIPO DE OPERAÇÃO - REMESSA
                            arquivoSaida += "REMESSA";                                                                                                  // IDENTIFICAÇÃO POR EXTENSO DO MOVIMENTO
                            arquivoSaida += "01";                                                                                                       // IDENTIFICAÇÃO DO TIPO DE SERVIÇO
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa("COBRANCA", 15);                                                        // IDENTIFICAÇÃO POR EXTENSO DO TIPO DE SERVIÇO
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), 4);        // AGÊNCIA MANTENEDORA DA CONTA
                            arquivoSaida += "00";                                                                                                       // COMPLEMENTO DE REGISTRO
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), 5);          // NÚMERO DA CONTA CORRENTE DA EMPRESA
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.dv_conta.EmptyIfNull().ToString().Trim(), 1);       // DÍGITO DE AUTO CONFERÊNCIA AG/CONTA EMPRESA
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 8);                                                       // COMPLEMENTO DO REGISTRO
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(record_conta_caixa.razao_social.EmptyIfNull().ToString().Trim(), 30);   // NOME POR EXTENSO DA "EMPRESA MÃE"
                            arquivoSaida += "341";                                                                                                      // Nº DO BANCO NA CÂMARA DE COMPENSAÇÃO
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa("BANCO ITAU SA", 15);                                                   // Nome do Banco
                            arquivoSaida += LibDateTime.getDataHoraBrasilia().ToString("ddMMyy");                                                       // Data de geração do arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 294);                                                     // COMPLEMENTO DO REGISTRO
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(numeroRegistro.ToString(), 6);                                         // Número sequencial do registro no arquivo
                            arquivoSaida += "\r\n";
                            numeroRegistro += 1;

                            // Registro
                            foreach (var dsRow in allFinanceiro)
                            {
                                String _tipoInscricaoPagador = String.Empty;
                                String _documentoPagador = String.Empty;
                                String _valorTitulo = (Decimal.Parse(dsRow["valor_total_bruto"].ToString()) * 100).ToString("0000000000000");
                                String _qtdDiasMora = "01";                                                                                     // QUANTIDADE DE DIAS - GESTOR PASSAR 01
                                String _nossoNumero = LibStringFormat.FormatarNumeroSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 7);
                                Decimal valorMultaFixo = 0;
                                DateTime _dataVencimento = LibDateTime.GetDateTimeDataRow(dsRow,"data_vencimento");

                                DateTime _dataMora = _dataVencimento.AddDays(Convert.ToInt32(_qtdDiasMora));                                    // DATA MULTA MORA SOMANDO A QUANTIDADE DE DIAS                                
                                _nossoNumero += GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularDigitoNossoNumero(record_conta_caixa.banco.EmptyIfNull().ToString().Trim(), record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), record_conta_caixa.carteira.EmptyIfNull().ToString().Trim(), _nossoNumero, record_conta_caixa.codigo_empresa.EmptyIfNull().ToString().Trim());

                                if (record_conta_caixa.multa_fixo.EmptyIfNull().ToString().Trim() != String.Empty)
                                { Decimal.TryParse(record_conta_caixa.multa_fixo.EmptyIfNull().ToString().Trim(), out valorMultaFixo); }
                                String _valorMultaFixo = (valorMultaFixo * 100).ToString("000000");

                                Decimal valorMultaDia = 0;
                                String _valorMultaDia = "0000000000000";
                                if (record_conta_caixa.multa_dia.EmptyIfNull().ToString().Trim() != String.Empty)
                                {
                                    Decimal.TryParse(record_conta_caixa.multa_dia.EmptyIfNull().ToString().Trim(), out valorMultaDia);
                                    valorMultaDia = ((Decimal.Parse(dsRow["valor_total_bruto"].ToString()) * valorMultaDia) / 100);
                                    valorMultaDia = (Math.Round(valorMultaDia, 2) * 100);
                                    _valorMultaDia = valorMultaDia.ToString("0000000000000");
                                }

                                if (dsRow["cpf"].EmptyIfNull().ToString().Trim() != String.Empty)
                                {
                                    _tipoInscricaoPagador = "01";
                                    _documentoPagador = LibStringFormat.FormatarNumeroSerasa(dsRow["cpf"].EmptyIfNull().ToString().Trim(), 14);
                                }
                                else
                                {
                                    _tipoInscricaoPagador = "02";
                                    _documentoPagador = LibStringFormat.FormatarNumeroSerasa(dsRow["cnpj"].EmptyIfNull().ToString().Trim(), 14);
                                };
                                String nomeCidadePagador = String.Empty;
                                if (dsRow["id_cidade_com"].EmptyIfNull().ToString().Trim() != string.Empty)
                                {
                                    nomeCidadePagador = allCidades.Find(c => c.id_cidade == int.Parse(dsRow["id_cidade_com"].EmptyIfNull().ToString().Trim())).nome.ToString();
                                }
                                String nomeUFPagador = String.Empty;
                                if (dsRow["id_uf_com"].EmptyIfNull().ToString().Trim() != string.Empty)
                                {
                                    nomeUFPagador = allUF.Find(c => c.id_uf == int.Parse(dsRow["id_uf_com"].EmptyIfNull().ToString().Trim())).sigla.ToString();
                                }

                                // REGISTRO DETALHE (OBRIGATÓRIO)
                                arquivoSaida += "1";                                                                                                    // IDENTIFICAÇÃO DO REGISTRO TRANSAÇÃO
                                arquivoSaida += "02";                                                                                                   // TIPO DE INSCRIÇÃO DA EMPRESA
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(_documentoPagador.EmptyIfNull().ToString().Trim(), 14);            // Nº DE INSCRIÇÃO DA EMPRESA (CPF/CNPJ)
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), 4);    // AGÊNCIA MANTENEDORA DA CONTA
                                arquivoSaida += "00";                                                                                                   // COMPLEMENTO DE REGISTRO
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), 5);      // NÚMERO DA CONTA CORRENTE DA EMPRESA
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.dv_conta.EmptyIfNull().ToString().Trim(), 1);   // DÍGITO DE AUTO CONFERÊNCIA AG/CONTA EMPRESA
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 4);                                                   // COMPLEMENTO DE REGISTRO
                                arquivoSaida += "0000";                                                                                                 // CÓD.INSTRUÇÃO/ALEGAÇÃO A SER CANCELADA
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 25);        // IDENTIFICAÇÃO DO TÍTULO NA EMPRESA
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 8);         // NOSSO NUMERO SEM DIGITO CALCULADO - GESTOR FRANQUIA NÃO ENVIA DIGITO
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 13);                                                 // QUANTIDADE DE MOEDA VARIÁVEL
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.carteira.EmptyIfNull().ToString().Trim(), 3);   // NÚMERO DA CARTEIRA NO BANCO
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 21);                                                  // IDENTIFICAÇÃO DA OPERAÇÃO NO BANCO
                                arquivoSaida += 'I';                                                                                                    // CÓDIGO DA CARTEIRA
                                if (dsRow["id_financeiro_status"].EmptyIfNull().ToString().Trim() == "1") { arquivoSaida += "01"; } // REMESSA
                                else if (dsRow["id_financeiro_status"].EmptyIfNull().ToString().Trim() == "3") { arquivoSaida += "02"; } // PEDIDO DE BAIXA
                                else { arquivoSaida += "00"; } // NÃO RECONHECIDO
                                arquivoSaida += "0000000000";                                                                                           // Nº DO DOCUMENTO DE COBRANÇA (DUPL.,NP ETC.)
                                //arquivoSaida += DateTime.Parse(dsRow["data_vencimento"].EmptyIfNull().ToString().Trim()).ToString("ddMMyy");            // Data Vencimento
                                //arquivoSaida += Convert.ToDateTime(dsRow["data_vencimento"].EmptyIfNull().ToString().Trim(), CultureInfo.InvariantCulture).ToString("ddMMyy");            // Data Vencimento
                                arquivoSaida += _valorTitulo;                                                                                           // Valor do Titulo 
                                arquivoSaida += "341";                                                                                                  // Código do Banco na compensação
                                arquivoSaida += "00000";                                                                                                // AGÊNCIA ONDE O TÍTULO SERÁ COBRADO
                                arquivoSaida += "06";                                                                                                   // ESPÉCIE DO TÍTULO - DUPLICATA DE SERVIÇO
                                arquivoSaida += "N";                                                                                                    // Não Aceito
                                //arquivoSaida += DateTime.Parse(dsRow["data_processamento"].EmptyIfNull().ToString().Trim()).ToString("ddMMyy");         // Data da Emissão do Título
                                //arquivoSaida += Convert.ToDateTime(dsRow["data_processamento"].EmptyIfNull().ToString().Trim(), CultureInfo.InvariantCulture).ToString("ddMMyy");         // Data da Emissão do Título
                                arquivoSaida += "05";                                                                                                   // 1ª INSTRUÇÃO DE COBRANÇA
                                arquivoSaida += "57";                                                                                                   // 2ª INSTRUÇÃO DE COBRANÇA
                                if (valorMultaDia > 0) { arquivoSaida += _valorMultaDia; }
                                else { arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 13); }                                        // Data dos Juros de Mora 
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 6);                                                  // DATA LIMITE PARA CONCESSÃO DE DESCONTO
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 13);                                                 // VALOR DO DESCONTO A SER CONCEDIDO
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 13);                                                 // VALOR DO I.O.F. RECOLHIDO P/ NOTAS SEGURO
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 13);                                                 // VALOR DO ABATIMENTO A SER CONCEDIDO
                                arquivoSaida += _tipoInscricaoPagador;                                                                                  // Tipo Inscrição do Pagador
                                arquivoSaida += _documentoPagador;                                                                                      // Documento do Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["nome"].EmptyIfNull().ToString().Trim(), 30);                 // Nome Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 10);                                                  // COMPLEMENTO DE REGISTRO
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["endereco_com"].EmptyIfNull().ToString().Trim(), 40);         // Endereco Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["bairro_com"].EmptyIfNull().ToString().Trim(), 12);           // Bairro Pagador
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(dsRow["cep_com"].EmptyIfNull().ToString().Trim(), 8);              // CEP Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(nomeCidadePagador, 15);                                             // Cidade Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(nomeUFPagador, 2);                                                  // UF Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 30);                                                  // NOME DO SACADOR OU AVALISTA
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 4);                                                   // COMPLEMENTO DE REGISTRO                                
                                arquivoSaida += _dataMora.ToString("ddMMyy");                                                                           // Data de Mora
                                arquivoSaida += _qtdDiasMora;                                                                                           // QUANTIDADE DE DIAS - GESTOR PASSAR 01
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1);                                                   // COMPLEMENTO DO REGISTRO
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(numeroRegistro.ToString(), 6);                                     // Número sequencial do registro no arquivo
                                arquivoSaida += "\r\n";
                                numeroRegistro += 1;

                                // Atualização do título
                                g_financeiro record_g_financeiro = db.g_financeiro.Find(dsRow["id_financeiro"]);
                                int idFinanceiroStatusInicial = record_g_financeiro.id_financeiro_status;
                                record_g_financeiro.id_financeiro_remessa = record_g_financeiro_remessas.id_financeiro_remessa;
                                if (record_g_financeiro.id_financeiro_status == 1) { record_g_financeiro.id_financeiro_status = 11; } // Cob. Enviada
                                else if (record_g_financeiro.id_financeiro_status == 3) { record_g_financeiro.id_financeiro_status = 13; } // Canc. Enviado
                                record_g_financeiro.cnab_nosso_numero = _nossoNumero;
                                record_g_financeiro.datahora_alteracao = DataHoraAtual;
                                record_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(record_g_financeiro).State = EntityState.Modified;

                                // Criar o histórico do titulo para ratreabilidade
                                g_financeiro_historicos record_g_financeiro_historicos = new g_financeiro_historicos();
                                record_g_financeiro_historicos.id_financeiro = record_g_financeiro.id_financeiro;
                                record_g_financeiro_historicos.id_financeiro_origem = 7; // Remessa Bancária
                                record_g_financeiro_historicos.id_financeiro_status_inicial = idFinanceiroStatusInicial;
                                record_g_financeiro_historicos.id_financeiro_status_final = record_g_financeiro.id_financeiro_status;
                                record_g_financeiro_historicos.id_financeiro_remessa = record_g_financeiro_remessas.id_financeiro_remessa;
                                record_g_financeiro_historicos.id_conta_caixa = view_record_g_contas_caixas.id_conta_caixa;
                                record_g_financeiro_historicos.id_pagamento_recebimento_tipo = record_g_financeiro.id_pagamento_recebimento_tipo.GetValueOrDefault();
                                record_g_financeiro_historicos.historico = "GERAÇÃO DE REM. BANC. (ITAÚ - 341)";
                                record_g_financeiro_historicos.id_coligada = 1;
                                record_g_financeiro_historicos.id_filial = 1;
                                record_g_financeiro_historicos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                record_g_financeiro_historicos.datahora_cadastro = datahora;
                                db.g_financeiro_historicos.Add(record_g_financeiro_historicos);
                            }

                            // TRAILER DE ARQUIVO REMESSA
                            arquivoSaida += "9";                                                                        // IDENTIFICAÇÃO DO REGISTRO TRAILER
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 393);                     // COMPLEMENTO DO REGISTRO
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(numeroRegistro.ToString(), 6);         // Número sequencial do registro no arquivo
                            arquivoSaida += "\r\n";
                            numeroRegistro += 1;

                            // Salvar os dados
                            db.SaveChanges();
                        }
                        else if (record_conta_caixa.banco == "756") // Layout Sicoob
                        {
                            nomeBanco = "Sicoob";
                            layoutRemessa = "Layout: Banco 756 - Sicoob";
                            arquivoSaida = String.Empty;

                            // HEADER DO ARQUIVO
                            arquivoSaida += "756"; // Código do Sicoob na Compensação: "756"
                            arquivoSaida += "0000"; // Lote de Serviço: "0000"
                            arquivoSaida += "0"; // Tipo de Registro: "0"
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 9); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                            arquivoSaida += "2"; // "Tipo de Inscrição da Empresa: '1' = CPF '2' = CGC / CNPJ"
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.cnpj.EmptyIfNull().ToString().Trim().Replace("-", "").Replace("/", "").Replace(".", ""), 14);  // Número de Inscrição da Empresa
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 20); // Código do Convênio no Sicoob: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), 5);                        // Prefixo da Cooperativa: vide planilha "Contracapa" deste arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Dígito Verificador do Prefixo: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), 12);  // Conta Corrente: vide planilha "Contracapa" deste arquivo
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.dv_conta.EmptyIfNull().ToString().Trim(), 1);
                            arquivoSaida += "0"; // Dígito Verificador da Ag/Conta: Preencher com zeros
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(record_conta_caixa.razao_social.EmptyIfNull().ToString().Trim(), 30);    // Nome da Empresa
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa("SICOOB", 30);    // Nome do Banco: SICOOB
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 10);    // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                            arquivoSaida += "1"; // Código Remessa / Retorno: "1"
                            arquivoSaida += LibDateTime.getDataHoraBrasilia().ToString("ddMMyyyy");                   // Data da geração do arquivo
                            arquivoSaida += LibDateTime.getDataHoraBrasilia().ToString("hhmmss");                   // Hora da geração do arquivo
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_g_financeiro_remessas.remessa_numero.ToString("D6"), 6); // Seqüência do Registro 
                            arquivoSaida += "081"; // No da Versão do Layout do Arquivo: "081"
                            arquivoSaida += "00000"; // Densidade de Gravação do Arquivo: "00000"
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 20);    // Para Uso Reservado do Banco: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 20);    // Para Uso Reservado da Empresa: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 29);    // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                            arquivoSaida += "\r\n";

                            // HEADER DO LOTE
                            arquivoSaida += "756"; // Código do Sicoob na Compensação: "756"
                            arquivoSaida += "0001"; // Número do Lote
                            arquivoSaida += "1"; // Tipo de Registro: "1"
                            arquivoSaida += "R"; // Tipo de Operação R
                            arquivoSaida += "01"; // Tipo de Serviço: 01
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 2); // Uso Exclusivo FEBRABAN/CNAB: Preencher com espaços em branco
                            arquivoSaida += "040"; // Nº da Versão do Layout do Lote: "040"
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Uso Exclusivo FEBRABAN/CNAB: Preencher com espaços em branco
                            arquivoSaida += "2"; // "Tipo de Inscrição da Empresa: '1' = CPF '2' = CGC / CNPJ"
                            arquivoSaida += " "; // "Espaço Layout"
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.cnpj.EmptyIfNull().ToString().Trim(), 14);  // Número de Inscrição da Empresa
                                                                                                                                                //arquivoSaida += LibStringFormat.FormatarAlphaSerasa(record_conta_caixa.codigo_convenio.EmptyIfNull().ToString().Trim(), 20);  // Código do Convênio no Banco: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 20);  // Código do Convênio no Banco: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), 5);                        // Prefixo da Cooperativa: vide planilha "Contracapa" deste arquivo
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Dígito Verificador do Prefixo: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), 12);  // Conta Corrente: vide planilha "Contracapa" deste arquivo
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.dv_conta.EmptyIfNull().ToString().Trim(), 1);
                            arquivoSaida += " "; // Dígito Verificador da Ag/Conta: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(record_conta_caixa.razao_social.EmptyIfNull().ToString().Trim(), 30);    // Nome da Empresa
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 40); // Mensagem 1. Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 40); // Mensagem 2. Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_g_financeiro_remessas.remessa_numero.EmptyIfNull().ToString().Trim(), 8); // Número Remessa/Retorno
                            arquivoSaida += LibDateTime.getDataHoraBrasilia().ToString("ddMMyyyy");                   // Data da geração do arquivo
                            arquivoSaida += "00000000";                   // Data do Crédito
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 33); // Uso Exclusivo FEBRABAN/CNAB: Preencher com espaços em branco
                            arquivoSaida += "\r\n";

                            // Registro
                            foreach (var dsRow in allFinanceiro)
                            {
                                String _tipoInscricaoPagador = String.Empty;
                                String _documentoPagador = String.Empty;
                                String _valorTitulo = (Decimal.Parse(dsRow["valor_total_bruto"].EmptyIfNull().ToString().Trim()) * 100).ToString("000000000000000");

                                // Cálculos Nosso Número
                                String _nossoNumeroTemp = string.Empty;
                                _nossoNumeroTemp += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), 4);
                                _nossoNumeroTemp += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), 10);
                                _nossoNumeroTemp += LibStringFormat.FormatarNumeroSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 7);
                                String _nossoNumero = dsRow["id_financeiro"].EmptyIfNull().EmptyIfNull().ToString().Trim() + GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularDigitoNossoNumero756(_nossoNumeroTemp);
                                //_nossoNumero = LibStringFormat.formatarAlphaSicoob(_nossoNumero, 10);
                                _nossoNumero = LibStringFormat.FormatarNumeroSerasa(_nossoNumero, 10);

                                Decimal valorMultaFixo = 0;
                                if (record_conta_caixa.multa_fixo.EmptyIfNull().ToString().Trim() != String.Empty)
                                {
                                    Decimal.TryParse(record_conta_caixa.multa_fixo.EmptyIfNull().ToString().Trim(), out valorMultaFixo);
                                }
                                String _valorMultaFixo = (valorMultaFixo * 100).ToString("000000");

                                Decimal valorMultaDia = 0;
                                if (record_conta_caixa.multa_dia.EmptyIfNull().ToString().Trim() != String.Empty)
                                {
                                    Decimal.TryParse(record_conta_caixa.multa_dia.EmptyIfNull().ToString().Trim(), out valorMultaDia);
                                }
                                String _valorMultaDia = (valorMultaDia * 100).ToString("000000000000000");

                                if (dsRow["cpf"].EmptyIfNull().ToString().Trim() != String.Empty)
                                {
                                    _tipoInscricaoPagador = "1";
                                    _documentoPagador = LibStringFormat.FormatarNumeroSerasa(dsRow["cpf"].EmptyIfNull().ToString().Trim(), 15);
                                }
                                else
                                {
                                    _tipoInscricaoPagador = "2";
                                    _documentoPagador = LibStringFormat.FormatarNumeroSerasa(dsRow["cnpj"].EmptyIfNull().ToString().Trim(), 15);
                                };
                                String nomeCidadePagador = String.Empty;
                                if (dsRow["id_cidade_com"].EmptyIfNull().ToString().Trim() != string.Empty)
                                {
                                    nomeCidadePagador = allCidades.Find(c => c.id_cidade == int.Parse(dsRow["id_cidade_com"].EmptyIfNull().ToString().Trim())).nome.ToString();
                                }
                                String nomeUFPagador = String.Empty;
                                if (dsRow["id_uf_com"].EmptyIfNull().ToString().Trim() != string.Empty)
                                {
                                    nomeUFPagador = allUF.Find(c => c.id_uf == int.Parse(dsRow["id_uf_com"].EmptyIfNull().ToString().Trim())).sigla.ToString();
                                }

                                // REGISTRO DETALHE SEGMENTO P
                                arquivoSaida += "756"; // Código do Sicoob na Compensação: "756"
                                arquivoSaida += "0001"; // Número do Lote
                                arquivoSaida += "3"; // Tipo de Registro
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(numeroRegistro.ToString(), 5); // Número sequencial do registro no lote
                                arquivoSaida += "P"; // Cód. Segmento do Registro Detalhe: "P"
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                                arquivoSaida += "01"; // Entrada de Títulos
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.agencia.EmptyIfNull().ToString().Trim(), 5);                        // Prefixo da Cooperativa: vide planilha "Contracapa" deste arquivo
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Dígito Verificador do Prefixo: Preencher com espaços em branco
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.conta.EmptyIfNull().ToString().Trim(), 12);  // Conta Corrente: vide planilha "Contracapa" deste arquivo
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.dv_conta.EmptyIfNull().ToString().Trim(), 1);
                                arquivoSaida += " "; // Dígito Verificador da Ag/Conta: Preencher com espaços em branco
                                arquivoSaida += _nossoNumero;
                                arquivoSaida += "01"; // Parcela
                                arquivoSaida += "01"; // Modalidade
                                arquivoSaida += "4"; // Tipo Formulário - A4 sem envelopamento
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 5); // Em Branco
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(record_conta_caixa.carteira.EmptyIfNull().ToString().Trim(), 1);    // Nome da Empresa
                                arquivoSaida += "0"; // Forma de Cadastr. do Título no Banco: "0"
                                arquivoSaida += " "; // Tipo de Documento: Preencher com espaços em branco
                                arquivoSaida += "2"; // "Identificação da Emissão do Boleto: '2' = Beneficiário Emite"
                                arquivoSaida += "2"; // "Identificação da Distribuição do Boleto: '2' = Beneficiário Emite"
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 15);
                                arquivoSaida += DateTime.Parse(dsRow["data_vencimento"].EmptyIfNull().ToString().Trim()).ToString("ddMMyyyy");                           // Data Vencimento
                                arquivoSaida += _valorTitulo; // Valor do Titulo 
                                arquivoSaida += "00000"; // Agência Encarregada da Cobrança: "00000"
                                arquivoSaida += " "; // Dígito Verificador da Agência: Preencher com espaços em branco
                                arquivoSaida += "04"; // Duplicata de Serviços
                                arquivoSaida += "N"; // Não Aceito
                                arquivoSaida += DateTime.Parse(dsRow["data_processamento"].EmptyIfNull().ToString().Trim()).ToString("ddMMyyyy"); // Data da Emissão do Título
                                if (valorMultaDia > 0)
                                {
                                    arquivoSaida += "2"; // Juros - Taxa Mensal
                                    arquivoSaida += DateTime.Parse(dsRow["data_vencimento"].EmptyIfNull().ToString().Trim()).ToString("ddMMyyyy"); // Data dos Juros de Mora 
                                }
                                else
                                {
                                    arquivoSaida += "0";    // Juros - Isento
                                    arquivoSaida += LibStringFormat.FormatarNumeroSerasa("", 8); // Data dos Juros de Mora    
                                }

                                arquivoSaida += _valorMultaDia; // Taxa de Juros Mensal
                                arquivoSaida += "0";// Código do Desconto - Não Conceder Desconto 
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 8); // Data do Desconto
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 15); // Valor percentual do desconto
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 15); // Valor IOF
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 15); // Valor Abatimento
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim(), 25);
                                arquivoSaida += "3";// Protesto - Não Protestar
                                arquivoSaida += "00";// Prazo Protesto
                                arquivoSaida += "0";// Código para Baixa/Devolução: "0"
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 3); // Número de Dias para Baixa / Devolução: Preencher com espaços em branco
                                arquivoSaida += "09"; // "Código da Moeda: '09' = Real"
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 10); // Nº do Contrato da Operação de Crédito. 
                                arquivoSaida += " "; // Uso Exclusivo FEBRABAN/CNAB: Preencher com espaços em branco
                                arquivoSaida += "\r\n";
                                numeroRegistro += 1;

                                // REGISTRO DETALHE SEGMENTO Q
                                arquivoSaida += "756"; // Código do Sicoob na Compensação: "756"
                                arquivoSaida += "0001"; // Número do Lote
                                arquivoSaida += "3"; // Tipo de Registro
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(numeroRegistro.ToString(), 5); // Número sequencial do registro no lote
                                arquivoSaida += "Q"; // Cód. Segmento do Registro Detalhe: "Q"
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(String.Empty, 1); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                                arquivoSaida += "01"; // Entrada de Títulos
                                arquivoSaida += _tipoInscricaoPagador;                                                                                          // Inscrição Pagador
                                arquivoSaida += _documentoPagador; // Documento Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["nome"].EmptyIfNull().ToString().Trim(), 40);                                // Nome Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["endereco_com"].EmptyIfNull().ToString().Trim(), 40);                         // Endereco Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(dsRow["bairro_com"].EmptyIfNull().ToString().Trim(), 15);                           // Bairro Pagador
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(dsRow["cep_com"].EmptyIfNull().ToString().Trim(), 8);                             // CEP Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(nomeCidadePagador, 15);                                                     // Cidade Pagador
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(nomeUFPagador, 2);                                                          // UF Pagador
                                arquivoSaida += "0";                                                    // Tipo de Inscrição Sacador Avalista:                                                                                         // Inscrição Pagador
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 15); // Número de Inscrição Sacador Avalista
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(string.Empty, 40);  // Nome Avalista
                                arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 3);  // Cód. Bco. Corresp. na Compensação	
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(string.Empty, 20);  // Nosso Nº no Banco Correspondente
                                arquivoSaida += LibStringFormat.FormatarAlphaSerasa(string.Empty, 8);   // Uso Exclusivo FEBRABAN/CNAB
                                arquivoSaida += "\r\n";
                                numeroRegistro += 1;

                                // Atualização do título
                                g_financeiro record_g_financeiro = db.g_financeiro.Find(dsRow["id_financeiro"]);
                                int idFinanceiroStatusInicial = record_g_financeiro.id_financeiro_status;
                                record_g_financeiro.id_financeiro_remessa = record_g_financeiro_remessas.id_financeiro_remessa;
                                if (record_g_financeiro.id_financeiro_status == 1) { record_g_financeiro.id_financeiro_status = 11; } // Cob. Enviada
                                else if (record_g_financeiro.id_financeiro_status == 3) { record_g_financeiro.id_financeiro_status = 13; } // Canc. Enviado
                                record_g_financeiro.cnab_nosso_numero = _nossoNumero;
                                record_g_financeiro.datahora_alteracao = DataHoraAtual;
                                record_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(record_g_financeiro).State = EntityState.Modified;

                                // Criar o histórico do titulo para ratreabilidade
                                g_financeiro_historicos record_g_financeiro_historicos = new g_financeiro_historicos();
                                record_g_financeiro_historicos.id_financeiro = record_g_financeiro.id_financeiro;
                                record_g_financeiro_historicos.id_financeiro_origem = 7; // Remessa Bancária
                                record_g_financeiro_historicos.id_financeiro_status_inicial = idFinanceiroStatusInicial;
                                record_g_financeiro_historicos.id_financeiro_status_final = record_g_financeiro.id_financeiro_status; // Cob. Enviada                                
                                record_g_financeiro_historicos.id_financeiro_remessa = record_g_financeiro_remessas.id_financeiro_remessa;
                                record_g_financeiro_historicos.id_conta_caixa = view_record_g_contas_caixas.id_conta_caixa;
                                record_g_financeiro_historicos.id_pagamento_recebimento_tipo = record_g_financeiro.id_pagamento_recebimento_tipo.GetValueOrDefault();
                                record_g_financeiro_historicos.historico = "GERAÇÃO DE REM. BANC. (SICOOB - 756)";
                                record_g_financeiro_historicos.id_coligada = 1;
                                record_g_financeiro_historicos.id_filial = 1;
                                record_g_financeiro_historicos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                record_g_financeiro_historicos.datahora_cadastro = datahora;
                                db.g_financeiro_historicos.Add(record_g_financeiro_historicos);
                            }

                            // REGISTRO TRAILLER DO LOTE		
                            arquivoSaida += "756"; // Código do Sicoob na Compensação: "756"
                            arquivoSaida += "0001"; // Número do Lote
                            arquivoSaida += "5"; // Tipo de Registro
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(string.Empty, 9);                                // Uso Exclusivo FEBRABAN/CNAB: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa((numeroRegistro - 1).ToString(), 6); // Quantidade de Registros no Lote
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa((numeroRegistro - 1).ToString(), 6); // Quantidade de Títulos em Cobrança
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(String.Empty, 17); // Valor Total dosTítulos em Carteiras
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 6); // Quantidade de Títulos em Cobrança - Vinculadas
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 17); // Valor Total dosTítulos em Carteiras
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 6); // Quantidade de Títulos em Cobrança - Caucionadas
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 17); // Valor Total dosTítulos em Carteiras
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 6); // Quantidade de Títulos em Cobrança - Descontadas
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 17); // Valor Total dosTítulos em Carteiras
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(string.Empty, 8);                                // Número do Aviso de Lançamento: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(string.Empty, 117); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                            arquivoSaida += "\r\n";

                            // REGISTRO TRAILLER DO ARQUIVO										
                            arquivoSaida += "756"; // Código do Sicoob na Compensação: "756"
                            arquivoSaida += "9999"; // Número do Lote
                            arquivoSaida += "9"; // Tipo de Registro
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(string.Empty, 9);                                // Uso Exclusivo FEBRABAN/CNAB: Preencher com espaços em branco
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa("1", 6); // Quantidade de Lotes do Arquivo
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa((numeroRegistro - 1).ToString(), 6); // Quantidade de Registros no Arquivo
                            arquivoSaida += LibStringFormat.FormatarNumeroSerasa(string.Empty, 6); // Qtde de Contas p/ Conc. (Lotes): "000000"
                            arquivoSaida += LibStringFormat.FormatarAlphaSerasa(string.Empty, 205); // Uso Exclusivo FEBRABAN / CNAB: Preencher com espaços em branco
                            arquivoSaida += "\r\n";

                            // Salvar os dados
                            db.SaveChanges();
                        }

                        // Nome do arquivo Itau-341 costumizado
                        if (record_conta_caixa.banco == "341")
                        {

                            fileNameExportacao = datahora.ToString("dd_MM_yy") + "_" + nomeBanco + "_" + record_g_financeiro_remessas.remessa_numero.EmptyIfNull().ToString().Trim() + ".txt";
                        }
                        else
                        {
                            fileNameExportacao = "CNAB_Boleto_" + nomeBanco + "_" + record_g_financeiro_remessas.remessa_numero.EmptyIfNull().ToString().Trim() + "-" + datahora.ToString("dd_MM_yy") + ".rem";
                        }

                        // Salvar o arquivo em disco
                        String DirTempFiles = Server.MapPath("~/_filestemp");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "reports");
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                        if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                        fileNameDestino = Path.Combine(DirTempFiles, fileNameExportacao);

                        using (StreamWriter w = new StreamWriter(fileNameDestino, true, Encoding.ASCII))
                        {
                            w.Write(arquivoSaida); // Write the text)
                        }

                        // Atualizar o registro do processamento
                        g_processamento record_g_processamento = new g_processamento();
                        record_g_processamento.id_processamento_tipo = 10; // Exportação Cnab Boletos
                        record_g_processamento.id_processamento_modulo = 1; // Modulo Relatorio
                        record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                        record_g_processamento.datahora_inicio = datahora;
                        record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                        record_g_processamento.qtd_registros = qtdRegistros;
                        record_g_processamento.qtd_reg_ok = qtdRegistros;
                        record_g_processamento.qtd_reg_erro = 0;
                        record_g_processamento.processando = false;
                        record_g_processamento.concluido = true;
                        record_g_processamento.pathfile = fileNameDestino;
                        record_g_processamento.id_coligada = 1;
                        record_g_processamento.id_filial = 1;
                        db.g_processamento.Add(record_g_processamento);
                        db.SaveChanges();

                        // Atualizar o processamento na remessa
                        record_g_financeiro_remessas.id_processamento = record_g_processamento.id_processamento;
                        record_g_financeiro_remessas.datahora_alteracao = DataHoraAtual;
                        record_g_financeiro_remessas.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_g_financeiro_remessas).State = EntityState.Modified;
                        db.SaveChanges();

                        sucesso = true;
                        idProcessamentoGravado = record_g_processamento.id_processamento.EmptyIfNull().ToString().Trim();

                        msgRetorno += "Remessa Bancária CNAB <b>Gerada</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>"
                                    + layoutRemessa + "<br/>"
                                    + "Qtd. Títulos: " + qtdRegistros.EmptyIfNull().ToString().Trim() + "<br/>"
                                    + "R$ Processado: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotal);
                    }
                    else
                    {
                        if (sucesso) // Processou corretamente
                        {
                            sucesso = false;
                            msgRetorno = "Não foram localizados Títulos com status \"Em aberto\" ou \"Em Cancelamento\" para o filtro informado!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-down", "", "cc0000", "");
                        }
                    }
                }
                else
                {
                    sucesso = false;
                    msgRetorno = "Realize a pesquisa antes de executar o processo!";
                }
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacaoIdProcessamento(ex, idProcessamentoGravado);
            }
            catch (Exception e)
            {
                return JsonAjaxErroIdProcessamento(e, idProcessamentoGravado);
            }

            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Boletos Bancários - Envio por Email
        [HttpPost]
        [GdiValidateAntiForgeryToken]
        public ActionResult ajaxEnviarBoletosEmail()
        {
            bool sucesso = true;
            int qtdBoletosEnviados = 0;
            int qtdBoletosErros = 0;
            String msgRetorno = String.Empty;
            String DirTempFiles = String.Empty;
            String SentencaSQL = String.Empty;

            try
            {
                String SentencaSQLTemp = string.Empty;

                SentencaSQL = "select f.* " +
                                "from g_financeiro f " +
                                "where f.id_financeiro is not null ";

                if (SentencaSQLTemp.Equals(String.Empty))
                {
                    sucesso = false;
                    msgRetorno = "Execute um filtro antes da enviar os boletos por e-mail";
                }
                else
                {
                    SentencaSQLTemp = SentencaSQLTemp.Substring(SentencaSQLTemp.IndexOf("where"));
                    SentencaSQLTemp = SentencaSQLTemp.Replace("where", "and");
                    SentencaSQL = SentencaSQL + " " + SentencaSQLTemp;
                }

                DataTable tableConsolidado = null;
                List<DataRow> allFinanceiro = null;

                if (sucesso == true)
                {
                    try
                    {
                        tableConsolidado = LibDB.GetDataTable(SentencaSQL, db);
                        allFinanceiro = tableConsolidado.AsEnumerable().ToList();
                    }
                    catch (Exception e)
                    {
                        sucesso = false;
                        msgRetorno = e.Message;
                    };
                }

                if ((sucesso == true) && (allFinanceiro != null) && (allFinanceiro.Count() > 0))
                {
                    foreach (var dsRow in allFinanceiro)
                    {
                        if (gerarBoletoEmail(int.Parse(dsRow["id_financeiro"].EmptyIfNull().ToString().Trim())))
                        {
                            qtdBoletosEnviados += 1;
                        }
                        else
                        {
                            qtdBoletosErros += 1;
                        }
                    }

                    msgRetorno = qtdBoletosEnviados.ToString() + " Boletos <b>Enviados</b> por email com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
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
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public bool gerarBoletoEmail(int id_financeiro)
        {
            bool sucesso = false;

            try
            {
                var fileImgBarcode = string.Empty;
                var allRecordsCidades = db.g_cidades.Select(g => new { g.id_cidade, g.nome }).ToList();
                var allRecordsUF = db.g_uf.Select(g => new { g.id_uf, g.sigla }).ToList();
                var allRecords = (from _f in db.g_financeiro
                                  join _c in db.g_clientes on _f.id_cliente equals _c.id_cliente
                                  join _cc in db.g_contas_caixas on _f.id_conta_caixa_geracao equals _cc.id_conta_caixa
                                  where _f.id_financeiro == id_financeiro
                                  select new { tableFinanceiro = _f, tableClientes = _c, tableContasCaixas = _cc }).ToList();

                CstFinanceiroBoletos record_cstFinanceiroBoletos = new CstFinanceiroBoletos();

                if (allRecords.Count > 0)
                {
                    var item = allRecords.First();

                    if (!item.tableClientes.email_principal.ToString().Trim().Equals(string.Empty))
                    {
                        g_filiais record_g_filiais = db.g_filiais.Where(f => f.id_filial == 1).FirstOrDefault();
                        record_cstFinanceiroBoletos.ELogoBanco = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/banco" + item.tableContasCaixas.banco.EmptyIfNull().ToString() + ".jpg";
                        record_cstFinanceiroBoletos.ECodBanco = item.tableContasCaixas.banco.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.ELocalPagamento = item.tableContasCaixas.local_pagamento.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EDataVencimento = item.tableFinanceiro.data_vencimento.ToString("dd/MM/yy");
                        record_cstFinanceiroBoletos.ECedenteNome = item.tableContasCaixas.razao_social.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EAgenciaCodCedente = GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularAgenciaCodigoCedente(item.tableContasCaixas.banco.EmptyIfNull().ToString(), item.tableContasCaixas.agencia.EmptyIfNull().ToString(), item.tableContasCaixas.dv_agencia.EmptyIfNull().ToString(), item.tableContasCaixas.conta.EmptyIfNull().ToString(), item.tableContasCaixas.dv_conta.EmptyIfNull().ToString(), item.tableContasCaixas.codigo_convenio.EmptyIfNull().ToString());
                        record_cstFinanceiroBoletos.EDataDocumento = item.tableFinanceiro.data_processamento.ToString("dd/MM/yy");
                        record_cstFinanceiroBoletos.ENumeroDocumento = item.tableFinanceiro.numero_documento.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EEspecieDoc = "NS";
                        record_cstFinanceiroBoletos.EAceite = "N";
                        record_cstFinanceiroBoletos.EDataProcessamento = item.tableFinanceiro.data_processamento.ToString("dd/MM/yy");
                        record_cstFinanceiroBoletos.ENossoNumeroDV = GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularNossoNumeroComDV(item.tableContasCaixas.banco.EmptyIfNull().ToString(), item.tableContasCaixas.agencia.EmptyIfNull().ToString(), item.tableContasCaixas.conta.EmptyIfNull().ToString(), item.tableContasCaixas.carteira.EmptyIfNull().ToString(), item.tableFinanceiro.id_financeiro, item.tableContasCaixas.inicial_nossonumero.EmptyIfNull().ToString(), item.tableContasCaixas.codigo_empresa.EmptyIfNull().ToString());
                        record_cstFinanceiroBoletos.ECarteira = item.tableContasCaixas.carteira.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EEspecieMoeda = item.tableContasCaixas.especie_moeda.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EValorTotal = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", item.tableFinanceiro.valor_total_liquido);
                        record_cstFinanceiroBoletos.EMensagemCaixa = item.tableContasCaixas.mensagem_caixa.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.ENomeSacado = item.tableClientes.nome.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.EEnderecoSacado = item.tableClientes.endereco_com.EmptyIfNull().ToString() + " " + item.tableClientes.bairro_com.EmptyIfNull().ToString();
                        record_cstFinanceiroBoletos.ECidadeSacado = allRecordsCidades.Find(c1 => c1.id_cidade == item.tableClientes.id_cidade_com).nome.ToString();
                        record_cstFinanceiroBoletos.ECepSacado = GdiPlataform.Lib.LibStringFormat.FormatarCEP(item.tableClientes.cep_com.EmptyIfNull().ToString());
                        record_cstFinanceiroBoletos.EUFSacado = allRecordsUF.Find(u1 => u1.id_uf == item.tableClientes.id_uf_com).sigla.ToString();
                        if (item.tableClientes.cpf.EmptyIfNull().ToString().Trim() != String.Empty)
                        { record_cstFinanceiroBoletos.EDocSacado = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("F", item.tableClientes.cpf.EmptyIfNull().ToString().Trim()); }
                        else { record_cstFinanceiroBoletos.EDocSacado = GdiPlataform.Lib.LibStringFormat.FormatarCPFCNPJ("J", item.tableClientes.cnpj.EmptyIfNull().ToString().Trim()); }
                        record_cstFinanceiroBoletos.ECodigoBarras = GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularCodigoBarras(item.tableContasCaixas.banco.EmptyIfNull().ToString(), item.tableContasCaixas.agencia.EmptyIfNull().ToString(), item.tableContasCaixas.conta.EmptyIfNull().ToString(), item.tableContasCaixas.carteira.EmptyIfNull().ToString(), item.tableContasCaixas.codigo_empresa.EmptyIfNull().ToString(), item.tableFinanceiro.id_financeiro, item.tableContasCaixas.inicial_nossonumero.EmptyIfNull().ToString(), item.tableFinanceiro.data_vencimento, item.tableFinanceiro.valor_total_bruto);
                        record_cstFinanceiroBoletos.ELinhaDigitavel = GdiPlataform.Areas.g.Lib.LibFinanceiroBoletos.CalcularLinhaDigitavel(item.tableContasCaixas.banco.EmptyIfNull().ToString(), record_cstFinanceiroBoletos.ECodigoBarras);
                        record_cstFinanceiroBoletos.EImgBarCode = Path.Combine(this.Request.Headers["Host"].ToLower(), "_filestemp/_barcode", CachePersister.userIdentity.SubDominio.ToString(), "barcode", item.tableFinanceiro.data_vencimento.ToString("yyyy"), item.tableFinanceiro.data_vencimento.ToString("MM"), record_cstFinanceiroBoletos.ECodigoBarras + ".png").Replace((char)92, (char)47);
                        LibBoletos.Generate_barcode(record_cstFinanceiroBoletos.ECodigoBarras, item.tableFinanceiro.data_vencimento, Server.MapPath("~/_filestemp"));
                        String boletoHtmlFormatado = RenderPartialViewToString("GerarFinanceiroBoletoEmail", record_cstFinanceiroBoletos);
                        sucesso = true;
                    }
                }
                return sucesso;
            }
            catch (Exception e)
            {
                throw (e);
            }
        }
        #endregion
        
        #region ModalCancelarTitulos
        public ActionResult ModalCancelarTitulos(String idFinanceiro)
        {
            ViewBag.Title = "Cancelar Título Financeiro";
            int id = 0;
            int.TryParse(idFinanceiro, out id);
            g_financeiro record_g_financeiro = db.g_financeiro.Find(id);
            if (record_g_financeiro == null)
            {
                record_g_financeiro = new g_financeiro { id_financeiro = id };
            }
            else
            {
                record_g_financeiro.motivo_cancelamento = "";
            }
            return View(record_g_financeiro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxCancelarTitulos(g_financeiro modal_g_financeiro)
        {
            return null;
        }
        #endregion

        #region ModalBaixarTitulos
        public ActionResult ModalBaixarTitulos(String idFinanceiro)
        {
            ViewBag.Title = "Baixar Títulos";
            int id = 0;
            int.TryParse(idFinanceiro, out id);
            g_financeiro record_g_financeiro = db.g_financeiro.Find(id);
            if (record_g_financeiro == null)
            {
                record_g_financeiro = new g_financeiro { id_financeiro = id };
            }
            else
            {
                record_g_financeiro.motivo_cancelamento = "";
            }
            return View(record_g_financeiro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxBaixarTitulos(g_financeiro modal_g_financeiro)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                int qtdLancamentosCancelados = 0;
                decimal valorTotalLancamentosCancelados = 0;
                g_financeiro record_g_financeiro = db.g_financeiro.Find(modal_g_financeiro.id_financeiro);
                record_g_financeiro.id_financeiro_status = 5; // Baixado
                record_g_financeiro.id_financeiro_remessa = null;
                record_g_financeiro.motivo_baixa = modal_g_financeiro.motivo_baixa;
                record_g_financeiro.datahora_baixa = DataHoraAtual;
                record_g_financeiro.id_usuario_baixa = CachePersister.userIdentity.IdUsuario;
                record_g_financeiro.datahora_alteracao = DataHoraAtual;
                record_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_financeiro).State = EntityState.Modified;

                IQueryable<g_financeiro_lancamentos> listaDbFinanceiroLancamentos = db.g_financeiro_lancamentos.Where(l => l.id_financeiro == record_g_financeiro.id_financeiro);
                foreach (g_financeiro_lancamentos record_g_financeiro_lancamentos in listaDbFinanceiroLancamentos)
                {
                    qtdLancamentosCancelados += 1;
                    valorTotalLancamentosCancelados += record_g_financeiro_lancamentos.valor_total_bruto;
                    record_g_financeiro_lancamentos.id_financeiro_status = 3; // Em Cancelamento                        
                    record_g_financeiro_lancamentos.motivo_cancelamento = modal_g_financeiro.motivo_baixa;
                    record_g_financeiro_lancamentos.datahora_cancelamento = DataHoraAtual;
                    record_g_financeiro_lancamentos.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                    record_g_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                    record_g_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_g_financeiro_lancamentos).State = EntityState.Modified;
                }

                db.SaveChanges();
                sucesso = true;
                msgRetorno += "Título <b>Baixado</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>"
                + "##### Título Baixado #####" + "<br/>"
                + "Id: " + record_g_financeiro.id_financeiro + "<br/>"
                + "Valor (R$): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_g_financeiro.valor_total_bruto) + "<br/><br/>"
                + "##### Lançamentos Cancelados #####" + "<br/>"
                + "Qtd.: " + qtdLancamentosCancelados.ToString() + "<br/><br/>";
            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalProrrogarVencimentoTitulo
        public ActionResult ModalProrrogarVencimentoTitulo(String idFinanceiro)
        {
            ViewBag.Title = "Prorrogar Vencimento Título";
            int id = 0;
            int.TryParse(idFinanceiro, out id);
            CstFinanceiroProrrogarVencimentoTitulos record_cstFinanceiroProrrogarVencimentoTitulos = new CstFinanceiroProrrogarVencimentoTitulos
            {
                data_vencimento = DateTime.Now,
                id_financeiro = id
            };
            return View(record_cstFinanceiroProrrogarVencimentoTitulos);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxSimularProrrogarVencimentoTitulos(CstFinanceiroProrrogarVencimentoTitulos record_cstFinanceiroProrrogarVencimentoTitulos)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            int qtdInconsistencias = 0;

            try
            {
                if (qtdInconsistencias == 0)
                {
                    g_financeiro record_g_financeiro = db.g_financeiro.Find(record_cstFinanceiroProrrogarVencimentoTitulos.id_financeiro);
                    g_contas_caixas record_g_contas_caixas = db.g_contas_caixas.Where(c => c.id_conta_caixa == record_g_financeiro.id_conta_caixa_geracao).FirstOrDefault();

                    if (record_g_financeiro.data_vencimento >= record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento.GetValueOrDefault())
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Data da prorrogação deve ser MAIOR que a data de vencimento do título, Id [" + record_g_financeiro.id_financeiro.ToString() + " ] vencimento [ " + record_g_financeiro.data_vencimento.ToString("dd/MM/yy") + " ]<br/>";
                    }

                    if (qtdInconsistencias == 0)
                    {
                        int diferencaDias = 0;
                        Decimal valorOriginalTitulo = record_g_financeiro.valor_total_bruto;
                        Decimal parametroMultaFixo = record_g_contas_caixas.multa_fixo;
                        Decimal parametroJurosDia = record_g_contas_caixas.multa_dia;
                        Decimal valorEncargos = 0;
                        Decimal valorDescontos = 0;
                        Decimal.TryParse(record_cstFinanceiroProrrogarVencimentoTitulos.juros_multas_valor.EmptyIfNull().Trim().ToString(), out valorEncargos);
                        Decimal valorOriginalFinanceiro = record_g_financeiro.valor_total_bruto;


                        if (record_cstFinanceiroProrrogarVencimentoTitulos.juros_multas_automatico == true)
                        {
                            if (record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento > record_g_financeiro.data_vencimento)
                            {
                                diferencaDias = record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento.GetValueOrDefault().Subtract(record_g_financeiro.data_vencimento).Days;
                            }
                            if (diferencaDias > 0)
                            {
                                valorEncargos += (record_g_financeiro.valor_total_bruto / 100) * parametroMultaFixo;
                                valorEncargos += ((record_g_financeiro.valor_total_bruto / 100) * parametroJurosDia) * diferencaDias;
                            }
                        }
                        else
                        {
                            valorEncargos = record_cstFinanceiroProrrogarVencimentoTitulos.juros_multas_valor;
                            valorDescontos = record_cstFinanceiroProrrogarVencimentoTitulos.descontos_valor;
                        }

                        sucesso = true;
                        msgRetorno += "<b>Valores da Simulação!</b><br/><br/>"
                                   + "R$ Original: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorOriginalTitulo) + "<br/>"
                                   + "Data Venc. Prorrogada: " + record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy") + "<br/><br/>";

                        if (valorDescontos > 0) { msgRetorno += "R$ Descontos (Total): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorDescontos) + "<br/>"; };
                        if (valorEncargos > 0) { msgRetorno += "R$ Encargos (Total): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorEncargos) + "<br/>"; };

                        msgRetorno += "<br/><br/>" + "R$ Total (Atualizado): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (valorOriginalTitulo + valorEncargos - valorDescontos));
                    }
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
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxProrrogarVencimentoTitulo(CstFinanceiroProrrogarVencimentoTitulos record_cstFinanceiroProrrogarVencimentoTitulos)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            int qtdInconsistencias = 0;

            try
            {
                if (qtdInconsistencias == 0)
                {
                    g_financeiro record_g_financeiro = db.g_financeiro.Find(record_cstFinanceiroProrrogarVencimentoTitulos.id_financeiro);
                    g_contas_caixas record_g_contas_caixas = db.g_contas_caixas.Where(c => c.id_conta_caixa == record_g_financeiro.id_conta_caixa_geracao).FirstOrDefault();

                    if (record_g_financeiro.data_vencimento >= record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento.GetValueOrDefault())
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Data da prorrogação deve ser MAIOR que a data de vencimento do título, Id [" + record_g_financeiro.id_financeiro.ToString() + " ] vencimento [ " + record_g_financeiro.data_vencimento.ToString("dd/MM/yy") + " ]<br/>";
                    }

                    if (qtdInconsistencias == 0)
                    {
                        int diferencaDias = 0;
                        Decimal valorOriginalTitulo = record_g_financeiro.valor_total_bruto;
                        Decimal parametroMultaFixo = record_g_contas_caixas.multa_fixo;
                        Decimal parametroJurosDia = record_g_contas_caixas.multa_dia;
                        Decimal valorEncargos = 0;
                        Decimal valorDescontos = 0;
                        Decimal.TryParse(record_cstFinanceiroProrrogarVencimentoTitulos.juros_multas_valor.EmptyIfNull().Trim().ToString(), out valorEncargos);
                        Decimal valorOriginalFinanceiro = record_g_financeiro.valor_total_bruto;

                        if (record_cstFinanceiroProrrogarVencimentoTitulos.juros_multas_automatico == true)
                        {
                            if (record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento > record_g_financeiro.data_vencimento)
                            {
                                diferencaDias = record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento.GetValueOrDefault().Subtract(record_g_financeiro.data_vencimento).Days;
                            }
                            if (diferencaDias > 0)
                            {
                                valorEncargos += (record_g_financeiro.valor_total_bruto / 100) * parametroMultaFixo;
                                valorEncargos += ((record_g_financeiro.valor_total_bruto / 100) * parametroJurosDia) * diferencaDias;
                            }
                        }
                        else
                        {
                            valorEncargos = record_cstFinanceiroProrrogarVencimentoTitulos.juros_multas_valor;
                            valorDescontos = record_cstFinanceiroProrrogarVencimentoTitulos.descontos_valor;
                        }

                        record_g_financeiro.valor_total_liquido += (valorEncargos - valorDescontos);
                        record_g_financeiro.valor_total_bruto += (valorEncargos - valorDescontos);
                        record_g_financeiro.valor_encargos += valorEncargos;
                        record_g_financeiro.valor_descontos += valorDescontos;
                        record_g_financeiro.data_vencimento = record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento.GetValueOrDefault();
                        record_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        record_g_financeiro.datahora_alteracao = DataHoraAtual;
                        record_g_financeiro.datahora_alteracao = DataHoraAtual;
                        record_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_g_financeiro).State = EntityState.Modified;
                        db.SaveChanges();
                        sucesso = true;

                        msgRetorno += "Título <b>Prorrogado</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>"
                                + "R$ Original: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorOriginalTitulo) + "<br/>"
                                + "Data Venc. Prorrogada: " + record_cstFinanceiroProrrogarVencimentoTitulos.data_vencimento.GetValueOrDefault().ToString("dd/MM/yy") + "<br/><br/>";
                        if (valorDescontos > 0) { msgRetorno += "R$ Descontos (Total): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorDescontos) + "<br/>"; };
                        if (valorEncargos > 0) { msgRetorno += "R$ Encargos (Total): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorEncargos) + "<br/>"; };
                        msgRetorno += "<br/><br/>" + "R$ Total (Atualizado): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (valorOriginalTitulo + valorEncargos - valorDescontos));
                    }
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
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalEditarTitulo
        public ActionResult ModalEditarTitulo(String idFinanceiro)
        {
            ViewBag.Title = "Editar Título - Reabrir Lançamentos";
            int id = 0;
            int.TryParse(idFinanceiro, out id);
            g_financeiro record_g_financeiro = db.g_financeiro.Find(id);
            if (record_g_financeiro == null)
            {
                record_g_financeiro = new g_financeiro { id_financeiro = id };
            }
            return View(record_g_financeiro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AjaxEditarTitulo(g_financeiro modal_g_financeiro)
        {
            bool sucesso = false;
            int qtdInconsistencias = 0;
            int qtdLancamentosAbertos = 0;
            decimal valorTotalLancamentosAbertos = 0;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                g_financeiro record_g_financeiro = db.g_financeiro.Find(modal_g_financeiro.id_financeiro);

                if (record_g_financeiro == null)
                {
                    qtdInconsistencias += 1;
                    msgRetorno += " - Título financeiro NÃO localizado!</b>";
                }
                else
                {
                    if (record_g_financeiro.id_financeiro_status != 1)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += " - Somente os títulos com status <b>Em Aberto</b> poderão ser editados!</b>";
                    }
                    else
                    {
                        IQueryable<g_financeiro> listaTitulosCliente = db.g_financeiro.Where(p => p.id_cliente == modal_g_financeiro.id_cliente && p.id_financeiro_status == 10).OrderBy(p => p.id_financeiro);
                        if (listaTitulosCliente.Count() > 0)
                        {
                            qtdInconsistencias += 1;
                            msgRetorno += " - Não é possível editar esse título, cliente já se encontra com outro título em edição!<br/>";
                        }
                    }
                }
                if ((qtdInconsistencias == 0) && (modal_g_financeiro.motivo_cancelamento.ToString().Trim().Length == 0))
                {
                    qtdInconsistencias += 1;
                    msgRetorno += " - Campo <b>Motivo Edição</b> é de preenchimento obrigatório!<br/>";
                }


                if (qtdInconsistencias == 0)
                {
                    // Edição do título Financeiro
                    record_g_financeiro.id_financeiro_status = 10; // Em Edição
                    record_g_financeiro.datahora_alteracao = DataHoraAtual;
                    record_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_g_financeiro).State = EntityState.Modified;

                    // Edição dos lançamentos associados ao título
                    IQueryable<g_financeiro_lancamentos> listaDbFinanceiroLancamentos = db.g_financeiro_lancamentos.Where(l => l.id_financeiro == record_g_financeiro.id_financeiro);
                    foreach (g_financeiro_lancamentos record_g_financeiro_lancamentos in listaDbFinanceiroLancamentos)
                    {
                        qtdLancamentosAbertos += 1;
                        valorTotalLancamentosAbertos += record_g_financeiro_lancamentos.valor_total_bruto;
                        record_g_financeiro_lancamentos.id_financeiro_status = 1; // Aberto
                        record_g_financeiro_lancamentos.valor_faturado = 0;
                        record_g_financeiro_lancamentos.datahora_alteracao = DataHoraAtual;
                        record_g_financeiro_lancamentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(record_g_financeiro_lancamentos).State = EntityState.Modified;
                    }

                    // Criação do histórico do título
                    g_financeiro_historicos record_g_financeiro_historicos = new g_financeiro_historicos();
                    record_g_financeiro_historicos.id_financeiro = record_g_financeiro.id_financeiro;
                    record_g_financeiro_historicos.id_financeiro_origem = 6; // Edição Título
                    record_g_financeiro_historicos.id_financeiro_status_inicial = 1;
                    record_g_financeiro_historicos.id_financeiro_status_final = 10;
                    record_g_financeiro_historicos.id_conta_caixa = record_g_financeiro.id_conta_caixa_geracao;
                    record_g_financeiro_historicos.historico = modal_g_financeiro.motivo_cancelamento;
                    record_g_financeiro_historicos.id_coligada = 1;
                    record_g_financeiro_historicos.id_filial = 1;
                    record_g_financeiro_historicos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    record_g_financeiro_historicos.datahora_cadastro = DataHoraAtual;
                    db.g_financeiro_historicos.Add(record_g_financeiro_historicos);

                    db.SaveChanges();
                    sucesso = true;
                    msgRetorno += "Título liberado para <b>Edição dos Lançamentos</b>!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>"
                    + "Id: " + record_g_financeiro.id_financeiro + "<br/>"
                    + "Valor (R$): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_g_financeiro.valor_total_bruto) + "<br/><br/>"
                    + "##### Lançamentos Reabertos #####" + "<br/>"
                    + "Qtd.: " + qtdLancamentosAbertos.ToString() + "<br/>"
                    + "Valor (R$): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", valorTotalLancamentosAbertos) + "<br/><br/>";
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
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region AjaxFinanceiroCancelamento
        [HttpPost]
        public ActionResult AjaxFinanceiroCancelamento(string id)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            int IdTitulo = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                int.TryParse(id, out IdTitulo);
                g_financeiro record_g_financeiro = db.g_financeiro.Find(IdTitulo);
                if (record_g_financeiro != null)
                {
                    record_g_financeiro.id_financeiro_status = 3; // Cancelado
                    record_g_financeiro.id_usuario_cancelamento = CachePersister.userIdentity.IdUsuario;
                    record_g_financeiro.datahora_cancelamento = LibDateTime.getDataHoraBrasilia();
                    record_g_financeiro.datahora_alteracao = DataHoraAtual;
                    record_g_financeiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_g_financeiro).State = EntityState.Modified;

                    gc_movimentos_financeiros record_gc_movimentos_financeiros = db.gc_movimentos_financeiros.Where(mf => mf.id_movimento == record_g_financeiro.id_financeiro_movimento && mf.ativo == true).FirstOrDefault();
                    if (record_gc_movimentos_financeiros != null)
                    {
                        if (record_gc_movimentos_financeiros.id_financeiro_1 == record_g_financeiro.id_financeiro) { record_gc_movimentos_financeiros.id_financeiro_1 = 0; }
                        else if (record_gc_movimentos_financeiros.id_financeiro_2 == record_g_financeiro.id_financeiro) { record_gc_movimentos_financeiros.id_financeiro_2 = 0; }
                        else if (record_gc_movimentos_financeiros.id_financeiro_3 == record_g_financeiro.id_financeiro) { record_gc_movimentos_financeiros.id_financeiro_3 = 0; }
                        else if (record_gc_movimentos_financeiros.id_financeiro_4 == record_g_financeiro.id_financeiro) { record_gc_movimentos_financeiros.id_financeiro_4 = 0; }
                        else if (record_gc_movimentos_financeiros.id_financeiro_5 == record_g_financeiro.id_financeiro) { record_gc_movimentos_financeiros.id_financeiro_5 = 0; }
                        db.Entry(record_gc_movimentos_financeiros).State = EntityState.Modified;
                    }
                    db.SaveChanges();

                    msgRetorno += "<b>----- Dados do título financeiro -----</b>" + "<br/>";
                    msgRetorno += "Id:   " + record_g_financeiro.id_financeiro.ToString() + "<br/>";
                    msgRetorno += "Venc:    " + record_g_financeiro.data_vencimento.ToString("dd/MM/yyyy") + "<br/>";
                    msgRetorno += "R$ Valor:    " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", record_g_financeiro.valor_total_bruto).Replace("R$ ", "") + "<br/>";
                    msgRetorno += "Tipo Rec:    " + db.g_pagrec_tipos.Find(record_g_financeiro.id_pagamento_recebimento_tipo).descricao.EmptyIfNull().ToString() + "<br/>";
                    msgRetorno += "Status:    " + "Título CANCELADO com sucesso!" + "<br/>";
                    sucesso = true;
                }
                else
                {
                    msgRetorno = "Título financeiro [Id: " + id + "] NÃO localizado!";
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
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
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

        private JsonResult JsonAjaxErroIdProcessamento(Exception ex, string idProcessamento)
        {
            return Json(new { success = false, msg = GdiMvcJsonResults.AjaxFailureMessage(ex), idProcessamento = idProcessamento ?? "0" }, JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacaoIdProcessamento(DbEntityValidationException ex, string idProcessamento)
        {
            return Json(new { success = false, msg = GdiMvcJsonResults.AjaxFailureValidationMessage(ex), idProcessamento = idProcessamento ?? "0" }, JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }

    }
}
