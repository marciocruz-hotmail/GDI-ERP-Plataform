using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstNfeAutorizacao
    {
        public string chNFe { get; set; }
        public string dhRecbto { get; set; }
        public string nProt { get; set; }
        public string digVal { get; set; }
        public string cStat { get; set; }
        public string xMotivo { get; set; }

        public cstNfeAutorizacao()
        {
            chNFe = string.Empty;
            dhRecbto = string.Empty;
            nProt = string.Empty;
            digVal = string.Empty;
            cStat = string.Empty;
            xMotivo = string.Empty;
        }
    }
}