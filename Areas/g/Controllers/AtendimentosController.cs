using GdiPlataform.Areas.g.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Atendimentos_*,g_Atendimentos_Default")]
    public partial class AtendimentosController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Atendimentos";
        private readonly ILookupQueryService _lookupQueryService;

        public AtendimentosController() : this(null) { }

        /// <summary>Piloto 1.9.2 — injeção opcional; fallback via <see cref="LookupQueryServiceAccessor"/>.</summary>
        public AtendimentosController(ILookupQueryService lookupQueryService)
        {
            _lookupQueryService = lookupQueryService ?? LookupQueryServiceAccessor.Current;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            PreencherLookupsIndexAtendimentos();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-ticket", "", "", "") + LibStringFormat.GetTabHtml(1) + "Gestão de Atendimentos";
            g_atendimentos record_g_atendimentos = new Db.g_atendimentos();
            record_g_atendimentos.id_atendimento = 0;
            record_g_atendimentos.solicitacao_id_usuario = 0;
            record_g_atendimentos.id_status = 0;
            record_g_atendimentos.responsavel_id_departamento = 0;
            record_g_atendimentos.responsavel_id_usuario = 0;
            return View(record_g_atendimentos);
        }
        public ActionResult getDadosAtendimentos(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null)
            {
                param = new jQueryDataTableParamModel();
            }
            try
            {
            String SentencaSQL = string.Empty;
            List<Db.g_atendimentos> allRecords = new List<Db.g_atendimentos>();
            List<Db.g_usuarios> ListaUsuarios = db.g_usuarios.ToList();
            List<Db.g_departamentos> ListaDepartamentos = db.g_departamentos.ToList();
            List<Db.g_atendimentos_status> ListaAtendimentosStatus = db.g_atendimentos_status.ToList();
            List<Db.g_atendimentos_categorias> ListaAtendimentosCategorias = db.g_atendimentos_categorias.ToList();
            DateTime DataHoraAtual = GdiPlataform.Lib.LibDateTime.getDataHoraBrasilia();

            param.yesCustomField01 = param.yesCustomField01.EmptyIfNull().ToString().Trim();
            param.yesCustomField02 = param.yesCustomField02.EmptyIfNull().ToString().Trim();
            param.yesCustomField03 = param.yesCustomField03.EmptyIfNull().ToString().Trim();
            param.yesCustomField04 = param.yesCustomField04.EmptyIfNull().ToString().Trim();
            param.yesCustomField05 = param.yesCustomField05.EmptyIfNull().ToString().Trim();

            SentencaSQL = "select * from g_atendimentos where id_atendimento > 0";
            if (param.yesCustomField01 != String.Empty && param.yesCustomField01 != "0") { SentencaSQL += " and id_atendimento = " + param.yesCustomField01; };
            if (param.yesCustomField02 != String.Empty && param.yesCustomField02 != "0") { SentencaSQL += " and solicitante_id_usuario = " + param.yesCustomField02; };
            if (param.yesCustomField03 != String.Empty && param.yesCustomField03 == "0") { SentencaSQL += " and id_status in (1,2) "; }
            else if (param.yesCustomField03 != String.Empty && param.yesCustomField03 != "0") { SentencaSQL += " and id_status = " + param.yesCustomField03; };
            if (param.yesCustomField04 != String.Empty && param.yesCustomField04 != "0") { SentencaSQL += " and responsavel_id_departamento = " + param.yesCustomField04; };
            if (param.yesCustomField05 != String.Empty && param.yesCustomField05 != "0") { SentencaSQL += " and responsavel_id_usuario = " + param.yesCustomField05; };

            LibDB.setFilterByUser(SentencaSQL, controllerName, true, db);
            allRecords = db.g_atendimentos.SqlQuery(SentencaSQL.ToString()).ToList();

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_atendimentos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.param_id_cliente) :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.param_id_cliente); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.param_id_cliente); }
                }
            }

            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {
                String _Solicitante = String.Empty;
                String _CategoriaSolicitacao = String.Empty;
                String _StatusAtendimento = String.Empty;
                var ArraySolicitante = ListaUsuarios.Find(u => u.id_usuario == c.solicitacao_id_usuario);
                var ArrayDepartamento = ListaDepartamentos.Find(d => d.id_departamento == c.responsavel_id_departamento);
                var ArrayResponsavel = ListaUsuarios.Find(u => u.id_usuario == c.responsavel_id_usuario);
                var ArrayStatus = ListaAtendimentosStatus.Find(u => u.id_status == c.id_status);
                var ArrayAtendimentoCategoria = ListaAtendimentosCategorias.Find(cat => cat.id_atendimento_categoria == c.id_atendimento_categoria);

                if (c.id_status == 1) { _StatusAtendimento = LibIcons.getIcon("fa-solid fa-list-check", "Aberto", "gray", ""); }
                else if (c.id_status == 2) { _StatusAtendimento = LibIcons.getIcon("fa-solid fa-ticket", "Atendimento", "orange", ""); }
                else if (c.id_status == 3) { _StatusAtendimento = LibIcons.getIcon("fa-solid fa-lock", "Concluído", "green", ""); }
                else if (c.id_status == 4) { _StatusAtendimento = LibIcons.getIcon("fa-solid fa-circle-xmark", "Cancelado", "red", ""); };

                TimeSpan TempoCorrido = DataHoraAtual - c.datahora_cadastro;
                String _TempoCorrido = TempoCorrido.Hours.ToString("00") + ":" + TempoCorrido.Minutes.ToString("00");
                if (TempoCorrido.Days > 0) { _TempoCorrido = TempoCorrido.Days.ToString() + "d+ " + _TempoCorrido; };
                if (ArrayAtendimentoCategoria != null) { _CategoriaSolicitacao = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<span style='font-size: 75%;'>[ " + EncodeAtendimentoDisplay(ArrayAtendimentoCategoria.nome.EmptyIfNull().ToString().Trim()) + " ]</span>"; };

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_atendimento.ToString(),
                                    ((ArraySolicitante != null) ? LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(ArraySolicitante.nome.ToString())) : String.Empty),
                                    EncodeAtendimentoDisplay(c.solicitacao.EmptyIfNull().ToString()) + _CategoriaSolicitacao,
                                    ((ArrayDepartamento != null) ? EncodeAtendimentoDisplay(ArrayDepartamento.nome.ToString()) : String.Empty),
                                    ((ArrayResponsavel != null) ? EncodeAtendimentoDisplay(LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(ArrayResponsavel.nome.ToString()))) : "Todos"),
                                    _StatusAtendimento,
                                    c.solicitacao_datahora.ToString("dd/MM/yy"),
                                    ((c.concluido == false) ? _TempoCorrido : String.Empty),
                                    c.id_status.EmptyIfNull().ToString().Trim(),
                                    "" // Botão
                                });
            }
            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count,
                iTotalDisplayRecords = allRecords.Count,
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        #region GetDadosGedAtendimento
        public ActionResult GetDadosGedAtendimento(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            int IdTable = 0;
            int.TryParse(param.yesCustomIdPK, out IdTable);
            List<g_usuarios> ListaUsuarios = db.g_usuarios.Where(u => u.id_usuario > 0).ToList();
            List<ged_arquivos> ListaArquivosGed = db.ged_arquivos.Where(g => g.ativo == true && g.id_atendimento == IdTable).ToList();
            List<ged_arquivos_tipos> ListaArquivosGedTipos = db.ged_arquivos_tipos.Where(t => t.id_arquivo_tipo > 0).ToList();
            List<string[]> list = new List<string[]>();

            var displayedRecords = ListaArquivosGed.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.ged_arquivos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_arquivo) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.filename :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.descricao :
                                     "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            foreach (var RecordGed in displayedRecords)
            {
                String DataReferencia = String.Empty;
                String DescricaoTipoArquivo = String.Empty;
                String NomeUsuario = String.Empty;
                var uCad = ListaUsuarios.Where(u => u.id_usuario == RecordGed.id_usuario_cadastro).FirstOrDefault();
                if (uCad != null) { NomeUsuario = uCad.login.EmptyIfNull().ToString(); }
                if (RecordGed.datahora_cadastro != null) { DataReferencia = RecordGed.datahora_cadastro.GetValueOrDefault().ToString("dd/MM/yy"); }
                ;
                if (RecordGed.id_arquivo_tipo > 0)
                {
                    ged_arquivos_tipos RecordArquivoTipo = ListaArquivosGedTipos.Where(t => t.id_arquivo_tipo == RecordGed.id_arquivo_tipo).FirstOrDefault();
                    if (RecordArquivoTipo != null) { DescricaoTipoArquivo = RecordArquivoTipo.descricao.EmptyIfNull().ToString(); }
                    ;
                }

                list.Add(new[] {
                                    RecordGed.id_arquivo.ToString(),
                                    DescricaoTipoArquivo.ToString(),
                                    RecordGed.descricao.ToString(),
                                    RecordGed.filename.ToString(),
                                    DataReferencia,
                                    NomeUsuario,
                                    "" // Botão Download
                                });
            }

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = ListaArquivosGed.Count(),
                iTotalDisplayRecords = ListaArquivosGed.Count(),
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

        #region ModalCreateNewAtendimento
        public ActionResult ModalCreateNewAtendimento()
        {
            try
            {
                g_atendimentos record_g_atendimentos = new Db.g_atendimentos();
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Novo Atendimento</b>";
                record_g_atendimentos.concluido = false;
                record_g_atendimentos.privado = false;
                PreencherLookupsAtendimentoFormulario();
                record_g_atendimentos.id_status = 1;
                return View("ModalCreateNewAtendimento", record_g_atendimentos);
            }
            catch (Exception ex)
            {
                String msg = LibExceptions.getExceptionShortMessage(ex);
                msg += "<br/>" + "AtendimentosController";
                msg += "<br/>" + "ModalCreateNewAtendimento";
                TempData["message"] = msg;
                TempData.Keep("message");
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        public ActionResult AjaxCreateEditAtendimento(g_atendimentos view_g_atendimentos)
        {
            bool sucesso = false;
            int QtdAtividadesAutomaticas = 0;
            String MsgRetorno = "";
            String LogAtendimento = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            g_atendimentos record_g_atendimentos = new Db.g_atendimentos();
            try
            {
                if (ModelState.IsValid)
                {

                    if (view_g_atendimentos.id_atendimento == 0)
                    {
                        if (view_g_atendimentos.responsavel_id_departamento <= 0)
                        {
                            ModelState.AddModelError("Model", "Campo [Departamento] é de preenchimento obrigatório");
                        }
                        if (view_g_atendimentos.solicitacao.EmptyIfNull().ToString().Trim().Length <= 0)
                        {
                            ModelState.AddModelError("Model", "Campo [Solicitação] é de preenchimento obrigatório");
                        }
                        if (view_g_atendimentos.descricao.EmptyIfNull().ToString().Trim().Length <= 0)
                        {
                            ModelState.AddModelError("Model", "Campo [Descrição] é de preenchimento obrigatório");
                        }
                        if (view_g_atendimentos.id_atendimento_categoria <= 0)
                        {
                            ModelState.AddModelError("Model", "Campo [Categoria] é de preenchimento obrigatório");
                        }
                        else
                        {
                            g_atendimentos_categorias RecordCategoria = db.g_atendimentos_categorias.Find(view_g_atendimentos.id_atendimento_categoria);
                            if (RecordCategoria.param_id_cliente == 1 && view_g_atendimentos.param_id_cliente <= 0) { ModelState.AddModelError("Model", "Campo [Cliente] é de preenchimento obrigatório para a Categoria selecionada"); };
                            if (RecordCategoria.param_numero_pedido == 1 && view_g_atendimentos.param_numero_pedido <= 0) { ModelState.AddModelError("Model", "Campo [Nº Pedido] é de preenchimento obrigatório para a Categoria selecionada"); };
                            if (RecordCategoria.param_numero_nf == 1 && view_g_atendimentos.param_numero_nf <= 0) { ModelState.AddModelError("Model", "Campo [Nº Nota Fiscal] é de preenchimento obrigatório para a Categoria selecionada"); };
                            if (RecordCategoria.param_id_produto == 1 && view_g_atendimentos.param_id_produto <= 0) { ModelState.AddModelError("Model", "Campo [Produto] é de preenchimento obrigatório para a Categoria selecionada"); };
                            if (RecordCategoria.param_limite_credito == 1 && view_g_atendimentos.param_limite_credito <= 0) { ModelState.AddModelError("Model", "Campo [Limite Crédito] é de preenchimento obrigatório para a Categoria selecionada"); };
                            if (RecordCategoria.param_id_vendedor == 1 && view_g_atendimentos.param_id_vendedor <= 0) { ModelState.AddModelError("Model", "Campo [Vendedor] é de preenchimento obrigatório para a Categoria selecionada"); };
                        }
                    }
                    if (view_g_atendimentos.id_atendimento > 0)
                    {
                        if (view_g_atendimentos.responsavel_id_departamento <= 0)
                        {
                            ModelState.AddModelError("Model", "Campo [Departamento] é de preenchimento obrigatório");
                        }
                        if (view_g_atendimentos.responsavel_id_departamento <= 0 && view_g_atendimentos.responsavel_id_usuario <= 0)
                        {
                            ModelState.AddModelError("Model", "Campo [Departamento] ou [Operador] é de preenchimento obrigatório");
                        }
                        if (view_g_atendimentos.responsavel_id_usuario > 0 && view_g_atendimentos.responsavel_id_departamento > 0)
                        {
                            g_usuarios RecordResponsavelAtual = db.g_usuarios.Find(view_g_atendimentos.responsavel_id_usuario);
                            if (RecordResponsavelAtual.id_departamento != view_g_atendimentos.responsavel_id_departamento)
                            {
                                ModelState.AddModelError("Model", "Operador informado não pertence ao Departamento selecionado!");
                            }
                        }
                        if (view_g_atendimentos.anotacoes.EmptyIfNull().ToString().Trim().Length <= 0)
                        {
                            ModelState.AddModelError("Model", "Campo [Anotações] é de preenchimento obrigatório");
                        }

                        if (view_g_atendimentos.concluido == true)
                        {
                            List<Db.g_atendimentos_atividades> ListaAtividadesAbertas = db.g_atendimentos_atividades.Where(a => a.id_atendimento == view_g_atendimentos.id_atendimento && a.concluido == false).ToList();
                            if (ListaAtividadesAbertas.Count() > 0)
                            {
                                ModelState.AddModelError("Model", "Não é possível finalizar o atendimento, existem " + ListaAtividadesAbertas.Count().ToString() + " atividade(s) abertas!");
                            }
                        }
                    }
                    if (ModelState.IsValid == false)
                    {
                        MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    }
                }
                else
                {
                    MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        if (view_g_atendimentos.id_atendimento == 0)
                        {
                            record_g_atendimentos.concluido = view_g_atendimentos.concluido;
                            record_g_atendimentos.solicitacao = view_g_atendimentos.solicitacao;
                            record_g_atendimentos.descricao = view_g_atendimentos.descricao;
                            record_g_atendimentos.privado = false;
                            record_g_atendimentos.enviar_atualizacoes = false;
                            record_g_atendimentos.param_id_cliente = view_g_atendimentos.param_id_cliente;
                            record_g_atendimentos.param_numero_pedido = view_g_atendimentos.param_numero_pedido;
                            record_g_atendimentos.param_numero_nf = view_g_atendimentos.param_numero_nf;
                            record_g_atendimentos.param_id_produto = view_g_atendimentos.param_id_produto;
                            record_g_atendimentos.param_limite_credito = view_g_atendimentos.param_limite_credito;
                            record_g_atendimentos.param_id_vendedor = view_g_atendimentos.param_id_vendedor;
                            if (record_g_atendimentos.concluido == false) { record_g_atendimentos.id_status = 1; } else { record_g_atendimentos.id_status = 3; }
                            record_g_atendimentos.id_atendimento_categoria = view_g_atendimentos.id_atendimento_categoria;
                            record_g_atendimentos.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                            record_g_atendimentos.solicitacao_datahora = DataHoraAtual;
                            record_g_atendimentos.responsavel_id_usuario = 0;
                            record_g_atendimentos.responsavel_id_departamento = view_g_atendimentos.responsavel_id_departamento;
                            record_g_atendimentos.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            record_g_atendimentos.datahora_cadastro = DataHoraAtual;
                            db.g_atendimentos.Add(record_g_atendimentos);
                            db.SaveChanges();
                            MsgRetorno += "Atendimento Nº <b>" + record_g_atendimentos.id_atendimento.EmptyIfNull().ToString() + "</b> REGISTRADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";

                            LogAtendimento = String.Empty;
                            LogAtendimento += "<b>##### NOVO ATENDIMENTO #####</b>";
                            LogAtendimento += "<br/>Departamento: " + EncodeAtendimentoDisplay(db.g_departamentos.Find(view_g_atendimentos.responsavel_id_departamento).nome.EmptyIfNull().ToString());
                            LogAtendimento += "<br/>Categoria: " + EncodeAtendimentoDisplay(db.g_atendimentos_categorias.Find(view_g_atendimentos.id_atendimento_categoria).nome.EmptyIfNull().ToString());
                            LogAtendimento += "<br/>Solicitação: " + EncodeAtendimentoDisplay(record_g_atendimentos.solicitacao.EmptyIfNull().ToString());
                            LogAtendimento += "<br/>Descrição: " + EncodeForLogHtml(record_g_atendimentos.descricao.EmptyIfNull().ToString());

                            List<Db.g_atendimentos_categorias_atividades> ListaAtividadesPadroes = db.g_atendimentos_categorias_atividades.Where(a => a.id_atendimento_categoria == record_g_atendimentos.id_atendimento_categoria && a.ativo == true).OrderBy(a => a.ordem).ToList();
                            if (ListaAtividadesPadroes.Count > 0)
                            {
                                MsgRetorno += "<br/>" + "Atividades automáticas do atendimento:";
                                LogAtendimento += "<br/>Atividades inseridas: ";
                                foreach (g_atendimentos_categorias_atividades RecordAtividade in ListaAtividadesPadroes)
                                {
                                    g_atendimentos_atividades record_g_atendimentos_atividades = new Db.g_atendimentos_atividades();
                                    record_g_atendimentos_atividades.id_atendimento = record_g_atendimentos.id_atendimento;
                                    record_g_atendimentos_atividades.id_atendimento_categoria_atividade = RecordAtividade.id_atendimento_categoria_atividade;
                                    record_g_atendimentos_atividades.concluido = false;
                                    record_g_atendimentos_atividades.privado = false;
                                    record_g_atendimentos_atividades.solicitacao = RecordAtividade.atividade;
                                    record_g_atendimentos_atividades.ordem = RecordAtividade.ordem;
                                    record_g_atendimentos_atividades.sla_datahora = DataHoraAtual.AddDays(RecordAtividade.sla_dias);
                                    record_g_atendimentos_atividades.solicitacao_id_usuario = record_g_atendimentos.solicitacao_id_usuario;
                                    record_g_atendimentos_atividades.solicitacao_datahora = record_g_atendimentos.solicitacao_datahora;
                                    record_g_atendimentos_atividades.responsavel_id_usuario = 0;
                                    record_g_atendimentos_atividades.responsavel_id_departamento = record_g_atendimentos.responsavel_id_departamento;
                                    record_g_atendimentos_atividades.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                    record_g_atendimentos_atividades.datahora_cadastro = DataHoraAtual;
                                    db.g_atendimentos_atividades.Add(record_g_atendimentos_atividades);
                                    QtdAtividadesAutomaticas += 1;
                                    MsgRetorno += "<br/> -> " + EncodeAtendimentoDisplay(RecordAtividade.atividade.EmptyIfNull().ToString());
                                    LogAtendimento += "<br/> -> " + EncodeAtendimentoDisplay(RecordAtividade.atividade.EmptyIfNull().ToString());
                                }
                                db.SaveChanges();
                            }
                            SaveAtendimentoLog(record_g_atendimentos.id_atendimento, record_g_atendimentos.responsavel_id_departamento.GetValueOrDefault(), LogAtendimento, true);
                        }
                        else
                        {
                            LogAtendimento = String.Empty;
                            record_g_atendimentos = db.g_atendimentos.Find(view_g_atendimentos.id_atendimento);

                            if (record_g_atendimentos.concluido != view_g_atendimentos.concluido)
                            {
                                LogAtendimento += "<br/>Status: ";
                                if (record_g_atendimentos.concluido == false) { LogAtendimento += "Aberto > "; } else { LogAtendimento += "Fechado > "; }
                                if (view_g_atendimentos.concluido == false) { LogAtendimento += "Aberto"; } else { LogAtendimento += "Fechado"; }
                                
                                if (view_g_atendimentos.concluido == true) 
                                { 
                                    record_g_atendimentos.id_status = 3; 
                                    LogAtendimento += " (Atendimento Finalizado)"; 
                                }
                                else if (view_g_atendimentos.concluido == false)
                                {
                                    record_g_atendimentos.id_status = 2;
                                    LogAtendimento += " (Atendimento Reaberto)";
                                }
                            }
                            if (record_g_atendimentos.concluido == false && record_g_atendimentos.id_status == 1)
                            {
                                record_g_atendimentos.id_status = 2;
                                LogAtendimento += "<br/>Status: Aberto > Em Atendimento";
                            }

                            if (record_g_atendimentos.responsavel_id_usuario != view_g_atendimentos.responsavel_id_usuario)
                            {
                                g_usuarios RecordResponsavelAnterior = db.g_usuarios.Find(record_g_atendimentos.responsavel_id_usuario);
                                g_usuarios RecordResponsavelAtual = db.g_usuarios.Find(view_g_atendimentos.responsavel_id_usuario);
                                if (RecordResponsavelAnterior != null) { LogAtendimento += "<br/>Operador: " + EncodeAtendimentoDisplay(RecordResponsavelAnterior.nome.EmptyIfNull().ToString()); } else { LogAtendimento += "Operador: Todos"; };
                                if (RecordResponsavelAtual != null) 
                                { 
                                    LogAtendimento += " > " + EncodeAtendimentoDisplay(RecordResponsavelAtual.nome.EmptyIfNull().ToString());
                                    view_g_atendimentos.responsavel_id_departamento = RecordResponsavelAtual.id_departamento;
                                    LogAtendimento += " (Atendimento transferido para outro operador)";
                                } 
                                else 
                                { 
                                    LogAtendimento += "Todos"; 
                                };                                
                            }
                            if (record_g_atendimentos.responsavel_id_departamento != view_g_atendimentos.responsavel_id_departamento)
                            {
                                g_departamentos RecordDepartamentoAnterior = db.g_departamentos.Find(record_g_atendimentos.responsavel_id_departamento);
                                g_departamentos RecordDepartamentoAtual = db.g_departamentos.Find(view_g_atendimentos.responsavel_id_departamento);
                                LogAtendimento += "<br/>Departamento: " + EncodeAtendimentoDisplay(RecordDepartamentoAnterior.nome.EmptyIfNull().ToString()) + " > " + EncodeAtendimentoDisplay(RecordDepartamentoAtual.nome.EmptyIfNull().ToString());
                            }
                            if (record_g_atendimentos.anotacoes.EmptyIfNull().ToString().Trim() != view_g_atendimentos.anotacoes.EmptyIfNull().ToString().Trim())
                            {
                                LogAtendimento += "<br/>Anotações: " + EncodeForLogHtml(record_g_atendimentos.anotacoes.EmptyIfNull().ToString()) + " > " + EncodeForLogHtml(view_g_atendimentos.anotacoes.EmptyIfNull().ToString());
                            }
                            record_g_atendimentos.concluido = view_g_atendimentos.concluido;
                            record_g_atendimentos.anotacoes = view_g_atendimentos.anotacoes;
                            record_g_atendimentos.param_id_cliente = view_g_atendimentos.param_id_cliente;
                            record_g_atendimentos.param_numero_pedido = view_g_atendimentos.param_numero_pedido;
                            record_g_atendimentos.param_numero_nf = view_g_atendimentos.param_numero_nf;
                            record_g_atendimentos.param_id_produto = view_g_atendimentos.param_id_produto;
                            record_g_atendimentos.param_limite_credito = view_g_atendimentos.param_limite_credito;
                            record_g_atendimentos.param_id_vendedor = view_g_atendimentos.param_id_vendedor;
                            record_g_atendimentos.responsavel_id_departamento = view_g_atendimentos.responsavel_id_departamento;
                            record_g_atendimentos.responsavel_id_usuario = view_g_atendimentos.responsavel_id_usuario;
                            record_g_atendimentos.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            record_g_atendimentos.datahora_alteracao = DataHoraAtual;

                            if (LogAtendimento.EmptyIfNull().ToString().Trim().Length > 0) 
                            {
                                db.Entry(record_g_atendimentos).State = EntityState.Modified;
                                db.SaveChanges();
                                if (record_g_atendimentos.concluido == true) 
                                {
                                    MsgRetorno = "Atendimento Nº <b>" + record_g_atendimentos.id_atendimento.EmptyIfNull().ToString() + "</b> FINALIZADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                                    LogAtendimento = "<b>##### FINALIZAÇÃO DO ATENDIMENTO #####</b>" + LogAtendimento;
                                }
                                else
                                {
                                    MsgRetorno = "Atendimento Nº <b>" + record_g_atendimentos.id_atendimento.EmptyIfNull().ToString() + "</b> ATUALIZADO com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                                    LogAtendimento = "<b>##### ATUALIZAÇÃO DO ATENDIMENTO #####</b>" + LogAtendimento;
                                }
                                SaveAtendimentoLog(record_g_atendimentos.id_atendimento, record_g_atendimentos.responsavel_id_departamento.GetValueOrDefault(), LogAtendimento, true);
                            };
                        };
                        sucesso = true;
                    }
                    catch (DbEntityValidationException ex)
                    {
                        sucesso = false;
                        MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
                    }
                    catch (Exception e)
                    {
                        sucesso = false;
                        MsgRetorno = LibExceptions.getExceptionShortMessage(e);
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Pedido - Anexos - Modal UploadFile
        public ActionResult ModalUploadFileAtendimentos(int? IdAtendimento, string tag)
        {
            CachePersister.userIdentity.FormNameActive = tag;
            CstUploadGed record_cstUploadGed = new CstUploadGed();
            {
                record_cstUploadGed.isAtendimento = true;
                record_cstUploadGed.id_atendimento = IdAtendimento.GetValueOrDefault();
                var ComboGedTipos = new List<SelectListItem>();
                ComboGedTipos.Add(new SelectListItem { Value = "0", Text = "[ SELECIONE O TIPO DO ANEXO ]" });
                List<ged_arquivos_tipos> ListaGedTipos = db.ged_arquivos_tipos.Where(g => g.id_arquivo_tipo == 37).OrderBy(p => p.descricao).ToList();
                foreach (var RecordGedTipo in ListaGedTipos) { ComboGedTipos.Add(new SelectListItem { Value = RecordGedTipo.id_arquivo_tipo.ToString(), Text = RecordGedTipo.descricao }); };
                ViewBag.ComboGedTipos = ComboGedTipos;
                record_cstUploadGed.id_arquivo_tipo = 37;
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Documentos</b>";
            }
            return View(record_cstUploadGed);
        }
        #endregion

        #region AjaxGetLookupCategorias
        public ActionResult AjaxGetLookupCategorias(g_departamentos record_g_departamentos)
        {
            var ListaCategorias = db.g_atendimentos_categorias
                .AsNoTracking()
                .Where(c => c.id_departamento == record_g_departamentos.id_departamento)
                .OrderBy(c => c.nome)
                .Select(c => new
                {
                    id_atendimento_categoria = c.id_atendimento_categoria,
                    nome = c.nome
                })
                .ToList();
            return Json(ListaCategorias, JsonRequestBehavior.AllowGet);
        }
        #endregion
        public bool SaveAtendimentoLog(int IdAtendimento, int IdDepartamento, String Log, bool LogAutomatico)
        {
            bool Sucess = false;
            try
            {
                g_atendimentos_logs record_g_atendimentos_logs = new Db.g_atendimentos_logs();
                record_g_atendimentos_logs.id_atendimento = IdAtendimento;
                record_g_atendimentos_logs.log_automatico = LogAutomatico;
                record_g_atendimentos_logs.privado = false;
                record_g_atendimentos_logs.log = Log;
                record_g_atendimentos_logs.responsavel_id_usuario = CachePersister.userIdentity.IdUsuario;
                record_g_atendimentos_logs.responsavel_id_departamento = IdDepartamento;
                record_g_atendimentos_logs.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_atendimentos_logs.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.g_atendimentos_logs.Add(record_g_atendimentos_logs);
                db.SaveChanges();
                Sucess = true;
            }
            catch (Exception)
            {
                Sucess = false;
            }
            return Sucess;
        }

        #region Edit Record
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Atendimentos_*,g_Atendimentos_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            try
            {
                if (id == null)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    g_atendimentos record_g_atendimentos = db.g_atendimentos.Find(id);
                    ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Atendimento</b>" + LibStringFormat.GetTabHtml(1) + record_g_atendimentos.id_atendimento.EmptyIfNull().ToString() + " - " + EncodeAtendimentoDisplay(record_g_atendimentos.solicitacao.EmptyIfNull().ToString());
                    PreencherLookupsAtendimentoEdit();
                    ViewBag.MsgCategoria = EncodeAtendimentoDisplay(db.g_atendimentos_categorias.Find(record_g_atendimentos.id_atendimento_categoria).nome.EmptyIfNull().ToString());
                    ViewBag.MsgCategoria += " (Solicitante: " + EncodeAtendimentoDisplay(db.g_usuarios.Find(record_g_atendimentos.solicitacao_id_usuario).nome.EmptyIfNull().ToString()) + ")";
                    return View("Edit", record_g_atendimentos);
                }
            }
            catch (Exception ex)
            {
                String msg = LibExceptions.getExceptionShortMessage(ex);
                msg += "<br/>" + "AtendimentosController";
                msg += "<br/>" + "Edit";
                TempData["message"] = msg;
                TempData.Keep("message");
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        #endregion

        #region getDadosAtividades
        public ActionResult getDadosAtividades(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null)
            {
                param = new jQueryDataTableParamModel();
            }
            try
            {
            int IdAtendimento = -1;
            int.TryParse(param.yesCustomIdPK, out IdAtendimento);

            var allRecords = db.g_atendimentos_atividades.Where(a => a.id_atendimento == IdAtendimento).OrderBy(a => a.ordem).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            var allOperadores = db.g_usuarios.ToList();


            List<string[]> list = new List<string[]>();
            foreach (var RecordAtividade in displayedRecords)
            {
                String IconeStatus = String.Empty;
                String NomeOperador = String.Empty;
                String DataSLA = String.Empty;
                String DataFinalizacao = String.Empty;

                if (RecordAtividade.concluido == true)
                {
                    IconeStatus = LibIcons.getIcon("fa-solid fa-lock", "Fechado", "", "");
                    DataFinalizacao = Convert.ToDateTime(RecordAtividade.conclusao_datahora, new CultureInfo("en-US")).ToString("dd/MM/yy");
                    if (DataFinalizacao.EmptyIfNull().ToString() == "01/01/01") { DataFinalizacao = ""; };
                }
                else
                {
                    IconeStatus = LibIcons.getIcon("fa-solid fa-list-check", "Aberto", "", "");
                    DataSLA = Convert.ToDateTime(RecordAtividade.sla_datahora, new CultureInfo("en-US")).ToString("dd/MM/yy");
                    if (DataSLA.EmptyIfNull().ToString() == "01/01/01") { DataSLA = ""; };
                }
                
                if (RecordAtividade.responsavel_id_usuario > 0)
                {
                    var op = allOperadores.Find(o => o.id_usuario == RecordAtividade.responsavel_id_usuario);
                    NomeOperador = op != null ? op.login.EmptyIfNull().ToString() : String.Empty;
                }
                list.Add(new[] {
                                    RecordAtividade.id_atendimento_atividade.EmptyIfNull().ToString(),
                                    IconeStatus,
                                    EncodeAtendimentoDisplay(RecordAtividade.solicitacao.EmptyIfNull().ToString()),
                                    DataSLA,
                                    DataFinalizacao,
                                    "",
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

        public ActionResult ModalCreateEditAtividade(int? IdAtendimento, int? IdAtendimentoAtividade)
        {
            try
            {
                if ((IdAtendimento == null) || (IdAtendimentoAtividade == null))
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    g_atendimentos_atividades record_g_atendimentos_atividades = new Db.g_atendimentos_atividades();
                    if (IdAtendimentoAtividade > 0)
                    {
                        record_g_atendimentos_atividades = db.g_atendimentos_atividades.Find(IdAtendimentoAtividade);
                        record_g_atendimentos_atividades.privado = false;
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Atividade</b>" + LibStringFormat.GetTabHtml(1) + EncodeAtendimentoDisplay(record_g_atendimentos_atividades.solicitacao.EmptyIfNull().ToString());
                    }
                    else
                    {
                        ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Nova Atividade</b>";
                        record_g_atendimentos_atividades.id_atendimento = IdAtendimento.GetValueOrDefault();
                        record_g_atendimentos_atividades.privado = false;
                    }
                    return View("ModalCreateEditAtividade", record_g_atendimentos_atividades);
                }
            }
            catch (Exception ex)
            {
                String msg = LibExceptions.getExceptionShortMessage(ex);
                msg += "<br/>" + "AtendimentosController";
                msg += "<br/>" + "ModalCreateAtividade";
                TempData["message"] = msg;
                TempData.Keep("message");
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }

        public ActionResult AjaxCreateEditAtividade(g_atendimentos_atividades view_g_atendimentos_atividades)
        {
            bool sucesso = false;
            String MsgRetorno = "";
            DateTime DataHoraAtual = GdiPlataform.Lib.LibDateTime.getDataHoraBrasilia();
            g_atendimentos_atividades record_g_atendimentos_atividades = new Db.g_atendimentos_atividades();
            g_atendimentos RecordAtendimento = db.g_atendimentos.Find(view_g_atendimentos_atividades.id_atendimento);

            try
            {
                if (ModelState.IsValid)
                {
                    if (view_g_atendimentos_atividades.id_atendimento_atividade == 0)
                    {
                        if (view_g_atendimentos_atividades.solicitacao.EmptyIfNull().ToString().Trim().Length == 0) { ModelState.AddModelError("Model", "Campo [Solicitação] é de preenchimento obrigatório"); }
                        if (view_g_atendimentos_atividades.descricao.EmptyIfNull().ToString().Trim().Length == 0) { ModelState.AddModelError("Model", "Campo [Descrição] é de preenchimento obrigatório"); }
                    }
                    else
                    {
                        if (view_g_atendimentos_atividades.anotacoes.EmptyIfNull().ToString().Trim().Length == 0) { ModelState.AddModelError("Model", "Campo [Anotações] é de preenchimento obrigatório"); }
                    }
                    if (ModelState.IsValid == false) { MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>")); };
                }
                else
                {
                    MsgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }

                if (ModelState.IsValid)
                {
                    String LogAtendimento = String.Empty;

                    if (view_g_atendimentos_atividades.id_atendimento_atividade == 0)
                    {
                        record_g_atendimentos_atividades.concluido = view_g_atendimentos_atividades.concluido;
                        record_g_atendimentos_atividades.privado = false;
                        record_g_atendimentos_atividades.id_atendimento = view_g_atendimentos_atividades.id_atendimento;
                        record_g_atendimentos_atividades.ordem = view_g_atendimentos_atividades.ordem;
                        record_g_atendimentos_atividades.solicitacao = view_g_atendimentos_atividades.solicitacao;
                        record_g_atendimentos_atividades.descricao = view_g_atendimentos_atividades.descricao;
                        record_g_atendimentos_atividades.solicitacao_id_usuario = CachePersister.userIdentity.IdUsuario;
                        record_g_atendimentos_atividades.solicitacao_datahora = DataHoraAtual;
                        record_g_atendimentos_atividades.responsavel_id_usuario = 0;
                        record_g_atendimentos_atividades.responsavel_id_departamento = view_g_atendimentos_atividades.responsavel_id_departamento;
                        record_g_atendimentos_atividades.id_atendimento_categoria_atividade = view_g_atendimentos_atividades.id_atendimento_categoria_atividade;
                        record_g_atendimentos_atividades.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        record_g_atendimentos_atividades.datahora_cadastro = DataHoraAtual;
                        db.g_atendimentos_atividades.Add(record_g_atendimentos_atividades);
                        db.SaveChanges();
                        if (record_g_atendimentos_atividades.concluido == false) 
                        { 
                            LogAtendimento += "<br/> Status: Aberto"; 
                        } 
                        else 
                        {
                            LogAtendimento += "<br/>Status: Finalizada";
                            record_g_atendimentos_atividades.conclusao_datahora = DataHoraAtual;
                            record_g_atendimentos_atividades.conclusao_id_usuario = CachePersister.userIdentity.IdUsuario;
                        }
                        if (RecordAtendimento.concluido == false && RecordAtendimento.id_status == 1)
                        {
                            String SentencaSQL = " update g_atendimentos set id_status = 2 where id_atendimento = " + RecordAtendimento.id_atendimento.EmptyIfNull().ToString() + "; ";
                            LibDB.dbQueryExec(SentencaSQL, db);
                            LogAtendimento += "<br/> Status: Aberto > Em Atendimento";
                        }
                        LogAtendimento += "<br/>Solicitação: " + EncodeAtendimentoDisplay(record_g_atendimentos_atividades.solicitacao.EmptyIfNull().ToString());
                        LogAtendimento += "<br/>Descrição: " + EncodeForLogHtml(record_g_atendimentos_atividades.descricao.EmptyIfNull().ToString());
                        db.SaveChanges();
                        if (record_g_atendimentos_atividades.concluido == true)
                        {
                            MsgRetorno = "Atividade Nº <b>" + record_g_atendimentos_atividades.id_atendimento_atividade.EmptyIfNull().ToString() + "</b> FINALIZADA com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                            LogAtendimento = "<b>##### NOVA ATIVIDADE - FINALIZADA #####</b>" + LogAtendimento;
                        }
                        else
                        {
                            MsgRetorno = "Atividade Nº <b>" + record_g_atendimentos_atividades.id_atendimento_atividade.EmptyIfNull().ToString() + "</b> CADASTRADA com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                            LogAtendimento = "<b>##### NOVA ATIVIDADE #####</b>" + LogAtendimento;
                        }
                        SaveAtendimentoLog(record_g_atendimentos_atividades.id_atendimento, record_g_atendimentos_atividades.responsavel_id_departamento.GetValueOrDefault(), LogAtendimento, true);
                        sucesso = true;
                    }
                    else
                    {
                        record_g_atendimentos_atividades = db.g_atendimentos_atividades.Find(view_g_atendimentos_atividades.id_atendimento_atividade);

                        if (record_g_atendimentos_atividades.concluido != view_g_atendimentos_atividades.concluido)
                        {
                            LogAtendimento += "<br/>Status: ";
                            if (record_g_atendimentos_atividades.concluido == false) { LogAtendimento += "Aberto > "; } else { LogAtendimento += "Fechado > "; }
                            if (view_g_atendimentos_atividades.concluido == false) { LogAtendimento += "Aberto"; } else { LogAtendimento += "Fechado"; }
                        }
                        if (RecordAtendimento.concluido == false && RecordAtendimento.id_status == 1)
                        {
                            String SentencaSQL = " update g_atendimentos set id_status = 2 where id_atendimento = " + RecordAtendimento.id_atendimento.EmptyIfNull().ToString() + "; ";
                            LibDB.dbQueryExec(SentencaSQL, db);
                            LogAtendimento += "<br/>Status: Aberto > Em Atendimento";
                        }
                        if (view_g_atendimentos_atividades.concluido == true)
                        {
                            LogAtendimento += "<br/>Status: Finalizada";
                            record_g_atendimentos_atividades.conclusao_datahora = DataHoraAtual;
                            record_g_atendimentos_atividades.conclusao_id_usuario = CachePersister.userIdentity.IdUsuario;
                        }
                        if (record_g_atendimentos_atividades.anotacoes.EmptyIfNull().ToString().Trim() != view_g_atendimentos_atividades.anotacoes.EmptyIfNull().ToString().Trim())
                        {
                            LogAtendimento += "<br/>Anotações: " + EncodeForLogHtml(record_g_atendimentos_atividades.anotacoes.EmptyIfNull().ToString()) + " > " + EncodeForLogHtml(view_g_atendimentos_atividades.anotacoes.EmptyIfNull().ToString());
                        }

                        if (LogAtendimento.EmptyIfNull().ToString().Length > 0)
                        {
                            record_g_atendimentos_atividades.concluido = view_g_atendimentos_atividades.concluido;
                            record_g_atendimentos_atividades.anotacoes = view_g_atendimentos_atividades.anotacoes;
                            record_g_atendimentos_atividades.datahora_alteracao = DataHoraAtual;
                            record_g_atendimentos_atividades.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_g_atendimentos_atividades).State = EntityState.Modified;
                            db.SaveChanges();
                            if (record_g_atendimentos_atividades.concluido == true)
                            {
                                MsgRetorno = "Atividade Nº <b>" + record_g_atendimentos_atividades.id_atendimento_atividade.EmptyIfNull().ToString() + "</b> FINALIZADA com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                                LogAtendimento = "<b>##### FINALIZAÇÃO DA ATIVIDADE #####</b>" + "<br/>Atividade: " + EncodeAtendimentoDisplay(record_g_atendimentos_atividades.solicitacao.EmptyIfNull().ToString()) + LogAtendimento;
                            }
                            else
                            {
                                MsgRetorno = "Atividade Nº <b>" + record_g_atendimentos_atividades.id_atendimento_atividade.EmptyIfNull().ToString() + "</b> ATUALIZADA com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/>";
                                LogAtendimento = "<b>##### ATUALIZAÇÃO DA ATIVIDADE #####</b>" + "<br/>Atividade: " + EncodeAtendimentoDisplay(record_g_atendimentos_atividades.solicitacao.EmptyIfNull().ToString()) + LogAtendimento;
                            }
                            SaveAtendimentoLog(record_g_atendimentos_atividades.id_atendimento, record_g_atendimentos_atividades.responsavel_id_departamento.GetValueOrDefault(), LogAtendimento, true);
                            sucesso = true;
                        };
                    };
                };
                
            }
            catch (DbEntityValidationException ex)
            {
                sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region getDadosLogs
        public ActionResult getDadosAtendimentosLogs(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null)
            {
                param = new jQueryDataTableParamModel();
            }
            try
            {
            int IdAtendimento = -1;
            int.TryParse(param.yesCustomIdPK, out IdAtendimento);

            var allRecords = db.g_atendimentos_logs.Where(l => l.id_atendimento == IdAtendimento && l.log_automatico == true).OrderBy(l => l.datahora_cadastro).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            List<Db.g_usuarios> ListaUsuarios = db.g_usuarios.ToList();
            List<Db.g_departamentos> ListaDepartamentos = db.g_departamentos.ToList();

            String _status = String.Empty;
            String _Operador = String.Empty;

            List<string[]> list = new List<string[]>();
            foreach (var l in displayedRecords)
            {
                String _DataUsuario = l.datahora_cadastro.ToString("dd/MM/yy HH:mm");
                var ArrayUsuario = ListaUsuarios.Find(u => u.id_usuario == l.id_usuario_cadastro);
                g_usuarios RecordUsuario = ListaUsuarios.Where(u => u.id_usuario == l.id_usuario_cadastro).FirstOrDefault();
                if (ArrayUsuario != null) { _DataUsuario += "<br/>" + EncodeAtendimentoDisplay(LibStringFormat.ToTitleCase(LibStringFormat.GetPrimeiroNome(ArrayUsuario.nome.ToString()))); };
                String _Log = l.log.EmptyIfNull().ToString().Trim().Replace("\r\n", "<br/>");
                list.Add(new[] {
                                    l.id_atendimento_log.EmptyIfNull().ToString(),
                                    _DataUsuario,
                                    _Log,
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

        private static string EncodeAtendimentoDisplay(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return String.Empty;
            }
            return HttpUtility.HtmlEncode(value);
        }

        private static string EncodeForLogHtml(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return String.Empty;
            }
            return HttpUtility.HtmlEncode(value.Trim())
                .Replace("\r\n", "<br/>")
                .Replace("\n", "<br/>")
                .Replace("\r", "<br/>");
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
    }
}