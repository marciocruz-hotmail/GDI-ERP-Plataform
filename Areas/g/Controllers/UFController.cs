// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.g.Models;
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
            var model = new CstUfIndex
            {
                UfIndex_id_uf = String.Empty,
                UfIndex_sigla = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, siglaRestore;
            if (TryParseFiltroUfSemicolon(filtroPersistido.sql_filtro, out idRestore, out siglaRestore))
            {
                model.UfIndex_id_uf = idRestore;
                model.UfIndex_sigla = siglaRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(siglaRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actionread")]
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

                var baseQuery = db.g_uf.AsNoTracking().Where(u => u.id_uf > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string siglaStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(siglaStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroUfSemicolon(recordFiltro.sql_filtro, out idStr, out siglaStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(siglaStr);
                }

                if (!listarTodosExplicito && !hasInline)
                {
                    return Json(new
                    {
                        errorMessage = "",
                        stackTrace = "",
                        yesFilterOnOff = "0",
                        sEcho = param.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = 0,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                IQueryable<Db.g_uf> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroUfNaQuery(query, idStr, siglaStr);
                    LibDB.setFilterByUser(MontarFiltroUfPersistido(idStr, siglaStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoUfNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(u => new { u.id_uf, u.sigla, u.nome })
                    .ToList();

                var list = page.Select(u => new[]
                {
                    "",
                    u.id_uf.ToString(),
                    u.sigla ?? "",
                    u.nome ?? ""
                }).ToList();

                filterOnOff = filterApplied ? "1" : "0";

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
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

        private static bool TryParseFiltroUfSemicolon(string raw, out string id, out string sigla)
        {
            id = sigla = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            sigla = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(sigla);
        }

        private static string MontarFiltroUfPersistido(string id, string sigla)
        {
            return (id ?? String.Empty) + ";" + (sigla ?? String.Empty);
        }

        private static IQueryable<Db.g_uf> AplicarFiltroUfNaQuery(IQueryable<Db.g_uf> query, string idStr, string siglaStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idUf))
            {
                query = query.Where(u => u.id_uf == idUf);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemCodigo(siglaStr, out string padraoSigla))
            {
                query = query.Where(u => u.sigla != null && DbFunctions.Like(u.sigla, padraoSigla));
            }
            return query;
        }

        private static IQueryable<Db.g_uf> AplicarOrdenacaoUfNaQuery(IQueryable<Db.g_uf> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 3)
                {
                    return asc ? query.OrderBy(u => u.nome) : query.OrderByDescending(u => u.nome);
                }
                if (param.iSortCol_0 == 2)
                {
                    return asc ? query.OrderBy(u => u.sigla) : query.OrderByDescending(u => u.sigla);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(u => u.id_uf) : query.OrderByDescending(u => u.id_uf);
                }
            }
            return query.OrderBy(u => u.id_uf);
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