using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace GdiPlataform.Lib
{
    public class LibXML
    {
        public static bool HasFirstChildValue(XmlElement Elemento)
        {
            bool Validado = false;
            try
            {
                if (Elemento.HasChildNodes)
                {
                    if (Elemento.FirstChild.Value != null)
                    {
                        if (Elemento.FirstChild.Value.EmptyIfNull().ToString().Length > 0) { Validado = true; };
                    }
                }
            }
            catch (Exception) {}
            return Validado;
        }
    }
}