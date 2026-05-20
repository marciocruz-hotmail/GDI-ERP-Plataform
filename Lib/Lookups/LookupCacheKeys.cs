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
        public const string GClientesFornecedores = "g_clientes_fornecedores";
        public const string GClientesFornecedoresComDoc = "g_clientes_fornecedores_com_doc";
        public const string GcTransportadora = "gc_transportadora";
        public const string GcLocaisEstoqueOrders = "gc_locais_estoque_orders";
        public const string GcClientesContatos = "gc_clientes_contatos";
        public const string GcClientesDestinatarios = "gc_clientes_destinatarios";
        public const string GcProdutosServicosDataset = "gc_produtos_servicos_dataset";
        public const string GVendedores = "g_vendedores";
        public const string GVendedoresDataset = "g_vendedores_dataset";
        public const string GcCfop = "gc_cfop";
        public const string SomenteGClientes = "somente_g_clientes";
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
