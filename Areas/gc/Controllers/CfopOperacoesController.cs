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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopOperacoes_*,g_CfopOperacoes_Default")]
    public partial class CfopOperacoesController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_CfopOperacoes";
        public CfopOperacoesController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopOperacoes_*,g_CfopOperacoes_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "CFOP/NFE - Operações";
            var model = new CstCfopOperacoesIndex
            {
                CfopOperacoesIndex_id = String.Empty,
                CfopOperacoesIndex_descricao = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, descRestore;
            if (TryParseFiltroCfopOperacoesSemicolon(filtroPersistido.sql_filtro, out idRestore, out descRestore))
            {
                model.CfopOperacoesIndex_id = idRestore;
                model.CfopOperacoesIndex_descricao = descRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(descRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopOperacoes_*,g_CfopOperacoes_Actionread")]
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

                var baseQuery = db.gc_cfop_operacoes.AsNoTracking().Where(c => c.id_cfop_operacao > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string descStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(descStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroCfopOperacoesSemicolon(recordFiltro.sql_filtro, out idStr, out descStr);
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

                IQueryable<Db.gc_cfop_operacoes> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroCfopOperacoesNaQuery(query, idStr, descStr);
                    LibDB.setFilterByUser(MontarFiltroCfopOperacoesPersistido(idStr, descStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoCfopOperacoesNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(c => new { c.id_cfop_operacao, c.ativo, c.descricao })
                    .ToList();

                var list = page.Select(c =>
                {
                    string _ativo = c.ativo == true
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                    return new[]
                    {
                        "",
                        c.id_cfop_operacao.ToString(),
                        _ativo,
                        c.descricao ?? ""
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

        private static bool TryParseFiltroCfopOperacoesSemicolon(string raw, out string id, out string descricao)
        {
            id = descricao = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            descricao = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(descricao);
        }

        private static string MontarFiltroCfopOperacoesPersistido(string id, string descricao)
        {
            return (id ?? String.Empty) + ";" + (descricao ?? String.Empty);
        }

        private static IQueryable<Db.gc_cfop_operacoes> AplicarFiltroCfopOperacoesNaQuery(IQueryable<Db.gc_cfop_operacoes> query, string idStr, string descStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idOperacao))
            {
                query = query.Where(c => c.id_cfop_operacao == idOperacao);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(descStr, out string padraoDesc))
            {
                query = query.Where(c => c.descricao != null && DbFunctions.Like(c.descricao, padraoDesc));
            }
            return query;
        }

        private static IQueryable<Db.gc_cfop_operacoes> AplicarOrdenacaoCfopOperacoesNaQuery(IQueryable<Db.gc_cfop_operacoes> query, jQueryDataTableParamModel param)
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
                    return asc ? query.OrderBy(c => c.id_cfop_operacao) : query.OrderByDescending(c => c.id_cfop_operacao);
                }
            }
            return query.OrderBy(c => c.id_cfop_operacao);
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalCreateEdit
        public ActionResult ModalCreateEditOperacao(int? IdOperacao)
        {
            gc_cfop_operacoes record_gc_cfop_operacoes = new Db.gc_cfop_operacoes();
            try
            {
                if ((IdOperacao != null) && (IdOperacao > 0))
                {
                    record_gc_cfop_operacoes = db.gc_cfop_operacoes.Find(IdOperacao);
                    if (record_gc_cfop_operacoes == null)
                    {
                        ViewBag.MsgBloqueio = GdiMvcJsonResults.EntidadeNaoEncontradaMensagem("Operação CFOP", IdOperacao);
                        PreencherLookupsCfop();
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Operação — (não localizada)</b>";
                        return View("ModalCreateEditOperacao", new gc_cfop_operacoes { id_cfop_operacao = IdOperacao.GetValueOrDefault() });
                    }
                }
                PreencherLookupsCfop();
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Operação - " + record_gc_cfop_operacoes.id_cfop_operacao.EmptyIfNull().ToString() + "</b>";
                return View("ModalCreateEditOperacao", record_gc_cfop_operacoes);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "CfopOperacoesController";
                msg += "<br/>" + "ModalCreateEditOperacao";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        #endregion

        public ActionResult AjaxCreateEditOperacao(gc_cfop_operacoes view_gc_cfop_operacoes)
        {
            bool sucesso = false;
            int qtdInconsistencias = 0;
            String msgRetorno = "";

            try
            {
                if (ModelState.IsValid)
                {
                    if (view_gc_cfop_operacoes.descricao.EmptyIfNull().ToString().Length == 0) 
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
                    db.Entry(view_gc_cfop_operacoes).State = EntityState.Modified;
                    db.SaveChanges();
                    msgRetorno += "Operação [" + view_gc_cfop_operacoes.descricao.EmptyIfNull().ToLower() + "] ALTERADA com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>";
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