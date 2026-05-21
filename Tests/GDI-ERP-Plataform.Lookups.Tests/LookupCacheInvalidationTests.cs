using System.Collections.Generic;
using System.Runtime.Caching;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Lookups.Tests
{
    internal static class LookupCacheInvalidationTests
    {
        public static void Run()
        {
            const string token = "inv-test";
            const string table = "g_clientes";
            var key1 = LookupCacheKeys.Combo(LookupCacheKeys.GcTransportadora, token);
            var key2 = LookupCacheKeys.Combo(LookupCacheKeys.GVendedores, token);
            var list1 = new List<string> { "a" };
            var list2 = new List<string> { "b" };

            MemoryCache.Default.Set(key1, list1, new CacheItemPolicy());
            MemoryCache.Default.Set(key2, list2, new CacheItemPolicy());
            LookupCacheRegistry.Register(token, table, key1);
            LookupCacheRegistry.Register(token, "g_vendedores", key2);

            LookupCacheRegistry.InvalidateTable(token, table);

            LookupTestAssert.IsTrue(MemoryCache.Default.Get(key1) == null, "key1 (g_clientes) deveria ser removida.");
            LookupTestAssert.SameReference(list2, MemoryCache.Default.Get(key2) as List<string>,
                "Combo de outra tabela não deve ser invalidada por g_clientes.");
            MemoryCache.Default.Remove(key2);
        }
    }
}
