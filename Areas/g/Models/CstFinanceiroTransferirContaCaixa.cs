using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class CstFinanceiroTransferirContaCaixa
    {
        [Display(Name = "Conta Caixa")]
        public int id_conta_caixa { get; set; }

        [Display(Name = "Data Vencimento")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime data_vencimento { get; set; }

        [Display(Name = "Calcular Juros?")]
        public bool calcular_juros { get; set; }

        [Display(Name = "Calcular Multa?")]
        public bool calcular_multa { get; set; }
    }
}