using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_cidadesMetadata
    {
        [Display(Name = "Id.")]
        public int id_cidade { get; set; }

        [Display(Name = "Nome")]
        [StringLength(50, ErrorMessage = "Campo [Nome] Tamanho máximo 50")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Nome] é obrigatório.")]
        public string nome { get; set; }

        public int id_usuario_cadastro { get; set; }
        public System.DateTime datahora_cadastro { get; set; }
        public Nullable<int> id_usuario_alteracao { get; set; }
        public Nullable<System.DateTime> datahora_alteracao { get; set; }
    }
}