using DocumentFormat.OpenXml.EMMA;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Areas.gc.Services;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Robos.CotacaoDolar;
using GdiPlataform.Security;
using Newtonsoft.Json;
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

namespace GdiPlataform.Areas.g.Controllers
{
    [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Produtos_*,g_Produtos_Default")]
    public partial class ProdutosController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "g_Produtos";

        public ProdutosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Produtos_*,g_Produtos_Actionread")]
        public ActionResult Index()
        {
            ViewBag.Title = LibIcons.getIcon("fa-regular fa-folder-open", "", "green", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Cadastro de Produtos/Serviços";
            ViewBag.RestoreFilterId = String.Empty;
            ViewBag.RestoreFilterPn = String.Empty;
            ViewBag.RestoreFilterNome = String.Empty;
            ViewBag.RestoreFilterAutoSearch = false;

            g_filtros filtroPersistido = ObterFiltroPersistidoUsuario();
            string idRestore, pnRestore, auxRestore, nomeRestore, descRestore;
            if (TryParseFiltroProdutosSemicolon(filtroPersistido.sql_filtro, out idRestore, out pnRestore, out auxRestore, out nomeRestore, out descRestore))
            {
                ViewBag.RestoreFilterId = idRestore;
                ViewBag.RestoreFilterPn = pnRestore;
                ViewBag.RestoreFilterNome = nomeRestore;
                ViewBag.RestoreFilterAutoSearch = !String.IsNullOrEmpty(idRestore)
                    || !String.IsNullOrEmpty(pnRestore)
                    || !String.IsNullOrEmpty(nomeRestore);
            }
            return View();
        }

        #region GetDados
        //Padronizar numéricos sem Replace(",000",""). Use ToString("N0") ou Math.Truncate.
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Produtos_*,g_Produtos_Actionread")]
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

            var baseQuery = db.g_produtos.AsNoTracking().Where(p => p.ativo);
            int totalRecords = baseQuery.Count();

            // --- FILTRO INLINE (yesCustomField01=Id, 02=PN, 03=Nome) ---
            string idStr = param.yesCustomField01.EmptyIfNull().ToString().Trim();
            string pnStr = param.yesCustomField02.EmptyIfNull().ToString().Trim();
            string nomeStr = param.yesCustomField03.EmptyIfNull().ToString().Trim();
            string auxStr = String.Empty;
            string descStr = String.Empty;
            bool hasInline = !string.IsNullOrEmpty(idStr) || !string.IsNullOrEmpty(pnStr) || !string.IsNullOrEmpty(nomeStr);

            if (!hasInline && !listarTodosExplicito)
            {
                TryParseFiltroProdutosSemicolon(recordFiltro.sql_filtro, out idStr, out pnStr, out auxStr, out nomeStr, out descStr);
                hasInline = !string.IsNullOrEmpty(idStr) || !string.IsNullOrEmpty(pnStr) || !string.IsNullOrEmpty(nomeStr)
                    || !string.IsNullOrEmpty(auxStr) || !string.IsNullOrEmpty(descStr);
            }

            if (!listarTodosExplicito && !hasInline)
            {
                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff = "0",
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = 0,
                    aaData = new List<string[]>()
                }, JsonRequestBehavior.AllowGet);
            }

            IQueryable<Db.g_produtos> query = baseQuery;

            if (hasInline)
            {
                filterApplied = true;
                query = AplicarFiltroProdutosNaQuery(query, idStr, pnStr, auxStr, nomeStr, descStr);
                if (!listarTodosExplicito)
                {
                    LibDB.setFilterByUser(MontarFiltroProdutosPersistido(idStr, pnStr, nomeStr), controllerName, true, db);
                }
            }
            query = query.OrderByDescending(p => p.importado).ThenBy(p => p.codigo);
            int totalDisplayRecords = query.Count();
            int start = Math.Max(0, param.iDisplayStart);
            int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
            if (length > 100) length = 100;

            // --- PAGINAÇÃO + PROJEÇÃO (só colunas necessárias) ---
            var page = query
                .Skip(start)
                .Take(length)
                .Select(p => new
                {
                    p.id_produto,
                    p.importado,
                    p.is_servico,
                    p.codigo,
                    p.nome,
                    p.fob1_dollar,
                    p.valor_base,
                    p.saldo_01_disponivel,
                    p.saldo_03_disponivel
                })
                .ToList();

            var list = page.Select(p =>
            {
                string perfil = p.importado ? LibIcons.getIcon("fa-solid fa-globe", "Produto Validado", "green", "fa-sm")
                                            : LibIcons.getIcon("fa-regular fa-hourglass-half", "Produto Temporário", "gray", "fa-sm");
                if (p.is_servico) perfil = LibIcons.getIcon("fa-solid fa-screwdriver-wrench", "Serviço", "green", "fa-sm");

                return new[]
                {
            "",
            p.id_produto.ToString(),
            perfil,
            p.codigo ?? "",
            p.nome ?? "",
            string.Format("{0:N}", p.fob1_dollar),
            (p.saldo_01_disponivel).ToString("N0"),
            (p.saldo_03_disponivel).ToString("N0")
        };
            }).ToList();

            filterOnOff = filterApplied ? "1" : "0";

            return Json(new
            {
                errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
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

        private static bool TryParseFiltroProdutosSemicolon(string raw, out string id, out string pn, out string aux, out string nome, out string desc)
        {
            id = pn = aux = nome = desc = String.Empty;
            if (String.IsNullOrWhiteSpace(raw)) return false;
            string[] campos = raw.Split(';');
            if (campos.Length < 5) return false;
            id = campos[0].EmptyIfNull().ToString().Trim();
            pn = campos[1].EmptyIfNull().ToString().Trim();
            aux = campos[2].EmptyIfNull().ToString().Trim();
            nome = campos[3].EmptyIfNull().ToString().Trim();
            desc = campos[4].EmptyIfNull().ToString().Trim();
            return !(String.IsNullOrEmpty(id) && String.IsNullOrEmpty(pn) && String.IsNullOrEmpty(aux)
                && String.IsNullOrEmpty(nome) && String.IsNullOrEmpty(desc));
        }

        private static string MontarFiltroProdutosPersistido(string id, string pn, string nome)
        {
            return (id ?? String.Empty) + ";" + (pn ?? String.Empty) + ";;" + (nome ?? String.Empty) + ";";
        }

        private static IQueryable<Db.g_produtos> AplicarFiltroProdutosNaQuery(
            IQueryable<Db.g_produtos> query,
            string idStr,
            string pnStr,
            string auxStr,
            string nomeStr,
            string descStr)
        {
            if (!String.IsNullOrEmpty(idStr) && idStr != "0")
            {
                if (int.TryParse(idStr, out int idProduto))
                {
                    query = query.Where(p => p.id_produto == idProduto);
                }
            }
            if (!String.IsNullOrEmpty(pnStr))
            {
                pnStr = pnStr.Trim();
                if (pnStr.StartsWith("PN:", StringComparison.OrdinalIgnoreCase))
                {
                    pnStr = pnStr.Substring(3).Trim();
                }
                if (pnStr.Length > 0 && LibStringFormat.TryMontarPadraoLikeContemCodigo(pnStr, out string padraoPn))
                {
                    query = query.Where(p => p.codigo != null && DbFunctions.Like(p.codigo, padraoPn));
                }
            }
            if (LibStringFormat.TryMontarPadraoLikeContemCodigo(auxStr, out string padraoAux))
            {
                query = query.Where(p => p.codigo_auxiliar != null && DbFunctions.Like(p.codigo_auxiliar, padraoAux));
            }
            if (!String.IsNullOrEmpty(nomeStr) && nomeStr != "0"
                && LibStringFormat.TryMontarPadraoLikeContemTexto(nomeStr, out string padraoNome))
            {
                query = query.Where(p => p.nome != null && DbFunctions.Like(p.nome, padraoNome));
            }
            if (LibStringFormat.TryMontarPadraoLikeContemTexto(descStr, out string padraoDesc))
            {
                query = query.Where(p => p.descricao != null && DbFunctions.Like(p.descricao, padraoDesc));
            }
            return query;
        }
        #endregion

        #region CreateEdit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Produtos_*,g_Produtos_Actionupdate,g_Produtos_Actionread")]
        public ActionResult ModalCreateEditProduto(int? IdProduto)
        {
            try
            {
                int idProduto = IdProduto.GetValueOrDefault();
                if (idProduto <= 0)
                {
                    return RedirectToAction("Index");
                }
                g_produtos record_g_produtos = db.g_produtos.Find(idProduto);
                if (record_g_produtos == null)
                {
                    return RedirectToAction("Index");
                }
                CachePersister.userIdentity.DataRowInUseSerialized = JsonConvert.SerializeObject(record_g_produtos);
                PreencherLookupsProdutoCreateEdit();
                if (record_g_produtos.is_servico == true) { ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Serviço</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos.id_produto.EmptyIfNull().ToString() + " - " + record_g_produtos.nome.EmptyIfNull().ToString(); }
                else { ViewBag.Title = LibIcons.getIcon("fa-solid fa-search", "", "#0066ff", "fa-lg") + "&nbsp|&nbsp" + LibIcons.getIcon("fa-regular fa-edit", "", "#B7950B", "") + LibStringFormat.GetTabHtml(1) + "<b>Serviço</b>" + LibStringFormat.GetTabHtml(1) + record_g_produtos.id_produto.EmptyIfNull().ToString() + " - " + record_g_produtos.nome.EmptyIfNull().ToString(); }
                return View("ModalCreateEditProduto", record_g_produtos);
            }
            catch (Exception ex)
            {
                String msg = GdiMvcJsonResults.AjaxFailureMessage(ex);
                msg += "<br/>" + "ProdutosController";
                msg += "<br/>" + "ModalCreateEditProduto";
                LibFlashMessage.SetModalMessage(this, msg);
                return RedirectToAction("ModalError", "Error", new { area = "" });
            }
        }
        #endregion

        #region Edit
        [CustomAuthorize(Roles = "SuperAdmin,Admin,g_Produtos_*,g_Produtos_Actionupdate,g_Produtos_Actionread")]
        public ActionResult Edit(int? id)
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult AjaxEditProduto(g_produtos ViewRecordProduto)
        {
            bool Sucesso = false;
            String MsgRetorno = String.Empty;
            String LogAlteracao = string.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            g_produtos SavedRecordProduto = db.g_produtos.Find(ViewRecordProduto.id_produto); 
            g_produtos OldRecordProduto = LibDB.CloneTObject(SavedRecordProduto);

            // Tipo Produto
            if (ViewRecordProduto.id_produto_tipo != SavedRecordProduto.id_produto_tipo)
            {
                if (ViewRecordProduto.id_produto_tipo <= 0)
                {
                    MsgRetorno += "Campo [Tipo do Produto] é de preenchimento obrigatório!<br/>";
                }
                else
                {
                    SavedRecordProduto.id_produto_tipo = ViewRecordProduto.id_produto_tipo;
                }
            }

            // Código
            if (ViewRecordProduto.codigo != SavedRecordProduto.codigo)
            {
                if (ViewRecordProduto.codigo.EmptyIfNull().ToString().Length <= 0)
                {
                    MsgRetorno += "Campo [Código do Produto / PN] é de preenchimento obrigatório!<br/>";
                }
                else
                {
                    ViewRecordProduto.codigo = LibStringFormat.RemoverEspacos(ViewRecordProduto.codigo.EmptyIfNull().Trim().ToUpperInvariant());
                    if (ViewRecordProduto.codigo.StartsWith("PN:")) { ViewRecordProduto.codigo = ViewRecordProduto.codigo.Replace("PN:", ""); };
                    SavedRecordProduto.codigo = ViewRecordProduto.codigo;
                }
            }

            if (SavedRecordProduto.is_servico == false)
            {
                SavedRecordProduto.importado = ViewRecordProduto.importado;
                SavedRecordProduto.has_corecharge = ViewRecordProduto.has_corecharge;
            }

            // Nome
            if (ViewRecordProduto.nome != SavedRecordProduto.nome)
            {
                if (ViewRecordProduto.nome.EmptyIfNull().ToString().Length <= 0)
                {
                    { ModelState.AddModelError("Model", ""); }
                    MsgRetorno += "Campo [Nome do Produto] é de preenchimento obrigatório!<br/>";
                }
                if ((ViewRecordProduto.codigo.EmptyIfNull().ToString().Length > 0) && (ViewRecordProduto.nome.EmptyIfNull().ToString().Length > 0))
                {
                    String PNCompleto = "PN:" + ViewRecordProduto.codigo.EmptyIfNull().ToString() + " - ";
                    if (ViewRecordProduto.nome.EmptyIfNull().ToString().ToUpperInvariant().StartsWith(PNCompleto) == true)
                    {
                        ViewRecordProduto.nome = ViewRecordProduto.nome.EmptyIfNull().Trim().ToUpperInvariant();
                        SavedRecordProduto.nome = ViewRecordProduto.nome;
                    }
                    else
                    {
                        ModelState.AddModelError("Model", "");
                        MsgRetorno += "Campo [Nome do Produto] deverá ter a seguinte estrutura [PN:CodigoDoProduto - NomeDoProduto]!<br/>";
                    }
                }
            }

            // Descricao
            if (ViewRecordProduto.descricao != SavedRecordProduto.descricao)
            {
                if (ViewRecordProduto.descricao.EmptyIfNull().ToString().Length <= 0)
                {
                    MsgRetorno += "Campo [Descrição do Produto] é de preenchimento obrigatório!<br/>";                    
                }
                if ((ViewRecordProduto.codigo.EmptyIfNull().ToString().Length > 0) && (ViewRecordProduto.descricao.EmptyIfNull().ToString().Length > 0))
                {
                    String PNCompleto = "PN:" + ViewRecordProduto.codigo.EmptyIfNull().ToString() + " - ";
                    if (ViewRecordProduto.descricao.EmptyIfNull().ToString().ToUpperInvariant().StartsWith(PNCompleto) == true)
                    {
                        ViewRecordProduto.descricao = ViewRecordProduto.nome.EmptyIfNull().Trim().ToUpperInvariant();
                        SavedRecordProduto.descricao = ViewRecordProduto.nome;
                    }
                    else
                    {
                        MsgRetorno += "Campo [Descrição do Produto] deverá ter a seguinte estrutura [PN:CodigoDoProduto - NomeDoProduto]!<br/>";                        
                    }
                }
            }

            // NCM
            if ((ViewRecordProduto.id_produto_ncm <= 0) && (ViewRecordProduto.importado == true) && (ViewRecordProduto.is_servico == false))
            {
                MsgRetorno += "Campo [NCM do Produto] é de preenchimento obrigatório!<br/>";                
            }
            else if ((ViewRecordProduto.id_produto_ncm != SavedRecordProduto.id_produto_ncm) && (SavedRecordProduto.is_servico == false))
            {
                if (ViewRecordProduto.id_produto_ncm <= 0)
                {
                    MsgRetorno += "Campo [NCM do Produto] é de preenchimento obrigatório!<br/>";                    
                }
                else
                {
                    SavedRecordProduto.id_produto_ncm = ViewRecordProduto.id_produto_ncm;
                }
            }

            // Unidade Medida
            if (ViewRecordProduto.id_unidade_medida_compra <= 0)
            {
                MsgRetorno += "Campo [Unidade Medida] é de preenchimento obrigatório!<br/>";                
            }
            else if ((ViewRecordProduto.id_unidade_medida_compra != SavedRecordProduto.id_unidade_medida_compra) && (SavedRecordProduto.is_servico == false))
            {
                SavedRecordProduto.id_unidade_medida_compra = ViewRecordProduto.id_unidade_medida_compra;
            }

            // Peso
            if (ViewRecordProduto.peso < 0)
            {
                MsgRetorno += "Campo [Peso] é de preenchimento obrigatório!<br/>";                
            }
            else if ((ViewRecordProduto.peso != SavedRecordProduto.peso) && (SavedRecordProduto.is_servico == false))
            {
                SavedRecordProduto.peso = ViewRecordProduto.peso;
            }

            // Regulado ANP
            if (((ViewRecordProduto.item_regulado_anp == true)) && (ViewRecordProduto.codigo_anp.EmptyIfNull().ToString().Length <= 0))
            {
                MsgRetorno += "Campo [Código ANP] é de preenchimento obrigatório para produtos regulamentados!<br/>";                
            }
            else if ((ViewRecordProduto.item_regulado_anp != SavedRecordProduto.item_regulado_anp) || (ViewRecordProduto.codigo_anp != SavedRecordProduto.codigo_anp))
            {
                SavedRecordProduto.item_regulado_anp = ViewRecordProduto.item_regulado_anp;
                SavedRecordProduto.codigo_anp = ViewRecordProduto.codigo_anp;
            }

            if (ViewRecordProduto.id_produto_tipo == 2)
            {
                if (ViewRecordProduto.codigo_anp.EmptyIfNull().ToString().Length <= 0)
                {
                    MsgRetorno += "Para produtos do tipo [ÓLEO LUBRIFICANTE] os campos [Item Regulamentado] e [Código ANP] são de preenchimento obrigatório!<br/>";                    
                }
            }

            SavedRecordProduto.fob1_dollar = ViewRecordProduto.fob1_dollar;
            SavedRecordProduto.fob2_dollar = ViewRecordProduto.fob2_dollar;
            SavedRecordProduto.fob3_dollar = ViewRecordProduto.fob3_dollar;
            SavedRecordProduto.fob1_id_importacao = ViewRecordProduto.fob1_id_importacao;
            SavedRecordProduto.fob2_id_importacao = ViewRecordProduto.fob2_id_importacao;
            SavedRecordProduto.fob3_id_importacao = ViewRecordProduto.fob3_id_importacao;
            SavedRecordProduto.id_estoque01_area = ViewRecordProduto.id_estoque01_area;
            SavedRecordProduto.id_estoque01_secao = ViewRecordProduto.id_estoque01_secao;
            SavedRecordProduto.id_estoque01_corredor = ViewRecordProduto.id_estoque01_corredor;
            SavedRecordProduto.id_estoque01_prateleira = ViewRecordProduto.id_estoque01_prateleira;
            SavedRecordProduto.id_estoque03_area = ViewRecordProduto.id_estoque03_area;
            SavedRecordProduto.id_estoque03_secao = ViewRecordProduto.id_estoque03_secao;
            SavedRecordProduto.id_estoque03_corredor = ViewRecordProduto.id_estoque03_corredor;
            SavedRecordProduto.id_estoque03_prateleira = ViewRecordProduto.id_estoque03_prateleira;
            SavedRecordProduto.item_regulado_ibama = ViewRecordProduto.item_regulado_ibama;
            SavedRecordProduto.item_regulado_anp = ViewRecordProduto.item_regulado_anp;
            SavedRecordProduto.codigo_anp = ViewRecordProduto.codigo_anp;
            SavedRecordProduto.item_regulado_pf = ViewRecordProduto.item_regulado_pf;
            SavedRecordProduto.item_regulado_joguelimpo = ViewRecordProduto.item_regulado_joguelimpo;
            SavedRecordProduto.controla_estoque = ViewRecordProduto.controla_estoque;
            SavedRecordProduto.controla_serial = ViewRecordProduto.controla_serial;
            SavedRecordProduto.controla_lote = ViewRecordProduto.controla_lote;
            SavedRecordProduto.item_venda = ViewRecordProduto.item_venda;
            SavedRecordProduto.item_revenda = ViewRecordProduto.item_revenda;
            SavedRecordProduto.item_uso_consumo = ViewRecordProduto.item_uso_consumo;

            if (MsgRetorno.EmptyIfNull().ToString().Trim().Length == 0)
            {
                bool ProdutoDuplicado = false;

                // Validar Duplicidade por código
                if (ViewRecordProduto.codigo.EmptyIfNull().ToString().Length > 0)
                {
                    g_produtos record_g_produto_duplicado = db.g_produtos.Where(p => p.codigo == ViewRecordProduto.codigo && p.id_produto != ViewRecordProduto.id_produto && p.ativo == true).FirstOrDefault();
                    if (record_g_produto_duplicado != null)
                    {
                        ProdutoDuplicado = true;
                        MsgRetorno += "Código [" + record_g_produto_duplicado.codigo.EmptyIfNull().ToString() + "] já cadastrado na base de dados Id. [" + record_g_produto_duplicado.id_produto.EmptyIfNull().ToString() + "]!<br/>";                        
                    }
                }

                // Validar Duplicidade por código auxiliar
                if ((ViewRecordProduto.codigo_auxiliar.EmptyIfNull().ToString().Length > 0) && (ProdutoDuplicado == false))
                {
                    g_produtos record_g_produto_duplicado = db.g_produtos.Where(p => p.codigo_auxiliar == ViewRecordProduto.codigo_auxiliar && p.id_produto != ViewRecordProduto.id_produto && p.ativo == true).FirstOrDefault();
                    if (record_g_produto_duplicado != null)
                    {
                        ProdutoDuplicado = true;
                        MsgRetorno += "Código [" + record_g_produto_duplicado.codigo_auxiliar.EmptyIfNull().ToString() + "] já cadastrado na base de dados Id. [" + record_g_produto_duplicado.id_produto.EmptyIfNull().ToString() + "]!<br/>";                        
                    }
                }

                // Validar Duplicidade por nome
                if ((ViewRecordProduto.nome.EmptyIfNull().ToString().Length > 0) && (ProdutoDuplicado == false))
                {
                    g_produtos record_g_produto_duplicado = db.g_produtos.Where(p => p.nome == ViewRecordProduto.nome && p.id_produto != ViewRecordProduto.id_produto && p.ativo == true).FirstOrDefault();
                    if (record_g_produto_duplicado != null)
                    {
                        ProdutoDuplicado = true;
                        MsgRetorno += "Produto [" + record_g_produto_duplicado.nome.EmptyIfNull().ToString() + "] já cadastrado na base de dados Id. [" + record_g_produto_duplicado.id_produto.EmptyIfNull().ToString() + "]!<br/>";                        
                    }
                }

                // Validar Duplicidade por descrição
                if ((ViewRecordProduto.descricao.EmptyIfNull().ToString().Length > 0) && (ProdutoDuplicado == false))
                {
                    g_produtos record_g_produto_duplicado = db.g_produtos.Where(p => p.descricao == ViewRecordProduto.descricao && p.id_produto != ViewRecordProduto.id_produto && p.ativo == true).FirstOrDefault();
                    if (record_g_produto_duplicado != null)
                    {
                        ProdutoDuplicado = true;
                        MsgRetorno += "Produto [" + record_g_produto_duplicado.descricao.EmptyIfNull().ToString() + "] já cadastrado na base de dados Id. [" + record_g_produto_duplicado.id_produto.EmptyIfNull().ToString() + "]!<br/>";                        
                    }
                }
            }

            if (MsgRetorno.EmptyIfNull().ToString().Trim().Length == 0)
            {
                SavedRecordProduto.codigo_auxiliar = LibStringFormat.GDIFormatarCodigoAuxiliarProduto(ViewRecordProduto.codigo);
                SavedRecordProduto.codigo_variacao1 = SavedRecordProduto.codigo_auxiliar.Replace("0", "O");
                SavedRecordProduto.codigo_variacao2 = SavedRecordProduto.codigo_auxiliar.Replace("O", "0");
                SavedRecordProduto.datahora_alteracao = LibDateTime.getDataHoraBrasilia();
                SavedRecordProduto.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                SavedRecordProduto.datahora_alteracao = DataHoraAtual;
                SavedRecordProduto.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(SavedRecordProduto).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();

                    // Log
                    LogAlteracao = LibDB.CompareDataTable(OldRecordProduto, SavedRecordProduto);
                    if (LogAlteracao.EmptyIfNull().ToString().Length > 0) { LibAudit.SaveAudit(db, true, "g_produtos", SavedRecordProduto.id_produto, "Atualização Dados | " + LogAlteracao.EmptyIfNull().ToLowerInvariant()); };

                    Sucesso = true;
                    MsgRetorno += "Produto <b>Alterado</b> com sucesso!";
                }
                catch (DbEntityValidationException ex)
                {
                    Sucesso = false;
                    MsgRetorno = GdiMvcJsonResults.AjaxFailureValidationMessage(ex);
                }
                catch (Exception e)
                {
                    Sucesso = false;
                    MsgRetorno = GdiMvcJsonResults.AjaxFailureMessage(e);
                }
            }
            return Json(new { success = Sucesso, msg = MsgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalAtualizarCadastroProdutos
        public ActionResult ModalAtualizarCadastroProdutos()
        {
            ViewBag.Title = "Produtos/Serviços - Atualizar Cadastro";
            return View();
        }

        [HttpPost]
        public ActionResult ajaxAtualizarCadastroProdutos()
        {
            bool Processado = false;
            String MsgRetorno = String.Empty;
            String TipoMovimento = String.Empty;
            String LogAudit = string.Empty;
            String ListaProdutosAtualizados = string.Empty;
            String ListaProdutosErro = string.Empty;
            int idProcessamentoGravado = 0;
            int QtdRegistrosAtualizados = 0;
            /*int QtdRegistrosAtualizar = 0;
            int QtdRegistrosProdutos = 0;
            int QtdRegistrosComexProduto = 0;
            int QtdRegistrosInvoicesItens = 0;
            int QtdRegistrosImportacoesItens = 0;*/
            String ArquivoSaida = String.Empty;
            String FileNameDestino = String.Empty;
            String FileNameExportacao = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            List<g_clientes> ListaClientesAtualizar = new List<g_clientes>();
            List<g_clientes_audit> ListaClientesAudit = new List<g_clientes_audit>();
            List<g_vendedores> ListaVendedores = db.g_vendedores.ToList();
            List<g_clientes> ListaClientesAtuais = db.g_clientes.Where(c => c.is_cliente == true && c.ativo == true).ToList();


            foreach (g_clientes RecordCliente in ListaClientesAtuais)
            {
                int IdVendedorGDI = 0;
                String LogAlteracao = String.Empty;
                LogAlteracao += "Atualização de carteira, vendedores anteriores (";

                try { LogAlteracao += ListaVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor.GetValueOrDefault()).FirstOrDefault().nome.EmptyIfNull().ToString() + ", "; } catch (Exception) { }
                ;
                try { LogAlteracao += ListaVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor2).FirstOrDefault().nome.EmptyIfNull().ToString() + ", "; } catch (Exception) { }
                ;
                try { LogAlteracao += ListaVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor3).FirstOrDefault().nome.EmptyIfNull().ToString() + ") "; } catch (Exception) { }
                ;

                if ((RecordCliente.id_vendedor == 1) || (RecordCliente.id_vendedor == 3) || (RecordCliente.id_vendedor == 4) || (RecordCliente.id_vendedor == 4) || (RecordCliente.id_vendedor == 8) || (RecordCliente.id_vendedor == 9)) { IdVendedorGDI = RecordCliente.id_vendedor.GetValueOrDefault(); }
                ;
                if (IdVendedorGDI <= 0)
                {
                    if ((RecordCliente.id_vendedor2 == 1) || (RecordCliente.id_vendedor2 == 3) || (RecordCliente.id_vendedor2 == 4) || (RecordCliente.id_vendedor2 == 4) || (RecordCliente.id_vendedor2 == 8) || (RecordCliente.id_vendedor2 == 9)) { IdVendedorGDI = RecordCliente.id_vendedor2; }
                    ;
                }
                if (IdVendedorGDI <= 0)
                {
                    if ((RecordCliente.id_vendedor3 == 1) || (RecordCliente.id_vendedor3 == 3) || (RecordCliente.id_vendedor3 == 4) || (RecordCliente.id_vendedor3 == 4) || (RecordCliente.id_vendedor3 == 8) || (RecordCliente.id_vendedor3 == 9)) { IdVendedorGDI = RecordCliente.id_vendedor3; }
                    ;
                }
                if (IdVendedorGDI <= 0)
                {
                    IdVendedorGDI = 2;
                }

                RecordCliente.id_vendedor = IdVendedorGDI;
                RecordCliente.id_vendedor2 = 2;
                RecordCliente.id_vendedor3 = 6;

                ListaClientesAtualizar.Add(RecordCliente);

                LogAlteracao += "vendedores atualizados (";
                try { LogAlteracao += ListaVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor.GetValueOrDefault()).FirstOrDefault().nome.EmptyIfNull().ToString() + ", "; } catch (Exception) { }
                ;
                try { LogAlteracao += ListaVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor2).FirstOrDefault().nome.EmptyIfNull().ToString() + ", "; } catch (Exception) { }
                ;
                try { LogAlteracao += ListaVendedores.Where(v => v.id_vendedor == RecordCliente.id_vendedor3).FirstOrDefault().nome.EmptyIfNull().ToString() + ") "; } catch (Exception) { }
                ;

                LibAudit.SaveAudit(db, true, "g_clientes", RecordCliente.id_cliente, LogAlteracao);
            }


            foreach (g_clientes RecordClienteAtualizar in ListaClientesAtualizar)
            {
                RecordClienteAtualizar.datahora_alteracao = DataHoraAtual;
                RecordClienteAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(RecordClienteAtualizar).State = EntityState.Modified;
                QtdRegistrosAtualizados += 1;
            }


            /*String SQLText = " select item.* from gc_movimentos_itens item " +
                             " left join gc_movimentos mov on (item.id_movimento = mov.id_movimento) " +
                             " where mov.id_movimento_tipo = 7 " +
                             " and mov.id_importacao > 0 " +
                             " and item.recebimento_peso_unit > 0 " +
                             " order by id_movimento_item desc ";

            List <gc_movimentos_itens> ListaItensImportados = db.gc_movimentos_itens.SqlQuery(SQLText).ToList();
            List<g_produtos> ListaProdutosGeral = db.g_produtos.Where(p => p.ativo == true && p.importado == true).ToList();
            List<g_produtos> ListaProdutosAtualizar = new List<g_produtos>();

            foreach (g_produtos RecordProduto in ListaProdutosGeral)
            {
                if (RecordProduto.peso <= 0)
                {
                    gc_movimentos_itens ItemImportado = ListaItensImportados.Where(i => i.id_produto == RecordProduto.id_produto).FirstOrDefault();
                    if (ItemImportado != null)
                    {
                        RecordProduto.peso = ItemImportado.recebimento_peso_unit;
                        ListaProdutosAtualizar.Add(RecordProduto);
                    }
                }
            }

            foreach (g_produtos RecordProdutoAtualizar in ListaProdutosAtualizar)
            {
                RecordProdutoAtualizar.datahora_alteracao = DataHoraAtual;
                RecordProdutoAtualizar.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(RecordProdutoAtualizar).State = EntityState.Modified;
                QtdRegistrosProdutos += 1;
            }

            if (QtdRegistrosProdutos > 0) { db.SaveChanges(); };*/

            Processado = true;
            MsgRetorno += QtdRegistrosAtualizados.ToString() + " Registros atualizados com sucesso" + "<br/><br/>";
            /*MsgRetorno += QtdRegistrosAtualizar.ToString() + " Itens atualizados com sucesso" + "<br/><br/>";
            MsgRetorno += QtdRegistrosProdutos.ToString() + " Produtos Atualizados" + "<br/>";
            MsgRetorno += QtdRegistrosComexProduto.ToString() + " Produtos Comex Atualizados" + "<br/>";
            MsgRetorno += QtdRegistrosInvoicesItens.ToString() + " Itens de Invoice Atualizados" + "<br/>";
            MsgRetorno += QtdRegistrosImportacoesItens.ToString() + " Itens de Importação Atualizados" + "<br/>";*/
            return Json(new { success = Processado, msg = MsgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }

        public decimal SetCotacaoDollarDia()
        {
            Decimal CotacaoDolarDia = CachePersister.userIdentity.CotacaoDollarDia;
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia().Date;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            g_cotacoes record_g_cotacoes = null;
            try
            {
                if (CotacaoDolarDia == 0)
                {
                    RoboCotacaoDolar BotCotacaoDolar = new RoboCotacaoDolar();
                    CotacaoDolarDia = Convert.ToDecimal(BotCotacaoDolar.GetCotacaoDolarDia());
                    if (CotacaoDolarDia > 0)
                    {
                        try
                        {
                            record_g_cotacoes = db.g_cotacoes.Where(c => c.id_moeda == 2 && c.cotacao_data == DataAtual).FirstOrDefault();
                            if (record_g_cotacoes == null)
                            {
                                record_g_cotacoes = new g_cotacoes();
                                record_g_cotacoes.id_moeda = 2;
                                record_g_cotacoes.cotacao_data = DataAtual;
                                record_g_cotacoes.cotacao_ultimo_valor = CotacaoDolarDia;
                                record_g_cotacoes.cotacao_menor_valor = CotacaoDolarDia;
                                record_g_cotacoes.cotacao_maior_valor = CotacaoDolarDia;
                                db.g_cotacoes.Add(record_g_cotacoes);
                                db.SaveChanges();
                            }
                            else
                            {
                                record_g_cotacoes.cotacao_ultimo_valor = CotacaoDolarDia;
                                if (CotacaoDolarDia < record_g_cotacoes.cotacao_menor_valor) { record_g_cotacoes.cotacao_menor_valor = CotacaoDolarDia; };
                                if (CotacaoDolarDia > record_g_cotacoes.cotacao_maior_valor) { record_g_cotacoes.cotacao_maior_valor = CotacaoDolarDia; };
                                db.Entry(record_g_cotacoes).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                        }
                        catch (Exception) { }
                        ;
                    }
                    else
                    {
                        try
                        {
                            record_g_cotacoes = db.g_cotacoes.OrderByDescending(c => c.id_cotacao).FirstOrDefault();
                            if (record_g_cotacoes != null) { CotacaoDolarDia = record_g_cotacoes.cotacao_ultimo_valor; } else { CotacaoDolarDia = 0; }
                        }
                        catch (Exception)
                        {
                            CotacaoDolarDia = 0;
                        }
                        ;
                    }
                    CachePersister.userIdentity.CotacaoDollarDia = CotacaoDolarDia;
                }

            }
            catch (Exception)
            {

            }
            ;
            return CotacaoDolarDia;
        }

        public String GetCodigoCuringa(String CodigoProduto)
        {
            CodigoProduto = CodigoProduto.Trim();
            String CodigoCuringa = string.Empty;
            int Slice = Convert.ToInt32(Math.Truncate(CodigoProduto.Length / 2d));

            if (Slice >= 15) { Slice = Slice - 6; }
            else if (Slice >= 13) { Slice = Slice - 5; }
            else if (Slice >= 11) { Slice = Slice - 4; }
            else if (Slice >= 9) { Slice = Slice - 3; }
            else if (Slice >= 7) { Slice = Slice - 2; }
            else if (Slice >= 5) { Slice = Slice - 1; }

            CodigoCuringa = CodigoProduto.Substring(0, CodigoProduto.Length - Slice);
            for (int x = 0; x < Slice; x++)
            {
                CodigoCuringa += "_";
            }
            return CodigoCuringa;
        }


        #endregion

        #region ModalDesativarCadastroProduto
        public ActionResult ModalDesativarCadastroProduto(String id)
        {
            int IdProduto = 0;
            int.TryParse(id, out IdProduto);
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-circle-info", "", "#808080", "fa-sm") + LibStringFormat.GetTabHtml(1) + "Produto - Desativar Cadastro";
            PreencherLookupsModalDesativarProduto();
            g_produtos view_g_produtos = db.g_produtos.Find(IdProduto);
            return View(view_g_produtos);
        }

        [HttpPost]
        public ActionResult ajaxDesativarCadastroProduto(g_produtos view_g_produtos)
        {
            int qtdErros = 0;
            bool sucesso = false;
            String msgRetorno = String.Empty;
            String TextoSQLAtualizacao = String.Empty;
            g_produtos ProdutoSubstituto = null;
            g_produtos ProdutoDesativado = null;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            try
            {
                if (view_g_produtos.id_produto <= 0)
                {
                    qtdErros += 1;
                    msgRetorno += "Item principal não localizado!" + "<br/>";
                }
                else
                {
                    ProdutoDesativado = db.g_produtos.Find(view_g_produtos.id_produto);
                    if (ProdutoDesativado.ativo == false)
                    {
                        qtdErros += 1;
                        msgRetorno += "Item principal está desativado!" + "<br/>";
                    }
                    if (ProdutoDesativado.bloqueado == true)
                    {
                        qtdErros += 1;
                        msgRetorno += "Item principal está Bloqueado e não pode ser desativado!" + "<br/>";
                    }
                }

                if (view_g_produtos.id_produto_substituto <= 0)
                {
                    qtdErros += 1;
                    msgRetorno += "Item substituto não localizado!" + "<br/>";
                }
                else
                {
                    ProdutoSubstituto = db.g_produtos.Find(view_g_produtos.id_produto_substituto);
                    if (ProdutoSubstituto.ativo == false)
                    {
                        qtdErros += 1;
                        msgRetorno += "Item substituto está desativado!" + "<br/>";
                    }
                }
                if (qtdErros == 0)
                {
                    if (view_g_produtos.id_produto == view_g_produtos.id_produto_substituto)
                    {
                        qtdErros += 1;
                        msgRetorno += "Item principal e substituto não podem ser o mesmo item!" + "<br/>";
                    }
                }
                if (qtdErros == 0)
                {
                    if ((ProdutoDesativado.fob1_dollar > 0) && (ProdutoSubstituto.fob1_dollar == 0))
                    {
                        ProdutoSubstituto.fob1_dollar = ProdutoDesativado.fob1_dollar;
                        ProdutoSubstituto.fob1_id_importacao = ProdutoDesativado.fob1_id_importacao;
                    }
                    if ((ProdutoDesativado.fob2_dollar > 0) && (ProdutoSubstituto.fob2_dollar == 0))
                    {
                        ProdutoSubstituto.fob2_dollar = ProdutoDesativado.fob2_dollar;
                        ProdutoSubstituto.fob2_id_importacao = ProdutoDesativado.fob2_id_importacao;
                    }
                    if ((ProdutoDesativado.fob3_dollar > 0) && (ProdutoSubstituto.fob3_dollar == 0))
                    {
                        ProdutoSubstituto.fob3_dollar = ProdutoDesativado.fob3_dollar;
                        ProdutoSubstituto.fob3_id_importacao = ProdutoDesativado.fob3_id_importacao;
                    }

                    ProdutoDesativado.ativo = false;
                    ProdutoDesativado.id_produto_substituto = view_g_produtos.id_produto_substituto;
                    ProdutoDesativado.datahora_desativacao = LibDateTime.getDataHoraBrasilia();
                    ProdutoDesativado.id_usuario_desativacao = CachePersister.userIdentity.IdUsuario;
                    ProdutoDesativado.datahora_alteracao = DataHoraAtual;
                    ProdutoDesativado.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                    db.Entry(ProdutoDesativado).State = EntityState.Modified;
                    db.SaveChanges();

                    // Itens de Pedidos
                    TextoSQLAtualizacao = " update gc_movimentos_itens set id_produto =  " + view_g_produtos.id_produto_substituto.EmptyIfNull().ToString() + " " +
                                          " where id_produto = " + view_g_produtos.id_produto.EmptyIfNull().ToString() + " " +
                                           " and id_movimento_item > 0";
                    int QtdItensPedidos = LibDB.dbQueryExec(TextoSQLAtualizacao, db);

                    // Produtos Comex
                    TextoSQLAtualizacao = " update gc_comex_produtos set id_produto =  " + view_g_produtos.id_produto_substituto.EmptyIfNull().ToString() + " " +
                                                 " where id_produto = " + view_g_produtos.id_produto.EmptyIfNull().ToString() + " " +
                                                 " and id_comex_produto > 0";
                    int QtdComexProdutos = LibDB.dbQueryExec(TextoSQLAtualizacao, db);


                    // Comex Invoices Itens
                    TextoSQLAtualizacao = " update gc_comex_invoices_itens set id_produto =  " + view_g_produtos.id_produto_substituto.EmptyIfNull().ToString() + " " +
                                                 " where id_produto = " + view_g_produtos.id_produto.EmptyIfNull().ToString() + " " +
                                                 " and id_invoice_item > 0";
                    int QtdComexInvoicesItens = LibDB.dbQueryExec(TextoSQLAtualizacao, db);


                    // Comex Invoices Itens
                    TextoSQLAtualizacao = " update gc_comex_importacoes_itens set id_produto =  " + view_g_produtos.id_produto_substituto.EmptyIfNull().ToString() + " " +
                                                 " where id_produto = " + view_g_produtos.id_produto.EmptyIfNull().ToString() + " " +
                                                 " and id_importacao_item > 0";
                    int QtdComexImportacoesItens = LibDB.dbQueryExec(TextoSQLAtualizacao, db);



                    sucesso = true;
                    msgRetorno += "Item substituído com sucesso!" + "<br/>";
                    if (QtdItensPedidos > 0) { msgRetorno += QtdItensPedidos.EmptyIfNull().ToString() + " Itens de Pedidos Atualizados!" + "<br/>"; }
                    ;
                    if (QtdComexProdutos > 0) { msgRetorno += QtdComexProdutos.EmptyIfNull().ToString() + " Produtos Comex Atualizados!" + "<br/>"; }
                    ;
                    if (QtdComexInvoicesItens > 0) { msgRetorno += QtdComexInvoicesItens.EmptyIfNull().ToString() + " Itens de Invoices Atualizados!" + "<br/>"; }
                    ;
                    if (QtdComexImportacoesItens > 0) { msgRetorno += QtdComexImportacoesItens.EmptyIfNull().ToString() + " Itens de Importação Atualizados!" + "<br/>"; }
                    ;
                }

            }
            catch (DbEntityValidationException ex)
            {
                return JsonAjaxErroValidacao(ex);
            }
            catch (Exception e)
            {
                return JsonAjaxErro(e);
            }

            return Json(new { success = sucesso, msg = msgRetorno }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ModalViewFichaEstoqueProduto
        public ActionResult ModalViewFichaEstoqueProduto(String id)
        {
            int IdProduto = 0;
            int.TryParse(id, out IdProduto);
            ViewBag.TitleIcon = LibIcons.getIcon("fa-solid fa-circle-info", "Ficha de estoque", "blue", "fa-lg");
            ViewBag.TitleLinha1 = "<b>FICHA ESTOQUE</b>";
            ViewBag.TitleLinha2 = null;
            g_produtos RecordProduto = db.g_produtos.Find(IdProduto);
            if (RecordProduto != null)
            {
                String NomeProduto = RecordProduto.nome.EmptyIfNull().ToString();
                int _DisplayScreenWidth = 0;
                int _SizeNomeItem = 100;
                int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                _SizeNomeItem = (_DisplayScreenWidth / 100) * 8;
                if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 500)) { _SizeNomeItem = (_DisplayScreenWidth / 100 * 10); }
                if (NomeProduto.Length > _SizeNomeItem) { NomeProduto = NomeProduto.Substring(0, _SizeNomeItem) + "..."; }
                ViewBag.TitleLinha2 = RecordProduto.id_produto.EmptyIfNull().ToString() + " - " + NomeProduto;
            }
            return View(RecordProduto);
        }

        public ActionResult GetFichaEstoqueProduto(jQueryDataTableParamModel param)
        {
            if (param == null) { param = new jQueryDataTableParamModel(); }
            String filterOnOff = "0";
            try
            {
            String SentencaSQL = string.Empty;
            int IdProduto = 0;
            int.TryParse(param.yesCustomIdPK, out IdProduto);
            SentencaSQL += " SELECT movest.id_estoque_movimento, ";
            SentencaSQL += "        movtipo.nome AS movimento_tipo, ";
            SentencaSQL += "        imp.numero AS importacao, ";
            SentencaSQL += "        movest.id_estoque_movimento_tipo, ";
            SentencaSQL += "        produto.nome AS produto, ";
            SentencaSQL += "        movest.id_produto, movest.id_local_estoque, movest.id_inventario, ";
            SentencaSQL += "        movest.id_movimento, movest.id_estoque_transferencia, ";
            SentencaSQL += "        movest.qtd_disponivel, movest.saldo_disponivel, ";
            SentencaSQL += "        movest.datahora_cadastro, movest.id_usuario_cadastro ";
            SentencaSQL += " FROM gc_estoque_movimento movest ";
            SentencaSQL += " LEFT JOIN gc_movimentos mov ON (mov.id_movimento = movest.id_movimento) ";
            SentencaSQL += " LEFT JOIN gc_estoque_movimento_tipo movtipo ON (movest.id_estoque_movimento_tipo = movtipo.id_estoque_movimento_tipo) ";
            SentencaSQL += " LEFT JOIN g_produtos produto ON (movest.id_produto = produto.id_produto) ";
            SentencaSQL += " LEFT JOIN gc_comex_importacoes imp ON (imp.id_importacao = mov.id_importacao) ";
            SentencaSQL += " WHERE movest.id_produto = " + IdProduto.ToString() + " ";
            SentencaSQL += " ORDER BY movest.datahora_cadastro DESC ";

            DataTable TableFichaEstoque = LibDB.GetDataTable(SentencaSQL, db);
            List<DataRow> AllMovimentosFicha = TableFichaEstoque.AsEnumerable().ToList();
            List<string[]> list = new List<string[]>();
            var displayedRecords = AllMovimentosFicha.Skip(param.iDisplayStart).Take(param.iDisplayLength);
            foreach (var row in displayedRecords)
            {
                String LocalEstoque = string.Empty;
                String QtdMovimentoBH = string.Empty;
                String QtdSaldoBH = string.Empty;
                String QtdMovimentoSP = string.Empty;
                String QtdSaldoSP = string.Empty;
                String TipoMovimento = string.Empty;

                if (row["id_local_estoque"].EmptyIfNull().ToString() == "1")
                {
                    LocalEstoque = "BH";
                    QtdMovimentoBH = row["qtd_disponivel"].EmptyIfNull().ToString().Replace(",000", "");
                    QtdSaldoBH = row["saldo_disponivel"].EmptyIfNull().ToString().Replace(",000", "");
                }
                else if (row["id_local_estoque"].EmptyIfNull().ToString() == "3")
                {
                    LocalEstoque = "SP";
                    QtdMovimentoSP = row["qtd_disponivel"].EmptyIfNull().ToString().Replace(",000", "");
                    QtdSaldoSP = row["saldo_disponivel"].EmptyIfNull().ToString().Replace(",000", "");
                }

                TipoMovimento += row["movimento_tipo"].EmptyIfNull().ToString().Trim();

                if (row["importacao"].EmptyIfNull().ToString().Trim().Length > 0) { TipoMovimento += "  " + row["importacao"].EmptyIfNull().ToString(); }
                else if ((row["id_inventario"].EmptyIfNull().ToString() != "-1") && (row["id_inventario"].EmptyIfNull().ToString() != "0")) { TipoMovimento += " Nº " + row["id_inventario"].EmptyIfNull().ToString(); }
                else if ((row["id_movimento"].EmptyIfNull().ToString() != "-1") && (row["id_movimento"].EmptyIfNull().ToString() != "0")) { TipoMovimento += " Nº " + row["id_movimento"].EmptyIfNull().ToString(); }
                else if ((row["id_estoque_transferencia"].EmptyIfNull().ToString() != "-1") && (row["id_estoque_transferencia"].EmptyIfNull().ToString() != "0")) { TipoMovimento += " Nº " + row["id_estoque_transferencia"].EmptyIfNull().ToString(); }

                list.Add(new[] {
                                    row["id_estoque_movimento"].EmptyIfNull().ToString(), // Coluna de Seleção
                                    Convert.ToDateTime(row["datahora_cadastro"]).ToString("dd/MM/yy HH:mm"),
                                    TipoMovimento,
                                    LocalEstoque,
                                    QtdMovimentoBH,
                                    QtdSaldoBH,
                                    QtdMovimentoSP,
                                    QtdSaldoSP,
                                });
            }

            return Json(new
            {
                errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                yesFilterOnOff = filterOnOff,
                sEcho = param.sEcho,
                iTotalRecords = AllMovimentosFicha.Count(),
                iTotalDisplayRecords = AllMovimentosFicha.Count(),
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

        private JsonResult JsonAjaxErro(Exception ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailure(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonAjaxErroValidacao(DbEntityValidationException ex)
        {
            return Json(GdiMvcJsonResults.AjaxFailureValidation(ex), JsonRequestBehavior.AllowGet);
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (db != null) { db.Dispose(); }
                ;
            }
            base.Dispose(disposing);
        }
    }
}
