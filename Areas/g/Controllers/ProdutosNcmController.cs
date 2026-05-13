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
    public class ProdutosNcmController : Controller
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
            return View();
        }

        #region PreencherLookupsCreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosNcm_*,g_ProdutosNcm_Actionread,g_ProdutosNcm_Actionupdate")]
        public void PreencherLookupsCreateEdit()
        {
            var comboCstIcmsEntrada = new List<SelectListItem>();
            IQueryable<gc_icms_cst> listaDbCstIcmsEntrada = null;
            if (CachePersister.userIdentity.IdPerfil == 1) { listaDbCstIcmsEntrada = db.gc_icms_cst.Where(p => p.id_icms_cst > 0).OrderBy(p => p.id_icms_cst); }
            else { listaDbCstIcmsEntrada = db.gc_icms_cst.Where(p => p.id_icms_cst > 0).OrderBy(p => p.id_icms_cst); };
            foreach (gc_icms_cst item1 in listaDbCstIcmsEntrada)
            {
                comboCstIcmsEntrada.Add(new SelectListItem { Value = item1.id_icms_cst.ToString(), Text = item1.codigo_cst + " - " + item1.descricao.ToString() });
            }
            ViewBag.comboCstIcmsEntrada = comboCstIcmsEntrada;


            var comboCstIpiEntrada = new List<SelectListItem>();
            IQueryable<gc_tributos_cst> listaDbCstIpiEntrada = null;
            listaDbCstIpiEntrada = db.gc_tributos_cst.Where(p => p.ativo == true && p.ipi_entrada == true).OrderBy(p => p.id_tributo_cst);
            foreach (gc_tributos_cst item2 in listaDbCstIpiEntrada)
            {
                comboCstIpiEntrada.Add(new SelectListItem { Value = item2.id_tributo_cst.ToString(), Text = item2.codigo + " - " + item2.descricao.ToString() });
            }
            ViewBag.comboCstIpiEntrada = comboCstIpiEntrada;


            var comboCstIpiSaida = new List<SelectListItem>();
            IQueryable<gc_tributos_cst> listaDbCstIpiSaida = null;
            listaDbCstIpiSaida = db.gc_tributos_cst.Where(p => p.ativo == true && p.ipi_saida == true).OrderBy(p => p.id_tributo_cst);
            foreach (gc_tributos_cst item3 in listaDbCstIpiSaida)
            {
                comboCstIpiSaida.Add(new SelectListItem { Value = item3.id_tributo_cst.ToString(), Text = item3.codigo + " - " + item3.descricao.ToString() });
            }
            ViewBag.comboCstIpiSaida = comboCstIpiSaida;


            var comboCstPisEntrada = new List<SelectListItem>();
            IQueryable<gc_tributos_cst> listaDbCstPisEntrada = null;
            listaDbCstPisEntrada = db.gc_tributos_cst.Where(p => p.ativo == true && p.pis_entrada == true).OrderBy(p => p.id_tributo_cst);
            foreach (gc_tributos_cst item4 in listaDbCstPisEntrada)
            {
                comboCstPisEntrada.Add(new SelectListItem { Value = item4.id_tributo_cst.ToString(), Text = item4.codigo + " - " + item4.descricao.ToString() });
            }
            ViewBag.comboCstPisEntrada = comboCstPisEntrada;


            var comboCstPisSaida = new List<SelectListItem>();
            IQueryable<gc_tributos_cst> listaDbCstPisSaida = null;
            listaDbCstPisSaida = db.gc_tributos_cst.Where(p => p.ativo == true && p.pis_saida == true).OrderBy(p => p.id_tributo_cst);
            foreach (gc_tributos_cst item5 in listaDbCstPisSaida)
            {
                comboCstPisSaida.Add(new SelectListItem { Value = item5.id_tributo_cst.ToString(), Text = item5.codigo + " - " + item5.descricao.ToString() });
            }
            ViewBag.comboCstPisSaida = comboCstPisSaida;


            var comboCstCofinsEntrada = new List<SelectListItem>();
            IQueryable<gc_tributos_cst> listaDbCstCofinsEntrada = null;
            listaDbCstCofinsEntrada = db.gc_tributos_cst.Where(p => p.ativo == true && p.cofins_entrada == true).OrderBy(p => p.id_tributo_cst);
            foreach (gc_tributos_cst item6 in listaDbCstCofinsEntrada)
            {
                comboCstCofinsEntrada.Add(new SelectListItem { Value = item6.id_tributo_cst.ToString(), Text = item6.codigo + " - " + item6.descricao.ToString() });
            }
            ViewBag.comboCstCofinsEntrada = comboCstCofinsEntrada;


            var comboCstCofinsSaida = new List<SelectListItem>();
            IQueryable<gc_tributos_cst> listaDbCstCofinsSaida = null;
            listaDbCstCofinsSaida = db.gc_tributos_cst.Where(p => p.ativo == true && p.cofins_saida == true).OrderBy(p => p.id_tributo_cst);
            foreach (gc_tributos_cst item7 in listaDbCstCofinsSaida)
            {
                comboCstCofinsSaida.Add(new SelectListItem { Value = item7.id_tributo_cst.ToString(), Text = item7.codigo + " - " + item7.descricao.ToString() });
            }
            ViewBag.comboCstCofinsSaida = comboCstCofinsSaida;

        }
        #endregion

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ProdutosNcm_*,g_ProdutosNcm_Actionread")]
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
            var allRecords = new List<Db.g_produtos_ncm>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                allRecords = db.g_produtos_ncm.Select(p => p).ToList();
            }
            if (filterDb)
            {
                SentencaSQL = string.Empty;
                if (record_g_filtro.advanced == true) { SentencaSQL = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); }
                else { SentencaSQL = "select * from g_produtos_ncm where " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); };
                allRecords = db.g_produtos_ncm.SqlQuery(SentencaSQL).ToList();
            }
            else if (filterAdvanced)
            {
                allRecords = db.g_produtos_ncm.Select(p => p).ToList();
            }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_produtos_ncm, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_produto_ncm) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? Convert.ToString(c.codigo_ncm) :
                                     "");

            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_produto_ncm); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.codigo_ncm); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_produto_ncm); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.codigo_ncm); }
                }
            }

            foreach (var c in displayedRecords)
            {
                String _ativo = c.ativo == true ? LibIcons.getIcon("fa-solid fa-circle", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle", "Inativo", "red", "");
                list.Add(new[] {
                                        "", // Coluna de Seleção
                                        c.id_produto_ncm.ToString(),
                                        _ativo,
                                        c.codigo_ncm.ToString()
                                    });
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
            cstUpload _cstUpload = new cstUpload();
            ViewBag.Title = "Atualizar Tabela IBPTax";
            return View(_cstUpload);
        }

        [HttpPost]
        public ActionResult AjaxAtualizarTabelaIBPT(cstUpload record_cstUpload)
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