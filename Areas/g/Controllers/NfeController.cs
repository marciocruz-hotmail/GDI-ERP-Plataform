using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Robos.ENotas;
using GdiPlataform.Security;
using Newtonsoft.Json.Linq;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Nfe_*,g_Nfe_Default")]
    public class NfeController : Controller
    {
        private GdiPlataformEntities db;
        private readonly string controllerName = "g_Nfe";

        public NfeController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Nfe_*,g_Nfe_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Notas Fiscais Eletrônicas (NFe)";
            return View(Enumerable.Empty<g_nfe>());
        }

        #region Edit / Create

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Nfe_*,g_Nfe_Actionread")]
        public ActionResult Edit(int? id)
        {
            if (id == null || id <= 0)
            {
                return RedirectToAction("Index");
            }
            g_nfe record = db.g_nfe.Find(id);
            if (record == null)
            {
                return RedirectToAction("Index");
            }
            PreencherLookupsCreateEdit(record);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>NFe</b>" + LibStringFormat.GetTabHtml(1) + record.id_nfe + " - " + record.nome.EmptyIfNull();
            return View("CreateEdit", record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Nfe_*,g_Nfe_Actioncreate,g_Nfe_Actionupdate")]
        public ActionResult Edit(g_nfe viewModel)
        {
            try
            {
                if (viewModel == null)
                {
                    return RedirectToAction("Index");
                }

                if (viewModel.id_nfe > 0)
                {
                    g_nfe entity = db.g_nfe.Find(viewModel.id_nfe);
                    if (entity == null)
                    {
                        return RedirectToAction("Index");
                    }
                    db.Entry(entity).CurrentValues.SetValues(viewModel);
                    entity.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                    entity.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.SaveChanges();
                    return RedirectToAction("Edit", new { id = entity.id_nfe });
                }

                g_nfe_config cfg = db.g_nfe_config.OrderBy(c => c.id_nfe_config).FirstOrDefault();
                if (cfg == null)
                {
                    TempData["message"] = "Não há configuração de NFe (g_nfe_config). Cadastre antes de incluir notas.";
                    return RedirectToAction("Index");
                }
                viewModel.id_nfe_config = cfg.id_nfe_config;
                viewModel.id_filial = cfg.id_filial;
                viewModel.id_coligada = cfg.id_coligada;
                if (viewModel.id_nfe_status <= 0)
                {
                    viewModel.id_nfe_status = 1;
                }
                viewModel.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                viewModel.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.g_nfe.Add(viewModel);
                db.SaveChanges();
                return RedirectToAction("Edit", new { id = viewModel.id_nfe });
            }
            catch (DbEntityValidationException ex)
            {
                TempData["message"] = LibExceptions.getDbEntityValidationException(ex);
                if (viewModel != null && viewModel.id_nfe > 0) { return RedirectToAction("Edit", new { id = viewModel.id_nfe }); }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["message"] = LibExceptions.getExceptionShortMessage(ex);
                if (viewModel != null && viewModel.id_nfe > 0) { return RedirectToAction("Edit", new { id = viewModel.id_nfe }); }
                return RedirectToAction("Index");
            }
        }

        private void PreencherLookupsCreateEdit(g_nfe record)
        {
            var comboCidade = new List<SelectListItem>();
            foreach (g_cidades item in db.g_cidades.Where(p => p.ativo == true).OrderBy(p => p.nome))
            {
                comboCidade.Add(new SelectListItem { Value = item.id_cidade.ToString(), Text = item.nome.ToString() });
            }
            ViewBag.comboCidade = comboCidade;

            var comboUF = new List<SelectListItem>();
            foreach (g_uf item in db.g_uf.OrderBy(p => p.sigla))
            {
                comboUF.Add(new SelectListItem { Value = item.id_uf.ToString(), Text = item.sigla.ToString() });
            }
            ViewBag.comboUF = comboUF;

            g_nfe_status st = db.g_nfe_status.FirstOrDefault(s => s.id_nfe_status == record.id_nfe_status);
            ViewBag.NfeStatus = st != null ? st.descricao.EmptyIfNull().ToString() : String.Empty;
            ViewBag.NfeKey = record.nfe_key.EmptyIfNull().ToString();
            ViewBag.UrlPDF = record.url_pdf.EmptyIfNull().ToString();
            ViewBag.UrlXML = record.url_xml.EmptyIfNull().ToString();
        }

        #endregion

        #region GetDados

        [HttpPost]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Nfe_*,g_Nfe_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
                if (db == null)
                {
                    return JsonDataTableException(new InvalidOperationException("Banco de dados não inicializado."), param, filterOnOff);
                }

                bool filterAdvanced = param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0;
                g_filtros recordFiltro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
                bool filterDb = recordFiltro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0;

                List<g_nfe> allRecords = new List<g_nfe>();
                var statusList = db.g_nfe_status.AsNoTracking().ToList();

                if (filterAdvanced)
                {
                    filterOnOff = "1";
                    string[] campos = param.yesFilterAdvancedText.EmptyIfNull().ToString().Split(';');
                    IQueryable<g_nfe> q = db.g_nfe.AsNoTracking().Where(n => n.id_nfe > 0);
                    if (campos.Length >= 8)
                    {
                        string sid = campos[0].Trim();
                        string nome = campos[1].Trim();
                        string cnpj = campos[2].Trim();
                        string cpf = campos[3].Trim();
                        string tipo = campos[4].Trim();
                        string idStatus = campos[5].Trim();
                        string d1s = campos[6].Trim();
                        string d2s = campos[7].Trim();
                        if (!string.IsNullOrEmpty(sid) && sid != "0" && int.TryParse(sid, out int idNfe)) { q = q.Where(n => n.id_nfe == idNfe); }
                        if (LibStringFormat.TryMontarPadraoLikeContemTexto(nome, out string padraoNome))
                        {
                            q = q.Where(n => n.nome != null && System.Data.Entity.DbFunctions.Like(n.nome, padraoNome));
                        }
                        if (LibStringFormat.TryMontarPadraoLikeContemCodigo(cnpj, out string padraoCnpj))
                        {
                            q = q.Where(n => n.cnpj != null && System.Data.Entity.DbFunctions.Like(n.cnpj, padraoCnpj));
                        }
                        if (LibStringFormat.TryMontarPadraoLikeContemCodigo(cpf, out string padraoCpf))
                        {
                            q = q.Where(n => n.cpf != null && System.Data.Entity.DbFunctions.Like(n.cpf, padraoCpf));
                        }
                        if (!string.IsNullOrEmpty(tipo) && tipo != "0") { q = q.Where(n => n.tipo_r_c == tipo); }
                        if (!string.IsNullOrEmpty(idStatus) && idStatus != "0" && int.TryParse(idStatus, out int idSt)) { q = q.Where(n => n.id_nfe_status == idSt); }
                        DateTime d1, d2;
                        if (DateTime.TryParse(d1s, new CultureInfo("pt-BR"), DateTimeStyles.None, out d1)
                            && DateTime.TryParse(d2s, new CultureInfo("pt-BR"), DateTimeStyles.None, out d2))
                        {
                            DateTime fim = d2.Date.AddDays(1).AddTicks(-1);
                            q = q.Where(n => n.data_vencimento >= d1 && n.data_vencimento <= fim);
                        }
                    }
                    allRecords = q.OrderByDescending(n => n.id_nfe).ToList();
                }
                else if (filterDb)
                {
                    filterOnOff = "1";
                    string sentenca = recordFiltro.advanced
                        ? recordFiltro.sql_filtro.Trim()
                        : "select * from g_nfe where id_nfe > 0 and " + recordFiltro.sql_filtro;
                    LibDB.setFilterByUser(sentenca, controllerName, true, db);
                    allRecords = db.g_nfe.SqlQuery(sentenca).ToList();
                }
                else if (!param.yesFilterField.EmptyIfNull().ToString().Trim().Equals(String.Empty)
                    && !param.yesFilterField.Trim().Equals("*")
                    && !param.yesFilterOperador.EmptyIfNull().ToString().Trim().Equals(String.Empty))
                {
                    filterOnOff = "1";
                    string sql = "select * from g_nfe where id_nfe > 0 and ";
                    sql += LibStringFormat.SentencaSQLFiltroGenerico(param.yesFilterField, param.yesFilterOperador, param.yesFilterText);
                    LibDB.setFilterByUser(sql, controllerName, true, db);
                    allRecords = db.g_nfe.SqlQuery(sql).ToList();
                }
                else
                {
                    allRecords = db.g_nfe.AsNoTracking().Where(n => n.id_nfe > 0).OrderByDescending(n => n.id_nfe).ToList();
                }

                var displayed = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength <= 0 ? 20 : param.iDisplayLength);
                var ci = CultureInfo.GetCultureInfo("pt-BR");
                var list = new List<string[]>();

                foreach (g_nfe n in displayed)
                {
                    g_nfe_status st = statusList.FirstOrDefault(s => s.id_nfe_status == n.id_nfe_status);
                    string statusLabel = String.Empty;
                    if (st != null)
                    {
                        if (st.nf_autorizada) { statusLabel = "Autorizada"; }
                        else if (st.nf_cancelada) { statusLabel = "Cancelada"; }
                        else { statusLabel = st.descricao_resumida.EmptyIfNull().ToString(); if (string.IsNullOrEmpty(statusLabel)) { statusLabel = st.descricao.EmptyIfNull().ToString(); } }
                    }
                    statusLabel = SanitizeDtCell(statusLabel);
                    string nomeCliente = SanitizeDtCell(n.nome.EmptyIfNull().ToString());
                    string doc = !string.IsNullOrWhiteSpace(n.cnpj.EmptyIfNull().ToString()) ? SanitizeDtCell(n.cnpj) : SanitizeDtCell(n.cpf.EmptyIfNull().ToString());
                    string tipo = SanitizeDtCell(n.tipo_r_c.EmptyIfNull().ToString());
                    string valor = string.Format(ci, "{0:C}", n.valor_total_liquido).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                    string urlPdf = n.url_pdf.EmptyIfNull().ToString().Replace("'", " ");
                    string urlXml = n.url_xml.EmptyIfNull().ToString().Replace("'", " ");

                    list.Add(new[]
                    {
                        "",
                        n.id_nfe.ToString(),
                        tipo,
                        statusLabel,
                        nomeCliente,
                        doc,
                        n.data_processamento.ToString("dd/MM/yy", ci),
                        n.data_vencimento.ToString("dd/MM/yy", ci),
                        valor,
                        urlPdf,
                        urlXml
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
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        private static string SanitizeDtCell(string s)
        {
            if (string.IsNullOrEmpty(s)) { return String.Empty; }
            return s.Replace(",", " ").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        #endregion

        #region GetDadosNfeLogs

        [HttpPost]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Nfe_*,g_Nfe_Actionread")]
        public ActionResult GetDadosNfeLogs(jQueryDataTableParamModel param)
        {
            string filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
                int idNfe = 0;
                int.TryParse(param.yesCustomIdPK, out idNfe);
                var logs = db.g_nfe_logs.AsNoTracking().Where(l => l.id_nfe == idNfe).OrderByDescending(l => l.datahora_cadastro).ToList();
                var usuarios = db.g_usuarios.AsNoTracking().Where(u => u.id_usuario > 0).ToList();
                var page = logs.Skip(param.iDisplayStart).Take(param.iDisplayLength <= 0 ? 10 : param.iDisplayLength).ToList();
                var list = new List<string[]>();
                foreach (var l in page)
                {
                    var u = usuarios.FirstOrDefault(x => x.id_usuario == l.id_usuario_cadastro);
                    string login = u != null ? SanitizeDtCell(u.login.EmptyIfNull().ToString()) : String.Empty;
                    string logTxt = SanitizeDtCell(l.log.EmptyIfNull().ToString());
                    list.Add(new[]
                    {
                        l.datahora_cadastro.ToString("dd/MM/yy HH:mm", CultureInfo.GetCultureInfo("pt-BR")),
                        logTxt,
                        login
                    });
                }

                return Json(new
                {
                    errorMessage = "",
                    stackTrace = "",
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = logs.Count,
                    iTotalDisplayRecords = logs.Count,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        #endregion

        #region Modais (retorno de partial views existentes)

        public ActionResult ModalNfeEnviarPorEmailUnitario(int? id)
        {
            g_nfe m = id > 0 ? db.g_nfe.Find(id) : new g_nfe();
            if (m == null) { m = new g_nfe(); }
            ViewBag.Title = "Enviar NF-e por e-mail";
            return View("ModalNfeEnviarPorEmailUnitario", m);
        }

        public ActionResult ModalExportarDadosNfePDF(int? id)
        {
            ViewBag.Title = "Exportar dados NF-e (PDF)";
            return View("ModalExportarDadosNfePDF", new CstExportacaoDadosNFEModel());
        }

        public ActionResult ModalGerarNfe(int? id)
        {
            ViewBag.Title = "Gerar NF-e";
            g_nfe m = id > 0 ? db.g_nfe.Find(id) : new g_nfe();
            if (m == null) { m = new g_nfe(); }
            return View("ModalGerarNfe", m);
        }

        public ActionResult ModalAtualizarStatusNfe(int? id)
        {
            ViewBag.Title = "Atualizar status NF-e";
            g_nfe m = id > 0 ? db.g_nfe.Find(id) : new g_nfe();
            if (m == null) { m = new g_nfe(); }
            return View("ModalAtualizarStatusNfe", m);
        }

        public ActionResult ModalEnviarCancelamentoNfe(int? id)
        {
            ViewBag.Title = "Enviar cancelamento NF-e";
            g_nfe m = id > 0 ? db.g_nfe.Find(id) : new g_nfe();
            if (m == null) { m = new g_nfe(); }
            return View("ModalEnviarCancelamentoNfe", m);
        }

        public ActionResult ModalCancelarNfe(int? id)
        {
            g_nfe m = id > 0 ? db.g_nfe.Find(id) : new g_nfe();
            if (m == null) { m = new g_nfe(); }
            ViewBag.Title = "Cancelar NF-e";
            return View("ModalCancelarNfe", m);
        }

        public ActionResult ModalSincronizarLotesNfe(int? id)
        {
            ViewBag.Title = "Transmitir/Receber NF-e";
            return View("ModalSincronizarLotesNfe", new g_nfe());
        }

        public ActionResult ModalImportarNfeLote(int? id)
        {
            ViewBag.Title = "Importar NF-e (lote)";
            return View("ModalImportarNfeLote", new g_nfe());
        }

        #endregion

        #region Ajax (integração e-Notas / RoboEnotasNFE)

        [HttpPost]
        public ActionResult AjaxClonarNfe()
        {
            if (db == null)
            {
                return Json(new { success = false, msg = "Banco de dados não inicializado." }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                if (Request.InputStream.CanSeek) { Request.InputStream.Position = 0; }
                string raw = new StreamReader(Request.InputStream, Encoding.UTF8).ReadToEnd();
                JObject jo = JObject.Parse(raw);
                JToken idTok = jo["record_g_nfe"] != null ? jo["record_g_nfe"]["id_nfe"] : null;
                if (idTok == null || idTok.Type == JTokenType.Null)
                {
                    return Json(new { success = false, msg = "Payload inválido: informe record_g_nfe.id_nfe." }, JsonRequestBehavior.AllowGet);
                }
                int idNfe;
                if (!int.TryParse(idTok.ToString(), out idNfe) || idNfe <= 0)
                {
                    return Json(new { success = false, msg = "Id. NFe inválido." }, JsonRequestBehavior.AllowGet);
                }
                g_nfe src = db.g_nfe.Find(idNfe);
                if (src == null)
                {
                    return Json(new { success = false, msg = "Registro g_nfe não encontrado." }, JsonRequestBehavior.AllowGet);
                }
                g_nfe novo = ClonarRegistroGNfe(src);
                db.g_nfe.Add(novo);
                db.SaveChanges();
                return Json(new { success = true, msg = "Registro clonado com sucesso. Novo id_nfe: " + novo.id_nfe + "." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = LibExceptions.getExceptionShortMessage(ex) }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AjaxCancelarNfe(g_nfe posted)
        {
            return JsonCancelarOuEnviarCancelamento(posted, false);
        }

        [HttpPost]
        public ActionResult AjaxEnviarCancelamentoNfe(g_nfe posted)
        {
            return JsonCancelarOuEnviarCancelamento(posted, true);
        }

        [HttpPost]
        public ActionResult AjaxNfeEnviarPorEmailUnitario(g_nfe posted)
        {
            bool sucesso = false;
            string msgRetorno = String.Empty;
            string idProcessamentoGravado = "0";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            if (db == null)
            {
                return Json(new { success = false, msg = "Banco de dados não inicializado.", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                if (posted == null || posted.id_nfe <= 0)
                {
                    return Json(new { success = false, msg = "Id. NFe inválido.", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
                }
                g_nfe linha = db.g_nfe.Find(posted.id_nfe);
                if (linha == null)
                {
                    return Json(new { success = false, msg = "NFe não encontrada.", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrWhiteSpace(linha.url_pdf.EmptyIfNull().ToString()))
                {
                    return Json(new { success = false, msg = "NFe sem URL de PDF (nota ainda não disponível para envio).", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
                }

                g_filiais record_g_filiais = db.g_filiais.Where(f => f.id_filial == 1).FirstOrDefault();
                int qtdEmailsVerificados = 1;
                int qtdEmailsEnviados = 0;
                int qtdEmailsNaoPodeSerEnviados = 0;
                string arquivoSaida = "Id. Cliente; Nome; Id. NFe; E-mail; R$ Valor; Status\r\n";
                string EmailDestinatario = linha.email.EmptyIfNull().ToString().Trim();
                bool enviarEmail = false;
                string status = String.Empty;

                if ((EmailDestinatario.Length > 0) && (EmailDestinatario != "NULL"))
                {
                    string validado = LibStringFormat.RemoverAcentos(EmailDestinatario);
                    if (EmailDestinatario.Equals(validado) && EmailDestinatario.Contains("@") && EmailDestinatario.Contains(".") && EmailDestinatario.IndexOf(",") < 0)
                    {
                        enviarEmail = true;
                        status = "NFe processada (envio e-mail conforme configuração LibEmail).";
                    }
                    else
                    {
                        enviarEmail = false;
                        qtdEmailsNaoPodeSerEnviados = 1;
                        status = "NÃO SERÁ POSSIVEL ENVIAR O E-MAIL, VALIDE O E-MAIL!";
                    }
                }
                else
                {
                    enviarEmail = false;
                    status = "E-MAIL NÃO CADASTRADO!";
                }

                if (enviarEmail)
                {
                    int? idFat = null;
                    if (linha.id_financeiro.HasValue && linha.id_financeiro.Value > 0)
                    {
                        g_financeiro fin = db.g_financeiro.Find(linha.id_financeiro.Value);
                        if (fin != null && fin.id_financeiro_faturamento.HasValue && fin.id_financeiro_faturamento.Value > 0)
                        {
                            idFat = fin.id_financeiro_faturamento.Value;
                        }
                    }
                    if (idFat.HasValue)
                    {
                        g_nfe_envio_email_log log = new g_nfe_envio_email_log
                        {
                            id_financeiro_faturamento = idFat.Value,
                            id_cliente = linha.id_cliente,
                            id_nfe = linha.id_nfe,
                            email_enviado = EmailDestinatario,
                            datahora_cadastro = LibDateTime.getDataHoraBrasilia(),
                            id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                        };
                        db.g_nfe_envio_email_log.Add(log);
                        db.SaveChanges();
                    }
                    qtdEmailsEnviados = 1;
                }

                arquivoSaida += linha.id_cliente + ";" + linha.nome.EmptyIfNull() + ";" + linha.id_nfe + ";" + EmailDestinatario + ";" + linha.valor_total_liquido + ";" + status + "\r\n";

                string fileNameExportacao = "Envio_NFe_unitario_" + LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + ".csv";
                string DirTempFiles = Server.MapPath("~/_filestemp");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "reports");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                LibFilesDisk.DeleteFilesInDirectory(DirTempFiles);
                string fileNameDestino = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_" + fileNameExportacao);
                using (StreamWriter w = new StreamWriter(fileNameDestino, true, Encoding.UTF8))
                {
                    w.Write(arquivoSaida);
                }

                g_processamento record_g_processamento = new g_processamento();
                record_g_processamento.id_processamento_tipo = 45;
                record_g_processamento.id_processamento_modulo = 2;
                record_g_processamento.detalhamento = "Relatório Envio NFe Por Email (unitário)";
                record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                record_g_processamento.datahora_inicio = DataHoraAtual;
                record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                record_g_processamento.qtd_registros = qtdEmailsVerificados;
                record_g_processamento.qtd_reg_ok = qtdEmailsEnviados;
                record_g_processamento.qtd_reg_erro = qtdEmailsNaoPodeSerEnviados;
                record_g_processamento.processando = false;
                record_g_processamento.concluido = true;
                record_g_processamento.pathfile = fileNameDestino;
                record_g_processamento.id_coligada = 1;
                record_g_processamento.id_filial = 1;
                db.g_processamento.Add(record_g_processamento);
                db.SaveChanges();

                sucesso = true;
                idProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                msgRetorno = "Processo gerado com sucesso! " + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>"
                    + "Filial remetente: " + (record_g_filiais != null ? record_g_filiais.nome.EmptyIfNull() : "") + "<br/>"
                    + "Total verificados: " + qtdEmailsVerificados + "<br/>Registrados para envio: " + qtdEmailsEnviados + "<br/>Com impedimento: " + qtdEmailsNaoPodeSerEnviados;
            }
            catch (Exception e)
            {
                sucesso = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    msgRetorno += "<br/><br/>" + e.InnerException.Message;
                }
            }
            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ajaxExportarDadosNfePDF()
        {
            bool sucesso = false;
            string msgRetorno = String.Empty;
            string idProcessamentoGravado = "0";
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            if (db == null)
            {
                return Json(new { success = false, msg = "Banco de dados não inicializado.", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                if (Request.InputStream.CanSeek) { Request.InputStream.Position = 0; }
                string raw = new StreamReader(Request.InputStream, Encoding.UTF8).ReadToEnd();
                JObject jo = JObject.Parse(raw);
                JObject rec = jo["record_cstExportacaoDadosNFEModel"] as JObject;
                if (rec == null)
                {
                    return Json(new { success = false, msg = "Payload inválido.", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
                }
                DateTime d1, d2;
                if (!DateTime.TryParse(rec["datahora_inicial"] != null ? rec["datahora_inicial"].ToString() : "", new CultureInfo("pt-BR"), DateTimeStyles.None, out d1)
                    || !DateTime.TryParse(rec["datahora_final"] != null ? rec["datahora_final"].ToString() : "", new CultureInfo("pt-BR"), DateTimeStyles.None, out d2))
                {
                    return Json(new { success = false, msg = "Informe data inicial e final válidas (dd/MM/yyyy).", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
                }
                DateTime fim = d2.Date.AddDays(1).AddTicks(-1);
                var lista = db.g_nfe.AsNoTracking().Where(n => n.data_processamento >= d1 && n.data_processamento <= fim && n.url_pdf != null && n.url_pdf != "").OrderBy(n => n.id_nfe).ToList();
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("id_nfe;nome;valor_total_liquido;url_pdf;url_xml;data_processamento");
                foreach (var n in lista)
                {
                    sb.Append(n.id_nfe).Append(";").Append(n.nome.EmptyIfNull().ToString().Replace(";", ",")).Append(";")
                        .Append(n.valor_total_liquido.ToString(CultureInfo.InvariantCulture)).Append(";")
                        .Append(n.url_pdf.EmptyIfNull()).Append(";").Append(n.url_xml.EmptyIfNull()).Append(";")
                        .Append(n.data_processamento.ToString("dd/MM/yyyy")).AppendLine();
                }

                string DirTempFiles = Server.MapPath("~/_filestemp");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "reports");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                LibFilesDisk.DeleteFilesInDirectory(DirTempFiles);
                string fileNameDestino = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_ExportNFePDF.csv");
                System.IO.File.WriteAllText(fileNameDestino, sb.ToString(), Encoding.UTF8);

                g_processamento proc = new g_processamento();
                proc.id_processamento_tipo = 49;
                proc.id_processamento_modulo = 2;
                proc.detalhamento = "Exportação g_nfe com PDF (período)";
                proc.id_usuario = CachePersister.userIdentity.IdUsuario;
                proc.datahora_inicio = DataHoraAtual;
                proc.datahora_final = LibDateTime.getDataHoraBrasilia();
                proc.qtd_registros = lista.Count;
                proc.qtd_reg_ok = lista.Count;
                proc.qtd_reg_erro = 0;
                proc.processando = false;
                proc.concluido = true;
                proc.pathfile = fileNameDestino;
                proc.id_coligada = 1;
                proc.id_filial = 1;
                db.g_processamento.Add(proc);
                db.SaveChanges();

                sucesso = true;
                idProcessamentoGravado = proc.id_processamento.ToString();
                msgRetorno = "Exportação concluída. Registros: " + lista.Count + ".";
            }
            catch (Exception ex)
            {
                msgRetorno = LibExceptions.getExceptionShortMessage(ex);
            }
            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ajaxGerarNfe(g_nfe posted)
        {
            if (db == null)
            {
                return Json(new { success = false, msg = "Banco de dados não inicializado." }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                if (posted == null || posted.id_nfe <= 0)
                {
                    return Json(new { success = false, msg = "Selecione uma NFe na grade (apenas um registro)." }, JsonRequestBehavior.AllowGet);
                }
                gc_movimentos_nf mov = ResolverMovimentoNfParaGNfe(posted.id_nfe);
                if (mov == null)
                {
                    return Json(new { success = false, msg = "Não há gc_movimentos_nf vinculado ao título financeiro desta NFe (id_financeiro_movimento). Gere a NF pelo fluxo de Movimentos." }, JsonRequestBehavior.AllowGet);
                }
                RoboEnotasNFE robo = new RoboEnotasNFE();
                bool ok = robo.GerarNFServicoByMovimentoNFId(mov.id_movimento_nf);
                if (ok)
                {
                    SincronizarGNfeComMovimentoNf(posted.id_nfe, mov.id_movimento_nf);
                }
                return Json(new { success = ok, msg = ok ? "Solicitação de geração (serviço) enviada ao gateway. Atualize o status na sequência." : "Falha na geração; verifique g_nfe_logs e movimentos." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = LibExceptions.getExceptionShortMessage(ex) }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AjaxAtualizarStatusNfe(g_nfe posted)
        {
            if (db == null)
            {
                return Json(new { success = false, msg = "Banco de dados não inicializado." }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                if (posted == null || posted.id_nfe <= 0)
                {
                    return Json(new { success = false, msg = "Selecione uma NFe na grade (apenas um registro)." }, JsonRequestBehavior.AllowGet);
                }
                RoboEnotasNFE robo = new RoboEnotasNFE();
                gc_movimentos_nf mov = ResolverMovimentoNfParaGNfe(posted.id_nfe);
                bool ok;
                if (mov != null)
                {
                    ok = robo.AtualizarStatusNFPbyMovimentoNFId(mov.id_movimento_nf);
                    SincronizarGNfeComMovimentoNf(posted.id_nfe, mov.id_movimento_nf);
                }
                else
                {
                    ok = robo.AtualizarStatusG_nfePorId(posted.id_nfe);
                }
                return Json(new { success = ok, msg = ok ? "Status atualizado conforme retorno do e-Notas." : "Não foi possível atualizar o status (verifique nfe_key e logs)." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = LibExceptions.getExceptionShortMessage(ex) }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AjaxSincronizarLotesNfe()
        {
            if (db == null)
            {
                return Json(new { success = false, msg = "Banco de dados não inicializado." }, JsonRequestBehavior.AllowGet);
            }
            int ok = 0, falha = 0;
            try
            {
                var ids = db.g_nfe.AsNoTracking()
                    .Where(n => n.nfe_key != null && n.nfe_key != "" && n.id_nfe > 0)
                    .OrderByDescending(n => n.id_nfe)
                    .Select(n => n.id_nfe)
                    .Take(200)
                    .ToList();
                RoboEnotasNFE robo = new RoboEnotasNFE();
                foreach (int idNfe in ids)
                {
                    try
                    {
                        gc_movimentos_nf mov = ResolverMovimentoNfParaGNfe(idNfe);
                        if (mov != null)
                        {
                            bool api = robo.AtualizarStatusNFPbyMovimentoNFId(mov.id_movimento_nf);
                            SincronizarGNfeComMovimentoNf(idNfe, mov.id_movimento_nf);
                            if (api) { ok++; } else { falha++; }
                        }
                        else
                        {
                            if (robo.AtualizarStatusG_nfePorId(idNfe)) { ok++; }
                            else { falha++; }
                        }
                    }
                    catch
                    {
                        falha++;
                    }
                }
                return Json(new { success = true, msg = "Sincronização em lote concluída. OK: " + ok + " | falhas: " + falha + " (máx. 200 notas mais recentes com nfe_key)." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = LibExceptions.getExceptionShortMessage(ex) }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult ajaxImportarNfeLote(HttpPostedFileBase filesource)
        {
            bool processado = false;
            string msgRetorno = String.Empty;
            string idProcessamentoGravado = "0";
            DateTime dataInicioProcesso = LibDateTime.getDataHoraBrasilia();
            if (db == null)
            {
                return Json(new { success = false, msg = "Banco de dados não inicializado.", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
            }
            if (filesource == null || filesource.ContentLength <= 0)
            {
                return Json(new { success = false, msg = "Selecione um arquivo.", idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                string fileNameOrigem = Path.GetFileName(filesource.FileName);
                string DirTempFiles = Server.MapPath("~/_filestemp");
                DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                LibFilesDisk.DeleteFilesInDirectory(DirTempFiles);
                string fileNameDestino = Path.Combine(DirTempFiles, fileNameOrigem);
                filesource.SaveAs(fileNameDestino);

                string relatorio = "Arquivo recebido: " + fileNameOrigem + " (" + filesource.ContentLength + " bytes). Armazenado para conferência manual / futura integração.";
                string fileNameProcessamento = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_ImportNFeLote.txt");
                System.IO.File.WriteAllText(fileNameProcessamento, relatorio, Encoding.UTF8);

                g_processamento record_g_processamento = new g_processamento();
                record_g_processamento.id_processamento_tipo = 50;
                record_g_processamento.id_processamento_modulo = 2;
                record_g_processamento.detalhamento = "Importação lote NFe (arquivo recebido)";
                record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                record_g_processamento.datahora_inicio = dataInicioProcesso;
                record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                record_g_processamento.qtd_registros = 1;
                record_g_processamento.qtd_reg_ok = 1;
                record_g_processamento.qtd_reg_erro = 0;
                record_g_processamento.processando = false;
                record_g_processamento.concluido = true;
                record_g_processamento.pathfile = fileNameProcessamento;
                record_g_processamento.id_coligada = 1;
                record_g_processamento.id_filial = 1;
                db.g_processamento.Add(record_g_processamento);
                db.SaveChanges();
                idProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                processado = true;
                msgRetorno = "Arquivo importado e registrado em processamentos.";
            }
            catch (Exception ex)
            {
                msgRetorno = LibExceptions.getExceptionShortMessage(ex);
            }
            return Json(new { success = processado, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonCancelarOuEnviarCancelamento(g_nfe posted, bool _enviarCancelamentoMenu)
        {
            if (db == null)
            {
                return Json(new { success = false, msg = "Banco de dados não inicializado." }, JsonRequestBehavior.AllowGet);
            }
            try
            {
                if (posted == null || posted.id_nfe <= 0)
                {
                    return Json(new { success = false, msg = "Id. NFe inválido." }, JsonRequestBehavior.AllowGet);
                }
                if (string.IsNullOrWhiteSpace(posted.motivo_cancelamento.EmptyIfNull().ToString()))
                {
                    return Json(new { success = false, msg = "Informe o motivo do cancelamento." }, JsonRequestBehavior.AllowGet);
                }
                string motivo = posted.motivo_cancelamento.Trim();
                int idNfe = posted.id_nfe;
                gc_movimentos_nf mov = ResolverMovimentoNfParaGNfe(idNfe);
                RoboEnotasNFE robo = new RoboEnotasNFE();
                bool ok;
                if (mov != null)
                {
                    ok = robo.CancelarNFPbyMovimentoNFId(mov.id_movimento_nf, motivo);
                    if (ok)
                    {
                        SincronizarGNfeComMovimentoNf(idNfe, mov.id_movimento_nf);
                        g_nfe nfeUpd = db.g_nfe.Find(idNfe);
                        if (nfeUpd != null)
                        {
                            nfeUpd.motivo_cancelamento = motivo;
                            nfeUpd.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                            nfeUpd.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(nfeUpd).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
                else
                {
                    ok = robo.CancelarG_nfePorId(idNfe, motivo);
                }
                string prefix = _enviarCancelamentoMenu ? "Cancelamento enviado" : "Cancelamento";
                return Json(new { success = ok, msg = ok ? (prefix + " processado no e-Notas.") : (prefix + " não concluído; verifique status, nfe_key e logs.") }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = LibExceptions.getExceptionShortMessage(ex) }, JsonRequestBehavior.AllowGet);
            }
        }

        private gc_movimentos_nf ResolverMovimentoNfParaGNfe(int idNfe)
        {
            g_nfe nfe = db.g_nfe.Find(idNfe);
            if (nfe == null || !nfe.id_financeiro.HasValue || nfe.id_financeiro.Value <= 0)
            {
                return null;
            }
            g_financeiro fin = db.g_financeiro.Find(nfe.id_financeiro.Value);
            if (fin == null || !fin.id_financeiro_movimento.HasValue || fin.id_financeiro_movimento.Value <= 0)
            {
                return null;
            }
            int idMov = fin.id_financeiro_movimento.Value;
            return db.gc_movimentos_nf.Where(x => x.id_movimento == idMov).OrderByDescending(x => x.id_movimento_nf).FirstOrDefault();
        }

        private void SincronizarGNfeComMovimentoNf(int idNfe, int idMovimentoNf)
        {
            g_nfe nfe = db.g_nfe.Find(idNfe);
            gc_movimentos_nf mov = db.gc_movimentos_nf.Find(idMovimentoNf);
            if (nfe == null || mov == null)
            {
                return;
            }
            nfe.id_nfe_status = mov.id_nfe_status;
            nfe.url_pdf = mov.nf_url_pdf.EmptyIfNull().ToString();
            nfe.url_xml = mov.nf_url_xml.EmptyIfNull().ToString();
            if (!string.IsNullOrWhiteSpace(mov.nf_identificador.EmptyIfNull().ToString()))
            {
                nfe.nfe_key = mov.nf_identificador.EmptyIfNull().ToString();
            }
            nfe.data_retorno_registro = LibDateTime.getDataHoraBrasilia();
            nfe.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
            nfe.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
            db.Entry(nfe).State = EntityState.Modified;
            db.SaveChanges();
        }

        private static g_nfe ClonarRegistroGNfe(g_nfe src)
        {
            return new g_nfe
            {
                id_financeiro = src.id_financeiro,
                tipo_r_c = src.tipo_r_c,
                id_nfe_status = 1,
                id_nfe_config = src.id_nfe_config,
                nfe_key = null,
                descricao = src.descricao,
                id_cliente = src.id_cliente,
                nome = src.nome,
                razao_social = src.razao_social,
                email = src.email,
                cpf = src.cpf,
                cnpj = src.cnpj,
                inscricao_municipal = src.inscricao_municipal,
                endereco_com = src.endereco_com,
                bairro_com = src.bairro_com,
                id_cidade_com = src.id_cidade_com,
                cep_com = src.cep_com,
                id_uf_com = src.id_uf_com,
                iss_display = src.iss_display,
                iss_valor = src.iss_valor,
                iss_retido = src.iss_retido,
                cofins_display = src.cofins_display,
                cofins_valor = src.cofins_valor,
                csll_display = src.csll_display,
                csll_valor = src.csll_valor,
                inss_valor = src.inss_valor,
                ir_display = src.ir_display,
                ir_valor = src.ir_valor,
                pis_display = src.pis_display,
                pis_valor = src.pis_valor,
                valor_descontos = src.valor_descontos,
                valor_encargos = src.valor_encargos,
                valor_total_bruto = src.valor_total_bruto,
                valor_total_liquido = src.valor_total_liquido,
                data_processamento = src.data_processamento,
                data_vencimento = src.data_vencimento,
                data_envio_registro = null,
                data_retorno_registro = null,
                url_pdf = null,
                url_xml = null,
                id_usuario_cancelamento = null,
                data_envio_cancelamento = null,
                motivo_cancelamento = null,
                data_retorno_cancelamento = null,
                id_coligada = src.id_coligada,
                id_filial = src.id_filial,
                datahora_cadastro = LibDateTime.getDataHoraBrasilia(),
                id_usuario_cadastro = CachePersister.userIdentity.IdUsuario,
                datahora_alteracao = null,
                id_usuario_alteracao = null
            };
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
            if (disposing && db != null) { db.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
