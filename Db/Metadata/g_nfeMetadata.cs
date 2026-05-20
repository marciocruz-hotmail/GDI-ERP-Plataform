using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_nfeMetadata
    {
        [Display(Name = "Id.")]
        public int id_nfe { get; set; }

        [Display(Name = "Tipo")]
        [Required(ErrorMessage = "Campo [Tipo] é obrigatório.")]
        public string tipo_r_c { get; set; }

        [Display(Name = "Status")]
        public int id_nfe_status { get; set; }

        [Display(Name = "Descrição")]
        [Required(ErrorMessage = "Campo [Descrição] é obrigatório.")]
        public string descricao { get; set; }

        [Display(Name = "Id. Cliente")]
        [Required(ErrorMessage = "Campo [Id. Cliente] é obrigatório.")]
        public int id_cliente { get; set; }

        [Display(Name = "Nome")]
        [StringLength(100, ErrorMessage = "Campo [Nome] Tamanho máximo 100")]
        public string nome { get; set; }

        [Display(Name = "Razão Social")]
        [StringLength(100, ErrorMessage = "Campo [Razão Social] Tamanho máximo 100")]
        public string razao_social { get; set; }

        [Display(Name = "Email")]
        [StringLength(60, ErrorMessage = "Campo [Email] Tamanho máximo 60")]
        public string email { get; set; }

        [Display(Name = "CPF")]
        [StringLength(11, ErrorMessage = "Campo [CPF] Tamanho máximo 11")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [CPF] preencha somente números")]
        public string cpf { get; set; }

        [Display(Name = "CNPJ")]
        [StringLength(14, ErrorMessage = "Campo [CNPJ] Tamanho máximo 14")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [CNPJ] preencha somente números")]
        public string cnpj { get; set; }

        [Display(Name = "Inscr. Municipal")]
        [StringLength(20, ErrorMessage = "Campo [Inscr. Municipal] Tamanho máximo 20")]
        //[RegularExpression("([0-9]+)", ErrorMessage = "Campo [Inscr. Municipal] preencha somente números")]
        [DataType(DataType.Text)]
        public string inscricao_municipal { get; set; }

        [Display(Name = "Endereço")]
        [StringLength(80, ErrorMessage = "Campo [Endereço] Tamanho máximo 80")]
        [Required(ErrorMessage = "Campo [Endereço] é obrigatório.")]
        [DataType(DataType.Text)]
        public string endereco_com { get; set; }

        [Display(Name = "Bairro")]
        [StringLength(80, ErrorMessage = "Campo [Bairro] Tamanho máximo 30")]
        [Required(ErrorMessage = "Campo [Bairro] é obrigatório.")]
        [DataType(DataType.Text)]
        public string bairro_com { get; set; }

        [Display(Name = "Cidade")]
        [Required(ErrorMessage = "Campo [Cidade] é obrigatório.")]
        public int id_cidade_com { get; set; }

        [Display(Name = "CEP")]
        [StringLength(8, ErrorMessage = "Campo [CEP] Tamanho máximo 8")]
        [Required(ErrorMessage = "Campo [CEP] é obrigatório.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Campo [CEP] preencha somente números")]
        public string cep_com { get; set; }

        [Display(Name = "UF")]
        [Required(ErrorMessage = "Campo [UF] é obrigatório.")]
        public int id_uf_com { get; set; }

        [Display(Name = "ISS (%)")]
        [DataType(DataType.Currency)]
        public decimal iss_valor { get; set; }

        [Display(Name = "ISS (Retido)")]
        public bool iss_retido { get; set; }

        [Display(Name = "Cofins (R$)")]
        [DataType(DataType.Currency)]
        public decimal cofins_valor { get; set; }

        [Display(Name = "Csll (R$)")]
        [DataType(DataType.Currency)]
        public decimal csll_valor { get; set; }

        [Display(Name = "INSS (R$)")]
        [DataType(DataType.Currency)]
        public decimal inss_valor { get; set; }

        [Display(Name = "IR (R$)")]
        [DataType(DataType.Currency)]
        public decimal ir_valor { get; set; }

        [Display(Name = "PIS (R$)")]
        [DataType(DataType.Currency)]
        public decimal pis_valor { get; set; }

        [Display(Name = "Liquido (R$)")]
        [DataType(DataType.Currency)]
        public decimal valor_total_liquido { get; set; }

        [Display(Name = "Bruto (R$)")]
        [DataType(DataType.Currency)]
        public decimal valor_total_bruto { get; set; }

        [Display(Name = "Descontos (R$)")]
        [DataType(DataType.Currency)]
        public decimal valor_descontos { get; set; }

        [Display(Name = "Encargos (R$)")]
        [DataType(DataType.Currency)]
        public decimal valor_encargos { get; set; }

        [Display(Name = "Dt. Proc.")]
        [DataType(DataType.Date, ErrorMessage = "Campo [Data Processamento] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime data_processamento { get; set; }

        [Display(Name = "Dt. Venc.")]
        [DataType(DataType.Date, ErrorMessage = "Campo [Data Vencimento] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime data_vencimento { get; set; }

        // Campos Internos
        public int id_nfe_config { get; set; }
        public string nfe_key { get; set; }
        public Nullable<System.DateTime> data_envio_registro { get; set; }
        public Nullable<System.DateTime> data_retorno_registro { get; set; }
        public Nullable<System.DateTime> data_envio_cancelamento { get; set; }
        public Nullable<System.DateTime> data_retorno_cancelamento { get; set; }
        public int id_coligada { get; set; }
        public int id_filial { get; set; }
        public System.DateTime datahora_cadastro { get; set; }
        public int id_usuario_cadastro { get; set; }
        public Nullable<System.DateTime> datahora_alteracao { get; set; }
        public Nullable<int> id_usuario_alteracao { get; set; }
    }
}