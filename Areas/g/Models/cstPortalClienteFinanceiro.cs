using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstPortalClienteFinanceiro
    {
        public int id_financeiro { get; set; }
        public string data_processamento { get; set; }
        public string data_vencimento { get; set; }
        public string descricao { get; set; }
        public string numero_documento { get; set; }
        public string valor_total { get; set; }
        public bool has_boleto { get; set; }
        public string link_boleto { get; set; }
        public bool has_nota_debito { get; set; }
        public string link_nota_debito { get; set; }
        public bool has_nota_fiscal { get; set; }
        public string link_nota_fiscal { get; set; }
        public bool nota_fiscal_autorizada { get; set; }
        public bool nota_fiscal_cancelada { get; set; }
    }
}