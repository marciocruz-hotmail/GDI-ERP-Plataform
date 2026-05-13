using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_assistentesMetadata
    {
        [Display(Name = "Id.")]
        public int id_assistente { get; set; }

        [Display(Name = "Assistente")]
        [StringLength(30, ErrorMessage = "Campo [Assistente] Tamanho máximo 30")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Assistente] é obrigatório.")]
        public string nome { get; set; }

        public int id_usuario_cadastro { get; set; }
        public System.DateTime datahora_cadastro { get; set; }
        public Nullable<int> id_usuario_alteracao { get; set; }
        public Nullable<System.DateTime> datahora_alteracao { get; set; }
    }
}