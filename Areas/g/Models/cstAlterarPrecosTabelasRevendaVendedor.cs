using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class CstAlterarPrecosTabelasRevendaVendedor
    {
        public int id_consulta_tabela { get; set; }
        public int tipo_aplicado { get; set; }      // por valor igual 1 ou por percetual igual 2
        public decimal valor_aplicado { get; set; }

    }
}