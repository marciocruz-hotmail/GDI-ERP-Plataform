using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_regioesMetadata
    {
        [Display(Name = "Id.")]
        public int id_regiao { get; set; }

        [Display(Name = "Nome")]
        [StringLength(20, ErrorMessage = "Campo [Nome] Tamanho máximo 20")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Nome] é obrigatório.")]
        public string nome { get; set; }
    }
}