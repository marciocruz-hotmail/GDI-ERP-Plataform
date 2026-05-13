using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GdiPlataform.Lib
{
    /// <summary>
    /// Validação de complexidade e reutilização de senha.
    /// Regras de complexidade: mínimo 8 caracteres, 1 maiúscula, 1 minúscula, 1 número, 1 símbolo.
    /// Regra de reutilização: nova senha não pode ser igual à atual nem às últimas N do histórico.
    /// </summary>
    public static class PasswordPolicy
    {
        private const int MinLength = 8;
        private static readonly Regex HasUpper = new Regex(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex HasLower = new Regex(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex HasDigit = new Regex(@"\d", RegexOptions.Compiled);
        private static readonly Regex HasSymbol = new Regex(@"[!@#$%¨&*()_+\-=\{\}\[\]:;<>,.?/\\|]", RegexOptions.Compiled);

        /// <summary>
        /// Valida se a senha atende ao padrão de segurança.
        /// </summary>
        /// <param name="senha">Senha a validar.</param>
        /// <param name="mensagemErro">Mensagem descritiva em caso de falha.</param>
        /// <returns>True se válida.</returns>
        public static bool ValidarComplexidade(string senha, out string mensagemErro)
        {
            mensagemErro = null;
            if (string.IsNullOrEmpty(senha))
            {
                mensagemErro = "A senha é obrigatória.";
                return false;
            }
            if (senha.Length < MinLength)
            {
                mensagemErro = "A senha deve ter no mínimo 8 caracteres.";
                return false;
            }
            if (!HasUpper.IsMatch(senha))
            {
                mensagemErro = "A senha deve conter pelo menos 1 letra maiúscula.";
                return false;
            }
            if (!HasLower.IsMatch(senha))
            {
                mensagemErro = "A senha deve conter pelo menos 1 letra minúscula.";
                return false;
            }
            if (!HasDigit.IsMatch(senha))
            {
                mensagemErro = "A senha deve conter pelo menos 1 número.";
                return false;
            }
            if (!HasSymbol.IsMatch(senha))
            {
                mensagemErro = "A senha deve conter pelo menos 1 símbolo (! @ # $ % & * etc.).";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida se a nova senha não é igual à senha atual nem a nenhuma das senhas proibidas (ex.: últimas 3 do histórico).
        /// </summary>
        /// <param name="novaSenha">Nova senha informada.</param>
        /// <param name="senhaAtual">Senha atual do usuário.</param>
        /// <param name="senhasProibidas">Lista de senhas que não podem ser reutilizadas (ex.: últimas 3 de g_usuarios_senhas_historico).</param>
        /// <param name="mensagemErro">Mensagem em caso de falha.</param>
        /// <returns>True se a nova senha é diferente da atual e de todas as proibidas.</returns>
        public static bool ValidarNaoReutilizacao(string novaSenha, string senhaAtual, IEnumerable<string> senhasProibidas, out string mensagemErro)
        {
            mensagemErro = null;
            if (string.IsNullOrEmpty(novaSenha)) { mensagemErro = "A nova senha é obrigatória."; return false; }
            if (novaSenha == senhaAtual)
            {
                mensagemErro = "A nova senha não pode ser igual à senha atual.";
                return false;
            }
            var lista = (senhasProibidas ?? Enumerable.Empty<string>()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (lista.Any(s => s == novaSenha))
            {
                mensagemErro = "A nova senha não pode ser igual a uma das últimas senhas já utilizadas. Escolha uma senha diferente.";
                return false;
            }
            return true;
        }
    }
}
