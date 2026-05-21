// Migrado em 2020_07_15

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Areas.g.Controllers
{

    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosNcm_*,g_ProdutosNcm_Default")]
    public partial class ProdutosNcmController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_ProdutosNcm";

        public ProdutosNcmController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosNcm_*,g_ProdutosNcm_Default")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de NCM";
            var model = new CstProdutosNcmIndex
            {
                ProdutosNcmIndex_id = String.Empty,
                ProdutosNcmIndex_codigo_ncm = String.Empty
            };
            ViewBag.RestoreFilterAutoSearch = false;
            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, codigoRestore;
            if (TryParseFiltroProdutosNcmSemicolon(filtroPersistido.sql_filtro, out idRestore, out codigoRestore))
            {
                model.ProdutosNcmIndex_id = idRestore;
                model.ProdutosNcmIndex_codigo_ncm = codigoRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore) || !String.IsNullOrEmpty(codigoRestore);
            }
            return View(model);
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosNcm_*,g_ProdutosNcm_Actionread")]
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
                    recordFiltro = LibDB.getFilterByUser(param, controllerName, db);
                }
                else
                {
                    recordFiltro = ObterFiltroPersistidoUsuario();
                }

                var baseQuery = db.g_produtos_ncm.AsNoTracking().Where(p => p.id_produto_ncm > 0);
                int totalRecords = baseQuery.Count();

                string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                string codigoStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
                bool hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(codigoStr);

                if (!hasInline && !listarTodosExplicito)
                {
                    TryParseFiltroProdutosNcmSemicolon(recordFiltro.sql_filtro, out idStr, out codigoStr);
                    hasInline = !String.IsNullOrEmpty(idStr) || !String.IsNullOrEmpty(codigoStr);
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

                IQueryable<Db.g_produtos_ncm> query = baseQuery;
                if (hasInline && !listarTodosExplicito)
                {
                    filterApplied = true;
                    query = AplicarFiltroProdutosNcmNaQuery(query, idStr, codigoStr);
                    LibDB.setFilterByUser(MontarFiltroProdutosNcmPersistido(idStr, codigoStr), controllerName, true, db);
                }

                int totalDisplayRecords = query.Count();
                int start = Math.Max(0, param.iDisplayStart);
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                if (length > 100) { length = 100; }

                query = AplicarOrdenacaoProdutosNcmNaQuery(query, param);
                var page = query
                    .Skip(start)
                    .Take(length)
                    .Select(p => new { p.id_produto_ncm, p.ativo, p.codigo_ncm })
                    .ToList();

                var list = page.Select(p =>
                {
                    string _ativo = p.ativo == true
                        ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "")
                        : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                    return new[]
                    {
                        "",
                        p.id_produto_ncm.ToString(),
                        _ativo,
                        p.codigo_ncm ?? ""
                    };
                }).ToList();

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

        private static bool TryParseFiltroProdutosNcmSemicolon(string raw, out string id, out string codigoNcm)
        {
            id = codigoNcm = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 2) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            codigoNcm = campos[1].EmptyIfNull().ToString().Trim();
            return !String.IsNullOrEmpty(id) || !String.IsNullOrEmpty(codigoNcm);
        }

        private static string MontarFiltroProdutosNcmPersistido(string id, string codigoNcm)
        {
            return (id ?? String.Empty) + ";" + (codigoNcm ?? String.Empty);
        }

        private static IQueryable<Db.g_produtos_ncm> AplicarFiltroProdutosNcmNaQuery(IQueryable<Db.g_produtos_ncm> query, string idStr, string codigoStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0" && int.TryParse(idStr, out int idNcm))
            {
                query = query.Where(p => p.id_produto_ncm == idNcm);
            }
            if (LibStringFormat.TryMontarPadraoLikeContemCodigo(codigoStr, out string padraoCodigo))
            {
                query = query.Where(p => p.codigo_ncm != null && DbFunctions.Like(p.codigo_ncm, padraoCodigo));
            }
            return query;
        }

        private static IQueryable<Db.g_produtos_ncm> AplicarOrdenacaoProdutosNcmNaQuery(IQueryable<Db.g_produtos_ncm> query, jQueryDataTableParamModel param)
        {
            bool asc = param.sSortDir_0.EmptyIfNull().ToString().Trim().ToLowerInvariant() != "desc";
            if (param.iSortingCols > 0)
            {
                if (param.iSortCol_0 == 3)
                {
                    return asc ? query.OrderBy(p => p.codigo_ncm) : query.OrderByDescending(p => p.codigo_ncm);
                }
                if (param.iSortCol_0 == 1)
                {
                    return asc ? query.OrderBy(p => p.id_produto_ncm) : query.OrderByDescending(p => p.id_produto_ncm);
                }
            }
            return query.OrderBy(p => p.id_produto_ncm);
        }
        #endregion

        #region Create
        [CustomAuthorize(Roles = "SuperAdmin,g_ProdutosNcm,g_ProdutosNcm_*,g_ProdutosNcm_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>NCM</b";
            PreencherLookupsCreateEdit();
            g_produtos_ncm newRecord = new g_produtos_ncm();
            newRecord.ativo = true;
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,g_ProdutosNcm,g_ProdutosNcm_*,g_ProdutosNcm_Actioncreate")]
        public ActionResult Create(g_produtos_ncm record_g_produtos_ncm)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>NCM</b";
            record_g_produtos_ncm.id_coligada = 1;
            record_g_produtos_ncm.id_filial = 1;

            if (ModelState.IsValid)
            {
                IQueryable<g_produtos_ncm> listaProdutosNCM = db.g_produtos_ncm.Where(p => p.codigo_ncm == record_g_produtos_ncm.codigo_ncm);
                foreach (g_produtos_ncm validacao in listaProdutosNCM)
                {
                    // Validação Nome
                    if (validacao.codigo_ncm.ToString().ToUpper().Equals(record_g_produtos_ncm.codigo_ncm.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Código NCM] duplicado na base de dados [" + validacao.codigo_ncm.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_produtos_ncm.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_produtos_ncm.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_produtos_ncm.Add(record_g_produtos_ncm);

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

            PreencherLookupsCreateEdit();
            return View("CreateEdit", record_g_produtos_ncm);
        }
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosNcm_*,g_ProdutosNcm_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_produtos_ncm record_g_produtos_ncm = db.g_produtos_ncm.Find(id);
            if (record_g_produtos_ncm == null)
            {
                return RedirectToAction("Index");
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>NCM</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos_ncm.id_produto_ncm.EmptyIfNull().ToString() + " - " + record_g_produtos_ncm.codigo_ncm.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_produtos_ncm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Produtos_*,g_Produtos_Actionupdate")]
        public ActionResult Edit(g_produtos_ncm record_g_produtos_ncm)
        {
            if (ModelState.IsValid)
            {
                IQueryable<g_produtos_ncm> listaProdutosNCM = db.g_produtos_ncm.Where(p => p.id_produto_ncm != record_g_produtos_ncm.id_produto_ncm);
                foreach (g_produtos_ncm validacao in listaProdutosNCM)
                {

                    // Validação Nome
                    if (validacao.codigo_ncm.ToString().ToUpper().Equals(record_g_produtos_ncm.codigo_ncm.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Código NCM] duplicado na base de dados [" + validacao.codigo_ncm.ToString() + "]"); }

                }
            }

            if (ModelState.IsValid)
            {
                record_g_produtos_ncm.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_produtos_ncm.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_produtos_ncm).State = EntityState.Modified;
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
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>NCM</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos_ncm.id_produto_ncm.EmptyIfNull().ToString() + " - " + record_g_produtos_ncm.codigo_ncm.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_produtos_ncm);
        }
        #endregion

        public ActionResult ModalAtualizarTabelaIBPT()
        {
            CstUpload _cstUpload = new CstUpload();
            ViewBag.Title = "Atualizar Tabela IBPTax";
            return View(_cstUpload);
        }

        [HttpPost]
        public ActionResult AjaxAtualizarTabelaIBPT(CstUpload record_cstUpload)
        {
            bool processado = false;
            bool erroProcessamento = false;
            decimal qtdItensProcessados = 0;
            decimal qtdNcmAtualizados = 0;
            decimal qtdIbptaxAtualizados = 0;
            decimal qtdIbptaxCadastrados = 0;
            String msgRetorno = String.Empty;
            String resultadoProcessamento = String.Empty;
            String fileNameDestino = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            var fileExt = System.IO.Path.GetExtension(record_cstUpload.filesource.FileName).Substring(1).ToLower();
            if (fileExt != "csv")
            {
                msgRetorno += "O Layout do arquivo informado não foi identificado pelo ERP" + "<br/>";
                erroProcessamento = true;
            }
            if ((record_cstUpload.filesource.ContentLength > 0) && (erroProcessamento == false))
            {
                try
                {
                    // Upload do Arquivo
                    processado = false;

                    var fileNameOrigem = Path.GetFileName(record_cstUpload.filesource.FileName);

                    String DirTempFiles = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "uploads");
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    DirTempFiles = Path.Combine(DirTempFiles, "user_" + CachePersister.userIdentity.IdUsuario.EmptyIfNull().ToString());
                    if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
                    LibFilesDisk.DeleteFilesInDirectory(DirTempFiles); // Apagar todos os arquivos que estiveremno diretório do usuario
                    fileNameDestino = Path.Combine(DirTempFiles, LibDateTime.getDataHoraBrasilia().ToString("yyyyMMddhhmmss") + "_" + fileNameOrigem);
                    record_cstUpload.filesource.SaveAs(fileNameDestino);

                    // Processamento do Arquivo
                    List<g_produtos_ncm> ListProdutosNcm = db.g_produtos_ncm.ToList();
                    List<g_produtos_ncm_ibptax> ListProdutosNcmIbptax = db.g_produtos_ncm_ibptax.ToList();

                    if (fileExt == "csv")
                    {
                        String line;
                        erroProcessamento = false;
                        String[] listaCampos = null;
                        System.IO.StreamReader file = new System.IO.StreamReader(fileNameDestino);

                        ListProdutosNcmIbptax.ForEach(p => p.ativo = false);
                        db.SaveChanges();

                        while ((line = file.ReadLine()) != null)
                        {
                            try { listaCampos = line.EmptyIfNull().ToString().Split(';'); } catch (Exception) { listaCampos = new string[1] { "" }; };
                            if (listaCampos.Count() > 2)
                            {
                                decimal trib_fed_nac = 0;
                                decimal trib_fed_imp = 0;
                                decimal trib_est = 0;
                                decimal trib_mun = 0;
                                try { trib_fed_nac = Decimal.Parse(listaCampos[4], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture); } catch (Exception) { };
                                try { trib_fed_imp = Decimal.Parse(listaCampos[5], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture); } catch (Exception) { };
                                try { trib_est = Decimal.Parse(listaCampos[6], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture); } catch (Exception) { };
                                try { trib_mun = Decimal.Parse(listaCampos[7], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture); } catch (Exception) { };

                                g_produtos_ncm record_g_produtos_ncm = ListProdutosNcm.Find(p => p.codigo_ncm == listaCampos[0]);
                                if (record_g_produtos_ncm != null)
                                {
                                    
                                    record_g_produtos_ncm.tributo_federal_nacional = trib_fed_nac;
                                    record_g_produtos_ncm.tributo_federal_importado = trib_fed_imp;
                                    record_g_produtos_ncm.tributo_estadual = trib_est;
                                    record_g_produtos_ncm.tributo_municipal = trib_mun;
                                    db.Entry(record_g_produtos_ncm).State = EntityState.Modified;
                                    qtdNcmAtualizados += 1;
                                }

                                g_produtos_ncm_ibptax record_g_produtos_ncm_ibptax = ListProdutosNcmIbptax.Find(p => p.codigo_ncm == listaCampos[0]);
                                if (record_g_produtos_ncm_ibptax != null)
                                {
                                    record_g_produtos_ncm_ibptax.ativo = true;
                                    record_g_produtos_ncm_ibptax.tributo_federal_nacional = trib_fed_nac;
                                    record_g_produtos_ncm_ibptax.tributo_federal_importado = trib_fed_imp;
                                    record_g_produtos_ncm_ibptax.tributo_estadual = trib_est;
                                    record_g_produtos_ncm_ibptax.tributo_municipal = trib_mun;
                                    record_g_produtos_ncm.datahora_alteracao = DataHoraAtual;
                                    record_g_produtos_ncm.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                                    db.Entry(record_g_produtos_ncm_ibptax).State = EntityState.Modified;
                                    qtdIbptaxAtualizados += 1;
                                }
                                else
                                {
                                    g_produtos_ncm_ibptax new_record_g_produtos_ncm_ibptax = new g_produtos_ncm_ibptax();
                                    new_record_g_produtos_ncm_ibptax.codigo_ncm = listaCampos[0];
                                    new_record_g_produtos_ncm_ibptax.ativo = true;
                                    new_record_g_produtos_ncm_ibptax.tributo_federal_nacional = trib_fed_nac;
                                    new_record_g_produtos_ncm_ibptax.tributo_federal_importado = trib_fed_imp;
                                    new_record_g_produtos_ncm_ibptax.tributo_estadual = trib_est;
                                    new_record_g_produtos_ncm_ibptax.tributo_municipal = trib_mun;
                                    new_record_g_produtos_ncm_ibptax.datahora_cadastro = DataHoraAtual;
                                    new_record_g_produtos_ncm_ibptax.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                                    db.g_produtos_ncm_ibptax.Add(new_record_g_produtos_ncm_ibptax);
                                    qtdIbptaxCadastrados += 1;
                                }
                            }
                            qtdItensProcessados += 1;
                        }
                        db.SaveChanges();
                    }

                    if (erroProcessamento == false)
                    {
                        processado = true;
                        msgRetorno += "Tabela IBPT Processada com sucesso!" + "<br/>";
                        msgRetorno += qtdItensProcessados.ToString() + LibStringFormat.GetTabHtml(1) + "Total de registros processados" + "<br/><br/>";
                        msgRetorno += qtdNcmAtualizados.ToString() + LibStringFormat.GetTabHtml(1) + "NCM(s) Atualizados" + "<br/>";
                        msgRetorno += qtdIbptaxAtualizados.ToString() + LibStringFormat.GetTabHtml(1) + "IBPTax Atualizados" + "<br/>";
                        msgRetorno += qtdIbptaxCadastrados.ToString() + LibStringFormat.GetTabHtml(1) + "IBPTax Cadastrados" + "<br/>";
                    }
                }
                catch (DbEntityValidationException ex)
                {
                    processado = false;
                    msgRetorno = LibExceptions.getDbEntityValidationException(ex);
                }
                catch (Exception e)
                {
                    processado = false;
                    msgRetorno = LibExceptions.getExceptionShortMessage(e);
                }
            }
            return Json(new { success = processado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null) { db.Dispose(); };
            }
            base.Dispose(disposing);
        }

    }


}