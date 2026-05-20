using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Db.Metadata
{
    public partial class g_usuariosMetadata
    {
        [Display(Name = "Id.")]
        public int id_usuario { get; set; }

        [Display(Name = "Nome")]
        [StringLength(20, ErrorMessage = "Campo [Nome] Tamanho máximo 20")]
        [Required(ErrorMessage = "O Campo [Nome] é obrigatório.")]
        public string nome { get; set; }

        [Display(Name = "Email")]
        [StringLength(50, ErrorMessage = "Campo [Email] Tamanho máximo 50")]
        [Required(ErrorMessage = "O Campo [Email] é obrigatório.")]
        public string email { get; set; }

        [Display(Name = "Login")]
        [StringLength(20, ErrorMessage = "Campo [Login] Tamanho máximo 20")]
        [Required(ErrorMessage = "O Campo [Login] é obrigatório.")]
        public string login { get; set; }

        [Display(Name = "Senha")]
        [StringLength(20, ErrorMessage = "Campo [Senha] Tamanho máximo 20")]
        [Required(ErrorMessage = "O Campo [Senha] é obrigatório.")]
        public string senha { get; set; }

        [Display(Name = "Ativo")]
        public bool ativo { get; set; }

        [Display(Name = "Perfil")]
        [Required(ErrorMessage = "O Campo [Perfil] é obrigatório.")]
        public int id_perfil { get; set; }

        [Display(Name = "Logomarca")]
        public int id_logomarca { get; set; }

        [Display(Name = "Coligada")]
        [Required(ErrorMessage = "O Campo [Coligada] é obrigatório.")]
        public int id_coligada { get; set; }

        [Display(Name = "Filial")]
        [Required(ErrorMessage = "O Campo [Filial] é obrigatório.")]
        public int id_filial { get; set; }
    }
}