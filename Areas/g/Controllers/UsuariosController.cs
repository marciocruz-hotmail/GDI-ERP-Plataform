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
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Controllers
{
    // O CustomAuthorize desse controler é segmentado por Actions
    // Todos podem trocar a senha
    [CustomAuthorize(Roles = "*")]
    public class UsuariosController : Controller
    {
        private GdiPlataformEntities db;

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
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Usuarios_*,g_Usuarios_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            const string filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.g_usuarios>(); // Lista vazia - Inicialização
            allRecords = db.g_usuarios.Where(p => p.id_perfil > 0).OrderByDescending(p => p.ativo).ThenBy(p => p.nome).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);

            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {
                String _ativo = c.ativo == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                String _nomePerfil = string.Empty;
                if (c.id_perfil > 0)
                {
                    var pf = db.g_perfis.Find(c.id_perfil);
                    if (pf != null) { _nomePerfil = pf.nome.ToString(); }
                }

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_usuario.ToString(),
                                    _ativo,
                                    c.nome.EmptyIfNull().ToString(),
                                    c.login.EmptyIfNull().ToString(),
                                    c.email.EmptyIfNull().ToString(),
                                    _nomePerfil.EmptyIfNull().ToString()
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
                    ModelState.AddModelError("Model", LibExceptions.getDbEntityValidationException(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", LibExceptions.getExceptionShortMessage(e));
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
                    ModelState.AddModelError("Model", LibExceptions.getDbEntityValidationException(ex));
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Model", LibExceptions.getExceptionShortMessage(e));
                }
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Usuário</b>" + LibStringFormat.GetTabHtml(1) + ViewRecordUsuarios.id_usuario.EmptyIfNull().ToString() + " - " + ViewRecordUsuarios.nome.EmptyIfNull().ToString();
            return View("CreateEdit", ViewRecordUsuarios);
        }
        #endregion

        #region ModalUsuarioTrocarSenha
        [CustomAuthorize(Roles = "*")]
        public ActionResult ModalUsuarioTrocarSenha(int? id)
        {
            String TokenAcesso = CachePersister.userIdentity.TokenAcesso;


            //String IdColigada = TempData["IdColigada"].ToString();
            

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

                return View();
        }

        [HttpPost]
        [CustomAuthorize(Roles = "*")]
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
                        sucesso = false;
                        msgRetorno = LibExceptions.getDbEntityValidationException(ex);
                    }
                    catch (Exception e)
                    {
                        sucesso = false;
                        msgRetorno = LibExceptions.getExceptionShortMessage(e);
                    }

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