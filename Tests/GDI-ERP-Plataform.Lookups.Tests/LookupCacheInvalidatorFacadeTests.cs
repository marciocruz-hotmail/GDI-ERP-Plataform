using System.Collections.Generic;
using System.Runtime.Caching;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Lookups.Tests
{
    /// <summary>1.9.3 — LookupCacheInvalidator.InvalidateForTable remove chaves registadas por tabela.</summary>
    internal static class LookupCacheInvalidatorFacadeTests
    {
        public static void Run()
        {
            // Sem HttpContext, LookupCacheInvalidator usa ResolveSessionTokenPublic() => "_"
            const string token = "_";
            const string tableClientes = "g_clientes";
            const string tableOutra = "g_vendedores";

            var keyClientes = LookupCacheKeys.Combo(LookupCacheKeys.GClientesFornecedores, token);
            var keyVendedores = LookupCacheKeys.Combo(LookupCacheKeys.GVendedores, token);
            var listClientes = new List<string> { "c1" };
            var listVendedores = new List<string> { "v1" };

            MemoryCache.Default.Set(keyClientes, listClientes, new CacheItemPolicy());
            MemoryCache.Default.Set(keyVendedores, listVendedores, new CacheItemPolicy());
            LookupCacheRegistry.Register(token, tableClientes, keyClientes);
            LookupCacheRegistry.Register(token, tableOutra, keyVendedores);

            LookupCacheInvalidator.InvalidateForTable(tableClientes);

            LookupTestAssert.IsTrue(MemoryCache.Default.Get(keyClientes) == null, "InvalidateForTable(g_clientes) deve remover combo clientes.");
            LookupTestAssert.SameReference(listVendedores, MemoryCache.Default.Get(keyVendedores) as List<string>,
                "Combo de outra tabela não deve ser invalidada.");

            MemoryCache.Default.Remove(keyVendedores);
        }
    }
}
