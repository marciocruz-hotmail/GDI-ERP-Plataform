using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_revendasMetadata
    {
        [Display(Name = "Id.")]
        public int id_revenda { get; set; }

        [Display(Name = "Ativo")]
        [Required(ErrorMessage = "Campo [Ativo] é obrigatório.")]
        public bool ativo { get; set; }

        [Display(Name = "Revenda")]
        [StringLength(30, ErrorMessage = "Campo [Revenda] Tamanho máximo 30")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Revenda] é obrigatório.")]
        public string nome { get; set; }

        [Display(Name = "Resp. Revenda")]
        [StringLength(30, ErrorMessage = "Campo [Resp. Revenda] Tamanho máximo 30")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Resp. Revenda] é obrigatório.")]
        public string resp_revenda { get; set; }

        public int id_usuario_cadastro { get; set; }
        public System.DateTime datahora_cadastro { get; set; }
        public Nullable<int> id_usuario_alteracao { get; set; }
        public Nullable<System.DateTime> datahora_alteracao { get; set; }
    }
}