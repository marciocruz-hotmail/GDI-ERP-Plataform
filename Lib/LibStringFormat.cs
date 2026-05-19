using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using GdiPlataform.Areas.g.Models;
using System.Xml;

namespace GdiPlataform.Lib
{
    public static class LibStringFormat
    {
        public static string ToTitleCase(string str)
        {
            String Retorno = String.Empty;
            if (str != null)
            {
                Retorno = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
                Retorno = Retorno.Replace(" Da ", " da ").Replace(" De ", " de ").Replace(" Di ", " di ").Replace(" Do ", " do ").Replace(" Das ", " das ").Replace(" Dos ", " dos ").Replace(" A ", " a ").Replace(" E ", " e ").Replace(" I ", " i ").Replace(" O ", " o ").Replace(" U ", " u ").Replace(" Com ", " com ").Replace("(A)", "(a)").Replace("(O)", "(o)");
            }
            return Retorno;
        }

        public static String EmptyIfNull(this object value)
        {
            if (value == null)
                return String.Empty;
            return value.ToString();
        }

        public static String FormatarNumeroSerasa(String Numero, int Tamanho)
        {
            Numero = Numero.Trim();
            Numero = SomenteAlfabetoeNumeros(RemoverAcentos(Numero));

            if (Numero.Length < Tamanho)
            {
                while (Numero.Length < Tamanho)
                {
                    Numero = "0" + Numero;
                }
            }
            else if (Numero.Length > Tamanho)
            {
                Numero = Numero.Substring(Numero.Length - Tamanho);
            }
            return Numero;
        }

        public static String FormatarNumeroSefaz(String Numero, int Tamanho)
        {
            Numero = Numero.Trim();
            Numero = SomenteAlfabetoeNumeros(RemoverAcentos(Numero));

            if (Numero.Length < Tamanho)
            {
                while (Numero.Length < Tamanho)
                {
                    Numero = "0" + Numero;
                }
            }
            else if (Numero.Length > Tamanho)
            {
                Numero = Numero.Substring(Numero.Length - Tamanho);
            }
            return Numero;
        }

        public static String FormatarAlphaSerasa(String Alpha, int Tamanho)
        {
            Alpha = Alpha.Trim();
            Alpha = SomenteAlfabetoSerasa(RemoverAcentos(Alpha));

            if (Alpha.Length < Tamanho)
            {
                while (Alpha.Length < Tamanho)
                {
                    Alpha += " ";
                }
            }
            else if (Alpha.Length > Tamanho)
            {
                Alpha = Alpha.Substring(0, Tamanho);
            }
            return Alpha;
        }

        public static String FormatarInserirEspacoDireita(String Alpha, int Tamanho)
        {
            Alpha = Alpha.Trim();

            if (Alpha.Length < Tamanho)
            {
                while (Alpha.Length < Tamanho)
                {
                    Alpha += " ";
                }
            }
            else if (Alpha.Length > Tamanho)
            {
                Alpha = Alpha.Substring(0, Tamanho);
            }
            return Alpha;
        }

        public static String FormatarNumeroContratoSerasa(String Alpha, int Tamanho)
        {
            Alpha = Alpha.Trim();
            if (Alpha.Length < Tamanho)
            {
                while (Alpha.Length < Tamanho)
                {
                    Alpha = "0" + Alpha;
                }
            }
            else if (Alpha.Length > Tamanho)
            {
                Alpha = Alpha.Substring(0, Tamanho);
            }
            return Alpha;
        }

        public static String FormatarEmailSerasa(String Alpha, int Tamanho)
        {
            Alpha = Alpha.Trim();
            Alpha = RemoverAcentos(Alpha);

            if (Alpha.Length < Tamanho)
            {
                while (Alpha.Length < Tamanho)
                {
                    Alpha += " ";
                }
            }
            else if (Alpha.Length > Tamanho)
            {
                Alpha = Alpha.Substring(0, Tamanho);
            }
            return Alpha;
        }

        public static String FormatarAlphaGateway(String Alpha, int Tamanho)
        {
            Alpha.Trim();

            if (Alpha.Length < Tamanho)
            {
                while (Alpha.Length < Tamanho)
                {
                    Alpha = "*" + Alpha;
                }
            }
            else if (Alpha.Length > Tamanho)
            {
                Alpha = Alpha.Substring(0, Tamanho);
            }
            return Alpha;
        }

        public static String FormatarCPFCNPJ(String tipoDocumento, String documento)
        {
            String retorno = documento;

            if ((tipoDocumento.Equals("F")) || (tipoDocumento.Equals("1")))
            {
                if (documento.Length == 11)
                { retorno = retorno.Substring(0, 3) + "." + retorno.Substring(3, 3) + "." + retorno.Substring(6, 3) + "-" + retorno.Substring(9, 2); }
                else if (documento.Length == 14)
                { retorno = retorno.Substring(3, 3) + "." + retorno.Substring(6, 3) + "." + retorno.Substring(9, 3) + "-" + retorno.Substring(12, 2); }
            }
            else if ((tipoDocumento.Equals("J")) || (tipoDocumento.Equals("2")))
            {
                if (documento.Length == 14)
                { retorno = retorno.Substring(0, 2) + "." + retorno.Substring(2, 3) + "." + retorno.Substring(5, 3) + "/" + retorno.Substring(8, 4) + "-" + retorno.Substring(12, 2); }
            }
            return retorno;
        }

        public static String FormatarTelefone(String telefone)
        {
            String retorno = telefone;
            try { retorno = "(" + telefone.Substring(0, 2) + ") " + telefone.Substring(2); } catch (Exception) { };
            return retorno;
        }

        public static String FormatarTelefoneDDD(String telefone)
        {
            String retorno = telefone;
            try { retorno = "(" + telefone.Substring(0, 2) + ") " + telefone.Substring(2, 4) + "-" + telefone.Substring(6, 4); } catch (Exception) { };

            return retorno;
        }

        public static String FormatarCEP(String cep)
        {
            String retorno = cep;
            if (cep.Length == 8)
            {
                retorno = cep.Substring(0, 5) + "-" + cep.Substring(5, 3);
            }
            return retorno;
        }

        public static String FormatarDataDDMMAAAA(String data)
        {
            String retorno = data;
            if (data.Length == 8)
            {
                retorno = retorno.Substring(0, 2) + "/" + retorno.Substring(2, 2) + "/" + retorno.Substring(4, 4);
            }
            return retorno;
        }

        public static String FormatarDataAAAAMMDD(String data)
        {
            String retorno = data;
            if (data.Length == 8)
            {
                retorno = retorno.Substring(6, 2) + "/" + retorno.Substring(4, 2) + "/" + retorno.Substring(0, 4);
            }
            return retorno;
        }

        public static String FormatarDataMMMAAAA(String data)
        {
            String retorno = data;
            if (data.Length == 8)
            {
                string mes = retorno.Substring(4, 2);
                if (mes.Equals("01")) { mes = "Jan"; }
                else if (mes.Equals("02")) { mes = "Fev"; }
                else if (mes.Equals("03")) { mes = "Mar"; }
                else if (mes.Equals("04")) { mes = "Abr"; }
                else if (mes.Equals("05")) { mes = "Mai"; }
                else if (mes.Equals("06")) { mes = "Jun"; }
                else if (mes.Equals("07")) { mes = "Jul"; }
                else if (mes.Equals("08")) { mes = "Ago"; }
                else if (mes.Equals("09")) { mes = "Set"; }
                else if (mes.Equals("10")) { mes = "Out"; }
                else if (mes.Equals("11")) { mes = "Nov"; }
                else if (mes.Equals("12")) { mes = "Dez"; };
                retorno = mes + "/" + retorno.Substring(0, 4);
            }
            return retorno;
        }

        public static String FormatarDataAAAAMM(String data)
        {
            String retorno = data;
            if (data.Length == 6)
            {
                retorno = retorno.Substring(4, 2) + "/" + retorno.Substring(0, 4);
            }
            return retorno;
        }

        public static String FormatarValorNumerico(String valor)
        {
            valor = valor.TrimStart('0');
            float _valor = 0;
            float.TryParse(valor, out _valor);
            if (_valor > 0) { _valor = _valor / 100; };
            return string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", _valor);
        }

        public static String RemoverAcentos(string texto)
        {
            if (texto != null)
            {
                string comAcentos = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇç";
                string semAcentos = "AAAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUuuuuCc";
                for (int i = 0; i < comAcentos.Length; i++)
                {
                    texto = texto.Replace(comAcentos[i].ToString(), semAcentos[i].ToString());
                }
            }
            return texto;
        }

        public static String SomenteAlfabetoeNumeros(string texto)
        {
            string alfabeto = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇçabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
            string textoSaida = String.Empty;
            for (int i = 0; i < texto.Length; i++)
            {
                if (alfabeto.IndexOf(texto[i].ToString()) > -1)
                {
                    textoSaida += texto[i].ToString();
                }
            }
            return textoSaida;
        }

        public static String SomenteNumeros(string texto)
        {
            string textoEntrada = "0123456789";
            string textoSaida = String.Empty;
            for (int i = 0; i < texto.Length; i++)
            {
                if (textoEntrada.IndexOf(texto[i].ToString()) > -1)
                {
                    textoSaida += texto[i].ToString();
                }
            }
            return textoSaida;
        }

        public static String SomenteAlfabetoSerasa(string texto)
        {
            string alfabeto = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇçabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789&@- ";
            string textoSaida = String.Empty;
            for (int i = 0; i < texto.Length; i++)
            {
                if (alfabeto.IndexOf(texto[i].ToString()) > -1)
                {
                    textoSaida += texto[i].ToString();
                }
            }
            return textoSaida;
        }

        public static String FormatarFileName(string texto)
        {
            string alfabeto = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇçabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-. ";
            string textoSaida = String.Empty;
            for (int i = 0; i < texto.Length; i++)
            {
                if (alfabeto.IndexOf(texto[i].ToString()) > -1)
                {
                    textoSaida += texto[i].ToString();
                }
            }
            return textoSaida;
        }


        public static String SomenteAlfabetoSefaz(string TextoOrigem)
        {
            String TextoSaida = String.Empty;
            if (TextoOrigem.EmptyIfNull().ToString().Length == 0)
            {
                TextoSaida = TextoOrigem;
            }
            else
            {
                TextoOrigem = RemoverAcentos(TextoOrigem.Trim().ToUpper());
                while (TextoOrigem.IndexOf("  ") > 0) { TextoOrigem = TextoOrigem.Replace("  ", " "); };
                TextoOrigem = TextoOrigem.Replace("\r", "");
                TextoOrigem = TextoOrigem.Replace("\n", "");
                TextoOrigem = TextoOrigem.Replace("\t", "");
                string AlfabetoPermitido = " ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,:+-*/'\'";
                for (int i = 0; i < TextoOrigem.Length; i++)
                {
                    if (AlfabetoPermitido.IndexOf(TextoOrigem[i].ToString()) > -1)
                    {
                        TextoSaida += TextoOrigem[i].ToString();
                    }
                }
            }
            return TextoSaida;
        }

        public static String SentencaSQLFiltroGenerico(string filterField, string filterOperador, string filterText)
        {
            var SentencaSQL = String.Empty;
            
            // Validar filterField (deve ser nome de coluna válido) - SQL Server
            if (string.IsNullOrWhiteSpace(filterField) || !System.Text.RegularExpressions.Regex.IsMatch(filterField, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                throw new ArgumentException("Nome de campo inválido: " + filterField);
            }
            
            // Escapar filterText para evitar SQL Injection
            string filterTextEscaped = filterText?.Replace("'", "''") ?? string.Empty;
            
            if (filterOperador.Equals("=") || filterOperador.Equals("<>") || filterOperador.Equals(">") || filterOperador.Equals(">=") || filterOperador.Equals("<") || filterOperador.Equals("<="))
            {
                SentencaSQL = "[" + filterField + "] " + filterOperador + " '" + filterTextEscaped + "'";
            }
            else if (filterOperador.Equals("Nulo"))
            {
                SentencaSQL = "[" + filterField + "] IS NULL";
            }
            else if (filterOperador.Equals("Não Nulo"))
            {
                SentencaSQL = "[" + filterField + "] IS NOT NULL";
            }
            else if (filterOperador.Equals("Contém"))
            {
                // Escapar caracteres especiais do LIKE (SQL Server)
                filterTextEscaped = filterTextEscaped.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                SentencaSQL = "[" + filterField + "] LIKE '%" + filterTextEscaped + "%'";
            }
            else if (filterOperador.Equals("Não Contém"))
            {
                // Escapar caracteres especiais do LIKE (SQL Server)
                filterTextEscaped = filterTextEscaped.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                SentencaSQL = "[" + filterField + "] NOT LIKE '%" + filterTextEscaped + "%'";
            }
            return SentencaSQL;
        }

        /// <summary>Normaliza termo de busca inline (nome, descrição): alinhado ao cadastro (<see cref="FormatarTextoSimples"/>).</summary>
        public static string NormalizarTermoBuscaTexto(string texto)
        {
            if (String.IsNullOrWhiteSpace(texto)) return String.Empty;
            return FormatarTextoSimples(texto.Trim());
        }

        /// <summary>PN, NCM, serial, sigla, documento: trim, maiúsculas, sem acento (mantém pontuação).</summary>
        public static string NormalizarTermoBuscaCodigo(string texto)
        {
            if (String.IsNullOrWhiteSpace(texto)) return String.Empty;
            return RemoverAcentos(texto.Trim().ToUpper());
        }

        /// <summary>Padrão SQL LIKE '%termo%' com escape de curingas (% _ [).</summary>
        public static string MontarPadraoLikeContem(string termo)
        {
            if (String.IsNullOrEmpty(termo)) return "%";
            string escaped = termo.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            return "%" + escaped + "%";
        }

        public static bool TryMontarPadraoLikeContemTexto(string termoBusca, out string padraoLike)
        {
            padraoLike = null;
            string termo = NormalizarTermoBuscaTexto(termoBusca);
            if (String.IsNullOrEmpty(termo)) return false;
            padraoLike = MontarPadraoLikeContem(termo);
            return true;
        }

        public static bool TryMontarPadraoLikeContemCodigo(string termoBusca, out string padraoLike)
        {
            padraoLike = null;
            string termo = NormalizarTermoBuscaCodigo(termoBusca);
            if (String.IsNullOrEmpty(termo)) return false;
            padraoLike = MontarPadraoLikeContem(termo);
            return true;
        }

        public static String FormatarStringGenerico(String texto, int tamanho, bool AcrescentarADireita = false, String CaracterAcrescentar = "")
        {
            texto = texto.Trim();
            if (texto.Length > tamanho) { texto = texto.Substring(1, tamanho); };
            int QuantidadeAcrescentar = tamanho - texto.Length;
            if (QuantidadeAcrescentar < 0) { QuantidadeAcrescentar = 0; };
            if (QuantidadeAcrescentar > 0)
            {
                for (int i = 0; i < QuantidadeAcrescentar; i++)
                {
                    if (AcrescentarADireita) { texto = texto + CaracterAcrescentar; } else { texto = CaracterAcrescentar + texto; };
                }
            }
            return texto;
        }

        public static String GetTabHtml(int qtdTabs)
        {
            string texto = string.Empty;
            for (int i = 0; i < qtdTabs; i++)
            {
                texto += "&nbsp&nbsp&nbsp&nbsp";
            }
            return texto;
        }

        public static String GetEspacesHtml(int qtdEspaces)
        {
            string texto = string.Empty;
            for (int i = 0; i < qtdEspaces; i++)
            {
                texto += "&nbsp";
            }
            return texto;
        }
        public static String FormatarMoedaReais(decimal valor)
        {
            return string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:c}", valor).Replace("R$", "R$ ").Replace("  ", " ");
        }

        public static bool IsSomenteAlfabeto(string TextoOrigem)
        {
            bool Resultado = false;
            int TotalCaracteresValidos = 0;
            TextoOrigem = RemoverAcentos(TextoOrigem.Trim().ToUpper());
            string AlfabetoPermitido = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int i = 0; i < TextoOrigem.Length; i++)
            {
                if (AlfabetoPermitido.IndexOf(TextoOrigem[i].ToString()) > -1)
                {
                    TotalCaracteresValidos += 1;
                }
            }

            if (TotalCaracteresValidos == TextoOrigem.Length) { Resultado = true; }
            return Resultado;
        }

        public static String FormatarTextoCadastroNormal(string TextoOrigem)
        {
            String TextoDestino = String.Empty;
            try
            {
                TextoOrigem = TextoOrigem.EmptyIfNull().ToString().Trim();
                TextoOrigem = TextoOrigem.Replace("  ", " ").Replace("  ", " ").Replace("  ", " ");
                TextoOrigem = TextoOrigem.Replace("\r", "");
                TextoOrigem = TextoOrigem.Replace("\n", "");
                TextoOrigem = TextoOrigem.Replace("\t", "");
                string AlfabetoPermitido = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇABCDEFGHIJKLMNOPQRSTUVWXYZäåáâàãäáâàãéêëèéêëèíîïìíîïìöóôòõöóôòõüúûüúûùçabcdefghijklmnopqrstuvwxyz0123456789.,:;+-*/|?!@#$%&()[]{}<> ";
                for (int i = 0; i < TextoOrigem.Length; i++)
                {
                    if (AlfabetoPermitido.IndexOf(TextoOrigem[i].ToString()) > -1)
                    {
                        TextoDestino += TextoOrigem[i].ToString();
                    }
                }
            }
            catch (Exception)
            {
                TextoDestino = TextoOrigem;
            }
            return TextoDestino;
        }

        public static String FormatarTextoSimples(string TextoOrigem)
        {
            String TextoDestino = String.Empty;
            try
            {
                TextoOrigem = TextoOrigem.EmptyIfNull().Trim().ToUpper();
                TextoOrigem = RemoverAcentos(TextoOrigem);
                if (TextoOrigem.IndexOf("  ") >= 0) { TextoOrigem = RemoverEspacosDuplos(TextoOrigem); }
                if (TextoOrigem.IndexOf("\r") >= 0) { TextoOrigem = TextoOrigem.Replace("\r", ""); }
                if (TextoOrigem.IndexOf("\n") >= 0) { TextoOrigem = TextoOrigem.Replace("\n", ""); }
                if (TextoOrigem.IndexOf("\t") >= 0) { TextoOrigem = TextoOrigem.Replace("\t", ""); }
                string AlfabetoPermitido = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-' ";
                for (int i = 0; i < TextoOrigem.Length; i++)
                {
                    if (AlfabetoPermitido.IndexOf(TextoOrigem[i].ToString()) > -1)
                    {
                        TextoDestino += TextoOrigem[i].ToString();
                    }
                }
            }
            catch (Exception)
            {
                TextoDestino = TextoOrigem;
            }
            return TextoDestino;
        }

        public static String FormatarTextoCadastroSimplificado(string TextoOrigem)
        {
            String TextoDestino = String.Empty;
            try
            {
                TextoOrigem = TextoOrigem.Trim().ToUpper();
                TextoOrigem = RemoverAcentos(TextoOrigem);
                if (TextoOrigem.IndexOf("  ") >= 0) { TextoOrigem = RemoverEspacosDuplos(TextoOrigem); }
                if (TextoOrigem.IndexOf("\r") >= 0) { TextoOrigem = TextoOrigem.Replace("\r", ""); }
                if (TextoOrigem.IndexOf("\n") >= 0) { TextoOrigem = TextoOrigem.Replace("\n", ""); }
                if (TextoOrigem.IndexOf("\t") >= 0) { TextoOrigem = TextoOrigem.Replace("\t", ""); }
                string AlfabetoPermitido = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.:()[]{}<> ";
                for (int i = 0; i < TextoOrigem.Length; i++)
                {
                    if (AlfabetoPermitido.IndexOf(TextoOrigem[i].ToString()) > -1)
                    {
                        TextoDestino += TextoOrigem[i].ToString();
                    }
                }
            }
            catch (Exception)
            {
                TextoDestino = TextoOrigem;
            }
            return TextoDestino;
        }

        public static String FormatarTextoCadastroExtendido(string TextoOrigem)
        {
            String TextoDestino = String.Empty;
            try
            {
                TextoOrigem = TextoOrigem.Trim().ToUpper();
                TextoOrigem = RemoverAcentos(TextoOrigem);
                if (TextoOrigem.IndexOf("  ") >= 0) { TextoOrigem = RemoverEspacosDuplos(TextoOrigem); }
                if (TextoOrigem.IndexOf("\r") >= 0) { TextoOrigem = TextoOrigem.Replace("\r", ""); }
                if (TextoOrigem.IndexOf("\n") >= 0) { TextoOrigem = TextoOrigem.Replace("\n", ""); }
                if (TextoOrigem.IndexOf("\t") >= 0) { TextoOrigem = TextoOrigem.Replace("\t", ""); }
                string AlfabetoPermitido = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,:;+-*/|#$&()[]{}<> ";
                for (int i = 0; i < TextoOrigem.Length; i++)
                {
                    if (AlfabetoPermitido.IndexOf(TextoOrigem[i].ToString()) > -1)
                    {
                        TextoDestino += TextoOrigem[i].ToString();
                    }
                }
            }
            catch (Exception)
            {
                TextoDestino = TextoOrigem;
            }
            return TextoDestino;
        }

        public static String RemoverEspacos(string texto)
        {
            if (texto == null) { texto = string.Empty; };
            while (texto.IndexOf(" ") > -1)
            {
                texto = texto.Replace(" ", "");
            }
            return texto;
        }

        public static String RemoverEspacosDuplos(string texto)
        {
            if (texto == null) { texto = string.Empty; };
            while (texto.IndexOf("  ") > -1)
            {
                texto = texto.Replace("  ", " ");
            }
            return texto;
        }

        public static String DecodeHtmlString(string TextoOrigem)
        {
            string TextoRetorno = string.Empty;
            if (TextoOrigem.EmptyIfNull().Length > 0)
            {
                byte[] bytes = Encoding.GetEncoding(1252).GetBytes(TextoOrigem);
                TextoRetorno = Encoding.UTF8.GetString(bytes);
            }
            return TextoRetorno;
        }
        public static String RemoverCaracteresEspeciaisCodificados(string texto)
        {
            if (texto != null)
            {
                String CaracteresEspeciais = "&AACUTE;|&ACIRC;|&AELIG;|&AGRAVE;|&ALEFSYM;|&ALPHA;|&AMP;|&AND;|&ANG;|&ARING;|&ASYMP;|&ATILDE;|&AUML;|&BETA;|&CAP;|&CCEDIL;|&CHI;|&CONG;|&COPY;|&CRARR;|&CUP;|&DEG;|&DELTA;|&DIVIDE;|&EACUTE;|&ECIRC;|&EGRAVE;|&EMPTY;|&EPSILON;|&EQUIV;|&ETA;|&ETH;|&EUML;|&EXIST;|&FORALL;|&FRASL;|&GAMMA;|&GE;|&GT;|&IACUTE;|&ICIRC;|&IGRAVE;|&IMAGE;|&INFIN;|&INT;|&IOTA;|&ISIN;|&IUML;|&KAPPA;|&LAMBDA;|&LCEIL;|&LE;|&LFLOOR;|&LOWAST;|&LOZ;|&LT;|&MICRO;|&MIDDOT;|&MINUS;|&MU;|&NABLA;|&NE;|&NOT;|&NOTIN;|&NTILDE;|&NU;|&OACUTE;|&OCIRC;|&OGRAVE;|&OMEGA;|&OMICRON;|&OPLUS;|&OR;|&OSLASH;|&OTILDE;|&OTIMES;|&OUML;|&PART;|&PERMIL;|&PERP;|&PHI;|&PI;|&PIV;|&PLUSMN;|&PRIME;|&PROD;|&PROP;|&PSI;|&QUOT;|&RADIC;|&RCEIL;|&REAL;|&REG;|&RFLOOR;|&RHO;|&SDOT;|&SIGMA;|&SUB;|&SUBE;|&SUM;|&SUP;|&SUPE;|&SZLIG;|&TAU;|&THERE4;|&THETA;|&THETASYM;|&THORN;|&TIMES;|&UACUTE;|&UCIRC;|&UGRAVE;|&UPSIH;|&UPSILON;|&UUML;|&XI;|&YACUTE;|&ZETA;|&#039;|&LT;|";
                String[] ListaCaracteresEspeciais = CaracteresEspeciais.Split('|');

                foreach (string CaracterEspecial in ListaCaracteresEspeciais)
                {
                    if (CaracterEspecial.EmptyIfNull().ToString().Length > 0)
                    {
                        if (texto.IndexOf(CaracterEspecial) > -1)
                        {
                            texto = texto.Replace(CaracterEspecial, " ");
                        }
                    }
                }
            }
            return texto;
        }

        public static string FormatarXML(string TextoOrigem)
        {
            String TextoDestino = String.Empty;
            try
            {
                TextoDestino = Regex.Replace(TextoOrigem, @"[^\u0020-\u007E\u00A0-\u00FF\s]", string.Empty);
                if (TextoDestino.IndexOf("\t") >= 0) { TextoDestino = TextoDestino.Replace("\t", ""); }
                if (TextoDestino.IndexOf("  ") >= 0) { TextoDestino = RemoverEspacosDuplos(TextoDestino); }
                TextoDestino = RemoverHexadecimal(TextoDestino);
                TextoDestino = RemoverCaracteresEspeciaisCodificados(TextoDestino);
            }
            catch (Exception)
            {
                TextoDestino = TextoOrigem;
            }
            return TextoDestino;
        }

        public static string RemoverHexadecimal(string inString)
        {
            if (inString == null) return null;
            StringBuilder newString = new StringBuilder();
            char ch;
            for (int i = 0; i < inString.Length; i++)
            {
                ch = inString[i];
                if (XmlConvert.IsXmlChar(ch)) { newString.Append(ch); }
            }
            return Regex.Replace(newString.ToString(), @"[\p{c}-[\t\r\n]]+", "");
        }

        public static string GetNomeAbreviado(string NomeOrigem)
        {
            var meio = " ";
            var nomes = NomeOrigem.Split(' ');
            for (var i = 1; i < nomes.Length - 1; i++)
            {
                if (!nomes[i].Equals("de", StringComparison.OrdinalIgnoreCase) &&
                    !nomes[i].Equals("da", StringComparison.OrdinalIgnoreCase) &&
                    !nomes[i].Equals("do", StringComparison.OrdinalIgnoreCase) &&
                    !nomes[i].Equals("das", StringComparison.OrdinalIgnoreCase) &&
                    !nomes[i].Equals("dos", StringComparison.OrdinalIgnoreCase))
                    meio += nomes[i][0] + ". ";
            }
            return nomes[0] + meio + nomes[nomes.Length - 1];
        }

        public static string GetPrimeiroNome(string NomeOrigem)
        {
            String NomeAbreviado = String.Empty;
            var Nomes = NomeOrigem.Split(' ');
            if (Nomes.Length > 0) { NomeAbreviado = Nomes[0].Trim(); } else { NomeAbreviado = NomeOrigem; };
            return NomeAbreviado;
        }

        public static string GetRazaoSocialAbreviada(string RazaoSocialOrigem)
        {
            var nomes = RazaoSocialOrigem.Split(' ');
            String RazaoSocialAbreviada = String.Empty;
            if (nomes.Length > 0) { RazaoSocialAbreviada += nomes[0].Trim(); };
            if (nomes.Length > 1)
            {
                RazaoSocialAbreviada += " " + nomes[1].Trim();
                if ((nomes[1].EmptyIfNull().Trim().Length <= 3) && (nomes.Length > 2)) { RazaoSocialAbreviada += " " + nomes[2].Trim(); };
            };
            if (RazaoSocialAbreviada.Trim().Length == 0) { RazaoSocialAbreviada = RazaoSocialOrigem; };
            return RazaoSocialAbreviada;
        }

        #region Formatações Específicas GDI
        public static String GDIFormatarCodigoProduto(string CodigoOriginal)
        {
            String CodigoFormatado = String.Empty;
            try
            {
                CodigoOriginal = RemoverCaracteresEspeciaisCodificados(CodigoOriginal);
                CodigoOriginal = CodigoOriginal.Replace(" , ", ", ").Replace(" ; ", "; ").Replace("?", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\"", "").Replace("'", "");
                CodigoOriginal = RemoverAcentos(CodigoOriginal);
                CodigoOriginal = CodigoOriginal.EmptyIfNull().ToString().Trim();
                CodigoOriginal = CodigoOriginal.ToUpperInvariant();
                string AlfabetoPermitido = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789&/-_.";
                for (int i = 0; i < CodigoOriginal.Length; i++)
                {
                    if (AlfabetoPermitido.IndexOf(CodigoOriginal[i].ToString()) > -1)
                    {
                        CodigoFormatado += CodigoOriginal[i].ToString();
                    }
                }
                CodigoFormatado = CodigoFormatado.Trim();
                if (CodigoFormatado.EndsWith(".") || CodigoFormatado.EndsWith("-") || CodigoFormatado.EndsWith(";")) { CodigoFormatado = CodigoFormatado.Substring(0, CodigoFormatado.Length - 1); };
                if (CodigoFormatado.StartsWith(".") || CodigoFormatado.StartsWith("-") || CodigoFormatado.StartsWith(";")) { CodigoFormatado = CodigoFormatado.Substring(1); };
            }
            catch (Exception)
            {
                CodigoFormatado = CodigoOriginal;
            }
            return CodigoFormatado;
        }


        public static String GDIFormatarDescricaoProduto(string DescricaoProduto)
        {
            try
            {
                String DescricaoTemp = String.Empty;
                DescricaoProduto = RemoverCaracteresEspeciaisCodificados(DescricaoProduto);
                DescricaoProduto = DescricaoProduto.Replace(" , ", ", ").Replace(" ; ", "; ").Replace("?", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\"", "").Replace("'", "");
                DescricaoProduto = RemoverAcentos(DescricaoProduto);
                DescricaoProduto = DescricaoProduto.EmptyIfNull().ToString().Trim();
                DescricaoProduto = DescricaoProduto.ToUpperInvariant();
                string AlfabetoPermitido = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:+*%&/;,-_()[]. ";
                for (int i = 0; i < DescricaoProduto.Length; i++)
                {
                    if (AlfabetoPermitido.IndexOf(DescricaoProduto[i].ToString()) > -1)
                    {
                        DescricaoTemp += DescricaoProduto[i].ToString();
                    }

                    if (i == (DescricaoProduto.Length - 1)) { DescricaoProduto = DescricaoTemp; };

                }
                DescricaoProduto = DescricaoProduto.Replace("- -", "-").Replace("--", "-");

                int PosSerial = DescricaoProduto.IndexOf("SERIAL:");
                if (PosSerial > 0) { DescricaoProduto = DescricaoProduto.Substring(0, PosSerial-1); };

                if (DescricaoProduto.EndsWith(".") || DescricaoProduto.EndsWith("-") || DescricaoProduto.EndsWith(";")) { DescricaoProduto = DescricaoProduto.Substring(0, DescricaoProduto.Length - 1); };
                if (DescricaoProduto.StartsWith(".") || DescricaoProduto.StartsWith("-") || DescricaoProduto.StartsWith(";")) { DescricaoProduto = DescricaoProduto.Substring(1); };
                if (!DescricaoProduto.StartsWith("PN:")) { DescricaoProduto = "PN:" + DescricaoProduto.Replace("PN:", "");  };
                DescricaoProduto = DescricaoProduto.Trim();
            }
            catch (Exception)
            {
            }
            return DescricaoProduto;
        }

        public static String GDIFormatarDescricaoProdutoImportadoSemPN(string TextoOrigem)
        {
            // Não pode remover espaços duplos, o PN é separado da descrição pelo espaço duplo
            
            int IndexPosicao = -1;
            int TamanhoMaximo = 250;
            String SiglaTemp = string.Empty;
            String NomeTemp = string.Empty;
            TextoOrigem = TextoOrigem.EmptyIfNull().ToString().Trim();
            if (TextoOrigem.StartsWith("PN:")) { TextoOrigem = TextoOrigem.Substring(3); };
            if (TextoOrigem.StartsWith("PN")) { TextoOrigem = TextoOrigem.Substring(2); };
            TextoOrigem = GDIFormatarDescricaoProduto(TextoOrigem);
            IndexPosicao = TextoOrigem.IndexOf("  "); // Espaço Duplo na SC Southern Cross
            if (TextoOrigem.StartsWith("PN:")) { SiglaTemp = TextoOrigem.Substring(0, IndexPosicao).Trim(); } else { SiglaTemp = "PN:" + TextoOrigem.Substring(0, IndexPosicao).Trim(); }
            if (SiglaTemp.EndsWith(".") || SiglaTemp.EndsWith("-") || SiglaTemp.EndsWith(";")) { SiglaTemp = SiglaTemp.Substring(0, SiglaTemp.Length - 1); };
            if (SiglaTemp.StartsWith(".") || SiglaTemp.StartsWith("-") || SiglaTemp.StartsWith(";")) { SiglaTemp = SiglaTemp.Substring(1); };
            NomeTemp = LibStringFormat.RemoverEspacosDuplos(TextoOrigem.Substring(IndexPosicao).Trim());
            if (NomeTemp.EndsWith(".") || NomeTemp.EndsWith("-") || NomeTemp.EndsWith(";")) { NomeTemp = NomeTemp.Substring(0, SiglaTemp.Length - 1); };
            if (NomeTemp.StartsWith(".") || NomeTemp.StartsWith("-") || NomeTemp.StartsWith(";")) { NomeTemp = NomeTemp.Substring(1); };
            TextoOrigem = SiglaTemp.Trim() + " - " + NomeTemp.Trim();
            if (TextoOrigem.Length > TamanhoMaximo) { TextoOrigem = TextoOrigem.Substring(0, TamanhoMaximo); };
            return TextoOrigem;
        }
        public static String GDIFormatarDescricaoProdutoTraduzidoComPN(string TextoOrigem)
        {
            int PosicaoInicial = -1;
            int PosicaoFinal = -1;
            int TamanhoMaximo = 250;
            String SiglaTemp = string.Empty;
            String NomeTemp = string.Empty;
            TextoOrigem = GDIFormatarDescricaoProduto(TextoOrigem);
            TextoOrigem = RemoverEspacosDuplos(TextoOrigem);
            TextoOrigem = RemoverCaracteresEspeciaisCodificados(TextoOrigem);
            if (TextoOrigem.IndexOf("ITEM REVISADO") > 0)
            {
                try
                {
                    PosicaoInicial = -1;
                    PosicaoFinal = -1;
                    PosicaoInicial = TextoOrigem.IndexOf("ITEM REVISADO");
                    PosicaoFinal = TextoOrigem.IndexOf(";", PosicaoInicial);
                    if ((PosicaoInicial > 0) && (PosicaoFinal > 0))
                    {
                        TextoOrigem = TextoOrigem.Substring(0, PosicaoInicial - 1) + TextoOrigem.Substring(PosicaoFinal + 1);
                    }
                }
                catch (Exception) { };
            }


            // Correções
            List<string> ListaCorrecao = new List<string>();
            ListaCorrecao.Add("APLICACAO ASA F" + ";" + "APLICACAO ASA FIXA");
            ListaCorrecao.Add("APLICACAO ASA FI" + ";" + "APLICACAO ASA FIXA");
            ListaCorrecao.Add("APLICACAO ASA FIX" + ";" + "APLICACAO ASA FIXA");
            ListaCorrecao.Add("APLICACAO ASA R" + ";" + "APLICACAO ASA ROTATIVA");
            ListaCorrecao.Add("APLICACAO ASA RO" + ";" + "APLICACAO ASA ROTATIVA");
            ListaCorrecao.Add("APLICACAO ASA ROT" + ";" + "APLICACAO ASA ROTATIVA");
            ListaCorrecao.Add("APLICACAO ASA ROTA" + ";" + "APLICACAO ASA ROTATIVA");
            ListaCorrecao.Add("APLICACAO ASA ROTAT" + ";" + "APLICACAO ASA ROTATIVA");
            ListaCorrecao.Add("APLICACAO ASA ROTATI" + ";" + "APLICACAO ASA ROTATIVA");
            ListaCorrecao.Add("APLICACAO ASA ROTATIV" + ";" + "APLICACAO ASA ROTATIVA");
            ListaCorrecao.Add("INDUSTRIA A" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AE" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AER" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AERO" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AERON" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AERONA" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AERONAU" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AERONAUT" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AERONAUTI" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("INDUSTRIA AERONAUTIC" + ";" + "INDUSTRIA AERONAUTICA");
            ListaCorrecao.Add("APLICACAO ASA F" + ";" + "APLICACAO ASA FIXA");
            ListaCorrecao.Add("APLICACAO ASA FI" + ";" + "APLICACAO ASA FIXA");
            ListaCorrecao.Add("APLICACAO ASA FIX" + ";" + "APLICACAO ASA FIXA");
            ListaCorrecao.Add("MATERIAL DE USO A" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AE" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AER" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AERO" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AERON" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AERONA" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AERONAU" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AERONAUT" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AERONAUTI" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("MATERIAL DE USO AERONAUTIC" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaCorrecao.Add("DE ACORDO COM AS NORMAS INTERN" + ";" + "DE ACORDO COM AS NORMAS INTERNACIONAIS");
            ListaCorrecao.Add("" + ";" + "");
            foreach (string TextoCorrecao in ListaCorrecao)
            {
                int Posicao = TextoCorrecao.IndexOf(";");
                if (Posicao > 0)
                {
                    String TextoCorrigir = TextoCorrecao.Substring(0, Posicao);
                    String TextoCorrigido = TextoCorrecao.Substring(Posicao + 1);
                    if ((TextoOrigem.IndexOf(TextoCorrigir) >= 0) && (TextoOrigem.IndexOf(TextoCorrigido) < 0))
                    {
                        TextoOrigem = TextoOrigem.Replace(TextoCorrigir, TextoCorrigido);
                    }
                }
            }

            // Correções
            List<string> ListaSubstituicao = new List<string>();
            ListaSubstituicao.Add("AERONAUTICA / AEROESPACIAL" + ";" + "AERONAUTICA / AEROESPACIAL");
            ListaSubstituicao.Add("AERONAUTICA/ AEROESPACIAL" + ";" + "AERONAUTICA / AEROESPACIAL");
            ListaSubstituicao.Add("AERONAUTICA /AEROESPACIAL" + ";" + "AERONAUTICA / AEROESPACIAL");
            ListaSubstituicao.Add("APLICACAO ASA FIXA." + ";" + "APLICACAO ASA FIXA;");
            ListaSubstituicao.Add("COMPONETES" + ";" + "COMPONENTES");
            ListaSubstituicao.Add("ITEM REVISADO/USADO (OH)" + ";" + "");
            ListaSubstituicao.Add("ITEM REVISADO/USADO" + ";" + "");
            ListaSubstituicao.Add("ITEM REVISADO/USAD" + ";" + "");
            ListaSubstituicao.Add("ITEM REVISADO/USA" + ";" + "");
            ListaSubstituicao.Add("ITEM REVISADO/US" + ";" + "");
            ListaSubstituicao.Add("ITEM REVISADO/U" + ";" + "");
            ListaSubstituicao.Add("ITEM REVISADO/" + ";" + "");
            ListaSubstituicao.Add("ITEM REVISADO" + ";" + "");
            ListaSubstituicao.Add("COMPONENTES PARA AERONAVES" + ";" + "MATERIAL DE USO AERONAUTICO");
            ListaSubstituicao.Add("USOAERONAUTICO" + ";" + "USO AERONAUTICO");
            ListaSubstituicao.Add("APLICACAO ASA FIXA." + ";" + "APLICACAO ASA FIXA");
            ListaSubstituicao.Add("- -" + ";" + "-");
            ListaSubstituicao.Add("FIXA .ROTATIVA" + ";" + "FIXA / ROTATIVA");
            ListaSubstituicao.Add("FIXA /. ROTATIVA" + ";" + "FIXA / ROTATIVA");
            ListaSubstituicao.Add("FIXA .ROTATIVA" + ";" + "FIXA / ROTATIVA");
            ListaSubstituicao.Add("FIXA. /. ROTATIVA" + ";" + "FIXA. / ROTATIVA");
            ListaSubstituicao.Add("DEUSO" + ";" + "DE USO");
            ListaSubstituicao.Add("ACORDOCOM" + ";" + "ACORDO COM");
            ListaSubstituicao.Add("PARA UO" + ";" + "PARA USO");
            foreach (string TextoSubstituicao in ListaSubstituicao)
            {
                int Posicao = TextoSubstituicao.IndexOf(";");
                if (Posicao > 0)
                {
                    String TextoCorrigir = TextoSubstituicao.Substring(0, Posicao);
                    String TextoCorrigido = TextoSubstituicao.Substring(Posicao + 1);

                    if (TextoOrigem.IndexOf(TextoCorrigir) >= 0)
                    {
                        TextoOrigem = TextoOrigem.Replace(TextoCorrigir, TextoCorrigido);
                    }
                }
            }
            if (TextoOrigem.Length > TamanhoMaximo) { TextoOrigem = TextoOrigem.Substring(0, TamanhoMaximo); };
            return TextoOrigem;
        }

        public static String GDIFormatarCodigoAuxiliarProduto(string CodigoAuxiliar)
        {
            String CodigoAuxiliarFormatado = String.Empty;
            try
            {
                CodigoAuxiliarFormatado = CodigoAuxiliar;
                CodigoAuxiliarFormatado = CodigoAuxiliarFormatado.EmptyIfNull().ToString().Trim();
                CodigoAuxiliarFormatado = CodigoAuxiliarFormatado.Replace(" , ", ", ").Replace(" ; ", "; ").Replace("?", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("\"", "").Replace("'", "");
                CodigoAuxiliarFormatado = CodigoAuxiliarFormatado.ToUpperInvariant();
                CodigoAuxiliarFormatado = RemoverAcentos(CodigoAuxiliarFormatado);
                CodigoAuxiliarFormatado = SomenteAlfabetoeNumeros(CodigoAuxiliarFormatado);
                CodigoAuxiliarFormatado = RemoverEspacos(CodigoAuxiliarFormatado);
                CodigoAuxiliarFormatado = CodigoAuxiliarFormatado.EmptyIfNull().ToString().Trim();
            }
            catch (Exception)
            {
                CodigoAuxiliarFormatado = CodigoAuxiliar; ;
            }
            return CodigoAuxiliarFormatado;
        }

        /*public static String GDIGetCodigoAuxiliarCuringa(String CodigoAuxiliar)
        {
            CodigoAuxiliar = CodigoAuxiliar.Trim();
            int SizeCodigo = CodigoAuxiliar.Length;
            String CodigoCuringa = string.Empty;
            int Slice = 2;

            if (SizeCodigo >= 7) { Slice = 3; }
            else if (SizeCodigo >= 10) { Slice = 4; }
            else if (SizeCodigo >= 13) { Slice = 5; };

            CodigoCuringa = CodigoAuxiliar.Substring(0, CodigoAuxiliar.Length - Slice);
            for (int x = 0; x < Slice; x++)
            {
                CodigoCuringa += "_";
            }
            return CodigoCuringa;
        }*/



        public static String ClienteGDIGetPartNumber(string DescricaoProduto)
        {
            int PosIndex = 0;
            String PartNumber = string.Empty;

            if (DescricaoProduto.StartsWith("PN:")) { DescricaoProduto = DescricaoProduto.Substring(3); };
            if (DescricaoProduto.StartsWith("PN")) { DescricaoProduto = DescricaoProduto.Substring(2); };

            if (DescricaoProduto.IndexOf(" - ") > 0) 
            {
                PosIndex = DescricaoProduto.IndexOf(" - ");
                PartNumber = DescricaoProduto.Substring(0, PosIndex); 
            }
            else if (DescricaoProduto.IndexOf("  ") > 0)
            {
                PosIndex = DescricaoProduto.IndexOf("  ");
                PartNumber = DescricaoProduto.Substring(0, PosIndex);
            }
            else if (DescricaoProduto.IndexOf(" ") > 0)
            {
                PosIndex = DescricaoProduto.IndexOf(" ");
                if (PosIndex > 0) { PartNumber = DescricaoProduto.Substring(0, PosIndex); }
            }
            return PartNumber;
        }

        public static String FormatarLinhaDigitavel(string LinhaDigitavelOriginal)
        {
            String Resultado = string.Empty;

            if (LinhaDigitavelOriginal.Length == 47)
            {
                Resultado += LinhaDigitavelOriginal.Substring(0, 5) + "." + LinhaDigitavelOriginal.Substring(5, 5) + " ";
                Resultado += LinhaDigitavelOriginal.Substring(10, 5) + "." + LinhaDigitavelOriginal.Substring(15, 5) + " ";
                Resultado += LinhaDigitavelOriginal.Substring(20, 5) + "." + LinhaDigitavelOriginal.Substring(25, 5) + " ";
                Resultado += LinhaDigitavelOriginal.Substring(30, 1) + " " + LinhaDigitavelOriginal.Substring(31);
            }
            else
            {
                Resultado = LinhaDigitavelOriginal;
            }

            return Resultado;
        }


        #endregion
    }
}