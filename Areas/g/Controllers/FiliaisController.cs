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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Default")]
    public partial class FiliaisController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Filiais";

        public FiliaisController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Filiais";
            var model = new CstFiliaisIndex
            {
                FiliaisIndex_id_filial = String.Empty,
                FiliaisIndex_nome = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, nomeRestore;
            if (TryParseFiltroFiliaisSemicolon(filtroPersistido.sql_filtro, out idRestore, out nomeRestore))
            {
                model.FiliaisIndex_id_filial = idRestore;
                model.FiliaisIndex_nome = nomeRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(nomeRestore);
            }
            return View(model);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actionread")]
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

                var baseQuery = db.g_filiais.AsNoTracking().Where(f => f.id_filial > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string nomeStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroFiliaisSemicolon(recordFiltro.sql_filtro, out idStr, out nomeStr);
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

                IQueryable<Db.g_filiais> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroFiliaisNaQuery(query, idStr, nomeStr);
                    LibDB.setFilterByUser(MontarFiltroFiliaisPersistido(idStr, LibStringFormat.NormalizarTermoBuscaTexto(nomeStr)), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoFiliaisNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(f => new { f.id_filial, f.nome, f.id_coligada })
                    .ToList();

                var coligadaIds = page.Select(f => f.id_coligada).Distinct().ToList();
                var coligadasPorId = db.g_coligadas.AsNoTracking()
                    .Where(c => coligadaIds.Contains(c.id_coligada))
                    .Select(c => new { c.id_coligada, c.razao_social })
                    .ToList()
                    .ToDictionary(c => c.id_coligada, c => c.razao_social ?? String.Empty);

                var list = page.Select(f =>
                {
                    string razao = coligadasPorId.ContainsKey(f.id_coligada) ? coligadasPorId[f.id_coligada] : String.Empty;
                    return new[]
                    {
                        "",
                        f.id_filial.ToString(),
                        f.nome ?? "",
                        razao
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

        private static bool TryParseFiltroFiliaisSemicolon(string raw, out string id, out string nome)
        {
            id = nome = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            nome = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(nome);
        }

        private static string MontarFiltroFiliaisPersistido(string id, string nome)
        {
            return (id ?? String.Empty) + ";" + (nome ?? String.Empty);
        }

        private static bool TryParseIdFilialFiltro(string idStr, out int idFilial)
        {
            idFilial = 0;
            if (String.IsNullOrWhiteSpace(idStr) || idStr == "0") return false;
            return int.TryParse(idStr.Trim(), out idFilial) && idFilial != 0;
        }

        private static IQueryable<Db.g_filiais> AplicarFiltroFiliaisNaQuery(IQueryable<Db.g_filiais> query, string idStr, string nomeStr)
        {
            if (TryParseIdFilialFiltro(idStr, out int idFilial))
            {
                query = query.Where(f => f.id_filial == idFilial);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(nomeStr, out string padraoNome))
            {
                query = query.Where(f => f.nome != null && DbFunctions.Like(f.nome, padraoNome));
            }
            return query;
        }

        private static IQueryable<Db.g_filiais> AplicarOrdenacaoFiliaisNaQuery(IQueryable<Db.g_filiais> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 2)
                {
                    return asc ? query.OrderBy(f => f.nome) : query.OrderByDescending(f => f.nome);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(f => f.id_filial) : query.OrderByDescending(f => f.id_filial);
                }
            }
            return query.OrderBy(f => f.nome);
        }

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Filial</b>";
            g_filiais newRecord = new g_filiais();
            PreencherLookupsCreateEdit();
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actioncreate")]
        public ActionResult Create(g_filiais record_g_filiais)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Filial</b>";
            record_g_filiais.nome = LibStringFormat.RemoverAcentos(record_g_filiais.nome);
            record_g_filiais.nome = record_g_filiais.nome.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_filiais> listaGFiliais = db.g_filiais.Where(p => p.nome == record_g_filiais.nome);
                foreach (g_filiais validacao in listaGFiliais)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_filiais.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_filiais.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_filiais.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_filiais.Add(record_g_filiais);
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
            PreencherLookupsCreateEdit();
            return View("CreateEdit", record_g_filiais);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_filiais record_g_filiais = db.g_filiais.Find(id);
            if (record_g_filiais == null)
            {
                return RedirectToAction("Index");
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = MontarTituloCreateEditFilial(record_g_filiais);
            return View("CreateEdit", record_g_filiais);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Filiais_*,g_Filiais_Actionupdate")]
        public ActionResult Edit(g_filiais record_g_filiais)
        {
            g_filiais existente = db.g_filiais.Find(record_g_filiais.id_filial);
            if (existente == null)
            {
                return RedirectToAction("Index");
            }

            string nomeNormalizado = LibStringFormat.RemoverAcentos(record_g_filiais.nome.EmptyIfNull().ToString()).Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_filiais> listaGFiliais = db.g_filiais.Where(p => (p.nome == nomeNormalizado) && (p.id_filial != existente.id_filial));
                foreach (g_filiais validacao in listaGFiliais)
                {
                    if (validacao.nome.ToString().ToUpper().Equals(nomeNormalizado))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                existente.nome = nomeNormalizado;
                existente.id_coligada = record_g_filiais.id_coligada;
                existente.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                existente.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
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

            record_g_filiais.nome = nomeNormalizado;
            PreencherLookupsCreateEdit();
            ViewBag.Title = MontarTituloCreateEditFilial(record_g_filiais);
            return View("CreateEdit", record_g_filiais);
        }

        private static string MontarTituloCreateEditFilial(g_filiais record)
        {
            return LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp"
                + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Filial</b>"
                + LibStringFormat.GetTabHtml(1) + record.id_filial.ToString() + " - " + record.nome.EmptyIfNull().ToString();
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

    }
}