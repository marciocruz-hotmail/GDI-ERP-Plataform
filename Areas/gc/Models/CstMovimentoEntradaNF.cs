using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstMovimentoEntradaNF
    {
        public int id_movimento { get; set; }
        public string cliente_nome { get; set; }
        public string movimento_nf { get; set; }
        public string movimento_data { get; set; }
        public bool faturado { get; set; }
        public bool movimento_permitido { get; set; }
        public string msg_erro { get; set; }
        public string url_danfe { get; set; }
        public List<CstMovimentoEntradaNFItem> allItens { get; set; }
        public CstMovimentoEntradaNF()
        {
            movimento_permitido = true;
            allItens = new List<CstMovimentoEntradaNFItem>();
        }
    }
}