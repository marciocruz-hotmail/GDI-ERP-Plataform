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
    /// <summary>Cadastros g + atendimentos + produtos fiscais (ex-Onda 6a).</summary>
    public sealed partial class LookupQueryService
    {
        public List<SelectListItem> GetComboGProdutosTipos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GProdutosTipos, "g_produtos_tipos", "LoadComboGProdutosTipos", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var t in db.g_produtos_tipos.AsNoTracking().Where(p => p.id_produto_tipo > 0).OrderBy(p => p.nome)
                    .Select(t => new { t.id_produto_tipo, t.nome }))
                    combo.Add(new SelectListItem { Value = t.id_produto_tipo.ToString(), Text = t.nome.Trim() });
                return combo;
            });

        public List<SelectListItem> GetComboGProdutosNcm(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GProdutosNcm, "g_produtos_ncm", "LoadComboGProdutosNCM", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var n in db.g_produtos_ncm.AsNoTracking().OrderBy(p => p.codigo_ncm)
                    .Select(n => new { n.id_produto_ncm, n.codigo_ncm }))
                    combo.Add(new SelectListItem { Value = n.id_produto_ncm.ToString(), Text = n.codigo_ncm.Trim() });
                return combo;
            });

        public List<SelectListItem> GetComboGcIcmsUfIsento(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcIcmsUfIsento, null, null, db, () => new List<SelectListItem>
            {
                new SelectListItem { Value = "false", Text = "NÃO" },
                new SelectListItem { Value = "true", Text = "SIM" }
            });

        public List<SelectListItem> GetComboGcIcmsCstSimples(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcIcmsCstSimples, "gc_icms_cst", "LoadComboGcIcmsCstSimples", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var r in db.gc_icms_cst.AsNoTracking().Where(p => p.ativo).OrderBy(p => p.codigo_cst)
                    .Select(r => new { r.id_icms_cst, r.codigo_cst, r.descricao }))
                    combo.Add(new SelectListItem { Value = r.id_icms_cst.ToString(), Text = r.codigo_cst + " - " + r.descricao });
                return combo;
            });

        public List<SelectListItem> GetComboGUnidadeMedida(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GUnidadeMedida, "g_unidade_medida", "LoadComboGUnidadeMedida", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var u in db.g_unidade_medida.AsNoTracking().Where(p => p.ativo).OrderBy(p => p.descricao)
                    .Select(u => new { u.id_unidade_medida, u.descricao }))
                    combo.Add(new SelectListItem { Value = u.id_unidade_medida.ToString(), Text = u.descricao });
                return combo;
            });

        public List<SelectListItem> GetComboGContratosTipos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GContratosTipos, "g_contratos_aviacao_tipos", "LoadComboGContratosTipos", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var t in db.g_contratos_aviacao_tipos.AsNoTracking().Where(p => p.ativo).OrderByDescending(p => p.id_contrato_tipo)
                    .Select(t => new { t.id_contrato_tipo, t.descricao }))
                    combo.Add(new SelectListItem { Value = t.id_contrato_tipo.ToString(), Text = t.descricao });
                return combo;
            });

        public List<SelectListItem> GetComboGcProdutosFamilia(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcProdutosFamilia, "g_produtos_familia", "LoadComboGcProdutosFamilia", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "-1", Text = "" } };
                foreach (var f in db.g_produtos_familia.AsNoTracking().Where(p => p.ativo)
                    .Select(f => new { f.id_produto_familia, f.descricao }))
                    combo.Add(new SelectListItem { Value = f.id_produto_familia.ToString(), Text = f.descricao });
                return combo;
            });

        public List<SelectListItem> GetComboGcProdutosStatus(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcProdutosStatus, "g_produtos_status", "LoadComboGcProdutosStatus", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "-1", Text = "" } };
                foreach (var s in db.g_produtos_status.AsNoTracking().Where(p => p.ativo)
                    .Select(s => new { s.id_produto_status, s.descricao }))
                    combo.Add(new SelectListItem { Value = s.id_produto_status.ToString(), Text = s.descricao });
                return combo;
            });

        public List<SelectListItem> GetComboGUsuariosAtendimentoResponsavel(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GUsuariosAtendimentoResponsavel, "g_usuarios", "LoadComboGUsuariosAtendimentoResponsavel", db, () => BuildComboUsuariosAtendimento(db, "[ Operador ]"));

        public List<SelectListItem> GetComboGUsuariosAtendimentoSolicitante(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GUsuariosAtendimentoSolicitante, "g_usuarios", "LoadComboGUsuariosAtendimentoSolicitante", db, () => BuildComboUsuariosAtendimento(db, "[ Solicitante ]"));

        public List<SelectListItem> GetComboGDepartamentos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GDepartamentos, "g_departamentos", "LoadComboGDepartamentos", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = "[ Departamento ]" } };
                var isManager = CachePersister.userIdentity.Roles.Contains("g_Atendimentos_*")
                    || CachePersister.userIdentity.Roles.Contains("g_Atendimentos_Actionmanager");
                var query = db.g_departamentos.AsNoTracking().Where(d => d.ativo);
                if (!isManager) query = query.Where(d => d.id_departamento != 8);
                foreach (var d in query.OrderBy(x => x.nome).Select(d => new { d.id_departamento, d.nome }))
                    combo.Add(new SelectListItem { Value = d.id_departamento.ToString(), Text = d.nome });
                return combo;
            });

        public List<SelectListItem> GetComboGAtendimentosStatus(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GAtendimentosStatus, "g_atendimentos_status", "LoadComboGAtendimentosStatus", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = "[ Pendentes ]" } };
                foreach (var s in db.g_atendimentos_status.AsNoTracking().Where(d => d.ativo).OrderBy(d => d.nome)
                    .Select(s => new { s.id_status, s.nome }))
                    combo.Add(new SelectListItem { Value = s.id_status.ToString(), Text = s.nome });
                return combo;
            });

        public List<SelectListItem> GetComboGAtendimentosCategorias(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GAtendimentosCategorias, "g_atendimentos_categorias", "LoadComboGAtendimentosCategorias", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = "[ Categorias ]" } };
                foreach (var c in db.g_atendimentos_categorias.AsNoTracking().Where(d => d.ativo).OrderBy(d => d.nome)
                    .Select(c => new { c.id_atendimento_categoria, c.nome }))
                    combo.Add(new SelectListItem { Value = c.id_atendimento_categoria.ToString(), Text = c.nome });
                return combo;
            });
        private static List<SelectListItem> BuildComboUsuariosAtendimento(GdiPlataformEntities db, string placeholder)
        {
            var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = placeholder } };
            foreach (var u in db.g_usuarios.AsNoTracking().Where(x => x.ativo && x.id_departamento > 0).OrderBy(x => x.nome)
                .Select(u => new { u.id_usuario, u.nome }))
                combo.Add(new SelectListItem { Value = u.id_usuario.ToString(), Text = u.nome });
            return combo;
        }
    }
}
