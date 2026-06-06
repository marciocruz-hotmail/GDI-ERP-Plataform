using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
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
        private readonly String controllerName = "g_CentrosCustos";

        public CentrosCustosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actionread")]
        [GdiPageScripts(GdiPageScriptsFlags.LayoutHubJstree)]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Centros de Custos";
            var treeSerializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            ViewBag.CentrosCustosTreeJson = treeSerializer.Serialize(MontarArvoreCentrosCustos());
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
            var allRecords = new List<Db.g_centros_custos>();
            List<string[]> list = new List<string[]>();

            g_filtros recordFiltro = LibDB.getFilterByUser(param, controllerName, db);
            bool filterDb = recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0;
            if (filterDb) { filterOnOff = "1"; }

            if (!filterDb)
            {
                allRecords = db.g_centros_custos.Where(x => x.id_centro_custo > 0).OrderBy(x => x.codigo).ToList();
            }
            else
            {
                string sentencaSql = recordFiltro.advanced == true
                    ? recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim()
                    : "select * from g_centros_custos where id_centro_custo > 0 and " + recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim();
                allRecords = db.g_centros_custos.SqlQuery(sentencaSql).ToList();
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
                errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
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
        private const string IconePastaCentroCusto = "fa-regular fa-folder-open";
        private const string IconeFolhaCentroCusto = "fa-regular fa-file";

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_CentrosCustos_*,g_CentrosCustos_Actionread")]
        public JsonResult GetTreeViewCentroCusto()
        {
            return Json(MontarArvoreCentrosCustos(), JsonRequestBehavior.AllowGet);
        }

        private JsTree3Node MontarArvoreCentrosCustos()
        {
            if (db == null)
            {
                return CriarNoRaizArvoreCentrosCustos("Árvore - Centros de Custos (sessão inválida)");
            }

            try
            {
                var lista = db.g_centros_custos.AsNoTracking()
                    .Where(x => x.id_centro_custo > 0)
                    .OrderBy(x => x.codigo)
                    .ToList();

                var filhosPorPai = lista
                    .GroupBy(x => x.id_centro_custo_pai ?? 0)
                    .ToDictionary(g => g.Key, g => g.OrderBy(x => x.codigo).ToList());

                var raizes = lista
                    .Where(cc => (cc.id_centro_custo_pai ?? 0) == 0)
                    .OrderBy(cc => cc.codigo)
                    .ToList();

                var root = CriarNoRaizArvoreCentrosCustos("Árvore - Centros de Custos");
                var visitados = new HashSet<int>();
                root.children = raizes
                    .Select(cc => CriarNoArvoreCentrosCustos(cc, filhosPorPai, visitados))
                    .Where(node => node != null)
                    .ToList();

                return root;
            }
            catch (Exception ex)
            {
                return CriarNoRaizArvoreCentrosCustos("Árvore - Centros de Custos (erro: " + GdiMvcJsonResults.AjaxFailureMessage(ex) + ")");
            }
        }

        private static JsTree3Node CriarNoRaizArvoreCentrosCustos(string titulo)
        {
            return new JsTree3Node
            {
                id = "-1",
                text = titulo,
                icon = "/LibUI_AdminLTE-4.0.0/plugins/startprime/images/icons8-genealogy-24.png",
                state = new State(true, false, false),
                children = new List<JsTree3Node>()
            };
        }

        private static JsTree3Node CriarNoArvoreCentrosCustos(
            g_centros_custos cc,
            Dictionary<int, List<g_centros_custos>> filhosPorPai,
            HashSet<int> visitados)
        {
            if (!visitados.Add(cc.id_centro_custo))
            {
                return null;
            }

            List<g_centros_custos> filhos;
            if (!filhosPorPai.TryGetValue(cc.id_centro_custo, out filhos))
            {
                filhos = new List<g_centros_custos>();
            }

            var texto = cc.codigo.EmptyIfNull().ToString() + " - " + cc.nome.EmptyIfNull().ToString();
            var node = JsTree3Node.NewNode(texto);
            node.id = cc.id_centro_custo.ToString();
            node.text = texto;
            node.state = new State(true, false, false);
            node.icon = filhos.Count > 0 ? IconePastaCentroCusto : IconeFolhaCentroCusto;

            foreach (var filho in filhos)
            {
                if (filho.id_centro_custo == cc.id_centro_custo)
                {
                    continue;
                }
                var childNode = CriarNoArvoreCentrosCustos(filho, filhosPorPai, visitados);
                if (childNode != null)
                {
                    node.children.Add(childNode);
                }
            }

            return node;
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
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureValidationMessage(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureMessage(e));
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
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureValidationMessage(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", GdiMvcJsonResults.AjaxFailureMessage(e));
                }
            }

            preencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Centro de Custo</b>" + LibStringFormat.GetTabHtml(1) + record_g_centros_custos.id_centro_custo.EmptyIfNull().ToString() + " - " + record_g_centros_custos.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_centros_custos);
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