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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopOperacoes_*,g_CfopOperacoes_Default")]
    public class CfopOperacoesController : Controller
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
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CfopOperacoes_*,g_CfopOperacoes_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
            var allRecords = new List<Db.gc_cfop_operacoes>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                allRecords = db.gc_cfop_operacoes.ToList();
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
                    SentencaSQL = "select * from gc_cfop_operacoes where id_cfop_operacao > 0 and " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim();
                }

                allRecords = db.gc_cfop_operacoes.SqlQuery(SentencaSQL).ToList();
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.gc_cfop_operacoes, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_cfop_operacao) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.descricao :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_cfop_operacao); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.descricao); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_cfop_operacao); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.descricao); }
                }
            }

            foreach (var c in displayedRecords)
            {
                String _ativo = c.ativo == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_cfop_operacao.ToString(),
                                    _ativo,
                                    c.descricao.ToString()
                                });
            }

            String filterOnOff = "0";
            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; };

            return Json(new
            {
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count(),
                iTotalDisplayRecords = allRecords.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalCreateEdit
        public ActionResult ModalCreateEditOperacao(int? IdOperacao)
        {
            gc_cfop_operacoes record_gc_cfop_operacoes = new Db.gc_cfop_operacoes();
            try
            {
                if ((IdOperacao != null) && (IdOperacao > 0)) { record_gc_cfop_operacoes = db.gc_cfop_operacoes.Find(IdOperacao); };
                ViewBag.comboCFOP = LibDataSets.LoadComboGcCfop(db);
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Edição de Operação - " + record_gc_cfop_operacoes.id_cfop_operacao.EmptyIfNull().ToString() + "</b>";
                return View("ModalCreateEditOperacao", record_gc_cfop_operacoes);
            }
            catch (Exception ex)
            {
                String msg = LibExceptions.getExceptionShortMessage(ex);
                msg += "<br/>" + "CfopOperacoesController";
                msg += "<br/>" + "ModalCreateEditOperacao";
                TempData["message"] = msg;
                TempData.Keep("message");
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