using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using GdiPlataform.Robos;
using GdiPlataform.Db;
using System.Data;
using System.Text;
using GdiPlataform.Lib;
using Microsoft.Ajax.Utilities;

namespace GdiPlataform.Models
{
    public class UserIdentity
    {
        public int IdUsuario { get; set; }
        public int IdDepartamento { get; set; }
        public string TokenAcesso { get; set; }
        public string SessionID { get; set; }
        public string VersionERP { get; set; }

        // Parametros de Perfil
        public int IdPerfil { get; set; }
        public string PerfilNome { get; set; }
        public int IdVendedor { get; set; }
        public string NomeRazaoSocial { get; set; }
        public bool Administrador { get; set; }
        public string Username { get; set; }
        public string Acesso { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        /// <summary>Login público portal do cliente (código + CPF/CNPJ).</summary>
        public string ClienteIdentificador { get; set; }
        public string ClienteCpfCnpj { get; set; }
        public int IdCliente { get; set; }
        public string Dominio { get; set; }
        public string SubDominio { get; set; }
        public string ImgLogoSubdominio { get; set; }
        public string[] Roles { get; set; }
        public DateTime DataHoraExpiracao { get; set; }
        public string AmbienteDatabase { get; set; }
        public string DataHoraUltimoLogin { get; set; }


        // Parametros da Empresa
        public int EmpresaID { get; set; }
        public string EmpresaNome { get; set; }


        // Parametros de Coligada
        public int id_coligada { get; set; }
        public string NomeColigada { get; set; }

        // Parametros de Filial
        public int id_filial { get; set; }
        public string FilialNome { get; set; }
        public string FilialCnpj { get; set; }

        // Parametros Backoffice
        public g_parametros record_g_parametros = new g_parametros();

        // Filtros do Usuário
        public List<Db.g_filtros> allFiltros = new List<Db.g_filtros>();

        // Perfil Conta Caixa
        public bool param_contacaixa_gc_has_edit { get; set; }
        public bool param_contacaixa_gc_has_view { get; set; }
        public bool param_contacaixa_gc_has_gerencial { get; set; }

        // Ids Ativos
        public string IdContaCaixaAtiva { get; set; }
        public string SaldoContaCaixaAtiva { get; set; }
        public string gc_ValorTotalPedido { get; set; }
        public int IdGcMovimentoAtivo { get; set; }
        public int IdGcComexImportacaoAtiva { get; set; }

        // Setenças SQL
        //public string SqlTempGcGetDadosLancamentos { get; set; }
        public String ParamSqlGcGetDadosLancamentos { get; set; }

        // Screen
        public string DisplayScreenWidth { get; set; }
        public string DisplayScreenHeight { get; set; }

        // Integração Sintegra
        public g_clientes NovoClienteSintegra { get; set; }

        // Rastreabilidade de Edição de Dados
        public g_clientes RecordGClienteEdicao { get; set; }
        public String DataRowInUseSerialized { get; set; }
        public String DataRowAuxInUseSerialized { get; set; }
        
        // Gestão Comercial
        public string GcParamGrupoVendedor { get; set; }
        public string GcParamLocalEstoquePedidos { get; set; }
        public decimal CotacaoDollarDia { get; set; }
        public string GcEstoqueCompetenciaAtual { get; set; }

        // Alerts para o Usuário
        public int alerts_qtd { get; set; }
        public string alerts_msg { get; set; }
        public bool alerts_show { get; set; }

        // Parametros de Sessão
        public string FormNameActive { get; set; }

        public List<ModelControlTableUpdate> ListTablesUpdate { get; set; }
        /*public DateTime LastDateTimeUpdateGcTransportadora { get; set; }
        public DateTime LastDateTimeUpdateGcIcmsCstSimples { get; set; }
        public DateTime LastDateTimeUpdateGUnidadeMedida { get; set; }
        public DateTime LastDateTimeUpdateGcCfop { get; set; }
        public DateTime LastDateTimeUpdateGcCfopOperacoes { get; set; }
        public DateTime LastDateTimeUpdateGcFreteResponsavel { get; set; }
        public DateTime LastDateTimeUpdateGcEntregasPrazos { get; set; }
        public DateTime LastDateTimeUpdateGProdutoCondicao { get; set; }
        public DateTime LastDateTimeUpdateGProdutosNCM { get; set; }
        public DateTime LastDateTimeUpdateGProdutosTipos { get; set; }
        public DateTime LastDateTimeUpdateGVendedores { get; set; }
        public DateTime LastDateTimeUpdateDatasetGVendedores { get; set; }
        public DateTime LastDateTimeUpdateGcClientesContatos { get; set; }
        public DateTime LastDateTimeUpdateDatasetGcClientesContatos { get; set; }
        public DateTime LastDateTimeUpdateGcProdutosServicosTodos { get; set; }
        public DateTime LastDateTimeUpdateGcProdutosServicosTodosComId { get; set; }
        public DateTime LastDateTimeUpdateGcProdutosServicosImportados { get; set; }
        public DateTime LastDateTimeUpdateDatasetGcProdutosServicos { get; set; }
        public DateTime LastDateTimeUpdateGcLocaisEstoqueOrders { get; set; }
        public DateTime LastDateTimeUpdateGClientesFornecedores { get; set; }
        public DateTime LastDateTimeUpdateGClientesFornecedoresComDoc { get; set; }
        public DateTime LastDateTimeUpdateSomenteGFornecedores { get; set; }
        public DateTime LastDateTimeUpdateSomenteGFornecedoresComDoc { get; set; }
        public DateTime LastDateTimeUpdateSomenteGClientes { get; set; }
        public DateTime LastDateTimeUpdateSomenteGClientesComDoc { get; set; }
        public DateTime LastDateTimeUpdateGcFinanceiroStatus { get; set; }
        public DateTime LastDateTimeUpdateARowColors { get; set; }
        public DateTime LastDateTimeUpdateGPagRecTipos { get; set; }
        public DateTime LastDateTimeUpdatePagRecCondicoes { get; set; }
        public DateTime LastDateTimeUpdateGContasCaixas { get; set; }
        public DateTime LastDateTimeUpdateGContasCaixasGerencial { get; set; }
        public DateTime LastDateTimeUpdateGContratosTipos { get; set; }
        public DateTime LastDateTimeUpdateGcMovimentosPosicao { get; set; }
        public DateTime LastDateTimeUpdateCfopFinalidade { get; set; }
        public DateTime LastDateTimeUpdateGcComexImportacoesTodas { get; set; }
        public DateTime LastDateTimeUpdateGcComexImportacoesAtivas { get; set; }
        public DateTime LastDateTimeUpdateGcProdutosFamilia { get; set; }
        public DateTime LastDateTimeUpdateGcProdutosStatus { get; set; }
        public DateTime LastDateTimeUpdateGcComexProdutosComID { get; set; }*/


        public UserIdentity()
        {
            DateTime DataHoraAtualizacaoInicial = LibDateTime.getDataHoraBrasilia().AddDays(-1);
            allFiltros = new List<Db.g_filtros>();
            FormNameActive = string.Empty;
            param_contacaixa_gc_has_edit = false;
            param_contacaixa_gc_has_view = false;
            param_contacaixa_gc_has_gerencial = false;
            NovoClienteSintegra = null;
            CotacaoDollarDia = 0;
            alerts_qtd = 0;
            alerts_show = false;
            ListTablesUpdate = new List<ModelControlTableUpdate>();
            /*LastDateTimeUpdateGcTransportadora = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcIcmsCstSimples = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGUnidadeMedida = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcCfop = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcCfopOperacoes = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcFreteResponsavel = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcEntregasPrazos = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGProdutoCondicao = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGProdutosNCM = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGProdutosTipos = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGVendedores = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateDatasetGVendedores = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcClientesContatos = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateDatasetGcClientesContatos = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcProdutosServicosTodos = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcProdutosServicosTodosComId = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcProdutosServicosImportados = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateDatasetGcProdutosServicos = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcLocaisEstoqueOrders = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGClientesFornecedores = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGClientesFornecedoresComDoc = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateSomenteGFornecedores = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateSomenteGFornecedoresComDoc = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateSomenteGClientes = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateSomenteGClientesComDoc = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcFinanceiroStatus = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateARowColors = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGPagRecTipos = DataHoraAtualizacaoInicial;
            LastDateTimeUpdatePagRecCondicoes = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGContasCaixas = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGContasCaixasGerencial = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGContratosTipos = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcMovimentosPosicao = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateCfopFinalidade = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcComexImportacoesTodas = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcComexImportacoesAtivas = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcProdutosFamilia = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcProdutosStatus = DataHoraAtualizacaoInicial;
            LastDateTimeUpdateGcComexProdutosComID = DataHoraAtualizacaoInicial;*/
            ParamSqlGcGetDadosLancamentos = string.Empty;
        }
    }
}