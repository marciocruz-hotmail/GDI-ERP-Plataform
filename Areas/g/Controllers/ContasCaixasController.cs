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
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Default")]
    public class ContasCaixasController : Controller
    {
        private GdiPlataformEntities db;

        public ContasCaixasController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_Actioncreate,g_ContasCaixas_Actionupdate")]
        public void PreencherLookupsCreateEdit()
        {
            var comboCidade = new List<SelectListItem>();
            IQueryable<g_cidades> listaDbCidade = db.g_cidades.Where(p => p.ativo == true).OrderBy(p => p.nome);
            foreach (g_cidades item_g_cidades in listaDbCidade)
            {
                comboCidade.Add(new SelectListItem { Value = item_g_cidades.id_cidade.ToString(), Text = item_g_cidades.nome.ToString() });
            }
            ViewBag.comboCidade = comboCidade;

            var comboUF = new List<SelectListItem>();
            IQueryable<g_uf> listaDbUF = db.g_uf.Select(p => p).OrderBy(p => p.sigla);
            foreach (g_uf item2 in listaDbUF)
            {
                comboUF.Add(new SelectListItem { Value = item2.id_uf.ToString(), Text = item2.sigla.ToString() });
            }
            ViewBag.comboUF = comboUF;
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Contas Caixas";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            if (param == null) param = new jQueryDataTableParamModel();
            const string filterOnOff = "0";
            try
            {
            var allRecords = new List<Db.g_contas_caixas>(); // Lista vazia - Inicialização

            // Perfil Adm visualiza todos os registros independente de Coligada e Filial
            if (CachePersister.userIdentity.IdPerfil == 1)
            { allRecords = db.g_contas_caixas.Where(p => p.id_conta_caixa > 0).OrderBy(p => p.nome).ToList(); }
            else
            { allRecords = db.g_contas_caixas.Where(p => p.id_conta_caixa > 0).OrderBy(p => p.nome).ToList(); }

            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_contas_caixas, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_conta_caixa) :
                                     param.iSortCol_0 == 2 && param.iSortingCols > 0 ? c.nome :
                                     "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_conta_caixa); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderBy(c => c.nome); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_conta_caixa); }
                    else if (param.iSortCol_0 == 2) { displayedRecords = displayedRecords.OrderByDescending(c => c.nome); }
                }
            }

            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {
                String _boleto = c.boleto_emissao == true ? LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg") : LibIcons.getIcon("fa-regular fa-thumbs-down", "", "cc0000", "");

                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_conta_caixa.ToString(),
                                    c.nome.ToString(),
                                    c.banco.ToString(),
                                    c.agencia.ToString()+"-"+c.dv_agencia.ToString(),
                                    c.conta.ToString()+"-"+c.dv_conta.ToString(),
                                    _boleto
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

        #region Create
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actioncreate")]
        public ActionResult Create()
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Conta Caixa</b";
            PreencherLookupsCreateEdit();
            g_contas_caixas newRecord = new g_contas_caixas();
            newRecord.is_gerencial = false;
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actioncreate")]
        public ActionResult Create(g_contas_caixas record_g_contas_caixas)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Conta Caixa</b";
            record_g_contas_caixas.id_coligada = 1;
            record_g_contas_caixas.id_filial = 1;
            if (record_g_contas_caixas.nome.EmptyIfNull().ToString() != String.Empty) { record_g_contas_caixas.nome = LibStringFormat.FormatarTextoSimples(record_g_contas_caixas.nome); }
            if (record_g_contas_caixas.nome_fantasia.EmptyIfNull().ToString() != String.Empty) { record_g_contas_caixas.nome_fantasia = LibStringFormat.FormatarTextoSimples(record_g_contas_caixas.nome); }

            // Validações Customizadas
            if (record_g_contas_caixas.cnpj != null)
            {
                if (!(LibStringValidate.ValidarCNPJ(record_g_contas_caixas.cnpj)))
                { ModelState.AddModelError("Model", "Campo [CNPJ] contém um CNPJ inválido"); }
            }

            if (ModelState.IsValid)
            {
                IQueryable<g_contas_caixas> listaContasCaixas = db.g_contas_caixas.Where(p => p.nome == record_g_contas_caixas.nome);
                foreach (g_contas_caixas validacao in listaContasCaixas)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_contas_caixas.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_contas_caixas.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                record_g_contas_caixas.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                db.g_contas_caixas.Add(record_g_contas_caixas);
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
            return View("CreateEdit", record_g_contas_caixas);
        }
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_contas_caixas record_g_contas_caixas = db.g_contas_caixas.Find(id);
            if (record_g_contas_caixas == null)
            {
                return RedirectToAction("Index");
            }
            PreencherLookupsCreateEdit();
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Conta Caixa</b>" + LibStringFormat.GetTabHtml(1) + record_g_contas_caixas.id_conta_caixa.EmptyIfNull().ToString() + " - " + record_g_contas_caixas.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_contas_caixas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_ContasCaixas_*,g_ContasCaixas_Actionupdate")]
        public ActionResult Edit(g_contas_caixas record_g_contas_caixas)
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            if (ModelState.IsValid)
            {
                if (record_g_contas_caixas.nome.EmptyIfNull().ToString() != String.Empty) { record_g_contas_caixas.nome = LibStringFormat.FormatarTextoSimples(record_g_contas_caixas.nome); }
                if (record_g_contas_caixas.nome_fantasia.EmptyIfNull().ToString() != String.Empty) { record_g_contas_caixas.nome_fantasia = LibStringFormat.FormatarTextoSimples(record_g_contas_caixas.nome); }

                IQueryable<g_contas_caixas> listaCidades = db.g_contas_caixas.Where(p => (p.nome == record_g_contas_caixas.nome) && (p.id_conta_caixa != record_g_contas_caixas.id_conta_caixa));
                foreach (g_contas_caixas validacao in listaCidades)
                {
                    // Validação Nome
                    if (validacao.nome.ToString().ToUpper().Equals(record_g_contas_caixas.nome.ToString().ToUpper()))
                    { ModelState.AddModelError("Model", "Campo [Nome] duplicado na base de dados [" + validacao.nome.ToString() + "]"); }
                }
            }

            if (ModelState.IsValid)
            {
                record_g_contas_caixas.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_contas_caixas.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_contas_caixas).State = EntityState.Modified;
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
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Conta Caixa</b>" + LibStringFormat.GetTabHtml(1) + record_g_contas_caixas.id_conta_caixa.EmptyIfNull().ToString() + " - " + record_g_contas_caixas.nome.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_contas_caixas);
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