// Migrado em 2020_07_15

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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Requisicoes_*,g_Requisicoes_Default,g_PortalVendedor_Default")]
    public class RequisicoesController : Controller
    {
        private GdiPlataformEntities db;

        public RequisicoesController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Requisicoes_*,g_Requisicoes_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Central de Requisições";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Requisicoes_*,g_Requisicoes_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            const string filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.g_requisicoes>();
            allRecords = db.g_requisicoes.ToList();

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            displayedRecords.OrderBy(c => c.concluido).ThenBy(c => c.datahora_requisicao);

            var allRecordsVendedores = db.g_vendedores.Select(v => new { v.id_vendedor, v.nome }).ToList();
            var allRecordsRequisicoesTipos = db.g_requisicoes_tipos.Select(r => new { r.id_requisicao_tipo, r.nome }).ToList();

            List<string[]> list = new List<string[]>();
            foreach (var r in displayedRecords)
            {
                String nomeUsuario = string.Empty;
                var tipoRec = allRecordsRequisicoesTipos.FirstOrDefault(t => t.id_requisicao_tipo == r.id_requisicao_tipo);
                String tipoRequisicao = tipoRec != null ? tipoRec.nome.EmptyIfNull().ToString() : string.Empty;

                String status = r.concluido ? "Fechado" : "Aberto";

                if (r.tipo_solicitante != null && r.tipo_solicitante.Equals("V"))
                {
                    var vend = allRecordsVendedores.FirstOrDefault(u => u.id_vendedor == r.id_usuario_requisicao);
                    nomeUsuario = vend != null ? vend.nome.EmptyIfNull().ToString() : string.Empty;
                }
                nomeUsuario = nomeUsuario + " (" + r.tipo_solicitante.EmptyIfNull().ToString() + ")";
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    r.id_requisicao.ToString(),
                                    status, // Status                                    
                                    "<b> " + tipoRequisicao + " - </b>" + r.descricao_solicitacao.ToString(),
                                    nomeUsuario,
                                    r.datahora_requisicao.ToString("dd/MM/yy HH:mm")
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


        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Requisicoes_*,g_Requisicoes_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_requisicoes record_g_requisicoes = db.g_requisicoes.Find(id);
            if (record_g_requisicoes == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.TipoRequisicao = db.g_requisicoes_tipos.Find(record_g_requisicoes.id_requisicao_tipo).nome.EmptyIfNull().ToString();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Central de Requisições</b> - [Atendimento]";
            return View("CreateEdit", record_g_requisicoes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Requisicoes_*,g_Requisicoes_Actionupdate")]
        public ActionResult Edit(g_requisicoes record_g_requisicoes)
        {
            if (ModelState.IsValid)
            {
                g_requisicoes_tipos record_g_requisicoes_tipos = db.g_requisicoes_tipos.Where(r => r.id_requisicao_tipo == record_g_requisicoes.id_requisicao_tipo).FirstOrDefault();
                var allRecordsVendedores = db.g_vendedores.Select(v => new { v.id_vendedor, v.nome, v.email }).ToList();
                record_g_requisicoes.concluido = true;
                record_g_requisicoes.datahora_conclusao = LibDateTime.getDataHoraBrasilia();
                record_g_requisicoes.id_usuario_conclusao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_requisicoes).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();

                    // Notificação por Email da conclusão da requisição
                    String solicitanteNome = string.Empty;
                    String solicitanteEmail = string.Empty;
                    if (record_g_requisicoes.tipo_solicitante.Equals("V"))
                    {
                        solicitanteNome = allRecordsVendedores.Where(v => v.id_vendedor == record_g_requisicoes.id_usuario_requisicao).FirstOrDefault().nome.EmptyIfNull().ToString();
                        solicitanteEmail = allRecordsVendedores.Where(v => v.id_vendedor == record_g_requisicoes.id_usuario_requisicao).FirstOrDefault().email.EmptyIfNull().ToString();
                    }
                    if ((solicitanteNome.Trim().Length > 0) && (solicitanteEmail.Trim().Length > 0))
                    {
                        String MensagemEmail = string.Empty;
                        String AssuntoEmail = "ERP Web - Atendimento da Requisição Nº " + record_g_requisicoes.id_requisicao.ToString() + " - " + record_g_requisicoes_tipos.nome.EmptyIfNull().ToString();
                        MensagemEmail += "<b><u>##### REQUISIÇÃO #####</u></b><br/>";
                        MensagemEmail += "<b>Nº:</b> " + record_g_requisicoes.id_requisicao.ToString() + "<br/>";
                        MensagemEmail += "<b>Data/Hora: </b> " + record_g_requisicoes.datahora_requisicao.ToString("dd/MM/yyyy HH:mm") + "<br/>";
                        MensagemEmail += "<b>Tipo:</b> " + record_g_requisicoes_tipos.nome.EmptyIfNull().ToString() + "<br/>";
                        MensagemEmail += "<b>Solicitação:</b> " + record_g_requisicoes.descricao_solicitacao.EmptyIfNull().ToString() + "<br/>";
                        if (record_g_requisicoes.tipo_solicitante.Equals("V")) { MensagemEmail += "<b>Vendedor:</b> " + solicitanteNome + "<br/>"; };
                        MensagemEmail += "<br/>";
                        MensagemEmail += "<br/>";
                        MensagemEmail += "<b><u>##### ATENDIMENTO/SOLUÇÃO #####</u></b><br/>";
                        MensagemEmail += "<b>Data/Hora:</b> " + record_g_requisicoes.datahora_conclusao.GetValueOrDefault().ToString("dd/MM/yyyy HH:mm") + "<br/>";
                        MensagemEmail += "<b>Solução:</b> " + record_g_requisicoes.descricao_solucao.EmptyIfNull().ToString() + "<br/>";
                        MensagemEmail += "<b>Atendente:</b> " + CachePersister.userIdentity.Username.ToString() + "<br/>";
                        if (solicitanteEmail.Trim().Length > 0)
                        {
                            try
                            {
                                record_g_requisicoes.notificacao_conclusao = true;
                                db.Entry(record_g_requisicoes).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                            catch { };
                        }
                    }
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Central de Requisições</b> - [Atendimento]";
            return View("CreateEdit", record_g_requisicoes);
        }

        #region ModalSolicitarBloqueioLogon
        public ActionResult ModalSolicitarBloqueioLogon(String id)
        {
            ViewBag.Title = "Solicitação - Bloqueio de Logon";
            TempData.Remove("IdCliente");
            TempData["IdCliente"] = id;
            TempData.Keep("IdCliente");
            g_requisicoes record_g_requisicoes = new g_requisicoes();
            return View(record_g_requisicoes);
        }

        [HttpPost]
        public ActionResult AjaxSolicitarBloqueioLogon(g_requisicoes view_g_requisicoes)
        {
            bool sucesso = false;
            String msgRetorno = String.Empty;
            int qtdErros = 0;
            String tempIds = TempData["IdCliente"].ToString();

            // Persistir os dados na Sessão do Browser
            TempData.Remove("IdCliente");
            TempData["IdCliente"] = tempIds;
            TempData.Keep("IdCliente");

            try
            {
                if (view_g_requisicoes.descricao_solicitacao.EmptyIfNull().ToString().Length == 0)
                {
                    qtdErros += 1;
                    msgRetorno = msgRetorno + "<br/>" + "Campo [Descrição] é de preenchimento obrigatório";
                }

                if (qtdErros == 0) // Se não houver Erros
                {
                    g_clientes record_g_clientes = db.g_clientes.Find(int.Parse(tempIds));
                    string descricaoSolicitacao = "Vendedor (" + CachePersister.userIdentity.Username + ") solicita o bloqueio de logons do cliente (" + record_g_clientes.id_cliente.ToString() + " - " + record_g_clientes.nome.ToString() + ") Obs (" + view_g_requisicoes.descricao_solicitacao + ")";

                    // Cadastrar a nova requisição
                    g_requisicoes_tipos record_g_requisicoes_tipos = db.g_requisicoes_tipos.Where(p => p.id_requisicao_tipo == 2).FirstOrDefault();
                    g_requisicoes new_g_requisicoes = new g_requisicoes();
                    new_g_requisicoes.id_requisicao_tipo = 2; // Clientes - Bloquear Logon
                    new_g_requisicoes.tipo_solicitante = "V"; // Vendedor
                    new_g_requisicoes.concluido = false; // Aberto
                    new_g_requisicoes.id_perfil_responsavel = record_g_requisicoes_tipos.id_perfil_responsavel;
                    new_g_requisicoes.id_usuario_responsavel = record_g_requisicoes_tipos.id_usuario_responsavel;
                    new_g_requisicoes.descricao_solicitacao = descricaoSolicitacao;
                    new_g_requisicoes.datahora_requisicao = LibDateTime.getDataHoraBrasilia();
                    new_g_requisicoes.id_usuario_requisicao = CachePersister.userIdentity.IdVendedor;
                    new_g_requisicoes.notificacao_requisicao = true;
                    db.g_requisicoes.Add(new_g_requisicoes);
                    db.SaveChanges();

                    // Notificação da Requisição
                    g_coligadas record_g_coligada = db.g_coligadas.Find(1);
                    String MensagemEmail = string.Empty;
                    String AssuntoEmail = "ERP Web - Requisição Nº " + new_g_requisicoes.id_requisicao.ToString() + " - " + record_g_requisicoes_tipos.nome.EmptyIfNull().ToString();
                    MensagemEmail += "<b><u>##### REQUISIÇÃO #####</u></b><br/>";
                    MensagemEmail += "<br/>";
                    MensagemEmail += "<b>Nº:</b> " + new_g_requisicoes.id_requisicao.ToString() + "<br/>";
                    MensagemEmail += "<b>Tipo:</b> " + record_g_requisicoes_tipos.nome.EmptyIfNull().ToString() + "<br/>";
                    MensagemEmail += "<b>Descrição:</b> " + descricaoSolicitacao + "<br/>";
                    MensagemEmail += "<br/>";
                    MensagemEmail += "<b>Filial/Revenda:</b> " + CachePersister.userIdentity.FilialNome.ToString() + "<br/>";
                    MensagemEmail += "<b>Cliente (ID):</b> " + record_g_clientes.id_cliente.EmptyIfNull().ToString() + "<br/>";
                    MensagemEmail += "<b>Cliente (Nome):</b> " + record_g_clientes.nome.EmptyIfNull().ToString() + "<br/>";
                    MensagemEmail += "<b>Cliente (Doc.):</b> " + record_g_clientes.cpf.EmptyIfNull().ToString().Trim() + record_g_clientes.cnpj.EmptyIfNull().ToString().Trim() + "<br/>";
                    MensagemEmail += "<br/>";
                    MensagemEmail += "<b>Vendedor:</b> " + CachePersister.userIdentity.Username.ToString() + "<br/>";
                    MensagemEmail += "<b>Data/Hora:</b> " + record_g_clientes.datahora_cadastro.ToString("dd/MM/yyyy HH:mm") + "<br/>";
                    MensagemEmail += "<br/>";
                    String emailsAdm = record_g_coligada.email_adm.EmptyIfNull().ToString();
                    if (emailsAdm.Trim().Length > 0)
                    {
                        try 
                        { 
                            
                        } catch 
                        { };
                    }
                    sucesso = true;
                    msgRetorno += " Requisição <b>Transmitida</b> com sucesso!";
                }
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
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}