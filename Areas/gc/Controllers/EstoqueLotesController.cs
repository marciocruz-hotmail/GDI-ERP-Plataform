using DocumentFormat.OpenXml.EMMA;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using Newtonsoft.Json;
using NPOI.HSSF.Record.Chart;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueLotes_*,gc_EstoqueLotes_Default")]
    public partial class EstoqueLotesController : Controller
    {
        private readonly GdiPlataformEntities db;

        public EstoqueLotesController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(string.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueLotes_*,gc_EstoqueLotes_Actionread")]
        public ActionResult Index()
        {
            LoadCombos();
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg")
                           + LibStringFormat.GetTabHtml(1)
                           + "Estoque - Lotes";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueLotes_*,gc_EstoqueLotes_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            string filtroCodigoLote = (param.yesCustomField01 ?? "").Trim();
            string filtroSerialLote = (param.yesCustomField02 ?? "").Trim();
            string filtroProduto = (param.yesCustomField03 ?? "").Trim();
            string filtroImportacao = (param.yesCustomField04 ?? "").Trim();

            var query = from l in db.gc_estoque_lotes
                        join p in db.g_produtos on l.id_produto equals p.id_produto
                        join imp in db.gc_comex_importacoes on l.id_importacao equals imp.id_importacao into _imp
                        from imp in _imp.DefaultIfEmpty()
                        where l.ativo
                        select new
                        {
                            l.id_estoque_lote,
                            l.id_produto,
                            l.id_importacao,
                            ProdutoPartNumber = p.codigo,
                            l.codigo_lote,
                            l.data_validade,
                            l.codigo_serial,
                            NumeroImportacao = imp.numero,
                            ProdutoDescricao = p.descricao,
                            l.saldo_01_disponivel,
                            l.saldo_03_disponivel
                        };

            if (LibStringFormat.TryMontarPadraoLikeContemCodigo(filtroCodigoLote, out string padraoLote))
            {
                query = query.Where(l => l.codigo_lote != null && System.Data.Entity.DbFunctions.Like(l.codigo_lote, padraoLote));
            }

            if (LibStringFormat.TryMontarPadraoLikeContemCodigo(filtroSerialLote, out string padraoSerial))
            {
                query = query.Where(l => l.codigo_serial != null && System.Data.Entity.DbFunctions.Like(l.codigo_serial, padraoSerial));
            }

            if (int.TryParse(filtroProduto, out int idProduto) && idProduto > 0)
            {
                query = query.Where(l => l.id_produto == idProduto);
            }

            if (int.TryParse(filtroImportacao, out int idImportacao) && idImportacao > 0)
            {
                query = query.Where(l => l.id_importacao == idImportacao);
            }

            int totalRecords = query.Count();

            // Ordenação simples pela coluna Id
            var ordered = query.OrderByDescending(l => l.id_estoque_lote);

            int start = param.iDisplayStart;
            int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;

            var page = ordered
                .Skip(start)
                .Take(length)
                .ToList();

            var list = page.Select(l =>
            {
                var desc = l.ProdutoDescricao.EmptyIfNull().ToString();
                if (desc.Length > 50) desc = desc.Substring(0, 50);

                var numImp = l.NumeroImportacao.EmptyIfNull().ToString();
                if (numImp.Length > 7) numImp = numImp.Substring(0, 7);

                var CodigoLoteTemp = l.codigo_lote.EmptyIfNull().ToString();
                //if (l.codigo_lote_final.EmptyIfNull().ToString().Length > 0) { CodigoLoteTemp += "/" + l.codigo_lote_final.EmptyIfNull().ToString(); };

                var SerialLoteTemp = l.codigo_serial.EmptyIfNull().ToString();
                //if (l.codigo_serial_final.EmptyIfNull().ToString().Length > 0) { SerialLoteTemp += "/" + l.codigo_serial_final.EmptyIfNull().ToString(); };

                return new[]
                {
                    "",
                    l.id_estoque_lote.ToString(),
                    l.ProdutoPartNumber.EmptyIfNull().ToString(),
                    CodigoLoteTemp,
                    l.data_validade.HasValue ? l.data_validade.Value.ToString("dd/MM/yyyy") : "",
                    SerialLoteTemp,
                    numImp,
                    desc,
                    l.saldo_01_disponivel.ToString("N3").Replace(",000",""),
                    l.saldo_03_disponivel.ToString("N3").Replace(",000",""),
                    ""
                };
            }).ToList();

            bool filtroAplicado = !string.IsNullOrWhiteSpace(filtroCodigoLote) || !string.IsNullOrWhiteSpace(filtroSerialLote)
                || (int.TryParse(filtroProduto, out int idProdutoFiltro) && idProdutoFiltro > 0)
                || (int.TryParse(filtroImportacao, out int idImportacaoFiltro) && idImportacaoFiltro > 0);
            string yesFilterOnOff = (param.yesFilterField.EmptyIfNull().ToString().Trim() == "*" && filtroAplicado) ? "1" : "0";

            return Json(new
            {
                errorMessage = "",
                stackTrace = "",
                yesFilterOnOff = yesFilterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = totalRecords,
                iTotalDisplayRecords = totalRecords,
                aaData = list
            }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param);
            }
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param)
        {
            string errorMessage = LibExceptions.getExceptionShortMessage(e);
            return Json(new
            {
                errorMessage = errorMessage,
                severity = "error",
                stackTrace = e.ToString(),
                yesFilterOnOff = "0",
                sEcho = param != null ? param.sEcho : null,
                iTotalRecords = 0,
                iTotalDisplayRecords = 0,
                aaData = new List<string[]>()
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Create / Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueLotes_*,gc_EstoqueLotes_Actioncreate")]
        public ActionResult Create()
        {
            String MsgBloqueiaEdicao = String.Empty;
            var record = new gc_estoque_lotes
            {
                ativo = false,
                id_produto = 0,
                codigo_lote = "0",
                codigo_lote_inicial = "0",
                data_validade = new DateTime(2099, 12, 31),
                datahora_cadastro = LibDateTime.getDataHoraBrasilia(),
                id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
            };

            db.gc_estoque_lotes.Add(record);
            db.SaveChanges();
            CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record);
            LoadCombos();

            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg")
                           + LibStringFormat.GetTabHtml(1)
                           + "<b>Estoque - Lotes (Novo)</b>"
                           + LibStringFormat.GetTabHtml(1)
                           + record.id_estoque_lote.EmptyIfNull().ToString();

            ViewBag.MsgBloqueiaEdicao = MsgBloqueiaEdicao;
            return View("ModalCreateEdit", record);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueLotes_*,gc_EstoqueLotes_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            String MsgBloqueiaEdicao = String.Empty;

            if (id == null || id == 0)
            {
                return RedirectToAction("Index");
            }

            var record = db.gc_estoque_lotes.Find(id);
            if (record == null)
            {
                return RedirectToAction("Index");
            }

            CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record);

            LoadCombos();

            if ((CachePersister.userIdentity.Roles.Contains("gc_EstoqueLotes_*")) || (CachePersister.userIdentity.Roles.Contains("gc_EstoqueLotes_Actionmanager"))) { }
            else { if (record.codigo_lote != "0") { MsgBloqueiaEdicao = "Bloqueado"; }; };

            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "")
                           + LibStringFormat.GetTabHtml(1)
                           + "<b>Estoque - Lotes</b>"
                           + LibStringFormat.GetTabHtml(1)
                           + record.id_estoque_lote.EmptyIfNull().ToString();

            ViewBag.MsgBloqueiaEdicao = MsgBloqueiaEdicao;
            return View("ModalCreateEdit", record);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueLotes_*,gc_EstoqueLotes_Actioncreate,gc_EstoqueLotes_Actionupdate")]
        public ActionResult AjaxSaveRecord(gc_estoque_lotes viewRecord)
        {
            bool sucesso = false;
            string msgRetorno = string.Empty;
            int qtdInconsistencias = 0;

            try
            {
                var oldRecord = new gc_estoque_lotes();
                if (viewRecord.id_estoque_lote > 0)
                {
                    oldRecord = JsonConvert.DeserializeObject<gc_estoque_lotes>(CachePersister.userIdentity.DataRowInUseSerialized);
                }

                if (ModelState.IsValid)
                {
                    if (viewRecord.id_produto <= 0)
                    {
                        qtdInconsistencias++;
                        msgRetorno += "Campo <b>[Produto]</b> é de preenchimento obrigatório!<br/>";
                    }
                    
                    if (string.IsNullOrWhiteSpace(viewRecord.codigo_lote))
                    {
                        qtdInconsistencias++;
                        msgRetorno += "Campo <b>[Código do Lote]</b> é de preenchimento obrigatório!<br/>";
                    }
                    else
                    {
                        if (viewRecord.codigo_lote.EmptyIfNull().ToString().Trim() == "0")
                        {
                            if (viewRecord.id_importacao == 0)
                            {
                                qtdInconsistencias++;
                                msgRetorno += "Campo <b>[Importação]</b> é de preenchimento obrigatório para lote 0!<br/>";
                            }
                            else
                            {
                                gc_comex_importacoes RecordImportacao = db.gc_comex_importacoes.Find(viewRecord.id_importacao);
                                String Numero = RecordImportacao.numero.EmptyIfNull().ToString().Trim();
                                if (Numero.Length > 7) { Numero = Numero.Substring(0, 7); };
                                viewRecord.codigo_lote = "IMP-" + Numero;
                            }
                        }
                        viewRecord.codigo_lote = viewRecord.codigo_lote.EmptyIfNull().Trim();
                        viewRecord.codigo_lote_inicial = viewRecord.codigo_lote;
                    }

                    if (!string.IsNullOrWhiteSpace(viewRecord.codigo_serial))
                    {
                        viewRecord.codigo_serial = viewRecord.codigo_serial.EmptyIfNull().Trim();
                    }

                    if (viewRecord.data_validade == null)
                    {
                        qtdInconsistencias++;
                        msgRetorno += "Campo <b>[Data Validade]</b> é de preenchimento obrigatório!<br/>";
                    }
                    else if (viewRecord.data_validade.GetValueOrDefault().Date <= DateTime.Now.Date)
                    {
                        qtdInconsistencias++;
                        msgRetorno += "Campo <b>[Data Validade]</b> contém uma data inválida!<br/>";
                    }

                    if ((viewRecord.saldo_01_disponivel + viewRecord.saldo_03_disponivel) <= 0)
                    {
                        qtdInconsistencias++;
                        msgRetorno += "Campo <b>[Saldo BH e/ou Saldo SP]</b> é de preenchimento obrigatório!<br/>";
                    }
                }
                else
                {
                    msgRetorno = string.Join(Environment.NewLine,
                        ModelState.Values.SelectMany(v => v.Errors)
                            .Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias++;
                }

                if ((qtdInconsistencias == 0) && (viewRecord.id_produto > 0))
                {
                    if (viewRecord.codigo_serial.EmptyIfNull().ToString().Trim().Length > 0)
                    {
                        gc_estoque_lotes RecordEstoqueLotes = db.gc_estoque_lotes.Where(l => l.id_estoque_lote != viewRecord.id_estoque_lote && l.id_produto == viewRecord.id_produto && l.codigo_lote == viewRecord.codigo_lote && l.codigo_serial == viewRecord.codigo_serial).FirstOrDefault();
                        if (RecordEstoqueLotes != null)
                        {
                            msgRetorno += "Produto/Lote/Serial duplicado, já registrado anteriormente!<br/>";
                            qtdInconsistencias++;
                        }
                    }
                    else if (viewRecord.codigo_lote.EmptyIfNull().ToString().Trim().Length > 0)
                    {
                        gc_estoque_lotes RecordEstoqueLotes = db.gc_estoque_lotes.Where(l => l.id_estoque_lote != viewRecord.id_estoque_lote && l.id_produto == viewRecord.id_produto && l.codigo_lote == viewRecord.codigo_lote).FirstOrDefault();
                        if (RecordEstoqueLotes != null)
                        {
                            msgRetorno += "Produto/Lote duplicado, já registrado anteriormente!<br/>";
                            qtdInconsistencias++;
                        }
                    }
                }

                if (qtdInconsistencias == 0)
                {
                    if (viewRecord.id_estoque_lote == 0)
                    {
                        viewRecord.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                        viewRecord.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        db.gc_estoque_lotes.Add(viewRecord);
                        db.SaveChanges();

                        string log = LibDB.CompareDataTable(oldRecord, viewRecord);
                        log = "Novo Registro | " + log;
                        LibAudit.SaveAudit(db, true, "gc_estoque_lotes", viewRecord.id_estoque_lote, log);
                        sucesso = true;
                    }
                    else
                    {
                        viewRecord.ativo = true;
                        viewRecord.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                        viewRecord.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(viewRecord).State = EntityState.Modified;
                        db.SaveChanges();

                        string log = LibDB.CompareDataTable(oldRecord, viewRecord);
                        log = "Atualização Dados | " + log;
                        LibAudit.SaveAudit(db, true, "gc_estoque_lotes", viewRecord.id_estoque_lote, log);
                        sucesso = true;
                    }
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

            return Json(new { success = sucesso, msg = msgRetorno, id_estoque_lote = viewRecord.id_estoque_lote }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        public ActionResult GetGedEstoqueLotes(jQueryDataTableParamModel param)
        {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            int IdTable = 0;
            int.TryParse(param.yesCustomIdPK, out IdTable);
            List<g_usuarios> ListaUsuarios = db.g_usuarios.Where(u => u.id_usuario > 0).ToList();
            List<ged_arquivos> ListaArquivosGed = db.ged_arquivos.Where(g => g.ativo == true && g.id_estoque_lote == IdTable).ToList();
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
                String NomeUsuario = ListaUsuarios.Where(u => u.id_usuario == RecordGed.id_usuario_cadastro).FirstOrDefault().login.EmptyIfNull().ToString();
                if (RecordGed.datahora_cadastro != null) { DataReferencia = RecordGed.datahora_cadastro.GetValueOrDefault().ToString("dd/MM/yy"); }
                ;
                if (RecordGed.id_arquivo_tipo > 0)
                {
                    ged_arquivos_tipos RecordArquivoTipo = ListaArquivosGedTipos.Where(t => t.id_arquivo_tipo == RecordGed.id_arquivo_tipo).FirstOrDefault();
                    if (RecordArquivoTipo != null) { DescricaoTipoArquivo = RecordArquivoTipo.descricao.EmptyIfNull().ToString(); };
                }

                list.Add(new[] {
                                    RecordGed.id_arquivo.ToString(),
                                    "", // Botão Desativar
                                    DescricaoTipoArquivo.ToString(),
                                    RecordGed.descricao.ToString(),
                                    RecordGed.filename.ToString(),
                                    DataReferencia,
                                    NomeUsuario,
                                    "" // Botão Download
                                });
            }

            String filterOnOff = "0";
            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; }
            ;

            return Json(new
            {
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = ListaArquivosGed.Count(),
                iTotalDisplayRecords = ListaArquivosGed.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
        }

        #region ModalUploadAnexoEstoqueLotes
        public ActionResult ModalUploadAnexoEstoqueLotes(int? IdEstoqueLote)
        {
            int IdArquivoTipo = 0;
            var recordUpload = new GdiPlataform.Areas.g.Models.CstUploadGed
            {
                isEstoqueLote = true
            };

            gc_estoque_lotes recordLote = db.gc_estoque_lotes.Find(IdEstoqueLote);
            recordUpload.id_estoque_lote = recordLote != null ? recordLote.id_estoque_lote : 0;

            var comboGedTipos = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ SELECIONE O TIPO DO ANEXO ]" }
            };
            List<ged_arquivos_tipos> listaGedTipos = db.ged_arquivos_tipos
                .Where(g => g.ativo == true && g.id_tipo_pai == 38)
                .OrderBy(p => p.descricao)
                .ToList();
            foreach (var tipo in listaGedTipos)
            {
                comboGedTipos.Add(new SelectListItem { Value = tipo.id_arquivo_tipo.ToString(), Text = tipo.descricao });
                if (IdArquivoTipo == 0) { IdArquivoTipo = tipo.id_arquivo_tipo; };
            }
            ViewBag.ComboGedTipos = comboGedTipos;

            recordUpload.id_arquivo_tipo = IdArquivoTipo;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Upload de Documentos - Lote de Estoque</b>";
            return View(recordUpload);
        }
        #endregion
    }
}

