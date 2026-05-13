using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Security;

namespace GdiPlataform.Lib
{
    public static class LibDataSets
    {
        public static List<SelectListItem> LoadComboGcClientesDestinatarios(GdiPlataformEntities db, int IdCliente)
        {
            var comboDestinatarios = new List<SelectListItem>();
            try
            {
                IQueryable<g_clientes_destinatarios> listaDestinatarios = null;
                comboDestinatarios.Add(new SelectListItem { Value = "0", Text = "[ O PRÓPRIO CLIENTE ]" });
                listaDestinatarios = db.g_clientes_destinatarios.Select(c => c).Where(c => c.id_cliente == IdCliente && c.ativo == true).OrderBy(c => c.nome);
                foreach (g_clientes_destinatarios Record in listaDestinatarios)
                {
                    comboDestinatarios.Add(new SelectListItem { Value = Record.id_cliente_destinatario.ToString(), Text = Record.nome.ToString() });
                }
            }
            finally { }
            CachePersister.contextoModel.gc_comboGcClientesDestinatarios = comboDestinatarios;
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcClientesDestinatarios));
        }

        public static List<g_clientes_destinatarios> LoadDatasetGcClientesDestinatarios(int IdCliente, GdiPlataformEntities db)
        {
            CachePersister.contextoModel.gc_dataSetClientesDestinatarios.Clear();
            try
            {
                CachePersister.contextoModel.gc_dataSetClientesDestinatarios.Clear();
                g_clientes_destinatarios RecordDestinatarioPadrao = new g_clientes_destinatarios();
                RecordDestinatarioPadrao.id_cliente_destinatario = 0;
                RecordDestinatarioPadrao.nome = "O PRÓPRIO CLIENTE";
                if (CachePersister.contextoModel.gc_dataSetClientesDestinatarios.IndexOf(RecordDestinatarioPadrao) == -1)
                {
                    CachePersister.contextoModel.gc_dataSetClientesDestinatarios.Add(RecordDestinatarioPadrao);
                }
                var listaDbClientesDestinatarios = db.g_clientes_destinatarios.Where(p => (p.ativo == true && p.id_cliente == IdCliente)).ToList();
                foreach (var Record in listaDbClientesDestinatarios)
                {
                    CachePersister.contextoModel.gc_dataSetClientesDestinatarios.Add(Record);
                }
            }
            finally { }
            return JsonConvert.DeserializeObject<List<g_clientes_destinatarios>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_dataSetClientesDestinatarios));
        }

        public static List<SelectListItem> LoadComboGcTransportadora(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.gc_comboGcTransportadora.Count == 0) || (LibDB.IsTableUpdate("g_clientes", "LoadComboGcTransportadora", db) == true))
            {
                var comboTransportadora = new List<SelectListItem>();
                comboTransportadora.Add(new SelectListItem { Value = "0", Text = "[ CLIENTE RETIRA ]" });
                try
                {
                    IQueryable<g_clientes> listaTransportadora = null;
                    listaTransportadora = db.g_clientes.Select(c => c).Where(c => c.param_gc_transportadora == true).OrderBy(c => c.nome);
                    foreach (g_clientes Record in listaTransportadora)
                    {
                        comboTransportadora.Add(new SelectListItem { Value = Record.id_cliente.ToString(), Text = Record.nome.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGcTransportadora = comboTransportadora;
            }
            else
            {
                List<SelectListItem> ListaTransportadora = CachePersister.contextoModel.gc_comboGcTransportadora;
                CachePersister.contextoModel.gc_comboGcTransportadora = ListaTransportadora;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcTransportadora));
        }

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
        {
            if ((CachePersister.contextoModel.gc_comboGcCfop.Count == 0) || (LibDB.IsTableUpdate("gc_cfop", "LoadComboGcCfop", db) == true))
            {
                var comboCFOP = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_cfop> listaCFOP = null;
                    listaCFOP = db.gc_cfop.Where(p => p.ativo == true).OrderBy(p => p.numero);
                    foreach (gc_cfop ItemCfop in listaCFOP)
                    {
                        String descricaoCFOP = ItemCfop.numero.ToString() + "  -  " + ItemCfop.descricao.ToString().Trim();
                        comboCFOP.Add(new SelectListItem { Value = ItemCfop.id_cfop.ToString(), Text = descricaoCFOP });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGcCfop = comboCFOP;
            }
            else
            {
                List<SelectListItem> listaComboCFOP = CachePersister.contextoModel.gc_comboGcCfop;
                CachePersister.contextoModel.gc_comboGcCfop = listaComboCFOP;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcCfop));
        }
        public static List<SelectListItem> LoadComboGcCfopOperacoesFaturamentoPedido(GdiPlataformEntities db, int IdCfopOperacao)
        {
            var ComboCfopOperacoes = new List<SelectListItem>();
            try
            {
                gc_cfop_operacoes RecordCfopOperacoes = db.gc_cfop_operacoes.Find(IdCfopOperacao);
                if (RecordCfopOperacoes != null) { ComboCfopOperacoes.Add(new SelectListItem { Value = RecordCfopOperacoes.id_cfop_operacao.EmptyIfNull().ToString(), Text = RecordCfopOperacoes.descricao_erp.EmptyIfNull().ToString() }); };

                gc_cfop_operacoes RecordCfopOperacaoVinculada = db.gc_cfop_operacoes.Where(o => o.id_operacao_predecessora == IdCfopOperacao).FirstOrDefault();
                if (RecordCfopOperacaoVinculada != null) { ComboCfopOperacoes.Add(new SelectListItem { Value = RecordCfopOperacaoVinculada.id_cfop_operacao.EmptyIfNull().ToString(), Text = RecordCfopOperacaoVinculada.descricao_erp.EmptyIfNull().ToString() }); };
            }
            catch (Exception){}
            finally { }
            CachePersister.contextoModel.gc_comboGcCfopOperacoes = ComboCfopOperacoes;
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcCfopOperacoes));
        }

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
        {
            if ((CachePersister.contextoModel.gc_comboFreteResponsavel.Count == 0) || (LibDB.IsTableUpdate("gc_frete_responsavel", "LoadComboGcFreteResponsavel", db) == true))
            {
                var comboFreteResponsavel = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_frete_responsavel> listaFreteResponsavel = null;
                    listaFreteResponsavel = db.gc_frete_responsavel.Select(p => p).Where(p => p.ativo == true).OrderBy(p => p.descricao);
                    foreach (gc_frete_responsavel Record in listaFreteResponsavel)
                    {
                        comboFreteResponsavel.Add(new SelectListItem { Value = Record.id_frete_responsavel.ToString(), Text = Record.descricao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboFreteResponsavel = comboFreteResponsavel;
            }
            else
            {
                List<SelectListItem> ListaFreteResponsavel = CachePersister.contextoModel.gc_comboFreteResponsavel;
                CachePersister.contextoModel.gc_comboFreteResponsavel = ListaFreteResponsavel;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboFreteResponsavel));
        }
        public static List<SelectListItem> LoadComboGcEntregasPrazos(GdiPlataformEntities db)
        {
            // Entregas Prazos
            if ((CachePersister.contextoModel.gc_comboEntregasPrazos.Count == 0) || (LibDB.IsTableUpdate("gc_entregas_prazos", "LoadComboGcEntregasPrazos", db) == true))
            {
                var comboEntregasPrazos = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_entregas_prazos> listaDbEntregasPrazos = db.gc_entregas_prazos.Select(p => p).OrderBy(p => p.id_entrega_prazo);
                    foreach (gc_entregas_prazos Record in listaDbEntregasPrazos)
                    {
                        comboEntregasPrazos.Add(new SelectListItem { Value = Record.id_entrega_prazo.ToString(), Text = Record.sigla.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboEntregasPrazos = comboEntregasPrazos;
            }
            else
            {
                List<SelectListItem> ListaEntregasPrazos = CachePersister.contextoModel.gc_comboEntregasPrazos;
                CachePersister.contextoModel.gc_comboEntregasPrazos = ListaEntregasPrazos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboEntregasPrazos));
        }
        public static List<SelectListItem> LoadComboGProdutoCondicao(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.gc_comboProdutosCondicoes.Count == 0) || (LibDB.IsTableUpdate("g_produtos_condicoes", "LoadComboGProdutoCondicao", db) == true))
            {
                var comboProdutosCondicoes = new List<SelectListItem>();
                try
                {
                    IQueryable<g_produtos_condicoes> listaDbProdutosCondicoes = db.g_produtos_condicoes.Select(p => p).OrderBy(p => p.id_produto_condicao);
                    foreach (g_produtos_condicoes Record in listaDbProdutosCondicoes)
                    {
                        comboProdutosCondicoes.Add(new SelectListItem { Value = Record.id_produto_condicao.ToString(), Text = Record.sigla.ToString().Trim() + " - " + Record.descricao.ToString().Trim() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboProdutosCondicoes = comboProdutosCondicoes;
            }
            else
            {
                List<SelectListItem> ListaProdutosCondicoes = CachePersister.contextoModel.gc_comboProdutosCondicoes;
                CachePersister.contextoModel.gc_comboProdutosCondicoes = ListaProdutosCondicoes;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboProdutosCondicoes));
        }

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
        {
            // Vendedores
            if ((CachePersister.contextoModel.gc_comboVendedores.Count == 0) || (LibDB.IsTableUpdate("g_vendedores", "LoadComboGVendedores", db) == true))
            {
                var comboVendedores = new List<SelectListItem>();
                try
                {
                    IQueryable<g_vendedores> listaDbVendedores = db.g_vendedores.Select(p => p).Where(p => p.ativo == true).OrderBy(p => p.nome);
                    comboVendedores.Add(new SelectListItem { Value = "-1", Text = "[ Selecionar ]" });
                    foreach (g_vendedores item3 in listaDbVendedores)
                    {
                        comboVendedores.Add(new SelectListItem { Value = item3.id_vendedor.ToString(), Text = item3.nome.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboVendedores = comboVendedores;
            }
            else
            {
                List<SelectListItem> ListaVendedores = CachePersister.contextoModel.gc_comboVendedores;
                CachePersister.contextoModel.gc_comboVendedores = ListaVendedores;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboVendedores));
        }
        public static List<g_vendedores> LoadDatasetGVendedores(GdiPlataformEntities db)
        {
            // Vendedores
            if ((CachePersister.contextoModel.gc_dataSetVendedores.Count == 0) || (LibDB.IsTableUpdate("g_vendedores", "LoadDatasetGVendedores", db) == true))
            {
                var dataSetVendedores = new List<g_vendedores>();
                try
                {
                    IQueryable<g_vendedores> listaDbVendedores = db.g_vendedores.Select(p => p).Where(p => p.ativo == true).OrderBy(p => p.nome);
                    foreach (g_vendedores item3 in listaDbVendedores)
                    {
                        dataSetVendedores.Add(item3);
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_dataSetVendedores = dataSetVendedores;
            }
            else
            {
                List<g_vendedores> DatasetVendedores = CachePersister.contextoModel.gc_dataSetVendedores;
                CachePersister.contextoModel.gc_dataSetVendedores = DatasetVendedores;
            }
            return CachePersister.contextoModel.gc_dataSetVendedores;
        }
        public static List<SelectListItem> LoadComboGcClientesContatos(GdiPlataformEntities db, int IdCliente)
        {
            // Clientes/Contatos
            var ComboClientesContatos = new List<SelectListItem>();
            ComboClientesContatos.Add(new SelectListItem { Value = "0", Text = "[ INFORME A PESSOA DE CONTATO ]" });
            try
            {
                var listaDbClientesContatos = db.g_clientes_contatos.Select(p => new { p.id_contato, p.ativo, p.id_cliente, p.contato, p.telefone, p.email }).Where(p => (p.ativo == true && p.id_cliente == IdCliente)).ToList();
                foreach (var item_g_clientes_contatos in listaDbClientesContatos)
                {
                    ComboClientesContatos.Add(new SelectListItem { Value = item_g_clientes_contatos.id_contato.ToString(), Text = item_g_clientes_contatos.contato.ToString() });
                }
            }
            finally { }
            CachePersister.contextoModel.gc_comboClientesContatos = ComboClientesContatos;
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboClientesContatos));
        }

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

        public static List<cstDatasetClientesContatos> LoadDatasetGcClientesContatos(GdiPlataformEntities db)
        {
            if ((CachePersister.contextoModel.gc_dataSetClientesContatos.Count == 0) || (LibDB.IsTableUpdate("g_clientes_contatos", "LoadDatasetGcClientesContatos", db) == true))
            {
                var dataSetClientesContatos = new List<cstDatasetClientesContatos>();
                try
                {
                    var listaDbClientesContatos = db.g_clientes_contatos.Select(p => new { p.id_contato, p.ativo, p.id_cliente, p.contato, p.telefone, p.email }).Where(p => (p.ativo == true)).ToList();
                    foreach (var item_g_clientes_contatos in listaDbClientesContatos)
                    {
                        cstDatasetClientesContatos record_cstDatasetClientesContatos = new cstDatasetClientesContatos();
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
                List<cstDatasetClientesContatos> ListaDataSetClientesContatos = CachePersister.contextoModel.gc_dataSetClientesContatos;
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
        {
            if ((CachePersister.contextoModel.gc_comboProdutosServicosTodos.Count == 0) || (LibDB.IsTableUpdate("g_produtos", "LoadComboGcProdutosServicosTodos", db) == true))
            {
                var comboProdutosServicos = new List<SelectListItem>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    int _SizeNomeItem = 100;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var listaDbProdutosServicos = db.g_produtos.Select(p => new { p.id_produto, p.codigo, p.nome, p.preco_venda, p.has_corecharge, p.ativo, p.importado }).Where(p => p.ativo == true).ToList();
                    comboProdutosServicos.Add(new SelectListItem { Value = "-1", Text = "" });
                    _SizeNomeItem = (_DisplayScreenWidth / 100) * 8;
                    if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 500)) { _SizeNomeItem = (_DisplayScreenWidth / 100 * 10); }
                    foreach (var item_g_produtos in listaDbProdutosServicos)
                    {
                        String IdProduto = item_g_produtos.id_produto.EmptyIfNull().ToString().Trim();
                        String NomeProduto = item_g_produtos.nome.EmptyIfNull().ToString().Trim();
                        if (NomeProduto.Length > _SizeNomeItem) { NomeProduto = NomeProduto.Substring(0, _SizeNomeItem) + "..."; };
                        if (NomeProduto.EmptyIfNull().ToString().Trim().Length > 0)
                        {
                            comboProdutosServicos.Add(new SelectListItem { Value = IdProduto, Text = NomeProduto });
                        }
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboProdutosServicosTodos = comboProdutosServicos;
            }
            else
            {
                List<SelectListItem> ListaProdutosServicos = CachePersister.contextoModel.gc_comboProdutosServicosTodos;
                CachePersister.contextoModel.gc_comboProdutosServicosTodos = ListaProdutosServicos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboProdutosServicosTodos));
        }

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
        {
            if ((CachePersister.contextoModel.gc_comboProdutosServicosImportados.Count == 0) || (LibDB.IsTableUpdate("g_produtos", "LoadComboGcProdutosServicosImportados", db) == true))
            {
                var comboProdutosServicos = new List<SelectListItem>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    int _SizeNomeItem = 100;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var listaDbProdutosServicos = db.g_produtos.Select(p => new { p.id_produto, p.codigo, p.nome, p.preco_venda, p.has_corecharge, p.ativo, p.importado }).Where(p => p.ativo == true && p.importado == true).ToList();
                    comboProdutosServicos.Add(new SelectListItem { Value = "-1", Text = "" });
                    _SizeNomeItem = (_DisplayScreenWidth / 100) * 8;
                    if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 500)) { _SizeNomeItem = (_DisplayScreenWidth / 100 * 10); }
                    foreach (var item_g_produtos in listaDbProdutosServicos)
                    {
                        String IdProduto = item_g_produtos.id_produto.EmptyIfNull().ToString().Trim();
                        String NomeProduto = item_g_produtos.nome.EmptyIfNull().ToString().Trim();
                        if (NomeProduto.Length > _SizeNomeItem) { NomeProduto = NomeProduto.Substring(0, _SizeNomeItem) + "..."; };
                        if (NomeProduto.EmptyIfNull().ToString().Trim().Length > 0)
                        {
                            comboProdutosServicos.Add(new SelectListItem { Value = IdProduto, Text = NomeProduto });
                        }
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboProdutosServicosImportados = comboProdutosServicos;
            }
            else
            {
                List<SelectListItem> ListaProdutosServicos = CachePersister.contextoModel.gc_comboProdutosServicosImportados;
                CachePersister.contextoModel.gc_comboProdutosServicosImportados = ListaProdutosServicos;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboProdutosServicosImportados));
        }

        public static List<cstDatasetProdutosServicos> LoadDatasetGcProdutosServicos(GdiPlataformEntities db)
        {
            // Produtos/Serviços
            if ((CachePersister.contextoModel.gc_dataSetProdutosServicos.Count == 0) || (LibDB.IsTableUpdate("g_produtos", "LoadDatasetGcProdutosServicos", db) == true))
            {
                var dataSetProdutosServicos = new List<cstDatasetProdutosServicos>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    int _SizeNomeItem = 100;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var listaDbProdutosServicos = db.g_produtos.Select(p => new { p.id_produto, p.codigo, p.nome, p.preco_venda, p.fob1_dollar, p.fob2_dollar, p.fob3_dollar, p.fob1_id_importacao, p.fob2_id_importacao, p.fob3_id_importacao, p.has_corecharge, p.ativo, p.id_unidade_medida_venda, p.id_produto_ncm, p.saldo_01_disponivel, p.saldo_03_disponivel }).Where(p => p.ativo == true).ToList(); // 20210618
                    if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 500)) { _SizeNomeItem = 50; }
                    if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 400)) { _SizeNomeItem = 40; }
                    if ((_DisplayScreenWidth > 0) && (_DisplayScreenWidth < 300)) { _SizeNomeItem = 30; }
                    foreach (var item_g_produtos in listaDbProdutosServicos)
                    {
                        String NomeProduto = item_g_produtos.nome.EmptyIfNull().ToString().Trim();
                        if (NomeProduto.Length > _SizeNomeItem) { NomeProduto = NomeProduto.Substring(0, _SizeNomeItem) + "..."; };
                        cstDatasetProdutosServicos record_cstDatasetProdutosServicos = new cstDatasetProdutosServicos();
                        record_cstDatasetProdutosServicos.id_produto_servico = item_g_produtos.id_produto;
                        record_cstDatasetProdutosServicos.descricao_longa = NomeProduto; // Aqui
                        record_cstDatasetProdutosServicos.codigo = item_g_produtos.codigo;
                        record_cstDatasetProdutosServicos.preco_venda = item_g_produtos.preco_venda;
                        record_cstDatasetProdutosServicos.fob1_dollar = item_g_produtos.fob1_dollar;
                        record_cstDatasetProdutosServicos.fob1_id_importacao = item_g_produtos.fob1_id_importacao;
                        record_cstDatasetProdutosServicos.fob2_dollar = item_g_produtos.fob2_dollar;
                        record_cstDatasetProdutosServicos.fob2_id_importacao = item_g_produtos.fob2_id_importacao;
                        record_cstDatasetProdutosServicos.fob3_dollar = item_g_produtos.fob3_dollar;
                        record_cstDatasetProdutosServicos.fob3_id_importacao = item_g_produtos.fob3_id_importacao;
                        record_cstDatasetProdutosServicos.has_corecharge = item_g_produtos.has_corecharge;
                        record_cstDatasetProdutosServicos.id_unidade_medida_venda = item_g_produtos.id_unidade_medida_venda;
                        record_cstDatasetProdutosServicos.id_produto_ncm = item_g_produtos.id_produto_ncm ;
                        record_cstDatasetProdutosServicos.saldo_01_disponivel = item_g_produtos.saldo_01_disponivel;
                        record_cstDatasetProdutosServicos.saldo_03_disponivel = item_g_produtos.saldo_03_disponivel;
                        dataSetProdutosServicos.Add(record_cstDatasetProdutosServicos);
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_dataSetProdutosServicos = dataSetProdutosServicos;
            }
            else
            {
                List<cstDatasetProdutosServicos> ListaDataSetProdutosServicos = CachePersister.contextoModel.gc_dataSetProdutosServicos;
                CachePersister.contextoModel.gc_dataSetProdutosServicos = ListaDataSetProdutosServicos;
            }
            return CachePersister.contextoModel.gc_dataSetProdutosServicos;
        }
        public static List<SelectListItem> LoadComboGcLocaisEstoqueOrders(GdiPlataformEntities db)
        {
            // Locais de Estoque
            if (CachePersister.contextoModel.gc_comboLocaisEstoqueOrders.Count == 0)
            {
                var comboLocaisEstoqueOrders = new List<SelectListItem>();
                try
                {
                    comboLocaisEstoqueOrders.Add(new SelectListItem { Value = "-1", Text = "Estoque" });
                    IQueryable<gc_locais_estoque> listaDbLocaisEstoqueOrders = db.gc_locais_estoque.Select(p => p).Where(p => p.allow_order == true).OrderBy(p => p.id_local_estoque);
                    foreach (gc_locais_estoque ItemLocaisEstoqueOrders in listaDbLocaisEstoqueOrders)
                    {
                        comboLocaisEstoqueOrders.Add(new SelectListItem { Value = ItemLocaisEstoqueOrders.id_local_estoque.ToString(), Text = ItemLocaisEstoqueOrders.sigla.EmptyIfNull().ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboLocaisEstoqueOrders = comboLocaisEstoqueOrders;
            }
            else
            {
                List<SelectListItem> ListaLocaisEstoqueOrders = CachePersister.contextoModel.gc_comboLocaisEstoqueOrders;
                CachePersister.contextoModel.gc_comboLocaisEstoqueOrders = ListaLocaisEstoqueOrders;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboLocaisEstoqueOrders));
        }
        public static List<SelectListItem> LoadComboGClientesFornecedores(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.gc_comboGClientesFornecedores.Count == 0) || (LibDB.IsTableUpdate("g_clientes", "LoadComboGClientesFornecedores", db) == true))
            {
                var ComboGClientesFornecedores = new List<SelectListItem>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var ListaTemp = db.g_clientes.Select(c => new { c.id_cliente, c.is_cliente, c.is_fornecedor, c.ativo, c.nome, c.id_ciclo_faturamento, c.id_coligada, c.id_filial }).Where(p => (p.ativo == true)).OrderBy(p => p.nome).ToList();
                    foreach (var RecordTemp in ListaTemp)
                    {
                        String NomeCliente = RecordTemp.nome.EmptyIfNull().ToString().Trim();
                        if ((NomeCliente.Length > 50) && (_DisplayScreenWidth < 500)) { NomeCliente = NomeCliente.Substring(0, 50); };
                        ComboGClientesFornecedores.Add(new SelectListItem { Value = RecordTemp.id_cliente.ToString(), Text = NomeCliente + "\xA0\xA0\xA0\xA0\xA0" + "[Id: " + RecordTemp.id_cliente.ToString() + "]" });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGClientesFornecedores = ComboGClientesFornecedores;
            }
            else
            {
                List<SelectListItem> ListaClientes = CachePersister.contextoModel.gc_comboGClientesFornecedores;
                CachePersister.contextoModel.gc_comboGClientesFornecedores = ListaClientes;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGClientesFornecedores));
        }
        public static List<SelectListItem> LoadComboGClientesFornecedoresComDoc(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.gc_comboGClientesFornecedoresComDoc.Count == 0) || (LibDB.IsTableUpdate("g_clientes", "LoadComboGClientesFornecedoresComDoc", db) == true))
            {
                var ComboGClientesFornecedoresComDoc = new List<SelectListItem>();
                try
                {
                    String Documento = string.Empty;
                    int _DisplayScreenWidth = 0;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var ListaTemp = db.g_clientes.Select(c => new { c.id_cliente, c.cpf, c.cnpj, c.is_cliente, c.is_fornecedor, c.ativo, c.nome, c.id_ciclo_faturamento, c.id_coligada, c.id_filial }).Where(p => (p.ativo == true)).OrderBy(p => p.nome).ToList();
                    foreach (var RecordTemp in ListaTemp)
                    {
                        if (RecordTemp.cpf.EmptyIfNull().ToString().Trim().Length > 0) { Documento = RecordTemp.cpf.Trim(); }
                        else if (RecordTemp.cnpj.EmptyIfNull().ToString().Trim().Length > 0) { Documento = RecordTemp.cnpj.Trim(); };
                        String NomeCliente = RecordTemp.nome.EmptyIfNull().ToString().Trim();
                        if ((NomeCliente.Length > 50) && (_DisplayScreenWidth < 500)) { NomeCliente = NomeCliente.Substring(0, 50); };
                        ComboGClientesFornecedoresComDoc.Add(new SelectListItem { Value = RecordTemp.id_cliente.ToString(), Text = NomeCliente + "\xA0\xA0\xA0\xA0\xA0" + "[ " + Documento + " ]" });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGClientesFornecedoresComDoc = ComboGClientesFornecedoresComDoc;
            }
            else
            {
                List<SelectListItem> ListaClientesComDoc = CachePersister.contextoModel.gc_comboGClientesFornecedoresComDoc;
                CachePersister.contextoModel.gc_comboGClientesFornecedoresComDoc = ListaClientesComDoc;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGClientesFornecedoresComDoc));
        }

        public static List<SelectListItem> LoadComboSomenteGClientes(GdiPlataformEntities db)
        {
            // Clientes
            if ((CachePersister.contextoModel.gc_comboSomenteGClientes.Count == 0) || (LibDB.IsTableUpdate("g_clientes", "LoadComboSomenteGClientes", db) == true))
            {
                var ComboSomenteGClientes = new List<SelectListItem>();
                try
                {
                    int _DisplayScreenWidth = 0;
                    int.TryParse(CachePersister.userIdentity.DisplayScreenWidth, out _DisplayScreenWidth);
                    var ListaTemp = db.g_clientes.Select(c => new { c.id_cliente, c.is_cliente, c.is_fornecedor, c.ativo, c.nome, c.id_ciclo_faturamento, c.id_coligada, c.id_filial }).Where(p => (p.ativo == true) && (p.is_cliente == true)).OrderBy(p => p.nome).ToList();
                    foreach (var RecordTemp in ListaTemp)
                    {
                        String NomeCliente = RecordTemp.nome.EmptyIfNull().ToString().Trim();
                        if ((NomeCliente.Length > 50) && (_DisplayScreenWidth < 500)) { NomeCliente = NomeCliente.Substring(0, 50); }
                        else if (NomeCliente.Length > 100) { NomeCliente = NomeCliente.Substring(0, 100); };
                        ComboSomenteGClientes.Add(new SelectListItem { Value = RecordTemp.id_cliente.ToString(), Text = NomeCliente + "\xA0\xA0\xA0\xA0\xA0" + "[Id: " + RecordTemp.id_cliente.ToString() + "]" }); 
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboSomenteGClientes = ComboSomenteGClientes;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.gc_comboSomenteGClientes;
                CachePersister.contextoModel.gc_comboSomenteGClientes = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboSomenteGClientes));
        }
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
        {
            // Contas Caixas
            if (CachePersister.contextoModel.gc_comboContasCaixas.Count == 0)
            {
                var comboContasCaixas = new List<SelectListItem>();
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
                            comboContasCaixas.Add(new SelectListItem { Value = itemContaCaixa.id_conta_caixa.ToString(), Text = itemContaCaixa.nome.ToString() });
                        }
                    }
                    else
                    {
                        comboContasCaixas.Add(new SelectListItem { Value = "0", Text = "CONTA CAIXA INTERNA" }); ;
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboContasCaixas = comboContasCaixas;
            }
            else
            {
                List<SelectListItem> ListaContasCaixas = CachePersister.contextoModel.gc_comboContasCaixas;
                CachePersister.contextoModel.gc_comboContasCaixas = ListaContasCaixas;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboContasCaixas));
        }
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
        {
            // Entregas Prazos
            if (CachePersister.contextoModel.gc_comboGcMovimentosPosicao.Count == 0)
            {
                var comboMovimentosPosicao = new List<SelectListItem>();
                try
                {
                    IQueryable<gc_movimentos_posicao> listaMovimentosPosicao = null;
                    listaMovimentosPosicao = db.gc_movimentos_posicao.Select(c => c).OrderBy(c => c.id_movimento_posicao);
                    comboMovimentosPosicao.Add(new SelectListItem { Value = "-1", Text = "Todos" });
                    foreach (gc_movimentos_posicao itemMovimentosPosicao in listaMovimentosPosicao)
                    {
                        comboMovimentosPosicao.Add(new SelectListItem { Value = itemMovimentosPosicao.id_movimento_posicao.ToString(), Text = itemMovimentosPosicao.id_movimento_posicao.ToString() + " - " + itemMovimentosPosicao.posicao.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboGcMovimentosPosicao = comboMovimentosPosicao;
            }
            else
            {
                List<SelectListItem> ListaMovimentosPosicao = CachePersister.contextoModel.gc_comboGcMovimentosPosicao;
                CachePersister.contextoModel.gc_comboGcMovimentosPosicao = ListaMovimentosPosicao;
            }
            //return CachePersister.contextoModel.gc_comboGcTransportadora;
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboGcMovimentosPosicao));
        }
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
        {
            if (CachePersister.contextoModel.gc_comboEstoqueEnderecoArea.Count == 0)
            {
                // Locais de Estoque
                IQueryable<gc_estoque_endereco_area> listaDb = null;
                var comboEstoqueEnderecoArea = new List<SelectListItem>();
                try
                {
                    comboEstoqueEnderecoArea.Add(new SelectListItem { Value = "0", Text = "[ Área ]" });
                    if (IdLocalEstoque == 0) { listaDb = db.gc_estoque_endereco_area.Where(p => p.ativo == true).OrderBy(p => p.id_local_estoque).ThenBy(p => p.id_estoque_area); }
                    else { listaDb = db.gc_estoque_endereco_area.Where(p => p.ativo == true && p.id_local_estoque == IdLocalEstoque).OrderBy(p => p.id_estoque_area); }
                    foreach (gc_estoque_endereco_area item in listaDb)
                    {
                        comboEstoqueEnderecoArea.Add(new SelectListItem { Value = item.id_estoque_area.ToString(), Text = item.nome.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboEstoqueEnderecoArea = comboEstoqueEnderecoArea;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.gc_comboEstoqueEnderecoArea;
                CachePersister.contextoModel.gc_comboEstoqueEnderecoArea = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboEstoqueEnderecoArea));
        }

        public static List<SelectListItem> LoadComboGcEstoqueEnderecoSecao(GdiPlataformEntities db, int IdLocalEstoque)
        {
            if (CachePersister.contextoModel.gc_comboEstoqueEnderecoSecao.Count == 0)
            {
                // Locais de Estoque
                IQueryable<gc_estoque_endereco_secao> listaDb = null;
                var comboEstoqueEnderecoSecao = new List<SelectListItem>();
                try
                {
                    comboEstoqueEnderecoSecao.Add(new SelectListItem { Value = "0", Text = "[ Seção ]" });
                    if (IdLocalEstoque == 0) { listaDb = db.gc_estoque_endereco_secao.Where(p => p.ativo == true).OrderBy(p => p.id_local_estoque).ThenBy(p => p.id_estoque_secao); }
                    else { listaDb = db.gc_estoque_endereco_secao.Where(p => p.ativo == true && p.id_local_estoque == IdLocalEstoque).OrderBy(p => p.id_estoque_secao); }
                    foreach (gc_estoque_endereco_secao item in listaDb)
                    {
                        comboEstoqueEnderecoSecao.Add(new SelectListItem { Value = item.id_estoque_secao.ToString(), Text = item.nome.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboEstoqueEnderecoSecao = comboEstoqueEnderecoSecao;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.gc_comboEstoqueEnderecoSecao;
                CachePersister.contextoModel.gc_comboEstoqueEnderecoSecao = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboEstoqueEnderecoSecao));
        }

        public static List<SelectListItem> LoadComboGcEstoqueEnderecoCorredor(GdiPlataformEntities db, int IdLocalEstoque)
        {
            if (CachePersister.contextoModel.gc_comboEstoqueEnderecoCorredor.Count == 0)
            {
                IQueryable<gc_estoque_endereco_corredor> listaDb = null;
                var comboEstoqueEnderecoCorredor = new List<SelectListItem>();
                try
                {
                    comboEstoqueEnderecoCorredor.Add(new SelectListItem { Value = "0", Text = "[ Corredor ]" });
                    if (IdLocalEstoque == 0) { listaDb = db.gc_estoque_endereco_corredor.Where(p => p.ativo == true).OrderBy(p => p.id_local_estoque).ThenBy(p => p.id_estoque_corredor); }
                    else { listaDb = db.gc_estoque_endereco_corredor.Where(p => p.ativo == true && p.id_local_estoque == IdLocalEstoque).OrderBy(p => p.id_estoque_corredor); }
                    foreach (gc_estoque_endereco_corredor item in listaDb)
                    {
                        comboEstoqueEnderecoCorredor.Add(new SelectListItem { Value = item.id_estoque_corredor.ToString(), Text = item.nome.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboEstoqueEnderecoCorredor = comboEstoqueEnderecoCorredor;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.gc_comboEstoqueEnderecoCorredor;
                CachePersister.contextoModel.gc_comboEstoqueEnderecoCorredor = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboEstoqueEnderecoCorredor));
        }

        public static List<SelectListItem> LoadComboGcEstoqueEnderecoPrateleira(GdiPlataformEntities db, int IdLocalEstoque)
        {
            if (CachePersister.contextoModel.gc_comboEstoqueEnderecoPrateleira.Count == 0)
            {
                // Locais de Estoque
                IQueryable<gc_estoque_endereco_prateleira> listaDb = null;
                var comboEstoqueEnderecoPrateleira = new List<SelectListItem>();
                try
                {
                    comboEstoqueEnderecoPrateleira.Add(new SelectListItem { Value = "0", Text = "[ Prateleira ]" });
                    if (IdLocalEstoque == 0) { listaDb = db.gc_estoque_endereco_prateleira.Where(p => p.ativo == true).OrderBy(p => p.id_local_estoque).ThenBy(p => p.id_estoque_prateleira); }
                    else { listaDb = db.gc_estoque_endereco_prateleira.Where(p => p.ativo == true && p.id_local_estoque == IdLocalEstoque).OrderBy(p => p.id_estoque_prateleira); }
                    foreach (gc_estoque_endereco_prateleira item in listaDb)
                    {
                        comboEstoqueEnderecoPrateleira.Add(new SelectListItem { Value = item.id_estoque_prateleira.ToString(), Text = item.nome.ToString() });
                    }
                }
                finally { }
                CachePersister.contextoModel.gc_comboEstoqueEnderecoPrateleira = comboEstoqueEnderecoPrateleira;
            }
            else
            {
                List<SelectListItem> ListaTemp = CachePersister.contextoModel.gc_comboEstoqueEnderecoPrateleira;
                CachePersister.contextoModel.gc_comboEstoqueEnderecoPrateleira = ListaTemp;
            }
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.gc_comboEstoqueEnderecoPrateleira));
        }

        public static List<SelectListItem> LoadComboGedArquivosTipos(GdiPlataformEntities db, int IdTipo, int IdTipoPai)
        {
            List<ged_arquivos_tipos> ListaGedTiposAll = new List<ged_arquivos_tipos>();

            if (IdTipo <=0 && IdTipoPai <= 0) { ListaGedTiposAll = db.ged_arquivos_tipos.Where(t => t.id_arquivo_tipo > 0).OrderBy(t => t.descricao).ToList(); }
            else 
            {
                if (IdTipo > 0) { ListaGedTiposAll = db.ged_arquivos_tipos.Where(t => t.id_arquivo_tipo == IdTipo).OrderBy(t => t.descricao).ToList(); }
                else if (IdTipoPai > 0) { ListaGedTiposAll = db.ged_arquivos_tipos.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == IdTipoPai).OrderBy(t => t.descricao).ToList(); }
            }

            var ComboGedTipos = new List<SelectListItem>();
            try
            {
                if (IdTipo <= 0) // Não passou um tipo especifico
                { 
                    ComboGedTipos.Add(new SelectListItem { Value = "0", Text = "[ Selecione o Tipo ]" });
                    var ListaGedTiposN1 = ListaGedTiposAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == IdTipoPai).OrderBy(t => t.descricao).ToList();
                    foreach (var ItemGedTipoN1 in ListaGedTiposN1)
                    {
                        if (ItemGedTipoN1.id_tipo_pai == IdTipoPai)
                        {
                            if (ItemGedTipoN1.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN1.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN1.descricao.EmptyIfNull().ToString() }); }
                            ;
                            var ListaGedTiposN2 = ListaGedTiposAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == ItemGedTipoN1.id_arquivo_tipo).OrderBy(t => t.descricao).ToList();
                            foreach (var ItemGedTipoN2 in ListaGedTiposN2)
                            {
                                //if (ItemGedTipoN2.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN2.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN1.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN2.descricao.EmptyIfNull().ToString() }); };
                                if (ItemGedTipoN2.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN2.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN2.descricao.EmptyIfNull().ToString() }); }
                                var ListaGedTiposN3 = ListaGedTiposAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == ItemGedTipoN2.id_arquivo_tipo).OrderBy(t => t.descricao).ToList();
                                foreach (var ItemGedTipoN3 in ListaGedTiposN3)
                                {
                                    //if (ItemGedTipoN3.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN3.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN1.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN2.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN3.descricao.EmptyIfNull().ToString() }); };
                                    if (ItemGedTipoN3.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN3.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN3.descricao.EmptyIfNull().ToString() }); };
                                    var ListaGedTiposN4 = ListaGedTiposAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == ItemGedTipoN3.id_arquivo_tipo).OrderBy(t => t.descricao).ToList();
                                    foreach (var ItemGedTipoN4 in ListaGedTiposN4)
                                    {

                                        //if (ItemGedTipoN4.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN4.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN1.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN2.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN3.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN4.descricao.EmptyIfNull().ToString() }); };
                                        if (ItemGedTipoN4.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN4.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN4.descricao.EmptyIfNull().ToString() }); };
                                        var ListaGedTiposN5 = ListaGedTiposAll.Where(t => t.id_arquivo_tipo > 0 && t.id_tipo_pai == ItemGedTipoN4.id_arquivo_tipo).OrderBy(t => t.descricao).ToList();
                                        foreach (var ItemGedTipoN5 in ListaGedTiposN5)
                                        {
                                            //if (ItemGedTipoN5.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN5.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN1.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN2.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN3.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN4.descricao.EmptyIfNull().ToString() + "  ->  " + ItemGedTipoN5.descricao.EmptyIfNull().ToString() }); };
                                            if (ItemGedTipoN5.ativo == true) { ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN5.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN5.descricao.EmptyIfNull().ToString() }); };
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else 
                {
                    var ListaGedTiposN1 = ListaGedTiposAll.Where(t => t.id_arquivo_tipo == IdTipo).OrderBy(t => t.descricao).ToList();
                    foreach (var ItemGedTipoN1 in ListaGedTiposN1)
                    {
                        ComboGedTipos.Add(new SelectListItem { Value = ItemGedTipoN1.id_arquivo_tipo.ToString(), Text = "  -  " + ItemGedTipoN1.descricao.EmptyIfNull().ToString() });
                    }
                };
            }
            finally { }
            CachePersister.contextoModel.g_comboGedArquivosTipos = ComboGedTipos;
            return JsonConvert.DeserializeObject<List<SelectListItem>>(JsonConvert.SerializeObject(CachePersister.contextoModel.g_comboGedArquivosTipos));
        }

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
