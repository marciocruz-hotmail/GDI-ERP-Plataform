using GdiPlataform.Db;
using GdiPlataform.Robos.Nfe;
using GdiPlataform.Robos.Nfe.CartaCorrecao;
using GdiPlataform.Security;
using GdiPlataform.Lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web;

namespace GdiPlataform.Robos.ENotas
{
    public class RoboEnotasNFE
    {
        /// <summary>URL base da API eNotas / Nota Gateway (homologação e produção).</summary>
        public const string EnotasApiBaseUrl = "https://api.notagateway.com.br";

        private readonly GdiPlataformEntities db;
        Boolean SucessoRobo;
        String AmbienteEmissaoNFE = String.Empty;
        String RespostaRoboEnotas;

        #region Create Robô
        public RoboEnotasNFE()
        {
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty))
            {
                db = new GdiPlataformEntities(CachePersister.dataBase);
            }
        }
        #endregion

        #region Falha transmissão eNotas (NF-e / NFS-e)

        /// <summary>Atualiza <c>gc_movimentos_nf.id_nfe_status</c> para 14 e grava <c>g_nfe_logs</c> em falha de rede/IO ao enviar JSON à API eNotas (ex.: conexão recusada). Erros secundários ao persistir são ignorados para não mascarar a exceção original.</summary>
        private void PersistirFalhaTransmissaoJsonEnotasMovimentoNf(gc_movimentos_nf nf, string identificadorNfe, Exception ex, DateTime dataHora)
        {
            if (nf == null || nf.id_movimento_nf <= 0)
            {
                return;
            }
            try
            {
                string det = LibExceptions.getExceptionShortMessage(ex);
                WebException wex = ex as WebException;
                if (wex != null)
                {
                    string web = LibExceptions.getWebException(wex);
                    if (!string.IsNullOrWhiteSpace(web))
                    {
                        det = web;
                    }
                }
                det = det.Replace("\r", " ").Replace("\n", " ");
                string logText = "Erro transmissão eNotas: " + det;
                if (logText.Length > 250)
                {
                    logText = logText.Substring(0, 250);
                }

                int idCliente = 0;
                if (nf.id_movimento > 0)
                {
                    gc_movimentos mv = db.gc_movimentos.Find(nf.id_movimento);
                    if (mv != null)
                    {
                        idCliente = mv.id_cliente;
                    }
                }

                nf.id_nfe_status = 14;
                nf.datahora_alteracao = dataHora;
                nf.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                db.Entry(nf).State = EntityState.Modified;

                db.g_nfe_logs.Add(new g_nfe_logs
                {
                    ativo = true,
                    id_nfe = 0,
                    id_nfe_gateway = nf.id_nfe_gateway,
                    id_movimento = nf.id_movimento,
                    id_movimento_nf = nf.id_movimento_nf,
                    id_cliente = idCliente,
                    id_coligada = nf.id_coligada,
                    id_filial = nf.id_filial,
                    envio = true,
                    retorno = true,
                    identificador_nfe = identificadorNfe ?? string.Empty,
                    log = logText,
                    datahora_cadastro = dataHora,
                    id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                });
                db.SaveChanges();
            }
            catch
            {
            }
        }

        #endregion

        #region Gerar Nota Fiscal Produtos - Importação V2
        public bool GerarNFPImportacaoByMovimentoNF(gc_movimentos_nf record_gc_movimento_nf)
        {
            bool sucesso = false;
            DateTime dataHoraAtual = LibDateTime.getDataHoraBrasilia();
            string identificadorNFE = string.Empty;

            g_nfe_gateway recordNfeGateway = db.g_nfe_gateway.Find(record_gc_movimento_nf.id_filial);
            if (recordNfeGateway == null)
            {
                recordNfeGateway = db.g_nfe_gateway.Find(1);
            }

            string ambienteEmissaoNFE = recordNfeGateway.producao ? "Producao" : "Homologacao";
            string key1 = recordNfeGateway.key1.EmptyIfNull().ToString();
            string key2 = recordNfeGateway.key2.EmptyIfNull().ToString();

            try
            {
                var listaProdutos = db.g_produtos.SqlQuery(
                    "select p.* from g_produtos p join gc_movimentos_itens i on(p.id_produto = i.id_produto) where i.id_movimento = " +
                    record_gc_movimento_nf.id_movimento.EmptyIfNull().ToString()).ToList();

                var listaProdutosNCM = db.g_produtos_ncm.SqlQuery(
                    "select n.* from g_produtos_ncm n " +
                    "join g_produtos p on (n.id_produto_ncm = p.id_produto_ncm) " +
                    "join gc_movimentos_itens i on (p.id_produto = i.id_produto) " +
                    "where i.id_movimento = " + record_gc_movimento_nf.id_movimento.EmptyIfNull().ToString()).ToList();

                List<gc_movimentos_itens> listaMovimentosItens = db.gc_movimentos_itens
                    .Where(i => i.id_movimento == record_gc_movimento_nf.id_movimento)
                    .ToList();

                List<gc_movimentos_nf> listaMovimentosNF = db.gc_movimentos_nf
                    .Where(i => i.id_movimento == record_gc_movimento_nf.id_movimento)
                    .ToList();

                gc_movimentos recordMovimento = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento);

                identificadorNFE = record_gc_movimento_nf.id_movimento.EmptyIfNull().ToString().Trim() + "." + (listaMovimentosNF.Count + 1);
                if (ambienteEmissaoNFE == "Homologacao")
                {
                    identificadorNFE += ".h";
                }

                RatearFreteNosItensImportacao(record_gc_movimento_nf, listaMovimentosItens);

                IDictionary<string, string> nfeGateway = new Dictionary<string, string>();
                IDictionary<string, string> clienteNfe = new Dictionary<string, string>();
                IDictionary<string, string> clienteEnderecoNfe = new Dictionary<string, string>();
                IDictionary<string, string> transporteNfe = new Dictionary<string, string>();
                IDictionary<string, string> transporteFreteNfe = new Dictionary<string, string>();
                IDictionary<string, string> transporteVolumeNfe = new Dictionary<string, string>();
                IDictionary<string, string> nfeGatewayAutorizados = new Dictionary<string, string>();

                JArray listaItensNFe = new JArray();

                foreach (gc_movimentos_itens itemMovimento in listaMovimentosItens)
                {
                    g_produtos recordProduto = listaProdutos.Find(p => p.id_produto == itemMovimento.id_produto);
                    g_produtos_ncm recordProdutoNCM = listaProdutosNCM.Find(n => n.id_produto_ncm == recordProduto.id_produto_ncm);

                    IDictionary<string, string> itemNFe = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostos = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosIcms = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosPis = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosPisPorAliquota = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosCofins = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosCofinsPorAliquota = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosIpi = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosIpiPorAliquota = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosII = new Dictionary<string, string>();
                    IDictionary<String, String> ItemNFe_Impostos_Venda_ibsCbs = new Dictionary<String, String>();
                    IDictionary<string, string> itemImpostosPercentualAprox = new Dictionary<string, string>();
                    IDictionary<string, string> itemImpostosPercentualAproxSimplificado = new Dictionary<string, string>();
                    IDictionary<string, string> declaracaoImportacao = new Dictionary<string, string>();
                    IDictionary<string, string> declaracaoImportacaoAdicao = new Dictionary<string, string>();

                    string pisCst = itemMovimento.pis_cst.EmptyIfNull().ToString().Trim();
                    if (pisCst == "0" || pisCst == "00" || pisCst == "000")
                    {
                        pisCst = "08";
                    }

                    string cofinsCst = itemMovimento.cofins_cst.EmptyIfNull().ToString().Trim();
                    if (cofinsCst == "0" || cofinsCst == "00" || cofinsCst == "000")
                    {
                        cofinsCst = "08";
                    }

                    itemNFe.Add("cfop", "3102");
                    itemNFe.Add("codigo", LibStringFormat.SomenteAlfabetoSefaz(recordProduto.codigo.EmptyIfNull().ToString()));
                    itemNFe.Add("descricao", LibStringFormat.SomenteAlfabetoSefaz(recordProduto.descricao.EmptyIfNull().ToString()));
                    itemNFe.Add("ncm", recordProdutoNCM.codigo_ncm.EmptyIfNull().ToString());
                    itemNFe.Add("quantidade", "|" + LibNumbers.DecimalToJson(itemMovimento.quantidade) + "|");
                    itemNFe.Add("unidadeMedida", "UN");
                    itemNFe.Add("valorUnitario", "|" + LibNumbers.DecimalToJson(itemMovimento.valor_unit) + "|");
                    itemNFe.Add("frete", "|" + LibNumbers.DecimalToJson(itemMovimento.valor_frete) + "|");
                    itemNFe.Add("outrasDespesas", "|" + LibNumbers.DecimalToJson(itemMovimento.valor_despesas) + "|");

                    itemImpostosPercentualAprox.Add("fonte", "IBPT");
                    itemImpostosPercentualAproxSimplificado.Add(
                        "percentual",
                        "|" + LibNumbers.DecimalToJson(
                            recordProdutoNCM.tributo_federal_importado +
                            recordProdutoNCM.tributo_estadual +
                            recordProdutoNCM.tributo_municipal) + "|");

                    // ICMS
                    itemImpostosIcms.Add("situacaoTributaria", itemMovimento.icms_cst.EmptyIfNull().ToString());
                    itemImpostosIcms.Add("origem", "|" + LibNumbers.DecimalToJson(itemMovimento.icms_orig) + "|");
                    itemImpostosIcms.Add("aliquota", "|" + LibNumbers.DecimalToJson(itemMovimento.icms_picms) + "|");
                    itemImpostosIcms.Add("baseCalculo", "|" + LibNumbers.DecimalToJson(itemMovimento.icms_vbc + itemMovimento.valor_frete) + "|");
                    itemImpostosIcms.Add("modalidadeBaseCalculo", "|" + LibNumbers.DecimalToJson(itemMovimento.icms_modbc) + "|");
                    itemImpostosIcms.Add("percentualReducaoBaseCalculo", "|" + LibNumbers.DecimalToJson(itemMovimento.icms_predbc) + "|");
                    itemImpostosIcms.Add("valor", "|" + LibNumbers.DecimalToJson(itemMovimento.icms_vicms) + "|");

                    // PIS
                    itemImpostosPis.Add("situacaoTributaria", pisCst);
                    itemImpostosPis.Add("origem", "|0|");
                    itemImpostosPisPorAliquota.Add("aliquota", "|" + LibNumbers.DecimalToJson(itemMovimento.pis_ppis) + "|");

                    // COFINS
                    itemImpostosCofins.Add("situacaoTributaria", cofinsCst);
                    itemImpostosCofins.Add("origem", "|0|");
                    itemImpostosCofinsPorAliquota.Add("aliquota", "|" + LibNumbers.DecimalToJson(itemMovimento.cofins_pcofins) + "|");

                    // IPI
                    itemImpostosIpi.Add("situacaoTributaria", itemMovimento.ipi_cst.EmptyIfNull().ToString().Trim());
                    itemImpostosIpi.Add("origem", "|0|");
                    itemImpostosIpiPorAliquota.Add("aliquota", "|" + LibNumbers.DecimalToJson(itemMovimento.ipi_pipi) + "|");

                    // II
                    itemImpostosII.Add("despesasAduaneiras", "|" + LibNumbers.DecimalToJson(itemMovimento.ii_vdespadu) + "|");
                    itemImpostosII.Add("valor", "|" + LibNumbers.DecimalToJson(itemMovimento.ii_vii) + "|");
                    itemImpostosII.Add("iof", "|" + LibNumbers.DecimalToJson(itemMovimento.ii_viof) + "|");

                    // IBS CBS
                    ItemNFe_Impostos_Venda_ibsCbs.Add("classificacaoTributaria", "000001"); // Aqui

                    declaracaoImportacao.Add("numero", itemMovimento.di_numero.EmptyIfNull().ToString());
                    declaracaoImportacao.Add("data", itemMovimento.di_data.GetValueOrDefault().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    declaracaoImportacao.Add("localDesembaraco", LibStringFormat.SomenteAlfabetoSefaz(itemMovimento.di_loc_desemb.EmptyIfNull().ToString()));
                    declaracaoImportacao.Add("ufDesembaraco", LibStringFormat.SomenteAlfabetoSefaz(itemMovimento.di_uf_desemb.EmptyIfNull().ToString()));
                    declaracaoImportacao.Add("dataDesembaraco", itemMovimento.di_data_desemb.GetValueOrDefault().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    declaracaoImportacao.Add("tipoViaTransporte", itemMovimento.di_via_transp.EmptyIfNull().ToString());
                    declaracaoImportacao.Add("valorAFRMM", "|" + LibNumbers.DecimalToJson(itemMovimento.afrmm_valor) + "|");
                    declaracaoImportacao.Add("tipoIntermedio", "ImportacaoPorContaPropria");
                    declaracaoImportacao.Add("cnpj", itemMovimento.di_cnpj.EmptyIfNull().ToString());
                    declaracaoImportacao.Add("ufTerceiro", itemMovimento.di_uf_terceiro.EmptyIfNull().ToString());
                    declaracaoImportacao.Add("codigoExportador", itemMovimento.di_cod_exportador.EmptyIfNull().ToString());

                    declaracaoImportacaoAdicao.Add("numero", "|" + itemMovimento.di_adicao_numero.EmptyIfNull().ToString().Trim() + "|");
                    declaracaoImportacaoAdicao.Add("codigoFabricante", itemMovimento.di_adicao_fabricante.EmptyIfNull().ToString());
                    declaracaoImportacaoAdicao.Add("numeroDrawback", "000");

                    JObject objItem = JObject.Parse(JsonConvert.SerializeObject(itemNFe, Formatting.None));
                    JObject objImpostos = JObject.Parse(JsonConvert.SerializeObject(itemImpostos, Formatting.None));

                    JObject objPercentualAprox = JObject.Parse(JsonConvert.SerializeObject(itemImpostosPercentualAprox, Formatting.None));
                    objPercentualAprox.Add("simplificado", JObject.Parse(JsonConvert.SerializeObject(itemImpostosPercentualAproxSimplificado, Formatting.None)));

                    JObject objPis = JObject.Parse(JsonConvert.SerializeObject(itemImpostosPis, Formatting.None));
                    objPis.Add("porAliquota", JObject.Parse(JsonConvert.SerializeObject(itemImpostosPisPorAliquota, Formatting.None)));

                    JObject objCofins = JObject.Parse(JsonConvert.SerializeObject(itemImpostosCofins, Formatting.None));
                    objCofins.Add("porAliquota", JObject.Parse(JsonConvert.SerializeObject(itemImpostosCofinsPorAliquota, Formatting.None)));

                    JObject objIpi = JObject.Parse(JsonConvert.SerializeObject(itemImpostosIpi, Formatting.None));
                    objIpi.Add("porAliquota", JObject.Parse(JsonConvert.SerializeObject(itemImpostosIpiPorAliquota, Formatting.None)));

                    JObject objDeclaracaoImportacao = JObject.Parse(JsonConvert.SerializeObject(declaracaoImportacao, Formatting.None));
                    JArray listaAdicoes = new JArray { JObject.Parse(JsonConvert.SerializeObject(declaracaoImportacaoAdicao, Formatting.None)) };
                    objDeclaracaoImportacao.Add("adicoes", listaAdicoes);

                    objImpostos.Add("percentualAproximadoTributos", objPercentualAprox);
                    objImpostos.Add("icms", JObject.Parse(JsonConvert.SerializeObject(itemImpostosIcms, Formatting.None)));
                    objImpostos.Add("pis", objPis);
                    objImpostos.Add("cofins", objCofins);
                    objImpostos.Add("ipi", objIpi);
                    objImpostos.Add("ii", JObject.Parse(JsonConvert.SerializeObject(itemImpostosII, Formatting.None)));

                    objItem.Add("impostos", objImpostos);
                    objItem.Add("declaracaoImportacao", objDeclaracaoImportacao);

                    listaItensNFe.Add(objItem);
                }

                if (record_gc_movimento_nf.id_frete_responsavel == 1)
                {
                    transporteFreteNfe.Add("modalidade", "PorContaDoEmitente");
                }
                else if (record_gc_movimento_nf.id_frete_responsavel == 2)
                {
                    transporteFreteNfe.Add("modalidade", "PorContaDoDestinatario");
                }
                else if (record_gc_movimento_nf.id_frete_responsavel == 3)
                {
                    transporteFreteNfe.Add("modalidade", "SemFrete");
                }
                else
                {
                    transporteFreteNfe.Add("modalidade", "PorContaDeTerceiros");
                }

                transporteFreteNfe.Add("valor", "|" + LibNumbers.DecimalToJson(record_gc_movimento_nf.frete_valor) + "|");

                if (record_gc_movimento_nf.frete_qvol > 0)
                {
                    transporteVolumeNfe.Add("quantidade", "|" + LibNumbers.DecimalToJson(record_gc_movimento_nf.frete_qvol) + "|");
                    transporteVolumeNfe.Add("especie", LibStringFormat.SomenteAlfabetoSefaz(record_gc_movimento_nf.frete_esp));
                    transporteVolumeNfe.Add("marca", LibStringFormat.SomenteAlfabetoSefaz(record_gc_movimento_nf.frete_marca));
                    transporteVolumeNfe.Add("numeracao", LibStringFormat.SomenteAlfabetoSefaz(record_gc_movimento_nf.frete_nvol));

                    if (record_gc_movimento_nf.frete_pesol > 0)
                    {
                        transporteVolumeNfe.Add("pesoLiquido", "|" + LibNumbers.DecimalToJson(record_gc_movimento_nf.frete_pesol) + "|");
                    }

                    if (record_gc_movimento_nf.frete_pesob > 0)
                    {
                        transporteVolumeNfe.Add("pesoBruto", "|" + LibNumbers.DecimalToJson(record_gc_movimento_nf.frete_pesob) + "|");
                    }
                }

                clienteNfe.Add("tipoPessoa", "F");
                clienteNfe.Add("nome", "SOUTHERN CROSS AVIATION, LLC");
                clienteNfe.Add("email", "teste@mail.com");
                clienteNfe.Add("indicadorContribuinteICMS", "NaoContribuinte");
                clienteNfe.Add("telefone", "0000000000");

                clienteEnderecoNfe.Add("logradouro", "NW 51 ST CT");
                clienteEnderecoNfe.Add("numero", "1120");
                clienteEnderecoNfe.Add("complemento", "33309");
                clienteEnderecoNfe.Add("bairro", "Fort Lauderdale");
                clienteEnderecoNfe.Add("cep", "99999999");
                clienteEnderecoNfe.Add("uf", "EX");
                clienteEnderecoNfe.Add("cidade", "EXTERIOR");
                clienteEnderecoNfe.Add("pais", "Estados Unidos");

                nfeGateway.Add("id", identificadorNFE);
                nfeGateway.Add("ambienteEmissao", ambienteEmissaoNFE);
                nfeGateway.Add("naturezaOperacao", "Compra para comercialização");
                nfeGateway.Add("tipoOperacao", "Entrada");
                nfeGateway.Add("finalidade", "Normal");
                nfeGateway.Add("consumidorFinal", "|true|");
                nfeGateway.Add("indicadorPresencaConsumidor", "NaoSeAplica");
                nfeGateway.Add("enviarPorEmail", "|false|");

                // Autorizados
                nfeGatewayAutorizados.Add("cpfCnpj", "45887403000132");
                JObject Object_nfeGatewayAutorizados = JObject.Parse(JsonConvert.SerializeObject(nfeGatewayAutorizados, Formatting.Indented));
                

                string textoInformacoesAdicionais = LibStringFormat.SomenteAlfabetoSefaz(record_gc_movimento_nf.informacoes_adicionais);
                if (!string.IsNullOrWhiteSpace(textoInformacoesAdicionais))
                {
                    nfeGateway.Add("informacoesAdicionais", textoInformacoesAdicionais);
                }

                JObject objectNfeGateway = JObject.Parse(JsonConvert.SerializeObject(nfeGateway, Formatting.None));

                JObject objectCliente = JObject.Parse(JsonConvert.SerializeObject(clienteNfe, Formatting.None));
                objectCliente.Add("endereco", JObject.Parse(JsonConvert.SerializeObject(clienteEnderecoNfe, Formatting.None)));

                JObject objectTransporte = JObject.Parse(JsonConvert.SerializeObject(transporteNfe, Formatting.None));
                objectTransporte.Add("frete", JObject.Parse(JsonConvert.SerializeObject(transporteFreteNfe, Formatting.None)));
                if (transporteVolumeNfe.Count > 0)
                {
                    objectTransporte.Add("volume", JObject.Parse(JsonConvert.SerializeObject(transporteVolumeNfe, Formatting.None)));
                }

                objectNfeGateway.Add("cliente", objectCliente);
                objectNfeGateway.Add("transporte", objectTransporte);
                objectNfeGateway.Add("itens", listaItensNFe);
                objectNfeGateway.Add("autorizados", Object_nfeGatewayAutorizados);

                string strJson = JsonConvert.SerializeObject(objectNfeGateway);
                strJson = strJson.Replace("\"|", "").Replace("|\"", "");

                record_gc_movimento_nf.xml_erp = strJson;

                record_gc_movimento_nf.id_cfop = 22;
                record_gc_movimento_nf.qtd_itens = recordMovimento.qtd_itens;
                record_gc_movimento_nf.qtd_produtos = recordMovimento.qtd_produtos;
                record_gc_movimento_nf.valor_total_produtos = recordMovimento.valor_total_produtos;
                record_gc_movimento_nf.valor_total_liquido = recordMovimento.valor_total_liquido;
                record_gc_movimento_nf.valor_total_bruto = recordMovimento.valor_total_bruto;
                record_gc_movimento_nf.id_nfe_status = 1;
                record_gc_movimento_nf.nf_data_geracao = dataHoraAtual;
                record_gc_movimento_nf.nf_id_usuario_geracao = CachePersister.userIdentity.IdUsuario;
                record_gc_movimento_nf.nf_identificador = identificadorNFE;
                record_gc_movimento_nf.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                record_gc_movimento_nf.datahora_cadastro = dataHoraAtual;
                record_gc_movimento_nf.id_nfe_gateway = 1;
                record_gc_movimento_nf.id_coligada = 1;
                record_gc_movimento_nf.id_filial = 1;

                db.gc_movimentos_nf.Add(record_gc_movimento_nf);
                db.SaveChanges();

                string urlAuth = EnotasApiBaseUrl + "/v2/empresas/" + key2 + "/nf-e";       // NFe - Produtos

                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                var request = (HttpWebRequest)WebRequest.Create(urlAuth);
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", "Basic " + key1);
                request.Method = "POST";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(strJson);
                }

                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseData = streamReader.ReadToEnd();

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception(responseData);
                    }

                    sucesso = true;

                    recordMovimento.id_nfe_status = 1;
                    recordMovimento.nf_key = identificadorNFE;
                    recordMovimento.nf_id_usuario_geracao = CachePersister.userIdentity.IdUsuario;
                    recordMovimento.nf_data_geracao = dataHoraAtual;
                    recordMovimento.movimento_faturado = true;
                    recordMovimento.movimento_nf = true;

                    if (recordMovimento.id_movimento_status < 2)
                    {
                        recordMovimento.id_movimento_status = 2;
                    }

                    if (recordMovimento.id_movimento_posicao < 4)
                    {
                        recordMovimento.id_movimento_posicao = 4;
                    }

                    recordMovimento.id_usuario_faturamento = CachePersister.userIdentity.IdUsuario;
                    recordMovimento.datahora_faturamento = dataHoraAtual;
                    recordMovimento.datahora_alteracao = dataHoraAtual;
                    recordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;

                    db.Entry(recordMovimento).State = EntityState.Modified;

                    g_nfe_logs record_g_nfe_logs = new g_nfe_logs
                    {
                        id_nfe = 0,
                        id_movimento = recordMovimento.id_movimento,
                        id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                        id_cliente = recordMovimento.id_cliente,
                        envio = true,
                        retorno = false,
                        identificador_nfe = identificadorNFE,
                        log = "NF Importação Transmitida com sucesso!",
                        datahora_cadastro = dataHoraAtual,
                        id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                    };
                    db.g_nfe_logs.Add(record_g_nfe_logs);

                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new a_yesprodutos_extrato
                    {
                        id_yesproduto = 2,
                        log = "NFe - Importação - Id: " + record_gc_movimento_nf.id_movimento_nf.EmptyIfNull().ToString(),
                        datahora_execucao = LibDateTime.getDataHoraBrasilia(),
                        id_usuario_execucao = CachePersister.userIdentity.IdUsuario
                    };
                    db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                PersistirFalhaTransmissaoJsonEnotasMovimentoNf(record_gc_movimento_nf, identificadorNFE, ex, dataHoraAtual);
                throw;
            }

            return sucesso;
        }
        private void RatearFreteNosItensImportacao(gc_movimentos_nf record_gc_movimento_nf, List<gc_movimentos_itens> listaMovimentosItens)
        {
            if (record_gc_movimento_nf.frete_valor <= 0)
            {
                foreach (var item in listaMovimentosItens)
                {
                    item.valor_frete = 0;
                }
                return;
            }

            decimal valorTotalProdutos = listaMovimentosItens.Sum(i => i.valor_total);
            decimal freteValorTotal = record_gc_movimento_nf.frete_valor;
            decimal freteTotalRateado = 0;
            int index = 0;

            foreach (var item in listaMovimentosItens)
            {
                index++;

                if (index < listaMovimentosItens.Count)
                {
                    decimal fretePercentualRateio = valorTotalProdutos == 0 ? 0 : ((item.valor_total * 100) / valorTotalProdutos);
                    decimal freteValorRateio = (freteValorTotal * (fretePercentualRateio / 100));
                    item.valor_frete = freteValorRateio;
                    freteTotalRateado += freteValorRateio;
                }
                else
                {
                    item.valor_frete = freteValorTotal - freteTotalRateado;
                }
            }
        }
        #endregion

        #region Gerar Nota Fiscal Produtos - Venda
        public bool GerarNFPVendaByMovimentoNF(gc_movimentos_nf record_gc_movimento_nf) 
        {
            bool sucesso = false;
            bool DifalGeralCalcular = true;
            bool DifalGeralZerar = false;
            bool DifalGeralNaoinformar = false;
            bool DifalCombCalcular = true;
            bool DifalCombZerar = false;
            bool DifalCombNaoinformar = false;
            bool DifalPresenteOperacao = false;
            bool OperacaoDentroUF = false;
            bool OperacaoForaUF = false;
            bool OperacaoContribuinte = false;
            bool OperacaoNaoContribuinte = false;
            bool OperacaoConsumidor = false;
            bool OperacaoRevenda = false;
            int QtdErros = 0;
            int IdMovimentoReferenciado = 0;
            String ClienteTipoPessoa = String.Empty;
            String ClienteDocumento = String.Empty;
            String ClienteCidade = String.Empty;
            String ClienteUF = String.Empty;
            String ClienteIndicadorIE = "Isento";
            String ClienteTelefone = String.Empty;
            String IdentificadorNFE = String.Empty;
            String MsgErro = String.Empty;
            String _naturezaOperacao = String.Empty;
            String _tipoOperacao = String.Empty;
            String _FinalidadeNfe = String.Empty;
            String _cfop = String.Empty;
            String _jsonEnvio = String.Empty;
            Decimal ValorIcmsDifalItem = 0;
            Decimal ValorIcmsDifalContasPagar = 0;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia();

            g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(record_gc_movimento_nf.id_filial);
            if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
            if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
            String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
            String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
            bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
            bool ServidorContingencia = RecordNfeGateway.contingencia;

            // Gateway001
            IDictionary<String, String> NfeGateway = new Dictionary<String, String>();
            IDictionary<String, String> NfeGateway_Autorizados = new Dictionary<String, String>();
            IDictionary<String, String> ClienteNfe = new Dictionary<String, String>();
            IDictionary<String, String> ClienteNfe_Endereco = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Combustivel = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Impostos_Venda = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Impostos_Venda_ibsCbs = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Impostos_Venda_Icms = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Impostos_Venda_Ipi = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Impostos_Venda_Pis = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Impostos_Venda_Cofins = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Impostos_Venda_PercentualAproximadoTributos = new Dictionary<String, String>();
            IDictionary<String, String> ItemNFe_Impostos_Venda_PercentualAproximadoTributos_Simplificado = new Dictionary<String, String>();
            IDictionary<String, String> TransporteNFe = new Dictionary<String, String>();
            IDictionary<String, String> TransporteNFe_Frete = new Dictionary<String, String>();
            IDictionary<String, String> TransporteNFe_Volume = new Dictionary<String, String>();
            IDictionary<String, String> TransporteNFe_Transportadora = new Dictionary<String, String>();
            IDictionary<String, String> Cobranca = new Dictionary<String, String>();
            IDictionary<String, String> Cobranca_Fatura = new Dictionary<String, String>();
            IDictionary<String, String> Cobranca_Parcelas = new Dictionary<String, String>();
            IDictionary<String, String> Nfe_Referenciada = new Dictionary<String, String>();
            JArray ListaItensNFe = new JArray();
            JArray ListaNfeReferenciada = new JArray();
            JArray ListaCobrancaParcelas = new JArray();

            NfeGateway.Clear();
            NfeGateway_Autorizados.Clear();
            ClienteNfe.Clear();
            ClienteNfe_Endereco.Clear();
            TransporteNFe.Clear();
            TransporteNFe_Frete.Clear();
            TransporteNFe_Volume.Clear();
            TransporteNFe_Transportadora.Clear();

            try
            {
                var ListaProdutos = new List<Db.g_produtos>();
                ListaProdutos = db.g_produtos.SqlQuery("select p.* from g_produtos p join gc_movimentos_itens i on(p.id_produto = i.id_produto) where i.id_movimento = " + record_gc_movimento_nf.id_movimento.EmptyIfNull().ToString()).ToList();
                var ListaProdutosNCM = new List<Db.g_produtos_ncm>();
                ListaProdutosNCM = db.g_produtos_ncm.SqlQuery("select n.*from g_produtos_ncm n join g_produtos p on (n.id_produto_ncm = p.id_produto_ncm) join gc_movimentos_itens i on (p.id_produto = i.id_produto) where i.id_movimento = " + record_gc_movimento_nf.id_movimento.EmptyIfNull().ToString()).ToList();
                List<gc_movimentos_itens> ListaMovimentosItens = db.gc_movimentos_itens.Where(i => i.id_movimento == record_gc_movimento_nf.id_movimento).ToList();
                List<gc_movimentos_nf> ListaMovimentosNF = db.gc_movimentos_nf.Where(i => i.id_movimento == record_gc_movimento_nf.id_movimento).ToList();
                List<g_unidade_medida> ListaUnidadeMedida = db.g_unidade_medida.ToList();
                List<g_nfe_logs> ListaEnviosNFE = db.g_nfe_logs.Where(l => (l.id_movimento == record_gc_movimento_nf.id_movimento && l.envio == true)).ToList();
                List<gc_cfop_parametros> ListaParametrosCFOP = db.gc_cfop_parametros.Where(p => (p.id_cfop_parametro  > 0)).ToList();
                gc_cfop_operacoes RecordCfopOperacao = db.gc_cfop_operacoes.Find(record_gc_movimento_nf.id_cfop_operacao);
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento);
                g_clientes RecordClienteDb = db.g_clientes.Find(RecordMovimento.id_cliente);
                g_clientes RecordCliente = LibDB.CloneTObject(RecordClienteDb);
                gc_movimentos_tipos RecordMovimentoTipo = db.gc_movimentos_tipos.Find(RecordMovimento.id_movimento_tipo);

                g_nfe_finalidade RecordNfeFinalidade = db.g_nfe_finalidade.Find(RecordCfopOperacao.id_nfe_finalidade);
                _FinalidadeNfe = RecordNfeFinalidade.resumo.EmptyIfNull().ToString();
                if (_FinalidadeNfe.EmptyIfNull().ToString().Length == 0) { _FinalidadeNfe = "Normal"; };
                g_uf RecordUfDestinatarioICMS = db.g_uf.Find(RecordCliente.id_uf_com);
                gc_parametros record_gc_parametros = db.gc_parametros.Find(1);

                // Natureza e Tipo da Operação
                _naturezaOperacao = RecordCfopOperacao.descricao_nfe.EmptyIfNull().ToString();
                if (RecordCfopOperacao.operacao_entrada == true) { _tipoOperacao = "Entrada"; } else { _tipoOperacao = "Saida"; };

                // Validar se o Destinatário é o mesmo do cliente

                if ((record_gc_movimento_nf.id_cliente_destinatario > 0) && ((record_gc_movimento_nf.id_cfop_operacao == 4) || (record_gc_movimento_nf.id_cfop_operacao == 18))) // Só muda o destinatário na NF nas operações de venda e remessa
                {
                    g_clientes_destinatarios record_g_clientes_destinatarios = db.g_clientes_destinatarios.Find(record_gc_movimento_nf.id_cliente_destinatario);
                    RecordCliente.nome = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.nome);
                    RecordCliente.razao_social = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.razao_social);
                    RecordCliente.nome_fantasia = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.razao_social);
                    RecordCliente.cnpj = record_g_clientes_destinatarios.cnpj;
                    RecordCliente.inscricao_municipal = record_g_clientes_destinatarios.inscricao_municipal;
                    RecordCliente.inscricao_estadual = record_g_clientes_destinatarios.inscricao_estadual;
                    RecordCliente.id_indicador_ie = record_g_clientes_destinatarios.id_indicador_ie;
                    RecordCliente.cpf = record_g_clientes_destinatarios.cpf;
                    RecordCliente.endereco_com = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.endereco_com);
                    RecordCliente.endereco_com_numero = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.endereco_com_numero);
                    RecordCliente.endereco_com_complemento = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.endereco_com_complemento);
                    RecordCliente.bairro_com = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.bairro_com);
                    RecordCliente.id_cidade_com = record_g_clientes_destinatarios.id_cidade_com;
                    RecordCliente.cep_com = record_g_clientes_destinatarios.cep_com;
                    RecordCliente.id_uf_com = record_g_clientes_destinatarios.id_uf_com;
                    RecordCliente.telefone_principal = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.telefone_1);
                    RecordCliente.telefone_2 = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.telefone_2);
                    RecordCliente.telefone_3 = LibStringFormat.SomenteAlfabetoSefaz(record_g_clientes_destinatarios.telefone_2);
                    RecordCliente.email_principal = record_g_clientes_destinatarios.email;
                    RecordUfDestinatarioICMS = db.g_uf.Find(RecordCliente.id_uf_com); // Configurar o novo destino para calcular o icms correto
                }
                else
                {
                    // Tratar nas informações complementares
                }

                if (record_gc_movimento_nf.id_filial == 1) { if (RecordUfDestinatarioICMS.id_uf == 11) { OperacaoDentroUF = true; } else { OperacaoForaUF = true; } }
                else if (record_gc_movimento_nf.id_filial == 2) { if (RecordUfDestinatarioICMS.id_uf == 26) { OperacaoDentroUF = true; } else { OperacaoForaUF = true; } };

                // Parâmetro para Zerar ou não o Difal
                gc_parametros_difal record_gc_parametros_difal = db.gc_parametros_difal.Where(d => d.id_uf == RecordUfDestinatarioICMS.id_uf).FirstOrDefault();
                if (record_gc_parametros_difal != null) 
                { 
                    DifalGeralCalcular = record_gc_parametros_difal.difal_geral_calcular;
                    DifalGeralZerar = record_gc_parametros_difal.difal_geral_zerar;
                    DifalGeralNaoinformar = record_gc_parametros_difal.difal_geral_naoinformar;
                    DifalCombCalcular = record_gc_parametros_difal.difal_comb_calcular;
                    DifalCombZerar = record_gc_parametros_difal.difal_comb_zerar;
                    DifalCombNaoinformar = record_gc_parametros_difal.difal_comb_naoinformar;
                };

                // Operacao - Consumidor Final ou Revenda
                if (RecordMovimento.id_cfop_finalidade == 2) { OperacaoRevenda = true; } else { OperacaoConsumidor = true; };

                // Ajustes nos dados do Cliente
                if (RecordCliente.razao_social.EmptyIfNull().Trim().Length == 0) { RecordCliente.razao_social = LibStringFormat.SomenteAlfabetoSefaz(RecordCliente.nome); };
                if (RecordCliente.nome_fantasia.EmptyIfNull().Trim().Length == 0) { RecordCliente.nome_fantasia = LibStringFormat.SomenteAlfabetoSefaz(RecordCliente.razao_social); };

                // Consistência de Moeda
                if (RecordMovimento.id_moeda != 1)
                {
                    QtdErros += 1;
                    MsgErro += "Moeda do pedido diferente de [R$ Real]" + "<br/>";
                }
                
                // Consistências de Produtos
                foreach (var RecordProduto in ListaProdutos)
                {
                    bool ErroItem = false;

                    if (record_gc_parametros.check_produto_manual == true)
                    {
                        if (RecordProduto.importado == false)
                        {
                            QtdErros += 1;
                            ErroItem = true;
                            MsgErro += "Item [" + RecordProduto.descricao.EmptyIfNull().ToString() + "] cadastrado manualmente, substituir pelo Item correto!" + "<br/>";
                        }
                    }

                    if ((ErroItem == false) && (RecordProduto.id_produto_ncm <= 0))
                    {
                        QtdErros += 1;
                        MsgErro += "Item ["+ RecordProduto.descricao.EmptyIfNull().ToString() + "] Sem classificação NCM" + "<br/>";
                    }
                }

                // Nota Fiscal Referenciada
                if (RecordCfopOperacao.has_nfe_referenciada == true)
                {
                    if (record_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Trim().Length > 0)
                    {
                        string NfChaveReferenciada = record_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Trim();
                        NfChaveReferenciada = LibStringFormat.RemoverEspacos(NfChaveReferenciada);
                        record_gc_movimento_nf.nf_chave_referenciada = NfChaveReferenciada;
                    }
                    
                    
                    if (record_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Trim().Length != 44)
                    {
                        QtdErros += 1;
                        MsgErro += "Informe os 44 dígitos da Chave de Acesso da Nota Fiscal referenciada!" + "<br/>";
                    }
                }
                if (record_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Length > 0)
                {
                    if (record_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Length == 44)
                    {
                        string ChaveAcessoReferenciada = record_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Trim();
                        gc_movimentos_nf record_gc_movimentos_nf = db.gc_movimentos_nf.Where(nf => nf.nf_chave_acesso == ChaveAcessoReferenciada).FirstOrDefault();

                        if (record_gc_movimentos_nf != null)
                        {
                            IdMovimentoReferenciado = record_gc_movimentos_nf.id_movimento;
                            ListaNfeReferenciada.Clear();
                            Nfe_Referenciada.Clear();
                            Nfe_Referenciada.Add("chaveAcesso", record_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Trim());
                            ListaNfeReferenciada.Add(JObject.Parse(JsonConvert.SerializeObject(Nfe_Referenciada, Formatting.Indented)));
                        }
                        else
                        {
                            QtdErros += 1;
                            MsgErro += "Chave de Acesso referenciada ["+ ChaveAcessoReferenciada + "] Não foi localizada no bando de dados!" + "<br/>";
                        }
                    }
                    else
                    {
                        QtdErros += 1;
                        MsgErro += "Informe os 44 dígitos da Chave de Acesso da Nota Fiscal referenciada!" + "<br/>";
                    }
                }

                if (QtdErros == 0)
                {
                    if (RecordCliente.cnpj.EmptyIfNull().ToString().Length > 10)
                    {
                        ClienteTipoPessoa = "J";
                        ClienteDocumento = RecordCliente.cnpj.EmptyIfNull().ToString();
                    }
                    else
                    {
                        ClienteTipoPessoa = "F";
                        ClienteDocumento = RecordCliente.cpf.EmptyIfNull().ToString();
                    }
                    
                    // Parâmetros - Contribuinte ou Não Contribuinte
                    if (RecordCliente.id_indicador_ie == 1) // Isento
                    {

                        if (record_gc_movimento_nf.id_filial == 1) { RecordUfDestinatarioICMS = db.g_uf.Find(11); } // Aplica os percentuais de MG
                        else if (record_gc_movimento_nf.id_filial == 2) { RecordUfDestinatarioICMS = db.g_uf.Find(26); }; // Aplica os percentuais de SP
                        OperacaoNaoContribuinte = true;
                    } 
                    else if (RecordCliente.id_indicador_ie == 2) // Contribuinte
                    { 
                        ClienteIndicadorIE = "Contribuinte";
                        OperacaoContribuinte = true;
                    }
                    else if (RecordCliente.id_indicador_ie == 3) 
                    { 
                        ClienteIndicadorIE = "NaoContribuinte";
                        OperacaoNaoContribuinte = true;
                    }
                    if (ClienteIndicadorIE == "NaoContribuinte")
                    {
                        if ((record_gc_movimento_nf.id_filial == 1) && (RecordUfDestinatarioICMS.id_uf != 11)) { DifalPresenteOperacao = true; }
                        else if ((record_gc_movimento_nf.id_filial == 2) && (RecordUfDestinatarioICMS.id_uf != 26)) { DifalPresenteOperacao = true; };
                    }

                    if (RecordCliente.telefone_principal.EmptyIfNull().ToString().Length > 0) { ClienteTelefone = RecordCliente.telefone_principal.EmptyIfNull().ToString(); } else { ClienteTelefone = "0000000000"; };
                    ClienteCidade = db.g_cidades.Find(RecordCliente.id_cidade_com).nome.EmptyIfNull().ToString().ToUpperInvariant();
                    ClienteUF = db.g_uf.Find(RecordCliente.id_uf_com).sigla.EmptyIfNull().ToString();
                    if ((RecordCliente.inscricao_municipal != null) && (RecordCliente.inscricao_municipal.ToString().Length <= 0)) { RecordCliente.inscricao_municipal = null; };
                    if ((RecordCliente.inscricao_estadual != null) && (RecordCliente.inscricao_estadual.ToString().Length <= 0)) { RecordCliente.inscricao_estadual = null; };
                    IdentificadorNFE = RecordMovimento.id_movimento.EmptyIfNull().ToString().Trim() + "." + (ListaMovimentosNF.Count + 1).ToString();
                    if (AmbienteEmissaoNFE == "Homologacao") { IdentificadorNFE += ".h"; };

                    // Proporcionalizar o valor do frete nos itens
                    if (record_gc_movimento_nf.frete_valor > 0)
                    {
                        Decimal ValorTotalProdutos = 0;
                        Decimal FreteValorTotal = record_gc_movimento_nf.frete_valor;
                        Decimal FreteValorRateio = 0;
                        Decimal FreteTotalRateado = 0;
                        Decimal FretePercentualRateio = 0;
                        int Index = 0;

                        foreach (gc_movimentos_itens ItemMovimento1 in ListaMovimentosItens)
                        {
                            ValorTotalProdutos += ItemMovimento1.valor_total;
                        }
                        foreach (gc_movimentos_itens ItemMovimento2 in ListaMovimentosItens)
                        {
                            Index += 1;
                            if (Index < ListaMovimentosItens.Count())
                            {
                                FretePercentualRateio = ((ItemMovimento2.valor_total * 100) / ValorTotalProdutos);
                                FreteValorRateio = LibNumbers.TruncateDecimal(((FreteValorTotal) * (FretePercentualRateio / 100)),2);
                                ItemMovimento2.valor_frete = FreteValorRateio;
                                FreteTotalRateado += FreteValorRateio;
                            }
                            else
                            {
                                FreteValorRateio = FreteValorTotal - FreteTotalRateado;
                                ItemMovimento2.valor_frete = FreteValorRateio;
                                FreteTotalRateado += FreteValorRateio;
                            }
                        }
                    }
                    else
                    {
                        foreach (gc_movimentos_itens ItemMovimento3 in ListaMovimentosItens)
                        {
                                ItemMovimento3.valor_frete = 0;
                        }
                    }

                    foreach (gc_movimentos_itens ItemMovimento in ListaMovimentosItens)
                    {
                        String InformacoesAdicionaisItem = string.Empty;
                        int IdGrupoProduto = 0;
                        int IdCfopProduto = 0;
                        gc_cfop RecordCfop = null;
                        g_produtos RecordProduto = ListaProdutos.Find(p => p.id_produto == ItemMovimento.id_produto);
                        g_produtos_ncm RecordProdutoNCM = ListaProdutosNCM.Find(p => p.id_produto_ncm == RecordProduto.id_produto_ncm);
                        g_unidade_medida RecordUnidadeMedida = ListaUnidadeMedida.Find(u => u.id_unidade_medida == RecordProduto.id_unidade_medida_venda);

                        // Grupo de Produtos
                        if (RecordProduto.id_produto_grupo > 0) { IdGrupoProduto = RecordProduto.id_produto_grupo; } else { IdGrupoProduto = 1; }
                        gc_cfop_parametros RecordParametroCFOP = ListaParametrosCFOP.Where(p => p.id_cfop_operacao == RecordCfopOperacao.id_cfop_operacao && (p.id_produto_grupo == IdGrupoProduto || p.id_produto_grupo == 0)).FirstOrDefault();

                        if ((OperacaoDentroUF == true) && (OperacaoContribuinte == true) && (OperacaoRevenda == true)) { IdCfopProduto = RecordParametroCFOP.id_cfop_dentrouf_contribuinte_revenda; }
                        if ((OperacaoDentroUF == true) && (OperacaoContribuinte == true) && (OperacaoConsumidor == true)) { IdCfopProduto = RecordParametroCFOP.id_cfop_dentrouf_contribuinte_consumidor; }
                        if ((OperacaoDentroUF == true) && (OperacaoNaoContribuinte == true) && (OperacaoRevenda == true)) { IdCfopProduto = RecordParametroCFOP.id_cfop_dentrouf_naocontribuinte_revenda; }
                        if ((OperacaoDentroUF == true) && (OperacaoNaoContribuinte == true) && (OperacaoConsumidor == true)) { IdCfopProduto = RecordParametroCFOP.id_cfop_dentrouf_naocontribuinte_consumidor; }
                        if ((OperacaoForaUF == true) && (OperacaoContribuinte == true) && (OperacaoRevenda == true)) { IdCfopProduto = RecordParametroCFOP.id_cfop_forauf_contribuinte_revenda; }
                        if ((OperacaoForaUF == true) && (OperacaoContribuinte == true) && (OperacaoConsumidor == true)) { IdCfopProduto = RecordParametroCFOP.id_cfop_forauf_contribuinte_consumidor; }
                        if ((OperacaoForaUF == true) && (OperacaoNaoContribuinte == true) && (OperacaoRevenda == true)) { IdCfopProduto = RecordParametroCFOP.id_cfop_forauf_naocontribuinte_revenda; }
                        if ((OperacaoForaUF == true) && (OperacaoNaoContribuinte == true) && (OperacaoConsumidor == true)) { IdCfopProduto = RecordParametroCFOP.id_cfop_forauf_naocontribuinte_consumidor; }

                        if (RecordParametroCFOP.id_produto_grupo == 0) {  if (record_gc_movimento_nf.id_cfop == 0) { record_gc_movimento_nf.id_cfop = IdCfopProduto; }; }
                        else if (RecordParametroCFOP.id_produto_grupo == 1) { if (record_gc_movimento_nf.id_cfop_grupo1 == 0) { record_gc_movimento_nf.id_cfop_grupo1 = IdCfopProduto; }; }
                        else if (RecordParametroCFOP.id_produto_grupo == 2) { if (record_gc_movimento_nf.id_cfop_grupo2 == 0) { record_gc_movimento_nf.id_cfop_grupo2 = IdCfopProduto; }; }
                        else if (RecordParametroCFOP.id_produto_grupo == 3) { if (record_gc_movimento_nf.id_cfop_grupo3 == 0) { record_gc_movimento_nf.id_cfop_grupo3 = IdCfopProduto; }; }
                        else if (RecordParametroCFOP.id_produto_grupo == 4) { if (record_gc_movimento_nf.id_cfop_grupo4 == 0) { record_gc_movimento_nf.id_cfop_grupo4 = IdCfopProduto; }; }
                        else if (RecordParametroCFOP.id_produto_grupo == 5) { if (record_gc_movimento_nf.id_cfop_grupo5 == 0) { record_gc_movimento_nf.id_cfop_grupo5 = IdCfopProduto; }; }

                        RecordCfop = db.gc_cfop.Find(IdCfopProduto);
                        _cfop = RecordCfop.numero.EmptyIfNull().ToString();
                        gc_icms_cst RecordIcmsCst = db.gc_icms_cst.Find(RecordCfop.id_icms_cst);


                        ItemNFe.Clear();
                        ItemNFe_Impostos_Venda.Clear();
                        ItemNFe_Impostos_Venda_Icms.Clear();
                        ItemNFe_Impostos_Venda_Ipi.Clear();
                        ItemNFe_Impostos_Venda_Pis.Clear();
                        ItemNFe_Impostos_Venda_Cofins.Clear();
                        ItemNFe_Impostos_Venda_ibsCbs.Clear();
                        ItemNFe_Impostos_Venda_PercentualAproximadoTributos.Clear();
                        ItemNFe_Impostos_Venda_PercentualAproximadoTributos_Simplificado.Clear();
                        ItemNFe_Combustivel.Clear();

                        ItemNFe.Add("cfop", RecordCfop.numero.EmptyIfNull().ToString());
                        ItemNFe.Add("codigo", RecordProduto.codigo.EmptyIfNull().ToString());
                        String DescricaoItemNFE = RecordProduto.descricao.EmptyIfNull().ToString().Trim();
                        if (DescricaoItemNFE.Length > 120) { DescricaoItemNFE = DescricaoItemNFE.Substring(0, 120); };
                            // Informações Adicionais do Item
                            if (ItemMovimento.serial.EmptyIfNull().ToString().Length > 0) { InformacoesAdicionaisItem += " |Serial:" + ItemMovimento.serial.EmptyIfNull().ToString(); };
                            if (ItemMovimento.lote01_identificador.EmptyIfNull().ToString().Length > 0) { InformacoesAdicionaisItem += " |Lote:" + ItemMovimento.lote01_identificador.EmptyIfNull().ToString(); };
                            if ((ItemMovimento.obs_nf == true) && (ItemMovimento.obs.EmptyIfNull().ToString().Length > 0)) { InformacoesAdicionaisItem += "|Obs:" + ItemMovimento.obs.EmptyIfNull().ToString(); };
                            if (InformacoesAdicionaisItem.EmptyIfNull().ToString().Length > 0)
                            {
                                if ((DescricaoItemNFE.Length + InformacoesAdicionaisItem.Length) > 120) { DescricaoItemNFE = DescricaoItemNFE.Substring(0, (120 - InformacoesAdicionaisItem.Length)); };
                            DescricaoItemNFE += InformacoesAdicionaisItem;
                            }
                        ItemNFe.Add("descricao", DescricaoItemNFE);
                        ItemNFe.Add("ncm", RecordProdutoNCM.codigo_ncm.EmptyIfNull().ToString());
                        ItemNFe.Add("quantidade", "|" + LibNumbers.DecimalToJson(ItemMovimento.quantidade) + "|");
                        if (RecordUnidadeMedida != null) { ItemNFe.Add("unidadeMedida", RecordUnidadeMedida.codigo.EmptyIfNull().ToString().Trim()); } else { ItemNFe.Add("unidadeMedida" , "UN"); };


                        // Tags xPed e nItemped na NFe
                        String _OrdemCompraNumero = LibStringFormat.SomenteNumeros(RecordMovimento.oc_numero.EmptyIfNull().ToString().Trim());
                        if (_OrdemCompraNumero.Length > 0)
                        {
                            String ItemPedidoCompra = LibNumbers.DecimalToJson(ItemMovimento.sequencia).Replace(".","").Replace(",", "");
                            if (ItemPedidoCompra.Length >= 3) { ItemPedidoCompra = ItemPedidoCompra.Substring(0, ItemPedidoCompra.Length - 2); }
                            ItemNFe.Add("numeroPedidoCompra", _OrdemCompraNumero);
                            ItemNFe.Add("itemPedidoCompra", "|" + ItemPedidoCompra + "|");
                        }
                        ItemNFe.Add("valorUnitario", "|" + LibNumbers.DecimalToJson(ItemMovimento.valor_unit) + "|");
                        ItemNFe.Add("frete", "|" + LibNumbers.DecimalToJson(ItemMovimento.valor_frete) + "|");
                        ItemNFe.Add("outrasDespesas", "|" + LibNumbers.DecimalToJson(ItemMovimento.valor_despesas) + "|");

                        if (RecordCfop.calcula_icms == true)
                        {
                            // Percentual Aproximado dos Tributos
                            ItemNFe_Impostos_Venda_PercentualAproximadoTributos.Add("fonte", "IBPT");
                            ItemNFe_Impostos_Venda_PercentualAproximadoTributos_Simplificado.Add("percentual", "|" + LibNumbers.DecimalToJson(RecordProdutoNCM.tributo_federal_importado + RecordProdutoNCM.tributo_estadual + RecordProdutoNCM.tributo_municipal) + "|");

                            // ICMS -- Cálculo do ICMS
                            Decimal IcmsReducaoBaseCalculo = 0;
                            Decimal IcmsBaseCalculo = 0;
                            Decimal IcmsValor = 0;

                            Decimal ParametroIcmsBaseReducao = 0;
                            Decimal ParametroIcmsPercentuaInterno = 0;
                            Decimal ParametroIcmsPercentuaInterestadual = 0;

                            if ((RecordMovimento.id_filial == 1) || (RecordMovimento.id_filial == 0))
                            {
                                ParametroIcmsBaseReducao = RecordUfDestinatarioICMS.basemg_icms_base_reducao;
                                ParametroIcmsPercentuaInterno = RecordUfDestinatarioICMS.basemg_icms_percentual_interno;
                                ParametroIcmsPercentuaInterestadual = RecordUfDestinatarioICMS.basemg_icms_interestadual;
                            }
                            else if (RecordMovimento.id_filial == 2)
                            {
                                ParametroIcmsBaseReducao = RecordUfDestinatarioICMS.basesp_icms_base_reducao;
                                ParametroIcmsPercentuaInterno = RecordUfDestinatarioICMS.basesp_icms_percentual_interno;
                                ParametroIcmsPercentuaInterestadual = RecordUfDestinatarioICMS.basesp_icms_interestadual;
                            }

                            int IcmsOrigem = 0;
                            int IcmsModalidadeBaseCalculo = 0;
                            String IcmsCst = string.Empty;

                            if (record_gc_movimento_nf.param_reducao_bc == true) 
                            { 
                                IcmsReducaoBaseCalculo = ((ItemMovimento.valor_total / 100) * ParametroIcmsBaseReducao);
                                IcmsBaseCalculo = (ItemMovimento.valor_total - IcmsReducaoBaseCalculo) + ItemMovimento.valor_frete;

                                if (RecordCfopOperacao.id_cfop_operacao == 12) // Para operações de devoluções tributadas, deve-se garantir o mesmo valor de ICMS da nota de origem
                                {
                                    gc_movimentos MovimentoReferenciado = db.gc_movimentos.Find(IdMovimentoReferenciado);
                                    g_clientes ClienteReferenciado = db.g_clientes.Find(MovimentoReferenciado.id_cliente);
                                    g_uf UfReferenciada = db.g_uf.Find(ClienteReferenciado.id_uf_com);

                                    if (ParametroIcmsPercentuaInterestadual != UfReferenciada.basemg_icms_interestadual)
                                    {
                                        IcmsBaseCalculo = (IcmsBaseCalculo / ParametroIcmsPercentuaInterestadual) * UfReferenciada.basemg_icms_interestadual;
                                    }
                                }
                            }
                            else
                            {
                                IcmsReducaoBaseCalculo = 0;
                                IcmsBaseCalculo = (ItemMovimento.valor_total - IcmsReducaoBaseCalculo);
                            }
                            IcmsValor = ((IcmsBaseCalculo / 100) * ParametroIcmsPercentuaInterestadual);
                            IcmsOrigem = int.Parse(RecordIcmsCst.codigo_origem.EmptyIfNull().ToString());

                            ItemNFe_Impostos_Venda_ibsCbs.Add("classificacaoTributaria", "000001");

                            ItemNFe_Impostos_Venda_Icms.Add("origem" , "|" + LibNumbers.DecimalToJson(int.Parse(RecordIcmsCst.codigo_origem.EmptyIfNull().ToString())) + "|");
                            if (record_gc_movimento_nf.param_reducao_bc == true)
                            {
                                IcmsCst = RecordIcmsCst.codigo_tributacao.EmptyIfNull().ToString();
                                ItemNFe_Impostos_Venda_Icms.Add("situacaoTributaria", RecordIcmsCst.codigo_tributacao.EmptyIfNull().ToString());
                                ItemNFe_Impostos_Venda_Icms.Add("percentualReducaoBaseCalculo", "|" + LibNumbers.DecimalToJson(ParametroIcmsBaseReducao) + "|");
                            }
                            else
                            {
                                ItemNFe_Impostos_Venda_Icms.Add("situacaoTributaria", "00"); // Tributado integralmente
                                ItemNFe_Impostos_Venda_Icms.Add("percentualReducaoBaseCalculo", "|" + LibNumbers.DecimalToJson(0) + "|");
                            }
                            IcmsModalidadeBaseCalculo = 3;
                            ItemNFe_Impostos_Venda_Icms.Add("modalidadeBaseCalculo", "|3|"); // Valor da operação
                            ItemNFe_Impostos_Venda_Icms.Add("aliquota", "|" + LibNumbers.DecimalToJson(ParametroIcmsPercentuaInterestadual) + "|");
                            ItemNFe_Impostos_Venda_Icms.Add("baseCalculo", "|" + LibNumbers.DecimalToJson(Math.Round(IcmsBaseCalculo, 2)) + "|");
                            ItemNFe_Impostos_Venda_Icms.Add("valor", "|" + LibNumbers.DecimalToJson(Math.Round(IcmsValor, 2)) + "|");


                            // Tag - cBenef - Especifico para Sefaz SP
                            if (record_gc_movimento_nf.id_filial == 2)
                            {
                                String TagCBenef = RecordIcmsCst.sefazsp_cbenef_tag_normal.EmptyIfNull().ToString();
                                if (record_gc_movimento_nf.param_reducao_bc == true) { TagCBenef = RecordIcmsCst.sefazsp_cbenef_tag_beneficio.EmptyIfNull().ToString(); };
                                if ((TagCBenef.EmptyIfNull().ToString().Length > 0) && (TagCBenef.EmptyIfNull().ToString() != "SEM PREENCHIMENTO"))
                                {
                                    ItemNFe_Impostos_Venda_Icms.Add("cBenef", TagCBenef.EmptyIfNull().ToString());
                                }
                            }

                            // Cálculo Difal
                            if (DifalPresenteOperacao == true)
                            {
                                bool DifalItemCalcular = true;
                                bool DifalItemZerar = false;
                                bool DifalItemNaoinformar = false;

                                ValorIcmsDifalItem = Math.Round(((IcmsBaseCalculo / 100) * (ParametroIcmsPercentuaInterno - ParametroIcmsPercentuaInterestadual)), 2);
                                if (RecordProduto.id_produto_grupo == 2) // Combustíveis
                                {
                                    DifalItemCalcular = DifalCombCalcular;
                                    DifalItemZerar = DifalCombZerar;
                                    DifalItemNaoinformar = DifalCombNaoinformar;
                                }
                                else // Geral
                                {
                                    DifalItemCalcular = DifalGeralCalcular;
                                    DifalItemZerar = DifalGeralZerar;
                                    DifalItemNaoinformar = DifalGeralNaoinformar;
                                }

                                if (DifalItemNaoinformar == true)
                                {
                                    ItemNFe_Impostos_Venda_Icms.Add("naoCalcularDifal", "|true|");
                                    ItemNFe_Impostos_Venda_Icms.Add("naoCalcularFCP", "|true|");
                                }
                                else if (DifalItemZerar == true)
                                {
                                    ItemNFe_Impostos_Venda_Icms.Add("baseCalculoUFDestinoDifal", "|0|");
                                    ItemNFe_Impostos_Venda_Icms.Add("aliquotaUFDestinoDifal", "|0|");
                                    ItemNFe_Impostos_Venda_Icms.Add("valorUFDestinoDifal", "|0|");
                                    ItemNFe_Impostos_Venda_Icms.Add("valorUFOrigemDifal", "|0|");
                                    ItemNFe_Impostos_Venda_Icms.Add("aliquotaInterestadualDifal", "|" + LibNumbers.DecimalToJson(ParametroIcmsPercentuaInterestadual) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("percentualPartilhaInterestadualDifal", "|" + LibNumbers.DecimalToJson(100) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("baseCalculoFundoCombatePobrezaDifal", "|0|");
                                    ItemNFe_Impostos_Venda_Icms.Add("percentualFCPDifal", "|0|");
                                    ItemNFe_Impostos_Venda_Icms.Add("valorFCPDifal", "|0|");
                                    ItemMovimento.icms_difal_vbcufdest = 0;
                                    ItemMovimento.icms_difal_picmsufdest = 0;
                                    ItemMovimento.icms_difal_vicmsufdest = 0;
                                    ItemMovimento.icms_difal_picmsinter = ParametroIcmsPercentuaInterestadual;
                                    ItemMovimento.icms_difal_picmsinterpart = 100;
                                    ItemMovimento.icms_difal_vicmsufremet = 0;
                                    ItemMovimento.icms_difal_vbcfcpufdest = 0;
                                    ItemMovimento.icms_difal_pfcpufdest = 0;
                                    ItemMovimento.icms_difal_vfcpufdest = 0;
                                }
                                else if (DifalItemCalcular == true) // Se parametro calcular difal está ativo e usuário não pediu para zerar o difal, calcular o valor
                                {
                                    ItemNFe_Impostos_Venda_Icms.Add("baseCalculoUFDestinoDifal", "|" + LibNumbers.DecimalToJson(Math.Round(IcmsBaseCalculo, 2)) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("aliquotaUFDestinoDifal", "|" + LibNumbers.DecimalToJson(ParametroIcmsPercentuaInterno) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("valorUFDestinoDifal", "|" + LibNumbers.DecimalToJson(ValorIcmsDifalItem) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("valorUFOrigemDifal", "|" + LibNumbers.DecimalToJson(0) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("aliquotaInterestadualDifal", "|" + LibNumbers.DecimalToJson(ParametroIcmsPercentuaInterestadual) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("percentualPartilhaInterestadualDifal", "|" + LibNumbers.DecimalToJson(100) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("baseCalculoFundoCombatePobrezaDifal", "|" + LibNumbers.DecimalToJson(0) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("percentualFCPDifal", "|" + LibNumbers.DecimalToJson(0) + "|");
                                    ItemNFe_Impostos_Venda_Icms.Add("valorFCPDifal", "|" + LibNumbers.DecimalToJson(0) + "|");
                                    ItemMovimento.icms_difal_vbcufdest = Math.Round(IcmsBaseCalculo, 2);
                                    ItemMovimento.icms_difal_picmsufdest = ParametroIcmsPercentuaInterno;
                                    ItemMovimento.icms_difal_vicmsufdest = ValorIcmsDifalItem;
                                    ItemMovimento.icms_difal_picmsinter = ParametroIcmsPercentuaInterestadual;
                                    ItemMovimento.icms_difal_picmsinterpart = 100;
                                    ItemMovimento.icms_difal_vicmsufremet = 0;
                                    ItemMovimento.icms_difal_vbcfcpufdest = 0;
                                    ItemMovimento.icms_difal_pfcpufdest = 0;
                                    ItemMovimento.icms_difal_vfcpufdest = 0;
                                    ValorIcmsDifalContasPagar += ValorIcmsDifalItem;
                                }
                            }

                            // Ajustar os valores de ICMS do item
                            ItemMovimento.icms_orig = IcmsOrigem;
                            ItemMovimento.icms_cst = IcmsCst;
                            if (record_gc_movimento_nf.param_reducao_bc == false) { ItemMovimento.icms_cst = "0"; };
                            ItemMovimento.icms_modbc = IcmsModalidadeBaseCalculo;
                            ItemMovimento.icms_predbc = ParametroIcmsBaseReducao;
                            ItemMovimento.icms_vbc = IcmsBaseCalculo;
                            ItemMovimento.icms_picms = ParametroIcmsPercentuaInterestadual;
                            ItemMovimento.icms_vicms = Math.Round(IcmsValor, 2);
                            ItemMovimento.datahora_alteracao = DataHoraAtual;
                            ItemMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(ItemMovimento).State = EntityState.Modified;

                            // PIS
                            //ItemNFe_Impostos_Venda_Pis.Add("situacaoTributaria", "07");
                            //ItemNFe_Impostos_Venda_Pis.Add("origem", "|0|");
                            ItemNFe_Impostos_Venda_Pis.Add("situacaoTributaria", "06");

                            // COFINS
                            //ItemNFe_Impostos_Venda_Cofins.Add("situacaoTributaria", "07");
                            //ItemNFe_Impostos_Venda_Cofins.Add("origem", "|0|");
                            ItemNFe_Impostos_Venda_Cofins.Add("situacaoTributaria", "06");
                        }
                        else 
                        {
                            // Percentual Aproximado dos Tributos
                            ItemNFe_Impostos_Venda_PercentualAproximadoTributos.Add("fonte", "IBPT");
                            ItemNFe_Impostos_Venda_PercentualAproximadoTributos_Simplificado.Add("percentual", "|" + LibNumbers.DecimalToJson(RecordProdutoNCM.tributo_federal_importado + RecordProdutoNCM.tributo_estadual + RecordProdutoNCM.tributo_municipal) + "|");

                            ItemNFe_Impostos_Venda_Icms.Add("situacaoTributaria", "41");
                            ItemNFe_Impostos_Venda_Icms.Add("origem", "|0|");

                            ItemNFe_Impostos_Venda_Pis.Add("situacaoTributaria", "07");
                            ItemNFe_Impostos_Venda_Pis.Add("origem", "|0|");

                            ItemNFe_Impostos_Venda_Cofins.Add("situacaoTributaria", "07");
                            ItemNFe_Impostos_Venda_Cofins.Add("origem", "|0|");
                        }
                        // Atualização do valor de venda do produto
                        RecordProduto.preco_venda = ItemMovimento.valor_unit;

                        //---------- COMBUSTÍVEL ----------//
                        if ((RecordCfop.regulamentado_anp == true) && (RecordProduto.codigo_anp.EmptyIfNull().ToString().Length > 0))
                        {
                            ItemNFe_Combustivel.Add("codigoProdutoANP", RecordProduto.codigo_anp.EmptyIfNull().ToString());
                            ItemNFe_Combustivel.Add("quantidadeFaturadaTempAmbiente", "|" + LibNumbers.DecimalToJson(ItemMovimento.quantidade) + "|");
                            ItemNFe_Combustivel.Add("ufConsumo", ClienteUF);
                        }
                        JObject Object_ItemNFe_Combustivel = JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Combustivel, Formatting.Indented));

                        //---------- JSON----------//
                        // Impostos
                        JObject Object_ItemNFe_Impostos_Venda = JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Impostos_Venda, Formatting.Indented));
                        if (ItemNFe_Impostos_Venda_ibsCbs.Count > 0) { Object_ItemNFe_Impostos_Venda.Add("ibsCbs", JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Impostos_Venda_ibsCbs, Formatting.Indented))); }
                        if (ItemNFe_Impostos_Venda_Icms.Count > 0) { Object_ItemNFe_Impostos_Venda.Add("icms", JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Impostos_Venda_Icms, Formatting.Indented))); }
                        if (ItemNFe_Impostos_Venda_Ipi.Count > 0) { Object_ItemNFe_Impostos_Venda.Add("ipi", JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Impostos_Venda_Ipi, Formatting.Indented))); }
                        if (ItemNFe_Impostos_Venda_Pis.Count > 0) { Object_ItemNFe_Impostos_Venda.Add("pis", JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Impostos_Venda_Pis, Formatting.Indented))); }
                        if (ItemNFe_Impostos_Venda_Cofins.Count > 0) { Object_ItemNFe_Impostos_Venda.Add("cofins", JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Impostos_Venda_Cofins, Formatting.Indented))); };

                        // Percentual Aproximado Triutos
                        JObject Object_ItemNFe_Impostos_Venda_PercentualAproximadoTributos = JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Impostos_Venda_PercentualAproximadoTributos, Formatting.Indented));
                        if (ItemNFe_Impostos_Venda_PercentualAproximadoTributos_Simplificado.Count > 0)
                        {
                            JObject Object_ItemNFe_Impostos_Venda_PercentualAproximadoTributos_Simplificado = JObject.Parse(JsonConvert.SerializeObject(ItemNFe_Impostos_Venda_PercentualAproximadoTributos_Simplificado, Formatting.Indented));
                            Object_ItemNFe_Impostos_Venda_PercentualAproximadoTributos.Add("simplificado", Object_ItemNFe_Impostos_Venda_PercentualAproximadoTributos_Simplificado);
                        }
                        Object_ItemNFe_Impostos_Venda.Add("percentualAproximadoTributos", Object_ItemNFe_Impostos_Venda_PercentualAproximadoTributos);
                        JObject Object_ItemNFe = JObject.Parse(JsonConvert.SerializeObject(ItemNFe, Formatting.Indented));
                        if (ItemNFe_Combustivel.Count > 0) { Object_ItemNFe.Add("combustivel", Object_ItemNFe_Combustivel); }
                        Object_ItemNFe.Add("impostos", Object_ItemNFe_Impostos_Venda);
                        ListaItensNFe.Add(Object_ItemNFe);
                    }

                    // Transporte
                    if (record_gc_movimento_nf.id_frete_responsavel == 1) { TransporteNFe_Frete.Add("modalidade", "PorContaDoEmitente"); }
                    else if (record_gc_movimento_nf.id_frete_responsavel == 2) { TransporteNFe_Frete.Add("modalidade", "PorContaDoDestinatario"); }
                    else if (record_gc_movimento_nf.id_frete_responsavel == 3) { TransporteNFe_Frete.Add("modalidade", "SemFrete"); }
                    else { TransporteNFe_Frete.Add("modalidade", "PorContaDeTerceiros"); }

                    TransporteNFe_Frete.Add("valor", "|" + LibNumbers.DecimalToJson(record_gc_movimento_nf.frete_valor) + "|" );
                    if (record_gc_movimento_nf.frete_qvol > 0)
                    {
                        TransporteNFe_Volume.Add("quantidade", "|" + LibNumbers.DecimalToJson(record_gc_movimento_nf.frete_qvol) + "|");
                        TransporteNFe_Volume.Add("especie", LibStringFormat.SomenteAlfabetoSefaz(record_gc_movimento_nf.frete_esp));
                        TransporteNFe_Volume.Add("marca", LibStringFormat.SomenteAlfabetoSefaz(record_gc_movimento_nf.frete_marca));
                        TransporteNFe_Volume.Add("numeracao", LibStringFormat.SomenteAlfabetoSefaz(record_gc_movimento_nf.frete_nvol));
                        if (record_gc_movimento_nf.frete_pesol > 0) { TransporteNFe_Volume.Add("pesoLiquido", "|" + LibNumbers.DecimalToJson(record_gc_movimento_nf.frete_pesol) + "|");};
                        if (record_gc_movimento_nf.frete_pesob > 0) { TransporteNFe_Volume.Add("pesoBruto", "|" + LibNumbers.DecimalToJson(record_gc_movimento_nf.frete_pesob) + "|");};
                    }

                    #region Transportadora
                    if (record_gc_movimento_nf.id_transportadora > 0)
                    {
                        g_clientes RecordTransportadora = db.g_clientes.Find(record_gc_movimento_nf.id_transportadora);
                        if (RecordTransportadora.cnpj.EmptyIfNull().ToString().Length > 10)
                        {
                            TransporteNFe_Transportadora.Add("tipoPessoa", "J");
                            TransporteNFe_Transportadora.Add("cpfCnpj", RecordTransportadora.cnpj.EmptyIfNull().ToString());
                        }
                        else
                        {
                            TransporteNFe_Transportadora.Add("tipoPessoa", "F");
                            TransporteNFe_Transportadora.Add("cpfCnpj", RecordTransportadora.cpf.EmptyIfNull().ToString());
                        }

                        if (RecordTransportadora.razao_social.EmptyIfNull().ToString().Length > 0)
                        {
                            TransporteNFe_Transportadora.Add("nome", LibStringFormat.SomenteAlfabetoSefaz(RecordTransportadora.razao_social.EmptyIfNull().ToString()).ToUpperInvariant()); 
                        }
                        else 
                        {
                            TransporteNFe_Transportadora.Add("nome", LibStringFormat.SomenteAlfabetoSefaz(RecordTransportadora.nome.EmptyIfNull().ToString()).ToUpperInvariant()); 
                        }

                        if (RecordTransportadora.inscricao_estadual.EmptyIfNull().ToString().Length > 0)
                        {
                            TransporteNFe_Transportadora.Add("inscricaoEstadual", RecordTransportadora.inscricao_estadual.EmptyIfNull().ToString());
                        }

                        String EnderecoTransportadora = LibStringFormat.SomenteAlfabetoSefaz(RecordTransportadora.endereco_com.EmptyIfNull().ToString().ToUpperInvariant()) + ", " + LibStringFormat.SomenteAlfabetoSefaz(RecordTransportadora.endereco_com_complemento.EmptyIfNull().ToString().ToUpperInvariant());
                        if (RecordTransportadora.endereco_com_complemento.EmptyIfNull().ToString().Trim().Length > 0) { EnderecoTransportadora += " " + LibStringFormat.SomenteAlfabetoSefaz(RecordTransportadora.endereco_com_complemento.EmptyIfNull().ToString().ToUpperInvariant()); };
                        if (EnderecoTransportadora.Length > 60) { EnderecoTransportadora = EnderecoTransportadora.Substring(0, 60).ToUpperInvariant(); };
                        TransporteNFe_Transportadora.Add("enderecoCompleto", EnderecoTransportadora.ToUpperInvariant());
                        TransporteNFe_Transportadora.Add("cidade", LibStringFormat.SomenteAlfabetoSefaz(db.g_cidades.Find(RecordTransportadora.id_cidade_com).nome.EmptyIfNull().ToString()).ToUpperInvariant());
                        TransporteNFe_Transportadora.Add("uf", db.g_uf.Find(RecordTransportadora.id_uf_com).sigla.EmptyIfNull().ToString().ToUpperInvariant());
                    }
                    JObject Object_TransporteNFe = JObject.Parse(JsonConvert.SerializeObject(TransporteNFe, Formatting.Indented));
                    if (TransporteNFe_Frete.Count > 0) { Object_TransporteNFe.Add("frete", JObject.Parse(JsonConvert.SerializeObject(TransporteNFe_Frete, Formatting.Indented))); }
                    if (TransporteNFe_Volume.Count > 0) { Object_TransporteNFe.Add("volume", JObject.Parse(JsonConvert.SerializeObject(TransporteNFe_Volume, Formatting.Indented))); }
                    if (TransporteNFe_Transportadora.Count > 0) { Object_TransporteNFe.Add("transportadora", JObject.Parse(JsonConvert.SerializeObject(TransporteNFe_Transportadora, Formatting.Indented))); }
                    #endregion

                    // Lançamentos Financeiros
                    int QtdParcelasFinanceiras = 0;
                    Decimal ValorTotalLancamentosFinanceiros = 0;
                    Boolean FinanceiroVencimentoAnterior = false;
                    JObject Object_Cobranca = null;
                    Cobranca_Fatura.Add("numero", "Pedido n " + record_gc_movimento_nf.id_movimento.EmptyIfNull().ToString());
                    Cobranca_Fatura.Add("desconto", "|" + LibNumbers.DecimalToJson(0) + "|");
                    Cobranca_Fatura.Add("valorOriginal", "|" + LibNumbers.DecimalToJson(RecordMovimento.valor_total_bruto) + "|");
                    List<gc_financeiro_lancamentos> ListaFinanceiroLancamentos = db.gc_financeiro_lancamentos.Where(i => (i.ativo == true) && (i.id_movimento == record_gc_movimento_nf.id_movimento) && (i.tipo_pag_rec == 2) && (i.is_adiantamento == false) && (i.is_provisao_imposto == false)).OrderBy(i => i.id_lancamento).ToList();
                    foreach (gc_financeiro_lancamentos record_gc_financeiro_lancamentos in ListaFinanceiroLancamentos)
                    {
                        ValorTotalLancamentosFinanceiros += record_gc_financeiro_lancamentos.valor_total;
                        QtdParcelasFinanceiras += 1;
                        Cobranca_Parcelas.Clear();
                        Cobranca_Parcelas.Add("numero", QtdParcelasFinanceiras.ToString("000"));
                        Cobranca_Parcelas.Add("valor", "|" + LibNumbers.DecimalToJson(record_gc_financeiro_lancamentos.valor_total) + "|");
                        Cobranca_Parcelas.Add("vencimento", record_gc_financeiro_lancamentos.data_vencimento.ToString("yyyy-MM-dd") + "T" + "23:59:59" + "Z");
                        ListaCobrancaParcelas.Add(JObject.Parse(JsonConvert.SerializeObject(Cobranca_Parcelas, Formatting.Indented)));
                        if (record_gc_financeiro_lancamentos.data_vencimento < DataAtual  ) { FinanceiroVencimentoAnterior = true; }; 
                    }
                    if (QtdParcelasFinanceiras > 0)
                    {
                        if ((FinanceiroVencimentoAnterior == true) || (ValorTotalLancamentosFinanceiros > record_gc_movimento_nf.valor_total_bruto))
                        {
                            QtdParcelasFinanceiras = 0;
                            Cobranca_Parcelas.Clear();
                            ListaCobrancaParcelas.Clear();
                        }
                        else
                        {
                            Object_Cobranca = JObject.Parse(JsonConvert.SerializeObject(Cobranca, Formatting.Indented));
                            Object_Cobranca.Add("fatura", JObject.Parse(JsonConvert.SerializeObject(Cobranca_Fatura, Formatting.Indented)));
                            Object_Cobranca.Add("parcelas", ListaCobrancaParcelas);
                        }
                    }

                    String TextoInformacoesAdicionais = LibStringFormat.SomenteAlfabetoSefaz(record_gc_movimento_nf.informacoes_adicionais);
                    RecordCliente.endereco_com = LibStringFormat.SomenteAlfabetoSefaz(RecordCliente.endereco_com.Trim());
                    if (RecordCliente.endereco_com.Length > 60) { RecordCliente.endereco_com = RecordCliente.endereco_com.Substring(0, 60); } // Tamanho máximo do campo logradouro

                    if (RecordCliente.id_cliente == 630)
                    {
                        ClienteNfe_Endereco.Add("logradouro", "NW 33RD AVE");
                        ClienteNfe_Endereco.Add("numero", "5250");
                        ClienteNfe_Endereco.Add("complemento", "ZIP CODE 33309");
                        ClienteNfe_Endereco.Add("bairro", "..");
                        ClienteNfe_Endereco.Add("cep", "");
                        ClienteNfe_Endereco.Add("uf", "EX");
                        ClienteNfe_Endereco.Add("cidade", "Fort Lauderdale");
                        ClienteNfe_Endereco.Add("pais", "Estados Unidos");
                    }
                    else
                    {
                        ClienteNfe_Endereco.Add("logradouro", RecordCliente.endereco_com.EmptyIfNull().ToString().Trim());
                        ClienteNfe_Endereco.Add("numero", RecordCliente.endereco_com_numero.EmptyIfNull().ToString().Trim());
                        ClienteNfe_Endereco.Add("complemento", RecordCliente.endereco_com_complemento.EmptyIfNull().ToString().Trim());
                        ClienteNfe_Endereco.Add("bairro", RecordCliente.bairro_com.EmptyIfNull().ToString().Trim());
                        ClienteNfe_Endereco.Add("cep", RecordCliente.cep_com.EmptyIfNull().ToString().Trim());
                        ClienteNfe_Endereco.Add("uf", ClienteUF);
                        ClienteNfe_Endereco.Add("cidade", ClienteCidade);
                        ClienteNfe_Endereco.Add("pais", "Brasil");
                    }
                    JObject Object_ClienteNfe_Endereco = JObject.Parse(JsonConvert.SerializeObject(ClienteNfe_Endereco, Formatting.Indented));

                    ClienteNfe.Add("tipoPessoa", ClienteTipoPessoa);
                    ClienteNfe.Add("nome", RecordCliente.razao_social.EmptyIfNull().ToString());
                    ClienteNfe.Add("email", RecordCliente.email_principal.EmptyIfNull().ToString());
                    ClienteNfe.Add("cpfCnpj", ClienteDocumento);
                    ClienteNfe.Add("indicadorContribuinteICMS", ClienteIndicadorIE);
                    ClienteNfe.Add("telefone", ClienteTelefone);
                    ClienteNfe.Add("inscricaoMunicipal", RecordCliente.inscricao_municipal);
                    ClienteNfe.Add("inscricaoEstadual", RecordCliente.inscricao_estadual);
                    JObject Object_ClienteNfe = JObject.Parse(JsonConvert.SerializeObject(ClienteNfe, Formatting.Indented));
                    Object_ClienteNfe.Add("endereco", Object_ClienteNfe_Endereco);

                    NfeGateway.Add("id", IdentificadorNFE);
                    NfeGateway.Add("ambienteEmissao", AmbienteEmissaoNFE);
                    NfeGateway.Add("forcarEmissaoContingencia", "|true|");
                    NfeGateway.Add("naturezaOperacao", _naturezaOperacao);
                    NfeGateway.Add("tipoOperacao", _tipoOperacao);
                    NfeGateway.Add("finalidade", _FinalidadeNfe);
                    NfeGateway.Add("consumidorFinal", "|true|");
                    NfeGateway.Add("indicadorPresencaConsumidor", "OperacaoPelaInternet");
                    NfeGateway.Add("enviarPorEmail", "|false|");
                    NfeGateway.Add("informacoesAdicionais", TextoInformacoesAdicionais);
                    NfeGateway.Add("informacoesAdicionaisFisco", "Operação sujeita à Lei Complementar nº 224/2025");

                    // Autorizados
                    NfeGateway_Autorizados.Add("cpfCnpj", "45887403000132");
                    JObject Object_NfeGateway_Autorizados = JObject.Parse(JsonConvert.SerializeObject(NfeGateway_Autorizados, Formatting.Indented));

                    // Object_NfeGateway 
                    JObject Object_NfeGateway = JObject.Parse(JsonConvert.SerializeObject(NfeGateway, Formatting.Indented));
                    Object_NfeGateway.Add("cliente", Object_ClienteNfe);
                    Object_NfeGateway.Add("transporte", Object_TransporteNFe);
                    Object_NfeGateway.Add("itens", ListaItensNFe);
                    Object_NfeGateway.Add("autorizados", Object_NfeGateway_Autorizados);
                    if (QtdParcelasFinanceiras > 0) { Object_NfeGateway.Add("cobranca", Object_Cobranca); };
                    if (ListaNfeReferenciada.Count > 0) { Object_NfeGateway.Add("nfeReferenciada", ListaNfeReferenciada); }

                    var strJson = JsonConvert.SerializeObject(Object_NfeGateway);
                    strJson = strJson.Replace("\"|", "").Replace("|\"", "");
                    var strContent = new StringContent(strJson, Encoding.UTF8, "application/json");

                    if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo1 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo1; }
                    else if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo2 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo2; }
                    else if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo3 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo3; }
                    else if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo4 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo4; }
                    else if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo5 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo5; };
                    record_gc_movimento_nf.xml_erp = strJson; // Guardar o Json que foi enviado!
                    record_gc_movimento_nf.qtd_itens = RecordMovimento.qtd_itens;
                    record_gc_movimento_nf.qtd_produtos = RecordMovimento.qtd_produtos;
                    record_gc_movimento_nf.valor_total_produtos = RecordMovimento.valor_total_produtos;
                    record_gc_movimento_nf.valor_total_liquido = RecordMovimento.valor_total_liquido;
                    record_gc_movimento_nf.valor_total_bruto = RecordMovimento.valor_total_bruto;
                    record_gc_movimento_nf.icms_difal_calculado = ValorIcmsDifalContasPagar;
                    record_gc_movimento_nf.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    record_gc_movimento_nf.datahora_cadastro = DataHoraAtual;
                    record_gc_movimento_nf.id_nfe_status = 1;
                    record_gc_movimento_nf.nf_identificador = IdentificadorNFE;
                    record_gc_movimento_nf.nf_id_usuario_geracao = CachePersister.userIdentity.IdUsuario;
                    record_gc_movimento_nf.nf_data_geracao = DataHoraAtual;
                    record_gc_movimento_nf.id_nfe_gateway = 1;

                    if (RecordMovimento.id_filial == 1) { record_gc_movimento_nf.id_nfe_gateway = 1; }
                    else if (RecordMovimento.id_filial == 2) { record_gc_movimento_nf.id_nfe_gateway = 2; }
                    else { record_gc_movimento_nf.id_nfe_gateway = 1; }

                    record_gc_movimento_nf.id_coligada = RecordMovimento.id_coligada;
                    record_gc_movimento_nf.id_filial = RecordMovimento.id_filial;
                    db.gc_movimentos_nf.Add(record_gc_movimento_nf); //
                    db.SaveChanges();

                    // Criar o log da nfe
                    g_nfe_logs record_g_nfe_logs1 = new g_nfe_logs
                    {
                        id_nfe = 0,
                        id_movimento = RecordMovimento.id_movimento,
                        id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                        id_cliente = RecordCliente.id_cliente,
                        envio = true,
                        retorno = false,
                        identificador_nfe = IdentificadorNFE,
                        log = "NF Gerada ERP!",
                        datahora_cadastro = DataHoraAtual,
                        id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                    };
                    db.g_nfe_logs.Add(record_g_nfe_logs1);
                    string URLAuth = EnotasApiBaseUrl + "/v2/empresas/" + Key2 + "/nf-e";           // NFe de Produtos

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                    var request = (HttpWebRequest)WebRequest.Create(URLAuth);
                    request.ContentType = "application/json";
                    request.Headers.Add("Authorization", "Basic " + Key1);
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(strJson);
                    }

                    var response = (HttpWebResponse)request.GetResponse();
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var responseData = streamReader.ReadToEnd();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            // Atualizar dados da Nota Fiscal
                            sucesso = true;
                            record_gc_movimento_nf.id_nfe_status = 1;
                            record_gc_movimento_nf.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento_nf.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento_nf).State = EntityState.Modified;
                            db.SaveChanges();

                            // Atualizar dados do Movimento
                            RecordMovimento.movimento_nf = true;
                            RecordMovimento.id_movimento_status = 2; // Fechado
                            RecordMovimento.frete1_transportadora = record_gc_movimento_nf.id_transportadora;
                            RecordMovimento.id_usuario_faturamento = CachePersister.userIdentity.IdUsuario;
                            RecordMovimento.datahora_faturamento = DataHoraAtual;
                            RecordMovimento.datahora_alteracao = DataHoraAtual;
                            RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordMovimento).State = EntityState.Modified;

                            // Criar o log da nfe
                            g_nfe_logs record_g_nfe_logs2 = new g_nfe_logs
                            {
                                id_nfe = 0,
                                id_movimento = RecordMovimento.id_movimento,
                                id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                                id_cliente = RecordCliente.id_cliente,
                                envio = true,
                                retorno = false,
                                identificador_nfe = IdentificadorNFE,
                                log = "NF Transmitida com sucesso!",
                                datahora_cadastro = DataHoraAtual,
                                id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                            };
                            db.g_nfe_logs.Add(record_g_nfe_logs2);

                            // Criar o log da utilização
                            a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato
                            {
                                id_yesproduto = 2, // Nota Fiscal Eletrônica - Enotas
                                log = "NFe - Saídas - Id: " + record_gc_movimento_nf.id_movimento_nf.EmptyIfNull().ToString(),
                                datahora_execucao = DataHoraAtual,
                                id_usuario_execucao = CachePersister.userIdentity.IdUsuario
                            };
                            ;
                            db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;

                            // Criar o log do Movimento
                            String LogAlteracoes = "NFe emitida | ";
                            if (record_gc_movimento_nf.id_cfop > 0) { LogAlteracoes += "CFOP: " + db.gc_cfop.Find(record_gc_movimento_nf.id_cfop).numero.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento_nf.id_transportadora > 0) { LogAlteracoes += "Transportadora: " + db.g_clientes.Find(record_gc_movimento_nf.id_transportadora).nome.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento_nf.id_cliente_destinatario > 0) { LogAlteracoes += "Destinatário: " + db.g_clientes_destinatarios.Find(record_gc_movimento_nf.id_cliente_destinatario).nome.EmptyIfNull().ToString() + " | "; } else { LogAlteracoes += "Destinatário: O Próprio cliente | "; };
                            if (record_gc_movimento_nf.id_frete_responsavel > 0) { LogAlteracoes += "Frete (Resp.): " + db.gc_frete_responsavel.Find(record_gc_movimento_nf.id_frete_responsavel).descricao.EmptyIfNull().ToString() + " | "; };
                            if (record_gc_movimento_nf.frete_valor > 0) { LogAlteracoes += "Frete (R$): " + record_gc_movimento_nf.frete_valor.ToString("0.00") + " | "; };
                            if (record_gc_movimento_nf.frete_qvol > 0) { LogAlteracoes += "Qtd. Volumes: " + record_gc_movimento_nf.frete_qvol.ToString() + " | "; };
                            if (record_gc_movimento_nf.frete_esp.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Espécie: " + record_gc_movimento_nf.frete_esp.ToString() + " | "; };
                            if (record_gc_movimento_nf.frete_marca.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Marca: " + record_gc_movimento_nf.frete_marca.ToString() + " | "; };
                            if (record_gc_movimento_nf.frete_nvol.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Numeração: " + record_gc_movimento_nf.frete_nvol.ToString() + " | "; };
                            if (record_gc_movimento_nf.frete_pesol > 0) { LogAlteracoes += "Peso Liq.: " + record_gc_movimento_nf.frete_pesol.ToString("0.000") + " | "; };
                            if (record_gc_movimento_nf.frete_pesob > 0) { LogAlteracoes += "Peso Bruto: " + record_gc_movimento_nf.frete_pesob.ToString("0.000") + " | "; };
                            if (record_gc_movimento_nf.frete_dimensao_altura > 0) { LogAlteracoes += "Medida-A: " + record_gc_movimento_nf.frete_dimensao_altura.ToString("0.000") + " | "; };
                            if (record_gc_movimento_nf.frete_dimensao_largura > 0) { LogAlteracoes += "Medida-L: " + record_gc_movimento_nf.frete_dimensao_largura.ToString("0.000") + " | "; };
                            if (record_gc_movimento_nf.frete_dimensao_profundidade > 0) { LogAlteracoes += "Medida-P: " + record_gc_movimento_nf.frete_dimensao_profundidade.ToString("0.000") + " | "; };
                            if (record_gc_movimento_nf.param_reducao_bc == true) { LogAlteracoes += "ICMS Reduzido: SIM | "; } else { LogAlteracoes += "ICMS Reduzido: NÃO | "; };
                            if (record_gc_movimento_nf.nf_chave_referenciada.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "NFe (Referenciada): " + record_gc_movimento_nf.nf_chave_referenciada.ToString() + " | "; };
                            if (record_gc_movimento_nf.informacoes_adicionais.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Informações Adicionais: " + record_gc_movimento_nf.informacoes_adicionais.ToString() + " | "; };

                            db.SaveChanges();

                            // Criar o contas a pagar Difal (Se Aplicável)
                            if (ValorIcmsDifalContasPagar > 0)
                            {
                                gc_financeiro_lancamentos RecordDifal = new gc_financeiro_lancamentos
                                {
                                    id_lancamento_origem = 1, // Difal NFE
                                    id_movimento = RecordMovimento.id_movimento,
                                    id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                                    tipo_pag_rec = 1, // Pagar
                                    id_pag_rec_tipo = 1, // Dinheiro
                                    ativo = false,
                                    fixo = false,
                                    is_difal = true,
                                    gerencial = false,
                                    parcela_atual = 1,
                                    parcela_total = 1,
                                    id_financeiro_status = 3, // Aberto
                                    id_cliente = RecordMovimento.id_cliente,
                                    data_vencimento = DataAtual,
                                    data_vencimento_original = DataAtual,
                                    data_pagamento = DataHoraAtual,
                                    ordem_pagamento = LibDB.GetNextGcLancamentosFinanceiroOrdemPagamento(2, DataHoraAtual, db),  // Selecionar o máximo + 10
                                    valor_pago = 0,
                                    valor_total = ValorIcmsDifalContasPagar
                                };
                                RecordDifal.descricao = "DIFAL " + RecordUfDestinatarioICMS.sigla.EmptyIfNull().ToString() + " - PEDIDO nº " + RecordDifal.id_movimento.EmptyIfNull().ToString();
                                RecordDifal.id_filial = RecordMovimento.id_filial;

                                if (RecordMovimento.id_filial == 1)
                                {
                                    if (RecordMovimento.id_vendedor == 6) { RecordDifal.id_conta_caixa = 4; } // SC BH - VENDAS
                                    else { RecordDifal.id_conta_caixa = 2; }; // GDI BH - VENDAS
                                }
                                else if (RecordMovimento.id_filial == 2)
                                {
                                    if (RecordMovimento.id_vendedor == 6) { RecordDifal.id_conta_caixa = 13; } // SC SP - VENDAS
                                    else { RecordDifal.id_conta_caixa = 11; }; // GDI SP - VENDAS
                                }

                                RecordDifal.datahora_cadastro = DataHoraAtual;
                                RecordDifal.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                                RecordDifal.id_classificacao_financeira = 71; // TRIBUTOS/IMPOSTOS
                                db.gc_financeiro_lancamentos.Add(RecordDifal);
                                LogAlteracoes += "DIFAL " + RecordUfDestinatarioICMS.sigla.EmptyIfNull().ToString() + ": " + ValorIcmsDifalContasPagar.ToString("0.00") + " | ";
                            }

                            db.SaveChanges();
                            if (sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true,"gc_movimentos", RecordMovimento.id_movimento, LogAlteracoes); }; };
                        }
                        else
                        {
                            throw new Exception(responseData);
                        }
                    }
                }
                else
                {
                    throw new Exception(MsgErro);
                }
            }
            catch (Exception ex)
            {
                PersistirFalhaTransmissaoJsonEnotasMovimentoNf(record_gc_movimento_nf, IdentificadorNFE, ex, DataHoraAtual);
                throw;
            }
            return sucesso;
        }
        #endregion

        #region Gerar Nota Fiscal Serviços
        public bool GerarNFServicoByMovimentoNF(gc_movimentos_nf record_gc_movimento_nf)
        {
            bool sucesso = false;
            int QtdErros = 0;
            String ClienteTipoPessoa = String.Empty;
            String ClienteDocumento = String.Empty;
            String ClienteCidade = String.Empty;
            String ClienteUF = String.Empty;
            String ClienteTelefone = String.Empty;
            String IdentificadorNFE = String.Empty;
            String MsgErro = String.Empty;
            String _jsonEnvio = String.Empty;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            DateTime DataAtual = LibDateTime.getDataHoraBrasilia();

            g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(record_gc_movimento_nf.id_filial);
            if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
            if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
            String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
            String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
            bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
            bool ServidorContingencia = RecordNfeGateway.contingencia;

            // Gateway001
            IDictionary<String, String> NfeGateway = new Dictionary<String, String>();
            IDictionary<String, String> ClienteNfe = new Dictionary<String, String>();
            IDictionary<String, String> ClienteNfe_Endereco = new Dictionary<String, String>();
            IDictionary<String, String> ServicoNfe = new Dictionary<String, String>();

            NfeGateway.Clear();
            ClienteNfe.Clear();
            ServicoNfe.Clear();

            try
            {
                var ListaProdutos = new List<Db.g_produtos>();
                ListaProdutos = db.g_produtos.SqlQuery("select p.* from g_produtos p join gc_movimentos_itens i on(p.id_produto = i.id_produto) where i.id_movimento = " + record_gc_movimento_nf.id_movimento.EmptyIfNull().ToString()).ToList();
                List<gc_movimentos_itens> ListaMovimentosItens = db.gc_movimentos_itens.Where(i => i.id_movimento == record_gc_movimento_nf.id_movimento).ToList();
                List<gc_movimentos_nf> ListaMovimentosNF = db.gc_movimentos_nf.Where(i => i.id_movimento == record_gc_movimento_nf.id_movimento).ToList();
                List<g_nfe_logs> ListaEnviosNFE = db.g_nfe_logs.Where(l => (l.id_movimento == record_gc_movimento_nf.id_movimento && l.envio == true)).ToList();
                gc_movimentos RecordMovimento = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento);
                g_clientes RecordClienteDb = db.g_clientes.Find(RecordMovimento.id_cliente);
                g_clientes RecordCliente = LibDB.CloneTObject(RecordClienteDb);
                gc_movimentos_tipos RecordMovimentoTipo = db.gc_movimentos_tipos.Find(RecordMovimento.id_movimento_tipo);

                // Consistência de Moeda
                if (RecordMovimento.id_moeda != 1)
                {
                    QtdErros += 1;
                    MsgErro += "Moeda do pedido diferente de [R$ Real]" + "<br/>";
                }

                // Consistências de Produtos
                if (ListaMovimentosItens.Count > 1)
                {
                    QtdErros += 1;
                    MsgErro += "NF-e de Serviços só poderá conter 1(um) único item!" + "<br/>";
                }
                else
                {
                    foreach (var RecordProduto in ListaProdutos)
                    {
                        if (RecordProduto.is_servico == false)
                        {
                            QtdErros += 1;
                            MsgErro += "Item [" + RecordProduto.descricao.EmptyIfNull().ToString() + "] Não é do tipo Serviço!" + "<br/>";
                        }
                    }
                }

                if (QtdErros == 0)
                {
                    if (RecordCliente.cnpj.EmptyIfNull().ToString().Length > 10)
                    {
                        ClienteTipoPessoa = "J";
                        ClienteDocumento = RecordCliente.cnpj.EmptyIfNull().ToString();
                    }
                    else
                    {
                        ClienteTipoPessoa = "F";
                        ClienteDocumento = RecordCliente.cpf.EmptyIfNull().ToString();
                    }

                    if (RecordCliente.telefone_principal.EmptyIfNull().ToString().Length > 0) { ClienteTelefone = RecordCliente.telefone_principal.EmptyIfNull().ToString(); } else { ClienteTelefone = "0000000000"; };
                    ClienteCidade = db.g_cidades.Find(RecordCliente.id_cidade_com).nome.EmptyIfNull().ToString().ToUpperInvariant();
                    ClienteUF = db.g_uf.Find(RecordCliente.id_uf_com).sigla.EmptyIfNull().ToString();
                    if ((RecordCliente.inscricao_municipal != null) && (RecordCliente.inscricao_municipal.ToString().Length <= 0)) { RecordCliente.inscricao_municipal = null; };
                    if ((RecordCliente.inscricao_estadual != null) && (RecordCliente.inscricao_estadual.ToString().Length <= 0)) { RecordCliente.inscricao_estadual = null; };
                    IdentificadorNFE = RecordMovimento.id_movimento.EmptyIfNull().ToString().Trim() + "." + (ListaMovimentosNF.Count + 1).ToString();
                    if (AmbienteEmissaoNFE == "Homologacao") { IdentificadorNFE += ".h"; };


                    // Dados Cliente - Endereço
                    ClienteNfe_Endereco.Add("logradouro", RecordCliente.endereco_com.EmptyIfNull().ToString().Trim());
                    ClienteNfe_Endereco.Add("numero", RecordCliente.endereco_com_numero.EmptyIfNull().ToString().Trim());
                    ClienteNfe_Endereco.Add("complemento", RecordCliente.endereco_com_complemento.EmptyIfNull().ToString().Trim());
                    ClienteNfe_Endereco.Add("bairro", RecordCliente.bairro_com.EmptyIfNull().ToString().Trim());
                    ClienteNfe_Endereco.Add("cep", RecordCliente.cep_com.EmptyIfNull().ToString().Trim());
                    ClienteNfe_Endereco.Add("uf", ClienteUF);
                    ClienteNfe_Endereco.Add("cidade", ClienteCidade);
                    JObject Object_ClienteNfe_Endereco = JObject.Parse(JsonConvert.SerializeObject(ClienteNfe_Endereco, Formatting.Indented));

                    
                    // Dados Cliente
                    ClienteNfe.Add("tipoPessoa", ClienteTipoPessoa);
                    ClienteNfe.Add("nome", RecordCliente.razao_social.EmptyIfNull().ToString());
                    ClienteNfe.Add("email", RecordCliente.email_principal.EmptyIfNull().ToString());
                    ClienteNfe.Add("cpfCnpj", ClienteDocumento);
                    JObject Object_ClienteNfe = JObject.Parse(JsonConvert.SerializeObject(ClienteNfe, Formatting.Indented));
                    Object_ClienteNfe.Add("endereco", Object_ClienteNfe_Endereco);


                    // Dados Serviço
                    foreach (gc_movimentos_itens ItemMovimento in ListaMovimentosItens)
                    {
                        String InformacoesAdicionaisItem = string.Empty;
                        g_produtos RecordProduto = ListaProdutos.Find(p => p.id_produto == ItemMovimento.id_produto);
                        String DescricaoItemNFE = RecordProduto.descricao.EmptyIfNull().ToString().Trim();
                        if (DescricaoItemNFE.Length > 120) { DescricaoItemNFE = DescricaoItemNFE.Substring(0, 120); };
                        if ((ItemMovimento.obs_nf == true) && (ItemMovimento.obs.EmptyIfNull().ToString().Length > 0)) { InformacoesAdicionaisItem += "|Obs:" + ItemMovimento.obs.EmptyIfNull().ToString(); };
                        if (InformacoesAdicionaisItem.EmptyIfNull().ToString().Length > 0)
                        {
                            if ((DescricaoItemNFE.Length + InformacoesAdicionaisItem.Length) > 120) { DescricaoItemNFE = DescricaoItemNFE.Substring(0, (120 - InformacoesAdicionaisItem.Length)); };
                            DescricaoItemNFE += InformacoesAdicionaisItem;
                        }
                        ServicoNfe.Add("descricao", DescricaoItemNFE);
                        ServicoNfe.Add("aliquotaIss", "|5|");
                        ServicoNfe.Add("issRetidoFonte", "|false|");
                        ServicoNfe.Add("codigoServicoMunicipio", "140100188");
                        ServicoNfe.Add("descricaoServicoMunicipio", "SERVIÇOS DE MANUTENÇÃO, LIMPEZA, LUSTRAÇÃO, REVISÃO, CONSERTO, RESTAURAÇÃO, BLINDAGEM, CONSERVAÇÃO DE MOTORES, MAQUINAS, APARELHOS, EQUIPAMENTOS.");
                        ServicoNfe.Add("itemListaServicoLC116", "14.01");
                        ServicoNfe.Add("ufPrestacaoServico", "MG");
                        ServicoNfe.Add("municipioPrestacaoServico", "Belo Horizonte");
                        ServicoNfe.Add("valorCofins", "|0|");
                        ServicoNfe.Add("valorCsll", "|0|");
                        ServicoNfe.Add("valorInss", "|0|");
                        ServicoNfe.Add("valorIr", "|0|");
                        ServicoNfe.Add("valorPis", "|0|");
                    }
                    JObject Object_ServicoNfe = JObject.Parse(JsonConvert.SerializeObject(ServicoNfe, Formatting.Indented));

                    // Dados Principais
                    NfeGateway.Add("idExterno", IdentificadorNFE);
                    NfeGateway.Add("ambienteEmissao", AmbienteEmissaoNFE);
                    //NfeGateway.Add("numeroRps", RecordMovimento.id_movimento.EmptyIfNull().ToString().Trim());
                    // GW3001: série RPS só numérica 1–49999; opcional por filial em g_nfe_gateway.key3
                    //NfeGateway.Add("serieRps", NormalizarSerieRpsEnotas(RecordNfeGateway.key3));
                    NfeGateway.Add("valorTotal", "|" + LibNumbers.DecimalToJson(RecordMovimento.valor_total_bruto) + "|");
                    NfeGateway.Add("descontos", "0");

                    JObject Object_NfeGateway = JObject.Parse(JsonConvert.SerializeObject(NfeGateway, Formatting.Indented));
                    Object_NfeGateway.Add("cliente", Object_ClienteNfe);
                    Object_NfeGateway.Add("servico", Object_ServicoNfe);

                    var strJson = JsonConvert.SerializeObject(Object_NfeGateway);
                    strJson = strJson.Replace("\"|", "").Replace("|\"", "");
                    var strContent = new StringContent(strJson, Encoding.UTF8, "application/json");

                    if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo1 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo1; }
                    else if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo2 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo2; }
                    else if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo3 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo3; }
                    else if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo4 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo4; }
                    else if ((record_gc_movimento_nf.id_cfop == 0) && (record_gc_movimento_nf.id_cfop_grupo5 > 0)) { record_gc_movimento_nf.id_cfop = record_gc_movimento_nf.id_cfop_grupo5; };
                    record_gc_movimento_nf.xml_erp = strJson; // Guardar o Json que foi enviado!
                    record_gc_movimento_nf.qtd_itens = RecordMovimento.qtd_itens;
                    record_gc_movimento_nf.qtd_produtos = RecordMovimento.qtd_produtos;
                    record_gc_movimento_nf.valor_total_produtos = RecordMovimento.valor_total_produtos;
                    record_gc_movimento_nf.valor_total_liquido = RecordMovimento.valor_total_liquido;
                    record_gc_movimento_nf.valor_total_bruto = RecordMovimento.valor_total_bruto;
                    record_gc_movimento_nf.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                    record_gc_movimento_nf.datahora_cadastro = DataHoraAtual;
                    record_gc_movimento_nf.id_nfe_status = 1;
                    record_gc_movimento_nf.nf_identificador = IdentificadorNFE;
                    record_gc_movimento_nf.nf_id_usuario_geracao = CachePersister.userIdentity.IdUsuario;
                    record_gc_movimento_nf.nf_data_geracao = DataHoraAtual;
                    record_gc_movimento_nf.id_nfe_gateway = 1;

                    if (RecordMovimento.id_filial == 1) { record_gc_movimento_nf.id_nfe_gateway = 1; }
                    else if (RecordMovimento.id_filial == 2) { record_gc_movimento_nf.id_nfe_gateway = 2; }
                    else { record_gc_movimento_nf.id_nfe_gateway = 1; }

                    record_gc_movimento_nf.id_coligada = RecordMovimento.id_coligada;
                    record_gc_movimento_nf.id_filial = RecordMovimento.id_filial;
                    db.gc_movimentos_nf.Add(record_gc_movimento_nf);
                    db.SaveChanges();

                    // Criar o log da nfe
                    g_nfe_logs record_g_nfe_logs1 = new g_nfe_logs
                    {
                        id_nfe = 0,
                        id_movimento = RecordMovimento.id_movimento,
                        id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                        id_cliente = RecordCliente.id_cliente,
                        envio = true,
                        retorno = false,
                        identificador_nfe = IdentificadorNFE,
                        log = "NFe Serviços Gerada ERP!",
                        datahora_cadastro = DataHoraAtual,
                        id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                    };
                    db.g_nfe_logs.Add(record_g_nfe_logs1);
                    string URLAuth = EnotasApiBaseUrl + "/v1/empresas/" + Key2 + "/nfes";       // NFe de Serviços - Emissão

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                    var request = (HttpWebRequest)WebRequest.Create(URLAuth);
                    request.ContentType = "application/json";
                    request.Headers.Add("Authorization", "Basic " + Key1);
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(strJson);
                    }

                    var response = (HttpWebResponse)request.GetResponse();
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var responseData = streamReader.ReadToEnd();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            // Atualizar dados da Nota Fiscal
                            sucesso = true;
                            record_gc_movimento_nf.id_nfe_status = 1;
                            record_gc_movimento_nf.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento_nf.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento_nf).State = EntityState.Modified;
                            db.SaveChanges();

                            // Atualizar dados do Movimento
                            RecordMovimento.movimento_nf = true;
                            RecordMovimento.id_movimento_status = 2; // Fechado
                            RecordMovimento.id_usuario_faturamento = CachePersister.userIdentity.IdUsuario;
                            RecordMovimento.datahora_faturamento = DataHoraAtual;
                            RecordMovimento.datahora_alteracao = DataHoraAtual;
                            RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordMovimento).State = EntityState.Modified;

                            // Criar o log da nfe
                            g_nfe_logs record_g_nfe_logs2 = new g_nfe_logs
                            {
                                id_nfe = 0,
                                id_movimento = RecordMovimento.id_movimento,
                                id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                                id_cliente = RecordCliente.id_cliente,
                                envio = true,
                                retorno = false,
                                identificador_nfe = IdentificadorNFE,
                                log = "NFe Serviços - Transmitida com sucesso!",
                                datahora_cadastro = DataHoraAtual,
                                id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                            };
                            db.g_nfe_logs.Add(record_g_nfe_logs2);

                            // Criar o log da utilização
                            a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato
                            {
                                id_yesproduto = 2, // Nota Fiscal Eletrônica - Enotas
                                log = "NFe Serviços - Id: " + record_gc_movimento_nf.id_movimento_nf.EmptyIfNull().ToString(),
                                datahora_execucao = DataHoraAtual,
                                id_usuario_execucao = CachePersister.userIdentity.IdUsuario
                            };
                            ;
                            db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;

                            // Criar o log do Movimento
                            String LogAlteracoes = "NFe Serviços emitida | ";
                            if (record_gc_movimento_nf.informacoes_adicionais.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Informações Adicionais: " + record_gc_movimento_nf.informacoes_adicionais.ToString() + " | "; };

                            db.SaveChanges();

                            if (sucesso == true) { if (LogAlteracoes.EmptyIfNull().ToString().Trim().Length > 0) { LibAudit.SaveAudit(db, true, "gc_movimentos", RecordMovimento.id_movimento, LogAlteracoes); }; };
                        }
                        else
                        {
                            throw new Exception(responseData);
                        }
                    }
                }
                else
                {
                    throw new Exception(MsgErro);
                }
            }
            catch (Exception ex)
            {
                PersistirFalhaTransmissaoJsonEnotasMovimentoNf(record_gc_movimento_nf, IdentificadorNFE, ex, DataHoraAtual);
                throw;
            }
            return sucesso;
        }
        #endregion

        /// <summary>JSON salvo em xml_erp na emissão NFS-e (eNotas) contém raiz "servico" e não contém "itens" (NF-e produto).</summary>
        private static bool IsJsonEnvioNfseServico(string xmlErp)
        {
            if (string.IsNullOrWhiteSpace(xmlErp)) { return false; }
            try
            {
                JObject jo = JObject.Parse(xmlErp);
                return jo["servico"] != null && jo["itens"] == null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>eNotas GW3001: série RPS apenas dígitos, inteiro 1–49999. Usar <paramref name="seriePreferida"/> (ex.: g_nfe_gateway.key3); inválido ou vazio → "1".</summary>
        private static string NormalizarSerieRpsEnotas(string seriePreferida)
        {
            if (string.IsNullOrWhiteSpace(seriePreferida)) { return "1"; }
            string s = seriePreferida.Trim();
            foreach (char ch in s)
            {
                if (!char.IsDigit(ch)) { return "1"; }
            }
            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) { return "1"; }
            if (v < 1 || v > 49999) { return "1"; }
            return v.ToString(CultureInfo.InvariantCulture);
        }

        #region Atualizar Nota Fiscal Enviada - Enotas
        public bool AtualizarStatusNFPbyMovimentoNF(gc_movimentos_nf record_gc_movimento_nf) // Implementado no Gateway 002
        {
            bool sucesso = false;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(record_gc_movimento_nf.id_filial);
            if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
            if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
            String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
            String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
            bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
            bool ServidorContingencia = RecordNfeGateway.contingencia;

            try
            {
                gc_movimentos RecordMovimento = null;

                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                string dadosEnviar = String.Empty;
                // NFS-e: consulta por idExterno em /v1/.../nfes/porIdExterno/... — /v2/.../nf-e/{id} é NF-e produto (GUID) e falha com GEN002 se idExterno não for chave NF-e.
                bool consultaNfseServicoV1 = IsJsonEnvioNfseServico(record_gc_movimento_nf.xml_erp);
                if (consultaNfseServicoV1)
                {
                    dadosEnviar = "/v1/empresas/" + Key2 + "/nfes/porIdExterno/" + Uri.EscapeDataString(record_gc_movimento_nf.nf_identificador.EmptyIfNull().ToString().Trim());
                }
                else
                {
                    dadosEnviar = "/v2/empresas/" + Key2 + "/nf-e/" + record_gc_movimento_nf.nf_identificador.EmptyIfNull().ToString().Trim();
                }
                URLAuth = EnotasApiBaseUrl + dadosEnviar;  // Atualizar Status

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("Authorization", "Basic " + Key1);
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                using (responseReader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseData = responseReader.ReadToEnd();
                }
                webResponse.Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    responseData = responseData.Replace(@"\""", "'");
                    responseData = responseData.Replace("\"numeroRps\":null", "\"numeroRps\":-1");
                    responseData = responseData.Replace("\"numero\":null", "\"numero\":0");
                    responseData = responseData.Replace("\"serie\":null", "\"serie\":0");
                    //DataNFPe dadosNfe = JsonConvert.DeserializeObject<DataNFPe>(responseData);
                    string statusProvedor;
                    string motivoStat;
                    string linkPdfOuDanfe;
                    string linkXml;
                    string chaveAcesso;
                    string sNumero;
                    string sSerie;
                    string sDataCriacao;
                    string sDataEmissao;
                    string sDataAutorizacao;
                    string sDataUltimaAlteracao;

                    if (consultaNfseServicoV1)
                    {
                        DataNFSe dns = JsonConvert.DeserializeObject<DataNFSe>(responseData);
                        statusProvedor = dns.status;
                        motivoStat = dns.motivoStatus.EmptyIfNull().ToString();
                        linkPdfOuDanfe = dns.linkDownloadPDF.EmptyIfNull().ToString();
                        linkXml = dns.linkDownloadXML.EmptyIfNull().ToString();
                        chaveAcesso = dns.chaveAcesso.EmptyIfNull().ToString();
                        if (!string.IsNullOrWhiteSpace(dns.numero)) { sNumero = dns.numero.Trim(); }
                        else if (dns.numeroRps != 0) { sNumero = dns.numeroRps.ToString(CultureInfo.InvariantCulture); }
                        else { sNumero = string.Empty; }
                        sSerie = dns.serieRps.EmptyIfNull().ToString();
                        sDataCriacao = dns.dataCriacao.EmptyIfNull().ToString();
                        sDataEmissao = !string.IsNullOrWhiteSpace(dns.dataEmissao) ? dns.dataEmissao.EmptyIfNull().ToString() : dns.dataCompetenciaRps.EmptyIfNull().ToString();
                        sDataAutorizacao = dns.dataAutorizacao.EmptyIfNull().ToString();
                        sDataUltimaAlteracao = dns.dataUltimaAlteracao.EmptyIfNull().ToString();
                    }
                    else
                    {
                        NFPResponse dadosNfe = JsonConvert.DeserializeObject<NFPResponse>(responseData);
                        statusProvedor = dadosNfe.status;
                        motivoStat = dadosNfe.motivoStatus.EmptyIfNull().ToString();
                        linkPdfOuDanfe = dadosNfe.linkDanfe.EmptyIfNull().ToString();
                        linkXml = dadosNfe.linkDownloadXML.EmptyIfNull().ToString();
                        chaveAcesso = dadosNfe.chaveAcesso.EmptyIfNull().ToString();
                        sNumero = dadosNfe.numero.ToString(CultureInfo.InvariantCulture);
                        sSerie = dadosNfe.serie.EmptyIfNull().ToString();
                        sDataCriacao = dadosNfe.dataCriacao.EmptyIfNull().ToString();
                        sDataEmissao = dadosNfe.dataEmissao.EmptyIfNull().ToString();
                        sDataAutorizacao = dadosNfe.dataAutorizacao.EmptyIfNull().ToString();
                        sDataUltimaAlteracao = dadosNfe.dataUltimaAlteracao.EmptyIfNull().ToString();
                    }

                    if (record_gc_movimento_nf != null)
                    {
                        String statusNota = string.Empty;
                        var allRecordsNfeStatus = db.g_nfe_status.ToList();
                        g_nfe_status record_g_nfe_status = allRecordsNfeStatus.Find(e => e.descricao_provedor == statusProvedor);

                        if (record_g_nfe_status != null)
                        {
                        }
                        else
                        {
                            record_g_nfe_status = new Db.g_nfe_status
                            {
                                id_nfe_status = 13,
                                descricao = statusProvedor.EmptyIfNull().Trim().ToString()
                            };
                        }

                        // Atualização do status da NFE
                        if (record_gc_movimento_nf.id_nfe_status != record_g_nfe_status.id_nfe_status)
                        {
                            RecordMovimento = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento);

                            record_gc_movimento_nf.id_nfe_status = record_g_nfe_status.id_nfe_status;
                            record_gc_movimento_nf.nf_url_pdf = linkPdfOuDanfe;
                            record_gc_movimento_nf.nf_url_xml = linkXml;
                            record_gc_movimento_nf.nf_chave_acesso = chaveAcesso;
                            record_gc_movimento_nf.nf_numero = sNumero;
                            record_gc_movimento_nf.nf_serie = sSerie;
                            DateTime DataCriacaoNFe = new DateTime(); DateTime.TryParse(sDataCriacao, new CultureInfo("pt-BR"), DateTimeStyles.None, out DataCriacaoNFe); if (DataCriacaoNFe > DataHoraAtual) { DataCriacaoNFe = DataHoraAtual; }
                            DateTime DataEmissaoNFe = new DateTime(); DateTime.TryParse(sDataEmissao, new CultureInfo("pt-BR"), DateTimeStyles.None, out DataEmissaoNFe); if (DataEmissaoNFe > DataHoraAtual) { DataEmissaoNFe = DataHoraAtual; }
                            DateTime DataAutorizacaoNFe = new DateTime(); DateTime.TryParse(sDataAutorizacao, new CultureInfo("pt-BR"), DateTimeStyles.None, out DataAutorizacaoNFe); if (DataAutorizacaoNFe > DataHoraAtual) { DataAutorizacaoNFe = DataHoraAtual; }
                            DateTime DataUltimaAlteracaoNFe = new DateTime(); DateTime.TryParse(sDataUltimaAlteracao, new CultureInfo("pt-BR"), DateTimeStyles.None, out DataUltimaAlteracaoNFe); if (DataUltimaAlteracaoNFe > DataHoraAtual) { DataUltimaAlteracaoNFe = DataHoraAtual; }
                            record_gc_movimento_nf.nf_data_criacao = DataCriacaoNFe;
                            record_gc_movimento_nf.nf_data_emissao = DataEmissaoNFe;
                            record_gc_movimento_nf.nf_data_autorizacao = DataAutorizacaoNFe;
                            record_gc_movimento_nf.datahora_alteracao = DataUltimaAlteracaoNFe;
                            record_gc_movimento_nf.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            record_gc_movimento_nf.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento_nf.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento_nf).State = EntityState.Modified;

                            // Criar o log da nfe
                            statusNota = statusProvedor.EmptyIfNull().ToString().Trim() + " " + motivoStat.EmptyIfNull().ToString().Trim();
                            if (statusNota.Length > 250) { statusNota = statusNota.Substring(0, 250); };
                            g_nfe_logs record_g_nfe_logs = new g_nfe_logs
                            {
                                id_nfe = 0,
                                id_movimento = record_gc_movimento_nf.id_movimento,
                                id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                                id_cliente = RecordMovimento.id_cliente,
                                identificador_nfe = record_gc_movimento_nf.nf_identificador,
                                envio = false,
                                retorno = true,
                                log = statusNota,
                                datahora_cadastro = DataHoraAtual,
                                id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                            };
                            db.g_nfe_logs.Add(record_g_nfe_logs);

                            // Atualizar Lançamentos Financeiros - Difal (se houver) e Contas a Receber
                            if (record_g_nfe_status.nf_autorizada == true)
                            {
                                gc_financeiro_lancamentos ContasPagarDifal = db.gc_financeiro_lancamentos.Where(l => l.ativo == false && l.id_movimento_nf == record_gc_movimento_nf.id_movimento_nf && l.is_difal == true && l.tipo_pag_rec == 1 && l.id_financeiro_status == 3).FirstOrDefault();
                                if (ContasPagarDifal != null)
                                {
                                    ContasPagarDifal.ativo = true;
                                    ContasPagarDifal.numero_documento = record_gc_movimento_nf.nf_numero;
                                    ContasPagarDifal.datahora_alteracao = DataHoraAtual;
                                    ContasPagarDifal.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(ContasPagarDifal).State = EntityState.Modified;
                                }
                                List<Db.gc_financeiro_lancamentos> ListaLancamentosFinanceiros = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.id_movimento == record_gc_movimento_nf.id_movimento && l.is_difal == false && l.tipo_pag_rec == 2).ToList();
                                foreach (gc_financeiro_lancamentos LancamentoFinanceiro in ListaLancamentosFinanceiros)
                                {
                                    if (LancamentoFinanceiro.id_movimento_nf == 0)
                                    {
                                        LancamentoFinanceiro.id_movimento_nf = record_gc_movimento_nf.id_movimento_nf;
                                        LancamentoFinanceiro.numero_documento = record_gc_movimento_nf.nf_numero;
                                        LancamentoFinanceiro.datahora_alteracao = DataHoraAtual;
                                        LancamentoFinanceiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                        db.Entry(LancamentoFinanceiro).State = EntityState.Modified;
                                    }
                                }

                                // Atualizar status do movimento
                                if (RecordMovimento != null)
                                {
                                    RecordMovimento.movimento_nf_autorizada = true;
                                    if (RecordMovimento.id_movimento_posicao < 4) { RecordMovimento.id_movimento_posicao = 4; } // NF
                                    RecordMovimento.datahora_alteracao = DataHoraAtual;
                                    RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(RecordMovimento).State = EntityState.Modified;

                                    // Criar o log do Movimento
                                    LibAudit.SaveAudit(db, true,"gc_movimentos", RecordMovimento.id_movimento, "Autorização NFe " + record_gc_movimento_nf.nf_serie.EmptyIfNull().ToString() + "/" + record_gc_movimento_nf.nf_numero.EmptyIfNull().ToString());
                                }
                            }
                            else if (record_g_nfe_status.nf_cancelada == true)
                            {
                                String LogAdicional = String.Empty;
                                
                                gc_financeiro_lancamentos ContasPagarDifal = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.id_movimento_nf == record_gc_movimento_nf.id_movimento_nf && l.is_difal == true && l.tipo_pag_rec == 1 && l.id_financeiro_status == 3).FirstOrDefault();
                                if (ContasPagarDifal != null)
                                {
                                    LogAdicional += "Lançamento Difal Id. " + ContasPagarDifal.id_lancamento + ", R$ " + record_gc_movimento_nf.frete_valor.ToString("0.00") + " Cancelado | ";
                                    ContasPagarDifal.ativo = false;
                                    ContasPagarDifal.descricao = ContasPagarDifal.descricao.Replace("(NF Cancelada)", "") + "(NF Cancelada)";
                                    ContasPagarDifal.datahora_alteracao = DataHoraAtual;
                                    ContasPagarDifal.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(ContasPagarDifal).State = EntityState.Modified;
                                }
                                // Títulos Financeiros Associados a NF
                                int QtdLancamentosFinanceirosAssociados = 0;
                                List<Db.gc_financeiro_lancamentos> ListaLancamentosFinanceiros = db.gc_financeiro_lancamentos.Where(l => l.ativo == true && l.id_movimento_nf == record_gc_movimento_nf.id_movimento_nf && l.is_difal == false && l.tipo_pag_rec == 2).ToList();
                                foreach (gc_financeiro_lancamentos LancamentoFinanceiro in ListaLancamentosFinanceiros)
                                {
                                    if (LancamentoFinanceiro.numero_documento.EmptyIfNull().ToString().Length > 0)
                                    {
                                        LogAdicional += "Lançamento Financeiro Id. " + LancamentoFinanceiro.id_lancamento + ", R$ " + LancamentoFinanceiro.valor_total.ToString("0.00") + " removido vínculo com a NFe  | ";
                                        LancamentoFinanceiro.id_movimento_nf = 0;
                                        LancamentoFinanceiro.numero_documento = "";
                                        LancamentoFinanceiro.datahora_alteracao = DataHoraAtual;
                                        LancamentoFinanceiro.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                        db.Entry(LancamentoFinanceiro).State = EntityState.Modified;
                                        QtdLancamentosFinanceirosAssociados += 1;
                                    }
                                }
                                if (QtdLancamentosFinanceirosAssociados == 0) { LogAdicional += "Não há lançamentos financeiro associados à Nota Fiscal Cancelada | "; }

                                // Verificar se tem mais notas fiscais autorizadas nesse movimento
                                if (RecordMovimento != null)
                                {
                                    int qtdNotasAutorizadas = LibDB.dbQueryCount("select count(*) from gc_movimentos_nf nf left join g_nfe_status status on (status.id_nfe_status = nf.id_nfe_status) where nf.id_movimento = " + RecordMovimento.id_movimento.ToString() + " and status.nf_ativa = 1 and id_movimento_nf != " + record_gc_movimento_nf.id_movimento_nf.ToString(), db);
                                    if (qtdNotasAutorizadas == 0) // Não existem notas autorizadas para esse movimento
                                    {
                                        RecordMovimento.movimento_nf = false;
                                        RecordMovimento.movimento_nf_autorizada = false;
                                        if (RecordMovimento.id_movimento_posicao == 4) { RecordMovimento.id_movimento_posicao = 3; };
                                        LogAdicional += "Não há Notas Fiscais Autorizadas nesse pedido | ";
                                    }
                                    else
                                    {
                                        LogAdicional += "Foram localizadas ("+ qtdNotasAutorizadas.ToString() + ") Notas Fiscais Autorizadas para pedido | ";
                                    }
                                    RecordMovimento.datahora_alteracao = DataHoraAtual;
                                    RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                                    db.Entry(RecordMovimento).State = EntityState.Modified;
                                }


                                // Criar o log do Movimento
                                LogAdicional = "Cancelamento NFe " + record_gc_movimento_nf.nf_serie.EmptyIfNull().ToString() + "/" + record_gc_movimento_nf.nf_numero.EmptyIfNull().ToString() + "  | " + LogAdicional;
                                LibAudit.SaveAudit(db, true,"gc_movimentos", RecordMovimento.id_movimento, LogAdicional);
                            }
                            db.SaveChanges();

                            RecordMovimento = AtualizarQtdNotasFiscais(RecordMovimento);
                            RecordMovimento.datahora_alteracao = DataHoraAtual;
                            RecordMovimento.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(RecordMovimento).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
                else
                {
                    String temp = responseData;
                }
            }
            catch (WebException ex)
            {
                string MsgWebException = LibExceptions.getWebException(ex);
                if (string.IsNullOrWhiteSpace(MsgWebException)) { MsgWebException = ex.Message.EmptyIfNull().ToString(); }

                MsgWebException = "Erro [ " + MsgWebException + "]";
                if (MsgWebException.Length > 250) { MsgWebException = MsgWebException.Substring(0, 250); };

                // Criar o log da nfe
                g_nfe_logs record_g_nfe_logs = new g_nfe_logs
                {
                    id_nfe = 0,
                    id_movimento = record_gc_movimento_nf.id_movimento,
                    id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                    id_cliente = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento).id_cliente,
                    identificador_nfe = record_gc_movimento_nf.nf_identificador,
                    envio = false,
                    retorno = true,
                    log = MsgWebException,
                    datahora_cadastro = DataHoraAtual,
                    id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                };
                db.g_nfe_logs.Add(record_g_nfe_logs);
                db.SaveChanges();

                throw new Exception(MsgWebException);
            }
            catch (Exception ex)
            {
                String msgErro = "Erro [ " + ex.Message.EmptyIfNull().ToString().Trim() + "]";
                if (msgErro.Length > 250) { msgErro = msgErro.Substring(0, 250); };

                // Criar o log da nfe
                g_nfe_logs record_g_nfe_logs = new g_nfe_logs
                {
                    id_nfe = 0,
                    id_movimento = record_gc_movimento_nf.id_movimento,
                    id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                    id_cliente = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento).id_cliente,
                    identificador_nfe = record_gc_movimento_nf.nf_identificador,
                    envio = false,
                    retorno = true,
                    log = msgErro,
                    datahora_cadastro = DataHoraAtual,
                    id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                };
                db.g_nfe_logs.Add(record_g_nfe_logs);
                db.SaveChanges();
                throw new Exception(msgErro);
            }
            return sucesso;
        }
        #endregion

        #region Cancelar Nota Fiscal
        public bool CancelarNFPbyMovimentoNF(gc_movimentos_nf record_gc_movimento_nf) // Implementado no Gateway 002
        {
            bool sucesso = false;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(record_gc_movimento_nf.id_filial);
            if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
            if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
            String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
            String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
            String UrlEnotas = String.Empty; 
            bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
            bool ServidorContingencia = RecordNfeGateway.contingencia;

            try
            {
                System.Net.ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                bool consultaNfseServicoV1 = IsJsonEnvioNfseServico(record_gc_movimento_nf.xml_erp);
                if (consultaNfseServicoV1)
                {
                    UrlEnotas = EnotasApiBaseUrl + "/v1/empresas/" + Key2 + "/nfes/" + Uri.EscapeDataString(record_gc_movimento_nf.nf_identificador.EmptyIfNull().ToString().Trim());
                }
                else
                {
                    UrlEnotas = EnotasApiBaseUrl + "/v2/empresas/" + Key2 + "/nf-e/" + Uri.EscapeDataString(record_gc_movimento_nf.nf_identificador.EmptyIfNull().ToString().Trim());
                }

                var options = new RestClientOptions(UrlEnotas);
                var client = new RestClient(options);
                var request = new RestRequest("");
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", "Basic " + Key1);
                var response = client.Delete(request);

                if (response.ResponseStatus == ResponseStatus.Completed && (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent))
                {
                    if (record_gc_movimento_nf != null)
                    {
                        // Atualização do status da NFE
                        if (record_gc_movimento_nf.id_nfe_status != 12) // Cancelamento Solicitado
                        {
                            record_gc_movimento_nf.id_nfe_status = 12;
                            record_gc_movimento_nf.nf_data_cancelamento = DataHoraAtual;
                            record_gc_movimento_nf.datahora_alteracao = DataHoraAtual;
                            record_gc_movimento_nf.id_usuario_alteracao = CachePersister.userIdentity.IdUsuario;
                            db.Entry(record_gc_movimento_nf).State = EntityState.Modified;

                            // Criar o log da nfe
                            g_nfe_logs record_g_nfe_logs = new g_nfe_logs
                            {
                                id_nfe = 0,
                                id_movimento = record_gc_movimento_nf.id_movimento,
                                id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                                id_cliente = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento).id_cliente,
                                identificador_nfe = record_gc_movimento_nf.nf_identificador,
                                envio = false,
                                retorno = true,
                                log = "Cancelamento da NF " + record_gc_movimento_nf.nf_serie.ToString() + "/" + record_gc_movimento_nf.nf_numero.ToString() + "solicitado",
                                datahora_cadastro = DataHoraAtual,
                                id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                            };
                            db.g_nfe_logs.Add(record_g_nfe_logs);

                            // Criar o log do Movimento
                            gc_movimentos RecordMovimento = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento);
                            if (RecordMovimento != null) 
                            {
                                String LogAuditoria = string.Empty;
                                LogAuditoria += "Solicitação de Cancelamento NFe " + record_gc_movimento_nf.nf_serie.EmptyIfNull().ToString() + "/" + record_gc_movimento_nf.nf_numero.EmptyIfNull().ToString();
                                LogAuditoria += " | Justificativa: " + record_gc_movimento_nf.justificativa_cancelamento.EmptyIfNull().ToString();
                                LogAuditoria += " | Protocolo Siare: " + record_gc_movimento_nf.protocolo_cancelamento_siare.EmptyIfNull().ToString();
                                LibAudit.SaveAudit(db, true,"gc_movimentos", RecordMovimento.id_movimento, LogAuditoria); 
                            };

                            db.SaveChanges();
                            sucesso = true;
                        }
                    }
                }
                else
                {
                    String msgErro = string.Empty;
                    msgErro += "ERRO: ";
                    msgErro += "StatusCode: " + response.StatusCode.EmptyIfNull().ToString() + " | ";
                    msgErro += "StatusDescription: " + response.StatusDescription.EmptyIfNull().ToString() + " | ";
                    msgErro += "ErrorMessage: " + response.ErrorMessage.EmptyIfNull().ToString() + " | ";
                    if (msgErro.Length > 250) { msgErro = msgErro.Substring(0, 250); };
                    // Criar o log da nfe
                    g_nfe_logs record_g_nfe_logs = new g_nfe_logs
                    {
                        id_nfe = 0,
                        id_movimento = record_gc_movimento_nf.id_movimento,
                        id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                        id_cliente = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento).id_cliente,
                        identificador_nfe = record_gc_movimento_nf.nf_identificador,
                        envio = false,
                        retorno = true,
                        log = msgErro,
                        datahora_cadastro = DataHoraAtual,
                        id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                    };
                    db.g_nfe_logs.Add(record_g_nfe_logs);
                    db.SaveChanges();
                    throw new Exception(msgErro);
                }
            }
            catch (Exception ex)
            {
                String msgErro = "Erro [ " + ex.Message.EmptyIfNull().ToString().Trim() + "]";
                if (msgErro.Length > 250) { msgErro = msgErro.Substring(0, 250); };
                // Criar o log da nfe
                g_nfe_logs record_g_nfe_logs = new g_nfe_logs
                {
                    id_nfe = 0,
                    id_movimento = record_gc_movimento_nf.id_movimento,
                    id_movimento_nf = record_gc_movimento_nf.id_movimento_nf,
                    id_cliente = db.gc_movimentos.Find(record_gc_movimento_nf.id_movimento).id_cliente,
                    identificador_nfe = record_gc_movimento_nf.nf_identificador,
                    envio = false,
                    retorno = true,
                    log = msgErro,
                    datahora_cadastro = DataHoraAtual,
                    id_usuario_cadastro = CachePersister.userIdentity.IdUsuario
                };
                db.g_nfe_logs.Add(record_g_nfe_logs);
                db.SaveChanges();
                throw new Exception(msgErro);
            }
            return sucesso;
        }
        #endregion

        #region Auxiliares por id (movimento NF) e g_nfe avulsa (e-Notas gateway)

        public bool AtualizarStatusNFPbyMovimentoNFId(int id_movimento_nf)
        {
            gc_movimentos_nf rec = db.gc_movimentos_nf.Find(id_movimento_nf);
            if (rec == null)
            {
                throw new Exception("movimento_nf não encontrado (id " + id_movimento_nf + ").");
            }
            return AtualizarStatusNFPbyMovimentoNF(rec);
        }

        public bool CancelarNFPbyMovimentoNFId(int id_movimento_nf, string justificativa)
        {
            gc_movimentos_nf rec = db.gc_movimentos_nf.Find(id_movimento_nf);
            if (rec == null)
            {
                throw new Exception("movimento_nf não encontrado (id " + id_movimento_nf + ").");
            }
            if (!string.IsNullOrWhiteSpace(justificativa))
            {
                rec.justificativa_cancelamento = justificativa.Trim();
                db.Entry(rec).State = EntityState.Modified;
                db.SaveChanges();
            }
            return CancelarNFPbyMovimentoNF(rec);
        }

        public bool GerarNFServicoByMovimentoNFId(int id_movimento_nf)
        {
            gc_movimentos_nf rec = db.gc_movimentos_nf.Find(id_movimento_nf);
            if (rec == null)
            {
                throw new Exception("movimento_nf não encontrado (id " + id_movimento_nf + ").");
            }
            return GerarNFServicoByMovimentoNF(rec);
        }

        /// <summary>Consulta status na API e atualiza registro em g_nfe (NF serviço / sem gc_movimentos_nf).</summary>
        public bool AtualizarStatusG_nfePorId(int id_nfe)
        {
            bool sucesso = false;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            String UrlEnotas = string.Empty;
            return sucesso;
        }

        /// <summary>Cancelamento via API e-Notas para registro em g_nfe (identificador nfe_key).</summary>
        public bool CancelarG_nfePorId(int id_nfe, string motivo)
        {
            bool sucesso = false;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();
            return sucesso;
        }

        #endregion

        #region Gerar Carta Correção - Enotas
        public bool GerarCartaCorrecaoNFPbyMovimentoNF(g_nfe_carta_correcao record_g_nfe_carta_correcao) // Implementado no Gateway 002
        {
            bool sucesso = false;
            DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

            gc_movimentos_nf RecordMovimentoNf = db.gc_movimentos_nf.Find(record_g_nfe_carta_correcao.id_movimento_nf); 
            g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(RecordMovimentoNf.id_filial);
            if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
            if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
            String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
            String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
            bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
            bool ServidorContingencia = RecordNfeGateway.contingencia;

            try
            {
                List<g_nfe_carta_correcao> ListaEnviosCartasCorrecaoTotal = db.g_nfe_carta_correcao.ToList();
                List<g_nfe_carta_correcao> ListaEnviosCartasCorrecaoNFAtual = db.g_nfe_carta_correcao.Where(l => (l.id_movimento_nf == record_g_nfe_carta_correcao.id_movimento_nf)).ToList();
                gc_movimentos_nf record_gc_movimentos_nf = db.gc_movimentos_nf.Find(record_g_nfe_carta_correcao.id_movimento_nf);
                CartaCorrecao record_CartaCorrecao = new CartaCorrecao
                {
                    ambienteEmissao = AmbienteEmissaoNFE
                };
                String IdCartaCorrecao = "cc." + (ListaEnviosCartasCorrecaoTotal.Count() + 1).ToString() + "." + DataHoraAtual.ToString("ff");
                if (record_CartaCorrecao.ambienteEmissao == "Homologacao") { IdCartaCorrecao += ".h"; };
                record_CartaCorrecao.id = IdCartaCorrecao;
                record_CartaCorrecao.numero = (ListaEnviosCartasCorrecaoNFAtual.Count() + 1);
                record_CartaCorrecao.correcao = record_g_nfe_carta_correcao.correcao.EmptyIfNull().ToString();
                record_CartaCorrecao.nfe.id = record_gc_movimentos_nf.nf_identificador.EmptyIfNull().ToString();

                // Teste objeto programação
                var strJson = JsonConvert.SerializeObject(record_CartaCorrecao);
                var strContent = new StringContent(strJson, Encoding.UTF8, "application/json");
                string URLAuth = EnotasApiBaseUrl + "/v2/empresas/" + Key2 + "/nf-e/cartaCorrecao/";

                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                var request = (HttpWebRequest)WebRequest.Create(URLAuth);
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", "Basic " + Key1);
                request.Method = "POST";
                request.ContentType = "application/json";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(strJson);
                }

                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var responseData = streamReader.ReadToEnd();
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        sucesso = true;
                        record_g_nfe_carta_correcao.identificador = record_CartaCorrecao.id;
                        record_g_nfe_carta_correcao.id_usuario_cadastro = CachePersister.userIdentity.IdUsuario;
                        record_g_nfe_carta_correcao.datahora_cadastro = DataHoraAtual;
                        record_g_nfe_carta_correcao.status = "Enviada";
                        db.g_nfe_carta_correcao.Add(record_g_nfe_carta_correcao);

                        // Criar o log do Movimento
                        gc_movimentos_nf RecordMovimentoNF = db.gc_movimentos_nf.Find(record_g_nfe_carta_correcao.id_movimento_nf);
                        if (RecordMovimentoNF != null)
                        {
                            gc_movimentos RecordMovimento = db.gc_movimentos.Find(RecordMovimentoNF.id_movimento);
                            if (RecordMovimento != null)
                            {
                                String LogAlteracoes = "Geração Carta Correção | ";
                                if (record_CartaCorrecao.id.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Id: " + record_CartaCorrecao.id.ToString() + " | "; };
                                LogAlteracoes += "NFe nº: " + record_gc_movimentos_nf.nf_serie.EmptyIfNull().ToString() + "/" + record_gc_movimentos_nf.nf_numero.ToString() + " | ";
                                if (record_CartaCorrecao.numero > 0) { LogAlteracoes += "CC nº: " + record_CartaCorrecao.numero.EmptyIfNull().ToString() + " | "; };
                                if (record_CartaCorrecao.correcao.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Correção: " + record_CartaCorrecao.correcao.ToString() + " | "; };
                                LibAudit.SaveAudit(db, true,"gc_movimentos", RecordMovimento.id_movimento, LogAlteracoes);
                            }
                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        throw new Exception(responseData);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return sucesso;
        }
        #endregion

        #region Atualizar Status Carta Correção - ENotas
        public bool AtualizarStatusCartaCorrecao(g_nfe_carta_correcao record_g_nfe_carta_correcao)
        {
            gc_movimentos_nf RecordMovimentoNf = db.gc_movimentos_nf.Find(record_g_nfe_carta_correcao.id_movimento_nf);
            g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(RecordMovimentoNf.id_filial);
            if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
            if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
            String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
            String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
            bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
            bool ServidorContingencia = RecordNfeGateway.contingencia;

            bool sucesso = false;
            try
            {
                gc_movimentos RecordMovimento = null;
                DateTime DataHoraAtual = LibDateTime.getDataHoraBrasilia();

                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                string dadosEnviar = String.Empty;
                dadosEnviar = "/v2/empresas/" + Key2 + "/nf-e/cartaCorrecao/" + record_g_nfe_carta_correcao.identificador.EmptyIfNull().ToString().Trim();
                URLAuth = EnotasApiBaseUrl + dadosEnviar;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("Authorization", "Basic " + Key1);
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                using (responseReader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseData = responseReader.ReadToEnd();
                }
                webResponse.Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    responseData = responseData.Replace(@"\""", "'");
                    responseData = responseData.Replace("\"numeroRps\":null", "\"numeroRps\":-1");
                    CartaCorrecaoRetorno dadosCartaCorrecao = JsonConvert.DeserializeObject<CartaCorrecaoRetorno>(responseData);

                    if (dadosCartaCorrecao != null)
                    {
                        record_g_nfe_carta_correcao.status = dadosCartaCorrecao.status.EmptyIfNull().ToString();
                        record_g_nfe_carta_correcao.motivo_status = dadosCartaCorrecao.motivoStatus.EmptyIfNull().ToString();
                        record_g_nfe_carta_correcao.protocolo_autorizacao = dadosCartaCorrecao.protocoloAutorizacao.EmptyIfNull().ToString();
                        record_g_nfe_carta_correcao.condicoes_uso = dadosCartaCorrecao.condicoesUso.EmptyIfNull().ToString();
                        db.Entry(record_g_nfe_carta_correcao).State = EntityState.Modified;

                        // Criar o log do Movimento
                        gc_movimentos_nf RecordMovimentoNF = db.gc_movimentos_nf.Find(record_g_nfe_carta_correcao.id_movimento_nf);
                        if (RecordMovimentoNF != null)
                        {
                            RecordMovimento = db.gc_movimentos.Find(RecordMovimentoNF.id_movimento);
                            if (RecordMovimento != null)
                            {
                                // Criar o log do Movimento
                                String LogAlteracoes = "Autorização Carta Correção | ";
                                LogAlteracoes += "Id: " + record_g_nfe_carta_correcao.id_nfe_carta_correcao.EmptyIfNull().ToString() + " | ";
                                LogAlteracoes += "NFe nº: " + RecordMovimentoNF.nf_serie.EmptyIfNull().ToString() + "/" + RecordMovimentoNF.nf_numero.ToString() + " | ";
                                LogAlteracoes += "CC nº: " + record_g_nfe_carta_correcao.identificador.EmptyIfNull().EmptyIfNull().ToString() + " | ";
                                if (record_g_nfe_carta_correcao.status.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Status: " + record_g_nfe_carta_correcao.status.ToString() + " | "; };
                                if (record_g_nfe_carta_correcao.protocolo_autorizacao.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Protocolo: " + record_g_nfe_carta_correcao.protocolo_autorizacao.ToString() + " | "; };
                                if (record_g_nfe_carta_correcao.condicoes_uso.EmptyIfNull().ToString().Trim().Length > 0) { LogAlteracoes += "Condições Uso: " + record_g_nfe_carta_correcao.condicoes_uso.ToString() + " | "; };
                                LibAudit.SaveAudit(db, true,"gc_movimentos", RecordMovimento.id_movimento, LogAlteracoes);
                            }
                        }
                        db.SaveChanges();

                        if (RecordMovimento != null)
                        {
                            RecordMovimento = AtualizarQtdCartasCorrecao(RecordMovimento);
                            db.Entry(record_g_nfe_carta_correcao).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }
                }
                else
                {
                    String temp = responseData;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return sucesso;
        }
        #endregion

        #region Get Carta Correção
        public String GetXMLCartaCorrecao(g_nfe_carta_correcao record_g_nfe_carta_correcao)
        {
            string XMLTemp = string.Empty;

            gc_movimentos_nf RecordMovimentoNf = db.gc_movimentos_nf.Find(record_g_nfe_carta_correcao.id_movimento_nf);
            g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(RecordMovimentoNf.id_filial);
            if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
            if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
            String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
            String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
            bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
            bool ServidorContingencia = RecordNfeGateway.contingencia;

            try
            {
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                string dadosEnviar = String.Empty;
                dadosEnviar = "/v2/empresas/" + Key2 + "/nf-e/cartaCorrecao/" + record_g_nfe_carta_correcao.identificador.EmptyIfNull().ToString().Trim();
                URLAuth = EnotasApiBaseUrl + dadosEnviar;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("Authorization", "Basic " + Key1);
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                using (responseReader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseData = responseReader.ReadToEnd();
                }
                webResponse.Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    responseData = responseData.Replace(@"\""", "'");
                    XMLTemp += responseData;
                }
                else
                {
                    XMLTemp = string.Empty;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return XMLTemp;
        }
        #endregion

        #region Get XML Envio Sefaz
        public String GetXMLEnvioSefaz(gc_movimentos_nf record_gc_movimentos_nf)
        {
            SucessoRobo = false;
            RespostaRoboEnotas = string.Empty;

            g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(record_gc_movimentos_nf.id_filial);
            if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
            if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
            String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
            String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
            bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
            bool ServidorContingencia = RecordNfeGateway.contingencia;

            try
            {
                int TempoEsperaMaximo = 20;
                int TempoEsperaAtual = 0;
                Thread th = new Thread(() => RequestXMLEnvioSefaz(record_gc_movimentos_nf));
                th.Start();
                while (th.IsAlive)
                {
                    Thread.Sleep(1000);
                    TempoEsperaAtual += 1;
                    if (TempoEsperaAtual >= TempoEsperaMaximo)
                    {
                        th.Abort();
                        break;
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                if (SucessoRobo == true)
                {
                    a_yesprodutos_extrato record_a_yesprodutos_extrato = new Db.a_yesprodutos_extrato
                    {
                        id_yesproduto = 2, // Nfe-ENotas
                        log = "Get XML Envio Sefaz NF " + record_gc_movimentos_nf.nf_numero.EmptyIfNull().ToString(), // SintegraWS
                        datahora_execucao = LibDateTime.getDataHoraBrasilia(),
                        id_usuario_execucao = CachePersister.userIdentity.IdUsuario
                    };
                    ;
                    db.Entry(record_a_yesprodutos_extrato).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            return RespostaRoboEnotas;
        }

        public void RequestXMLEnvioSefaz(gc_movimentos_nf record_gc_movimentos_nf)
        {
            try
            {
                g_nfe_gateway RecordNfeGateway = db.g_nfe_gateway.Find(record_gc_movimentos_nf.id_filial);
                if (RecordNfeGateway != null) { } else { RecordNfeGateway = db.g_nfe_gateway.Find(1); }
                if (RecordNfeGateway.producao == true) { AmbienteEmissaoNFE = "Producao"; } else { AmbienteEmissaoNFE = "Homologacao"; };
                String Key1 = RecordNfeGateway.key1.EmptyIfNull().ToString(); // Api Key
                String Key2 = RecordNfeGateway.key2.EmptyIfNull().ToString(); // Empresa ID
                bool ParametroCalcularDifal = RecordNfeGateway.calcular_difal;
                bool ServidorContingencia = RecordNfeGateway.contingencia;

                SucessoRobo = false;
                string URLAuth = "";
                HttpWebRequest webRequest;
                HttpWebResponse webResponse;
                StreamReader responseReader;
                string responseData;
                string dadosEnviar = String.Empty;
                dadosEnviar = "/v1/empresas/" + Key2 + "/nfes/porIdExterno/" + record_gc_movimentos_nf.nf_identificador.EmptyIfNull().Trim() + "/xml";
                URLAuth = EnotasApiBaseUrl + dadosEnviar;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Headers.Add("Authorization", "Basic " + Key1);
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                using (responseReader = new StreamReader(webResponse.GetResponseStream()))
                {
                    responseData = responseReader.ReadToEnd();
                }
                webResponse.Close();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    responseData = responseData.Replace(@"\""", "'");
                    RespostaRoboEnotas += responseData;
                }
                else
                {
                    SucessoRobo = false;
                    RespostaRoboEnotas += "#ERRO# " + responseData;
                }
            }
            catch (Exception ex)
            {
                SucessoRobo = false; 
                throw ex;
            }
        }
        #endregion

        #region Atualizar Quantidade de Notas Fiscais
        public gc_movimentos AtualizarQtdNotasFiscais(gc_movimentos RecordMovimento)
        {
            try
            {
                int qtd_nf_geral = 0;
                int qtd_nf_autorizadas = 0;
                int qtd_nf_canceladas = 0;
                int qtd_nf_ativas = 0;
                Decimal icms_difal_pagar = 0;
                String SentencaSQL = string.Empty;

                SentencaSQL =   " select count(*) as qtd_nf_geral,  " +
                                " sum(case when status.nf_autorizada = 1 then 1 else 0 end) as 'qtd_nf_autorizadas', " +
                                " sum(case when status.nf_cancelada = 1 then 1 else 0 end) as 'qtd_nf_canceladas', " +
                                " sum(case when status.nf_ativa = 1 then 1 else 0 end) as 'qtd_nf_ativas' " +
                                " from gc_movimentos_nf nf " +
                                " left join g_nfe_status status on (nf.id_nfe_status = status.id_nfe_status) " +
                                " where nf.id_movimento = " + RecordMovimento.id_movimento.ToString();
                DataTable TableNotasFiscais = LibDB.GetDataTable(SentencaSQL, db);
                if (TableNotasFiscais.Rows.Count > 0)
                {
                    qtd_nf_geral = int.Parse(TableNotasFiscais.Rows[0]["qtd_nf_geral"].EmptyIfNull().ToString().Trim());
                    qtd_nf_autorizadas = int.Parse(TableNotasFiscais.Rows[0]["qtd_nf_autorizadas"].EmptyIfNull().ToString().Trim());
                    qtd_nf_canceladas = int.Parse(TableNotasFiscais.Rows[0]["qtd_nf_canceladas"].EmptyIfNull().ToString().Trim());
                    qtd_nf_ativas = int.Parse(TableNotasFiscais.Rows[0]["qtd_nf_ativas"].EmptyIfNull().ToString().Trim());
                }


                SentencaSQL =   " select sum(icms_difal_calculado) as 'icms_difal_pagar'  " +
                                " from gc_movimentos_nf nf  " +
                                " join g_nfe_status status on (nf.id_nfe_status = status.id_nfe_status and status.nf_autorizada = 1)  " +
                                " where nf.id_movimento = " + RecordMovimento.id_movimento.ToString();
                DataTable TableDifalPagar = LibDB.GetDataTable(SentencaSQL, db);
                if (TableDifalPagar.Rows.Count > 0)
                {
                    Decimal.TryParse(TableDifalPagar.Rows[0]["icms_difal_pagar"].EmptyIfNull().ToString().Trim(), out icms_difal_pagar);
                }

                RecordMovimento.qtd_nfs_geradas = qtd_nf_geral;
                RecordMovimento.qtd_nfs_autorizadas = qtd_nf_autorizadas;
                RecordMovimento.qtd_nfs_canceladas = qtd_nf_canceladas;
                RecordMovimento.qtd_nfs_ativas = qtd_nf_ativas;
                RecordMovimento.icms_difal_pagar = icms_difal_pagar;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            return RecordMovimento;
        }


        public gc_movimentos AtualizarQtdCartasCorrecao(gc_movimentos RecordMovimento)
        {
            try
            {
                int qtd_cc_geral = 0;
                int qtd_cc_autorizadas = 0;
                String SentencaSQL = string.Empty;

                SentencaSQL = " select count(*) as qtd_cc_geral,    " +
                                " sum(case when cc.status = 'Autorizada' then 1 else 0 end) as 'qtd_cc_autorizadas',  " +
                                " sum(case when cc.status != 'Autorizada' then 1 else 0 end) as 'qtd_cc_negadas' " +
                                " from g_nfe_carta_correcao cc " +
                                " left join gc_movimentos_nf nf on (nf.id_movimento_nf = cc.id_movimento_nf) " +
                                " where nf.id_movimento = " + RecordMovimento.id_movimento.ToString();
                DataTable TableCC = LibDB.GetDataTable(SentencaSQL, db);
                if (TableCC.Rows.Count > 0)
                {
                    qtd_cc_geral = int.Parse(TableCC.Rows[0]["qtd_cc_geral"].EmptyIfNull().ToString().Trim());
                    qtd_cc_autorizadas = int.Parse(TableCC.Rows[0]["qtd_cc_autorizadas"].EmptyIfNull().ToString().Trim());
                }

                RecordMovimento.qtd_cc_geradas = qtd_cc_geral;
                RecordMovimento.qtd_cc_ativas = qtd_cc_geral;
                RecordMovimento.qtd_cc_autorizadas = qtd_cc_autorizadas;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            return RecordMovimento;
        }

        #endregion
    }
}