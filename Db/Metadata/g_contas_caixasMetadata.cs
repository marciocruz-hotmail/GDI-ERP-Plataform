using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_contas_caixasMetadata
    {
        [Display(Name = "Id.")]
        public int id_conta_caixa { get; set; }

        [Display(Name = "Nome")]
        [StringLength(30, ErrorMessage = "Campo [Nome] Tamanho máximo 30")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Nome] é obrigatório.")]
        public string nome { get; set; }

        [Display(Name = "Boleto Emissão")]
        public bool boleto_emissao { get; set; }

        [Display(Name = "Boleto Cnab Retorno")]
        public bool boleto_cnab_retorno { get; set; }

        [Display(Name = "Banco")]
        [StringLength(3, ErrorMessage = "Campo [Banco] Tamanho máximo 3")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Banco] preencha somente números")]
        [Required(ErrorMessage = "Campo [Banco] é obrigatório.")]
        public string banco { get; set; }

        [Display(Name = "Agência")]
        [StringLength(4, ErrorMessage = "Campo [Agência] Tamanho máximo 4")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Agência] preencha somente números")]
        [Required(ErrorMessage = "Campo [Agência] é obrigatório.")]
        public string agencia { get; set; }

        [Display(Name = "Agência (dv)")]
        [StringLength(1, ErrorMessage = "Campo [Agência (dv)] Tamanho máximo 1")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Agência (dv)] preencha somente números")]
        [Required(ErrorMessage = "Campo [Agência (dv)] é obrigatório.")]
        public string dv_agencia { get; set; }

        [Display(Name = "Conta")]
        [StringLength(15, ErrorMessage = "Campo [Conta] Tamanho máximo 15")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Conta] preencha somente números")]
        [Required(ErrorMessage = "Campo [Conta] é obrigatório.")]
        public string conta { get; set; }

        [Display(Name = "Conta (dv)")]
        [StringLength(1, ErrorMessage = "Campo [Conta (dv)] Tamanho máximo 1")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Conta (dv)] preencha somente números")]
        [Required(ErrorMessage = "Campo [Conta (dv)] é obrigatório.")]
        public string dv_conta { get; set; }

        [Display(Name = "Carteira")]
        [StringLength(10, ErrorMessage = "Campo [Carteira] Tamanho máximo 10")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Carteira] preencha somente números")]
        public string carteira { get; set; }

        [Display(Name = "Cód. Empresa")]
        [StringLength(10, ErrorMessage = "Campo [Cód. Empresa] Tamanho máximo 10")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Cód. Empresa] preencha somente números")]
        public string codigo_empresa { get; set; }

        [Display(Name = "Cód. Convênio")]
        [StringLength(10, ErrorMessage = "Campo [Cód. Convênio] Tamanho máximo 10")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Cód. Convênio] preencha somente números")]
        public string codigo_convenio { get; set; }

        [Display(Name = "Nosso Número")]
        public Nullable<float> nossonumero { get; set; }

        [Display(Name = "Moeda")]
        [StringLength(3, ErrorMessage = "Campo [Moeda] Tamanho máximo 3")]
        [Required(ErrorMessage = "Campo [Moeda] é obrigatório.")]
        public string especie_moeda { get; set; }

        [Display(Name = "Aceite")]
        public string aceite { get; set; }

        [Display(Name = "Desc. (Fixo)")]
        [DataType(DataType.Currency)]
        public float desconto_fixo { get; set; }

        [Display(Name = "Desc. (Dia)")]
        [DataType(DataType.Currency)]
        public float desconto_dia { get; set; }

        [Display(Name = "Multa/Mora (Fixo)")]
        [DataType(DataType.Currency)]
        public float multa_fixo { get; set; }

        [Display(Name = "Multa/Mora (Dia)")]
        [DataType(DataType.Currency)]
        public float multa_dia { get; set; }

        [Display(Name = "Protesto Automático")]
        public bool protesto_automatico { get; set; }

        [Display(Name = "Protesto (Qtd. Dias)")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Protesto (Qtd. Dias)] preencha somente números")]
        public int protesto_dias { get; set; }

        [Display(Name = "Operação")]
        [StringLength(3, ErrorMessage = "Campo [Operação] Tamanho máximo 3")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Operação] preencha somente números")]
        public string operacao { get; set; }

        [Display(Name = "Versão Layout")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Versão Layout] preencha somente números")]
        public Nullable<int> versao_layout { get; set; }

        [Display(Name = "Nosso Núm. (Inicial)")]
        public Nullable<int> inicial_nossonumero { get; set; }

        [Display(Name = "Contra Apresentação")]
        public bool contra_apresentacao { get; set; }

        [Display(Name = "Valor Zerado")]
        public bool valor_zerado { get; set; }

        [Display(Name = "Razão Social")]
        [StringLength(50, ErrorMessage = "Campo [Razão Social] Tamanho máximo 50")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Razão Social] é obrigatório.")]
        public string razao_social { get; set; }

        [Display(Name = "Nome Fantasia")]
        [StringLength(50, ErrorMessage = "Campo [Nome Fantasia] Tamanho máximo 50")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Nome Fantasia] é obrigatório.")]
        public string nome_fantasia { get; set; }

        [Display(Name = "CNPJ")]
        [StringLength(14, ErrorMessage = "Campo [CNPJ] Tamanho máximo 14")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [CNPJ] preencha somente números")]
        public string cnpj { get; set; }

        [Display(Name = "Endereço")]
        [StringLength(80, ErrorMessage = "Campo [Endereço] Tamanho máximo 80")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Endereço] é obrigatório.")]
        public string endereco_com { get; set; }

        [Display(Name = "Bairro")]
        [StringLength(30, ErrorMessage = "Campo [Bairro] Tamanho máximo 30")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Bairro] é obrigatório.")]
        public string bairro_com { get; set; }

        [Display(Name = "Cidade")]
        [Required(ErrorMessage = "Campo [Cidade] é obrigatório.")]
        public int id_cidade_com { get; set; }

        [Display(Name = "Cep")]
        [StringLength(9, ErrorMessage = "Campo [Cep] Tamanho máximo 9")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Cep] é obrigatório.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Cep] preencha somente números")]
        public string cep_com { get; set; }

        [Display(Name = "UF")]
        [Required(ErrorMessage = "Campo [Uf] é obrigatório.")]
        public int id_uf_com { get; set; }

        [Display(Name = "Msg Cliente")]
        [DataType(DataType.Text)]
        public string mensagem_cliente { get; set; }

        [Display(Name = "Msg Caixa")]
        [DataType(DataType.Text)]
        public string mensagem_caixa { get; set; }

        [Display(Name = "Logotipo")]
        [DataType(DataType.Text)]
        public string logotipo_url { get; set; }

        [Display(Name = "Local Pagto.")]
        [StringLength(100, ErrorMessage = "Campo [Local Pagto.] Tamanho máximo 100")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Local Pagto.] é obrigatório.")]
        public string local_pagamento { get; set; }

        [Display(Name = "Telefone")]
        [StringLength(11, ErrorMessage = "Campo [Telefone] Tamanho máximo 11")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Telefone] preencha somente números")]
        public string telefone { get; set; }
    }
}