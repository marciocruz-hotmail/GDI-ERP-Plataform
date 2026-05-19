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
            var model = new CstCidadesIndex
            {
                CidadesIndex_id_cidade = String.Empty,
                CidadesIndex_nome = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, nomeRestore;
            if (TryParseFiltroCidadesSemicolon(filtroPersistido.sql_filtro, out idRestore, out nomeRestore))
            {
                model.CidadesIndex_id_cidade = idRestore;
                model.CidadesIndex_nome = nomeRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(nomeRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Cidades_*,g_Cidades_Actionread")]
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
                    recordFiltro = LibDB.getFilterByUser(param, controllerName, false, db);
                }
                else
                {
                    recordFiltro = ObterFiltroPersistidoUsuario();
                }

                // id 1 pode ser cidade real (ex.: Belo Horizonte); excluir apenas id inválido 0
                var baseQuery = db.g_cidades.AsNoTracking().Where(c => c.id_cidade > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string nomeStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroCidadesSemicolon(recordFiltro.sql_filtro, out idStr, out nomeStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);
                }

                if (!listarTodosExplicito && !hasInline)
                {
                    return Json(new
                    {
                        errorMessage = "",
                        stackTrace = "",
                        yesFilterOnOff = "0",
                        sEcho = param.sEcho,
                        iTotalRecords = totalRecords,
                        iTotalDisplayRecords = 0,
                        aaData = new List<string[]>()
                    }, JsonRequestBehavior.AllowGet);
                }

                IQueryable<Db.g_cidades> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroCidadesNaQuery(query, idStr, nomeStr);
                    LibDB.setFilterByUser(MontarFiltroCidadesPersistido(idStr, LibStringFormat.NormalizarTermoBuscaTexto(nomeStr)), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoCidadesNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(c => new { c.id_cidade, c.ativo, c.nome })
                    .ToList();

                var list = page.Select(c =>
                {
                    string _ativo = c.ativo == true
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                    return new[]
                    {
                        "",
                        c.id_cidade.ToString(),
                        _ativo,
                        c.nome ?? ""
                    };
                }).ToList();

                filterOnOff = filterApplied ? "1" : "0";

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
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

        private static bool TryParseFiltroCidadesSemicolon(string raw, out string id, out string nome)
        {
            id = nome = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            nome = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(nome);
        }

        private static string MontarFiltroCidadesPersistido(string id, string nome)
        {
            return (id ?? String.Empty) + ";" + (nome ?? String.Empty);
        }

        private static IQueryable<Db.g_cidades> AplicarFiltroCidadesNaQuery(IQueryable<Db.g_cidades> query, string idStr, string nomeStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idCidade))
            {
                query = query.Where(c => c.id_cidade == idCidade);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(nomeStr, out string padraoLike))
            {
                query = query.Where(c => c.nome != null && DbFunctions.Like(c.nome, padraoLike));
            }
            return query;
        }

        private static IQueryable<Db.g_cidades> AplicarOrdenacaoCidadesNaQuery(IQueryable<Db.g_cidades> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 3)
                {
                    return asc ? query.OrderBy(c => c.nome) : query.OrderByDescending(c => c.nome);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(c => c.id_cidade) : query.OrderByDescending(c => c.id_cidade);
                }
            }
            return query.OrderBy(c => c.id_cidade);
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