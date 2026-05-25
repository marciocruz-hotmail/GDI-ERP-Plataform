using Amazon.S3;
using Amazon.S3.Model;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Robos.Aws;
using GdiPlataform.Security;
using NPOI.POIFS.Crypt;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.g.Controllers
{
    public partial class GedController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Ged";
        private static readonly Regex RegexMultiplosEspacos = new Regex(@"\s{2,}", RegexOptions.Compiled);
        private static readonly Regex RegexPermitidoAposNormalizar = new Regex(@"[^a-z0-9_-]", RegexOptions.Compiled);
        private static readonly Regex RegexMultiplosUnderline = new Regex(@"_+", RegexOptions.Compiled);
        private static readonly Regex RegexMultiplosHifens = new Regex(@"-+", RegexOptions.Compiled);

        // Extensões permitidas no GED — adicione novas extensões aqui conforme necessidade do negócio.
        private static readonly HashSet<string> _extensoesGedPermitidas =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".docx", ".doc", ".xlsx", ".xls",
                ".jpg", ".jpeg", ".png", ".gif",
                ".txt", ".xml", ".zip", ".p7s"
            };

        // Content-Types declarados pelo browser que indicam executável — bloqueados independentemente da extensão.
        private static readonly HashSet<string> _mimeTypesBloqueados =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "application/x-msdownload", "application/x-msdos-program",
                "application/x-dosexec",    "application/x-executable",
                "application/x-sh",         "text/x-sh"
            };

        public GedController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            PreencherLookupsGedTiposFiltro();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Gestão Eletrônica de Documentos";
            return View();
        }

        #region GetDados
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            string filterOnOff = "0";
            try
            {
            // Parâmetros
            bool filterDb = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, db);
            List<g_usuarios> allUsuarios = db.g_usuarios.Where(u => u.id_usuario > 0).ToList();
            List<ged_arquivos_tipos> allArquivosTipos = db.ged_arquivos_tipos.Where(t => t.ativo == true).ToList();
            var allRecords = new List<Db.ged_arquivos>();
            List<string[]> list = new List<string[]>();
            DateTime DataField02 = new DateTime();
            DateTime DataField03 = new DateTime();
            DateTime.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField02);
            DateTime.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField03);

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }

            SentencaSQL = " select * from ged_arquivos ged where ged.ativo = 1 ";

            if (!filterDb)
            {
                if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1") && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "0"))
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  " + param.yesCustomField01.EmptyIfNull().ToString().Trim();
                    int IdTipo = int.Parse(param.yesCustomField01.EmptyIfNull().ToString().Trim());
                    ged_arquivos_tipos record_ged_arquivos_tipos = allArquivosTipos.Where(t => t.id_arquivo_tipo == IdTipo).FirstOrDefault();
                    if (record_ged_arquivos_tipos != null && ((record_ged_arquivos_tipos.controle_anual == true) || (record_ged_arquivos_tipos.controle_mensal == true)))
                    {
                        if ((param.yesCustomField02.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField03.EmptyIfNull().ToString().Trim().Length > 0))
                        { 
                            SentencaSQL += " and ((ged.data_referencia between '" + DataField02.ToString("yyyy-MM-dd") + "' and '" + DataField03.ToString("yyyy-MM-dd") + " ') or (ged.controla_data_referencia = 0))"; 
                        }
                    }
                }
                else
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  -1 ";
                }
                SentencaSQL += " order by ged.filename";
                allRecords = db.ged_arquivos.SqlQuery(SentencaSQL).ToList();
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.ged_arquivos, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_arquivo) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.filename :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.descricao :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_arquivo); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.filename); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.descricao); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_arquivo); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.filename); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.descricao); }
                }
            }

            foreach (var ged in displayedRecords)
            {
                //allUsuarios.Where(u => u.id_usuario == ged.id_usuario_cadastro).FirstOrDefault().nome.EmptyIfNull().ToString();
                var uCad = allUsuarios.FirstOrDefault(u => u.id_usuario == ged.id_usuario_cadastro);
                String NomeUsuario = uCad != null ? uCad.login.EmptyIfNull().ToString() : string.Empty;
                String DataReferencia = ged.data_referencia.GetValueOrDefault().ToString("dd/MM/yy");
                if (ged.controla_data_referencia == false) { DataReferencia = "Padrão";  };



                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    ged.id_arquivo.ToString(),
                                    ged.descricao.ToString(),
                                    ged.filename.ToString(),
                                    ged.filetype.ToString(),
                                    ged.versao.ToString(),
                                    DataReferencia, 
                                    NomeUsuario,
                                    "", // Botão Editar
                                    "" // Botão Download
                                });
            }

            if (filterDb) { filterOnOff = "1"; }

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

        #region UploadFileGed
        public ActionResult ModalUploadFileGed(int? IdGed, int? IdTipo, int? IdTipoPai)
        {
            if (IdGed == null) { IdGed = 0; };
            if (IdTipo == null) { IdTipo = 0; };
            if (IdTipoPai == null) { IdTipoPai = 0; };
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            CstUploadGed record_cstUploadGed = new CstUploadGed();
            if (IdGed > 0)
            {
                ged_arquivos record_ged_arquivos = db.ged_arquivos.Find(IdGed);
                record_cstUploadGed.id_arquivo = record_ged_arquivos.id_arquivo;
                record_cstUploadGed.id_arquivo_tipo = record_ged_arquivos.id_arquivo_tipo;
                record_cstUploadGed.descricao = record_ged_arquivos.descricao;
                record_cstUploadGed.observacao = record_ged_arquivos.observacao;
                record_cstUploadGed.data_referencia = record_ged_arquivos.data_referencia;
            }
            else
            {
                record_cstUploadGed.data_referencia = DataHoraAtual;
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Arquivo</b>";
            }

            PreencherLookupsGedTipos(IdTipo.GetValueOrDefault(), IdTipoPai.GetValueOrDefault());
            return View("ModalUploadFileGed", record_cstUploadGed);
        }

        public ActionResult AjaxUploadFileGed(CstUploadGed view_record_cstUploadGed)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            string Tag = CachePersister.userIdentity.FormNameActive.EmptyIfNull().ToString().Trim();
            try
            {
                Sucesso = true;
                ServiceUploadFileGed(view_record_cstUploadGed);
                MsgRetorno += "Upload <b>Concluído</b> com sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
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
            return Json(new { success = Sucesso, msg = MsgRetorno, tag = Tag }, JsonRequestBehavior.AllowGet);
        }

        public ged_arquivos ServiceUploadFileGed(CstUploadGed record_cstUploadGed)
        {
            int QtdErros = 0;
            String MsgErro = "";
            String FileExtension = string.Empty;
            ged_arquivos record_ged_arquivo = new Db.ged_arquivos();
            try
            {
                if (ModelState.IsValid)
                {
                    FileExtension = System.IO.Path.GetExtension(record_cstUploadGed.filesource.FileName).ToLowerInvariant();
                    if ((record_cstUploadGed.filesource.EmptyIfNull().ToString().Trim().Length == 0) || (record_cstUploadGed.filesource.FileName.EmptyIfNull().ToString().Trim().Length == 0) || (record_cstUploadGed.filesource.ContentLength == 0))
                    {
                        QtdErros += 1;
                        MsgErro += " - Campo [Arquivo] é de preenchimento obrigatório!<br/>";
                    }
                    else if (!_extensoesGedPermitidas.Contains(FileExtension))
                    {
                        QtdErros += 1;
                        MsgErro += " - Tipo de arquivo não permitido [" + FileExtension + "]. Tipos aceitos: PDF, DOC(X), XLS(X), JPG, PNG, GIF, TXT, XML, ZIP, P7S.<br/>";
                    }
                    else if (_mimeTypesBloqueados.Contains(record_cstUploadGed.filesource.ContentType))
                    {
                        QtdErros += 1;
                        MsgErro += " - Tipo de conteúdo do arquivo não é permitido [" + record_cstUploadGed.filesource.ContentType + "].<br/>";
                    }
                    if (record_cstUploadGed.id_arquivo_tipo == 0)
                    {
                        QtdErros += 1;
                        MsgErro += " - Campo [Tipo/Classificação] é de preenchimento obrigatório!<br/>";
                    }
                    if (record_cstUploadGed.descricao.EmptyIfNull().ToString().Length == 0)
                    {
                        QtdErros += 1;
                        MsgErro += " - Campo [Descrição] é de preenchimento obrigatório!<br/>";
                    }
                    if (record_cstUploadGed.isComexInvoicePDF == true)
                    {
                        if (record_cstUploadGed.comex_numero_invoice.EmptyIfNull().ToString().Length == 0)
                        {
                            QtdErros += 1;
                            MsgErro += " - Campo [Nº Invoice] é de preenchimento obrigatório!<br/>";
                        }
                        if (record_cstUploadGed.comex_sales_order.EmptyIfNull().ToString().Length == 0)
                        {
                            QtdErros += 1;
                            MsgErro += " - Campo [Sales Order] é de preenchimento obrigatório!<br/>";
                        }
                    }
                    if (record_cstUploadGed.id_contrato > 0)
                    {
                        String FileName = record_cstUploadGed.filesource.FileName.EmptyIfNull().ToString().Trim().ToLowerInvariant();
                        if (FileName.IndexOf("[assinado]") < 0)
                        {
                            QtdErros += 1;
                            MsgErro += " - O arquivo de Contrato não contém a tag de assinatura eletrônica!<br/>";
                        }
                    }

                    if (QtdErros > 0) { throw new Exception(MsgErro); };
                }
                else
                {
                    QtdErros += 1;
                    MsgErro = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    throw new Exception(MsgErro);
                }

                if (QtdErros == 0)
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                    if (record_cstUploadGed.id_arquivo == 0) // Novo Arquivo
                    {
                        if (record_cstUploadGed.data_referencia != null) { record_ged_arquivo.data_referencia = record_cstUploadGed.data_referencia; } else { record_ged_arquivo.data_referencia = DataHoraAtual; };

                        // Dados do Arquivo
                        String FileNameDestino = Path.GetFileName(record_cstUploadGed.filesource.FileName);
                        if (record_cstUploadGed.file_name_new.EmptyIfNull().ToString().Length > 0) { FileNameDestino = record_cstUploadGed.file_name_new.EmptyIfNull().ToString(); };
                        FileNameDestino = LibStringFormat.FormatarFileName(FileNameDestino);
                        String FileTypeOrigem = Path.GetExtension(FileNameDestino).ToString().Replace(".", "").ToLowerInvariant();
                        Decimal FileSizeBytes = Decimal.Round(System.Convert.ToDecimal(record_cstUploadGed.filesource.ContentLength), 2);
                        Decimal FileSizeKbytes = Decimal.Round(System.Convert.ToDecimal(record_cstUploadGed.filesource.ContentLength) / (1024), 2);
                        Decimal FileSizeMbytes = Decimal.Round(System.Convert.ToDecimal(record_cstUploadGed.filesource.ContentLength) / (1024 * 1024), 2);
                        Decimal FileSizeGbytes = Decimal.Round(System.Convert.ToDecimal(record_cstUploadGed.filesource.ContentLength) / (1024 * 1024 * 1024), 2);
                        record_ged_arquivo.versao = record_cstUploadGed.versao;
                        record_ged_arquivo.size_bytes = FileSizeBytes;
                        record_ged_arquivo.size_kbytes = FileSizeKbytes;
                        record_ged_arquivo.size_mbytes = FileSizeMbytes;
                        record_ged_arquivo.size_gbytes = FileSizeGbytes;
                        record_ged_arquivo.filename = FileNameDestino;
                        record_ged_arquivo.filetype = FileTypeOrigem;
                        record_ged_arquivo.downloads = 0;
                        record_ged_arquivo.id_arquivo = record_cstUploadGed.id_arquivo;
                        record_ged_arquivo.id_arquivo_tipo = record_cstUploadGed.id_arquivo_tipo;
                        record_ged_arquivo.id_cliente_relacionado = record_cstUploadGed.id_cliente;
                        record_ged_arquivo.id_gc_movimento = record_cstUploadGed.id_gc_movimento;
                        record_ged_arquivo.id_gc_financeiro = record_cstUploadGed.id_gc_financeiro;
                        record_ged_arquivo.id_comex_importacao = record_cstUploadGed.id_comex_importacao;
                        record_ged_arquivo.id_comex_invoice = record_cstUploadGed.id_comex_invoice;
                        record_ged_arquivo.id_comex_financeiro = record_cstUploadGed.id_comex_financeiro;
                        record_ged_arquivo.id_atendimento = record_cstUploadGed.id_atendimento;
                        record_ged_arquivo.id_estoque_lote = record_cstUploadGed.id_estoque_lote;
                        record_ged_arquivo.descricao = record_cstUploadGed.descricao;
                        record_ged_arquivo.observacao = record_cstUploadGed.observacao;
                        ged_arquivos_tipos record_ged_arquivo_tipos = db.ged_arquivos_tipos.Find(record_ged_arquivo.id_arquivo_tipo);
                        record_ged_arquivo.datahora_cadastro = DataHoraAtual;
                        record_ged_arquivo.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        record_ged_arquivo_tipos.controle_anual = true;
                        record_ged_arquivo_tipos.controle_mensal = true;
                        record_ged_arquivo.controla_data_referencia = true;
                        record_ged_arquivo.bucket = record_ged_arquivo_tipos.bucket_s3;
                        String BucketNameS3 = record_ged_arquivo_tipos.bucket_s3;
                        String FileNameS3 = String.Empty;

                       
                        if (record_ged_arquivo_tipos.id_arquivo_tipo == 39)
                        {
                            FileNameS3 += "files-public-forms/";  // Pasta do assunto
                        }
                        else
                        {
                            FileNameS3 += SanitizarNomePastaAssunto(record_ged_arquivo_tipos.descricao.EmptyIfNull().ToString()) + "/";  // Pasta do assunto
                            FileNameS3 += record_ged_arquivo.data_referencia.GetValueOrDefault().ToString("yyyy") + "/"; // Ano
                            FileNameS3 += record_ged_arquivo.data_referencia.GetValueOrDefault().ToString("MM") + "/";  // Mes
                        }

                        if (record_cstUploadGed.isLancamentoFinanceiro == true)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_gc_movimento.ToString();

                        }
                        else if (record_cstUploadGed.isCotacaoPedido == true)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_gc_movimento.ToString();
                        }
                        else if (record_ged_arquivo.id_gc_movimento > 0)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_gc_movimento.ToString();
                        }
                        else if (record_ged_arquivo.id_comex_importacao > 0)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_comex_importacao.ToString();
                            gc_comex_importacoes RecordImportacao = db.gc_comex_importacoes.Find(record_ged_arquivo.id_comex_importacao);
                            if (RecordImportacao != null)
                            {
                                record_cstUploadGed.folder_index_registro += " - " + RecordImportacao.numero.EmptyIfNull().ToString();
                            }
                        }
                        else if (record_ged_arquivo.id_comex_invoice > 0)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_comex_invoice.ToString();
                        }
                        else if (record_ged_arquivo.id_comex_financeiro > 0)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_comex_financeiro.ToString();
                        }
                        else if (record_ged_arquivo.id_cliente_relacionado > 0)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_cliente_relacionado.ToString();
                        }
                        else if (record_cstUploadGed.isAtendimento == true && record_ged_arquivo.id_atendimento > 0)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_atendimento.ToString();
                        }
                        else if (record_cstUploadGed.isEstoqueLote == true && record_ged_arquivo.id_estoque_lote > 0)
                        {
                            record_cstUploadGed.folder_index_registro = record_ged_arquivo.id_estoque_lote.ToString();
                        }

                        if (record_cstUploadGed.folder_index_registro.EmptyIfNull().ToString().Length > 0) { FileNameS3 += record_cstUploadGed.folder_index_registro.EmptyIfNull().ToString() + "/"; };  // Contexto do Registro / PK

                        record_ged_arquivo.ativo = false;
                        db.ged_arquivos.Add(record_ged_arquivo);
                        db.SaveChanges();

                        string nomeBaseS3 = Path.GetFileNameWithoutExtension(FileNameDestino) + "_Id-" + record_ged_arquivo.id_arquivo.EmptyIfNull().ToString();
                        nomeBaseS3 = SanitizarNomeBaseArquivo(nomeBaseS3);
                        FileNameS3 += nomeBaseS3 + FileExtension.ToLowerInvariant(); // Recebeu o ID
                        record_ged_arquivo.filebucket = FileNameS3;

                        bool uploadPublico = record_ged_arquivo_tipos.set_public_files;
                        BotAwsS3.UploadStreamS3(BucketNameS3, FileNameS3, record_cstUploadGed.filesource.InputStream, uploadPublico);
                        if (uploadPublico) { record_ged_arquivo.public_url = BotAwsS3.BuildPublicObjectUrl(BucketNameS3, FileNameS3); };

                        record_ged_arquivo.ativo = true; // ativar o arquivo
                        db.Entry(record_ged_arquivo).State = EntityState.Modified;
                        db.SaveChanges();

                        // Comex Invoices PDF
                        if (record_cstUploadGed.isComexInvoicePDF == true)
                        {
                            gc_comex_invoices_pdf record_gc_comex_invoices_pdf = new gc_comex_invoices_pdf();
                            record_gc_comex_invoices_pdf.id_importacao = record_ged_arquivo.id_comex_importacao;
                            record_gc_comex_invoices_pdf.id_ged = record_ged_arquivo.id_arquivo;
                            record_gc_comex_invoices_pdf.ativo = true;
                            record_gc_comex_invoices_pdf.invoice = record_cstUploadGed.comex_numero_invoice.Trim();
                            record_gc_comex_invoices_pdf.sales_order = record_cstUploadGed.comex_sales_order.Trim();
                            record_gc_comex_invoices_pdf.id_coligada = 0;
                            record_gc_comex_invoices_pdf.id_filial = 0;
                            record_gc_comex_invoices_pdf.datahora_cadastro = DataHoraAtual;
                            record_gc_comex_invoices_pdf.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                            db.gc_comex_invoices_pdf.Add(record_gc_comex_invoices_pdf);
                            db.SaveChanges();
                        }
                        else if (record_cstUploadGed.id_contrato > 0)
                        {
                            int QtdArquivosContrato = 0;
                            List<ged_arquivos> ListaArquivosGED = db.ged_arquivos.Where(a => a.id_contrato_relacionado == record_cstUploadGed.id_contrato).ToList();
                            QtdArquivosContrato = ListaArquivosGED.Count();
                            foreach (var ItemGed in ListaArquivosGED)
                            {
                                // Último arquivo salvo - Atualizar a versão dele
                                if (ItemGed.id_arquivo == record_ged_arquivo.id_arquivo)
                                {
                                    if (ItemGed.versao != QtdArquivosContrato)
                                    {
                                        ItemGed.versao = QtdArquivosContrato;
                                        db.Entry(ItemGed).State = EntityState.Modified;
                                    }
                                }
                                else
                                {
                                    if (ItemGed.ativo == true)
                                    {
                                        ItemGed.ativo = false;
                                        db.Entry(ItemGed).State = EntityState.Modified;

                                        // Gravar o log de update
                                        ged_arquivos_logs record_ged_arquivos_logs = new Db.ged_arquivos_logs();
                                        record_ged_arquivos_logs.id_arquivo = ItemGed.id_arquivo;
                                        record_ged_arquivos_logs.log = "Update";
                                        record_ged_arquivos_logs.datahora_cadastro = DataHoraAtual;
                                        record_ged_arquivos_logs.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                        db.Entry(record_ged_arquivos_logs).State = EntityState.Added;
                                    }
                                }
                            }
                            db.SaveChanges();
                            g_contratos_aviacao record_g_contratos_aviacao = db.g_contratos_aviacao.Find(record_cstUploadGed.id_contrato);
                            record_g_contratos_aviacao.anexo = true;
                            record_g_contratos_aviacao.id_ged = record_ged_arquivo.id_arquivo;
                            db.Entry(record_g_contratos_aviacao).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw (e);
            }
            return record_ged_arquivo;
        }
        #endregion

        internal static string SanitizarNomeBaseArquivo(string nomeSemExtensao)
        {
            if (string.IsNullOrEmpty(nomeSemExtensao))
                return string.Empty;

            string s = nomeSemExtensao.Trim();
            s = RegexMultiplosEspacos.Replace(s, " ");
            s = s.Replace(' ', '_');
            s = LibStringFormat.RemoverAcentos(s);
            s = s.ToLowerInvariant();
            s = RegexPermitidoAposNormalizar.Replace(s, string.Empty);
            s = RegexMultiplosUnderline.Replace(s, "_");
            s = RegexMultiplosHifens.Replace(s, "-");
            s = s.Trim('_', '-');
            return s;
        }

        internal static string SanitizarNomePastaAssunto(string NomePasta)
        {
            if (string.IsNullOrEmpty(NomePasta)) return string.Empty;
            NomePasta = NomePasta.Trim();
            NomePasta = NomePasta.Replace(">", "-");
            NomePasta = NomePasta.Replace(" ", "");
            NomePasta = LibStringFormat.RemoverAcentos(NomePasta);
            NomePasta = NomePasta.ToLowerInvariant();
            NomePasta = NomePasta.Trim('_', '-');
            return NomePasta;
        }



        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "arquivo";
            var name = Path.GetFileName(fileName);

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Trim();
        }

        public ActionResult AjaxDownloadGedContrato(ged_arquivos view_ged_arquivos)
        {
            bool sucesso = false;
            String msgRetorno = "";
            String urlS3 = "";
            try
            {
                g_contratos_aviacao record_g_contratos_aviacao = db.g_contratos_aviacao.Find(view_ged_arquivos.id_arquivo);
                if (record_g_contratos_aviacao.id_ged > 0)
                {
                    // Localizar o arquivo
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                    ged_arquivos RecordGedArquivos = db.ged_arquivos.Find(record_g_contratos_aviacao.id_ged);
                    ged_arquivos_tipos RecordGedArquivosTipos = db.ged_arquivos_tipos.Find(RecordGedArquivos.id_arquivo_tipo);
                    RecordGedArquivos.downloads = RecordGedArquivos.downloads + 1;
                    string FileBucket = RecordGedArquivos.filebucket;
                    db.Entry(RecordGedArquivos).State = EntityState.Modified;

                    // Gravar o log
                    ged_arquivos_logs record_ged_arquivos_logs = new Db.ged_arquivos_logs();
                    record_ged_arquivos_logs.id_arquivo = RecordGedArquivos.id_arquivo;
                    record_ged_arquivos_logs.log = "Download";
                    record_ged_arquivos_logs.datahora_cadastro = DataHoraAtual;
                    record_ged_arquivos_logs.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_ged_arquivos_logs).State = EntityState.Added;
                    db.SaveChanges();

                    String BucketNameS3 = RecordGedArquivosTipos.bucket_s3;
                    GdiAwsS3BucketRules.ThrowIfGedRowBucketDiffersFromTipo(RecordGedArquivosTipos.bucket_s3, RecordGedArquivos.bucket, "GED download contrato");
                    GdiAwsS3BucketRules.ThrowIfBucketNotAllowed(BucketNameS3, "GED download contrato");
                    using (AmazonS3Client client = GdiAwsS3Credentials.CreateS3Client())
                    {
                        var bucketName = BucketNameS3;
                        GetPreSignedUrlRequest request1 =
                           new GetPreSignedUrlRequest()
                           {
                               BucketName = bucketName,
                               Key = FileBucket,
                               Expires = DateTime.Now.AddMinutes(5)
                           };
                        string url = client.GetPreSignedURL(request1);
                        sucesso = true;
                        urlS3 = url;
                    };
                }
                else
                {
                    sucesso = false;
                    msgRetorno = "Contrato eletrônico não localizado";
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
            return Json(new { success = sucesso, msg = msgRetorno, url = urlS3 }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AjaxDownloadFileS3(ged_arquivos view_ged_arquivos)
        {
            bool sucesso = false;
            String msgRetorno = "";
            String urlS3 = "";
            try
            {
                // Localizar o arquivo
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                ged_arquivos RecordGedArquivos = db.ged_arquivos.Find(view_ged_arquivos.id_arquivo);
                ged_arquivos_tipos RecordGedArquivosTipos = db.ged_arquivos_tipos.Find(RecordGedArquivos.id_arquivo_tipo);
                RecordGedArquivos.downloads = RecordGedArquivos.downloads + 1;
                string FileBucket = RecordGedArquivos.filebucket;
                db.Entry(RecordGedArquivos).State = EntityState.Modified;

                // Gravar o log
                ged_arquivos_logs record_ged_arquivos_logs = new Db.ged_arquivos_logs();
                record_ged_arquivos_logs.id_arquivo = RecordGedArquivos.id_arquivo;
                record_ged_arquivos_logs.log = "Download";
                record_ged_arquivos_logs.datahora_cadastro = DataHoraAtual;
                record_ged_arquivos_logs.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_ged_arquivos_logs).State = EntityState.Added;
                db.SaveChanges();

                // Verificar se é um arquivo público
                if (RecordGedArquivos.public_url.EmptyIfNull().ToString().Trim().Length > 0)
                {
                    var urlCandidata = RecordGedArquivos.public_url.EmptyIfNull().ToString().Trim();
                    if (!GdiAwsS3BucketRules.TryValidateStoredPublicUrl(urlCandidata, out var msgUrl))
                    {
                        sucesso = false;
                        msgRetorno = msgUrl;
                    }
                    else
                    {
                        sucesso = true;
                        urlS3 = urlCandidata;
                    }
                }
                else
                {
                    //String BucketNameS3 = RecordGedArquivosTipos.bucket_s3;
                    String BucketNameS3 = RecordGedArquivos.bucket.EmptyIfNull().ToString();
                    GdiAwsS3BucketRules.ThrowIfGedRowBucketDiffersFromTipo(RecordGedArquivosTipos.bucket_s3, RecordGedArquivos.bucket, "GED download");
                    GdiAwsS3BucketRules.ThrowIfBucketNotAllowed(BucketNameS3, "GED download");
                    using (AmazonS3Client client = GdiAwsS3Credentials.CreateS3Client())
                    {
                        var bucketName = BucketNameS3;
                        GetPreSignedUrlRequest request1 =
                           new GetPreSignedUrlRequest()
                           {
                               BucketName = bucketName,
                               Key = FileBucket,
                               Expires = DateTime.Now.AddMinutes(5)
                           };
                        string url = client.GetPreSignedURL(request1);
                        sucesso = true;
                        urlS3 = url;
                    }
                    ;
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
            return Json(new { success = sucesso, msg = msgRetorno, url = urlS3 }, JsonRequestBehavior.AllowGet);
        }

        #region ModalDesativarGed
        public ActionResult ModalDesativarGed(int? IdArquivo, string Tag)
        {
            ged_arquivos RecordGedAquivo = db.ged_arquivos.Find(IdArquivo);
            if (Tag == "1") { RecordGedAquivo.tag_string = "#dtGcMovimentosGed"; };
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-trash", "", "red", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Desativar o arquivo: " + RecordGedAquivo.filename.EmptyIfNull().Trim();
            return View(RecordGedAquivo);
        }

        [HttpPost]
        public ActionResult AjaxDesativarAnexo(ged_arquivos ModalGedArquivo)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            String Tag = ModalGedArquivo.tag_string.EmptyIfNull().ToString();
            try
            {
                ged_arquivos RecordGedAquivo = db.ged_arquivos.Find(ModalGedArquivo.id_arquivo);
                if (RecordGedAquivo.id_usuario_cadastro == CachePersister.userIdentity.IdUsuario) // Somente o responsável pelo anexo poderá desativá-lo
                {
                    DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                    RecordGedAquivo.ativo = false;
                    RecordGedAquivo.id_usuario_desativacao = CachePersister.userIdentity.IdUsuario;
                    RecordGedAquivo.datahora_desativacao = DataHoraAtual;
                    db.Entry(RecordGedAquivo).State = EntityState.Modified;
                    db.SaveChanges();
                    Sucesso = true;
                }
                else
                {
                    Sucesso = false;
                    MsgRetorno = " - Informe o Motivo!";
                }
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
            return Json(new { success = Sucesso, msg = MsgRetorno, tag = Tag }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        /*[HttpPost]
        public ActionResult AjaxDesativarAnexo(ged_arquivos record_ged_arquivos)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            try
            {
                ged_arquivos RecordGedArquivo = db.ged_arquivos.Find(record_ged_arquivos.id_arquivo);
                if (RecordGedArquivo != null)
                {
                    if (RecordGedArquivo.id_usuario_cadastro == CachePersister.userIdentity.IdUsuario) // Somente o responsável pelo anexo poderá desativá-lo
                    {
                        RecordGedArquivo.ativo = false;
                        RecordGedArquivo.id_usuario_desativacao = CachePersister.userIdentity.IdUsuario;
                        RecordGedArquivo.datahora_desativacao = LibDateTime.getDataHoraBrasilia();
                        db.Entry(RecordGedArquivo).State = EntityState.Modified;
                        db.SaveChanges();
                        Sucesso = true;
                    }
                    else
                    {
                        MsgRetorno += "Somente o usuário responsável pelo anexo poderá desativá-lo!";
                    }
                }
                else
                {
                    MsgRetorno += "Não foi possível desativar o Anexo, arquivo não localizado!";
                }
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
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }*/

        public ActionResult ModalEditFileGed(int? IdGed, int? IdTipo, int? IdTipoPai)
        {
            if (IdGed == null) { IdGed = 0; };
            if (IdTipo == null) { IdTipo = 0; };
            if (IdTipoPai == null) { IdTipoPai = 0; };
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            CstUploadGed record_cstUploadGed = new CstUploadGed();
            if (IdGed > 0)
            {
                ged_arquivos record_ged_arquivos = db.ged_arquivos.Find(IdGed);
                record_cstUploadGed.id_arquivo = record_ged_arquivos.id_arquivo;
                record_cstUploadGed.id_arquivo_tipo = record_ged_arquivos.id_arquivo_tipo;
                record_cstUploadGed.descricao = record_ged_arquivos.descricao;
                record_cstUploadGed.observacao = record_ged_arquivos.observacao;
                record_cstUploadGed.data_referencia = record_ged_arquivos.data_referencia;
            }
            else
            {
                record_cstUploadGed.data_referencia = DataHoraAtual;
                ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Alterar Metadados de Arquivo</b>";
            }

            PreencherLookupsGedTipos(0, 0);
            return View("ModalEditFileGed", record_cstUploadGed);
        }

        public ActionResult AjaxEditFileGed(CstUploadGed record_cstUploadGed)
        {
            int QtdErros = 0;
            bool Sucesso = false;
            String MsgRetorno = "";
            try
            {
                if (ModelState.IsValid)
                {
                    if (record_cstUploadGed.id_arquivo <= 0)
                    {
                        QtdErros += 1;
                        MsgRetorno += " - Arquivo não localizado no GED!<br/>";
                    }
                    if (record_cstUploadGed.id_arquivo_tipo == 0)
                    {
                        QtdErros += 1;
                        MsgRetorno += " - Campo [Tipo/Classificação] é de preenchimento obrigatório!<br/>";
                    }
                    if (record_cstUploadGed.descricao.EmptyIfNull().ToString().Length == 0)
                    {
                        QtdErros += 1;
                        MsgRetorno += " - Campo [Descrição] é de preenchimento obrigatório!<br/>";
                    }
                }
                else
                {
                    QtdErros += 1;
                    MsgRetorno += String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                }

                if (QtdErros == 0)
                {
                    ged_arquivos record_ged_arquivo = db.ged_arquivos.Find(record_cstUploadGed.id_arquivo);
                    record_ged_arquivo.id_arquivo_tipo = record_cstUploadGed.id_arquivo_tipo;
                    record_ged_arquivo.data_referencia = record_cstUploadGed.data_referencia;
                    record_ged_arquivo.descricao = record_cstUploadGed.descricao;
                    record_ged_arquivo.observacao = record_cstUploadGed.observacao;
                    record_ged_arquivo.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                    record_ged_arquivo.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(record_ged_arquivo).State = EntityState.Modified;
                    db.SaveChanges();
                    Sucesso = true;
                }
            }
            catch (DbEntityValidationException ex)
            {
                MsgRetorno += LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                MsgRetorno += LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
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
