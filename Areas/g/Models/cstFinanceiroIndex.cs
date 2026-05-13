using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstFinanceiroIndex
    {
        public int FinanceiroIndex_id_cliente { get; set; }
        public int FinanceiroIndex_id_financeiro_status { get; set; }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> FinanceiroIndex_data1 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public Nullable<System.DateTime> FinanceiroIndex_data2 { get; set; }
    }
}