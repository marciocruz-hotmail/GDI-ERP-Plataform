using System.Collections.Generic;
using System.Runtime.Caching;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Lookups.Tests
{
    /// <summary>1.9.3 — registo por tabela: invalidação isolada por LookupCacheRegistry.</summary>
    internal static class LookupCacheRegistryTableIsolationTests
    {
        public static void Run()
        {
            const string token = "registry-iso";
            var keyA = LookupCacheKeys.Combo(LookupCacheKeys.GcPagRecCondicoesTodas, token);
            var keyB = LookupCacheKeys.Combo(LookupCacheKeys.GcFinanceiroStatus, token);
            MemoryCache.Default.Set(keyA, new List<string> { "a" }, new CacheItemPolicy());
            MemoryCache.Default.Set(keyB, new List<string> { "b" }, new CacheItemPolicy());
            LookupCacheRegistry.Register(token, "g_pagrec_condicoes", keyA);
            LookupCacheRegistry.Register(token, "gc_financeiro_status", keyB);

            LookupCacheRegistry.InvalidateTable(token, "g_pagrec_condicoes");

            LookupTestAssert.IsTrue(MemoryCache.Default.Get(keyA) == null, "Chave da tabela invalidada.");
            LookupTestAssert.IsTrue(MemoryCache.Default.Get(keyB) != null, "Chave de outra tabela permanece.");

            MemoryCache.Default.Remove(keyB);
        }
    }
}
