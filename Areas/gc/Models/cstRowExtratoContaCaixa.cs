using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.gc.Models
{
    public class cstRowExtratoContaCaixa
    {
        public int id_conta_caixa { get; set; }
        public DateTime data_pagamento { get; set; }
        public Decimal total_pago { get; set; }
        public Decimal total_recebido { get; set; }

        public Decimal saldo_dia { get; set; }


    }
}