// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
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
    // Autorização por action: CRUD exige g_Usuarios_*; troca de senha inclui portal/vendedor (sem role "*" na classe).
    public class UsuariosController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Usuarios";

        public UsuariosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_*,g_Usuarios_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Usuários";
            var model = new CstUsuariosIndex
            {
                UsuariosIndex_id_usuario = String.Empty,
                UsuariosIndex_nome = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, nomeRestore;
            if (TryParseFiltroUsuariosSemicolon(filtroPersistido.sql_filtro, out idRestore, out nomeRestore))
            {
                model.UsuariosIndex_id_usuario = idRestore;
                model.UsuariosIndex_nome = nomeRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(nomeRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_*,g_Usuarios_Actionread")]
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

                var baseQuery = db.g_usuarios.AsNoTracking().Where(p => p.id_perfil > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string nomeStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroUsuariosSemicolon(recordFiltro.sql_filtro, out idStr, out nomeStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(nomeStr);
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

                IQueryable<Db.g_usuarios> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroUsuariosNaQuery(query, idStr, nomeStr);
                    LibDB.setFilterByUser(MontarFiltroUsuariosPersistido(idStr, nomeStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoUsuariosNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(u => new { u.id_usuario, u.ativo, u.nome, u.login, u.email, u.id_perfil })
                    .ToList();

                var idsPerfil = page.Where(x => x.id_perfil > 0).Select(x => x.id_perfil).Distinct().ToList();
                var dictPerfil = idsPerfil.Count == 0
                    ? new Dictionary<int, string>()
                    : db.g_perfis.AsNoTracking().Where(p => idsPerfil.Contains(p.id_perfil))
                        .ToDictionary(p => p.id_perfil, p => p.nome ?? String.Empty);

                var list = new List<string[]>();
                foreach (var c in page)
                {
                    string _ativo = c.ativo == true
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                    string _nomePerfil = String.Empty;
                    if (c.id_perfil > 0 && dictPerfil.TryGetValue(c.id_perfil, out string np))
                    {
                        _nomePerfil = np;
                    }
                    list.Add(new[]
                    {
                        "",
                        c.id_usuario.ToString(),
                        _ativo,
                        c.nome.EmptyIfNull().ToString(),
                        c.login.EmptyIfNull().ToString(),
                        c.email.EmptyIfNull().ToString(),
                        _nomePerfil
                    });
                }

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

        private static bool TryParseFiltroUsuariosSemicolon(string raw, out string id, out string nome)
        {
            id = nome = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            nome = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(nome);
        }

        private static string MontarFiltroUsuariosPersistido(string id, string nome)
        {
            return (id ?? String.Empty) + ";" + (nome ?? String.Empty);
        }

        private static IQueryable<Db.g_usuarios> AplicarFiltroUsuariosNaQuery(IQueryable<Db.g_usuarios> query, string idStr, string nomeStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idUsuario))
            {
                query = query.Where(p => p.id_usuario == idUsuario);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(nomeStr, out string padraoNome))
            {
                query = query.Where(p => p.nome != null && DbFunctions.Like(p.nome, padraoNome));
            }
            return query;
        }

        private static IQueryable<Db.g_usuarios> AplicarOrdenacaoUsuariosNaQuery(IQueryable<Db.g_usuarios> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                switch (param.iSortCol_0)
                {
                    case 1:
                        return asc ? query.OrderByDescending(p => p.ativo).ThenBy(p => p.id_usuario)
                            : query.OrderBy(p => p.ativo).ThenByDescending(p => p.id_usuario);
                    case 3:
                        return asc ? query.OrderByDescending(p => p.ativo).ThenBy(p => p.nome)
                            : query.OrderByDescending(p => p.ativo).ThenByDescending(p => p.nome);
                    case 4:
                        return asc ? query.OrderByDescending(p => p.ativo).ThenBy(p => p.login)
                            : query.OrderByDescending(p => p.ativo).ThenByDescending(p => p.login);
                    case 5:
                        return asc ? query.OrderByDescending(p => p.ativo).ThenBy(p => p.email)
                            : query.OrderByDescending(p => p.ativo).ThenByDescending(p => p.email);
                }
            }
            return query.OrderByDescending(p => p.ativo).ThenBy(p => p.nome);
        }
        #endregion

        #region PreencherLookupsCreateEdit()
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_*,g_Usuarios_Actioncreate,g_Usuarios_Actionupdate")]
        public void PreencherLookupsCreateEdit()
        {
            // ComboPerfil
            var comboPerfil = new List<SelectListItem>();
            IQueryable<g_perfis> listaDbPefil = null;
            if (CachePersister.userIdentity.IdPerfil == 1) { listaDbPefil = db.g_perfis.Select(p => p).OrderBy(p => p.nome); }
            else { listaDbPefil = db.g_perfis.Where(p => p.id_perfil > 1).OrderBy(p => p.nome); };
            foreach (g_perfis item1 in listaDbPefil)
            {
                comboPerfil.Add(new SelectListItem { Value = item1.id_perfil.ToString(), Text = item1.nome.ToString() });
            }
            ViewBag.comboPerfil = comboPerfil;


            // ComboColigada
            var comboColigada = new List<SelectListItem>();
            IQueryable<g_coligadas> listaDbColigada = null;
            if (CachePersister.userIdentity.IdPerfil == 1) { listaDbColigada = db.g_coligadas.Select(p => p).OrderBy(p => p.razao_social); }
            foreach (g_coligadas item2 in listaDbColigada)
            {
                comboColigada.Add(new SelectListItem { Value = item2.id_coligada.ToString(), Text = item2.razao_social.ToString() });
            }
            ViewBag.comboColigada = comboColigada;


            // ComboFilial
            var comboFilial = new List<SelectListItem>();
            IQueryable<g_filiais> listaDbFilial = null;
            if (CachePersister.userIdentity.IdPerfil == 1) { listaDbFilial = db.g_filiais.Select(p => p).OrderBy(p => p.nome); }
            else { listaDbFilial = db.g_filiais.Where(p => p.id_coligada == 1 && p.id_filial == 1).OrderBy(p => p.nome); };
            foreach (g_filiais item3 in listaDbFilial)
            {
                comboFilial.Add(new SelectListItem { Value = item3.id_filial.ToString(), Text = item3.nome.ToString() });
            }
            ViewBag.comboFilial = comboFilial;


            // ComboLogomarcas
            var comboLogomarca = new List<SelectListItem>();
            IQueryable<g_logomarcas> listaDbLogomarca = null;
            if (CachePersister.userIdentity.IdPerfil == 1) { listaDbLogomarca = db.g_logomarcas.Select(p => p).OrderBy(p => p.nome); }
            else { listaDbLogomarca = db.g_logomarcas.Where(p => p.id_logomarca > 0).OrderBy(p => p.nome); };
            foreach (g_logomarcas item4 in listaDbLogomarca)
            {
                comboLogomarca.Add(new SelectListItem { Value = item4.id_logomarca.ToString(), Text = item4.nome.ToString() });
            }
            ViewBag.comboLogomarca = comboLogomarca;
        }
        #endregion

        #region Create
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_*,g_Usuarios_Actioncreate")]
        public ActionResult Create()
        {
            PreencherLookupsCreateEdit();
            g_usuarios newRecord = new g_usuarios();
            newRecord.ativo = true;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Usuário</b";
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_*,g_Usuarios_Actioncreate")]
        public ActionResult Create(g_usuarios ViewRecordUsuarios)
        {
            if (ModelState.IsValid)
            {
                ViewRecordUsuarios.nome = LibStringFormat.FormatarTextoCadastroNormal(ViewRecordUsuarios.nome);

                IQueryable<g_usuarios> listaUsuarios = db.g_usuarios.Where(p => p.nome == ViewRecordUsuarios.nome || p.login == ViewRecordUsuarios.login || p.email == ViewRecordUsuarios.email);
                foreach (g_usuarios validacao in listaUsuarios)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(ViewRecordUsuarios.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [Id. " + validacao.id_usuario.ToString() + "]"); }

                    // Validação Email
                    if (validacao.email.ToString().ToUpper().Equals(ViewRecordUsuarios.email.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Email] duplicado na base de dados [Id. " + validacao.id_usuario.ToString() + "]"); }

                    // Validação Login
                    if (validacao.login.ToString().ToUpper().Equals(ViewRecordUsuarios.login.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Login] duplicado na base de dados [Id. " + validacao.id_usuario.ToString() + "]"); }
                }
            }
            if (ModelState.IsValid)
            {
                ViewRecordUsuarios.gc_param_grupo_vendedor = "0";
                ViewRecordUsuarios.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                ViewRecordUsuarios.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_usuarios.Add(ViewRecordUsuarios);
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
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Usuário</b";
            return View("CreateEdit", ViewRecordUsuarios);
        }
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_*,g_Usuarios_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0)) // O Usuário SuperAdm não pode ser editado
            {
                return RedirectToAction("Index", "Error", new { area = "" });
            }
            g_usuarios record_g_usuarios = db.g_usuarios.Find(id);
            if (record_g_usuarios == null)
            {
                return RedirectToAction("Index");
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Usuário</b>" + LibStringFormat.GetTabHtml(1) + record_g_usuarios.id_usuario.EmptyIfNull().ToString() + " - " + record_g_usuarios.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_usuarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_*,g_Usuarios_Actionupdate")]
        public ActionResult Edit(g_usuarios ViewRecordUsuarios)
        {
            if (ModelState.IsValid)
            {
                ViewRecordUsuarios.nome = LibStringFormat.FormatarTextoCadastroNormal(ViewRecordUsuarios.nome);
                IQueryable<g_usuarios> listaUsuarios = db.g_usuarios.Where(p => (p.nome == ViewRecordUsuarios.nome || p.login == ViewRecordUsuarios.login || p.email == ViewRecordUsuarios.email) && (p.id_usuario != ViewRecordUsuarios.id_usuario));
                foreach (g_usuarios validacao in listaUsuarios)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(ViewRecordUsuarios.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [Id. " + validacao.id_usuario.ToString() + "]"); }

                    // Validação Email
                    if (validacao.email.ToString().ToUpper().Equals(ViewRecordUsuarios.email.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Email] duplicado na base de dados [Id. " + validacao.id_usuario.ToString() + "]"); }

                    // Validação Login
                    if (validacao.login.ToString().ToUpper().Equals(ViewRecordUsuarios.login.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Login] duplicado na base de dados [Id. " + validacao.id_usuario.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                g_usuarios RecordUsuario = db.g_usuarios.Find(ViewRecordUsuarios.id_usuario);
                RecordUsuario.ativo = ViewRecordUsuarios.ativo;
                RecordUsuario.nome = ViewRecordUsuarios.nome;
                RecordUsuario.email = ViewRecordUsuarios.email;
                RecordUsuario.login = ViewRecordUsuarios.login;
                RecordUsuario.senha = ViewRecordUsuarios.senha;
                RecordUsuario.id_perfil = ViewRecordUsuarios.id_perfil;
                RecordUsuario.id_logomarca = ViewRecordUsuarios.id_logomarca;
                RecordUsuario.id_coligada = ViewRecordUsuarios.id_coligada;
                RecordUsuario.id_filial = ViewRecordUsuarios.id_filial;
                RecordUsuario.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                RecordUsuario.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(RecordUsuario).State = EntityState.Modified;
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
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Usuário</b>" + LibStringFormat.GetTabHtml(1) + ViewRecordUsuarios.id_usuario.EmptyIfNull().ToString() + " - " + ViewRecordUsuarios.nome.EmptyIfNull().ToString();
            return View("CreateEdit", ViewRecordUsuarios);
        }
        #endregion

        #region ModalUsuarioTrocarSenha
        /// <summary>Troca de senha (logons U/L/C/V). Roles <c>g_Vendedores_*</c> — perfil vendedor; módulo PortalVendedor foi removido (NFE-1).</summary>
        [CustomAuthorize(Roles = "SuperAdmin,Admin,*,Home,gc_PortalCliente_PortalFinanceiro,g_Vendedores_Default,g_Vendedores_*")]
        public ActionResult ModalUsuarioTrocarSenha(int? id)
        {
            String TokenAcesso = CachePersister.userIdentity.TokenAcesso;


            if (TokenAcesso.StartsWith("U")) //Usuario
            {
                ViewBag.Title = "Usuário - Alterar Senha";
            }
            else if (TokenAcesso.StartsWith("L")) // Logon
            {
                ViewBag.Title = "Logon - Alterar Senha";
            }
            else if (TokenAcesso.StartsWith("C")) // Cliente
            {
                ViewBag.Title = "Cliente - Alterar Senha";
            }
            else if (TokenAcesso.StartsWith("V")) // Vendedor
            {
                ViewBag.Title = "Vendedor - Alterar Senha";
            }

                return View("ModalUsuarioTrocarSenha");
        }

        [HttpPost]
        [GdiValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,*,Home,gc_PortalCliente_PortalFinanceiro,g_Vendedores_Default,g_Vendedores_*")]
        public ActionResult AjaxUsuarioTrocarSenha(g_usuarios record_g_usuarios)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            if ((record_g_usuarios.senha.EmptyIfNull().ToString().Length < 3) || (record_g_usuarios.login.EmptyIfNull().ToString().Length < 3))
            {
                msgRetorno = "A Senha deve ter no mínimo 3 caracteres";
            }
            else if (record_g_usuarios.senha.EmptyIfNull().ToString() != record_g_usuarios.login.EmptyIfNull().ToString())
            {
                msgRetorno = "A Senha/Confirmação devem ser idênticas";
            }
            else if (record_g_usuarios.senha.EmptyIfNull().ToString().IndexOf("*") >= 0)
            {
                msgRetorno = "A Senha não poderá conter o caracter * (asterisco)";
            }
            else
            {
                String TokenAcesso = CachePersister.userIdentity.TokenAcesso;
                if (TokenAcesso.StartsWith("U")) // Usuário Comum
                {
                    g_usuarios record_g_usuario_alterar = db.g_usuarios.Find(CachePersister.userIdentity.IdUsuario);
                    record_g_usuario_alterar.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                    record_g_usuario_alterar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    record_g_usuario_alterar.senha = record_g_usuarios.senha;
                    db.Entry(record_g_usuario_alterar).State = EntityState.Modified;
                    try
                    {
                        db.SaveChanges();
                        sucesso = true;
                        msgRetorno = "Senha do Usuário <b>Alterada</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                    }
                    catch (DbEntityValidationException ex)
                    {
                        return JsonAjaxErroValidacao(ex);
                    }
                    catch (Exception e)
                    {
                        return JsonAjaxErro(e);
                    }

                }
            }
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
        }

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