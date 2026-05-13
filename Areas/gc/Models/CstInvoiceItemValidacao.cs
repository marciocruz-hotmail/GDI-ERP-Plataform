using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class CstInvoiceItemValidacao
    {
        public int id_invoice_item { get; set; }
        public int id_comex_produto { get; set; }
        public int id_invoice { get; set; }
        public int id_importacao { get; set; }
        public string pn { get; set; }
        public string pn_auxiliar { get; set; }
        public string pn_variacao1 { get; set; }
        public string pn_variacao2 { get; set; }
        public string description { get; set; }
        public string traducao { get; set; }
        public string invoice_nome { get; set; }
        public bool validado_planilha { get; set; }

        public CstInvoiceItemValidacao()
        {
            id_invoice_item = 0;
            id_comex_produto = 0;
            id_invoice = 0;
            id_importacao = 0;
            pn = String.Empty;
            pn_auxiliar = String.Empty;
            pn_variacao1 = String.Empty;
            pn_variacao2 = String.Empty;
            description = String.Empty;
            traducao = String.Empty;
            invoice_nome = String.Empty;
            validado_planilha = false;
        }
    }
}
