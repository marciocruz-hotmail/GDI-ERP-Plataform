using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{
    public static class LibNumbers
    {
        public static Decimal ConvertDecimal(string valueTemp)
        {
            valueTemp = valueTemp.Replace(".", ",");
            decimal resultado = 0;
            decimal.TryParse(valueTemp, out resultado);
            if (resultado > 0) { resultado = Math.Round(resultado, 2); };
            return resultado;
        }

        public static Decimal ConvertMoney(string valueTemp)
        {
            valueTemp = valueTemp.Replace(".", "");
            valueTemp = valueTemp.Replace(",", "");
            decimal resultado = 0;
            decimal.TryParse(valueTemp, out resultado);
            if (resultado > 0) 
            {
                resultado = resultado / 100;
                resultado = Math.Round(resultado, 2); 
            };
            return resultado;
        }


        public static int ConvertInt(string valueTemp)
        {
            int resultado = 0;
            int.TryParse(valueTemp, out resultado);
            return resultado;
        }

        public static decimal TruncateDecimal(decimal number, int digits)
        {
            decimal stepper = (decimal)(Math.Pow(10.0, (double)digits));
            int temp = (int)(stepper * number);
            return (decimal)temp / stepper;
        }

        public static bool IsValidDecimal(string numero)
        {
            bool validado = false;
            try
            {
                if (numero.EmptyIfNull().ToString().Trim().Length > 0)
                {
                    decimal saida = -1;
                    decimal.TryParse(numero, out saida);
                    if (saida > -1) { validado = true; };
                }

            }
            catch (Exception)
            {
                validado = false;
            }
            return validado;
        }

        public static decimal getValidDecimal(string numero)
        {
            decimal decimalSaida = 0;
            if (numero.EmptyIfNull().ToString().Trim().Length > 0)
            {
                decimal.TryParse(numero, out decimalSaida);
            }
            return decimalSaida;
        }

        public static bool IsValidInteger(string numero)
        {
            bool validado = false;
            try
            {
                if (numero.EmptyIfNull().ToString().Trim().Length > 0)
                {
                    Int64 saida = -1;
                    Int64.TryParse(numero, out saida);
                    if (saida > -1) { validado = true; };
                }

            }
            catch (Exception)
            {
                validado = false;
            }
            return validado;
        }

        public static string IntegerToJson(int numero)
        {
            return numero.ToString().Trim().Replace(".", "").Replace(",", ".");
        }

        public static string DecimalToJson(decimal numero)
        {
            return string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", numero).Replace("R$", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
        }

        public static double DecimalToDoublePercent(decimal NumeroDecimal)
        {
            Double Resultado = 0;
            String DecimalStr = string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:C}", NumeroDecimal).Replace("R$", "");
            Double.TryParse(DecimalStr, out Resultado);
            if (Resultado > 0) { Resultado = Resultado / 100; };
            return Resultado;
        }


    }
}