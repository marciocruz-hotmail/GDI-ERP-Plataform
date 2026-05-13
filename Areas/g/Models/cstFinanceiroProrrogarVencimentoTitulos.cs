using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstFinanceiroProrrogarVencimentoTitulos
    {
        public int id_financeiro { get; set; }
        public bool ativo { get; set; }
        public bool juros_multas_automatico { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> data_vencimento { get; set; }
        public decimal juros_multas_valor { get; set; }
        public decimal descontos_valor { get; set; }
    }
}