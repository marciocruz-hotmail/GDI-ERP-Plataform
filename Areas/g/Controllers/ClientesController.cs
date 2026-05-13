using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using GdiPlataform.Controllers;
using System.Net;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Robos.SintegraWS;
using GdiPlataform.Robos.SintegraWS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using GdiPlataform.Robos.CpfCnpj;
using GdiPlataform.Models;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Default")]
    public class ClientesController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Clientes";

        public ClientesController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Clientes/Fornecedores";
            if (CachePersister.userIdentity.IdPerfil == -800) { ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Clientes (Acesso Vendedor)"; };
            return View();
        }

        #region PreencherLookupsCreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actioncreate,g_Clientes_Actionupdate")]
        public void PreencherLookupsCreateEdit()
        {
            var comboCidade = new List<SelectListItem>();
            try
            {
                IQueryable<g_cidades> listaDbCidade = db.g_cidades.Where(p => p.ativo == true).OrderBy(p => p.nome);
                foreach (g_cidades item_g_cidades in listaDbCidade)
                {
                    comboCidade.Add(new SelectListItem { Value = item_g_cidades.id_cidade.ToString(), Text = item_g_cidades.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboCidade = comboCidade;

            var comboUF = new List<SelectListItem>();
            try
            {
                IQueryable<g_uf> listaDbUF = db.g_uf.Select(p => p).OrderBy(p => p.sigla);
                foreach (g_uf item2 in listaDbUF)
                {
                    comboUF.Add(new SelectListItem { Value = item2.id_uf.ToString(), Text = item2.sigla.ToString() });
                }
            }
            finally { }
            ViewBag.comboUF = comboUF;

            var comboPais = new List<SelectListItem>();
            try
            {
                IQueryable<g_pais> listaDbPais = db.g_pais.Select(p => p).OrderBy(p => p.nome);
                foreach (g_pais item3 in listaDbPais)
                {
                    comboPais.Add(new SelectListItem { Value = item3.id_pais.ToString(), Text = item3.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboPais = comboPais;

            var comboVendedor = new List<SelectListItem>();
            try
            {
                IQueryable<g_vendedores> listaDbVendedor = null;
                if (CachePersister.userIdentity.IdPerfil == 1) { listaDbVendedor = db.g_vendedores.Select(p => p).OrderBy(p => p.nome); }                
                else { listaDbVendedor = db.g_vendedores.Where(v => v.ativo == true); };

                comboVendedor.Add(new SelectListItem { Value = "0", Text = "Selecione" });
                foreach (g_vendedores item4 in listaDbVendedor)
                {
                    comboVendedor.Add(new SelectListItem { Value = item4.id_vendedor.ToString(), Text = item4.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboVendedor = comboVendedor;

            var comboContaCaixa = new List<SelectListItem>();
            try
            {
                IQueryable<g_contas_caixas> listaDbContaCaixa = null;
                if (CachePersister.userIdentity.IdPerfil == 1) { listaDbContaCaixa = db.g_contas_caixas.Select(p => p).OrderBy(p => p.nome); }
                else { listaDbContaCaixa = db.g_contas_caixas.Where(c => c.ativo == true).OrderBy(p => p.nome); };
                foreach (g_contas_caixas item7 in listaDbContaCaixa)
                {
                    comboContaCaixa.Add(new SelectListItem { Value = item7.id_conta_caixa.ToString(), Text = item7.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboContaCaixa = comboContaCaixa;

            var comboPagRecCondicoes = new List<SelectListItem>();
            try
            {
                IQueryable<g_pagrec_condicoes> listaPagRecCondicoes = null;
                listaPagRecCondicoes = db.g_pagrec_condicoes.Select(p => p).OrderBy(p => p.descricao);
                foreach (g_pagrec_condicoes item11 in listaPagRecCondicoes)
                {
                    comboPagRecCondicoes.Add(new SelectListItem { Value = item11.id_pagrec_condicao.ToString(), Text = item11.descricao.ToString() });
                }
            }
            finally { }
            ViewBag.comboPagRecCondicoes = comboPagRecCondicoes;

            var comboFreteResponsavel = new List<SelectListItem>();
            try
            {
                IQueryable<gc_frete_responsavel> listaFreteResponsavel = null;
                listaFreteResponsavel = db.gc_frete_responsavel.Select(p => p).Where(p => p.ativo == true).OrderBy(p => p.descricao);
                foreach (gc_frete_responsavel item12 in listaFreteResponsavel)
                {
                    comboFreteResponsavel.Add(new SelectListItem { Value = item12.id_frete_responsavel.ToString(), Text = item12.descricao.ToString() });
                }
            }
            finally { }
            ViewBag.comboFreteResponsavel = comboFreteResponsavel;

            var comboCFOP = new List<SelectListItem>();
            try
            {
                IQueryable<gc_cfop> listaCFOP = null;
                listaCFOP = db.gc_cfop.Select(p => p).OrderBy(p => p.descricao);
                if (listaCFOP.Count() > 0)
                {
                    foreach (gc_cfop item13 in listaCFOP)
                    {
                        String descricaoCFOP = item13.numero.ToString() + "  -  " + item13.descricao.ToString();
                        comboCFOP.Add(new SelectListItem { Value = item13.id_cfop.ToString(), Text = descricaoCFOP });
                    }
                }

                
            }
            finally { }
            ViewBag.comboCFOP = comboCFOP;

            var comboImpostosAcao = new List<SelectListItem>();
            comboImpostosAcao.Add(new SelectListItem { Value = " ", Text = " " });
            comboImpostosAcao.Add(new SelectListItem { Value = "D", Text = "D" });
            comboImpostosAcao.Add(new SelectListItem { Value = "+", Text = "+" });
            comboImpostosAcao.Add(new SelectListItem { Value = "-", Text = "-" });
            ViewBag.comboImpostosAcao = comboImpostosAcao;

            var comboNFTipo = new List<SelectListItem>();
            comboNFTipo.Add(new SelectListItem { Value = "S", Text = "S" });
            comboNFTipo.Add(new SelectListItem { Value = "N", Text = "N" });
            ViewBag.comboNFTipo = comboNFTipo;

            var comboIndicadorIE = new List<SelectListItem>();
            comboIndicadorIE.Add(new SelectListItem { Value = "1", Text = "Isento" });
            comboIndicadorIE.Add(new SelectListItem { Value = "2", Text = "Contribuinte" });
            comboIndicadorIE.Add(new SelectListItem { Value = "3", Text = "Não Contribuinte" });
            ViewBag.comboIndicadorIE = comboIndicadorIE;

            ViewBag.ComboGcClientesContatosTipos = LibDataSets.LoadComboGcClientesContatosTipos(db);
        }
        #endregion

        #region ModalFiltroAvancadoView
        public ActionResult ModalFiltroAvancadoView(String id)
        {
            ViewBag.Title = "Clientes - Filtro Avançado";
            return View();
        }
        #endregion

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            bool filterDb = false;
            bool filterAdvanced = false;
            string errorMessage = "";
            string filterOnOff = "0";

            if (param == null)
            {
                param = new jQueryDataTableParamModel();
            }

            if (db == null)
            {
                return Json(new
                {
                    errorMessage = "Conexão com o banco de dados não inicializada.",
                    severity = "error",
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = 0,
                    iTotalDisplayRecords = 0,
                    aaData = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var recordFiltro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
                if (!string.IsNullOrWhiteSpace(recordFiltro.sql_filtro)) filterDb = true;
                else if (!string.IsNullOrWhiteSpace(param.yesFilterAdvancedText)) filterAdvanced = true;

                // ---------- Filtro complementar por perfil ----------
                int? idVendedor = null;
                int? idColigada = null;
                int? idFilial = null;

                if (CachePersister.userIdentity.IdPerfil == 1)
                {
                    // Admin vê tudo
                }
                else if (CachePersister.userIdentity.IdPerfil == -800)
                {
                    idVendedor = CachePersister.userIdentity.IdVendedor;
                    idColigada = 1;
                    idFilial = 1;
                }
                else
                {
                    idColigada = 1;
                    idFilial = 1;
                }

                int start = param.iDisplayStart;
                int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

                // ---------- Base WHERE (perfil + filtros) ----------
                var whereParts = new List<string> { "c.id_cliente > 0" };
                var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                // Perfil
                if (idVendedor.HasValue)
                {
                    whereParts.Add("c.id_vendedor = @idVendedor");
                    args["@idVendedor"] = idVendedor.Value;
                }
                if (idColigada.HasValue)
                {
                    whereParts.Add("c.id_coligada = @idColigada");
                    args["@idColigada"] = idColigada.Value;
                }
                if (idFilial.HasValue)
                {
                    whereParts.Add("c.id_filial = @idFilial");
                    args["@idFilial"] = idFilial.Value;
                }

                // ---------- Filtro persistido ----------
                if (filterDb)
                {
                    string frag = (recordFiltro.sql_filtro ?? "").Trim();

                    string fragLower = frag.ToLowerInvariant();
                    if (fragLower.Contains(";") || fragLower.Contains("--") || fragLower.Contains("/*") || fragLower.Contains("*/") ||
                        fragLower.Contains(" drop ") || fragLower.Contains(" delete ") || fragLower.Contains(" insert ") ||
                        fragLower.Contains(" update ") || fragLower.Contains(" union ") || fragLower.Contains(" exec ") ||
                        fragLower.Contains(" xp_"))
                    {
                        throw new Exception("Filtro persistido inválido.");
                    }

                    // IMPORTANTE: se frag tiver parâmetros com nomes iguais aos já usados (@idFilial etc),
                    // vai duplicar nomes no comando. Ideal é que frag não use parâmetros, só literais/valores.
                    whereParts.Add("(" + frag + ")");
                }
                else if (filterAdvanced)
                {
                    var campos = (param.yesFilterAdvancedText ?? "").Split(';');
                    if (campos.Length == 5)
                    {
                        string id = campos[0]?.Trim();
                        string nome = campos[1]?.Trim();
                        string razao = campos[2]?.Trim();
                        string cpf = campos[3]?.Trim();
                        string cnpj = campos[4]?.Trim();

                        if (!string.IsNullOrWhiteSpace(id) && id != "0" && int.TryParse(id, out int idCli))
                        {
                            whereParts.Add("c.id_cliente = @idCliente");
                            args["@idCliente"] = idCli;
                        }
                        if (!string.IsNullOrWhiteSpace(nome))
                        {
                            whereParts.Add("c.nome LIKE @nome");
                            args["@nome"] = "%" + nome + "%";
                        }
                        if (!string.IsNullOrWhiteSpace(razao))
                        {
                            whereParts.Add("c.razao_social LIKE @razao");
                            args["@razao"] = "%" + razao + "%";
                        }
                        if (!string.IsNullOrWhiteSpace(cpf))
                        {
                            whereParts.Add("c.cpf = @cpf");
                            args["@cpf"] = cpf;
                        }
                        if (!string.IsNullOrWhiteSpace(cnpj))
                        {
                            whereParts.Add("c.cnpj = @cnpj");
                            args["@cnpj"] = cnpj;
                        }
                    }
                }

                string whereSql = string.Join(" AND ", whereParts);

                // ---------- WHERE base (somente perfil) ----------
                var wherePartsBase = new List<string> { "c.id_cliente > 0" };
                var argsBase = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                if (idVendedor.HasValue)
                {
                    wherePartsBase.Add("c.id_vendedor = @idVendedor");
                    argsBase["@idVendedor"] = idVendedor.Value;
                }
                if (idColigada.HasValue)
                {
                    wherePartsBase.Add("c.id_coligada = @idColigada");
                    argsBase["@idColigada"] = idColigada.Value;
                }
                if (idFilial.HasValue)
                {
                    wherePartsBase.Add("c.id_filial = @idFilial");
                    argsBase["@idFilial"] = idFilial.Value;
                }

                string whereSqlBase = string.Join(" AND ", wherePartsBase);

                // Helper: cria parâmetros NOVOS (não reutiliza instâncias)
                System.Data.SqlClient.SqlParameter[] BuildParams(Dictionary<string, object> dict)
                    => dict.Select(kv => new System.Data.SqlClient.SqlParameter(kv.Key, kv.Value ?? DBNull.Value)).ToArray();

                // ---------- TOTALS ----------
                string sqlCountBase = $"SELECT COUNT(1) FROM g_clientes c WHERE {whereSqlBase}";
                int totalRecords = db.Database.SqlQuery<int>(sqlCountBase, BuildParams(argsBase)).First();

                string sqlCountFiltered = $"SELECT COUNT(1) FROM g_clientes c WHERE {whereSql}";
                int totalDisplayRecords = db.Database.SqlQuery<int>(sqlCountFiltered, BuildParams(args)).First();

                // ---------- PAGE DATA ----------
                string sqlPage = $@"
            SELECT
                c.id_cliente,
                c.nome,
                c.razao_social,
                c.gc_produtor_rural,
                c.gc_consultor_aviacao,
                c.param_gc_transportadora,
                c.cnpj,
                c.cpf,
                c.ativo,
                c.is_cliente,
                c.is_fornecedor,
                c.datahora_cadastro,
                v.apelido AS vendedor_apelido
            FROM g_clientes c
            LEFT JOIN g_vendedores v ON v.id_vendedor = c.id_vendedor
            WHERE {whereSql}
            ORDER BY c.id_cliente DESC
            OFFSET @start ROWS FETCH NEXT @length ROWS ONLY;
        ";

                // parâmetros do page = args + start/length (novas instâncias)
                var argsPage = new Dictionary<string, object>(args, StringComparer.OrdinalIgnoreCase)
                {
                    ["@start"] = start,
                    ["@length"] = length
                };

                var page = db.Database.SqlQuery<ClienteGridRow>(sqlPage, BuildParams(argsPage)).ToList();

                // ---------- Monta aaData ----------
                var list = new List<string[]>(page.Count);

                foreach (var r in page)
                {
                    string clienteNome = (r.nome ?? "").Trim().ToUpperInvariant();
                    string razao = (r.razao_social ?? "").Trim().ToUpperInvariant();
                    if (!string.IsNullOrWhiteSpace(razao) && razao != clienteNome)
                        clienteNome += "<br/>" + razao;

                    if (r.gc_produtor_rural) clienteNome += "   (Produtor Rural)";
                    else if (r.gc_consultor_aviacao) clienteNome += "   (Consultor Aviação)";
                    else if (r.param_gc_transportadora) clienteNome += "   (Transportadora)";

                    string doc = "";
                    string cnpj = (r.cnpj ?? "").Trim();
                    string cpf = (r.cpf ?? "").Trim();
                    if (cnpj.Length == 14) doc = LibStringFormat.FormatarCPFCNPJ("J", cnpj);
                    else if (cpf.Length == 11) doc = LibStringFormat.FormatarCPFCNPJ("F", cpf);

                    string icone = "";
                    if (r.ativo)
                    {
                        if (r.is_cliente) icone += LibIcons.getIcon("fa-solid fa-address-book", "Cliente", "green", "");
                        if (r.is_fornecedor) icone += LibIcons.getIcon("fa-solid fa-screwdriver-wrench", "Fornecedor", "orange", "");
                    }
                    else
                    {
                        if (r.is_cliente) icone += LibIcons.getIcon("fa-solid fa-address-book", "Cliente (Inativo)", "grey", "");
                        if (r.is_fornecedor) icone += LibIcons.getIcon("fa-solid fa-screwdriver-wrench", "Fornecedor (Inativo)", "grey", "");
                    }

                    list.Add(new[]
                    {
                "",
                r.id_cliente.ToString(),
                icone,
                clienteNome,
                (r.vendedor_apelido ?? "").Trim(),
                doc,
                r.datahora_cadastro.ToString("dd/MM/yyyy")
            });
                }

                if (filterDb || filterAdvanced) filterOnOff = "1";

                return Json(new
                {
                    errorMessage = errorMessage,
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
        #endregion

        #region GetDadosContatos
        public ActionResult GetDadosContatos(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            const string filterOnOff = "0";
            try
            {
            int idCliente = -1;
            int.TryParse(param.yesCustomIdPK, out idCliente);

            // Base query
            var baseQuery = db.g_clientes_contatos
                .AsNoTracking()
                .Where(c => c.id_contato > 0 && c.id_cliente == idCliente);

            // Totais (DataTables)
            int totalRecords = baseQuery.Count();
            int totalDisplayRecords = totalRecords; // aqui não há filtro adicional

            int start = param.iDisplayStart;
            int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

            // Página com joins (tipo + usuário cadastro)
            var page = (
                from c in baseQuery
                join t in db.g_clientes_contatos_tipos.AsNoTracking().Where(x => x.ativo)
                    on c.id_contato_tipo equals t.id_contato_tipo into tipos
                from t in tipos.DefaultIfEmpty()

                join u in db.g_usuarios.AsNoTracking().Where(x => x.ativo)
                    on c.id_usuario_cadastro equals u.id_usuario into usuarios
                from u in usuarios.DefaultIfEmpty()

                orderby c.ativo descending, c.datahora_cadastro descending
                select new
                {
                    c.id_contato,
                    c.ativo,
                    TipoContato = t.nome,
                    c.contato,
                    c.setor,
                    c.telefone,
                    c.email,
                    c.datahora_cadastro,
                    UsuarioCadastro = u.login
                }
            )
            .Skip(start)
            .Take(length)
            .ToList();

            // Monta aaData
            var list = new List<string[]>(page.Count);

            foreach (var r in page)
            {
                string iconAtivo = r.ativo
                    ? LibIcons.getIcon("fa-solid fa-toggle-on", "Contato Ativo", "green", "fa-lg")
                    : LibIcons.getIcon("fa-solid fa-toggle-off", "Contato Inativo", "red", "fa-lg");

                list.Add(new[]
                {
            r.id_contato.ToString(),
            iconAtivo,
            (r.TipoContato ?? ""),
            (r.contato ?? ""),
            (r.setor ?? ""),
            (r.telefone ?? ""),
            (r.email ?? ""),
            r.datahora_cadastro.ToString("dd/MM/yyyy"),
            (r.UsuarioCadastro ?? ""),
            "", // Botão Editar
            "", // Botão Remover
        });
            }

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
        #endregion

        #region Aba - Destinatários
        public ActionResult GetDadosDestinatarios(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            const string filterOnOff = "0";
            try
            {
            int idCliente = -1;
            int.TryParse(param.yesCustomIdPK, out idCliente);

            // Se não vier cliente válido, retorna vazio sem bater pesado no banco
            if (idCliente <= 0)
            {
                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = 0,
                    iTotalDisplayRecords = 0,
                    aaData = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }

            var baseQuery = db.g_clientes_destinatarios
                .AsNoTracking()
                .Where(d => d.id_cliente == idCliente && d.ativo == true);

            int totalRecords = baseQuery.Count();
            int totalDisplayRecords = totalRecords; // aqui não há filtro adicional

            int start = param.iDisplayStart;
            int length = (param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);

            // Pagina + traz cidade/UF via join (evita carregar todas cidades/ufs)
            var page = (
                from d in baseQuery
                join c in db.g_cidades.AsNoTracking() on d.id_cidade_com equals c.id_cidade into cidades
                from c in cidades.DefaultIfEmpty()
                join u in db.g_uf.AsNoTracking() on d.id_uf_com equals u.id_uf into ufs
                from u in ufs.DefaultIfEmpty()
                orderby d.id_cliente_destinatario descending
                select new
                {
                    d.id_cliente_destinatario,
                    d.nome,
                    d.cpf,
                    d.cnpj,
                    CidadeNome = c.nome,
                    UfNome = u.nome
                }
            )
            .Skip(start)
            .Take(length)
            .ToList();

            var list = new List<string[]>(page.Count);

            foreach (var r in page)
            {
                string documento = ((r.cpf ?? "").Trim() + (r.cnpj ?? "").Trim());

                if (documento.Length == 11) documento = LibStringFormat.FormatarCPFCNPJ("F", documento);
                else if (documento.Length == 14) documento = LibStringFormat.FormatarCPFCNPJ("J", documento);

                string cidade = $"{(r.CidadeNome ?? "")} / {(r.UfNome ?? "")}";

                list.Add(new[]
                {
            r.id_cliente_destinatario.ToString(),
            (r.nome ?? ""),
            documento,
            cidade,
            "" // Botão Editar
        });
            }

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

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actioncreate,g_Clientes_Actionupdate")]
        public void PreencherLookupsCreateEditDestinatarios()
        {
            var comboCidade = new List<SelectListItem>();
            try
            {
                IQueryable<g_cidades> listaDbCidade = db.g_cidades.Where(p => p.ativo == true).OrderBy(p => p.nome);
                foreach (g_cidades item_g_cidades in listaDbCidade)
                {
                    comboCidade.Add(new SelectListItem { Value = item_g_cidades.id_cidade.ToString(), Text = item_g_cidades.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboCidade = comboCidade;

            var comboUF = new List<SelectListItem>();
            try
            {
                IQueryable<g_uf> listaDbUF = db.g_uf.Select(p => p).OrderBy(p => p.sigla);
                foreach (g_uf item2 in listaDbUF)
                {
                    comboUF.Add(new SelectListItem { Value = item2.id_uf.ToString(), Text = item2.sigla.ToString() });
                }
            }
            finally { }
            ViewBag.comboUF = comboUF;

            var comboIndicadorIE = new List<SelectListItem>();
            comboIndicadorIE.Add(new SelectListItem { Value = "1", Text = "Isento" });
            comboIndicadorIE.Add(new SelectListItem { Value = "2", Text = "Contribuinte" });
            comboIndicadorIE.Add(new SelectListItem { Value = "3", Text = "Não Contribuinte" });
            ViewBag.comboIndicadorIE = comboIndicadorIE;
        }

        public ActionResult ModalCadastrarDestinatario(int? idCliente)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cadastrar Novo Destinatário</b>";
            ViewBag.idCliente = idCliente;
            g_clientes_destinatarios record_g_clientes_destinatarios = new g_clientes_destinatarios();
            record_g_clientes_destinatarios.id_cliente = idCliente.GetValueOrDefault();
            record_g_clientes_destinatarios.ativo = true;
            record_g_clientes_destinatarios.id_cidade_com = 0;
            record_g_clientes_destinatarios.id_uf_com = 0;
            PreencherLookupsCreateEditDestinatarios();
            return View("ModalCreateEditDestinatario", record_g_clientes_destinatarios);
        }

        public ActionResult ModalEditarDestinatario(int? idDestinatario)
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Alterar Dados Destinatário</b>";
            g_clientes_destinatarios record_g_clientes_destinatarios = db.g_clientes_destinatarios.Find(idDestinatario.GetValueOrDefault());
            CachePersister.userIdentity.DataRowAuxInUseSerialized = JsonConvert.SerializeObject(record_g_clientes_destinatarios);
            PreencherLookupsCreateEditDestinatarios();
            return View("ModalCreateEditDestinatario", record_g_clientes_destinatarios);
        }

        public ActionResult ModalValidarPefinOnLine(int? id)
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Validar Pefin OnLine</b>";
            g_clientes record_g_clientes_destinatarios = new Db.g_clientes();
            return View("ModalValidarPefinOnLine", record_g_clientes_destinatarios);
        }

        [HttpPost]
        public ActionResult AjaxCreateEditDestinatario(g_clientes_destinatarios view_record_g_clientes_destinatarios)
        {
            bool cadastrado = false;
            int QtdErros = 0;
            String msgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (view_record_g_clientes_destinatarios.nome.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    msgRetorno += "Campo <b>Nome</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                if ((view_record_g_clientes_destinatarios.cpf.EmptyIfNull().ToString().Trim().Length == 0) && (view_record_g_clientes_destinatarios.cnpj.EmptyIfNull().ToString().Trim().Length == 0))
                {
                    msgRetorno += "Campo <b>CPF</b> ou <b>CNPJ</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                if ((view_record_g_clientes_destinatarios.cpf.EmptyIfNull().ToString().Trim().Length > 0) && (view_record_g_clientes_destinatarios.cnpj.EmptyIfNull().ToString().Trim().Length > 0))
                {
                    msgRetorno += "Campo <b>CPF</b> e <b>CNPJ</b> não podem ser preenchidos simultaneamente!<br/>";
                    QtdErros += 1;
                }
                if (view_record_g_clientes_destinatarios.cpf.EmptyIfNull().ToString().Trim().Length > 0)
                {
                    view_record_g_clientes_destinatarios.cpf = view_record_g_clientes_destinatarios.cpf.Replace(".", "").Replace("-", "");
                    if (!(LibStringValidate.ValidarCPF(view_record_g_clientes_destinatarios.cpf)))
                    { 
                        msgRetorno += "Campo <b>CPF</b> contém um CPF inválido!<br/>";
                        QtdErros += 1;
                    }
                }
                if (view_record_g_clientes_destinatarios.cnpj.EmptyIfNull().ToString().Trim().Length > 0)
                {
                    view_record_g_clientes_destinatarios.cnpj = view_record_g_clientes_destinatarios.cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
                    if (!(LibStringValidate.ValidarCNPJ(view_record_g_clientes_destinatarios.cnpj)))
                    { 
                        msgRetorno += "Campo <b>CNPJ</b> contém um CNPJ inválido!<br/>";
                        QtdErros += 1;
                    }
                }
                if (view_record_g_clientes_destinatarios.endereco_com.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    msgRetorno += "Campo <b>Endereço</b> é de preenchimento obrigatório!!<br/>";
                    QtdErros += 1;
                }
                if (view_record_g_clientes_destinatarios.endereco_com_numero.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    msgRetorno += "Campo <b>Número do Endereço</b> é de preenchimento obrigatório!!<br/>";
                    QtdErros += 1;
                }
                if (view_record_g_clientes_destinatarios.bairro_com.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    msgRetorno += "Campo <b>Bairro</b> é de preenchimento obrigatório!!<br/>";
                    QtdErros += 1;
                }
                if (view_record_g_clientes_destinatarios.id_cidade_com == 0)
                {
                    msgRetorno += "Campo <b>Cidade</b> é de preenchimento obrigatório!!<br/>";
                    QtdErros += 1;
                }
                if (view_record_g_clientes_destinatarios.cep_com.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    msgRetorno += "Campo <b>CEP</b> é de preenchimento obrigatório!!<br/>";
                    QtdErros += 1;
                }
                if (view_record_g_clientes_destinatarios.id_uf_com == 0)
                {
                    msgRetorno += "Campo <b>UF</b> é de preenchimento obrigatório!!<br/>";
                    QtdErros += 1;
                }
                if (QtdErros == 0)
                {
                    view_record_g_clientes_destinatarios.nome = LibStringFormat.FormatarTextoSimples(view_record_g_clientes_destinatarios.nome);
                    view_record_g_clientes_destinatarios.razao_social = LibStringFormat.FormatarTextoSimples(view_record_g_clientes_destinatarios.razao_social);
                    view_record_g_clientes_destinatarios.endereco_com = LibStringFormat.FormatarTextoSimples(view_record_g_clientes_destinatarios.endereco_com);
                    view_record_g_clientes_destinatarios.endereco_com_numero = LibStringFormat.FormatarTextoSimples(view_record_g_clientes_destinatarios.endereco_com_numero);
                    view_record_g_clientes_destinatarios.endereco_com_complemento = LibStringFormat.FormatarTextoSimples(view_record_g_clientes_destinatarios.endereco_com_complemento);
                    view_record_g_clientes_destinatarios.bairro_com = LibStringFormat.FormatarTextoSimples(view_record_g_clientes_destinatarios.bairro_com);

                    if (view_record_g_clientes_destinatarios.id_cliente_destinatario == 0) // Novo registro
                    {
                        view_record_g_clientes_destinatarios.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                        view_record_g_clientes_destinatarios.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                        view_record_g_clientes_destinatarios.id_coligada = 1;
                        view_record_g_clientes_destinatarios.id_filial = 1;
                        db.Entry(view_record_g_clientes_destinatarios).State = EntityState.Added;
                        db.SaveChanges();
                        cadastrado = true;
                        msgRetorno = "Destinatário <b>Cadastrado</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");

                        // Audit
                        String LogAlteracao = LibDB.CompareDataTable(new g_clientes_destinatarios(), view_record_g_clientes_destinatarios);
                        if (LogAlteracao.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true, "g_clientes", view_record_g_clientes_destinatarios.id_cliente, "Novo Destinatário | " + LogAlteracao.EmptyIfNull()); };
                    }
                    else // Editar Registro
                    {
                        // Audit
                        g_clientes_destinatarios old_record_g_clientes_destinatarios = new Db.g_clientes_destinatarios();
                        if (view_record_g_clientes_destinatarios.id_cliente_destinatario > 0) { old_record_g_clientes_destinatarios = JsonConvert.DeserializeObject<g_clientes_destinatarios>(CachePersister.userIdentity.DataRowAuxInUseSerialized); };
                        String LogAlteracao = LibDB.CompareDataTable(old_record_g_clientes_destinatarios, view_record_g_clientes_destinatarios);
                        if (LogAlteracao.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, false, "g_clientes", view_record_g_clientes_destinatarios.id_cliente, "Atualização Destinatário | " + LogAlteracao.EmptyIfNull()); };

                        view_record_g_clientes_destinatarios.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                        view_record_g_clientes_destinatarios.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                        view_record_g_clientes_destinatarios.id_coligada = 1;
                        view_record_g_clientes_destinatarios.id_filial = 1;
                        view_record_g_clientes_destinatarios.datahora_alteracao = DataHoraAtual;
                        view_record_g_clientes_destinatarios.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(view_record_g_clientes_destinatarios).State = EntityState.Modified;
                        db.SaveChanges();
                        cadastrado = true;
                        msgRetorno = "Destinatário <b>Atualizado</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                cadastrado = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                cadastrado = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = cadastrado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Create Record
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actioncreate")]
        public ActionResult CreateSintegra()
        {
            CachePersister.userIdentity.NovoClienteSintegra = null;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cliente/Fornecedor</b>";
            g_clientes newRecord = new g_clientes();
            return View("ModalNovoCadastroRoboSintegra", newRecord);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cliente/Fornecedor</b>";
            PreencherLookupsCreateEdit();
            g_clientes newRecord = new g_clientes();
            if (CachePersister.userIdentity.NovoClienteSintegra != null) 
            { 
                newRecord = CachePersister.userIdentity.NovoClienteSintegra; 
            }
            else
            {
                newRecord.id_indicador_ie = 3;
            }
            newRecord.ativo = true;
            newRecord.email_principal = " ";
            newRecord.id_pais = 1;
            newRecord.data_cadastro = LibDateTime.getDataHoraBrasilia();
            newRecord.is_cliente = true;
            newRecord.is_fornecedor = false;
            @ViewBag.PefinStatus = "";
            @ViewBag.MeProtejaStatus = "Em Homologação";
            if (CachePersister.userIdentity.IdPerfil == -800) { newRecord.id_vendedor = CachePersister.userIdentity.IdVendedor; };
            @ViewBag.LabelIdentificador = "CHE";
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actioncreate")]
        public ActionResult Create(g_clientes view_record_g_clientes)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cliente/Fornecedor</b>";
            view_record_g_clientes.id_coligada = 1;
            view_record_g_clientes.id_filial = 1;

            if (view_record_g_clientes.nome.EmptyIfNull().ToString() != String.Empty) { view_record_g_clientes.nome = LibStringFormat.FormatarTextoSimples(view_record_g_clientes.nome); }
            if (view_record_g_clientes.razao_social.EmptyIfNull().ToString() != String.Empty) { view_record_g_clientes.razao_social = LibStringFormat.FormatarTextoSimples(view_record_g_clientes.razao_social); }
            if (view_record_g_clientes.nome_fantasia.EmptyIfNull().ToString() != String.Empty) { view_record_g_clientes.nome_fantasia = LibStringFormat.FormatarTextoSimples(view_record_g_clientes.nome_fantasia); }
            if (view_record_g_clientes.endereco_com.EmptyIfNull().ToString() != String.Empty) { view_record_g_clientes.endereco_com = LibStringFormat.FormatarTextoSimples(view_record_g_clientes.endereco_com); }
            if (view_record_g_clientes.endereco_com_numero.EmptyIfNull().ToString() != String.Empty) { view_record_g_clientes.endereco_com_numero = LibStringFormat.FormatarTextoSimples(view_record_g_clientes.endereco_com_numero); }
            if (view_record_g_clientes.endereco_com_complemento.EmptyIfNull().ToString() != String.Empty) { view_record_g_clientes.endereco_com_complemento = LibStringFormat.FormatarTextoSimples(view_record_g_clientes.endereco_com_complemento); }
            if (view_record_g_clientes.bairro_com.EmptyIfNull().ToString() != String.Empty) { view_record_g_clientes.bairro_com = LibStringFormat.FormatarTextoSimples(view_record_g_clientes.bairro_com); }

            // Validações Customizadas
            if ((view_record_g_clientes.is_cliente == false) && (view_record_g_clientes.is_fornecedor == false))
            {
                { ModelState.AddModelError("Model", "Deverá ser marcado o tipo do cadastro (Cliente/Fornecedor/Ambos)"); }
            }
            if (view_record_g_clientes.cpf.EmptyIfNull().ToString() != String.Empty)
            {
                view_record_g_clientes.cpf = view_record_g_clientes.cpf.Replace(".", "").Replace("-", "");
                if (!(LibStringValidate.ValidarCPF(view_record_g_clientes.cpf)))
                {
                    ModelState.AddModelError("Model", "Campo [CPF] contém um CPF inválido");
                }
            }
            if (view_record_g_clientes.cnpj.EmptyIfNull().ToString() != String.Empty)
            {
                view_record_g_clientes.cnpj = view_record_g_clientes.cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
                if (!(LibStringValidate.ValidarCNPJ(view_record_g_clientes.cnpj)))
                {
                    ModelState.AddModelError("Model", "Campo [CNPJ] contém um CNPJ inválido");
                }
            }
            if ((view_record_g_clientes.faturamento_boleto == true) && (view_record_g_clientes.faturamento_debito_conta == true))
            {
                { ModelState.AddModelError("Model", "Campo [Emissão Boleto] e [Conta Corrente] não podem ser preenchidos simultaneamente"); }
            }
            if ((view_record_g_clientes.cpf.EmptyIfNull().ToString().Equals(String.Empty)) && (view_record_g_clientes.cnpj.EmptyIfNull().ToString().Equals(String.Empty)))
            {
                { ModelState.AddModelError("Model", "Campo [CPF] ou [CNPJ] é de preenchimento obrigatório"); }
            }
            if ((view_record_g_clientes.cnpj.EmptyIfNull().ToString() != String.Empty) && (view_record_g_clientes.razao_social.EmptyIfNull().ToString().Equals(String.Empty)))
            {
                { ModelState.AddModelError("Model", "Campo [Razão Social] é de preenchimento obrigatório"); }
            }
            if ((view_record_g_clientes.cpf.EmptyIfNull().ToString() != String.Empty) && (view_record_g_clientes.cnpj.EmptyIfNull().ToString() != String.Empty))
            {
                ModelState.AddModelError("Model", "Campos [CPF] e [CNPJ] não podem ser preenchidos simultaneamente");
            }
            if ((view_record_g_clientes.inscricao_estadual.EmptyIfNull().ToString().Length == 0) && (view_record_g_clientes.id_indicador_ie != 3))
            {
                ModelState.AddModelError("Model", "Campo [IE] é de preenchimento obrigatório para situação diferente de [Não Contribuinte]");
            }
            if (view_record_g_clientes.is_cliente == true)
            {
                if ((view_record_g_clientes.id_vendedor.EmptyIfNull().ToString().Length == 0) || (view_record_g_clientes.id_vendedor.EmptyIfNull().ToString() == "0"))
                {
                    ModelState.AddModelError("Model", "Campo [Vendedor] é de preenchimento obrigatório!");
                }
            }

            // PREENCHIMENTO OBRIGATÓRIO DE E-MAIL E TELEFONE - TODOS 19/09/2022
            if (view_record_g_clientes.contato_principal.EmptyIfNull().ToString().Length == 0)
            {
                ModelState.AddModelError("Model", "Campo [Contato (P)] é de preenchimento obrigatório");
            }
            if (view_record_g_clientes.contato_principal_setor.EmptyIfNull().ToString().Length == 0)
            {
                ModelState.AddModelError("Model", "Campo [Setor (P)] é de preenchimento obrigatório");
            }
            if (view_record_g_clientes.telefone_principal.EmptyIfNull().ToString().Length == 0)
            {
                ModelState.AddModelError("Model", "Campo [Telefone (P)] é de preenchimento obrigatório");
            }
            else
            {
                String Telefone = LibStringFormat.SomenteNumeros(view_record_g_clientes.telefone_principal.EmptyIfNull().ToString().Trim());
                if ((Telefone.Length != 10) && (Telefone.Length != 11))
                {
                    ModelState.AddModelError("Model", "Campo <b>Telefone</b> deverá conter a seguinte formatação DDNNNNNNNNN onde os 2 primeiros dígitos deverão ser o DDD e os dígitos seguintes o número do telefone ou celular com 8 ou 9 dígitos!");
                }
            }
            if ((view_record_g_clientes.email_principal.EmptyIfNull().ToString().Length > 0) && (LibStringValidate.ValidarEmail(view_record_g_clientes.email_principal.EmptyIfNull().ToString()) == false))
            { 
                ModelState.AddModelError("Model", "Campo <b>Email</b> contém um email inválido!");
            }

            //PREENCHIMENTO OBRIGATORIO ENDEREÇO
            if (view_record_g_clientes.endereco_com.EmptyIfNull().ToString().Equals(String.Empty))
            {
                { ModelState.AddModelError("Model", "Campo [Endereço] na aba endereço é de preenchimento obrigatório"); }
            }
            if (view_record_g_clientes.endereco_com_numero.EmptyIfNull().ToString().Equals(String.Empty))
            {
                { ModelState.AddModelError("Model", "Campo [Número do Endereço] na aba endereço é de preenchimento obrigatório"); }
            }
            if (view_record_g_clientes.bairro_com.EmptyIfNull().ToString().Equals(String.Empty))
            {
                { ModelState.AddModelError("Model", "Campo [Bairro] na aba endereço é de preenchimento obrigatório"); }
            }
            if (view_record_g_clientes.cep_com.EmptyIfNull().ToString().Equals(String.Empty))
            {
                { ModelState.AddModelError("Model", "Campo [Cep] na aba endereço é de preenchimento obrigatório"); }
            }

            if (view_record_g_clientes.faturamento_debito_conta == true)
            {
                if ((view_record_g_clientes.banco.EmptyIfNull().Equals(String.Empty))
                || (view_record_g_clientes.agencia.EmptyIfNull().Equals(String.Empty))
                || (view_record_g_clientes.dv_agencia.EmptyIfNull().Equals(String.Empty))
                || (view_record_g_clientes.conta.EmptyIfNull().Equals(String.Empty))
                || (view_record_g_clientes.dv_conta.EmptyIfNull().Equals(String.Empty)))
                { ModelState.AddModelError("Model", "Para faturamento [Débito em Conta] os campos (Banco, Agência e Conta) deverão ser preenchidos"); }
            }


            if (ModelState.IsValid)
            {
                string documentoCliente = view_record_g_clientes.cpf.EmptyIfNull().ToString() + view_record_g_clientes.cnpj.EmptyIfNull().ToString();
                view_record_g_clientes.senha_portal = documentoCliente.Substring(0, 6);

                List<g_clientes> allClientesBase = null;
                allClientesBase = db.g_clientes.Where(d => (d.id_cliente > 0) && (d.ativo == true)).ToList();


                if (view_record_g_clientes.nome.EmptyIfNull().ToString().Length > 0)
                {
                    g_clientes view_record_g_clientesByNome = allClientesBase.Where(p => (p.nome == view_record_g_clientes.nome)).FirstOrDefault();
                    if (view_record_g_clientesByNome != null) { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [Id. " + view_record_g_clientesByNome.id_cliente.ToString() + "]"); }
                }

                // Validação de CPF duplicado
                if (view_record_g_clientes.cpf.EmptyIfNull().ToString().Length > 0)
                {
                    g_clientes view_record_g_clientesByCPF = allClientesBase.Where(p => (p.cpf == view_record_g_clientes.cpf)).FirstOrDefault();
                    if (view_record_g_clientesByCPF != null)
                    {
                        if (view_record_g_clientes.gc_produtor_rural == true)
                        {
                            if (view_record_g_clientes.inscricao_estadual.EmptyIfNull().ToString().Length == 0)
                            {
                                ModelState.AddModelError("Model", "Campo [CPF] duplicado na base de dados [Id. " + view_record_g_clientesByCPF.id_cliente.ToString() + "], para Produtores Rurais é necessário informar a Inscrição Estadual!");
                            }
                            else
                            {
                                if (view_record_g_clientesByCPF.inscricao_estadual == view_record_g_clientes.inscricao_estadual)
                                {
                                    ModelState.AddModelError("Model", "Campo [CPF] duplicado na base de dados para a mesma IE [Id. " + view_record_g_clientesByCPF.id_cliente.ToString() + "]");
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("Model", "Campo [CPF] duplicado na base de dados [Id. " + view_record_g_clientesByCPF.id_cliente.ToString() + "], verifique se o cliente é Produtor Rural, caso positivo, marque o parâmetro correspondente no cadastro comercial!");
                        }
                    }
                }

                // Validação de CNPJ duplicado
                if (view_record_g_clientes.cnpj.EmptyIfNull().ToString().Length > 0)
                {
                    g_clientes view_record_g_clientesByCNPJ = allClientesBase.Where(p => (p.ativo == true && p.cnpj == view_record_g_clientes.cnpj)).FirstOrDefault();
                    if (view_record_g_clientesByCNPJ != null)
                    {
                        if (view_record_g_clientes.gc_produtor_rural == true)
                        {
                            if (view_record_g_clientes.inscricao_estadual.EmptyIfNull().ToString().Length == 0)
                            {
                                ModelState.AddModelError("Model", "Campo [CNPJ] duplicado na base de dados [Id. " + view_record_g_clientesByCNPJ.id_cliente.ToString() + "], para Produtores Rurais é necessário informar a Inscrição Estadual!");
                            }
                            else
                            {
                                if (view_record_g_clientesByCNPJ.inscricao_estadual == view_record_g_clientes.inscricao_estadual)
                                {
                                    ModelState.AddModelError("Model", "Campo [CNPJ] duplicado na base de dados para a mesma IE [Id. " + view_record_g_clientesByCNPJ.id_cliente.ToString() + "]");
                                }
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("Model", "Campo [CNPJ] duplicado na base de dados [Id. " + view_record_g_clientesByCNPJ.id_cliente.ToString() + "], verifique se o cliente é Produtor Rural, caso positivo, marque o parâmetro correspondente no cadastro comercial!");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                if (view_record_g_clientes.email_principal != null) { view_record_g_clientes.email_principal = view_record_g_clientes.email_principal.ToLowerInvariant().Trim(); };
                view_record_g_clientes.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                view_record_g_clientes.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                view_record_g_clientes.gdc_pefin_status = 0;
                view_record_g_clientes.faturamento_boleto = false;
                view_record_g_clientes.faturamento_debito_conta = false;
                view_record_g_clientes.id_vendedor2 = 2;
                view_record_g_clientes.id_vendedor3 = 6;
                view_record_g_clientes.gc_limite_credito = 15000;

                db.g_clientes.Add(view_record_g_clientes);
                try
                {
                    db.SaveChanges();

                    g_clientes_contatos RecordContato = new g_clientes_contatos();
                    RecordContato.id_cliente = view_record_g_clientes.id_cliente;
                    RecordContato.ativo = true;
                    RecordContato.contato = view_record_g_clientes.contato_principal;
                    RecordContato.setor = view_record_g_clientes.contato_principal_setor;
                    RecordContato.id_contato_tipo = view_record_g_clientes.contato_principal_tipo;
                    RecordContato.telefone = view_record_g_clientes.telefone_principal;
                    RecordContato.email = view_record_g_clientes.email_principal;
                    RecordContato.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                    RecordContato.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                    RecordContato.id_coligada = 1;
                    RecordContato.id_filial = 1;
                    db.Entry(RecordContato).State = EntityState.Added;
                    db.SaveChanges();

                    // Audit Novo Cliente
                    String LogAlteracao = LibDB.CompareDataTable(new g_clientes(), view_record_g_clientes);
                    if (LogAlteracao.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true, "g_clientes", view_record_g_clientes.id_cliente, "Novo Cliente/Fornecedor | " + LogAlteracao.EmptyIfNull()); };

                    // Audit Novo Contato
                    String TipoContato = String.Empty;
                    List<Db.g_clientes_contatos_tipos> AllContatosTipos = db.g_clientes_contatos_tipos.Where(c => c.ativo == true).ToList();
                    g_clientes_contatos_tipos RecordTipoContato = AllContatosTipos.Where(t => t.id_contato_tipo == RecordContato.id_contato_tipo).FirstOrDefault();
                    if (RecordTipoContato != null) { TipoContato = RecordTipoContato.nome.EmptyIfNull().ToString(); };
                    String LogAlteracaoContato = "Novo contato | ";
                    LogAlteracaoContato += "Tipo: " + TipoContato + " | ";
                    LogAlteracaoContato += "Contato: " + RecordContato.contato.EmptyIfNull().ToString().Trim() + " | ";
                    LogAlteracaoContato += "Setor: " + RecordContato.setor.EmptyIfNull().ToString().Trim() + " | ";
                    LogAlteracaoContato += "Telefone: " + RecordContato.telefone.EmptyIfNull().ToString().Trim() + " | ";
                    LogAlteracaoContato += "Email: " + RecordContato.email.EmptyIfNull().ToString().Trim() + " | ";
                    if (LogAlteracaoContato.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true, "g_clientes", RecordContato.id_cliente, LogAlteracaoContato.EmptyIfNull()); };

                    // Módulo GDC - Somente se o módulo estiver ativo
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
            @ViewBag.LabelIdentificador = "CHE";
            return View("CreateEdit", view_record_g_clientes);
        }
        #endregion

        #region Edit Record
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            g_clientes record_g_clientes = db.g_clientes.Find(id);
            CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record_g_clientes);
            if (record_g_clientes == null)
            {
                return RedirectToAction("Index");
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Cliente/Fornecedor</b>" + LibStringFormat.GetTabHtml(1) + record_g_clientes.id_cliente.EmptyIfNull().ToString() + " - " + record_g_clientes.nome.EmptyIfNull().ToString();
            @ViewBag.LabelIdentificador = "CHE";
            CachePersister.userIdentity.RecordGClienteEdicao = LibDB.CloneTObject(record_g_clientes); // Guardar o cliente que será editado
            return View("CreateEdit", record_g_clientes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Clientes_*,g_Clientes_Actionupdate")]
        public ActionResult Edit(g_clientes ViewRecordCliente)
        {
            if (ViewRecordCliente.nome.EmptyIfNull().ToString() != String.Empty) { ViewRecordCliente.nome = LibStringFormat.FormatarTextoSimples(ViewRecordCliente.nome); }
            if (ViewRecordCliente.razao_social.EmptyIfNull().ToString() != String.Empty) { ViewRecordCliente.razao_social = LibStringFormat.FormatarTextoSimples(ViewRecordCliente.razao_social); }
            if (ViewRecordCliente.nome_fantasia.EmptyIfNull().ToString() != String.Empty) { ViewRecordCliente.nome_fantasia = LibStringFormat.FormatarTextoSimples(ViewRecordCliente.nome_fantasia); }
            if (ViewRecordCliente.endereco_com.EmptyIfNull().ToString() != String.Empty) { ViewRecordCliente.endereco_com = LibStringFormat.FormatarTextoSimples(ViewRecordCliente.endereco_com); }
            if (ViewRecordCliente.endereco_com_numero.EmptyIfNull().ToString() != String.Empty) { ViewRecordCliente.endereco_com_numero = LibStringFormat.FormatarTextoSimples(ViewRecordCliente.endereco_com_numero); }
            if (ViewRecordCliente.endereco_com_complemento.EmptyIfNull().ToString() != String.Empty) { ViewRecordCliente.endereco_com_complemento = LibStringFormat.FormatarTextoSimples(ViewRecordCliente.endereco_com_complemento); }
            if (ViewRecordCliente.bairro_com.EmptyIfNull().ToString() != String.Empty) { ViewRecordCliente.bairro_com = LibStringFormat.FormatarTextoSimples(ViewRecordCliente.bairro_com); }

            // Validações Customizadas
            if ((ViewRecordCliente.is_cliente == false) && (ViewRecordCliente.is_fornecedor == false)) { ModelState.AddModelError("Model", "Deverá ser marcado o tipo do cadastro (Cliente/Fornecedor/Ambos)"); }
            if (ViewRecordCliente.cpf.EmptyIfNull().ToString() != String.Empty)
            {
                ViewRecordCliente.cpf = ViewRecordCliente.cpf.Replace(".", "").Replace("-", "");
                if (!(LibStringValidate.ValidarCPF(ViewRecordCliente.cpf)))
                {
                    ModelState.AddModelError("Model", "Campo [CPF] contém um CPF inválido");
                }
            }
            if (ViewRecordCliente.cnpj.EmptyIfNull().ToString() != String.Empty)
            {
                ViewRecordCliente.cnpj = ViewRecordCliente.cnpj.Replace(".", "").Replace("-", "").Replace("/", "");
                if (!(LibStringValidate.ValidarCNPJ(ViewRecordCliente.cnpj)))
                {
                    ModelState.AddModelError("Model", "Campo [CNPJ] contém um CNPJ inválido");
                }
            }
            if ((ViewRecordCliente.faturamento_boleto == true) && (ViewRecordCliente.faturamento_debito_conta == true))
            {
                ModelState.AddModelError("Model", "Campo [Emissão Boleto] e [Débito em Conta] não podem ser preenchidos simultaneamente");
            }
            if ((ViewRecordCliente.cpf.EmptyIfNull().ToString().Equals(String.Empty)) && (ViewRecordCliente.cnpj.EmptyIfNull().ToString().Equals(String.Empty)))
            {
                ModelState.AddModelError("Model", "Campo [CPF] ou [CNPJ] é de preenchimento obrigatório");
            }
            if ((ViewRecordCliente.cnpj.EmptyIfNull().ToString() != String.Empty) && (ViewRecordCliente.razao_social.EmptyIfNull().ToString().Equals(String.Empty)))
            {
                ModelState.AddModelError("Model", "Campo [Razão Social] é de preenchimento obrigatório");
            }
            if ((ViewRecordCliente.cpf.EmptyIfNull().ToString() != String.Empty) && (ViewRecordCliente.cnpj.EmptyIfNull().ToString() != String.Empty))
            {
                ModelState.AddModelError("Model", "Campos [CPF] e [CNPJ] não podem ser preenchidos simultaneamente");
            }
            if ((ViewRecordCliente.inscricao_estadual.EmptyIfNull().ToString().Length == 0) && (ViewRecordCliente.id_indicador_ie != 3))
            {
                ModelState.AddModelError("Model", "Campo [IE] é de preenchimento obrigatório para situação diferente de [Não Contribuinte]");
            }
            if (ViewRecordCliente.is_cliente == true)
            {
                if ((ViewRecordCliente.id_vendedor.EmptyIfNull().ToString().Length == 0) || (ViewRecordCliente.id_vendedor.EmptyIfNull().ToString() == "0"))
                {
                    ModelState.AddModelError("Model", "Campo [Vendedor] é de preenchimento obrigatório!");
                }
            }

            //PREENCHIMENTO OBRIGATORIO ENDEREÇO
            if (ViewRecordCliente.endereco_com.EmptyIfNull().ToString().Equals(String.Empty)) { ModelState.AddModelError("Model", "Campo [Endereço] na aba endereço é de preenchimento obrigatório"); }
            if (ViewRecordCliente.endereco_com_numero.EmptyIfNull().ToString().Equals(String.Empty)) { ModelState.AddModelError("Model", "Campo [Número do Endereço] na aba endereço é de preenchimento obrigatório"); }
            if (ViewRecordCliente.bairro_com.EmptyIfNull().ToString().Equals(String.Empty)) { ModelState.AddModelError("Model", "Campo [Bairro] na aba endereço é de preenchimento obrigatório"); }
            if (ViewRecordCliente.cep_com.EmptyIfNull().ToString().Equals(String.Empty)) { ModelState.AddModelError("Model", "Campo [Cep] na aba endereço é de preenchimento obrigatório"); }

            if (ViewRecordCliente.faturamento_debito_conta == true)
            {
                if ((ViewRecordCliente.banco.EmptyIfNull().Equals(String.Empty))
                || (ViewRecordCliente.agencia.EmptyIfNull().Equals(String.Empty))
                || (ViewRecordCliente.dv_agencia.EmptyIfNull().Equals(String.Empty))
                || (ViewRecordCliente.conta.EmptyIfNull().Equals(String.Empty))
                || (ViewRecordCliente.dv_conta.EmptyIfNull().Equals(String.Empty)))
                { ModelState.AddModelError("Model", "Para faturamento [Débito em Conta] os campos (Banco, Agência e Conta) deverão ser preenchidos"); }
            }

            if (ModelState.IsValid)
            {
                List<g_clientes> allClientesBase = null;
                if (ViewRecordCliente.cnpj.EmptyIfNull().ToString().Length > 0) { allClientesBase = db.g_clientes.Where(d => (d.ativo == true) && (d.id_cliente > 0) && (d.id_cliente != ViewRecordCliente.id_cliente) && (d.cnpj == ViewRecordCliente.cnpj)).ToList(); }
                else if (ViewRecordCliente.cpf.EmptyIfNull().ToString().Length > 0) { allClientesBase = db.g_clientes.Where(d => (d.ativo == true) && (d.id_cliente > 0) && (d.id_cliente != ViewRecordCliente.id_cliente) && (d.cpf == ViewRecordCliente.cpf)).ToList(); }
                foreach (g_clientes validacao in allClientesBase)
                {
                    // Validação de CPF duplicado
                    if ((validacao.cpf != null) && (validacao.cpf != String.Empty) && (ViewRecordCliente.cpf != null) && (ViewRecordCliente.cpf != String.Empty))
                    {
                        if (ViewRecordCliente.gc_produtor_rural == true)
                        {
                            if (ViewRecordCliente.inscricao_estadual.EmptyIfNull().ToString().Length == 0)
                            {
                                ModelState.AddModelError("Model", "Campo [CPF] duplicado na base de dados [Id. " + validacao.id_cliente.ToString() + "], para Produtores Rurais é necessário informar a Inscrição Estadual!");
                            }
                            else
                            {
                                if (validacao.inscricao_estadual == ViewRecordCliente.inscricao_estadual)
                                {
                                    ModelState.AddModelError("Model", "Campo [CPF] duplicado na base de dados para a mesma IE [Id. " + validacao.id_cliente.ToString() + "]");
                                }
                            }
                        }
                    }

                    // Validação de CNPJ duplicado
                    if ((validacao.cnpj != null) && (validacao.cnpj != String.Empty) && (ViewRecordCliente.cnpj != null) && (ViewRecordCliente.cnpj != String.Empty))
                    {
                        if (ViewRecordCliente.gc_produtor_rural == true)
                        {
                            if (ViewRecordCliente.inscricao_estadual.EmptyIfNull().ToString().Length == 0)
                            {
                                ModelState.AddModelError("Model", "Campo [CNPJ] duplicado na base de dados [Id. " + validacao.id_cliente.ToString() + "], para Produtores Rurais é necessário informar a Inscrição Estadual!");
                            }
                            else
                            {
                                if (validacao.inscricao_estadual == ViewRecordCliente.inscricao_estadual)
                                {
                                    ModelState.AddModelError("Model", "Campo [CNPJ] duplicado na base de dados para a mesma IE [Id. " + validacao.id_cliente.ToString() + "]");
                                }
                            }
                        }
                    }

                    // Validação Matrícula
                    if ((validacao.matricula != null) && (validacao.matricula != String.Empty) && (ViewRecordCliente.matricula != null) && (ViewRecordCliente.matricula != String.Empty))
                    {
                        if (validacao.matricula.ToString().ToUpper().Equals(ViewRecordCliente.matricula.ToString().ToUpper()))
                        {
                            ModelState.AddModelError("Model", "Campo [Matrícula] duplicado na base de dados [Id. " + validacao.id_cliente.ToString() + "]");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                g_clientes RecordClienteDb = new Db.g_clientes();
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                RecordClienteDb = db.g_clientes.Find(ViewRecordCliente.id_cliente);
                if (ViewRecordCliente.email_principal != null) { ViewRecordCliente.email_principal = ViewRecordCliente.email_principal.ToLowerInvariant().Trim(); };
                if (ViewRecordCliente.email_2 != null) { ViewRecordCliente.email_2 = ViewRecordCliente.email_2.ToLowerInvariant().Trim(); };
                if (ViewRecordCliente.email_3 != null) { ViewRecordCliente.email_3 = ViewRecordCliente.email_3.ToLowerInvariant().Trim(); };
                ViewRecordCliente.datahora_alteracao = DataHoraAtual;
                ViewRecordCliente.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;

                RecordClienteDb.ativo = ViewRecordCliente.ativo;
                RecordClienteDb.is_cliente = ViewRecordCliente.is_cliente;
                RecordClienteDb.is_fornecedor = ViewRecordCliente.is_fornecedor;
                RecordClienteDb.nome = ViewRecordCliente.nome;
                RecordClienteDb.razao_social = ViewRecordCliente.razao_social;
                RecordClienteDb.nome_fantasia = ViewRecordCliente.nome_fantasia;
                RecordClienteDb.data_cadastro = ViewRecordCliente.data_cadastro;
                RecordClienteDb.contrato = ViewRecordCliente.contrato;
                RecordClienteDb.identificador = ViewRecordCliente.identificador;
                RecordClienteDb.obs = ViewRecordCliente.obs;
                RecordClienteDb.email_notificacao = ViewRecordCliente.email_notificacao;
                RecordClienteDb.contato_principal = ViewRecordCliente.contato_principal;
                RecordClienteDb.contato_principal_setor = ViewRecordCliente.contato_principal_setor;
                RecordClienteDb.telefone_principal = ViewRecordCliente.telefone_principal;
                RecordClienteDb.email_principal = ViewRecordCliente.email_principal;
                RecordClienteDb.contato_2 = ViewRecordCliente.contato_2;
                RecordClienteDb.contato_2_setor = ViewRecordCliente.contato_2_setor;
                RecordClienteDb.telefone_2 = ViewRecordCliente.telefone_2;
                RecordClienteDb.email_2 = ViewRecordCliente.email_2;
                RecordClienteDb.contato_3 = ViewRecordCliente.contato_3;
                RecordClienteDb.contato_3_setor = ViewRecordCliente.contato_3_setor;
                RecordClienteDb.telefone_3 = ViewRecordCliente.telefone_3;
                RecordClienteDb.email_3 = ViewRecordCliente.email_3;
                RecordClienteDb.cpf = ViewRecordCliente.cpf;
                RecordClienteDb.data_nasc = ViewRecordCliente.data_nasc;
                RecordClienteDb.matricula = ViewRecordCliente.matricula;
                RecordClienteDb.rg = ViewRecordCliente.rg;
                RecordClienteDb.cnpj = ViewRecordCliente.cnpj;
                RecordClienteDb.id_indicador_ie = ViewRecordCliente.id_indicador_ie;
                RecordClienteDb.inscricao_estadual = ViewRecordCliente.inscricao_estadual;
                RecordClienteDb.inscricao_municipal = ViewRecordCliente.inscricao_municipal;
                RecordClienteDb.constituicao = ViewRecordCliente.constituicao;
                RecordClienteDb.inicio_atividade = ViewRecordCliente.inicio_atividade;
                RecordClienteDb.endereco_com = ViewRecordCliente.endereco_com;
                RecordClienteDb.endereco_com_numero = ViewRecordCliente.endereco_com_numero;
                RecordClienteDb.endereco_com_complemento = ViewRecordCliente.endereco_com_complemento;
                RecordClienteDb.bairro_com = ViewRecordCliente.bairro_com;
                RecordClienteDb.id_cidade_com = ViewRecordCliente.id_cidade_com;
                RecordClienteDb.cep_com = ViewRecordCliente.cep_com;
                RecordClienteDb.id_uf_com = ViewRecordCliente.id_uf_com;
                RecordClienteDb.id_pais = ViewRecordCliente.id_pais;
                RecordClienteDb.param_gc_transportadora = ViewRecordCliente.param_gc_transportadora;
                RecordClienteDb.gc_produtor_rural = ViewRecordCliente.gc_produtor_rural;
                RecordClienteDb.gc_consultor_aviacao = ViewRecordCliente.gc_consultor_aviacao;
                RecordClienteDb.param_gc_pedidos_oc = ViewRecordCliente.param_gc_pedidos_oc;
                RecordClienteDb.id_pagrec_condicao = ViewRecordCliente.id_pagrec_condicao;
                RecordClienteDb.id_frete_responsavel = ViewRecordCliente.id_frete_responsavel;
                RecordClienteDb.id_cfop_venda = ViewRecordCliente.id_cfop_venda;
                RecordClienteDb.obs_financeira = ViewRecordCliente.obs_financeira;
                RecordClienteDb.id_conta_caixa = ViewRecordCliente.id_conta_caixa;
                RecordClienteDb.valor_despesas_cobranca = ViewRecordCliente.valor_despesas_cobranca;
                RecordClienteDb.faturamento_boleto = ViewRecordCliente.faturamento_boleto;
                RecordClienteDb.faturamento_debito_conta = ViewRecordCliente.faturamento_debito_conta;
                RecordClienteDb.boleto_impresso = ViewRecordCliente.boleto_impresso;
                RecordClienteDb.boleto_email = ViewRecordCliente.boleto_email;
                RecordClienteDb.email_boleto = ViewRecordCliente.email_boleto;
                RecordClienteDb.email_nfe = ViewRecordCliente.email_nfe;
                RecordClienteDb.banco = ViewRecordCliente.banco;
                RecordClienteDb.operacao = ViewRecordCliente.operacao;
                RecordClienteDb.agencia = ViewRecordCliente.agencia;
                RecordClienteDb.dv_agencia = ViewRecordCliente.dv_agencia;
                RecordClienteDb.conta = ViewRecordCliente.conta;
                RecordClienteDb.dv_conta = ViewRecordCliente.dv_conta;
                RecordClienteDb.iss_percentual = ViewRecordCliente.iss_percentual;
                RecordClienteDb.iss_tipo = ViewRecordCliente.iss_tipo;
                RecordClienteDb.ir_percentual = ViewRecordCliente.ir_percentual;
                RecordClienteDb.ir_tipo = ViewRecordCliente.ir_tipo;
                RecordClienteDb.pis_percentual = ViewRecordCliente.pis_percentual;
                RecordClienteDb.pis_tipo = ViewRecordCliente.pis_tipo;
                RecordClienteDb.cofins_percentual = ViewRecordCliente.cofins_percentual;
                RecordClienteDb.cofins_tipo = ViewRecordCliente.cofins_tipo;
                RecordClienteDb.csll_percentual = ViewRecordCliente.csll_percentual;
                RecordClienteDb.csll_tipo = ViewRecordCliente.csll_tipo;
                RecordClienteDb.nf_percentual = ViewRecordCliente.nf_percentual;
                RecordClienteDb.nf_tipo = ViewRecordCliente.nf_tipo;
                RecordClienteDb.pcc_percentual = ViewRecordCliente.pcc_percentual;
                RecordClienteDb.pcc_tipo = ViewRecordCliente.pcc_tipo;
                RecordClienteDb.inss_percentual = ViewRecordCliente.inss_percentual;
                RecordClienteDb.inss_tipo = ViewRecordCliente.inss_tipo;
                RecordClienteDb.param_gdc_emitir_nota_debito = ViewRecordCliente.param_gdc_emitir_nota_debito;
                RecordClienteDb.param_gdc_emitir_nota_fiscal = ViewRecordCliente.param_gdc_emitir_nota_fiscal;
                RecordClienteDb.optante_simples = ViewRecordCliente.optante_simples;
                RecordClienteDb.datahora_alteracao = DataHoraAtual;
                RecordClienteDb.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(RecordClienteDb).State = EntityState.Modified;
                try
                {
                    String LogAlteracao = LibDB.CompareDataTable(RecordClienteDb, ViewRecordCliente);
                    if (LogAlteracao.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, false, "g_clientes", ViewRecordCliente.id_cliente, "Atualização Dados | " + LogAlteracao); };

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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Cliente/Fornecedor</b>" + LibStringFormat.GetTabHtml(1) + ViewRecordCliente.id_cliente.EmptyIfNull().ToString() + " - " + ViewRecordCliente.nome.EmptyIfNull().ToString();
            @ViewBag.LabelIdentificador = "CHE";
            return View("CreateEdit", ViewRecordCliente);
        }
        #endregion

        #region ModalContatos
        public ActionResult ModalCreateEditContato(int? IdCliente, int? IdContato)
        {
            IdCliente = IdCliente.GetValueOrDefault();
            IdContato = IdContato.GetValueOrDefault();
            if (IdCliente == null) { IdCliente = 0; };
            if (IdContato == null) { IdContato = 0; };

            g_clientes_contatos RecordContato = new g_clientes_contatos();
            if (IdContato == 0)
            {
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cadastrar Novo Contato</b>";
                ViewBag.idCliente = IdCliente;
                RecordContato.id_cliente = IdCliente.GetValueOrDefault();
            }
            else
            {
                ViewBag.Title = LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Editar Contato</b>";
                ViewBag.idCliente = IdCliente;
                RecordContato = db.g_clientes_contatos.Find(IdContato);
            }
            ViewBag.ComboGcClientesContatosTipos = LibDataSets.LoadComboGcClientesContatosTipos(db);
            return View(RecordContato);
        }

        [HttpPost]
        public ActionResult AjaxCreateEditContato(g_clientes_contatos ViewRecordContatos)
        {
            int QtdErros = 0;
            bool Cadastrado = false;
            String MsgRetorno = String.Empty;
            try
            {
                if (ViewRecordContatos.contato.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    MsgRetorno += "Campo <b>Contato</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                if (ViewRecordContatos.setor.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    MsgRetorno += "Campo <b>Setor</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                if (ViewRecordContatos.telefone.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    MsgRetorno += "Campo <b>Telefone</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                else
                {
                    String Telefone = LibStringFormat.SomenteNumeros(ViewRecordContatos.telefone.EmptyIfNull().ToString().Trim());
                    if ((Telefone.Length != 10) && (Telefone.Length != 11))
                    {
                        MsgRetorno += "Campo <b>Telefone</b> deverá conter a seguinte formatação DDNNNNNNNNN onde os 2 primeiros dígitos deverão ser o DDD e os dígitos seguintes o número do telefone ou celular com 8 ou 9 dígitos!<br/>";
                    }
                }
                if (ViewRecordContatos.email.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    MsgRetorno += "Campo <b>Email</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                else
                {
                    if (LibStringValidate.ValidarEmail(ViewRecordContatos.email.EmptyIfNull().ToString()) == false)
                    {
                        MsgRetorno += "Campo <b>Email</b> contém um email inválido!<br/>";
                        QtdErros += 1;
                    }
                }

                if (QtdErros == 0)
                {
                    if (ViewRecordContatos.id_contato == 0) // Novo Contato
                    {
                        // Logs
                        ViewRecordContatos.ativo = true;
                        ViewRecordContatos.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                        ViewRecordContatos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                        ViewRecordContatos.id_coligada = 1;
                        ViewRecordContatos.id_filial = 1;
                        db.Entry(ViewRecordContatos).State = EntityState.Added;
                        db.SaveChanges();
                        Cadastrado = true;
                        MsgRetorno = "Contato <b>Cadastrado</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");

                        String TipoContato = String.Empty;
                        List<Db.g_clientes_contatos_tipos> AllContatosTipos = db.g_clientes_contatos_tipos.Where(c => c.ativo == true).ToList();
                        g_clientes_contatos_tipos RecordTipoContato = AllContatosTipos.Where(t => t.id_contato_tipo == ViewRecordContatos.id_contato_tipo).FirstOrDefault();
                        if (RecordTipoContato != null) { TipoContato = RecordTipoContato.nome.EmptyIfNull().ToString(); };

                        // Audit
                        String LogAlteracao = "Novo contato | ";
                        LogAlteracao += "Contato: " + ViewRecordContatos.contato.EmptyIfNull().ToString().Trim() + " | ";
                        LogAlteracao += "Tipo: " + TipoContato + " | ";
                        LogAlteracao += "Telefone: " + ViewRecordContatos.telefone.EmptyIfNull().ToString().Trim() + " | ";
                        LogAlteracao += "Email: " + ViewRecordContatos.email.EmptyIfNull().ToString().Trim() + " | ";
                        if (LogAlteracao.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true, "g_clientes", ViewRecordContatos.id_cliente, LogAlteracao.EmptyIfNull()); };
                    }
                    else // Edição de Dados
                    {
                        // Logs
                        g_clientes_contatos EditRecordContato = db.g_clientes_contatos.Find(ViewRecordContatos.id_contato);

                        EditRecordContato.contato = ViewRecordContatos.contato;
                        EditRecordContato.id_contato_tipo = ViewRecordContatos.id_contato_tipo;
                        EditRecordContato.setor = ViewRecordContatos.setor;
                        EditRecordContato.telefone = ViewRecordContatos.telefone;
                        EditRecordContato.email = ViewRecordContatos.email;
                        EditRecordContato.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                        EditRecordContato.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                        db.Entry(EditRecordContato).State = EntityState.Modified;
                        db.SaveChanges();
                        Cadastrado = true;
                        MsgRetorno = "Contato <b>Atualizado</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");

                        String TipoContatoOrigem = String.Empty;
                        String TipoContatoDestino = String.Empty;
                        List<Db.g_clientes_contatos_tipos> AllContatosTipos = db.g_clientes_contatos_tipos.Where(c => c.ativo == true).ToList();
                        g_clientes_contatos_tipos RecordTipoContatoOrigem = AllContatosTipos.Where(t => t.id_contato_tipo == ViewRecordContatos.id_contato_tipo).FirstOrDefault();
                        if (RecordTipoContatoOrigem != null) { TipoContatoOrigem = RecordTipoContatoOrigem.nome.EmptyIfNull().ToString(); };
                        g_clientes_contatos_tipos RecordTipoContatoDestino = AllContatosTipos.Where(t => t.id_contato_tipo == ViewRecordContatos.id_contato_tipo).FirstOrDefault();
                        if (RecordTipoContatoDestino != null) { TipoContatoDestino = RecordTipoContatoDestino.nome.EmptyIfNull().ToString(); };

                        // Audit
                        String LogAlteracao = string.Empty;
                        if (ViewRecordContatos.contato.EmptyIfNull().ToString().Trim() != EditRecordContato.contato.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Contato: " + ViewRecordContatos.contato.EmptyIfNull().ToString().Trim() + " > " + EditRecordContato.contato.EmptyIfNull().ToString().Trim() + " | "; }
                        if (ViewRecordContatos.id_contato_tipo.EmptyIfNull().ToString().Trim() != EditRecordContato.id_contato_tipo.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Tipo: " + TipoContatoOrigem + " > " + TipoContatoDestino + " | "; };
                        if (ViewRecordContatos.setor.EmptyIfNull().ToString().Trim() != EditRecordContato.setor.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Setor: " + ViewRecordContatos.setor.EmptyIfNull().ToString().Trim() + " > " + EditRecordContato.contato.EmptyIfNull().ToString().Trim() + " | "; }
                        if (ViewRecordContatos.telefone.EmptyIfNull().ToString().Trim() != EditRecordContato.telefone.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Telefone: " + ViewRecordContatos.telefone.EmptyIfNull().ToString().Trim() + " > " + EditRecordContato.telefone.EmptyIfNull().ToString().Trim() + " | "; };
                        if (ViewRecordContatos.email.EmptyIfNull().ToString().Trim() != EditRecordContato.email.EmptyIfNull().ToString().Trim()) { LogAlteracao += "Email: " + ViewRecordContatos.email.EmptyIfNull().ToString().Trim() + " > " + EditRecordContato.email.EmptyIfNull().ToString().Trim() + " | "; };
                        if (LogAlteracao.EmptyIfNull().ToString().Length > 0) 
                        {
                            LogAlteracao = "Alteração dados do contato | " + LogAlteracao ;
                            LibAudit.SaveAudit(db, true, "g_clientes", ViewRecordContatos.id_cliente, LogAlteracao.EmptyIfNull().ToLowerInvariant()); 
                        };
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                Cadastrado = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Cadastrado = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Cadastrado, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ModalDesativarContato(int? IdContato)
        {
            IdContato = IdContato.GetValueOrDefault();
            if (IdContato == null) { IdContato = 0; };
            g_clientes_contatos RecordContato = db.g_clientes_contatos.Find(IdContato);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-power-off", "", "red", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Desativar Contato</b>";
            ViewBag.ComboGcClientesContatosTipos = LibDataSets.LoadComboGcClientesContatosTipos(db);
            return View(RecordContato);
        }

        [HttpPost]
        public ActionResult AjaxDesativarContato(g_clientes_contatos ViewRecordContatos)
        {
            bool Desativado = false;
            String MsgRetorno = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            try
            {
                if (ViewRecordContatos.motivo_desativacao.EmptyIfNull().ToString().Equals(String.Empty))
                {
                    MsgRetorno += "Campo [Motivo] é de preenchimento obrigatório!<br/>";
                }
                else
                {
                    // Logs
                    g_clientes_contatos EditRecordContato = db.g_clientes_contatos.Find(ViewRecordContatos.id_contato);

                    EditRecordContato.ativo = false;
                    EditRecordContato.motivo_desativacao = ViewRecordContatos.motivo_desativacao;
                    EditRecordContato.id_usuario_desativacao = CachePersister.userIdentity.IdUsuario; ;
                    EditRecordContato.datahora_desativacao = LibDateTime.getDataHoraBrasilia();
                    EditRecordContato.datahora_alteracao = DataHoraAtual;
                    EditRecordContato.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(EditRecordContato).State = EntityState.Modified;
                    db.SaveChanges();
                    MsgRetorno = "Contato <b>Desativado</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                    Desativado = true;

                    String TipoContatoOrigem = String.Empty;
                    List<Db.g_clientes_contatos_tipos> AllContatosTipos = db.g_clientes_contatos_tipos.Where(c => c.ativo == true).ToList();
                    g_clientes_contatos_tipos RecordTipoContatoOrigem = AllContatosTipos.Where(t => t.id_contato_tipo == ViewRecordContatos.id_contato_tipo).FirstOrDefault();
                    if (RecordTipoContatoOrigem != null) { TipoContatoOrigem = RecordTipoContatoOrigem.nome.EmptyIfNull().ToString(); };

                    // Audit
                    String LogAlteracao = "Desativação do contato | ";
                    LogAlteracao += "Contato: " + EditRecordContato.contato.EmptyIfNull().ToString().Trim() + " | ";
                    LogAlteracao += "Tipo: " + TipoContatoOrigem;
                    LogAlteracao += "Telefone: " + EditRecordContato.telefone.EmptyIfNull().ToString().Trim() + " | ";
                    LogAlteracao += "Email: " + EditRecordContato.email.EmptyIfNull().ToString().Trim() + " | ";
                    LogAlteracao += "Motivo: " + EditRecordContato.motivo_desativacao.EmptyIfNull().ToString().Trim() + " | ";
                    if (LogAlteracao.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true, "g_clientes", ViewRecordContatos.id_cliente, LogAlteracao.EmptyIfNull().ToLowerInvariant()); };
                }
            }
            catch (DbEntityValidationException ex)
            {
                Desativado = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Desativado = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Desativado, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalAtualizarVendedorConsultor
        public ActionResult ModalAtualizarVendedorConsultor(int? idCliente)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Atualizar Vendedor/Consultor</b>";
            ViewBag.idCliente = idCliente;
            g_clientes record_g_clientes = db.g_clientes.Find(idCliente.GetValueOrDefault());
            var comboVendedor = new List<SelectListItem>();
            try
            {
                IQueryable<g_vendedores> listaDbVendedor = null;
                if (CachePersister.userIdentity.IdPerfil == 1) { listaDbVendedor = db.g_vendedores.Select(p => p).OrderBy(p => p.nome); }
                else { listaDbVendedor = db.g_vendedores.Where(v => v.ativo == true).OrderBy(p => p.nome); };
                foreach (g_vendedores record_g_vendedores in listaDbVendedor)
                {
                    comboVendedor.Add(new SelectListItem { Value = record_g_vendedores.id_vendedor.ToString(), Text = record_g_vendedores.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboVendedor = comboVendedor;
            ViewBag.comboConsultor = comboVendedor;
            return View("ModalAtualizarVendedorConsultor", record_g_clientes);
        }

        [HttpPost]
        public ActionResult AjaxModalAtualizarVendedorConsultor(g_clientes view_record_g_clientes)
        {
            bool cadastrado = false;
            int QtdErros = 0;
            int QtdAlteracoes = 0;
            String msgRetorno = String.Empty;
            try
            {
                if ((view_record_g_clientes.id_vendedor.EmptyIfNull().ToString().Length == 0) || (view_record_g_clientes.id_vendedor.EmptyIfNull().ToString() == "0"))
                {
                    msgRetorno += "Campo <b>Vendedor</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                if (QtdErros == 0)
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                    g_clientes record_g_clientes = db.g_clientes.Find(view_record_g_clientes.id_cliente);
                    List<g_vendedores> AllVendedores = db.g_vendedores.Where(v => v.ativo == true).OrderBy(p => p.nome).ToList(); 

                    String LogAlteracoes = string.Empty;

                    if (view_record_g_clientes.id_vendedor == 0)
                    {
                        msgRetorno = "Preencha corretamente todos os campos!";
                        QtdErros += 1;
                        cadastrado = false;
                    }
                    else
                    {
                        if (record_g_clientes.id_vendedor != view_record_g_clientes.id_vendedor) 
                        { 
                            if (record_g_clientes.id_vendedor == 0) { LogAlteracoes += " Vendedor(1): " + AllVendedores.Where(v => v.id_vendedor == view_record_g_clientes.id_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + " | "; }
                            else { LogAlteracoes += " Vendedor(1): " + AllVendedores.Where(v => v.id_vendedor == record_g_clientes.id_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + " > " + AllVendedores.Where(v => v.id_vendedor == view_record_g_clientes.id_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + " | "; };
                            QtdAlteracoes += 1;
                        }

                                                
                        
                        if (QtdAlteracoes > 0)
                        {
                            record_g_clientes.id_vendedor = view_record_g_clientes.id_vendedor;
                            record_g_clientes.datahora_alteracao = DataHoraAtual;
                            record_g_clientes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            record_g_clientes.datahora_alteracao = DataHoraAtual;
                            record_g_clientes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_g_clientes).State = EntityState.Modified;
                            db.SaveChanges();
                            if (LogAlteracoes.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true,"g_clientes", record_g_clientes.id_cliente, "Atualização Vendedor/Consultor | " + LogAlteracoes.EmptyIfNull().ToLowerInvariant()); };
                            cadastrado = true;
                            msgRetorno = "Dados do cliente atualizados com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                        }
                        else
                        {
                            cadastrado = true;
                            msgRetorno = "Não houve alteração de dados!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-solid fa-circle-xmark", "Erro", "red", "");
                        }
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                cadastrado = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                cadastrado = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = cadastrado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalAtualizarVendedorConsultor
        public ActionResult ModalAtualizarLimiteCredito(int? idCliente)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Atualizar Limite de Crédito</b>";
            ViewBag.idCliente = idCliente;
            g_clientes record_g_clientes = db.g_clientes.Find(idCliente.GetValueOrDefault());
            cstClienteLimiteCredito RecordLimiteCredito = new cstClienteLimiteCredito();
            RecordLimiteCredito.id_cliente = record_g_clientes.id_cliente;
            RecordLimiteCredito.limite_credito_atual = record_g_clientes.gc_limite_credito;
            RecordLimiteCredito.consulta_cadastral_data = LibDateTime.getDataHoraBrasilia();
            return View("ModalAtualizarLimiteCredito", RecordLimiteCredito);
        }

        [HttpPost]
        public ActionResult AjaxModalAtualizarLimiteCredito(cstClienteLimiteCredito RecordLimiteCredito)
        {
            bool cadastrado = false;
            int QtdErros = 0;
            String msgRetorno = String.Empty;
            try
            {
                if ((RecordLimiteCredito.novo_limite_credito.EmptyIfNull().ToString().Length == 0) || (RecordLimiteCredito.novo_limite_credito.EmptyIfNull().ToString() == "0"))
                {
                    msgRetorno += "Campo <b>Novo Limite Crédito</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                else
                {
                    if (LibNumbers.IsValidDecimal(RecordLimiteCredito.novo_limite_credito.EmptyIfNull().ToString()) == false)
                    {
                        msgRetorno += "Campo <b>Novo Limite Crédito</b> contém um valor inválido!<br/>";
                        QtdErros += 1;
                    }
                }
                if (RecordLimiteCredito.aprovado_por.EmptyIfNull().ToString().Length == 0)
                {
                    msgRetorno += "Campo <b>Aprovado Por</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                if (RecordLimiteCredito.justificativa.EmptyIfNull().ToString().Length == 0)
                {
                    msgRetorno += "Campo <b>Justificativa</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                
                if (QtdErros == 0)
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                    g_clientes record_g_clientes = db.g_clientes.Find(RecordLimiteCredito.id_cliente);
                    RecordLimiteCredito.limite_credito_atual = record_g_clientes.gc_limite_credito;
                    record_g_clientes.gc_limite_credito = RecordLimiteCredito.novo_limite_credito;
                    record_g_clientes.datahora_alteracao = DataHoraAtual;
                    record_g_clientes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_g_clientes).State = EntityState.Modified;
                    db.SaveChanges();

                    // Log de Alterações
                    String LogAlteracoes = string.Empty;
                    if (RecordLimiteCredito.consulta_cadastral_realizada == true)
                    {
                        LogAlteracoes += "Consulta Realizada: Sim | ";
                        LogAlteracoes += "Data Consulta: " + RecordLimiteCredito.consulta_cadastral_data.GetValueOrDefault().ToString("dd/MM/yyyy") + " | ";
                        if (RecordLimiteCredito.consulta_cadastral_restricoes == true) { LogAlteracoes += "Restrições: Sim | "; } else { LogAlteracoes += "Restrições: Não | "; }
                        LogAlteracoes += "Score: " + RecordLimiteCredito.consulta_cadastral_score.EmptyIfNull().ToString() + " | ";
                    }
                    else
                    {
                        LogAlteracoes += "Consulta Realizada: Não | ";
                    }
                    LogAlteracoes += "Limite Crédito (Atual): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordLimiteCredito.limite_credito_atual) + " | ";
                    LogAlteracoes += "Limite Crédito (Novo): " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordLimiteCredito.novo_limite_credito) + " | ";
                    LogAlteracoes += "Justificativa: " + RecordLimiteCredito.justificativa.EmptyIfNull().ToString() + " | ";
                    LogAlteracoes += "Aprovado por: " + RecordLimiteCredito.aprovado_por.EmptyIfNull().ToString() + " | ";
                    if (LogAlteracoes.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true,"g_clientes", record_g_clientes.id_cliente, "Atualização limite crédito | " + LogAlteracoes.EmptyIfNull().ToLowerInvariant()); };
                    cadastrado = true;
                    msgRetorno = "Novo limite de crédito do cliente: "+ string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordLimiteCredito.novo_limite_credito) + "!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                }
            }
            catch (DbEntityValidationException ex)
            {
                cadastrado = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                cadastrado = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = cadastrado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalNovoCadastroRoboSintegra
        public ActionResult ModalNovoCadastroRoboSintegra()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Cadastrar Novo Cliente</b>";
            g_clientes record_g_clientes = new g_clientes();
            record_g_clientes.data_nasc = null;
            return View(record_g_clientes);
        }



        [HttpPost]
        public ActionResult AjaxModalCadastroNovoCliente(g_clientes view_record_g_clientes)
        {
            bool CadastroLiberado = true;
            bool IntegradoSintegra = false;
            bool TipoDocumentoCNPJ = false;
            bool TipoDocumentoCPF = false;
            string ApiStatus = String.Empty;
            string ApiCpf = String.Empty;
            string ApiNascimento = String.Empty;
            string ApiNome = String.Empty;
            string ApiSituacao = String.Empty;
            string ApiSituacaoData = String.Empty;
            string ApiId = String.Empty;
            string ApiCnpj = String.Empty;
            string ApiRazaoSocial = String.Empty;
            string ApiNomeFantasia = String.Empty;
            string ApiDataAbertura = String.Empty;
            string ApiEndereco = String.Empty;
            string ApiEnderecoNumero = String.Empty;
            string ApiEnderecoComplemento = String.Empty;
            string ApiBairro = String.Empty;
            string ApiCEP = String.Empty;
            string ApiCidade = String.Empty;
            string ApiUF = String.Empty;
            String msgRetorno = String.Empty;
            try
            {
                if (view_record_g_clientes.cnpj.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    CadastroLiberado = false;
                    msgRetorno += "Campo <b>CPF/CNPJ</b> é de preenchimento obrigatório!<br/>";
                }
                else
                {
                    String Documento = LibStringFormat.SomenteNumeros(view_record_g_clientes.cnpj.ToString().Trim());
                    if (Documento.Length == 11)
                    {
                        TipoDocumentoCPF = true;
                        if (LibStringValidate.ValidarCPF(Documento) == false)
                        {
                            CadastroLiberado = false;
                            msgRetorno += "CPF Inválido!<br/>";
                        }
                        else
                        {
                            g_clientes record_g_clientes = db.g_clientes.Where(c => c.cpf == Documento).FirstOrDefault();
                            if (record_g_clientes != null)
                            {
                                if (record_g_clientes.gc_produtor_rural == false)
                                {
                                    CadastroLiberado = false;
                                    msgRetorno += "CPF já está cadastrado na base de dados!<br/>";
                                    msgRetorno += "verifique se o cliente é Produtor Rural, caso positivo, marque o parâmetro correspondente no cadastro comercial!<br/>";
                                }
                            }
                        }

                    }
                    else if (Documento.Length == 14)
                    {
                        TipoDocumentoCNPJ = true;
                        if (LibStringValidate.ValidarCNPJ(Documento) == false)
                        {
                            CadastroLiberado = false;
                            msgRetorno += "CNPJ Inválido!<br/>";
                        }
                        else
                        {
                            g_clientes record_g_clientes = db.g_clientes.Where(c => c.ativo == true && c.cnpj == Documento).FirstOrDefault();
                            if (record_g_clientes != null)
                            {
                                CadastroLiberado = false;
                                msgRetorno += "CNPJ já está cadastrado na base de dados!<br/>";
                                msgRetorno += "verifique se o cliente é Produtor Rural, caso positivo, marque o parâmetro correspondente no cadastro comercial!<br/>";
                            }

                        }
                    }
                    else
                    {
                        CadastroLiberado = false;
                        msgRetorno += "Informe os 11 números do CPF ou os 14 números do CNPJ!<br/>";
                    }

                    if (CadastroLiberado == true)
                    {
                        if (TipoDocumentoCNPJ == true)
                        {
                            // Validação Robô Sintegra
                            try
                            {
                                String RetornoSintegraWS = String.Empty;
                                RoboSintegraWS _RoboSintegraWS = new RoboSintegraWS();
                                RetornoSintegraWS = _RoboSintegraWS.GetDadosSintegraCNPJ(Documento);
                                cstRetornoSintegraWS _cstRetornoSintegraWS = new cstRetornoSintegraWS();
                                var data = (JObject)JsonConvert.DeserializeObject(RetornoSintegraWS);
                                try { _cstRetornoSintegraWS.sintegra_code = data["code"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_status = data["status"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_message = data["message"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_ie = data["inscricao_estadual"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_razaosocial = data["nome_empresarial"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.informacao_ie_como_destinatario = data["informacao_ie_como_destinatario"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_cep = data["cep"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_uf = data["uf"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_municipio = data["municipio"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_bairro = data["bairro"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_logradouro = data["logradouro"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_numero = data["numero"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.sintegra_complemento = data["complemento"].Value<string>(); } catch (Exception) { };
                                try { _cstRetornoSintegraWS.situacao_ie = data["situacao_ie"].Value<string>(); } catch (Exception) { }; // String = Ativo | Inativo | Baixado
                                try { _cstRetornoSintegraWS.data_inicio_atividade = data["data_inicio_atividade"].Value<string>(); } catch (Exception) { }; // String = Data do início da atividade no formato dd-MM-yyyy.
                                try { _cstRetornoSintegraWS.contribuinte_icms = data["contribuinte_icms"].Value<string>(); } catch (Exception) { }; // Boolean = true | false
                                if (_cstRetornoSintegraWS.sintegra_code.Equals("0"))
                                {
                                    g_clientes novo_cliente = new g_clientes();
                                    novo_cliente.inscricao_estadual = _cstRetornoSintegraWS.sintegra_ie.EmptyIfNull().ToString().Trim();
                                    novo_cliente.nome = _cstRetornoSintegraWS.sintegra_razaosocial.EmptyIfNull().ToString().Trim().ToUpperInvariant();
                                    novo_cliente.razao_social = novo_cliente.nome;
                                    novo_cliente.cep_com = LibStringFormat.SomenteNumeros(_cstRetornoSintegraWS.sintegra_cep);
                                    novo_cliente.id_uf_com = getIdUF(_cstRetornoSintegraWS.sintegra_uf);
                                    novo_cliente.id_cidade_com = getIdCidade(_cstRetornoSintegraWS.sintegra_municipio);
                                    novo_cliente.bairro_com = _cstRetornoSintegraWS.sintegra_bairro.ToUpper();
                                    novo_cliente.endereco_com = _cstRetornoSintegraWS.sintegra_logradouro.ToUpper();
                                    if (_cstRetornoSintegraWS.sintegra_numero.EmptyIfNull().ToString().Length > 0) { novo_cliente.endereco_com_numero = _cstRetornoSintegraWS.sintegra_numero.EmptyIfNull().ToString().ToUpperInvariant(); }
                                    if (_cstRetornoSintegraWS.sintegra_complemento.EmptyIfNull().ToString().Length > 0) { novo_cliente.endereco_com_complemento = _cstRetornoSintegraWS.sintegra_complemento.EmptyIfNull().ToString().ToUpperInvariant(); }
                                    if (Documento.Length == 11) { novo_cliente.cpf = Documento; }
                                    else if (Documento.Length == 14) { novo_cliente.cnpj = Documento; };
                                    if ((LibStringFormat.RemoverAcentos(_cstRetornoSintegraWS.informacao_ie_como_destinatario.EmptyIfNull().ToString().ToUpperInvariant()).IndexOf("OBRIGATORIA") >= 0) || (_cstRetornoSintegraWS.contribuinte_icms.EmptyIfNull().ToString().ToUpperInvariant().IndexOf("TRUE") >= 0))
                                    { novo_cliente.id_indicador_ie = 2; } // Contribuinte
                                    else { novo_cliente.id_indicador_ie = 3; } // Não Contribuinte
                                    novo_cliente.sintegra_integracao = true;
                                    novo_cliente.sintegra_datahora = LibDateTime.getDataHoraBrasilia();
                                    msgRetorno = "Documento consultado no Sintegra com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                                    msgRetorno += "<br/><br/>";
                                    msgRetorno += "<b>CNPJ:</b> " + LibStringFormat.FormatarCPFCNPJ("J", Documento) + "<br/>";
                                    msgRetorno += "<b>IE:</b> " + novo_cliente.inscricao_estadual + "<br/>";
                                    msgRetorno += "<b>RAZÃO SOCIAL:</b> " + novo_cliente.razao_social + "<br/><br/>";
                                    msgRetorno += "<b>Atenção:</b> Informe os dados complementares do cadastro do cliente!";
                                    CachePersister.userIdentity.NovoClienteSintegra = novo_cliente;
                                    CadastroLiberado = true;
                                    IntegradoSintegra = true;
                                }
                                else
                                {
                                    CachePersister.userIdentity.NovoClienteSintegra = null;
                                    msgRetorno += "<b>***** ATENÇÃO *****</b><br/>Documento informado [" + Documento + "] não consta na base de dados do SINTEGRA<br/><br/>";
                                    IntegradoSintegra = false;
                                }
                            }
                            catch (Exception)
                            {
                                msgRetorno += "***** ATENÇÃO *****<br/><br/>Não foi possível consultar o SINTEGRA<br/>";
                                IntegradoSintegra = false;
                            }

                            // Validação Robô Receita Federal
                            if (IntegradoSintegra == false)
                            {
                                RoboCpfCnpj _RoboCpfCnpj = new RoboCpfCnpj();
                                ModelApiResponse RetornoRobo = _RoboCpfCnpj.GetDadosCNPJ(Documento);
                                if (RetornoRobo.SucessoRobo == true)
                                {
                                    var data = (JObject)JsonConvert.DeserializeObject(RetornoRobo.RetornoRobo);
                                    try { ApiStatus = data["status"].Value<string>(); } catch (Exception) { };
                                    try { ApiCnpj = data["cnpj"].Value<string>(); } catch (Exception) { };
                                    try { ApiRazaoSocial = data["razao"].Value<string>(); } catch (Exception) { };
                                    try { ApiNomeFantasia = data["fantasia"].Value<string>(); } catch (Exception) { };
                                    try { ApiDataAbertura = data["inicioAtividade"].Value<string>(); } catch (Exception) { };
                                    try { ApiSituacao = data["situacao"]["nome"].Value<string>(); } catch (Exception) { };
                                    try { ApiSituacaoData = data["situacao"]["data"].Value<string>(); } catch (Exception) { };
                                    try { ApiId = data["consultaID"].Value<string>(); } catch (Exception) { };
                                    try { ApiEndereco = data["matrizEndereco"]["tipo"].Value<string>(); } catch (Exception) { };
                                    try { ApiEndereco += " " + data["matrizEndereco"]["logradouro"].Value<string>(); } catch (Exception) { };
                                    try { ApiEnderecoNumero = data["matrizEndereco"]["numero"].Value<string>(); } catch (Exception) { };
                                    try { ApiEnderecoComplemento = data["matrizEndereco"]["complemento"].Value<string>(); } catch (Exception) { };
                                    try { ApiBairro = data["matrizEndereco"]["bairro"].Value<string>(); } catch (Exception) { };
                                    try { ApiCEP = data["matrizEndereco"]["cep"].Value<string>(); } catch (Exception) { };
                                    try { ApiCidade = data["matrizEndereco"]["cidade"].Value<string>(); } catch (Exception) { };
                                    try { ApiUF = data["matrizEndereco"]["uf"].Value<string>(); } catch (Exception) { };

                                    if (ApiStatus == "1")
                                    {
                                        if ((ApiSituacao.ToUpperInvariant() == "REGULAR") || (ApiSituacao.ToUpperInvariant() == "ATIVA"))
                                        {
                                            msgRetorno += "Documento consultado na Receita Federal com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                                            msgRetorno += "<br/><br/>";
                                            msgRetorno += "<b>CNPJ:</b> " + LibStringFormat.FormatarCPFCNPJ("J", Documento) + "<br/>";
                                            msgRetorno += "<b>Razão Social:</b> " + ApiRazaoSocial + "<br/>";
                                            msgRetorno += "<b>Nome Fantasia:</b> " + ApiNomeFantasia + "<br/>";
                                            msgRetorno += "<b>Data Abertura:</b> " + ApiDataAbertura + "<br/>";
                                            msgRetorno += "<b>Situação CNPJ:</b> " + ApiSituacao + "<br/>";
                                            msgRetorno += "<b>Comprovante:</b> " + ApiId + "<br/>";
                                            msgRetorno += "<b>Atenção:</b> Informe os dados complementares do cadastro do cliente!";
                                            g_clientes novo_cliente = new g_clientes();
                                            novo_cliente.nome = ApiNomeFantasia;
                                            novo_cliente.razao_social = ApiRazaoSocial;
                                            novo_cliente.cnpj = Documento;
                                            if (ApiCEP.EmptyIfNull().ToString().Length > 0) { novo_cliente.cep_com = LibStringFormat.SomenteNumeros(ApiCEP); };
                                            if (ApiUF.EmptyIfNull().ToString().Length > 0) { novo_cliente.id_uf_com = getIdUF(ApiUF); };
                                            if (ApiCidade.EmptyIfNull().ToString().Length > 0) { novo_cliente.id_cidade_com = getIdCidade(ApiCidade); };
                                            if (ApiBairro.EmptyIfNull().ToString().Length > 0) { novo_cliente.bairro_com = ApiBairro.ToUpperInvariant(); };
                                            if (ApiEndereco.EmptyIfNull().ToString().Length > 0) { novo_cliente.endereco_com = ApiEndereco.ToUpperInvariant(); };
                                            if (ApiEnderecoNumero.EmptyIfNull().ToString().Length > 0) { novo_cliente.endereco_com_numero = ApiEnderecoNumero.ToUpperInvariant(); };
                                            if (ApiEnderecoComplemento.EmptyIfNull().ToString().Length > 0) { novo_cliente.endereco_com_complemento = ApiEnderecoComplemento.ToUpperInvariant(); };
                                            novo_cliente.sintegra_integracao = true;
                                            novo_cliente.sintegra_datahora = LibDateTime.getDataHoraBrasilia();
                                            CachePersister.userIdentity.NovoClienteSintegra = novo_cliente;
                                            CadastroLiberado = true;
                                        }
                                        else
                                        {
                                            msgRetorno += "<b>***** ATENÇÃO *****</b>";
                                            msgRetorno += "<br/>Documento informado está com a situação [" + ApiSituacao.EmptyIfNull().ToUpperInvariant() + "] em ["+ ApiSituacaoData + "] na Receita Federal!<br/><br/>";
                                            msgRetorno += "<b>NÃO Realize o cadastro desse cliente no ERP</b>";
                                            CadastroLiberado = false;
                                        }
                                    }
                                    else
                                    {
                                        msgRetorno += "***** ATENÇÃO *****<br/><br/>Não foi possível consultar a Receita Federal<br/>O cadastro deverá ser realizado manualmente!";
                                        if (RetornoRobo.MsgErro.EmptyIfNull().ToString().Length > 0) { msgRetorno += "<br/><br/>" + RetornoRobo.MsgErro.EmptyIfNull().ToString(); }
                                        CadastroLiberado = true;
                                    }
                                }
                                else
                                {
                                    msgRetorno += "***** ATENÇÃO *****<br/><br/>Não foi possível consultar a Receita Federal<br/>O cadastro deverá ser realizado manualmente!";
                                    if (RetornoRobo.MsgErro.EmptyIfNull().ToString().Length > 0) { msgRetorno += "<br/><br/>" + RetornoRobo.MsgErro.EmptyIfNull().ToString(); }
                                    CadastroLiberado = true;
                                }
                            }
                        }
                        else if (TipoDocumentoCPF == true)
                        {
                            // Validação Robô Receita Federal
                            try
                            {
                                RoboCpfCnpj _RoboCpfCnpj = new RoboCpfCnpj();
                                ModelApiResponse RetornoRobo = _RoboCpfCnpj.GetDadosCPF(Documento);
                                if (RetornoRobo.SucessoRobo == true)
                                {
                                    var data = (JObject)JsonConvert.DeserializeObject(RetornoRobo.RetornoRobo);
                                    try { ApiStatus = data["status"].Value<string>(); } catch (Exception) { };
                                    try { ApiCpf = data["cpf"].Value<string>(); } catch (Exception) { };
                                    try { ApiNascimento = data["nascimento"].Value<string>(); } catch (Exception) { };
                                    try { ApiNome = data["nome"].Value<string>(); } catch (Exception) { };
                                    try { ApiSituacao = data["situacao"].Value<string>(); } catch (Exception) { };
                                    try { ApiId = data["consultaID"].Value<string>(); } catch (Exception) { };

                                    if (ApiStatus == "1")
                                    {
                                        if ((ApiSituacao.ToUpperInvariant() == "REGULAR") || (ApiSituacao.ToUpperInvariant() == "ATIVA"))
                                        {
                                            msgRetorno += "Documento consultado na Receita Federal com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                                            msgRetorno += "<br/><br/>";
                                            msgRetorno += "<b>CPF:</b> " + LibStringFormat.FormatarCPFCNPJ("J", Documento) + "<br/>";
                                            msgRetorno += "<b>Nome:</b> " + ApiNome + "<br/>";
                                            msgRetorno += "<b>Data Nasc:</b> " + ApiNascimento + "<br/>";
                                            msgRetorno += "<b>Situação CPF:</b> " + ApiSituacao + "<br/>";
                                            msgRetorno += "<b>Comprovante:</b> " + ApiId + "<br/>";
                                            msgRetorno += "<b>Atenção:</b> Informe os dados complementares do cadastro do cliente!";
                                            g_clientes novo_cliente = new g_clientes();
                                            novo_cliente.nome = ApiNome;
                                            novo_cliente.razao_social = ApiNome;
                                            novo_cliente.cpf = Documento;
                                            novo_cliente.sintegra_integracao = true;
                                            novo_cliente.sintegra_datahora = LibDateTime.getDataHoraBrasilia();
                                            CachePersister.userIdentity.NovoClienteSintegra = novo_cliente;
                                            CadastroLiberado = true;
                                        }
                                        else
                                        {
                                            msgRetorno += "<b>***** ATENÇÃO *****</b><br/>Documento informado está com a situação [" + ApiSituacao + "] na Receita Federal!<br/><br/>";
                                            msgRetorno += "<b>NÃO Realize o cadastro desse cliente no ERP</b>";
                                            CadastroLiberado = false;
                                            IntegradoSintegra = false;
                                        }
                                    }
                                    else
                                    {
                                        msgRetorno += "***** ATENÇÃO *****<br/><br/>Não foi possível consultar a Receita Federal<br/>O cadastro deverá ser realizado manualmente!";
                                        if (RetornoRobo.MsgErro.EmptyIfNull().ToString().Length > 0) { msgRetorno += "<br/><br/>" + RetornoRobo.MsgErro.EmptyIfNull().ToString(); }
                                        CadastroLiberado = true;
                                        IntegradoSintegra = false;
                                    }
                                }
                                else
                                {
                                    msgRetorno += "***** ATENÇÃO *****<br/><br/>Não foi possível consultar a Receita Federal<br/>O cadastro deverá ser realizado manualmente!";
                                    if (RetornoRobo.MsgErro.EmptyIfNull().ToString().Length > 0) { msgRetorno += "<br/><br/>" + RetornoRobo.MsgErro.EmptyIfNull().ToString(); }
                                    CadastroLiberado = true;
                                    IntegradoSintegra = false;
                                }
                            }
                            catch (Exception)
                            {
                                msgRetorno += "***** ATENÇÃO *****<br/><br/>Não foi possível consultar a Receita Federal<br/>O cadastro deverá ser realizado manualmente!";
                                CadastroLiberado = true;
                                IntegradoSintegra = false;
                            }
                        }
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                CadastroLiberado = false;
                IntegradoSintegra = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                CadastroLiberado = false;
                IntegradoSintegra = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            finally
            {
                if (IntegradoSintegra == true)
                {

                }
            }
            return Json(new { success = CadastroLiberado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        public int getIdCidade(String NomeCidade)
        {
            NomeCidade = LibStringFormat.FormatarTextoSimples(NomeCidade).ToUpper();
            g_cidades record_g_cidades = db.g_cidades.Where(c => c.nome == NomeCidade && c.ativo == true).FirstOrDefault();
            if (record_g_cidades == null)
            {
                record_g_cidades = new Db.g_cidades();
                record_g_cidades.nome = NomeCidade;
                record_g_cidades.ativo = true;
                record_g_cidades.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_cidades.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.g_cidades.Add(record_g_cidades);
                db.SaveChanges();
            }
            return record_g_cidades.id_cidade;
        }

        public int getIdUF(String NomeUF)
        {
            NomeUF = LibStringFormat.FormatarTextoSimples(NomeUF);
            g_uf record_g_uf = db.g_uf.Where(u => u.sigla == NomeUF).FirstOrDefault();
            if (record_g_uf == null)
            {
                record_g_uf = new Db.g_uf();
                record_g_uf.id_uf = 0;
            }
            return record_g_uf.id_uf;
        }
        #endregion

        [HttpPost]
        public ActionResult AjaxGetDadosContato(g_clientes_contatos view_g_clientes_contatos)
        {
            bool Sucesso = false;
            string MsgRetorno = String.Empty;
            string ContatoNome = string.Empty;
            string ContatoTelefone = string.Empty;
            string ContatoEmail = string.Empty;
            try
            {
                g_clientes_contatos RecordContato = db.g_clientes_contatos.Find(view_g_clientes_contatos.id_contato);

                if (RecordContato != null)
                {
                    ContatoNome = RecordContato.contato.EmptyIfNull().ToString().Trim();
                    ContatoTelefone = RecordContato.telefone.EmptyIfNull().ToString().Trim();
                    ContatoEmail = RecordContato.email.EmptyIfNull().ToString().Trim().ToLowerInvariant();
                }
                Sucesso = true;
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, contato_telefone = ContatoTelefone, contato_email = ContatoEmail, contato_nome = ContatoNome }, JsonRequestBehavior.AllowGet);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null) { db.Dispose(); };
            }
            base.Dispose(disposing);
        }
        
        // DTO para mapear o retorno do SQL paginado
        private class ClienteGridRow
        {
            public int id_cliente { get; set; }
            public string nome { get; set; }
            public string razao_social { get; set; }
            public bool gc_produtor_rural { get; set; }
            public bool gc_consultor_aviacao { get; set; }
            public bool param_gc_transportadora { get; set; }
            public string cnpj { get; set; }
            public string cpf { get; set; }
            public bool ativo { get; set; }
            public bool is_cliente { get; set; }
            public bool is_fornecedor { get; set; }
            public DateTime datahora_cadastro { get; set; }
            public string vendedor_apelido { get; set; }
        }
    }

}