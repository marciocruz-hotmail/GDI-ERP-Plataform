using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstGerarNFCompraExterior
    {
        public int id_movimento { get; set; }
        public int id_nfe_status { get; set; }
        public string descricao_nfe_status { get; set; }
        public bool validado { get; set; }
        public bool nfe_gerada { get; set; }
        public string msg { get; set; }
        public string chaveAcesso { get; set; }
        public string linkDanfe { get; set; }
        public string linkDownloadXML { get; set; }
    }
}