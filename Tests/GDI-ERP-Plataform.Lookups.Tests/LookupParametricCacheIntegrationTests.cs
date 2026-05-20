using System.Collections.Generic;
using System.Runtime.Caching;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Lookups.Tests
{
    /// <summary>Integracao: MemoryCache isola entradas por parametro (combo parametrico).</summary>
    internal static class LookupParametricCacheIntegrationTests
    {
        public static void Run()
        {
            var cache = MemoryCache.Default;
            const string token = "test-token-parametric";

            var keyCliente1 = LookupCacheKeys.Combo(LookupCacheKeys.GcClientesContatos, token, 100);
            var keyCliente2 = LookupCacheKeys.Combo(LookupCacheKeys.GcClientesContatos, token, 200);

            var list1 = new List<string> { "contato-a" };
            var list2 = new List<string> { "contato-b" };

            cache.Set(keyCliente1, list1, new CacheItemPolicy());
            cache.Set(keyCliente2, list2, new CacheItemPolicy());

            var hit1 = cache.Get(keyCliente1) as List<string>;
            var hit2 = cache.Get(keyCliente2) as List<string>;

            LookupTestAssert.SameReference(list1, hit1, "Cache hit cliente 100.");
            LookupTestAssert.SameReference(list2, hit2, "Cache hit cliente 200.");
            LookupTestAssert.AreNotEqual(hit1[0], hit2[0], "Listas por IdCliente devem divergir.");

            cache.Remove(keyCliente1);
            cache.Remove(keyCliente2);
        }
    }
}
