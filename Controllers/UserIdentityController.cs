using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using GdiPlataform.Db;
using GdiPlataform.Domain;
using GdiPlataform.Models;
using GdiPlataform.Security;
using GdiPlataform.ViewModels;
using GdiPlataform.Lib;
using ICSharpCode.SharpZipLib.Checksum;

namespace GdiPlataform.Controllers
{
    public class UserIdentityController : Controller
    {
        private GdiPlataformEntities db;

        // GET: Account
        public ActionResult Index()
        {
            try
            {
                String MsgTokenMFA = string.Empty;
                String MsgError = string.Empty;
                String MsgInfo = string.Empty;
                String LogMFA = string.Empty;
                String SessionId = String.Empty;
                String DeviceId = String.Empty;
                String DeviceInfo = String.Empty;
                String DeviceCRC32 = String.Empty;

                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                if (CachePersister.userIdentity != null)
                    return RedirectToAction("Index", "Home", new { area = "" });
                CachePersister.logout();
                HttpContext.Session["TokenId"] = HttpContext.Session.SessionID.ToString();
                System.Web.HttpContext context = System.Web.HttpContext.Current;

                string hostTenant = GetHostWithoutPort(Request);
                ViewBag.PortalClienteLogin = IsPortalClienteHost(hostTenant);

                string host = this.Request.Headers["Host"].ToLower().Replace("http://", "").Replace("https://", "").Replace("www.", "").Trim().ToLowerInvariant();
                int.TryParse(DateTime.Now.ToString("HH"), out int horaAtual);
                String SessionID = String.Empty;
                int DiaDoAno = LibDateTime.getDataHoraBrasilia().DayOfYear;

                // SessionID
                try
                {
                    SessionID = HttpContext.Session.SessionID.ToString().ToUpper();
                    if (SessionID.ToString().Trim().Length > 20) { SessionID = SessionID.Substring(0, 20); };
                }
                catch (Exception) { }

                // DeviceId 
                string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string[] addresses = ipAddress.Split(',');
                    if (addresses.Length != 0)
                    {
                        DeviceId = addresses[0];
                    }
                }
                else
                {
                    DeviceId = context.Request.ServerVariables["REMOTE_ADDR"];
                }

                // DeviceInfo + DeviceCRC32
                DeviceInfo += "Platform: " + context.Request.Browser.Platform.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "UserAgent: " + context.Request.UserAgent.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenCharactersHeight: " + context.Request.Browser.ScreenCharactersHeight.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenCharactersWidth: " + context.Request.Browser.ScreenCharactersWidth.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenPixelsHeight: " + context.Request.Browser.ScreenPixelsHeight.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenPixelsWidth: " + context.Request.Browser.ScreenPixelsWidth.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenBitDepth: " + context.Request.Browser.ScreenBitDepth.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "Browser.Type: " + context.Request.Browser.Type.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "Browser.Version: " + context.Request.Browser.Version.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "Browser: " + context.Request.Browser.Browser + ";";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(DeviceInfo);
                System.IO.Hashing.Crc32 crc32 = new System.IO.Hashing.Crc32();
                crc32.Append(data);
                byte[] hashBytes = crc32.GetCurrentHash();
                DeviceCRC32 = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();


                ViewBag.WallPaper = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/wallpaper/" + DiaDoAno.ToString() + ".jpg";
                ViewBag.Version = ControlVersion.getShortVersion();
                ViewBag.SessionID = SessionID;
                ViewBag.DeviceId = DeviceId;

                SaveLoginChromeToTempData(
                    ViewBag.WallPaper.ToString(),
                    ViewBag.SessionID.ToString(),
                    ViewBag.DeviceId.ToString(),
                    ViewBag.Version.ToString());
            }
            catch (Exception)
            {
                ViewBag.WallPaper = "";
            }
            ViewBag.Version = ControlVersion.getShortVersion();
            return View(new IdentityViewModel { userIdentity = new UserIdentity() });
        }

        [HttpPost]
        public ActionResult Index(IdentityViewModel avm)
        {
            try
            {
                String MsgTokenMFA = string.Empty;
                String MsgError = string.Empty;
                String MsgInfo = string.Empty;
                String LogMFA = string.Empty;
                String SessionID = String.Empty;
                String DeviceId = String.Empty;
                String DeviceInfo = String.Empty;
                String DeviceCRC32 = String.Empty;

                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                if (CachePersister.userIdentity != null)
                    return RedirectToAction("Index", "Home", new { area = "" });
                CachePersister.logout();
                HttpContext.Session["TokenId"] = HttpContext.Session.SessionID.ToString();
                System.Web.HttpContext context = System.Web.HttpContext.Current;

                // Localizando o Tentanty
                string subDominio = "";
                string host = this.Request.Headers["Host"].ToLower().Replace("http://", "").Replace("https://", "").Replace("www.", "").Trim();
                string dominio = this.Request.Headers["Host"].ToLower().Trim();
                if (host.IndexOf(":") >= 0)
                {
                    host = host.Substring(0, host.IndexOf(":"));
                }
                var index = host.IndexOf(".");
                if (index < 0)
                {
                    subDominio = host.Trim();
                }
                else
                {
                    subDominio = host.Substring(0, index);
                }

                ViewBag.PortalClienteLogin = IsPortalClienteHost(host);

                if (IsPortalClienteHost(host))
                {
                    if (avm?.userIdentity == null)
                    {
                        ViewBag.Error = "Conta inválida!";
                        return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                    }

                    string clienteIdentificador = LibStringFormat.SomenteNumeros(avm.userIdentity.ClienteIdentificador.EmptyIfNull().ToString()).Trim();
                    string clienteCpfCnpj = LibStringFormat.SomenteNumeros(avm.userIdentity.ClienteCpfCnpj.EmptyIfNull().ToString()).Trim();

                    if (clienteCpfCnpj.Length != 11 && clienteCpfCnpj.Length != 14)
                    {
                        ViewBag.Error = "CPF/CNPJ Inválido!";
                        return View("Index", avm);
                    }
                    if (clienteIdentificador.Length == 0)
                    {
                        ViewBag.Error = "Código do Cliente Inválido!";
                        return View("Index", avm);
                    }

                    var allTenantsPortal = SetTenants();
                    CstTenant currentTenantPortal = allTenantsPortal.FirstOrDefault(t => t.subDominio == subDominio);
                    if (currentTenantPortal == null)
                    {
                        ViewBag.Error = "Domínio inválido [" + subDominio + "]!";
                        return View("Index", avm);
                    }

                    LibDB.CheckConnectionDB(currentTenantPortal.database);
                    db = new GdiPlataformEntities(currentTenantPortal.database);

                    int clienteIdentificadorInt = int.Parse(clienteIdentificador);
                    g_clientes recordClientePortal = db.g_clientes.Find(clienteIdentificadorInt);
                    if (recordClientePortal == null)
                    {
                        ViewBag.Error = "Cliente não localizado!";
                        return View("Index", avm);
                    }
                    if (recordClientePortal.ativo == false)
                    {
                        ViewBag.Error = "Cliente Inativo!";
                        return View("Index", avm);
                    }
                    if (recordClientePortal.cpf != clienteCpfCnpj && recordClientePortal.cnpj != clienteCpfCnpj)
                    {
                        ViewBag.Error = "CPF/CNPJ Inválido!";
                        return View("Index", avm);
                    }

                    string dataLimiteSqlP = Convert.ToDateTime(DateTime.Now.AddYears(-1), new CultureInfo("en-US")).ToString("yyyy-MM-dd 00:00:00");
                    string sqlPedidosP = " select mov.* from gc_movimentos mov where id_cliente = " + recordClientePortal.id_cliente.ToString()
                        + " and mov.id_movimento_tipo in (3,4,8) "
                        + " and mov.id_movimento_status = 2 and mov.id_movimento_posicao >= 4 "
                        + " and mov.datahora_aprovacao > '" + dataLimiteSqlP + "' "
                        + " order by mov.datahora_aprovacao desc ";
                    if (db.gc_movimentos.SqlQuery(sqlPedidosP).ToList().Count == 0)
                    {
                        ViewBag.Error = "Não há pedidos para o cliente!";
                        return View("Index", avm);
                    }

                    return CompletePortalClienteLogin(db, recordClientePortal, currentTenantPortal, dominio, subDominio);
                }

                // SessionID
                try
                {
                    SessionID = HttpContext.Session.SessionID.ToString().ToUpper();
                    if (SessionID.ToString().Trim().Length > 20) { SessionID = SessionID.Substring(0, 20); };
                }
                catch (Exception) { }

                // DeviceId 
                string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    string[] addresses = ipAddress.Split(',');
                    if (addresses.Length != 0)
                    {
                        DeviceId = addresses[0];
                    }
                }
                else
                {
                    DeviceId = context.Request.ServerVariables["REMOTE_ADDR"];
                }

                // DeviceInfo + DeviceCRC32
                DeviceInfo += "Platform: " + context.Request.Browser.Platform.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "UserAgent: " + context.Request.UserAgent.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenCharactersHeight: " + context.Request.Browser.ScreenCharactersHeight.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenCharactersWidth: " + context.Request.Browser.ScreenCharactersWidth.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenPixelsHeight: " + context.Request.Browser.ScreenPixelsHeight.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenPixelsWidth: " + context.Request.Browser.ScreenPixelsWidth.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "ScreenBitDepth: " + context.Request.Browser.ScreenBitDepth.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "Browser.Type: " + context.Request.Browser.Type.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "Browser.Version: " + context.Request.Browser.Version.EmptyIfNull().ToString().Trim() + ";";
                DeviceInfo += "Browser: " + context.Request.Browser.Browser + ";";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(DeviceInfo);
                System.IO.Hashing.Crc32 crc32 = new System.IO.Hashing.Crc32();
                crc32.Append(data);
                byte[] hashBytes = crc32.GetCurrentHash();
                DeviceCRC32 = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();


                UserIdentity userIdentity = new UserIdentity
                {
                    DataHoraExpiracao = LibDateTime.getDataHoraBrasilia().AddHours(4), // Tempo máximo de sessão
                    VersionERP = ControlVersion.getShortVersion(),
                    DisplayScreenHeight = avm.userIdentity.DisplayScreenHeight,
                    DisplayScreenWidth = avm.userIdentity.DisplayScreenWidth
                };

                string _ImgLogoSubdominio = string.Empty;
                string _database = string.Empty;
                string _DbConnectionString = string.Empty;

                if (string.IsNullOrEmpty(avm.userIdentity.Acesso) || string.IsNullOrEmpty(avm.userIdentity.Password))
                {
                    ViewBag.Error = "Invalid account!";
                    return View("Index");
                }

                // Tenants
                var allTenants = new List<CstTenant>();
                allTenants = SetTenants();
                CstTenant currentTenant = allTenants.Where(t => t.subDominio == subDominio).FirstOrDefault();
                if (currentTenant == null)
                {
                    ViewBag.Error = "Invalid domain [" + subDominio + "]!";
                    return View("Index");
                }
                else
                {
                    _ImgLogoSubdominio = currentTenant.ImgLogoSubdominio;
                    _database = currentTenant.database;
                    LibDB.CheckConnectionDB(_database);

                    db = new GdiPlataformEntities(_database);
                    List<Db.a_sistemas> allSistemas = db.a_sistemas.ToList();
                    List<Db.a_sistemas_modulos> allSistemasModulos = db.a_sistemas_modulos.ToList();
                    List<Db.a_yesprodutos> allYesProdutos = db.a_yesprodutos.ToList();
                    a_parametros record_a_parametros = db.a_parametros.FirstOrDefault();
                    g_parametros record_g_parametros = db.g_parametros.FirstOrDefault();
                    g_usuarios regUsuario = null;

                    // Parametros
                    userIdentity.EmpresaID = record_a_parametros.empresa_id.GetValueOrDefault();
                    userIdentity.EmpresaNome = record_a_parametros.empresa_nome.ToString();
                    userIdentity.record_g_parametros = record_g_parametros;

                    #region Login por Usuário Padrão
                    // Usuário Padrão
                    var queryUsuario = db.g_usuarios.Where(p => p.ativo == true && p.login == avm.userIdentity.Acesso && p.senha == avm.userIdentity.Password).ToList();
                    if (queryUsuario.Count() > 0) { regUsuario = queryUsuario.First(); }
                    if (regUsuario != null)
                    {
                        // Senha vencida: obrigar troca antes de autenticar
                        if (regUsuario.datahora_proxima_troca.HasValue && DataHoraAtual >= regUsuario.datahora_proxima_troca.Value)
                        {
                            Session["TrocaObrigatoria_IdUsuario"] = regUsuario.id_usuario;
                            Session["TrocaObrigatoria_Login"] = regUsuario.login;
                            Session["TrocaObrigatoria_Database"] = _database;
                            Session["TrocaObrigatoria_IdColigada"] = regUsuario.id_coligada;
                            Session["TrocaObrigatoria_IdFilial"] = regUsuario.id_filial;
                            ApplyLoginChromeToViewBag();
                            return RedirectToAction("TrocaObrigatoriaSenha");
                        }

                        // LogoMarca - Verificar se o usuário tem uma logo específica
                        if (regUsuario.id_logomarca > 0)
                        {
                            int id_logomarca = regUsuario.id_logomarca.GetValueOrDefault();
                            g_logomarcas record_g_logomarcas = db.g_logomarcas.Where(p => p.id_logomarca == id_logomarca).ToList().FirstOrDefault();
                            if (record_g_logomarcas != null)
                            {
                                //_ImgLogoSubdominio = subDominio + "/" + record_g_logomarcas.arquivo.ToString();
                                _ImgLogoSubdominio = record_g_logomarcas.arquivo.ToString();
                            }
                        }

                        // Base de Dados
                        CachePersister.dataBase = _database;

                        _DbConnectionString = ConfigurationManager.ConnectionStrings[_database].ConnectionString;
                        if (_DbConnectionString.IndexOf("homologacao") > 0) { userIdentity.AmbienteDatabase = "Homologação"; }
                        else if (_DbConnectionString.IndexOf("producao") > 0) { userIdentity.AmbienteDatabase = "Produção"; }
                        else { userIdentity.AmbienteDatabase = "Desconhecido"; }


                        // Identificação do Usuário
                        userIdentity.IdUsuario = regUsuario.id_usuario;
                        userIdentity.IdPerfil = regUsuario.id_perfil;
                        userIdentity.TokenAcesso = "U" + userIdentity.IdUsuario; // Usuário + Código do Usuário

                        // Administrador
                        if (regUsuario.id_perfil == 1) { userIdentity.Administrador = true; }
                        else { userIdentity.Administrador = false; }

                        userIdentity.Username = regUsuario.nome;
                        userIdentity.Acesso = regUsuario.login.ToString();
                        userIdentity.Email = regUsuario.email.ToString();
                        userIdentity.Password = regUsuario.senha.ToString();
                        userIdentity.Dominio = dominio;
                        userIdentity.SubDominio = subDominio;
                        userIdentity.ImgLogoSubdominio = _ImgLogoSubdominio;
                        userIdentity.DataHoraUltimoLogin = LibDateTime.getDataHoraBrasilia().ToString("dd/MM/yy HH:mm");
                        CachePersister.userIdentity = userIdentity;

                        // Acessos do Usuário
                        var contexto = new Contexto();
                        ContextoModel contextoModel = new ContextoModel
                        {
                            allNavbarItemMenu = contexto.getNavbarItemsMenu().ToList()
                        };
                        CachePersister.contextoModel = contextoModel;

                        // Sistemas Ativos nos Parâmetros Administrativos
                        CachePersister.allSistemas = allSistemas;

                        // Modulos Ativos nos Parâmetros Administrativos
                        CachePersister.allSistemasModulos = allSistemasModulos;

                        // Produtos Ativos
                        CachePersister.allYesProdutos = allYesProdutos;

                        // Coligada, Filial e Perfil

                        //Verificar se existe dados na tabela g_coligada
                        int idColigadaVerificar = regUsuario.id_coligada;
                        g_coligadas record_g_coligadas = db.g_coligadas.Where(c => c.id_coligada == idColigadaVerificar).ToList().FirstOrDefault();

                        if (record_g_coligadas != null)
                        {
                            CachePersister.userIdentity.id_coligada = regUsuario.id_coligada;
                            CachePersister.userIdentity.NomeColigada = db.g_coligadas.Find(regUsuario.id_coligada).razao_social.ToString();
                        }
                        else
                        {
                            g_usuarios_login_logs RecordUsuarioLoginLog = new g_usuarios_login_logs
                            {
                                id_usuario = regUsuario.id_usuario,
                                login_datahora = DataHoraAtual,
                                login_ip = DeviceId.EmptyIfNull().ToString().Trim(),
                                log = "ERRO: Acesso não liberado, Tabela Coligada Não Parametrizada, acesso negado para usuário!",
                                id_usuario_cadastro = 0,
                                datahora_cadastro = DataHoraAtual,
                                id_coligada = 0,
                                id_filial = 0
                            };
                            db.Entry(RecordUsuarioLoginLog).State = EntityState.Added;
                            db.SaveChanges();

                            ViewBag.Error = "Tabela Coligada Não Parametrizada, acesso negado para usuário!";
                            ApplyLoginChromeToViewBag(SessionID, DeviceId);
                            return View("Index");
                        }

                        //Verificar se existe dados na tabela g_filial
                        int idFilialVerificar = regUsuario.id_filial;
                        g_filiais record_g_filiais = db.g_filiais.Where(f => f.id_filial == idFilialVerificar).ToList().FirstOrDefault();

                        if (record_g_filiais != null)
                        {
                            CachePersister.userIdentity.id_filial = regUsuario.id_filial;
                            CachePersister.userIdentity.FilialNome = db.g_filiais.Find(regUsuario.id_filial).nome.ToString();
                        }
                        else
                        {
                            g_usuarios_login_logs RecordUsuarioLoginLog = new g_usuarios_login_logs
                            {
                                id_usuario = regUsuario.id_usuario,
                                login_datahora = DataHoraAtual,
                                login_ip = DeviceId.EmptyIfNull().ToString().Trim(),
                                log = "ERRO: Acesso não liberado, Tabela Filial Não Parametrizada, acesso negado para usuário!",
                                id_usuario_cadastro = 0,
                                datahora_cadastro = DataHoraAtual,
                                id_coligada = 0,
                                id_filial = 0
                            };
                            db.Entry(RecordUsuarioLoginLog).State = EntityState.Added;
                            db.SaveChanges();

                            ViewBag.Error = "Tabela Filial Não Parametrizada, acesso negado para usuário!";
                            ApplyLoginChromeToViewBag(SessionID, DeviceId);
                            return View("Index");
                        }
                                                
                        //Verificar se existe dados tabela g_perfis
                        int idPerfilVerificar = userIdentity.IdPerfil;
                        g_perfis record_g_perfis = db.g_perfis.Where(p => p.id_perfil == idPerfilVerificar).ToList().FirstOrDefault();
                        
                        if (record_g_perfis != null)
                        {
                            if (userIdentity.IdPerfil > 0) { CachePersister.userIdentity.PerfilNome = db.g_perfis.Find(userIdentity.IdPerfil).nome_display.ToString(); };
                        }
                        else
                        {
                            g_usuarios_login_logs RecordUsuarioLoginLog = new g_usuarios_login_logs
                            {
                                id_usuario = regUsuario.id_usuario,
                                login_datahora = DataHoraAtual,
                                login_ip = DeviceId.EmptyIfNull().ToString().Trim(),
                                log = "ERRO: Acesso não liberado, Tabela Perfil Não Parametrizada, acesso negado para usuário!",
                                id_usuario_cadastro = 0,
                                datahora_cadastro = DataHoraAtual,
                                id_coligada = 0,
                                id_filial = 0
                            };
                            db.Entry(RecordUsuarioLoginLog).State = EntityState.Added;
                            db.SaveChanges();

                            ViewBag.Error = "Tabela Perfil Não Parametrizada, acesso negado para usuário!";
                            ApplyLoginChromeToViewBag(SessionID, DeviceId);
                            return View("Index");
                        }

                        // Verificar se o usuário atual é um vendedor
                        g_vendedores record_g_vendedores = db.g_vendedores.Where(v => v.id_usuario == regUsuario.id_usuario).FirstOrDefault();
                        if (record_g_vendedores != null) { userIdentity.IdVendedor = record_g_vendedores.id_vendedor; }
                        else { userIdentity.IdVendedor = 0; }

                        // Verificar os Parâmetros GC
                        CachePersister.userIdentity.GcParamGrupoVendedor = regUsuario.gc_param_grupo_vendedor.EmptyIfNull().ToString();
                        CachePersister.userIdentity.IdDepartamento = regUsuario.id_departamento;

                        // Verificar a competência do estoque
                        gc_estoque_competencia RecordEstoqueCompetencia = db.gc_estoque_competencia.Where(e => e.status == "A").FirstOrDefault();
                        if (RecordEstoqueCompetencia != null) { CachePersister.userIdentity.GcEstoqueCompetenciaAtual = DateTime.Now.ToString("MM/yyyy"); }
                        else { CachePersister.userIdentity.GcEstoqueCompetenciaAtual = "<b style='color:red;'>FECHADA</b>"; };

                        // Apagar os filtros do usuários
                        String _token = CachePersister.userIdentity.TokenAcesso.EmptyIfNull().ToString().Trim();
                        db.g_filtros.RemoveRange(db.g_filtros.Where(f => f.token == _token).ToList());
                        db.SaveChanges();

                        return RedirectToAction("Index", "Home", new { area = "" });
                    }
                    else
                    {
                        // Se chegar aqui, não atendeu nenhum critério de login
                        g_usuarios_login_logs RecordUsuarioLoginLog = new g_usuarios_login_logs();
                        if (regUsuario != null) { RecordUsuarioLoginLog.id_usuario = regUsuario.id_usuario; } else { RecordUsuarioLoginLog.id_usuario = 0; }
                        RecordUsuarioLoginLog.login_datahora = DataHoraAtual;
                        RecordUsuarioLoginLog.login_ip = DeviceId.EmptyIfNull().ToString().Trim();
                        RecordUsuarioLoginLog.log = "ERRO: Acesso não liberado, Credenciais Inválidas. Usuário: " + avm.userIdentity.Acesso.EmptyIfNull().ToString().Trim() + " Senha: " + avm.userIdentity.Password.EmptyIfNull().ToString().Trim();
                        RecordUsuarioLoginLog.id_usuario_cadastro = 0;
                        RecordUsuarioLoginLog.datahora_cadastro = DataHoraAtual;
                        RecordUsuarioLoginLog.id_coligada = 0;
                        RecordUsuarioLoginLog.id_filial = 0;
                        db.Entry(RecordUsuarioLoginLog).State = EntityState.Added;
                        db.SaveChanges();

                        ViewBag.Error = "Credenciais Inválidas!";
                        ApplyLoginChromeToViewBag(SessionID, DeviceId);
                        return View("Index");
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                CachePersister.logout();
                String msgErro = LibExceptions.getExceptionShortMessage(ex);
                msgErro = msgErro.Replace("The underlying provider failed on Open", "Failed to connect to the database.");
                ViewBag.Error = "Erro ["+msgErro+"]";
                ApplyLoginChromeToViewBag();
                return View("Index");
            }
        }

        #region Troca obrigatória de senha (senha expirada)
        [HttpGet]
        public ActionResult TrocaObrigatoriaSenha()
        {
            if (Session["TrocaObrigatoria_Login"] == null || Session["TrocaObrigatoria_Database"] == null)
            {
                return RedirectToAction("Index");
            }
            var vm = new TrocaObrigatoriaSenhaViewModel { Login = Session["TrocaObrigatoria_Login"].ToString() };
            ViewBag.Version = ControlVersion.getShortVersion();
            ApplyLoginChromeToViewBag();
            if (ViewBag.WallPaper == null || String.IsNullOrEmpty(ViewBag.WallPaper.ToString()))
            {
                ViewBag.WallPaper = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/wallpaper/1.jpg";
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TrocaObrigatoriaSenhaExecute(TrocaObrigatoriaSenhaViewModel vm)
        {
            string login = Session["TrocaObrigatoria_Login"] as string;
            string database = Session["TrocaObrigatoria_Database"] as string;
            object idUsuarioObj = Session["TrocaObrigatoria_IdUsuario"];
            object idColigadaObj = Session["TrocaObrigatoria_IdColigada"];
            object idFilialObj = Session["TrocaObrigatoria_IdFilial"];

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(database) || idUsuarioObj == null)
            {
                LibFlashMessage.SetError(this, "Sessão de troca de senha inválida. Efetue o login novamente.");
                return RedirectToAction("Index");
            }

            int idUsuario = (int)idUsuarioObj;
            int idColigada = idColigadaObj != null ? (int)idColigadaObj : 0;
            int idFilial = idFilialObj != null ? (int)idFilialObj : 0;

            if (string.IsNullOrEmpty(vm?.SenhaAtual))
            {
                LibFlashMessage.SetError(this, "Informe a senha atual.");
                return RedirectToAction("TrocaObrigatoriaSenha");
            }
            if (string.IsNullOrEmpty(vm.NovaSenha))
            {
                LibFlashMessage.SetError(this, "Informe a nova senha.");
                return RedirectToAction("TrocaObrigatoriaSenha");
            }
            if (vm.NovaSenha != vm.ConfirmacaoNovaSenha)
            {
                LibFlashMessage.SetError(this, "A nova senha e a confirmação não conferem.");
                return RedirectToAction("TrocaObrigatoriaSenha");
            }

            string msgComplexidade;
            if (!Lib.PasswordPolicy.ValidarComplexidade(vm.NovaSenha, out msgComplexidade))
            {
                LibFlashMessage.SetError(this, msgComplexidade);
                return RedirectToAction("TrocaObrigatoriaSenha");
            }

            GdiPlataformEntities ctx = null;
            try
            {
                ctx = new GdiPlataformEntities(database);
                g_usuarios regUsuario = ctx.g_usuarios.Find(idUsuario);
                if (regUsuario == null || regUsuario.login != login)
                {
                    LibFlashMessage.SetError(this, "Usuário não encontrado. Efetue o login novamente.");
                    LimparSessionTrocaObrigatoria();
                    return RedirectToAction("Index");
                }
                if (regUsuario.senha != vm.SenhaAtual)
                {
                    LibFlashMessage.SetError(this, "Senha atual incorreta.");
                    return RedirectToAction("TrocaObrigatoriaSenha");
                }

                var ultimas3Senhas = ctx.g_usuarios_senhas_historico
                    .Where(h => h.id_usuario == idUsuario)
                    .OrderByDescending(h => h.datahora_troca)
                    .Take(3)
                    .Select(h => h.senha)
                    .ToList();
                if (!Lib.PasswordPolicy.ValidarNaoReutilizacao(vm.NovaSenha, regUsuario.senha, ultimas3Senhas, out string msgReutilizacao))
                {
                    LibFlashMessage.SetError(this, msgReutilizacao);
                    return RedirectToAction("TrocaObrigatoriaSenha");
                }

                DateTime dataHoraBrasilia = LibDateTime.getDataHoraBrasilia();
                string senhaAntiga = regUsuario.senha;

                using (var transaction = ctx.Database.BeginTransaction())
                {
                    try
                    {
                        var historico = new g_usuarios_senhas_historico
                        {
                            id_usuario = idUsuario,
                            senha = senhaAntiga,
                            sequencia = (ctx.g_usuarios_senhas_historico.Where(h => h.id_usuario == idUsuario).Count() + 1),
                            datahora_troca = dataHoraBrasilia,
                            id_usuario_cadastro = idUsuario,
                            datahora_cadastro = dataHoraBrasilia
                        };
                        ctx.g_usuarios_senhas_historico.Add(historico);

                        regUsuario.senha = vm.NovaSenha;
                        regUsuario.datahora_ultima_troca = dataHoraBrasilia;
                        regUsuario.datahora_proxima_troca = dataHoraBrasilia.AddDays(90);
                        regUsuario.datahora_alteracao = dataHoraBrasilia;
                        regUsuario.id_usuario_alteracao = idUsuario;
                        ctx.Entry(regUsuario).State = EntityState.Modified;

                        var logLogin = new g_usuarios_login_logs
                        {
                            id_usuario = idUsuario,
                            id_cliente = 0,
                            login_datahora = dataHoraBrasilia,
                            login_ip = Request.ServerVariables["REMOTE_ADDR"] ?? "",
                            log = "TROCA_OBRIGATORIA_CONCLUIDA",
                            id_usuario_cadastro = idUsuario,
                            datahora_cadastro = dataHoraBrasilia,
                            id_usuario_alteracao = idUsuario,
                            id_coligada = idColigada,
                            id_filial = idFilial
                        };
                        ctx.g_usuarios_login_logs.Add(logLogin);

                        ctx.SaveChanges();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

                LimparSessionTrocaObrigatoria();
                LibFlashMessage.SetInfo(this, "Senha alterada com sucesso. Efetue o login com a nova senha.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                String msgErro = LibExceptions.getExceptionShortMessage(ex);
                LibFlashMessage.SetError(this, "Erro ao alterar senha [" + msgErro + "]");
                if (ctx != null) ctx.Dispose();
                return RedirectToAction("TrocaObrigatoriaSenha");
            }
            finally
            {
                ctx?.Dispose();
            }
        }

        public ActionResult TrocaObrigatoriaSenhaCancelar()
        {
            LimparSessionTrocaObrigatoria();
            return RedirectToAction("Index");
        }

        private void LimparSessionTrocaObrigatoria()
        {
            Session.Remove("TrocaObrigatoria_IdUsuario");
            Session.Remove("TrocaObrigatoria_Login");
            Session.Remove("TrocaObrigatoria_Database");
            Session.Remove("TrocaObrigatoria_IdColigada");
            Session.Remove("TrocaObrigatoria_IdFilial");
        }
        #endregion

        #region KeepAlive
        [HttpPost]
        public ActionResult KeepAlive()
        {
            // A chegada da request já renova a ASP.NET Session e o SlidingExpiration do MemoryCache.
            // Chamado pelo sessionInactivity.js a cada 5 min enquanto o usuário está ativo.
            return new HttpStatusCodeResult(200);
        }
        #endregion

        #region Logout
        public ActionResult Logout()
        {
            bool porInatividade = "inactivity".Equals(Request.QueryString["reason"], StringComparison.OrdinalIgnoreCase);
            if (porInatividade)
            {
                LibFlashMessage.SetInfo(this, "A sessão foi encerrada devido à inatividade (15 minutos de inatividade). Por favor, faça login novamente.");
            }
            CachePersister.logout();
            return RedirectToAction("Index");
        }
        #endregion

        #region Login chrome TempData (G-LOGIN-01)
        private const string LoginTdWallPaper = "WallPaper";
        private const string LoginTdSessionId = "SessionID";
        private const string LoginTdDeviceId = "DeviceId";
        private const string LoginTdVersion = "Version";

        private void SaveLoginChromeToTempData(string wallpaper, string sessionId, string deviceId, string version)
        {
            TempData[LoginTdWallPaper] = wallpaper;
            TempData[LoginTdSessionId] = sessionId;
            TempData[LoginTdDeviceId] = deviceId;
            TempData[LoginTdVersion] = version;
            KeepLoginChromeTempData();
        }

        private void KeepLoginChromeTempData()
        {
            TempData.Keep(LoginTdWallPaper);
            TempData.Keep(LoginTdSessionId);
            TempData.Keep(LoginTdDeviceId);
            TempData.Keep(LoginTdVersion);
        }

        private void ApplyLoginChromeToViewBag(string sessionIdFallback = null, string deviceIdFallback = null)
        {
            if (TempData[LoginTdWallPaper] != null) { ViewBag.WallPaper = TempData[LoginTdWallPaper]; }
            ViewBag.SessionID = sessionIdFallback ?? (TempData[LoginTdSessionId] ?? ViewBag.SessionID);
            ViewBag.DeviceId = deviceIdFallback ?? (TempData[LoginTdDeviceId] ?? ViewBag.DeviceId);
            if (TempData[LoginTdVersion] != null) { ViewBag.Version = TempData[LoginTdVersion]; }
            KeepLoginChromeTempData();
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region setTenants
        public List<CstTenant> SetTenants()
        {
            var allTenants = new List<CstTenant>();

            CstTenant tenant1 = new CstTenant
            {
                subDominio = "localhost",
                ImgLogoSubdominio = "logoMicrosoft.png",
                database = "GdiPlataformEntities_localhost",
            };
            allTenants.Add(tenant1);

            CstTenant tenant2 = new CstTenant
            {
                subDominio = "gdidigital",
                ImgLogoSubdominio = "logoGdi.png",
                database = "GdiPlataformEntities_gdi_producao",
            };
            allTenants.Add(tenant2);

            CstTenant tenant3 = new CstTenant
            {
                subDominio = "gdidigitalhomologacao",
                ImgLogoSubdominio = "logoGdi.png",
                database = "GdiPlataformEntities_gdi_homologacao",
            };
            allTenants.Add(tenant3);

            CstTenant tenant4 = new CstTenant
            {
                subDominio = "aeroflightx",
                ImgLogoSubdominio = "logoGdi.png",
                database = "GdiPlataformEntities_gdi_producao",
            };
            allTenants.Add(tenant4);

            CstTenant tenant5 = new CstTenant
            {
                subDominio = "homologacao",
                ImgLogoSubdominio = "logoGdi.png",
                database = "GdiPlataformEntities_gdi_homologacao",
            };
            allTenants.Add(tenant5);

            // Portal do cliente: DNS público (app = este ERP monólito; legado GDI-PortalCliente-Plataform descontinuado)
            CstTenant tenantPortalCliente = new CstTenant
            {
                subDominio = "portalflightx",
                ImgLogoSubdominio = "logoGdi.png",
                database = "GdiPlataformEntities_gdi_producao",
            };
            allTenants.Add(tenantPortalCliente);

            return allTenants;
        }
        #endregion

        /// <summary>Sessão portal cliente (CachePersister + role) — usado por AcessoPortal e POST Index em hosts portal.</summary>
        private ActionResult CompletePortalClienteLogin(GdiPlataformEntities dbCtx, g_clientes recordCliente, CstTenant currentTenant, string dominio, string subDominio)
        {
            a_parametros recordAParametros = dbCtx.a_parametros.FirstOrDefault();
            g_parametros recordGParametros = dbCtx.g_parametros.FirstOrDefault();
            if (recordAParametros == null)
            {
                ViewBag.Error = "Parâmetros administrativos não localizados.";
                ViewBag.PortalClienteLogin = true;
                ViewBag.Version = ControlVersion.getShortVersion();
                return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
            }

            UserIdentity userIdentity = new UserIdentity
            {
                DataHoraExpiracao = LibDateTime.getDataHoraBrasilia().AddHours(4),
                VersionERP = ControlVersion.getShortVersion(),
                EmpresaID = recordAParametros.empresa_id.GetValueOrDefault(),
                EmpresaNome = recordAParametros.empresa_nome.ToString(),
                record_g_parametros = recordGParametros ?? new g_parametros(),
                IdPerfil = -900,
                IdUsuario = -900,
                IdCliente = recordCliente.id_cliente,
                TokenAcesso = "C" + recordCliente.id_cliente,
                Administrador = false,
                Username = recordCliente.nome,
                Acesso = recordCliente.nome,
                Email = recordCliente.email_principal,
                Password = recordCliente.senha_portal,
                Dominio = dominio,
                SubDominio = subDominio,
                ImgLogoSubdominio = currentTenant.ImgLogoSubdominio,
                PerfilNome = "Portal do Cliente"
            };

            string connStr = ConfigurationManager.ConnectionStrings[currentTenant.database].ConnectionString;
            if (connStr.IndexOf("homologacao", StringComparison.OrdinalIgnoreCase) >= 0) userIdentity.AmbienteDatabase = "Homologação";
            else if (connStr.IndexOf("producao", StringComparison.OrdinalIgnoreCase) >= 0) userIdentity.AmbienteDatabase = "Produção";
            else userIdentity.AmbienteDatabase = "Desconhecido";

            CachePersister.dataBase = currentTenant.database;

            var contexto = new Contexto();
            var contextoModel = new ContextoModel { allNavbarItemMenu = contexto.getNavbarItemsMenu().ToList() };
            CachePersister.contextoModel = contextoModel;

            CachePersister.userIdentity = userIdentity;
            CachePersister.userIdentity.id_coligada = recordCliente.id_coligada;
            CachePersister.userIdentity.NomeColigada = dbCtx.g_filiais.Find(recordCliente.id_filial).nome.ToString();
            CachePersister.userIdentity.id_filial = recordCliente.id_filial;
            CachePersister.userIdentity.FilialNome = dbCtx.g_filiais.Find(recordCliente.id_filial).nome.ToString();

            string token = CachePersister.userIdentity.TokenAcesso.EmptyIfNull().ToString().Trim();
            dbCtx.g_filtros.RemoveRange(dbCtx.g_filtros.Where(f => f.token == token).ToList());
            dbCtx.SaveChanges();

            var regrasAcesso = new List<string> { "gc_PortalCliente_PortalFinanceiro" };
            CachePersister.userIdentity.Roles = regrasAcesso.ToArray();
            contextoModel.allNavbarItemMenu.Clear();
            const int portalMenuGrupoId = -910001;
            const int portalMenuPedidosId = -910002;
            contextoModel.allNavbarItemMenu.Add(new NavbarItemMenu
            {
                Id = portalMenuGrupoId,
                level = 1,
                Ordem = 0,
                nameOption = "Portal do Cliente",
                imageClass = "fa-solid fa-building-user",
                status = true,
                isParent = true,
                parentId = 0
            });
            contextoModel.allNavbarItemMenu.Add(new NavbarItemMenu
            {
                Id = portalMenuPedidosId,
                level = 2,
                Ordem = 1,
                nameOption = "Pedidos",
                controller = "Pedidos",
                action = "Index",
                area = "crm",
                imageClass = "",
                status = true,
                isParent = false,
                parentId = portalMenuGrupoId
            });

            return RedirectToAction("Index", "Pedidos", new { area = "crm" });
        }

        /// <summary>Hosts onde o login é exclusivo do portal do cliente (não o login staff).</summary>
        private static bool IsPortalClienteHost(string hostWithoutPort)
        {
            if (string.IsNullOrWhiteSpace(hostWithoutPort)) return false;
            var h = hostWithoutPort.Trim().ToLowerInvariant();
            if (h.StartsWith("www.", StringComparison.Ordinal)) h = h.Substring(4);
            if (h.EndsWith("portalflightx.com", StringComparison.Ordinal)) return true;
            if (h.IndexOf("portalflightx", StringComparison.Ordinal) >= 0
                && (string.Equals(h, "portalflightx", StringComparison.Ordinal)
                    || h.EndsWith(".local", StringComparison.Ordinal)))
                return true;
            return false;
        }

        private static string GetHostWithoutPort(HttpRequestBase request)
        {
            string host = request.Headers["Host"].EmptyIfNull().ToString().ToLowerInvariant()
                .Replace("http://", "").Replace("https://", "").Replace("www.", "").Trim();
            int c = host.IndexOf(":");
            if (c >= 0) host = host.Substring(0, c);
            return host.Trim();
        }

        private static string GetSubDominio(string hostWithoutPort)
        {
            if (string.IsNullOrEmpty(hostWithoutPort)) return string.Empty;
            int index = hostWithoutPort.IndexOf(".");
            if (index < 0) return hostWithoutPort.Trim();
            return hostWithoutPort.Substring(0, index);
        }

        /// <summary>GET público para links em e-mail: entrada no portal integrado neste ERP (AcessoPortal).</summary>
        [HttpGet]
        public ActionResult AcessoPortal(string codigocliente, string documentocliente)
        {
            try
            {
                CachePersister.logout();
                HttpContext.Session["TokenId"] = HttpContext.Session.SessionID.ToString();

                string host = GetHostWithoutPort(Request);
                string dominio = Request.Headers["Host"].EmptyIfNull().ToString().ToLowerInvariant().Trim();
                string subDominio = GetSubDominio(host);
                ViewBag.PortalClienteLogin = true;

                if (string.IsNullOrEmpty(codigocliente) || string.IsNullOrEmpty(documentocliente))
                {
                    ViewBag.Error = "Conta inválida!";
                    return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                }

                var allTenants = SetTenants();
                CstTenant currentTenant = allTenants.FirstOrDefault(t => t.subDominio == subDominio);
                if (currentTenant == null)
                {
                    ViewBag.Error = "Domínio inválido [" + subDominio + "]!";
                    return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                }

                LibDB.CheckConnectionDB(currentTenant.database);
                db = new GdiPlataformEntities(currentTenant.database);

                string clienteIdentificador = LibStringFormat.SomenteNumeros(codigocliente.EmptyIfNull().ToString()).Trim();
                string clienteCpfCnpj = LibStringFormat.SomenteNumeros(documentocliente.EmptyIfNull().ToString()).Trim();

                if (clienteCpfCnpj.Length != 11 && clienteCpfCnpj.Length != 14)
                {
                    ViewBag.Error = "CPF/CNPJ Inválido!";
                    return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                }
                if (clienteIdentificador.Length == 0)
                {
                    ViewBag.Error = "Código do Cliente Inválido!";
                    return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                }

                int clienteIdentificadorInt = int.Parse(clienteIdentificador);
                g_clientes recordCliente = db.g_clientes.Find(clienteIdentificadorInt);
                if (recordCliente == null)
                {
                    ViewBag.Error = "Cliente não localizado!";
                    return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                }
                if (recordCliente.ativo == false)
                {
                    ViewBag.Error = "Cliente Inativo!";
                    return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                }
                if (recordCliente.cpf != clienteCpfCnpj && recordCliente.cnpj != clienteCpfCnpj)
                {
                    ViewBag.Error = "CPF/CNPJ Inválido!";
                    return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                }

                string dataLimiteSql = Convert.ToDateTime(DateTime.Now.AddYears(-1), new CultureInfo("en-US")).ToString("yyyy-MM-dd 00:00:00");
                string sqlPedidos = " select mov.* from gc_movimentos mov where id_cliente = " + recordCliente.id_cliente.ToString()
                    + " and mov.id_movimento_tipo in (3,4,8) "
                    + " and mov.id_movimento_status = 2 and mov.id_movimento_posicao >= 4 "
                    + " and mov.datahora_aprovacao > '" + dataLimiteSql + "' "
                    + " order by mov.datahora_aprovacao desc ";
                if (db.gc_movimentos.SqlQuery(sqlPedidos).ToList().Count == 0)
                {
                    ViewBag.Error = "Não há pedidos para o cliente!";
                    return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
                }

                return CompletePortalClienteLogin(db, recordCliente, currentTenant, dominio, subDominio);
            }
            catch (Exception ex)
            {
                CachePersister.logout();
                string msgErro = LibExceptions.getExceptionShortMessage(ex);
                msgErro = msgErro.Replace("The underlying provider failed on Open", "Falha ao conectar com o Banco de Dados");
                ViewBag.Error = "Erro [" + msgErro + "]";
                ViewBag.PortalClienteLogin = true;
                return View("Index", new IdentityViewModel { userIdentity = new UserIdentity() });
            }
        }
    }
}

