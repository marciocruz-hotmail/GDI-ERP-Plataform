using ClosedXML.Excel;
using GdiPlataform.Areas.g.Controllers;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.GDI;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Windows;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexImportacoes_*,gc_ComexImportacoes_Default")]
    public class ComexImportacoesController : Controller
    {
        private GdiPlataformEntities db;
        private HSSFWorkbook _workbookCatalogo;

        public ComexImportacoesController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexImportacoes_*,gc_ComexImportacoes_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-globe", "", "", "") + LibStringFormat.GetTabHtml(1) + "Comex - Gestão de Importações";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexImportacoes_*,gc_ComexImportacoes_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string errorMessage = "";
            string stackTrace = "";

            try
            {
                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // -----------------------------
                // Base query (LINQ + AsNoTracking)
                // -----------------------------
                IQueryable<Db.gc_comex_importacoes> query = db.gc_comex_importacoes
                    .AsNoTracking()
                    .Where(i => i.id_importacao > 0 && i.ativo == true);

                // Totais do DataTables (com filtros aplicados)
                int totalRecords = query.Count();
                int totalDisplayRecords = totalRecords;

                // -----------------------------
                // Ordenação + paginação (OrderBy antes do Skip)
                // -----------------------------
                var page = query
                    .OrderByDescending(i => i.data_registro)
                    .Skip(start)
                    .Take(length)
                    .Select(i => new
                    {
                        i.id_importacao,
                        i.data_registro,
                        i.numero,
                        i.di_numero,
                        i.id_importacao_status,
                        i.di_data_registro,
                        i.di_cambio,
                        i.cambio_debito,
                        i.despesas_fob_ajustado
                    })
                    .ToList();

                // -----------------------------
                // Lookup de status (1 query, vira dicionário)
                // -----------------------------
                var statusMap = db.gc_comex_importacoes_status
                    .AsNoTracking()
                    .Select(s => new { s.id_importacao_status, s.status })
                    .ToList()
                    .ToDictionary(x => x.id_importacao_status, x => x.status);

                // -----------------------------
                // Montagem aaData
                // -----------------------------
                var list = new List<string[]>(page.Count);

                foreach (var i in page)
                {
                    string diDataRegistro = i.di_data_registro.HasValue
                        ? i.di_data_registro.Value.ToString("dd/MM/yy")
                        : "";

                    string status = statusMap.TryGetValue(i.id_importacao_status, out var st) ? (st ?? "") : "";

                    list.Add(new[]
                    {
                "", // Coluna de Seleção
                i.id_importacao.ToString(),
                i.data_registro.ToString("dd/MM/yy"),
                i.numero.EmptyIfNull().ToString(),
                i.di_numero.EmptyIfNull().ToString(),
                status,
                diDataRegistro,
                i.di_cambio.ToString("###,###,##0.00000"),
                i.cambio_debito.ToString("###,###,##0.00"),
                i.despesas_fob_ajustado.ToString("###,###,##0.00"),
                "" // Botão Editar
            });
                }

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
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

            // ✅ Retorno padrão do DataTables + erro real
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = stackTrace, // se quiser ocultar em produção, devolva ""
                yesFilterOnOff = "0",
                sEcho = param.sEcho,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Create
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexImportacoes_*,gc_ComexImportacoes_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Comércio Exterior - Nova Importação</b>";
            gc_comex_importacoes record_gc_comex_importacoes = new gc_comex_importacoes();
            record_gc_comex_importacoes.id_importacao_status = 1;
            record_gc_comex_importacoes.data_registro = LibDateTime.getDataHoraBrasilia();
            PreencherLookupsCreateEdit();
            return View("CreateEdit", record_gc_comex_importacoes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexImportacoes_*,gc_ComexImportacoes_Actioncreate")]
        public ActionResult Create(gc_comex_importacoes view_gc_comex_importacoes)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Comércio Exterior - Nova Importação</b>";
            if (view_gc_comex_importacoes.numero.EmptyIfNull().ToString().Equals(String.Empty)) { ModelState.AddModelError("Model", "Campo [Número da Importação (LI)] é de preenchimento obrigatório"); }
            if (view_gc_comex_importacoes.data_registro == null) { ModelState.AddModelError("Model", "Campo [Data Registro DI] é de preenchimento obrigatório"); }

            if (ModelState.IsValid)
            {
                view_gc_comex_importacoes.ativo = true;
                view_gc_comex_importacoes.id_coligada = 0;  // Definição de que Cidade é Global
                view_gc_comex_importacoes.id_filial = 0;    // Definição de que Cidade é Global
                view_gc_comex_importacoes.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                view_gc_comex_importacoes.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.gc_comex_importacoes.Add(view_gc_comex_importacoes);
                try
                {
                    db.SaveChanges();
                    String Logs = LibDB.CompareDataTable(new Db.gc_comex_importacoes(), view_gc_comex_importacoes);
                    if (Logs.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true,"gc_comex_importacoes", view_gc_comex_importacoes.id_importacao, "Nova Importação | " + Logs); };
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    ModelState.AddModelError("Model", LibExceptions.getDbEntityValidationException(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", LibExceptions.getExceptionShortMessage(e));
                }
            }
            PreencherLookupsCreateEdit();
            return View("CreateEdit", view_gc_comex_importacoes);
        }
        #endregion

        #region PreencherLookupsCreateEdit
        public void PreencherLookupsCreateEdit()
        {
            var comboStatusImportacao = new List<SelectListItem>();
            try
            {
                IQueryable<gc_comex_importacoes_status> listaDbImportacoesStatus = db.gc_comex_importacoes_status.Where(s => s.id_importacao_status > 0).OrderBy(i => i.ordem);
                foreach (gc_comex_importacoes_status item_gc_comex_importacoes_status in listaDbImportacoesStatus)
                {
                    comboStatusImportacao.Add(new SelectListItem { Value = item_gc_comex_importacoes_status.id_importacao_status.ToString(), Text = item_gc_comex_importacoes_status.status.ToString() });
                }
            }
            finally { }
            ViewBag.comboStatusImportacao = comboStatusImportacao;
        }
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexImportacoes_*,gc_ComexImportacoes_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(id);
            CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record_gc_comex_importacoes);
            if (record_gc_comex_importacoes == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Comércio Exterior - Importação</b>" + LibStringFormat.GetTabHtml(1) + "(Id: " + record_gc_comex_importacoes.id_importacao.EmptyIfNull().ToString() + ")" + LibStringFormat.GetTabHtml(1) + record_gc_comex_importacoes.numero.EmptyIfNull().ToString();
            CachePersister.userIdentity.IdGcComexImportacaoAtiva = id.GetValueOrDefault();
            PreencherLookupsCreateEdit();
            return View("CreateEdit", record_gc_comex_importacoes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_ComexImportacoes_*,gc_ComexImportacoes_Actionupdate")]
        public ActionResult Edit(gc_comex_importacoes view_gc_comex_importacoes)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            Decimal FechamentoTotalDespesasDesembaracoReais = 0;
            if (view_gc_comex_importacoes.numero.EmptyIfNull().ToString().Equals(String.Empty)) { ModelState.AddModelError("Model", "Campo [Número da Importação (LI)] é de preenchimento obrigatório"); }
            if (view_gc_comex_importacoes.data_registro == null) { ModelState.AddModelError("Model", "Campo [Data Registro DI] é de preenchimento obrigatório"); }

            if (ModelState.IsValid)
            {
                if (view_gc_comex_importacoes.di_cambio > 1)
                {
                    FechamentoTotalDespesasDesembaracoReais = 0;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_ipi;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_ii;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_pis;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_cofins;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_siscomex;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_icms;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_ibs;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_cbs;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_csll;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_sda;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_marinha_mercante;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_armazenagem_primaria;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_armazenagem_secundaria;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_transp_rodo_remocao;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_armazenagem_infraero;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_despachante;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_taxa_expediente_santos;
                    FechamentoTotalDespesasDesembaracoReais += view_gc_comex_importacoes.despesas_taxa_capatazia;
                    view_gc_comex_importacoes.total_custo = view_gc_comex_importacoes.despesas_fob_ajustado + (FechamentoTotalDespesasDesembaracoReais / view_gc_comex_importacoes.di_cambio);
                    view_gc_comex_importacoes.percentual_custo_fob = ((view_gc_comex_importacoes.total_custo * 100) / view_gc_comex_importacoes.despesas_fob_ajustado);
                }

                view_gc_comex_importacoes.datahora_alteracao = DataHoraAtual;
                view_gc_comex_importacoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(view_gc_comex_importacoes).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                    if (view_gc_comex_importacoes.id_importacao > 0) 
                    {
                        string Logs = LibDB.CompareDataTable(JsonConvert.DeserializeObject<gc_comex_importacoes>(CachePersister.userIdentity.DataRowInUseSerialized), view_gc_comex_importacoes);
                        if (Logs.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true,"gc_comex_importacoes", view_gc_comex_importacoes.id_importacao, "Atualização Dados | " + Logs); };
                    };
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    ModelState.AddModelError("Model", LibExceptions.getDbEntityValidationException(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", LibExceptions.getExceptionShortMessage(e));
                }
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Comércio Exterior - Importação</b>" + LibStringFormat.GetTabHtml(1) + "(Id: " + view_gc_comex_importacoes.id_importacao.EmptyIfNull().ToString() + ")" + LibStringFormat.GetTabHtml(1) + view_gc_comex_importacoes.numero.EmptyIfNull().ToString();
            return View("CreateEdit", view_gc_comex_importacoes);
        }
        #endregion

        #region GetDadosItensImportacao
        public ActionResult GetDadosItensImportacao(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
                int QtdItensImportacao = 0;
                int QtdProdutosImportacao = 0;
                decimal ValorTotalImportacao = 0;
                int IdImportacao = -1;
                int.TryParse(param.yesCustomIdPK, out IdImportacao);

                var allRecords = new List<Db.gc_comex_importacoes_itens>(); // Lista vazia - Inicialização

                allRecords = db.gc_comex_importacoes_itens.Where(i => (i.id_importacao_item > 0 && i.ativo == true && i.id_importacao == IdImportacao)).OrderBy(i => i.id_importacao_item).ToList();
                var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
                Func<Db.gc_comex_importacoes_itens, string> orderingFunction = (i =>
                                         param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(i.id_importacao_item) :
                                         param.iSortCol_0 == 2 && param.iSortingCols > 0 ? Convert.ToString(i.id_importacao_item) :
                                         "");
                if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
                else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

                List<string[]> list = new List<string[]>();
                foreach (var itemImportacao in allRecords)
                {
                    QtdItensImportacao += int.Parse(itemImportacao.quantidade.ToString().Replace(",000", "").Replace(",00", ""));
                    QtdProdutosImportacao += 1;
                    ValorTotalImportacao += itemImportacao.valor_total;
                }
                foreach (var i in displayedRecords)
                {
                    list.Add(new[] {
                                        i.quantidade.ToString().Replace(",000","").Replace(",00",""),
                                        i.descricao.ToString(),
                                        string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.valor_unit).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
                                        string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", i.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "")
                                    });
                }
                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    yesDisplayField01 = QtdItensImportacao.ToString(),
                    yesDisplayField02 = QtdProdutosImportacao.ToString() + " Registros",
                    yesDisplayField03 = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorTotalImportacao).Replace("R$ ", "").Replace("R$", "").Replace("$", ""),
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

        #region GetGedComex
        public ActionResult GetGedComex(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            List<g_usuarios> allUsuarios = db.g_usuarios.Where(u => u.id_usuario > 0).ToList();
            List<Db.ged_arquivos> allRecords = db.ged_arquivos.Where(g => g.ativo == true && g.id_comex_importacao == CachePersister.userIdentity.IdGcComexImportacaoAtiva).ToList();
            List<string[]> list = new List<string[]>();

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.ged_arquivos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_arquivo) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.filename :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.descricao :
                                     "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            foreach (var ged in displayedRecords)
            {
                String DataReferencia = String.Empty;
                String NomeUsuario = allUsuarios.Where(u => u.id_usuario == ged.id_usuario_cadastro).FirstOrDefault().login.EmptyIfNull().ToString();
                if (ged.datahora_cadastro != null) { DataReferencia = ged.datahora_cadastro.GetValueOrDefault().ToString("dd/MM/yy"); }; 

                list.Add(new[] {
                                    ged.id_arquivo.ToString(),
                                    ged.descricao.ToString(),
                                    ged.filename.ToString(),
                                    ged.filetype.ToString(),
                                    ged.versao.ToString(),
                                    DataReferencia,
                                    NomeUsuario,
                                    "", // Botão Editar
                                    "" // Botão Download
                                });
            }

            String filterOnOff = "0";
            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; };

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
                return JsonDataTableException(e, param, "0");
            }
        }
        #endregion

        #region GetGedInvoicesComex
        public ActionResult GetGedInvoicesComex(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQLGED = string.Empty;
            List<Db.gc_comex_invoices_pdf> AllRecordsInvoicesPDF = db.gc_comex_invoices_pdf.Where(i => i.ativo == true && i.id_importacao == CachePersister.userIdentity.IdGcComexImportacaoAtiva).ToList();
            List<string[]> list = new List<string[]>();
            SentencaSQLGED = "select * from ged_arquivos where ativo = 1 and id_arquivo in (select distinct id_ged from gc_comex_invoices_pdf where id_importacao = " + CachePersister.userIdentity.IdGcComexImportacaoAtiva + ")";
            List<Db.ged_arquivos> AllRecodsGedArquivos = db.ged_arquivos.SqlQuery(SentencaSQLGED).ToList();
            var displayedRecords = AllRecordsInvoicesPDF.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            foreach (var invoice in displayedRecords)
            {
                String GedIdArquivo = "0";
                String GedArquivo = string.Empty;
                String GedVersao = string.Empty;
                ged_arquivos GedInvoice = AllRecodsGedArquivos.Where(g => g.id_arquivo == invoice.id_ged).FirstOrDefault();
                if (GedInvoice != null)
                {
                    GedIdArquivo = GedInvoice.id_arquivo.EmptyIfNull().ToString();
                    GedArquivo = GedInvoice.filename.EmptyIfNull().ToString();
                    GedVersao = GedInvoice.versao.EmptyIfNull().ToString();
                }
                list.Add(new[] {
                                    GedIdArquivo,
                                    invoice.invoice.EmptyIfNull().ToString(),
                                    invoice.sales_order.EmptyIfNull().ToString(),
                                    GedArquivo,
                                    GedVersao,
                                    "" // Botão Download
                                });
            }

            filterOnOff = "0";
            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; };

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = AllRecordsInvoicesPDF.Count(),
                iTotalDisplayRecords = AllRecordsInvoicesPDF.Count(),
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

        #region ModalCancelarImportacaoComex
        public ActionResult ModalCancelarImportacaoComex(String idImportacao)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cancelar Importação</b>";
            gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(int.Parse(idImportacao));
            CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record_gc_comex_importacoes);
            record_gc_comex_importacoes.exclusao_motivo = "";
            return View(record_gc_comex_importacoes);
        }

        [HttpPost]
        public ActionResult AjaxModalCancelarImportacaoComex(gc_comex_importacoes modal_gc_comex_importacoes)
        {
            bool Sucesso = false;
            bool ErroProcessamento = false;
            String MsgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (modal_gc_comex_importacoes.exclusao_motivo.EmptyIfNull().ToString().Length == 0)
                {
                    ErroProcessamento = true;
                    MsgRetorno += "Campo [Motivo] é de preenchimento obrigatório!" + "</br>";
                }
                else
                {
                    var allImportacoes = db.gc_comex_invoices.Where(i => (i.id_importacao == modal_gc_comex_importacoes.id_importacao) && (i.ativo == true)).ToList();
                    if (allImportacoes.Count() > 0)
                    {
                        ErroProcessamento = true;
                        MsgRetorno += "Foram localizadas [" + allImportacoes.Count().ToString() + "] Invoices associadas à essa importação, não é possível cancelar importações com Invoices ativas!" + "</br>";
                    }
                }
                if (ErroProcessamento == false)
                {
                    gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(modal_gc_comex_importacoes.id_importacao);
                    record_gc_comex_importacoes.ativo = false;
                    record_gc_comex_importacoes.exclusao_datahora = DataHoraAtual;
                    record_gc_comex_importacoes.exclusao_id_usuario = CachePersister.userIdentity.IdUsuario; ;
                    record_gc_comex_importacoes.exclusao_motivo = modal_gc_comex_importacoes.exclusao_motivo;
                    record_gc_comex_importacoes.datahora_alteracao = DataHoraAtual;
                    record_gc_comex_importacoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_gc_comex_importacoes).State = EntityState.Modified;

                    db.SaveChanges();
                    Sucesso = true;
                    MsgRetorno += "Importação <b>Cancelada</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");

                    string Logs = "Cancelar Importação | " + LibDB.CompareDataTable(JsonConvert.DeserializeObject<gc_comex_importacoes>(CachePersister.userIdentity.DataRowInUseSerialized), record_gc_comex_importacoes);
                    LibAudit.SaveAudit(db, true,"gc_comex_importacoes", record_gc_comex_importacoes.id_importacao, Logs);
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

        #region ModalCarregarItensImportacao
        public ActionResult ModalCarregarItensImportacao()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-file-excel", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Carregar Itens Importação (xlsx)</b>";
            return View();

        }
        public ActionResult AjaxModalCarregarItensImportacao(HttpPostedFileBase filesource) // UPLOAD PLANILHA DE ITENS
        {
            int IndexNfNumero = 1;
            int IndexDiNumero = 2;
            int IndexDiData = 3;
            int IndexPN = 5;
            int IndexDescricao = 6;
            int IndexDiAdicaoNumero = 7;
            int IndexDiAdicaoSequencial = 8;
            int IndexNcmCodigo = 9;
            int IndexValorFob = 10;
            int IndexValorFrete = 11;
            int IndexQuantidade = 15;
            int IndexUnidade = 16;
            int IndexValorUnit = 17;
            int IndexValorTotal = 18;
            int IndexIiPercentual = 20;
            int IndexIiValor = 19;
            int IndexIpiBaseCalculo = 21;
            int IndexIpiPercentual = 22;
            int IndexIpiValor = 23;
            int IndexIcmsBaseCalculo = 24;
            int IndexIcmsBaseReduzida = 25;
            int IndexIcmsPercentual = 26;
            int IndexIcmsValor = 27;
            int IndexPisBaseCalculo = 28;
            int IndexPisPercentual = 29;
            int IndexPisValor = 30;
            int IndexCofinsBaseCalculo = 28;
            int IndexCofinsPercentual = 31;
            int IndexCofinsValor = 32;
            int IndexSiscomexValor = 34;
            int IndexPesoLiquido = 35;
            int IndexPesoBruto = 36;
            int IndexLiNumero = 37;

            int IndexIbsCbsCst = 38;
            int IndexcClassTrib = 39;
            int IndexIbsCbsBaseCalculo = 40;
            int IndexIbsPercentual = 41;
            int IndexIbsValor = 42;
            int IndexCbsPercentual = 43;
            int IndexCbsValor = 44;
            int IndexSdaValor = 45;
            int IndexMarinhaValor = 46;

            int QtdItensInvoicesSemProduto = 0;
            int QtdItensSomentePlanilha = 0;
            int QtdItensSomenteInvoices = 0;

            int QtdProdutosERPCadastrar = 0;
            int QtdProdutosERPNomesAtualizar = 0;

            //int QtdProdutosComexMultiplosCadastros = 0;
            int QtdProdutosComexNaoCadastrados = 0;
            int QtdRegistrosArquivo = 0;
            
            int QtdProdutosNCMCadastrados = 0;
            int QtdProdutosUnidadesMedidasCadastradas = 0;
            int QtdProdutosNCMAtualizados = 0;
            int qtdProdutosUnidadeMedidaAtualizadas = 0;

            Decimal QtdItensArquivo = 0;
            Decimal ValorTotalArquivo = 0;
            bool Processado = false;
            bool ErroProcessamento = false;
            bool ErroArquivoXlxs = false;
            string String_Item_Agrupador = string.Empty;
            string MsgRetorno = string.Empty;
            string IdentificadorDI = string.Empty;
            string ResultadoProcessamento = String.Empty;
            string PnProdutosComexNaoCadastrados = String.Empty;
            string PnItensSomentePlanilha = String.Empty;
            string PnItensSomenteInvoices = String.Empty;
            string PnProdutosERPMultiplosCadastros = String.Empty;
            string PnProdutosComexMultiplosCadastros = String.Empty;
            string PnItensInvoicesSemProduto = String.Empty;
            String PNOficial = String.Empty;
            String PNAuxiliar = String.Empty;
            String PNCuringaOH = String.Empty;
            String PNCuringaZERO = String.Empty;

            string PnProdutosDescricaoDivergente = String.Empty;
            String Logs = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DateTime DataDeclaracaoImportacao = LibDateTime.getDataHoraBrasilia();
            gc_comex_importacoes_itens record_gc_comex_importacoes_itens = null;
            List<g_produtos> ListaProdutosGDI = new List<g_produtos>();
            List<gc_comex_produtos> ListaComexProdutos = new List<gc_comex_produtos>();
            List<g_produtos_ncm> ListaProdutosNCM = new List<g_produtos_ncm>();
            List<g_unidade_medida> ListaUnidadesMedidas = new List<g_unidade_medida>();
            List<CstModelComexItemImportacao> ListaPlanilhaItens = new List<CstModelComexItemImportacao>();
            var fileExt = System.IO.Path.GetExtension(filesource.FileName.ToLower()).Substring(1);
            gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(CachePersister.userIdentity.IdGcComexImportacaoAtiva);

            // Totalizadores
            Decimal TotalizadorFobReais = 0;
            Decimal TotalizadorFobDollar = 0;
            Decimal TotalizadorFrete = 0;
            Decimal TotalizadorPesoLiquido = 0;
            Decimal TotalizadorPesoBruto = 0;
            Decimal TotalizadorTaxaDollar = 0;

            Decimal FechamentoPesoLiquidoGDI = 0;
            Decimal FechamentoPesoLiquidoSC = 0;
            Decimal FechamentoPesoLiquidoImportacao = 0;
            Decimal FechamentoPesoBrutoGDI = 0;
            Decimal FechamentoPesoBrutoSC = 0;
            Decimal FechamentoPesoBrutoImportacao = 0;
            Decimal FechamentoTotalFobGDI = 0;
            Decimal FechamentoTotalFobSC = 0;
            Decimal FechamentoTotalFobImportacaoReais = 0;
            Decimal FechamentoValorTotalImportacao = 0;
            Decimal FechamentoValorTotalII = 0;
            Decimal FechamentoValorTotalIPI = 0;
            Decimal FechamentoValorTotalICMS = 0;
            Decimal FechamentoValorTotalPIS = 0;
            Decimal FechamentoValorTotalCofins = 0;
            Decimal FechamentoValorTotalIbs = 0;
            Decimal FechamentoValorTotalCbs = 0;
            Decimal FechamentoValorTotalSiscomex = 0;
            Decimal FechamentoValorTotalSDA = 0;
            Decimal FechamentoValorTotalMarinha = 0;
            Decimal FechamentoValorFrete = 0;
            Decimal FechamentoTotalDespesasDesembaracoReais = 0;

            if (fileExt != "xlsx")
            {
                ErroProcessamento = true;
                MsgRetorno = " - Arquivo de itens deve ser do tipo Planilha Excel (.xlsx)";
                ErroArquivoXlxs = true;
            }
            if (filesource.ContentLength > 500000)
            {
                ErroProcessamento = true;
                MsgRetorno = " - O Tamanho do arquivo não pode exceder 500 Kb!";
                ErroArquivoXlxs = true;
            }
            if (filesource.ContentLength == 0)
            {
                ErroProcessamento = true;
                MsgRetorno = " - O Arquivo está vazio!";
                ErroArquivoXlxs = true;
            }

            if (ErroProcessamento == false)
            {
                try
                {
                    ListaProdutosGDI = db.g_produtos.Where(p => p.id_produto > 0 && p.ativo == true).ToList();
                    ListaProdutosNCM = db.g_produtos_ncm.Where(n => n.id_produto_ncm > 0  && n.ativo == true).ToList();
                    ListaUnidadesMedidas = db.g_unidade_medida.Where(u => (u.id_unidade_medida > 0 && u.ativo == true)).ToList();
                    ListaComexProdutos = db.gc_comex_produtos.Where(c => c.ativo == true && c.id_comex_produto > 0).ToList();

                    MsgRetorno = String.Empty;
                    var fileNameOrigem = Path.GetFileName(filesource.FileName);
                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    var FileNameInvoice = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_Planilha-Itens_" + fileNameOrigem);
                    filesource.SaveAs(FileNameInvoice);

                    // Link Excel
                    XLWorkbook WorkBook = new XLWorkbook(FileNameInvoice);
                    IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                    // Linha Cabeçalho
                    try
                    {
                        IdentificadorDI = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(2).Cell(2).Value);
                        String RaizIdentificadorDI = LibStringFormat.SomenteAlfabetoeNumeros(IdentificadorDI).EmptyIfNull().ToString().Trim();
                        if (RaizIdentificadorDI.Length < 5)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += " - Identificador da DI [" + IdentificadorDI + "] não foi localizado no arquivo!" + "<br/>";
                        }
                        else
                        {
                            record_gc_comex_importacoes_itens = db.gc_comex_importacoes_itens.Where(i => (i.di_numero == IdentificadorDI && i.ativo == true)).FirstOrDefault();
                            if (record_gc_comex_importacoes_itens != null)
                            {
                                ErroProcessamento = true;
                                MsgRetorno += " - Itens da DI [" + IdentificadorDI + "] já foram importados anteriormente" + "<br/>";
                            }
                        }

                        String DataDI = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(2).Cell(3).Value);
                        DataDI = LibStringFormat.SomenteAlfabetoeNumeros(DataDI).EmptyIfNull().ToString().Trim();
                        if (DataDI.Length < 6)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += " - Data da DI [" + DataDI + "] não foi localizada no arquivo!" + "<br/>";
                        }
                    }
                    catch (Exception ex)
                    {
                        ErroProcessamento = true;
                        MsgRetorno += " - Identificador da DI NÃO localizado" + "<br/>";
                        MsgRetorno += LibExceptions.getExceptionShortMessage(ex);
                        ErroArquivoXlxs = true;
                    }

                    // Dados da Importação
                    if (record_gc_comex_importacoes != null)
                    {
                        if (record_gc_comex_importacoes.di_cambio == 0)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += " - Campo [Valor Câmbio / Taxa Dollar] é de preenchimento obrigatório!" + "<br/>";
                        }
                        else if (record_gc_comex_importacoes.di_cambio < 1)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += " - Campo [Valor Câmbio / Taxa Dollar] contém um valor inválido!" + "<br/>";
                        }
                        else
                        {
                            TotalizadorTaxaDollar = record_gc_comex_importacoes.di_cambio;
                        }
                    }
                    else
                    {
                        ErroProcessamento = true;
                        MsgRetorno += " - Dados da Importação não foram localizados no ERP!" + "<br/>";
                    }

                    // Header
                    if (ErroProcessamento == false)
                    {
                        try
                        {
                            if (WorkSheet.Row(1).Cell(1).Value.IsBlank == false)
                            {
                                CstModelComexItemImportacao ItemImportacao = new CstModelComexItemImportacao();
                                ItemImportacao.String_NfNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexNfNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_DiNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_DiData = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiData).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPN).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemImportacao.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemImportacao.String_PN);
                                ItemImportacao.String_PN_Variacao1 = ItemImportacao.String_PN_Auxiliar.Replace("0", "O");
                                ItemImportacao.String_PN_Variacao2 = ItemImportacao.String_PN_Auxiliar.Replace("O", "0");
                                ItemImportacao.String_Descricao = LibStringFormat.GDIFormatarDescricaoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDescricao).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemImportacao.String_DiAdicaoNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiAdicaoNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_DiAdicaoSequencial = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiAdicaoSequencial).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_NcmCodigo = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexNcmCodigo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_ValorFob = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorFob).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_ValorFrete = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorFrete).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_Quantidade = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexQuantidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_UnidadeMedida = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexUnidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_ValorUnit = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorUnit).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_ValorTotal = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorTotal).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_PesoLiquido = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPesoLiquido).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_PesoBruto = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPesoBruto).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                if (ItemImportacao.IsValidHeader() == false)
                                {
                                    // Tentar o layout com uma coluna a mais (Variação do layout principal)
                                    IndexNfNumero += 1;
                                    IndexDiNumero += 1;
                                    IndexDiData += 1;
                                    IndexPN += 1;
                                    IndexDescricao += 1;
                                    IndexDiAdicaoNumero += 1;
                                    IndexDiAdicaoSequencial += 1;
                                    IndexNcmCodigo += 1;
                                    IndexValorFob += 1;
                                    IndexValorFrete += 1;
                                    IndexQuantidade += 1;
                                    IndexUnidade += 1;
                                    IndexValorUnit += 1;
                                    IndexValorTotal += 1;
                                    IndexPesoLiquido += 1;
                                    IndexPesoBruto += 1;
                                    ItemImportacao.String_NfNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexNfNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiData = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiData).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPN).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemImportacao.String_PN);
                                    ItemImportacao.String_PN_Variacao1 = ItemImportacao.String_PN_Auxiliar.Replace("0", "O");
                                    ItemImportacao.String_PN_Variacao2 = ItemImportacao.String_PN_Auxiliar.Replace("O", "0");
                                    ItemImportacao.String_Descricao = LibStringFormat.GDIFormatarDescricaoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDescricao).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_DiAdicaoNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiAdicaoNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiAdicaoSequencial = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiAdicaoSequencial).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_NcmCodigo = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexNcmCodigo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorFob = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorFob).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorFrete = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorFrete).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_Quantidade = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexQuantidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_UnidadeMedida = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexUnidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorUnit = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorUnit).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorTotal = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorTotal).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PesoLiquido = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPesoLiquido).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PesoBruto = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPesoBruto).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    if (ItemImportacao.IsValidHeader() == false)
                                    {
                                        ErroProcessamento = true;
                                        MsgRetorno += " - Cabeçalho da planilha NÃO localizado!" + "<br/>";
                                        ErroArquivoXlxs = true;
                                    }
                                }
                            }
                            else
                            {
                                ErroProcessamento = true;
                                MsgRetorno += " - Cabeçalho da planilha NÃO localizado!" + "<br/>";
                                ErroArquivoXlxs = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += " - Cabeçalho da planilha NÃO localizado!" + "<br/>";
                            ErroArquivoXlxs = true;
                            MsgRetorno += LibExceptions.getExceptionShortMessage(ex);
                        }
                    }

                    if (ErroProcessamento == false)
                    {
                        try
                        {
                            List<gc_comex_invoices_itens> ListaIntesInvoicesValidar = db.gc_comex_invoices_itens.Where(i => i.ativo == true && i.id_importacao == CachePersister.userIdentity.IdGcComexImportacaoAtiva).ToList();
                            for (int IndexRow = 2; IndexRow <= (WorkSheet.RowsUsed().Count()); IndexRow++)
                            {
                                if (WorkSheet.Row(IndexRow).Cell(1).Value.IsBlank == false)
                                {
                                    CstModelComexItemImportacao ItemImportacao = new CstModelComexItemImportacao();
                                    ItemImportacao.String_NfNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexNfNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDiNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_LiNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexLiNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiData = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDiData).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPN).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemImportacao.String_PN);
                                    ItemImportacao.String_PN_Variacao1 = ItemImportacao.String_PN_Auxiliar.Replace("0", "O");
                                    ItemImportacao.String_PN_Variacao2 = ItemImportacao.String_PN_Auxiliar.Replace("O", "0");
                                    ItemImportacao.String_Descricao = LibStringFormat.GDIFormatarDescricaoProdutoTraduzidoComPN(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDescricao).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_DiAdicaoNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDiAdicaoNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiAdicaoSequencial = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDiAdicaoSequencial).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_NcmCodigo = LibStringFormat.SomenteNumeros(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexNcmCodigo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_ValorFob = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexValorFob).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorFrete = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexValorFrete).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_Quantidade = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexQuantidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_UnidadeMedida = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexUnidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    if (ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString().Trim().Length == 0) { ItemImportacao.String_UnidadeMedida = "UN"; };
                                    ItemImportacao.String_ValorUnit = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexValorUnit).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorTotal = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexValorTotal).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IiPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIiPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IiValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIiValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IpiBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIpiBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IpiPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIpiPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IpiValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIpiValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IcmsBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIcmsBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IcmsBaseReduzida = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIcmsBaseReduzida).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IcmsPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIcmsPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IcmsValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIcmsValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PisBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPisBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PisPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPisPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PisValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPisValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_CofinsBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCofinsBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_CofinsPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCofinsPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_CofinsValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCofinsValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IbsCbsCst = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIbsCbsCst).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_cClassTrib = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexcClassTrib).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IbsCbsBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIbsCbsBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IbsPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIbsPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IbsValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIbsValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_CbsPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCbsPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_CbsValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCbsValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_SiscomexValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexSiscomexValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_SdaValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexSdaValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_MarinhaValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexMarinhaValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PesoLiquido = LibExcelReader.GetWeightCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPesoLiquido).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PesoBruto = LibExcelReader.GetWeightCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPesoBruto).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();

                                    // CADASTRO DE PRODUTOS - 13
                                    PNOficial = ItemImportacao.String_PN.EmptyIfNull().ToString();
                                    PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                                    PNCuringaOH = PNAuxiliar.Replace("0", "O");
                                    PNCuringaZERO = PNAuxiliar.Replace("O", "0");
                                    gc_comex_invoices_itens ItemInvoice = ListaIntesInvoicesValidar.Where(i => i.pn == PNOficial).FirstOrDefault();
                                    try { if (ItemInvoice == null) { ItemInvoice = ListaIntesInvoicesValidar.Where(p => p.pn_auxiliar == PNAuxiliar || p.pn_variacao1 == PNCuringaOH || p.pn_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar
                                    if (ItemInvoice != null)
                                    {
                                        ItemImportacao.IdInvoiceItemERP = ItemInvoice.id_invoice_item;
                                        if (ItemInvoice.customer.EmptyIfNull().ToString().ToUpperInvariant().StartsWith("GDI ")) { ItemImportacao.IsGDI = true; } else { ItemImportacao.IsSC = true; }
                                    }

                                    // Validação do item nos cadastros de produtos
                                    if (ItemImportacao.IsValidItem())
                                    {
                                        // Validação com o Cadastro de Produtos Comex
                                        PNOficial = ItemImportacao.String_PN.EmptyIfNull().ToString();
                                        PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                                        PNCuringaOH = PNAuxiliar.Replace("0", "O");
                                        PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                                        List<gc_comex_produtos> ProdutosComexIdentificados = ListaComexProdutos.Where(c => (c.pn == PNOficial || c.pn_auxiliar == PNAuxiliar || c.pn_variacao1 == PNCuringaOH || c.pn_variacao2 == PNCuringaZERO)).ToList();
                                        if (ProdutosComexIdentificados.Count() > 1)
                                        {
                                            //aqui QtdProdutosComexMultiplosCadastros += 1;
                                            //aqui PnProdutosComexMultiplosCadastros += ItemImportacao.String_PN.EmptyIfNull().ToString() + ", ";
                                        }
                                        if (ProdutosComexIdentificados.Count() == 1)
                                        {
                                            ItemImportacao.IdComexProdutoERP = ProdutosComexIdentificados.FirstOrDefault().id_comex_produto;
                                        }
                                        ListaPlanilhaItens.Add(ItemImportacao);
                                    }
                                    else
                                    {
                                        ErroProcessamento = true;
                                        ErroArquivoXlxs = true;
                                        if (ItemImportacao.IsRowEmpty()) { MsgRetorno += " - Linha [" + (IndexRow).ToString() + "] não contém dados!" + "<br/>"; }
                                        else if (ItemImportacao.String_Descricao.IndexOf("SERIAL") > 0) { MsgRetorno += " - Linha [" + (IndexRow).ToString() + "] contém informações de SERIAL!" + "<br/>"; }
                                        else if (ItemImportacao.String_Descricao.IndexOf("PREFIXO") > 0) { MsgRetorno += " - Linha [" + (IndexRow).ToString() + "] contém informações de PREFIXO!" + "<br/>"; }
                                        else { MsgRetorno += " - Erro ao processar a linha [" + (IndexRow).ToString() + "]!" + "<br/>"; };
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += LibExceptions.getExceptionShortMessage(ex) + "<br/>";
                        }
                        finally
                        {
                            WorkBook.Dispose();
                        }
                    }

                    if (ErroProcessamento == false)
                    {
                        //////////   VERIFICAR CADASTRO DE NCM E UNIDADE DE MEDIDAS   //////////
                        foreach (CstModelComexItemImportacao ItemImportacao in ListaPlanilhaItens)
                        {
                            // Validar NCMs
                            if (ItemImportacao.String_NcmCodigo.EmptyIfNull().ToString().Trim().Length > 0)
                            {
                                g_produtos_ncm record_g_produtos_ncm = ListaProdutosNCM.Where(n => n.codigo_ncm == ItemImportacao.String_NcmCodigo).FirstOrDefault();
                                if (record_g_produtos_ncm == null)
                                {
                                    record_g_produtos_ncm = LibGDI.CadastrarProdutoNCM(db, ItemImportacao.String_NcmCodigo);
                                    ListaProdutosNCM.Add(record_g_produtos_ncm);
                                    QtdProdutosNCMCadastrados += 1;
                                };
                            }

                            // Validar Unidade de Medidas
                            if (ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString().Trim().Length > 0)
                            {
                                g_unidade_medida record_g_unidade_medida = ListaUnidadesMedidas.Where(u => u.codigo == ItemImportacao.String_UnidadeMedida).FirstOrDefault();
                                if (record_g_unidade_medida == null)
                                {
                                    record_g_unidade_medida = LibGDI.CadastrarUnidadeMedida(db, ItemImportacao.String_UnidadeMedida);
                                    ListaUnidadesMedidas.Add(record_g_unidade_medida);
                                    QtdProdutosUnidadesMedidasCadastradas += 1;
                                };
                            }
                        }



                        //////////   VALIDACAO DOS ITENS DAS INVOICES  //////////
                        String SqlItensInvoices = string.Empty;
                        SqlItensInvoices += " select * from gc_comex_invoices_itens  ";
                        SqlItensInvoices += " where id_importacao = " + CachePersister.userIdentity.IdGcComexImportacaoAtiva + " ";
                        SqlItensInvoices += " and ativo = 1 ";
                        List<gc_comex_invoices_itens> ListaComexInvoicesItens = db.gc_comex_invoices_itens.SqlQuery(SqlItensInvoices).ToList();
                        List<gc_comex_invoices_itens> ListaComexInvoicesItensAtualizar = new List<gc_comex_invoices_itens>();
                        foreach (gc_comex_invoices_itens RecordItemInvoice in ListaComexInvoicesItens)
                        {
                            if (RecordItemInvoice.id_produto <= 0)
                            {
                                gc_comex_produtos RecordComexProduto = ListaComexProdutos.Where(c => c.id_comex_produto == RecordItemInvoice.id_comex_produto).FirstOrDefault(); ;
                                if (RecordComexProduto != null)
                                {
                                    if (RecordComexProduto.id_produto >= 0)
                                    {
                                        RecordItemInvoice.id_produto = RecordComexProduto.id_produto;
                                        ListaComexInvoicesItensAtualizar.Add(RecordItemInvoice);

                                    }
                                    else
                                    {
                                        QtdItensInvoicesSemProduto += 1;
                                        PnItensInvoicesSemProduto += RecordItemInvoice.pn.EmptyIfNull().ToString() + ", ";
                                    }
                                }
                                else
                                {
                                    QtdItensInvoicesSemProduto += 1;
                                    PnItensInvoicesSemProduto += RecordItemInvoice.pn.EmptyIfNull().ToString() + ", ";
                                }
                            }
                        }
                        foreach (gc_comex_invoices_itens RecordItemInvoiceAtualizar in ListaComexInvoicesItensAtualizar)
                        {
                            RecordItemInvoiceAtualizar.datahora_alteracao = DataHoraAtual;
                            RecordItemInvoiceAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordItemInvoiceAtualizar).State = EntityState.Modified;
                        }
                        db.SaveChanges();





                        //////////   VALIDACAO PLANILHA DE ITENS X INVOICES   //////////
                        // Carregamento dos itens das invoices  
                        List<CstInvoiceItemValidacao> ListaItensInvoices = new List<CstInvoiceItemValidacao>();
                        var allInvoices = db.gc_comex_invoices.Select(i => new { i.id_importacao, i.id_invoice, i.ativo, i.invoice }).Where(i => i.ativo == true && i.id_importacao == CachePersister.userIdentity.IdGcComexImportacaoAtiva).ToList();
                        foreach (var RowInvoice in allInvoices)
                        {
                            var allItensInvoices = db.gc_comex_invoices_itens.Select(i => new { i.id_invoice_item, i.id_invoice, i.id_comex_produto, i.id_importacao, i.ativo, i.pn, i.description, i.traducao}).Where(i => i.ativo == true && i.id_invoice == RowInvoice.id_invoice).ToList();
                            foreach (var RowInvoiceItem in allItensInvoices)
                            {
                                CstInvoiceItemValidacao RowCstInvoiceItemValidacao = new CstInvoiceItemValidacao();
                                RowCstInvoiceItemValidacao.id_invoice_item = RowInvoiceItem.id_invoice_item;
                                RowCstInvoiceItemValidacao.id_comex_produto = RowInvoiceItem.id_comex_produto;
                                RowCstInvoiceItemValidacao.id_invoice = RowInvoiceItem.id_invoice;
                                RowCstInvoiceItemValidacao.id_importacao = RowInvoiceItem.id_importacao;
                                RowCstInvoiceItemValidacao.pn = RowInvoiceItem.pn;
                                RowCstInvoiceItemValidacao.pn_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(RowInvoiceItem.pn);
                                RowCstInvoiceItemValidacao.pn_variacao1 = RowCstInvoiceItemValidacao.pn_auxiliar.Replace("0", "O");
                                RowCstInvoiceItemValidacao.pn_variacao2 = RowCstInvoiceItemValidacao.pn_auxiliar.Replace("O", "0");

                                RowCstInvoiceItemValidacao.description = RowInvoiceItem.description;
                                RowCstInvoiceItemValidacao.traducao = RowInvoiceItem.traducao;
                                RowCstInvoiceItemValidacao.invoice_nome = RowInvoice.invoice;
                                RowCstInvoiceItemValidacao.validado_planilha = false;
                                ListaItensInvoices.Add(RowCstInvoiceItemValidacao);
                            }
                        }

                        // Validar todos os itens das invoices nas planilhas
                        // CADASTRO DE PRODUTOS - 12
                        foreach (CstInvoiceItemValidacao RowItemInvoices in ListaItensInvoices)
                        {
                            PNOficial = RowItemInvoices.pn.EmptyIfNull().ToString();
                            PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                            PNCuringaOH = PNAuxiliar.Replace("0", "O");
                            PNCuringaZERO = PNAuxiliar.Replace("O", "0");
                            CstModelComexItemImportacao ItemPlanilha = ListaPlanilhaItens.Where(i => i.String_PN == RowItemInvoices.pn).FirstOrDefault();
                            try { if (ItemPlanilha == null) { ItemPlanilha = ListaPlanilhaItens.Where(p => p.String_PN_Auxiliar == PNAuxiliar || p.String_PN_Variacao1 == PNCuringaOH || p.String_PN_Variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar
                            if (ItemPlanilha == null)
                            {
                                QtdItensSomenteInvoices += 1;
                                PnItensSomenteInvoices += RowItemInvoices.pn.EmptyIfNull().ToString() + ", ";
                            }
                        }

                        // Validar todos os itens da planilha nas invoices
                        foreach (CstModelComexItemImportacao ItemPlanilhaItens in ListaPlanilhaItens)
                        {
                            CstInvoiceItemValidacao RowItemInvoice = ListaItensInvoices.Where(i => i.pn == ItemPlanilhaItens.String_PN).FirstOrDefault();
                            if (RowItemInvoice != null)
                            {
                                ListaItensInvoices.Where(i => i.pn == ItemPlanilhaItens.String_PN).FirstOrDefault().validado_planilha = true;
                            }
                            else
                            {
                                QtdItensSomentePlanilha += 1;
                                PnItensSomentePlanilha += ItemPlanilhaItens.String_PN + ", ";
                            }
                        }

                        // Validações e Totalizadores
                        TotalizadorFobReais = 0;
                        TotalizadorFobDollar = 0;
                        TotalizadorFrete = 0;
                        TotalizadorPesoLiquido = 0;
                        TotalizadorPesoBruto = 0;
                        foreach (CstModelComexItemImportacao ItemImportacao in ListaPlanilhaItens)
                        {
                            TotalizadorFobReais += ItemImportacao.Decimal_ValorFob;
                            TotalizadorFrete += ItemImportacao.Decimal_ValorFrete;
                            TotalizadorPesoLiquido += ItemImportacao.Decimal_PesoLiquido;
                            TotalizadorPesoBruto += ItemImportacao.Decimal_PesoBruto;

                            // CADASTRO DE PRODUTOS - 11
                            // Validar produtos não localizados no cadastro
                            PNOficial = ItemImportacao.String_PN.EmptyIfNull().ToString();
                            PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                            PNCuringaOH = PNAuxiliar.Replace("0", "O");
                            PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                            gc_comex_produtos ProdutoComex = ListaComexProdutos.Where(p => p.pn == PNOficial).FirstOrDefault();
                            try { if (ProdutoComex == null) { ProdutoComex = ListaComexProdutos.Where(p => p.pn_auxiliar == PNAuxiliar || p.pn_variacao1 == PNCuringaOH || p.pn_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };
                            if (ProdutoComex == null)
                            {
                                QtdProdutosComexNaoCadastrados += 1;
                                PnProdutosComexNaoCadastrados += ItemImportacao.String_PN + ",";
                            }
                        }
                        TotalizadorFobDollar = (TotalizadorFobReais / TotalizadorTaxaDollar);

                        if ((QtdItensSomentePlanilha + QtdItensSomenteInvoices + QtdProdutosComexNaoCadastrados + QtdItensInvoicesSemProduto) > 0)
                        {
                            ErroProcessamento = true;
                            if (QtdItensSomentePlanilha > 0) { MsgRetorno += " - " + QtdItensSomentePlanilha.ToString() + " Produtos encontrados somente na Planilha de Itens!" + "<br/>" + PnItensSomentePlanilha + "<br/>"; };
                            if (QtdItensSomenteInvoices > 0) { MsgRetorno += " - " + QtdItensSomenteInvoices.ToString() + " Produtos encontrados somente nas Invoices!" + "<br/>" + PnItensSomenteInvoices + "<br/>"; };
                            if (QtdProdutosComexNaoCadastrados > 0) { MsgRetorno += " - " + QtdProdutosComexNaoCadastrados.ToString() + " Produtos Comex Não Cadastrados!" + "<br/>" + PnProdutosComexNaoCadastrados + "<br/>"; };
                            if (QtdItensInvoicesSemProduto > 0) { MsgRetorno += " - " + QtdItensInvoicesSemProduto.ToString() + " Itens das invoices não relacionados à produtos !" + "<br/>" + PnItensInvoicesSemProduto + "<br/>"; };
                        }
                        else
                        {
                            // Registro do arquivo XLSX
                            gc_comex_importacoes_files_xls record_gc_comex_importacoes_files_xls = new Db.gc_comex_importacoes_files_xls();
                            record_gc_comex_importacoes_files_xls.id_importacao = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                            record_gc_comex_importacoes_files_xls.ativo = true;
                            record_gc_comex_importacoes_files_xls.di_numero = IdentificadorDI;
                            record_gc_comex_importacoes_files_xls.filename = fileNameOrigem;
                            record_gc_comex_importacoes_files_xls.qtd_registros = QtdRegistrosArquivo;
                            record_gc_comex_importacoes_files_xls.qtd_itens = Decimal.ToInt32(Decimal.Truncate(QtdItensArquivo));
                            record_gc_comex_importacoes_files_xls.valor_total = ValorTotalArquivo;
                            record_gc_comex_importacoes_files_xls.id_coligada = 1;
                            record_gc_comex_importacoes_files_xls.id_filial = 1;
                            record_gc_comex_importacoes_files_xls.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            record_gc_comex_importacoes_files_xls.datahora_cadastro = DataHoraAtual;
                            db.gc_comex_importacoes_files_xls.Add(record_gc_comex_importacoes_files_xls);

                            // NF Despachante Número
                            String NfDespachanteNumero = String.Empty;
                            foreach (CstModelComexItemImportacao ItemImportacao in ListaPlanilhaItens)
                            {
                                if (ItemImportacao.String_NfNumero.EmptyIfNull().ToString().Trim().Length > 0)
                                {
                                    if (NfDespachanteNumero.IndexOf("|" + ItemImportacao.String_NfNumero.EmptyIfNull().ToString().Trim() + "|") < 0)
                                    {
                                        if (NfDespachanteNumero.EmptyIfNull().ToString().Trim().Length == 0)
                                        {
                                            NfDespachanteNumero = "|" + ItemImportacao.String_NfNumero.EmptyIfNull().ToString().Trim() + "|";
                                        }
                                        else
                                        {
                                            NfDespachanteNumero += ItemImportacao.String_NfNumero.EmptyIfNull().ToString().Trim() + "|";
                                        }
                                    }
                                };
                            }



                            foreach (CstModelComexItemImportacao ItemImportacao in ListaPlanilhaItens)
                            {
                                gc_comex_produtos ProdutoComex = null;
                                g_produtos ProdutoGDI = null;

                                // ********** ATUALIZAÇÃO DO PRODUTO COMEX ********** //
                                // CADASTRO DE PRODUTOS - 10
                                bool ProdutoComexAtualizado = false;

                                PNOficial = ItemImportacao.String_PN.EmptyIfNull().ToString();
                                PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                                PNCuringaOH = PNAuxiliar.Replace("0", "O");
                                PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                                ProdutoComex = ListaComexProdutos.Where(p => p.pn == PNOficial).FirstOrDefault();
                                try { if (ProdutoComex == null) { ProdutoComex = ListaComexProdutos.Where(p => p.pn_auxiliar == PNAuxiliar || p.pn_variacao1 == PNCuringaOH || p.pn_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };
                                if (ProdutoComex != null)
                                {
                                    // Atualizar NCM
                                    if (ItemImportacao.String_NcmCodigo.EmptyIfNull().ToString().Length > 0)
                                    {
                                        if (ItemImportacao.String_NcmCodigo.EmptyIfNull().ToString() != ProdutoComex.ncm.EmptyIfNull().ToString())
                                        {
                                            string LogAlteracao = "Atualização NCM: " + ProdutoComex.ncm.EmptyIfNull().ToString() + " > " + ItemImportacao.String_NcmCodigo.EmptyIfNull().ToString();
                                            ProdutoComex.ncm = ItemImportacao.String_NcmCodigo.EmptyIfNull().ToString();
                                            ProdutoComexAtualizado = true;
                                            LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, LogAlteracao);
                                            //ListaComexProdutos.Where(p => p.id_comex_produto == ProdutoComex.id_comex_produto).FirstOrDefault().ncm = ProdutoComex.ncm.EmptyIfNull().ToString();
                                        }
                                    }

                                    // Atualizar UNIDADE DE MEDIDA
                                    if (ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString().Length > 0)
                                    {
                                        if (ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString() != ProdutoComex.unidade_medida.EmptyIfNull().ToString())
                                        {
                                            string LogAlteracao = "Atualização Unidade Medida: " + ProdutoComex.unidade_medida.EmptyIfNull().ToString() + " > " + ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString();
                                            ProdutoComex.unidade_medida = ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString();
                                            ProdutoComexAtualizado = true;
                                            LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, LogAlteracao);
                                            ListaComexProdutos.Where(p => p.id_comex_produto == ProdutoComex.id_comex_produto).FirstOrDefault().unidade_medida = ProdutoComex.unidade_medida.EmptyIfNull().ToString();
                                        }
                                    }

                                    // Atualizar TRADUCAO
                                    if (ItemImportacao.String_Descricao.EmptyIfNull().ToString().Length > 0)
                                    {
                                        if (ItemImportacao.String_Descricao.EmptyIfNull().ToString() != ProdutoComex.traducao.EmptyIfNull().ToString())
                                        {
                                            string LogAlteracao = "Atualização Tradução: " + ProdutoComex.traducao.EmptyIfNull().ToString() + " > " + ItemImportacao.String_Descricao.EmptyIfNull().ToString();
                                            ProdutoComex.traducao = ItemImportacao.String_Descricao.EmptyIfNull().ToString();
                                            ProdutoComexAtualizado = true;
                                            LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, LogAlteracao);
                                            ListaComexProdutos.Where(p => p.id_comex_produto == ProdutoComex.id_comex_produto).FirstOrDefault().traducao = ProdutoComex.traducao.EmptyIfNull().ToString();
                                        }
                                    }

                                    // Verificar se o produto associado ao comex está válido
                                    if (ProdutoComex.id_produto > 0)
                                    {
                                        ProdutoGDI = ListaProdutosGDI.Where(p => p.ativo == true & p.id_produto == ProdutoComex.id_produto).FirstOrDefault();
                                        if (ProdutoGDI == null)
                                        {
                                            ProdutoComex.id_produto = 0;
                                            ListaComexProdutos.Where(p => p.id_comex_produto == ProdutoComex.id_comex_produto).FirstOrDefault().id_produto = 0;
                                        }
                                    }

                                    // CADASTRO DE PRODUTOS - 9
                                    if (ProdutoComex.id_produto == 0)
                                    {

                                        PNOficial = ItemImportacao.String_PN.EmptyIfNull().ToString();
                                        PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                                        PNCuringaOH = PNAuxiliar.Replace("0", "O");
                                        PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                                        ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                                        try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar
                                        if (ProdutoGDI != null)
                                        {
                                            ProdutoComex.id_produto = ProdutoGDI.id_produto;
                                            ProdutoComex.datahora_alteracao = DataHoraAtual;
                                            ProdutoComex.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                            db.Entry(ProdutoComex).State = EntityState.Modified;
                                            ListaComexProdutos.Where(c => c.id_comex_produto == ProdutoComex.id_comex_produto).FirstOrDefault().id_produto = ProdutoGDI.id_produto;
                                            ProdutoComex.item_cadastro_novo = false;
                                            ProdutoComex.item_cadastro_atualizar = false;
                                            ProdutoComexAtualizado = true;
                                            LibAudit.SaveAudit(db, false, "gc_comex_produtos", ProdutoComex.id_comex_produto, "Vinculação ao Produto Comex id: " + ProdutoComex.id_produto.ToString());
                                        }
                                        else
                                        {
                                            // Novos produtos a cadastrar
                                            ProdutoComex.id_produto = 0;
                                            ProdutoComex.item_cadastro_novo = true;
                                            ProdutoComex.item_cadastro_atualizar = false;
                                            ProdutoComexAtualizado = true;
                                            ListaComexProdutos.Where(c => c.id_comex_produto == ProdutoComex.id_comex_produto).FirstOrDefault().id_produto = 0;
                                            QtdProdutosERPCadastrar += 1;
                                        }
                                    }

                                    if (ProdutoComexAtualizado == true)
                                    {
                                        ProdutoComex.datahora_alteracao = DataHoraAtual;
                                        ProdutoComex.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                        db.Entry(ProdutoComex).State = EntityState.Modified;
                                        db.SaveChanges();
                                    }
                                }


                                // Atualização do produto GDI
                                if (ProdutoGDI != null)
                                {
                                    bool ProdutoGDIAtualizado = false;
                                    String NcmProdutoGDI = "0";
                                    String UnidadeMedidaProdutoGDI = "0";
                                    int IdNcmProdutoImportado = ListaProdutosNCM.Where(n => n.codigo_ncm == ItemImportacao.String_NcmCodigo).FirstOrDefault().id_produto_ncm;
                                    if (ProdutoGDI.id_produto_ncm > 0) { NcmProdutoGDI = ListaProdutosNCM.Where(n => n.id_produto_ncm == ProdutoGDI.id_produto_ncm).FirstOrDefault().codigo_ncm.EmptyIfNull().ToString(); };
                                    int IdUnidadeMedidaProdutoImportado = ListaUnidadesMedidas.Where(u => u.codigo == ItemImportacao.String_UnidadeMedida).FirstOrDefault().id_unidade_medida;
                                    if (ProdutoGDI.id_unidade_medida_compra > 0) { UnidadeMedidaProdutoGDI = ListaUnidadesMedidas.Where(u => u.id_unidade_medida == ProdutoGDI.id_unidade_medida_compra).FirstOrDefault().codigo.EmptyIfNull().ToString(); };

                                    // Verificar o NCM do Produto GDI
                                    if ((ItemImportacao.String_NcmCodigo.EmptyIfNull().ToString().Length > 0) && (ItemImportacao.String_NcmCodigo.EmptyIfNull().ToString() != NcmProdutoGDI))
                                    {
                                        ProdutoGDI.id_produto_ncm = IdNcmProdutoImportado;
                                        LibAudit.SaveAudit(db, false,"g_produtos", ProdutoGDI.id_produto, "Atualização NCM: " + NcmProdutoGDI + " > " + ItemImportacao.String_NcmCodigo.EmptyIfNull().ToString());
                                        ProdutoGDIAtualizado = true;
                                    }

                                    // Atualizar UNIDADE DE MEDIDA
                                    if ((ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString().Length > 0) && (ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString() != UnidadeMedidaProdutoGDI))
                                    {
                                        ProdutoGDI.id_unidade_medida_compra = IdUnidadeMedidaProdutoImportado;
                                        ProdutoGDI.id_unidade_medida_venda = IdUnidadeMedidaProdutoImportado;
                                        LibAudit.SaveAudit(db, false, "g_produtos", ProdutoGDI.id_produto, "Atualização Unidade Medida: " + UnidadeMedidaProdutoGDI + " > " + ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString());
                                        ProdutoGDIAtualizado = true;
                                    }

                                    // Atualizar TRADUCAO
                                    if (ItemImportacao.String_Descricao.EmptyIfNull().ToString().Length > 0)
                                    {
                                        if (ItemImportacao.String_Descricao.EmptyIfNull().ToString() != ProdutoGDI.nome.EmptyIfNull().ToString())
                                        {
                                            if (ProdutoGDI.importado == false)
                                            {
                                                ProdutoGDI.nome = ItemImportacao.String_Descricao.EmptyIfNull().ToString();
                                                ProdutoGDI.descricao = ItemImportacao.String_Descricao.EmptyIfNull().ToString();
                                                LibAudit.SaveAudit(db, false, "g_produtos", ProdutoGDI.id_produto, "Atualização Tradução: " + ProdutoGDI.nome.EmptyIfNull().ToString() + " > " + ItemImportacao.String_Descricao.EmptyIfNull().ToString());
                                                ProdutoGDIAtualizado = true;
                                                ProdutoComex.item_cadastro_atualizar = false;
                                                ProdutoComex.datahora_alteracao = DataHoraAtual;
                                                ProdutoComex.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                                db.Entry(ProdutoComex).State = EntityState.Modified;
                                            }
                                            else
                                            {
                                                ProdutoComex.item_cadastro_novo = false;
                                                ProdutoComex.item_cadastro_atualizar = true;
                                                ProdutoComex.datahora_alteracao = DataHoraAtual;
                                                ProdutoComex.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                                db.Entry(ProdutoComex).State = EntityState.Modified;
                                                QtdProdutosERPNomesAtualizar += 1;
                                            }
                                        }
                                    }

                                    if (ProdutoGDI.importado == false)
                                    {
                                        if ((ProdutoGDI.id_produto_ncm > 0) && (ProdutoGDI.id_unidade_medida_venda > 0))
                                        {
                                            ProdutoGDI.importado = true;
                                            ProdutoGDIAtualizado = true;
                                            LibAudit.SaveAudit(db, false, "g_produtos", ProdutoGDI.id_produto, "Atualização Status: Produto temporário > Produto importado");
                                        }
                                    }

                                    if (ProdutoGDIAtualizado == true)
                                    {
                                        ProdutoGDI.datahora_alteracao = DataHoraAtual;
                                        ProdutoGDI.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                        db.Entry(ProdutoGDI).State = EntityState.Modified;
                                        db.SaveChanges();
                                    }
                                }


                                gc_comex_importacoes_itens novo_item_importacao = new Db.gc_comex_importacoes_itens();
                                novo_item_importacao.ativo = true;
                                novo_item_importacao.id_importacao = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                                novo_item_importacao.id_importacao_file_xls = record_gc_comex_importacoes_files_xls.id_importacao_file_xls;
                                novo_item_importacao.id_comex_produto = ProdutoComex.id_comex_produto;
                                novo_item_importacao.id_produto = ProdutoComex.id_produto;
                                novo_item_importacao.id_invoice_item = ItemImportacao.IdInvoiceItemERP;
                                novo_item_importacao.nf_numero = ItemImportacao.String_NfNumero;
                                novo_item_importacao.li_numero = ItemImportacao.String_LiNumero;
                                novo_item_importacao.nf_despachante_numero = ItemImportacao.String_NfNumero;
                                novo_item_importacao.di_numero = ItemImportacao.String_DiNumero;
                                novo_item_importacao.pn = ItemImportacao.String_PN;
                                novo_item_importacao.pn_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemImportacao.String_PN);
                                novo_item_importacao.pn_variacao1 = ItemImportacao.String_PN_Auxiliar.Replace("0", "O");
                                novo_item_importacao.pn_variacao2 = ItemImportacao.String_PN_Auxiliar.Replace("O", "0");
                                novo_item_importacao.descricao = ItemImportacao.String_Descricao;
                                novo_item_importacao.di_adicao_numero = ItemImportacao.Int_DiAdicaoNumero;
                                novo_item_importacao.di_adicao_sequencial = ItemImportacao.Int_DiAdicaoSequencial;
                                novo_item_importacao.codigo_ncm = ItemImportacao.String_NcmCodigo;
                                novo_item_importacao.valor_fob_entrada = ItemImportacao.Decimal_ValorFob;
                                novo_item_importacao.valor_frete = ItemImportacao.Decimal_ValorFrete;
                                novo_item_importacao.quantidade = ItemImportacao.Decimal_Quantidade;
                                novo_item_importacao.valor_unit = ItemImportacao.Decimal_ValorUnit;
                                novo_item_importacao.valor_total = Decimal.Round((novo_item_importacao.quantidade * novo_item_importacao.valor_unit), 2);
                                novo_item_importacao.un = ItemImportacao.String_UnidadeMedida;
                                novo_item_importacao.ii_percentual = ItemImportacao.Decimal_IiPercentual;
                                novo_item_importacao.ii_valor = ItemImportacao.Decimal_IiValor;
                                novo_item_importacao.ipi_bc = ItemImportacao.Decimal_IpiBaseCalculo;
                                novo_item_importacao.ipi_percentual = ItemImportacao.Decimal_IpiPercentual;
                                novo_item_importacao.ipi_valor = ItemImportacao.Decimal_IpiValor;
                                novo_item_importacao.icms_bc = ItemImportacao.Decimal_IcmsBaseCalculo;
                                novo_item_importacao.icms_bc_reduzida = ItemImportacao.Decimal_IcmsBaseReduzida;
                                novo_item_importacao.icms_percentual = ItemImportacao.Decimal_IcmsPercentual;
                                novo_item_importacao.icms_valor = ItemImportacao.Decimal_IcmsValor;
                                novo_item_importacao.pis_bc = ItemImportacao.Decimal_PisBaseCalculo;
                                novo_item_importacao.pis_percentual = ItemImportacao.Decimal_PisPercentual;
                                novo_item_importacao.pis_valor = ItemImportacao.Decimal_PisValor;
                                novo_item_importacao.cofins_bc = ItemImportacao.Decimal_CofinsBaseCalculo;
                                novo_item_importacao.cofins_percentual = ItemImportacao.Decimal_CofinsPercentual;
                                novo_item_importacao.cofins_valor = ItemImportacao.Decimal_CofinsValor;
                                novo_item_importacao.ibs_cbs_vbc = ItemImportacao.Decimal_IbsCbsBaseCalculo;
                                novo_item_importacao.ibs_pibs = ItemImportacao.Decimal_IbsPercentual;
                                novo_item_importacao.ibs_vibs = ItemImportacao.Decimal_IbsValor;
                                novo_item_importacao.cbs_pcbs = ItemImportacao.Decimal_CbsPercentual;
                                novo_item_importacao.cbs_vcbs = ItemImportacao.Decimal_CbsValor;
                                novo_item_importacao.siscomex_valor = ItemImportacao.Decimal_SiscomexValor;
                                novo_item_importacao.sda_valor = ItemImportacao.Decimal_SdaValor;
                                novo_item_importacao.marinha_valor = ItemImportacao.Decimal_MarinhaValor;
                                novo_item_importacao.peso_liquido = ItemImportacao.Decimal_PesoLiquido;
                                novo_item_importacao.peso_bruto = ItemImportacao.Decimal_PesoBruto;
                                novo_item_importacao.is_gdi = ItemImportacao.IsGDI;
                                novo_item_importacao.is_sc = ItemImportacao.IsSC;

                                // Dados Calculados
                                novo_item_importacao.valor_fob_percentual = ((novo_item_importacao.valor_fob_entrada * 100) / TotalizadorFobReais);
                                novo_item_importacao.valor_frete_percentual = ((novo_item_importacao.valor_frete * 100) / TotalizadorFrete);
                                novo_item_importacao.peso_liquido_percentual = ((novo_item_importacao.peso_liquido * 100) / TotalizadorPesoLiquido);
                                novo_item_importacao.peso_bruto_percentual = ((novo_item_importacao.peso_bruto * 100) / TotalizadorPesoBruto);
                                novo_item_importacao.valor_fob_reais_unidade = (novo_item_importacao.valor_fob_entrada / novo_item_importacao.quantidade);
                                novo_item_importacao.valor_fob_dollar_unidade = (novo_item_importacao.valor_fob_reais_unidade / record_gc_comex_importacoes.di_cambio);
                                novo_item_importacao.valor_custo_dollar_unidade = 0;
                                novo_item_importacao.valor_custo_reais_unidade = 0;
                                novo_item_importacao.id_coligada = 1;
                                novo_item_importacao.id_filial = 1;
                                novo_item_importacao.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                novo_item_importacao.datahora_cadastro = DataHoraAtual;
                                db.gc_comex_importacoes_itens.Add(novo_item_importacao);

                                // Totalizadores da Importação
                                FechamentoPesoLiquidoImportacao += novo_item_importacao.peso_liquido;
                                FechamentoPesoBrutoImportacao += novo_item_importacao.peso_bruto;
                                FechamentoTotalFobImportacaoReais += novo_item_importacao.valor_fob_entrada;
                                FechamentoValorFrete += novo_item_importacao.valor_frete;
                                FechamentoValorTotalImportacao += novo_item_importacao.valor_total;
                                FechamentoValorTotalII += novo_item_importacao.ii_valor;
                                FechamentoValorTotalIPI += novo_item_importacao.ipi_valor;
                                FechamentoValorTotalICMS += novo_item_importacao.icms_valor;
                                FechamentoValorTotalPIS += novo_item_importacao.pis_valor;
                                FechamentoValorTotalCofins += novo_item_importacao.cofins_valor;
                                FechamentoValorTotalSiscomex += novo_item_importacao.siscomex_valor;
                                FechamentoValorTotalSDA += novo_item_importacao.sda_valor;
                                FechamentoValorTotalIbs += novo_item_importacao.ibs_vibs;
                                FechamentoValorTotalCbs += novo_item_importacao.cbs_vcbs;
                                FechamentoValorTotalMarinha += novo_item_importacao.marinha_valor;

                                if (novo_item_importacao.is_gdi == true)
                                {
                                    FechamentoPesoLiquidoGDI +=  novo_item_importacao.peso_liquido;
                                    FechamentoPesoBrutoGDI += novo_item_importacao.peso_bruto;
                                    FechamentoTotalFobGDI += novo_item_importacao.valor_fob_entrada;
                                }
                                else 
                                {
                                    FechamentoPesoLiquidoSC += novo_item_importacao.peso_liquido;
                                    FechamentoPesoBrutoSC += novo_item_importacao.peso_bruto;
                                    FechamentoTotalFobSC += novo_item_importacao.valor_fob_entrada;
                                }
                            }

                            if (record_gc_comex_importacoes != null)
                            {
                                record_gc_comex_importacoes.nf_despachante_numero = NfDespachanteNumero;
                                record_gc_comex_importacoes.di_valor_total = FechamentoValorTotalImportacao;
                                record_gc_comex_importacoes.peso_bruto = FechamentoPesoBrutoImportacao;
                                record_gc_comex_importacoes.peso_liquido = FechamentoPesoLiquidoImportacao;
                                record_gc_comex_importacoes.di_numero = IdentificadorDI;
                                record_gc_comex_importacoes.percentual_valor_sc = ((FechamentoTotalFobSC * 100) / (FechamentoTotalFobImportacaoReais));
                                record_gc_comex_importacoes.percentual_valor_gdi = 100 - record_gc_comex_importacoes.percentual_valor_sc;
                                record_gc_comex_importacoes.percentual_peso_liquido_sc = ((FechamentoPesoLiquidoSC * 100) / (FechamentoPesoLiquidoImportacao));
                                record_gc_comex_importacoes.percentual_peso_liquido_gdi = 100 - record_gc_comex_importacoes.percentual_peso_liquido_sc;
                                record_gc_comex_importacoes.percentual_peso_bruto_sc = ((FechamentoPesoBrutoSC * 100) / (FechamentoPesoBrutoImportacao));
                                record_gc_comex_importacoes.percentual_peso_bruto_gdi = 100 - record_gc_comex_importacoes.percentual_peso_bruto_sc;
                                record_gc_comex_importacoes.despesas_fob = (FechamentoTotalFobImportacaoReais / record_gc_comex_importacoes.di_cambio);
                                record_gc_comex_importacoes.despesas_fob_ajustado = record_gc_comex_importacoes.despesas_fob;
                                record_gc_comex_importacoes.despesas_ii = FechamentoValorTotalII;
                                record_gc_comex_importacoes.despesas_ipi = FechamentoValorTotalIPI;
                                record_gc_comex_importacoes.despesas_icms = FechamentoValorTotalICMS;
                                record_gc_comex_importacoes.despesas_pis = FechamentoValorTotalPIS;
                                record_gc_comex_importacoes.despesas_cofins = FechamentoValorTotalCofins;
                                record_gc_comex_importacoes.despesas_siscomex = FechamentoValorTotalSiscomex;
                                record_gc_comex_importacoes.despesas_sda = FechamentoValorTotalSDA;
                                record_gc_comex_importacoes.despesas_ibs = FechamentoValorTotalIbs;
                                record_gc_comex_importacoes.despesas_cbs = FechamentoValorTotalCbs;
                                record_gc_comex_importacoes.despesas_marinha_mercante = FechamentoValorTotalMarinha;
                                record_gc_comex_importacoes.despesas_frete_internacional = (FechamentoValorFrete / record_gc_comex_importacoes.di_cambio);
                                record_gc_comex_importacoes.invoices_finalizadas = true;
                                record_gc_comex_importacoes.datahora_alteracao = DataHoraAtual;



                                if (record_gc_comex_importacoes.di_cambio > 1)
                                {
                                    FechamentoTotalDespesasDesembaracoReais = 0;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_ipi;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_ii;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_pis;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_cofins;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_siscomex;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_icms;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_ibs;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_cbs;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_csll;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_sda;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_marinha_mercante;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_armazenagem_primaria;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_armazenagem_secundaria;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_transp_rodo_remocao;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_armazenagem_infraero;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_despachante;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_taxa_expediente_santos;
                                    FechamentoTotalDespesasDesembaracoReais += record_gc_comex_importacoes.despesas_taxa_capatazia;
                                    record_gc_comex_importacoes.total_custo = record_gc_comex_importacoes.despesas_fob_ajustado + (FechamentoTotalDespesasDesembaracoReais / record_gc_comex_importacoes.di_cambio);
                                    record_gc_comex_importacoes.percentual_custo_fob = ((record_gc_comex_importacoes.total_custo * 100) / record_gc_comex_importacoes.despesas_fob_ajustado);
                                }

                                record_gc_comex_importacoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(record_gc_comex_importacoes).State = EntityState.Modified;
                            }
                            db.SaveChanges();

                            if (fileNameOrigem.Trim().ToUpperInvariant().IndexOf("(NOGED)") == -1) // Verificar se é para enviar ao GED
                            {
                                // GED - XML
                                // Verificar se há outro XML GED para a mesma Importação
                                int VersaoGedXML = 0;
                                String DescricaoGedXML = String.Empty;
                                DescricaoGedXML = "Planilha de Itens [DI: " + IdentificadorDI + "] (xlsx)";
                                IQueryable<ged_arquivos> listaGedXML = db.ged_arquivos.Where(g => (g.ativo == true) && (g.descricao == DescricaoGedXML));
                                if (listaGedXML.Count() > 0)
                                {
                                    foreach (ged_arquivos itemGedXML in listaGedXML)
                                    {
                                        if (itemGedXML.versao > VersaoGedXML) { VersaoGedXML = itemGedXML.versao; };
                                        itemGedXML.ativo = false;
                                        itemGedXML.datahora_alteracao = DataHoraAtual;
                                        itemGedXML.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                        db.Entry(itemGedXML).State = EntityState.Modified;
                                    }
                                }
                                // Realizar o upload do XML para o GED
                                CstUploadGed record_cstUploadGedXML = new CstUploadGed();
                                record_cstUploadGedXML.id_arquivo = 0;
                                record_cstUploadGedXML.id_arquivo_tipo = 13; // Comex - Importações
                                record_cstUploadGedXML.filesource = filesource;
                                record_cstUploadGedXML.id_comex_importacao = CachePersister.userIdentity.IdGcComexImportacaoAtiva; ;
                                record_cstUploadGedXML.descricao = DescricaoGedXML;
                                record_cstUploadGedXML.observacao = DescricaoGedXML + ", processado em " + DataHoraAtual.ToString("dd/MM/yyyy HH:mm") + " por " + CachePersister.userIdentity.Username;
                                record_cstUploadGedXML.versao = VersaoGedXML + 1;
                                var ResultUploadFileXML = new GedController().ServiceUploadFileGed(record_cstUploadGedXML);
                                db.SaveChanges();
                            }

                            Logs = string.Empty;

                            int QtdProdutosFobAtualizados = AtualizarFobProdutos(CachePersister.userIdentity.IdGcComexImportacaoAtiva);

                            MsgRetorno += "DI [ " + IdentificadorDI + " ] <b>PROCESSADA</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                            MsgRetorno += "<b>Qtd. Itens</b> " + ListaPlanilhaItens.Count().ToString() + "<br/>";
                            MsgRetorno += "<b>R$ Total</b>: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", FechamentoValorTotalImportacao).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + "<br/><br/>";
                            if (QtdProdutosNCMCadastrados > 0) { MsgRetorno += "NCMs Cadastrados: " + QtdProdutosNCMCadastrados.ToString() + "<br/>"; };
                            if (QtdProdutosUnidadesMedidasCadastradas > 0) { MsgRetorno += "Unidades de Medidas Cadastradas: " + QtdProdutosUnidadesMedidasCadastradas.ToString() + "<br/>"; };
                            if (QtdProdutosNCMAtualizados > 0) { MsgRetorno += "NCMs Atualizados: " + QtdProdutosNCMAtualizados.ToString() + "<br/>"; };
                            if (qtdProdutosUnidadeMedidaAtualizadas > 0) { MsgRetorno += "Unidades de Medidas Atualizadas: " + qtdProdutosUnidadeMedidaAtualizadas.ToString() + "<br/>"; };
                            if (QtdProdutosFobAtualizados > 0) { MsgRetorno += "Produtos Atualizados (Valor Fob): " + QtdProdutosFobAtualizados.ToString() + "<br/>"; };

                            if (MsgRetorno.EmptyIfNull().ToString() != String.Empty) { MsgRetorno += "<br/><br/>"; }
                            if (QtdProdutosNCMCadastrados > 0) { MsgRetorno += QtdProdutosNCMCadastrados.ToString() + LibStringFormat.GetTabHtml(1) + "Novos NCMs Cadastrados" + "<br/>"; };
                            if (QtdProdutosUnidadesMedidasCadastradas > 0) { MsgRetorno += QtdProdutosUnidadesMedidasCadastradas.ToString() + LibStringFormat.GetTabHtml(1) + "Novas Unidades de Medidas Cadastradas" + "<br/>"; };
                            if (QtdProdutosNCMAtualizados > 0) { MsgRetorno += QtdProdutosNCMAtualizados.ToString() + LibStringFormat.GetTabHtml(1) + "NCMs Atualizados" + "<br/>"; };
                            if (qtdProdutosUnidadeMedidaAtualizadas > 0) { MsgRetorno += qtdProdutosUnidadeMedidaAtualizadas.ToString() + LibStringFormat.GetTabHtml(1) + "Unidades de Medida Atualizadas" + "<br/>"; };


                            if ((QtdProdutosERPCadastrar > 0) || (QtdProdutosERPNomesAtualizar > 0)) 
                            { 
                                MsgRetorno += "<br/><b>" + " - - - - - A T E N Ç Ã O - - - - - " + "</b><br/>";
                                if (QtdProdutosERPCadastrar > 0) { MsgRetorno += QtdProdutosERPCadastrar.ToString() + LibStringFormat.GetTabHtml(1) + "Novos produtos Comex a serem validados!" + "<br/>"; };
                                if (QtdProdutosERPNomesAtualizar > 0) { MsgRetorno += QtdProdutosERPNomesAtualizar.ToString() + LibStringFormat.GetTabHtml(1) + "Novas traduções a serem validadas!" + "<br/>"; };
                                MsgRetorno += "Foram identificados os itens acima a serem validados, execute o processo de Cadastro/Atualização de Novos Itens do ERP GDI!" + "<br/>";
                            };


                            Logs += "Upload Planilha Itens [";
                            Logs += "Filename: " + fileNameOrigem + " | ";
                            Logs += "DI: " + IdentificadorDI + " | ";
                            Logs += "Qtd. Itens: " + ListaPlanilhaItens.Count().ToString() + " | ";
                            Logs += "R$ Total: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", FechamentoValorTotalImportacao).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " | ";
                            Logs += "NCMs Cadastrados: " + QtdProdutosNCMCadastrados.ToString() + " | ";
                            Logs += "Unidades de Medidas Cadastradas: " + QtdProdutosUnidadesMedidasCadastradas.ToString() + " | ";
                            Logs += "NCMs Atualizados: " + QtdProdutosNCMAtualizados.ToString() + " | ";
                            Logs += "Unidades de Medidas Atualizadas: " + qtdProdutosUnidadeMedidaAtualizadas.ToString() + " | ";
                            Logs += "Produtos Atualizados (Valor Fob): " + QtdProdutosFobAtualizados.ToString() + "]";
                            LibAudit.SaveAudit(db, true,"gc_comex_importacoes", CachePersister.userIdentity.IdGcComexImportacaoAtiva, Logs);

                            try { Clipboard.SetText(MsgRetorno); } catch (Exception) { };
                            Processado = true;
                        }
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    Processado = false;
                    MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
                    try { Clipboard.SetText(MsgRetorno); } catch (Exception) { };
                }
                catch (Exception e)
                {
                    Processado = false;
                    MsgRetorno = LibExceptions.getExceptionShortMessage(e);
                    try { Clipboard.SetText(MsgRetorno); } catch (Exception) { };
                }
                if (ErroArquivoXlxs == true)
                {
                    MsgRetorno += "<br/>" + "Verifique o conteúdo do arquivo xlsx!";
                    try { Clipboard.SetText(MsgRetorno); } catch (Exception) { };
                }
            }
            return Json(new { success = Processado, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AjaxReprocessarPlanilhaItens(HttpPostedFileBase filesource) // UPLOAD PLANILHA DE ITENS
        {
            int IndexNfNumero = 1;
            int IndexDiNumero = 2;
            int IndexDiData = 3;
            int IndexPN = 5;
            int IndexDescricao = 6;
            int IndexDiAdicaoNumero = 7;
            int IndexDiAdicaoSequencial = 8;
            int IndexNcmCodigo = 9;
            int IndexValorFob = 10;
            int IndexValorFrete = 11;
            int IndexQuantidade = 15;
            int IndexUnidade = 16;
            int IndexValorUnit = 17;
            int IndexValorTotal = 18;
            int IndexIiPercentual = 20;
            int IndexIiValor = 19;
            int IndexIpiBaseCalculo = 21;
            int IndexIpiPercentual = 22;
            int IndexIpiValor = 23;
            int IndexIcmsBaseCalculo = 24;
            int IndexIcmsBaseReduzida = 25;
            int IndexIcmsPercentual = 26;
            int IndexIcmsValor = 27;
            int IndexPisBaseCalculo = 28;
            int IndexPisPercentual = 29;
            int IndexPisValor = 30;
            int IndexCofinsBaseCalculo = 28;
            int IndexCofinsPercentual = 31;
            int IndexCofinsValor = 32;
            int IndexSiscomexValor = 34;
            int IndexPesoLiquido = 35;
            int IndexPesoBruto = 36;
            int IndexLiNumero = 37;
            int IndexSdaValor = 38;

            int QtdItensProcessados = 0;
            int QtdItensAtualizados = 0;
            int QtdItensNovos = 0;

            bool Processado = false;
            bool ErroProcessamento = false;
            bool ErroArquivoXlxs = false;
            string String_Item_Agrupador = string.Empty;
            string MsgRetorno = string.Empty;
            string IdentificadorDI = string.Empty;
            string ResultadoProcessamento = String.Empty;
            string PnProdutosComexNaoCadastrados = String.Empty;
            string PnItensSomentePlanilha = String.Empty;
            string PnItensSomenteInvoices = String.Empty;
            string PnProdutosERPMultiplosCadastros = String.Empty;
            string PnProdutosComexMultiplosCadastros = String.Empty;

            string PnProdutosDescricaoDivergente = String.Empty;
            String Logs = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DateTime DataDeclaracaoImportacao = LibDateTime.getDataHoraBrasilia();
            List<g_produtos> ListaProdutosERP = new List<g_produtos>();
            List<g_produtos_ncm> ListaProdutosNCM = new List<g_produtos_ncm>();
            List<g_unidade_medida> ListaUnidadesMedidas = new List<g_unidade_medida>();
            List<gc_comex_produtos> ListaComexProdutos = new List<gc_comex_produtos>();
            List<CstModelComexItemImportacao> ListaPlanilhaItens = new List<CstModelComexItemImportacao>();
            var fileExt = System.IO.Path.GetExtension(filesource.FileName.ToLower()).Substring(1);
            gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(CachePersister.userIdentity.IdGcComexImportacaoAtiva);

            // Totalizadores
            Decimal TotalizadorFobReais = 0;
            Decimal TotalizadorFobDollar = 0;
            Decimal TotalizadorFrete = 0;
            Decimal TotalizadorPesoLiquido = 0;
            Decimal TotalizadorPesoBruto = 0;
            Decimal TotalizadorTaxaDollar = 0;

            //Decimal FechamentoValorTotalImportacao = 0;


            if (fileExt != "xlsx")
            {
                ErroProcessamento = true;
                MsgRetorno = " - Arquivo de itens deve ser do tipo Planilha Excel (.xlsx)";
                ErroArquivoXlxs = true;
            }
            if (filesource.ContentLength > 500000)
            {
                ErroProcessamento = true;
                MsgRetorno = " - O Tamanho do arquivo não pode exceder 500 Kb!";
                ErroArquivoXlxs = true;
            }
            if (filesource.ContentLength == 0)
            {
                ErroProcessamento = true;
                MsgRetorno = " - O Arquivo está vazio!";
                ErroArquivoXlxs = true;
            }

            if (ErroProcessamento == false)
            {
                try
                {
                    ListaComexProdutos = db.gc_comex_produtos.Where(c => c.ativo == true && c.id_comex_produto > 0).ToList();

                    MsgRetorno = String.Empty;
                    var fileNameOrigem = Path.GetFileName(filesource.FileName);
                    var DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    var FileNameInvoice = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_Planilha-Itens_" + fileNameOrigem);
                    filesource.SaveAs(FileNameInvoice);

                    // Link Excel
                    XLWorkbook WorkBook = new XLWorkbook(FileNameInvoice);
                    IXLWorksheet WorkSheet = WorkBook.Worksheet(1);

                    // Linha Cabeçalho
                    try
                    {
                        IdentificadorDI = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(2).Cell(2).Value);
                        String RaizIdentificadorDI = LibStringFormat.SomenteAlfabetoeNumeros(IdentificadorDI).EmptyIfNull().ToString().Trim();
                        if (RaizIdentificadorDI.Length < 4)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += " - Identificador da DI [" + IdentificadorDI + "] não foi localizado no arquivo!" + "<br/>";
                        }
                    }
                    catch (Exception ex)
                    {
                        ErroProcessamento = true;
                        MsgRetorno += " - Identificador da DI NÃO localizado" + "<br/>";
                        MsgRetorno += LibExceptions.getExceptionShortMessage(ex);
                        ErroArquivoXlxs = true;
                    }

                    // Dados da Importação
                    if (record_gc_comex_importacoes != null)
                    {
                        if (record_gc_comex_importacoes.di_cambio > 0)
                        {
                            TotalizadorTaxaDollar = record_gc_comex_importacoes.di_cambio;
                        }
                        else
                        {
                            TotalizadorTaxaDollar = 5;
                        }
                    }
                    else
                    {
                        ErroProcessamento = true;
                        MsgRetorno += " - Dados da Importação não foram localizados no ERP!" + "<br/>";
                    }

                    // Header
                    if (ErroProcessamento == false)
                    {
                        try
                        {
                            if (WorkSheet.Row(1).Cell(1).Value.IsBlank == false)
                            {
                                CstModelComexItemImportacao ItemImportacao = new CstModelComexItemImportacao();
                                ItemImportacao.String_NfNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexNfNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_DiNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_DiData = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiData).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPN).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemImportacao.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemImportacao.String_PN);
                                ItemImportacao.String_PN_Variacao1 = ItemImportacao.String_PN_Auxiliar.Replace("0", "O");
                                ItemImportacao.String_PN_Variacao2 = ItemImportacao.String_PN_Auxiliar.Replace("O", "0");
                                ItemImportacao.String_Descricao = LibStringFormat.GDIFormatarDescricaoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDescricao).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                ItemImportacao.String_DiAdicaoNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiAdicaoNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_DiAdicaoSequencial = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiAdicaoSequencial).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_NcmCodigo = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexNcmCodigo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_ValorFob = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorFob).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_ValorFrete = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorFrete).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_Quantidade = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexQuantidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_UnidadeMedida = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexUnidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_ValorUnit = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorUnit).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_ValorTotal = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorTotal).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_PesoLiquido = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPesoLiquido).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                ItemImportacao.String_PesoBruto = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPesoBruto).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                if (ItemImportacao.IsValidHeader() == false)
                                {
                                    // Tentar o layout com uma coluna a mais (Variação do layout principal)
                                    IndexNfNumero += 1;
                                    IndexDiNumero += 1;
                                    IndexDiData += 1;
                                    IndexPN += 1;
                                    IndexDescricao += 1;
                                    IndexDiAdicaoNumero += 1;
                                    IndexDiAdicaoSequencial += 1;
                                    IndexNcmCodigo += 1;
                                    IndexValorFob += 1;
                                    IndexValorFrete += 1;
                                    IndexQuantidade += 1;
                                    IndexUnidade += 1;
                                    IndexValorUnit += 1;
                                    IndexValorTotal += 1;
                                    IndexPesoLiquido += 1;
                                    IndexPesoBruto += 1;
                                    ItemImportacao.String_NfNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexNfNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiData = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiData).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPN).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemImportacao.String_PN);
                                    ItemImportacao.String_PN_Variacao1 = ItemImportacao.String_PN_Auxiliar.Replace("0", "O");
                                    ItemImportacao.String_PN_Variacao2 = ItemImportacao.String_PN_Auxiliar.Replace("O", "0");
                                    ItemImportacao.String_Descricao = LibStringFormat.GDIFormatarDescricaoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDescricao).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_DiAdicaoNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiAdicaoNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiAdicaoSequencial = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexDiAdicaoSequencial).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_NcmCodigo = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexNcmCodigo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorFob = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorFob).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorFrete = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorFrete).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_Quantidade = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexQuantidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_UnidadeMedida = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexUnidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorUnit = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorUnit).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorTotal = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexValorTotal).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PesoLiquido = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPesoLiquido).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PesoBruto = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(1).Cell(IndexPesoBruto).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    if (ItemImportacao.IsValidHeader() == false)
                                    {
                                        ErroProcessamento = true;
                                        MsgRetorno += " - Cabeçalho da planilha NÃO localizado!" + "<br/>";
                                        ErroArquivoXlxs = true;
                                    }
                                }
                            }
                            else
                            {
                                ErroProcessamento = true;
                                MsgRetorno += " - Cabeçalho da planilha NÃO localizado!" + "<br/>";
                                ErroArquivoXlxs = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += " - Cabeçalho da planilha NÃO localizado!" + "<br/>";
                            ErroArquivoXlxs = true;
                            MsgRetorno += LibExceptions.getExceptionShortMessage(ex);
                        }
                    }

                    if (ErroProcessamento == false)
                    {
                        try
                        {
                            List<gc_comex_invoices_itens> ListaIntesInvoicesValidar = db.gc_comex_invoices_itens.Where(i => i.ativo == true && i.id_importacao == CachePersister.userIdentity.IdGcComexImportacaoAtiva).ToList();
                            for (int IndexRow = 2; IndexRow <= (WorkSheet.RowsUsed().Count()); IndexRow++)
                            {
                                if (WorkSheet.Row(IndexRow).Cell(1).Value.IsBlank == false)
                                {
                                    CstModelComexItemImportacao ItemImportacao = new CstModelComexItemImportacao();
                                    ItemImportacao.String_NfNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexNfNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDiNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_LiNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexLiNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiData = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDiData).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PN = LibStringFormat.GDIFormatarCodigoProduto(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPN).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_PN_Auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemImportacao.String_PN);
                                    ItemImportacao.String_PN_Variacao1 = ItemImportacao.String_PN_Auxiliar.Replace("0", "O");
                                    ItemImportacao.String_PN_Variacao2 = ItemImportacao.String_PN_Auxiliar.Replace("O", "0");
                                    ItemImportacao.String_Descricao = LibStringFormat.GDIFormatarDescricaoProdutoTraduzidoComPN(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDescricao).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_DiAdicaoNumero = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDiAdicaoNumero).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_DiAdicaoSequencial = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexDiAdicaoSequencial).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_NcmCodigo = LibStringFormat.SomenteNumeros(LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexNcmCodigo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant());
                                    ItemImportacao.String_ValorFob = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexValorFob).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorFrete = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexValorFrete).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_Quantidade = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexQuantidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_UnidadeMedida = LibExcelReader.GetStringCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexUnidade).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    if (ItemImportacao.String_UnidadeMedida.EmptyIfNull().ToString().Trim().Length == 0) { ItemImportacao.String_UnidadeMedida = "UN"; };
                                    ItemImportacao.String_ValorUnit = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexValorUnit).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_ValorTotal = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexValorTotal).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IiPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIiPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IiValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIiValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IpiBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIpiBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IpiPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIpiPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IpiValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIpiValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IcmsBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIcmsBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IcmsBaseReduzida = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIcmsBaseReduzida).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IcmsPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIcmsPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_IcmsValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexIcmsValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PisBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPisBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PisPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPisPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PisValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPisValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_CofinsBaseCalculo = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCofinsBaseCalculo).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_CofinsPercentual = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCofinsPercentual).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_CofinsValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexCofinsValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_SiscomexValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexSiscomexValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_SdaValor = LibExcelReader.GetDecimalCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexSdaValor).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PesoLiquido = LibExcelReader.GetWeightCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPesoLiquido).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    ItemImportacao.String_PesoBruto = LibExcelReader.GetWeightCellXlsx(WorkSheet.Row(IndexRow).Cell(IndexPesoBruto).Value).EmptyIfNull().ToString().Trim().ToUpperInvariant();

                                    gc_comex_invoices_itens ItemInvoice = ListaIntesInvoicesValidar.Where(i => i.pn == ItemImportacao.String_PN).FirstOrDefault();
                                    if (ItemInvoice != null)
                                    {
                                        ItemImportacao.IdInvoiceItemERP = ItemInvoice.id_invoice_item;
                                        if (ItemInvoice.customer.EmptyIfNull().ToString().ToUpperInvariant().StartsWith("GDI ")) { ItemImportacao.IsGDI = true; } else { ItemImportacao.IsSC = true; }
                                    }

                                    // Validação do item nos cadastros de produtos
                                    if (ItemImportacao.IsValidItem())
                                    {
                                        ListaPlanilhaItens.Add(ItemImportacao);
                                    }
                                    else
                                    {
                                        ErroProcessamento = true;
                                        ErroArquivoXlxs = true;
                                        if (ItemImportacao.IsRowEmpty()) { MsgRetorno += " - Linha [" + (IndexRow).ToString() + "] não contém dados!" + "<br/>"; }
                                        else { MsgRetorno += " - Erro ao processar a linha [" + (IndexRow).ToString() + "]!" + "<br/>"; };
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ErroProcessamento = true;
                            MsgRetorno += LibExceptions.getExceptionShortMessage(ex) + "<br/>";
                        }
                        finally
                        {
                            WorkBook.Dispose();
                        }
                    }


                    if (ErroProcessamento == false)
                    {
                        // Validações e Totalizadores
                        TotalizadorFobReais = 0;
                        TotalizadorFobDollar = 0;
                        TotalizadorFrete = 0;
                        TotalizadorPesoLiquido = 0;
                        TotalizadorPesoBruto = 0;
                        foreach (CstModelComexItemImportacao ItemImportacao in ListaPlanilhaItens)
                        {
                            TotalizadorFobReais += ItemImportacao.Decimal_ValorFob;
                            TotalizadorFrete += ItemImportacao.Decimal_ValorFrete;
                            TotalizadorPesoLiquido += ItemImportacao.Decimal_PesoLiquido;
                            TotalizadorPesoBruto += ItemImportacao.Decimal_PesoBruto;
                        }
                        TotalizadorFobDollar = (TotalizadorFobReais / TotalizadorTaxaDollar);

                        String NfDespachanteNumero = String.Empty;

                        List<gc_comex_importacoes_itens> ListaIntesItensImportacao = db.gc_comex_importacoes_itens.Where(i => i.ativo == true && i.id_importacao == CachePersister.userIdentity.IdGcComexImportacaoAtiva).ToList();
                        List<gc_comex_importacoes_itens> ListaIntesItensImportacaoAtualizar = new List<gc_comex_importacoes_itens>();

                        // CADASTRO DE PRODUTOS - 8
                        foreach (CstModelComexItemImportacao ItemImportacao in ListaPlanilhaItens)
                        {
                            QtdItensProcessados += 1;

                            String PNOficial = ItemImportacao.String_PN.EmptyIfNull().ToString();
                            String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                            String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                            String PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                            gc_comex_importacoes_itens ItemAtualizar = ListaIntesItensImportacao.Where(i => i.pn == PNOficial).FirstOrDefault();
                            try { if (ItemAtualizar == null) { ItemAtualizar = ListaIntesItensImportacao.Where(p => p.pn_auxiliar == PNAuxiliar || p.pn_variacao1 == PNCuringaOH || p.pn_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };

                            if (ItemAtualizar != null)
                            {
                                ItemAtualizar.valor_fob_entrada = ItemImportacao.Decimal_ValorFob;
                                ItemAtualizar.valor_frete = ItemImportacao.Decimal_ValorFrete;
                                ItemAtualizar.quantidade = ItemImportacao.Decimal_Quantidade;
                                ItemAtualizar.valor_unit = ItemImportacao.Decimal_ValorUnit;
                                ItemAtualizar.valor_total = Decimal.Round((ItemAtualizar.quantidade * ItemAtualizar.valor_unit), 2);
                                ItemAtualizar.ii_percentual = ItemImportacao.Decimal_IiPercentual;
                                ItemAtualizar.ii_valor = ItemImportacao.Decimal_IiValor;
                                ItemAtualizar.ipi_bc = ItemImportacao.Decimal_IpiBaseCalculo;
                                ItemAtualizar.ipi_percentual = ItemImportacao.Decimal_IpiPercentual;
                                ItemAtualizar.ipi_valor = ItemImportacao.Decimal_IpiValor;
                                ItemAtualizar.icms_bc = ItemImportacao.Decimal_IcmsBaseCalculo;
                                ItemAtualizar.icms_bc_reduzida = ItemImportacao.Decimal_IcmsBaseReduzida;
                                ItemAtualizar.icms_percentual = ItemImportacao.Decimal_IcmsPercentual;
                                ItemAtualizar.icms_valor = ItemImportacao.Decimal_IcmsValor;
                                ItemAtualizar.pis_bc = ItemImportacao.Decimal_PisBaseCalculo;
                                ItemAtualizar.pis_percentual = ItemImportacao.Decimal_PisPercentual;
                                ItemAtualizar.pis_valor = ItemImportacao.Decimal_PisValor;
                                ItemAtualizar.cofins_bc = ItemImportacao.Decimal_CofinsBaseCalculo;
                                ItemAtualizar.cofins_percentual = ItemImportacao.Decimal_CofinsPercentual;
                                ItemAtualizar.cofins_valor = ItemImportacao.Decimal_CofinsValor;
                                ItemAtualizar.ibs_cbs_cst = ItemImportacao.String_IbsCbsCst;
                                ItemAtualizar.c_class_trib = ItemImportacao.String_cClassTrib;
                                ItemAtualizar.ibs_cbs_vbc = ItemImportacao.Decimal_IbsCbsBaseCalculo;
                                ItemAtualizar.ibs_pibs = ItemImportacao.Decimal_IbsPercentual;
                                ItemAtualizar.ibs_vibs = ItemImportacao.Decimal_IbsValor;
                                ItemAtualizar.cbs_pcbs = ItemImportacao.Decimal_CbsPercentual;
                                ItemAtualizar.cbs_vcbs = ItemImportacao.Decimal_CbsValor;
                                ItemAtualizar.siscomex_valor = ItemImportacao.Decimal_SiscomexValor;
                                ItemAtualizar.sda_valor = ItemImportacao.Decimal_SdaValor;
                                ItemAtualizar.marinha_valor = ItemImportacao.Decimal_MarinhaValor;
                                ItemAtualizar.peso_liquido = ItemImportacao.Decimal_PesoLiquido;
                                ItemAtualizar.peso_bruto = ItemImportacao.Decimal_PesoBruto;
                                ItemAtualizar.is_gdi = ItemImportacao.IsGDI;
                                ItemAtualizar.is_sc = ItemImportacao.IsSC;

                                // Dados Calculados
                                ItemAtualizar.valor_fob_percentual = ((ItemAtualizar.valor_fob_entrada * 100) / TotalizadorFobReais);
                                ItemAtualizar.valor_frete_percentual = ((ItemAtualizar.valor_frete * 100) / TotalizadorFrete);
                                ItemAtualizar.peso_liquido_percentual = ((ItemAtualizar.peso_liquido * 100) / TotalizadorPesoLiquido);
                                ItemAtualizar.peso_bruto_percentual = ((ItemAtualizar.peso_bruto * 100) / TotalizadorPesoBruto);
                                ItemAtualizar.valor_fob_reais_unidade = (ItemAtualizar.valor_fob_entrada / ItemAtualizar.quantidade);
                                ItemAtualizar.valor_fob_dollar_unidade = (ItemAtualizar.valor_fob_reais_unidade / record_gc_comex_importacoes.di_cambio);
                                ItemAtualizar.valor_custo_dollar_unidade = 0;
                                ItemAtualizar.valor_custo_reais_unidade = 0;
                                ItemAtualizar.id_coligada = 1;
                                ItemAtualizar.id_filial = 1;
                                ItemAtualizar.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                ItemAtualizar.datahora_cadastro = DataHoraAtual;
                                ListaIntesItensImportacaoAtualizar.Add(ItemAtualizar);
                            }
                            if (ItemAtualizar == null)
                            {
                                // CADASTRO DE PRODUTOS - 7
                                QtdItensNovos += 1;
                                int IdProdutoGDI = 0;
                                int IdProdutoComex = 0;
                                PNOficial = ItemImportacao.String_PN.EmptyIfNull().ToString();
                                PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                                PNCuringaOH = PNAuxiliar.Replace("0", "O");
                                PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                                g_produtos ProdutoGDI = ListaProdutosERP.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                                try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosERP.Where(p => p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar
                                if (ProdutoGDI != null) { IdProdutoGDI = ProdutoGDI.id_produto; }

                                gc_comex_produtos ProdutoComex = ListaComexProdutos.Where(p => p.pn == PNOficial).FirstOrDefault();
                                try { if (ProdutoComex == null) { ProdutoComex = ListaComexProdutos.Where(p => p.pn_auxiliar == PNAuxiliar || p.pn_auxiliar == PNCuringaOH || p.pn_auxiliar == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { };
                                if (ProdutoComex != null) { IdProdutoComex = ProdutoComex.id_comex_produto; }

                                gc_comex_importacoes_itens novo_item_importacao = new Db.gc_comex_importacoes_itens();
                                novo_item_importacao.ativo = true;
                                novo_item_importacao.id_importacao = 0;
                                novo_item_importacao.id_importacao_file_xls = 0;
                                novo_item_importacao.id_comex_produto = IdProdutoComex;
                                novo_item_importacao.id_produto = IdProdutoGDI;
                                novo_item_importacao.id_invoice_item = ItemImportacao.IdInvoiceItemERP;
                                novo_item_importacao.nf_numero = ItemImportacao.String_NfNumero;
                                novo_item_importacao.li_numero = ItemImportacao.String_LiNumero;
                                novo_item_importacao.nf_despachante_numero = ItemImportacao.String_NfNumero;
                                novo_item_importacao.di_numero = ItemImportacao.String_DiNumero;
                                novo_item_importacao.pn = ItemImportacao.String_PN;
                                novo_item_importacao.pn_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ItemImportacao.String_PN);
                                novo_item_importacao.pn_variacao1 = novo_item_importacao.pn_auxiliar.Replace("0", "O");
                                novo_item_importacao.pn_variacao2 = novo_item_importacao.pn_auxiliar.Replace("O", "0");
                                novo_item_importacao.descricao = ItemImportacao.String_Descricao;
                                novo_item_importacao.di_adicao_numero = ItemImportacao.Int_DiAdicaoNumero;
                                novo_item_importacao.di_adicao_sequencial = ItemImportacao.Int_DiAdicaoSequencial;
                                novo_item_importacao.codigo_ncm = ItemImportacao.String_NcmCodigo;
                                novo_item_importacao.valor_fob_entrada = ItemImportacao.Decimal_ValorFob;
                                novo_item_importacao.valor_frete = ItemImportacao.Decimal_ValorFrete;
                                novo_item_importacao.quantidade = ItemImportacao.Decimal_Quantidade;
                                novo_item_importacao.valor_unit = ItemImportacao.Decimal_ValorUnit;
                                novo_item_importacao.valor_total = Decimal.Round((novo_item_importacao.quantidade * novo_item_importacao.valor_unit), 2);
                                novo_item_importacao.un = ItemImportacao.String_UnidadeMedida;
                                novo_item_importacao.ii_percentual = ItemImportacao.Decimal_IiPercentual;
                                novo_item_importacao.ii_valor = ItemImportacao.Decimal_IiValor;
                                novo_item_importacao.ipi_bc = ItemImportacao.Decimal_IpiBaseCalculo;
                                novo_item_importacao.ipi_percentual = ItemImportacao.Decimal_IpiPercentual;
                                novo_item_importacao.ipi_valor = ItemImportacao.Decimal_IpiValor;
                                novo_item_importacao.icms_bc = ItemImportacao.Decimal_IcmsBaseCalculo;
                                novo_item_importacao.icms_bc_reduzida = ItemImportacao.Decimal_IcmsBaseReduzida;
                                novo_item_importacao.icms_percentual = ItemImportacao.Decimal_IcmsPercentual;
                                novo_item_importacao.icms_valor = ItemImportacao.Decimal_IcmsValor;
                                novo_item_importacao.pis_bc = ItemImportacao.Decimal_PisBaseCalculo;
                                novo_item_importacao.pis_percentual = ItemImportacao.Decimal_PisPercentual;
                                novo_item_importacao.pis_valor = ItemImportacao.Decimal_PisValor;
                                novo_item_importacao.cofins_bc = ItemImportacao.Decimal_CofinsBaseCalculo;
                                novo_item_importacao.cofins_percentual = ItemImportacao.Decimal_CofinsPercentual;
                                novo_item_importacao.cofins_valor = ItemImportacao.Decimal_CofinsValor;
                                novo_item_importacao.ibs_cbs_cst = ItemImportacao.String_IbsCbsCst;
                                novo_item_importacao.c_class_trib = ItemImportacao.String_cClassTrib;
                                novo_item_importacao.ibs_cbs_vbc = ItemImportacao.Decimal_IbsCbsBaseCalculo;
                                novo_item_importacao.ibs_pibs = ItemImportacao.Decimal_IbsPercentual;
                                novo_item_importacao.ibs_vibs = ItemImportacao.Decimal_IbsValor;
                                novo_item_importacao.cbs_pcbs = ItemImportacao.Decimal_CbsPercentual;
                                novo_item_importacao.cbs_vcbs = ItemImportacao.Decimal_CbsValor;
                                novo_item_importacao.siscomex_valor = ItemImportacao.Decimal_SiscomexValor;
                                novo_item_importacao.sda_valor = ItemImportacao.Decimal_SdaValor;
                                novo_item_importacao.marinha_valor = ItemImportacao.Decimal_MarinhaValor;
                                novo_item_importacao.peso_liquido = ItemImportacao.Decimal_PesoLiquido;
                                novo_item_importacao.peso_bruto = ItemImportacao.Decimal_PesoBruto;
                                novo_item_importacao.is_gdi = ItemImportacao.IsGDI;
                                novo_item_importacao.is_sc = ItemImportacao.IsSC;

                                // Dados Calculados
                                novo_item_importacao.valor_fob_percentual = ((novo_item_importacao.valor_fob_entrada * 100) / TotalizadorFobReais);
                                novo_item_importacao.valor_frete_percentual = ((novo_item_importacao.valor_frete * 100) / TotalizadorFrete);
                                novo_item_importacao.peso_liquido_percentual = ((novo_item_importacao.peso_liquido * 100) / TotalizadorPesoLiquido);
                                novo_item_importacao.peso_bruto_percentual = ((novo_item_importacao.peso_bruto * 100) / TotalizadorPesoBruto);
                                novo_item_importacao.valor_fob_reais_unidade = (novo_item_importacao.valor_fob_entrada / novo_item_importacao.quantidade);
                                novo_item_importacao.valor_fob_dollar_unidade = (novo_item_importacao.valor_fob_reais_unidade / record_gc_comex_importacoes.di_cambio);
                                novo_item_importacao.valor_custo_dollar_unidade = 0;
                                novo_item_importacao.valor_custo_reais_unidade = 0;
                                novo_item_importacao.id_coligada = 1;
                                novo_item_importacao.id_filial = 1;
                                novo_item_importacao.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                novo_item_importacao.datahora_cadastro = DataHoraAtual;
                                db.gc_comex_importacoes_itens.Add(novo_item_importacao);
                            }
                        }

                        foreach (gc_comex_importacoes_itens ItemImportacaoAtualizar in ListaIntesItensImportacaoAtualizar)
                        {
                            ItemImportacaoAtualizar.datahora_alteracao = DataHoraAtual;
                            ItemImportacaoAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(ItemImportacaoAtualizar).State = EntityState.Modified;
                            QtdItensAtualizados += 1;
                        }
                        db.SaveChanges();

                        MsgRetorno += "DI [ " + IdentificadorDI + " ] <b>REPROCESSADA</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
                        MsgRetorno += "<b>Itens Processados:</b> " + QtdItensProcessados.ToString() + "<br/>";
                        MsgRetorno += "<b>Itens Atualizados:</b> " + QtdItensAtualizados.ToString() + "<br/>";
                        MsgRetorno += "<b>Itens Novos:</b> " + QtdItensNovos.ToString() + "<br/>";
                        if (MsgRetorno.EmptyIfNull().ToString() != String.Empty) { MsgRetorno += "<br/><br/>"; }

                        try { Clipboard.SetText(MsgRetorno); } catch (Exception) { };
                        Processado = true;
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    Processado = false;
                    MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
                    try { Clipboard.SetText(MsgRetorno); } catch (Exception) { };
                }
                catch (Exception e)
                {
                    Processado = false;
                    MsgRetorno = LibExceptions.getExceptionShortMessage(e);
                    try { Clipboard.SetText(MsgRetorno); } catch (Exception) { };
                }
                if (ErroArquivoXlxs == true)
                {
                    MsgRetorno += "<br/>" + "Verifique o conteúdo do arquivo xlsx!";
                    try { Clipboard.SetText(MsgRetorno); } catch (Exception) { };
                }
            }
            return Json(new { success = Processado, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult AjaxAtualizarFobProdutos(String id)
        {
            bool Sucesso = false;
            int IdImportacao = 0;
            bool ErroProcessamento = false;
            int QtdProdutosAtualizados = 0;
            String MsgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                int.TryParse(id, out IdImportacao);
                
                if (IdImportacao.EmptyIfNull().ToString().Length <= 0)
                {
                    ErroProcessamento = true;
                    MsgRetorno += "Importação não foi localizada!" + "</br>";
                }

                if (ErroProcessamento == false)
                {
                    QtdProdutosAtualizados = AtualizarFobProdutos(IdImportacao);
                    ErroProcessamento = false;
                    Sucesso = true;
                    MsgRetorno += "Atualização de Custos Realizado com Sucesso!" + "</br>";
                    MsgRetorno += QtdProdutosAtualizados + " Produto(s) Atualizado(s)" + "</br>";

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

        public int AtualizarFobProdutos(int IdImportacao)
        {
            int QtdProdutosAtualizados = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                String LogAudit = string.Empty;
                gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(IdImportacao);
                List<gc_comex_invoices_itens> ListaItensInvoices = db.gc_comex_invoices_itens.Where(i => i.ativo == true && i.id_importacao == record_gc_comex_importacoes.id_importacao).ToList();
                List<g_produtos> ListaProdutosGDI = db.g_produtos.Where(p => p.ativo == true).ToList();

                // Cálculo do Custo dos Itens
                foreach (var ItemInvoice in ListaItensInvoices)
                {
                    // CADASTRO DE PRODUTOS - 6
                    Decimal ValorFob = ItemInvoice.item_unit_price;
                    String PNOficial = ItemInvoice.pn.EmptyIfNull().ToString();
                    String PNAuxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(PNOficial);
                    String PNCuringaOH = PNAuxiliar.Replace("0", "O");
                    String PNCuringaZERO = PNAuxiliar.Replace("O", "0");

                    g_produtos ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo == PNOficial).FirstOrDefault(); // Buscar pelo PN principal
                    try { if (ProdutoGDI == null) { ProdutoGDI = ListaProdutosGDI.Where(p => p.codigo_auxiliar == PNAuxiliar || p.codigo_variacao1 == PNCuringaOH || p.codigo_variacao2 == PNCuringaZERO).FirstOrDefault(); }; } catch (Exception) { }; // Buscar pelo PN Auxiliar

                    if ((ProdutoGDI != null) && (ItemInvoice.item_unit_price > 0))
                    {
                        if (IdImportacao > ProdutoGDI.fob1_id_importacao)
                        {
                            ProdutoGDI.fob3_dollar = ProdutoGDI.fob2_dollar;
                            ProdutoGDI.fob3_id_importacao = ProdutoGDI.fob2_id_importacao;

                            ProdutoGDI.fob2_dollar = ProdutoGDI.fob1_dollar;
                            ProdutoGDI.fob2_id_importacao = ProdutoGDI.fob1_id_importacao;

                            ProdutoGDI.fob1_dollar = ItemInvoice.item_unit_price;
                            ProdutoGDI.fob1_id_importacao = IdImportacao;

                            ProdutoGDI.tag1 = true;
                            ListaProdutosGDI[ListaProdutosGDI.IndexOf(ProdutoGDI)] = ProdutoGDI;
                            LogAudit = string.Empty;
                            LogAudit += "Fob Dollar: " + ProdutoGDI.fob1_dollar.ToString("###,###,##0.00000") + " > " + ItemInvoice.item_unit_price.ToString("###,###,##0.00000") + " | ";
                            LogAudit += "Id. Importação: " + IdImportacao.ToString();
                            LibAudit.SaveAudit(db, false, "g_produtos", ProdutoGDI.id_produto, LogAudit);
                        }
                        else if (IdImportacao == ProdutoGDI.fob1_id_importacao)
                        {
                            if (ProdutoGDI.fob1_dollar != ItemInvoice.item_unit_price)
                            {
                                ProdutoGDI.fob1_dollar = ItemInvoice.item_unit_price;
                                ProdutoGDI.tag1 = true;
                                ListaProdutosGDI[ListaProdutosGDI.IndexOf(ProdutoGDI)] = ProdutoGDI;
                                LogAudit = string.Empty;
                                LogAudit += "Fob Dollar: " + ProdutoGDI.fob1_dollar.ToString("###,###,##0.00000") + " > " + ItemInvoice.item_unit_price.ToString("###,###,##0.00000") + " | ";
                                LogAudit += "Id. Importação: " + IdImportacao.ToString();
                                LibAudit.SaveAudit(db, false, "g_produtos", ProdutoGDI.id_produto, LogAudit);
                            }
                        }
                    }
                }

                foreach (var ItemAtualizar in ListaProdutosGDI)
                {
                    if (ItemAtualizar.tag1 == true)
                    {
                        ItemAtualizar.tag1 = false;
                        ItemAtualizar.datahora_alteracao = DataHoraAtual;
                        ItemAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(ItemAtualizar).State = EntityState.Modified;
                        QtdProdutosAtualizados += 1;
                    }
                }

                record_gc_comex_importacoes.fob_produtos_atualizado = true;
                record_gc_comex_importacoes.fob_produtos_id_data_hora = LibDateTime.getDataHoraBrasilia();
                record_gc_comex_importacoes.fob_produtos_id_usuario = CachePersister.userIdentity.IdUsuario;
                record_gc_comex_importacoes.datahora_alteracao = DataHoraAtual;
                record_gc_comex_importacoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_gc_comex_importacoes).State = EntityState.Modified;

                db.SaveChanges();
            }
            catch (Exception e)
            {
                throw (e);
            }
            return QtdProdutosAtualizados;
        }
        #endregion

        #region ModalExcluirItensImportacao
        public ActionResult ModalExcluirItensImportacao()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-ban", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Excluir Itens Importação</b>";
            gc_comex_importacoes_files_xls record_gc_comex_importacoes_files_xls = new Db.gc_comex_importacoes_files_xls();
            record_gc_comex_importacoes_files_xls.exclusao_motivo = "";
            return View(record_gc_comex_importacoes_files_xls);
        }

        [HttpPost]
        public ActionResult AjaxModalExcluirItensImportacao(gc_comex_importacoes_files_xls modal_gc_comex_importacoes_files_xls)
        {
            bool Sucesso = false;
            bool ErroProcessamento = false;
            String MsgRetorno = String.Empty;
            String Logs = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            int IdImportacaoAtiva = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
            try
            {
                if (modal_gc_comex_importacoes_files_xls.exclusao_motivo.EmptyIfNull().ToString().Length == 0)
                {
                    ErroProcessamento = true;
                    MsgRetorno += "Campo [Motivo] é de preenchimento obrigatório!" + "</br>";
                }
                if (ErroProcessamento == false)
                {
                    Logs += "Exclusão Itens Importação | ";

                    List<Db.gc_comex_importacoes_files_xls> ListaImportacoesFiles = db.gc_comex_importacoes_files_xls.Where(f => (f.id_importacao == IdImportacaoAtiva) && (f.ativo == true)).ToList();

                    if (ListaImportacoesFiles.Count > 0)
                    {
                        foreach (gc_comex_importacoes_files_xls RecordFileXls in ListaImportacoesFiles)
                        {
                            RecordFileXls.ativo = false;
                            RecordFileXls.exclusao_id_usuario = CachePersister.userIdentity.IdUsuario;
                            RecordFileXls.exclusao_datahora = DataHoraAtual;
                            RecordFileXls.exclusao_motivo = modal_gc_comex_importacoes_files_xls.exclusao_motivo;
                            db.Entry(RecordFileXls).State = EntityState.Modified;

                            Logs += "DI: " + RecordFileXls.di_numero.EmptyIfNull().ToString() + " |";
                            Logs += "Filename: " + RecordFileXls.filename.EmptyIfNull().ToString() + " |";
                            Logs += "Qtd. Registros: " + RecordFileXls.qtd_registros.EmptyIfNull().ToString() + " |";
                            Logs += "Qtd. Itens: " + RecordFileXls.qtd_itens.EmptyIfNull().ToString() + " |";
                            Logs += "R$ Total: " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordFileXls.valor_total).Replace("R$ ", "").Replace("R$", "").Replace("$", "") + " |";

                            gc_comex_importacoes RecordImportacao = db.gc_comex_importacoes.Find(RecordFileXls.id_importacao);
                            if (RecordImportacao != null)
                            {
                                RecordImportacao.invoices_finalizadas = false;
                                RecordImportacao.datahora_alteracao = DataHoraAtual;
                                RecordImportacao.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                db.Entry(RecordImportacao).State = EntityState.Modified;
                            }
                        }

                        // Atualização dos status dos itens
                        String textoSQLExcluirItensImportacao = "update gc_comex_importacoes_itens set ativo = 0 where id_importacao = " + IdImportacaoAtiva.ToString();
                        int qtdItensExcluidos = LibDB.dbQueryExec(textoSQLExcluirItensImportacao, db);

                        db.SaveChanges();
                        Sucesso = true;
                        MsgRetorno += "Itens da Importação <b>Excluídos</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");

                        Logs += "Motivo: " + modal_gc_comex_importacoes_files_xls.exclusao_motivo;
                        LibAudit.SaveAudit(db, true,"gc_comex_importacoes", CachePersister.userIdentity.IdGcComexImportacaoAtiva, Logs);
                    }
                    else
                    {
                        ErroProcessamento = true;
                        MsgRetorno += "Não foram localizados itens a serem excluídos!" + "</br>";
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

        #region ModalUploadFileComexDocs
        public ActionResult ModalUploadFileComexDocs(int? IdGed)
        {
            CstUploadGed record_cstUploadGed = new CstUploadGed();
            if (IdGed > 0)
            {
                ged_arquivos record_ged_arquivos = db.ged_arquivos.Find(IdGed);
                record_cstUploadGed.id_arquivo = record_ged_arquivos.id_arquivo;
                record_cstUploadGed.id_arquivo_tipo = record_ged_arquivos.id_arquivo_tipo;
                record_cstUploadGed.descricao = record_ged_arquivos.descricao;
                record_cstUploadGed.observacao = record_ged_arquivos.observacao;
            }
            else
            {
                int IdImportacaoAtiva = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(IdImportacaoAtiva);
                record_cstUploadGed.id_comex_importacao = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                record_cstUploadGed.id_arquivo_tipo = 13;
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Documentos</b>";
            }
            return View("ModalUploadFileComexDocs", record_cstUploadGed);
        }
        #endregion

        #region ModalUploadFileComexInvoicesPDF
        public ActionResult ModalUploadFileComexInvoicesPDF(int? IdGed)
        {
            CstUploadGed record_cstUploadGed = new CstUploadGed();
            if (IdGed > 0)
            {
                /*ged_arquivos record_ged_arquivos = db.ged_arquivos.Find(IdGed);
                record_cstUploadGedInvoicePDF.id_arquivo = record_ged_arquivos.id_arquivo;
                record_cstUploadGedInvoicePDF.id_arquivo_tipo = record_ged_arquivos.id_arquivo_tipo;
                record_cstUploadGedInvoicePDF.data_referencia = record_ged_arquivos.data_referencia;
                record_cstUploadGedInvoicePDF.descricao = record_ged_arquivos.descricao;
                record_cstUploadGedInvoicePDF.observacao = record_ged_arquivos.observacao;*/
            }
            else
            {
                int IdImportacaoAtiva = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(IdImportacaoAtiva);
                record_cstUploadGed.id_comex_importacao = CachePersister.userIdentity.IdGcComexImportacaoAtiva;
                record_cstUploadGed.id_arquivo_tipo = 13;
                record_cstUploadGed.isComexInvoicePDF = true;
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Invoice de Cliente</b>";
            }
            return View("ModalUploadFileComexInvoicesPDF", record_cstUploadGed);
        }
        #endregion

        #region ModalImportacoesLogs
        public ActionResult ModalImportacoesLogs(int? idImportacao)
        {
            int temp = idImportacao.GetValueOrDefault();
            gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(temp);
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetEspacesHtml(3) + "Histórico da Importação " + record_gc_comex_importacoes.id_importacao.ToString();
            return View("ModalImportacoesLogs", record_gc_comex_importacoes);
        }

        public ActionResult GetDadosImportacoesLogs(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            String filterOnOff = "0";
            try
            {
                String Historico = string.Empty;
                int IdImportacao = 0;
                int.TryParse(param.yesCustomIdPK, out IdImportacao);
                String SentencaSQL = string.Empty;
                SentencaSQL += " select logs.*, usuarios.nome from g_logs logs ";
                SentencaSQL += " left join g_usuarios usuarios on (logs.id_usuario_cadastro = usuarios.id_usuario) ";
                SentencaSQL += " where logs.tabela = 'gc_importacoes' and logs.id_tabela = " + IdImportacao.EmptyIfNull().ToString();
                SentencaSQL += " order by datahora_cadastro desc ";
                DataTable TableHistorico = LibDB.GetDataTable(SentencaSQL, db);
                List<DataRow> AllItensHistorico = TableHistorico.AsEnumerable().ToList();
                List<string[]> list = new List<string[]>();
                foreach (var dsRowItemHistorico in AllItensHistorico)
                {
                    Historico = string.Empty;
                    if (dsRowItemHistorico["log"].EmptyIfNull().ToString().Trim().Length > 0) { Historico += dsRowItemHistorico["log"].EmptyIfNull().ToString().Trim(); }
                    Historico += "   ( Usuário: " + dsRowItemHistorico["nome"].EmptyIfNull().ToString() + " | Data/Hora: " + Convert.ToDateTime(dsRowItemHistorico["datahora_cadastro"].EmptyIfNull().ToString().Trim(), CultureInfo.InvariantCulture).ToString("dd/MM/yy HH:mm") + " | Id: " + dsRowItemHistorico["id_movimento_log"].EmptyIfNull().ToString() + " )";
                    list.Add(new[] { Historico, });
                }
                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = AllItensHistorico.Count(),
                    iTotalDisplayRecords = AllItensHistorico.Count(),
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

        #region ModalCancelarImportacaoComex
        public ActionResult ModalFechamentoCustosImportacao(String idImportacao)
        {
            gc_comex_importacoes record_gc_comex_importacoes = db.gc_comex_importacoes.Find(int.Parse(idImportacao));
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Fechamento Custos da Importação Nº " + record_gc_comex_importacoes.numero.EmptyIfNull().ToString();
            return View(record_gc_comex_importacoes);
        }

        [HttpPost]
        public ActionResult AjaxModalFechamentoCustosImportacao(gc_comex_importacoes modal_gc_comex_importacoes)
        {
            bool Sucesso = false;
            int NumeroRegistrosExportados = 0;
            String MsgRetorno = String.Empty;
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            String IdProcessamentoGravado = "0";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String FileNameTemplate = Path.Combine(Server.MapPath("~/Lib/Templates"), "template_fechamento_custos_importacao.xls");
            String FechamentoNumeroImportacao = String.Empty;
            Decimal FechamentoCambioValor = 0;
            Decimal FechamentoTotalPesoLiquido = 0;
            Decimal FechamentoTotalPesoBruto = 0;
            Decimal FechamentoTotalFobDi = 0;
            Decimal FechamentoTotalFobDiSC = 0;
            Decimal FechamentoTotalFobDiGDI = 0;
            Decimal FechamentoTotalFobAjustado = 0;
            Decimal FechamentoTotalFobAjustadoSC = 0;
            Decimal FechamentoTotalFobAjustadoGDI = 0;
            Decimal FechamentoPesoLiquidoSC = 0;
            Decimal FechamentoPesoLiquidoGDI = 0;
            Decimal FechamentoPesoBrutoSC = 0;
            Decimal FechamentoPesoBrutoGDI = 0;
            Decimal FechamentoTotalDespesasDesembaracoReais = 0;
            Decimal FechamentoTotalDespesasDesembaracoReaisSC = 0;
            Decimal FechamentoTotalDespesasDesembaracoReaisGDI = 0;
            Decimal FechamentoTotalDespesasDesembaracoDollar = 0;
            Decimal FechamentoTotalDespesasDesembaracoDollarSC = 0;
            Decimal FechamentoTotalDespesasDesembaracoDollarGDI = 0;
            Decimal FechamentoTotalFreteNacionalReais = 0;
            Decimal FechamentoTotalFreteNacionalReaisSC = 0;
            Decimal FechamentoTotalFreteNacionalReaisGDI = 0;
            Decimal FechamentoTotalFreteNacionalDollar = 0;
            Decimal FechamentoTotalFreteNacionalDollarSC = 0;
            Decimal FechamentoTotalFreteNacionalDollarGDI = 0;
            Decimal FechamentoPesoFreteInternacionalSC = 0;
            Decimal FechamentoPesoFreteInternacionalGDI = 0;
            Decimal FechamentoTotalFreteInternacionalDollarSC = 0;
            Decimal FechamentoTotalFreteInternacionalDollarGDI = 0;
            Decimal FechamentoTotalFreteInternacionalDollar = 0;
            Decimal FechamentoTotalCustoDolllar = 0;
            Decimal FechamentoTotalCustoDolllarSC = 0;
            Decimal FechamentoTotalCustoDolllarGDI = 0;

            try
            {
                gc_comex_importacoes RecordImportacao = db.gc_comex_importacoes.Find(modal_gc_comex_importacoes.id_importacao);

                FechamentoNumeroImportacao = RecordImportacao.numero.EmptyIfNull().ToString().Trim();
                FechamentoCambioValor = RecordImportacao.di_cambio;
                FechamentoTotalPesoLiquido = RecordImportacao.peso_liquido;
                FechamentoTotalPesoBruto = RecordImportacao.peso_bruto;

                // FOB
                FechamentoTotalFobDi = RecordImportacao.despesas_fob;
                FechamentoTotalFobDiSC = (FechamentoTotalFobDi / 100) * (RecordImportacao.percentual_valor_sc);
                FechamentoTotalFobDiGDI = (FechamentoTotalFobDi / 100) * (RecordImportacao.percentual_valor_gdi);
                FechamentoTotalFobAjustado = RecordImportacao.despesas_fob_ajustado;
                FechamentoTotalFobAjustadoSC = (FechamentoTotalFobAjustado / 100) * (RecordImportacao.percentual_valor_sc);
                FechamentoTotalFobAjustadoGDI = (FechamentoTotalFobAjustado / 100) * (RecordImportacao.percentual_valor_gdi);

                FechamentoPesoLiquidoSC = (RecordImportacao.peso_liquido / 100) * (RecordImportacao.percentual_peso_liquido_sc);
                FechamentoPesoLiquidoGDI = (RecordImportacao.peso_liquido / 100) * (RecordImportacao.percentual_peso_liquido_gdi); ;
                FechamentoPesoBrutoSC = (RecordImportacao.peso_bruto / 100) * (RecordImportacao.percentual_peso_bruto_sc);
                FechamentoPesoBrutoGDI = (RecordImportacao.peso_bruto / 100) * (RecordImportacao.percentual_peso_bruto_gdi);

                FechamentoTotalDespesasDesembaracoReais = 0;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_ipi;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_ii;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_pis;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_cofins;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_siscomex;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_icms;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_ibs;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_cbs;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_sda;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_marinha_mercante;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_armazenagem_primaria;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_armazenagem_secundaria;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_transp_rodo_remocao;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_armazenagem_infraero;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_despachante;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_marinha_mercante;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_taxa_expediente_santos;
                FechamentoTotalDespesasDesembaracoReais += RecordImportacao.despesas_taxa_capatazia;
                FechamentoTotalDespesasDesembaracoReaisSC = (FechamentoTotalDespesasDesembaracoReais / 100) * RecordImportacao.percentual_valor_sc;
                FechamentoTotalDespesasDesembaracoReaisGDI = (FechamentoTotalDespesasDesembaracoReais / 100) * RecordImportacao.percentual_valor_gdi;
                FechamentoTotalDespesasDesembaracoDollar = FechamentoTotalDespesasDesembaracoReais / FechamentoCambioValor;
                FechamentoTotalDespesasDesembaracoDollarSC = FechamentoTotalDespesasDesembaracoReaisSC / FechamentoCambioValor;
                FechamentoTotalDespesasDesembaracoDollarGDI = FechamentoTotalDespesasDesembaracoReaisGDI / FechamentoCambioValor;

                FechamentoTotalFreteNacionalReaisSC = ((RecordImportacao.despesas_frete_brasil / FechamentoCambioValor) / 100) * RecordImportacao.percentual_peso_bruto_sc;
                FechamentoTotalFreteNacionalReaisGDI = ((RecordImportacao.despesas_frete_brasil / FechamentoCambioValor) / 100) * RecordImportacao.percentual_peso_bruto_gdi;
                FechamentoPesoFreteInternacionalSC = ((RecordImportacao.peso_bruto / 100) * RecordImportacao.percentual_peso_bruto_sc);
                FechamentoPesoFreteInternacionalGDI = ((RecordImportacao.peso_bruto / 100) * RecordImportacao.percentual_peso_bruto_gdi);

                FechamentoTotalFreteInternacionalDollar = RecordImportacao.despesas_frete_internacional;
                FechamentoTotalFreteInternacionalDollarSC = ((RecordImportacao.despesas_frete_internacional / 100) * RecordImportacao.percentual_peso_bruto_sc);
                FechamentoTotalFreteInternacionalDollarGDI = ((RecordImportacao.despesas_frete_internacional / 100) * RecordImportacao.percentual_peso_bruto_gdi);

                FechamentoTotalFreteNacionalReais = RecordImportacao.despesas_frete_brasil;
                FechamentoTotalFreteNacionalReaisSC = (RecordImportacao.despesas_frete_brasil / 100) * RecordImportacao.percentual_peso_bruto_sc;
                FechamentoTotalFreteNacionalReaisGDI = (RecordImportacao.despesas_frete_brasil / 100) * RecordImportacao.percentual_peso_bruto_gdi;
                FechamentoTotalFreteNacionalDollar = FechamentoTotalFreteNacionalReais / FechamentoCambioValor;
                FechamentoTotalFreteNacionalDollarSC = FechamentoTotalFreteNacionalReaisSC / FechamentoCambioValor;
                FechamentoTotalFreteNacionalDollarGDI = FechamentoTotalFreteNacionalReaisGDI / FechamentoCambioValor;

                // Totalizador de Custos
                FechamentoTotalCustoDolllar = FechamentoTotalDespesasDesembaracoDollar + FechamentoTotalFreteNacionalDollar + FechamentoTotalFreteInternacionalDollar;
                FechamentoTotalCustoDolllarSC = FechamentoTotalDespesasDesembaracoDollarSC + FechamentoTotalFreteNacionalDollarSC + FechamentoTotalFreteInternacionalDollarSC;
                FechamentoTotalCustoDolllarGDI = FechamentoTotalDespesasDesembaracoDollarGDI + FechamentoTotalFreteNacionalDollarGDI + FechamentoTotalFreteInternacionalDollarGDI;

                FileStream FileTemplate = new FileStream(FileNameTemplate, FileMode.Open, FileAccess.Read);
                _workbookCatalogo = new HSSFWorkbook(FileTemplate);
                ISheet sheetCatalogo = _workbookCatalogo.GetSheet("Fechamento");

                sheetCatalogo.GetCell(3, 2).SetCellValue(FechamentoNumeroImportacao); // B3
                sheetCatalogo.GetCell(4, 2).SetCellValue(RecordImportacao.awb_numero.EmptyIfNull().ToString()); // B3
                sheetCatalogo.GetCell(5, 2).SetCellValue(RecordImportacao.data_registro); // B3
                sheetCatalogo.GetCell(6, 2).SetCellValue(decimal.ToDouble(RecordImportacao.di_cambio)); // B3

                // Peso Liquido
                sheetCatalogo.GetCell(14, 8).SetCellValue(decimal.ToDouble(FechamentoPesoLiquidoSC)); // B3
                sheetCatalogo.GetCell(14, 9).SetCellValue(decimal.ToDouble(RecordImportacao.percentual_peso_liquido_sc / 100)); // B3
                sheetCatalogo.GetCell(14, 10).SetCellValue(decimal.ToDouble(FechamentoPesoLiquidoGDI)); // B3
                sheetCatalogo.GetCell(14, 11).SetCellValue(decimal.ToDouble(RecordImportacao.percentual_peso_liquido_gdi / 100)); // B3
                sheetCatalogo.GetCell(14, 12).SetCellValue(decimal.ToDouble(FechamentoTotalPesoLiquido)); // E3

                // Peso Bruto
                sheetCatalogo.GetCell(15, 8).SetCellValue(decimal.ToDouble(FechamentoPesoBrutoSC)); // B3
                sheetCatalogo.GetCell(15, 9).SetCellValue(decimal.ToDouble(RecordImportacao.percentual_peso_bruto_sc / 100)); // B3
                sheetCatalogo.GetCell(15, 10).SetCellValue(decimal.ToDouble(FechamentoPesoBrutoGDI)); // B3
                sheetCatalogo.GetCell(15, 11).SetCellValue(decimal.ToDouble(RecordImportacao.percentual_peso_bruto_gdi / 100)); // B3
                sheetCatalogo.GetCell(15, 12).SetCellValue(decimal.ToDouble(FechamentoTotalPesoBruto)); // E3

                // Desembaraço Alfandegário
                sheetCatalogo.GetCell(20, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_ipi + RecordImportacao.despesas_pis + RecordImportacao.despesas_cofins + RecordImportacao.despesas_ii));
                sheetCatalogo.GetCell(21, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_siscomex));
                sheetCatalogo.GetCell(22, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_icms));
                sheetCatalogo.GetCell(23, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_sda));
                sheetCatalogo.GetCell(24, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_armazenagem_primaria));
                sheetCatalogo.GetCell(25, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_armazenagem_secundaria));
                sheetCatalogo.GetCell(26, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_transp_rodo_remocao));
                sheetCatalogo.GetCell(27, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_armazenagem_infraero));
                sheetCatalogo.GetCell(28, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_despachante));
                sheetCatalogo.GetCell(29, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_marinha_mercante)); // marinha
                sheetCatalogo.GetCell(30, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_taxa_expediente_santos)); // porto
                sheetCatalogo.GetCell(31, 3).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_taxa_capatazia)); // capatazia

                // Total Despesas Desembaraço
                sheetCatalogo.GetCell(34, 3).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoReaisSC));
                sheetCatalogo.GetCell(34, 4).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoReaisGDI));
                sheetCatalogo.GetCell(34, 5).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoReais));
                sheetCatalogo.GetCell(35, 3).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoDollarSC));
                sheetCatalogo.GetCell(35, 4).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoDollarGDI));
                sheetCatalogo.GetCell(35, 5).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoDollar));

                // Frete Nacional
                sheetCatalogo.GetCell(22, 10).SetCellValue(decimal.ToDouble(FechamentoTotalFreteNacionalReaisSC));
                sheetCatalogo.GetCell(22, 11).SetCellValue(decimal.ToDouble(FechamentoTotalFreteNacionalReaisGDI));
                sheetCatalogo.GetCell(22, 12).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_frete_brasil));                           // Reais

                sheetCatalogo.GetCell(23, 10).SetCellValue(decimal.ToDouble(FechamentoTotalFreteNacionalReaisSC / FechamentoCambioValor));
                sheetCatalogo.GetCell(23, 11).SetCellValue(decimal.ToDouble(FechamentoTotalFreteNacionalReaisGDI / FechamentoCambioValor));
                sheetCatalogo.GetCell(23, 12).SetCellValue(decimal.ToDouble(RecordImportacao.despesas_frete_brasil / FechamentoCambioValor));   // Dollar

                // Frete Internacional
                sheetCatalogo.GetCell(43, 3).SetCellValue(decimal.ToDouble(FechamentoPesoFreteInternacionalSC));
                sheetCatalogo.GetCell(43, 4).SetCellValue(decimal.ToDouble(FechamentoPesoFreteInternacionalGDI));
                sheetCatalogo.GetCell(43, 5).SetCellValue(decimal.ToDouble(FechamentoPesoFreteInternacionalSC + FechamentoPesoFreteInternacionalGDI));

                sheetCatalogo.GetCell(45, 3).SetCellValue(decimal.ToDouble(FechamentoTotalFreteInternacionalDollarSC));
                sheetCatalogo.GetCell(45, 4).SetCellValue(decimal.ToDouble(FechamentoTotalFreteInternacionalDollarGDI));
                sheetCatalogo.GetCell(45, 5).SetCellValue(decimal.ToDouble(FechamentoTotalFreteInternacionalDollar));


                // Quadro Principal | Valores FOB - DI
                sheetCatalogo.GetCell(4, 8).SetCellValue(decimal.ToDouble(FechamentoTotalFobDiSC)); // B3
                sheetCatalogo.GetCell(4, 9).SetCellValue(decimal.ToDouble(RecordImportacao.percentual_valor_sc / 100)); // B3
                sheetCatalogo.GetCell(4, 10).SetCellValue(decimal.ToDouble(FechamentoTotalFobDiGDI)); // B3
                sheetCatalogo.GetCell(4, 11).SetCellValue(decimal.ToDouble(RecordImportacao.percentual_valor_gdi / 100)); // B3
                sheetCatalogo.GetCell(4, 12).SetCellValue(decimal.ToDouble(FechamentoTotalFobDi)); // E3

                // Quadro Principal | Valores FOB - SC
                sheetCatalogo.GetCell(5, 8).SetCellValue(decimal.ToDouble(FechamentoTotalFobAjustadoSC)); // B3
                sheetCatalogo.GetCell(5, 9).SetCellValue(decimal.ToDouble(Math.Round((FechamentoTotalFobAjustadoSC * 100) / FechamentoTotalFobAjustado, 2))); // B3
                sheetCatalogo.GetCell(5, 10).SetCellValue(decimal.ToDouble(FechamentoTotalFobAjustadoGDI)); // B3
                sheetCatalogo.GetCell(5, 11).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalFobAjustadoGDI * 100) / FechamentoTotalFobAjustado)); // B3
                sheetCatalogo.GetCell(5, 12).SetCellValue(decimal.ToDouble(FechamentoTotalFobAjustado)); // E3

                // Quadro Principal | Desembaraço Alfandegário
                sheetCatalogo.GetCell(6, 8).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoDollarSC));
                sheetCatalogo.GetCell(6, 9).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalDespesasDesembaracoDollarSC * 100) / FechamentoTotalDespesasDesembaracoDollar));
                sheetCatalogo.GetCell(6, 10).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoDollarGDI));
                sheetCatalogo.GetCell(6, 11).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalDespesasDesembaracoDollarGDI * 100) / FechamentoTotalDespesasDesembaracoDollar));
                sheetCatalogo.GetCell(6, 12).SetCellValue(decimal.ToDouble(FechamentoTotalDespesasDesembaracoDollar));

                // Quadro Principal | Frete Nacional
                sheetCatalogo.GetCell(7, 8).SetCellValue(decimal.ToDouble(FechamentoTotalFreteNacionalDollarSC));
                sheetCatalogo.GetCell(7, 9).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalFreteNacionalDollarSC * 100) / FechamentoTotalFreteNacionalDollar));
                sheetCatalogo.GetCell(7, 10).SetCellValue(decimal.ToDouble(FechamentoTotalFreteNacionalDollarGDI));
                sheetCatalogo.GetCell(7, 11).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalFreteNacionalDollarGDI * 100) / FechamentoTotalFreteNacionalDollar));
                sheetCatalogo.GetCell(7, 12).SetCellValue(decimal.ToDouble(FechamentoTotalFreteNacionalDollar));

                // Quadro Principal | Frete Internacional
                sheetCatalogo.GetCell(8, 8).SetCellValue(decimal.ToDouble(FechamentoTotalFreteInternacionalDollarSC));
                sheetCatalogo.GetCell(8, 9).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalFreteInternacionalDollarSC * 100) / FechamentoTotalFreteInternacionalDollar));
                sheetCatalogo.GetCell(8, 10).SetCellValue(decimal.ToDouble(FechamentoTotalFreteInternacionalDollarGDI));
                sheetCatalogo.GetCell(8, 11).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalFreteInternacionalDollarGDI * 100) / FechamentoTotalFreteInternacionalDollar));
                sheetCatalogo.GetCell(8, 12).SetCellValue(decimal.ToDouble(FechamentoTotalFreteInternacionalDollar));

                // Quadro Principal | Total Custos
                sheetCatalogo.GetCell(10, 8).SetCellValue(decimal.ToDouble(FechamentoTotalCustoDolllarSC));
                sheetCatalogo.GetCell(10, 9).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalCustoDolllarSC * 100) / FechamentoTotalCustoDolllar));
                sheetCatalogo.GetCell(10, 10).SetCellValue(decimal.ToDouble(FechamentoTotalCustoDolllarGDI));
                sheetCatalogo.GetCell(10, 11).SetCellValue(LibNumbers.DecimalToDoublePercent((FechamentoTotalCustoDolllarGDI * 100) / FechamentoTotalCustoDolllar));
                sheetCatalogo.GetCell(10, 12).SetCellValue(decimal.ToDouble(FechamentoTotalCustoDolllar));

                // Quadro Principal | Total Custos + Fob
                Decimal TotalFobAjustadoAndCustosSC = FechamentoTotalFobAjustadoSC + FechamentoTotalCustoDolllarSC;
                Decimal TotalFobAjustadoAndCustosGDI = FechamentoTotalFobAjustadoGDI + FechamentoTotalCustoDolllarGDI;
                Decimal TotalFobAjustadoAndCustos = TotalFobAjustadoAndCustosSC + TotalFobAjustadoAndCustosGDI;
                sheetCatalogo.GetCell(11, 8).SetCellValue(decimal.ToDouble(TotalFobAjustadoAndCustosSC));
                sheetCatalogo.GetCell(11, 9).SetCellValue(LibNumbers.DecimalToDoublePercent((TotalFobAjustadoAndCustosSC * 100) / TotalFobAjustadoAndCustos));
                sheetCatalogo.GetCell(11, 10).SetCellValue(decimal.ToDouble(TotalFobAjustadoAndCustosGDI));
                sheetCatalogo.GetCell(11, 11).SetCellValue(LibNumbers.DecimalToDoublePercent((TotalFobAjustadoAndCustosGDI * 100) / TotalFobAjustadoAndCustos));
                sheetCatalogo.GetCell(11, 12).SetCellValue(decimal.ToDouble(TotalFobAjustadoAndCustos));

                // Quadro Principal | Percentual do FOB
                Decimal CustoPercentualFobSC = 0;
                Decimal CustoPercentualFobGDI = 0;
                if ((FechamentoTotalCustoDolllarSC + FechamentoTotalFobAjustadoSC) > 0) { CustoPercentualFobSC = Math.Round((FechamentoTotalCustoDolllarSC * 100) / FechamentoTotalFobAjustadoSC, 2); };
                if ((FechamentoTotalCustoDolllarGDI + FechamentoTotalFobAjustadoGDI) > 0) { CustoPercentualFobGDI = Math.Round((FechamentoTotalCustoDolllarGDI * 100) / FechamentoTotalFobAjustadoGDI, 2); };
                sheetCatalogo.GetCell(12, 9).SetCellValue(LibNumbers.DecimalToDoublePercent(CustoPercentualFobSC));
                sheetCatalogo.GetCell(12, 11).SetCellValue(LibNumbers.DecimalToDoublePercent(CustoPercentualFobGDI));

                // Quadro Principal | Custo por KG
                Decimal TotalCustoPorKgSC = 0;
                Decimal TotalCustoPorKgGDI = 0;
                if (FechamentoPesoBrutoSC > 0) { TotalCustoPorKgSC = TotalFobAjustadoAndCustosSC / FechamentoPesoBrutoSC; };
                if (FechamentoPesoBrutoGDI > 0) { TotalCustoPorKgGDI = TotalFobAjustadoAndCustosGDI / FechamentoPesoBrutoGDI; };
                sheetCatalogo.GetCell(16, 8).SetCellValue(decimal.ToDouble(TotalCustoPorKgSC));
                sheetCatalogo.GetCell(16, 10).SetCellValue(decimal.ToDouble(TotalCustoPorKgGDI));

                // Atualização dos dados calculados do custo
                RecordImportacao.calculo_custos_executado = true;
                RecordImportacao.calculo_custos_id_usuario = CachePersister.userIdentity.IdUsuario;
                RecordImportacao.calculo_custos_data_hora = LibDateTime.getDataHoraBrasilia();
                RecordImportacao.total_custo = FechamentoTotalCustoDolllar;
                RecordImportacao.percentual_custo_fob = ((FechamentoTotalCustoDolllar * 100) / FechamentoTotalFobAjustado);
                RecordImportacao.datahora_alteracao = DataHoraAtual;
                RecordImportacao.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(RecordImportacao).State = EntityState.Modified;
                db.SaveChanges();

                // Salvar o arquivo em disco
                

                String DirTempFiles = Server.MapPath("~/_filestemp");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "reports");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                FileNameExportacao = Path.Combine(DirTempFiles, "Fechamento Scross e GDI " + RecordImportacao.numero.EmptyIfNull().ToString().Trim() + ".xls");
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
                    record_g_processamento.detalhamento = "Relatório Fechamento Importação";
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
                    MsgRetorno = "Relatório GERADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<br/><br/>" + "O Download do relatório será iniciado automaticamente!";
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