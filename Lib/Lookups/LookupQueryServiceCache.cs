using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Db;
using GdiPlataform.Domain;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Cache MemoryCache exclusivo para lookups (Fase 3 — sem duplicar em ContextoModel).</summary>
    internal static class LookupQueryServiceCache
    {
        private static readonly MemoryCache Cache = MemoryCache.Default;
        private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(15);

        internal static void EnsureContextoModel()
        {
            if (CachePersister.contextoModel == null)
            {
                CachePersister.contextoModel = new ContextoModel();
            }
        }

        internal static string ResolveSessionToken()
        {
            try
            {
                if (HttpContext.Current?.Session == null) return "_";
                var token = HttpContext.Current.Session["TokenId"];
                if (token == null) return "_";
                return token.ToString().Trim();
            }
            catch
            {
                return "_";
            }
        }

        internal static string ResolveSessionTokenPublic() => ResolveSessionToken();

        internal static List<SelectListItem> CloneCombo(IEnumerable<SelectListItem> source)
        {
            if (source == null) return new List<SelectListItem>();
            return source.Select(i => new SelectListItem
            {
                Value = i.Value,
                Text = i.Text,
                Selected = i.Selected,
                Disabled = i.Disabled
            }).ToList();
        }

        /// <summary>Combo global: apenas MemoryCache; invalidação via LibDB.IsTableUpdate + registo por tabela.</summary>
        internal static List<SelectListItem> GetOrLoadCombo(
            string lookupName,
            string tableName,
            string processName,
            GdiPlataformEntities db,
            Func<List<SelectListItem>> factory)
        {
            EnsureContextoModel();
            var token = ResolveSessionToken();
            var cacheKey = LookupCacheKeys.Combo(lookupName, token);

            var tableStale = !string.IsNullOrEmpty(tableName)
                && LibDB.IsTableUpdate(tableName, processName, db);

            if (!tableStale)
            {
                var cached = Cache.Get(cacheKey) as List<SelectListItem>;
                if (cached != null) return CloneCombo(cached);
            }

            var list = factory();
            Cache.Set(cacheKey, list, new CacheItemPolicy { SlidingExpiration = SlidingExpiration });
            LookupCacheRegistry.Register(token, tableName, cacheKey);
            return CloneCombo(list);
        }

        /// <summary>Combo paramétrico: chave composta; sem slot em ContextoModel.</summary>
        internal static List<SelectListItem> GetOrLoadParametricCombo(
            string lookupName,
            string tableName,
            object[] parameters,
            Func<List<SelectListItem>> factory)
        {
            EnsureContextoModel();
            var token = ResolveSessionToken();
            var cacheKey = LookupCacheKeys.Combo(lookupName, token, parameters);

            var cached = Cache.Get(cacheKey) as List<SelectListItem>;
            if (cached != null) return CloneCombo(cached);

            var list = factory();
            Cache.Set(cacheKey, list, new CacheItemPolicy { SlidingExpiration = SlidingExpiration });
            if (!string.IsNullOrEmpty(tableName))
                LookupCacheRegistry.Register(token, tableName, cacheKey);
            return CloneCombo(list);
        }

        internal static List<TItem> GetOrLoadDataset<TItem>(
            string lookupName,
            string tableName,
            string processName,
            GdiPlataformEntities db,
            Func<List<TItem>> factory) where TItem : class
        {
            EnsureContextoModel();
            var token = ResolveSessionToken();
            var cacheKey = LookupCacheKeys.Combo(lookupName, token) + ":ds";

            var tableStale = !string.IsNullOrEmpty(tableName)
                && LibDB.IsTableUpdate(tableName, processName, db);

            if (!tableStale)
            {
                var cached = Cache.Get(cacheKey) as List<TItem>;
                if (cached != null) return cached;
            }

            var list = factory();
            Cache.Set(cacheKey, list, new CacheItemPolicy { SlidingExpiration = SlidingExpiration });
            LookupCacheRegistry.Register(token, tableName, cacheKey);
            return list;
        }

        internal static List<T> GetOrLoadParametricDataset<T>(
            string lookupName,
            object[] parameters,
            Func<List<T>> factory) where T : class
        {
            EnsureContextoModel();
            var token = ResolveSessionToken();
            var cacheKey = LookupCacheKeys.Combo(lookupName, token, parameters) + ":ds";
            var cached = Cache.Get(cacheKey) as List<T>;
            if (cached != null) return cached;

            var list = factory();
            Cache.Set(cacheKey, list, new CacheItemPolicy { SlidingExpiration = SlidingExpiration });
            return list;
        }
    }
}
