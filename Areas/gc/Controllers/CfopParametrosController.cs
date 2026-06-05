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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopParametros_*,g_CfopParametros_Default")]
    public partial class CfopParametrosController : Controller
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
            var model = new CstCfopParametrosIndex
            {
                CfopParametrosIndex_id = String.Empty,
                CfopParametrosIndex_descricao = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, descRestore;
            if (TryParseFiltroCfopParametrosSemicolon(filtroPersistido.sql_filtro, out idRestore, out descRestore))
            {
                model.CfopParametrosIndex_id = idRestore;
                model.CfopParametrosIndex_descricao = descRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(descRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopParametros_*,g_CfopParametros_Actionread")]
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

                var baseQuery = db.gc_cfop_parametros.AsNoTracking().Where(c => c.id_cfop_parametro > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string descStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(descStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroCfopParametrosSemicolon(recordFiltro.sql_filtro, out idStr, out descStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(descStr);
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

                IQueryable<Db.gc_cfop_parametros> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroCfopParametrosNaQuery(query, idStr, descStr);
                    LibDB.setFilterByUser(MontarFiltroCfopParametrosPersistido(idStr, descStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoCfopParametrosNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(c => new { c.id_cfop_parametro, c.descricao })
                    .ToList();

                var list = page.Select(c => new[]
                {
                    "",
                    c.id_cfop_parametro.ToString(),
                    LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", ""),
                    c.descricao ?? ""
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

        private static bool TryParseFiltroCfopParametrosSemicolon(string raw, out string id, out string descricao)
        {
            id = descricao = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            descricao = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(descricao);
        }

        private static string MontarFiltroCfopParametrosPersistido(string id, string descricao)
        {
            return (id ?? String.Empty) + ";" + (descricao ?? String.Empty);
        }

        private static IQueryable<Db.gc_cfop_parametros> AplicarFiltroCfopParametrosNaQuery(IQueryable<Db.gc_cfop_parametros> query, string idStr, string descStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idParametro))
            {
                query = query.Where(c => c.id_cfop_parametro == idParametro);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(descStr, out string padraoDesc))
            {
                query = query.Where(c => c.descricao != null && DbFunctions.Like(c.descricao, padraoDesc));
            }
            return query;
        }

        private static IQueryable<Db.gc_cfop_parametros> AplicarOrdenacaoCfopParametrosNaQuery(IQueryable<Db.gc_cfop_parametros> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 3)
                {
                    return asc ? query.OrderBy(c => c.descricao) : query.OrderByDescending(c => c.descricao);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(c => c.id_cfop_parametro) : query.OrderByDescending(c => c.id_cfop_parametro);
                }
            }
            return query.OrderBy(c => c.id_cfop_parametro);
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalCreateEdit
        public ActionResult ModalCreateEditParametro(int? IdParametro)
        {
            gc_cfop_parametros record_gc_cfop_parametros = new Db.gc_cfop_parametros();
            try
            {
                if ((IdParametro != null) && (IdParametro > 0))
                {
                    record_gc_cfop_parametros = db.gc_cfop_parametros.Find(IdParametro);
                    if (record_gc_cfop_parametros == null)
                    {
                        ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Parâmetro CFOP", IdParametro);
                        PreencherLookupsCfop();
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Parâmetros — (não localizado)</b>";
                        return View("ModalCreateEditParametro", new gc_cfop_parametros { id_cfop_parametro = IdParametro.GetValueOrDefault() });
                    }
                }
                PreencherLookupsCfop();
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Parâmetros - " + record_gc_cfop_parametros.id_cfop_parametro.EmptyIfNull().ToString() + "</b>";
                return View("ModalCreateEditParametro", record_gc_cfop_parametros);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "CfopParametrosController";
                msg += "<br/>" + "ModalCreateEditParametro";
                LibFlashMessage.SetModalMessage(this, msg);
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
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

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