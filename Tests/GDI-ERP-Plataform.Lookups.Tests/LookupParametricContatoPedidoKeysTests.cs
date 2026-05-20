using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Lookups.Tests
{
    /// <summary>1.9.3 — chaves de cache para combo paramétrico cliente/contato (pedido).</summary>
    internal static class LookupParametricContatoPedidoKeysTests
    {
        public static void Run()
        {
            const string token = "param-contato";

            var keyContatos = LookupCacheKeys.Combo(LookupCacheKeys.GcClientesContatos, token, 100);
            var keyContatosOutroCliente = LookupCacheKeys.Combo(LookupCacheKeys.GcClientesContatos, token, 200);
            var keyContatoPedido = LookupCacheKeys.Combo(LookupCacheKeys.GcClientesContatosPedido, token, 100);

            LookupTestAssert.AreNotEqual(keyContatos, keyContatosOutroCliente, "GcClientesContatos deve variar por idCliente.");
            LookupTestAssert.AreNotEqual(keyContatos, keyContatoPedido, "Contatos genérico vs pedido devem ter chaves distintas.");
            LookupTestAssert.IsTrue(keyContatoPedido.Contains("100"), "Chave paramétrica deve incluir idCliente.");
            LookupTestAssert.IsTrue(keyContatoPedido.StartsWith(LookupCacheKeys.Prefix), "Prefixo lookup:");
        }
    }
}
