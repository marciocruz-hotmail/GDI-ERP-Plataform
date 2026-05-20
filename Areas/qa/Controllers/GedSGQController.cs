using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.qa.Controllers
{
    public partial class GedSGQController : Controller
    {
        private GdiPlataformEntities db;
        private readonly String controllerName = "qa_GedSGQ";
        public GedSGQController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        #region DocsSGQ
        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexDocsSGQ_*,qa_GedSGQ_IndexDocsSGQ_Actionread")]
        public ActionResult IndexDocsSGQ()
        {
            PreencherLookupsGedTiposFiltro(8, 0); // Qualidade
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Qualidade - Documentos SGQ";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexDocsSGQ_*,qa_GedSGQ_IndexDocsSGQ_Actionread")]
        public ActionResult GetDadosDocsSGQ(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
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
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; };

            SentencaSQL = " select * from ged_arquivos ged where ged.ativo = 1 ";

            if ((filterDb == false) && (filterAdvanced == false))
            {
                if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1") && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "0"))
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  " + param.yesCustomField01.EmptyIfNull().ToString().Trim();
                    int IdTipo = int.Parse(param.yesCustomField01.EmptyIfNull().ToString().Trim());
                    ged_arquivos_tipos record_ged_arquivos_tipos = allArquivosTipos.Where(t => t.id_arquivo_tipo == IdTipo).FirstOrDefault();
                    if ((record_ged_arquivos_tipos.controle_anual == true) || (record_ged_arquivos_tipos.controle_mensal == true))
                    {
                        if ((param.yesCustomField02.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField03.EmptyIfNull().ToString().Trim().Length > 0))
                        {
                            SentencaSQL += " and ((ged.data_referencia between '" + DataField02.ToString("yyyy-MM-dd") + "' and '" + DataField03.ToString("yyyy-MM-dd") + " ') or (ged.controla_data_referencia = false))";
                        }
                    }
                }
                else
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  -1 ";
                }
                SentencaSQL += " order by ged.data_referencia desc";
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
                String NomeUsuario = allUsuarios.Where(u => u.id_usuario == ged.id_usuario_cadastro).FirstOrDefault().login.EmptyIfNull().ToString();
                String DataReferencia = ged.data_referencia.GetValueOrDefault().ToString("dd/MM/yy");
                if (ged.controla_data_referencia == false) { DataReferencia = "Padrão"; };


                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    ged.id_arquivo.EmptyIfNull().ToString(),
                                    ged.descricao.EmptyIfNull().ToString(),
                                    ged.observacao.EmptyIfNull().ToString(),
                                    ged.filetype.EmptyIfNull().ToString(),
                                    ged.versao.EmptyIfNull().ToString(),
                                    DataReferencia,
                                    NomeUsuario,
                                    "" // Botão Download
                                });
            }

            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; };

            return Json(new
            {
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

        #region Pops
        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexPops_*,qa_GedSGQ_IndexPops_Actionread")]
        public ActionResult IndexPops()
        {
            PreencherLookupsGedTiposFiltro(34, 0); // Pops
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Qualidade - Procedimentos Operacionais Padrão";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexPops_*,qa_GedSGQ_IndexPops_Actionread")]
        public ActionResult GetDadosPops(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
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
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; }
            ;

            SentencaSQL = " select * from ged_arquivos ged where ged.ativo = 1 ";

            if ((filterDb == false) && (filterAdvanced == false))
            {
                if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1") && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "0"))
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  " + param.yesCustomField01.EmptyIfNull().ToString().Trim();
                    int IdTipo = int.Parse(param.yesCustomField01.EmptyIfNull().ToString().Trim());
                    ged_arquivos_tipos record_ged_arquivos_tipos = allArquivosTipos.Where(t => t.id_arquivo_tipo == IdTipo).FirstOrDefault();
                    if ((record_ged_arquivos_tipos.controle_anual == true) || (record_ged_arquivos_tipos.controle_mensal == true))
                    {
                        if ((param.yesCustomField02.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField03.EmptyIfNull().ToString().Trim().Length > 0))
                        {
                            SentencaSQL += " and ((ged.data_referencia between '" + DataField02.ToString("yyyy-MM-dd") + "' and '" + DataField03.ToString("yyyy-MM-dd") + " ') or (ged.controla_data_referencia = false))";
                        }
                    }
                }
                else
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  -1 ";
                }
                SentencaSQL += " order by ged.data_referencia desc";
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
                String NomeUsuario = allUsuarios.Where(u => u.id_usuario == ged.id_usuario_cadastro).FirstOrDefault().login.EmptyIfNull().ToString();
                String DataReferencia = ged.data_referencia.GetValueOrDefault().ToString("dd/MM/yy");
                if (ged.controla_data_referencia == false) { DataReferencia = "Padrão"; }
                ;


                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    ged.id_arquivo.EmptyIfNull().ToString(),
                                    ged.descricao.EmptyIfNull().ToString(),
                                    ged.observacao.EmptyIfNull().ToString(),
                                    ged.filetype.EmptyIfNull().ToString(),
                                    ged.versao.EmptyIfNull().ToString(),
                                    DataReferencia,
                                    NomeUsuario,
                                    "" // Botão Download
                                });
            }

            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; }
            ;

            return Json(new
            {
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

        #region Comunicados
        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexComunicados_*,qa_GedSGQ_IndexComunicados_Actionread")]
        public ActionResult IndexComunicados()
        {
            PreencherLookupsGedTiposFiltro(20, 0); // Comunicados
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Qualidade - Comunicados Internos";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexComunicados_*,qa_GedSGQ_IndexComunicados_Actionread")]
        public ActionResult GetDadosComunicados(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
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
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; }
            ;

            SentencaSQL = " select * from ged_arquivos ged where ged.ativo = 1 ";

            if ((filterDb == false) && (filterAdvanced == false))
            {
                if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1") && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "0"))
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  " + param.yesCustomField01.EmptyIfNull().ToString().Trim();
                    int IdTipo = int.Parse(param.yesCustomField01.EmptyIfNull().ToString().Trim());
                    ged_arquivos_tipos record_ged_arquivos_tipos = allArquivosTipos.Where(t => t.id_arquivo_tipo == IdTipo).FirstOrDefault();
                    if ((record_ged_arquivos_tipos.controle_anual == true) || (record_ged_arquivos_tipos.controle_mensal == true))
                    {
                        if ((param.yesCustomField02.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField03.EmptyIfNull().ToString().Trim().Length > 0))
                        {
                            SentencaSQL += " and ((ged.data_referencia between '" + DataField02.ToString("yyyy-MM-dd") + "' and '" + DataField03.ToString("yyyy-MM-dd") + " ') or (ged.controla_data_referencia = false))";
                        }
                    }
                }
                else
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  -1 ";
                }
                SentencaSQL += " order by ged.data_referencia desc";
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
                String NomeUsuario = allUsuarios.Where(u => u.id_usuario == ged.id_usuario_cadastro).FirstOrDefault().login.EmptyIfNull().ToString();
                String DataReferencia = ged.data_referencia.GetValueOrDefault().ToString("dd/MM/yy");
                if (ged.controla_data_referencia == false) { DataReferencia = "Padrão"; }
                ;


                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    ged.id_arquivo.EmptyIfNull().ToString(),
                                    ged.descricao.EmptyIfNull().ToString(),
                                    ged.observacao.EmptyIfNull().ToString(),
                                    ged.filetype.EmptyIfNull().ToString(),
                                    ged.versao.EmptyIfNull().ToString(),
                                    DataReferencia,
                                    NomeUsuario,
                                    "" // Botão Download
                                });
            }

            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; }
            ;

            return Json(new
            {
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

        #region AtasReunioes
        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexAtasReunioes_*,qa_GedSGQ_IndexAtasReunioes_Actionread")]
        public ActionResult IndexAtasReunioes()
        {
            PreencherLookupsGedTiposFiltro(33, 0); // AtasReunioes
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Qualidade - Atas de Reuniões";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexAtasReunioes_*,qa_GedSGQ_IndexAtasReunioes_Actionread")]
        public ActionResult GetDadosAtasReunioes(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
            // Parâmetros
            bool filterDb = false;
            bool filterAdvanced = false;
            String SentencaSQL = string.Empty;
            g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, filterAdvanced, db);
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
            else if (param.yesFilterAdvancedText.EmptyIfNull().ToString().Trim().Length > 0) { filterAdvanced = true; }
            ;

            SentencaSQL = " select * from ged_arquivos ged where ged.ativo = 1 ";

            if ((filterDb == false) && (filterAdvanced == false))
            {
                if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1") && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "0"))
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  " + param.yesCustomField01.EmptyIfNull().ToString().Trim();
                    int IdTipo = int.Parse(param.yesCustomField01.EmptyIfNull().ToString().Trim());
                    ged_arquivos_tipos record_ged_arquivos_tipos = allArquivosTipos.Where(t => t.id_arquivo_tipo == IdTipo).FirstOrDefault();
                    if ((record_ged_arquivos_tipos.controle_anual == true) || (record_ged_arquivos_tipos.controle_mensal == true))
                    {
                        if ((param.yesCustomField02.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField03.EmptyIfNull().ToString().Trim().Length > 0))
                        {
                            SentencaSQL += " and ((ged.data_referencia between '" + DataField02.ToString("yyyy-MM-dd") + "' and '" + DataField03.ToString("yyyy-MM-dd") + " ') or (ged.controla_data_referencia = false))";
                        }
                    }
                }
                else
                {
                    SentencaSQL += " and ged.id_arquivo_tipo =  -1 ";
                }
                SentencaSQL += " order by ged.data_referencia desc";
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
                String NomeUsuario = allUsuarios.Where(u => u.id_usuario == ged.id_usuario_cadastro).FirstOrDefault().login.EmptyIfNull().ToString();
                String DataReferencia = ged.data_referencia.GetValueOrDefault().ToString("dd/MM/yy");
                if (ged.controla_data_referencia == false) { DataReferencia = "Padrão"; }
                ;


                list.Add(new[] {
                                    "", // Coluna de Seleção
                                    ged.id_arquivo.EmptyIfNull().ToString(),
                                    ged.descricao.EmptyIfNull().ToString(),
                                    ged.observacao.EmptyIfNull().ToString(),
                                    ged.filetype.EmptyIfNull().ToString(),
                                    ged.versao.EmptyIfNull().ToString(),
                                    DataReferencia,
                                    NomeUsuario,
                                    "" // Botão Download
                                });
            }

            if ((filterDb == true) || (filterAdvanced == true)) { filterOnOff = "1"; }
            ;

            return Json(new
            {
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