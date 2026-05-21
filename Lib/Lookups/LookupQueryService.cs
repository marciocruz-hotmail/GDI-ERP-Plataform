using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Domain;
using GdiPlataform.Lib;
using GdiPlataform.Security;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Implementação Fase 2/3 — lookups em MemoryCache (sem slots em ContextoModel). Partials: Comercial, Financeiro, CadastrosG (ex-Wave6a).</summary>
    public sealed partial class LookupQueryService : ILookupQueryService
    {
        public List<SelectListItem> GetComboGcTransportadora(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GcTransportadora,
                "g_clientes",
                "LoadComboGcTransportadora",
                db,
                () =>
                {
                    var combo = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "0", Text = "[ CLIENTE RETIRA ]" }
                    };
                    foreach (var r in db.g_clientes.AsNoTracking()
                        .Where(c => c.param_gc_transportadora == true)
                        .OrderBy(c => c.nome)
                        .Select(c => new { c.id_cliente, c.nome }))
                        combo.Add(new SelectListItem { Value = r.id_cliente.ToString(), Text = r.nome });
                    return combo;
                });

        public List<SelectListItem> GetComboGVendedores(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GVendedores,
                "g_vendedores",
                "LoadComboGVendedores",
                db,
                () =>
                {
                    var combo = new List<SelectListItem> { new SelectListItem { Value = "-1", Text = "[ Selecionar ]" } };
                    foreach (var v in db.g_vendedores.AsNoTracking().Where(p => p.ativo == true).OrderBy(p => p.nome)
                        .Select(v => new { v.id_vendedor, v.nome }))
                        combo.Add(new SelectListItem { Value = v.id_vendedor.ToString(), Text = v.nome });
                    return combo;
                });

        public List<g_vendedores> GetDatasetGVendedores(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadDataset(
                LookupCacheKeys.GVendedoresDataset,
                "g_vendedores",
                "LoadDatasetGVendedores",
                db,
                () => db.g_vendedores.AsNoTracking().Where(p => p.ativo == true).OrderBy(p => p.nome).ToList());

        public List<SelectListItem> GetComboGcCfop(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GcCfop,
                "gc_cfop",
                "LoadComboGcCfop",
                db,
                () =>
                {
                    var combo = new List<SelectListItem>();
                    foreach (var item in db.gc_cfop.AsNoTracking().Where(p => p.ativo == true).OrderBy(p => p.numero)
                        .Select(p => new { p.id_cfop, p.numero, p.descricao }))
                    {
                        var desc = item.numero + "  -  " + item.descricao.Trim();
                        combo.Add(new SelectListItem { Value = item.id_cfop.ToString(), Text = desc });
                    }
                    return combo;
                });

        public List<SelectListItem> GetComboGcFreteResponsavel(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GcFreteResponsavel,
                "gc_frete_responsavel",
                "LoadComboGcFreteResponsavel",
                db,
                () =>
                {
                    var combo = new List<SelectListItem>();
                    foreach (var r in db.gc_frete_responsavel.AsNoTracking().Where(p => p.ativo == true).OrderBy(p => p.descricao)
                        .Select(r => new { r.id_frete_responsavel, r.descricao }))
                        combo.Add(new SelectListItem { Value = r.id_frete_responsavel.ToString(), Text = r.descricao });
                    return combo;
                });

        public List<SelectListItem> GetComboGcEntregasPrazos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GcEntregasPrazos,
                "gc_entregas_prazos",
                "LoadComboGcEntregasPrazos",
                db,
                () =>
                {
                    var combo = new List<SelectListItem>();
                    foreach (var r in db.gc_entregas_prazos.AsNoTracking().OrderBy(p => p.id_entrega_prazo)
                        .Select(r => new { r.id_entrega_prazo, r.sigla }))
                        combo.Add(new SelectListItem { Value = r.id_entrega_prazo.ToString(), Text = r.sigla });
                    return combo;
                });

        public List<SelectListItem> GetComboGProdutoCondicao(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GProdutoCondicao,
                "g_produtos_condicoes",
                "LoadComboGProdutoCondicao",
                db,
                () =>
                {
                    var combo = new List<SelectListItem>();
                    foreach (var r in db.g_produtos_condicoes.AsNoTracking().OrderBy(p => p.id_produto_condicao)
                        .Select(r => new { r.id_produto_condicao, r.sigla, r.descricao }))
                        combo.Add(new SelectListItem
                        {
                            Value = r.id_produto_condicao.ToString(),
                            Text = r.sigla.Trim() + " - " + r.descricao.Trim()
                        });
                    return combo;
                });

        public List<SelectListItem> GetComboGcLocaisEstoqueOrders(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GcLocaisEstoqueOrders,
                null,
                null,
                db,
                () =>
                {
                    var combo = new List<SelectListItem> { new SelectListItem { Value = "-1", Text = "Estoque" } };
                    foreach (var loc in db.gc_locais_estoque.AsNoTracking().Where(p => p.allow_order == true).OrderBy(p => p.id_local_estoque)
                        .Select(loc => new { loc.id_local_estoque, loc.sigla }))
                        combo.Add(new SelectListItem { Value = loc.id_local_estoque.ToString(), Text = loc.sigla.EmptyIfNull().ToString() });
                    return combo;
                });

        public List<SelectListItem> GetComboGcMovimentosPosicao(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GcMovimentosPosicao,
                null,
                null,
                db,
                () =>
                {
                    var combo = new List<SelectListItem> { new SelectListItem { Value = "-1", Text = "Todos" } };
                    foreach (var p in db.gc_movimentos_posicao.AsNoTracking().OrderBy(c => c.id_movimento_posicao)
                        .Select(p => new { p.id_movimento_posicao, p.posicao }))
                        combo.Add(new SelectListItem
                        {
                            Value = p.id_movimento_posicao.ToString(),
                            Text = p.id_movimento_posicao + " - " + p.posicao
                        });
                    return combo;
                });

        public List<SelectListItem> GetComboGContasCaixas(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GContasCaixas,
                null,
                null,
                db,
                () => BuildComboContasCaixas(db, false));

        private static List<SelectListItem> BuildComboContasCaixas(GdiPlataformEntities db, bool gerencial)
        {
            var combo = new List<SelectListItem>();
            var idUsuario = CachePersister.userIdentity.IdUsuario.ToString();
            var sql = "select c.* from g_contas_caixas c "
                + "left join g_contas_caixas_acessos a on (c.id_conta_caixa = a.id_conta_caixa) "
                + "where (c.ativo = 1) and (c.is_gerencial = 1) and (a.id_usuario = " + idUsuario + ") order by c.ordem";
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

        public List<SelectListItem> GetComboGcProdutosServicosTodos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GcProdutosServicosTodos,
                "g_produtos",
                "LoadComboGcProdutosServicosTodos",
                db,
                () => BuildComboProdutosServicos(db, importadosOnly: false, includeIdInText: false));

        public List<SelectListItem> GetComboGcProdutosServicosImportados(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadCombo(
                LookupCacheKeys.GcProdutosServicosImportados,
                "g_produtos",
                "LoadComboGcProdutosServicosImportados",
                db,
                () => BuildComboProdutosServicos(db, importadosOnly: true, includeIdInText: false));

        /// <summary>Sem cache: truncamento depende de <see cref="CachePersister.userIdentity.DisplayScreenWidth"/> por utilizador.</summary>
        public List<SelectListItem> GetComboGcProdutosPosicaoEstoqueIndex(GdiPlataformEntities db) =>
            BuildComboGcProdutosPosicaoEstoqueIndex(db);

        private static List<SelectListItem> BuildComboGcProdutosPosicaoEstoqueIndex(GdiPlataformEntities db)
        {
            int sizeNomeItem = 100;
            int displayWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);
            if (displayWidth > 0 && displayWidth < 500) sizeNomeItem = 50;
            if (displayWidth > 0 && displayWidth < 400) sizeNomeItem = 40;
            if (displayWidth > 0 && displayWidth < 300) sizeNomeItem = 30;

            var combo = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "[ TODOS OS PRODUTOS ]" },
                new SelectListItem { Value = "-1", Text = "[ PRODUTOS COM SALDO ]" }
            };
            foreach (var p in db.g_produtos.AsNoTracking().Where(x => x.ativo).OrderBy(x => x.nome)
                .Select(p => new { p.id_produto, p.nome }))
            {
                var nome = p.nome.EmptyIfNull().ToString().Trim();
                if (nome.Length > sizeNomeItem) nome = nome.Substring(0, sizeNomeItem) + "...";
                combo.Add(new SelectListItem { Value = p.id_produto.ToString(), Text = nome });
            }
            return combo;
        }

        private static List<SelectListItem> BuildComboProdutosServicos(GdiPlataformEntities db, bool importadosOnly, bool includeIdInText)
        {
            var combo = new List<SelectListItem> { new SelectListItem { Value = "-1", Text = "" } };
            int displayWidth = 0;
            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);
            int sizeNome = (displayWidth / 100) * 8;
            if (displayWidth > 0 && displayWidth < 500) sizeNome = (displayWidth / 100 * 10);

            var query = db.g_produtos.AsNoTracking()
                .Where(p => p.ativo == true)
                .Select(p => new { p.id_produto, p.nome, p.importado });
            if (importadosOnly) query = query.Where(p => p.importado == true);
            foreach (var item in query.ToList())
            {
                var nome = item.nome.EmptyIfNull().ToString().Trim();
                if (nome.Length > sizeNome) nome = nome.Substring(0, sizeNome) + "...";
                if (nome.Length == 0) continue;
                var id = item.id_produto.EmptyIfNull().ToString().Trim();
                var text = includeIdInText ? "[Id: " + id + "] " + nome : nome;
                combo.Add(new SelectListItem { Value = id, Text = text });
            }
            return combo;
        }

        public List<CstDatasetProdutosServicos> GetDatasetGcProdutosServicos(GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadDataset(
                LookupCacheKeys.GcProdutosServicosDataset,
                "g_produtos",
                "LoadDatasetGcProdutosServicos",
                db,
                () =>
                {
                    var dataSet = new List<CstDatasetProdutosServicos>();
                    int displayWidth = 0;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out displayWidth);
                    int sizeNome = 100;
                    if (displayWidth > 0 && displayWidth < 500) sizeNome = 50;
                    if (displayWidth > 0 && displayWidth < 400) sizeNome = 40;
                    if (displayWidth > 0 && displayWidth < 300) sizeNome = 30;

                    var lista = db.g_produtos.AsNoTracking()
                        .Where(p => p.ativo == true)
                        .Select(p => new
                        {
                            p.id_produto, p.codigo, p.nome, p.preco_venda, p.fob1_dollar, p.fob2_dollar, p.fob3_dollar,
                            p.fob1_id_importacao, p.fob2_id_importacao, p.fob3_id_importacao, p.has_corecharge,
                            p.id_unidade_medida_venda, p.id_produto_ncm, p.saldo_01_disponivel, p.saldo_03_disponivel
                        }).ToList();

                    foreach (var item in lista)
                    {
                        var nome = item.nome.EmptyIfNull().ToString().Trim();
                        if (nome.Length > sizeNome) nome = nome.Substring(0, sizeNome) + "...";
                        dataSet.Add(new CstDatasetProdutosServicos
                        {
                            id_produto_servico = item.id_produto,
                            descricao_longa = nome,
                            codigo = item.codigo,
                            preco_venda = item.preco_venda,
                            fob1_dollar = item.fob1_dollar,
                            fob1_id_importacao = item.fob1_id_importacao,
                            fob2_dollar = item.fob2_dollar,
                            fob2_id_importacao = item.fob2_id_importacao,
                            fob3_dollar = item.fob3_dollar,
                            fob3_id_importacao = item.fob3_id_importacao,
                            has_corecharge = item.has_corecharge,
                            id_unidade_medida_venda = item.id_unidade_medida_venda,
                            id_produto_ncm = item.id_produto_ncm,
                            saldo_01_disponivel = item.saldo_01_disponivel,
                            saldo_03_disponivel = item.saldo_03_disponivel
                        });
                    }
                    return dataSet;
                });

        public List<SelectListItem> GetComboGcClientesContatos(GdiPlataformEntities db, int idCliente) =>
            LookupQueryServiceCache.GetOrLoadParametricCombo(
                LookupCacheKeys.GcClientesContatos,
                "g_clientes_contatos",
                new object[] { idCliente },
                () =>
                {
                    var combo = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "0", Text = "[ INFORME A PESSOA DE CONTATO ]" }
                    };
                    var lista = db.g_clientes_contatos.AsNoTracking()
                        .Where(p => p.ativo == true && p.id_cliente == idCliente)
                        .Select(p => new { p.id_contato, p.contato })
                        .ToList();
                    foreach (var c in lista)
                        combo.Add(new SelectListItem { Value = c.id_contato.ToString(), Text = c.contato });
                    return combo;
                });

        public List<SelectListItem> GetComboGcClientesDestinatarios(GdiPlataformEntities db, int idCliente) =>
            LookupQueryServiceCache.GetOrLoadParametricCombo(
                LookupCacheKeys.GcClientesDestinatarios,
                "g_clientes_destinatarios",
                new object[] { idCliente },
                () =>
                {
                    var combo = new List<SelectListItem>
                    {
                        new SelectListItem { Value = "0", Text = "[ O PRÓPRIO CLIENTE ]" }
                    };
                    foreach (var r in db.g_clientes_destinatarios.AsNoTracking()
                        .Where(c => c.id_cliente == idCliente && c.ativo == true)
                        .OrderBy(c => c.nome)
                        .Select(c => new { c.id_cliente_destinatario, c.nome }))
                    {
                        combo.Add(new SelectListItem
                        {
                            Value = r.id_cliente_destinatario.ToString(),
                            Text = r.nome
                        });
                    }
                    return combo;
                });

        public List<g_clientes_destinatarios> GetDatasetGcClientesDestinatarios(int idCliente, GdiPlataformEntities db) =>
            LookupQueryServiceCache.GetOrLoadParametricDataset(
                LookupCacheKeys.GcClientesDestinatariosDataset,
                new object[] { idCliente },
                () =>
                {
                    var lista = new List<g_clientes_destinatarios>
                    {
                        new g_clientes_destinatarios { id_cliente_destinatario = 0, nome = "O PRÓPRIO CLIENTE" }
                    };
                    lista.AddRange(db.g_clientes_destinatarios.AsNoTracking()
                        .Where(p => p.ativo == true && p.id_cliente == idCliente)
                        .ToList());
                    return lista;
                });

        public List<SelectListItem> GetComboGcCfopOperacoesFaturamentoPedido(GdiPlataformEntities db, int idCfopOperacao) =>
            LookupQueryServiceCache.GetOrLoadParametricCombo(
                LookupCacheKeys.GcCfopOperacoesFaturamentoPedido,
                "gc_cfop_operacoes",
                new object[] { idCfopOperacao },
                () =>
                {
                    var combo = new List<SelectListItem>();
                    try
                    {
                        var record = db.gc_cfop_operacoes.Find(idCfopOperacao);
                        if (record != null)
                            combo.Add(new SelectListItem
                            {
                                Value = record.id_cfop_operacao.EmptyIfNull().ToString(),
                                Text = record.descricao_erp.EmptyIfNull().ToString()
                            });
                        var vinculada = db.gc_cfop_operacoes
                            .FirstOrDefault(o => o.id_operacao_predecessora == idCfopOperacao);
                        if (vinculada != null)
                            combo.Add(new SelectListItem
                            {
                                Value = vinculada.id_cfop_operacao.EmptyIfNull().ToString(),
                                Text = vinculada.descricao_erp.EmptyIfNull().ToString()
                            });
                    }
                    catch { }
                    return combo;
                });

        public List<SelectListItem> GetComboGcEstoqueEnderecoArea(GdiPlataformEntities db, int idLocalEstoque) =>
            BuildComboEstoqueEndereco(db, idLocalEstoque, LookupCacheKeys.GcEstoqueEnderecoArea, "gc_estoque_endereco_area", "[ Área ]",
                () => db.gc_estoque_endereco_area.AsNoTracking().Where(p => p.ativo == true),
                idLocalEstoque,
                q => idLocalEstoque == 0 ? q.OrderBy(p => p.id_local_estoque).ThenBy(p => p.id_estoque_area)
                    : q.Where(p => p.id_local_estoque == idLocalEstoque).OrderBy(p => p.id_estoque_area),
                i => new SelectListItem { Value = i.id_estoque_area.ToString(), Text = i.nome });

        public List<SelectListItem> GetComboGcEstoqueEnderecoSecao(GdiPlataformEntities db, int idLocalEstoque) =>
            BuildComboEstoqueEndereco(db, idLocalEstoque, LookupCacheKeys.GcEstoqueEnderecoSecao, "gc_estoque_endereco_secao", "[ Seção ]",
                () => db.gc_estoque_endereco_secao.AsNoTracking().Where(p => p.ativo == true),
                idLocalEstoque,
                q => idLocalEstoque == 0 ? q.OrderBy(p => p.id_local_estoque).ThenBy(p => p.id_estoque_secao)
                    : q.Where(p => p.id_local_estoque == idLocalEstoque).OrderBy(p => p.id_estoque_secao),
                i => new SelectListItem { Value = i.id_estoque_secao.ToString(), Text = i.nome });

        public List<SelectListItem> GetComboGcEstoqueEnderecoCorredor(GdiPlataformEntities db, int idLocalEstoque) =>
            BuildComboEstoqueEndereco(db, idLocalEstoque, LookupCacheKeys.GcEstoqueEnderecoCorredor, "gc_estoque_endereco_corredor", "[ Corredor ]",
                () => db.gc_estoque_endereco_corredor.AsNoTracking().Where(p => p.ativo == true),
                idLocalEstoque,
                q => idLocalEstoque == 0 ? q.OrderBy(p => p.id_local_estoque).ThenBy(p => p.id_estoque_corredor)
                    : q.Where(p => p.id_local_estoque == idLocalEstoque).OrderBy(p => p.id_estoque_corredor),
                i => new SelectListItem { Value = i.id_estoque_corredor.ToString(), Text = i.nome });

        public List<SelectListItem> GetComboGcEstoqueEnderecoPrateleira(GdiPlataformEntities db, int idLocalEstoque) =>
            BuildComboEstoqueEndereco(db, idLocalEstoque, LookupCacheKeys.GcEstoqueEnderecoPrateleira, "gc_estoque_endereco_prateleira", "[ Prateleira ]",
                () => db.gc_estoque_endereco_prateleira.AsNoTracking().Where(p => p.ativo == true),
                idLocalEstoque,
                q => idLocalEstoque == 0 ? q.OrderBy(p => p.id_local_estoque).ThenBy(p => p.id_estoque_prateleira)
                    : q.Where(p => p.id_local_estoque == idLocalEstoque).OrderBy(p => p.id_estoque_prateleira),
                i => new SelectListItem { Value = i.id_estoque_prateleira.ToString(), Text = i.nome });

        private static List<SelectListItem> BuildComboEstoqueEndereco<T>(
            GdiPlataformEntities db,
            int idLocalEstoque,
            string lookupKey,
            string tableName,
            string labelPadrao,
            Func<IQueryable<T>> baseQuery,
            int idLocal,
            Func<IQueryable<T>, IOrderedQueryable<T>> order,
            Func<T, SelectListItem> map) where T : class
        {
            return LookupQueryServiceCache.GetOrLoadParametricCombo(
                lookupKey,
                tableName,
                new object[] { idLocalEstoque },
                () =>
                {
                    var combo = new List<SelectListItem> { new SelectListItem { Value = "0", Text = labelPadrao } };
                    foreach (var item in order(baseQuery()).ToList())
                        combo.Add(map(item));
                    return combo;
                });
        }

        public List<SelectListItem> GetComboGedArquivosTipos(GdiPlataformEntities db, int idTipo, int idTipoPai) =>
            LookupQueryServiceCache.GetOrLoadParametricCombo(
                LookupCacheKeys.GedArquivosTipos,
                "ged_arquivos_tipos",
                new object[] { idTipo, idTipoPai },
                () => BuildComboGedArquivosTipos(db, idTipo, idTipoPai));

        private static List<SelectListItem> BuildComboGedArquivosTipos(GdiPlataformEntities db, int idTipo, int idTipoPai)
        {
            List<ged_arquivos_tipos> listaAll;
            if (idTipo <= 0 && idTipoPai <= 0)
                listaAll = db.ged_arquivos_tipos.AsNoTracking().Where(t => t.id_arquivo_tipo > 0).OrderBy(t => t.descricao).ToList();
            else if (idTipo > 0)
                listaAll = db.ged_arquivos_tipos.AsNoTracking().Where(t => t.id_arquivo_tipo == idTipo).OrderBy(t => t.descricao).ToList();
            else
                listaAll = db.ged_arquivos_tipos.AsNoTracking().Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == idTipoPai).OrderBy(t => t.descricao).ToList();

            var combo = new List<SelectListItem>();
            if (idTipo <= 0)
            {
                combo.Add(new SelectListItem { Value = "0", Text = "[ Selecione o Tipo ]" });
                AppendGedTiposHierarquia(combo, listaAll, idTipoPai);
            }
            else
            {
                foreach (var n1 in listaAll.Where(t => t.id_arquivo_tipo == idTipo).OrderBy(t => t.descricao))
                {
                    combo.Add(new SelectListItem
                    {
                        Value = n1.id_arquivo_tipo.ToString(),
                        Text = "  -  " + n1.descricao.EmptyIfNull().ToString()
                    });
                }
            }
            return combo;
        }

        private static void AppendGedTiposHierarquia(
            List<SelectListItem> combo,
            List<ged_arquivos_tipos> listaAll,
            int idTipoPai)
        {
            var n1List = listaAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == idTipoPai).OrderBy(t => t.descricao).ToList();
            foreach (var n1 in n1List)
            {
                if (n1.id_tipo_pai != idTipoPai) continue;
                if (n1.ativo == true)
                    combo.Add(new SelectListItem { Value = n1.id_arquivo_tipo.ToString(), Text = "  -  " + n1.descricao.EmptyIfNull() });
                var n2List = listaAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == n1.id_arquivo_tipo).OrderBy(t => t.descricao);
                foreach (var n2 in n2List)
                {
                    if (n2.ativo == true)
                        combo.Add(new SelectListItem { Value = n2.id_arquivo_tipo.ToString(), Text = "  -  " + n2.descricao.EmptyIfNull() });
                    var n3List = listaAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == n2.id_arquivo_tipo).OrderBy(t => t.descricao);
                    foreach (var n3 in n3List)
                    {
                        if (n3.ativo == true)
                            combo.Add(new SelectListItem { Value = n3.id_arquivo_tipo.ToString(), Text = "  -  " + n3.descricao.EmptyIfNull() });
                        var n4List = listaAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == n3.id_arquivo_tipo).OrderBy(t => t.descricao);
                        foreach (var n4 in n4List)
                        {
                            if (n4.ativo == true)
                                combo.Add(new SelectListItem { Value = n4.id_arquivo_tipo.ToString(), Text = "  -  " + n4.descricao.EmptyIfNull() });
                            var n5List = listaAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == n4.id_arquivo_tipo).OrderBy(t => t.descricao);
                            foreach (var n5 in n5List)
                            {
                                if (n5.ativo == true)
                                    combo.Add(new SelectListItem { Value = n5.id_arquivo_tipo.ToString(), Text = "  -  " + n5.descricao.EmptyIfNull() });
                            }
                        }
                    }
                }
            }
        }
    }
}
