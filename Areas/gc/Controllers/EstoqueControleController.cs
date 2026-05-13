using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.GDI;
using GdiPlataform.Security;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.gc.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Default")]
    public class EstoqueControleController : Controller
    {
        private GdiPlataformEntities db;

        public EstoqueControleController()
        {
            String Inicio = String.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Produtos - Controles e Aferições";
            return View();
        }

        #region GetDados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle,gc_EstoqueControle_*,gc_EstoqueControle_Actionread")]
        public ActionResult GetDados(jQueryDataTableParamModel param)
        {
            var allRecords = new List<Db.g_produtos_controle>();
            var allRecordsProdutos = db.g_produtos.Select(p => new { p.id_produto, p.descricao }).ToList();
            var allRecordsProdutosStatus = db.g_produtos_status.Select(s => new { s.id_produto_status, s.descricao }).ToList();

            // Perfil Adm visualiza todos os registros independente de Coligada e Filial
            allRecords = db.g_produtos_controle.Where(p => p.id_produto > 0 && p.ativo == true).OrderBy(p => p.id_produto).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            Func<Db.g_produtos_controle, string> orderingFunction = (c =>
                                     param.iSortCol_0 == 1 && param.iSortingCols > 0 ? Convert.ToString(c.id_produto) :
                                     "");
            if (param.sSortDir_0 == "asc") displayedRecords = displayedRecords.OrderBy(orderingFunction);
            else displayedRecords = displayedRecords.OrderByDescending(orderingFunction);

            if (param.iSortingCols > 0)
            {
                if (param.sSortDir_0 == "asc")
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderBy(c => c.id_produto); }
                }
                else
                {
                    if (param.iSortCol_0 == 1) { displayedRecords = displayedRecords.OrderByDescending(c => c.id_produto); }
                }
            }

            List<string[]> list = new List<string[]>();
            foreach (var c in displayedRecords)
            {
                String _ativo = c.ativo == true ? LibIcons.getIcon("fa-solid fa-circle", "Ativo", "green", "") : LibIcons.getIcon("fa-solid fa-circle", "Inativo", "red", "");
                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    c.id_produto_controle.ToString(),
                                    c.serial.EmptyIfNull().ToString(),
                                    allRecordsProdutos.Find(p => p.id_produto == c.id_produto).descricao.EmptyIfNull().ToString(),
                                    allRecordsProdutosStatus.Find(s => s.id_produto_status == c.id_produto_status).descricao.EmptyIfNull().ToString(),
                                    c.lote.EmptyIfNull().ToString(),
                                    c.data_validade.EmptyIfNull().ToString(),
                                    "" // Botão Editar
                                }); ;
            }

            return Json(new
            {
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count(),
                iTotalDisplayRecords = allRecords.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actioncreate")]
        public ActionResult Create()
        {
            g_produtos_controle newRecord = new g_produtos_controle();
            newRecord.ativo = true;
            newRecord.id_coligada = 1;
            newRecord.id_filial = 1;
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosTodos(db);
            ViewBag.comboProdutosFamilia = LibDataSets.LoadComboGcProdutosFamilia(db);
            ViewBag.comboProdutosStatus = LibDataSets.LoadComboGcProdutosStatus(db);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Produtos - Controles e Aferições (Novo)</b";
            return View("CreateEdit", newRecord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actioncreate")]
        public ActionResult AjaxSaveRecord(g_produtos_controle view_g_produtos_controle)
        {
            int qtdInconsistencias = 0;
            String msgRetorno = "";
            bool sucesso = false;
            g_produtos_controle record_old_g_produtos_controle = new g_produtos_controle();

            try
            {
                if (view_g_produtos_controle.id_produto_controle > 0) { record_old_g_produtos_controle = JsonConvert.DeserializeObject<g_produtos_controle>(CachePersister.userIdentity.DataRowInUseSerialized); };

                if (ModelState.IsValid)
                {
                    if (view_g_produtos_controle.id_produto <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += "Campo <b>[Produto]<b/> é de preenchimento obrigatório!" + "<br/>";
                    }
                    if (view_g_produtos_controle.id_produto_familia <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += "Campo <b>[Família]<b/> é de preenchimento obrigatório!" + "<br/>";
                    }
                    if (view_g_produtos_controle.id_produto_status <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += "Campo <b>[Status]<b/> é de preenchimento obrigatório!" + "<br/>";
                    }
                    if (view_g_produtos_controle.serial.EmptyIfNull().ToString().Length <= 0)
                    {
                        qtdInconsistencias += 1;
                        msgRetorno += "Campo <b>[Serial]<b/> é de preenchimento obrigatório!" + "<br/>";
                    }
                }
                else
                {
                    msgRetorno = String.Join(Environment.NewLine, ModelState.Values.SelectMany(v => v.Errors).Select(v => v.ErrorMessage + " " + v.Exception + "<br/>"));
                    qtdInconsistencias += 1;
                }


                if (qtdInconsistencias == 0)
                {
                    if (view_g_produtos_controle.id_produto_controle == 0)
                    {
                        view_g_produtos_controle.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                        view_g_produtos_controle.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                        db.g_produtos_controle.Add(view_g_produtos_controle);
                        db.SaveChanges();

                        String LogAlteracao = LibDB.CompareDataTable(record_old_g_produtos_controle, view_g_produtos_controle);
                        LogAlteracao = "Novo Registro | " + LogAlteracao;
                        LibAudit.SaveAudit(db, true,"g_produtos_controle", view_g_produtos_controle.id_produto_controle, LogAlteracao);
                        sucesso = true;
                    }
                    else
                    {
                        view_g_produtos_controle.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                        view_g_produtos_controle.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                        db.Entry(view_g_produtos_controle).State = EntityState.Modified;
                        db.SaveChanges();

                        String LogAlteracao = LibDB.CompareDataTable(record_old_g_produtos_controle, view_g_produtos_controle);
                        LogAlteracao = "Atualização Dados | " + LogAlteracao;
                        LibAudit.SaveAudit(db, true,"g_produtos_controle", view_g_produtos_controle.id_produto_controle, LogAlteracao);
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
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosImportados(db);
            ViewBag.comboProdutosFamilia = LibDataSets.LoadComboGcProdutosFamilia(db);
            ViewBag.comboProdutosStatus = LibDataSets.LoadComboGcProdutosStatus(db);
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Produtos - Controles e Aferições";
            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actionupdate")]
        public ActionResult Edit(int? id)
        {
            if ((id == null) || (id == 0))
            {
                return RedirectToAction("Index");
            }
            g_produtos_controle record_g_produtos_controle = db.g_produtos_controle.Find(id);
            if (record_g_produtos_controle == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosImportados(db);
            ViewBag.comboProdutosFamilia = LibDataSets.LoadComboGcProdutosFamilia(db);
            ViewBag.comboProdutosStatus = LibDataSets.LoadComboGcProdutosStatus(db);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Produtos - Controles e Aferições</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos_controle.id_produto_controle.EmptyIfNull().ToString() + " - " + record_g_produtos_controle.serial.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_produtos_controle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,gc_EstoqueControle_*,gc_EstoqueControle_Actionupdate")]
        public ActionResult Edit(g_produtos_controle record_g_produtos_controle)
        {
            record_g_produtos_controle.serial = record_g_produtos_controle.serial.Trim().ToUpper();

            if (ModelState.IsValid)
            {
                record_g_produtos_controle.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                record_g_produtos_controle.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(record_g_produtos_controle).State = EntityState.Modified;
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
            ViewBag.comboProdutosServicos = LibDataSets.LoadComboGcProdutosServicosImportados(db);
            ViewBag.comboProdutosFamilia = LibDataSets.LoadComboGcProdutosFamilia(db);
            ViewBag.comboProdutosStatus = LibDataSets.LoadComboGcProdutosStatus(db);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Produtos - Controles e Aferições</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos_controle.id_produto_controle.EmptyIfNull().ToString() + " - " + record_g_produtos_controle.serial.EmptyIfNull().ToString();
            return View("CreateEdit", record_g_produtos_controle);
        }
        #endregion

        #region Medicoes
        public ActionResult GetDadosMedicoes(jQueryDataTableParamModel param)
        {
            int IdProdutoControle = -1;
            int.TryParse(param.yesCustomIdPK, out IdProdutoControle);

            var allRecords = (from _m in db.g_produtos_medicoes
                              join _u in db.g_usuarios on _m.id_usuario_cadastro equals _u.id_usuario into _U
                              from _u in _U.DefaultIfEmpty() // Left Join
                              where (_m.id_produto_controle == IdProdutoControle && _m.ativo == true)
                              orderby _m.datahora_cadastro
                              select new { medicoes = _m, usuario = _u.nome.ToString() }).ToList();
            var displayedRecords = allRecords.Skip(param.iDisplayStart).Take(param.iDisplayLength);

            List<string[]> list = new List<string[]>();
            foreach (var l in displayedRecords)
            {
                list.Add(new[] {
                                    l.medicoes.id_produto_medicao.EmptyIfNull().ToString(), // Coluna de Seleção                
                                    l.medicoes.data_medicao.ToString("dd/MM/yyyy"),
                                    l.medicoes.tensao.ToString(),
                                    "", // Botão Remover
                                });
            }

            return Json(new
            {
                sEcho = param.sEcho,
                iTotalRecords = allRecords.Count(),
                iTotalDisplayRecords = allRecords.Count(),
                aaData = list
            },
            JsonRequestBehavior.AllowGet);
        }
        
        public ActionResult ModalCreateMedicao(int? id)
        {
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-folder-plus", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "<b>Registrar Medição/Aferição</b>";
            ViewBag.id = id;
            g_produtos_medicoes record_g_produtos_medicoes = new g_produtos_medicoes();
            record_g_produtos_medicoes.id_produto_controle = id.GetValueOrDefault();
            record_g_produtos_medicoes.data_medicao = LibDateTime.getDataHoraBrasilia();
            record_g_produtos_medicoes.ativo = true;
            record_g_produtos_medicoes.id_coligada = 1;
            record_g_produtos_medicoes.id_filial = 1;
            return View("ModalCreateMedicao", record_g_produtos_medicoes);
        }

        [HttpPost]
        public ActionResult AjaxCreateMedicao(g_produtos_medicoes view_g_produtos_medicoes)
        {
            bool cadastrado = false;
            int QtdErros = 0;
            String msgRetorno = String.Empty;
            try
            {
                if (view_g_produtos_medicoes.tensao.EmptyIfNull().ToString().Trim().Length == 0)
                {
                    msgRetorno += "Campo <b>Tensão</b> é de preenchimento obrigatório!<br/>";
                    QtdErros += 1;
                }
                if (QtdErros == 0)
                {
                    view_g_produtos_medicoes.ativo = true;
                    view_g_produtos_medicoes.datahora_cadastro = LibDateTime.getDataHoraBrasilia();
                    view_g_produtos_medicoes.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario; ;
                    view_g_produtos_medicoes.id_coligada = 1;
                    view_g_produtos_medicoes.id_filial = 1;
                    db.Entry(view_g_produtos_medicoes).State = EntityState.Added;
                    db.SaveChanges();
                    cadastrado = true;
                    msgRetorno = "Medição <b>Cadastrada</b> com Sucesso!" + LibStringFormat.GetTabHtml(1) + LibIcons.getIcon("fa-regular fa-thumbs-up", "", "#008000", "fa-lg");
                }
            }
            catch (DbEntityValidationException ex)
            {
                cadastrado = false;
                msgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                cadastrado = false;
                msgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = cadastrado, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult AjaxCancelamentoMedicao(g_produtos_medicoes view_g_produtos_medicoes)
        {
            bool Sucesso = false;
            String MsgRetorno = "";
            try
            {
                g_produtos_medicoes record_g_produtos_medicoes = db.g_produtos_medicoes.Find(view_g_produtos_medicoes.id_produto_medicao);
                if (record_g_produtos_medicoes != null)
                {
                    record_g_produtos_medicoes.ativo = false;
                    record_g_produtos_medicoes.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                    record_g_produtos_medicoes.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario; ;
                    db.Entry(record_g_produtos_medicoes).State = EntityState.Modified;
                    db.SaveChanges();
                    MsgRetorno = "Medição CANCELADA com sucesso!";
                    Sucesso = true;
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
        }
        #endregion

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