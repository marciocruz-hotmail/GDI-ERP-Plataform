using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstNfeIcmsTotal
    {
        public decimal vBC { get; set; }
        public decimal vICMS { get; set; }
        public decimal vICMSDeson { get; set; }
        public decimal vFCP { get; set; }
        public decimal vBCST { get; set; }
        public decimal vST { get; set; }
        public decimal vFCPST { get; set; }
        public decimal vFCPSTRet { get; set; }
        public decimal vProd { get; set; }
        public decimal vFrete { get; set; }
        public decimal vSeg { get; set; }
        public decimal vDesc { get; set; }
        public decimal vII { get; set; }
        public decimal vIPI { get; set; }
        public decimal vIPIDevol { get; set; }
        public decimal vPIS { get; set; }
        public decimal vCOFINS { get; set; }
        public decimal vOutro { get; set; }
        public decimal vNF { get; set; }
        public decimal vTotTrib { get; set; }

        public cstNfeIcmsTotal()
        {
            vBC = 0;
            vICMS = 0;
            vICMSDeson = 0;
            vFCP = 0;
            vBCST = 0;
            vST = 0;
            vFCPST = 0;
            vFCPSTRet = 0;
            vProd = 0;
            vFrete = 0;
            vSeg = 0;
            vDesc = 0;
            vII = 0;
            vIPI = 0;
            vIPIDevol = 0;
            vPIS = 0;
            vCOFINS = 0;
            vOutro = 0;
            vNF = 0;
            vTotTrib = 0;
        }
    }
}