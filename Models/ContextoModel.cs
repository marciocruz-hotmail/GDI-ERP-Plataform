using GdiPlataform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Db;
using System.Web.Mvc;
using GdiPlataform.Areas.gc.Models;

namespace GdiPlataform.Domain
{
    public class ContextoModel
    {
        public g_filiais g_filial { get; set; }
        public string versaoPlataforma { get; set; }
        public List<NavbarItemMessage> allNavbarItemMessage { get; set; }
        public List<NavbarItemMenu> allNavbarItemMenu { get; set; }
        public List<NavbarItemTask> allNavbarItemTask { get; set; }
        public List<NavbarItemAlert> allNavbarItemAlert { get; set; }
        public List<NavbarItemAtividade> allNavbarItemAtividade { get; set; }
        public UserIdentity userIdentity { get; set; }

        // Informações Módulo Comercial
        public List<SelectListItem> a_comboRowsColors { get; set; }
        public List<SelectListItem> g_comboContratosTipos { get; set; }
        public List<SelectListItem> g_comboDebitoCredito { get; set; }
        public List<SelectListItem> g_comboGedArquivosTipos { get; set; }
        //public List<SelectListItem> g_comboGedArquivosTiposFiltro { get; set; }
        public List<SelectListItem> g_comboUnidadeMedida{ get; set; }
        public List<SelectListItem> g_comboDepartamentos { get; set; }
        public List<SelectListItem> g_comboAtendimentosCategorias { get; set; }
        public List<SelectListItem> g_comboAtendimentosStatus { get; set; }
        public List<SelectListItem> gc_comboGClientesFornecedores { get; set; }
        public List<SelectListItem> gc_comboGClientesFornecedoresComDoc { get; set; }
        public List<SelectListItem> gc_comboClientesContatos { get; set; }
        public List<SelectListItem> gc_comboClientesContatosTipos { get; set; }
        public List<SelectListItem> gc_comboClientesFornecedores { get; set; }
        public List<SelectListItem> gc_comboClientesFornecedoresComDoc { get; set; }
        public List<SelectListItem> gc_comboContasCaixas { get; set; }
        public List<SelectListItem> gc_comboContasCaixasGerencial { get; set; }
        public List<SelectListItem> gc_comboEntregasPrazos { get; set; }
        public List<SelectListItem> gc_comboFiltroDebitoCredito { get; set; }
        public List<SelectListItem> gc_comboFinanceiroFiltroStatus { get; set; }
        public List<SelectListItem> gc_comboFinanceiroStatus { get; set; }
        public List<SelectListItem> gc_comboSomenteGClientes { get; set; }
        public List<SelectListItem> gc_comboSomenteGClientesComDoc { get; set; }
        public List<SelectListItem> gc_comboSomenteGFornecedores { get; set; }
        public List<SelectListItem> gc_comboSomenteGFornecedoresComDoc { get; set; }
        public List<SelectListItem> gc_comboFreteResponsavel { get; set; }
        public List<SelectListItem> gc_comboGcCfop { get; set; }
        public List<SelectListItem> gc_comboGcCfopFinalidade { get; set; }
        public List<SelectListItem> gc_comboGcCfopOperacoes { get; set; }
        public List<SelectListItem> gc_comboGcCfopOperacoesVendedor { get; set; }
        public List<SelectListItem> gc_comboGcClientesDestinatarios { get; set; }
        public List<SelectListItem> gc_comboGcComexImportacoesAtivas { get; set; }
        public List<SelectListItem> gc_comboGcComexImportacoesTodas { get; set; }
        public List<SelectListItem> gc_comboGcComexProdutosComId { get; set; }
        public List<SelectListItem> gc_comboGcMovimentosPosicao { get; set; }
        public List<SelectListItem> gc_comboGcTransportadora { get; set; }
        public List<SelectListItem> gc_comboGcTarefasTipos { get; set; }
        public List<SelectListItem> gc_comboIcmsUfIsento { get; set; }
        public List<SelectListItem> gc_comboIcmsCstSimples { get; set; }
        public List<SelectListItem> gc_comboLocaisEstoque { get; set; }
        public List<SelectListItem> gc_comboLocaisEstoqueOrders { get; set; }
        public List<SelectListItem> gc_comboEstoqueEnderecoArea { get; set; }
        public List<SelectListItem> gc_comboEstoqueEnderecoSecao { get; set; }
        public List<SelectListItem> gc_comboEstoqueEnderecoCorredor { get; set; }
        public List<SelectListItem> gc_comboEstoqueEnderecoPrateleira { get; set; }
        public List<SelectListItem> gc_comboMoedas { get; set; }
        public List<SelectListItem> gc_comboPagRecCondicoesTodas { get; set; }
        public List<SelectListItem> gc_comboPagRecCondicoesFaturaveis { get; set; }
        public List<SelectListItem> gc_comboPagRecTiposTodos { get; set; }
        public List<SelectListItem> gc_comboPagRecTiposFaturaveis { get; set; }
        public List<SelectListItem> gc_comboProdutosCondicoes { get; set; }
        public List<SelectListItem> gc_comboProdutosFamilia { get; set; }
        public List<SelectListItem> gc_comboProdutosNCM { get; set; }
        public List<SelectListItem> gc_comboProdutosServicosTodos { get; set; }
        public List<SelectListItem> gc_comboProdutosServicosImportados { get; set; }
        public List<SelectListItem> gc_comboProdutosServicosTodosComId { get; set; }
        public List<SelectListItem> gc_comboProdutosTipos { get; set; }
        public List<SelectListItem> gc_comboProdutosStatus { get; set; }
        public List<SelectListItem> gc_comboStatusMovimentos { get; set; }
        public List<SelectListItem> gc_comboTiposMovimentosCompras { get; set; }
        public List<SelectListItem> gc_comboTiposMovimentosCreateEdit { get; set; }
        public List<SelectListItem> gc_comboTiposMovimentosVendas { get; set; }
        public List<SelectListItem> gc_comboVendedores { get; set; }

        public List<SelectListItem> g_comboGClassificacaoFinanceira { get; set; }
        public List<CstDatasetProdutosServicos> gc_dataSetProdutosServicos { get; set; }
        public List<CstDatasetClientesContatos> gc_dataSetClientesContatos { get; set; }
        public List<g_clientes_destinatarios> gc_dataSetClientesDestinatarios { get; set; }
        public List<g_vendedores> gc_dataSetVendedores { get; set; }
        public List<gc_cfop> gc_dataSetCfop { get; set; }
        public List<gc_cfop_operacoes> gc_dataSetCfopOperacoes { get; set; }
        public ContextoModel()
        {
            g_filiais g_filial = new g_filiais();
            userIdentity = new UserIdentity();
            allNavbarItemMessage = new List<NavbarItemMessage>();
            allNavbarItemMenu = new List<NavbarItemMenu>();
            allNavbarItemTask = new List<NavbarItemTask>();
            allNavbarItemAlert = new List<NavbarItemAlert>();
            allNavbarItemAtividade = new List<NavbarItemAtividade>();
            a_comboRowsColors = new List<SelectListItem>();
            g_comboContratosTipos = new List<SelectListItem>();
            g_comboDebitoCredito = new List<SelectListItem>();
            g_comboGedArquivosTipos = new List<SelectListItem>();
            g_comboUnidadeMedida = new List<SelectListItem>();
            g_comboDepartamentos = new List<SelectListItem>();
            g_comboAtendimentosCategorias = new List<SelectListItem>();
            g_comboAtendimentosStatus = new List<SelectListItem>();
            gc_comboGClientesFornecedores = new List<SelectListItem>();
            gc_comboGClientesFornecedoresComDoc = new List<SelectListItem>();
            gc_comboClientesContatos = new List<SelectListItem>();
            gc_comboClientesContatosTipos = new List<SelectListItem>();
            gc_comboClientesFornecedores = new List<SelectListItem>();
            gc_comboClientesFornecedoresComDoc = new List<SelectListItem>();
            gc_comboContasCaixas = new List<SelectListItem>();
            gc_comboContasCaixasGerencial = new List<SelectListItem>();
            gc_comboEntregasPrazos = new List<SelectListItem>();
            gc_comboFiltroDebitoCredito = new List<SelectListItem>();
            gc_comboFinanceiroFiltroStatus = new List<SelectListItem>();
            gc_comboFinanceiroStatus = new List<SelectListItem>();
            gc_comboSomenteGClientes = new List<SelectListItem>();
            gc_comboSomenteGClientesComDoc = new List<SelectListItem>();
            gc_comboSomenteGFornecedores = new List<SelectListItem>();
            gc_comboSomenteGFornecedoresComDoc = new List<SelectListItem>();
            gc_comboFreteResponsavel = new List<SelectListItem>();
            gc_comboGcCfop = new List<SelectListItem>();
            gc_comboGcCfopFinalidade = new List<SelectListItem>();
            gc_comboGcCfopOperacoes = new List<SelectListItem>();
            gc_comboGcCfopOperacoesVendedor = new List<SelectListItem>();
            gc_comboGcClientesDestinatarios = new List<SelectListItem>();
            gc_comboGcComexImportacoesAtivas = new List<SelectListItem>();
            gc_comboGcComexImportacoesTodas = new List<SelectListItem>();
            gc_comboGcComexProdutosComId = new List<SelectListItem>();
            gc_comboGcMovimentosPosicao = new List<SelectListItem>();
            gc_comboGcTransportadora = new List<SelectListItem>();
            gc_comboGcTarefasTipos = new List<SelectListItem>();
            gc_comboIcmsUfIsento = new List<SelectListItem>();
            gc_comboIcmsCstSimples = new List<SelectListItem>();
            gc_comboLocaisEstoque = new List<SelectListItem>();
            gc_comboLocaisEstoqueOrders = new List<SelectListItem>();
            gc_comboMoedas = new List<SelectListItem>();
            gc_comboPagRecCondicoesTodas = new List<SelectListItem>();
            gc_comboPagRecCondicoesFaturaveis = new List<SelectListItem>();
            gc_comboPagRecTiposTodos = new List<SelectListItem>();
            gc_comboPagRecTiposFaturaveis = new List<SelectListItem>();
            gc_comboProdutosCondicoes = new List<SelectListItem>();
            gc_comboProdutosFamilia = new List<SelectListItem>();
            gc_comboProdutosNCM = new List<SelectListItem>();
            gc_comboProdutosServicosTodos = new List<SelectListItem>();
            gc_comboProdutosServicosTodosComId = new List<SelectListItem>();
            gc_comboProdutosServicosImportados = new List<SelectListItem>();
            gc_comboProdutosStatus = new List<SelectListItem>();
            gc_comboProdutosTipos = new List<SelectListItem>();
            gc_comboStatusMovimentos = new List<SelectListItem>();
            gc_comboTiposMovimentosCompras = new List<SelectListItem>();
            gc_comboTiposMovimentosCreateEdit = new List<SelectListItem>();
            gc_comboTiposMovimentosVendas = new List<SelectListItem>();
            gc_comboVendedores = new List<SelectListItem>();
            g_comboGClassificacaoFinanceira = new List<SelectListItem>();
            gc_dataSetCfop = new List<gc_cfop>();
            gc_dataSetCfopOperacoes = new List<gc_cfop_operacoes>();
            gc_dataSetClientesContatos = new List<CstDatasetClientesContatos>();
            gc_dataSetClientesDestinatarios = new List<g_clientes_destinatarios>();
            gc_dataSetProdutosServicos = new List<CstDatasetProdutosServicos>();
            gc_dataSetVendedores = new List<g_vendedores>();
            gc_comboEstoqueEnderecoArea = new List<SelectListItem>();
            gc_comboEstoqueEnderecoSecao = new List<SelectListItem>();
            gc_comboEstoqueEnderecoCorredor = new List<SelectListItem>();
            gc_comboEstoqueEnderecoPrateleira = new List<SelectListItem>();
        }
    }
}
