using System.ComponentModel.DataAnnotations;

namespace GdiPlataform.ViewModels
{
    /// <summary>
    /// ViewModel para a tela de troca obrigatória de senha (login com senha expirada).
    /// </summary>
    public class TrocaObrigatoriaSenhaViewModel
    {
        [Display(Name = "Usuário")]
        public string Login { get; set; }

        [Display(Name = "Senha atual")]
        public string SenhaAtual { get; set; }

        [Display(Name = "Nova senha")]
        public string NovaSenha { get; set; }

        [Display(Name = "Confirmação da nova senha")]
        public string ConfirmacaoNovaSenha { get; set; }
    }
}
