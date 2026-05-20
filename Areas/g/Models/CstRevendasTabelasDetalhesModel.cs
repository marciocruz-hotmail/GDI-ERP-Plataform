using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GdiPlataform.Areas.g.Models
{
    public class CstRevendasTabelasDetalhesModel
    {
        public int? id_consulta_tabela_revenda { get; set; }
        public int id_consulta { get; set; }
        public string nome { get; set; }
        public decimal? valor_unit { get; set; }
        public decimal? valor_base { get; set; }
    }
}