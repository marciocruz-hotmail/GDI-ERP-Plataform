using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Domain;
using GdiPlataform.Lib.Lookups;
using GdiPlataform.Security;

namespace GdiPlataform.Lib
{
    public static class LibDataSets
    {
        private static void EnsureContextoModel()
        {
            if (CachePersister.contextoModel == null)
            {
                CachePersister.contextoModel = new ContextoModel();
            }
        }

        /// <summary>Copia defensiva para combos parametricos (sem cache global nem JSON clone).</summary>
        private static List<SelectListItem> CloneSelectList(IEnumerable<SelectListItem> source)
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

        public static List<SelectListItem> LoadComboGcClientesDestinatarios(GdiPlataformEntities db, int IdCliente)
            => LookupQueryServiceAccessor.Current.GetComboGcClientesDestinatarios(db, IdCliente);

        public static List<g_clientes_destinatarios> LoadDatasetGcClientesDestinatarios(int IdCliente, GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetDatasetGcClientesDestinatarios(IdCliente, db);

        public static List<SelectListItem> LoadComboGcTransportadora(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGcTransportadora(db);

        public static List<SelectListItem> LoadComboGcIcmsUfIsento(GdiPlataformEntities db)
        {
            if (CachePersister.contextoModel.gc_comboIcmsUfIsento.Count == 0)
            {
                var comboIcmsUfIsento = new List<SelectListItem>();
                comboIcmsUfIsento.Add(new SelectListItem { Value = "false", Text = "NÃO" });
                comboIcmsUfIsento.Add(new SelectListItem { Value = "true", Text = "SIM" });
            }
            else
            {
                List<SelectListItem> ListaIcmsUfIsento = CachePersister.contextoModel.gc_comboIcmsUfIsento;
                CachePersister.contextoModel.gc_comboIcmsUfIsento = ListaIcmsUfIsento;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboIcmsUfIsento));
        }

        public static List<SelectListItem> LoadComboGcIcmsCstSimples(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.gc_comboIcmsCstSimples.Count == 0) || (LibDB.IsTableUpdate("gc_icms_cst", "LoadComboGcIcmsCstSimples", db) == true))
            {
                var comboIcmsCstSimples = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_icms_cst> ListaIcmsCstSimples = db.gc_icms_cst.Where(p => p.ativo == true).OrderBy(p => p.codigo_cst);
                    foreach (gc_icms_cst Record in ListaIcmsCstSimples)
                    {
                        comboIcmsCstSimples.Add(new SelectListItem { Value = Record.id_icms_cst.ToString(), Text = Record.codigo_cst.ToString() + " - " + Record.descricao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboIcmsCstSimples = comboIcmsCstSimples;
            }
            else
            {
                List<SelectListItem> comboIcmsCstSimples = CachePersister.contextoModel.gc_comboIcmsCstSimples;
                CachePersister.contextoModel.gc_comboIcmsCstSimples = comboIcmsCstSimples;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboIcmsCstSimples));
        }

        public static List<SelectListItem> LoadComboGUnidadeMedida(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.g_comboUnidadeMedida.Count == 0) || (LibDB.IsTableUpdate("g_unidade_medida", "LoadComboGUnidadeMedida", db) == true))
            {
                var comboUnidadeMedida = new List<SelectListItem>();
                IQueryable<g_unidade_medida> listaComboUnidadeMedida = db.g_unidade_medida.Where(p => p.ativo == true).OrderBy(p => p.descricao);
                foreach (g_unidade_medida item_unidade_medida in listaComboUnidadeMedida)
                {
                    comboUnidadeMedida.Add(new SelectListItem { Value = item_unidade_medida.id_unidade_medida.ToString(), Text = item_unidade_medida.descricao.ToString() });
                }
                CachePersister.contextoModel.g_comboUnidadeMedida = comboUnidadeMedida;
            }
            else
            {
                List<SelectListItem> listaComboUnidadeMedida = CachePersister.contextoModel.g_comboUnidadeMedida;
                CachePersister.contextoModel.g_comboUnidadeMedida = listaComboUnidadeMedida;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.g_comboUnidadeMedida));
        }


        public static List<SelectListItem> LoadComboGcCfop(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGcCfop(db);

        public static List<SelectListItem> LoadComboGcCfopOperacoesFaturamentoPedido(GdiPlataformEntities db, int IdCfopOperacao)
            => LookupQueryServiceAccessor.Current.GetComboGcCfopOperacoesFaturamentoPedido(db, IdCfopOperacao);

        public static List<SelectListItem> LoadComboGcCfopOperacoesTelaPedido(GdiPlataformEntities db)
        {

            if ((CachePersister.contextoModel.gc_comboGcCfop.Count == 0) || (LibDB.IsTableUpdate("gc_cfop_operacoes", "LoadComboGcCfopOperacoesTelaPedido", db) == true))
            {
                var ComboCfopOperacoesVendedor = new List<SelectListItem>();
                try
                {
                    ComboCfopOperacoesVendedor.Add(new SelectListItem { Value = "0", Text = "[ Selecione a Operação ]" });

                    if ((CachePersister.userIdentity.Roles.Contains("gc_Movimentos_*")) || (CachePersister.userIdentity.Roles.Contains("gc_Movimentos_NfeCfopOperacoesFull")))
                    {
                        List<gc_cfop_operacoes> ListaOperacoesAdm = db.gc_cfop_operacoes.Where(o => o.ativo == true && o.id_operacao_predecessora == 0 && o.bloqueio_comercial == false && o.perfil_adm == true && o.show_tela_pedido == true).OrderBy(o => o.ordem).ToList();
                        foreach (gc_cfop_operacoes RecordCfopOperacoes in ListaOperacoesAdm)
                        {
                            ComboCfopOperacoesVendedor.Add(new SelectListItem { Value = RecordCfopOperacoes.id_cfop_operacao.EmptyIfNull().ToString(), Text = RecordCfopOperacoes.descricao_erp.EmptyIfNull().ToString() });
                        }
                    }
                    else if (CachePersister.userIdentity.IdVendedor > 0)
                    {
                        List<gc_cfop_operacoes> ListaOperacoesVendedor = db.gc_cfop_operacoes.Where(o => o.ativo == true && o.id_operacao_predecessora == 0 && o.bloqueio_comercial == false && o.perfil_vendedor == true && o.show_tela_pedido == true).OrderBy(o => o.ordem).ToList();
                        foreach (gc_cfop_operacoes RecordCfopOperacoes in ListaOperacoesVendedor)
                        {
                            ComboCfopOperacoesVendedor.Add(new SelectListItem { Value = RecordCfopOperacoes.id_cfop_operacao.EmptyIfNull().ToString(), Text = RecordCfopOperacoes.descricao_erp.EmptyIfNull().ToString() });
                        }
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGcCfopOperacoesVendedor = ComboCfopOperacoesVendedor;
            }
            else
            {
                List<SelectListItem> ListaOperacoes = CachePersister.contextoModel.gc_comboGcCfopOperacoesVendedor;
                CachePersister.contextoModel.gc_comboGcCfopOperacoesVendedor = ListaOperacoes;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcCfopOperacoesVendedor));
        }


        public static List<gc_cfop_operacoes> LoadDatasetGcCfopOperacoes(GdiPlataformEntities db)
        {
            // Vendedores
            if ((CachePersister.contextoModel.gc_dataSetCfopOperacoes.Count == 0) || (LibDB.IsTableUpdate("gc_cfop_operacoes", "LoadDatasetGcCfopOperacoes", db) == true))
            {
                var DataSetCfopOperacoes = new List<gc_cfop_operacoes>();
                try
                {
                    IQueryable<gc_cfop_operacoes> listaDbCfopOperacao = db.gc_cfop_operacoes.Where(o => o.ativo == true && o.bloqueio_comercial == false && (o.perfil_vendedor == true || o.perfil_adm == true)).OrderBy(p => p.ordem);
                    foreach (gc_cfop_operacoes itemCfopOperacao in listaDbCfopOperacao)
                    {
                        DataSetCfopOperacoes.Add(itemCfopOperacao);
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_dataSetCfopOperacoes = DataSetCfopOperacoes;
            }
            else
            {
                List<gc_cfop_operacoes> DataSetCfopOperacoes = CachePersister.contextoModel.gc_dataSetCfopOperacoes;
                CachePersister.contextoModel.gc_dataSetCfopOperacoes = DataSetCfopOperacoes;
            }
            return CachePersister.contextoModel.gc_dataSetCfopOperacoes;
        }

        public static List<SelectListItem> LoadComboGcFreteResponsavel(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGcFreteResponsavel(db);

        public static List<SelectListItem> LoadComboGcEntregasPrazos(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGcEntregasPrazos(db);

        public static List<SelectListItem> LoadComboGProdutoCondicao(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGProdutoCondicao(db);

        public static List<SelectListItem> LoadComboGProdutosNCM(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.gc_comboProdutosNCM.Count == 0) || (LibDB.IsTableUpdate("g_produtos_ncm", "LoadComboGProdutosNCM", db) == true))
            {
                var comboProdutosNCM = new List<SelectListItem>();
                try
                {
                    IQueryable<g_produtos_ncm> listaDbProdutosNCM = db.g_produtos_ncm.Select(p => p).OrderBy(p => p.codigo_ncm);
                    foreach (g_produtos_ncm Record in listaDbProdutosNCM)
                    {
                        comboProdutosNCM.Add(new SelectListItem { Value = Record.id_produto_ncm.ToString(), Text = Record.codigo_ncm.ToString().Trim() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboProdutosNCM = comboProdutosNCM;
            }
            else
            {
                List<SelectListItem> ListaProdutosNCM = CachePersister.contextoModel.gc_comboProdutosNCM;
                CachePersister.contextoModel.gc_comboProdutosNCM = ListaProdutosNCM;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboProdutosNCM));
        }

        public static List<SelectListItem> LoadComboGProdutosTipos(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.gc_comboProdutosTipos.Count == 0) || (LibDB.IsTableUpdate("g_produtos_tipos", "LoadComboGProdutosTipos", db) == true))
            {
                var comboProdutosTipos = new List<SelectListItem>();
                try
                {
                    IQueryable<g_produtos_tipos> listaDbProdutosTipos = db.g_produtos_tipos.Where(p => p.id_produto_tipo > 0).OrderBy(p => p.nome);
                    foreach (g_produtos_tipos Record in listaDbProdutosTipos)
                    {
                        comboProdutosTipos.Add(new SelectListItem { Value = Record.id_produto_tipo.ToString(), Text = Record.nome.ToString().Trim() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboProdutosTipos = comboProdutosTipos;
            }
            else
            {
                List<SelectListItem> listaDbProdutosTipos = CachePersister.contextoModel.gc_comboProdutosTipos;
                CachePersister.contextoModel.gc_comboProdutosTipos = listaDbProdutosTipos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboProdutosTipos));
        }

        public static List<SelectListItem> LoadComboGMoedas(GdiPlataformEntities db)
        {
            if (CachePersister.contextoModel.gc_comboMoedas.Count == 0)
            {
                var comboMoedas = new List<SelectListItem>();
                try
                {
                    IQueryable<g_moedas> listaDbMoedas = db.g_moedas.Select(p => p).OrderBy(p => p.id_moeda);
                    foreach (g_moedas item4 in listaDbMoedas)
                    {
                        comboMoedas.Add(new SelectListItem { Value = item4.id_moeda.ToString(), Text = item4.descricao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboMoedas = comboMoedas;
            }
            else
            {
                List<SelectListItem> ListaMoedas = CachePersister.contextoModel.gc_comboMoedas;
                CachePersister.contextoModel.gc_comboMoedas = ListaMoedas;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboMoedas));
        }
        public static List<SelectListItem> LoadComboGVendedores(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGVendedores(db);

        public static List<g_vendedores> LoadDatasetGVendedores(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetDatasetGVendedores(db);

        public static List<SelectListItem> LoadComboGcClientesContatos(GdiPlataformEntities db, int IdCliente)
            => LookupQueryServiceAccessor.Current.GetComboGcClientesContatos(db, IdCliente);

        public static List<SelectListItem> LoadComboGcClientesContatosPedido(GdiPlataformEntities db, int IdCliente)
        {
            // Clientes/Contatos
            var ComboClientesContatosPedido = new List<SelectListItem>();
            ComboClientesContatosPedido.Add(new SelectListItem { Value = "0", Text = "[ INFORME A PESSOA DE CONTATO ]" });
            try
            {
                var listaDbClientesContatos = db.g_clientes_contatos.Select(p => new { p.id_contato, p.ativo, p.id_cliente, p.contato, p.telefone, p.email }).Where(p => (p.ativo == true && p.id_cliente == IdCliente)).ToList();
                foreach (var item_g_clientes_contatos in listaDbClientesContatos)
                {
                    ComboClientesContatosPedido.Add(new SelectListItem { Value = item_g_clientes_contatos.id_contato.ToString(), Text = item_g_clientes_contatos.contato.ToString() });
                }
            }
            finally { }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(ComboClientesContatosPedido));
        }


        public static List<SelectListItem> LoadComboGcClientesContatosTipos(GdiPlataformEntities db)
        {
            // Clientes/Contatos
            if (CachePersister.contextoModel.gc_comboClientesContatosTipos.Count == 0)
            {
                var comboClientesContatosTipos = new List<SelectListItem>();
                try
                {
                    var listaDbClientesContatosTipos = db.g_clientes_contatos_tipos.Select(t => new { t.id_contato_tipo, t.nome, t.ativo}).Where(t => (t.ativo == true)).ToList();
                    foreach (var item_g_clientes_contatos_tipos in listaDbClientesContatosTipos)
                    {
                        comboClientesContatosTipos.Add(new SelectListItem { Value = item_g_clientes_contatos_tipos.id_contato_tipo.ToString(), Text = item_g_clientes_contatos_tipos.nome.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboClientesContatosTipos = comboClientesContatosTipos;
            }
            else
            {
                List<SelectListItem> ListaClientesContatosTipos = CachePersister.contextoModel.gc_comboClientesContatosTipos;
                CachePersister.contextoModel.gc_comboClientesContatosTipos = ListaClientesContatosTipos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboClientesContatosTipos));
        }

        public static List<CstDatasetClientesContatos> LoadDatasetGcClientesContatos(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.gc_dataSetClientesContatos.Count == 0) || (LibDB.IsTableUpdate("g_clientes_contatos", "LoadDatasetGcClientesContatos", db) == true))
            {
                var dataSetClientesContatos = new List<CstDatasetClientesContatos>();
                try
                {
                    var listaDbClientesContatos = db.g_clientes_contatos.Select(p => new { p.id_contato, p.ativo, p.id_cliente, p.contato, p.telefone, p.email }).Where(p => (p.ativo == true)).ToList();
                    foreach (var item_g_clientes_contatos in listaDbClientesContatos)
                    {
                        CstDatasetClientesContatos record_cstDatasetClientesContatos = new CstDatasetClientesContatos();
                        record_cstDatasetClientesContatos.id_cliente_contato = item_g_clientes_contatos.id_contato;
                        record_cstDatasetClientesContatos.id_cliente = item_g_clientes_contatos.id_cliente;
                        record_cstDatasetClientesContatos.contato = item_g_clientes_contatos.contato;
                        record_cstDatasetClientesContatos.email = item_g_clientes_contatos.email;
                        record_cstDatasetClientesContatos.telefone = item_g_clientes_contatos.telefone;
                        dataSetClientesContatos.Add(record_cstDatasetClientesContatos);
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_dataSetClientesContatos = dataSetClientesContatos;
            }
            else
            {
                List<CstDatasetClientesContatos> ListaDataSetClientesContatos = CachePersister.contextoModel.gc_dataSetClientesContatos;
                CachePersister.contextoModel.gc_dataSetClientesContatos = ListaDataSetClientesContatos;
            }
            return CachePersister.contextoModel.gc_dataSetClientesContatos;
        }
        public static List<SelectListItem> LoadComboGcTiposMovimentosCompras(GdiPlataformEntities db)
        {
            // Tipos de Movimentos
            if (CachePersister.contextoModel.gc_comboTiposMovimentosCompras.Count == 0)
            {
                var comboTiposMovimentosCompras = new List<SelectListItem>();
                comboTiposMovimentosCompras.Add(new SelectListItem { Value = "-1", Text = "Todos" });
                comboTiposMovimentosCompras.Add(new SelectListItem { Value = "3", Text = "Cotações" });
                comboTiposMovimentosCompras.Add(new SelectListItem { Value = "4", Text = "Pedidos" });
                comboTiposMovimentosCompras.Add(new SelectListItem { Value = "8", Text = "OS" });
                CachePersister.contextoModel.gc_comboTiposMovimentosCompras = comboTiposMovimentosCompras;

            }
            else
            {
                List<SelectListItem> ListaTiposMovimentos = CachePersister.contextoModel.gc_comboTiposMovimentosCompras;
                CachePersister.contextoModel.gc_comboTiposMovimentosCompras = ListaTiposMovimentos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboTiposMovimentosCompras));
        }
        public static List<SelectListItem> LoadComboGcTiposMovimentosVendas(GdiPlataformEntities db)
        {
            // Tipos de Movimentos
            if (CachePersister.contextoModel.gc_comboTiposMovimentosVendas.Count == 0)
            {
                var comboTiposMovimentosVendas = new List<SelectListItem>();
                comboTiposMovimentosVendas.Add(new SelectListItem { Value = "-1", Text = "Todos" });
                comboTiposMovimentosVendas.Add(new SelectListItem { Value = "3", Text = "Cotações" });
                comboTiposMovimentosVendas.Add(new SelectListItem { Value = "4", Text = "Pedidos" });
                comboTiposMovimentosVendas.Add(new SelectListItem { Value = "8", Text = "OS" });
                CachePersister.contextoModel.gc_comboTiposMovimentosVendas = comboTiposMovimentosVendas;

            }
            else
            {
                List<SelectListItem> ListaTiposMovimentos = CachePersister.contextoModel.gc_comboTiposMovimentosVendas;
                CachePersister.contextoModel.gc_comboTiposMovimentosVendas = ListaTiposMovimentos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboTiposMovimentosVendas));
        }
        public static List<SelectListItem> LoadComboGcTiposMovimentosCreateEdit(GdiPlataformEntities db)
        {
            // Tipos de Movimentos - No Create/Edit
            if (CachePersister.contextoModel.gc_comboTiposMovimentosCreateEdit.Count == 0)
            {
                var comboTiposMovimentosCreateEdit = new List<SelectListItem>();
                comboTiposMovimentosCreateEdit.Add(new SelectListItem { Value = "3", Text = "Cotação" });
                comboTiposMovimentosCreateEdit.Add(new SelectListItem { Value = "4", Text = "Pedido" });
                comboTiposMovimentosCreateEdit.Add(new SelectListItem { Value = "8", Text = "OS" });
                comboTiposMovimentosCreateEdit.Add(new SelectListItem { Value = "19", Text = "Transferência" });
                CachePersister.contextoModel.gc_comboTiposMovimentosCreateEdit = comboTiposMovimentosCreateEdit;
            }
            else
            {
                List<SelectListItem> ListaTiposMovimentosCreateEdit = CachePersister.contextoModel.gc_comboTiposMovimentosCreateEdit;
                CachePersister.contextoModel.gc_comboTiposMovimentosCreateEdit = ListaTiposMovimentosCreateEdit;
            }
            //return CachePersister.contextoModel.gc_comboTiposMovimentosCreateEdit;
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboTiposMovimentosCreateEdit));
        }
        public static List<SelectListItem> LoadComboGcStatusMovimentos(GdiPlataformEntities db)
        {
            // Status de Movimentos
            if (CachePersister.contextoModel.gc_comboStatusMovimentos.Count == 0)
            {
                var comboStatusMovimentos = new List<SelectListItem>();
                comboStatusMovimentos.Add(new SelectListItem { Value = "-1", Text = "Todos" });
                comboStatusMovimentos.Add(new SelectListItem { Value = "1", Text = "Aberto" });
                comboStatusMovimentos.Add(new SelectListItem { Value = "2", Text = "Fechado" });
                comboStatusMovimentos.Add(new SelectListItem { Value = "3", Text = "Cancelado" });
                CachePersister.contextoModel.gc_comboStatusMovimentos = comboStatusMovimentos;
            }
            else
            {
                List<SelectListItem> ListaStatusMovimentos = CachePersister.contextoModel.gc_comboStatusMovimentos;
                CachePersister.contextoModel.gc_comboStatusMovimentos = ListaStatusMovimentos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboStatusMovimentos));
        }
        public static List<SelectListItem> LoadComboGcProdutosServicosTodos(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGcProdutosServicosTodos(db);

        public static List<SelectListItem> LoadComboGcProdutosServicosTodosComId(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.gc_comboProdutosServicosTodosComId.Count == 0) || (LibDB.IsTableUpdate("g_produtos", "LoadComboGcProdutosServicosTodosComId", db) == true))
            {
                var comboProdutosServicosComId = new List<SelectListItem>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    int _SizeNomeItem = 100;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var listaDbProdutosServicos = db.g_produtos.Select(p => new { p.id_produto, p.codigo, p.nome, p.preco_venda, p.has_corecharge, p.ativo, p.importado }).Where(p => p.ativo == true).ToList();
                    comboProdutosServicosComId.Add(new SelectListItem { Value = "-1", Text = "" });
                    _SizeNomeItem = (_DisplayScreenWidth / 100) * 8;
                    if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 500)) { _SizeNomeItem = (_DisplayScreenWidth / 100 * 10); }
                    foreach (var item_g_produtos in listaDbProdutosServicos)
                    {
                        String IdProduto = item_g_produtos.id_produto.EmptyIfNull().ToString().Trim();
                        String NomeProduto = item_g_produtos.nome.EmptyIfNull().ToString().Trim();
                        if (NomeProduto.Length > _SizeNomeItem) { NomeProduto = NomeProduto.Substring(0, _SizeNomeItem) + "..."; };
                        if (NomeProduto.EmptyIfNull().ToString().Trim().Length > 0)
                        {
                            comboProdutosServicosComId.Add(new SelectListItem { Value = IdProduto, Text = "[Id: " + IdProduto + "] " + NomeProduto });
                        }
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboProdutosServicosTodosComId = comboProdutosServicosComId;
            }
            else
            {
                List<SelectListItem> ListaProdutosServicos = CachePersister.contextoModel.gc_comboProdutosServicosTodosComId;
                CachePersister.contextoModel.gc_comboProdutosServicosTodosComId = ListaProdutosServicos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboProdutosServicosTodosComId));
        }

        public static List<SelectListItem> LoadComboGcProdutosServicosImportados(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGcProdutosServicosImportados(db);

        public static List<CstDatasetProdutosServicos> LoadDatasetGcProdutosServicos(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetDatasetGcProdutosServicos(db);

        public static List<SelectListItem> LoadComboGcLocaisEstoqueOrders(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGcLocaisEstoqueOrders(db);

        public static List<SelectListItem> LoadComboGClientesFornecedores(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGClientesFornecedores(db);

        public static List<SelectListItem> LoadComboGClientesFornecedoresComDoc(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGClientesFornecedoresComDoc(db);

        public static List<SelectListItem> LoadComboSomenteGClientes(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboSomenteGClientes(db);
        public static List<SelectListItem> LoadComboSomenteGClientesComDoc(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.gc_comboSomenteGClientesComDoc.Count == 0) || (LibDB.IsTableUpdate("g_clientes", "LoadComboSomenteGClientesComDoc", db) == true))
            {
                var ComboSomenteGClientesComDoc = new List<SelectListItem>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    String Documento = string.Empty;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var ListaTemp = db.g_clientes.Select(c => new { c.id_cliente, c.cpf, c.cnpj, c.is_cliente, c.is_fornecedor, c.ativo, c.nome, c.id_ciclo_faturamento, c.id_coligada, c.id_filial }).Where(p => (p.ativo == true) && (p.is_cliente == true)).OrderBy(p => p.nome).ToList();
                    foreach (var Record in ListaTemp)
                    {
                        foreach (var RecordTemp in ListaTemp)
                        {
                            if (RecordTemp.cpf.EmptyIfNull().ToString().Trim().Length > 0) { Documento = RecordTemp.cpf.Trim(); }
                            else if (RecordTemp.cnpj.EmptyIfNull().ToString().Trim().Length > 0) { Documento = RecordTemp.cnpj.Trim(); };
                            String NomeCliente = RecordTemp.nome.EmptyIfNull().ToString().Trim();
                            if ((NomeCliente.Length > 50) && (_DisplayScreenWidth < 500)) { NomeCliente = NomeCliente.Substring(0, 50); };
                            ComboSomenteGClientesComDoc.Add(new SelectListItem { Value = RecordTemp.id_cliente.ToString(), Text = NomeCliente + "\xA0\xA0\xA0\xA0\xA0" + "[ " + Documento + " ]" });
                        }
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboSomenteGClientesComDoc = ComboSomenteGClientesComDoc;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.gc_comboSomenteGClientesComDoc;
                CachePersister.contextoModel.gc_comboSomenteGClientesComDoc = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboSomenteGClientesComDoc));
        }

        public static List<SelectListItem> LoadComboSomenteGFornecedores(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.gc_comboSomenteGFornecedores.Count == 0) || (LibDB.IsTableUpdate("g_clientes", "LoadComboSomenteGFornecedores", db) == true))
            {
                var ComboSomenteGFornecedores = new List<SelectListItem>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var ListaTemp = db.g_clientes.Select(c => new { c.id_cliente, c.is_cliente, c.is_fornecedor, c.ativo, c.nome, c.id_ciclo_faturamento, c.id_coligada, c.id_filial }).Where(p => (p.ativo == true) && (p.is_fornecedor == true)).OrderBy(p => p.nome).ToList();
                    foreach (var RecordTemp in ListaTemp)
                    {
                        String NomeCliente = RecordTemp.nome.EmptyIfNull().ToString().Trim();
                        if ((NomeCliente.Length > 50) && (_DisplayScreenWidth < 500)) { NomeCliente = NomeCliente.Substring(0, 50); };
                        ComboSomenteGFornecedores.Add(new SelectListItem { Value = RecordTemp.id_cliente.ToString(), Text = NomeCliente + "\xA0\xA0\xA0\xA0\xA0" + "[Id: " + RecordTemp.id_cliente.ToString() + "]" });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboSomenteGFornecedores = ComboSomenteGFornecedores;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.gc_comboSomenteGFornecedores;
                CachePersister.contextoModel.gc_comboSomenteGFornecedores = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboSomenteGFornecedores));
        }
        public static List<SelectListItem> LoadComboSomenteGFornecedoresComDoc(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.gc_comboSomenteGFornecedoresComDoc.Count == 0) || (LibDB.IsTableUpdate("g_clientes", "LoadComboSomenteGFornecedoresComDoc", db) == true))
            {
                var ComboSomenteGFornecedoresComDoc = new List<SelectListItem>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    String Documento = string.Empty;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var ListaTemp = db.g_clientes.Select(c => new { c.id_cliente, c.cpf, c.cnpj, c.is_cliente, c.is_fornecedor, c.ativo, c.nome, c.id_ciclo_faturamento, c.id_coligada, c.id_filial }).Where(p => (p.ativo == true) && (p.is_fornecedor == true)).OrderBy(p => p.nome).ToList();
                    foreach (var RecordTemp in ListaTemp)
                    {
                        if (RecordTemp.cpf.EmptyIfNull().ToString().Trim().Length > 0) { Documento = RecordTemp.cpf.Trim(); }
                        else if (RecordTemp.cnpj.EmptyIfNull().ToString().Trim().Length > 0) { Documento = RecordTemp.cnpj.Trim(); };
                        String NomeCliente = RecordTemp.nome.EmptyIfNull().ToString().Trim();
                        if ((NomeCliente.Length > 50) && (_DisplayScreenWidth < 500)) { NomeCliente = NomeCliente.Substring(0, 50); };
                        ComboSomenteGFornecedoresComDoc.Add(new SelectListItem { Value = RecordTemp.id_cliente.ToString(), Text = NomeCliente + "\xA0\xA0\xA0\xA0\xA0" + "[ " + Documento + " ]" });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboSomenteGFornecedoresComDoc = ComboSomenteGFornecedoresComDoc;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.gc_comboSomenteGFornecedoresComDoc;
                CachePersister.contextoModel.gc_comboSomenteGFornecedoresComDoc = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboSomenteGFornecedoresComDoc));
        }
        public static List<SelectListItem> LoadComboGcFinanceiroStatus(GdiPlataformEntities db)
        {
            // Financeiro Status
            if (CachePersister.contextoModel.gc_comboFinanceiroStatus.Count == 0)
            {
                var comboFinanceiroStatus = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_financeiro_status> listaDbFinanceiroStatus = db.gc_financeiro_status.Select(p => p).OrderBy(p => p.id_financeiro_status);
                    foreach (gc_financeiro_status itemFinanceiroStatus in listaDbFinanceiroStatus)
                    {
                        comboFinanceiroStatus.Add(new SelectListItem { Value = itemFinanceiroStatus.id_financeiro_status.ToString(), Text = itemFinanceiroStatus.nome.ToString() }); ;
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboFinanceiroStatus = comboFinanceiroStatus;
            }
            else
            {
                List<SelectListItem> ListaFinanceiroStatus = CachePersister.contextoModel.gc_comboFinanceiroStatus;
                CachePersister.contextoModel.gc_comboFinanceiroStatus = ListaFinanceiroStatus;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboFinanceiroStatus));
        }
        public static List<SelectListItem> LoadComboRowColors(GdiPlataformEntities db)
        {
            // Rows Colors
            if (CachePersister.contextoModel.a_comboRowsColors.Count == 0)
            {
                var comboRowColors = new List<SelectListItem>();
                IQueryable<a_tablesrows_colors> listaRowColors = db.a_tablesrows_colors.Where(c => c.controller == "gc.FinanceiroLancamentos").OrderBy(c => c.nome);
                comboRowColors.Add(new SelectListItem { Value = "0", Text = "Default" });
                foreach (a_tablesrows_colors item_a_tablesrows_colors in listaRowColors)
                {
                    comboRowColors.Add(new SelectListItem { Value = item_a_tablesrows_colors.id_tablerow_color.ToString(), Text = item_a_tablesrows_colors.nome.ToString() });
                }
                CachePersister.contextoModel.a_comboRowsColors = comboRowColors;
            }
            else
            {
                List<SelectListItem> ListaRowsColors = CachePersister.contextoModel.a_comboRowsColors;
                CachePersister.contextoModel.a_comboRowsColors = ListaRowsColors;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.a_comboRowsColors));
        }
        public static List<SelectListItem> LoadComboFiltroDebitoCredito(GdiPlataformEntities db)
        {
            if (CachePersister.contextoModel.gc_comboFiltroDebitoCredito.Count == 0)
            {
                var comboFiltroDebitoCredito = new List<SelectListItem>();
                try
                {
                    comboFiltroDebitoCredito.Add(new SelectListItem { Value = "0", Text = "DÉBITO / CRÉDITO" }); ;
                    comboFiltroDebitoCredito.Add(new SelectListItem { Value = "1", Text = "DÉBITO" }); ;
                    comboFiltroDebitoCredito.Add(new SelectListItem { Value = "2", Text = "CRÉDITO" }); ;
                }
                finally { }
                CachePersister.contextoModel.gc_comboFiltroDebitoCredito = comboFiltroDebitoCredito;
            }
            else
            {
                List<SelectListItem> ListaFinanceiroFiltroStatus = CachePersister.contextoModel.gc_comboFiltroDebitoCredito;
                CachePersister.contextoModel.gc_comboFiltroDebitoCredito = ListaFinanceiroFiltroStatus;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboFiltroDebitoCredito));
        }
        public static List<SelectListItem> LoadComboViewDebitoCredito(GdiPlataformEntities db)
        {
            // Financeiro Status
            if (CachePersister.contextoModel.g_comboDebitoCredito.Count == 0)
            {
                var g_comboDebitoCredito = new List<SelectListItem>();
                g_comboDebitoCredito.Add(new SelectListItem { Value = "1", Text = "Débito" });
                g_comboDebitoCredito.Add(new SelectListItem { Value = "2", Text = "Crédito" });
                CachePersister.contextoModel.g_comboDebitoCredito = g_comboDebitoCredito;
            }
            else
            {
                List<SelectListItem> ListaDebitoCredito = CachePersister.contextoModel.g_comboDebitoCredito;
                CachePersister.contextoModel.g_comboDebitoCredito = ListaDebitoCredito;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.g_comboDebitoCredito));
        }
        public static List<SelectListItem> LoadComboPagRecTiposTodos(GdiPlataformEntities db)
        {
            // PagRec Tipos
            if (CachePersister.contextoModel.gc_comboPagRecTiposTodos.Count == 0)
            {
                var ComboPagRecTiposTodos = new List<SelectListItem>();
                ComboPagRecTiposTodos.Add(new SelectListItem { Value = "0", Text = "-" });
                try
                {
                    IQueryable<g_pagrec_tipos> listaDbPagRecTipos = db.g_pagrec_tipos.Where(p => p.ativo == true).OrderBy(p => p.id_pagrec_tipo);
                    foreach (g_pagrec_tipos item6 in listaDbPagRecTipos)
                    {
                        ComboPagRecTiposTodos.Add(new SelectListItem { Value = item6.id_pagrec_tipo.ToString(), Text = item6.descricao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboPagRecTiposTodos = ComboPagRecTiposTodos;
            }
            else
            {
                List<SelectListItem> ListaPagRecTipos = CachePersister.contextoModel.gc_comboPagRecTiposTodos;
                CachePersister.contextoModel.gc_comboPagRecTiposTodos = ListaPagRecTipos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboPagRecTiposTodos));
        }

        public static List<SelectListItem> LoadComboPagRecTiposFaturaveis(GdiPlataformEntities db)
        {
            // PagRec Tipos
            if (CachePersister.contextoModel.gc_comboPagRecTiposFaturaveis.Count == 0)
            {
                var ComboPagRecTiposFaturaveis = new List<SelectListItem>();
                ComboPagRecTiposFaturaveis.Add(new SelectListItem { Value = "0", Text = "-" });
                try
                {
                    IQueryable<g_pagrec_tipos> listaDbPagRecTipos = db.g_pagrec_tipos.Where(p => p.ativo == true && p.id_pagrec_tipo != 5).OrderBy(p => p.id_pagrec_tipo);
                    foreach (g_pagrec_tipos item6 in listaDbPagRecTipos)
                    {
                        ComboPagRecTiposFaturaveis.Add(new SelectListItem { Value = item6.id_pagrec_tipo.ToString(), Text = item6.descricao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboPagRecTiposFaturaveis = ComboPagRecTiposFaturaveis;
            }
            else
            {
                List<SelectListItem> ListaPagRecTipos = CachePersister.contextoModel.gc_comboPagRecTiposFaturaveis;
                CachePersister.contextoModel.gc_comboPagRecTiposFaturaveis = ListaPagRecTipos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboPagRecTiposFaturaveis));
        }

        public static List<SelectListItem> LoadComboPagRecCondicoesTodas(GdiPlataformEntities db)
        {
            // PagRec Condições
            if (CachePersister.contextoModel.gc_comboPagRecCondicoesTodas.Count == 0)
            {
                var ComboPagRecCondicoesTodas = new List<SelectListItem>();
                try
                {
                    IQueryable<g_pagrec_condicoes> listaDbPagRecCondicoes = db.g_pagrec_condicoes.Select(p => p).Where(p => p.ativo).OrderBy(p => p.ordem);
                    foreach (g_pagrec_condicoes item5 in listaDbPagRecCondicoes)
                    {
                        ComboPagRecCondicoesTodas.Add(new SelectListItem { Value = item5.id_pagrec_condicao.ToString(), Text = item5.descricao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboPagRecCondicoesTodas = ComboPagRecCondicoesTodas;
            }
            else
            {
                List<SelectListItem> ListaPagRecCondicoes = CachePersister.contextoModel.gc_comboPagRecCondicoesTodas;
                CachePersister.contextoModel.gc_comboPagRecCondicoesTodas = ListaPagRecCondicoes;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboPagRecCondicoesTodas));
        }

        public static List<SelectListItem> LoadComboPagRecCondicoesFaturaveis(GdiPlataformEntities db)
        {
            // PagRec Condições
            if (CachePersister.contextoModel.gc_comboPagRecCondicoesFaturaveis.Count == 0)
            {
                var ComboPagRecCondicoesFaturaveis = new List<SelectListItem>();
                try
                {
                    IQueryable<g_pagrec_condicoes> listaDbPagRecCondicoes = db.g_pagrec_condicoes.Select(p => p).Where(p => p.ativo && p.id_pagrec_tipo != 5).OrderBy(p => p.ordem);
                    foreach (g_pagrec_condicoes item5 in listaDbPagRecCondicoes)
                    {
                        ComboPagRecCondicoesFaturaveis.Add(new SelectListItem { Value = item5.id_pagrec_condicao.ToString(), Text = item5.descricao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboPagRecCondicoesFaturaveis = ComboPagRecCondicoesFaturaveis;
            }
            else
            {
                List<SelectListItem> ListaPagRecCondicoes = CachePersister.contextoModel.gc_comboPagRecCondicoesFaturaveis;
                CachePersister.contextoModel.gc_comboPagRecCondicoesFaturaveis = ListaPagRecCondicoes;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboPagRecCondicoesFaturaveis));
        }

        public static List<SelectListItem> LoadComboFiltroFinanceiroStatus(GdiPlataformEntities db)
        {

            if (CachePersister.contextoModel.gc_comboFinanceiroFiltroStatus.Count == 0)
            {
                var comboFinanceiroFiltroStatus = new List<SelectListItem>();
                try
                {
                    comboFinanceiroFiltroStatus.Add(new SelectListItem { Value = "0", Text = "[ Todos ]" });
                    comboFinanceiroFiltroStatus.Add(new SelectListItem { Value = "3", Text = "[ Abertos ]" });
                    comboFinanceiroFiltroStatus.Add(new SelectListItem { Value = "1", Text = "[ Liquidados ]" });
                }
                finally { }
                CachePersister.contextoModel.gc_comboFinanceiroFiltroStatus = comboFinanceiroFiltroStatus;
            }
            else
            {
                List<SelectListItem> ListaFinanceiroFiltroStatus = CachePersister.contextoModel.gc_comboFinanceiroFiltroStatus;
                CachePersister.contextoModel.gc_comboFinanceiroFiltroStatus = ListaFinanceiroFiltroStatus;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboFinanceiroFiltroStatus));
        }
        public static List<SelectListItem> LoadComboGContasCaixas(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGContasCaixas(db);
        public static List<SelectListItem> LoadComboGContasCaixasGerencial(GdiPlataformEntities db)
        {
            // Contas Caixas
            if (CachePersister.contextoModel.gc_comboContasCaixasGerencial.Count == 0)
            {
                int IdUsuarioAtual = -1;
                int.TryParse(CachePersister.userIdentity.IdUsuario.ToString(), out IdUsuarioAtual);
                var comboContasCaixasGerencial = new List<SelectListItem>();

                g_contas_caixas_acessos ObjectTodasContasCaixas = db.g_contas_caixas_acessos.Where(a => a.id_conta_caixa == 999 && a.id_usuario == IdUsuarioAtual).FirstOrDefault();
                if (ObjectTodasContasCaixas != null)
                {
                    comboContasCaixasGerencial.Add(new SelectListItem { Value = "999", Text = "[ TODAS ]" });
                }
                try
                {
                    var listaDbContasCaixas = new List<Db.g_contas_caixas>();
                    String SentencaSQL = string.Empty;
                    SentencaSQL += "select c.* from g_contas_caixas c ";
                    SentencaSQL += "     left join g_contas_caixas_acessos a on (c.id_conta_caixa = a.id_conta_caixa) ";
                    SentencaSQL += "     where (c.ativo = 1) and (c.is_gerencial = 1) and (a.id_usuario = " + CachePersister.userIdentity.IdUsuario.ToString() + ") order by c.ordem";
                    listaDbContasCaixas = db.g_contas_caixas.SqlQuery(SentencaSQL).ToList();
                    if (listaDbContasCaixas.Count > 0)
                    {
                        foreach (g_contas_caixas itemContaCaixa in listaDbContasCaixas)
                        {
                            comboContasCaixasGerencial.Add(new SelectListItem { Value = itemContaCaixa.id_conta_caixa.ToString(), Text = itemContaCaixa.nome.ToString() });
                        }
                    }
                    else
                    {
                        comboContasCaixasGerencial.Add(new SelectListItem { Value = "0", Text = "CONTA CAIXA INTERNA" }); ;
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboContasCaixasGerencial = comboContasCaixasGerencial;
            }
            else
            {
                List<SelectListItem> ListaContasCaixasGerencial = CachePersister.contextoModel.gc_comboContasCaixasGerencial;
                CachePersister.contextoModel.gc_comboContasCaixasGerencial = ListaContasCaixasGerencial;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboContasCaixasGerencial));
        }
        public static List<SelectListItem> LoadComboGContratosTipos(GdiPlataformEntities db)
        {
            // ContratosTipos
            if (CachePersister.contextoModel.g_comboContratosTipos.Count == 0)
            {
                var comboContratosTipos = new List<SelectListItem>();
                try
                {
                    IQueryable<g_contratos_aviacao_tipos> listaContratosTipos = db.g_contratos_aviacao_tipos.Where(p => p.ativo == true).OrderByDescending(p => p.id_contrato_tipo);
                    foreach (g_contratos_aviacao_tipos ItemContrato in listaContratosTipos)
                    {
                        comboContratosTipos.Add(new SelectListItem { Value = ItemContrato.id_contrato_tipo.ToString(), Text = ItemContrato.descricao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.g_comboContratosTipos = comboContratosTipos;
            }
            else
            {
                List<SelectListItem> ListaContratosTipos = CachePersister.contextoModel.g_comboContratosTipos;
                CachePersister.contextoModel.g_comboContratosTipos = ListaContratosTipos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.g_comboContratosTipos));
        }
        public static List<SelectListItem> LoadComboGcMovimentosPosicao(GdiPlataformEntities db)
            => LookupQueryServiceAccessor.Current.GetComboGcMovimentosPosicao(db);
        public static List<SelectListItem> LoadComboGcCfopFinalidade(GdiPlataformEntities db)
        {
            // Entregas Prazos
            if (CachePersister.contextoModel.gc_comboGcCfopFinalidade.Count == 0)
            {
                var comboGcCfopFinalidade = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_cfop_finalidade> listaCfopFinalidade = null;
                    listaCfopFinalidade = db.gc_cfop_finalidade.Select(f => f).Where(f => f.ativo == true).OrderBy(f => f.finalidade);
                    comboGcCfopFinalidade.Add(new SelectListItem { Value = "-1", Text = "Selecionar" });
                    foreach (gc_cfop_finalidade itemCfopFinalidade in listaCfopFinalidade)
                    {
                        comboGcCfopFinalidade.Add(new SelectListItem { Value = itemCfopFinalidade.id_cfop_finalidade.ToString(), Text = itemCfopFinalidade.finalidade.EmptyIfNull().ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGcCfopFinalidade = comboGcCfopFinalidade;
            }
            else
            {
                List<SelectListItem> listaCfopFinalidade = CachePersister.contextoModel.gc_comboGcCfopFinalidade;
                CachePersister.contextoModel.gc_comboGcCfopFinalidade = listaCfopFinalidade;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcCfopFinalidade));
        }

        public static List<SelectListItem> LoadComboGcComexImportacoesTodas(GdiPlataformEntities db)
        {
            // Produtos Condições
            if ((CachePersister.contextoModel.gc_comboGcComexImportacoesTodas.Count == 0) || (LibDB.IsTableUpdate("gc_comex_importacoes", "LoadComboGcComexImportacoesTodas", db) == true))
            {
                var comboComexImportacoesTodas = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_comex_importacoes> listaDbComexImportacoesTodas = db.gc_comex_importacoes.Select(p => p).Where(c => c.ativo == true).OrderByDescending(c => c.id_importacao);
                    foreach (gc_comex_importacoes itemComexImportacoesTodas in listaDbComexImportacoesTodas)
                    {
                        comboComexImportacoesTodas.Add(new SelectListItem { Value = itemComexImportacoesTodas.id_importacao.ToString(), Text = itemComexImportacoesTodas.numero.EmptyIfNull().ToString().Trim() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGcComexImportacoesTodas = comboComexImportacoesTodas;
            }
            else
            {
                List<SelectListItem> listaDbComexImportacoesTodas = CachePersister.contextoModel.gc_comboGcComexImportacoesTodas;
                CachePersister.contextoModel.gc_comboGcComexImportacoesTodas = listaDbComexImportacoesTodas;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcComexImportacoesTodas));
        }

        public static List<SelectListItem> LoadComboGcComexImportacoesAtivas(GdiPlataformEntities db)
        {
            // Produtos Condições
            if ((CachePersister.contextoModel.gc_comboGcComexImportacoesAtivas.Count == 0) || (LibDB.IsTableUpdate("gc_comex_importacoes", "LoadComboGcComexImportacoesAtivas", db) == true))
            {
                var comboComexImportacoesAtivas = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_comex_importacoes> listaDbComexImportacoesAtivas = db.gc_comex_importacoes.Select(p => p).Where(c => c.ativo == true && c.invoices_finalizadas == false).OrderByDescending(c => c.id_importacao);
                    foreach (gc_comex_importacoes itemComexImportacoesAtivas in listaDbComexImportacoesAtivas)
                    {
                        comboComexImportacoesAtivas.Add(new SelectListItem { Value = itemComexImportacoesAtivas.id_importacao.ToString(), Text = itemComexImportacoesAtivas.numero.EmptyIfNull().ToString().Trim() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGcComexImportacoesAtivas = comboComexImportacoesAtivas;
            }
            else
            {
                List<SelectListItem> listaDbComexImportacoesAtivas = CachePersister.contextoModel.gc_comboGcComexImportacoesAtivas;
                CachePersister.contextoModel.gc_comboGcComexImportacoesAtivas = listaDbComexImportacoesAtivas;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcComexImportacoesAtivas));
        }


        public static List<SelectListItem> LoadComboGcProdutosFamilia(GdiPlataformEntities db)
        {
            if (CachePersister.contextoModel.gc_comboProdutosFamilia.Count == 0)
            {
                var comboProdutosFamilia = new List<SelectListItem>();
                try
                {
                    var listaDbProdutosFamilia = db.g_produtos_familia.Select(p => new { p.id_produto_familia, p.ativo, p.descricao }).Where(p => p.ativo == true).ToList();
                    comboProdutosFamilia.Add(new SelectListItem { Value = "-1", Text = "" });
                    foreach (var item_g_produtos_familia in listaDbProdutosFamilia)
                    {
                        comboProdutosFamilia.Add(new SelectListItem { Value = item_g_produtos_familia.id_produto_familia.EmptyIfNull().ToString(), Text = item_g_produtos_familia.descricao });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboProdutosFamilia = comboProdutosFamilia;
            }
            else
            {
                List<SelectListItem> ListaProdutosFamilia = CachePersister.contextoModel.gc_comboProdutosFamilia;
                CachePersister.contextoModel.gc_comboProdutosFamilia = ListaProdutosFamilia;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboProdutosFamilia));
        }

        public static List<SelectListItem> LoadComboGcProdutosStatus(GdiPlataformEntities db)
        {
            if (CachePersister.contextoModel.gc_comboProdutosStatus.Count == 0)
            {
                var comboProdutosStatus = new List<SelectListItem>();
                try
                {
                    var listaDbProdutosStatus = db.g_produtos_status.Select(p => new { p.id_produto_status, p.ativo, p.descricao }).Where(p => p.ativo == true).ToList();
                    comboProdutosStatus.Add(new SelectListItem { Value = "-1", Text = "" });
                    foreach (var item_g_produtos_status in listaDbProdutosStatus)
                    {
                        comboProdutosStatus.Add(new SelectListItem { Value = item_g_produtos_status.id_produto_status.EmptyIfNull().ToString(), Text = item_g_produtos_status.descricao });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboProdutosStatus = comboProdutosStatus;
            }
            else
            {
                List<SelectListItem> ListaProdutosStatus = CachePersister.contextoModel.gc_comboProdutosStatus;
                CachePersister.contextoModel.gc_comboProdutosStatus = ListaProdutosStatus;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboProdutosStatus));
        }

        public static List<SelectListItem> LoadComboGcComexProdutosComID(GdiPlataformEntities db)
        {
            int _DisplayScreenWidth = 0;
            int _SizeNomeItem = 100;
            if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 500)) { _SizeNomeItem = 50; }
            if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 400)) { _SizeNomeItem = 40; }
            if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 300)) { _SizeNomeItem = 30; }

            int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
            // Produtos Condições
            if ((CachePersister.contextoModel.gc_comboGcComexProdutosComId.Count == 0) || (LibDB.IsTableUpdate("gc_comex_produtos", "LoadComboGcComexProdutosComID", db) == true))
            {
                var comboGcComexProdutosComId = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_comex_produtos> listaDbComexProdutosComId = db.gc_comex_produtos.Select(p => p).Where(c => c.ativo == true).OrderByDescending(c => c.id_produto);
                    foreach (gc_comex_produtos ItemComexProduto in listaDbComexProdutosComId)
                    {
                        String NomeProduto = "[Id: " + ItemComexProduto.id_comex_produto.EmptyIfNull().ToString() + "] ";
                        if (ItemComexProduto.traducao.EmptyIfNull().ToString().Trim().Length > 0)  { NomeProduto += ItemComexProduto.traducao.EmptyIfNull().ToString().Trim(); }
                        else { NomeProduto += ItemComexProduto.description.EmptyIfNull().ToString().Trim(); };
                        if (NomeProduto.Length > _SizeNomeItem) { NomeProduto = NomeProduto.Substring(0, _SizeNomeItem) + "..."; };
                        comboGcComexProdutosComId.Add(new SelectListItem { Value = ItemComexProduto.id_comex_produto.ToString(), Text = NomeProduto });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGcComexProdutosComId = comboGcComexProdutosComId;
            }
            else
            {
                List<SelectListItem> listaDbComexProdutosComId = CachePersister.contextoModel.gc_comboGcComexProdutosComId;
                CachePersister.contextoModel.gc_comboGcComexProdutosComId = listaDbComexProdutosComId;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcComexProdutosComId));
        }


        public static List<SelectListItem> LoadComboGcEstoqueEnderecoArea(GdiPlataformEntities db, int IdLocalEstoque)
            => LookupQueryServiceAccessor.Current.GetComboGcEstoqueEnderecoArea(db, IdLocalEstoque);

        public static List<SelectListItem> LoadComboGcEstoqueEnderecoSecao(GdiPlataformEntities db, int IdLocalEstoque)
            => LookupQueryServiceAccessor.Current.GetComboGcEstoqueEnderecoSecao(db, IdLocalEstoque);

        public static List<SelectListItem> LoadComboGcEstoqueEnderecoCorredor(GdiPlataformEntities db, int IdLocalEstoque)
            => LookupQueryServiceAccessor.Current.GetComboGcEstoqueEnderecoCorredor(db, IdLocalEstoque);

        public static List<SelectListItem> LoadComboGcEstoqueEnderecoPrateleira(GdiPlataformEntities db, int IdLocalEstoque)
            => LookupQueryServiceAccessor.Current.GetComboGcEstoqueEnderecoPrateleira(db, IdLocalEstoque);

        public static List<SelectListItem> LoadComboGedArquivosTipos(GdiPlataformEntities db, int IdTipo, int IdTipoPai)
            => LookupQueryServiceAccessor.Current.GetComboGedArquivosTipos(db, IdTipo, IdTipoPai);

        public static List<SelectListItem> LoadComboGClassificacaoFinanceira(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.g_comboGClassificacaoFinanceira.Count == 0) || (LibDB.IsTableUpdate("g_classificacao_financeira", "LoadComboGClassificacaoFinanceira", db) == true))
            {
                var ComboGClassificacaoFinanceira = new List<SelectListItem>();
                ComboGClassificacaoFinanceira.Add(new SelectListItem { Value = "0", Text = "[ Informe a Classificação Financeira ]" });
                try
                {
                    List<g_classificacao_financeira> ListaClassificacaoFinanceira = db.g_classificacao_financeira.Where(c => c.consolidador == false).OrderBy(c => c.descricao_resumida).ToList();
                    foreach (var RecordClassificaoFinanceira in ListaClassificacaoFinanceira)
                    {
                        ComboGClassificacaoFinanceira.Add(new SelectListItem { Value = RecordClassificaoFinanceira.id_classificacao_financeira.EmptyIfNull().ToString(), Text = RecordClassificaoFinanceira.descricao_resumida.EmptyIfNull().ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.g_comboGClassificacaoFinanceira = ComboGClassificacaoFinanceira;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.g_comboGClassificacaoFinanceira;
                CachePersister.contextoModel.g_comboGClassificacaoFinanceira = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.g_comboGClassificacaoFinanceira));
        }

        public static List<SelectListItem> LoadComboGUsuariosAtendimentoResponsavel(GdiPlataformEntities db)
        {
            // Usuarios
            var ComboGUsuarios = new List<SelectListItem>();
            ComboGUsuarios.Add(new SelectListItem { Value = "0", Text = "[ Operador ]" });
            try
            {
                List<g_usuarios> ListaUsuarios = db.g_usuarios.Where(u => u.ativo == true && u.id_departamento > 0).OrderBy(u => u.nome).ToList();
                foreach (var RecordUsuarios in ListaUsuarios)
                {
                    ComboGUsuarios.Add(new SelectListItem { Value = RecordUsuarios.id_usuario.EmptyIfNull().ToString(), Text = RecordUsuarios.nome.EmptyIfNull().ToString() });
                }
            }
            finally { }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(ComboGUsuarios));
        }

        public static List<SelectListItem> LoadComboGUsuariosAtendimentoSolicitante(GdiPlataformEntities db)
        {
            // Usuarios
            var ComboGUsuarios = new List<SelectListItem>();
            ComboGUsuarios.Add(new SelectListItem { Value = "0", Text = "[ Solicitante ]" });
            try
            {
                List<g_usuarios> ListaUsuarios = db.g_usuarios.Where(u => u.ativo == true && u.id_departamento > 0).OrderBy(u => u.nome).ToList();
                foreach (var RecordUsuarios in ListaUsuarios)
                {
                    ComboGUsuarios.Add(new SelectListItem { Value = RecordUsuarios.id_usuario.EmptyIfNull().ToString(), Text = RecordUsuarios.nome.EmptyIfNull().ToString() });
                }
            }
            finally { }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(ComboGUsuarios));
        }


        public static List<SelectListItem> LoadComboGDepartamentos(GdiPlataformEntities db)
        {
            // Departamentos
            if ((CachePersister.contextoModel.g_comboDepartamentos.Count == 0) || (LibDB.IsTableUpdate("g_departamentos", "LoadComboGDepartamentos", db) == true))
            {
                var ComboGDepartamentos = new List<SelectListItem>();
                ComboGDepartamentos.Add(new SelectListItem { Value = "0", Text = "[ Departamento ]" });
                try
                {
                    List<g_departamentos> ListaGDepartamentos = new List<g_departamentos>();

                    if ((CachePersister.userIdentity.Roles.Contains("g_Atendimentos_*")) || (CachePersister.userIdentity.Roles.Contains("g_Atendimentos_Actionmanager")))
                    {
                        ListaGDepartamentos = db.g_departamentos.Where(d => d.ativo == true).OrderBy(d => d.nome).ToList();
                    }
                    else
                    {
                        ListaGDepartamentos = db.g_departamentos.Where(d => d.ativo == true && d.id_departamento != 8).OrderBy(d => d.nome).ToList();
                    }
                    foreach (var RecordDepartamento in ListaGDepartamentos)
                    {
                        ComboGDepartamentos.Add(new SelectListItem { Value = RecordDepartamento.id_departamento.EmptyIfNull().ToString(), Text = RecordDepartamento.nome.EmptyIfNull().ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.g_comboDepartamentos = ComboGDepartamentos;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.g_comboDepartamentos;
                CachePersister.contextoModel.g_comboDepartamentos = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.g_comboDepartamentos));
        }

        public static List<SelectListItem> LoadComboGAtendimentosStatus(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.g_comboAtendimentosStatus.Count == 0) || (LibDB.IsTableUpdate("g_atendimentos_status", "LoadComboGAtendimentosStatus", db) == true))
            {
                var ComboGAtendimentosStatus = new List<SelectListItem>();
                ComboGAtendimentosStatus.Add(new SelectListItem { Value = "0", Text = "[ Pendentes ]" });
                try
                {
                    List<g_atendimentos_status> ListaGAtendimentosStatus = db.g_atendimentos_status.Where(d => d.ativo == true).OrderBy(d => d.nome).ToList();
                    foreach (var RecordStatus in ListaGAtendimentosStatus)
                    {
                        ComboGAtendimentosStatus.Add(new SelectListItem { Value = RecordStatus.id_status.EmptyIfNull().ToString(), Text = RecordStatus.nome.EmptyIfNull().ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.g_comboAtendimentosStatus = ComboGAtendimentosStatus;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.g_comboAtendimentosStatus;
                CachePersister.contextoModel.g_comboAtendimentosStatus = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.g_comboAtendimentosStatus));
        }

        public static List<SelectListItem> LoadComboGAtendimentosCategorias(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.g_comboAtendimentosCategorias.Count == 0) || (LibDB.IsTableUpdate("g_atendimentos_categorias", "LoadComboGAtendimentosCategorias", db) == true))
            {
                var ComboGAtendimentosCategorias = new List<SelectListItem>();
                ComboGAtendimentosCategorias.Add(new SelectListItem { Value = "0", Text = "[ Categorias ]" });
                try
                {
                    List<g_atendimentos_categorias> ListaGAtendimentosCategorias = db.g_atendimentos_categorias.Where(d => d.ativo == true).OrderBy(d => d.nome).ToList();
                    foreach (var RecordCategorias in ListaGAtendimentosCategorias)
                    {
                        ComboGAtendimentosCategorias.Add(new SelectListItem { Value = RecordCategorias.id_atendimento_categoria.EmptyIfNull().ToString(), Text = RecordCategorias.nome.EmptyIfNull().ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.g_comboAtendimentosCategorias = ComboGAtendimentosCategorias;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.g_comboAtendimentosCategorias;
                CachePersister.contextoModel.g_comboAtendimentosCategorias = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.g_comboAtendimentosCategorias));
        }



    }
}
