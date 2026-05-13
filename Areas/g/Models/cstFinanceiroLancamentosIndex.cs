using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstFinanceiroLancamentosIndex
    {
        public int LancamentosIndex_id_cliente { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> LancamentosIndex_data1 { get; set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")] 
        public Nullable<System.DateTime> LancamentosIndex_data2 { get; set; }
    }
}