using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopParametros_*,g_CfopParametros_Default")]
    public class CfopParametrosController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_CfopParametros";
        public CfopParametrosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopParametros_*,g_CfopParametros_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "CFOP/NFE - Parâmetros";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopParametros_*,g_CfopParametros_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
            var allRecords = new List<Db.gc_cfop_parametros>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                allRecords = db.gc_cfop_parametros.ToList();
            }
            if (filterDb)
            {
                SentencaSQL = string.Empty;
                if (record_g_filtro.advanced == true)
                {
                    SentencaSQL = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim();
                }
                else
                {
                    SentencaSQL = "select * from gc_cfop_parametros where id_cfop_parametro > 0 and " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim();
                }

                allRecords = db.gc_cfop_parametros.SqlQuery(SentencaSQL).ToList();
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.gc_cfop_parametros, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_cfop_parametro) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.descricao :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_cfop_parametro); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.descricao); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_cfop_parametro); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.descricao); }
                }
            }

            foreach (var c in displayedRecords)
            {
                String _ativo = LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "");
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_cfop_parametro.ToString(),
                                    _ativo,
                                    c.descricao.ToString()
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
                return JsonDataTableException(e, param);
            }
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalCreateEdit
        public ActionResult ModalCreateEditParametro(int? IdParametro)
        {
            gc_cfop_parametros record_gc_cfop_parametros = new Db.gc_cfop_parametros();
            try
            {
                if ((IdParametro != null) && (IdParametro > 0)) { record_gc_cfop_parametros = db.gc_cfop_parametros.Find(IdParametro); };
                ViewBag.comboCFOP = LibDataSets.LoadComboGcCfop(db);
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Parâmetros - " + record_gc_cfop_parametros.id_cfop_parametro.EmptyIfNull().ToString() + "</b>";
                return View("ModalCreateEditParametro", record_gc_cfop_parametros);
            }
            catch (Exception ex)
            {
                String msg = LibExceptions.getExceptionShortMessage(ex);
                msg += "<br/>" + "CfopParametrosController";
                msg += "<br/>" + "ModalCreateEditParametro";
                TempData["message"] = msg;
                TempData.Keep("message");
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        #endregion

        public ActionResult AjaxCreateEditParametro(gc_cfop_parametros view_gc_cfop_parametros)
        {
            bool sucesso = false;
            int qtdInconsistencias = 0;
            String msgRetorno = "";
            try
            {
                if (ModelState.IsValid)
                {
                    if (view_gc_cfop_parametros.descricao.EmptyIfNull().ToString().Length == 0)
                    {
                        msgRetorno += " - [Descrição] é de preenchimento obrigatório!<br/>";
                        qtdInconsistencias += 1;
                    }
                }
                else
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }

                if (qtdInconsistencias == 0)
                {
                    db.Entry(view_gc_cfop_parametros).State = EntityState.Modified;
                    db.SaveChanges();
                    msgRetorno += "Operação [" + view_gc_cfop_parametros.descricao.EmptyIfNull().ToLower() + "] ALTERADA com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
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

    }
}