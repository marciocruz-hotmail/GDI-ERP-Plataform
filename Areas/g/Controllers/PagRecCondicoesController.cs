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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Default")]
    public class PagRecCondicoesController : Controller
    {
        private GdiPlataformEntities db;

        public PagRecCondicoesController()
        {
            String Inicio = String.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes,g_PagRecCondicoes_*,g_PagRecCondicoes_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Pag/Rec Condições";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes,g_PagRecCondicoes_*,g_PagRecCondicoes_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            const string filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.g_pagrec_condicoes>(); // Lista vazia - Inicialização

            // Perfil Adm visualiza todos os registros independente de Coligada e Filial
            allRecords = db.g_pagrec_condicoes.Where(p => p.id_pagrec_condicao > 0).OrderBy(p => p.descricao).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_pagrec_condicoes, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_pagrec_condicao) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? Convert.ToString(c.ativo) :
                                     param.iSortCol_0 == 3 && param.iSortingCols > 0 ? c.descricao :
                                     param.iSortCol_0 == 4 && param.iSortingCols > 0 ? Convert.ToString(c.pagamento) :
                                     param.iSortCol_0 == 5 && param.iSortingCols > 0 ? Convert.ToString(c.recebimento) :
                                     param.iSortCol_0 == 6 && param.iSortingCols > 0 ? Convert.ToString(c.qtd_dias) :
                                     param.iSortCol_0 == 7 && param.iSortingCols > 0 ? Convert.ToString(c.qtd_parcelas) :
                                     "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_pagrec_condicao); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.descricao); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_pagrec_condicao); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.descricao); }
                }
            }

            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {

                String _ativo = c.ativo == true ? LibIcons.getIcon("fa-solid fa-circle-check", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle-xmark", "Inativo", "red", "");
                String _pagamento = c.pagamento == true ? LibIcons.getIcon("fa-regular fa-thumbs-up", "Habilitado para Pagamentos", "#008000", "fa-lg") : LibIcons.getIcon("fa-regular fa-thumbs-down", "Desabilitado para Pagamentos", "cc0000", "");
                String _recebimento = c.recebimento == true ? LibIcons.getIcon("fa-regular fa-thumbs-up", "Habilitado para Recebimentos", "#008000", "fa-lg") : LibIcons.getIcon("fa-regular fa-thumbs-down", "Desabilitado para Recebimentos", "cc0000", "");

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_pagrec_condicao.ToString(),
                                    _ativo,
                                    c.descricao.ToString(),
                                    _pagamento,
                                    _recebimento,
                                    c.qtd_dias.ToString(),
                                    c.qtd_parcelas.ToString()
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

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Condição</b";
            g_pagrec_condicoes newRecord = new g_pagrec_condicoes();
            newRecord.ativo = true;
            newRecord.qtd_dias = 0;
            newRecord.qtd_parcelas = 0;
            return View("CreateEdit", newRecord);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Actioncreate")]
        public ActionResult Create(g_pagrec_condicoes record_g_pagrec_condicoes)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Condição</b";
            record_g_pagrec_condicoes.id_coligada = 1;
            record_g_pagrec_condicoes.id_filial = 1;
            record_g_pagrec_condicoes.descricao = record_g_pagrec_condicoes.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_pagrec_condicoes> listaPagRecCondicao = db.g_pagrec_condicoes.Where(p => p.descricao == record_g_pagrec_condicoes.descricao);
                foreach (g_pagrec_condicoes validacao in listaPagRecCondicao)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_g_pagrec_condicoes.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_pagrec_condicoes.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_pagrec_condicoes.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_pagrec_condicoes.Add(record_g_pagrec_condicoes);
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
            return View("CreateEdit", record_g_pagrec_condicoes);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_pagrec_condicoes record_g_pagrec_condicoes = db.g_pagrec_condicoes.Find(id);
            if (record_g_pagrec_condicoes == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Condição</b>" + LibStringFormat.GetTabHtml(1) + record_g_pagrec_condicoes.id_pagrec_condicao.EmptyIfNull().ToString() + " - " + record_g_pagrec_condicoes.descricao.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_pagrec_condicoes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_PagRecCondicoes_*,g_PagRecCondicoes_Actionupdate")]
        public ActionResult Edit(g_pagrec_condicoes record_g_pagrec_condicoes)
        {
            record_g_pagrec_condicoes.descricao = record_g_pagrec_condicoes.descricao.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                IQueryable<g_pagrec_condicoes> listaPagRecCondicao = db.g_pagrec_condicoes.Where(p => (p.descricao == record_g_pagrec_condicoes.descricao) && (p.id_pagrec_condicao != record_g_pagrec_condicoes.id_pagrec_condicao));
                foreach (g_pagrec_condicoes validacao in listaPagRecCondicao)
                {
                    // Validação Descrição
                    if (validacao.descricao.ToString().ToUpper().Equals(record_g_pagrec_condicoes.descricao.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Descrição] duplicado na base de dados [" + validacao.descricao.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_pagrec_condicoes.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_pagrec_condicoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_pagrec_condicoes).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Pag/Rec Condição</b>" + LibStringFormat.GetTabHtml(1) + record_g_pagrec_condicoes.id_pagrec_condicao.EmptyIfNull().ToString() + " - " + record_g_pagrec_condicoes.descricao.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_pagrec_condicoes);
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