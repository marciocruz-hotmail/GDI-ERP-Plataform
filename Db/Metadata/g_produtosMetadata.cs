using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_produtosMetadata
    {
        [Display(Name = "Id.")]
        public int id_produto { get; set; }

        [Display(Name = "Tipo Produto")]
        [Required(ErrorMessage = "Campo [Tipo de Produto] é obrigatório")]
        public int id_produto_tipo { get; set; }

        [Display(Name = "Ativo")]
        public bool ativo { get; set; }

        [Display(Name = "Serviço")]
        [Required(ErrorMessage = "Campo [Serviço] é obrigatório")]
        public sbyte is_servico { get; set; }

        [Display(Name = "Código")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Código] é obrigatório.")]
        public string codigo { get; set; }

        [Display(Name = "Nome")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Nome] é obrigatório.")]
        public string nome { get; set; }

        [Display(Name = "Descrição")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Descrição] é obrigatório.")]
        public string descricao { get; set; }

        [Display(Name = "R$ Referência")]
        [DataType(DataType.Currency)]
        [Required(ErrorMessage = "Campo [R$ Referência] é obrigatório.")]
        public decimal valor_base { get; set; }
    }
}