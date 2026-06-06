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
                        errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                        stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
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
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actionupdate")]
        public ActionResult ModalCreateEditUF(int? IdUf)
        {
            try
            {
                int idUf = IdUf.GetValueOrDefault();
                if (idUf <= 0)
                {
                    return RedirectToAction("Index");
                }
                g_uf record_g_uf = db.g_uf.Find(idUf);
                if (record_g_uf == null)
                {
                    return RedirectToAction("Index");
                }
                ViewBag.Title = MontarTituloCreateEditUF(record_g_uf);
                return View("ModalCreateEditUF", record_g_uf);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "UFController";
                msg += "<br/>" + "ModalCreateEditUF";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actionupdate")]
        public ActionResult AjaxCreateEditUF(g_uf view_g_uf)
        {
            try
            {
                if (view_g_uf.id_uf <= 0)
                {
                    return Json(GdiMvcJsonResults.AjaxFailure(GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("UF", view_g_uf.id_uf)), JsonRequestBehavior.AllowGet);
                }

                g_uf existente = db.g_uf.Find(view_g_uf.id_uf);
                if (existente == null)
                {
                    return Json(GdiMvcJsonResults.AjaxFailure(GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("UF", view_g_uf.id_uf)), JsonRequestBehavior.AllowGet);
                }

                existente.basemg_icms_percentual_interno = view_g_uf.basemg_icms_percentual_interno;
                existente.basemg_icms_interestadual = view_g_uf.basemg_icms_interestadual;
                existente.basemg_icms_base_reducao = view_g_uf.basemg_icms_base_reducao;
                existente.basesp_icms_percentual_interno = view_g_uf.basesp_icms_percentual_interno;
                existente.basesp_icms_interestadual = view_g_uf.basesp_icms_interestadual;
                existente.basesp_icms_base_reducao = view_g_uf.basesp_icms_base_reducao;
                existente.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                existente.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.SaveChanges();
                return Json(new { success = true, msg = "UF <b>" + existente.sigla.EmptyIfNull().ToString() + "</b> atualizada com sucesso!" }, JsonRequestBehavior.AllowGet);
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

        private static string MontarTituloCreateEditUF(g_uf record)
        {
            return LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp"
                + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>UF</b>"
                + LibStringFormat.GetTabHtml(1) + record.id_uf.EmptyIfNull().ToString() + " - " + record.nome.EmptyIfNull().ToString();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actioncreate,gdc_Pefin_Default")]
        public ActionResult Create()
        {
            return RedirectToAction("Index");
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
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureValidationMessage(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureMessage(e));
                }
            }

            return View("CreateEdit", record_g_uf);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_UF_*,g_UF_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            return RedirectToAction("Index");
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
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureValidationMessage(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureMessage(e));
                }
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>UF</b>" + LibStringFormat.GetTabHtml(1) + record_g_uf.id_uf.EmptyIfNull().ToString() + " - " + record_g_uf.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_uf);
        }
        #endregion

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
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