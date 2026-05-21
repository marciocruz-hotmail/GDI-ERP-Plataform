using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using GdiPlataform.Domain;
using GdiPlataform.Models;

namespace GdiPlataform.Security
{
    /// <summary>Cache MemoryCache (TTL curto) dos fragmentos navbar — mensagens, tarefas, atividades (PERF-002).</summary>
    internal static class NavbarFragmentCache
    {
        private const string RequestLoadedKey = "GdiNavbarFragmentsLoaded";

        private static readonly MemoryCache Cache = MemoryCache.Default;
        private static readonly TimeSpan FragmentTtl = TimeSpan.FromSeconds(60);

        /// <summary>Indica se <see cref="ApplyToContextoModel"/> já correu nesta request HTTP (navbar → footer).</summary>
        public static bool IsLoadedThisRequest()
        {
            return HttpContext.Current?.Items[RequestLoadedKey] as bool? == true;
        }

        private sealed class Snapshot
        {
            public List<NavbarItemMessage> Messages { get; set; }
            public List<NavbarItemTask> Tasks { get; set; }
            public List<NavbarItemAlert> Alerts { get; set; }
            public List<NavbarItemAtividade> Atividades { get; set; }
            public int AlertsQtd { get; set; }
            public string AlertsMsg { get; set; }
            public bool AlertsShow { get; set; }
        }

        /// <summary>Preenche listas do <see cref="ContextoModel"/> e alertas em <see cref="CachePersister.userIdentity"/> (cache ou DB).</summary>
        public static void ApplyToContextoModel(ContextoModel contextoModel)
        {
            if (contextoModel == null) return;

            var key = BuildCacheKey();
            if (key == null)
            {
                ApplySnapshot(contextoModel, LoadSnapshotFromDatabase());
                MarkLoadedThisRequest();
                return;
            }

            var cached = Cache.Get(key) as Snapshot;
            if (cached != null)
            {
                ApplySnapshot(contextoModel, cached);
                MarkLoadedThisRequest();
                return;
            }

            var snapshot = LoadSnapshotFromDatabase();
            Cache.Set(key, snapshot, new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Add(FragmentTtl)
            });
            ApplySnapshot(contextoModel, snapshot);
            MarkLoadedThisRequest();
        }

        private static void MarkLoadedThisRequest()
        {
            if (HttpContext.Current != null)
                HttpContext.Current.Items[RequestLoadedKey] = true;
        }

        public static void InvalidateSession(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId)) return;
            try
            {
                Cache.Remove(PrefixKey(tokenId.Trim()));
            }
            catch
            {
                // não interromper logout
            }
        }

        private static string PrefixKey(string tokenId) => "navbar_fragments_" + tokenId;

        private static string BuildCacheKey()
        {
            try
            {
                if (HttpContext.Current?.Session == null) return null;
                var token = HttpContext.Current.Session["TokenId"]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(token)) return null;
                return PrefixKey(token);
            }
            catch
            {
                return null;
            }
        }

        private static Snapshot LoadSnapshotFromDatabase()
        {
            var contexto = new Contexto();
            var messages = contexto.getNavbarItemsMessage().ToList();
            var tasks = contexto.getNavbarItemsTask().ToList();
            var alerts = contexto.getNavbarItemsAlert().ToList();
            var atividades = contexto.getNavbarItemsAtividade().ToList();

            var identity = CachePersister.userIdentity;
            return new Snapshot
            {
                Messages = messages,
                Tasks = tasks,
                Alerts = alerts,
                Atividades = atividades,
                AlertsQtd = identity?.alerts_qtd ?? 0,
                AlertsMsg = identity?.alerts_msg ?? string.Empty,
                AlertsShow = identity?.alerts_show ?? false
            };
        }

        private static void ApplySnapshot(ContextoModel contextoModel, Snapshot snapshot)
        {
            if (snapshot == null) return;

            contextoModel.allNavbarItemMessage = CloneMessages(snapshot.Messages);
            contextoModel.allNavbarItemTask = CloneTasks(snapshot.Tasks);
            contextoModel.allNavbarItemAlert = snapshot.Alerts != null
                ? new List<NavbarItemAlert>(snapshot.Alerts)
                : new List<NavbarItemAlert>();
            contextoModel.allNavbarItemAtividade = CloneAtividades(snapshot.Atividades);

            if (CachePersister.userIdentity != null)
            {
                CachePersister.userIdentity.alerts_qtd = snapshot.AlertsQtd;
                CachePersister.userIdentity.alerts_msg = snapshot.AlertsMsg ?? string.Empty;
                CachePersister.userIdentity.alerts_show = snapshot.AlertsShow;
            }
        }

        private static List<NavbarItemMessage> CloneMessages(List<NavbarItemMessage> source)
        {
            if (source == null || source.Count == 0) return new List<NavbarItemMessage>();
            return source.Select(m => new NavbarItemMessage
            {
                Id = m.Id,
                nameHeader = m.nameHeader,
                message = m.message,
                reference = m.reference,
                href = m.href
            }).ToList();
        }

        private static List<NavbarItemTask> CloneTasks(List<NavbarItemTask> source)
        {
            if (source == null || source.Count == 0) return new List<NavbarItemTask>();
            return source.Select(t => new NavbarItemTask
            {
                Id = t.Id,
                Name = t.Name,
                Message = t.Message,
                progressBarType = t.progressBarType,
                valueMin = t.valueMin,
                valueMax = t.valueMax,
                ValueNow = t.ValueNow,
                href = t.href
            }).ToList();
        }

        private static List<NavbarItemAtividade> CloneAtividades(List<NavbarItemAtividade> source)
        {
            if (source == null || source.Count == 0) return new List<NavbarItemAtividade>();
            return source.Select(a => new NavbarItemAtividade
            {
                Id = a.Id,
                iconClass = a.iconClass,
                reference = a.reference,
                message = a.message,
                href = a.href
            }).ToList();
        }
    }
}
