using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstFinanceiroImpostos
    {
        public decimal iss_percentual { get; set; }
        public decimal iss_display { get; set; }
        public decimal iss_valor { get; set; }
        public decimal ir_percentual { get; set; }
        public decimal ir_display { get; set; }
        public decimal ir_valor { get; set; }
        public decimal pis_percentual { get; set; }
        public decimal pis_display { get; set; }
        public decimal pis_valor { get; set; }
        public decimal cofins_percentual { get; set; }
        public decimal cofins_display { get; set; }
        public decimal cofins_valor { get; set; }
        public decimal csll_percentual { get; set; }
        public decimal csll_display { get; set; }
        public decimal csll_valor { get; set; }
        public decimal pcc_percentual { get; set; }
        public decimal pcc_display { get; set; }
        public decimal pcc_valor { get; set; }
        public decimal inss_percentual { get; set; }
        public decimal inss_display { get; set; }
        public decimal inss_valor { get; set; }
        public cstFinanceiroImpostos()
        {
            iss_percentual = 0;
            iss_display = 0;
            iss_valor = 0;
            ir_percentual = 0;
            ir_display = 0;
            ir_valor = 0;
            pis_percentual = 0;
            pis_display = 0;
            pis_valor = 0;
            cofins_percentual = 0;
            cofins_display = 0;
            cofins_valor = 0;
            csll_percentual = 0;
            csll_display = 0;
            csll_valor = 0;
            pcc_percentual = 0;
            pcc_display = 0;
            pcc_valor = 0;
            inss_percentual = 0;
            inss_display = 0;
            inss_valor = 0;
        }
    }
}