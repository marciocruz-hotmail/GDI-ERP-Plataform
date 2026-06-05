using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Default")]
    public class CfopController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "gc_Cfop";

        public CfopController()
        {
            String Inicio = String.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop,gc_Cfop_*,gc_Cfop_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de CFOPs";
            var model = new CstCfopIndex
            {
                CfopIndex_id = String.Empty,
                CfopIndex_numero = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, numeroRestore;
            if (TryParseFiltroCfopSemicolon(filtroPersistido.sql_filtro, out idRestore, out numeroRestore))
            {
                model.CfopIndex_id = idRestore;
                model.CfopIndex_numero = numeroRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(numeroRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop,gc_Cfop_*,gc_Cfop_Actionread")]
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

                var baseQuery = db.gc_cfop.AsNoTracking().Where(c => c.id_cfop > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string numeroStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(numeroStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroCfopSemicolon(recordFiltro.sql_filtro, out idStr, out numeroStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(numeroStr);
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

                IQueryable<Db.gc_cfop> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroCfopNaQuery(query, idStr, numeroStr);
                    LibDB.setFilterByUser(MontarFiltroCfopPersistido(idStr, numeroStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoCfopNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(c => new { c.id_cfop, c.ativo, c.numero, c.descricao })
                    .ToList();

                var list = page.Select(c =>
                {
                    string _ativo = c.ativo == true
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                    return new[]
                    {
                        "",
                        c.id_cfop.ToString(),
                        _ativo,
                        c.numero.ToString(),
                        c.descricao ?? "",
                        "",
                        ""
                    };
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

        private static bool TryParseFiltroCfopSemicolon(string raw, out string id, out string numero)
        {
            id = numero = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            numero = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(numero);
        }

        private static string MontarFiltroCfopPersistido(string id, string numero)
        {
            return (id ?? String.Empty) + ";" + (numero ?? String.Empty);
        }

        private static IQueryable<Db.gc_cfop> AplicarFiltroCfopNaQuery(IQueryable<Db.gc_cfop> query, string idStr, string numeroStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idCfop))
            {
                query = query.Where(c => c.id_cfop == idCfop);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(numeroStr, out string padraoNumero))
            {
                query = query.Where(c => c.numero != null && DbFunctions.Like(c.numero, padraoNumero));
            }
            return query;
        }

        private static IQueryable<Db.gc_cfop> AplicarOrdenacaoCfopNaQuery(IQueryable<Db.gc_cfop> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 3)
                {
                    return asc ? query.OrderBy(c => c.numero) : query.OrderByDescending(c => c.numero);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(c => c.id_cfop) : query.OrderByDescending(c => c.id_cfop);
                }
            }
            return query.OrderBy(c => c.numero);
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Actioncreate")]
        public ActionResult Create()
        {
            gc_cfop newRecord = new gc_cfop();
            newRecord.ativo = true;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>CFOP</b";
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Actioncreate")]
        public ActionResult Create(gc_cfop record_gc_cfop)
        {
            record_gc_cfop.id_coligada = 1;
            record_gc_cfop.id_filial = 1;
            record_gc_cfop.descricao = record_gc_cfop.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<gc_cfop> listaCfop = db.gc_cfop.Where(p => p.descricao == record_gc_cfop.descricao);
                foreach (gc_cfop validacao in listaCfop)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_gc_cfop.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_gc_cfop.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_gc_cfop.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.gc_cfop.Add(record_gc_cfop);
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>CFOP</b";
            return View("CreateEdit", record_gc_cfop);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            gc_cfop record_g_cfop = db.gc_cfop.Find(id);
            if (record_g_cfop == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>CFOP</b>" + LibStringFormat.GetTabHtml(1) + record_g_cfop.id_cfop.EmptyIfNull().ToString() + " - " + record_g_cfop.numero.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_cfop);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_Cfop_*,gc_Cfop_Actionupdate")]
        public ActionResult Edit(gc_cfop record_gc_cfop)
        {
            record_gc_cfop.descricao = record_gc_cfop.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<gc_cfop> listaCfop = db.gc_cfop.Where(p => (p.descricao == record_gc_cfop.descricao) && (p.id_cfop != record_gc_cfop.id_cfop));
                foreach (gc_cfop validacao in listaCfop)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_gc_cfop.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_gc_cfop.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_gc_cfop.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_gc_cfop).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>CFOP</b>" + LibStringFormat.GetTabHtml(1) + record_gc_cfop.id_cfop.EmptyIfNull().ToString() + " - " + record_gc_cfop.numero.EmptyIfNull().ToString();
            return View("CreateEdit", record_gc_cfop);
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