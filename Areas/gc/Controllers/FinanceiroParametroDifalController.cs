using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    public class FinanceiroParametroDifalController : Controller
    {
        private GdiPlataformEntities db;
        private readonly string controllerName = "gc_FinanceiroParametroDifal";

        public FinanceiroParametroDifalController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_FinanceiroParametroDifal_*,gc_FinanceiroParametroDifal_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Parâmetros Difal";
            var model = new CstFinanceiroParametroDifalIndex
            {
                FinanceiroParametroDifalIndex_id = String.Empty,
                FinanceiroParametroDifalIndex_sigla = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, siglaRestore;
            if (TryParseFiltroDifalSemicolon(filtroPersistido.sql_filtro, out idRestore, out siglaRestore))
            {
                model.FinanceiroParametroDifalIndex_id = idRestore;
                model.FinanceiroParametroDifalIndex_sigla = siglaRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(siglaRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_FinanceiroParametroDifal_*,gc_FinanceiroParametroDifal_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
                bool filterApplied = false;
                string yesFilterField = param.yesFilterField.EmptyIfNull().ToString().Trim();
                bool listarTodosExplicito = yesFilterField == "*";

                g_filtros recordFiltro;
                if (listarTodosExplicito)
                {
                    recordFiltro = LibDB.getFilterByUser(param, controllerName, db);
                }
                else
                {
                    recordFiltro = ObterFiltroPersistidoUsuario();
                }

                int totalRecords = db.gc_parametros_difal.AsNoTracking().Where(c => c.id_parametro_difal > 1).Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string siglaStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrWhiteSpace(idStr) || !String.IsNullOrWhiteSpace(siglaStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroDifalSemicolon(recordFiltro.sql_filtro, out idStr, out siglaStr);
                    hasInline = !String.IsNullOrWhiteSpace(idStr) || !String.IsNullOrWhiteSpace(siglaStr);
                }

                if (!listarTodosExplicito && !hasInline)
                {
                    return Json(new
                    {
                        errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                        stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                        yesFilterOnOff = "0",
                        sEcho = param.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = 0,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                bool temFiltroIdExplicito = TryParseIdParametroDifalFiltro(idStr, out _);
                IQueryable<Db.gc_parametros_difal> query = db.gc_parametros_difal.AsNoTracking();
                if (!temFiltroIdExplicito)
                {
                    query = query.Where(c => c.id_parametro_difal > 1);
                }
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroDifalNaQuery(query, idStr, siglaStr);
                    LibDB.setFilterByUser(
                        MontarFiltroDifalPersistido(idStr, LibStringFormat.NormalizarTermoBuscaCodigo(siglaStr)),
                        controllerName,
                        true,
                        db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 30 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoDifalNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .ToList();

                var list = new List<string[]>();
                foreach (var c in page)
                {
                    string _DifalGeralCalcular = c.difal_geral_calcular == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Difal Geral Calcular - Sim", "green", "") : "";
                    string _DifalGeralZerar = c.difal_geral_zerar == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Difal Geral Zerar - Sim", "green", "") : "";
                    string _DifalGeralNaoinformar = c.difal_geral_naoinformar == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Difal Geral Não Informar - Sim", "green", "") : "";
                    string _DifalCombCalcular = c.difal_comb_calcular == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Difal Combustível Calcular - Sim", "green", "") : "";
                    string _DifalCombZerar = c.difal_comb_zerar == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Difal Combustível Zerar - Sim", "green", "") : "";
                    string _DifalCombNaoinformar = c.difal_comb_naoinformar == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Difal Combustível Não Informar - Sim", "green", "") : "";
                    list.Add(new[]
                    {
                        "",
                        c.id_parametro_difal.EmptyIfNull().ToString(),
                        c.sigla.EmptyIfNull().ToString(),
                        c.estado.EmptyIfNull().ToString(),
                        _DifalGeralCalcular, _DifalGeralZerar, _DifalGeralNaoinformar,
                        _DifalCombCalcular, _DifalCombZerar, _DifalCombNaoinformar
                    });
                }

                filterOnOff = filterApplied ? "1" : "0";

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
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

        private g_filtros ObterFiltroPersistidoUsuario()
        {
            if (CachePersister.userIdentity.allFiltros == null)
            {
                return new g_filtros();
            }
            string token = CachePersister.userIdentity.TokenAcesso.EmptyIfNull().ToString().Trim();
            g_filtros filtro = CachePersister.userIdentity.allFiltros
                .Where(f => f.token == token && f.controller == controllerName)
                .FirstOrDefault();
            return filtro ?? new g_filtros();
        }

        private static bool TryParseFiltroDifalSemicolon(string raw, out string id, out string sigla)
        {
            id = sigla = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            sigla = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(sigla);
        }

        private static string MontarFiltroDifalPersistido(string id, string sigla)
        {
            return (id ?? String.Empty) + ";" + (sigla ?? String.Empty);
        }

        private static bool TryParseIdParametroDifalFiltro(string idStr, out int idParametro)
        {
            idParametro = 0;
            if (String.IsNullOrWhiteSpace(idStr) || idStr == "0") return false;
            return int.TryParse(idStr.Trim(), out idParametro) && idParametro != 0;
        }

        /// <summary>Id. = igualdade; sigla = LIKE %termo% (código normalizado).</summary>
        private static IQueryable<Db.gc_parametros_difal> AplicarFiltroDifalNaQuery(IQueryable<Db.gc_parametros_difal> query, string idStr, string siglaStr)
        {
            if (TryParseIdParametroDifalFiltro(idStr, out int idParametro))
            {
                query = query.Where(c => c.id_parametro_difal == idParametro);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemCodigo(siglaStr, out string padraoSigla))
            {
                query = query.Where(c => c.sigla != null && DbFunctions.Like(c.sigla, padraoSigla));
            }
            return query;
        }

        private static IQueryable<Db.gc_parametros_difal> AplicarOrdenacaoDifalNaQuery(IQueryable<Db.gc_parametros_difal> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 2)
                {
                    return asc ? query.OrderBy(c => c.sigla) : query.OrderByDescending(c => c.sigla);
                }
                if (param.iSortCol_0 == 3)
                {
                    return asc ? query.OrderBy(c => c.estado) : query.OrderByDescending(c => c.estado);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(c => c.id_parametro_difal) : query.OrderByDescending(c => c.id_parametro_difal);
                }
            }
            return query.OrderBy(c => c.id_parametro_difal);
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_FinanceiroParametroDifal_*,gc_FinanceiroParametroDifal_Actionupdate")]
        public ActionResult ModalCreateEditParametroDifal(int? IdParametroDifal)
        {
            try
            {
                int idParametroDifal = IdParametroDifal.GetValueOrDefault();
                if (idParametroDifal <= 0)
                {
                    return RedirectToAction("Index");
                }
                gc_parametros_difal record_gc_parametros_difal = db.gc_parametros_difal.Find(idParametroDifal);
                if (record_gc_parametros_difal == null)
                {
                    return RedirectToAction("Index");
                }
                CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record_gc_parametros_difal);
                ViewBag.Title = MontarTituloCreateEditParametroDifal(record_gc_parametros_difal);
                return View("ModalCreateEditParametroDifal", record_gc_parametros_difal);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "FinanceiroParametroDifalController";
                msg += "<br/>" + "ModalCreateEditParametroDifal";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_FinanceiroParametroDifal_*,gc_FinanceiroParametroDifal_Actionupdate")]
        public ActionResult AjaxCreateEditParametroDifal(gc_parametros_difal view_record_gc_parametros_difal)
        {
            try
            {
                if (view_record_gc_parametros_difal.id_parametro_difal <= 0)
                {
                    return Json(GdiMvcJsonResults.AjaxFailure(GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Parâmetro Difal", view_record_gc_parametros_difal.id_parametro_difal)), JsonRequestBehavior.AllowGet);
                }

                gc_parametros_difal existente = db.gc_parametros_difal.Find(view_record_gc_parametros_difal.id_parametro_difal);
                if (existente == null)
                {
                    return Json(GdiMvcJsonResults.AjaxFailure(GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Parâmetro Difal", view_record_gc_parametros_difal.id_parametro_difal)), JsonRequestBehavior.AllowGet);
                }

                if (!ModelState.IsValid)
                {
                    string msgErro = String.Join("<br/>", ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage));
                    return Json(GdiMvcJsonResults.AjaxFailure(msgErro), JsonRequestBehavior.AllowGet);
                }

                view_record_gc_parametros_difal.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                view_record_gc_parametros_difal.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(view_record_gc_parametros_difal).State = EntityState.Modified;

                gc_parametros_difal record_old_gc_parametros_difal = JsonConvert.DeserializeObject<gc_parametros_difal>(CachePersister.userIdentity.DataRowInUseSerialized);
                String LogAlteracao = LibDB.CompareDataTable(record_old_gc_parametros_difal, view_record_gc_parametros_difal);
                if (LogAlteracao.EmptyIfNull().ToString().Length > 0)
                {
                    LibAudit.SaveAudit(db, true, "gc_parametros_difal", view_record_gc_parametros_difal.id_parametro_difal, "Atualização Dados |  " + LogAlteracao + " |");
                }

                db.SaveChanges();
                return Json(new
                {
                    success = true,
                    msg = "Parâmetro Difal <b>" + view_record_gc_parametros_difal.sigla.EmptyIfNull().ToString() + "</b> atualizado com sucesso!"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (DbEntityValidationException ex)
            {
                return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(GdiMvcJsonResults.AjaxFailure(e), JsonRequestBehavior.AllowGet);
            }
        }

        private static string MontarTituloCreateEditParametroDifal(gc_parametros_difal record)
        {
            return LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp"
                + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Parâmetros Difal</b>"
                + LibStringFormat.GetTabHtml(1) + record.sigla.EmptyIfNull().ToString() + " - " + record.estado.EmptyIfNull().ToString();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_FinanceiroParametroDifal_*,gc_FinanceiroParametroDifal_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_FinanceiroParametroDifal_*,gc_FinanceiroParametroDifal_Actionupdate")]
        public ActionResult Edit(gc_parametros_difal view_record_gc_parametros_difal)
        {
            if (ModelState.IsValid)
            {
                // Alterar o parâmetro difal
                view_record_gc_parametros_difal.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                view_record_gc_parametros_difal.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(view_record_gc_parametros_difal).State = EntityState.Modified;
                gc_parametros_difal record_old_gc_parametros_difal = JsonConvert.DeserializeObject<gc_parametros_difal>(CachePersister.userIdentity.DataRowInUseSerialized); ;

                // Criar o log
                String LogAlteracao = LibDB.CompareDataTable(record_old_gc_parametros_difal, view_record_gc_parametros_difal);
                if (LogAlteracao.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true,"gc_parametros_difal", view_record_gc_parametros_difal.id_parametro_difal, "Atualização Dados |  " + LogAlteracao + " |"); };

                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (DbEntityValidationException ex)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureValidationMessage(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureMessage(e));
                }
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Parâmetros Difal</b>" + LibStringFormat.GetTabHtml(1) + view_record_gc_parametros_difal.sigla.EmptyIfNull().ToString() + " - " + view_record_gc_parametros_difal.estado.EmptyIfNull().ToString();
            return View("CreateEdit", view_record_gc_parametros_difal);
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null) { db.Dispose(); };
            }
            base.Dispose(disposing);
        }
    }
}