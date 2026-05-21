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
    /// <summary>Comercial / movimentos / COMEX / CFOP / contatos pedido (ex-Onda 6a).</summary>
    public sealed partial class LookupQueryService
    {
        public List<SelectListItem> GetComboGcTiposMovimentosVendas(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcTiposMovimentosVendas, null, null, db, BuildComboGcTiposMovimentosVendas);

        public List<SelectListItem> GetComboGcTiposMovimentosCreateEdit(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcTiposMovimentosCreateEdit, null, null, db, BuildComboGcTiposMovimentosCreateEdit);

        public List<SelectListItem> GetComboGcStatusMovimentos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcStatusMovimentos, null, null, db, BuildComboGcStatusMovimentos);

        public List<SelectListItem> GetComboGMoedas(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcMoedas, "g_moedas", "LoadComboGMoedas", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var m in db.g_moedas.AsNoTracking().OrderBy(p => p.id_moeda).Select(m => new { m.id_moeda, m.descricao }))
                    combo.Add(new SelectListItem { Value = m.id_moeda.ToString(), Text = m.descricao });
                return combo;
            });
        public List<SelectListItem> GetComboGcCfopFinalidade(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcCfopFinalidade, "gc_cfop_finalidade", "LoadComboGcCfopFinalidade", db, () =>
            {
                var combo = new List<SelectListItem> { new SelectListItem { Value = "-1", Text = "Selecionar" } };
                foreach (var f in db.gc_cfop_finalidade.AsNoTracking().Where(x => x.ativo).OrderBy(x => x.finalidade)
                    .Select(f => new { f.id_cfop_finalidade, f.finalidade }))
                    combo.Add(new SelectListItem { Value = f.id_cfop_finalidade.ToString(), Text = f.finalidade });
                return combo;
            });

        public List<SelectListItem> GetComboGcCfopOperacoesTelaPedido(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcCfopOperacoesTelaPedido, "gc_cfop_operacoes", "LoadComboGcCfopOperacoesTelaPedido", db, () => BuildComboGcCfopOperacoesTelaPedido(db));

        public List<SelectListItem> GetComboGcComexImportacoesTodas(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcComexImportacoesTodas, "gc_comex_importacoes", "LoadComboGcComexImportacoesTodas", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var i in db.gc_comex_importacoes.AsNoTracking().Where(c => c.ativo).OrderByDescending(c => c.id_importacao)
                    .Select(i => new { i.id_importacao, i.numero }))
                    combo.Add(new SelectListItem { Value = i.id_importacao.ToString(), Text = i.numero.EmptyIfNull().ToString().Trim() });
                return combo;
            });

        public List<SelectListItem> GetComboGcComexProdutosComId(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcComexProdutosComId, "gc_comex_produtos", "LoadComboGcComexProdutosComID", db, () => BuildComboGcComexProdutosComId(db));

        public List<SelectListItem> GetComboGcClientesContatosTipos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(LookupCacheKeys.GcClientesContatosTipos, "g_clientes_contatos_tipos", "LoadComboGcClientesContatosTipos", db, () =>
            {
                var combo = new List<SelectListItem>();
                foreach (var t in db.g_clientes_contatos_tipos.AsNoTracking().Where(x => x.ativo)
                    .Select(t => new { t.id_contato_tipo, t.nome }))
                    combo.Add(new SelectListItem { Value = t.id_contato_tipo.ToString(), Text = t.nome });
                return combo;
            });

        public List<SelectListItem> GetComboGcClientesContatosPedido(GdiPlataformEntities db, int idCliente) =>
            LookupQueryServiceCache.GetOrLoadParametricCombo(
                LookupCacheKeys.GcClientesContatosPedido,
                "g_clientes_contatos",
                new object[] { idCliente },
                () =>
                {
                    var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = "[ INFORME A PESSOA DE CONTATO ]" } };
                    var lista = db.g_clientes_contatos.AsNoTracking()
                        .Where(p => p.ativo && p.id_cliente == idCliente)
                        .Select(p => new { p.id_contato, p.contato })
                        .ToList();
                    foreach (var c in lista)
                        combo.Add(new SelectListItem { Value = c.id_contato.ToString(), Text = c.contato });
                    return combo;
                });

        public List<CstDatasetClientesContatos> GetDatasetGcClientesContatos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadDataset(
                LookupCacheKeys.GcClientesContatosDataset,
                "g_clientes_contatos",
                "LoadDatasetGcClientesContatos",
                db,
                () =>
                {
                    var dataSet = new List<CstDatasetClientesContatos>();
                    var lista = db.g_clientes_contatos.AsNoTracking()
                        .Where(p => p.ativo)
                        .Select(p => new { p.id_contato, p.id_cliente, p.contato, p.telefone, p.email })
                        .ToList();
                    foreach (var c in lista)
                    {
                        dataSet.Add(new CstDatasetClientesContatos
                        {
                            id_cliente_contato = c.id_contato,
                            id_cliente = c.id_cliente,
                            contato = c.contato,
                            email = c.email,
                            telefone = c.telefone
                        });
                    }
                    return dataSet;
                });
        private static List<SelectListItem> BuildComboGcTiposMovimentosVendas()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "Todos" },
                new SelectListItem { Value = "3", Text = "Cotações" },
                new SelectListItem { Value = "4", Text = "Pedidos" },
                new SelectListItem { Value = "8", Text = "OS" }
            };
        }

        private static List<SelectListItem> BuildComboGcTiposMovimentosCreateEdit()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "3", Text = "Cotação" },
                new SelectListItem { Value = "4", Text = "Pedido" },
                new SelectListItem { Value = "8", Text = "OS" },
                new SelectListItem { Value = "19", Text = "Transferência" }
            };
        }

        private static List<SelectListItem> BuildComboGcStatusMovimentos()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "-1", Text = "Todos" },
                new SelectListItem { Value = "1", Text = "Aberto" },
                new SelectListItem { Value = "2", Text = "Fechado" },
                new SelectListItem { Value = "3", Text = "Cancelado" }
            };
        }

        private static List<SelectListItem> BuildComboGcCfopOperacoesTelaPedido(GdiPlataformEntities db)
        {
            var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = "[ Selecione a Operação ]" } };
            if (CachePersister.userIdentity.Roles.Contains("gc_Movimentos_*")
                || CachePersister.userIdentity.Roles.Contains("gc_Movimentos_NfeCfopOperacoesFull"))
            {
                foreach (var r in db.gc_cfop_operacoes.AsNoTracking()
                    .Where(o => o.ativo && o.id_operacao_predecessora == 0 && !o.bloqueio_comercial && o.perfil_adm && o.show_tela_pedido)
                    .OrderBy(o => o.ordem)
                    .Select(o => new { o.id_cfop_operacao, o.descricao_erp }))
                    combo.Add(new SelectListItem { Value = r.id_cfop_operacao.ToString(), Text = r.descricao_erp });
            }
            else if (CachePersister.userIdentity.IdVendedor > 0)
            {
                foreach (var r in db.gc_cfop_operacoes.AsNoTracking()
                    .Where(o => o.ativo && o.id_operacao_predecessora == 0 && !o.bloqueio_comercial && o.perfil_vendedor && o.show_tela_pedido)
                    .OrderBy(o => o.ordem)
                    .Select(o => new { o.id_cfop_operacao, o.descricao_erp }))
                    combo.Add(new SelectListItem { Value = r.id_cfop_operacao.ToString(), Text = r.descricao_erp });
            }
            return combo;
        }

        private static List<SelectListItem> BuildComboGcComexProdutosComId(GdiPlataformEntities db)
        {
            int sizeNome = 100;
            int displayWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);
            if (displayWidth > 0 && displayWidth < 500) sizeNome = 50;
            if (displayWidth > 0 && displayWidth < 400) sizeNome = 40;
            if (displayWidth > 0 && displayWidth < 300) sizeNome = 30;

            var combo = new List<SelectListItem>();
            foreach (var p in db.gc_comex_produtos.AsNoTracking().Where(c => c.ativo).OrderByDescending(c => c.id_produto)
                .Select(p => new { p.id_comex_produto, p.traducao, p.description }))
            {
                var nome = "[Id: " + p.id_comex_produto + "] ";
                if (p.traducao.EmptyIfNull().ToString().Trim().Length > 0)
                    nome += p.traducao.Trim();
                else
                    nome += p.description.EmptyIfNull().ToString().Trim();
                if (nome.Length > sizeNome) nome = nome.Substring(0, sizeNome) + "...";
                combo.Add(new SelectListItem { Value = p.id_comex_produto.ToString(), Text = nome });
            }
            return combo;
        }
    }
}
