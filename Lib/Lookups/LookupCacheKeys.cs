using System;
using System.Globalization;
using System.Linq;
using System.Text;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Models;
using GdiPlataform.Security;

namespace GdiPlataform.Lib.Lookups
{
    /// <summary>Constantes e construção de chaves MemoryCache para lookups (Fase 2).</summary>
    public static class LookupCacheKeys
    {
        public const string Prefix = "lookup:";

        public const string GcProdutosServicosTodos = "gc_produtos_servicos_todos";
        public const string GcTransportadora = "gc_transportadora";
        public const string GcLocaisEstoqueOrders = "gc_locais_estoque_orders";
        public const string GcClientesContatos = "gc_clientes_contatos";
        public const string GcClientesDestinatarios = "gc_clientes_destinatarios";
        public const string GcProdutosServicosDataset = "gc_produtos_servicos_dataset";
        public const string GVendedores = "g_vendedores";
        public const string GVendedoresDataset = "g_vendedores_dataset";
        public const string GcCfop = "gc_cfop";
        public const string GedArquivosTipos = "ged_arquivos_tipos";
        public const string GcProdutosServicosImportados = "gc_produtos_servicos_importados";
        public const string GcEntregasPrazos = "gc_entregas_prazos";
        public const string GProdutoCondicao = "g_produto_condicao";
        public const string GContasCaixas = "g_contas_caixas";
        public const string GcMovimentosPosicao = "gc_movimentos_posicao";
        public const string GcFreteResponsavel = "gc_frete_responsavel";
        public const string GcCfopOperacoesFaturamentoPedido = "gc_cfop_operacoes_faturamento_pedido";
        public const string GcEstoqueEnderecoArea = "gc_estoque_endereco_area";
        public const string GcEstoqueEnderecoSecao = "gc_estoque_endereco_secao";
        public const string GcEstoqueEnderecoCorredor = "gc_estoque_endereco_corredor";
        public const string GcEstoqueEnderecoPrateleira = "gc_estoque_endereco_prateleira";
        public const string GcClientesDestinatariosDataset = "gc_clientes_destinatarios_dataset";
        public const string GcClientesContatosDataset = "gc_clientes_contatos_dataset";
        public const string GcTiposMovimentosVendas = "gc_tipos_movimentos_vendas";
        public const string GcTiposMovimentosCreateEdit = "gc_tipos_movimentos_create_edit";
        public const string GcStatusMovimentos = "gc_status_movimentos";
        public const string GcMoedas = "gc_moedas";
        public const string GcPagRecCondicoesTodas = "gc_pagrec_condicoes_todas";
        public const string GcPagRecCondicoesFaturaveis = "gc_pagrec_condicoes_faturaveis";
        public const string GcPagRecTiposFaturaveis = "gc_pagrec_tipos_faturaveis";
        public const string GcFinanceiroStatus = "gc_financeiro_status";
        public const string GcFinanceiroFiltroStatus = "gc_financeiro_filtro_status";
        public const string GContasCaixasGerencial = "g_contas_caixas_gerencial";
        public const string GDebitoCredito = "g_debito_credito";
        public const string ARowsColors = "a_rows_colors";
        public const string GClassificacaoFinanceira = "g_classificacao_financeira";
        public const string GcCfopFinalidade = "gc_cfop_finalidade";
        public const string GcCfopOperacoesTelaPedido = "gc_cfop_operacoes_tela_pedido";
        public const string GcComexImportacoesTodas = "gc_comex_importacoes_todas";
        public const string GcComexProdutosComId = "gc_comex_produtos_com_id";
        public const string GcClientesContatosTipos = "gc_clientes_contatos_tipos";
        public const string GcClientesContatosPedido = "gc_clientes_contatos_pedido";
        public const string GProdutosTipos = "g_produtos_tipos";
        public const string GProdutosNcm = "g_produtos_ncm";
        public const string GcIcmsUfIsento = "gc_icms_uf_isento";
        public const string GcIcmsCstSimples = "gc_icms_cst_simples";
        public const string GUnidadeMedida = "g_unidade_medida";
        public const string GContratosTipos = "g_contratos_tipos";
        public const string GcProdutosFamilia = "gc_produtos_familia";
        public const string GcProdutosStatus = "gc_produtos_status";
        public const string GUsuariosAtendimentoResponsavel = "g_usuarios_atendimento_responsavel";
        public const string GUsuariosAtendimentoSolicitante = "g_usuarios_atendimento_solicitante";
        public const string GDepartamentos = "g_departamentos";
        public const string GAtendimentosStatus = "g_atendimentos_status";
        public const string GAtendimentosCategorias = "g_atendimentos_categorias";
        public const string GColigadas = "g_coligadas";
        public const string GRevendasVendedor = "g_revendas_vendedor";
        public const string GCidadesAtivas = "g_cidades_ativas";
        public const string GUf = "g_uf";
        public const string GFinanceiroStatusTitulos = "g_financeiro_status_titulos";
        public const string GContasCaixasBoleto = "g_contas_caixas_boleto";
        public const string GcIcmsCstNcm = "gc_icms_cst_ncm";
        public const string GcTributosIpiEntrada = "gc_tributos_ipi_entrada";
        public const string GcTributosIpiSaida = "gc_tributos_ipi_saida";
        public const string GcTributosPisEntrada = "gc_tributos_pis_entrada";
        public const string GcTributosPisSaida = "gc_tributos_pis_saida";
        public const string GcTributosCofinsEntrada = "gc_tributos_cofins_entrada";
        public const string GcTributosCofinsSaida = "gc_tributos_cofins_saida";

        /// <summary>Combo global (sem parâmetros de negócio).</summary>
        public static string Combo(string lookupName, string sessionToken)
        {
            return Prefix + (sessionToken ?? "_") + ":" + lookupName;
        }

        /// <summary>Combo/dataset com parâmetros (IdCliente, IdLocalEstoque, etc.).</summary>
        public static string Combo(string lookupName, string sessionToken, params object[] parameters)
        {
            var sb = new StringBuilder();
            sb.Append(Prefix);
            sb.Append(sessionToken ?? "_");
            sb.Append(':');
            sb.Append(lookupName);
            if (parameters != null && parameters.Length > 0)
            {
                sb.Append(':');
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0) sb.Append('|');
                    sb.Append(FormatParam(parameters[i]));
                }
            }
            return sb.ToString();
        }

        private static string FormatParam(object value)
        {
            if (value == null) return "null";
            if (value is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
        }

        /// <summary>Carimbo de versão da tabela após verificação IsTableUpdate (invalida cache quando dados mudam).</summary>
        public static string TableVersionStamp(string tableName, string processName, GdiPlataformEntities db)
        {
            if (string.IsNullOrEmpty(tableName)) return "0";
            LibDB.IsTableUpdate(tableName, processName, db);
            var ui = CachePersister.userIdentity;
            if (ui?.ListTablesUpdate == null) return "0";
            var row = ui.ListTablesUpdate
                .FirstOrDefault(t => string.Equals(t.TableName, tableName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(t.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
            if (row == null) return "0";
            return row.DateTimeUpdate.ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
