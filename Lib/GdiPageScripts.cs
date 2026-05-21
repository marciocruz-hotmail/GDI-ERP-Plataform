using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace GdiPlataform.Lib
{
    /// <summary>
    /// Flags de bibliotecas opcionais no <c>_Layout</c> (G-PERF-20).
    /// <see cref="Core"/> é sempre obrigatório (jQuery, AdminLTE, SweetAlert2, start.js, sessão).
    /// Fase 3 (G-PERF-20d): filter + <c>_Layout</c> consomem <see cref="ViewBag.GdiPageScripts"/>; Tempus via flag <see cref="TempusDominus"/> nos partials opcionais do layout.
    /// </summary>
    [Flags]
    public enum GdiPageScriptsFlags
    {
        None = 0,
        /// <summary>jQuery, OverlayScrollbars, AdminLTE, SweetAlert2, spin, start.js, gdi-session-handler (sempre).</summary>
        Core = 1,
        DataTables = 2,
        Select2 = 4,
        TempusDominus = 8,
        Jstree = 16,
        BootstrapToggle = 32,

        /// <summary>Padrão áreas g/gc: Core + DataTables + Select2 (modais Ajax frequentes).</summary>
        DefaultGcG = Core | DataTables | Select2,

        /// <summary>Padrão efetivo g/gc/qa via <see cref="GdiPageScriptsDefaults.Resolve"/> (DefaultGcG + Toggle).</summary>
        DefaultGcGArea = DefaultGcG | BootstrapToggle,

        /// <summary>Todas as libs hoje no _Layout autenticado.</summary>
        FullAuthenticated = Core | DataTables | Select2 | TempusDominus | Jstree | BootstrapToggle,

        /// <summary>G-PERF-20e — Hub relatório: sem DT/S2; Tempus no layout para modais com datas.</summary>
        LayoutHubReport = Core | BootstrapToggle | TempusDominus,

        /// <summary>G-PERF-20e — Hub relatório com lookup Select2 no modal (ex. lançamentos financeiros).</summary>
        LayoutHubReportSelect2 = Core | BootstrapToggle | Select2 | TempusDominus,

        /// <summary>G-PERF-20e — Index hierárquico jstree (sem DataTables).</summary>
        LayoutHubJstree = Core | BootstrapToggle | Jstree,

        /// <summary>G-PERF-20e — Página simples (parâmetros, etc.).</summary>
        LayoutLite = Core | BootstrapToggle,

        /// <summary>G-PERF-20 Fase 4 — Portal cliente (crm): sem DataTables/Select2 no layout.</summary>
        LayoutPortalCliente = Core | BootstrapToggle,
    }

    /// <summary>
    /// Declara dependências de script/CSS da action (sobrescreve default da área/controller).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class GdiPageScriptsAttribute : Attribute
    {
        public GdiPageScriptsAttribute(GdiPageScriptsFlags flags)
        {
            Flags = flags;
        }

        public GdiPageScriptsFlags Flags { get; }
    }

    /// <summary>Resolve flags por rota MVC (inventário Fase 0 + overrides G-PERF-20e).</summary>
    public static class GdiPageScriptsDefaults
    {
        /// <summary>Chave HTML opcional para diagnóstico em homologação (Fase 3).</summary>
        public const string HtmlDiagnosticAttribute = "data-gdi-page-scripts";

        /// <summary>Controllers que usam jstree no Index (inventário 2026-05-20).</summary>
        private static readonly string[] JstreeControllers =
        {
            "CentrosCustos",
            "ClassificacaoFinanceira",
        };

        /// <summary>Fallback sem DT/S2 (preferir <see cref="GdiPageScriptsAttribute"/> nos hubs — G-PERF-20e).</summary>
        private static readonly string[] NoDataTablesControllers =
        {
            "CentrosCustos",
            "ClassificacaoFinanceira",
            "Parametros",
            "RelatoriosCadastrais",
            "RelatoriosComerciais",
            "RelatoriosFinanceiros",
            "RelatoriosRegulamentacao",
            "Treinamentos",
        };

        /// <summary>
        /// G-PERF-20 Fase 4 lote C — actions sem DataTables/Select2 na view (CreateEdit, formulários, POPs).
        /// Inventário: <c>Scripts/2026_05_20_gdi_inventory_layout_no_datatables.py</c>.
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> LayoutLiteActionsByController =
            BuildLayoutLiteActionsByController();

        /// <summary>Rotas com <c>jsDatepicker*</c> na view host (inventário 2026-05-21). Chave: area|controller, valor: actions (* = todas).</summary>
        private static readonly Dictionary<string, HashSet<string>> TempusActionsByAreaController =
            BuildTempusActionsByAreaController();

        public static GdiPageScriptsFlags Resolve(
            string area,
            string controller,
            string action = null,
            GdiPageScriptsFlags? attributeOverride = null)
        {
            if (attributeOverride.HasValue)
                return EnsureCore(attributeOverride.Value);

            var ctrl = controller ?? string.Empty;
            var act = action ?? string.Empty;
            if (TryResolveLayoutLiteAction(ctrl, act, out var liteFlags))
                return EnsureCore(liteFlags);

            var flags = GdiPageScriptsFlags.Core;

            if (string.IsNullOrEmpty(area))
                return flags | GdiPageScriptsFlags.BootstrapToggle;

            if (area == "g" || area == "gc" || area == "qa")
            {
                flags |= GdiPageScriptsFlags.BootstrapToggle;
                if (!ContainsIgnoreCase(NoDataTablesControllers, ctrl))
                    flags |= GdiPageScriptsFlags.DataTables | GdiPageScriptsFlags.Select2;
            }
            else if (area == "a")
            {
                flags |= GdiPageScriptsFlags.BootstrapToggle;
                if (string.Equals(ctrl, "Parametros", StringComparison.OrdinalIgnoreCase))
                    flags |= GdiPageScriptsFlags.DataTables;
            }
            else if (area == "crm")
                flags |= GdiPageScriptsFlags.BootstrapToggle;

            if (ContainsIgnoreCase(JstreeControllers, ctrl))
                flags |= GdiPageScriptsFlags.Jstree;

            if (NeedsTempusDominus(area, ctrl, act))
                flags |= GdiPageScriptsFlags.TempusDominus;

            return flags;
        }

        public static bool HasFlag(GdiPageScriptsFlags flags, GdiPageScriptsFlags test)
        {
            return (flags & test) == test;
        }

        private static GdiPageScriptsFlags EnsureCore(GdiPageScriptsFlags flags)
        {
            return (flags & GdiPageScriptsFlags.Core) == GdiPageScriptsFlags.Core
                ? flags
                : flags | GdiPageScriptsFlags.Core;
        }

        private static bool ContainsIgnoreCase(string[] items, string value)
        {
            if (items == null || string.IsNullOrEmpty(value)) return false;
            for (var i = 0; i < items.Length; i++)
            {
                if (string.Equals(items[i], value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool NeedsTempusDominus(string area, string controller, string action)
        {
            if (string.IsNullOrEmpty(controller)) return false;
            var key = (area ?? string.Empty) + "|" + controller;
            HashSet<string> actions;
            if (!TempusActionsByAreaController.TryGetValue(key, out actions))
                return false;
            return actions.Contains("*") || (!string.IsNullOrEmpty(action) && actions.Contains(action));
        }

        private static Dictionary<string, HashSet<string>> BuildTempusActionsByAreaController()
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            // Nomes de action MVC (não nome do ficheiro .cshtml). Inventário: Scripts/2026_05_20_gdi_cross_audit_view_libraries.py
            var pairs = new[]
            {
                Tuple.Create("g", "Clientes", "Create"), Tuple.Create("g", "Clientes", "Edit"), Tuple.Create("g", "Clientes", "Index"),
                Tuple.Create("g", "ContratosAviacao", "Create"), Tuple.Create("g", "ContratosAviacao", "Edit"),
                Tuple.Create("g", "Financeiro", "Index"),
                Tuple.Create("g", "Ged", "Index"),
                Tuple.Create("g", "Nfe", "Edit"), Tuple.Create("g", "Nfe", "Index"),
                Tuple.Create("gc", "ComexImportacoes", "Create"), Tuple.Create("gc", "ComexImportacoes", "Edit"),
                Tuple.Create("gc", "ComexFinanceiro", "Index"),
                Tuple.Create("gc", "EstoqueControle", "Create"), Tuple.Create("gc", "EstoqueControle", "Edit"),
                Tuple.Create("gc", "FinanceiroLancamentos", "Index"),
                Tuple.Create("gc", "Fretes", "Index"),
                Tuple.Create("gc", "Movimentos", "CreateCotacao"), Tuple.Create("gc", "Movimentos", "CreatePedido"),
                Tuple.Create("gc", "Movimentos", "CreateOS"), Tuple.Create("gc", "Movimentos", "EditPedido"),
                Tuple.Create("gc", "Movimentos", "IndexPedido"), Tuple.Create("gc", "Movimentos", "PainelPedidos"),
                Tuple.Create("qa", "GedSGQ", "IndexAtasReunioes"),
                Tuple.Create("qa", "GedSGQ", "IndexComunicados"),
                Tuple.Create("qa", "GedSGQ", "IndexDocsSGQ"),
            };
            for (var i = 0; i < pairs.Length; i++)
            {
                var routeKey = pairs[i].Item1 + "|" + pairs[i].Item2;
                HashSet<string> set;
                if (!map.TryGetValue(routeKey, out set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    map[routeKey] = set;
                }
                set.Add(pairs[i].Item3);
            }
            return map;
        }

        private static bool TryResolveLayoutLiteAction(string controller, string action, out GdiPageScriptsFlags flags)
        {
            flags = GdiPageScriptsFlags.LayoutLite;
            if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action))
                return false;
            HashSet<string> actions;
            if (!LayoutLiteActionsByController.TryGetValue(controller, out actions))
                return false;
            return actions.Contains(action);
        }

        private static Dictionary<string, HashSet<string>> BuildLayoutLiteActionsByController()
        {
            var map = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var pairs = new[]
            {
                Tuple.Create("GedSGQ", "IndexPops"),
                Tuple.Create("Treinamentos", "IndexTreinamentoAviacao001"),
                Tuple.Create("CentrosCustos", "Create"), Tuple.Create("CentrosCustos", "Edit"),
                Tuple.Create("ClassificacaoFinanceira", "Create"), Tuple.Create("ClassificacaoFinanceira", "Edit"),
                Tuple.Create("Cidades", "Create"), Tuple.Create("Cidades", "Edit"),
                Tuple.Create("ContasCaixas", "Create"), Tuple.Create("ContasCaixas", "Edit"),
                Tuple.Create("Filiais", "Create"), Tuple.Create("Filiais", "Edit"),
                Tuple.Create("PagRecCondicoes", "Create"), Tuple.Create("PagRecCondicoes", "Edit"),
                Tuple.Create("PagRecTipos", "Create"), Tuple.Create("PagRecTipos", "Edit"),
                Tuple.Create("Perfis", "Create"), Tuple.Create("Perfis", "Edit"),
                Tuple.Create("ProdutosNcm", "Create"), Tuple.Create("ProdutosNcm", "Edit"),
                Tuple.Create("UF", "Create"), Tuple.Create("UF", "Edit"),
                Tuple.Create("Usuarios", "Create"), Tuple.Create("Usuarios", "Edit"),
                Tuple.Create("Cfop", "Create"), Tuple.Create("Cfop", "Edit"),
                Tuple.Create("FinanceiroParametroDifal", "Create"), Tuple.Create("FinanceiroParametroDifal", "Edit"),
                Tuple.Create("ComexProdutos", "FormProcessarProdutosPreNovos"),
                Tuple.Create("ComexProdutos", "FormProcessarProdutosPreAtualizar"),
                Tuple.Create("MovimentosEntradas", "FormProcessarNFCompraNacional"),
                Tuple.Create("MovimentosEntradas", "FormProcessarNFDevolucao"),
                Tuple.Create("MovimentosEntradas", "FormProcessarNFImportacao"),
            };
            for (var i = 0; i < pairs.Length; i++)
            {
                var ctrl = pairs[i].Item1;
                var act = pairs[i].Item2;
                HashSet<string> set;
                if (!map.TryGetValue(ctrl, out set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    map[ctrl] = set;
                }
                set.Add(act);
            }
            return map;
        }

    }

    /// <summary>
    /// Preenche <c>ViewBag.GdiPageScripts</c> antes da view (G-PERF-20d).
    /// </summary>
    public sealed class GdiPageScriptsActionFilter : IActionFilter
    {
        public const string ViewBagKey = "GdiPageScripts";

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext?.ActionDescriptor == null) return;

            var route = filterContext.RouteData;
            var area = route.DataTokens["area"] as string ?? string.Empty;
            var controller = route.Values["controller"] as string ?? string.Empty;
            var action = route.Values["action"] as string ?? string.Empty;

            GdiPageScriptsFlags? attrFlags = null;
            var attr = filterContext.ActionDescriptor.GetCustomAttributes(typeof(GdiPageScriptsAttribute), true);
            if (attr != null && attr.Length > 0 && attr[0] is GdiPageScriptsAttribute gpsa)
                attrFlags = gpsa.Flags;
            else
            {
                var ctrlAttrs = filterContext.ActionDescriptor.ControllerDescriptor
                    .GetCustomAttributes(typeof(GdiPageScriptsAttribute), true);
                if (ctrlAttrs != null && ctrlAttrs.Length > 0 && ctrlAttrs[0] is GdiPageScriptsAttribute gpsc)
                    attrFlags = gpsc.Flags;
            }

            var flags = GdiPageScriptsDefaults.Resolve(area, controller, action, attrFlags);
            if (filterContext.Controller != null)
            {
                // ViewData: indexador OK; ViewBag expõe as mesmas chaves como propriedade dinâmica (ViewBag.GdiPageScripts).
                // Não usar ViewBag[chave] — DynamicObject não suporta indexação [].
                filterContext.Controller.ViewData[ViewBagKey] = flags;
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }
    }
}
