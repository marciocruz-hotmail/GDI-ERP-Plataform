using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Lookups.Tests
{
    internal static class LookupCacheKeysTests
    {
        public static void Run()
        {
            var k1 = LookupCacheKeys.Combo(LookupCacheKeys.GcClientesContatos, "token-a", 1);
            var k2 = LookupCacheKeys.Combo(LookupCacheKeys.GcClientesContatos, "token-a", 2);
            LookupTestAssert.AreNotEqual(k1, k2, "Chaves devem diferir por IdCliente.");

            var k3 = LookupCacheKeys.Combo(LookupCacheKeys.GcClientesContatos, "token-b", 1);
            LookupTestAssert.AreNotEqual(k1, k3, "Chaves devem diferir por TokenId.");

            var global1 = LookupCacheKeys.Combo(LookupCacheKeys.GcTransportadora, "tok");
            var global2 = LookupCacheKeys.Combo(LookupCacheKeys.GcCfop, "tok");
            LookupTestAssert.AreNotEqual(global1, global2, "Combos globais devem ter chaves distintas.");

            LookupTestAssert.IsTrue(global1.StartsWith(LookupCacheKeys.Prefix), "Prefixo lookup:");
        }
    }
}
