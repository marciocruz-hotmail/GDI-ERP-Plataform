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
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContratosAviacao_*,g_ContratosAviacao_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
            List<g_contratos_aviacao_tipos> allContratosTipos = db.g_contratos_aviacao_tipos.Where(c => c.id_contrato_tipo > 0).ToList();
            var allRecordsClientes = db.g_clientes.Select(c => new { c.id_cliente, c.nome }).ToList();
            var allRecords = new List<Db.g_contratos_aviacao>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                allRecords = db.g_contratos_aviacao.Where(c => c.ativo == true).OrderByDescending(c => c.id_contrato).ToList();
            }
            if (filterDb)
            {
                SentencaSQL = string.Empty;
                if (record_g_filtro.advanced == true)
                {
                    SentencaSQL = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim();
                }
                else
                {
                    SentencaSQL = "select * from g_contratos_aviacao where id_contrato > 1 and " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim() + " order by id_contrato desc";
                }

                allRecords = db.g_contratos_aviacao.SqlQuery(SentencaSQL).ToList();
            }
            else if (filterAdvanced)
            {
                // Filtro Avançado
                String[] listaCampos = null;
                SentencaSQL = string.Empty;
                try { listaCampos = param.yesFilterAdvancedText.EmptyIfNull().ToString().Split(';'); } catch (Exception) { listaCampos = new string[1] { "" }; };

                if (listaCampos.Count() == 5)
                {
                    SentencaSQL = " select c.* from g_contratos_aviacao c where id_contrato > 0 and c.ativo = 1";
                    if ((!listaCampos[0].ToString().Trim().Equals(String.Empty)) && (!listaCampos[0].ToString().Trim().Equals("0")))
                    {
                        SentencaSQL += " and ( c.id_contrato = '" + listaCampos[0].ToString().Trim() + "' )";
                    }
                    if ((!listaCampos[1].ToString().Trim().Equals(String.Empty)) && (!listaCampos[1].ToString().Trim().Equals("0")) && (!listaCampos[1].ToString().Trim().Equals("-1")))
                    {
                        SentencaSQL += " and ( c.id_cliente = '" + listaCampos[1].ToString().Trim() + "' )";
                    }
                    if (!listaCampos[2].ToString().Trim().Equals(String.Empty))
                    {
                        SentencaSQL += " and ( c.descricao like '%" + listaCampos[2].ToString().Trim() + "%' )";
                    }
                    if ((!listaCampos[3].ToString().Trim().Equals(String.Empty)) && (!listaCampos[4].ToString().Trim().Equals(String.Empty)))
                    {
                        SentencaSQL += " and ( c.data_assinatura between '" + DateTime.Parse(listaCampos[3].ToString().Trim()).ToString("yyyy-MM-dd") + " 00:00:00" + "' and '" + DateTime.Parse(listaCampos[4].ToString().Trim()).ToString("yyyy-MM-dd") + " 23:59:59') ";
                    }
                    SentencaSQL += " order by c.id_contrato desc";
                    LibDB.setFilterByUser(SentencaSQL, controllerName, true, db);
                    allRecords = db.g_contratos_aviacao.SqlQuery(SentencaSQL.ToString()).ToList();
                }
                else
                {
                    allRecords = db.g_contratos_aviacao.Where(c => c.ativo == true).OrderByDescending(c => c.id_contrato).ToList();
                }
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_contratos_aviacao, string> orderingFunction = (c => param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_contrato) : "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(c => c.id_contrato);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            foreach (var c in displayedRecords)
            {
                String ContratoVigente = String.Empty;
                String ContratoAnexo = String.Empty;
                String TipoContrato = String.Empty;
                String NomeCliente = String.Empty;
                String DataAssinatura = String.Empty;
                String DescricaoContrato = c.descricao.EmptyIfNull().ToString().ToLowerInvariant();
                if (DescricaoContrato.Length > 40) { DescricaoContrato = DescricaoContrato.Substring(0, 40) + "..."; };
                if (c.ativo == true)
                {
                    ContratoVigente = LibIcons.getIcon("fa-solid fa-clipboard-check", "Contrato Vigente", "#008000", "fa-lg");
                    DataAssinatura = c.data_assinatura.GetValueOrDefault().ToString("dd/MM/yy");
                }
                else
                {
                    ContratoVigente = LibIcons.getIcon("fa-solid fa-clipboard-check", "Contrato Inativo", "", "fa-lg");
                    DataAssinatura = "00/00/0000";
                }
                if (c.anexo == true) { ContratoAnexo = LibIcons.getIcon("fa-solid fa-file-pdf", "Contrato Anexo", "#008000", "fa-lg"); } else { ContratoAnexo = LibIcons.getIcon("fa-solid fa-file-pdf", "Contrato NÃO anexado", "#CACFD2", "fa-sm"); }
                if (c.id_contrato_tipo > 0)
                {
                    var tipoCtr = allContratosTipos.FirstOrDefault(c1 => c1.id_contrato_tipo == c.id_contrato_tipo);
                    TipoContrato = tipoCtr != null ? tipoCtr.descricao.EmptyIfNull().ToString() : String.Empty;
                }
                if (c.id_cliente > 0)
                {
                    var cli = allRecordsClientes.FirstOrDefault(c2 => c2.id_cliente == c.id_cliente);
                    NomeCliente = cli != null ? cli.nome.ToString() : String.Empty;
                }
                list.Add(new[] {
                    "", // Coluna de Seleção
                    c.id_contrato.ToString(),
                    ContratoVigente,
                    ContratoAnexo,
                    NomeCliente,
                    TipoContrato,
                    DataAssinatura,
                    "" // Botão Download
                }); ;
            }

            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; };

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

        #region ModalFiltroAvancadoView
        public ActionResult ModalFiltroAvancadoView(String id)
        {
            ViewBag.comboClientes = LibDataSets.LoadComboGClientesFornecedores(db);
            ViewBag.comboClientes.Insert(0, new SelectListItem { Value = "-1", Text = "[ TODOS OS CLIENTES ]" });
            ViewBag.Title = "Contratos - Filtro Avançado";
            cstModalFiltroAvancado ViewCstModalFiltroAvancado = new cstModalFiltroAvancado();
            ViewCstModalFiltroAvancado.Field_Text_02 = "-1";
            return View(ViewCstModalFiltroAvancado);
        }
        #endregion


        public ActionResult ModalUploadContratoAssinado(int? IdContrato)
        {
            cstUploadGed record_cstUploadGed = new cstUploadGed();
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