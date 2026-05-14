// Migrado em 2020_07_15

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
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Default")]
    public class VendedoresController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Vendedores";

        public VendedoresController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Vendedores";
            return View();
        }

        #region PreencherLookupsCreateEdit
        public void PreencherLookupsCreateEdit()
        {

            var comboRevenda = new List<SelectListItem>();
            try
            {
                IQueryable<g_revendas> listaDbRevenda = null;
                if (CachePersister.userIdentity.IdPerfil == 1)
                {
                    listaDbRevenda = db.g_revendas.Select(p => p).OrderBy(p => p.nome);
                }
                else
                {
                    listaDbRevenda = db.g_revendas.Where(p => p.id_revenda > 0).OrderBy(p => p.nome);
                };
                comboRevenda.Add(new SelectListItem { Value = "0", Text = " " });
                foreach (g_revendas item1 in listaDbRevenda)
                {
                    comboRevenda.Add(new SelectListItem { Value = item1.id_revenda.ToString(), Text = item1.nome.ToString() });
                }
            }
            finally { }
            ViewBag.comboRevenda = comboRevenda;
        }
        #endregion

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actionread")]
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
            var allRecords = new List<Db.g_vendedores>();
            List<string[]> list = new List<string[]>();

            // Verificação se há algum filtro ativo
            if (record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0) { filterDb = true; }
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            if ((filterDb == false) && (filterAdvanced == false))
            {
                // Não há filtro
                if (CachePersister.userIdentity.IdPerfil == 1) // Perfil Adm visualiza todos os registros independente de Coligada e Filial
                {
                    allRecords = db.g_vendedores.Select(p => p).ToList();
                }
                else // Demais Perfis visualizam os registros de sua coligada e sua filial
                {
                    allRecords = db.g_vendedores.Where(p => p.id_vendedor > 0).ToList();
                }
            }
            if (filterDb)
            {
                SentencaSQL = string.Empty;
                if (record_g_filtro.advanced == true) { SentencaSQL = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); }
                else { SentencaSQL = "select * from g_vendedores where " + record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim(); };
                allRecords = db.g_vendedores.SqlQuery(SentencaSQL).ToList();
            }
            else if (filterAdvanced)
            {
                // Filtro Avançado - Não implementado
                allRecords = db.g_vendedores.ToList();
            }


            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_vendedores, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_vendedor) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.nome :
                                     param.iSortCol_0 == 3 && param.iSortingCols > 0 ? c.email :
                                     param.iSortCol_0 == 4 && param.iSortingCols > 0 ? Convert.ToString(c.ativo) :
                                     "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_vendedor); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.nome); }
                    else if (param.iSortCol_0 == 3) { displayedRecords = displayedRecords.OrderBy(c => c.email); }
                    else if (param.iSortCol_0 == 4) { displayedRecords = displayedRecords.OrderBy(c => c.ativo); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_vendedor); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.nome); }
                    else if (param.iSortCol_0 == 3) { displayedRecords = displayedRecords.OrderByDescending(c => c.email); }
                    else if (param.iSortCol_0 == 4) { displayedRecords = displayedRecords.OrderByDescending(c => c.ativo); }
                }
            }


            var allRevendas = db.g_revendas.ToList();
            g_revendas record_g_revendas = new g_revendas();

            foreach (var c in displayedRecords)
            {
                String _ativo = c.ativo == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                String NomeRevenda = String.Empty;
                if ((c.id_revenda.EmptyIfNull().ToString().Equals(String.Empty)) || (c.id_revenda.EmptyIfNull().ToString() == "0"))
                {
                    NomeRevenda = "";
                }
                else
                {

                    record_g_revendas = allRevendas.Find(e => e.id_revenda == c.id_revenda);
                    if (record_g_revendas != null) { NomeRevenda = record_g_revendas.nome.EmptyIfNull().ToString(); }
                }

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_vendedor.ToString(),
                                    _ativo,
                                    c.nome.EmptyIfNull().ToString(),
                                    NomeRevenda,
                                    c.email.EmptyIfNull().ToString()
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
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actioncreate")]
        public ActionResult Create()
        {
            PreencherLookupsCreateEdit();
            cstViewVendedoresTabelasModel record_cstViewVendedoresTabelasDetalhesModel = new cstViewVendedoresTabelasModel();
            record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.ativo = true;
            record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_revenda = 0;
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Vendedor</b";
            return View("CreateEdit", record_cstViewVendedoresTabelasDetalhesModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actioncreate")]
        public ActionResult Create(cstViewVendedoresTabelasModel record_cstViewVendedoresTabelasDetalhesModel)
        {
            PreencherLookupsCreateEdit();
            record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_coligada = 1;
            record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_filial = 1;
            //record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome = record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.ToUpper();
            if (ModelState.IsValid)
            {
                IQueryable<g_vendedores> listaVendedores = db.g_vendedores.Where(p => p.nome == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome && p.id_coligada == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_coligada && p.id_filial == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_filial);
                foreach (g_vendedores validacao in listaVendedores)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }
            if (ModelState.IsValid)
            {
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_vendedores.Add(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores);
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Vendedor</b";
            return View("CreateEdit", record_cstViewVendedoresTabelasDetalhesModel);
        }
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            cstViewVendedoresTabelasModel record_cstViewVendedoresTabelasDetalhesModel = new cstViewVendedoresTabelasModel();

            g_vendedores record_g_vendedores = new g_vendedores();

            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            else
            {
                record_g_vendedores = db.g_vendedores.Find(id);

                if (record_g_vendedores == null)
                {
                    return RedirectToAction("Index");
                }
                else if (CachePersister.userIdentity.IdPerfil > 1)
                {
                    record_cstViewVendedoresTabelasDetalhesModel.g_vendedores = record_g_vendedores;

                    String sqlTemp = " select gdc_consultas.id_consulta, gdc_consultas.nome, " +
                                        " gdc_consultas_tabelas_vendedores.id_consulta_tabela_vendedor, " +
                                        " gdc_consultas_tabelas_vendedores.valor_unit, g_produtos.valor_base " +
                                        " from gdc_consultas " +
                                        " left join gdc_consultas_tabelas_vendedores on " +
                                        " (gdc_consultas_tabelas_vendedores.id_consulta = gdc_consultas.id_consulta " +
                                        " and gdc_consultas_tabelas_vendedores.id_vendedor = " + id.ToString() + ") " +
                                        " left join g_produtos on (g_produtos.id_produto = gdc_consultas.id_produto) " +
                                        " where gdc_consultas.ativo = 1 and gdc_consultas.extrato_tabelas = 1 " +
                                        " order by gdc_consultas.id_consulta ";


                    var allRecords = db.Database.SqlQuery<cstVendedoresTabelasDetalhesModel>(sqlTemp).ToList();
                    for (int i = 0; i < allRecords.Count; i++)
                    {
                        if (allRecords[i].id_consulta_tabela_vendedor == null)
                        {
                            allRecords[i].id_consulta_tabela_vendedor = 0;
                        }
                        if (allRecords[i].valor_unit == null)
                        {
                            if (allRecords[i].valor_base == null)
                            { allRecords[i].valor_unit = 0; }
                            else { allRecords[i].valor_unit = allRecords[i].valor_base; }
                        }
                    }
                    record_cstViewVendedoresTabelasDetalhesModel.allcstVendedoresTabelasDetalhesModel = allRecords;
                }
                else
                {

                    record_cstViewVendedoresTabelasDetalhesModel.g_vendedores = record_g_vendedores;

                    String sqlTemp = " select gdc_consultas.id_consulta, gdc_consultas.nome, " +
                                            " gdc_consultas_tabelas_vendedores.id_consulta_tabela_vendedor, " +
                                            " gdc_consultas_tabelas_vendedores.valor_unit, g_produtos.valor_base " +
                                            " from gdc_consultas " +
                                            " left join gdc_consultas_tabelas_vendedores on " +
                                            " (gdc_consultas_tabelas_vendedores.id_consulta = gdc_consultas.id_consulta " +
                                            " and gdc_consultas_tabelas_vendedores.id_vendedor = " + id.ToString() + ") " +
                                            " left join g_produtos on (g_produtos.id_produto = gdc_consultas.id_produto) " +
                                            " where gdc_consultas.ativo = 1 and gdc_consultas.extrato_tabelas = 1 " +
                                            " order by gdc_consultas.id_consulta ";

                    var allRecords = db.Database.SqlQuery<cstVendedoresTabelasDetalhesModel>(sqlTemp).ToList();
                    for (int i = 0; i < allRecords.Count; i++)
                    {
                        if (allRecords[i].id_consulta_tabela_vendedor == null)
                        {
                            allRecords[i].id_consulta_tabela_vendedor = 0;
                        }
                        if (allRecords[i].valor_unit == null)
                        {
                            if (allRecords[i].valor_base == null)
                            { allRecords[i].valor_unit = 0; }
                            else { allRecords[i].valor_unit = allRecords[i].valor_base; }
                        }
                    }
                    record_cstViewVendedoresTabelasDetalhesModel.allcstVendedoresTabelasDetalhesModel = allRecords;
                }
            }

            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Vendedor</b>" + LibStringFormat.GetTabHtml(1) + record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_vendedor.EmptyIfNull().ToString() + " - " + record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_cstViewVendedoresTabelasDetalhesModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Vendedores_*,g_Vendedores_Actionupdate")]
        public ActionResult Edit(cstViewVendedoresTabelasModel record_cstViewVendedoresTabelasDetalhesModel)
        {
            PreencherLookupsCreateEdit();
            if (ModelState.IsValid)
            {
                IQueryable<g_vendedores> listaVendedores = db.g_vendedores.Where(p => (p.nome == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome && p.id_coligada == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_coligada && p.id_filial == record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_filial) && (p.id_vendedor != record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_vendedor));
                foreach (g_vendedores validacao in listaVendedores)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }
            if (ModelState.IsValid)
            {

                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.datahora_alteracao = DataHoraAtual;
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome = record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.ToUpper();
                db.Entry(record_cstViewVendedoresTabelasDetalhesModel.g_vendedores).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Vendedor</b>" + LibStringFormat.GetTabHtml(1) + record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.id_vendedor.EmptyIfNull().ToString() + " - " + record_cstViewVendedoresTabelasDetalhesModel.g_vendedores.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_cstViewVendedoresTabelasDetalhesModel);
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
            if (disposing)
            {
                if (db != null) { db.Dispose(); };
            }
            base.Dispose(disposing);
        }
    }
}