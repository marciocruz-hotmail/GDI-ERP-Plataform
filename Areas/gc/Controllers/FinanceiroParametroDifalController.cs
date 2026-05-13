using Newtonsoft.Json;
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
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_FinanceiroParametroDifal_*,gc_FinanceiroParametroDifal_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
            var allRecords = new List<Db.gc_parametros_difal>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                // Não há filtro
                //allRecords = db.g_cidades.Where(c => c.ativo == true).ToList();
                allRecords = db.gc_parametros_difal.ToList();
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
                    SentencaSQL = "select * from gc_parametros_difal where id_parametro_difal > 1 and " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim();
                }

                allRecords = db.gc_parametros_difal.SqlQuery(SentencaSQL).ToList();
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.gc_parametros_difal, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_parametro_difal) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.sigla :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_parametro_difal); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.sigla); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_parametro_difal); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.sigla); }
                }
            }

            foreach (var c in displayedRecords)
            {
                String _DifalGeralCalcular = c.difal_geral_calcular == true ? LibIcons.getIcon("fa-solid fa-circle", "Difal Geral Calcular - Sim", "green", "") : "";
                String _DifalGeralZerar = c.difal_geral_zerar == true ? LibIcons.getIcon("fa-solid fa-circle", "Difal Geral Zerar - Sim", "green", "") : "";
                String _DifalGeralNaoinformar = c.difal_geral_naoinformar == true ? LibIcons.getIcon("fa-solid fa-circle", "Difal Geral Não Informar - Sim", "green", "") : "";
                String _DifalCombCalcular = c.difal_comb_calcular == true ? LibIcons.getIcon("fa-solid fa-circle", "Difal Combustível Calcular - Sim", "green", "") : "";
                String _DifalCombZerar = c.difal_comb_zerar == true ? LibIcons.getIcon("fa-solid fa-circle", "Difal Combustível Zerar - Sim", "green", "") : "";
                String _DifalCombNaoinformar = c.difal_comb_naoinformar == true ? LibIcons.getIcon("fa-solid fa-circle", "Difal Combustível Não Informar - Sim", "green", "") : "";

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_parametro_difal.EmptyIfNull().ToString(),
                                    c.sigla.EmptyIfNull().ToString(),
                                    c.estado.EmptyIfNull().ToString(),
                                    _DifalGeralCalcular, _DifalGeralZerar, _DifalGeralNaoinformar,
                                    _DifalCombCalcular, _DifalCombZerar, _DifalCombNaoinformar
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

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_FinanceiroParametroDifal_*,gc_FinanceiroParametroDifal_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            gc_parametros_difal record_gc_parametros_difal = db.gc_parametros_difal.Find(id);
            CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record_gc_parametros_difal);
            if (record_gc_parametros_difal == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Parâmetros Difal</b>" + LibStringFormat.GetTabHtml(1) + record_gc_parametros_difal.sigla.EmptyIfNull().ToString() + " - " + record_gc_parametros_difal.estado.EmptyIfNull().ToString();
            return View("CreateEdit", record_gc_parametros_difal);
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
                    ModelState.AddModelError("Model", LibExceptions.getDbEntityValidationException(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", LibExceptions.getExceptionShortMessage(e));
                }
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Parâmetros Difal</b>" + LibStringFormat.GetTabHtml(1) + view_record_gc_parametros_difal.sigla.EmptyIfNull().ToString() + " - " + view_record_gc_parametros_difal.estado.EmptyIfNull().ToString();
            return View("CreateEdit", view_record_gc_parametros_difal);
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