using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class cstDadosTituloFinanceiroEdicao
    {
        public int id_financeiro { get; set; }
        public int id_cliente { get; set; }
        public String data_vencimento { get; set; }
        public decimal valor_encargos { get; set; }
    }
}