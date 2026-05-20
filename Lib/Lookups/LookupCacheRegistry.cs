using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Rastreia chaves MemoryCache por sessão+tabela para invalidação (Fase 3).</summary>
    internal static class LookupCacheRegistry
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>> KeysBySessionTable =
            new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private static string SessionTableKey(string sessionToken, string tableName)
        {
            return (sessionToken ?? "_") + "|" + (tableName ?? "_").Trim().ToLowerInvariant();
        }

        internal static void Register(string sessionToken, string tableName, string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey) || string.IsNullOrEmpty(tableName)) return;
            var bucket = KeysBySessionTable.GetOrAdd(SessionTableKey(sessionToken, tableName), _ => new HashSet<string>(StringComparer.Ordinal));
            lock (bucket)
            {
                bucket.Add(cacheKey);
            }
        }

        internal static void InvalidateTable(string sessionToken, string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return;
            var stKey = SessionTableKey(sessionToken, tableName);
            if (!KeysBySessionTable.TryRemove(stKey, out var keys)) return;
            lock (keys)
            {
                foreach (var cacheKey in keys)
                {
                    MemoryCache.Default.Remove(cacheKey);
                }
            }
        }

        /// <summary>Invalida todas as tabelas registadas da sessão (ex.: logout).</summary>
        internal static void InvalidateSession(string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken)) return;
            var prefix = sessionToken + "|";
            var toRemove = new List<string>();
            foreach (var kv in KeysBySessionTable)
            {
                if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    toRemove.Add(kv.Key);
            }
            foreach (var stKey in toRemove)
            {
                if (!KeysBySessionTable.TryRemove(stKey, out var keys)) continue;
                lock (keys)
                {
                    foreach (var cacheKey in keys)
                        MemoryCache.Default.Remove(cacheKey);
                }
            }
        }
    }
}
