using Rotativa;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContratosAviacao_*,g_ContratosAviacao_Default")]
    public class ContratosAviacaoController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_contratos_aviacao";
        public ContratosAviacaoController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContratosAviacao_*,g_ContratosAviacao_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Gestão de Contratos com Clientes";
            var model = new CstContratosAviacaoIndex
            {
                ContratosAviacaoIndex_id = String.Empty,
                ContratosAviacaoIndex_descricao = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, descRestore;
            if (TryParseFiltroContratosAviacaoSemicolon(filtroPersistido.sql_filtro, out idRestore, out descRestore))
            {
                model.ContratosAviacaoIndex_id = idRestore;
                model.ContratosAviacaoIndex_descricao = descRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(descRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContratosAviacao_*,g_ContratosAviacao_Actionread")]
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
                    recordFiltro = LibDB.getFilterByUser(param, controllerName, false, db);
                }
                else
                {
                    recordFiltro = ObterFiltroPersistidoUsuario();
                }

                var baseQuery = db.g_contratos_aviacao.AsNoTracking().Where(c => c.id_contrato > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string descStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(descStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroContratosAviacaoSemicolon(recordFiltro.sql_filtro, out idStr, out descStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(descStr);
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

                IQueryable<Db.g_contratos_aviacao> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroContratosAviacaoNaQuery(query, idStr, descStr);
                    LibDB.setFilterByUser(MontarFiltroContratosAviacaoPersistido(idStr, descStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoContratosAviacaoNaQuery(query, param);
                var page = query.Skip(start).Take(length).ToList();

                var clienteIds = page.Where(c => c.id_cliente > 0).Select(c => c.id_cliente).Distinct().ToList();
                var tipoIds = page.Where(c => c.id_contrato_tipo > 0).Select(c => c.id_contrato_tipo).Distinct().ToList();
                var clientesPorId = clienteIds.Count == 0
                    ? new Dictionary<int, string>()
                    : db.g_clientes.AsNoTracking()
                        .Where(cl => clienteIds.Contains(cl.id_cliente))
                        .Select(cl => new { cl.id_cliente, cl.nome })
                        .ToList()
                        .ToDictionary(cl => cl.id_cliente, cl => cl.nome ?? String.Empty);
                var tiposPorId = tipoIds.Count == 0
                    ? new Dictionary<int, string>()
                    : db.g_contratos_aviacao_tipos.AsNoTracking()
                        .Where(t => tipoIds.Contains(t.id_contrato_tipo))
                        .Select(t => new { t.id_contrato_tipo, t.descricao })
                        .ToList()
                        .ToDictionary(t => t.id_contrato_tipo, t => t.descricao ?? String.Empty);

                var list = page.Select(c => MontarLinhaContratoAviacao(c, clientesPorId, tiposPorId)).ToList();

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

        private static string[] MontarLinhaContratoAviacao(
            Db.g_contratos_aviacao c,
            Dictionary<int, string> clientesPorId,
            Dictionary<int, string> tiposPorId)
        {
            string contratoVigente;
            string dataAssinatura;
            if (c.ativo == true)
            {
                contratoVigente = LibIcons.getIcon("fa-solid fa-clipboard-check", "Contrato Vigente", "#008000", "fa-lg");
                dataAssinatura = c.data_assinatura.GetValueOrDefault().ToString("dd/MM/yy");
            }
            else
            {
                contratoVigente = LibIcons.getIcon("fa-solid fa-clipboard-check", "Contrato Inativo", "", "fa-lg");
                dataAssinatura = "00/00/0000";
            }
            string contratoAnexo = c.anexo == true
                ? LibIcons.getIcon("fa-solid fa-file-pdf", "Contrato Anexo", "#008000", "fa-lg")
                : LibIcons.getIcon("fa-solid fa-file-pdf", "Contrato NÃO anexado", "#CACFD2", "fa-sm");
            string tipoContrato = String.Empty;
            if (c.id_contrato_tipo > 0)
            {
                string descTipo;
                if (tiposPorId.TryGetValue(c.id_contrato_tipo, out descTipo))
                {
                    tipoContrato = descTipo;
                }
            }
            string nomeCliente = String.Empty;
            if (c.id_cliente > 0)
            {
                string nomeCli;
                if (clientesPorId.TryGetValue(c.id_cliente, out nomeCli))
                {
                    nomeCliente = nomeCli;
                }
            }
            return new[]
            {
                "",
                c.id_contrato.ToString(),
                contratoVigente,
                contratoAnexo,
                nomeCliente,
                tipoContrato,
                dataAssinatura,
                ""
            };
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

        private static bool TryParseFiltroContratosAviacaoSemicolon(string raw, out string id, out string descricao)
        {
            id = descricao = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            descricao = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(descricao);
        }

        private static string MontarFiltroContratosAviacaoPersistido(string id, string descricao)
        {
            return (id ?? String.Empty) + ";" + (descricao ?? String.Empty);
        }

        private static IQueryable<Db.g_contratos_aviacao> AplicarFiltroContratosAviacaoNaQuery(IQueryable<Db.g_contratos_aviacao> query, string idStr, string descStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idContrato))
            {
                query = query.Where(c => c.id_contrato == idContrato);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(descStr, out string padraoDesc))
            {
                query = query.Where(c => c.descricao != null && DbFunctions.Like(c.descricao, padraoDesc));
            }
            return query;
        }

        private static IQueryable<Db.g_contratos_aviacao> AplicarOrdenacaoContratosAviacaoNaQuery(IQueryable<Db.g_contratos_aviacao> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0 && param.iSortCol_0 == 1)
            {
                return asc ? query.OrderBy(c => c.id_contrato) : query.OrderByDescending(c => c.id_contrato);
            }
            return query.OrderByDescending(c => c.id_contrato);
        }
        #endregion 

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContratosAviacao_*,g_ContratosAviacao_Actioncreate,gdc_Pefin_Default")]
        public ActionResult Create()
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Contrato</b";
            g_contratos_aviacao newRecord = new g_contratos_aviacao();
            newRecord.ativo = true;
            newRecord.anexo = false;
            newRecord.id_cliente = -1;
            newRecord.data_assinatura = DataHoraAtual.Date;
            ViewBag.comboClientes = LibDataSets.LoadComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]" });
            ViewBag.comboContratosTipos = LibDataSets.LoadComboGContratosTipos(db);
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContratosAviacao_*,g_ContratosAviacao_Actioncreate,gdc_Pefin_Default")]
        public ActionResult Create(g_contratos_aviacao record_g_contratos_aviacao)
        {
            String MsgRetorno = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Contrato</b";
            record_g_contratos_aviacao.id_coligada = 0;  // Definição de que Cidade é Global
            record_g_contratos_aviacao.id_filial = 0;    // Definição de que Cidade é Global
            if (record_g_contratos_aviacao.descricao.EmptyIfNull().ToString() != String.Empty) { record_g_contratos_aviacao.descricao = LibStringFormat.FormatarTextoSimples(record_g_contratos_aviacao.descricao); }

            if (ModelState.IsValid)
            {
                if (record_g_contratos_aviacao.id_cliente <= 0)
                {
                    ModelState.AddModelError("Model", "Cliente/Fornecedor é de preenchimento obrigatório!");
                }
                if (record_g_contratos_aviacao.descricao.EmptyIfNull().ToString().Length == 0)
                {
                    ModelState.AddModelError("Model", "Descrição é de preenchimento obrigatório!");
                }
                if (record_g_contratos_aviacao.id_contrato_tipo <= 0)
                {
                    ModelState.AddModelError("Model", "Tipo Contrato é de preenchimento obrigatório!");
                }
                else
                {
                    g_contratos_aviacao_tipos RecordContratoTipo = db.g_contratos_aviacao_tipos.Find(record_g_contratos_aviacao.id_contrato_tipo);

                    if (RecordContratoTipo.assinatura_eletronica == true)
                    {
                        if (record_g_contratos_aviacao.signatario1_nome.EmptyIfNull().ToString().Length == 0)
                        {
                            ModelState.AddModelError("Model", "Signatário (Nome) é de preenchimento obrigatório!");
                        }
                        if (record_g_contratos_aviacao.signatario1_email.EmptyIfNull().ToString().Length == 0)
                        {
                            ModelState.AddModelError("Model", "Signatário (Email) é de preenchimento obrigatório!");
                        }
                        if (record_g_contratos_aviacao.signatario1_telefone.EmptyIfNull().ToString().Length == 0)
                        {
                            ModelState.AddModelError("Model", "Signatário (Telefone) é de preenchimento obrigatório!");
                        }
                        if ((record_g_contratos_aviacao.signatario1_cpf.EmptyIfNull().ToString().Length == 0) && (record_g_contratos_aviacao.signatario1_cnpj.EmptyIfNull().ToString().Length == 0))
                        {
                            ModelState.AddModelError("Model", "Signatário (CPF ou CNPJ) é de preenchimento obrigatório!");
                        }
                        if ((record_g_contratos_aviacao.signatario1_cpf.EmptyIfNull().ToString().Length > 0) && (record_g_contratos_aviacao.signatario1_cnpj.EmptyIfNull().ToString().Length > 0))
                        {
                            ModelState.AddModelError("Model", "Signatário (CPF e CNPJ) não podem ser preenchidos simultaneamente!");
                        }
                    }
                    if (RecordContratoTipo.identificador_obrigatorio == true)
                    {
                        if (record_g_contratos_aviacao.identificador.EmptyIfNull().ToString().Length == 0)
                        {
                            ModelState.AddModelError("Model", "Identificador é de preenchimento obrigatório!");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_contratos_aviacao.datahora_cadastro = DataHoraAtual;
                record_g_contratos_aviacao.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.g_contratos_aviacao.Add(record_g_contratos_aviacao);
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
            ViewBag.comboClientes = LibDataSets.LoadComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]" });
            ViewBag.comboContratosTipos = LibDataSets.LoadComboGContratosTipos(db);
            return View("CreateEdit", record_g_contratos_aviacao);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContratosAviacao_*,g_ContratosAviacao_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_contratos_aviacao record_g_contratos_aviacao = db.g_contratos_aviacao.Find(id);
            if (record_g_contratos_aviacao == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Contrato</b>" + LibStringFormat.GetTabHtml(1) + record_g_contratos_aviacao.id_contrato.EmptyIfNull().ToString() + " - " + record_g_contratos_aviacao.descricao.EmptyIfNull().ToString();
            ViewBag.comboClientes = LibDataSets.LoadComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]" });
            ViewBag.comboContratosTipos = LibDataSets.LoadComboGContratosTipos(db);
            return View("CreateEdit", record_g_contratos_aviacao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContratosAviacao_*,g_ContratosAviacao_Actionupdate")]
        public ActionResult Edit(g_contratos_aviacao record_g_contratos_aviacao)
        {
            if (record_g_contratos_aviacao.descricao.EmptyIfNull().ToString() != String.Empty) { record_g_contratos_aviacao.descricao = LibStringFormat.FormatarTextoSimples(record_g_contratos_aviacao.descricao); }

            if (ModelState.IsValid)
            {
                db.Entry(record_g_contratos_aviacao).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Contrato</b>" + LibStringFormat.GetTabHtml(1) + record_g_contratos_aviacao.id_contrato.EmptyIfNull().ToString() + " - " + record_g_contratos_aviacao.descricao.EmptyIfNull().ToString();
            ViewBag.comboClientes = LibDataSets.LoadComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ INFORME O CLIENTE ]" });
            ViewBag.comboContratosTipos = LibDataSets.LoadComboGContratosTipos(db);
            return View("CreateEdit", record_g_contratos_aviacao);
        }
        #endregion

        [HttpPost]
        public ActionResult AjaxGetDadosClientes(g_clientes record_g_clientes)
        {
            bool Sucesso = false;
            String MsgRetorno = String.Empty;
            try
            {
                if (record_g_clientes.id_cliente > 0)
                {
                    record_g_clientes = db.g_clientes.Find(record_g_clientes.id_cliente);
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
            return Json(new { success = Sucesso, msg = MsgRetorno, RecordCliente = record_g_clientes }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AjaxDownloadContratoAviacaoPDF(int? id)
        {
            bool sucesso = false;
            String msgRetorno = string.Empty;
            String fileNamePDF_Contrato = string.Empty;
            String idProcessamentoGravado = "0";
            var pdf = new ViewAsPdf();
            try
            {
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                String formatoMoeda = String.Empty;
                String DirTempFiles = Server.MapPath("~/_filestemp");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "reports");
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                int id_contrato = id.GetValueOrDefault();
                g_contratos_aviacao record_record_g_contrato = db.g_contratos_aviacao.Find(id);
                g_clientes record_g_cliente = db.g_clientes.Find(record_record_g_contrato.id_cliente);
                String SignatarioDocumento = String.Empty;
                String ContratanteDocumento = String.Empty;
                if (record_record_g_contrato.signatario1_cpf.EmptyIfNull().ToString().Length > 0) { SignatarioDocumento = LibStringFormat.FormatarCPFCNPJ("F", record_record_g_contrato.signatario1_cpf); }
                else if (record_record_g_contrato.signatario1_cnpj.EmptyIfNull().ToString().Length > 0) { SignatarioDocumento = LibStringFormat.FormatarCPFCNPJ("J", record_record_g_contrato.signatario1_cnpj); };
                if (record_g_cliente.cpf.EmptyIfNull().ToString().Length > 0) { ContratanteDocumento = LibStringFormat.FormatarCPFCNPJ("F", record_g_cliente.cpf); }
                else if (record_g_cliente.cnpj.EmptyIfNull().ToString().Length > 0) { ContratanteDocumento = LibStringFormat.FormatarCPFCNPJ("J", record_g_cliente.cnpj); };
                ViewBag.ContratanteNome = record_g_cliente.nome;
                ViewBag.ContratanteDocumento = ContratanteDocumento;
                ViewBag.ContratanteIdentificador = record_record_g_contrato.identificador;
                ViewBag.SignatarioNome = record_record_g_contrato.signatario1_nome;
                ViewBag.SignatarioDocumento = SignatarioDocumento;
                ViewBag.ContratoNumero = record_record_g_contrato.id_contrato.ToString("000000");
                ViewBag.ImgLogoSubdominio = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/logoGdi.png";
                ViewBag.imgAssinaturaContratada = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/AssinaturaGdiGustavo.png";
                String NomeCliente = record_g_cliente.nome.EmptyIfNull().ToString();
                NomeCliente = LibStringFormat.RemoverAcentos(NomeCliente.ToUpperInvariant());
                NomeCliente = LibStringFormat.SomenteAlfabetoeNumeros(NomeCliente);
                NomeCliente = LibStringFormat.RemoverEspacosDuplos(NomeCliente);
                NomeCliente = NomeCliente.Replace(" ", "_");
                if (NomeCliente.Length > 50) { NomeCliente = NomeCliente.Substring(0, 50); };
                String FileNamePDF = "Contrato-" + record_record_g_contrato.id_contrato.ToString("000000") + "-" + NomeCliente;
                pdf = new ViewAsPdf
                {
                    ViewName = "ReportPDFContrato001",
                    Model = record_record_g_contrato,
                    FileName = FileNamePDF
                };
                byte[] applicationPDFData_BL = pdf.BuildFile(ControllerContext);
                fileNamePDF_Contrato = string.Empty;
                fileNamePDF_Contrato = FileNamePDF + ".pdf";
                fileNamePDF_Contrato = Path.Combine(DirTempFiles, fileNamePDF_Contrato);
                var fileStream_BL = new FileStream(fileNamePDF_Contrato, FileMode.Create, FileAccess.Write);
                fileStream_BL.Write(applicationPDFData_BL, 0, applicationPDFData_BL.Length);
                fileStream_BL.Close();

                // Atualizar o registro do processamento
                g_processamento record_g_processamento = new g_processamento();
                record_g_processamento.id_processamento_tipo = 37;
                record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                record_g_processamento.datahora_inicio = LibDateTime.getDataHoraBrasilia();
                record_g_processamento.datahora_final = LibDateTime.getDataHoraBrasilia();
                record_g_processamento.qtd_registros = 1;
                record_g_processamento.qtd_reg_ok = 1;
                record_g_processamento.qtd_reg_erro = 0;
                record_g_processamento.processando = false;
                record_g_processamento.concluido = true;
                record_g_processamento.pathfile = fileNamePDF_Contrato;
                record_g_processamento.id_coligada = 1;
                record_g_processamento.id_filial = 1;
                db.g_processamento.Add(record_g_processamento);
                db.SaveChanges();
                idProcessamentoGravado = record_g_processamento.id_processamento.ToString();

                sucesso = true;
                msgRetorno = "Contrato gerado com sucesso" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") + "<br/><br/>" + "Obs: O Download será iniciado automaticamente";
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
            return Json(new { success = sucesso, msg = msgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ModalUploadContratoAssinado(int? IdContrato)
        {
            CstUploadGed record_cstUploadGed = new CstUploadGed();
            if (IdContrato > 0)
            {
                g_contratos_aviacao record_g_contratos_aviacao = db.g_contratos_aviacao.Find(IdContrato);
                g_clientes record_g_cliente = db.g_clientes.Find(record_g_contratos_aviacao.id_cliente);
                String NomeCliente = record_g_cliente.nome.EmptyIfNull().ToString();
                NomeCliente = LibStringFormat.RemoverAcentos(NomeCliente.ToUpperInvariant());
                NomeCliente = LibStringFormat.SomenteAlfabetoeNumeros(NomeCliente);
                NomeCliente = LibStringFormat.RemoverEspacosDuplos(NomeCliente);
                NomeCliente = NomeCliente.Replace(" ", "_");
                if (NomeCliente.Length > 50) { NomeCliente = NomeCliente.Substring(0, 50); };
                String FileNamePDF = "Contrato-Aviação-" + record_g_contratos_aviacao.id_contrato.ToString("000000") + "-" + NomeCliente;
                record_cstUploadGed.id_cliente = record_g_contratos_aviacao.id_cliente;
                record_cstUploadGed.id_contrato = record_g_contratos_aviacao.id_contrato;
                record_cstUploadGed.descricao = FileNamePDF;
                record_cstUploadGed.id_arquivo_tipo = 17;
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Contrato Assinado</b>";
            }
            return View("ModalUploadContratoAssinado", record_cstUploadGed);
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