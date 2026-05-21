using DocumentFormat.OpenXml.Drawing.Charts;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Models;
using GdiPlataform.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;

namespace GdiPlataform.Domain
{
    public class Contexto
    {
        private GdiPlataformEntities _contextDbInstance;

        private GdiPlataformEntities ContextDb
        {
            get
            {
                if (_contextDbInstance != null)
                    return _contextDbInstance;
                var name = CachePersister.dataBase;
                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidOperationException("A base de dados da sessão não está disponível. Efetue nova conexão.");
                _contextDbInstance = new GdiPlataformEntities(name);
                return _contextDbInstance;
            }
        }

        public IEnumerable<NavbarItemMenu> getNavbarItemsMenu()
        {
            var menu = new List<NavbarItemMenu>();
            UserIdentity g_usuarioContexto = CachePersister.userIdentity;

            try
            {
                // Inicializando a QueryDeControllers
                var queryControllers = (from _c in ContextDb.a_sistemas_controllers
                                        join _p in ContextDb.g_perfis_acessos on _c.id_sistema_controller equals _p.id_sistema_controller
                                        select new { controllers = _c, nomeModulo = String.Empty, nomeSistema = String.Empty, perfil = _p }).ToList();

                // Usuário Contexto
                // Regras de Acesso
                List<string> regrasAcesso = new List<string>();
                regrasAcesso.Add("*");
                if (g_usuarioContexto.IdUsuario == 0) { regrasAcesso.Add("SuperAdmin"); }
                else if (g_usuarioContexto.Administrador == true) { regrasAcesso.Add("Admin"); }
                else if (g_usuarioContexto.IdUsuario > 0) { regrasAcesso.Add("Home"); }



                if (g_usuarioContexto.IdUsuario == 0)   // Lista de todos os Controllers ativos, Módulos Ativos e Sistemas Ativos
                {
                    queryControllers = (from _c in ContextDb.a_sistemas_controllers
                                        where _c.ativo == true
                                        join _m in ContextDb.a_sistemas_modulos on _c.id_sistema_modulo equals _m.id_sistema_modulo
                                        where _m.ativo == true
                                        join _s in ContextDb.a_sistemas on _c.id_sistema equals _s.id_sistema
                                        where _s.ativo == true
                                        from _p in ContextDb.g_perfis_acessos.Where(o => o.id_perfil == 0).DefaultIfEmpty() // Left outer join
                                        select new { controllers = _c, nomeModulo = _m.nome, nomeSistema = _s.nome, perfil = _p }).ToList();
                }
                else if (g_usuarioContexto.Administrador == true) // Lista de todos os Controllers ativos, Módulos Ativos e Sistemas Ativos (Exceto controles especiais)
                {
                    queryControllers = (from _c in ContextDb.a_sistemas_controllers
                                        where ((_c.ativo == true && _c.id_perfil_especial == null) || (_c.ativo == true && _c.id_perfil_especial != null && _c.adm_perfil_especial == true))
                                        join _m in ContextDb.a_sistemas_modulos on _c.id_sistema_modulo equals _m.id_sistema_modulo
                                        where _m.ativo == true
                                        join _s in ContextDb.a_sistemas on _c.id_sistema equals _s.id_sistema
                                        where _s.ativo == true
                                        from _p in ContextDb.g_perfis_acessos.Where(o => o.id_perfil == 0).DefaultIfEmpty() // Left outer join
                                        select new { controllers = _c, nomeModulo = _m.nome, nomeSistema = _s.nome, perfil = _p }).ToList();
                }
                else
                {
                    queryControllers = (from _c in ContextDb.a_sistemas_controllers
                                        where _c.ativo == true
                                        join _m in ContextDb.a_sistemas_modulos on _c.id_sistema_modulo equals _m.id_sistema_modulo
                                        where _m.ativo == true
                                        join _s in ContextDb.a_sistemas on _c.id_sistema equals _s.id_sistema
                                        where _s.ativo == true
                                        join _p in ContextDb.g_perfis_acessos on _c.id_sistema_controller equals _p.id_sistema_controller
                                        where _p.id_perfil == g_usuarioContexto.IdPerfil
                                        select new { controllers = _c, nomeModulo = _m.nome, nomeSistema = _s.nome, perfil = _p }).ToList();
                }

                //Lista de todos os Grupos de Módulos ativos
                IQueryable<a_sistemas_grupos> listaSistemasGrupos = ContextDb.a_sistemas_grupos.Where(gruposSistemas => gruposSistemas.ativo == true);
                foreach (a_sistemas_grupos grupoSistemas in listaSistemasGrupos)
                {
                    var queryControllersN1 = queryControllers.Where(p => p.controllers.ativo == true);

                    if (g_usuarioContexto.IdUsuario == 0)                  // Se SuperAdmin poderá visualizar todos os Sistemas
                    {
                        //queryControllersN1 = queryControllers.Where((p => p.controllers.id_sistema_grupo == grupoSistemas.id_sistema_grupo)).OrderBy(p => p.controllers.ordem).ThenBy(p => p.controllers.title_menu);
                        queryControllersN1 = queryControllers.Where((p => p.controllers.id_sistema_grupo == grupoSistemas.id_sistema_grupo)).OrderBy(p => p.controllers.title_menu);
                    }
                    else if (g_usuarioContexto.Administrador == true)      // Se Admin poderá ver visualizar os grupos exceto administração
                    {
                        //queryControllersN1 = queryControllers.Where((p => p.controllers.id_sistema_grupo == grupoSistemas.id_sistema_grupo)).Where(p => p.controllers.id_sistema > 1).OrderBy(p => p.controllers.ordem).ThenBy(p => p.controllers.title_menu);
                        queryControllersN1 = queryControllers.Where((p => p.controllers.id_sistema_grupo == grupoSistemas.id_sistema_grupo)).Where(p => p.controllers.id_sistema > 1).OrderBy(p => p.controllers.title_menu);
                    }
                    else if (g_usuarioContexto.IdUsuario > 0)               // Os usuários padrões irão visualizar os módulos parametrizados para seu perfil
                    {
                        //queryControllersN1 = queryControllers.Where((p => p.controllers.id_sistema_grupo == grupoSistemas.id_sistema_grupo)).Where(p => p.controllers.id_sistema > 1).OrderBy(p => p.controllers.ordem).ThenBy(p => p.controllers.title_menu);
                        queryControllersN1 = queryControllers.Where((p => p.controllers.id_sistema_grupo == grupoSistemas.id_sistema_grupo)).Where(p => p.controllers.id_sistema > 1).OrderBy(p => p.controllers.title_menu);
                    }
                    else if (g_usuarioContexto.IdUsuario < 0)               // Os usuários especiais irão visualizar os módulos parametrizados para seu perfil
                    {
                        //queryControllersN1 = queryControllers.Where((p => p.controllers.id_sistema_grupo == grupoSistemas.id_sistema_grupo)).Where(p => p.controllers.id_sistema > 1).OrderBy(p => p.controllers.ordem).ThenBy(p => p.controllers.title_menu);
                        queryControllersN1 = queryControllers.Where((p => p.controllers.id_sistema_grupo == grupoSistemas.id_sistema_grupo)).Where(p => p.controllers.id_sistema > 1).OrderBy(p => p.controllers.title_menu);
                    }

                    if (queryControllersN1.Count() > 0)
                    {
                        int qtdControllersMenu = 0;
                        foreach (var itemControllerN1 in queryControllersN1)
                        {
                            if (itemControllerN1.controllers.is_menu == true)
                            {
                                qtdControllersMenu += 1;
                                menu.Add(new NavbarItemMenu
                                {
                                    Id = itemControllerN1.controllers.id_sistema_controller + 100,
                                    level = 2,
                                    Ordem = itemControllerN1.controllers.ordem,
                                    nameOption = itemControllerN1.controllers.title_menu,
                                    controller = itemControllerN1.controllers.controller.ToString(),
                                    action = itemControllerN1.controllers.action.ToString(),
                                    status = true,
                                    isParent = false,
                                    parentId = grupoSistemas.id_sistema_grupo,
                                    area = itemControllerN1.controllers.area
                                });
                            }
                            regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_Default");
                            if ((g_usuarioContexto.IdUsuario == 0) || (g_usuarioContexto.Administrador == true))
                            {
                                if (itemControllerN1.controllers.action.ToString().ToLower() == "index")
                                {
                                    regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_*");
                                }
                                else
                                {
                                    regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString());
                                    regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString() + "_*");
                                }
                            }
                            else
                            {
                                if (itemControllerN1.controllers.action.ToString().ToLower() == "index") 
                                {
                                    if (itemControllerN1.perfil.action_create == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_Actioncreate"); };
                                    if (itemControllerN1.perfil.action_read == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_Actionread"); };
                                    if (itemControllerN1.perfil.action_update == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_Actionupdate"); };
                                    if (itemControllerN1.perfil.action_delete == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_Actiondelete"); };
                                    if (itemControllerN1.perfil.action_run == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_Actionrun"); };
                                    if (itemControllerN1.perfil.action_manager == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_Actionmanager"); };
                                }
                                else
                                {
                                    if (itemControllerN1.perfil.action_create == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString() + "_Actioncreate"); };
                                    if (itemControllerN1.perfil.action_read == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString() + "_Actionread"); };
                                    if (itemControllerN1.perfil.action_update == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString() + "_Actionupdate"); };
                                    if (itemControllerN1.perfil.action_delete == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString() + "_Actiondelete"); };
                                    if (itemControllerN1.perfil.action_run == true) 
                                    { 
                                        regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString() + "_Actionrun");
                                        regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString());
                                    };
                                    if (itemControllerN1.perfil.action_manager == true) { regrasAcesso.Add(itemControllerN1.controllers.area.ToString() + "_" + itemControllerN1.controllers.controller.ToString() + "_" + itemControllerN1.controllers.action.ToString() + "_Actionmanager"); };
                                }
                            }
                        }
                        if (qtdControllersMenu > 0)
                        {
                            menu.Add(new NavbarItemMenu { Id = grupoSistemas.id_sistema_grupo, level = 1, Ordem = grupoSistemas.ordem, nameOption = grupoSistemas.nome.ToString(), imageClass = grupoSistemas.image_class.ToString(), status = true, isParent = true, parentId = 0 });
                        }

                    }
                }
                
                CachePersister.userIdentity.Roles = regrasAcesso.ToArray(); // Todas as roles
            }
            catch (Exception ex)
            {
                String DescErro = LibExceptions.getExceptionShortMessage(ex);
            }
            return menu.ToList();
        }

        public IEnumerable<NavbarItemMessage> getNavbarItemsMessage()
        {
            var listMessages = new List<NavbarItemMessage>();
            UserIdentity g_usuarioContexto = CachePersister.userIdentity;
            try
            {
                var queryMessages = from _m in ContextDb.g_messages
                                    where (_m.ativo == true && (_m.id_perfil == g_usuarioContexto.IdPerfil || _m.id_usuario == g_usuarioContexto.IdUsuario))
                                    select _m;
                foreach (var itemMessages in queryMessages)
                {
                    listMessages.Add(new NavbarItemMessage
                    {
                        Id = itemMessages.id_message,
                        nameHeader = itemMessages.titulo.ToString(),
                        message = itemMessages.descricao.ToString(),
                        reference = itemMessages.datahora.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                        href = itemMessages.href.ToString()
                    });
                }
            }
            catch (Exception)
            {

            }
            return listMessages.ToList();
        }

        public IEnumerable<NavbarItemTask> getNavbarItemsTask()
        {
            var listTasks = new List<NavbarItemTask>();
            UserIdentity g_usuarioContexto = CachePersister.userIdentity;
            try
            {
                var queryTasks = (from _t in ContextDb.g_tasks
                                  where (_t.ativo == true && (_t.id_perfil == g_usuarioContexto.IdPerfil || _t.id_usuario == g_usuarioContexto.IdUsuario))
                                  select _t).Take(50);
                foreach (var itemTasks in queryTasks)
                {
                    listTasks.Add(new NavbarItemTask
                    {
                        Id = itemTasks.id_task,
                        Name = itemTasks.titulo.ToString(),
                        Message = itemTasks.descricao.ToString(),
                        progressBarType = itemTasks.progressbar_type.ToString(),
                        valueMin = itemTasks.value_min,
                        valueMax = itemTasks.value_max,
                        ValueNow = itemTasks.value_now,
                        href = itemTasks.href.ToString()
                    });
                }
            }
            catch (Exception)
            {

            }
            return listTasks.ToList();
        }

        public IEnumerable<NavbarItemAlert> getNavbarItemsAlert()
        {
            return new List<NavbarItemAlert>();
        }

        public IEnumerable<NavbarItemAtividade> getNavbarItemsAtividade()
        {
            var listAtividades = new List<NavbarItemAtividade>();
            UserIdentity g_usuarioContexto = CachePersister.userIdentity;
            try
            {
                CachePersister.userIdentity.alerts_qtd = 0;
                CachePersister.userIdentity.alerts_msg = string.Empty;
                CachePersister.userIdentity.alerts_show = false;
                /*String SqlAtendimento = " select atendimento.id_atendimento, atendimento.datahora_cadastro, categoria.nome as 'categoria' " +
                                        " from g_atendimentos atendimento " +
                                        " left join g_atendimentos_categorias categoria on (categoria.id_atendimento_categoria = atendimento.id_atendimento_categoria) " +
                                        " where atendimento.concluido = 0 " +
                                        " and (responsavel_id_usuario = " + g_usuarioContexto.IdUsuario.ToString() + " or responsavel_id_departamento = " + g_usuarioContexto.IdDepartamento.ToString() + ") ";
                System.Data.DataTable TableAtendimento = LibDB.GetDataTable(SqlAtendimento, db);
                List<DataRow> RowsAtividades = TableAtendimento.AsEnumerable().ToList();*/

                List<g_atendimentos> ListaAtendimentos = ContextDb.g_atendimentos.Where(a => a.concluido == false && (a.responsavel_id_usuario == g_usuarioContexto.IdUsuario || a.responsavel_id_departamento == g_usuarioContexto.IdDepartamento)).ToList();
                List<g_atendimentos_categorias> ListaCategorias = ContextDb.g_atendimentos_categorias.ToList();

                if (ListaAtendimentos.Count > 0)
                {
                    foreach (g_atendimentos Atendimento in ListaAtendimentos)
                    {
                        listAtividades.Add(new NavbarItemAtividade
                        {
                            Id = Atendimento.id_atendimento,
                            iconClass = "fa-solid fa-ticket",
                            reference = Atendimento.datahora_cadastro.ToString("dd/MM/yy HH:mm"),
                            message = ContextDb.g_atendimentos_categorias.Find(Atendimento.id_atendimento_categoria).nome.EmptyIfNull().ToString(),
                            href = "../../../../../../g/Atendimentos/Edit/" + Atendimento.id_atendimento.EmptyIfNull().ToString().Trim()
                        });
                    }
                    CachePersister.userIdentity.alerts_qtd = ListaAtendimentos.Count;
                    CachePersister.userIdentity.alerts_msg = CachePersister.userIdentity.alerts_qtd.ToString() + " Atendimento(s) Aberto(s)";
                    CachePersister.userIdentity.alerts_show = true;
                }
            }
            catch (Exception ex)
            {
                String DescErro = LibExceptions.getExceptionShortMessage(ex);
            }
            return listAtividades.ToList();
        }
    }

    internal class List<T1, T2, T3>
    {
    }
}