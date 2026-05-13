using System;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Text;
using GdiPlataform.Lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using GdiPlataform.Db;
using GdiPlataform.Security;
using System.Data.Entity;
using System.Net.Http;

namespace GdiPlataform.Robos.Itau
{
    public class RoboItauBolecode
    {
        private GdiPlataformEntities db;
        private String CertificadoPath;
        private String CertificadoSenha;
        private String ClientID;
        private String ClientSecret;
        public RoboItauBolecode(int IdContaCaixa, String FileNameCertificado)
        {
            CertificadoPath = FileNameCertificado;
            CertificadoSenha = string.Empty;
            ClientID = string.Empty;
            ClientSecret = string.Empty;
            if (!CachePersister.dataBase.EmptyIfNull().ToString().Equals(String.Empty)) { db = new GdiPlataformEntities(CachePersister.dataBase); };
            if (IdContaCaixa == 7)       // GDI BH
            {
                CertificadoSenha = @"123456";
                ClientID = "349579fc-abb0-4ee6-8e15-b84d87fb62b0";
                ClientSecret = "8712be77-33f9-4d34-9f66-304603a24b29";
            }
            else if (IdContaCaixa == 10) // GDI SP
            {
                CertificadoSenha = @"12345678";
                ClientID = "9a8b5d4e-56b9-46b3-a989-351ba0ff0064";
                ClientSecret = "e1035a69-5f01-4ade-99c5-ab69010673d6";
            }
        }

        public string GetTokenApiItau(String CertificadoPath)
        {
            String TokenItau = String.Empty;
            string RetornoItau = String.Empty;

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            var request = (HttpWebRequest)WebRequest.Create("https://sts.itau.com.br/api/oauth/token");
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            var dataBody = "grant_type=" + Uri.EscapeDataString("client_credentials");
            dataBody += "&client_id=" + Uri.EscapeDataString(ClientID);
            dataBody += "&client_secret=" + Uri.EscapeDataString(ClientSecret);
            var body = Encoding.ASCII.GetBytes(dataBody);
            request.ContentLength = body.Length;

            Stream myWriter = null;
            try
            {
                X509Certificate2 certificate = new X509Certificate2();
                certificate.Import(CertificadoPath, CertificadoSenha, X509KeyStorageFlags.DefaultKeySet);
                request.ClientCertificates.Add(certificate);
                myWriter = request.GetRequestStream();
                myWriter.Write(body, 0, body.Length);
                var requestResponse = (HttpWebResponse)request.GetResponse();
                RetornoItau = new StreamReader(requestResponse.GetResponseStream()).ReadToEnd();
                var data = (JObject)JsonConvert.DeserializeObject(RetornoItau);
                try { TokenItau = data["access_token"].Value<string>(); } catch (Exception) { };
                return TokenItau;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao solicitar o token.\r\nDetalhe: " + ex.Message);
            }
            finally
            {
                if (myWriter != null) myWriter.Close();
            }
        }


        public ModelItauBolecode RegistrarBolecode(ModelItauBolecode NewBolecode, String CertificadoPath)
        {
            try
            {
                IDictionary<String, String> Bolecode = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_Beneficiario = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_Beneficiario_TipoPessoa = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_Beneficiario_Endereco = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_DadosBoleto = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_DadosBoleto_Pagador = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_DadosBoleto_Pagador_Pessoa = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_DadosBoleto_Pagador_Endereco = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_DadosBoleto_DadosIndividuais = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_DadosBoleto_Juros = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_DadosBoleto_Multa = new Dictionary<String, String>();
                IDictionary<String, String> Bolecode_QrCode = new Dictionary<String, String>();
                JArray ListaBolecode_DadosBoleto_DadosIndividuais = new JArray();

                // Dados Gerais
                Bolecode.Add("codigo_canal_operacao", NewBolecode.Bolecode_CodigoCanalOperacao);
                Bolecode.Add("codigo_operador", NewBolecode.Bolecode_CodigoOperador);
                Bolecode.Add("etapa_processo_boleto", NewBolecode.Bolecode_EtapaProcessoBoleto); // simulacao ou efetivacao

                // Beneficiário - Dados
                Bolecode_Beneficiario.Add("id_beneficiario", NewBolecode.Bolecode_Beneficiario_IdBeneficiario);
                Bolecode_Beneficiario.Add("nome_cobranca", NewBolecode.Bolecode_Beneficiario_NomeCobranca);

                // Beneficiário - Tipo Pessoa
                Bolecode_Beneficiario_TipoPessoa.Add("codigo_tipo_pessoa", NewBolecode.Bolecode_Beneficiario_TipoPessoa_CodigoTipoPessoa);
                Bolecode_Beneficiario_TipoPessoa.Add("numero_cadastro_nacional_pessoa_juridica", NewBolecode.Bolecode_Beneficiario_TipoPessoa_NumeroCadastroNacionaPessoaJuridica);

                // Beneficiário - Endereço
                Bolecode_Beneficiario_Endereco.Add("nome_logradouro", NewBolecode.Bolecode_Beneficiario_Endereco_NomeLogradouro);
                Bolecode_Beneficiario_Endereco.Add("nome_bairro", NewBolecode.Bolecode_Beneficiario_Endereco_NomeBairro);
                Bolecode_Beneficiario_Endereco.Add("nome_cidade", NewBolecode.Bolecode_Beneficiario_Endereco_NomeCidade);
                Bolecode_Beneficiario_Endereco.Add("sigla_UF", NewBolecode.Bolecode_Beneficiario_Endereco_SiglaUF);
                Bolecode_Beneficiario_Endereco.Add("numero_CEP", NewBolecode.Bolecode_Beneficiario_Endereco_NumeroCep);
                Bolecode_Beneficiario_Endereco.Add("numero", NewBolecode.Bolecode_Beneficiario_Endereco_Numero);
                Bolecode_Beneficiario_Endereco.Add("complemento", NewBolecode.Bolecode_Beneficiario_Endereco_Complemento);

                // Dados do Boleto
                Bolecode_DadosBoleto.Add("descricao_instrumento_cobranca", NewBolecode.Bolecode_DadosBoleto_DescricaoInstrumentoCobranca);
                Bolecode_DadosBoleto.Add("tipo_boleto", NewBolecode.Bolecode_DadosBoleto_TipoBoleto);
                Bolecode_DadosBoleto.Add("codigo_carteira", NewBolecode.Bolecode_DadosBoleto_CodigoCarteira);
                Bolecode_DadosBoleto.Add("valor_total_titulo", NewBolecode.Bolecode_DadosBoleto_ValorTotalTitulo);
                Bolecode_DadosBoleto.Add("codigo_especie", NewBolecode.Bolecode_DadosBoleto_CodigoEspecie);
                Bolecode_DadosBoleto.Add("data_emissao", NewBolecode.Bolecode_DadosBoleto_DataEmissao);
                Bolecode_DadosBoleto.Add("valor_abatimento", NewBolecode.Bolecode_DadosBoleto_ValorAbatimento);
                Bolecode_DadosBoleto.Add("codigo_tipo_vencimento", NewBolecode.Bolecode_DadosBoleto_CodigoTipoVencimento);
                Bolecode_DadosBoleto.Add("pagamento_parcial", NewBolecode.Bolecode_DadosBoleto_PagamentoParcial);
                Bolecode_DadosBoleto.Add("desconto_expresso", NewBolecode.Bolecode_DadosBoleto_DescontoExpresso);

                // Dados do Boleto Pagador
                Bolecode_DadosBoleto_Pagador_Pessoa.Add("nome_pessoa", NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_NomePessoa);
                Bolecode_DadosBoleto_Pagador_Pessoa.Add("nome_fantasia", NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_NomeFantasia);

                // Dados do Boleto Pagador TipoPessoa
                Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa.Add("codigo_tipo_pessoa", NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_CodigoTipoPessoa);

                if (NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_CodigoTipoPessoa == "F") { Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa.Add("numero_cadastro_pessoa_fisica", NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_NumeroCadastroPessoaFisica); }
                else { Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa.Add("numero_cadastro_nacional_pessoa_juridica", NewBolecode.Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_NumeroCadastroNacionalPessoaJuridica); }

                // Dados do Boleto Pagador Endereço
                Bolecode_DadosBoleto_Pagador_Endereco.Add("nome_logradouro", NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_NomeLogradouro);
                Bolecode_DadosBoleto_Pagador_Endereco.Add("nome_bairro", NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_NomeBairro);
                Bolecode_DadosBoleto_Pagador_Endereco.Add("nome_cidade", NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_NomeCidade);
                Bolecode_DadosBoleto_Pagador_Endereco.Add("sigla_UF", NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_siglaUF);
                Bolecode_DadosBoleto_Pagador_Endereco.Add("numero_CEP", NewBolecode.Bolecode_DadosBoleto_Pagador_Endereco_NumeroCep);

                // Dados do Boleto DadosIndividuais
                Bolecode_DadosBoleto_DadosIndividuais.Add("texto_seu_numero", NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_TextoSeuNumero);
                Bolecode_DadosBoleto_DadosIndividuais.Add("numero_nosso_numero", NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_NumeroNossoNumero);
                Bolecode_DadosBoleto_DadosIndividuais.Add("data_vencimento", NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_DataVencimento);
                Bolecode_DadosBoleto_DadosIndividuais.Add("texto_uso_beneficiario", NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_TextoUsoBeneficiario);
                Bolecode_DadosBoleto_DadosIndividuais.Add("valor_titulo", NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_ValorTitulo);
                Bolecode_DadosBoleto_DadosIndividuais.Add("data_limite_pagamento", NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_DataLimitePagamento);

                // Dados do Boleto Juros
                Bolecode_DadosBoleto_Juros.Add("data_juros", NewBolecode.Bolecode_DadosBoleto_Juros_DataJuros);
                //Bolecode_DadosBoleto_Juros.Add("codigo_tipo_juros", NewBolecode.Bolecode_DadosBoleto_Juros_CodigoTipoJuros);
                //Bolecode_DadosBoleto_Juros.Add("valor_juros", NewBolecode.Bolecode_DadosBoleto_Juros_ValorJuros);

                Bolecode_DadosBoleto_Juros.Add("codigo_tipo_juros", "93");
                Bolecode_DadosBoleto_Juros.Add("valor_juros", "00000000000000100");

                //Bolecode_DadosBoleto_Juros.Add("codigo_tipo_juros", "90");
                //Bolecode_DadosBoleto_Juros.Add("percentual_juros", "000001000000");

                // Dados do Boleto Multa
                Bolecode_DadosBoleto_Multa.Add("codigo_tipo_multa", NewBolecode.Bolecode_DadosBoleto_Multa_CodigoTipoMulta);
                Bolecode_DadosBoleto_Multa.Add("valor_multa", NewBolecode.Bolecode_DadosBoleto_Multa_ValorMulta);

                // Dados QrCode
                Bolecode_QrCode.Add("chave", NewBolecode.Bolecode_QrCode_Chave);

                JObject Object_Bolecode = JObject.Parse(JsonConvert.SerializeObject(Bolecode, Formatting.Indented));
                JObject Object_Bolecode_Beneficiario = JObject.Parse(JsonConvert.SerializeObject(Bolecode_Beneficiario, Formatting.Indented));
                JObject Object_Bolecode_Beneficiario_TipoPessoa = JObject.Parse(JsonConvert.SerializeObject(Bolecode_Beneficiario_TipoPessoa, Formatting.Indented));
                JObject Object_Bolecode_Beneficiario_Endereco = JObject.Parse(JsonConvert.SerializeObject(Bolecode_Beneficiario_Endereco, Formatting.Indented));
                JObject Object_Bolecode_DadosBoleto = JObject.Parse(JsonConvert.SerializeObject(Bolecode_DadosBoleto, Formatting.Indented));
                JObject Object_Bolecode_DadosBoleto_Pagador = JObject.Parse(JsonConvert.SerializeObject(Bolecode_DadosBoleto_Pagador, Formatting.Indented));
                JObject Object_Bolecode_DadosBoleto_Pagador_Pessoa = JObject.Parse(JsonConvert.SerializeObject(Bolecode_DadosBoleto_Pagador_Pessoa, Formatting.Indented));
                JObject Object_Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa = JObject.Parse(JsonConvert.SerializeObject(Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa, Formatting.Indented));
                JObject Object_Bolecode_DadosBoleto_Pagador_Endereco = JObject.Parse(JsonConvert.SerializeObject(Bolecode_DadosBoleto_Pagador_Endereco, Formatting.Indented));
                ListaBolecode_DadosBoleto_DadosIndividuais.Add(JObject.Parse(JsonConvert.SerializeObject(Bolecode_DadosBoleto_DadosIndividuais, Formatting.Indented)));
                JObject Object_Bolecode_DadosBoleto_Juros = JObject.Parse(JsonConvert.SerializeObject(Bolecode_DadosBoleto_Juros, Formatting.Indented));
                JObject Object_Bolecode_DadosBoleto_Multa = JObject.Parse(JsonConvert.SerializeObject(Bolecode_DadosBoleto_Multa, Formatting.Indented));
                JObject Object_Bolecode_QrCode = JObject.Parse(JsonConvert.SerializeObject(Bolecode_QrCode, Formatting.Indented));

                Object_Bolecode_Beneficiario.Add("tipo_pessoa", Object_Bolecode_Beneficiario_TipoPessoa);
                Object_Bolecode_Beneficiario.Add("endereco", Object_Bolecode_Beneficiario_Endereco);
                Object_Bolecode_DadosBoleto_Pagador_Pessoa.Add("tipo_pessoa", Object_Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa);
                Object_Bolecode_DadosBoleto_Pagador.Add("pessoa", Object_Bolecode_DadosBoleto_Pagador_Pessoa);
                Object_Bolecode_DadosBoleto_Pagador.Add("endereco", Object_Bolecode_DadosBoleto_Pagador_Endereco);
                Object_Bolecode_DadosBoleto.Add("pagador", Object_Bolecode_DadosBoleto_Pagador);
                Object_Bolecode_DadosBoleto.Add("dados_individuais_boleto", ListaBolecode_DadosBoleto_DadosIndividuais);
                Object_Bolecode_DadosBoleto.Add("juros", Object_Bolecode_DadosBoleto_Juros);
                Object_Bolecode_DadosBoleto.Add("multa", Object_Bolecode_DadosBoleto_Multa);
                Object_Bolecode.Add("beneficiario", Object_Bolecode_Beneficiario);
                Object_Bolecode.Add("dado_boleto", Object_Bolecode_DadosBoleto);
                Object_Bolecode.Add("dados_qrcode", Object_Bolecode_QrCode);
                var strJson = JsonConvert.SerializeObject(Object_Bolecode);
                strJson = strJson.Replace("\"|", "").Replace("|\"", "");

                String TokenItau = GetTokenApiItau(CertificadoPath);
                string RetornoItau = String.Empty;

                ServicePointManager.Expect100Continue = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                var request = (HttpWebRequest)WebRequest.Create("https://secure.api.itau/pix_recebimentos_conciliacoes/v2/boletos_pix");
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Accept = "application/json";
                request.Headers.Add("x-correlationID", "123456");
                request.Headers.Add("Authorization", "Bearer " + TokenItau); // Pegar o token
                var body = Encoding.ASCII.GetBytes(strJson);
                request.ContentLength = body.Length;

                Stream myWriter = null;
                try
                {
                    NewBolecode.Enviado = true;
                    X509Certificate2 certificate = new X509Certificate2();
                    certificate.Import(CertificadoPath, CertificadoSenha, X509KeyStorageFlags.DefaultKeySet);
                    request.ClientCertificates.Add(certificate);
                    myWriter = request.GetRequestStream();
                    myWriter.Write(body, 0, body.Length);
                    var requestResponse = (HttpWebResponse)request.GetResponse();
                    RetornoItau = new StreamReader(requestResponse.GetResponseStream()).ReadToEnd();
                    NewBolecode.Bolecode_RetornoItau = RetornoItau;

                    if (requestResponse.StatusCode == HttpStatusCode.OK)
                    {
                        NewBolecode.Registrado = true;
                        var JsonData = (JObject)JsonConvert.DeserializeObject(RetornoItau);
                        try { NewBolecode.Bolecode_QrCode_Chave = JsonData["data"]["dados_qrcode"]["chave"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_QrCode_Emv = JsonData["data"]["dados_qrcode"]["emv"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_QrCode_Base64 = JsonData["data"]["dados_qrcode"]["base64"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_QrCode_Txid = JsonData["data"]["dados_qrcode"]["txid"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_QrCode_IdLocation = JsonData["data"]["dados_qrcode"]["id_location"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_QrCode_Location = JsonData["data"]["dados_qrcode"]["location"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_QrCode_TipoCobranca = JsonData["data"]["dados_qrcode"]["tipo_cobranca"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_QrCode_Base64 = JsonData["data"]["dados_qrcode"]["base64"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_CodigoBarras = JsonData["data"]["dado_boleto"]["dados_individuais_boleto"][0]["codigo_barras"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_NumeroLinhaDigitavel = JsonData["data"]["dado_boleto"]["dados_individuais_boleto"][0]["numero_linha_digitavel"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_DacTitulo = JsonData["data"]["dado_boleto"]["dados_individuais_boleto"][0]["dac_titulo"].Value<string>(); } catch (Exception) { };
                        try { NewBolecode.Bolecode_DadosBoleto_DadosIndividuais_IdBoletoIndividual = JsonData["data"]["dado_boleto"]["dados_individuais_boleto"][0]["id_boleto_individual"].Value<string>(); } catch (Exception) { };
                    }
                    else
                    {
                        NewBolecode.Erro = true;
                        NewBolecode.Bolecode_MsgErro = RetornoItau;
                    }
                    return NewBolecode;
                }
                catch (WebException e)
                {
                    NewBolecode.Erro = true;
                    NewBolecode.Bolecode_MsgErro = LibExceptions.getWebException(e);
                    return NewBolecode;
                }
                catch (Exception ex)
                {
                    NewBolecode.Erro = true;
                    NewBolecode.Bolecode_MsgErro = ex.Message;
                    return NewBolecode;
                }
                finally
                {
                    if (myWriter != null) myWriter.Close();
                }
            }
            catch (Exception ex)
            {
                NewBolecode.Erro = true;
                NewBolecode.Bolecode_MsgErro = ex.Message;
                return NewBolecode;
            }
            finally
            {

            }
        }
    }
}