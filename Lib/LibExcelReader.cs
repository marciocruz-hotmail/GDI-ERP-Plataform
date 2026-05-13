using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{
    public static class LibExcelReader
    {
        public static String GetStringCellXlsx(Object Cell)
        {
            String Resultado = String.Empty;
            try
            {
                if (Cell != null)
                {
                    Resultado = Convert.ToString(Cell);
                    Resultado = Resultado.Trim().ToUpperInvariant();
                }
            }
            catch { }
            return Resultado;
        }

        public static String GetDecimalCellXlsx(Object Cell)
        {
            String TextoEntrada = String.Empty;
            String Resultado = String.Empty;
            String Template = "0123456789,";
            try
            {
                if (Cell != null)
                {
                    TextoEntrada = Convert.ToString(Cell);
                    TextoEntrada = TextoEntrada.Trim();
                    TextoEntrada = TextoEntrada.ToUpperInvariant();
                    if ((TextoEntrada.IndexOf(",") >= 0) && (TextoEntrada.IndexOf(".") >= 0 )) // 000,000.00
                    {
                        TextoEntrada = TextoEntrada.Replace(",", "");
                        TextoEntrada = TextoEntrada.Replace(".", ",");
                    }
                    else if ((TextoEntrada.IndexOf(",") >= 0) && (TextoEntrada.IndexOf(".") == -1 )) // 000000,00
                    {

                    }
                    else if ((TextoEntrada.IndexOf(",") == -1) && (TextoEntrada.IndexOf(".") >= 0 )) // 000000.00
                    {
                        TextoEntrada = TextoEntrada.Replace(".", ",");
                    }
                    for (int i = 0; i < TextoEntrada.Length; i++) { if (Template.IndexOf(TextoEntrada[i].ToString()) > -1) { Resultado += TextoEntrada[i].ToString(); } }
                }
            }
            catch { }
            return Resultado;
        }

        public static String GetWeightCellXlsx(Object Cell)
        {
            String TextoEntrada = String.Empty;
            String Resultado = String.Empty;
            String Template = "0123456789,";
            try
            {
                if (Cell != null)
                {
                    TextoEntrada = Convert.ToString(Cell);
                    TextoEntrada = TextoEntrada.Trim();
                    TextoEntrada = TextoEntrada.ToUpperInvariant();
                    //TextoEntrada = TextoEntrada.Replace(",", "");
                    //TextoEntrada = TextoEntrada.Replace(".", ",");
                    TextoEntrada = TextoEntrada.Replace(".", "");
                    for (int i = 0; i < TextoEntrada.Length; i++) { if (Template.IndexOf(TextoEntrada[i].ToString()) > -1) { Resultado += TextoEntrada[i].ToString(); } }
                }
            }
            catch { }
            return Resultado;
        }

        public static String GetStringCellXls(String Cell)
        {
            String Resultado = String.Empty;
            try
            {
                if (Cell != null)
                {
                    Resultado = Convert.ToString(Cell);
                    Resultado = Resultado.Trim().ToUpperInvariant();
                }
            }
            catch { }
            return Resultado;
        }

        public static String GetNumericCellXls(String Cell)
        {
            String TextoEntrada = String.Empty;
            String Resultado = String.Empty;
            String Template = "0123456789";
            try
            {
                if (Cell != null)
                {
                    TextoEntrada = Convert.ToString(Cell);
                    TextoEntrada = TextoEntrada.EmptyIfNull().Trim().ToUpperInvariant();
                    for (int i = 0; i < Template.Length; i++)
                    {
                        if (Template.IndexOf(TextoEntrada[i].ToString()) > -1)
                        {
                            Resultado += TextoEntrada[i].ToString();
                        }
                    }
                }
            }
            catch { }
            return Resultado;
        }


        public static String GetDecimalCellXls(String Cell)
        {
            String TextoEntrada = String.Empty;
            String Resultado = String.Empty;
            String Template = "0123456789";
            String SeparadorDecimal = String.Empty;
            try
            {
                if (Cell != null)
                {
                    TextoEntrada = Convert.ToString(Cell);
                    TextoEntrada = TextoEntrada.EmptyIfNull().Trim();
                    TextoEntrada = TextoEntrada.ToUpperInvariant();
                    for (int i = 0; i < TextoEntrada.Length; i++) { if (Template.IndexOf(TextoEntrada[i].ToString()) > -1) { Resultado += TextoEntrada[i].ToString(); } }
                }
            }
            catch { }
            return Resultado;
        }

        /*public static String GetWeigthCellXls(String Cell)
        {
            String TextoEntrada = String.Empty;
            String Resultado = String.Empty;
            String Template = "0123456789,";
            try
            {
                if (Cell != null)
                {
                    TextoEntrada = TextoEntrada.Replace(",", "");
                    TextoEntrada = TextoEntrada.Replace(".", ",");
                    TextoEntrada = Convert.ToString(Cell);
                    TextoEntrada = TextoEntrada.Trim();
                    TextoEntrada = TextoEntrada.ToUpperInvariant();
                    for (int i = 0; i < TextoEntrada.Length; i++) { if (Template.IndexOf(TextoEntrada[i].ToString()) > -1) { Resultado += TextoEntrada[i].ToString(); } }
                }
            }
            catch { }
            return Resultado;
        }*/


        public static String GetWeightCellXls(String Cell)
        {
            String TextoEntrada = String.Empty;
            String Resultado = String.Empty;
            String Template = "0123456789,";
            try
            {
                if (Cell != null)
                {
                    TextoEntrada = Convert.ToString(Cell);
                    TextoEntrada = TextoEntrada.Trim();
                    TextoEntrada = TextoEntrada.ToUpperInvariant();
                    TextoEntrada = TextoEntrada.Replace(",", "");
                    TextoEntrada = TextoEntrada.Replace(".", ",");
                    for (int i = 0; i < TextoEntrada.Length; i++) { if (Template.IndexOf(TextoEntrada[i].ToString()) > -1) { Resultado += TextoEntrada[i].ToString(); } }
                }
            }
            catch { }
            return Resultado;
        }



    }
}