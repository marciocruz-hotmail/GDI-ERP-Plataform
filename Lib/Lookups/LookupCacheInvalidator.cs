using GdiPlataform.Security;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Invalidação de lookups em MemoryCache alinhada a IsTableUpdate (Fase 3).</summary>
    public static class LookupCacheInvalidator
    {
        /// <summary>Remove entradas lookup:* da sessão atual associadas à tabela SQL (ex.: g_clientes).</summary>
        public static void InvalidateForTable(string tableName)
        {
            LibDB.ResetTableUpdateVerification(tableName);
            var token = LookupQueryServiceCache.ResolveSessionTokenPublic();
            LookupCacheRegistry.InvalidateTable(token, tableName);
        }

        internal static void OnTableUpdated(string tableName)
        {
            if (!string.IsNullOrEmpty(tableName))
                InvalidateForTable(tableName);
        }
    }
}
