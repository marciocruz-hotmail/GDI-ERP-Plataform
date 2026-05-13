using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GdiPlataform.Lib;

namespace GdiPlataform.Areas.g.Lib
{
    public static class LibFinanceiroBoletos
    {
        public static String CalcularAgenciaCodigoCedente(String banco, String agencia, String dv_agencia, String conta, String dv_conta, String codigo_convenio)
        {
            String resultado = string.Empty;
            if (banco == "033") // Modelo Teste - Falta Homologação
            {
                resultado = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0");
                resultado += codigo_convenio.EmptyIfNull().ToString();
            }
            else if (banco == "237") // Itaú
            {
                resultado = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0");
                resultado += "/";
                resultado += conta;
                resultado += "-";
                resultado += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(dv_conta, 1, false, "0");
            }
            else if (banco == "341") // Itaú
            {
                resultado = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0");
                resultado += "/";
                resultado += conta;
                resultado += "-";
                resultado += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(dv_conta, 1, false, "0");
            }
            else if (banco == "756") // Bancoob
            {
                resultado = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0");
                resultado += "/";
                resultado += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(conta, 5, false, "0"); ;
                resultado += "-";
                resultado += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(dv_conta, 1, false, "0");
            }
            return resultado;
        }

        public static String CalcularCodigoBarras(String banco, string agencia, string conta, string carteira, string codigo_empresa, int id_financeiro, String inicial_nossonumero, DateTime data_vencimento, decimal valor_total)
        {
            String CodigoBarrasCompleto = string.Empty;
            String codigoMoeda = "9";
            String ENossoNumero = string.Empty;
            String FV = CalcularFatorVencimento(data_vencimento);
            String CodigoBarras = String.Empty;
            String valorFormatado = (valor_total).ToString("n2").Replace(".", "").Replace(",", "");
            if ((inicial_nossonumero == null) || (inicial_nossonumero == string.Empty)) { inicial_nossonumero = "0"; };
            //if (carteira.Length > 1) { carteira = carteira.Substring(1, 1); };

            if (banco == "033") // Santander
            {
                ENossoNumero = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico((inicial_nossonumero + id_financeiro).ToString(), 12, false, "0");
                ENossoNumero = ENossoNumero + CalcularMod11(ENossoNumero);
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(banco, 3, false, "0");
                CodigoBarras += codigoMoeda;
                CodigoBarras += FV;
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(valorFormatado, 10, false, "0");
                CodigoBarras += "9";
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(codigo_empresa, 7, false, "0");
                CodigoBarras += ENossoNumero;
                CodigoBarras += "0";
                CodigoBarras += "101"; // Tipo de Modalidade Carteira | 101 - Cobrança Rápida COM Registro
                CodigoBarrasCompleto = CalcularCodigoBarrasComDigito(banco, CodigoBarras);
            }
            else if (banco == "341")
            {
                ENossoNumero = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico((inicial_nossonumero + id_financeiro).ToString(), 8, false, "0");
                String Dac1 = CalcularMod10(GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0") + GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(conta, 5, false, "0") + GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(carteira, 3, false, "0") + ENossoNumero);
                String Dac2 = CalcularMod10(GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0") + GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(conta, 5, false, "0"));
                if (Dac2 == "10") { Dac2 = "0"; };

                CodigoBarras = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(banco, 3, false, "0");
                CodigoBarras += codigoMoeda;
                CodigoBarras += FV;
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(valorFormatado, 10, false, "0");
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(carteira, 3, false, "0");
                CodigoBarras += ENossoNumero;
                CodigoBarras += Dac1;
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0");
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(conta, 5, false, "0");
                CodigoBarras += Dac2; // Dac Conta
                CodigoBarras += "000"; // Livre
                CodigoBarrasCompleto = CalcularCodigoBarrasComDigito(banco, CodigoBarras);
            }
            else if (banco == "756") // Bancoob
            {
                // Modelo 1 //
                ENossoNumero = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico((inicial_nossonumero + id_financeiro).ToString(), 7, false, "0");
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(banco, 3, false, "0");
                CodigoBarras += codigoMoeda;
                CodigoBarras += FV;
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(valorFormatado, 10, false, "0");
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(carteira, 1, false, "0");
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0");
                CodigoBarras += "01"; // Modalidade
                CodigoBarras += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(codigo_empresa, 7, false, "0");
                CodigoBarras += ENossoNumero;
                CodigoBarras += CalcularDigitoNossoNumero(banco, agencia, conta, carteira, ENossoNumero, codigo_empresa);
                CodigoBarras += "001"; // Parcela
                CodigoBarrasCompleto = CalcularCodigoBarrasComDigito(banco, CodigoBarras);
            }

            return CodigoBarrasCompleto;
        }

        public static String CalcularNossoNumeroComDV(String banco, string agencia, string conta, string carteira, int id_financeiro, string inicial_nossonumero, string codigo_empresa)
        {
            String ENossoNumero = string.Empty;
            if ((inicial_nossonumero == null) || (inicial_nossonumero == string.Empty)) { inicial_nossonumero = "0"; };
            if (banco == "033") // Santander - Teste - Falta Homologação
            {
                int Soma = int.Parse(inicial_nossonumero) + id_financeiro;
                ENossoNumero = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(Soma.ToString(), 7, false, "0");
                ENossoNumero = ENossoNumero + "-" + CalcularDigitoNossoNumero(banco, agencia, conta, carteira, ENossoNumero, codigo_empresa);
            }
            else if (banco == "341") // Bancoob
            {
                int Soma = int.Parse(inicial_nossonumero) + id_financeiro;
                ENossoNumero = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(Soma.ToString(), 7, false, "0");
                ENossoNumero = ENossoNumero + "-" + CalcularDigitoNossoNumero(banco, agencia, conta, carteira, ENossoNumero, codigo_empresa);
            }
            else if (banco == "756") // Bancoob
            {
                int Soma = int.Parse(inicial_nossonumero) + id_financeiro;
                ENossoNumero = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(Soma.ToString(), 7, false, "0");
                ENossoNumero = ENossoNumero + "-" + CalcularDigitoNossoNumero(banco, agencia, conta, carteira, ENossoNumero, codigo_empresa);
            }
            return ENossoNumero;
        }

        public static String CalcularCodigoBarrasComDigito(String banco, String codigo_barras)
        {
            String CodigoBarrasCompleto = string.Empty;
            int subsoma = 0;
            int soma = 0;
            String constante = "4329876543298765432987654329876543298765432";
            if ((banco.Equals("033")) && (codigo_barras.Length == 43)) // Santander - Teste - Falta Homologação
            {
                for (int x = 0; x < 43; x++)
                {
                    subsoma = int.Parse(codigo_barras[x].ToString()) * int.Parse(constante[x].ToString());
                    soma += subsoma;
                }
                int digito = 11 - (soma % 11);
                if ((digito <= 1) || (digito > 9)) { digito = 1; }
                CodigoBarrasCompleto = codigo_barras.Substring(0, 4) + digito.ToString() + codigo_barras.Substring(4, 39);
            }
            else if ((banco.Equals("341")) && (codigo_barras.Length == 43)) // Santander - Teste - Falta Homologação
            {
                for (int x = 0; x < 43; x++)
                {
                    subsoma = int.Parse(codigo_barras[x].ToString()) * int.Parse(constante[x].ToString());
                    soma += subsoma;
                }
                int digito = 11 - (soma % 11);
                if ((digito <= 1) || (digito > 9)) { digito = 1; }
                CodigoBarrasCompleto = codigo_barras.Substring(0, 4) + digito.ToString() + codigo_barras.Substring(4, 39);
            }
            else if ((banco.Equals("756")) && (codigo_barras.Length == 43)) // Sicoob
            {
                for (int x = 0; x < 43; x++)
                {
                    subsoma = int.Parse(codigo_barras[x].ToString()) * int.Parse(constante[x].ToString());
                    soma += subsoma;
                }
                int digito = 11 - (soma % 11);
                if ((digito <= 1) || (digito > 9)) { digito = 1; }
                CodigoBarrasCompleto = codigo_barras.Substring(0, 4) + digito.ToString() + codigo_barras.Substring(4, 39);
            }
            return CodigoBarrasCompleto;
        }


        public static String CalcularDigitoNossoNumero(String banco, string agencia, string conta, string carteira, String ENossoNumero, String codigo_empresa)
        {
            String EDigitoNossoNumero = string.Empty;
            if (banco == "033") // Santander - Teste - Falta Homologação
            {
                try
                {
                    EDigitoNossoNumero = "3027";
                    EDigitoNossoNumero = EDigitoNossoNumero + GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(codigo_empresa, 10, false, "0");
                    EDigitoNossoNumero = EDigitoNossoNumero + GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(ENossoNumero, 7, false, "0");
                    EDigitoNossoNumero = CalcularDigitoNossoNumero756(EDigitoNossoNumero);
                }
                catch (Exception)
                {
                    EDigitoNossoNumero = "E";
                }
            }
            else if (banco == "341") // Itaú - Falta Homologação
            {
                try
                {
                    EDigitoNossoNumero = GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(agencia, 4, false, "0");
                    EDigitoNossoNumero += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(conta, 5, false, "0");
                    EDigitoNossoNumero += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(carteira, 3, false, "0");
                    EDigitoNossoNumero += GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(ENossoNumero, 8, false, "0");
                    EDigitoNossoNumero = CalcularMod10(EDigitoNossoNumero);
                }
                catch (Exception)
                {
                    EDigitoNossoNumero = "E";
                }
            }
            else if (banco == "756") // Bancoob
            {
                try
                {
                    EDigitoNossoNumero = "3027";
                    EDigitoNossoNumero = EDigitoNossoNumero + GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(codigo_empresa, 10, false, "0");
                    EDigitoNossoNumero = EDigitoNossoNumero + GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(ENossoNumero, 7, false, "0");
                    EDigitoNossoNumero = CalcularDigitoNossoNumero756(EDigitoNossoNumero);
                }
                catch (Exception)
                {
                    EDigitoNossoNumero = "E";
                }
            }
            return EDigitoNossoNumero;
        }

        public static String CalcularDigitoNossoNumero756(String Valor)
        {
            int subsoma = 0;
            int soma = 0;
            int ponteiro = 0;
            int digitoVerificador = 0;
            String constante = "3197";
            for (int x = 0; x < Valor.Length; x++)
            {
                subsoma = int.Parse(Valor[x].ToString()) * int.Parse(constante[ponteiro].ToString());
                soma += subsoma;
                if (ponteiro < 3) { ponteiro += 1; } else { ponteiro = 0; };
            }
            int resto = soma % 11;
            if ((resto == 0) || (resto == 1))
            {
                digitoVerificador = 0;
            }
            else
            {
                digitoVerificador = 11 - resto;
            }
            return digitoVerificador.ToString();
        }

        public static String CalcularLinhaDigitavel(String banco, String ECodigoBarras)
        {
            String linhaDigitavel = string.Empty;

            if (ECodigoBarras.Length != 44)
            {
                linhaDigitavel = "0.0.0.0";
            }
            else if (banco == "033") // Santander
            {
                String p1, p2, p3, p4, p5, p6, p7;
                String Campo1, Campo2, Campo3, Campo4, Campo5;

                p1 = ECodigoBarras.Substring(0, 4);
                p2 = "9";
                p3 = ECodigoBarras.Substring(20, 4);
                p4 = CalcularMod10(p1 + p2 + p3); // Conferir
                p5 = p1 + p2 + p3 + p4;
                p6 = p4.Substring(0, 5);
                p7 = p4.Substring(5, 5);
                Campo1 = p5 + '.' + p6;

                p1 = ECodigoBarras.Substring(24, 10);
                p2 = CalcularMod10(p1);
                p3 = p1 + p2;
                p4 = p3.Substring(0, 5);
                p5 = p3.Substring(5, 6);
                Campo2 = p4 + '.' + p5;

                p1 = ECodigoBarras.Substring(34, 10);
                p2 = CalcularMod10(p1); // conferir
                p3 = p1 + p2;
                p4 = p3.Substring(0, 5);
                p5 = p3.Substring(5, 6);
                Campo3 = p4 + '.' + p5;

                Campo4 = ECodigoBarras.Substring(4, 1);

                Campo5 = ECodigoBarras.Substring(5, 14);

                linhaDigitavel = Campo1 + ' ' + Campo2 + ' ' + Campo3 + ' ' + Campo4 + ' ' + Campo5;
            }
            else if ((banco == "341") || (banco == "756")) // Itaú e Bancoob
            {
                String p1, p2, p3, p4, p5, p6;
                String Campo1, Campo2, Campo3, Campo4, Campo5;

                p1 = ECodigoBarras.Substring(0, 4);
                p2 = ECodigoBarras.Substring(19, 5);
                p3 = CalcularMod10Exclusivo756(p1 + p2); // Conferir
                p4 = p1 + p2 + p3;
                p5 = p4.Substring(0, 5);
                p6 = p4.Substring(5, 5);
                Campo1 = p5 + '.' + p6;

                p1 = ECodigoBarras.Substring(24, 10);
                p2 = CalcularMod10Exclusivo756(p1); // conferir
                p3 = p1 + p2;
                p4 = p3.Substring(0, 5);
                p5 = p3.Substring(5, 6);
                Campo2 = p4 + '.' + p5;

                p1 = ECodigoBarras.Substring(34, 10);
                p2 = CalcularMod10Exclusivo756(p1); // conferir
                p3 = p1 + p2;
                p4 = p3.Substring(0, 5);
                p5 = p3.Substring(5, 6);
                Campo3 = p4 + '.' + p5;

                Campo4 = ECodigoBarras.Substring(4, 1);

                Campo5 = ECodigoBarras.Substring(5, 14);

                linhaDigitavel = Campo1 + ' ' + Campo2 + ' ' + Campo3 + ' ' + Campo4 + ' ' + Campo5;
            }
            return linhaDigitavel;
        }


        public static string CalcularFatorVencimento(DateTime dataVencimento)
        {
            int diferencaDias = Convert.ToInt32((dataVencimento - Convert.ToDateTime("07/10/1997")).TotalDays);
            return GdiPlataform.Lib.LibStringFormat.FormatarStringGenerico(diferencaDias.ToString(), 4, false, "0");
        }

        public static string CalcularMod10(String Valor)
        {
            int Somatorio = 0;
            int Resto = 0;
            int Peso = 2;
            int Multiplicacao = 0;
            int Contador = 0;
            for (Contador = Valor.Length - 1; Contador >= 0; Contador--)
            {
                Multiplicacao = int.Parse(Valor[Contador].ToString()) * Peso;
                if (Multiplicacao > 9) { Multiplicacao = int.Parse(Multiplicacao.ToString().Substring(0, 1)) + int.Parse(Multiplicacao.ToString().Substring(1, 1)); };
                Somatorio += Multiplicacao;
                if (Peso == 1) { Peso = 2; } else { Peso = 1; };
            }
            Resto = (Somatorio % 10);
            int digito = (10 - Resto);
            if (digito > 9) { digito = 0; };
            return digito.ToString();
        }

        public static string CalcularMod10Exclusivo756(String Valor)
        {
            int Somatorio = 0;
            int AuxiliarDezenaSuperior = 0;
            int Peso = 2;
            int Multiplicacao = 0;
            int Contador = 0;
            for (Contador = Valor.Length - 1; Contador >= 0; Contador--)
            {
                Multiplicacao = int.Parse(Valor[Contador].ToString()) * Peso;
                if (Multiplicacao > 9) { Multiplicacao = int.Parse(Multiplicacao.ToString().Substring(0, 1)) + int.Parse(Multiplicacao.ToString().Substring(1, 1)); };
                Somatorio += Multiplicacao;
                if (Peso == 1) { Peso = 2; } else { Peso = 1; };
            }
            AuxiliarDezenaSuperior = Somatorio;
            while ((AuxiliarDezenaSuperior % 10) != 0)
            {
                AuxiliarDezenaSuperior += 1;
            }
            return (AuxiliarDezenaSuperior - Somatorio).ToString();
        }

        public static string CalcularMod11(String Valor)
        {
            int Somatorio = 0;
            int Resto = 0;
            int Peso = 2;
            int Multiplicacao = 0;
            int Contador = 0;
            for (Contador = Valor.Length - 1; Contador >= 0; Contador--)
            {
                Multiplicacao = int.Parse(Valor[Contador].ToString()) * Peso;
                if (Multiplicacao > 9) { Multiplicacao = int.Parse(Multiplicacao.ToString().Substring(0, 1)) + int.Parse(Multiplicacao.ToString().Substring(1, 1)); };
                Somatorio += Multiplicacao;
                if (Peso == 1) { Peso = 2; } else { Peso = 1; };
            }
            int digito = 0;
            Resto = (Somatorio % 11);
            if (Resto == 10) { digito = 1; }
            else if ((Resto == 0) || (Resto == 1)) { digito = 0; }
            else digito = 11 - Resto;
            return digito.ToString();
        }

    }
}