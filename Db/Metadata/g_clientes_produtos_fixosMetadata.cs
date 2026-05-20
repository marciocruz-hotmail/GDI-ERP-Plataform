using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public partial class g_clientes_produtos_fixosMetadata
    {

        [Display(Name = "Id.")]
        public int id_cliente_produto_fixo { get; set; }

        [Display(Name = "Produto")]
        public int id_produto { get; set; }

        [Display(Name = "Data")]
        [DataType(DataType.Date, ErrorMessage = "Campo [Data] contém uma data inválida")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime datahora_contratacao { get; set; }

        [Display(Name = "Qtd")]
        [Required(ErrorMessage = "O campo [Qtd] é obrigatório.")]
        public int qtd { get; set; }

        [Display(Name = "R$ Unit")]
        [Required(ErrorMessage = "O campo [R$ Unit] é obrigatório.")]
        public decimal valor_unit { get; set; }


        public decimal valor_total { get; set; }
        public int id_coligada { get; set; }
        public int id_filial { get; set; }
        public System.DateTime datahora_cadastro { get; set; }
        public int id_usuario_cadastro { get; set; }
        public Nullable<System.DateTime> datahora_alteracao { get; set; }
        public Nullable<int> id_usuario_alteracao { get; set; }
        
    }
}