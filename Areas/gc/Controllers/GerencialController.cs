using GdiPlataform.Areas.g.Models;
using GdiPlataform.Areas.gc.Models;
using GdiPlataform.Db;
using GdiPlataform.Robos.Nfe;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace GdiPlataform.Areas.gc.Controllers
{
    public class GerencialController : Controller
    {
        private GdiPlataformEntities db;

        public GerencialController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }

        // GET: gc/Gerencial
        public ActionResult IndexPainelComercialGerencial()
        {
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            int QtdCotacoes = 0;
            int QtdPedidosAprovados = 0;
            int QtdPedidosSeparados = 0;
            int QtdPedidosFaturados = 0;
            int QtdPedidosNotaFiscal = 0;
            int QtdPedidosExpedidos = 0;
            int QtdPedidosEntregues = 0;
            int QtdPedidosProcessamentoTOTAL = 0;

            int QtdPedidosGDIHoje = 0;
            int QtdPedidosSCHoje = 0;
            int QtdPedidosGDIMes = 0;
            int QtdPedidosSCMes = 0;
            int QtdPedidosDiarioTOTAL = 0;
            int QtdPedidosMesTOTAL = 0;

            Decimal ValorPedidosGDIHoje = 0;
            Decimal ValorPedidosSCHoje = 0;
            Decimal ValorPedidosGDIMes = 0;
            Decimal ValorPedidosSCMes = 0;

            bool ListarPedido = false;
            Decimal ValorTotalCotacoes = 0;
            Decimal ValorTotalPedidos = 0;
            List<int> ListaPedidosProcessamento = new List<int>();
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            ListaPedidosProcessamento.Add(0);
            String SentencaSQL = string.Empty;
            String sentencaSqlDiario = string.Empty;
            String sentencaSqlMes = string.Empty;
            String StatusPedido = string.Empty;
            CstPainelComercialGerencial ViewcstPainelComercialGerencial = new CstPainelComercialGerencial();


            //********** PEDIDOS EM PROCESSAMENTO **********//
            SentencaSQL = string.Empty;
            /*SentencaSQL += " select gc_movimentos.*, ";
            SentencaSQL += " g_clientes.nome as [cliente], g_vendedores.nome as \"vendedor\", gc_movimentos_nf.nf_numero as \"nota_fiscal\", gc_locais_estoque.sigla as \"estoque\" ";
            SentencaSQL += " from gc_movimentos ";
            SentencaSQL += " join gc_locais_estoque on (gc_locais_estoque.id_local_estoque = gc_movimentos.id_local_estoque) ";
            SentencaSQL += " left join g_clientes on (gc_movimentos.id_cliente = g_clientes.id_cliente) ";
            SentencaSQL += " left join g_vendedores on (gc_movimentos.id_vendedor = g_vendedores.id_vendedor) ";
            SentencaSQL += " left join gc_movimentos_nf on (gc_movimentos_nf.id_movimento = gc_movimentos.id_movimento and gc_movimentos_nf.id_nfe_status in (8, 17, 22))  ";
            SentencaSQL += " join gc_cfop_operacoes on(gc_cfop_operacoes.id_cfop_operacao = gc_movimentos.id_cfop_operacao) ";
            SentencaSQL += " where  gc_movimentos.id_movimento_status = 2 ";
            SentencaSQL += " and gc_movimentos.movimento_aprovado = 1 ";
            SentencaSQL += " and gc_movimentos.id_movimento_posicao in (1,2,3,4,5) ";
            SentencaSQL += " and gc_movimentos.id_local_estoque > 0 ";
            SentencaSQL += " and gc_movimentos.datahora_aprovacao >= '2023-04-17' ";
            SentencaSQL += " and gc_cfop_operacoes.is_venda = 1 ";
            SentencaSQL += " order by gc_movimentos.datahora_aprovacao desc ";*/

            SentencaSQL += " SELECT m.*, c.nome AS[cliente], v.nome AS[vendedor], mnf.nf_numero AS[nota_fiscal], le.sigla AS[estoque] ";
            SentencaSQL += " FROM gc_movimentos AS m ";
            SentencaSQL += " JOIN gc_locais_estoque AS le ON le.id_local_estoque = m.id_local_estoque ";
            SentencaSQL += " LEFT JOIN g_clientes AS c ON m.id_cliente = c.id_cliente ";
            SentencaSQL += " LEFT JOIN g_vendedores AS v ON m.id_vendedor = v.id_vendedor ";
            SentencaSQL += " OUTER APPLY ( ";
            SentencaSQL += "     SELECT TOP 1 mnf_inner.nf_numero ";
            SentencaSQL += "     FROM gc_movimentos_nf AS mnf_inner ";
            SentencaSQL += "     WHERE mnf_inner.id_movimento = m.id_movimento ";
            SentencaSQL += "       AND mnf_inner.id_nfe_status IN (8, 17, 22) ";
            SentencaSQL += "     ORDER BY mnf_inner.id_movimento_nf DESC ";
            SentencaSQL += " ) AS mnf ";
            SentencaSQL += " JOIN gc_cfop_operacoes AS co ON co.id_cfop_operacao = m.id_cfop_operacao ";
            SentencaSQL += " WHERE m.id_movimento_status = 2 ";
            SentencaSQL += " AND m.movimento_aprovado = 1 ";
            SentencaSQL += " AND m.id_movimento_posicao IN(1,2,3,4,5) ";
            SentencaSQL += " AND m.id_local_estoque > 0 ";
            SentencaSQL += " AND m.datahora_aprovacao >= DATEFROMPARTS(2023, 4, 17) ";
            SentencaSQL += " AND co.is_venda = 1 ";
            SentencaSQL += " ORDER BY m.datahora_aprovacao DESC; ";

            String DataHoraAprovacaoFormatada = string.Empty;
            String PosicaoMovimento = string.Empty;
            String AtividadeMovimento = string.Empty;

            System.Data.DataTable tableItem = LibDB.GetDataTable(SentencaSQL, db);
            List<DataRow> allItens = tableItem.AsEnumerable().ToList();
            int Index = 0;
            foreach (var dsRowItem in allItens)
            {
                int IndexVendedor = 0;
                decimal valorTotal = 0;
                decimal.TryParse(dsRowItem["valor_total_bruto"].EmptyIfNull().ToString(), out valorTotal);
                ListarPedido = false;
                QtdCotacoes += 1;
                ValorTotalCotacoes += valorTotal;
                AtividadeMovimento = string.Empty;
                if (dsRowItem["id_movimento_posicao"].EmptyIfNull().ToString() == "1")
                {
                    ListarPedido = true;
                    QtdPedidosAprovados += 1;
                }
                else if (dsRowItem["id_movimento_posicao"].EmptyIfNull().ToString() == "2")
                {
                    ListarPedido = true;
                    QtdPedidosSeparados += 1;
                }
                else if (dsRowItem["id_movimento_posicao"].EmptyIfNull().ToString() == "3")
                {
                    ListarPedido = true;
                    QtdPedidosFaturados += 1;
                }
                else if (dsRowItem["id_movimento_posicao"].EmptyIfNull().ToString() == "4")
                {
                    ListarPedido = true;
                    QtdPedidosNotaFiscal += 1;
                }
                else if (dsRowItem["id_movimento_posicao"].EmptyIfNull().ToString() == "5")
                {
                    ListarPedido = true;
                    QtdPedidosExpedidos += 1;
                }
                else if (dsRowItem["id_movimento_posicao"].EmptyIfNull().ToString() == "6")
                {
                    ListarPedido = true;
                    QtdPedidosEntregues += 1;
                }

                if (ListarPedido == true)
                {
                    ValorTotalPedidos += valorTotal;
                    Index += 1;
                    int.TryParse(dsRowItem["id_vendedor"].EmptyIfNull().ToString().Trim(), out IndexVendedor);
                    ListaPedidosProcessamento[IndexVendedor] = ListaPedidosProcessamento[IndexVendedor] + 1;
                    QtdPedidosProcessamentoTOTAL += 1;
                }
            }

            //********** PEDIDOS DIÁRIO **********//
            ViewcstPainelComercialGerencial.QtdPedidosDiarioDaniel = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioGustavo = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioJoao = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioAndre = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioPaulo = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioVivian = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioCarlos = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioDeborah = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioLeonardo = "0";
            ViewcstPainelComercialGerencial.QtdPedidosDiarioTOTAL = "0";

            sentencaSqlDiario = string.Empty;
            sentencaSqlDiario += " SELECT ";
            sentencaSqlDiario += "     g_vendedores.id_vendedor, ";
            sentencaSqlDiario += "     g_vendedores.nome, ";
            sentencaSqlDiario += "     COUNT(*) AS qtd_diario, ";
            sentencaSqlDiario += "     SUM(gc_movimentos.valor_total_bruto) AS valor_total_bruto ";
            sentencaSqlDiario += " FROM gc_movimentos ";
            sentencaSqlDiario += " INNER JOIN g_vendedores ";
            sentencaSqlDiario += "     ON gc_movimentos.id_vendedor = g_vendedores.id_vendedor ";
            sentencaSqlDiario += " INNER JOIN gc_cfop_operacoes ";
            sentencaSqlDiario += "     ON gc_cfop_operacoes.id_cfop_operacao = gc_movimentos.id_cfop_operacao ";
            sentencaSqlDiario += " WHERE gc_movimentos.movimento_aprovado = 1 ";
            sentencaSqlDiario += "   AND COALESCE(gc_movimentos.datahora_nf, ( ";
            sentencaSqlDiario += "       SELECT MIN(nf_inner.nf_data_autorizacao) FROM gc_movimentos_nf nf_inner ";
            sentencaSqlDiario += "       WHERE nf_inner.id_movimento = gc_movimentos.id_movimento ";
            sentencaSqlDiario += "         AND nf_inner.id_nfe_status IN (8, 17, 22) ";
            sentencaSqlDiario += "         AND nf_inner.nf_data_autorizacao IS NOT NULL ";
            sentencaSqlDiario += "   )) BETWEEN ";
            sentencaSqlDiario += "       '" + DataHoraAtual.ToString("yyyy-MM-dd 00:00:00") + "' ";
            sentencaSqlDiario += "       AND '" + DataHoraAtual.ToString("yyyy-MM-dd 23:59:59") + "' ";
            sentencaSqlDiario += "   AND gc_cfop_operacoes.is_venda = 1 ";
            sentencaSqlDiario += "   AND EXISTS ( ";
            sentencaSqlDiario += "       SELECT 1 FROM gc_movimentos_nf ";
            sentencaSqlDiario += "       WHERE gc_movimentos_nf.id_movimento = gc_movimentos.id_movimento ";
            sentencaSqlDiario += "         AND gc_movimentos_nf.id_nfe_status IN (8, 17, 22) ";
            sentencaSqlDiario += "   ) ";
            sentencaSqlDiario += " GROUP BY ";
            sentencaSqlDiario += "     g_vendedores.id_vendedor, ";
            sentencaSqlDiario += "     g_vendedores.nome ";
            System.Data.DataTable TableDiario = LibDB.GetDataTable(sentencaSqlDiario, db);
            int QtdPedidosDiario = 0;
            Decimal ValorDiario = 0;
            List<DataRow> AllDiario = TableDiario.AsEnumerable().ToList();
            foreach (var dsRowDiario in AllDiario)
            {
                ValorDiario = 0;
                QtdPedidosDiario = 0;
                if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "1") { ViewcstPainelComercialGerencial.QtdPedidosDiarioDaniel = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                else if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "2") { ViewcstPainelComercialGerencial.QtdPedidosDiarioGustavo = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                else if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "3") { ViewcstPainelComercialGerencial.QtdPedidosDiarioJoao = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                else if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "4") { ViewcstPainelComercialGerencial.QtdPedidosDiarioAndre = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                else if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "6") { ViewcstPainelComercialGerencial.QtdPedidosDiarioPaulo = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                else if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "7") { ViewcstPainelComercialGerencial.QtdPedidosDiarioVivian = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                else if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "8") { ViewcstPainelComercialGerencial.QtdPedidosDiarioCarlos = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                else if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "9") { ViewcstPainelComercialGerencial.QtdPedidosDiarioDeborah = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                else if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "10") { ViewcstPainelComercialGerencial.QtdPedidosDiarioLeonardo = dsRowDiario["qtd_diario"].EmptyIfNull().ToString(); }
                decimal.TryParse(dsRowDiario["valor_total_bruto"].EmptyIfNull().ToString(), out ValorDiario);
                int.TryParse(dsRowDiario["qtd_diario"].EmptyIfNull().ToString(), out QtdPedidosDiario);

                if (dsRowDiario["id_vendedor"].EmptyIfNull().ToString() == "6") // SC
                {
                    QtdPedidosSCHoje += QtdPedidosDiario;
                    ValorPedidosSCHoje += ValorDiario;
                }
                else
                {
                    QtdPedidosGDIHoje += QtdPedidosDiario;
                    ValorPedidosGDIHoje += ValorDiario;
                }
            }
            QtdPedidosDiarioTOTAL = QtdPedidosSCHoje + QtdPedidosGDIHoje;


            //********** PEDIDOS MÊS **********//
            sentencaSqlMes = string.Empty;
            sentencaSqlMes += " SELECT ";
            sentencaSqlMes += "     g_vendedores.id_vendedor, ";
            sentencaSqlMes += "     g_vendedores.nome, ";
            sentencaSqlMes += "     COUNT(*) AS qtd_mes, ";
            sentencaSqlMes += "     SUM(gc_movimentos.valor_total_bruto) AS valor_total_bruto ";
            sentencaSqlMes += " FROM gc_movimentos ";
            sentencaSqlMes += " INNER JOIN g_vendedores ";
            sentencaSqlMes += "     ON gc_movimentos.id_vendedor = g_vendedores.id_vendedor ";
            sentencaSqlMes += " INNER JOIN gc_cfop_operacoes ";
            sentencaSqlMes += "     ON gc_cfop_operacoes.id_cfop_operacao = gc_movimentos.id_cfop_operacao ";
            sentencaSqlMes += " WHERE gc_movimentos.movimento_aprovado = 1 ";
            sentencaSqlMes += "   AND COALESCE(gc_movimentos.datahora_nf, ( ";
            sentencaSqlMes += "       SELECT MIN(nf_inner.nf_data_autorizacao) FROM gc_movimentos_nf nf_inner ";
            sentencaSqlMes += "       WHERE nf_inner.id_movimento = gc_movimentos.id_movimento ";
            sentencaSqlMes += "         AND nf_inner.id_nfe_status IN (8, 17, 22) ";
            sentencaSqlMes += "         AND nf_inner.nf_data_autorizacao IS NOT NULL ";
            sentencaSqlMes += "   )) BETWEEN ";
            sentencaSqlMes += "       '" + LibDateTime.getPrimeiroDiaMesAtual().ToString("yyyy-MM-dd 00:00:00") + "' ";
            sentencaSqlMes += "       AND '" + LibDateTime.getUltimoDiaMesAtual().ToString("yyyy-MM-dd 23:59:59") + "' ";
            sentencaSqlMes += "   AND gc_cfop_operacoes.is_venda = 1 ";
            sentencaSqlMes += "   AND EXISTS ( ";
            sentencaSqlMes += "       SELECT 1 FROM gc_movimentos_nf ";
            sentencaSqlMes += "       WHERE gc_movimentos_nf.id_movimento = gc_movimentos.id_movimento ";
            sentencaSqlMes += "         AND gc_movimentos_nf.id_nfe_status IN (8, 17, 22) ";
            sentencaSqlMes += "   ) ";
            sentencaSqlMes += " GROUP BY ";
            sentencaSqlMes += "     g_vendedores.id_vendedor, ";
            sentencaSqlMes += "     g_vendedores.nome ";
            System.Data.DataTable TableMes = LibDB.GetDataTable(sentencaSqlMes, db);
            int QtdPedidosMes = 0;
            Decimal ValorMes = 0;
            QtdPedidosSCMes = 0;
            ValorPedidosSCMes = 0;
            QtdPedidosGDIMes = 0;
            ValorPedidosGDIMes = 0;
            List<DataRow> AllMes = TableMes.AsEnumerable().ToList();
            foreach (var dsRowMes in AllMes)
            {
                ValorMes = 0;
                QtdPedidosMes = 0;

                if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "1") 
                { 
                    ViewcstPainelComercialGerencial.QtdPedidosMesDaniel = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesDaniel = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }
                else if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "2") 
                { 
                    ViewcstPainelComercialGerencial.QtdPedidosMesGustavo = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesGustavo = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }
                else if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "3") { 
                    ViewcstPainelComercialGerencial.QtdPedidosMesJoao = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesJoao = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }
                else if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "4") 
                { 
                    ViewcstPainelComercialGerencial.QtdPedidosMesAndre = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesAndre = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }
                else if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "6") 
                { 
                    ViewcstPainelComercialGerencial.QtdPedidosMesPaulo = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesPaulo = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }
                else if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "7") 
                { 
                    ViewcstPainelComercialGerencial.QtdPedidosMesVivian = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesVivian = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }
                else if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "8") 
                { 
                    ViewcstPainelComercialGerencial.QtdPedidosMesCarlos = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesCarlos = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }
                else if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "9")
                {
                    ViewcstPainelComercialGerencial.QtdPedidosMesDeborah = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesDeborah = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }
                else if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "10")
                {
                    ViewcstPainelComercialGerencial.QtdPedidosMesLeonardo = dsRowMes["qtd_mes"].EmptyIfNull().ToString();
                    ViewcstPainelComercialGerencial.ValorPedidosMesLeonardo = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString())).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
                }

                decimal.TryParse(dsRowMes["valor_total_bruto"].EmptyIfNull().ToString(), out ValorMes);
                int.TryParse(dsRowMes["qtd_mes"].EmptyIfNull().ToString(), out QtdPedidosMes);

                if (dsRowMes["id_vendedor"].EmptyIfNull().ToString() == "6") // SC
                {
                    QtdPedidosSCMes += QtdPedidosMes;
                    ValorPedidosSCMes += ValorMes;
                }
                else
                {
                    QtdPedidosGDIMes += QtdPedidosMes;
                    ValorPedidosGDIMes += ValorMes;
                }
            }
            QtdPedidosMesTOTAL = QtdPedidosSCMes + QtdPedidosGDIMes;

            //********** PEDIDOS ENTREGUES **********//
            string sentencaSqlPedidosEntregues = string.Empty;
            sentencaSqlPedidosEntregues += " SELECT ";
            sentencaSqlPedidosEntregues += "     COUNT(*) AS qtd_entregues ";
            sentencaSqlPedidosEntregues += " FROM gc_movimentos ";
            sentencaSqlPedidosEntregues += " INNER JOIN gc_cfop_operacoes ";
            sentencaSqlPedidosEntregues += "     ON gc_cfop_operacoes.id_cfop_operacao = gc_movimentos.id_cfop_operacao ";
            sentencaSqlPedidosEntregues += " WHERE gc_movimentos.movimento_aprovado = 1 ";
            sentencaSqlPedidosEntregues += "   AND gc_movimentos.id_movimento_posicao = 6 ";
            sentencaSqlPedidosEntregues += "   AND COALESCE(gc_movimentos.datahora_nf, ( ";
            sentencaSqlPedidosEntregues += "       SELECT MIN(nf_inner.nf_data_autorizacao) FROM gc_movimentos_nf nf_inner ";
            sentencaSqlPedidosEntregues += "       WHERE nf_inner.id_movimento = gc_movimentos.id_movimento ";
            sentencaSqlPedidosEntregues += "         AND nf_inner.id_nfe_status IN (8, 17, 22) ";
            sentencaSqlPedidosEntregues += "         AND nf_inner.nf_data_autorizacao IS NOT NULL ";
            sentencaSqlPedidosEntregues += "   )) BETWEEN ";
            sentencaSqlPedidosEntregues += "       '" + LibDateTime.getPrimeiroDiaMesAtual().ToString("yyyy-MM-dd 00:00:00") + "' ";
            sentencaSqlPedidosEntregues += "       AND '" + LibDateTime.getUltimoDiaMesAtual().ToString("yyyy-MM-dd 23:59:59") + "' ";
            sentencaSqlPedidosEntregues += "   AND gc_cfop_operacoes.is_venda = 1 ";
            sentencaSqlPedidosEntregues += "   AND EXISTS ( ";
            sentencaSqlPedidosEntregues += "       SELECT 1 FROM gc_movimentos_nf ";
            sentencaSqlPedidosEntregues += "       WHERE gc_movimentos_nf.id_movimento = gc_movimentos.id_movimento ";
            sentencaSqlPedidosEntregues += "         AND gc_movimentos_nf.id_nfe_status IN (8, 17, 22) ";
            sentencaSqlPedidosEntregues += "   ) ";
            System.Data.DataTable TablePedidosEntregues = LibDB.GetDataTable(sentencaSqlPedidosEntregues, db);
            List<DataRow> AllPedidosEntregues = TablePedidosEntregues.AsEnumerable().ToList();
            foreach (var dsRowPedidosEntregues in AllPedidosEntregues)
            {
                int.TryParse(dsRowPedidosEntregues["qtd_entregues"].EmptyIfNull().ToString(), out QtdPedidosEntregues);
            }


            ////////// Pedidos por Status ////////// 
            ViewcstPainelComercialGerencial.QtdPedidosAprovados = QtdPedidosAprovados.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosSeparados = QtdPedidosSeparados.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosFaturados = QtdPedidosFaturados.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosNotaFiscal = QtdPedidosNotaFiscal.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosExpedidos = QtdPedidosExpedidos.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosEntregues = QtdPedidosEntregues.ToString();


            ////////// Pedidos por Vendedor[Processamento | Diário | Mensal] //////////
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoAndre = ListaPedidosProcessamento[4].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoCarlos = ListaPedidosProcessamento[8].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoDeborah = ListaPedidosProcessamento[9].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoLeonardo = ListaPedidosProcessamento[10].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoDaniel = ListaPedidosProcessamento[1].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoGustavo = ListaPedidosProcessamento[2].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoJoao = ListaPedidosProcessamento[3].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoPaulo = ListaPedidosProcessamento[6].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoVivian = ListaPedidosProcessamento[7].ToString();
            ViewcstPainelComercialGerencial.QtdPedidosProcessamentoTOTAL = QtdPedidosProcessamentoTOTAL.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosDiarioTOTAL = QtdPedidosDiarioTOTAL.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosMesTOTAL = QtdPedidosMesTOTAL.ToString();


            ////////// Pedidos por Período ////////// 
            ViewcstPainelComercialGerencial.QtdPedidosGDIHoje = QtdPedidosGDIHoje.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosSCHoje = QtdPedidosSCHoje.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosGeralHoje = (QtdPedidosGDIHoje + QtdPedidosSCHoje).ToString();
            ViewcstPainelComercialGerencial.ValorPedidosGDIHoje = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorPedidosGDIHoje).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
            ViewcstPainelComercialGerencial.ValorPedidosSCHoje = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorPedidosSCHoje).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
            ViewcstPainelComercialGerencial.ValorPedidosGeralHoje = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorPedidosGDIHoje + ValorPedidosSCHoje)).Replace("R$ ", "").Replace("R$", "").Replace("$", "");

            ViewcstPainelComercialGerencial.QtdPedidosGDIMes = QtdPedidosGDIMes.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosSCMes = QtdPedidosSCMes.ToString();
            ViewcstPainelComercialGerencial.QtdPedidosGeralMes = (QtdPedidosGDIMes + QtdPedidosSCMes).ToString();
            ViewcstPainelComercialGerencial.ValorPedidosGDIMes = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorPedidosGDIMes).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
            ViewcstPainelComercialGerencial.ValorPedidosSCMes = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorPedidosSCMes).Replace("R$ ", "").Replace("R$", "").Replace("$", "");
            ViewcstPainelComercialGerencial.ValorPedidosGeralMes = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", (ValorPedidosGDIMes + ValorPedidosSCMes)).Replace("R$ ", "").Replace("R$", "").Replace("$", "");

            ViewcstPainelComercialGerencial.TextPedidosGDIHoje = "Hoje |  " + ViewcstPainelComercialGerencial.QtdPedidosGDIHoje + " Pedidos | R$ " + ViewcstPainelComercialGerencial.ValorPedidosGDIHoje;
            ViewcstPainelComercialGerencial.TextPedidosGDIMes = "Mês | " + ViewcstPainelComercialGerencial.QtdPedidosGDIMes + " Pedidos | R$ " + ViewcstPainelComercialGerencial.ValorPedidosGDIMes;
            ViewcstPainelComercialGerencial.TextPedidosSCHoje = "Hoje | " + ViewcstPainelComercialGerencial.QtdPedidosSCHoje + " Pedidos | R$ " + ViewcstPainelComercialGerencial.ValorPedidosSCHoje;
            ViewcstPainelComercialGerencial.TextPedidosSCMes = "Mês | " + ViewcstPainelComercialGerencial.QtdPedidosSCMes + " Pedidos | R$ " + ViewcstPainelComercialGerencial.ValorPedidosSCMes;
            ViewcstPainelComercialGerencial.TextPedidosGeralHoje = "Hoje | " + ViewcstPainelComercialGerencial.QtdPedidosGeralHoje + " Pedidos | R$ " + ViewcstPainelComercialGerencial.ValorPedidosGeralHoje;
            ViewcstPainelComercialGerencial.TextPedidosGeralMes = "Mês | " + ViewcstPainelComercialGerencial.QtdPedidosGeralMes + " Pedidos | R$ " + ViewcstPainelComercialGerencial.ValorPedidosGeralMes;

            ViewBag.Title = LibIcons.getIcon("fa-solid fa-magnifying-glass-chart", "", "#008000", "fa-lg") + LibStringFormat.GetTabHtml(1) + "Gestão Comercial à Vista";
            return View(ViewcstPainelComercialGerencial);
        }
    }
}