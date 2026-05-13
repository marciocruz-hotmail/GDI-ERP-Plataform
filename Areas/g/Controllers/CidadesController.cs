using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Controllers
{
    // Logons podem cadastrar cidades
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Cidades_*,g_Cidades_Default,gdc_Pefin_*,gdc_Pefin_Default")]
    public class CidadesController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Cidades";

        public CidadesController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Cidades_*,g_Cidades_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Cidades";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Cidades_*,g_Cidades_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
            var allRecords = new List<Db.g_cidades>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                // Não há filtro
                //allRecords = db.g_cidades.Where(c => c.ativo == true).ToList();
                allRecords = db.g_cidades.ToList();
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
                    SentencaSQL = "select * from g_cidades where id_cidade > 1 and " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim();
                }
                
                allRecords = db.g_cidades.SqlQuery(SentencaSQL).ToList();
            }
            else if (filterAdvanced)
            {
                // Filtro Avançado
                String[] listaCampos = null;
                SentencaSQL = string.Empty;
                try { listaCampos = param.yesFilterAdvancedText.EmptyIfNull().ToString().Split(';'); } catch (Exception) { listaCampos = new string[1] { "" }; };

                if (listaCampos.Count() == 2)
                {
                    SentencaSQL = " select c.* from g_cidades c where id_cidade > 1 and c.id_cidade is not null";
                    if ((!listaCampos[0].ToString().Trim().Equals(String.Empty)) && (!listaCampos[0].ToString().Trim().Equals("0")))
                    {
                        SentencaSQL += " and c.id_cidade = " + listaCampos[0].ToString().Trim();
                    }
                    if (!listaCampos[1].ToString().Trim().Equals(String.Empty))
                    {
                        SentencaSQL += " and c.nome like '%" + listaCampos[1].ToString().Trim() + "%'";
                    }
                    LibDB.setFilterByUser(SentencaSQL, controllerName, true, db);
                    allRecords = db.g_cidades.SqlQuery(SentencaSQL.ToString()).ToList();
                }
                else
                {
                    //allRecords = db.g_cidades.Where(c => c.ativo == true).ToList();
                    allRecords = db.g_cidades.ToList();
                }
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_cidades, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_cidade) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.nome :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_cidade); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.nome); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_cidade); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.nome); }
                }
            }

            foreach (var c in displayedRecords)
            {
                String _ativo = c.ativo == true ? LibIcons.getIcon("fa-solid fa-circle", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle", "Inativo", "red", "");

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_cidade.ToString(),
                                    _ativo,
                                    c.nome.ToString()
                                });
            }

            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; };

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

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Cidades_*,g_Cidades_Actioncreate,gdc_Pefin_Default")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cidade</b";
            g_cidades newRecord = new g_cidades();
            newRecord.ativo = true;
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Cidades_*,g_Cidades_Actioncreate,gdc_Pefin_Default")]
        public ActionResult Create(g_cidades record_g_cidades)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cidade</b";
            record_g_cidades.id_coligada = 0;  // Definição de que Cidade é Global
            record_g_cidades.id_filial = 0;    // Definição de que Cidade é Global
            if (record_g_cidades.nome.EmptyIfNull().ToString() != String.Empty) { record_g_cidades.nome = LibStringFormat.FormatarTextoSimples(record_g_cidades.nome); }

            if (ModelState.IsValid)
            {
                //IQueryable<g_cidades> listaCidades = db.g_cidades.Where(p => (p.ativo == true) && (p.nome == record_g_cidades.nome));
                IQueryable<g_cidades> listaCidades = db.g_cidades.Where(p => (p.nome == record_g_cidades.nome));
                foreach (g_cidades validacao in listaCidades)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_cidades.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_cidades.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_cidades.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.g_cidades.Add(record_g_cidades);
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

            return View("CreateEdit", record_g_cidades);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Cidades_*,g_Cidades_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_cidades record_g_cidade = db.g_cidades.Find(id);
            if (record_g_cidade == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Cidade</b>" + LibStringFormat.GetTabHtml(1) + record_g_cidade.id_cidade.EmptyIfNull().ToString() + " - " + record_g_cidade.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_cidade);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Cidades_*,g_Cidades_Actionupdate")]
        public ActionResult Edit(g_cidades record_g_cidades)
        {
            if (record_g_cidades.nome.EmptyIfNull().ToString() != String.Empty) { record_g_cidades.nome = LibStringFormat.FormatarTextoSimples(record_g_cidades.nome); }

            if (ModelState.IsValid)
            {
                IQueryable<g_cidades> listaCidades = db.g_cidades.Where(p => (p.nome == record_g_cidades.nome) && (p.id_cidade != record_g_cidades.id_cidade));
                foreach (g_cidades validacao in listaCidades)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_cidades.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_cidades.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_cidades.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_cidades).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Cidade</b>" + LibStringFormat.GetTabHtml(1) + record_g_cidades.id_cidade.EmptyIfNull().ToString() + " - " + record_g_cidades.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_cidades);
        }
        #endregion

        #region ModalCadastrarNovaCidade
        public ActionResult ModalCadastrarNovaCidade(int? id)
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cidades</b>   Cadastrar Nova";
            return View();
        }

        [HttpPost]
        public ActionResult AjaxCadastrarNovaCidade(g_cidades record_g_cidades)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            record_g_cidades.nome = LibStringFormat.RemoverAcentos(record_g_cidades.nome);
            record_g_cidades.nome = record_g_cidades.nome.ToUpper().Trim();

            if (ModelState.IsValid)
            {
                IQueryable<g_cidades> listaCidades = db.g_cidades.Where(p => p.nome == record_g_cidades.nome);
                foreach (g_cidades validacao in listaCidades)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_cidades.nome.ToString().ToUpper()))
                    {
                        ModelState.AddModelError("Model", "Cidade já cadastrada na base de dados [" + validacao.nome.ToString() + "]");
                        msgRetorno = "Cidade <b>" + validacao.nome.ToString() + "</b> já está cadastrada na base de dados!";
                    }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_cidades.ativo = true;
                record_g_cidades.id_coligada = 0;  // Definição de que Cidade é Global
                record_g_cidades.id_filial = 0;    // Definição de que Cidade é Global
                record_g_cidades.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_cidades.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_cidades.Add(record_g_cidades);
                try
                {
                    db.SaveChanges();
                    sucesso = true;
                    msgRetorno = "Nova Cidade <b>Cadastrada</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
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
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalFiltroAvancadoView
        public ActionResult ModalFiltroAvancadoView(String id)
        {
            ViewBag.Title = "Cidades - Filtro Avançado";
            return View();
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