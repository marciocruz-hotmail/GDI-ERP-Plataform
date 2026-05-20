using GdiPlataform.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using GdiPlataform.Domain;
using System.Runtime.Caching;
using GdiPlataform.Lib;
using GdiPlataform.Lib.Lookups;

namespace GdiPlataform.Security
{
    public static class CachePersister
    {
        private static MemoryCache _cache = MemoryCache.Default;
        public static UserIdentity userIdentity
        {
            get
            {
                try
                {
                    String key = "*";
                    key = "userIdentity_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    if (_cache.Get(key) == null) return null;
                    var sessionUserIdentity = (UserIdentity)_cache.Get(key);
                    if (sessionUserIdentity != null) return sessionUserIdentity as UserIdentity;
                    return null;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao ler {0}: {1}", "userIdentity", ex.Message);
                    return null;
                }
            }
            set
            {
                try
                {
                    String key = "userIdentity_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                    cacheItemPolicy.SlidingExpiration = TimeSpan.FromMinutes(15);
                    _cache.Set(key, value, cacheItemPolicy);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao gravar {0}: {1}", "userIdentity", ex.Message);
                    throw;
                }
            }
        }

        public static ContextoModel contextoModel
        {
            get
            {
                try
                {
                    String key = "contextoModel_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    if (_cache.Get(key) == null)
                        return null;
                    var cacheContextoModel = (ContextoModel)_cache.Get(key);
                    if (cacheContextoModel != null)
                        return cacheContextoModel as ContextoModel;
                    return null;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao ler {0}: {1}", "contextoModel", ex.Message);
                    return null;
                }
            }
            set
            {
                try
                {
                    String key = "contextoModel_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                    cacheItemPolicy.SlidingExpiration = TimeSpan.FromMinutes(15);
                    _cache.Set(key, value, cacheItemPolicy);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao gravar {0}: {1}", "contextoModel", ex.Message);
                    throw;
                }
            }
        }

        public static String dataBase
        {
            get
            {
                try
                {
                    if ((HttpContext.Current.Session["TokenId"] == null) || (HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim().Length <= 0))
                    {
                        return null;
                    }
                    else
                    {
                        String key = "dataBase_" + HttpContext.Current.Session["TokenId"].ToString();
                        if (_cache.Get(key) == null)
                        {
                            Trace.TraceWarning("[CachePersister] dataBase em cache ausente para TokenId (chave {0}).", key);
                            return null;
                        }
                        var sessionUserDatabase = (String)_cache.Get(key);
                        if (sessionUserDatabase != null) return sessionUserDatabase.ToString();
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao ler {0}: {1}", "dataBase", ex.Message);
                    return null;
                }
            }
            set
            {
                try
                {
                    String key = "dataBase_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                    cacheItemPolicy.SlidingExpiration = TimeSpan.FromMinutes(15);
                    _cache.Set(key, value, cacheItemPolicy);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao gravar {0}: {1}", "dataBase", ex.Message);
                    throw;
                }
            }
        }

        public static List<Db.a_sistemas> allSistemas
        {
            get
            {
                try
                {
                    String key = "allSistemas_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    if (_cache.Get(key) == null) return null;
                    var sessionAllSistemas = (List<Db.a_sistemas>)_cache.Get(key);
                    if (sessionAllSistemas != null) return sessionAllSistemas;
                    return null;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao ler {0}: {1}", "allSistemas", ex.Message);
                    return null;
                }
            }
            set
            {
                try
                {
                    String key = "allSistemas_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                    cacheItemPolicy.SlidingExpiration = TimeSpan.FromMinutes(15);
                    _cache.Set(key, value, cacheItemPolicy);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao gravar {0}: {1}", "allSistemas", ex.Message);
                    throw;
                }
            }
        }

        public static List<Db.a_sistemas_modulos> allSistemasModulos
        {
            get
            {
                try
                {

                    String key = "allSistemasModulos_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    if (_cache.Get(key) == null) return null;
                    var sessionAllSistemasModulos = (List<Db.a_sistemas_modulos>)_cache.Get(key);
                    if (sessionAllSistemasModulos != null) return sessionAllSistemasModulos;
                    return null;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao ler {0}: {1}", "allSistemasModulos", ex.Message);
                    return null;
                }
            }
            set
            {
                try
                {
                    String key = "allSistemasModulos_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                    cacheItemPolicy.SlidingExpiration = TimeSpan.FromMinutes(15);
                    _cache.Set(key, value, cacheItemPolicy);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao gravar {0}: {1}", "allSistemasModulos", ex.Message);
                    throw;
                }
            }
        }

        public static List<Db.a_yesprodutos> allYesProdutos
        {
            get
            {
                try
                {

                    String key = "allYesProdutos_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    if (_cache.Get(key) == null) return null;
                    var sessionAllYesProdutos = (List<Db.a_yesprodutos>)_cache.Get(key);
                    if (sessionAllYesProdutos != null) return sessionAllYesProdutos;
                    return null;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao ler {0}: {1}", "allYesProdutos", ex.Message);
                    return null;
                }
            }
            set
            {
                try
                {
                    String key = "allYesProdutos_" + HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim();
                    CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                    cacheItemPolicy.SlidingExpiration = TimeSpan.FromMinutes(15);
                    _cache.Set(key, value, cacheItemPolicy);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[CachePersister] Erro ao gravar {0}: {1}", "allYesProdutos", ex.Message);
                    throw;
                }
            }
        }


        public static void logout()
        {
            String tokenId = String.Empty;
            if ((HttpContext.Current.Session["TokenId"] != null) && (HttpContext.Current.Session["TokenId"].EmptyIfNull().ToString().Trim().Length > 0))
            {
                try
                {
                    tokenId = HttpContext.Current.Session["TokenId"].ToString();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[CachePersister] logout: erro ao ler TokenId: {0}", ex.Message);
                }
                try
                {
                    LookupCacheRegistry.InvalidateSession(tokenId);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[CachePersister] logout: LookupCacheRegistry.InvalidateSession {0}: {1}", tokenId, ex.Message);
                }
                try
                {
                    _cache.Remove("contextoModel_" + tokenId);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[CachePersister] logout: Remove contextoModel_{0}: {1}", tokenId, ex.Message);
                }
                try
                {
                    _cache.Remove("userIdentity_" + tokenId);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[CachePersister] logout: Remove userIdentity_{0}: {1}", tokenId, ex.Message);
                }
                try
                {
                    _cache.Remove("dataBase_" + tokenId);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[CachePersister] logout: Remove dataBase_{0}: {1}", tokenId, ex.Message);
                }
                try
                {
                    _cache.Remove("allSistemas_" + tokenId);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[CachePersister] logout: Remove allSistemas_{0}: {1}", tokenId, ex.Message);
                }
                try
                {
                    _cache.Remove("allSistemasModulos_" + tokenId);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[CachePersister] logout: Remove allSistemasModulos_{0}: {1}", tokenId, ex.Message);
                }
                try
                {
                    _cache.Remove("allYesProdutos_" + tokenId);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[CachePersister] logout: Remove allYesProdutos_{0}: {1}", tokenId, ex.Message);
                }
            }
            HttpContext.Current.Session.RemoveAll();
        }
    }
}
