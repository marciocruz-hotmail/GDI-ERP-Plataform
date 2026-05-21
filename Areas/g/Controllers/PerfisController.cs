// Migrado em 2020_07_15

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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Perfis_*,g_Perfis_Default")]
    public class PerfisController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Perfis";

        public PerfisController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Perfis_*,g_Perfis_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Perfis";
            var model = new CstPerfisIndex
            {
                PerfisIndex_id_perfil = String.Empty,
                PerfisIndex_nome = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, nomeRestore;
            if (TryParseFiltroPerfisSemicolon(filtroPersistido.sql_filtro, out idRestore, out nomeRestore))
            {
                model.PerfisIndex_id_perfil = idRestore;
                model.PerfisIndex_nome = nomeRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(nomeRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Perfis_*,g_Perfis_Actionread")]
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

                // Universo da grade (lista geral): oculta perfis de sistema id <= 1; busca por Id. explícito pode retornar qualquer id válido.
                int totalRecords = db.g_perfis.AsNoTracking().Where(p => p.id_perfil > 1).Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string nomeStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrWhiteSpace(idStr) || !String.IsNullOrWhiteSpace(nomeStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroPerfisSemicolon(recordFiltro.sql_filtro, out idStr, out nomeStr);
                    hasInline = !String.IsNullOrWhiteSpace(idStr) || !String.IsNullOrWhiteSpace(nomeStr);
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

                bool temFiltroIdExplicito = TryParseIdPerfilFiltro(idStr, out _);
                IQueryable<Db.g_perfis> query = db.g_perfis.AsNoTracking();
                if (!temFiltroIdExplicito)
                {
                    query = query.Where(p => p.id_perfil > 1);
                }
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroPerfisNaQuery(query, idStr, nomeStr);
                    LibDB.setFilterByUser(
                        MontarFiltroPerfisPersistido(idStr, LibStringFormat.NormalizarTermoBuscaTexto(nomeStr)),
                        controllerName,
                        true,
                        db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoPerfisNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(p => new { p.id_perfil, p.nome })
                    .ToList();

                var list = page.Select(p => new[]
                {
                    "",
                    p.id_perfil.ToString(),
                    p.nome ?? ""
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

        private static bool TryParseFiltroPerfisSemicolon(string raw, out string id, out string nome)
        {
            id = nome = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            nome = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(nome);
        }

        private static string MontarFiltroPerfisPersistido(string id, string nome)
        {
            return (id ?? String.Empty) + ";" + (nome ?? String.Empty);
        }

        /// <summary>Id. numérico informado no filtro inline (0 e não numérico = sem critério de id).</summary>
        private static bool TryParseIdPerfilFiltro(string idStr, out int idPerfil)
        {
            idPerfil = 0;
            if (String.IsNullOrWhiteSpace(idStr) || idStr == "0") return false;
            return int.TryParse(idStr.Trim(), out idPerfil) && idPerfil != 0;
        }

        /// <summary>Numérico = igualdade; nome = LIKE %termo% (texto normalizado).</summary>
        private static IQueryable<Db.g_perfis> AplicarFiltroPerfisNaQuery(IQueryable<Db.g_perfis> query, string idStr, string nomeStr)
        {
            if (TryParseIdPerfilFiltro(idStr, out int idPerfil))
            {
                query = query.Where(p => p.id_perfil == idPerfil);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(nomeStr, out string padraoNome))
            {
                query = query.Where(p => p.nome != null && DbFunctions.Like(p.nome, padraoNome));
            }
            return query;
        }

        private static IQueryable<Db.g_perfis> AplicarOrdenacaoPerfisNaQuery(IQueryable<Db.g_perfis> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 2)
                {
                    return asc ? query.OrderBy(p => p.nome) : query.OrderByDescending(p => p.nome);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(p => p.id_perfil) : query.OrderByDescending(p => p.id_perfil);
                }
            }
            return query.OrderBy(p => p.id_perfil);
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Perfis_*,g_Perfis_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Perfil</b";
            CstViewPerfisAcessosModel record_YesPerfisAcessosModel = new CstViewPerfisAcessosModel();
            return View("CreateEdit", record_YesPerfisAcessosModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Perfis_*,g_Perfis_Actioncreate")]
        public ActionResult Create(CstViewPerfisAcessosModel record_YesPerfisAcessosModel)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Perfil</b";
            record_YesPerfisAcessosModel.g_perfis.id_coligada = 1;
            record_YesPerfisAcessosModel.g_perfis.id_filial = 1;
            record_YesPerfisAcessosModel.g_perfis.nome = record_YesPerfisAcessosModel.g_perfis.nome.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_perfis> listaPerfis = db.g_perfis.Where(p => p.nome == record_YesPerfisAcessosModel.g_perfis.nome);
                foreach (g_perfis validacao in listaPerfis)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_YesPerfisAcessosModel.g_perfis.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_YesPerfisAcessosModel.g_perfis.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_YesPerfisAcessosModel.g_perfis.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_perfis.Add(record_YesPerfisAcessosModel.g_perfis);
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

            return View("CreateEdit", record_YesPerfisAcessosModel);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Perfis_*,g_Perfis_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0) || (id == 1))
            {
                return RedirectToAction("Index");
            }

            g_perfis record_g_perfis = db.g_perfis.Find(id);

            if (record_g_perfis == null)
            {
                return RedirectToAction("Index");
            }

            // Novo Modelo
            CstViewPerfisAcessosModel record_YesPerfisAcessosModel = new CstViewPerfisAcessosModel();
            record_YesPerfisAcessosModel.g_perfis = record_g_perfis;

             // Perfis Acessos
             String sqlTempAcessos =
            "select a_sistemas_controllers.id_sistema_controller, a_sistemas_grupos.nome as 'grupo',  a_sistemas_controllers.title_perfil, a_sistemas_controllers.title_menu, a_sistemas_controllers.id_sistema, " +
            "cast(a_sistemas_controllers.is_crud as bit) as 'is_crud', cast(a_sistemas_controllers.is_process as bit) as 'is_process', cast(a_sistemas_controllers.is_report as bit) as 'is_report', cast(a_sistemas_controllers.is_panel as bit) as 'is_panel', " +
            "a_sistemas_controllers.id_sistema_modulo, a_sistemas_controllers.id_sistema_controller_pai, g_perfis_acessos.id_perfil_acesso,  " +
            "cast(case when g_perfis_acessos.id_perfil_acesso is null then 0 else 1 end as bit) as 'ativo', " +
            "cast(case when g_perfis_acessos.action_run = 1 then 1 else 0 end as bit) as 'action_run', " +
            "cast(case when g_perfis_acessos.action_create = 1 then 1 else 0 end as bit) as 'action_create', " +
            "cast(case when g_perfis_acessos.action_read = 1 then 1 else 0 end as bit) as 'action_read', " +
            "cast(case when g_perfis_acessos.action_update = 1 then 1 else 0 end as bit) as 'action_update', " +
            "cast(case when g_perfis_acessos.action_delete = 1 then 1 else 0 end as bit) as 'action_delete', " +
            "cast(case when g_perfis_acessos.action_manager = 1 then 1 else 0 end as bit) as 'action_manager' " +
            "from a_sistemas_controllers a_sistemas_controllers " +
            "join a_sistemas a_sistemas on (a_sistemas_controllers.id_sistema = a_sistemas.id_sistema and a_sistemas.id_sistema > 1) " +
            "left join g_perfis_acessos g_perfis_acessos on (a_sistemas_controllers.id_sistema_controller = g_perfis_acessos.id_sistema_controller and g_perfis_acessos.id_perfil = " + id.ToString() + ") " +
            "left join a_sistemas_modulos a_sistemas_modulos on (a_sistemas_controllers.id_sistema_modulo = a_sistemas_modulos.id_sistema_modulo) " +
            "left join a_sistemas_grupos a_sistemas_grupos on(a_sistemas_grupos.id_sistema_grupo = a_sistemas_controllers.id_sistema_grupo) " +
            "where a_sistemas_controllers.ativo = 1 " +
            "and ((a_sistemas_controllers.id_perfil_especial is null) or (a_sistemas_controllers.adm_perfil_especial = 1)) " +
            "and a_sistemas.ativo = 1 " +
            "and a_sistemas_modulos.ativo = 1 " +
            "order by a_sistemas_controllers.id_sistema, a_sistemas_grupos.nome, a_sistemas_controllers.title_perfil ";
            var allCustomAcessosN1 = db.Database.SqlQuery<CstPerfisAcessos>(sqlTempAcessos).ToList();
            //var allCustomAcessosN2 = db.Database.SqlQuery<CstPerfisAcessos>(sqlTempAcessos).ToList();

            foreach (CstPerfisAcessos ItemN1 in allCustomAcessosN1)
            {
                if (ItemN1.id_sistema_controller_pai == 0)
                {
                    record_YesPerfisAcessosModel.allCstPerfisAcessos.Add(ItemN1);
                    var allCustomAcessosN2 = allCustomAcessosN1.Where(n => n.id_sistema_controller_pai == ItemN1.id_sistema_controller).ToList();
                    foreach (CstPerfisAcessos ItemN2 in allCustomAcessosN2)
                    {
                        ItemN2.title_perfil = "-&nbsp;-&nbsp;-&nbsp;-&nbsp;-&nbsp;&nbsp;" + ItemN2.title_perfil;
                        record_YesPerfisAcessosModel.allCstPerfisAcessos.Add(ItemN2);
                    }
                }
            }
            var allControllers = db.a_sistemas_controllers.ToList();

            if (record_g_perfis == null)
            {
                return RedirectToAction("Index", "Error", new { area = "" });
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Perfil</b>" + LibStringFormat.GetTabHtml(1) + record_YesPerfisAcessosModel.g_perfis.id_perfil.EmptyIfNull().ToString() + " - " + record_YesPerfisAcessosModel.g_perfis.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_YesPerfisAcessosModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Perfis_*,g_Perfis_Actionupdate")]
        public ActionResult Edit(CstViewPerfisAcessosModel record_YesPerfisAcessosModel)
        {
            if (ModelState.IsValid)
            {
                IQueryable<g_perfis> listaPerfis = db.g_perfis.Where(p => (p.nome == record_YesPerfisAcessosModel.g_perfis.nome) && (p.id_perfil != record_YesPerfisAcessosModel.g_perfis.id_perfil));
                foreach (g_perfis validacao in listaPerfis)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_YesPerfisAcessosModel.g_perfis.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                g_perfis RecordGPerfis = db.g_perfis.Find(record_YesPerfisAcessosModel.g_perfis.id_perfil);
                RecordGPerfis.nome = record_YesPerfisAcessosModel.g_perfis.nome.Trim().ToUpper();
                RecordGPerfis.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                RecordGPerfis.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(RecordGPerfis).State = EntityState.Modified;
                try
                {
                    // Remover os acessos do perfil atual e cadastrar os novos 
                    if ((CachePersister.userIdentity.Administrador == true) || (CachePersister.userIdentity.IdPerfil > 0)) // (somente os administradores tem esse acesso)
                    {
                        db.g_perfis_acessos.RemoveRange(db.g_perfis_acessos.Where(x => x.id_perfil == RecordGPerfis.id_perfil));
                        foreach (CstPerfisAcessos itemcstPerfisAcessos in record_YesPerfisAcessosModel.allCstPerfisAcessos)
                        {
                            if (itemcstPerfisAcessos.id_sistema_controller == 130) 
                            {
                                int i = itemcstPerfisAcessos.id_sistema_controller;
                            }
                            if ((itemcstPerfisAcessos.ativo == true) && ((itemcstPerfisAcessos.action_read == true) || (itemcstPerfisAcessos.action_run == true))) // O Item precisa estar ativo e o perfil precisa estar autorizado para ler ou executar
                            {
                                g_perfis_acessos newRecord_g_perfis_acessos = new g_perfis_acessos();
                                newRecord_g_perfis_acessos.id_perfil = record_YesPerfisAcessosModel.g_perfis.id_perfil;
                                newRecord_g_perfis_acessos.id_sistema_controller = itemcstPerfisAcessos.id_sistema_controller;
                                newRecord_g_perfis_acessos.action_run = itemcstPerfisAcessos.action_run;
                                newRecord_g_perfis_acessos.action_create = itemcstPerfisAcessos.action_create;
                                newRecord_g_perfis_acessos.action_read = itemcstPerfisAcessos.action_read;
                                newRecord_g_perfis_acessos.action_update = itemcstPerfisAcessos.action_update;
                                newRecord_g_perfis_acessos.action_delete = itemcstPerfisAcessos.action_delete;
                                newRecord_g_perfis_acessos.action_manager = itemcstPerfisAcessos.action_manager;
                                newRecord_g_perfis_acessos.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                                newRecord_g_perfis_acessos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                                db.g_perfis_acessos.Add(newRecord_g_perfis_acessos);
                            }
                        }
                    }
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

            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Perfil</b>" + LibStringFormat.GetTabHtml(1) + record_YesPerfisAcessosModel.g_perfis.id_perfil.EmptyIfNull().ToString() + " - " + record_YesPerfisAcessosModel.g_perfis.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_YesPerfisAcessosModel);
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