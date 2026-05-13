using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_produtos_tiposMetadata
    {
        [Display(Name = "Id.")]
        public int id_produto_tipo { get; set; }

        [Display(Name = "Nome")]
        [StringLength(30, ErrorMessage = "Campo [Nome] Tamanho máximo 30")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Nome] é obrigatório.")]
        public string nome { get; set; }
    }
}