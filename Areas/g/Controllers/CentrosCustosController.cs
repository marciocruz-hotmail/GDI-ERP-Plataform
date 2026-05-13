using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using GdiPlataform.Controllers;
using GdiPlataform.Models;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Default")]
    public class CentrosCustosController : Controller
    {
        private GdiPlataformEntities db;
        public CentrosCustosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Centros de Custos";
            return View();
        }

        #region preencherLookupsCreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actioncreate,g_CentrosCustos_Actionupdate")]
        public void preencherLookupsCreateEdit()
        {
            var comboCentroCustoPai = new List<SelectListItem>();
            comboCentroCustoPai.Add(new SelectListItem { Value = "0", Text = "RAIZ" });
            try
            {
                IQueryable<g_centros_custos> listaDbCentrosCustos = db.g_centros_custos.Select(p => p).OrderBy(p => p.codigo);
                foreach (g_centros_custos item1 in listaDbCentrosCustos)
                {
                    comboCentroCustoPai.Add(new SelectListItem { Value = item1.id_centro_custo.ToString(), Text = item1.codigo.ToString() + " - " + item1.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboCentroCustoPai = comboCentroCustoPai;

        }
        #endregion

        #region getDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actionread")]
        public ActionResult getDados(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            if (!param.yesFilterField.EmptyIfNull().ToString().Equals(String.Empty)) { filterOnOff = "1"; }

            var allRecords = new List<Db.g_centros_custos>();
            List<string[]> list = new List<string[]>();

            if (param.yesFilterField.EmptyIfNull().ToString().Equals(String.Empty))
            {
                allRecords = db.g_centros_custos.Where(x => x.id_centro_custo > 0).OrderBy(x => x.codigo).ToList();
            }
            else
            {
                String SentencaSQL = "select * from g_centros_custos where id_centro_custo > 0 and ";
                SentencaSQL += LibStringFormat.SentencaSQLFiltroGenerico(param.yesFilterField, param.yesFilterOperador, param.yesFilterText);
                allRecords = db.g_centros_custos.SqlQuery(SentencaSQL.ToString()).ToList();
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_centros_custos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_centro_custo) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? Convert.ToString(c.id_centro_custo_pai) :
                                     param.iSortCol_0 == 4 && param.iSortingCols > 0 ? Convert.ToString(c.consolidador) :
                                     param.iSortCol_0 == 3 && param.iSortingCols > 0 ? c.codigo :
                                     param.iSortCol_0 == 3 && param.iSortingCols > 0 ? c.nome :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_centro_custo); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.id_centro_custo_pai); }
                    else if (param.iSortCol_0 == 3) { displayedRecords = displayedRecords.OrderBy(c => c.consolidador); }
                    else if (param.iSortCol_0 == 4) { displayedRecords = displayedRecords.OrderBy(c => c.codigo); }
                    else if (param.iSortCol_0 == 5) { displayedRecords = displayedRecords.OrderBy(c => c.nome); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_centro_custo); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_centro_custo_pai); }
                    else if (param.iSortCol_0 == 3) { displayedRecords = displayedRecords.OrderByDescending(c => c.consolidador); }
                    else if (param.iSortCol_0 == 4) { displayedRecords = displayedRecords.OrderByDescending(c => c.codigo); }
                    else if (param.iSortCol_0 == 5) { displayedRecords = displayedRecords.OrderByDescending(c => c.nome); }
                }
            }

            String _consolidador = String.Empty;
            String _nomeCentroCustoPai = String.Empty;
            var allCentroCustoPai = db.g_centros_custos.ToList();

            foreach (g_centros_custos c in displayedRecords)
            {

                _consolidador = c.consolidador == true ? LibIcons.getIcon("fa-solid fa-check", "", "", "fa-lg") : "";

                if (c.id_centro_custo_pai == 0)
                {
                    _nomeCentroCustoPai = "RAIZ";
                }
                else if (c.id_centro_custo_pai > 0)
                {
                    var pai = allCentroCustoPai.Find(e => e.id_centro_custo == c.id_centro_custo_pai);
                    _nomeCentroCustoPai = pai != null ? pai.nome.EmptyIfNull().ToString() : "ERRO";
                }

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_centro_custo.ToString(),
                                    _nomeCentroCustoPai,
                                    _consolidador.ToString(),
                                    c.codigo.ToString(),
                                    c.nome.ToString()
                                });
            }


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

        #region GetTreeViewCentroCusto
        public JsonResult GetTreeViewCentroCusto()
        {
            var root = new JsTree3Node() // Create our root node and ensure it is opened
            {
                id = "-1",
                text = "Árvore - Centros de Custos",
                icon = "/LibUI_AdminLTE-4.0.0/plugins/jstree-3.3.4/images/icons8-genealogy-24.png",
                state = new State(true, false, false)
            };
            var children = new List<JsTree3Node>();
            //String nameIconAgrupador = "/LibUI_AdminLTE-4.0.0/plugins/jstree-3.3.4/images/icons8-genealogy-24.png";
            //String nameIconCentroCusto = "/LibUI_AdminLTE-4.0.0/plugins/jstree-3.3.4/images/icons8-pie-chart-report-script-24.png";
            String nameIconAgrupador = "fa-regular fa-folder-open";
            String nameIconCentroCusto = "fa-regular fa-file";
            var listaCentrosCustos = (from _c1 in db.g_centros_custos select new { g_centros_custos = _c1 }).ToList();

            // Nível 1
            var listaCentrosCustosN1 = listaCentrosCustos.Where(N1 => N1.g_centros_custos.id_centro_custo_pai == 0).OrderBy(N1 => N1.g_centros_custos.codigo).ToList();
            foreach (var CentrosCustosN1 in listaCentrosCustosN1)
            {
                var nodeN1 = JsTree3Node.NewNode(CentrosCustosN1.g_centros_custos.codigo.ToString() + " - " + CentrosCustosN1.g_centros_custos.nome.ToString());
                nodeN1.id = CentrosCustosN1.g_centros_custos.id_centro_custo.ToString();
                nodeN1.text = CentrosCustosN1.g_centros_custos.codigo.ToString() + " - " + CentrosCustosN1.g_centros_custos.nome.ToString();
                nodeN1.icon = nameIconAgrupador;
                nodeN1.state = new State(true, false, false);

                // Nível 2
                var listaCentrosCustosN2 = listaCentrosCustos.Where(N2 => (N2.g_centros_custos.id_centro_custo_pai == CentrosCustosN1.g_centros_custos.id_centro_custo) && (N2.g_centros_custos.id_centro_custo != CentrosCustosN1.g_centros_custos.id_centro_custo)).OrderBy(N2 => N2.g_centros_custos.codigo).ToList();
                foreach (var CentrosCustosN2 in listaCentrosCustosN2)
                {
                    var nodeN2 = JsTree3Node.NewNode(CentrosCustosN2.g_centros_custos.codigo.ToString() + " - " + CentrosCustosN2.g_centros_custos.nome.ToString());
                    nodeN2.id = CentrosCustosN2.g_centros_custos.id_centro_custo.ToString();
                    nodeN2.text = CentrosCustosN2.g_centros_custos.codigo.ToString() + " - " + CentrosCustosN2.g_centros_custos.nome.ToString();
                    nodeN2.icon = nameIconCentroCusto;
                    nodeN2.state = new State(true, false, false);


                    // Nível 3
                    var listaCentrosCustosN3 = listaCentrosCustos.Where(N3 => (N3.g_centros_custos.id_centro_custo_pai == CentrosCustosN2.g_centros_custos.id_centro_custo) && (N3.g_centros_custos.id_centro_custo != CentrosCustosN2.g_centros_custos.id_centro_custo)).OrderBy(N3 => N3.g_centros_custos.codigo).ToList();
                    foreach (var CentrosCustosN3 in listaCentrosCustosN3)
                    {
                        var nodeN3 = JsTree3Node.NewNode(CentrosCustosN3.g_centros_custos.codigo.ToString() + " - " + CentrosCustosN3.g_centros_custos.nome.ToString());
                        nodeN3.id = CentrosCustosN3.g_centros_custos.id_centro_custo.ToString();
                        nodeN3.text = CentrosCustosN3.g_centros_custos.codigo.ToString() + " - " + CentrosCustosN3.g_centros_custos.nome.ToString();
                        nodeN3.icon = nameIconCentroCusto;
                        nodeN3.state = new State(true, false, false);

                        // Nível 4
                        var listaCentrosCustosN4 = listaCentrosCustos.Where(N4 => (N4.g_centros_custos.id_centro_custo_pai == CentrosCustosN3.g_centros_custos.id_centro_custo) && (N4.g_centros_custos.id_centro_custo != CentrosCustosN3.g_centros_custos.id_centro_custo)).OrderBy(N3 => N3.g_centros_custos.codigo).ToList();
                        foreach (var CentrosCustosN4 in listaCentrosCustosN4)
                        {
                            var nodeN4 = JsTree3Node.NewNode(CentrosCustosN4.g_centros_custos.codigo.ToString() + " - " + CentrosCustosN4.g_centros_custos.nome.ToString());
                            nodeN4.id = CentrosCustosN4.g_centros_custos.id_centro_custo.ToString();
                            nodeN4.text = CentrosCustosN4.g_centros_custos.codigo.ToString() + " - " + CentrosCustosN4.g_centros_custos.nome.ToString();
                            nodeN4.icon = nameIconCentroCusto;
                            nodeN4.state = new State(true, false, false);
                            nodeN3.children.Add(nodeN4);
                        }
                        if (listaCentrosCustosN4.Count() > 0) { nodeN3.icon = nameIconAgrupador; }

                        nodeN2.children.Add(nodeN3);
                    }
                    if (listaCentrosCustosN3.Count() > 0) { nodeN2.icon = nameIconAgrupador; }

                    nodeN1.children.Add(nodeN2);
                }

                children.Add(nodeN1);
            }

            // Add the sturcture to the root nodes children property
            root.children = children;

            // Return the object as JSON
            return Json(root, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actioncreate")]
        public ActionResult Create()
        {
            preencherLookupsCreateEdit();
            g_centros_custos newRecord = new g_centros_custos();
            newRecord.ativo = true;
            newRecord.id_coligada = 0;  // Definição de que Clientes Categorias Sub é Global
            newRecord.id_filial = 0;    // Definição de que Clientes Categorias Sub é Global            
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Novo Centro de Custo</b";
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actioncreate")]
        public ActionResult Create(g_centros_custos record_g_centros_custos)
        {
            record_g_centros_custos.nome = record_g_centros_custos.nome.ToUpper();
            if (ModelState.IsValid)
            {

                if (record_g_centros_custos.codigo.EmptyIfNull().ToString().Equals(String.Empty))
                {
                    { ModelState.AddModelError("Model", "Campo [Código] é de preenchimento obrigatório"); }
                }

                IQueryable<g_centros_custos> listaCentrosCustos = db.g_centros_custos.Where(p => p.codigo == record_g_centros_custos.codigo || p.nome == record_g_centros_custos.nome);
                foreach (g_centros_custos validacao in listaCentrosCustos)
                {

                    // Validação Código
                    if ((validacao.codigo != null) && (validacao.codigo != String.Empty))
                    {
                        if (validacao.codigo.ToString().ToUpper().Equals(record_g_centros_custos.codigo.EmptyIfNull().ToString().ToUpper()))
                        { ModelState.AddModelError("Model", "Campo [Códido] duplicado na base de dados [Id.: " + validacao.id_centro_custo + " / Centro de Custo: " + validacao.nome.ToString() + "]"); }
                    }

                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_centros_custos.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_centros_custos.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_centros_custos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.g_centros_custos.Add(record_g_centros_custos);
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

            preencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Novo Centro de Custo</b";
            return View("CreateEdit", record_g_centros_custos);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_centros_custos record_g_centros_custos = db.g_centros_custos.Find(id);
            if (record_g_centros_custos == null)
            {
                return RedirectToAction("Index", "Error", new { area = "" });
            }
            preencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Centro de Custo</b>" + LibStringFormat.GetTabHtml(1) + record_g_centros_custos.id_centro_custo.EmptyIfNull().ToString() + " - " + record_g_centros_custos.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_centros_custos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actionupdate")]
        public ActionResult Edit(g_centros_custos record_g_centros_custos)
        {
            record_g_centros_custos.nome = record_g_centros_custos.nome.ToUpper();
            if (ModelState.IsValid)
            {
                IQueryable<g_centros_custos> listaCentrosCustos = db.g_centros_custos.Where(p => (p.codigo == record_g_centros_custos.codigo || p.nome == record_g_centros_custos.nome) && (p.id_centro_custo != record_g_centros_custos.id_centro_custo));
                foreach (g_centros_custos validacao in listaCentrosCustos)
                {
                    // Validação Código
                    if ((validacao.codigo != null) && (validacao.codigo != String.Empty))
                    {
                        if (validacao.codigo.ToString().ToUpper().Equals(record_g_centros_custos.codigo.EmptyIfNull().ToString().ToUpper()))
                        { ModelState.AddModelError("Model", "Campo [Códido] duplicado na base de dados [Id.: " + validacao.id_centro_custo + " / Centro de Custo: " + validacao.nome.ToString() + "]"); }
                    }

                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_centros_custos.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_centros_custos.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_centros_custos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_centros_custos).State = EntityState.Modified;
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

            preencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Centro de Custo</b>" + LibStringFormat.GetTabHtml(1) + record_g_centros_custos.id_centro_custo.EmptyIfNull().ToString() + " - " + record_g_centros_custos.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_centros_custos);
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

        [CustomAuthorize(Roles = "*")]
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