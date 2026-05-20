using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class gc_movimentos_itensMetadata
    {
        [Display(Name = "Sequencia")]
        //[RegularExpression(@"^\d+.?\d{0,2}$", ErrorMessage = "Campo [Sequência] contém um valor inválido!")]
        public decimal sequencia { get; set; }

        [Display(Name = "Qtd.")]
        //[RegularExpression(@"^\d+.?\d{0,2}$", ErrorMessage = "Campo [Quantidade] contém um valor inválido!")]
        public decimal quantidade { get; set; }

        [Display(Name = "R$ Unit.")]
        [DataType(DataType.Currency)]
        //[RegularExpression(@"^\d+.?\d{0,2}$", ErrorMessage = "Campo [R$ Unit.] contém um valor inválido!")]
        public decimal valor_unit { get; set; }

        [Display(Name = "R$ Total")]
        [DataType(DataType.Currency)]
        //[RegularExpression(@"^\d+.?\d{0,2}$", ErrorMessage = "Campo [R$ Total] contém um valor inválido!")]
        public decimal valor_total { get; set; }

        [Display(Name = "R$ Core")]
        [DataType(DataType.Currency)]
        public decimal valor_unit_corecharge { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime lote01_validade { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime lote02_validade { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public System.DateTime lote03_validade { get; set; }
        
        [StringLength(20, ErrorMessage = "Campo [Serial] Tamanho máximo 20")]
        public string serial { get; set; }
        
        [StringLength(30, ErrorMessage = "Campo [Lote 1] Tamanho máximo 30")]
        public string lote01_identificador { get; set; }

        [StringLength(30, ErrorMessage = "Campo [Lote 2] Tamanho máximo 30")]
        public string lote02_identificador { get; set; }

        [StringLength(30, ErrorMessage = "Campo [Lote 3] Tamanho máximo 30")]
        public string lote03_identificador { get; set; }
        
        [StringLength(30, ErrorMessage = "Campo [Lote 4] Tamanho máximo 30")]
        public string lote04_identificador { get; set; }

        [StringLength(30, ErrorMessage = "Campo [Lote 5] Tamanho máximo 30")]
        public string lote05_identificador { get; set; }

        [StringLength(50, ErrorMessage = "Campo [Obs] Tamanho máximo 50")] 
        public string obs { get; set; }
    }
}