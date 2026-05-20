using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Lib
{
    public static class LibCnabBancario
    {
        public static string GetCodigoBarras(String LinhaDigitavel)
        {
            string CodigoBarras = string.Empty;
            LinhaDigitavel = LibStringFormat.SomenteNumeros(LinhaDigitavel);
            CodigoBarras = LinhaDigitavel.Substring(0, 4) + LinhaDigitavel.Substring(32, 15) + LinhaDigitavel.Substring(4, 5) + LinhaDigitavel.Substring(10, 6) + LinhaDigitavel.Substring(16, 4) + LinhaDigitavel.Substring(21, 10);
            return CodigoBarras;
        }
    }
}