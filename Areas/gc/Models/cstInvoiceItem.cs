using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstInvoiceItem
    {
        public string indexItem { get; set; }
        public int pagina { get; set; }
        public string qtd { get; set; }
        public string produto_codigo { get; set; }
        public string produto_nome { get; set; }
        public string valor_unit { get; set; }
        public string valor_total { get; set; }
        public string valor_total_core_charge { get; set; }
        public string cd { get; set; }
        public string delivery { get; set; }
    }
}