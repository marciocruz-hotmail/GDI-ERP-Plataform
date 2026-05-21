using System;
using System.Web.Mvc;
namespace GdiPlataform.Lib
{
    /// <summary>
    /// Leitura segura de <see cref="GdiPageScriptsActionFilter.ViewBagKey"/> nas views / _Layout (G-PERF-20 Fase 3).
    /// </summary>
    public static class GdiPageScriptsView
    {
        public static GdiPageScriptsFlags GetFlags(ViewContext viewContext)
        {
            if (viewContext == null)
                return GdiPageScriptsFlags.Core | GdiPageScriptsFlags.BootstrapToggle;

            var viewData = viewContext.ViewData;
            if (viewData != null && viewData.ContainsKey(GdiPageScriptsActionFilter.ViewBagKey))
            {
                var bag = viewData[GdiPageScriptsActionFilter.ViewBagKey];
                if (bag is GdiPageScriptsFlags flags)
                    return flags;
            }

            var route = viewContext.RouteData;
            if (route == null)
                return GdiPageScriptsFlags.Core | GdiPageScriptsFlags.BootstrapToggle;

            var area = route.DataTokens["area"] as string ?? string.Empty;
            var controller = Convert.ToString(route.Values["controller"]) ?? string.Empty;
            var action = Convert.ToString(route.Values["action"]) ?? string.Empty;
            return GdiPageScriptsDefaults.Resolve(area, controller, action);
        }

        public static void EnsureViewBag(ViewContext viewContext)
        {
            if (viewContext?.ViewData == null) return;
            if (viewContext.ViewData[GdiPageScriptsActionFilter.ViewBagKey] is GdiPageScriptsFlags)
                return;
            var flags = GetFlags(viewContext);
            viewContext.ViewData[GdiPageScriptsActionFilter.ViewBagKey] = flags;
        }
    }
}
