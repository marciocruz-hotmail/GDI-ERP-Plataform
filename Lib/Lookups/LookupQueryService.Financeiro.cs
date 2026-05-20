using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Financeiro / pagrec / contas caixas gerencial (ex-Onda 6a).</summary>
    public sealed partial class LookupQueryService
    {
        public List<SelectListItem> GetComboPagRecCondicoesTodas(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcPagRecCondicoesTodas, "g_pagrec_condicoes", "LoadComboPagRecCondicoesTodas", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var c in db.g_pagrec_condicoes.AsNoTracking().Where(p => p.ativo).OrderBy(p => p.ordem)
                    .Select(c => new { c.id_pagrec_condicao, c.descricao }))
                    combo.Add(new SelectListItem { Value = c.id_pagrec_condicao.ToString(), Text = c.descricao });
                return combo;
            });

        public List<SelectListItem> GetComboPagRecCondicoesFaturaveis(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcPagRecCondicoesFaturaveis, "g_pagrec_condicoes", "LoadComboPagRecCondicoesFaturaveis", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var c in db.g_pagrec_condicoes.AsNoTracking().Where(p => p.ativo && p.id_pagrec_tipo != 5).OrderBy(p => p.ordem)
                    .Select(c => new { c.id_pagrec_condicao, c.descricao }))
                    combo.Add(new SelectListItem { Value = c.id_pagrec_condicao.ToString(), Text = c.descricao });
                return combo;
            });

        public List<SelectListItem> GetComboPagRecTiposFaturaveis(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcPagRecTiposFaturaveis, "g_pagrec_tipos", "LoadComboPagRecTiposFaturaveis", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = "-" } };
                foreach (var t in db.g_pagrec_tipos.AsNoTracking().Where(p => p.ativo && p.id_pagrec_tipo != 5).OrderBy(p => p.id_pagrec_tipo)
                    .Select(t => new { t.id_pagrec_tipo, t.descricao }))
                    combo.Add(new SelectListItem { Value = t.id_pagrec_tipo.ToString(), Text = t.descricao });
                return combo;
            });

        public List<SelectListItem> GetComboGcFinanceiroStatus(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcFinanceiroStatus, "gc_financeiro_status", "LoadComboGcFinanceiroStatus", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var s in db.gc_financeiro_status.AsNoTracking().OrderBy(p => p.id_financeiro_status)
                    .Select(s => new { s.id_financeiro_status, s.nome }))
                    combo.Add(new SelectListItem { Value = s.id_financeiro_status.ToString(), Text = s.nome });
                return combo;
            });

        public List<SelectListItem> GetComboFiltroFinanceiroStatus(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcFinanceiroFiltroStatus, null, null, db, () => new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ Todos ]" },
                new SelectListItem { Value = "3", Text = "[ Abertos ]" },
                new SelectListItem { Value = "1", Text = "[ Liquidados ]" }
            });

        public List<SelectListItem> GetComboGContasCaixasGerencial(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GContasCaixasGerencial, "g_contas_caixas", "LoadComboGContasCaixasGerencial", db, () => BuildComboContasCaixasGerencial(db));

        public List<SelectListItem> GetComboViewDebitoCredito(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GDebitoCredito, null, null, db, () => new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Débito" },
                new SelectListItem { Value = "2", Text = "Crédito" }
            });

        public List<SelectListItem> GetComboRowColors(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.ARowsColors, "a_tablesrows_colors", "LoadComboRowColors", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = "Default" } };
                foreach (var c in db.a_tablesrows_colors.AsNoTracking().Where(x => x.controller == "gc.FinanceiroLancamentos").OrderBy(x => x.nome)
                    .Select(c => new { c.id_tablerow_color, c.nome }))
                    combo.Add(new SelectListItem { Value = c.id_tablerow_color.ToString(), Text = c.nome });
                return combo;
            });

        public List<SelectListItem> GetComboGClassificacaoFinanceira(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GClassificacaoFinanceira, "g_classificacao_financeira", "LoadComboGClassificacaoFinanceira", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = "[ Informe a Classificação Financeira ]" } };
                foreach (var c in db.g_classificacao_financeira.AsNoTracking().Where(x => x.consolidador == false).OrderBy(x => x.descricao_resumida)
                    .Select(c => new { c.id_classificacao_financeira, c.descricao_resumida }))
                    combo.Add(new SelectListItem { Value = c.id_classificacao_financeira.ToString(), Text = c.descricao_resumida });
                return combo;
            });
        private static List<SelectListItem> BuildComboContasCaixasGerencial(GdiPlataformEntities db)
        {
            int idUsuario;
            int.TryParse(CachePersister.userIdentity.IdUsuario.ToString(), out idUsuario);
            var combo = new List<SelectListItem>();
            if (db.g_contas_caixas_acessos.AsNoTracking().Any(a => a.id_conta_caixa == 999 && a.id_usuario == idUsuario))
                combo.Add(new SelectListItem { Value = "999", Text = "[ TODAS ]" });

            var sql = "select c.* from g_contas_caixas c "
                + "left join g_contas_caixas_acessos a on (c.id_conta_caixa = a.id_conta_caixa) "
                + "where (c.ativo = 1) and (c.is_gerencial = 1) and (a.id_usuario = "
                + CachePersister.userIdentity.IdUsuario + ") order by c.ordem";
            var lista = db.g_contas_caixas.SqlQuery(sql).ToList();
            if (lista.Count > 0)
            {
                foreach (var item in lista)
                    combo.Add(new SelectListItem { Value = item.id_conta_caixa.ToString(), Text = item.nome });
            }
            else
            {
                combo.Add(new SelectListItem { Value = "0", Text = "CONTA CAIXA INTERNA" });
            }
            return combo;
        }
    }
}
