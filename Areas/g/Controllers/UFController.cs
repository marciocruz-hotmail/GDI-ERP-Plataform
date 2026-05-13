// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Default")]
    public class UFController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_UF";

        public UFController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de UFs";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
            var allRecords = new List<Db.g_uf>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                // Não há filtro
                allRecords = db.g_uf.ToList();
            }
            if (filterDb)
            {
                SentencaSQL = string.Empty;
                if (record_g_filtro.advanced == true) { SentencaSQL = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); }
                else { SentencaSQL = "select * from g_uf where " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); };
                allRecords = db.g_uf.SqlQuery(SentencaSQL).ToList();
            }
            else if (filterAdvanced)
            {
                // Filtro Avançado - Não implementado
                allRecords = db.g_uf.ToList();
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_uf, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_uf) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.sigla :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_uf); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.sigla); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_uf); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.sigla); }
                }
            }

            foreach (var c in displayedRecords)
            {
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_uf.ToString(),
                                    c.sigla.ToString(),
                                    c.nome.ToString()
                                });
            }

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
                return JsonDataTableException(e, param, filterOnOff);
            }
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actioncreate,gdc_Pefin_Default")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>UF</b";
            g_uf newRecord = new g_uf();
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actioncreate,gdc_Pefin_Default")]
        public ActionResult Create(g_uf record_g_uf)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>UF</b";
            if (record_g_uf.nome.EmptyIfNull().ToString() != String.Empty)
            {
                record_g_uf.nome = record_g_uf.nome.ToUpper();
            }
            if (ModelState.IsValid)
            {
                IQueryable<g_uf> listaUF = db.g_uf.Where(p => p.nome == record_g_uf.nome);
                foreach (g_uf validacao in listaUF)
                {
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_uf.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                    if (validacao.sigla.ToString().ToUpper().Equals(record_g_uf.sigla.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Sigla] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_uf.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_uf.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_uf.Add(record_g_uf);
                try
                {
                    db.SaveChanges();
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

            return View("CreateEdit", record_g_uf);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_uf record_g_uf = db.g_uf.Find(id);
            if (record_g_uf == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>UF</b>" + LibStringFormat.GetTabHtml(1) + record_g_uf.id_uf.EmptyIfNull().ToString() + " - " + record_g_uf.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_uf);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actionupdate")]
        public ActionResult Edit(g_uf record_g_uf)
        {
            if (record_g_uf.nome.EmptyIfNull().ToString() != String.Empty)
            {
                record_g_uf.nome = record_g_uf.nome.ToUpper();
            }
            if (ModelState.IsValid)
            {
                IQueryable<g_uf> listaUF = db.g_uf.Where(p => (p.nome == record_g_uf.nome) && (p.id_uf != record_g_uf.id_uf));
                foreach (g_uf validacao in listaUF)
                {
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_uf.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                    if (validacao.sigla.ToString().ToUpper().Equals(record_g_uf.sigla.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Sigla] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_uf.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_uf.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_uf).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>UF</b>" + LibStringFormat.GetTabHtml(1) + record_g_uf.id_uf.EmptyIfNull().ToString() + " - " + record_g_uf.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_uf);
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