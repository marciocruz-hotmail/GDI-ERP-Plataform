using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public class g_vendedoresMetadata
    {
        [Display(Name = "Id.")]
        public int id_vendedor { get; set; }

        [Display(Name = "Ativo")]
        [Required(ErrorMessage = "Campo [Ativo] é obrigatório.")]
        public bool ativo { get; set; }

        [Display(Name = "Vendedor")]
        [StringLength(30, ErrorMessage = "Campo [Vendedor] Tamanho máximo 30")]
        [DataType(DataType.Text)]
        [Required(ErrorMessage = "Campo [Vendedor] é obrigatório.")]
        public string nome { get; set; }

        [Display(Name = "Revenda")]
        public Nullable<int> id_revenda { get; set; }

        [Display(Name = "E-mail")]
        [StringLength(50, ErrorMessage = "Campo [Email] Tamanho máximo 50")]
        [DataType(DataType.EmailAddress)]
        public string email { get; set; }

        [Display(Name = "Login")]
        [StringLength(20, ErrorMessage = "Campo [Login] Tamanho máximo 20")]
        [DataType(DataType.Text)]
        public string login { get; set; }

        [Display(Name = "Senha")]
        [StringLength(20, ErrorMessage = "Campo [Senha] Tamanho máximo 20")]
        [DataType(DataType.Text)]
        public string senha { get; set; }

        [Display(Name = "Telefone")]
        [StringLength(20, ErrorMessage = "Campo [Telefone] Tamanho máximo 20")]
        [DataType(DataType.Text)]
        public string telefone_1 { get; set; }

    }
}