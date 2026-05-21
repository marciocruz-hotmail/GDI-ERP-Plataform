using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Management;
using GdiPlataform.Controllers;
using GdiPlataform.Db;
using GdiPlataform.Security;
using System.Text;
using GdiPlataform.Models;

namespace GdiPlataform.Lib
{
    public static class LibDB
    {
        public static void CheckConnectionDB(string _database)
        {
            try
            {
                if (!_database.EmptyIfNull().ToString().Equals(String.Empty))
                {
                    string ConnectionString = ConfigurationManager.ConnectionStrings[_database].ConnectionString.ToString();
                    
                    // SQL Server: extrair connection string do Entity Framework se necessário
                    // Entity Framework connection strings têm formato: metadata=...;provider=System.Data.SqlClient;provider connection string="..."
                    if (ConnectionString.IndexOf("provider connection string=", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        // Extrair a connection string do provider do Entity Framework
                        int startIndex = ConnectionString.IndexOf("provider connection string=", StringComparison.OrdinalIgnoreCase) + "provider connection string=".Length;
                        string remaining = ConnectionString.Substring(startIndex);
                        
                        // Remover o &quot; ou " inicial se houver
                        remaining = remaining.TrimStart(' ', '&', 'q', 'u', 'o', 't', ';', '"');
                        
                        // Encontrar o final da connection string (próximo &quot; ou " ou fim da string)
                        int endIndex = remaining.IndexOf("&quot;", StringComparison.OrdinalIgnoreCase);
                        if (endIndex < 0)
                        {
                            endIndex = remaining.IndexOf("\"", StringComparison.OrdinalIgnoreCase);
                        }
                        if (endIndex > 0)
                        {
                            ConnectionString = remaining.Substring(0, endIndex);
                        }
                        else
                        {
                            ConnectionString = remaining;
                        }
                        
                        // Decodificar HTML entities
                        ConnectionString = ConnectionString.Replace("&quot;", "\"").Replace("&amp;", "&");
                    }
                    // Se for connection string direta do SQL Server (contém Data Source ou Server), usar como está
                    
                    using (SqlConnection _conn = new SqlConnection(ConnectionString))
                    {
                        if (_conn.State != ConnectionState.Open)
                        {
                            _conn.Open();
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public static System.Data.DataTable GetDataTable(string sqlQuery, GdiPlataformEntities db, params System.Data.Common.DbParameter[] parameters)
        {
            try
            {
                DbProviderFactory factory = System.Data.Common.DbProviderFactories.GetFactory(db.Database.Connection);
                using (var cmd = factory.CreateCommand())
                {
                    cmd.CommandText = sqlQuery;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = db.Database.Connection;
                    
                    // Adicionar parâmetros SQL (SQL Server usa @parametro)
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (var param in parameters)
                        {
                            // Clonar parâmetro para evitar problemas de reutilização
                            var clonedParam = factory.CreateParameter();
                            clonedParam.ParameterName = param.ParameterName;
                            clonedParam.Value = param.Value ?? DBNull.Value;
                            clonedParam.DbType = param.DbType;
                            clonedParam.Direction = param.Direction;
                            if (param.Size > 0) clonedParam.Size = param.Size;
                            cmd.Parameters.Add(clonedParam);
                        }
                    }
                    
                    using (var adapter = factory.CreateDataAdapter())
                    {
                        adapter.SelectCommand = cmd;
                        var tb = new DataTable();
                        adapter.Fill(tb);
                        return tb;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SqlExecutionException(string.Format("Error occurred during SQL query execution {0}", sqlQuery), ex);
            }
        }

        public static String dbQueryValue(string sqlQuery, GdiPlataformEntities db)
        {
            string resultado = string.Empty;
            try
            {
                DataTable tableTemp1 = null;
                tableTemp1 = LibDB.GetDataTable(sqlQuery, db);
                List<DataRow> allRecords = null;
                allRecords = tableTemp1.AsEnumerable().ToList();
                var dsRow = allRecords.FirstOrDefault();
                // SQL Server: verificar NULL antes de acessar valores
                if (dsRow != null && dsRow[0] != null && !DBNull.Value.Equals(dsRow[0]))
                {
                    resultado = dsRow[0].EmptyIfNull().ToString().Trim();
                }
            }
            catch (Exception) { }
            return resultado;
        }

        public static int dbQueryExec(string sqlQuery, GdiPlataformEntities db, params System.Data.Common.DbParameter[] parameters)
        {
            int rows = 0;
            var connection = db.Database.Connection;
            var command = connection.CreateCommand();
            try
            {
                connection.Open();
                command.CommandText = sqlQuery;
                
                // Adicionar parâmetros SQL (SQL Server usa @parametro)
                if (parameters != null && parameters.Length > 0)
                {
                    foreach (var param in parameters)
                    {
                        var clonedParam = command.CreateParameter();
                        clonedParam.ParameterName = param.ParameterName;
                        clonedParam.Value = param.Value ?? DBNull.Value;
                        clonedParam.DbType = param.DbType;
                        clonedParam.Direction = param.Direction;
                        if (param.Size > 0) clonedParam.Size = param.Size;
                        command.Parameters.Add(clonedParam);
                    }
                }
                
                rows = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new SqlExecutionException(string.Format("Error occurred during SQL query execution {0}", sqlQuery), ex);
            }
            finally
            {
                if (connection.State == ConnectionState.Open) connection.Close();
            }
            return rows;
        }

        public static int dbQueryCount(string sqlQuery, GdiPlataformEntities db)
        {
            int resultado = 0;
            try
            {
                DataTable tableTemp1 = null;
                tableTemp1 = LibDB.GetDataTable(sqlQuery, db);
                List<DataRow> allRecords = null;
                allRecords = tableTemp1.AsEnumerable().ToList();
                var dsRow = allRecords.FirstOrDefault();
                // SQL Server: verificar NULL antes de acessar valores
                if (dsRow != null && dsRow[0] != null && !DBNull.Value.Equals(dsRow[0]))
                {
                    resultado = int.Parse(dsRow[0].EmptyIfNull().ToString().Trim());
                }
            }
            catch (Exception) { }
            return resultado;
        }

        /// <summary>Filtro persistido em CachePersister.userIdentity.allFiltros (token + controller). yesFilterField=="*" limpa.</summary>
        public static g_filtros getFilterByUser(jQueryDataTableParamModel param, string controllerName, GdiPlataformEntities db)
        {
            g_filtros record_g_filtro = new g_filtros();

            if (param.yesFilterField.EmptyIfNull().ToString().Equals("*"))
            {
                String _token = CachePersister.userIdentity.TokenAcesso.EmptyIfNull().ToString().Trim();
                CachePersister.userIdentity.allFiltros.Remove(CachePersister.userIdentity.allFiltros.Where(f => f.token == _token && f.controller == controllerName).FirstOrDefault());
                param.yesFilterField = String.Empty;
                record_g_filtro.sql_filtro = string.Empty;
            }
            else
            {
                String _token = CachePersister.userIdentity.TokenAcesso.EmptyIfNull().ToString().Trim();
                record_g_filtro = CachePersister.userIdentity.allFiltros.Where(f => f.token == _token && f.controller == controllerName).FirstOrDefault();
                if (record_g_filtro == null)
                {
                    record_g_filtro = new g_filtros();
                    record_g_filtro.sql_filtro = string.Empty;
                }
            }
            return record_g_filtro;
        }


        public static void setFilterByUser(string filtro, string controllerName, bool paramAdvanced, GdiPlataformEntities db)
        {
            // Remover o Filtro
            String _token = CachePersister.userIdentity.TokenAcesso.EmptyIfNull().ToString().Trim();
            CachePersister.userIdentity.allFiltros.Remove(CachePersister.userIdentity.allFiltros.Where(f => f.token == _token && f.controller == controllerName).FirstOrDefault());

            // Gravação do Filtro
            g_filtros record_g_filtro = new g_filtros();
            record_g_filtro.token = _token;
            record_g_filtro.sql_filtro = filtro;
            record_g_filtro.advanced = paramAdvanced;
            record_g_filtro.controller = controllerName;
            record_g_filtro.session = CachePersister.userIdentity.SessionID.EmptyIfNull().ToString().Trim();
            CachePersister.userIdentity.allFiltros.Add(record_g_filtro);
        }

        public static int GetNextGcLancamentosFinanceiroOrdemPagamento(int IdContaCaixa, DateTime DataReferencia, GdiPlataformEntities db)
        {
            int Resultado = 0;
            try
            {
                // SQL Server: usar parâmetros SQL para segurança
                String SqlQuery = "SELECT MAX([ordem_pagamento]) FROM [gc_financeiro_lancamentos] WHERE [id_conta_caixa] = @idContaCaixa AND CAST([data_pagamento] AS DATE) = CAST(@dataReferencia AS DATE)";
                
                var factory = System.Data.Common.DbProviderFactories.GetFactory(db.Database.Connection);
                var param1 = factory.CreateParameter();
                param1.ParameterName = "@idContaCaixa";
                param1.Value = IdContaCaixa;
                param1.DbType = System.Data.DbType.Int32;
                
                var param2 = factory.CreateParameter();
                param2.ParameterName = "@dataReferencia";
                param2.Value = DataReferencia.ToString("yyyy-MM-dd");
                param2.DbType = System.Data.DbType.String;
                param2.Size = 10;
                
                DataTable tableTemp1 = null;
                tableTemp1 = LibDB.GetDataTable(SqlQuery, db, param1, param2);
                List<DataRow> allRecords = null;
                allRecords = tableTemp1.AsEnumerable().ToList();
                var dsRow = allRecords.FirstOrDefault();
                if (dsRow != null && dsRow[0] != null && !DBNull.Value.Equals(dsRow[0]))
                {
                    Resultado = int.Parse(dsRow[0].EmptyIfNull().ToString().Trim());
                    Resultado += 10;
                }
            }
            catch (Exception) { }
            return Resultado;
        }

        public static gc_movimentos_itens CloneGcMovimentosItens(gc_movimentos_itens RecordItemOrigem)
        {
            gc_movimentos_itens RecordItemNovo = new gc_movimentos_itens();
            RecordItemNovo.id_movimento = RecordItemOrigem.id_movimento;
            RecordItemNovo.id_produto = RecordItemOrigem.id_produto;
            RecordItemNovo.id_produto_condicao = RecordItemOrigem.id_produto_condicao;
            RecordItemNovo.id_entrega_prazo = RecordItemOrigem.id_entrega_prazo;
            RecordItemNovo.sequencia = RecordItemOrigem.sequencia;
            RecordItemNovo.quantidade = RecordItemOrigem.quantidade;
            RecordItemNovo.valor_unit = RecordItemOrigem.valor_unit;
            RecordItemNovo.valor_total = RecordItemOrigem.valor_total;
            RecordItemNovo.valor_total_corecharge = RecordItemOrigem.valor_total_corecharge;
            RecordItemNovo.valor_unit_corecharge = RecordItemOrigem.valor_unit_corecharge;
            RecordItemNovo.valor_total_trib = RecordItemOrigem.valor_total_trib;
            RecordItemNovo.valor_desconto = RecordItemOrigem.valor_desconto;
            RecordItemNovo.valor_frete = RecordItemOrigem.valor_frete;
            RecordItemNovo.valor_seguro = RecordItemOrigem.valor_seguro;
            RecordItemNovo.valor_despesas = RecordItemOrigem.valor_despesas;
            RecordItemNovo.icms_orig = RecordItemOrigem.icms_orig;
            RecordItemNovo.icms_cst = RecordItemOrigem.icms_cst;
            RecordItemNovo.icms_modbc = RecordItemOrigem.icms_modbc;
            RecordItemNovo.icms_predbc = RecordItemOrigem.icms_predbc;
            RecordItemNovo.icms_vbc = RecordItemOrigem.icms_vbc;
            RecordItemNovo.icms_picms = RecordItemOrigem.icms_picms;
            RecordItemNovo.icms_vicms = RecordItemOrigem.icms_vicms;
            RecordItemNovo.icms_modbcst = RecordItemOrigem.icms_modbcst;
            RecordItemNovo.icms_pmvast = RecordItemOrigem.icms_pmvast;
            RecordItemNovo.icms_predbcst = RecordItemOrigem.icms_predbcst;
            RecordItemNovo.icms_vbcst = RecordItemOrigem.icms_vbcst;
            RecordItemNovo.icms_picmsst = RecordItemOrigem.icms_picmsst;
            RecordItemNovo.icms_vicmsst = RecordItemOrigem.icms_vicmsst;
            RecordItemNovo.ipi_qselo = RecordItemOrigem.ipi_qselo;
            RecordItemNovo.ipi_cenq = RecordItemOrigem.ipi_cenq;
            RecordItemNovo.ipi_cst = RecordItemOrigem.ipi_cst;
            RecordItemNovo.ipi_vbc = RecordItemOrigem.ipi_vbc;
            RecordItemNovo.ipi_pipi = RecordItemOrigem.ipi_pipi;
            RecordItemNovo.ipi_vipi = RecordItemOrigem.ipi_vipi;
            RecordItemNovo.pis_cst = RecordItemOrigem.pis_cst;
            RecordItemNovo.pis_vbc = RecordItemOrigem.pis_vbc;
            RecordItemNovo.pis_ppis = RecordItemOrigem.pis_ppis;
            RecordItemNovo.pis_qbcprod = RecordItemOrigem.pis_qbcprod;
            RecordItemNovo.pis_valiqprod = RecordItemOrigem.pis_valiqprod;
            RecordItemNovo.pis_vpis = RecordItemOrigem.pis_vpis;
            RecordItemNovo.cofins_cst = RecordItemOrigem.cofins_cst;
            RecordItemNovo.cofins_vbc = RecordItemOrigem.cofins_vbc;
            RecordItemNovo.cofins_pcofins = RecordItemOrigem.cofins_pcofins;
            RecordItemNovo.cofins_vcofins = RecordItemOrigem.cofins_vcofins;
            RecordItemNovo.ii_vbc = RecordItemOrigem.ii_vbc;
            RecordItemNovo.ii_vdespadu = RecordItemOrigem.ii_vdespadu;
            RecordItemNovo.ii_vii = RecordItemOrigem.ii_vii;
            RecordItemNovo.ii_viof = RecordItemOrigem.ii_viof;
            RecordItemNovo.afrmm_valor = RecordItemOrigem.afrmm_valor;
            RecordItemNovo.di_numero = RecordItemOrigem.di_numero;
            RecordItemNovo.di_data = RecordItemOrigem.di_data;
            RecordItemNovo.di_loc_desemb = RecordItemOrigem.di_loc_desemb;
            RecordItemNovo.di_uf_desemb = RecordItemOrigem.di_uf_desemb;
            RecordItemNovo.di_data_desemb = RecordItemOrigem.di_data_desemb;
            RecordItemNovo.di_via_transp = RecordItemOrigem.di_via_transp;
            RecordItemNovo.di_tipo_itermedio = RecordItemOrigem.di_tipo_itermedio;
            RecordItemNovo.di_cnpj = RecordItemOrigem.di_cnpj;
            RecordItemNovo.di_uf_terceiro = RecordItemOrigem.di_uf_terceiro;
            RecordItemNovo.di_cod_exportador = RecordItemOrigem.di_cod_exportador;
            RecordItemNovo.di_adicao_numero = RecordItemOrigem.di_adicao_numero;
            RecordItemNovo.di_adicao_sequencial = RecordItemOrigem.di_adicao_sequencial;
            RecordItemNovo.di_adicao_fabricante = RecordItemOrigem.di_adicao_fabricante;
            RecordItemNovo.di_imposto_bc = RecordItemOrigem.di_imposto_bc;
            RecordItemNovo.di_desp_aduaneiras = RecordItemOrigem.di_desp_aduaneiras;
            RecordItemNovo.di_imposto_importacao = RecordItemOrigem.di_imposto_importacao;
            RecordItemNovo.di_imposto_iof = RecordItemOrigem.di_imposto_iof;
            RecordItemNovo.obs = RecordItemOrigem.obs;
            RecordItemNovo.id_coligada = RecordItemOrigem.id_coligada;
            RecordItemNovo.id_filial = RecordItemOrigem.id_filial;
            return RecordItemNovo;
        }

        public static string CompareGcMovimentos(gc_movimentos oldObject, gc_movimentos newObject, GdiPlataformEntities db)
        {
            String LogAlteracoes = string.Empty;

            // Tipo
            if (oldObject.id_movimento_tipo != newObject.id_movimento_tipo)
            {
                try
                {
                    List<gc_movimentos_tipos> AllMovimentosTipos = db.gc_movimentos_tipos.Where(p => p.id_movimento_tipo > 0).ToList();
                    LogAlteracoes += "Tipo: ";
                    if ((oldObject.id_movimento != 0) && (oldObject.id_movimento_tipo > 0)) { LogAlteracoes += AllMovimentosTipos.Where(v => v.id_movimento_tipo == oldObject.id_movimento_tipo).FirstOrDefault().descricao.EmptyIfNull().ToString() + " > "; }
                    if (newObject.id_movimento_tipo > 0) { LogAlteracoes += AllMovimentosTipos.Where(v => v.id_movimento_tipo == newObject.id_movimento_tipo).FirstOrDefault().descricao.EmptyIfNull().ToString() + " | "; };
                }
                catch (Exception) { };
            }
            if (oldObject.id_cliente != newObject.id_cliente)
            {
                try
                {
                    List<g_clientes> AllClientes = db.g_clientes.Where(p => p.id_cliente > 0).OrderBy(c => c.nome).ToList();
                    LogAlteracoes += "Cliente: ";
                    if (oldObject.id_cliente != 0) { LogAlteracoes += AllClientes.Where(c => c.id_cliente == oldObject.id_cliente).FirstOrDefault().nome.EmptyIfNull().ToString() + " > "; }
                    LogAlteracoes += AllClientes.Where(c => c.id_cliente == newObject.id_cliente).FirstOrDefault().nome.EmptyIfNull().ToString() + " | ";
                }
                catch (Exception) { };
            }
            // Vendedor
            if (oldObject.id_vendedor != newObject.id_vendedor)
            {
                try
                {
                    List<g_vendedores> AllVendedores = db.g_vendedores.Where(p => p.id_vendedor > 0).ToList();
                    LogAlteracoes += "Vendedor: ";
                    if (oldObject.id_vendedor != 0) { LogAlteracoes += AllVendedores.Where(v => v.id_vendedor == oldObject.id_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + " > "; }
                    LogAlteracoes += AllVendedores.Where(v => v.id_vendedor == newObject.id_vendedor).FirstOrDefault().nome.EmptyIfNull().ToString() + " | ";

                }
                catch (Exception) { };
            }
            // Finalidade
            if (oldObject.id_cfop_finalidade != newObject.id_cfop_finalidade)
            {
                try
                {
                    List<gc_cfop_finalidade> AllFinalidade = db.gc_cfop_finalidade.Where(p => p.id_cfop_finalidade > 0).ToList();
                    LogAlteracoes += "Finalidade: ";
                    if (oldObject.id_local_estoque != 0) { LogAlteracoes += AllFinalidade.Where(p => p.id_cfop_finalidade == oldObject.id_cfop_finalidade).FirstOrDefault().finalidade.EmptyIfNull().ToString() + " > "; }
                    LogAlteracoes += AllFinalidade.Where(p => p.id_cfop_finalidade == newObject.id_cfop_finalidade).FirstOrDefault().finalidade.EmptyIfNull().ToString() + " | ";

                }
                catch (Exception) { };
            }
            // Local de Estoque
            if (oldObject.id_local_estoque != newObject.id_local_estoque)
            {
                try
                {
                    List<gc_locais_estoque> AllLocaisEstoque = db.gc_locais_estoque.Where(p => p.id_local_estoque > 0).ToList();
                    LogAlteracoes += "Local Estoque: ";
                    if (oldObject.id_local_estoque != 0) { LogAlteracoes += AllLocaisEstoque.Where(p => p.id_local_estoque == oldObject.id_local_estoque).FirstOrDefault().sigla.EmptyIfNull().ToString() + " > "; }
                    LogAlteracoes += AllLocaisEstoque.Where(p => p.id_local_estoque == newObject.id_local_estoque).FirstOrDefault().sigla.EmptyIfNull().ToString() + " | ";
                }
                catch (Exception) { };
            }
            // Data Vencimento
            if (oldObject.data_vencimento != newObject.data_vencimento)
            {
                try
                {
                    if (oldObject.data_vencimento != null)
                    {
                        if (oldObject.data_vencimento.GetValueOrDefault().ToString("dd/MM/yyyy") != newObject.data_vencimento.GetValueOrDefault().ToString("dd/MM/yyyy"))
                        {
                            LogAlteracoes += "Dt. Venc: ";
                            LogAlteracoes += oldObject.data_vencimento.GetValueOrDefault().ToString("dd/MM/yyyy") + " > ";
                            LogAlteracoes += newObject.data_vencimento.GetValueOrDefault().ToString("dd/MM/yyyy") + " | ";
                        }
                    }
                    else
                    {
                        LogAlteracoes += "Dt. Venc: ";
                        LogAlteracoes += newObject.data_vencimento.GetValueOrDefault().ToString("dd/MM/yyyy") + " | ";
                    }
                }
                catch (Exception) { };
            }
            // Moeda
            if (oldObject.id_moeda != newObject.id_moeda)
            {
                try
                {
                    LogAlteracoes += "Moeda: ";
                    if (oldObject.id_moeda == 1) { LogAlteracoes += "R$ > "; } else if (oldObject.id_moeda == 2) { LogAlteracoes += "USD > "; } else { LogAlteracoes += ""; };
                    if (newObject.id_moeda == 1) { LogAlteracoes += "R$ "; } else if (newObject.id_moeda == 2) { LogAlteracoes += "USD "; } else { LogAlteracoes += ""; };
                    LogAlteracoes += " | ";

                }
                catch (Exception) { };
            }
            // US$. Pedido
            if (oldObject.cotacao_dolar_venda.ToString("0.0000") != newObject.cotacao_dolar_venda.ToString("0.0000"))
            {
                try
                {
                    LogAlteracoes += "US$. Pedido: ";
                    if (oldObject.cotacao_dolar_venda != 0) { LogAlteracoes += oldObject.cotacao_dolar_venda.ToString("0.0000") + " > "; };
                    LogAlteracoes += newObject.cotacao_dolar_venda.ToString("0.0000") + " | ";
                }
                catch (Exception) { };
            }
            // US$. Oficial
            if (oldObject.cotacao_dolar_oficial_venda.ToString("0.0000") != newObject.cotacao_dolar_oficial_venda.ToString("0.0000"))
            {
                LogAlteracoes += "US$. Oficial: ";
                if (oldObject.cotacao_dolar_oficial_venda != 0) { LogAlteracoes += oldObject.cotacao_dolar_oficial_venda.ToString("0.0000") + " > "; };
                LogAlteracoes += newObject.cotacao_dolar_oficial_venda.ToString("0.0000") + " | ";
            }
            // Pagto
            if (oldObject.id_pagrec_condicao != newObject.id_pagrec_condicao)
            {
                try
                {
                    List<g_pagrec_condicoes> AllCondicoes = db.g_pagrec_condicoes.Where(p => p.id_pagrec_condicao > 0).ToList();
                    LogAlteracoes += "Pagto: ";
                    if (oldObject.id_movimento > 0) { LogAlteracoes += AllCondicoes.Where(c => c.id_pagrec_condicao == oldObject.id_pagrec_condicao).FirstOrDefault().descricao.EmptyIfNull().ToString() + " > "; };
                    LogAlteracoes += AllCondicoes.Where(c => c.id_pagrec_condicao == newObject.id_pagrec_condicao).FirstOrDefault().descricao.EmptyIfNull().ToString() + " | ";
                }
                catch (Exception) { };
            }
            // Adiant. (R$)
            if (oldObject.valor_total_adiantamento.ToString("0.00") != newObject.valor_total_adiantamento.ToString("0.00"))
            {
                LogAlteracoes += "Adiant. (R$): ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", oldObject.valor_total_adiantamento) + " > "; };
                LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", newObject.valor_total_adiantamento) + " | ";
            }
            // Operação
            if (oldObject.id_cfop_operacao != newObject.id_cfop_operacao)
            {
                try
                {
                    List<gc_cfop_operacoes> AllOperacoes = db.gc_cfop_operacoes.Where(p => p.id_cfop_operacao > 0).ToList();
                    LogAlteracoes += "Operacao: ";
                    if (oldObject.id_movimento != 0) { LogAlteracoes += AllOperacoes.Where(p => p.id_cfop_operacao == oldObject.id_cfop_operacao).FirstOrDefault().descricao.EmptyIfNull().ToString() + " > "; }
                    LogAlteracoes += AllOperacoes.Where(p => p.id_cfop_operacao == newObject.id_cfop_operacao).FirstOrDefault().descricao.EmptyIfNull().ToString() + " | ";
                }
                catch (Exception) { };
            }
            // Benefício Aviação
            if (oldObject.has_beneficio_aviacao != newObject.has_beneficio_aviacao)
            {
                LogAlteracoes += "Benefício Aviação: ";
                if (oldObject.id_movimento > 0) { if (oldObject.has_beneficio_aviacao == false) { LogAlteracoes += "Não > "; } else { LogAlteracoes += "Sim > "; } };
                if (newObject.has_beneficio_aviacao == false) { LogAlteracoes += "Não | "; } else { LogAlteracoes += "Sim | "; }
            }
            // Prefixo
            if (oldObject.aeronave_prefixo != newObject.aeronave_prefixo)
            {
                LogAlteracoes += "Prefixo: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.aeronave_prefixo.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.aeronave_prefixo.EmptyIfNull().ToString() + " | ";
            }
            // Destinatário
            if (oldObject.id_cliente_destinatario != newObject.id_cliente_destinatario)
            {
                try
                {
                    if ((oldObject.id_cliente_destinatario > 0) || (newObject.id_cliente_destinatario > 0))
                    {
                        List<g_clientes_destinatarios> AllDestinatarios = db.g_clientes_destinatarios.Where(p => p.id_cliente == newObject.id_cliente).ToList();
                        LogAlteracoes += "Destinatário: ";
                        if ((oldObject.id_movimento > 0) && (oldObject.id_cliente_destinatario > 0))
                        {
                            LogAlteracoes += AllDestinatarios.Where(p => p.id_cliente_destinatario == oldObject.id_cliente_destinatario).FirstOrDefault().nome.EmptyIfNull().ToString() + " > ";
                        };
                        if (newObject.id_cliente_destinatario > 0)
                        {
                            LogAlteracoes += AllDestinatarios.Where(p => p.id_cliente_destinatario == newObject.id_cliente_destinatario).FirstOrDefault().nome.EmptyIfNull().ToString() + " |";
                        }
                        else
                        {
                            LogAlteracoes += "O Próprio Cliente |";
                        }
                    }
                }
                catch (Exception) { };
            }
            // OC
            if (oldObject.oc_numero != newObject.oc_numero)
            {
                LogAlteracoes += "Ordem de Compra: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.oc_numero.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.oc_numero.EmptyIfNull().ToString() + " | ";
            }
            // Obs Cotação
            if (oldObject.obs != newObject.obs)
            {
                if ((oldObject.obs.EmptyIfNull().ToString().Trim() != string.Empty) || (newObject.obs.EmptyIfNull().ToString().Trim() != string.Empty))
                {
                    LogAlteracoes += "Obs Cotação: ";
                    if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.obs.EmptyIfNull().ToString().Trim() + " > "; };
                    LogAlteracoes += newObject.obs.EmptyIfNull().ToString().Trim() + " | ";
                }
            }
            // Obs NF
            if (oldObject.informacoes_complementares_nf != newObject.informacoes_complementares_nf)
            {
                if ((oldObject.informacoes_complementares_nf.EmptyIfNull().ToString().Trim() != string.Empty) || (newObject.informacoes_complementares_nf.EmptyIfNull().ToString().Trim() != string.Empty))
                {
                    LogAlteracoes += "Obs NF: ";
                    if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.informacoes_complementares_nf.EmptyIfNull().ToString().Trim() + " > "; };
                    LogAlteracoes += newObject.informacoes_complementares_nf.EmptyIfNull().ToString().Trim() + " | ";
                }
            }
            // Frete
            if (oldObject.frete_valor.ToString("0.0000") != newObject.frete_valor.ToString("0.0000"))
            {
                LogAlteracoes += "R$ Frete: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", oldObject.frete_valor) + " > "; };
                LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", newObject.frete_valor) + " | ";
            }
            // Frete Gerencial
            if (oldObject.frete_gerencial.ToString("0.0000") != newObject.frete_gerencial.ToString("0.0000"))
            {
                LogAlteracoes += "R$ Frete Gerencial: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", oldObject.frete_gerencial) + " > "; };
                LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", newObject.frete_gerencial) + " | ";
            }
            // Frete Custo
            if (oldObject.frete1_custo.ToString("0.0000") != newObject.frete1_custo.ToString("0.0000"))
            {
                LogAlteracoes += "R$ Frete Custo: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", oldObject.frete1_custo) + " > "; };
                LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", newObject.frete1_custo) + " | ";
            }
            // Frete Responsável
            if (oldObject.id_frete_responsavel != newObject.id_frete_responsavel)
            {
                List<gc_frete_responsavel> AllFreteResponsavel = db.gc_frete_responsavel.Where(p => p.id_frete_responsavel > 0).ToList();
                LogAlteracoes += "Resp. Frete: ";
                if (oldObject.id_movimento != 0) { LogAlteracoes += AllFreteResponsavel.Where(p => p.id_frete_responsavel == oldObject.id_frete_responsavel).FirstOrDefault().descricao.EmptyIfNull().ToString() + " > "; }
                LogAlteracoes += AllFreteResponsavel.Where(p => p.id_frete_responsavel == newObject.id_frete_responsavel).FirstOrDefault().descricao.EmptyIfNull().ToString() + " | ";
            }
            // Transportadora
            if (oldObject.frete1_transportadora != newObject.frete1_transportadora)
            {
                try
                {
                    List<g_clientes> AllTransportadoras = db.g_clientes.Where(p => p.param_gc_transportadora == true).ToList();
                    LogAlteracoes += "Transportadora: ";
                    if (oldObject.id_movimento != 0) 
                    {
                        if (oldObject.frete1_transportadora == 0) { LogAlteracoes += "Cliente Retira > "; }
                        else { LogAlteracoes += AllTransportadoras.Where(p => p.id_cliente == oldObject.frete1_transportadora).FirstOrDefault().nome.EmptyIfNull().ToString() + " > "; };

                    }
                    if (newObject.frete1_transportadora == 0) { LogAlteracoes += "Cliente Retira |"; }
                    else { LogAlteracoes += AllTransportadoras.Where(p => p.id_cliente == newObject.frete1_transportadora).FirstOrDefault().nome.EmptyIfNull().ToString() + " | "; };
                }
                catch (Exception) { };
            }
            // Nº Cotação
            if (oldObject.frete1_documento != newObject.frete1_documento)
            {
                LogAlteracoes += "Nº Cotação: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.frete1_documento.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.frete1_documento.EmptyIfNull().ToString() + " | ";
            }
            // Nº Rastreio
            if (oldObject.frete1_rastreio != newObject.frete1_rastreio)
            {
                LogAlteracoes += "Nº Rastreio: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.frete1_rastreio.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.frete1_rastreio.EmptyIfNull().ToString() + " | ";
            }
            // Obs Frete
            if (oldObject.frete_observacoes != newObject.frete_observacoes)
            {
                LogAlteracoes += "Obs Frete: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.frete_observacoes.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.frete_observacoes.EmptyIfNull().ToString() + " | ";
            }
            // Transportadora Complementar
            if ((oldObject.frete2_transportadora != newObject.frete2_transportadora) && (oldObject.frete2_transportadora > -1 && newObject.frete2_transportadora > -1))
            {
                try
                {
                    List<g_clientes> AllTransportadoras = db.g_clientes.Where(p => p.param_gc_transportadora == true).ToList();
                    LogAlteracoes += "Transp. Complementar: ";
                    if (oldObject.id_movimento != 0) { LogAlteracoes += AllTransportadoras.Where(p => p.id_cliente == oldObject.frete2_transportadora).FirstOrDefault().nome.EmptyIfNull().ToString() + " > "; }
                    LogAlteracoes += AllTransportadoras.Where(p => p.id_cliente == newObject.frete2_transportadora).FirstOrDefault().nome.EmptyIfNull().ToString() + " | ";
                }
                catch (Exception) { };
            }
            // Frete Custo
            if (oldObject.frete2_custo.ToString("0.0000") != newObject.frete2_custo.ToString("0.0000"))
            {
                LogAlteracoes += "R$ Custo Frete Complementar: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", oldObject.frete2_custo) + " > "; };
                LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", newObject.frete2_custo) + " | ";
            }
            // Nº Cotação
            if (oldObject.frete2_documento != newObject.frete2_documento)
            {
                LogAlteracoes += "Nº Cotação Frete Complementar: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.frete2_documento.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.frete2_documento.EmptyIfNull().ToString() + " | ";
            }
            // Nº Rastreio
            if (oldObject.frete2_rastreio != newObject.frete2_rastreio)
            {
                LogAlteracoes += "Nº Rastreio Frete Complementar: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.frete2_rastreio.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.frete2_rastreio.EmptyIfNull().ToString() + " | ";
            }
            // Aeronave Modelo
            if (oldObject.aeronave_modelo != newObject.aeronave_modelo)
            {
                LogAlteracoes += "Modelo Aeronave: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.aeronave_modelo.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.aeronave_modelo.EmptyIfNull().ToString() + " | ";
            }
            // Aeronave Serie
            if (oldObject.aeronave_modelo != newObject.aeronave_modelo)
            {
                LogAlteracoes += "Série Aeronave: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.aeronave_serie.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.aeronave_serie.EmptyIfNull().ToString() + " | ";
            }
            // Aeronave Protocolo
            if (oldObject.aeronave_modelo != newObject.aeronave_modelo)
            {
                LogAlteracoes += "Protocolo Aeronave: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.aeronave_registro.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.aeronave_registro.EmptyIfNull().ToString() + " | ";
            }
            // Chave Referência NFe
            if (oldObject.aeronave_modelo != newObject.aeronave_modelo)
            {
                LogAlteracoes += "Chave Referência NFe: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.nf_chave_referenciada.EmptyIfNull().ToString() + " > "; };
                LogAlteracoes += newObject.nf_chave_referenciada.EmptyIfNull().ToString() + " | ";
            }
            // Qtd Itens
            if (oldObject.qtd_itens.ToString() != newObject.qtd_itens.ToString())
            {
                LogAlteracoes += "Qtd Itens: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.qtd_itens.ToString() + " > "; };
                LogAlteracoes += newObject.qtd_itens.ToString() + " | ";
            }
            // R$ Total
            if (oldObject.valor_total_bruto.ToString("0.00") != newObject.valor_total_bruto.ToString("0.00"))
            {
                LogAlteracoes += "R$ Total: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", oldObject.valor_total_bruto) + " > "; };
                LogAlteracoes += string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", newObject.valor_total_bruto) + " | ";
            }
            // Core
            if (oldObject.valor_total_corecharge.ToString("0.00") != newObject.valor_total_corecharge.ToString("0.00"))
            {
                LogAlteracoes += "Core: ";
                if (oldObject.id_movimento > 0) { LogAlteracoes += oldObject.valor_total_corecharge.ToString("0.00") + " > "; };
                LogAlteracoes += newObject.valor_total_corecharge.ToString("0.00") + " | ";
            }
            return LogAlteracoes;
        }
        public static string CompareDataTable<T>(T oldObject, T newObject)
        {
            string Comparacao = string.Empty;
            int IndexField = 0;
            bool NewRecord = false;
            try
            {
                var oldProperties = oldObject.GetType().GetProperties();
                foreach (PropertyInfo newProperty in newObject.GetType().GetProperties())
                {
                    IndexField += 1;
                    try
                    {
                        PropertyInfo oldProperty = oldProperties.Single<PropertyInfo>(pi => pi.Name == newProperty.Name);
                        object NewValue = newProperty.GetValue(newObject, null);
                        object OldValue = oldProperty.GetValue(oldObject, null);
                        String NewValueCompare = string.Empty;
                        String OldValueCompare = string.Empty;

                        if ((NewValue != null) && (OldValue != null) && (NewValue.GetType() == typeof(decimal)) && (OldValue.GetType() == typeof(decimal)))
                        {
                            NewValueCompare = String.Format("{0:0.00####}", NewValue);
                            OldValueCompare = String.Format("{0:0.00####}", OldValue);
                        }
                        else
                        {
                            NewValueCompare = NewValue.EmptyIfNull().ToString();
                            OldValueCompare = OldValue.EmptyIfNull().ToString();
                        }

                        if ((IndexField == 1) && (oldProperty.GetValue(oldObject, null).EmptyIfNull().ToString().Trim() == "0")) { NewRecord = true; };

                        if (NewValueCompare != OldValueCompare)
                        {
                            if ((newProperty.Name.EmptyIfNull().ToString() != "id_usuario_alteracao") && (newProperty.Name.EmptyIfNull().ToString() != "datahora_alteracao"))
                            {
                                if (NewRecord) { Comparacao += newProperty.Name.EmptyIfNull().ToString().Trim() + ": " + newProperty.GetValue(newObject, null).EmptyIfNull().ToString().Trim() + " | "; }
                                else { Comparacao += newProperty.Name.EmptyIfNull().ToString().Trim() + ": " + oldProperty.GetValue(oldObject, null).EmptyIfNull().ToString().Trim() + " > " + newProperty.GetValue(newObject, null).EmptyIfNull().ToString().Trim() + " | "; };
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        return LibExceptions.getExceptionShortMessage(e);
                    }
                }
            }
            catch (Exception) { };
            return Comparacao;
        }

        public static T CloneTObject<T>(T obj)
        {
            var ObjSaida = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(ObjSaida);
        }

        /// <summary>TTL absoluto entre verificações MAX (PERF-015). Alinhado ao sliding 15 min do MemoryCache de lookups.</summary>
        private static readonly TimeSpan IsTableUpdateTtlDefault = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan IsTableUpdateTtlLargeTable = TimeSpan.FromMinutes(15);
        private static readonly HashSet<string> IsTableUpdateLargeTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "g_clientes",
            "g_produtos"
        };

        private static TimeSpan GetIsTableUpdateVerificationTtl(string tableName)
        {
            if (!string.IsNullOrEmpty(tableName) && IsTableUpdateLargeTables.Contains(tableName))
                return IsTableUpdateTtlLargeTable;
            return IsTableUpdateTtlDefault;
        }

        private static bool IsTableUpdateVerificationFresh(ModelControlTableUpdate record, string tableName)
        {
            if (record == null || record.DateTimeLastVerified == default(DateTime)) return false;
            var elapsed = LibDateTime.getDataHoraBrasilia() - record.DateTimeLastVerified;
            return elapsed >= TimeSpan.Zero && elapsed < GetIsTableUpdateVerificationTtl(tableName);
        }

        /// <summary>Força próxima IsTableUpdate a executar MAX (ex.: invalidação explícita de lookup).</summary>
        public static void ResetTableUpdateVerification(string dataTableName)
        {
            if (string.IsNullOrWhiteSpace(dataTableName)) return;
            var ui = CachePersister.userIdentity;
            if (ui?.ListTablesUpdate == null) return;
            foreach (var row in ui.ListTablesUpdate.Where(u =>
                string.Equals(u.TableName, dataTableName, StringComparison.OrdinalIgnoreCase)))
            {
                row.DateTimeLastVerified = default(DateTime);
            }
        }

        public static bool IsTableUpdate(String DataTableName, String ProcessName, GdiPlataformEntities db)
        {
            bool TableUpdate = false;
            DateTime DataHoraCadastro;
            DateTime DataHoraAlteracao;
            DateTime LastDateTimeUpdateTable;
            DateTime DateTimeUpdateTable = DateTime.Now.AddDays(-1); 
            String SqlQuery = string.Empty;
            ModelControlTableUpdate RecordModelControlTableUpdate;
            try
            {
                if (CachePersister.userIdentity == null) return true;
                if (CachePersister.userIdentity.ListTablesUpdate == null)
                {
                    CachePersister.userIdentity.ListTablesUpdate = new List<ModelControlTableUpdate>();
                }

                RecordModelControlTableUpdate = CachePersister.userIdentity.ListTablesUpdate.Where(u => u.TableName == DataTableName && u.ProcessName == ProcessName).FirstOrDefault();

                if (RecordModelControlTableUpdate != null) { LastDateTimeUpdateTable = RecordModelControlTableUpdate.DateTimeUpdate; }
                else
                {
                    RecordModelControlTableUpdate = new ModelControlTableUpdate();
                    RecordModelControlTableUpdate.TableName = DataTableName;
                    RecordModelControlTableUpdate.ProcessName = ProcessName;
                    RecordModelControlTableUpdate.DateTimeUpdate = DateTime.Now.AddDays(-1);
                    RecordModelControlTableUpdate.DateTimeLastVerified = default(DateTime);
                    CachePersister.userIdentity.ListTablesUpdate.Add(RecordModelControlTableUpdate);
                    LastDateTimeUpdateTable = DateTime.Now.AddYears(-1);
                }

                // SQL Server: validar nome de tabela para evitar SQL Injection
                string tableName = DataTableName.EmptyIfNull().ToString().Trim();
                if (string.IsNullOrWhiteSpace(tableName) || !System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                {
                    throw new ArgumentException("Nome de tabela inválido: " + tableName);
                }

                // PERF-015: carimbo fresco na sessão — combo cacheado não repete MAX na mesma janela TTL
                if (IsTableUpdateVerificationFresh(RecordModelControlTableUpdate, tableName))
                {
                    return false;
                }

                SqlQuery = "SELECT MAX([datahora_cadastro]) AS [datahora_cadastro], MAX([datahora_alteracao]) AS [datahora_alteracao] FROM [" + tableName + "]";
                DataTable tableTemp1 = null;
                tableTemp1 = LibDB.GetDataTable(SqlQuery, db);
                List<DataRow> allRecords = null;
                allRecords = tableTemp1.AsEnumerable().ToList();
                if (allRecords.Count > 0)
                {
                    var dsRow = allRecords.FirstOrDefault();
                    if (dsRow != null)
                    {
                        // SQL Server: tratar NULL retornado por MAX() quando não há registros
                        if (!DBNull.Value.Equals(dsRow["datahora_alteracao"]) && dsRow["datahora_alteracao"] != null && TableUpdate == false)
                        {
                            DataHoraAlteracao = Convert.ToDateTime(dsRow["datahora_alteracao"]);
                            if (DataHoraAlteracao > LastDateTimeUpdateTable) { TableUpdate = true; DateTimeUpdateTable = DataHoraAlteracao; }
                        }
                        if (!DBNull.Value.Equals(dsRow["datahora_cadastro"]) && dsRow["datahora_cadastro"] != null && TableUpdate == false)
                        {
                            DataHoraCadastro = Convert.ToDateTime(dsRow["datahora_cadastro"]);
                            if (DataHoraCadastro > LastDateTimeUpdateTable) { TableUpdate = true; DateTimeUpdateTable = DataHoraCadastro; }
                        }
                    }
                }
                var stampRow = CachePersister.userIdentity.ListTablesUpdate
                    .FirstOrDefault(u => u.TableName == DataTableName && u.ProcessName == ProcessName);
                if (stampRow != null)
                {
                    stampRow.DateTimeLastVerified = LibDateTime.getDataHoraBrasilia();
                    if (TableUpdate)
                        stampRow.DateTimeUpdate = DateTimeUpdateTable;
                }
            }
            catch (Exception) 
            {
                TableUpdate = true; // Forçar a atualizar o cache
            }
            if (TableUpdate)
            {
                GdiPlataform.Lib.Lookups.LookupCacheInvalidator.OnTableUpdated(DataTableName);
            }
            return TableUpdate;
        }
    }
}