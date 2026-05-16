using GdiPlataform.Areas.crm.Models;
using GdiPlataform.Areas.g.Lib;
using GdiPlataform.Areas.g.Models;
using GdiPlataform.Db;
using GdiPlataform.Lib;
using GdiPlataform.Security;
using Rotativa;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Zen.Barcode;

namespace GdiPlataform.Areas.crm.Controllers
{
    [CustomAuthorize(Roles = "gc_PortalCliente_PortalFinanceiro")]
    public class PedidosController : Controller
    {
        private GdiPlataformEntities db;
        private List<string> listaPdfsGerados = new List<string>();
        private List<string> listaVendedoresPdfs = new List<string>();

        public PedidosController()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        [OutputCache(Duration = 0, NoStore = true, VaryByParam = "*")]
        public ActionResult Index()
        {
            var dataLimite = DateTime.Now.AddYears(-1);
            List<Db.gc_movimentos> ListaPedidosCliente = db.gc_movimentos.SqlQuery(
                "SELECT mov.* FROM gc_movimentos mov" +
                " WHERE id_cliente = @idCliente" +
                " AND mov.id_movimento_tipo IN (3, 4, 8)" +
                " AND mov.id_movimento_status = 2 AND mov.id_movimento_posicao >= 4" +
                " AND mov.datahora_aprovacao > @dataLimite" +
                " ORDER BY mov.datahora_aprovacao DESC",
                new System.Data.SqlClient.SqlParameter("@idCliente", CachePersister.userIdentity.IdCliente),
                new System.Data.SqlClient.SqlParameter("@dataLimite", dataLimite)
            ).ToList();

            cstListaPedidosPortal ListaPedidosPortal = new cstListaPedidosPortal();
            foreach (gc_movimentos RecordMovimento in ListaPedidosCliente)
            {
                // Notas Fiscais
                cstDadosPedidoPortal DadosPedidoPortal = new cstDadosPedidoPortal();


                int IndexNfe = 0;
                List<Db.gc_movimentos_nf> ListaNFeAtivas = db.gc_movimentos_nf.SqlQuery(
                    "SELECT nf.* FROM gc_movimentos_nf nf" +
                    " WHERE nf.id_movimento = @idMovimento" +
                    " AND nf.id_nfe_status IN (SELECT DISTINCT id_nfe_status FROM g_nfe_status WHERE nf_ativa = 1)",
                    new System.Data.SqlClient.SqlParameter("@idMovimento", RecordMovimento.id_movimento)
                ).ToList();
                foreach (gc_movimentos_nf RecordNF in ListaNFeAtivas)
                {
                    IndexNfe += 1;
                    if (IndexNfe == 1)
                    {
                        DadosPedidoPortal.NFe1DanfeDescricao = "NFe " + RecordNF.nf_numero.EmptyIfNull().ToString() + " - " + RecordNF.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy");
                        DadosPedidoPortal.NFe1DanfeURL = RecordNF.nf_url_pdf.EmptyIfNull().ToString();
                        DadosPedidoPortal.NFe1XmlDescricao = "XML " + RecordNF.nf_numero.EmptyIfNull().ToString() + " - " + RecordNF.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy");
                        DadosPedidoPortal.NFe1XmlURL = RecordNF.nf_url_xml.EmptyIfNull().ToString();
                    }
                    else if (IndexNfe == 2)
                    {
                        DadosPedidoPortal.NFe2DanfeDescricao = "NFe " + RecordNF.nf_numero.EmptyIfNull().ToString() + " - " + RecordNF.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy");
                        DadosPedidoPortal.NFe2DanfeURL = RecordNF.nf_url_pdf.EmptyIfNull().ToString();
                        DadosPedidoPortal.NFe2XmlDescricao = "XML " + RecordNF.nf_numero.EmptyIfNull().ToString() + " - " + RecordNF.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy");
                        DadosPedidoPortal.NFe2XmlURL = RecordNF.nf_url_xml.EmptyIfNull().ToString();
                    }
                    else if (IndexNfe == 3)
                    {
                        DadosPedidoPortal.NFe3DanfeDescricao = "NFe " + RecordNF.nf_numero.EmptyIfNull().ToString() + " - " + RecordNF.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy");
                        DadosPedidoPortal.NFe3DanfeURL = RecordNF.nf_url_pdf.EmptyIfNull().ToString();
                        DadosPedidoPortal.NFe3XmlDescricao = "XML " + RecordNF.nf_numero.EmptyIfNull().ToString() + " - " + RecordNF.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy");
                        DadosPedidoPortal.NFe3XmlURL = RecordNF.nf_url_xml.EmptyIfNull().ToString();
                    }
                    else if (IndexNfe == 4)
                    {
                        DadosPedidoPortal.NFe4DanfeDescricao = "NFe " + RecordNF.nf_numero.EmptyIfNull().ToString() + " - " + RecordNF.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy");
                        DadosPedidoPortal.NFe4DanfeURL = RecordNF.nf_url_pdf.EmptyIfNull().ToString();
                        DadosPedidoPortal.NFe4XmlDescricao = "XML " + RecordNF.nf_numero.EmptyIfNull().ToString() + " - " + RecordNF.nf_data_geracao.GetValueOrDefault().ToString("dd/MM/yy");
                        DadosPedidoPortal.NFe4XmlURL = RecordNF.nf_url_xml.EmptyIfNull().ToString();
                    }
                    DadosPedidoPortal.PedidoQtdNotas += 1;
                }


                // Boletos
                int IndexFinanceiro = 0;
                List<gc_financeiro_lancamentos> ListaBoletosCliente = db.gc_financeiro_lancamentos.Where(p => p.ativo == true && p.tipo_pag_rec == 2 && p.id_financeiro_status == 3 && p.id_movimento == RecordMovimento.id_movimento).ToList();
                var allRecordsPagRecTipos = db.g_pagrec_tipos.Select(t => new { t.id_pagrec_tipo, t.descricao }).ToList();
                List<g_vendedores> ListaVendedores = db.g_vendedores.Where(v => v.ativo == true).ToList();
                foreach (gc_financeiro_lancamentos Lancamento in ListaBoletosCliente)
                {
                    String Descricao = string.Empty;
                    String Id = string.Empty;
                    IndexFinanceiro += 1;

                    if (Lancamento.id_pag_rec_tipo == 1) { Descricao = "Cartão Crédito - Venc " + Lancamento.data_vencimento.ToString("dd/MM/yy") + " - " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Lancamento.valor_total); Id = "-1"; }
                    else if (Lancamento.id_pag_rec_tipo == 2) { Descricao = "Ted/Doc/Pix - Venc " + Lancamento.data_vencimento.ToString("dd/MM/yy") + " - " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Lancamento.valor_total); Id = "-2"; }
                    else if (Lancamento.id_pag_rec_tipo == 3) { Descricao = "Boleto - Venc " + Lancamento.data_vencimento.ToString("dd/MM/yy") + " - " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", Lancamento.valor_total); Id = Lancamento.id_lancamento.ToString(); }
                    if (IndexFinanceiro == 1)
                    {
                        DadosPedidoPortal.Financeiro1Descricao = Descricao;
                        DadosPedidoPortal.Financeiro1Id = Id;
                    }
                    else if (IndexFinanceiro == 2)
                    {
                        DadosPedidoPortal.Financeiro2Descricao = Descricao;
                        DadosPedidoPortal.Financeiro2Id = Id;
                    }
                    else if (IndexFinanceiro == 3)
                    {
                        DadosPedidoPortal.Financeiro3Descricao = Descricao;
                        DadosPedidoPortal.Financeiro3Id = Id;
                    }
                    else if (IndexFinanceiro == 4)
                    {
                        DadosPedidoPortal.Financeiro4Descricao = Descricao;
                        DadosPedidoPortal.Financeiro4Id = Id;
                    }
                    else if (IndexFinanceiro == 5)
                    {
                        DadosPedidoPortal.Financeiro5Descricao = Descricao;
                        DadosPedidoPortal.Financeiro5Id = Id;
                    }
                    else if (IndexFinanceiro == 6)
                    {
                        DadosPedidoPortal.Financeiro6Descricao = Descricao;
                        DadosPedidoPortal.Financeiro6Id = Id;
                    }
                    else if (IndexFinanceiro == 7)
                    {
                        DadosPedidoPortal.Financeiro7Descricao = Descricao;
                        DadosPedidoPortal.Financeiro7Id = Id;
                    }
                    else if (IndexFinanceiro == 8)
                    {
                        DadosPedidoPortal.Financeiro8Descricao = Descricao;
                        DadosPedidoPortal.Financeiro8Id = Id;
                    }
                    else if (IndexFinanceiro == 9)
                    {
                        DadosPedidoPortal.Financeiro9Descricao = Descricao;
                        DadosPedidoPortal.Financeiro9Id = Id;
                    }
                    DadosPedidoPortal.PedidoQtdFinanceiro += 1;
                }

                // GED
                int IndexArquivoGed = 0;
                List<Db.ged_arquivos> ListArquivosGED = db.ged_arquivos.Where(g => g.ativo == true && g.id_gc_movimento == RecordMovimento.id_movimento).ToList();
                foreach (ged_arquivos GedArquivo in ListArquivosGED)
                {
                    IndexArquivoGed += 1;
                    String DescricaoGed = string.Empty;
                    String IdGed = string.Empty;
                    DescricaoGed = GedArquivo.descricao.EmptyIfNull().ToString();
                    IdGed = GedArquivo.id_arquivo.EmptyIfNull().ToString();

                    if (IndexArquivoGed == 1)
                    {
                        DadosPedidoPortal.Ged1Id = IdGed;
                        DadosPedidoPortal.Ged1Descricao = DescricaoGed;
                    }
                    else if (IndexArquivoGed == 2)
                    {
                        DadosPedidoPortal.Ged2Id = IdGed;
                        DadosPedidoPortal.Ged2Descricao = DescricaoGed;
                    }
                    else if (IndexArquivoGed == 3)
                    {
                        DadosPedidoPortal.Ged3Id = IdGed;
                        DadosPedidoPortal.Ged3Descricao = DescricaoGed;
                    }
                    else if (IndexArquivoGed == 4)
                    {
                        DadosPedidoPortal.Ged4Id = IdGed;
                        DadosPedidoPortal.Ged4Descricao = DescricaoGed;
                    }
                    else if (IndexArquivoGed == 5)
                    {
                        DadosPedidoPortal.Ged5Id = IdGed;
                        DadosPedidoPortal.Ged5Descricao = DescricaoGed;
                    }
                    else if (IndexArquivoGed == 6)
                    {
                        DadosPedidoPortal.Ged6Id = IdGed;
                        DadosPedidoPortal.Ged6Descricao = DescricaoGed;
                    }
                    else if (IndexArquivoGed == 7)
                    {
                        DadosPedidoPortal.Ged7Id = IdGed;
                        DadosPedidoPortal.Ged7Descricao = DescricaoGed;
                    }
                    else if (IndexArquivoGed == 8)
                    {
                        DadosPedidoPortal.Ged8Id = IdGed;
                        DadosPedidoPortal.Ged8Descricao = DescricaoGed;
                    }
                    else if (IndexArquivoGed == 9)
                    {
                        DadosPedidoPortal.Ged9Id = IdGed;
                        DadosPedidoPortal.Ged9Descricao = DescricaoGed;
                    }
                }
                DadosPedidoPortal.PedidoNumero = RecordMovimento.id_movimento.EmptyIfNull().ToString();
                DadosPedidoPortal.PedidoData = RecordMovimento.datahora_aprovacao.GetValueOrDefault().ToString("dd/MM/yy");
                DadosPedidoPortal.PedidoValor = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordMovimento.valor_total_bruto);
                DadosPedidoPortal.PedidoQtdItens = RecordMovimento.qtd_itens.EmptyIfNull().ToString();
                g_vendedores RecordVendedor = ListaVendedores.Where(v => v.id_vendedor == RecordMovimento.id_vendedor).FirstOrDefault();
                if (RecordVendedor != null) { DadosPedidoPortal.ConsultorNome = RecordVendedor.nome.EmptyIfNull().ToString(); }
                ;
                ListaPedidosPortal.ListaPedidos.Add(DadosPedidoPortal);
            }
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetMaxAge(TimeSpan.Zero);
            Response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
            ViewBag.Title = "Portal do Cliente — Pedidos";
            return View(ListaPedidosPortal);
        }

        #region AjaxFinanceiroBoletoGCPDF
        [HttpPost]
        [OutputCache(Duration = 0, NoStore = true, VaryByParam = "*")]
        public ActionResult AjaxFinanceiroBoletoGCPDF(gc_financeiro_lancamentos view_record_gc_financeiro_lancamentos)
        {
            bool Sucesso = false;
            String MsgRetorno = "AjaxFinanceiroBoletoGCPDF";
            String idProcessamentoGravado = String.Empty;
            String sentencaSQLFinanceiro = String.Empty;
            String dirExportacaoPDF = String.Empty;
            g_contas_caixas RecordContaCaixaBoleto = new Db.g_contas_caixas();

            try
            {
                gc_financeiro_lancamentos record_gc_financeiro_lancamentos = db.gc_financeiro_lancamentos.Find(view_record_gc_financeiro_lancamentos.id_lancamento);

                if (record_gc_financeiro_lancamentos == null)
                {
                    Sucesso = false;
                    MsgRetorno = "Lançamento financeiro [" + view_record_gc_financeiro_lancamentos.id_lancamento + "] não encontrado!";
                }
                else if (record_gc_financeiro_lancamentos.cnab_linha_digitavel.EmptyIfNull().ToString().Length == 0)
                {
                    Sucesso = false;
                    MsgRetorno = "Boleto [" + view_record_gc_financeiro_lancamentos.id_lancamento + "] não encontrado!";
                }
                else
                {
                    // Salvar o arquivo em disco
                    DateTime dataAtual = LibDateTime.getDataHoraBrasilia();
                    dirExportacaoPDF = Server.MapPath("~/_filestemp");
                    if (!Directory.Exists(dirExportacaoPDF)) { Directory.CreateDirectory(dirExportacaoPDF); }
                    dirExportacaoPDF = Path.Combine(dirExportacaoPDF, "Downloads");
                    if (!Directory.Exists(dirExportacaoPDF)) { Directory.CreateDirectory(dirExportacaoPDF); }
                    dirExportacaoPDF = Path.Combine(dirExportacaoPDF, dataAtual.ToString("yyyy"));
                    if (!Directory.Exists(dirExportacaoPDF)) { Directory.CreateDirectory(dirExportacaoPDF); }
                    dirExportacaoPDF = Path.Combine(dirExportacaoPDF, dataAtual.ToString("MM"));
                    if (!Directory.Exists(dirExportacaoPDF)) { Directory.CreateDirectory(dirExportacaoPDF); }
                    dirExportacaoPDF = Path.Combine(dirExportacaoPDF, "g-boletos");
                    if (!Directory.Exists(dirExportacaoPDF)) { Directory.CreateDirectory(dirExportacaoPDF); }

                    // Contas Caixas
                    String sentencaSQLContasCaixas = String.Empty;
                    DataTable tableContasCaixas = null;
                    List<DataRow> allContasCaixas = null;
                    sentencaSQLContasCaixas = " select CC.*, CI.nome " +
                                              " from g_contas_caixas CC " +
                                              " join g_cidades CI on (CI.id_cidade = CC.id_cidade_com)";
                    tableContasCaixas = LibDB.GetDataTable(sentencaSQLContasCaixas, db);
                    allContasCaixas = tableContasCaixas.AsEnumerable().ToList();

                    if (record_gc_financeiro_lancamentos.boleto_banco == "237") { RecordContaCaixaBoleto = db.g_contas_caixas.Find(1); }
                    else if (record_gc_financeiro_lancamentos.boleto_banco == "341") { RecordContaCaixaBoleto = db.g_contas_caixas.Find(7); }

                    // Financeiro
                    DataTable TableFinanceiroLancamentos = null;
                    List<DataRow> AllFinanceiroLancamentos = null;
                    sentencaSQLFinanceiro = "select FL.*, " +
                                            "CL.id_cliente as 'cliente.id_cliente', CL.nome as 'cliente.nome', CL.cpf as 'cliente.cpf', CL.cnpj as 'cliente.cnpj', " +
                                            "CL.endereco_com as 'cliente.endereco_com', CL.bairro_com as 'cliente.bairro_com', CI.nome as 'cliente.cidade_com', " +
                                            "CL.cep_com as 'cliente.cep_com', UF.sigla as 'cliente.uf_com' " +
                                            "from gc_financeiro_lancamentos FL " +
                                            "join g_clientes CL on (CL.id_cliente = FL.id_cliente) " +
                                            "join g_cidades CI on (CI.id_cidade = CL.id_cidade_com) " +
                                            "join g_uf UF on (UF.id_uf = CL.id_uf_com) ";
                    if (view_record_gc_financeiro_lancamentos.id_lancamento > 0)
                    {
                        sentencaSQLFinanceiro += "where FL.id_lancamento = " + view_record_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToString();
                    }
                    TableFinanceiroLancamentos = LibDB.GetDataTable(sentencaSQLFinanceiro, db);
                    AllFinanceiroLancamentos = TableFinanceiroLancamentos.AsEnumerable().ToList();

                    if (AllFinanceiroLancamentos.Count > 0)
                    {
                        foreach (var dsRowFinanceiro in AllFinanceiroLancamentos)
                        {
                            gc_financeiro_lancamentos RecordFinanceiroLancamento = db.gc_financeiro_lancamentos.Find(Convert.ToInt32(view_record_gc_financeiro_lancamentos.id_lancamento.EmptyIfNull().ToString()));
                            cstFinanceiroBoletos record_cstFinanceiroBoletos = new cstFinanceiroBoletos();
                            record_cstFinanceiroBoletos.idFinanceiro = view_record_gc_financeiro_lancamentos.id_lancamento;
                            int idContaCaixa = 0;
                            int.TryParse(dsRowFinanceiro["id_conta_caixa"].EmptyIfNull().ToString(), out idContaCaixa);
                            var dsRowContaCaixa = tableContasCaixas.Select("id_conta_caixa = " + idContaCaixa).FirstOrDefault();

                            // Cabeçalho
                            record_cstFinanceiroBoletos.EDadosCabecalho1 = "FATURA - PEDIDO Nº " + dsRowFinanceiro["id_movimento"].EmptyIfNull().ToString().ToUpper();
                            //record_cstFinanceiroBoletos.ECedenteNome = dsRowContaCaixa["razao_social"].EmptyIfNull().ToString().ToUpper();
                            //record_cstFinanceiroBoletos.ECedenteComplemento1 = dsRowContaCaixa["endereco_com"].EmptyIfNull().ToString().ToUpper() + " " + dsRowContaCaixa["bairro_com"].EmptyIfNull().ToString().ToUpper() + " CEP: " + dsRowContaCaixa["cep_com"].EmptyIfNull().ToString().ToUpper();
                            record_cstFinanceiroBoletos.ECedenteNome = RecordContaCaixaBoleto.razao_social.EmptyIfNull().ToString().ToUpper();
                            record_cstFinanceiroBoletos.ECedenteComplemento1 = RecordContaCaixaBoleto.endereco_com.EmptyIfNull().ToString().ToUpper() + " " + RecordContaCaixaBoleto.bairro_com.EmptyIfNull().ToString().ToUpper() + " CEP: " + RecordContaCaixaBoleto.cep_com.EmptyIfNull().ToString().ToUpper();
                            record_cstFinanceiroBoletos.ECedenteComplemento2 = "BELO HORIZONTE - MG";

                            // Cliente
                            record_cstFinanceiroBoletos.EClienteNome = dsRowFinanceiro["cliente.nome"].EmptyIfNull().ToString().ToUpper();
                            if (dsRowFinanceiro["cliente.cpf"].EmptyIfNull().ToString().Trim() != String.Empty) { record_cstFinanceiroBoletos.EClienteDocumento = LibStringFormat.FormatarCPFCNPJ("F", dsRowFinanceiro["cliente.cpf"].EmptyIfNull().ToString().Trim()); }
                            else { record_cstFinanceiroBoletos.EClienteDocumento = LibStringFormat.FormatarCPFCNPJ("J", dsRowFinanceiro["cliente.cnpj"].EmptyIfNull().ToString().Trim()); }
                            record_cstFinanceiroBoletos.EClienteCodigo = dsRowFinanceiro["cliente.id_cliente"].EmptyIfNull().ToString();
                            String _clienteEndereco = string.Empty;
                            String _clienteEnderecoCidadeUF = string.Empty;
                            _clienteEndereco += dsRowFinanceiro["cliente.endereco_com"].EmptyIfNull().ToString().ToUpper() + " ";
                            _clienteEndereco += dsRowFinanceiro["cliente.bairro_com"].EmptyIfNull().ToString().ToUpper() + " ";
                            _clienteEnderecoCidadeUF += dsRowFinanceiro["cliente.cidade_com"].EmptyIfNull().ToString().ToUpper() + " ";
                            _clienteEnderecoCidadeUF += dsRowFinanceiro["cliente.uf_com"].EmptyIfNull().ToString().ToUpper() + " ";
                            _clienteEnderecoCidadeUF += "CEP " + LibStringFormat.FormatarCEP(dsRowFinanceiro["cliente.cep_com"].EmptyIfNull().ToString().ToUpper());

                            record_cstFinanceiroBoletos.EClienteEndereco = _clienteEndereco;
                            record_cstFinanceiroBoletos.EClienteEnderecoCidadeUF = _clienteEnderecoCidadeUF;

                            DateTime DataVencimento = RecordFinanceiroLancamento.data_vencimento;
                            DateTime DataProcessamento = DataVencimento;
                            decimal ValorTotal = RecordFinanceiroLancamento.valor_total;
                            

                            record_cstFinanceiroBoletos.EClienteMensagem = RecordContaCaixaBoleto.mensagem_cliente.EmptyIfNull().ToString();
                            //record_cstFinanceiroBoletos.EValorLiquido = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowFinanceiro["valor_total"].EmptyIfNull().ToString()));
                            //record_cstFinanceiroBoletos.EValorBruto = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", decimal.Parse(dsRowFinanceiro["valor_total"].EmptyIfNull().ToString()));
                            record_cstFinanceiroBoletos.EValorLiquido = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordFinanceiroLancamento.valor_total);
                            record_cstFinanceiroBoletos.EValorBruto = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", RecordFinanceiroLancamento.valor_total);


                            // MensagemCaixa
                            decimal ValorMulta = ((ValorTotal / 100) * 2);
                            decimal ValorJuros = ((ValorTotal / 100) * 1);
                            String MsgCaixa = String.Empty;
                            MsgCaixa += "Sr(a). Cliente pague esse título até o vencimento em toda a rede bancária, <br/> casas lotéricas, caixas eletrônicos, internet banking ou aplicativo do seu banco" + "<br><br>";
                            MsgCaixa += "O Não pagamento até o vencimento <b>" + DataVencimento.ToString("dd/MM/yy") + "</b> acarretará a cobrança de multa no valor de " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorMulta) + ",<br>"; ;
                            MsgCaixa += "Acrescido de juros diários de " + string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorJuros) + "." + "<br>";

                            // Boleto


                            if (RecordContaCaixaBoleto.banco.EmptyIfNull().ToString() == "341") { record_cstFinanceiroBoletos.ECodBanco = "341-7"; }
                            else if (RecordContaCaixaBoleto.banco.EmptyIfNull().ToString() == "237") { record_cstFinanceiroBoletos.ECodBanco = "237-0"; }
                            else { record_cstFinanceiroBoletos.ECodBanco = RecordContaCaixaBoleto.banco.EmptyIfNull().ToString(); }
                            record_cstFinanceiroBoletos.ELogoBanco = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/banco" + record_gc_financeiro_lancamentos.boleto_banco.EmptyIfNull().ToString() + ".png";
                            record_cstFinanceiroBoletos.ELogoBoletoBanco = "https://bucket-gdi-public-files.s3.sa-east-1.amazonaws.com/FilesImages/LogoBoleto" + record_gc_financeiro_lancamentos.boleto_banco.EmptyIfNull().ToString() + ".png";
                            record_cstFinanceiroBoletos.EDataVencimento = DataVencimento.ToString("dd/MM/yy");
                            record_cstFinanceiroBoletos.ECedenteNome = RecordContaCaixaBoleto.razao_social.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.ECedenteComplemento1 = RecordContaCaixaBoleto.endereco_com.EmptyIfNull().ToString().ToUpper() + " " + RecordContaCaixaBoleto.bairro_com.EmptyIfNull().ToString().ToUpper();
                            record_cstFinanceiroBoletos.ECedenteComplemento2 = " CEP: " + RecordContaCaixaBoleto.cep_com.EmptyIfNull().ToString().ToUpper() + " CNPJ: " + LibStringFormat.FormatarCPFCNPJ("J", RecordContaCaixaBoleto.cnpj.EmptyIfNull().ToString().ToUpper());
                            record_cstFinanceiroBoletos.EAgenciaCodCedente = LibFinanceiroBoletos.CalcularAgenciaCodigoCedente(RecordContaCaixaBoleto.banco.EmptyIfNull().ToString(), RecordContaCaixaBoleto.agencia.EmptyIfNull().ToString(), RecordContaCaixaBoleto.dv_agencia.EmptyIfNull().ToString(), RecordContaCaixaBoleto.conta.EmptyIfNull().ToString(), RecordContaCaixaBoleto.dv_conta.EmptyIfNull().ToString(), RecordContaCaixaBoleto.codigo_convenio.EmptyIfNull().ToString());
                            record_cstFinanceiroBoletos.EDataDocumento = DataProcessamento.ToString("dd/MM/yy");
                            record_cstFinanceiroBoletos.ENossoNumeroDV = dsRowFinanceiro["cnab_nosso_numero"].EmptyIfNull().ToString();
                            if (dsRowFinanceiro["numero_documento"].EmptyIfNull().ToString().Length > 0) { record_cstFinanceiroBoletos.ENumeroDocumento = "NF " + dsRowFinanceiro["numero_documento"].EmptyIfNull().ToString(); } else { record_cstFinanceiroBoletos.ENumeroDocumento = dsRowFinanceiro["descricao"].EmptyIfNull().ToString(); }
                            record_cstFinanceiroBoletos.EEspecieDoc = "NS";
                            record_cstFinanceiroBoletos.EAceite = "N";
                            record_cstFinanceiroBoletos.EDataProcessamento = DataProcessamento.ToString("dd/MM/yy");
                            record_cstFinanceiroBoletos.ENossoNumeroDV = dsRowFinanceiro["cnab_nosso_numero"].EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.ECarteira = RecordContaCaixaBoleto.carteira.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.EEspecieMoeda = RecordContaCaixaBoleto.especie_moeda.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.EValorTotal = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", ValorTotal);
                            record_cstFinanceiroBoletos.EMensagemCaixa = MsgCaixa;
                            record_cstFinanceiroBoletos.ENomeSacado = dsRowFinanceiro["cliente.nome"].EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.EEnderecoSacado = dsRowFinanceiro["cliente.endereco_com"].EmptyIfNull().ToString() + " " + dsRowFinanceiro["cliente.bairro_com"].EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.ECidadeSacado = dsRowFinanceiro["cliente.cidade_com"].EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.ECepSacado = LibStringFormat.FormatarCEP(dsRowFinanceiro["cliente.cep_com"].EmptyIfNull().ToString());
                            record_cstFinanceiroBoletos.EUFSacado = dsRowFinanceiro["cliente.uf_com"].EmptyIfNull().ToString();
                            if (dsRowFinanceiro["cliente.cpf"].EmptyIfNull().ToString().Trim() != String.Empty)
                            { record_cstFinanceiroBoletos.EDocSacado = LibStringFormat.FormatarCPFCNPJ("F", dsRowFinanceiro["cliente.cpf"].EmptyIfNull().ToString().Trim()); }
                            else { record_cstFinanceiroBoletos.EDocSacado = LibStringFormat.FormatarCPFCNPJ("J", dsRowFinanceiro["cliente.cnpj"].EmptyIfNull().ToString().Trim()); }
                            record_cstFinanceiroBoletos.ELinhaDigitavel = LibStringFormat.FormatarLinhaDigitavel(dsRowFinanceiro["cnab_linha_digitavel"].EmptyIfNull().ToString());
                            record_cstFinanceiroBoletos.ECodigoBarras = LibCnabBancario.GetCodigoBarras(record_cstFinanceiroBoletos.ELinhaDigitavel);
                            record_cstFinanceiroBoletos.EImgBarCode = Generate_barcode(record_cstFinanceiroBoletos.ECodigoBarras, DateTime.Parse(record_cstFinanceiroBoletos.EDataVencimento));
                            if (dsRowFinanceiro["pix_base64"].EmptyIfNull().ToString().Trim().Length > 0)
                            {
                                record_cstFinanceiroBoletos.HasPix = true;
                                record_cstFinanceiroBoletos.EPixEMV = dsRowFinanceiro["pix_emv"].EmptyIfNull().ToString().Trim().Replace(" ", "&nbsp;");
                                record_cstFinanceiroBoletos.EImgPixQrCode = Generate_PixQrCode(record_cstFinanceiroBoletos.idFinanceiro, dsRowFinanceiro["pix_base64"].EmptyIfNull().ToString().Trim(), DateTime.Parse(record_cstFinanceiroBoletos.EDataVencimento));
                            }
                            ;
                            ViewBag.Title = "Boleto - " + record_cstFinanceiroBoletos.EClienteNome.EmptyIfNull().ToString();
                            record_cstFinanceiroBoletos.printPDF = true;

                            //String TemplateBoleto = "BoletoPDF";
                            String TemplateBoleto = "BoletoPdfFebraban";

                            var pdf = new ViewAsPdf
                            {
                                //ViewName = "BoletoPDF",
                                ViewName = TemplateBoleto,
                                Model = record_cstFinanceiroBoletos
                            };
                            // Criar o PDF
                            byte[] applicationPDFData = pdf.BuildFile(ControllerContext);
                            string fileNamePDF1 = "Boleto - " + LibStringFormat.SomenteAlfabetoeNumeros(record_cstFinanceiroBoletos.EClienteNome.ToString()) + ".pdf";
                            fileNamePDF1 = Path.Combine(dirExportacaoPDF, fileNamePDF1);
                            var fileStream = new FileStream(fileNamePDF1, FileMode.Create, FileAccess.Write);
                            fileStream.Write(applicationPDFData, 0, applicationPDFData.Length);
                            fileStream.Close();
                            listaPdfsGerados.Add(fileNamePDF1);

                            // Atualizar o registro do processamento
                            g_processamento record_g_processamento = new g_processamento();
                            record_g_processamento.id_processamento_tipo = 37;
                            record_g_processamento.id_usuario = CachePersister.userIdentity.IdUsuario;
                            record_g_processamento.datahora_inicio = LibDateTime.getDataHoraBrasilia();
                            record_g_processamento.datahora_final = record_g_processamento.datahora_inicio;
                            record_g_processamento.qtd_registros = 1;
                            record_g_processamento.qtd_reg_ok = 1;
                            record_g_processamento.qtd_reg_erro = 0;
                            record_g_processamento.processando = false;
                            record_g_processamento.concluido = true;
                            record_g_processamento.pathfile = fileNamePDF1;
                            record_g_processamento.id_coligada = CachePersister.userIdentity.id_coligada;
                            record_g_processamento.id_filial = CachePersister.userIdentity.id_filial;
                            db.g_processamento.Add(record_g_processamento);
                            db.SaveChanges();
                            idProcessamentoGravado = record_g_processamento.id_processamento.ToString();
                            MsgRetorno = "Boleto Gerado com Sucesso!";
                            Sucesso = true;
                        }
                    }
                    else
                    {
                        Sucesso = false;
                        MsgRetorno = "Boleto [" + view_record_gc_financeiro_lancamentos.id_lancamento + "] não encontrado!";
                    }
                }
            }
            catch (DbEntityValidationException ex)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getDbEntityValidationException(ex);
            }
            catch (Exception e)
            {
                Sucesso = false;
                MsgRetorno = LibExceptions.getExceptionShortMessage(e);
            }
            return Json(new { success = Sucesso, msg = MsgRetorno, idProcessamento = idProcessamentoGravado }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        public String Generate_barcode(string txt, DateTime data_vencimento)
        {
            Code25BarcodeDraw bdw = BarcodeDrawFactory.Code25InterleavedWithoutChecksum;
            System.Drawing.Image img = bdw.Draw(txt, 50, 1);
            MemoryStream stream = new MemoryStream();
            img.Save(stream, ImageFormat.Png);
            String DirTempFiles = Server.MapPath("~/_filestemp");
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, "barcode");
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, data_vencimento.ToString("yyyy"));
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, data_vencimento.ToString("MM"));
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            String fileNameDestino = Path.Combine(DirTempFiles, txt + ".png");
            img.Save(fileNameDestino);

            string nameFileImage = String.Empty;
            nameFileImage += "/_filestemp";
            nameFileImage += "/" + "barcode";
            nameFileImage += "/" + data_vencimento.ToString("yyyy");
            nameFileImage += "/" + data_vencimento.ToString("MM");
            nameFileImage += "/" + txt + ".png";

            return nameFileImage;
        }
        public String Generate_PixQrCode(int IdFinanceiro, string base64String, DateTime data_vencimento)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            Image ImagemPix = null;
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                ImagemPix = Image.FromStream(ms, true);
            }

            String DirTempFiles = Server.MapPath("~/_filestemp");
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, "pix");
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, data_vencimento.ToString("yyyy"));
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            DirTempFiles = Path.Combine(DirTempFiles, data_vencimento.ToString("MM"));
            if (!Directory.Exists(DirTempFiles)) { Directory.CreateDirectory(DirTempFiles); }
            String fileNameDestino = Path.Combine(DirTempFiles, IdFinanceiro + ".png");
            ImagemPix.Save(fileNameDestino);

            string nameFileImage = String.Empty;
            nameFileImage += "/_filestemp";
            nameFileImage += "/" + "pix";
            nameFileImage += "/" + data_vencimento.ToString("yyyy");
            nameFileImage += "/" + data_vencimento.ToString("MM");
            nameFileImage += "/" + IdFinanceiro.ToString() + ".png";

            return nameFileImage;
        }

    }
}