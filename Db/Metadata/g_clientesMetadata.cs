using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public partial class g_clientesMetadata
    {
        [Display(Name = "Id.")]
        public int id_cliente { get; set; }

        [Display(Name = "Ativo")]
        [Required(ErrorMessage = "Campo [Ativo] é obrigatório.")]
        public short ativo { get; set; }

        [Display(Name = "Nome")]
        [StringLength(100, ErrorMessage = "Campo [Nome] Tamanho máximo 100")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Nome] é obrigatório.")]
        public string nome { get; set; }

        [Display(Name = "Nome Fantasia")]
        [StringLength(100, ErrorMessage = "Campo [Nome Fantasia] Tamanho máximo 100")]
        [DataType(DataType.Text)]
        public string nome_fantasia { get; set; }

        [Display(Name = "Razão Social")]
        [Required(ErrorMessage = "Campo [Razão Social] é obrigatório.")]
        [StringLength(100, ErrorMessage = "Campo [Razão Social] Tamanho máximo 100")]
        [DataType(DataType.Text)]
        public string razao_social { get; set; }

        [Display(Name = "Vendedor")]
        public Nullable<int> id_vendedor { get; set; }

        [Display(Name = "Vendedor (2)")]
        public Nullable<int> id_vendedor2 { get; set; }

        [Display(Name = "Vendedor (3)")]
        public Nullable<int> id_vendedor3 { get; set; }

        [Display(Name = "Contrato")]
        [StringLength(10, ErrorMessage = "Campo [Contrato] Tamanho máximo 10")]
        [DataType(DataType.Text)]
        public string contrato { get; set; }

        [Display(Name = "CNPJ")]
        [StringLength(14, ErrorMessage = "Campo [CNPJ] Tamanho máximo 14")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [CNPJ] preencha somente números")]
        public string cnpj { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Campo [Constituição] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "Constituição")]
        public Nullable<System.DateTime> constituicao { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Campo [Ini. Ativ.] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "Ini. Ativ.")]
        public Nullable<System.DateTime> inicio_atividade { get; set; }

        [Display(Name = "Inscr. Municipal")]
        [StringLength(20, ErrorMessage = "Campo [Inscr. Municipal] Tamanho máximo 20")]
        [DataType(DataType.Text)]
        public string inscricao_municipal { get; set; }

        [Display(Name = "Inscr. Estadual")]
        [StringLength(20, ErrorMessage = "Campo [Inscr. Estadual] Tamanho máximo 20")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Inscr. Estadual] preencha somente números")]
        [DataType(DataType.Text)]
        public string inscricao_estadual { get; set; }

        [Display(Name = "Indicador IE")]
        public int id_indicador_ie { get; set; }


        [Display(Name = "Data Cad.")]
        [DataType(DataType.Date, ErrorMessage = "Campo [Data Cadastro] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> data_cadastro { get; set; }

        [Display(Name = "CPF")]
        [StringLength(11, ErrorMessage = "Campo [CPF] Tamanho máximo 11")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [CPF] preencha somente números")]
        public string cpf { get; set; }

        [Display(Name = "Data Nasc.")]
        [DataType(DataType.Date, ErrorMessage = "Campo [Data Nasc.] contém uma data inválida")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public Nullable<System.DateTime> data_nasc { get; set; }

        [Display(Name = "Matrícula")]
        [StringLength(15, ErrorMessage = "Campo [Matrícula] Tamanho máximo 15")]
        public string matricula { get; set; }

        [Display(Name = "RG")]
        [StringLength(15, ErrorMessage = "Campo [RG] Tamanho máximo 15")]
        public string rg { get; set; }

        [Display(Name = "Endereço")]
        [StringLength(80, ErrorMessage = "Campo [Endereço] Tamanho máximo 80")]
        public string endereco_com { get; set; }

        [Display(Name = "Bairro")]
        [StringLength(30, ErrorMessage = "Campo [Bairro] Tamanho máximo 30")]
        public string bairro_com { get; set; }

        [Display(Name = "Cidade")]
        public Nullable<int> id_cidade_com { get; set; }

        [Display(Name = "CEP")]
        [StringLength(8, ErrorMessage = "Campo [CEP] Tamanho máximo 8")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [CEP] preencha somente números")]
        public string cep_com { get; set; }

        [Display(Name = "UF")]
        public Nullable<int> id_uf_com { get; set; }

        [Display(Name = "Região")]
        public Nullable<int> id_regiao_com { get; set; }

        [Display(Name = "Contato (P)")]
        [StringLength(30, ErrorMessage = "Campo [Contato (Principal)] Tamanho máximo 30")]
        public string contato_principal { get; set; }

        [Display(Name = "Contato (2)")]
        [StringLength(30, ErrorMessage = "Campo [Contato (2)] Tamanho máximo 30")]
        public string contato_2 { get; set; }

        [Display(Name = "Contato (3)")]
        [StringLength(30, ErrorMessage = "Campo [Contato (3)] Tamanho máximo 30")]
        public string contato_3 { get; set; }

        [Display(Name = "E-mail (P)")]
        [DataType(DataType.EmailAddress)]
        [StringLength(60, ErrorMessage = "Campo [E-Mail (Principal)] Tamanho máximo 60")]
        public string email_principal { get; set; }

        [Display(Name = "E-mail (2)")]
        [DataType(DataType.EmailAddress)]
        [StringLength(60, ErrorMessage = "Campo [E-Mail (2)] Tamanho máximo 60")]
        public string email_2 { get; set; }

        [Display(Name = "E-mail (3)")]
        [DataType(DataType.EmailAddress)]
        [StringLength(60, ErrorMessage = "Campo [E-Mail (3)] Tamanho máximo 60")]
        public string email_3 { get; set; }

        [Display(Name = "Telefone (P)")]
        [StringLength(11, ErrorMessage = "Campo [Telefone (Principal)] Tamanho máximo 11")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Telefone (Principal)] preencha somente números")]
        public string telefone_principal { get; set; }

        [Display(Name = "Telefone (2)")]
        [StringLength(11, ErrorMessage = "Campo [Telefone (2)] Tamanho máximo 11")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Telefone (2)] preencha somente números")]
        public string telefone_2 { get; set; }

        [Display(Name = "Telefone (3)")]
        [StringLength(11, ErrorMessage = "Campo [Telefone (3)] Tamanho máximo 11")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [Telefone(3)] preencha somente números")]
        public string telefone_3 { get; set; }
                
        [Display(Name = "E-mail (Boleto)")]        
        [DataType(DataType.EmailAddress)]        
        [StringLength(60, ErrorMessage = "Campo [E-Mail Boleto] Tamanho máximo 60")]
        public string email_boleto { get; set; }

        [Display(Name = "E-mail (Nfe)")]
        [DataType(DataType.EmailAddress)]
        [StringLength(60, ErrorMessage = "Campo [E-Mail Nfe] Tamanho máximo 60")]
        public string email_nfe { get; set; }

        [Display(Name = "Ciclo Faturamento")]
        public Nullable<int> id_ciclo_faturamento { get; set; }

        [Display(Name = "Senha Portal")]
        [StringLength(10, ErrorMessage = "Campo [Senha Portal] Tamanho máximo 10")]
        public string senha_portal { get; set; }

        [Display(Name = "Obs")]
        public string obs { get; set; }

        [Display(Name = "Pefin Serasa")]
        public short param_gdc_pefin_ativo { get; set; }

        [Display(Name = "Consolidar Consumo")]
        public bool param_gdc_consolidar_consumo { get; set; }

        [Display(Name = "Serasa Relacionamento")]
        public bool param_gdc_serasa_relacionamento { get; set; }

        [Display(Name = "Emitir Nota Débito")]
        public bool param_gdc_emitir_nota_debito { get; set; }

        [Display(Name = "Emitir Nota Fiscal")]
        public bool param_gdc_emitir_nota_fiscal { get; set; }

        [Display(Name = "Fat. Boleto")]
        public bool faturamento_boleto { get; set; }

        [Display(Name = "Fat. Débito Conta")]
        public bool faturamento_debito_conta { get; set; }

        [Display(Name = "Banco")]
        [StringLength(3, ErrorMessage = "Campo [Banco] Tamanho máximo 3")]
        public string banco { get; set; }

        [Display(Name = "Agência")]
        [StringLength(4, ErrorMessage = "Campo [Agência] Tamanho máximo 4")]
        public string agencia { get; set; }

        [Display(Name = "Agência (dv)")]
        [StringLength(1, ErrorMessage = "Campo [Agência (dv)] Tamanho máximo 1")]
        public string dv_agencia { get; set; }

        [Display(Name = "Conta")]
        [StringLength(15, ErrorMessage = "Campo [Conta] Tamanho máximo 15")]
        public string conta { get; set; }

        [Display(Name = "Conta (dv)")]
        [StringLength(1, ErrorMessage = "Campo [Conta (dv)] Tamanho máximo 1")]
        public string dv_conta { get; set; }

        [Display(Name = "Operação")]
        [StringLength(3, ErrorMessage = "Campo [Operação] Tamanho máximo 3")]
        public string operacao { get; set; }

        [Display(Name = "R$ Tabela")]
        public Nullable<int> id_consulta_tabela { get; set; }

        [Display(Name = "R$ Consumo Mínimo")]
        [DataType(DataType.Currency)]
        public decimal valor_consumo_minimo { get; set; }

        [Display(Name = "Conta Caixa")]
        public Nullable<int> id_conta_caixa { get; set; }

        [Display(Name = "ISS (%)")]
        public decimal iss_percentual { get; set; }

        [Display(Name = "IR (%)")]
        public decimal ir_percentual { get; set; }

        [Display(Name = "PIS (%)")]
        public decimal pis_percentual { get; set; }

        [Display(Name = "Cofins (%)")]
        public decimal cofins_percentual { get; set; }

        [Display(Name = "CSLL (%)")]
        public decimal csll_percentual { get; set; }

        [Display(Name = "NF (%)")]
        public decimal nf_percentual { get; set; }

        [Display(Name = "PCC (%)")]
        public decimal pcc_percentual { get; set; }

        [Display(Name = "INSS (%)")]
        public decimal inss_percentual { get; set; }

        [Display(Name = "Opt. Simples")]
        public bool optante_simples { get; set; }

        [Display(Name = "R$ Desp. Cob.")]
        [DataType(DataType.Currency)]
        public decimal valor_despesas_cobranca { get; set; }
                
        [Display(Name = "R$ Taxa Adesão")]
        [DataType(DataType.Currency)]
        public decimal valor_taxa_adesao { get; set; }

        [Display(Name = "R$ Limite Consultas")]
        [DataType(DataType.Currency)]
        public decimal valor_limite_consultas { get; set; }

        [Display(Name = "Boleto Impresso?")]
        public bool boleto_impresso { get; set; }

        [Display(Name = "Boleto Email?")]
        public bool boleto_email { get; set; }

        [Display(Name = "Condição Pag/Rec")]
        public int id_pagrec_condicao { get; set; }

        [Display(Name = "CFOP (Venda)")]
        public int id_cfop_venda { get; set; }

        [Display(Name = "Frete (Responsável)")]
        public int id_frete_responsavel { get; set; }

        [Display(Name = "MeProteja Serasa")]
        public bool gdc_meproteja_ativo { get; set; }

    }
}
 