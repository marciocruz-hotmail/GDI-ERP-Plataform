using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstNfeEmitente
    {
        public string CNPJ { get; set; }
        public string xNome { get; set; }
        public string xFant { get; set; }
        public string IE { get; set; }
        public string xLgr { get; set; }
        public string nro { get; set; }
        public string xBairro { get; set; }
        public string xMun { get; set; }
        public string UF { get; set; }
        public string CEP { get; set; }
        public string fone { get; set; }
        public CstNfeEmitente()
        {
            CNPJ = string.Empty;
            xNome = string.Empty;
            xFant = string.Empty;
            IE = string.Empty;
            xLgr = string.Empty;
            nro = string.Empty;
            xBairro = string.Empty;
            xMun = string.Empty;
            UF = string.Empty;
            CEP = string.Empty;
            fone = string.Empty;
        }
    }
}