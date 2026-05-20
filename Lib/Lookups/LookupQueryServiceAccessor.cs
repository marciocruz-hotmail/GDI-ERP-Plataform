using System;
using System.Web.Mvc;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Resolve ILookupQueryService via MVC DependencyResolver ou instância padrão.</summary>
    public static class LookupQueryServiceAccessor
    {
        private static readonly Lazy<ILookupQueryService> DefaultInstance =
            new Lazy<ILookupQueryService>(() => new LookupQueryService());

        public static ILookupQueryService Current
        {
            get
            {
                try
                {
                    var resolver = DependencyResolver.Current;
                    if (resolver != null)
                    {
                        var svc = resolver.GetService(typeof(ILookupQueryService)) as ILookupQueryService;
                        if (svc != null) return svc;
                    }
                }
                catch
                {
                    // fora de request MVC (scripts, testes)
                }
                return DefaultInstance.Value;
            }
        }
    }
}
