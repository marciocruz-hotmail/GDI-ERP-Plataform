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
        public ActionResult GetDadosDocsSGQ(jQueryDataTableParamModel param) => JsonGedSgqArquivosDataTable(param);
        #endregion

        #region Pops
        [GdiPageScripts(GdiPageScriptsFlags.LayoutLite)]
        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexPops_*,qa_GedSGQ_IndexPops_Actionread")]
        public ActionResult IndexPops()
        {
            PreencherLookupsGedTiposFiltro(34, 0); // Pops
            ViewBag.Title = LibIcons.getIcon("fa-solid fa-box-archive", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Qualidade - Procedimentos Operacionais Padrão";
            return View();
        }

        [CustomAuthorize(Roles = "SuperAdmin,Admin,qa_GedSGQ_Default,qa_GedSGQ_IndexPops_*,qa_GedSGQ_IndexPops_Actionread")]
        public ActionResult GetDadosPops(jQueryDataTableParamModel param) => JsonGedSgqArquivosDataTable(param);
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
        public ActionResult GetDadosComunicados(jQueryDataTableParamModel param) => JsonGedSgqArquivosDataTable(param);
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
        public ActionResult GetDadosAtasReunioes(jQueryDataTableParamModel param) => JsonGedSgqArquivosDataTable(param);
        #endregion

        /// <summary>Grid GED SGQ — COUNT + OFFSET/FETCH (PERF-007); usado pelas 4 actions GetDados*.</summary>
        private JsonResult JsonGedSgqArquivosDataTable(jQueryDataTableParamModel param)
        {
            String filterOnOff = "0";
            if (param == null) { param = new jQueryDataTableParamModel(); }
            try
            {
                g_filtros record_g_filtro = LibDB.getFilterByUser(param, controllerName, db);
                bool filterDb = record_g_filtro.sql_filtro.EmptyIfNull().ToString().Trim().Length > 0;
                List<ged_arquivos_tipos> allArquivosTipos = db.ged_arquivos_tipos.AsNoTracking().Where(t => t.ativo == true).ToList();
                List<string[]> list = new List<string[]>();
                DateTime DataField02 = new DateTime();
                DateTime DataField03 = new DateTime();
                DateTime.TryParse(param.yesCustomField02.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField02);
                DateTime.TryParse(param.yesCustomField03.EmptyIfNull().ToString().Trim(), new CultureInfo("pt-BR"), DateTimeStyles.None, out DataField03);

                int start = param.iDisplayStart;
                int length = param.iDisplayLength <= 0 ? 20 : param.iDisplayLength;
                int totalRecords = 0;
                var pageRecords = new List<Db.ged_arquivos>();

                string sentencaSql = " select * from ged_arquivos ged where ged.ativo = 1 ";

                if (!filterDb)
                {
                    if ((param.yesCustomField01.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "-1") && (param.yesCustomField01.EmptyIfNull().ToString().Trim() != "0"))
                    {
                        sentencaSql += " and ged.id_arquivo_tipo =  " + param.yesCustomField01.EmptyIfNull().ToString().Trim();
                        int IdTipo = int.Parse(param.yesCustomField01.EmptyIfNull().ToString().Trim());
                        ged_arquivos_tipos record_ged_arquivos_tipos = allArquivosTipos.FirstOrDefault(t => t.id_arquivo_tipo == IdTipo);
                        if (record_ged_arquivos_tipos != null && (record_ged_arquivos_tipos.controle_anual == true || record_ged_arquivos_tipos.controle_mensal == true))
                        {
                            if ((param.yesCustomField02.EmptyIfNull().ToString().Trim().Length > 0) && (param.yesCustomField03.EmptyIfNull().ToString().Trim().Length > 0))
                            {
                                sentencaSql += " and ((ged.data_referencia between '" + DataField02.ToString("yyyy-MM-dd") + "' and '" + DataField03.ToString("yyyy-MM-dd") + " ') or (ged.controla_data_referencia = 0))";
                            }
                        }
                    }
                    else
                    {
                        sentencaSql += " and ged.id_arquivo_tipo =  -1 ";
                    }
                    string sqlData = sentencaSql + " order by ged.data_referencia desc";
                    totalRecords = LibDataTableSqlPaging.SqlCount(db, sqlData);
                    pageRecords = db.ged_arquivos.SqlQuery(LibDataTableSqlPaging.SqlPage(sqlData, start, length)).ToList();
                }

                var usuarioIds = pageRecords.Select(g => g.id_usuario_cadastro).Where(id => id > 0).Distinct().ToList();
                var usuariosDict = usuarioIds.Count > 0
                    ? db.g_usuarios.AsNoTracking()
                        .Where(u => usuarioIds.Contains(u.id_usuario))
                        .ToDictionary(u => u.id_usuario, u => u.login.EmptyIfNull().ToString())
                    : new Dictionary<int, string>();

                foreach (var ged in pageRecords)
                {
                    string login;
                    usuariosDict.TryGetValue(ged.id_usuario_cadastro.GetValueOrDefault(), out login);
                    String NomeUsuario = login ?? string.Empty;
                    String DataReferencia = ged.data_referencia.GetValueOrDefault().ToString("dd/MM/yy");
                    if (ged.controla_data_referencia == false) { DataReferencia = "Padrão"; }

                    list.Add(new[] {
                        "",
                        ged.id_arquivo.EmptyIfNull().ToString(),
                        ged.descricao.EmptyIfNull().ToString(),
                        ged.observacao.EmptyIfNull().ToString(),
                        ged.filetype.EmptyIfNull().ToString(),
                        ged.versao.EmptyIfNull().ToString(),
                        DataReferencia,
                        NomeUsuario,
                        ""
                    });
                }

                if (param.yesFilterField.EmptyIfNull().ToString().Trim() == "*")
                {
                    if (filterDb) { filterOnOff = "1"; }
                    else
                    {
                        string tipoArquivo = param.yesCustomField01.EmptyIfNull().ToString().Trim();
                        if (tipoArquivo.Length > 0 && tipoArquivo != "-1" && tipoArquivo != "0") { filterOnOff = "1"; }
                    }
                }

                return Json(new
                {
                    errorMessage = GdiMvcJsonResults.DataTableSuccessErrorMessage,
                    stackTrace = GdiMvcJsonResults.DataTableSuccessStackTrace,
                    yesFilterOnOff = filterOnOff,
                    sEcho = param.sEcho,
                    iTotalRecords = totalRecords,
                    iTotalDisplayRecords = totalRecords,
                    aaData = list
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return JsonDataTableException(e, param, filterOnOff);
            }
        }

        private JsonResult JsonDataTableException(Exception e, jQueryDataTableParamModel param, string yesFilterOnOff)
        {
            return Json(GdiMvcJsonResults.DataTableError(e, param, yesFilterOnOff), JsonRequestBehavior.AllowGet);
        }

    }
}
