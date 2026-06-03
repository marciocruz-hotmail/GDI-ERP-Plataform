using GdiPlataform.Models;
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

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Default")]
    public class ClassificacaoFinanceiraController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_ClassificacaoFinanceira";

        public ClassificacaoFinanceiraController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Actionread")]
        [GdiPageScripts(GdiPageScriptsFlags.LayoutHubJstree)]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Classificação Financeira";
            return View();
        }

        #region preencherLookupsCreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Actioncreate,g_ClassificacaoFinanceira_Actionupdate")]
        public void preencherLookupsCreateEdit()
        {
            var comboClassificacaoFinanceiraPai = new List<SelectListItem>();
            comboClassificacaoFinanceiraPai.Add(new SelectListItem { Value = "0", Text = "RAIZ" });
            try
            {
                IQueryable<g_classificacao_financeira> listaDbClassificacaoFinanceira = db.g_classificacao_financeira.Where(p => p.id_classificacao_financeira > 0).OrderBy(p => p.codigo);
                foreach (g_classificacao_financeira item1 in listaDbClassificacaoFinanceira)
                {
                    comboClassificacaoFinanceiraPai.Add(new SelectListItem { Value = item1.id_classificacao_financeira.ToString(), Text = item1.codigo.ToString() + " - " + item1.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboClassificacaoFinanceiraPai = comboClassificacaoFinanceiraPai;

        }
        #endregion

        #region getDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Actionread")]
        public ActionResult getDados(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            List<string[]> list = new List<string[]>();
            int start = param.iDisplayStart;
            int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;

            g_filtros recordFiltro = LibDB.getFilterByUser(param, controllerName, db);
            bool filterDb = recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0;
            if (filterDb) { filterOnOff = "1"; }

            List<Db.g_classificacao_financeira> pageList;
            int totalRecords;

            if (!filterDb)
            {
                var query = db.g_classificacao_financeira.AsNoTracking()
                    .Where(x => x.id_classificacao_financeira > 0);
                totalRecords = query.Count();
                IQueryable<Db.g_classificacao_financeira> ordered = query.OrderBy(x => x.codigo);
                if (param.iSortingCols > 0)
                {
                    bool asc = (param.sSortDir_0 ?? "asc").Equals("asc", StringComparison.OrdinalIgnoreCase);
                    switch (param.iSortCol_0)
                    {
                        case 1: ordered = asc ? query.OrderBy(c => c.id_classificacao_financeira) : query.OrderByDescending(c => c.id_classificacao_financeira); break;
                        case 2: ordered = asc ? query.OrderBy(c => c.id_classificacao_financeira_pai) : query.OrderByDescending(c => c.id_classificacao_financeira_pai); break;
                        case 3: ordered = asc ? query.OrderBy(c => c.consolidador) : query.OrderByDescending(c => c.consolidador); break;
                        case 4: ordered = asc ? query.OrderBy(c => c.codigo) : query.OrderByDescending(c => c.codigo); break;
                        case 5: ordered = asc ? query.OrderBy(c => c.nome) : query.OrderByDescending(c => c.nome); break;
                    }
                }
                pageList = ordered.Skip(start).Take(length).ToList();
            }
            else
            {
                string sentencaSql = recordFiltro.advanced == true
                    ? recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim()
                    : "select * from g_classificacao_financeira where id_classificacao_financeira > 1 and " + recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim();
                string sqlData = sentencaSql + " order by codigo";
                totalRecords = LibDataTableSqlPaging.SqlCount(db, sqlData);
                pageList = db.g_classificacao_financeira.SqlQuery(
                    LibDataTableSqlPaging.SqlPage(sqlData, start, length)).ToList();
            }

            var paiIds = pageList.Select(c => c.id_classificacao_financeira_pai).Where(id => id > 0).Distinct().ToList();
            var paiDict = paiIds.Count > 0
                ? db.g_classificacao_financeira.AsNoTracking()
                    .Where(p => paiIds.Contains(p.id_classificacao_financeira))
                    .ToDictionary(p => p.id_classificacao_financeira, p => p.nome.EmptyIfNull().ToString())
                : new Dictionary<int, string>();

            String _consolidador = String.Empty;
            String _credito = String.Empty;
            String _debito = String.Empty;

            foreach (g_classificacao_financeira c in pageList)
            {

                _consolidador = c.consolidador == true ? LibIcons.getIcon("fa-solid fa-check", "", "", "fa-lg") : "";
                _credito = c.credito == true ? LibIcons.getIcon("fa-solid fa-check", "", "", "fa-lg") : "";
                _debito = c.debito == true ? LibIcons.getIcon("fa-solid fa-check", "", "", "fa-lg") : "";
                string nomePai;
                paiDict.TryGetValue(c.id_classificacao_financeira_pai, out nomePai);
                if (nomePai == null) nomePai = String.Empty;

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_classificacao_financeira.ToString(),
                                    //c.id_classificacao_financeira_pai.ToString(),
                                    nomePai,
                                    _consolidador.ToString(),
                                    c.codigo.ToString(),
                                    c.nome.ToString(),
                                    _credito.ToString(),
                                    _debito.ToString()
                                });
            }

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = totalRecords,
                iTotalDisplayRecords = totalRecords,
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

        #region GetTreeViewClassificacaoFinanceira
        /// <summary>Registo sistema oculto na listagem (<c>getDados</c> usa <c>id &gt; 1</c>); filhos reais usam <c>pai = 1</c>.</summary>
        private const int IdRegistroRaizSistemaClassificacaoFinanceira = 1;

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Actionread")]
        public JsonResult GetTreeViewClassificacaoFinanceira()
        {
            if (db == null)
            {
                return Json(new JsTree3Node
                {
                    id = "-1",
                    text = "Árvore - Classificação Financeira (sessão inválida)",
                    state = new State(true, false, false),
                    children = new List<JsTree3Node>()
                }, JsonRequestBehavior.AllowGet);
            }

            var root = new JsTree3Node() // Create our root node and ensure it is opened
            {
                id = "-1",
                text = "Árvore - Classificação Financeira",
                icon = "/LibUI_AdminLTE-4.0.0/plugins/jstree-3.3.4/images/icons8-genealogy-24.png",
                state = new State(true, false, false)
            };
            var children = new List<JsTree3Node>();
            String tipoCentroCusto = String.Empty;
            String nameIconAgrupador = "fa-regular fa-folder-open";
            String nameIconClassificacaoFinanceira = "fa-regular fa-file";
            var listaClassificacaoFinanceira = (from _c1 in db.g_classificacao_financeira
                                                where _c1.id_classificacao_financeira > 0
                                                select new { g_classificacao_financeira = _c1 }).ToList();

            // Nível 1 — raiz visível: pai=0 (exceto registo sistema id=1) ou filhos diretos de id=1 (legado BD)
            var listaClassificacaoFinanceiraN1 = listaClassificacaoFinanceira
                .Where(N1 =>
                {
                    var cf = N1.g_classificacao_financeira;
                    var pai = cf.id_classificacao_financeira_pai;
                    var id = cf.id_classificacao_financeira;
                    return (pai == 0 && id != IdRegistroRaizSistemaClassificacaoFinanceira) || pai == IdRegistroRaizSistemaClassificacaoFinanceira;
                })
                .OrderBy(N1 => N1.g_classificacao_financeira.codigo).ToList();
            foreach (var ClassificacaoFinanceiraN1 in listaClassificacaoFinanceiraN1)
            {
                var nodeN1 = JsTree3Node.NewNode(ClassificacaoFinanceiraN1.g_classificacao_financeira.codigo.ToString() + " - " + ClassificacaoFinanceiraN1.g_classificacao_financeira.nome.ToString());
                if (ClassificacaoFinanceiraN1.g_classificacao_financeira.consolidador == true) { tipoCentroCusto = "   (*)"; }
                else if ((ClassificacaoFinanceiraN1.g_classificacao_financeira.debito == true) && (ClassificacaoFinanceiraN1.g_classificacao_financeira.credito == true)) { tipoCentroCusto = "   (+/-)"; }
                else if ((ClassificacaoFinanceiraN1.g_classificacao_financeira.debito == true) && (ClassificacaoFinanceiraN1.g_classificacao_financeira.credito == false)) { tipoCentroCusto = "   (-)"; }
                else if ((ClassificacaoFinanceiraN1.g_classificacao_financeira.debito == false) && (ClassificacaoFinanceiraN1.g_classificacao_financeira.credito == true)) { tipoCentroCusto = "   (+)"; }
                else { tipoCentroCusto = String.Empty; };
                nodeN1.id = ClassificacaoFinanceiraN1.g_classificacao_financeira.id_classificacao_financeira.ToString();
                nodeN1.text = (ClassificacaoFinanceiraN1.g_classificacao_financeira.codigo.ToString() + " - " + ClassificacaoFinanceiraN1.g_classificacao_financeira.nome.ToString() + tipoCentroCusto).ToString().Trim();
                nodeN1.icon = nameIconAgrupador;
                nodeN1.state = new State(true, false, false);

                // Nível 2
                var listaClassificacaoFinanceiraN2 = listaClassificacaoFinanceira.Where(N2 => (N2.g_classificacao_financeira.id_classificacao_financeira_pai == ClassificacaoFinanceiraN1.g_classificacao_financeira.id_classificacao_financeira) && (N2.g_classificacao_financeira.id_classificacao_financeira != ClassificacaoFinanceiraN1.g_classificacao_financeira.id_classificacao_financeira)).OrderBy(N2 => N2.g_classificacao_financeira.codigo).ToList();
                foreach (var ClassificacaoFinanceiraN2 in listaClassificacaoFinanceiraN2)
                {
                    var nodeN2 = JsTree3Node.NewNode(ClassificacaoFinanceiraN2.g_classificacao_financeira.codigo.ToString() + " - " + ClassificacaoFinanceiraN2.g_classificacao_financeira.nome.ToString());
                    if (ClassificacaoFinanceiraN2.g_classificacao_financeira.consolidador == true) { tipoCentroCusto = "   (*)"; }
                    else if ((ClassificacaoFinanceiraN2.g_classificacao_financeira.debito == true) && (ClassificacaoFinanceiraN2.g_classificacao_financeira.credito == true)) { tipoCentroCusto = "   (+/-)"; }
                    else if ((ClassificacaoFinanceiraN2.g_classificacao_financeira.debito == true) && (ClassificacaoFinanceiraN2.g_classificacao_financeira.credito == false)) { tipoCentroCusto = "   (-)"; }
                    else if ((ClassificacaoFinanceiraN2.g_classificacao_financeira.debito == false) && (ClassificacaoFinanceiraN2.g_classificacao_financeira.credito == true)) { tipoCentroCusto = "   (+)"; }
                    else { tipoCentroCusto = String.Empty; };
                    nodeN2.id = ClassificacaoFinanceiraN2.g_classificacao_financeira.id_classificacao_financeira.ToString();
                    nodeN2.text = (ClassificacaoFinanceiraN2.g_classificacao_financeira.codigo.ToString() + " - " + ClassificacaoFinanceiraN2.g_classificacao_financeira.nome.ToString() + tipoCentroCusto).ToString().Trim();
                    nodeN2.icon = nameIconClassificacaoFinanceira;
                    nodeN2.state = new State(true, false, false);

                    // Nível 3
                    var listaClassificacaoFinanceiraN3 = listaClassificacaoFinanceira.Where(N3 => (N3.g_classificacao_financeira.id_classificacao_financeira_pai == ClassificacaoFinanceiraN2.g_classificacao_financeira.id_classificacao_financeira) && (N3.g_classificacao_financeira.id_classificacao_financeira != ClassificacaoFinanceiraN2.g_classificacao_financeira.id_classificacao_financeira)).OrderBy(N3 => N3.g_classificacao_financeira.codigo).ToList();
                    foreach (var ClassificacaoFinanceiraN3 in listaClassificacaoFinanceiraN3)
                    {
                        var nodeN3 = JsTree3Node.NewNode(ClassificacaoFinanceiraN3.g_classificacao_financeira.codigo.ToString() + " - " + ClassificacaoFinanceiraN3.g_classificacao_financeira.nome.ToString());
                        if (ClassificacaoFinanceiraN3.g_classificacao_financeira.consolidador == true) { tipoCentroCusto = "   (*)"; }
                        else if ((ClassificacaoFinanceiraN3.g_classificacao_financeira.debito == true) && (ClassificacaoFinanceiraN3.g_classificacao_financeira.credito == true)) { tipoCentroCusto = "   (+/-)"; }
                        else if ((ClassificacaoFinanceiraN3.g_classificacao_financeira.debito == true) && (ClassificacaoFinanceiraN3.g_classificacao_financeira.credito == false)) { tipoCentroCusto = "   (-)"; }
                        else if ((ClassificacaoFinanceiraN3.g_classificacao_financeira.debito == false) && (ClassificacaoFinanceiraN3.g_classificacao_financeira.credito == true)) { tipoCentroCusto = "   (+)"; }
                        else { tipoCentroCusto = String.Empty; };
                        nodeN3.id = ClassificacaoFinanceiraN3.g_classificacao_financeira.id_classificacao_financeira.ToString();
                        nodeN3.text = (ClassificacaoFinanceiraN3.g_classificacao_financeira.codigo.ToString() + " - " + ClassificacaoFinanceiraN3.g_classificacao_financeira.nome.ToString() + tipoCentroCusto).ToString().Trim();
                        nodeN3.icon = nameIconClassificacaoFinanceira;
                        nodeN3.state = new State(true, false, false);

                        // Nível 4
                        var listaClassificacaoFinanceiraN4 = listaClassificacaoFinanceira.Where(N4 => (N4.g_classificacao_financeira.id_classificacao_financeira_pai == ClassificacaoFinanceiraN3.g_classificacao_financeira.id_classificacao_financeira) && (N4.g_classificacao_financeira.id_classificacao_financeira != ClassificacaoFinanceiraN3.g_classificacao_financeira.id_classificacao_financeira)).OrderBy(N3 => N3.g_classificacao_financeira.codigo).ToList();
                        foreach (var ClassificacaoFinanceiraN4 in listaClassificacaoFinanceiraN4)
                        {
                            var nodeN4 = JsTree3Node.NewNode(ClassificacaoFinanceiraN4.g_classificacao_financeira.codigo.ToString() + " - " + ClassificacaoFinanceiraN4.g_classificacao_financeira.nome.ToString());
                            if (ClassificacaoFinanceiraN4.g_classificacao_financeira.consolidador == true) { tipoCentroCusto = "   (*)"; }
                            else if ((ClassificacaoFinanceiraN4.g_classificacao_financeira.debito == true) && (ClassificacaoFinanceiraN4.g_classificacao_financeira.credito == true)) { tipoCentroCusto = "   (+/-)"; }
                            else if ((ClassificacaoFinanceiraN4.g_classificacao_financeira.debito == true) && (ClassificacaoFinanceiraN4.g_classificacao_financeira.credito == false)) { tipoCentroCusto = "   (-)"; }
                            else if ((ClassificacaoFinanceiraN4.g_classificacao_financeira.debito == false) && (ClassificacaoFinanceiraN4.g_classificacao_financeira.credito == true)) { tipoCentroCusto = "   (+)"; }
                            else { tipoCentroCusto = String.Empty; };
                            nodeN4.id = ClassificacaoFinanceiraN4.g_classificacao_financeira.id_classificacao_financeira.ToString();
                            nodeN4.text = (ClassificacaoFinanceiraN4.g_classificacao_financeira.codigo.ToString() + " - " + ClassificacaoFinanceiraN4.g_classificacao_financeira.nome.ToString() + tipoCentroCusto).ToString().Trim();
                            nodeN4.icon = nameIconClassificacaoFinanceira;
                            nodeN4.state = new State(true, false, false);
                            nodeN3.children.Add(nodeN4);
                        }
                        if (listaClassificacaoFinanceiraN4.Count() > 0) { nodeN3.icon = nameIconAgrupador; }

                        nodeN2.children.Add(nodeN3);
                    }
                    if (listaClassificacaoFinanceiraN3.Count() > 0) { nodeN2.icon = nameIconAgrupador; }

                    nodeN1.children.Add(nodeN2);
                }

                children.Add(nodeN1);
            }

            root.children = children;

            // Return the object as JSON
            return Json(root, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Actioncreate")]
        public ActionResult Create()
        {
            g_classificacao_financeira newRecord = new g_classificacao_financeira();
            newRecord.ativo = true;
            newRecord.id_coligada = 0;  // Definição de que Clientes Categorias Sub é Global
            newRecord.id_filial = 0;    // Definição de que Clientes Categorias Sub é Global            
            preencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Nova Classificação Financeira</b";
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Actioncreate")]
        public ActionResult Create(g_classificacao_financeira record_g_classificacao_financeira)
        {
            record_g_classificacao_financeira.nome = record_g_classificacao_financeira.nome.ToUpper();
            if (ModelState.IsValid)
            {
                if (record_g_classificacao_financeira.codigo.EmptyIfNull().ToString().Equals(String.Empty))
                {
                    { ModelState.AddModelError("Model", "Campo [Código] é de preenchimento obrigatório"); }
                }
                if ((record_g_classificacao_financeira.credito == false) && (record_g_classificacao_financeira.debito == false))
                {
                    ModelState.AddModelError("Model", "Tipo [Crédito ou Débito] é de preenchimento obrigatório");
                }
                if ((record_g_classificacao_financeira.credito == true) && (record_g_classificacao_financeira.debito == true))
                {
                    ModelState.AddModelError("Model", "Tipo deve ser [Crédito ou Débito] NÃO é permitido Ambos");
                }

                IQueryable<g_classificacao_financeira> listaClassificacaoFinanceira = db.g_classificacao_financeira.Where(p => p.codigo == record_g_classificacao_financeira.codigo || p.nome == record_g_classificacao_financeira.nome);
                foreach (g_classificacao_financeira validacao in listaClassificacaoFinanceira)
                {

                    // Validação Código
                    if ((validacao.codigo != null) && (validacao.codigo != String.Empty))
                    {
                        if (validacao.codigo.ToString().ToUpper().Equals(record_g_classificacao_financeira.codigo.EmptyIfNull().ToString().ToUpper()))
                        { ModelState.AddModelError("Model", "Campo [Códido] duplicado na base de dados [Id.: " + validacao.id_classificacao_financeira + " / Centro de Custo: " + validacao.nome.ToString() + "]"); }
                    }

                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_classificacao_financeira.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_classificacao_financeira.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_classificacao_financeira.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.g_classificacao_financeira.Add(record_g_classificacao_financeira);
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Nova Classificação Financeira</b";
            return View("CreateEdit", record_g_classificacao_financeira);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_classificacao_financeira record_g_classificacao_financeira = db.g_classificacao_financeira.Find(id);
            if (record_g_classificacao_financeira == null)
            {
                return RedirectToAction("Index", "Error", new { area = "" });
            }
            preencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Classificação Financeira</b>" + LibStringFormat.GetTabHtml(1) + record_g_classificacao_financeira.id_classificacao_financeira.EmptyIfNull().ToString() + " - " + record_g_classificacao_financeira.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_classificacao_financeira);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ClassificacaoFinanceira_*,g_ClassificacaoFinanceira_Actionupdate")]
        public ActionResult Edit(g_classificacao_financeira record_g_classificacao_financeira)
        {
            record_g_classificacao_financeira.nome = record_g_classificacao_financeira.nome.ToUpper();

            if (ModelState.IsValid)
            {

                IQueryable<g_classificacao_financeira> listaClassificacaoFinanceira = db.g_classificacao_financeira.Where(p => (p.codigo == record_g_classificacao_financeira.codigo || p.nome == record_g_classificacao_financeira.nome) && (p.id_classificacao_financeira != record_g_classificacao_financeira.id_classificacao_financeira));
                foreach (g_classificacao_financeira validacao in listaClassificacaoFinanceira)
                {
                    // Validação Código
                    if ((validacao.codigo != null) && (validacao.codigo != String.Empty))
                    {
                        if (validacao.codigo.ToString().ToUpper().Equals(record_g_classificacao_financeira.codigo.EmptyIfNull().ToString().ToUpper()))
                        { ModelState.AddModelError("Model", "Campo [Códido] duplicado na base de dados [Id.: " + validacao.id_classificacao_financeira + " / Centro de Custo: " + validacao.nome.ToString() + "]"); }
                    }

                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_classificacao_financeira.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }

                if ((record_g_classificacao_financeira.credito == false) && (record_g_classificacao_financeira.debito == false))
                {
                    ModelState.AddModelError("Model", "Tipo [Crédito ou Débito] é de preenchimento obrigatório");
                }
                if ((record_g_classificacao_financeira.credito == true) && (record_g_classificacao_financeira.debito == true))
                {
                    ModelState.AddModelError("Model", "Tipo deve ser [Crédito ou Débito] NÃO é permitido Ambos");
                }
            }

            if (ModelState.IsValid)
            {
                record_g_classificacao_financeira.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_classificacao_financeira.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_classificacao_financeira).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Classificação Financeira</b>" + LibStringFormat.GetTabHtml(1) + record_g_classificacao_financeira.id_classificacao_financeira.EmptyIfNull().ToString() + " - " + record_g_classificacao_financeira.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_classificacao_financeira);
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