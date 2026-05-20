using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Robos.Itau
{
    public class ModelItauBolecode
    {
        public bool Enviado { get; set; }
        public bool Registrado { get; set; }
        public bool Erro { get; set; }
        public string Bolecode_CodigoCanalOperacao { get; set; }
        public string Bolecode_CodigoOperador { get; set; }
        public string Bolecode_EtapaProcessoBoleto { get; set; }
        public string Bolecode_Beneficiario_IdBeneficiario { get; set; }
        public string Bolecode_Beneficiario_NomeCobranca { get; set; }
        public string Bolecode_Beneficiario_TipoPessoa_CodigoTipoPessoa { get; set; }
        public string Bolecode_Beneficiario_TipoPessoa_NumeroCadastroNacionaPessoaJuridica { get; set; }
        public string Bolecode_Beneficiario_Endereco_NomeLogradouro { get; set; }
        public string Bolecode_Beneficiario_Endereco_NomeBairro { get; set; }
        public string Bolecode_Beneficiario_Endereco_NomeCidade { get; set; }
        public string Bolecode_Beneficiario_Endereco_SiglaUF { get; set; }
        public string Bolecode_Beneficiario_Endereco_NumeroCep { get; set; }
        public string Bolecode_Beneficiario_Endereco_Numero { get; set; }
        public string Bolecode_Beneficiario_Endereco_Complemento { get; set; }
        public string Bolecode_DadosBoleto_DescricaoInstrumentoCobranca { get; set; }
        public string Bolecode_DadosBoleto_TipoBoleto { get; set; }
        public string Bolecode_DadosBoleto_CodigoCarteira { get; set; }
        public string Bolecode_DadosBoleto_ValorTotalTitulo { get; set; }
        public string Bolecode_DadosBoleto_CodigoEspecie { get; set; }
        public string Bolecode_DadosBoleto_DataEmissao { get; set; }
        public string Bolecode_DadosBoleto_ValorAbatimento { get; set; }
        public string Bolecode_DadosBoleto_CodigoTipoVencimento { get; set; }
        public string Bolecode_DadosBoleto_PagamentoParcial { get; set; }
        public string Bolecode_DadosBoleto_DescontoExpresso { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Pessoa_NomePessoa { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Pessoa_NomeFantasia { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_CodigoTipoPessoa { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_NumeroCadastroPessoaFisica { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Pessoa_TipoPessoa_NumeroCadastroNacionalPessoaJuridica { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Endereco_NomeLogradouro { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Endereco_NomeBairro { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Endereco_NomeCidade { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Endereco_siglaUF { get; set; }
        public string Bolecode_DadosBoleto_Pagador_Endereco_NumeroCep { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_TextoSeuNumero { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_NumeroNossoNumero { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_DataVencimento { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_TextoUsoBeneficiario { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_ValorTitulo { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_DataLimitePagamento { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_CodigoBarras { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_NumeroLinhaDigitavel { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_DacTitulo { get; set; }
        public string Bolecode_DadosBoleto_DadosIndividuais_IdBoletoIndividual { get; set; }
        public string Bolecode_DadosBoleto_Juros_DataJuros { get; set; }
        public string Bolecode_DadosBoleto_Juros_CodigoTipoJuros { get; set; }
        public string Bolecode_DadosBoleto_Juros_ValorJuros { get; set; }
        public string Bolecode_DadosBoleto_Multa_CodigoTipoMulta { get; set; }
        public string Bolecode_DadosBoleto_Multa_ValorMulta { get; set; }
        public string Bolecode_QrCode_Chave { get; set; }
        public string Bolecode_QrCode_Emv { get; set; }
        public string Bolecode_QrCode_Base64 { get; set; }
        public string Bolecode_QrCode_Txid { get; set; }
        public string Bolecode_QrCode_IdLocation { get; set; }
        public string Bolecode_QrCode_Location { get; set; }
        public string Bolecode_QrCode_TipoCobranca { get; set; }
        public string Bolecode_MsgErro { get; set; }
        public string Bolecode_RetornoItau { get; set; }
        public ModelItauBolecode()
        {
            Enviado = false;
            Registrado = false;
            Erro = false;
        }
    }
}